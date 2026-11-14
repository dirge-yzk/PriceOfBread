# (1137) c111rb 突然変異の台本：使い捨ての木（git archive の写し）に 1 本ずつ変異を入れ、ビルドして機構の試験（RailByRegionTests・RailNeedByOutputTests）を回し、どの試験が赤かを表にする。
# 使い方: python -X utf8 -I mutate_rb.py <使い捨ての木> <成果物のフォルダ> <出力の表> [名の並び BASE,M1,…]   ・変異ごとに元のバイトへ戻してから次へ。
import sys, os, subprocess, re
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
S = 'PriceOfBread.SimCore/'
SH = 'w.Coef.RailNeedByOutput > 0 ? (b.TotalPot > 1e-9 ? r.GdpOutputMemo / b.TotalPot : b.Even) : RegionShare(b, r);'
MUT = [
    ('BASE', S + 'Transport.cs', 'namespace PriceOfBread.SimCore;', 'namespace PriceOfBread.SimCore;', '変異なし'),
    ('F1', S + 'Coefficients.p04_trade.cs', '    public double RailNeedByOutput;', '    public double RailNeedByOutput = 1.0;', '旗の既定 1（変異ではない・旗 1 の木の試験の束の先読み）'),
    ('M1', S + 'Transport.cs', SH, 'RegionShare(ShareBasis(w, w.Coef), r);', 'G-W・G-F'),
    ('M2', S + 'BilateralMarket.cs', 'Transport.RailNeedShare(wR, (railEvenN[nt0], railPotN[nt0]), rr)', 'Transport.RegionShare(Transport.ShareBasis(wR, wR.Coef), rr)', 'G-F（清算）'),
    ('M3', S + 'Transport.cs', 'double need = target * RailNeedShare(w, basis, r);', 'double need = target * RegionShare(ShareBasis(w, c), r);', 'G-F（敷き）・G-W'),
    ('M4', S + 'Transport.cs', SH, SH.replace('r.GdpOutputMemo / b.TotalPot', 'r.GdpOutputMemo'), 'G-F・G-φ'),
    ('M5', S + 'Transport.cs', SH, SH.replace('r.GdpOutputMemo / b.TotalPot', 'r.SumPopulation() / w.TotalPopulation'), 'G-W 下限・G-F'),
    ('M6', S + 'Transport.cs', 'c.TransportDecay * System.Math.Min(tPre, need);', 'c.TransportDecay * tPre;', 'G7'),
    ('M8', S + 'World.cs', 'if (byOut) r.GdpOutputMemo = gr;', 'if (byOut) r.GdpOutputMemo = 12.0 * gr;', 'G-F・G-φ'),
    ('M9', S + 'Transport.cs', 'w.GdpPerMonth.Value) : ShareBasis(w, c);', '4.0 * w.GdpPerMonth.Value) : ShareBasis(w, c);', 'G-F・G-φ 下限（世界の和の代わりに 4 国の盤面で ×4）'),
    ('M10', S + 'World.cs', '                    if (good is null || good.Tier == GoodTier.Service) continue;\n                    g += s.Output * good.RefPrice;\n                    if (byOut) gr += s.Output * good.RefPrice;',
     '                    if (byOut && good is not null) gr += s.Output * good.RefPrice;\n                    if (good is null || good.Tier == GoodTier.Service) continue;\n                    g += s.Output * good.RefPrice;', 'G-F'),
]
only = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None
rows = []
env = dict(os.environ)
for name, f, old, new, gate in MUT:
    if only is not None and name not in only: continue
    p = os.path.join(tree, f)
    b = open(p, 'rb').read()
    crlf = b'\r\n' in b
    o, n = old.encode('utf-8'), new.encode('utf-8')
    if crlf: o, n = o.replace(b'\n', b'\r\n'), n.replace(b'\n', b'\r\n')
    if b.count(o) != 1:
        rows.append((name, gate, f'変異を入れられない（一致 {b.count(o)}）', '')); continue
    open(p, 'wb').write(b.replace(o, n))
    try:
        r = subprocess.run(['dotnet', 'build', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--disable-build-servers'],
                           capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        if r.returncode != 0:
            rows.append((name, gate, 'ビルド失敗', '')); continue
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~RailByRegionTests|FullyQualifiedName~RailNeedByOutputTests'], capture_output=True, text=True, encoding='utf-8', errors='replace', env=env)
        fails = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.(\w+\.[^\s\[]+(?:\(flag: \d\))?)', r.stdout)))
        tot = re.findall(r'(失敗!|成功!)[^\n]*', r.stdout)
        rows.append((name, gate, '赤' if fails else '緑', ' / '.join(fails) + ' ' + (tot[-1] if tot else '')))
    finally:
        open(p, 'wb').write(b)
    with open(out, 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('変異\t予言の門\t試験の束\t赤になった試験・合計\n')
        for row in rows: fo.write('\t'.join(row) + '\n')
print(open(out, encoding='utf-8').read())
