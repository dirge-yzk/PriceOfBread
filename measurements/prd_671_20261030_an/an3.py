import csv,collections,sys,io,math
exec(open('an1.py').read().split("best=collections.Counter()")[0])
def thr(agg,margin=0.0):
    base=list(agg); base[6]=0; s=sum(base); base=[b/s for b in base]
    lo,hi=0,1
    for _ in range(60):
        c=(lo+hi)/2
        a=[(1-c)*b for b in base]; a[6]=c
        sp={k:sup(a,v) for k,v in PC.items()}
        o=max(v for k,v in sp.items() if k!=6)
        if sp[6]-o>margin: hi=c
        else: lo=c
    return hi
# Russia & a few
rows={}
for s in (1,2,3):
    for r in csv.DictReader(open(D+'ideology_s%d.tsv'%s,encoding='utf-8',errors='replace'),delimiter='\t'):
        if r['nation'] in ('ロシア','イギリス','フランス','プロイセン') and r['year'] in ('1783','1850','1917','1930','1949') and r['quarter'].isdigit():
            agg=[float(r[c]) for c in cols]
            if (s,r['nation'],r['year']) in rows: continue
            rows[(s,r['nation'],r['year'])]=1
            sp={k:round(sup(agg,v),3) for k,v in PC.items()}
            print(s,r['nation'],r['year'],'com',round(agg[6],4),'soc',round(agg[5],3),'lib',round(agg[2],3),'rep',round(agg[3],3),'thr_c(PD>others)',round(thr(agg),3),'thr_c(+0.2)',round(thr(agg,0.2),3))
