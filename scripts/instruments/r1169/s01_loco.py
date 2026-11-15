# 読むだけ (1169)：S01-07 機関車の必要性の〈費用が立たない〉（disc_s の cost＜0＝NoPrice か非有限）の割合を 1880 年以後の機関車の行で数える。
# 使い方: python -X utf8 -I s01_loco.py <生のフォルダ> <種の数>
import sys, os, csv, collections, statistics
B, NS = sys.argv[1], int(sys.argv[2])
k = collections.Counter(); n = 0; bad = 0; ratios = []; miss = 0; kinds = collections.Counter()
for s in range(1, NS + 1):
    p = os.path.join(B, f'disc_s{s}.tsv')
    if not os.path.exists(p): miss += 1; continue
    for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
        kinds[r['kind']] += 1
        if r['row'] != '機関車' or int(r['year']) < 1880: continue
        n += 1; c = float(r['cost']); v = float(r['replaced'])
        if not (c >= 0): bad += 1
        elif c > 0 and v >= 0: ratios.append(v / c)
print(f'種 {NS - miss}/{NS}・disc の kind {dict(kinds)}・1880 年以後の機関車の行 {n}・費用が立たない {bad} = {bad / max(n, 1):.1%}・立った行の比（置き換える÷費用）の中央 {statistics.median(ratios) if ratios else float("nan"):.2f}')
