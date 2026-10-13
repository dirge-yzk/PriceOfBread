import sys, os, collections, statistics, glob, re
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
def table(p):
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))
def fl(x):
    try: return float(x)
    except: return float('nan')
closed=0; obs=0; per=[]
for sd in range(1,11):
    c=o=0
    for r in table(os.path.join(D,f'fiscal_s{sd}.tsv')):
        if r['nation']!='イギリス': continue
        if int(r['monthIndex'])>=24: continue
        o+=1
        if r['borrowGateOpenFlag']=='0': c+=1
    per.append(c); closed+=c; obs+=o
print('英 monthIndex<24 門閉(旗0)', closed, '/', obs, '種ごと', per)
# peg & defaults (all nations)
peg=0; tot=0; dflt=0; ukpeg=0; uktot=0
for sd in range(1,11):
    last={}
    for r in table(os.path.join(D,f'nation_s{sd}.tsv')):
        b=fl(r.get('bondRateMeanPerYear','nan'))
        if b==b:
            tot+=1
            if b>=0.299: peg+=1
            if r['nation']=='イギリス':
                uktot+=1
                if b>=0.299: ukpeg+=1
        v=fl(r.get('sovereignDefaultCumulativeCount','nan'))
        if v==v: last[r['nation']]=max(last.get(r['nation'],0),v)
    dflt+=sum(last.values())
print('張り付き 全', peg, tot, f'{peg/tot:.5f}', ' 英', ukpeg, uktot, '  世界の不履行(種ごとの累計最大の和)', dflt)
# interest/revenue Q5-40 UK seeds median, debt/gdp Q40
irs=[]; dg=[]
for sd in range(1,11):
    s_i=s_r=0; d=None
    for r in table(os.path.join(D,f'nation_s{sd}.tsv')):
        if r['nation']!='イギリス': continue
        q=int(r['quarter'])
        if 4<=q<=39: s_i+=fl(r['interestMeanPerMonth']); s_r+=fl(r['revenueBaseMeanPerMonth'])
        if q==39: d=fl(r['govDebtStock'])/fl(r['nominalGdpPerYear'])
    irs.append(s_i/s_r); dg.append(d)
print('英 利払い/歳入 Q5-40 (窓の和比) 種の中央', statistics.median(irs), ' 債務/年GDP Q40末 中央', statistics.median(dg), dg)
