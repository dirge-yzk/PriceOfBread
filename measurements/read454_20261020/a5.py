import sys,pickle,collections,math
sys.stdout.reconfigure(encoding="utf-8")
T,E=pickle.load(open('eng.pkl','rb'))
M=['イギリス','フランス','プロイセン','アメリカ','ロシア','清','日本','オスマン']
P=[(1783,1809),(1810,1839),(1840,1869),(1870,1899),(1900,1950)]
def per(y):
    for i,(a,b) in enumerate(P):
        if a<=y<=b: return i
def med(L):
    L=sorted(L); return L[len(L)//2] if L else float('nan')
g=collections.defaultdict(list)
for (s,qt,y,n,row,eng,tech,kn,need,rep,cost) in T:
    if n in M: g[(row,n,per(y))].append((kn,need,rep,cost,tech))
rows=[]
for k in g:
    if k[0] not in rows: rows.append(k[0])
for row in rows:
    print('##',row)
    for n in M:
        s=f"  {n:6}"
        for i in range(len(P)):
            L=g.get((row,n,i),[])
            if not L: s+=" | -"; continue
            kn=sum(x[0] for x in L)/len(L)
            rat=[math.log(x[2]/x[3]) for x in L if x[2]>0 and x[3]>0]
            nd=sum(1 for x in L if x[2]<0)/len(L); nc=sum(1 for x in L if x[2]>=0 and x[3]<0)/len(L)
            s+=f" | k{kn:.1f} N{med([x[1] for x in L]):.0f} T{med([x[4] for x in L if not math.isnan(x[4])]) if any(not math.isnan(x[4]) for x in L) else float('nan'):.0f} r{med([x[2] for x in L if x[2]>0]):.2f} c{med([x[3] for x in L if x[3]>0]):.2f} ln{med(rat):+.2f}"+(f" nD{nd:.1f}" if nd>0.05 else "")+(f" nC{nc:.1f}" if nc>0.05 else "")
        print(s)
