import csv, collections, statistics as st, glob, sys
def load(root):
    gdp=collections.defaultdict(list); rev=collections.defaultdict(list); fill=collections.defaultdict(list)
    for s in range(1,11):
        g=collections.defaultdict(list)
        for r in csv.DictReader(open(f'{root}/nation_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
            y=int(r['year'])
            g[(r['nation'],y)].append(float(r['nominalGdpPerYear']))
        f=collections.defaultdict(list)
        for r in csv.DictReader(open(f'{root}/fiscal_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
            y=int(r['year']); f[(r['nation'],y)].append((float(r['revenueBasePerMonth']),float(r['adminFillShare'])))
        for k,v in f.items():
            if k in g:
                gm=sum(g[k])/len(g[k])
                rm=sum(a for a,b in v)/len(v)
                rev[k].append(rm*12/gm if gm>0 else float('nan'))
                fill[k].append(sum(b for a,b in v)/len(v))
    return rev,fill
a,fa=load('D:/PoB_work/keep_397/m397_ins'); b,fb=load('D:/PoB_work/keep_407/m407_ins')
nats=['イギリス','フランス','清','ロシア','オスマン','プロイセン','オーストリア','日本']
print('revenueBase*12/nominalGDP  median over seeds : m397(pre-399) -> m407 ; fill m397 / m407')
for n in nats:
    row=[]
    for y in (1784,1790,1800,1820,1850,1900):
        k=(n,y)
        if k in a and k in b:
            row.append('%d: %.3f->%.3f (x%.2f) f %.2f/%.2f'%(y,st.median(a[k]),st.median(b[k]),st.median(b[k])/st.median(a[k]),st.median(fa[k]),st.median(fb[k])))
    print(n,' | '.join(row))
