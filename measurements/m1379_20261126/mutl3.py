import sys,subprocess
k=sys.argv[1]
pat={'l3':'Instruments.LawstepSideBegin(w);','l3e':'Instruments.LawstepSideEnd(w, 4);'}[k]
p='D:/PoB_work/c146kt_work/mut/PriceOfBread.SimCore/Politics.cs'
L=open(p,'rb').read().decode('utf-8').split('\n')
n=0
for i,l in enumerate(L):
    if pat in l and 'LawstepSide' in l and l.lstrip().startswith('Instruments.'):
        L[i]=''; n+=1
assert n==1,n
open(p,'wb').write('\n'.join(L).encode('utf-8'))
r=subprocess.run(['dotnet','test','D:/PoB_work/c146kt_work/mut/PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/c146kt_art_mut','--filter','FullyQualifiedName~LawstepInstrumentTests'],capture_output=True)
out=r.stdout.decode('cp932',errors='replace')
for l in out.splitlines():
    if '!' in l and 'dll' in l or l.strip().startswith(('失敗 ','Failed ')) or 'error CS' in l: print(k,l.encode('ascii','replace').decode()[:200])
