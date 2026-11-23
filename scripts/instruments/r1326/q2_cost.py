# (1326) 問い2：国×年の値段（nationgood_s の priceMeanGold）から 電灯網・ガス灯 の 1 単位の投入費を組み、電化インフラの値段と並べる
# 投入の量は Simulation.cs:1164-1167 の行を写した（労働力 0.5・高度労働力 0.2）。値段は国の平均＝州の値段ではない（近似）。
import csv,sys,collections,statistics as st
R=sys.argv[1]; out=open(sys.argv[2],'w',encoding='utf-8'); YRS=[int(y) for y in sys.argv[3].split(',')]
EL={'送電網':0.4,'電化製品':0.3,'鋼':0.2,'労働力':0.5,'高度労働力':0.2}
GL={'ガス':0.6,'鋼':0.2,'労働力':0.5,'高度労働力':0.2}
NAT=['イギリス','フランス','プロイセン','清','アメリカ合衆国','ロシア','日本']
out.write('種\t年\t国\tP電化インフラ\t電灯網の投入費\tガス灯の投入費\t内訳(送電網,電化製品,鋼,労,高労)\tP送電網\t送電網の産出\t送電網の需要\t送電網の不足\n')
ratios=collections.defaultdict(list)
for s in range(1,11):
    px=collections.defaultdict(dict); q=collections.defaultdict(dict)
    with open(f'{R}/nationgood_s{s}.tsv',encoding='utf-8') as f:
        for r in csv.DictReader(f,delimiter='\t'):
            y=int(r['year'])
            if y not in YRS: continue
            try: px[(y,r['nation'])][r['good']]=float(r['priceMeanGold'])
            except: pass
            if r['good']=='送電網': q[(y,r['nation'])]=(r['producedQty'],r['demandQty'],r['shortageQty'])
    for (y,n),p in sorted(px.items()):
        if n not in NAT or '電化インフラ' not in p: continue
        try:
            el=[EL[k]*p[k] for k in EL]; gl=sum(GL[k]*p[k] for k in GL)
        except KeyError: continue
        ratios[y].append(sum(el)/max(gl,1e-12))
        out.write(f's{s}\t{y}\t{n}\t{p["電化インフラ"]:.3g}\t{sum(el):.3g}\t{gl:.3g}\t{",".join(f"{v:.2g}" for v in el)}\t{p.get("送電網",float("nan")):.3g}\t{q[(y,n)][0] if (y,n) in q else ""}\t{q[(y,n)][1] if (y,n) in q else ""}\t{q[(y,n)][2] if (y,n) in q else ""}\n')
for y in sorted(ratios): out.write(f'# {y} 電灯網÷ガス灯の投入費 中央={st.median(ratios[y]):.3g} 最小={min(ratios[y]):.3g} 最大={max(ratios[y]):.3g} n={len(ratios[y])}\n')
