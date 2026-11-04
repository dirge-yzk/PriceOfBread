"""(837) 突然変異の台本（使い捨ての木で 1 本ずつ当て、SubstInstrumentTests と CornintInstrumentTests を回して赤と緑を書く）。
使い方: python -I mut837.py <使い捨ての木> <隔離ビルドのフォルダ> <出力.txt>
当てる先は <木>/PriceOfBread.SimCore/InstrumentsSubst.cs と InstrumentsCornint.cs。元の文が 1 回でなければ当てない。"""
import sys, os, subprocess
tree, art, outp = sys.argv[1], sys.argv[2], sys.argv[3]
SUB = os.path.join(tree, 'PriceOfBread.SimCore', 'InstrumentsSubst.cs')
COR = os.path.join(tree, 'PriceOfBread.SimCore', 'InstrumentsCornint.cs')
M = [
 ('Mu1 能力を産出で読む（D1 の分母を farmProduced に）', SUB, 'double d1 = SubstD1(sold, cap), d2', 'double d1 = SubstD1(sold, produced), d2'),
 ('Mu2 σ に州どうしの流れを足す（国境を越えた分に州の市場の PrevImport・PrevExport を足す）', COR,
  '            double nan = double.NaN;\n', '            foreach (var rr in w.Regions) foreach (var mm in rr.Markets) { aImp += mm.PrevImport; aExp += mm.PrevExport; }\n            double nan = double.NaN;\n'),
 ('Mu3 PaySales の地代を落とす（輸出の地代を数えない）', COR, 'rentExp[b] += rent; rentBg[b, s.GoodId] += rent;', 'rentExp[b] += 0; rentBg[b, s.GoodId] += 0;'),
 ('Mu4 較正の定数を 1.1 に戻す', SUB, 'internal const double SubstCalibration = 0.89;', 'internal const double SubstCalibration = 1.1;'),
 ('Mu5 D1 の clamp を外す', SUB, 'cap > 0 ? System.Math.Clamp(1.0 - sold / cap, 0.0, 1.0) : double.NaN', 'cap > 0 ? 1.0 - sold / cap : double.NaN'),
 ('Mu6 国内の地代の按分を産出の値でなく頭数にする（所有する RGO の数で等分）', COR,
  'double share = rent * v / ownerTot[pi];', 'double share = rent / System.Math.Max(1, r.Producers.Count(x => x.IsRgo && x.OwnerPopIndex == pi));'),
 ('Mu7 ブロックの三項式を入れ替える（近代エリートと大衆）', COR, 'p.Closed ? 0 : p.IsModernElite ? 1 : 2;', 'p.Closed ? 0 : p.IsModernElite ? 2 : 1;'),
 ('Mu8 人頭税の対象に閉鎖階級を入れる', SUB, 'if (pollLevel > 1e-9 && !p.Closed && !p.IsSoldier', 'if (pollLevel > 1e-9 && !p.IsSoldier'),
]
files = {SUB: open(SUB, 'rb').read(), COR: open(COR, 'rb').read()}
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('(837) 突然変異（木 %s・SubstInstrumentTests 4 本＋CornintInstrumentTests 4 本）\n' % tree)
env = dict(os.environ, AVALONIA_TELEMETRY_OPTOUT='1')
csproj = os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')
for name, f, a, b in M:
    a, b = a.encode(), b.encode()
    orig = files[f]
    if orig.count(a) != 1: o.write('%s：当てられない（元の文が %d 回）\n' % (name, orig.count(a))); o.flush(); continue
    open(f, 'wb').write(orig.replace(a, b))
    try:
        r = subprocess.run(['dotnet', 'build', csproj, '-c', 'Release', '--artifacts-path', art, '--disable-build-servers', '-v', 'q'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        if r.returncode != 0: o.write('%s：ビルドの失敗\n' % name); continue
        r = subprocess.run(['dotnet', 'test', csproj, '-c', 'Release', '--no-build', '--artifacts-path', art, '--filter', 'FullyQualifiedName~SubstInstrumentTests|FullyQualifiedName~CornintInstrumentTests', '--logger', 'console;verbosity=normal'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        txt = (r.stdout or '') + chr(10) + (r.stderr or '')
        fails = sorted({l.strip().split('InstrumentTests.')[1].split(' [')[0] for l in txt.splitlines() if ('InstrumentTests.' in l) and l.strip().startswith('失敗 ')})
        summ = [l.strip() for l in r.stdout.splitlines() if '合格:' in l or '失敗:' in l]
        o.write('%s：%s・赤 %s\n' % (name, summ[-1] if summ else '（集計の行なし）', '・'.join(fails) if fails else 'なし（緑のまま）'))
    finally:
        open(f, 'wb').write(orig)
    o.flush()
o.close(); print(open(outp, encoding='utf-8').read())
