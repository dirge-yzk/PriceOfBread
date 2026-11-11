# (1030) c97eb：突然変異 M3（Productivity も賦存で割る＝能力が 1 倍へ戻る）の紙＝基線の prodcap_s の能力の重みで share0（産地を持たない国の割合）を旧と M3 で出す。
# 使い方: python -X utf8 -I paperm3.py <基線の生> <種の範囲>
#   M3 の能力＝級 E・X の行の capacity÷endowFromProductivity（他の行はそのまま）。産出は能力に比例すると置く〈紙の近似〉。
import sys, csv, collections, statistics as st
csv.field_size_limit(1 << 30)
RAW, RNG = sys.argv[1:3]; lo, hi = map(int, RNG.split('-'))
for Y in (1870, 1913):
    for g in ('鉄鉱石', '石炭'):
        b0 = []; m3 = []
        for s in range(lo, hi + 1):
            f = open(f'{RAW}/prodcap_s{s}.tsv', encoding='utf-8', newline=''); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            cap = collections.defaultdict(float); capm = collections.defaultdict(float); en = set()
            for row in r:
                if int(row[ix['year']]) != Y or row[ix['good']] != g: continue
                n = row[ix['ownerNation']]; c = float(row[ix['capacity']]); e = float(row[ix['endowFromProductivity']])
                cl = row[ix['class']]
                if cl in ('E', 'X'): en.add(n)
                cap[n] += c; capm[n] += c / e if cl in ('E', 'X') and e > 0 else c
            T = sum(cap.values()); TM = sum(capm.values())
            b0.append(sum(v for n, v in cap.items() if n not in en) / T); m3.append(sum(v for n, v in capm.items() if n not in en) / TM)
        print(f'{Y} {g} share0（能力の重み・種の中央値）旧 {st.median(b0):.4f} M3 {st.median(m3):.4f} 差 {st.median(m3)-st.median(b0):+.4f}（G2 は新≦旧−0.05 で通る）')
