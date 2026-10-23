# (505) 線 ir：m500_ins の eqr_s（挙げた国×レシピ×四半期）から〈利幅の差 u〉の目安を組む読むだけの道具（事前登録 measurements/ir505_20261023_prereg.txt [3]）。
# u＝(m̄_m−m̄_S)÷(1＋m̄_S)・m̄_m＝Σm²÷Σm・m̄_S＝Σ(Scale×m)÷ΣScale（レシピ別の集計・利幅は 1 州あたりの平均・州の分割は無視）。gross＝1.2×u×(1+2.4×techLevelMean) は cons_s の範囲（1830 年ごろまで）だけ。
# 使い方：PYTHONUTF8=1 python gap_from_eqr.py <計器の生のフォルダ（m500_ins）>
import sys
import csv,collections
base=sys.argv[1].rstrip('/')+'/'
def load(fn,key):
    r=collections.defaultdict(list)
    with open(fn,encoding='utf-8') as fh:
        for d in csv.DictReader(fh,delimiter='\t'): r[key(d)].append(d)
    return r
out=[]
for seed in (1,5):
    eqr=load(base+f'eqr_s{seed}.tsv',lambda d:(d['nation'],int(d['year']),int(d['quarter'])%4))
    cons=load(base+f'cons_s{seed}.tsv',lambda d:(d['nation'],int(d['year'])))
    for nat in ['イギリス','フランス','清','日本','アメリカ']:
        line=[]
        for y in (1783,1800,1820,1850,1880,1913):
            L=None
            for q in (0,1,2,3):
                if (nat,y,q) in eqr: L=eqr[(nat,y,q)]; break
            if not L: continue
            S=[];M=[]
            for d in L:
                go=float(d['gateOpenRegions'])
                if go<=0: continue
                S.append(float(d['scaleSum'])); M.append(float(d['marginSum'])/go)
            T=sum(S);W=sum(M)
            if T<=0 or W<=0: continue
            mS=sum(s*m for s,m in zip(S,M))/T; mm=sum(m*m for m in M)/W
            u=max(0,(mm-mS)/(1+mS))
            C=cons.get((nat,y),[])
            tl=[float(d['techLevelMean']) for d in C if d['techLevelMean'] not in ('nan','NaN','')]
            t=sum(tl)/len(tl) if tl else float('nan')
            g=1.2*u*(1+2.4*t) if t==t else float('nan')
            line.append('%d:u%.2f/tl%.2f/g%.2f'%(y,u,t,g))
        print('seed',seed,nat,' '.join(line))
