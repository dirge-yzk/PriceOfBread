import sys
root=sys.argv[1]
p=root+'/PriceOfBread.SimCore/PopDecision.cs'
b=open(p,'rb').read()
nl=b'\r\n' if b'\r\n' in b else b'\n'
anchor='        if (flow > 0) Move(rural, urban, flow);     // 農村 → 都市'.encode('utf-8')
assert b.count(anchor)==1, b.count(anchor)
diag=('        if (System.Environment.GetEnvironmentVariable("POB_DIAG_URB") is { } dg0 && (region.Name == "アムステルダム" || region.Name == "ロンドン" || region.Name == "パリ"))' + '\n'
 '        { // 診断だけ（段0・コミットしない）：自給と貨幣の所得・飢えの門・目標の都市の割合\n'
 '            var k = region.Name; int nn; lock (_diagN) { _diagN.TryGetValue(k, out nn); _diagN[k] = nn + 1; }\n'
 '            double zr = 1.0 / (1.0 + System.Math.Exp(-z));\n'
 '            System.IO.File.AppendAllText(dg0 + "_" + (k == "アムステルダム" ? "ams" : k == "ロンドン" ? "lon" : "par") + ".tsv", string.Join("\t", new object[] { k, nn, rural.Size.ToString("R"), urban.Size.ToString("R"), incRural.ToString("R"), incUrban.ToString("R"), zr.ToString("R"), targetUrban.ToString("R"), curUrban.ToString("R"), flow.ToString("R"), urban.FoodFill.ToString("R"), rural.FoodFill.ToString("R"), rural.Subsistence.ToString("R"), region.ArableLand.ToString("R") }) + "\\n");\n'
 '        }\n').encode('utf-8')
diag=diag.replace(b'\n',nl)
b=b.replace(anchor,diag+anchor)
anc2='    private const double Eps = 1e-9;'.encode('utf-8')
assert b.count(anc2)==1
b=b.replace(anc2,anc2+nl+b'    private static readonly System.Collections.Generic.Dictionary<string,int> _diagN = new();')
open(p,'wb').write(b)
print('patched',p)
