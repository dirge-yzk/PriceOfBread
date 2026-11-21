# (1283) c132fg 紙の台本 2（読むだけ）：rdh_s（POB_INSTRUMENTS_RDH・州×四半期）で、飢えの門の月 F と g<0 の月 N の重なりの上下の限り（下 max(0,F+N−M)・上 min(F,N)）を格ごとに数える。
#  戻る量の見積もりの上限＝門の月×Mobility×|g の四半期の平均|×(州の人口−農村の人口)（g の平均は四半期の全ての月の平均＝近似）。
# 使い方: python paper_rdh.py <rdh_s のファイル>
import sys,collections
f=open(sys.argv[1],encoding='utf-8'); h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
acc=collections.defaultdict(lambda:[0,0,0,0,0.0])
for line in f:
    c=line.rstrip('\n').split('\t')
    if c[ix['kind']]!='Q': continue
    y=int(c[ix['year']]); M=int(c[ix['months']]); F=int(c[ix['famineGateMonths']]); N=int(c[ix['gNegMonths']])
    if M<=0: continue
    gm=float(c[ix['gSum']])/M; urb=float(c[ix['populationK']])-float(c[ix['ruralPopulationK']])
    lo=max(0,F+N-M); hi=min(F,N)
    for k in ((c[ix['tier']],'all'),('全て','all'),(c[ix['tier']],'1850前' if y<1850 else '1850後')):
        a=acc[k]; a[0]+=M; a[1]+=F; a[2]+=lo; a[3]+=hi
        if F>0 and gm<0: a[4]+=F*0.02*(-gm)*urb
print('格\t期間\t州×月\t門の月\t門の割合\tg<0 の重なり 下\t上\t門の中の g<0 の割合 下\t上\t戻る量の上限の見積もり千人')
for k in sorted(acc):
    M,F,lo,hi,ret=acc[k]
    print(f"{k[0]}\t{k[1]}\t{M}\t{F}\t{F/M:.4f}\t{lo}\t{hi}\t{lo/F if F else float('nan'):.4f}\t{hi/F if F else float('nan'):.4f}\t{ret:.1f}")
