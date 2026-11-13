# (1097) c105rr 突然変異の台本：使い捨ての木（<木>）に 1 本ずつ変異を入れ、ビルドして機構の試験（RailByRegionTests・KmComponentTests）を回し、どの試験が赤かを表にする。
# 使い方: python -X utf8 -I mutate.py <使い捨ての木> <成果物のフォルダ> <出力の表> [名の並び BASE,M2,…]   ・変異ごとに元へ戻してから次へ（git checkout -- <ファイル>）。
import sys, os, subprocess, re
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
S = 'PriceOfBread.SimCore/'
MUT = [
    ('M1', S + 'BilateralMarket.cs', 'double phi = Transport.RailPhi(rr.RailTrack, railTgtN[nt0] * Transport.RegionShare((railEvenN[nt0], railPotN[nt0]), rr));',
     'double phi = Discovery.RailTransport(nations[nt0].World.Discovered) ? 1.0 : 0.0;', 'G1'),
    ('M2', S + 'Transport.cs', '            mv?.End(r);\n            return 0.0;',
     '            r.RailTrack = from.RailCapacity * sh;\n            mv?.End(r);\n            return 0.0;', 'G4'),
    ('M3', S + 'Transport.cs', 'c.TransportDecay * System.Math.Min(tPre, need);', 'c.TransportDecay * tPre;', 'G7'),
    ('M4', S + 'Transport.cs', 'r.RailTrack += add / terrLay;', 'r.RailTrack += add;', 'G2'),
    ('M5', S + 'BilateralMarket.cs', '                        railRecv[snA] += rS; railRecv[t] += rB;\n                        freightPool += frG - rS - rB;',
     '                        freightPool += frG;', 'G3'),
    ('M6', S + 'Transport.cs', 'return f >= 1.0 ? 1.0 : (f > 0 ? f : 0.0);', 'return f > 0 ? f : 0.0;', 'G1'),
    ('M7', S + 'BilateralMarket.cs', 'bool byReg = railByRegN[i] = W.Coef.RailByRegion > 0;', 'bool byReg = railByRegN[i] = true;', '旗0'),
    ('C1', S + 'BilateralMarket.cs', 'double viaSea = (st & KmStSea) != 0 ? sea + ea + eb : double.PositiveInfinity;',
     'double viaSea = (st & KmStSea) != 0 ? sea + eb + ea : double.PositiveInfinity;', 'Km'),
]
MUT = [('BASE', S + 'Transport.cs', 'namespace PriceOfBread.SimCore;', 'namespace PriceOfBread.SimCore;', '変異なし')] + MUT
only = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None   # 名を選んで回す（例 BASE,M2,M7）
rows = []
for name, f, old, new, gate in MUT:
    if only is not None and name not in only: continue
    p = os.path.join(tree, f)
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    if b.count(o) != 1:
        rows.append((name, gate, f'変異を入れられない（一致 {b.count(o)}）', '')); continue
    open(p, 'wb').write(b.replace(o, n))
    try:
        r = subprocess.run(['dotnet', 'build', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art],
                           capture_output=True, text=True, encoding='utf-8', errors='replace')
        if r.returncode != 0:
            rows.append((name, gate, 'ビルド失敗', '')); continue
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~RailByRegionTests|FullyQualifiedName~KmComponentTests'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        fails = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.(\w+\.[^\s\[]+)', r.stdout)))
        tot = re.findall(r'(失敗!|成功!)[^\n]*', r.stdout)
        rows.append((name, gate, '赤' if fails else '緑', ' / '.join(fails) + ' ' + (tot[-1] if tot else '')))
    finally:
        open(p, 'wb').write(b)
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('# (1097) c105rr 突然変異の表（使い捨ての木・機構の試験 RailByRegionTests と KmComponentTests）\n')
    fo.write('変異\t狙いの門\t結果\t赤の試験と合計\n')
    for r in rows: fo.write('\t'.join(r) + '\n')
print(open(out, encoding='utf-8').read())
