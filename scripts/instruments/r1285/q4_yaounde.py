# (1285) 問い4：ヤウンデ王国（keep_1274/a11 種 19）の名目 GDP 0 の区間の周りを表ごとに抜く（読むだけ）。
# 使い方: python -X utf8 -I q4_yaounde.py <生> <種> <国名> <出力のフォルダ>
import sys, csv, os, collections
raw, s, nat, out = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
csv.field_size_limit(10**8)
def take(t, keycol, cols, filt=None):
    p = f"{raw}/{t}_s{s}.tsv"; rows = []
    with open(p, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for c in rd:
            if len(c) < len(h) or c[ix[keycol]] != nat: continue
            if filt and not filt(ix, c): continue
            rows.append([c[ix[k]] for k in cols])
    with open(f"{out}/y_{t}.tsv", 'w', encoding='utf-8', newline='\n') as g:
        g.write('\t'.join(cols) + '\n')
        for r in rows: g.write('\t'.join(r) + '\n')
    return len(rows)
print('pxmonth', take('pxmonth', 'nation', ['time', 'year', 'priceLevelIndex', 'broadMoneyStock', 'treasuryStock', 'outstandingLoansStock', 'convertibilityShare', 'stapleRelPriceMedianRatio']))
print('class', take('class', 'nation', ['time', 'year', 'classId', 'className', 'sizeK', 'savingsStock', 'earnedPerMonth', 'paidPerMonth']))
print('factory', take('factory', 'nation', ['quarter', 'year', 'factoryCount', 'factoryBankruptCount', 'factoryActiveCount', 'capacityRefValuePerMonth', 'outputRefValuePerMonth', 'inputFillShare']))
print('goods', take('goods', 'nation', ['quarter', 'year', 'goodName', 'tier', 'lastProducedQty', 'lastDemandQty', 'lastSoldQty', 'lastImportQty', 'lastExportQty', 'stockQty', 'priceMean']))
print('stock', take('stock', 'nation', ['time', 'year', 'populationK', 'totalMoneyStock', 'treasuryStock', 'govDebtStock', 'gdpRealPerYear']))
print('mintcap', take('mintcap', 'nation', ['year', 'q', 'law', 'onGold', 'months', 'fiatM', 'capStock', 'loans']))
print('labor', take('labor', 'nation', ['quarter', 'year', 'lawCode', 'plainDemandPerMonth', 'plainSoldPerMonth', 'plainWageBillPerMonth', 'plainUnemploymentShare', 'farmDemandPerMonth', 'farmSoldPerMonth', 'farmWageBillPerMonth']))
