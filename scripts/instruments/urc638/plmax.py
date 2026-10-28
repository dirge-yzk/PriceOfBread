# ④の基線の値（読むだけ）：価格水準の最大。内戦の月が 0 かつ戦争の月が 0 の国×四半期に絞る。使い方：python -X utf8 -I plmax.py <ディレクトリ>...
import csv,sys
for d in sys.argv[1:]:
    best=[(0,'',0,0)]*3  # 全行 / 内戦なし / 内戦なし＋戦争なし
    best=[[0,'',0,0],[0,'',0,0],[0,'',0,0]]
    for s in range(1,11):
        rdr=csv.DictReader(open(f'{d}/nation_s{s}.tsv',encoding='utf-8'),delimiter='\t')
        for r in rdr:
            try: p=float(r['priceLevel'])
            except: continue
            if p!=p: continue
            cw=float(r['civilWarMonths'] or 0); aw=float(r['atWarMonths'] or 0)
            for k,ok in enumerate((True,cw==0,cw==0 and aw==0)):
                if ok and p>best[k][0]: best[k]=[p,r['nation'],int(r['year']),s]
    print(d.split('/')[-1],' 全行',best[0],' 内戦なし',best[1],' 内戦なし・戦争なし',best[2])
