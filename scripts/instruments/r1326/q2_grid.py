# (1326) 問い2：電灯網・ガス灯・水道局の能力・産出・規模・投入の充足・採算の係数（prodcap_s）を年ごとに 10 種で集める
import csv,sys,collections,statistics as st
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8'); YRS={1850,1880,1900,1913,1930,1949}
REC={'電灯網','ガス灯','水道局'}
agg=collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1,11):
    with open(f'{R}/prodcap_s{s}.tsv',encoding='utf-8') as f:
        rd=csv.DictReader(f,delimiter='\t')
        for r in rd:
            if r['recipe'] not in REC: continue
            y=int(r['year'])
            if y not in YRS: continue
            k=(r['recipe'],y,s,r['q'])
            a=agg[k]
            for c in ('capacity','output','scale','lastInputFill','profitFactor','productivity'):
                a[c].append(float(r[c]) if r[c] not in ('','NaN') else float('nan'))
# 種ごとに年の最後の四半期を取る
res=collections.defaultdict(list)
last={}
for (rec,y,s,q) in agg: last[(rec,y,s)]=max(last.get((rec,y,s),'0'),q)
for (rec,y,s),q in last.items():
    a=agg[(rec,y,s,q)]
    res[(rec,y)].append((s,len(a['capacity']),sum(a['capacity']),sum(a['output']),sum(a['scale']),st.median(a['lastInputFill']),max(a['lastInputFill']),st.median(a['profitFactor']),sum(1 for v in a['lastInputFill'] if v==0)))
out.write('recipe\tyear\t種\t行(州)\tcap和\tout和\tscale和\tfill中央\tfill最大\tprofit中央\tfill=0の行\n')
for k in sorted(res):
    for t in sorted(res[k]):
        out.write(f'{k[0]}\t{k[1]}\ts{t[0]}\t{t[1]}\t{t[2]:.4g}\t{t[3]:.4g}\t{t[4]:.4g}\t{t[5]:.3g}\t{t[6]:.3g}\t{t[7]:.3g}\t{t[8]}\n')
