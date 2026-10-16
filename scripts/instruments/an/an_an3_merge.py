# (372) 合流：線 an の生の出力を消す前に、反証役 an〔bv〕の中#4（主要国の人口の B÷A とヌルの幅）と低#5（群の分解の (c) 征服が 0 件）を読む。読むだけ。
# 使い方: PYTHONUTF8=1 python an_an3_merge.py <作業フォルダ(例 D:/PoB_work/an)> <出力txt>
import csv, sys, collections, statistics as st
W, OUT = sys.argv[1], sys.argv[2]
ARMS = ['A', 'B', 'N1', 'N2']; SEEDS = range(1, 11)
out = []; P = out.append
def med(v):
    v = [x for x in v if x is not None]
    return st.median(v) if v else None
def f(x, s='.3f'):
    return '-' if x is None else format(x, s)
def anchor(arm, s):
    d = {}
    with open(f"{W}/out_{arm}_10/ins/anchor_s{s}.tsv", encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for ln in r:
            d[(ln[ix['nation']], int(ln[ix['quarter']]))] = (int(ln[ix['born']]), float(ln[ix['popK']]), float(ln[ix['anchorRegions']]), int(ln[ix['anchorRegionsN']]), int(ln[ix['activeRegionsN']]))
    return d
def coll(arm, s):
    res = []
    with open(f"{W}/out_{arm}_10/ins/nation_s{s}.tsv", encoding='utf-8', errors='replace', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for ln in r:
            try:
                fill = float(ln[ix['supplyFillShare']]); pop = float(ln[ix['populationK']])
            except Exception:
                continue
            if fill < 0.3 and pop >= 100:
                res.append((ln[ix['nation']], int(ln[ix['quarter']])))
    return res
D = {a: {s: anchor(a, s) for s in SEEDS} for a in ARMS}
C = {a: {s: coll(a, s) for s in SEEDS} for a in ARMS}
P('# (372) 合流の読み：線 an の生の出力（D:/PoB_work/an/out_<腕>_10/ins の anchor_s と nation_s）')
P('')
P('## 1. 群の分解の (c) 征服の確かめ（反証役 an〔bv〕低#5）')
P('  an_an1.py の (c)＝崩壊したその四半期の anchorRegions が、その国の最初の行の 1.000001 倍超。下は同じ崩壊の行を 3 つの定義で数え直した。')
P('  (c1) 同じ定義（その四半期の州の和＞最初の行）／(c2) それまでに一度でも州の和が最初の行を超えた／(c3) 活きた州の数が最初の行より多い')
for a in ARMS:
    g = collections.Counter(); ratios = []
    for s in SEEDS:
        d = D[a][s]
        rows = collections.defaultdict(list)
        for (n, q), v in d.items():
            rows[n].append((q, v))
        first = {}; runmax = {}
        for n, lst in rows.items():
            lst.sort(key=lambda t: t[0]); first[n] = lst[0][1]
            m = -1.0; rm = {}
            for q, v in lst:
                m = max(m, v[2]); rm[q] = m
            runmax[n] = rm
        for (n, q) in C[a][s]:
            v = d.get((n, q))
            if v is None:
                g['不明'] += 1; continue
            if v[0] == 0 and v[3] == 0:
                g['a'] += 1; continue
            if v[0] == 1:
                g['b'] += 1; continue
            fr = first[n]
            if fr[2] > 0: ratios.append(v[2] / fr[2])
            g['d 計'] += 1
            if v[2] > fr[2] * (1 + 1e-6): g['c1'] += 1
            if runmax[n].get(q, 0) > fr[2] * (1 + 1e-6): g['c2'] += 1
            if v[4] > fr[4]: g['c3'] += 1
    P(f"  {a}: a {g['a']}・b {g['b']}・(c)(d) の計 {g['d 計']}（うち c1 {g['c1']}・c2 {g['c2']}・c3 {g['c3']}）・不明 {g['不明']}・(c)(d) の行の 州の和÷最初の行 の中央 {f(med(ratios))}・最大 {f(max(ratios) if ratios else None)}")
P('')
P('## 2. 主要国の人口の B÷A とヌルの幅（反証役 an〔bv〕中#4・H3 は小国 14 だけだった）')
P('  国ごとに 種ごとの popK(腕)÷popK(A) の中央（両方の腕でその四半期に行がある種だけ）。幅＝max(|N1−1|, |N2−1|)。B が幅の外なら ★')
MAJ = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'スペイン', 'ポルトガル', 'ネーデルラント', 'オスマン', '清', '日本', 'アメリカ', 'サルデーニャ＝ピエモンテ', 'デンマーク', 'スウェーデン']
for q in (524, 667):
    P(f"  四半期 {q}")
    P('  国\t種\tB÷A\tN1÷A\tN2÷A\t幅\t')
    for n in MAJ:
        r = {a: [] for a in ('B', 'N1', 'N2')}; ns = 0
        for s in SEEDS:
            va = D['A'][s].get((n, q))
            if not va or va[1] <= 0: continue
            ok = True
            for a in r:
                vb = D[a][s].get((n, q))
                if not vb: ok = False
            if not ok: continue
            ns += 1
            for a in r:
                r[a].append(D[a][s][(n, q)][1] / va[1])
        if ns == 0:
            P(f"  {n}\t0"); continue
        mb, m1, m2 = med(r['B']), med(r['N1']), med(r['N2'])
        w = max(abs(m1 - 1), abs(m2 - 1))
        P(f"  {n}\t{ns}\t{f(mb)}\t{f(m1)}\t{f(m2)}\t{f(w)}\t{'★' if abs(mb - 1) > w else ''}")
P('')
P('## 3. 補給崩壊の総数とヌルの幅（an_an1.py の H2 の再掲・旗 1 のヌル腕は回していない）')
for a in ARMS:
    P(f"  {a}: {sum(len(C[a][s]) for s in SEEDS)}・種ごと {[len(C[a][s]) for s in SEEDS]}")
with open(OUT, 'w', encoding='utf-8') as fh:
    fh.write('\n'.join(out) + '\n')
print('\n'.join(out))
