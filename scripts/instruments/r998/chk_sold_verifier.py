# (998) c91td の反証役が書いた集計（chemgood の soldPerMonth÷需要と S1 の比べ・(1001) 合流が反証役のスクラッチから写した）
import sys,os,statistics as st
RAW=sys.argv[1]; MAJ={'イギリス','フランス','ロシア','プロイセン','清','アメリカ'}
R={}
for s in range(1,11):
    acc={}
    with open(os.path.join(RAW,f'chemgood_s{s}.tsv'),encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t'); ix={c:i for i,c in enumerate(h)}
        for l in f:
            r=l.rstrip('\n').split('\t'); y=int(r[ix['year']])
            if y not in (1790,1830,1870,1913,1948) or r[ix['nation']] not in MAJ: continue
            P,D,S,I=(float(r[ix[c]]) if r[ix[c]] not in ('nan','') else 0 for c in ('outputPerMonth','demandPerMonth','soldPerMonth','importPerMonth'))
            a=acc.setdefault((r[ix['good']],y),[0]*5); a[0]+=min(P,D); a[1]+=D; a[2]+=S; a[3]+=P; a[4]+=I
    for k,a in acc.items(): R.setdefault(k,[]).append((a[0]/a[1], a[2]/a[1], a[3]/a[1], (a[3]+a[4])/a[1]))
for k in sorted(R): print(k, ' '.join('%s=%.3f'%(n,st.median([x[i] for x in R[k]])) for i,n in enumerate(['S1','sold/D','P/D','(P+I)/D'])))
