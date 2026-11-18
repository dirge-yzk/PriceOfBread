# (1203) 問い① 四半期ごとに〈開幕からある国の満足度の最小 <0.02〉の種の数（dcont_s・4 四半期ごとの標本）
# 使い方: python -X utf8 -I satbyq.py <生> <種0> <種1> <四半期の下限> <出力>
import sys, csv, collections
raw, s0, s1, qmin, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
cnt = collections.Counter(); seen = collections.Counter(); who = collections.defaultdict(list)
for s in range(s0, s1 + 1):
    byq = collections.defaultdict(list); first = None; born = set()
    with open(f'{raw}/dcont_s{s}.tsv', encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            q = int(row['quarter'])
            if first is None: first = q
            if q <= first + 3: born.add(row['nation'])
            if row['nation'] not in born or q < qmin: continue
            try: v = float(row['satMeanShare']); mk = float(row['massPopK'])
            except ValueError: continue
            if mk > 0: byq[q].append((v, row['nation']))
    for q, l in byq.items():
        if q % 4 != 3: continue
        seen[q] += 1; m = min(l)
        if m[0] < 0.02: cnt[q] += 1; who[q].append(f's{s}:{m[1]}')
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('quarter\tseeds\tmin_lt002_seeds\twho\n')
    for q in sorted(seen): o.write(f'{q}\t{seen[q]}\t{cnt[q]}\t{" ".join(who[q])}\n')
