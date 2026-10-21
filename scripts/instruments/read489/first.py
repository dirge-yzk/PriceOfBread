import io,csv, sys
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding="utf-8")
import collections, statistics as st
D='D:/PoB_work/keep_484/m484_ins'
rows_of_interest=None
res=collections.defaultdict(list)  # row -> list of (seed, first nation, A, tech, need, rankA, rankNeed, n_at_T, dTech, dNeed)
for s in range(1,11):
    E=collections.defaultdict(list); T=collections.defaultdict(lambda: collections.defaultdict(dict))
    with open(f'{D}/engineer_s{s}.tsv',encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); next(r)
        for x in r:
            k,q,y,nat,row,dec=x[:6]
            a,tech,kn,need=float(x[6]),float(x[7]),x[8],float(x[9])
            q=int(q)
            if k=='E': E[row].append((q,nat,a,tech,need,int(y),int(dec)))
            else: T[row][q][nat]=(a,tech,need,kn)
    for row,lst in E.items():
        lst.sort()
        q,nat,a,tech,need,y,dec=lst[0]
        if dec<=1780: continue
        # T snapshot at largest q' <= q with entries, unknown nations
        qs=[qq for qq in T[row] if qq<=q]
        if not qs: continue
        qq=max(qs); snap={n:v for n,v in T[row][qq].items()}
        if nat not in snap: continue
        byA=sorted(snap, key=lambda n:-snap[n][0]); byN=sorted(snap,key=lambda n:-snap[n][2]); byT=sorted(snap,key=lambda n:-(snap[n][1]+snap[n][2]))
        res[row].append((s,nat,y,dec,round(a,1),round(tech,1),round(need,1),byA.index(nat)+1,byN.index(nat)+1,len(snap), byA[0], round(st.median(v[2] for v in snap.values()),1), round(max(v[2] for v in snap.values())-min(v[2] for v in snap.values() if v[0]>0),1) if any(v[0]>0 for v in snap.values()) else None))
for row in sorted(res, key=lambda r: res[r][0][3]):
    L=res[row]
    c=collections.Counter(x[1] for x in L)
    rA=collections.Counter(x[7] for x in L); rN=collections.Counter(x[8] for x in L)
    print(f'{row}\t{L[0][3]}\tfirst={dict(c.most_common(4))}\trankA={dict(sorted(rA.items()))}\trankNeed={dict(sorted(rN.items()))}\tmedYear={st.median(x[2] for x in L)}\ttopA={dict(collections.Counter(x[10] for x in L).most_common(3))}')
