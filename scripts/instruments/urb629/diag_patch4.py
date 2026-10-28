import sys
root=sys.argv[1]
p=root+'/PriceOfBread.SimCore/World.cs'
b=open(p,'rb').read()
nl=b'\r\n' if b'\r\n' in b else b'\n'
anchor=('        if (!(total > 0) || pops.Count == 0) return 0;'+'\n        double sw = 0;').replace('\n',nl.decode()).encode('utf-8')
assert b.count(anchor)==1, b.count(anchor)
bs=chr(92)
diag=('        if (System.Environment.GetEnvironmentVariable("POB_DIAG_KILL") is { } dgk && total > 0 && pops.Count > 0) foreach (var rr in Regions) if (rr.Name == "アムステルダム" && rr.Pops.Contains(pops[0]))\n'
 '        { var st = new System.Diagnostics.StackTrace(1, false); string cs = ""; for (int i = 0; i < 3 && i < st.FrameCount; i++) cs += st.GetFrame(i)!.GetMethod()!.Name + ">";\n'
 '          System.IO.File.AppendAllText(dgk, Tick + "'+bs+'t" + cs + "'+bs+'t" + total.ToString("R") + "'+bs+'t" + violent + "'+bs+'n"); }\n').encode('utf-8')
diag=diag.replace(b'\n',nl)
b=b.replace(anchor,diag+anchor)
open(p,'wb').write(b)
print('patched',p)
