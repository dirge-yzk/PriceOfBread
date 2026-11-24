# 問い 1: ある州への流入（出どころごと）と流出（行き先ごと）を段ごとに集計（migration_s は国内の州の間）
import sys,collections
d,seed,reg,q0,q1,q2=sys.argv[1],sys.argv[2],sys.argv[3],int(sys.argv[4]),int(sys.argv[5]),int(sys.argv[6])
inn=[collections.Counter(),collections.Counter()]; out=[collections.Counter(),collections.Counter()]
with open(f'{d}/migration_s{seed}.tsv',encoding='utf-8') as f:
    f.readline()
    for line in f:
        a=line.split('\t'); q=int(a[0])
        if q<q0 or q>=q2: continue
        ph=0 if q<q1 else 1
        if a[2]==reg: inn[ph][a[1]]+=float(a[3])
        if a[1]==reg: out[ph][a[2]]+=float(a[3])
for ph,name in ((0,f'膨らむ段 q{q0}-{q1-1}'),(1,f'抜ける段 q{q1}-{q2-1}')):
    ti=sum(inn[ph].values()); to=sum(out[ph].values())
    print(f'## {name}: 流入の和 {ti:.2f}K・流出の和 {to:.2f}K')
    print('  流入の上位:', ', '.join(f'{k} {v:.2f}' for k,v in inn[ph].most_common(8)))
    print('  流出の上位:', ', '.join(f'{k} {v:.2f}' for k,v in out[ph].most_common(8)))
