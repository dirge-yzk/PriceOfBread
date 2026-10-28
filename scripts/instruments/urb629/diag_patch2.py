import sys
root=sys.argv[1]
p=root+'/PriceOfBread.SimCore/Simulation.cs'
b=open(p,'rb').read()
nl=b'\r\n' if b'\r\n' in b else b'\n'
anchor='            double regBWr = regBW[ri];'.encode('utf-8')
assert b.count(anchor)==1, b.count(anchor)
bs=chr(92)
diag=('            if (System.Environment.GetEnvironmentVariable("POB_DIAG_DEM") is { } dgd && r.Name == "アムステルダム")\n'
 '            { double rs = 0, dwS = 0, sz = 0, dwAll = 0; foreach (var q in r.Pops) if (!q.IsSoldier) { sz += q.Size; dwS += q.Size * AgeMortalityIndex(q, mortW); }\n'
 '              System.IO.File.AppendAllText(dgd, string.Join("'+bs+'t", new object[] { World.Tick, sz.ToString("R"), (regKey[ri]).ToString("R"), massBirthKey.ToString("R"), massPop.ToString("R"), dwS.ToString("R"), massDeathWeight.ToString("R"), totalBirths.ToString("R"), deathRate.ToString("R"), recovering, net.ToString("R"), r.FoodFill.ToString("R") }) + "'+bs+'n"); }\n').encode('utf-8')
diag=diag.replace(b'\n',nl)
b=b.replace(anchor,diag+anchor)
open(p,'wb').write(b)
print('patched',p)
