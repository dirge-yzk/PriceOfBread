import sys,glob,collections
tot=collections.Counter(); rows=0; bad=0
for f in glob.glob('mintreg_s*.tsv'):
    L=open(f,encoding='utf-8').read().splitlines(); h=L[0].split('\t')
    for l in L[1:]:
        r=dict(zip(h,l.split('\t'))); rows+=1
        i=lambda k:int(r[k])
        s=i('fiatM')+i('noGoodM')+i('noStockM')+i('aboveM')+i('gapM')+i('wouldBuyM')
        if s!=i('months') or i('atMarketM')>i('coverShortM'): bad+=1
        for k in ('months','fiatM','noGoodM','noStockM','aboveM','gapM','wouldBuyM','boughtM','notPaidM','atMarketM','coverShortM','modelMissM'): tot[k]+=i(k)
print(rows,bad,dict(tot))
