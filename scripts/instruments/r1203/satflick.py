# (1203) 問い① 満足度・最小国の〈最後の四半期の一点〉の当たりやすさを dcont_s の satMeanShare で数える（近似：SpawnedDuringPlay・瓦礫国の除外は nation_s の州・人口で近似しない＝dcont の行の国そのまま）
# 開幕の 4 四半期（dcont は 9 国の外を 4 四半期ごとに書く）に dcont に無い国＝プレイ中に生まれた国として除く（SpawnedDuringPlay の近似）
# 使い方: python -X utf8 -I satflick.py <生のフォルダ> <種の最初> <種の最後> <年の下限> <出力>
import sys, csv, collections
raw, s0, s1, y0, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
out = open(outp, 'w', encoding='utf-8', newline='\n')
out.write('seed\tquarter_count\tmin_lt002_share\tmin_lt005_share\tfinal_min\tfinal_min_nation\tworst_nations_by_lt002_share\n')
tot = collections.Counter()
for s in range(s0, s1 + 1):
    byq = collections.defaultdict(list); first=None; born=set()
    with open(f'{raw}/dcont_s{s}.tsv', encoding='utf-8') as f:
        r = csv.DictReader(f, delimiter='\t')
        for row in r:
            y = int(row['year'])
            q0 = int(row['quarter'])
            if first is None: first = q0
            if q0 <= first + 3: born.add(row['nation'])
            if row['nation'] not in born: continue
            if y < y0: continue
            try: v = float(row['satMeanShare']); mk = float(row['massPopK'])
            except ValueError: continue
            if not (mk > 0): continue
            byq[int(row['quarter'])].append((v, row['nation']))
    qs = sorted(byq)
    lt2 = sum(1 for q in qs if min(byq[q])[0] < 0.02)
    lt5 = sum(1 for q in qs if min(byq[q])[0] < 0.05)
    nat = collections.Counter(); natn = collections.Counter()
    for q in qs:
        for v, n in byq[q]:
            natn[n] += 1
            if v < 0.02: nat[n] += 1
    worst = sorted(((nat[n] / natn[n], n) for n in nat), reverse=True)[:4]
    fm = min(byq[qs[-1]])
    tot['q'] += len(qs); tot['lt2'] += lt2; tot['lt5'] += lt5; tot['seeds'] += 1; tot['finalfail'] += fm[0] < 0.02
    out.write(f"{s}\t{len(qs)}\t{lt2/len(qs):.3f}\t{lt5/len(qs):.3f}\t{fm[0]:.4f}\t{fm[1]}\t{'・'.join(f'{n}:{p:.2f}' for p, n in worst)}\n")
out.write(f"ALL\t{tot['q']}\t{tot['lt2']/tot['q']:.3f}\t{tot['lt5']/tot['q']:.3f}\tfinal_lt002={tot['finalfail']}/{tot['seeds']}\t\t\n")
