import csv,glob,sys,collections
sys.stdout.reconfigure(encoding='utf-8')
d='D:/PoB_work/keep_412/m412_ins'
def rd(pat):
    for f in sorted(glob.glob(d+'/'+pat)):
        seed=int(f.split('_s')[-1].split('.')[0])
        with open(f,encoding='utf-8') as fh:
            for r in csv.DictReader(fh,delimiter='\t'):
                r['seed']=seed; yield r
go=collections.Counter(); n=0; hi=0
for r in rd('secession_s*.tsv'):
    n+=1
    if r['goFlag']=='1': go[(r['nation'])]+=1
    try:
        if float(r['militancyMeanIndex'])>0.5: hi+=1
    except: pass
print('secession rows',n,'goFlag=1 total',sum(go.values()),'militancyMeanIndex>0.5 rows',hi)
print(go.most_common(12))
# polity transitions & civil war months per seed (8 nations)
trans=collections.Counter(); last={}
cw=0
for r in rd('scalars_s*.tsv'):
    k=(r['seed'],r['nation']); p=r['polityCode']
    if k in last and last[k]!=p: trans[r['nation']]+=1
    last[k]=p
print('polity transitions (8 INS nations x10 seeds)',sum(trans.values()),trans.most_common(9))
for r in rd('nation_s*.tsv'):
    try:
        if float(r['civilWarMonths'])>0: cw+=1
    except: pass
print('nation_s rows with civilWarMonths>0:',cw)
