# (1358) 読むだけ：tsreg_s の生から、指定の国の指定の州（部分一致）の主食の行を 1 枚の表にする。
# 使い方: python -X utf8 -I tsreg_table.py <tsreg_s*.tsv のあるフォルダ> <国名> <州名の一部,...> [年,...]
# 出力：種ごとではなく全ての種を足した年×州×財の和（rows・止まり方・需要・輸入・不足）。年の引数に ALL を渡すと年を畳んで州×財の和にする。
import os, re, sys, collections
d, nation, pats = sys.argv[1], sys.argv[2], sys.argv[3].split(',')
fold = len(sys.argv) > 4 and sys.argv[4] == 'ALL'
years = set(sys.argv[4].split(',')) if len(sys.argv) > 4 and not fold else None
acc = collections.OrderedDict(); seeds = 0
for f in sorted(os.listdir(d)):
    if not re.fullmatch(r'tsreg_s\d+\.tsv', f): continue
    seeds += 1
    L = open(os.path.join(d, f), encoding='utf8').read().splitlines()
    H = L[0].split('\t')
    for l in L[1:]:
        r = dict(zip(H, l.split('\t')))
        if r['nation'] != nation or not any(p in r['region'] for p in pats): continue
        if years and r['year'] not in years: continue
        k = (r['region'], r['portFlag'], 'ALL' if fold else r['year'], r['good'])
        a = acc.setdefault(k, collections.Counter())
        for c in H[5:]: a[c] += float(r[c])
cols = ['rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'xSoldOut', 'xCap', 'xWedge', 'xPass', 'exCapMid', 'exDealt', 'demand', 'import', 'shortage']
print('seeds', seeds, '（全ての種の和）')
print('\t'.join(['region', 'port', 'year', 'good'] + cols))
for k in sorted(acc):
    if acc[k]['rows'] == 0: continue
    print('\t'.join(list(k) + ['%.6g' % acc[k][c] for c in cols]))
