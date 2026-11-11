# (944) c83hd プローブ（読むだけ・決定を変えない・事前登録 measurements/m944_20261107_prereg_hd.txt [12]）
#   python -X utf8 -I probe944.py <使い捨ての木（器の木の写し・旗の既定 0）> <出力フォルダ> <artifacts> [名,名,…]
#   InstrumentsTests.私的な建設の工業化度の累積… の走り（種 1789・20 四半期）を 4 通り（旗 0・旗 1・n1＝旗 0＋SupplySlope 0.50000001・n2＝旗 0＋0.49999999）で回す。
#   試験の本体の中だけを書き換える：①計器の国の絞り（イギリス・フランス）を全ての国 "*" へ（計器は読むだけ）②Snap の頭で四半期・国の数・パリを持つ国・国の名の一覧を書く
#   ③判定の直前に計器のフォルダを <出力>/<名>/ へ写す。試験の判定は変えない。終わりに木のバイトを戻す。
import sys, os, subprocess
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
T = os.path.join(tree, 'PriceOfBread.Tests', 'InstrumentsTests.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
t0 = open(T, 'rb').read(); co0 = open(CO, 'rb').read()
NAME = 'public void 私的な建設の工業化度の累積は建った束と1束の工業化度の積に閉じる()'.encode('utf-8')
i0 = t0.index(NAME); i1 = t0.index(b'[Fact]', i0)
body = t0[i0:i1]
CONF = b'Instruments.Configure(dir, string.Join(",", picked));'
SNAP = b'            void Snap(int q)\n            {\n'
A = b'            Assert.True(bad.Count == 0, string.Join(" | ", bad.Take(30))'
for k in (CONF, SNAP, A): assert body.count(k) == 1, (k, body.count(k))
VAR = {
    'flag0': (b'0.0', b'0.5'),
    'flag1': (b'1.0', b'0.5'),
    'n1': (b'0.0', b'0.50000001'),
    'n2': (b'0.0', b'0.49999999'),
}
FL = b'public double EmptyPopDebtToHeirs = 0.0;'; SS = b'public double SupplySlope = 0.5;'
assert co0.count(FL) == 1 and co0.count(SS) == 1
names = sys.argv[4].split(',') if len(sys.argv) > 4 else list(VAR)
try:
    for tag in names:
        fl, ss = VAR[tag]
        od = os.path.join(out, tag).replace('\\', '/'); os.makedirs(od, exist_ok=True)
        snapf = od + '/snap.txt'
        if os.path.exists(snapf): os.remove(snapf)
        snapline = ('                { var __o = sim.Nations.FirstOrDefault(n => n.World.Regions.Any(rr => rr.Name == "パリ"));'
                    f' System.IO.File.AppendAllText(@"{snapf}", $"Q{{q}}\\t{{sim.Nations.Count}}\\t{{(__o is null ? "-" : Instruments.NameOf(__o.World))}}\\t{{string.Join(",", sim.Nations.Select(n => Instruments.NameOf(n.World)))}}\\n"); }}\n').encode('utf-8')
        copyline = (f'            foreach (var __f in System.IO.Directory.GetFiles(dir)) System.IO.File.Copy(__f, System.IO.Path.Combine(@"{od}", System.IO.Path.GetFileName(__f)), true);\n').encode('utf-8')
        nb = body.replace(CONF, b'Instruments.Configure(dir, "*");').replace(SNAP, SNAP + snapline).replace(A, copyline + A)
        open(T, 'wb').write(t0[:i0] + nb + t0[i1:])
        open(CO, 'wb').write(co0.replace(FL, b'public double EmptyPopDebtToHeirs = ' + fl + b';').replace(SS, b'public double SupplySlope = ' + ss + b';'))
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~InstrumentsTests.私的な建設の工業化度の累積は建った束と1束の工業化度の積に閉じる'], capture_output=True)
        txt = r.stdout.decode('utf-8', 'replace') + r.stderr.decode('utf-8', 'replace')
        keep = [l for l in txt.splitlines() if ('成功' in l or '失敗' in l or ' error ' in l.lower() or 'Passed!' in l or 'Failed!' in l or 'bad' in l or '／' in l)]
        with open(os.path.join(out, f'test_{tag}.txt'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(f'# (944) プローブ {tag}（旗 {fl.decode()}・SupplySlope {ss.decode()}・rc={r.returncode}）\n' + '\n'.join(keep[:40]) + '\n')
        print(tag, 'rc', r.returncode, flush=True)
finally:
    open(T, 'wb').write(t0); open(CO, 'wb').write(co0)
