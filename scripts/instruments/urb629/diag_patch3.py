import sys
root=sys.argv[1]
p=root+'/PriceOfBread.SimCore/Simulation.cs'
b=open(p,'rb').read()
nl=b'\r\n' if b'\r\n' in b else b'\n'
anchor='        if (!ConservationProbe) return;\n'.replace('\n',nl.decode()).encode('utf-8')
assert b.count(anchor)==1, b.count(anchor)
bs=chr(92)
diag=('        if (System.Environment.GetEnvironmentVariable("POB_DIAG_STG") is { } dgs) foreach (var rr in World.Regions) if (rr.Name == "アムステルダム")\n'
 '        { double s1 = 0, s2 = 0; foreach (var q in rr.Pops) { if (q.IsSoldier) s2 += q.Size; else s1 += q.Size; }\n'
 '          System.IO.File.AppendAllText(dgs, label + "'+bs+'t" + World.Tick + "'+bs+'t" + s1.ToString("R") + "'+bs+'t" + s2.ToString("R") + "'+bs+'n"); }\n').encode('utf-8')
diag=diag.replace(b'\n',nl)
b=b.replace(anchor,diag+anchor)
open(p,'wb').write(b)
print('patched',p)
