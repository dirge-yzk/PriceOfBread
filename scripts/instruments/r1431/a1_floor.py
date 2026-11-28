# (1431) 足切りの下限：c152sdB の facfloor（旗 1・φP_n）と keep_1425/base_ins の dcont（旗 0）を読むだけ
import csv, sys, statistics as st
SD='D:/PoB_work/keep_1425_rej/c152sdB'; BASE='D:/PoB_work/keep_1425/base_ins'
def rows(p):
    with open(p, newline='', encoding='utf-8') as f:
        yield from csv.DictReader(f, delimiter='\t')
def q(xs, ps):
    xs=sorted(xs); n=len(xs)
    return [xs[min(n-1,int(p*n))] if n else float('nan') for p in ps]
P=[0.1,0.5,0.9]
out=[]
def say(*a): out.append(' '.join(str(x) for x in a))
# --- 旗 1 の腕（facfloor）
tot=0; facrows=0; small=0; smallFac=0; smallBig=0; zeroP=0; sizes=[]; sizesSmall=[]
bins={}  # 国の大きさ × P_n の区分
tinyFacRows=0; tinyFac=0
for s in range(1,11):
    for r in rows(f'{SD}/facfloor_s{s}.tsv'):
        tot+=1
        popK=float(r['popK']); pn=float(r['partDK']); nf=int(r['facSeenCount']); ca=int(r['cellsAboveP']); pa=float(r['partAbovePK'])
        if pn<=0: zeroP+=1
        nb='<0.1M' if popK<100 else '<1M' if popK<1000 else '<10M' if popK<10000 else '>=10M'
        pb='Pn=0' if pn<=0 else 'Pn<100人' if pn<0.1 else 'Pn<1万人' if pn<10 else 'Pn<100万人' if pn<1000 else 'Pn>=100万人'
        b=bins.setdefault((nb,pb),[0,0,0]); b[0]+=1
        if nf>0:
            facrows+=1; b[1]+=1; b[2]+=nf
            if ca>0:
                m=pa/ca*1000.0  # 人（足切りを越えた組の平均）
                sizes.append(m)
                if pn<0.1: sizesSmall.append(m)
                if m<1.0: tinyFacRows+=1; tinyFac+=nf
            if pn<0.1:
                small+=1; smallFac+=nf
                if popK>1000: smallBig+=1
say('== 旗 1 の腕 c152sdB facfloor（種 1〜10・国×四半期）')
say('行', tot, '・P_n＝0 の行', zeroP, f'({zeroP/tot:.3f})', '・勢力のある行', facrows)
say('P_n 100 人未満で勢力のある行', small, '・その勢力', smallFac, '・その中の人口 100 万超の行', smallBig)
say('足切りを越えた組の平均の大きさ（人・勢力のある行）p10/p50/p90', ['%.3g'%x for x in q(sizes,P)])
say('同じ（P_n 100 人未満の行）p10/p50/p90', ['%.3g'%x for x in q(sizesSmall,P)])
say('組の平均が 1 人未満の行', tinyFacRows, '・その勢力', tinyFac)
say('-- 国の大きさ × P_n の区分：行・勢力のある行・勢力の数の和')
for k in sorted(bins): say(k, bins[k])
# --- 基準（旗 0）の dcont：D の重みの頭数（大衆の層）≈ P_n の近似
tot=0; pnz=0; pnsmall=0; pnsmallBig=0; facB=0; pns=[]
for s in range(1,11):
    for r in rows(f'{BASE}/dcont_s{s}.tsv'):
        tot+=1
        mk=float(r['massPopK']); dm=r['discontentMeanShare']
        try: d=float(dm)
        except: d=0.0
        if d!=d: d=0.0
        pn=mk*d
        pns.append(pn*1000)
        if pn<=0: pnz+=1
        if pn<0.1:
            pnsmall+=1
            if mk>1000: pnsmallBig+=1
        facB+=int(r['factionsCount'])
say('== 基準 keep_1425/base_ins dcont（旗 0・大衆の D の頭数＝P_n の近似〈推〉）')
say('行', tot, '・P_n≈0 の行', pnz, f'({pnz/tot:.3f})', '・P_n≈100 人未満の行', pnsmall, f'({pnsmall/tot:.3f})', '・その中の大衆 100 万超', pnsmallBig)
say('P_n≈（人）p10/p50/p90', ['%.3g'%x for x in q(pns,P)], '・勢力の数の平均', '%.3f'%(facB/tot))
open('D:/PoB_work/r1431_work/a1_floor.txt','w',encoding='utf-8').write('\n'.join(out)+'\n')
print('\n'.join(out))
