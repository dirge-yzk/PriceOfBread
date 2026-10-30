# (687) 線 urb2 後半・第 3 版：M2（失業）と M3'（飢えの死）の読み手。読むだけ。
# 使い方：python gates3.py <基線> <N1> <N2> [<腕> --seeds=1-10]   ・出力は標準出力。
# M2＝9 国の失業（labor_s plainUnemploymentShare・全四半期の平均・種の中央値）。
# M3'＝9 国×5 窓の飢えの死亡率（pop_s scope=N の deathsFamineK の窓の和 ÷ popK の窓の平均÷窓の年数・千人あたり年・種の中央値）。両側（流れの符号と無関係）で測れる。
# 外の定義：腕の値 v が、ヌル 3 本（基線・N1・N2）の最大 hi・幅 w＝hi−lo に対し v > hi + max(2w, F)。F は各指標のヌル幅の最大（データ由来の床・AI が置いた）。
import sys, csv, statistics as st, collections
NAT9 = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "日本", "清", "アメリカ"]
EPS = [(1783, 1815), (1815, 1850), (1850, 1880), (1880, 1914), (1914, 1951)]
def ep(y):
    for a, b in EPS:
        if a <= y < b: return f"{a}-{b-1}"
args = [a for a in sys.argv[1:] if not a.startswith("--")]
seeds = list(range(1, 11))
for a in sys.argv[1:]:
    if a.startswith("--seeds="):
        lo, hi = a[8:].split("-"); seeds = list(range(int(lo), int(hi) + 1))
def m2(d, seeds):
    per = collections.defaultdict(list)
    for s in seeds:
        with open(f"{d}/labor_s{s}.tsv", encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ina, ic = h.index("nation"), h.index("plainUnemploymentShare")
            acc = collections.defaultdict(list)
            for row in r:
                if row[ina] not in NAT9: continue
                try: v = float(row[ic])
                except: continue
                if v == v: acc[row[ina]].append(v)
            for n, l in acc.items(): per[n].append(st.mean(l))
    return {n: st.median(l) for n, l in per.items()}
def m3(d, seeds):
    per = collections.defaultdict(list)
    for s in seeds:
        with open(f"{d}/pop_s{s}.tsv", encoding="utf-8") as f:
            acc = collections.defaultdict(lambda: [0.0, 0.0, 0])
            for row in csv.DictReader(f, delimiter="\t"):
                if row["scope"] != "N" or row["nation"] not in NAT9: continue
                k = (row["nation"], ep(int(row["year"])))
                try: acc[k][0] += float(row["deathsFamineK"]); acc[k][1] += float(row["popK"]); acc[k][2] += 1
                except: pass
            for k, (dth, pop, q) in acc.items():
                if q > 0 and pop > 0: per[k].append(dth / (pop / q) / (q / 4) * 1000)   # 千人あたり年
    return {k: st.median(l) for k, l in per.items()}
def widths(vals):
    keys = [k for k in vals[0] if all(k in v for v in vals)]
    return {k: (max(v[k] for v in vals), max(v[k] for v in vals) - min(v[k] for v in vals)) for k in keys}
def outs(arm, nullvals, F):
    w = widths(nullvals); n = 0; lst = []
    for k, (hi, wd) in w.items():
        if k in arm and arm[k] > hi + max(2 * wd, F): n += 1; lst.append(k)
    return n, len(w), lst
def loo(vals, F):
    res = []
    for i in range(3):
        o = [vals[j] for j in range(3) if j != i]
        res.append(outs(vals[i], o, F)[:2])
    return res
nulls = [x.rstrip("/") for x in args[:3]]; arms = [x.rstrip("/") for x in args[3:]]
for name, fn in (("M2", m2), ("M3'", m3)):
    v = [fn(d, seeds) for d in nulls]
    w = widths(v); F = max(wd for _, wd in w.values())
    print(f"{name}：ヌル 3 本の幅の最大 F={F:.5f}（データ由来の床）・ヌル幅の中央値 {st.median(wd for _, wd in w.values()):.5f}")
    print(f"  一つ抜きの外の数（腕＝基線・N1・N2 の順）:", loo(v, F), "  ←門 M2＝9 国中 5 以上・M3'＝45 窓中 14 以上")
    for a in arms:
        av = fn(a, seeds); n, tot, lst = outs(av, v, F)
        print(f"  腕 {a.split('/')[-1]}：外 {n} / {tot}", [f"{k}" for k in lst])
