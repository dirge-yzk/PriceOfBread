import sys
f=sys.argv[1]
pos=neg=0; zero_gate=0; n=0; gate_hits=0
cum=[]
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t'); k=int(c[1]); v=[float(x) for x in c[2:]]
    fl=v[7]; 
    if fl>0: pos+=fl
    else: neg+=-fl
    # gate: raw target > cur (would flow +) but flow==0
    if v[5]>v[6]+1e-9 and fl==0.0 and v[8]<0.8 and v[8]<v[9]: gate_hits+=1
    n+=1
    if k in (120,360,720,1200,1560,1980): cum.append((1783+k//12,round(pos,1),round(neg,1)))
print('months',n,'sum rural->urban',round(pos,1),'sum urban->rural',round(neg,1),'gate-blocked months',gate_hits)
print(cum)
