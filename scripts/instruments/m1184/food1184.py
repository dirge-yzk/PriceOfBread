# (1184) 合流：戦時の主食の不足（記録・門ではない）＝(1174) 反証役 v2 の物差し。
#   nation_s の atWarMonths>0 の国×四半期のうち foodMetShare<0.9 の割合（人口 1 万以上・非有限の行は除く）と、平時の同じ割合。
#   python -X utf8 -I food1184.py <ins> <種の起点> <種の終わり> [<ins> <起点> <終わり> ...]（3 つ組を並べると腕ごとに 1 行）
import csv, math, os, sys
sys.stdout.reconfigure(newline='\n')
a = sys.argv[1:]
for i in range(0, len(a), 3):
    B, sb, se = a[i], int(a[i + 1]), int(a[i + 2])
    w = [0, 0]; p = [0, 0]
    for s in range(sb, se + 1):
        f = os.path.join(B, f'nation_s{s}.tsv')
        if not os.path.exists(f): continue
        for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
            try: fm = float(r['foodMetShare']); pk = float(r['populationK']); aw = float(r['atWarMonths'])
            except (ValueError, KeyError): continue
            if not (math.isfinite(fm) and pk >= 10): continue
            t = w if aw > 0 else p
            t[0] += 1; t[1] += 1 if fm < 0.9 else 0
    print(f'{B}\t戦時 {w[1]}/{w[0]}={w[1]/max(w[0],1):.4f}\t平時 {p[1]}/{p[0]}={p[1]/max(p[0],1):.4f}')
