import sys,pickle,collections,math
sys.stdout.reconfigure(encoding="utf-8")
T,E=pickle.load(open('eng.pkl','rb'))
def q(L,p):
    L=sorted(L)
    if not L: return float('nan')
    return L[min(len(L)-1,int(p*(len(L)-1)+0.5))]
P=[(1783,1789),(1790,1809),(1810,1839),(1840,1869),(1870,1899),(1900,1950)]
def per(y):
    for i,(a,b) in enumerate(P):
        if a<=y<=b: return i
g=collections.defaultdict(list)
for (s,qt,y,n,row,eng,tech,kn,need,rep,cost) in T:
    if kn!=0: continue
    g[(row,per(y))].append((need,rep,cost,tech,n))
rows=[]
for (row,b) in g:
    if row not in rows: rows.append(row)
for row in rows:
    print('##',row)
    for i in range(len(P)):
        L=g.get((row,i))
        if not L: continue
        need=[x[0] for x in L]; tech=[x[3] for x in L]
        cap=[x for x in L if x[3]>=40]
        n30=sum(1 for x in need if x>=30)/len(L)
        t30=sum(1 for x in tech if x>=30)/len(L)
        rat=[math.log(x[1]/x[2]) for x in L if x[1]>0 and x[2]>0]
        nodem=sum(1 for x in L if x[1]<0)/len(L)
        nocost=sum(1 for x in L if x[1]>=0 and x[2]<0)/len(L)
        repv=[x[1] for x in L if x[1]>0]; costv=[x[2] for x in L if x[2]>0]
        s=f"  {P[i][0]}-{P[i][1]} n={len(L)} need med/p90/max={q(need,.5):.0f}/{q(need,.9):.0f}/{max(need):.0f} need>=30:{n30:.2f} tech med/max={q(tech,.5):.0f}/{max(tech):.0f} tech>=30:{t30:.2f} | rep={q(repv,.5):.3g} cost={q(costv,.5):.3g} lnR med/p90={q(rat,.5):.2f}/{q(rat,.9):.2f} noDem={nodem:.2f} noCost={nocost:.2f}"
        if cap:
            cr=[math.log(x[1]/x[2]) for x in cap if x[1]>0 and x[2]>0]
            cn=collections.Counter(x[4] for x in cap).most_common(3)
            s+=f" || tech>=40: n={len(cap)} need med={q([x[0] for x in cap],.5):.0f} lnR={q(cr,.5):.2f} rep={q([x[1] for x in cap if x[1]>0],.5):.3g} cost={q([x[2] for x in cap if x[2]>0],.5):.3g} noDem={sum(1 for x in cap if x[1]<0)/len(cap):.2f} who={cn}"
        print(s)
