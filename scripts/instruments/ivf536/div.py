import csv, sys, statistics as st
csv.field_size_limit(1<<30)
def run(d):
    out=[]
    for sd in range(1,11):
        rows=list(csv.DictReader(open(f'{d}/nation_s{sd}.tsv',encoding='utf-8'),delimiter='\t'))
        last={}; first={}
        for r in rows:
            k=r['nationIndex']
            try: q=int(r['quarter']); p=float(r['populationK']); g=float(r['nominalGdpPerYear'])
            except: continue
            if p!=p: continue
            if k not in first or q<first[k][0]: first[k]=(q,p,g)
            if k not in last or q>last[k][0]: last[k]=(q,p,g)
        wp0=sum(v[1] for v in first.values()); wp1=sum(v[1] for v in last.values() if v[0]>=660)
        # per-head nominal gdp growth (price proxy) of the biggest nations at end vs start
        ratios=[]
        for k,v in last.items():
            if v[0]>=660 and k in first and first[k][1]>1000 and first[k][2]>0 and v[1]>1000:
                ratios.append((v[2]/v[1])/(first[k][2]/first[k][1]))
        mx=max((v[1] for v in last.values()),default=0)
        out.append((wp1/wp0, max(ratios) if ratios else float('nan'), st.median(ratios) if ratios else float('nan'), mx))
    return out
for name,d in [('A','D:/PoB_work/ivf_536b/out_A_10/ins'),('C','D:/PoB_work/ivf_536b/out_C_10/ins'),('B','D:/PoB_work/ivf_536b/out_B_10/ins'),('N1','D:/PoB_work/keep_533/n1'),('N2','D:/PoB_work/keep_533/n2')]:
    o=run(d)
    print(name,'世界人口 終/始 %.2f〜%.2f'%(min(x[0] for x in o),max(x[0] for x in o)),'名目GDP/人の倍率 最大 %.1f 中央値 %.1f'%(max(x[1] for x in o),st.median(x[2] for x in o)),'最大国の人口K %.0f'%max(x[3] for x in o))
