# (1285) 問い4：〈民間の貨幣がほぼ 0・国庫は正〉の国×年（stock_s・人口 100 千人以上・totalMoneyStock＜1e-3×treasuryStock）を数える＝根が閉じたかの読み
# 使い方: python -X utf8 -I q4_lock.py <生> <種の起点> <種の数>
import sys, csv, collections
raw, sb, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tot = 0; per = collections.Counter()
for s in range(sb, sb + ns):
    for r in csv.DictReader(open(f"{raw}/stock_s{s}.tsv", encoding='utf-8'), delimiter='\t'):
        try: p, m, t = float(r['populationK']), float(r['totalMoneyStock']), float(r['treasuryStock'])
        except ValueError: continue
        if p >= 100 and t > 0 and m < 1e-3 * t: tot += 1; per[(s, r['nation'])] += 1
print(f"{raw} 種 {sb}..{sb+ns-1}: 国×年 {tot}・国 {len(per)}・" + ' '.join(f"s{k[0]}:{k[1]}:{v}" for k, v in per.most_common(12)))
