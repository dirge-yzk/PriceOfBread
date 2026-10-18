import csv,glob,sys,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
d='D:/PoB_work/keep_412/m412_ins'
rows=[]
for f in sorted(glob.glob(d+'/scalars_s*.tsv')):
    seed=int(f.split('_s')[-1].split('.')[0])
    with open(f,encoding='utf-8') as fh:
        for r in csv.DictReader(fh,delimiter='\t'):
            r['seed']=seed; rows.append(r)
def fl(x):
    try: return float(x)
    except: return float('nan')
for n in ['清','フランス','イギリス','ロシア','オスマン','プロイセン','オーストリア','日本','アメリカ']:
    rr=[r for r in rows if r['nation']==n]
    if not rr: print(n,'none'); continue
    leg=sorted(fl(r['legitimacy']) for r in rr); mil=sorted(fl(r['militancyMean']) for r in rr)
    q=lambda a,p:a[min(len(a)-1,int(len(a)*p))]
    print('%s rows %d | legitimacy min %.3f p5 %.3f p25 %.3f med %.3f | militancyMean max %.3f p99 %.3f p90 %.3f med %.3f  >0.3:%d >0.5:%d'%(n,len(rr),leg[0],q(leg,.05),q(leg,.25),q(leg,.5),mil[-1],q(mil,.99),q(mil,.9),q(mil,.5),sum(m>0.3 for m in mil),sum(m>0.5 for m in mil)))
