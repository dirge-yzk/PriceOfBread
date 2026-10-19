#!/usr/bin/env python
"""(425) 突然変異の記録：本体を 1 か所ずつ壊して PopulationDynamicsTests が赤になることを確かめる（壊す→試験→git checkout で戻す）。
使い方：PYTHONUTF8=1 python mut.py <worktree> <artifacts-path> <出力ファイル>
前提：worktree の SimCore と Tests は全てコミット済み（git checkout で戻すため）。1 変異＝ビルド＋試験で約 1〜2 分。"""
import subprocess, sys, re, os
wt, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
only = sys.argv[4] if len(sys.argv) > 4 else ''   # 名前にこの文字列を含む変異だけ回す
PD = 'PriceOfBread.SimCore/PopulationDynamics.cs'
IP = 'PriceOfBread.SimCore/InstrumentsPopC.cs'
M = [
 ('M1 医療の層別の重みを逆順に', PD, '1.0 / (1.0 + HealthBandWeight[b] * healthTerm)', '1.0 / (1.0 + HealthBandWeight[7 - b] * healthTerm)'),
 ('M2 結婚している割合の分母 34→35', PD, 'internal const double ReproductiveSpanYears = 34.0;', 'internal const double ReproductiveSpanYears = 35.0;'),
 ('M3 手取り÷生活費の符号反転', PD, '- c.PopMarriageRealIncome * Math.Log', '+ c.PopMarriageRealIncome * Math.Log'),
 ('M4 夫婦の中の制限で K を無視', PD, 'return 1.0 - knowledge * (1.0 - Logistic(v));', 'return 1.0 - (1.0 - Logistic(v));'),
 ('M5 カップル律速を外す', PD, 'double couple = Math.Min(f, mm);', 'double couple = f;'),
 ('M6 飢えの死を台帳から引かない', PD, 'w.PopulationDelta += intendedNet + ghostDeaths - famineDead;', 'w.PopulationDelta += intendedNet + ghostDeaths;'),
 ('M7 15 歳の余命の指数を 10→9', PD, 's *= Math.Exp(-10.0 * mu[b]); }', 's *= Math.Exp(-9.0 * mu[b]); }'),
 ('M8 女 15〜49 の層 1 の按分を 0.5→1', PD, 'f = 0.5 * a[3] + a[5]', 'f = a[3] + a[5]'),
 ('M9 計器の初婚の年齢を重みなしで積む', IP, 'a.Age += weight * age;', 'a.Age += age;'),
 ('M10 生活費の主食を市場で買った分だけに', PD, 'staple = p.FoodMet * staplePrice / p.Size;', 'staple = Math.Max(0.0, p.FoodMet - p.Subsistence * p.FoodNeed) * staplePrice / p.Size;'),
 ('M11 自給の値打ちを稼ぎへ足さない', PD, '(earnedLastPerMonth * taxKeep / size + selfSupplyPerCapita) / adultShare', '(earnedLastPerMonth * taxKeep / size) / adultShare'),
 ('M12 死の率に水準を掛けない', PD, 'mu[b] = shape[2 * b] * level * h * sanitationMul * o;', 'mu[b] = shape[2 * b] * h * sanitationMul * o;'),
 ('M13 出生を規模に比例させない（規模の 2 乗）', PD, 'births = size0 * couple * perMonthTfr * mar * cLim;', 'births = size0 * size0 * 0.001 * couple * perMonthTfr * mar * cLim;'),
]
def run(cmd):
    return subprocess.run(cmd, cwd=wt, capture_output=True, text=True, encoding='utf-8', errors='replace')
lines = []
for name, f, old, new in M:
    if only and only not in name: continue
    p = os.path.join(wt, f)
    src = open(p, 'rb').read().decode('utf-8')
    if src.count(old) != 1:
        lines.append(f'{name}\t適用できない（一致 {src.count(old)} 件）'); continue
    open(p, 'wb').write(src.replace(old, new).encode('utf-8'))
    try:
        r = run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~PopulationDynamicsTests'])
        txt = r.stdout + r.stderr
        m = re.search(r'(成功|失敗)!\s+-失敗:\s*(\d+)、合格:\s*(\d+)', txt)
        failed = re.findall(r'失敗 PriceOfBread\.Tests\.PopulationDynamicsTests\.(\S+)', txt)
        if 'error CS' in txt: res = 'ビルドが通らない'
        elif m: res = f'{"赤" if int(m.group(2)) > 0 else "生き残った"}（失敗 {m.group(2)}・合格 {m.group(3)}）' + (' ' + ' / '.join(sorted(set(failed))[:4]) if failed else '')
        else: res = '結果が読めない'
    finally:
        run(['git', 'checkout', '--', f])
    lines.append(f'{name}\t{res}')
    open(out, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
