#!/usr/bin/env python
"""(370) 後半：割合（BasketFill の日用品の側）の目安＝dailyPaid÷dailyWant の分布（読むだけ・ce_s の C 行）。
  python an_ce_frac0370.py <ins のフォルダ> [種の数=10]
  出す物：国（英・仏・清）×階級ごとに Q5〜Q40 の四半期で dailyPaid÷dailyWant が 0.35 を超える割合・中央値・最大。
  事前登録 measurements/ce370_20261016_prereg.txt 追記 1-2 の〈測る量の追加〉。
  ⚠限界：台帳の NeedMet÷NeedTotal（量の比）ではなく値段の比の目安（ce_s に割合の列は無い）。国×階級×四半期の月平均＝POP ごとでない。"""
import sys, csv, collections, statistics
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1].replace(chr(92), '/').rstrip('/') + '/'
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
rows = collections.defaultdict(list)
for s in range(1, NS + 1):
    with open(D + f'ce_s{s}.tsv', encoding='utf-8', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['scope'] != 'C' or r['nation'] not in ('イギリス', 'フランス', '清') or not (5 <= int(r['quarter']) <= 40):
                continue
            try:
                w = float(r['dailyWantPerMonth']); p = float(r['dailyPaidPerMonth'])
            except ValueError:
                continue
            if w > 0:
                rows[(r['nation'], r['className'])].append(p / w)
print('国 階級 四半期数 中央値 最大 0.35超の割合')
for k in sorted(rows):
    v = rows[k]
    print(k[0], k[1], len(v), '%.3f' % statistics.median(v), '%.3f' % max(v), '%.3f' % (sum(1 for x in v if x > 0.35) / len(v)))
