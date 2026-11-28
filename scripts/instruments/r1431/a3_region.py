# (1431) 案 A（足切り φ·州の人口）の下限：州の人口の分布（基準 base_ins と腕 c152sdB の region）と、腕の小さい P_n の行の組の取り分
import csv
SD='D:/PoB_work/keep_1425_rej/c152sdB'; BASE='D:/PoB_work/keep_1425/base_ins'
def rows(p):
    with open(p, newline='', encoding='utf-8') as f:
        yield from csv.DictReader(f, delimiter='\t')
def fl(x):
    try:
        v=float(x); return v if v==v else 0.0
    except: return 0.0
def q(xs, ps):
    xs=sorted(xs); n=len(xs)
    return ['%.3g'%xs[min(n-1,int(p*n))] if n else 'nan' for p in ps]
out=[]
def say(*a): out.append(' '.join(str(x) for x in a))
for tag,root in (('基準 base_ins',BASE),('腕 c152sdB',SD)):
    pops=[]; small={}; names={}
    for s in range(1,11):
        for r in rows(f'{root}/region_s{s}.tsv'):
            if r['activeFlag']!='1': continue
            pk=fl(r['populationK']); pops.append(pk*1000)
            for lim in (1e3,1e4,1e5):
                if pk*1000<lim: small[lim]=small.get(lim,0)+1
            if pk*1000<1e4: names[r['region']]=names.get(r['region'],0)+1
    say(f'== {tag}：有効な州×四半期 {len(pops)}・州の人口（人）p0/p1/p10/p50', q(pops,[0,0.01,0.1,0.5]))
    say('   1000 人未満・1 万人未満・10 万人未満の州×四半期', [small.get(x,0) for x in (1e3,1e4,1e5)], '→ φ·N_r が 10 人・100 人・1000 人未満')
    say('   1 万人未満の州（州×四半期の数の多い順 8）', sorted(names.items(), key=lambda x:-x[1])[:8])
# 腕：小さい P_n の行で足切りを越えた組の取り分（案 (c) の S/P_n）と、国の参加の割合 P_n/N
sh=[]; share_rows=0; pnN=[]; tot=0; below=0
for s in range(1,11):
    for r in rows(f'{SD}/facfloor_s{s}.tsv'):
        pn=fl(r['partDK']); pk=fl(r['popK']); nf=int(r['facSeenCount']); ca=int(r['cellsAboveP'])
        if pk<=0: continue
        tot+=1; pnN.append(pn/pk)
        if pn/pk<0.01: below+=1
        if nf>0 and 0<pn<0.1 and ca>0: sh.append(fl(r['partAbovePK'])/ca/pn)
say('== 腕：P_n 100 人未満で勢力のある行の 組の平均の取り分 S/P_n p10/p50/p90', q(sh,[0.1,0.5,0.9]), f'（行 {len(sh)}）')
say(f'== 腕：国の参加の割合 P_n/N p10/p50/p90', q(pnN,[0.1,0.5,0.9]), f'・P_n/N < φ（0.01）の行 {below}/{tot}＝{below/tot:.3f}（案 A では、その国の組は どれも φN_r に届くには一つの州に不満が寄る必要がある〈推〉）')
open('D:/PoB_work/r1431_work/a3_region.txt','w',encoding='utf-8').write('\n'.join(out)+'\n')
