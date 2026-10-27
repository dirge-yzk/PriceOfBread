"""(609) 合流：全ての国の価格水準の発散の確かめ（nation_s の priceLevel・死んだ国も含む・読むだけ）。
使い方：python -I plvl_all.py <計器の出力フォルダ> [種の数=10] [ラベル]
出す物：種ごとに、四半期の比 P[t]/P[t-1] の最大（国と年）・P の最大（国と年）・非有限の行の数・比 ≥100 の行の数・P ≥1e3 の (国) の数。
止める線の読み（(603) G1 の全ての国への広げ）：比 ≥100 の行 0・非有限 0。記録の本体は P の最大と比の最大。"""
import sys, math, os, csv
d=sys.argv[1]; N=int(sys.argv[2]) if len(sys.argv)>2 else 10; lab=sys.argv[3] if len(sys.argv)>3 else os.path.basename(d.rstrip('/'))
tot={'jump100':0,'nonfin':0,'maxratio':0.0,'maxP':0.0}
print(f'# {lab}  種 {N}')
print('種\t比の最大\t国\t年\tPの最大\t国\t年\t非有限\t比≥100\tP≥1e3の国の数')
for s in range(1,N+1):
    p=os.path.join(d,f'nation_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(無い)'); continue
    last={}; mr=(0.0,'',0); mp=(0.0,'',0); nf=0; j100=0; big=set()
    with open(p,encoding='utf-8') as f:
        rd=csv.reader(f,delimiter='\t'); h=next(rd); iq=h.index('quarter'); iy=h.index('year'); inn=h.index('nation'); ip=h.index('priceLevel')
        for r in rd:
            try: P=float(r[ip])
            except: P=float('nan')
            n=r[inn]; y=int(r[iy])
            if not math.isfinite(P): nf+=1; continue
            if P>mp[0]: mp=(P,n,y)
            if P>=1e3: big.add(n)
            q=int(r[iq])
            if n in last and last[n][0]==q-1 and last[n][1]>0:
                rt=P/last[n][1]
                if rt>mr[0]: mr=(rt,n,y)
                if rt>=100: j100+=1
            last[n]=(q,P)
    print(f'{s}\t{mr[0]:.4g}\t{mr[1]}\t{mr[2]}\t{mp[0]:.4g}\t{mp[1]}\t{mp[2]}\t{nf}\t{j100}\t{len(big)}')
    tot['jump100']+=j100; tot['nonfin']+=nf; tot['maxratio']=max(tot['maxratio'],mr[0]); tot['maxP']=max(tot['maxP'],mp[0])
print(f"# 合計：比≥100 の行 {tot['jump100']}・非有限 {tot['nonfin']}・比の最大 {tot['maxratio']:.4g}・P の最大 {tot['maxP']:.4g}")
