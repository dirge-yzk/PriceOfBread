import csv,sys,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
YEARS=[1913,1950]
def scan(root,nat):
    out={y:[] for y in YEARS}
    for s in range(1,11):
        with open(f'{root}/nation_s{s}.tsv',encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); i={n:k for k,n in enumerate(h)}
            last={}
            for row in r:
                if row[i['nation']]==nat:
                    y=int(row[i['year']])
                    if y in YEARS: last[y]=row
            for y,row in last.items(): out[y].append(float(row[i['totalPopulationK']]))
    return out
for nat in ('ロシア','スウェーデン'):
    for name,root in (('arm','D:/PoB_work/c7hl_arm/out_c7hlB_10/ins'),('base','D:/PoB_work/keep_773/base_ins'),('N1','D:/PoB_work/keep_773/n1'),('N2','D:/PoB_work/keep_773/n2')):
        o=scan(root,nat)
        print(nat,name,{y:(round(st.mean(v),0),len(v)) for y,v in o.items()})
