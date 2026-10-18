import csv,collections,sys,statistics as st
csv.field_size_limit(1<<30)
def agg(d,seeds,nat,qa,qb):
    a=collections.defaultdict(float)
    for s in seeds:
        try: rows=list(csv.DictReader(open(f'{d}/ce_s{s}.tsv',encoding='utf-8'),delimiter='\t'))
        except Exception as e: continue
        for r in rows:
            if r['scope']!='C' or r['nation']!=nat: continue
            q=int(r['quarter'])
            if not(qa<=q<qb): continue
            c=r['className']
            for k in ['dailyWantPerMonth','dailySuppliedWantPerMonth','dailyPaidPerMonth','dailyDemandPerMonth']:
                v=r[k]
                if v=='': continue
                a[(c,k)]+=float(v); a[('ALL',k)]+=float(v)
    return a
A=sys.argv[1]; B=sys.argv[2]
for nat in ['フランス','イギリス','清','日本']:
  for (qa,qb) in [(5,40),(40,100),(100,300),(300,668)]:
    print(nat,qa,qb,end=' ')
    for c in ['ALL','農民']:
        for lab,d in (('A',A),('B',B)):
            a=agg(d,range(1,11),nat,qa,qb)
            w=a[(c,'dailyWantPerMonth')]
            if w>0: print(f"{c}{lab}: 充足 {a[(c,'dailySuppliedWantPerMonth')]/w:.3f} 支払/欲 {a[(c,'dailyPaidPerMonth')]/w:.3f} |",end=' ')
    print()
