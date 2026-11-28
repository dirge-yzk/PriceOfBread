# (1431) 強さの物差し：dcont（基準 base_ins＝旗 0・腕 c152sdB＝旗 1）と facfloor（腕）を読むだけ
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
P=[0.5,0.9,0.99]
def summ(tag, root):
    gate=0; strong=0; rat=[]; revolt={}; nf=0; n=0; ms_old=0; ms_new=0; partNew=0; mass=0; dis=0; mil=0
    for s in range(1,11):
        for r in rows(f'{root}/dcont_s{s}.tsv'):
            n+=1; nf+=int(r['factionsCount'])
            g=int(r['facCohGateCount']); st=int(r['facStrongCount'])
            if g>0: gate+=1; rat.append(fl(r['strengthRatioMaxIndex']))
            if st>0: strong+=1
            k=(s,r['nation']); revolt[k]=max(revolt.get(k,0),int(r['factionRevoltsCum']))
            mk=fl(r['massPopK']); mass+=mk; dis+=mk*fl(r['discontentMeanShare']); mil+=mk*fl(r['militancyMeanShare'])
    say(f'== {tag}: 行 {n}・勢力の数の平均 {nf/n:.3f}・結束 0.8 以上の勢力のいる行 {gate}・強い勢力（F/(H+L)>1）の行 {strong}・蜂起（国の最大の和）{sum(revolt.values())}')
    say('   結束の門の行の強さの比の最大 p50/p90/p99', q(rat,P), f'・大衆の D の平均 {dis/mass:.4f}・M の平均 {mil/mass:.4f}')
    return rat
rb=summ('基準 m1425 base_ins（旗 0：参加 max(D,M)・足切り φN）', BASE)
ra=summ('腕 c152sdB（旗 1：参加 D・足切り φP_n）', SD)
# 同じ盤面の参加の縮み k＝Σ N max(D,M) M（全ての層・計器が自分で回した partMaxK と milSumDK から近似）
# facfloor：partDK＝Σ N D・partMaxK＝Σ N max(D,M)・milSumDK＝Σ N D M。Σ N max(D,M) M は表に無い＝dcont の milSumNewK（φN を越えた組だけ）を同じ国×年で使う〈推〉
ff={}
for s in range(1,11):
    for r in rows(f'{SD}/facfloor_s{s}.tsv'):
        ff[(s,r['nation'],r['quarter'])]=(fl(r['partDK']),fl(r['partMaxK']),fl(r['milSumDK']))
kms=[]; km1=[]; match=0; kk=[]; cf={}; kpart=[]
for s in range(1,11):
    for r in rows(f'{SD}/dcont_s{s}.tsv'):
        key=(s,r['nation'],str(int(r['quarter'])+1))
        if key not in ff: continue
        match+=1
        pd,pm,md=ff[key]
        if pm>0: kpart.append(pd/pm)
        if int(r['facCohGateCount'])>0 and md>0:
            msn=fl(r['milSumNewK']); k=msn/md if msn>0 else 0
            kk.append(k)
            ratio=fl(r['strengthRatioMaxIndex'])
            cf['gate']=cf.get('gate',0)+1
            mm=fl(r['milSumMilK']); km=mm/md
            kms.append(km)
            if ratio*km>1: cf['kM2']=cf.get('kM2',0)+1
            k1=fl(r['partMilK'])/md; km1.append(k1)
            if ratio*k1>1: cf['kM1']=cf.get('kM1',0)+1
            if ratio>1: cf['strong']=cf.get('strong',0)+1
            if ratio*k>1: cf['krow']=cf.get('krow',0)+1
            for kx in (3.0, 10.0, 30.0):
                if ratio*kx>1: cf[kx]=cf.get(kx,0)+1
say(f'== 同じ盤面（腕）の比べ：dcont と facfloor の突き合わせ {match} 行')
say('   参加の縮み Σ N D ÷ Σ N max(D,M)（全ての層）p50/p90/p99', q(kpart,[0.1,0.5,0.9]), '（p10/p50/p90）')
say('   結束の門の行の Σ N max(D,M) M（φN を越えた組）÷ Σ N D M（全て）p10/p50/p90', q(kk,[0.1,0.5,0.9]))
say('   突き合わせた結束の門の行', cf.get('gate',0), '・その中の比>1', cf.get('strong',0), '・比×k_row>1（k_row＝上の行ごとの比）', cf.get('krow',0), '・比×3/10/30>1', [cf.get(x,0) for x in (3.0,10.0,30.0)])
say('   F_M2 案：Σ N M²（闘争度の重みの組・φN を越えた組・dcont milSumMilK）÷ Σ N D M p10/p50/p90', q(kms,[0.1,0.5,0.9]), '・比×その比>1 の行', cf.get('kM2',0))
say('   F_M 案：Σ N M（全ての層・dcont partMilK）÷ Σ N D M p10/p50/p90', q(km1,[0.1,0.5,0.9]), '・比×その比>1 の行', cf.get('kM1',0))
bm=[];bn=[]
for s in range(1,11):
    for r in rows(f'{BASE}/dcont_s{s}.tsv'):
        if int(r['facCohGateCount'])>0 and fl(r['milSumNewK'])>0:
            bm.append(fl(r['milSumMilK'])/fl(r['milSumNewK'])); bn.append(fl(r['partMilK'])/fl(r['milSumNewK']))
say('   基準の結束の門の行の Σ N M²（milSumMilK）÷ Σ N max(D,M) M（milSumNewK）p10/p50/p90', q(bm,[0.1,0.5,0.9]), '・Σ N M（partMilK）÷ 同じ', q(bn,[0.1,0.5,0.9]))
open('D:/PoB_work/r1431_work/a2_strength.txt','w',encoding='utf-8').write('\n'.join(out)+'\n')
