import sys,pickle,statistics as st,collections,math
sys.stdout.reconfigure(encoding="utf-8")
T,E=pickle.load(open('eng.pkl','rb'))
def q(L,p):
    L=sorted(L); 
    if not L: return float('nan')
    return L[min(len(L)-1,int(p*(len(L)-1)+0.5))]
B=lambda y: 1783 if y<1800 else (y//20)*20
g=collections.defaultdict(list)
for (s,qt,y,n,row,eng,tech,kn,need,rep,cost) in T:
    g[(row,B(y),kn)].append((need,rep,cost,tech,n))
rows=[]
for (row,b,kn) in g: 
    if row not in rows: rows.append(row)
for row in rows:
    print('##',row)
    for kn in (0,1):
        for b in sorted(set(k[1] for k in g if k[0]==row and k[2]==kn)):
            L=g[(row,b,kn)]
            need=[x[0] for x in L]; tech=[x[3] for x in L if not math.isnan(x[3])]
            rep=[x[1] for x in L if not math.isnan(x[1])]
            cost=[x[2] for x in L if not math.isnan(x[2])]
            nodem=sum(1 for x in L if x[1]<0)/len(L)
            nocost=sum(1 for x in L if x[1]>=0 and x[2]<0)/len(L)
            le1=sum(1 for x in L if x[1]>0 and x[2]>0 and x[1]<=x[2])/len(L)
            repv=[x[1] for x in L if x[1]>0]; costv=[x[2] for x in L if x[2]>0]
            rat=[math.log(x[1]/x[2]) for x in L if x[1]>0 and x[2]>0]
            print(f"  k{kn} {b} n={len(L)} need med={q(need,.5):.1f} p90={q(need,.9):.1f} max={max(need):.1f} tech med={q(tech,.5) if tech else float('nan'):.1f} | rep med={q(repv,.5):.3g} cost med={q(costv,.5):.3g} lnratio med={q(rat,.5):.2f} p90={q(rat,.9):.2f} | noDem={nodem:.2f} noCost={nocost:.2f} costHigher={le1:.2f}")
