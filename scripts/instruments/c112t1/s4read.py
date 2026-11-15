import glob,collections,statistics,sys
d=sys.argv[1]; want=['清','イギリス','フランス','インド','日本','アメリカ']
acc=collections.defaultdict(list)
for f in glob.glob(d+'/s4cost_s*.tsv'):
    with open(f,encoding='utf-8') as fh:
        h=fh.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for l in fh:
            v=l.rstrip('\n').split('\t'); n=v[ix['nation']]
            if n not in want: continue
            y=int(v[ix['year']])
            if y not in (1800,1830,1870,1900,1913,1930): continue
            def g(k):
                try: return float(v[ix[k]])
                except: return float('nan')
            o=g('outputValueGold')
            if not o>0: continue
            acc[(n,y)].append((g('selfFxCostSum')/o,g('inputFxCostSum')/o,g('fxBiasPinnedShare'),g('laborPriceGrowth10y'),g('priceLevel'),g('costPriceLevel'),g('laborPriceGold')))
print('国 年 自己費用÷産出 投入÷産出 偏りの釘づけ 労働の値段の10年の伸び 物価水準 自己費用の物価水準 労働の値段(金)  ＝10種の中央')
for (n,y) in sorted(acc):
    a=acc[(n,y)]; med=[statistics.median([x[i] for x in a if x[i]==x[i]] or [float('nan')]) for i in range(7)]
    print(n,y,' '.join(f'{m:.3f}' for m in med))
