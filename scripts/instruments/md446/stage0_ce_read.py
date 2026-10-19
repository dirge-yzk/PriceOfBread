import csv,statistics as st,collections,sys
sys.stdout.reconfigure(encoding='utf-8')
cols=['popK','earnedLastPerMonth','savingsStartStock','foodWantPerMonth','foodDemandPerMonth','foodPaidPerMonth','dailyWantPerMonth','dailyDemandPerMonth','dailySuppliedWantPerMonth','dailyPaidPerMonth','luxuryWantPerMonth','luxuryDemandPerMonth','luxurySuppliedWantPerMonth','luxuryPaidPerMonth','otherWantPerMonth','otherPaidPerMonth','savingsDrawPerMonth','remainderPerMonth']
def run(nation,cl):
    rows=collections.defaultdict(list)
    for s in range(1,11):
        with open('D:/PoB_work/keep_443/m443_ins/ce_s%d.tsv'%s,encoding='utf-8') as f:
            r=csv.reader(f,delimiter='\t'); h=next(r); ix={n:i for i,n in enumerate(h)}
            for row in r:
                if row[0]!='C' or row[5]!=nation or row[7]!=cl: continue
                y=int(row[3]); per=('1783-99' if y<1800 else '1800-49' if y<1850 else '1850-99' if y<1900 else '1900-49')
                rows[per].append([float(row[ix[c]] or 0) for c in cols])
    print(nation,cl)
    for per in sorted(rows):
        m=[st.median(c) for c in zip(*rows[per])]
        print(' ',per,len(rows[per]),' '.join(f'{n[:-8] if n.endswith("PerMonth") else n}={v:.0f}' for n,v in zip(cols,m)))
for n,c in [('イギリス','農民'),('イギリス','工場労働者'),('清','農民'),('プロイセン','農民')]: run(n,c)
