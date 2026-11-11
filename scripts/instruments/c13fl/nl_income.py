# (810) c13fl 腕の前の確かめ (2)（読むだけ・記録）：旧の形（形 B・旗 1）の種 16 で、名目 GDP 0 の四半期（1923〜1950）にネーデルラントの投資家の階級（3 資本家・10 商人・4 技師・9 医者）の
#   貯蓄へ入った額の出どころを flow_s の書き手（ファイル:行）ごとに足す。使い方：python -X utf8 -I nl_income.py <ins> <種> <国> <年0> <年1>
import sys, csv, collections
d, s, nat, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
INV = {'3', '10', '4', '9'}
pos = collections.Counter(); neg = collections.Counter(); allpos = collections.Counter()
with open(f'{d}/flow_s{s}.tsv', encoding='utf-8') as f:
    r = csv.reader(f, delimiter='\t'); next(r)
    for row in r:
        if row[2] != nat or not (y0 <= int(row[1]) <= y1): continue
        v = float(row[8]) * 3   # 四半期の額（月平均×3）
        k = f'{row[4]}|{row[5]}:{row[6]}|{row[7]}'
        if v > 0: allpos[(row[3], k)] += v
        if row[3] not in INV: continue
        (pos if v > 0 else neg)[k] += v
tp = sum(pos.values()); tn = sum(neg.values())
print(f'{nat} 種 {s} {y0}〜{y1}：投資家の階級の受け取り（正の流れ）の和 {tp:.6g}・払い（負の流れ）の和 {tn:.6g}（各国の通貨・四半期の額の和）')
print('受け取りの書き手（種類|ファイル:行|相手の側）\t額\t割合')
for k, v in pos.most_common(15): print(f'  {k}\t{v:.6g}\t{v / tp:.3f}')
print('払いの書き手\t額\t割合')
for k, v in sorted(neg.items(), key=lambda kv: kv[1])[:8]: print(f'  {k}\t{v:.6g}\t{v / tn:.3f}')
