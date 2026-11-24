# 問い 1 の表: 年ごと（4 四半期の和か末）に持ち主・人口・流入（出どころ別）・流出（行き先別）・食・機会・持ち分・AbsorbCap〜・上限〜
import sys,collections
d,seed,reg=sys.argv[1],sys.argv[2],sys.argv[3]
Q={}
for line in open(sys.argv[4],encoding='utf-8').readlines()[1:]:
    a=line.rstrip('\n').split('\t'); Q[int(a[0])]=a
IN=collections.defaultdict(collections.Counter); OUT=collections.defaultdict(collections.Counter)
with open(f'{d}/migration_s{seed}.tsv',encoding='utf-8') as f:
    f.readline()
    for line in f:
        a=line.split('\t'); y=1783+int(a[0])//4
        if a[2]==reg: IN[y][a[1]]+=float(a[3])
        if a[1]==reg: OUT[y][a[2]]+=float(a[3])
print('年\t持ち主\t人口K\t機会\t持ち分\t食\tAbsorbCap〜\t上限〜(K/年)\t流入K/年(上位)\t流出K/年(上位)')
for y in range(1817,1950):
    qs=[q for q in Q if 1783+q//4==y]
    if not qs: continue
    a=Q[max(qs)]
    cap=sum(0.012*float(Q[q][3])*float(Q[q][17]) for q in qs)
    fi=lambda C: f"{sum(C.values()):.2f} ["+', '.join(f'{k}{v:.2f}' for k,v in C.most_common(3))+']'
    print(f"{y}\t{a[2]}\t{float(a[3]):.1f}\t{float(a[4]):.2f}\t{float(a[5]):.2f}\t{float(a[6]):.2f}\t{float(a[17]):.2f}\t{cap:.2f}\t{fi(IN[y])}\t{fi(OUT[y])}")
