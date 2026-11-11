import sys
src=sys.argv[1]; sd=1
ser={}
with open(f'{src}/rule_s{sd}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
    for line in f:
        c=line.split('\t')
        if c[0]=='quarter': continue
        ser.setdefault((c[ix['nation']],c[ix['region']]),set()).add(int(c[0]))
names={}
for (n,r),qs in ser.items(): names.setdefault(r,[]).append((n,min(qs),max(qs)))
with open(f'{src}/cederule_s{sd}.tsv',encoding='utf-8') as f:
    h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
    k=0
    for line in f:
        c=line.rstrip('\n').split('\t')
        if c[0]=='quarter': continue
        q=int(c[0]); rcv=c[ix['receiver']]; reg=c[ix['region']]
        qs=ser.get((rcv,reg),set())
        if (q+1) not in qs and k<12:
            k+=1
            print(c[ix['how']],q,c[ix['sender']],'->',rcv,reg,'own',c[ix['ownerAtQuarterEnd']],'mv',c[ix['movedAgainFlag']],'| has q?',q in qs,'span',(min(qs),max(qs)) if qs else None,'| others',[x for x in names.get(reg,[]) if x[1]<=q+2<=x[2]+2][:4])
