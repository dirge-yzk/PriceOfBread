# (1291) c133b1 記録（門にしない）：prodcap_s と region_s から数える（エンジンの関数を呼ばない・読む州と財は台本が自分で選ぶ）。
# 使い方: python -X utf8 -I rec_b1.py <生のフォルダ> <種の始め> <種の終わり>
# ① ジャマイカのコーヒーの産出（prodcap_s の年の断面・種ごと→中央・最小・最大）。
# ② 植民地格の州（region_s の tier=Colonial のその年のどれかの四半期）の賦存の RGO（isRgo=1・class E）のうち産出 0 の行の数と全行（種の和）。
import sys, csv, collections, statistics as st
csv.field_size_limit(1 << 30)
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
YEARS = (1790, 1830, 1870, 1913, 1940)
jam = collections.defaultdict(dict); zero = collections.Counter(); tot = collections.Counter()
for s in range(s0, s1 + 1):
    col = collections.defaultdict(set)
    with open(f'{raw}/region_s{s}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {c: i for i, c in enumerate(h)}
        for r in rd:
            try: y = int(r[ix['year']])
            except ValueError: continue
            if y in YEARS and r[ix['tier']] == 'Colonial': col[y].add(r[ix['region']])
    acc = collections.Counter()
    with open(f'{raw}/prodcap_s{s}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {c: i for i, c in enumerate(h)}
        for r in rd:
            try: y = int(r[ix['year']])
            except ValueError: continue
            if y not in YEARS: continue   # prodcap_s は年ごとの 1 断面（q は通しの四半期）
            out = float(r[ix['output']])
            if r[ix['region']] == 'ジャマイカ' and r[ix['good']] == 'コーヒー': acc[y] += out
            if r[ix['region']] in col[y] and r[ix['isRgo']] == '1' and r[ix['class']] == 'E':
                tot[y] += 1; zero[y] += not (out > 0)
    for y in YEARS: jam[y][s] = acc[y]
for y in YEARS:
    v = list(jam[y].values())
    print(f'{y}\tジャマイカのコーヒーの産出\t中央 {st.median(v):.4g}\t最小 {min(v):.4g}\t最大 {max(v):.4g}\t種 {len(v)}')
for y in YEARS:
    print(f'{y}\t植民地格の州の賦存の RGO（級 E）で産出 0\t{zero[y]}/{tot[y]}（種 {s0}〜{s1} の和）')
