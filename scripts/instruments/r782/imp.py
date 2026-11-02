# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os, collections
d=sys.argv[1]; seeds=[int(x) for x in sys.argv[2].split(',')]; nat='イギリス'
tot=collections.Counter(); ex=collections.Counter(); n=0
for s in seeds:
    with open(os.path.join(d,f'trroute_s{s}.tsv'),encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        for x in r:
            y=int(x[0])
            if not (1790<=y<=1799): continue
            v=float(x[ix['valueGoldPerYear']] or 0)
            if x[ix['buyerNation']]==nat and x[ix['sellerNation']]!=nat: tot[x[ix['sellerNation']]]+=v/10/len(seeds)
            if x[ix['sellerNation']]==nat and x[ix['buyerNation']]!=nat: ex[x[ix['buyerNation']]]+=v/10/len(seeds)
print(d, '英の輸入（金・年・1790〜99 の平均・種の平均）合計 %.0f'%sum(tot.values()), ' 上位:', ', '.join(f'{k} {v:.0f}' for k,v in tot.most_common(8)))
print(d, '英の輸出 合計 %.0f'%sum(ex.values()), ' 上位:', ', '.join(f'{k} {v:.0f}' for k,v in ex.most_common(5)))
