#!/usr/bin/env python
"""(422) 発見の技術力の形の直し：W0（DiscoveryPoolHalfPerMonth）と Am（DiscoveryOwnHalfPerMonth）の census の読み手。事前登録 measurements/dd_20261019_prereg.txt [5] の規則どおり。

入力：<計器の出力>/pool_s<seed>.tsv（列＝kind quarter year row decade poolPerMonth holders ownNation ownPerMonth top1Nation top1PerMonth britainPerMonth francePerMonth prussiaPerMonth chinaPerMonth・旗0の走りで出す）。
規則：各行（d＝(年代−1780)/10 が正）×種について、行の年代 +5 年の ±tol 年（既定 2）以内で最も近い T の標本を読む。W≦0 は捨てる。
      k ＝ s÷(1−s)・s ＝ √(55÷70) ＝ 0.8864 ⇒ k ＝ 7.80。W0 ＝ （行×種の）W÷(k×d) の中央値。Am ＝ （行×種×{英・仏・プロイセン}で A＞0 のもの）の A の中央値 ÷ k。
追記 2（2026-10-19・腕の前）：W0 は median(W÷d)（S＝0.5 が行の年代 +5 年の中央値の行）に改めた。k の規則は W0 には使わない（Am には使う）。理由は prereg の追記 2。
記録：観測の数・捨てた数・W0 と Am の四分位・行ごとの W÷(k×d)（行間の開き）。
使い方：PYTHONUTF8=1 python pool_census.py <計器の出力フォルダ> [--tol=2] [種の番号...]
   --t0 <W0> <Am>：1785 年（Q8）の標本で、engineer_s の各国×行の A と pool_s の W から 70×S×C を求め、0.786×70 以上の国×行の数と国別の内訳（狙い T0）。
"""
import csv, glob, math, os, statistics, sys

S = math.sqrt(55.0 / 70.0)
K = S / (1.0 - S)
PW = {"britainPerMonth": "イギリス", "francePerMonth": "フランス", "prussiaPerMonth": "プロイセン"}


def fl(x):
    try:
        v = float(x)
    except ValueError:
        return float("nan")
    return v


def load(root, seeds):
    files = sorted(glob.glob(os.path.join(root, "pool_s*.tsv")))
    if seeds:
        files = [f for f in files if os.path.basename(f)[len("pool_s"):-4] in seeds]
    out = []
    for f in files:
        seed = os.path.basename(f)[len("pool_s"):-4]
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("chinaPerMonth") is None or r.get("kind") != "T":
                    continue  # 書きかけの末尾の行・E の行は規則の読みに使わない
                out.append((seed, r))
    return out


def med(v):
    return statistics.median(v) if v else float("nan")


def quart(v):
    if len(v) < 4:
        return (float("nan"),) * 3
    q = statistics.quantiles(v, n=4)
    return q[0], q[1], q[2]


def census(root, tol, seeds):
    rows = load(root, seeds)
    obs = {}
    for seed, r in rows:
        dec = int(r["decade"])
        if not (dec - 1780) > 0:
            continue
        obs.setdefault((seed, r["row"], dec), []).append(r)
    wv, wh, av, dropped = [], [], [], []
    per = []
    for (seed, row, dec), lst in sorted(obs.items()):
        d = (dec - 1780) / 10.0
        target = dec + 5
        r = min(lst, key=lambda t: (abs(int(t["year"]) - target), int(t["year"])))
        if abs(int(r["year"]) - target) > tol:
            dropped.append((row, dec, seed, r["year"]))
            continue
        w = fl(r["poolPerMonth"])
        if not w > 0:
            dropped.append((row, dec, seed, r["year"] + "(W<=0)"))
            continue
        wv.append(w / (K * d))
        wh.append(w / d)
        per.append((row, dec, seed, int(r["year"]), w, w / (K * d)))
        for col in PW:
            a = fl(r[col])
            if a > 0:
                av.append(a)
    print("row\tdecade\tseed\tyear\tW\tW/(k*d)")
    for t in per:
        print(f"{t[0]}\t{t[1]}\t{t[2]}\t{t[3]}\t{t[4]:.6g}\t{t[5]:.6g}")
    w0 = med(wv)
    am = med(av) / K if av else float("nan")
    print(f"# k={K:.4f}（s=√(55/70)={S:.4f}）許容幅 ±{tol} 年・観測 {len(wv)} 件（捨てた {len(dropped)} 件）")
    print(f"# W0 = median(W/(k*d)) = {w0:.6g}   四分位 {quart(wv)}")
    print(f"# 追記 2 の規則（W0 は半分点そのもの＝行の年代 +5 年の中央値の行で S＝0.5）：W0 = median(W/d) = {med(wh):.6g}   四半期 {quart(wh)}")
    print(f"# Am = median(A_power, A>0)/k = {am:.6g}   （A の中央値 {med(av):.6g}・件数 {len(av)}・四分位 {quart(av)}）")
    if dropped:
        print("# 捨てた:", dropped[:20], "…" if len(dropped) > 20 else "")
    return w0, am


def t0(root, w0, am, seeds):
    """1785 年（Q8）の標本：engineer_s の A と pool_s の W（同じ四半期・同じ行）から 70×S×C。"""
    # pool: (seed,quarter,row)->W
    pool = {}
    for seed, r in load(root, seeds):
        y = int(r["year"])
        if y <= 1785:   # pool_s の quarter は tick の四半期（最初の T は 8＝1785 年）。engineer_s の quarter とは別の数え方なので年で突き合わせる
            pool[(seed, y, r["row"])] = (fl(r["poolPerMonth"]), int(r["decade"]))
    cnt = {}
    n = 0
    hi = []
    for f in sorted(glob.glob(os.path.join(root, "engineer_s*.tsv"))):
        seed = os.path.basename(f)[len("engineer_s"):-4]
        if seeds and seed not in seeds:
            continue
        with open(f, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("knownFlag") is None or r["kind"] != "T" or int(r["year"]) > 1785:
                    continue
                key = (seed, int(r["year"]), r["row"])
                if key not in pool:
                    continue
                w, dec = pool[key]
                d = (dec - 1780) / 10.0
                a = fl(r["engineerPerMonth"])
                if not d > 0 or not a == a:
                    continue
                s = w / (w + w0 * d) if w > 0 else 0.0
                c = a / (a + am) if a > 0 else 0.0
                n += 1
                if 70.0 * s * c >= 55.0:
                    hi.append((seed, int(r["year"]), r["row"], r["nation"], a, w, 70.0 * s * c))
                    cnt[r["nation"]] = cnt.get(r["nation"], 0) + 1
    print(f"# T0：1785 年（Q8）の標本 {n} 組・70×S×C ≧ 55 の国×行＝{len(hi)} 件・国別 {sorted(cnt.items(), key=lambda kv: -kv[1])[:8]}")
    for t in hi[:40]:
        print("#  ", t)


def main():
    args = sys.argv[1:]
    tol, seeds, root, t0args = 2, [], None, None
    i = 0
    while i < len(args):
        a = args[i]
        if a.startswith("--tol="):
            tol = int(a[6:])
        elif a == "--t0":
            t0args = (float(args[i + 1]), float(args[i + 2])); i += 2
        elif root is None:
            root = a
        else:
            seeds.append(a)
        i += 1
    if t0args:
        t0(root, t0args[0], t0args[1], seeds)
    else:
        census(root, tol, seeds)


if __name__ == "__main__":
    main()
