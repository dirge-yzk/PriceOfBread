import subprocess
p='PriceOfBread.SimCore/MultiNationSim.cs'
orig=open(p,'rb').read().decode('utf-8')
muts={
 'mu1':('pw.ForeignDebt += World.CrossFx(rw, pw, rw.ForeignDebt, book: false); ',''),
}
out=[]
try:
  for k,(a,b) in muts.items():
    assert orig.count(a)==1,(k,orig.count(a))
    open(p,'wb').write(orig.replace(a,b).encode('utf-8'))
    r=subprocess.run('dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c127ex_art --filter "FullyQualifiedName~DissolveNationTests"',shell=True,capture_output=True)
    t=r.stdout.decode('utf-8','replace')
    line=[l.strip() for l in t.splitlines() if 'Assert' in l or 'error' in l.lower() or 'Expected' in l or 'Actual' in l or 'Assert.' in l][:3]
    out.append(k+' rc=%d '%r.returncode+' | '.join(line)[:400])
finally:
  open(p,'wb').write(orig.encode('utf-8'))
open('measurements/m1248_20261120_mutations_ex_mu1.txt','w',encoding='utf-8').write('\n'.join(out)+'\n')
