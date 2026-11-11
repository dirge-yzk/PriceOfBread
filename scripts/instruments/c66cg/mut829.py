"""(829) 突然変異の台本（使い捨ての木で 1 本ずつ当て、CearnInstrumentTests を回して赤と緑を書く）。
使い方: python -I mut829.py <使い捨ての木> <隔離ビルドのフォルダ> <出力.txt>"""
import sys, os, subprocess
tree, art, outp = sys.argv[1], sys.argv[2], sys.argv[3]
F = os.path.join(tree, 'PriceOfBread.SimCore', 'InstrumentsCearn.cs')
M = [
 ('M1 群の振り分けを入れ替える（closed と modernElite）', 'p.IsSoldier ? 3 : p.Closed ? 2 : p.IsModernElite ? 1 : 0;', 'p.IsSoldier ? 3 : p.Closed ? 1 : p.IsModernElite ? 2 : 0;'),
 ('M2 兵士を外す（兵士を mass へ）', 'p.IsSoldier ? 3 : p.Closed ? 2 : p.IsModernElite ? 1 : 0;', 'p.Closed ? 2 : p.IsModernElite ? 1 : 0;'),
 ('M3 月初の在庫を月の終わりで読む', 'double end = p.Savings.Value;', 'double end = p.Savings.Value; cw.Begin = end;'),
 ('M4 空の印の比較を逆にする', 'claim > 0 && end <= CgEmptyShare', 'claim > 0 && end > CgEmptyShare'),
 ('M5 門の向きを逆にする', 'GetEnvironmentVariable("POB_CEARN") != "0"', 'GetEnvironmentVariable("POB_CEARN") == "0"'),
 ('M6 人頭税に閉鎖階級を入れる', 'level > 1e-9 && !p.Closed && !p.IsSoldier', 'level > 1e-9 && !p.IsSoldier'),
 ('M7 群の流れを足さない（精算以外）', '        ga.Flow[(int)col] += isIn ? delta : -delta;\n', ''),
 ('M8 月の窓の書き込みを数えない', 'if (CgPops.TryGetValue(p, out var cw) && cw.InWin) cw.Win += delta;', 'if (CgPops.TryGetValue(p, out var cw) && cw.InWin) cw.Win += 0;'),
]
orig = open(F, 'rb').read()
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('(829) 突然変異（木 %s・CearnInstrumentTests 9 本）\n' % tree)
env = dict(os.environ, AVALONIA_TELEMETRY_OPTOUT='1')
csproj = os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')
for name, a, b in M:
    a, b = a.encode(), b.encode()
    if orig.count(a) != 1: o.write('%s：当てられない（元の文が %d 回）\n' % (name, orig.count(a))); continue
    open(F, 'wb').write(orig.replace(a, b))
    try:
        r = subprocess.run(['dotnet', 'build', csproj, '-c', 'Release', '--artifacts-path', art, '--disable-build-servers', '-v', 'q'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        if r.returncode != 0: o.write('%s：ビルドの失敗\n' % name); continue
        r = subprocess.run(['dotnet', 'test', csproj, '-c', 'Release', '--no-build', '--artifacts-path', art, '--filter', 'FullyQualifiedName~CearnInstrumentTests', '--logger', 'console;verbosity=normal'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        txt = (r.stdout or '') + chr(10) + (r.stderr or '')
        fails = sorted({l.split('CearnInstrumentTests.')[1].split(' ')[0] for l in txt.splitlines() if 'CearnInstrumentTests.' in l and ('[FAIL]' in l or l.strip().startswith('失敗 ') or l.strip().startswith('Failed '))})
        summ = [l.strip() for l in r.stdout.splitlines() if '合計:' in l]
        o.write('%s：%s・赤 %s\n' % (name, summ[-1] if summ else '（集計の行なし）', '・'.join(fails) if fails else 'なし（緑のまま）'))
    finally:
        open(F, 'wb').write(orig)
    o.flush()
o.close(); print(open(outp, encoding='utf-8').read())
