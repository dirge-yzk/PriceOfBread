import sys,re,glob,os,statistics,collections
sys.stdout.reconfigure(encoding='utf-8')
UK='イギリス'
arms={'A':'D:/PoB_work/l3_299/out_A_10/ins','B':'D:/PoB_work/l3_299/out_B_10/ins','C':'D:/PoB_work/l3_299/out_C_10/ins'}
def fl(x):
    try: return float(x)
    except: return float('nan')
def rows(p):
    with open(p,encoding='utf-8') as fh:
        h=fh.readline().rstrip('\n').split('\t')
        for ln in fh: yield dict(zip(h,ln.rstrip('\n').split('\t')))
res={}
for a,D in arms.items():
    for sd in range(1,11):
        sav=collections.defaultdict(float); ear=collections.defaultdict(float); n=collections.defaultdict(int)
        for r in rows(f'{D}/class_s{sd}.tsv'):
            if r['nation']!=UK: continue
            m=re.fullmatch(r'[Qq]?(\d+)',r['time'])
            if not m: continue
            q=int(m.group(1)); c=int(r['classId'])
            if c in (0,3) and 40<=q<=159:
                sav[q]+=fl(r['savingsStock']); ear[q]+=fl(r['earnedPerMonth'])
        qs=sorted(sav)
        lux=0.0; luxq=0.0
        for r in rows(f'{D}/spend_s{sd}.tsv'):
            if r['nation']==UK and r['bucket']=='3' and r['kind']=='good' and r['tier']=='Luxury' and r['classId'] in ('0','3'):
                lux+=fl(r['payPerMonth']); luxq+=fl(r['qtyPerMonth'])
        res[(a,sd)]=(statistics.median([sav[q] for q in qs]),statistics.median([ear[q] for q in qs]),lux,luxq)
print('Q41-160 英の貴族＋資本家：貯蓄（中央）・所得/月（中央）・奢侈品の支払い/月・数量/月（B÷A を種ごと）')
for b in ('B','C'):
    print('arm',b)
    for i,name in enumerate(['貯蓄','所得','奢侈品の支払い','奢侈品の数量']):
        v=[res[(b,s)][i]/res[('A',s)][i] for s in range(1,11)]
        print(f'  {name} {b}÷A 種ごと',' '.join(f'{x:.3f}' for x in v),f'中央 {statistics.median(v):.3f}')
