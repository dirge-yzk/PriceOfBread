#!/usr/bin/env python
"""(461) 発見の時計の細い傾斜：W0（DiscoveryClockWorldPerMonth）と Am（DiscoveryOwnHalfPerMonth）の予備計測の読み手と止める規則 K1〜K4。
事前登録 measurements/dk_20261020_prereg.txt [5] の規則どおり（予備計測の前に固定・予備計測を読んだ後に規則を改めない）。

入力：<計器の出力フォルダ>/clock_s<seed>.tsv（旗 0＝既定の世界の予備計測の走り・列は InstrumentsClock.cs）。
規則：
  W85  ＝ 最初の標本（1785 年）の W 行の worldTotalPerMonth の種の中央値。
  W_hi ＝ W の軌道（種の中央値・8 四半期ごとの標本）の 90 パーセンタイル（線形補間）。
  d_max＝ T 行に現れる d＞0 の行の d の最大（登録は 14.0）・d_min（登録は 2.0）。w＝0.1（固定）。
  W0   ＝ 0.8 × W_hi ÷ ( d_max × (1 + w/2) )。
  Am   ＝ 各行（d＞0）×種×{英・仏・プロイセン}で A_n＞0 のもの（行の年代 +5 年の ±2 年以内で最も近い T 行）の A_n の中央値 ÷ k（k＝s÷(1−s)・s＝√(55÷70)＝7.80）。
  K1   ＝ W_hi÷W85 ≧ 9.67（d＝2 の行が 1785 年に S＝0）。外れ＝腕 B を回さない。
  K2   ＝ 種の中央値の W の軌道が全ての d＞0 の行について W0·d·(1+w/2) 以上に届く。外れ＝腕 B を回さない。
  K3   ＝ W_hi÷W85 ≧ 10。外れ＝腕 B を回さない。
  K4   ＝ C 行（世界の合計の巡回の累計の秒と訪れた生産者）を 668 四半期へ伸ばした推定が 8 秒を超えたら巡回を軽くする案を追記に書く。
読み c（記録）：行（d）ごとに S＞0 になる年・S＝1 になる年と行の年代。読み a/b（記録）：枝 dd の規則・ln W の直線性。
使い方：PYTHONUTF8=1 python clock_census.py <計器の出力フォルダ> [種の番号...]
"""
import csv, glob, math, os, statistics, sys

S = math.sqrt(55.0 / 70.0)
K = S / (1.0 - S)
WIDTH = 0.1
NAMED = ["britainPerMonth", "francePerMonth", "prussiaPerMonth"]


def fl(x):
    try:
        return float(x)
    except (ValueError, TypeError):
        return float("nan")


def med(v):
    return statistics.median(v) if v else float("nan")


def pct(xs, p):
    s = sorted(xs)
    k = (len(s) - 1) * p / 100.0
    f = int(math.floor(k)); c = min(f + 1, len(s) - 1)
    return s[f] + (s[c] - s[f]) * (k - f)


def load(root, seeds):
    out = {}
    for f in sorted(glob.glob(os.path.join(root, "clock_s*.tsv"))):
        seed = os.path.basename(f)[len("clock_s"):-4]
        if seeds and seed not in seeds:
            continue
        with open(f, encoding="utf-8") as fh:
            out[seed] = [r for r in csv.DictReader(fh, delimiter="\t") if r.get("chinaPerMonth") is not None]
    return out


def main():
    args = sys.argv[1:]
    root, seeds = args[0], args[1:]
    data = load(root, seeds)
    if not data:
        print("!! clock_s*.tsv が無い")
        sys.exit(1)
    wtraj = {s: {int(r["year"]): fl(r["worldTotalPerMonth"]) for r in rows if r["kind"] == "W"} for s, rows in data.items()}
    years = sorted({y for t in wtraj.values() for y in t})
    wmed = {y: med([t[y] for t in wtraj.values() if y in t and t[y] == t[y]]) for y in years}
    y0 = years[0]
    w85 = med([t[y0] for t in wtraj.values() if y0 in t])
    whi = pct([wmed[y] for y in years], 90)
    print(f"# 種 {sorted(data)}・標本の年 {years[0]}〜{years[-1]}（{len(years)} 点）・W85 は {y0} 年の標本")
    print("year\tW(種の中央値)\tW/W85\t種ごと")
    for y in years:
        if y % 10 in (3, 5) or y in (y0, 1885, 1950):
            print(f"{y}\t{wmed[y]:.6g}\t{wmed[y] / w85:.4g}\t" + " ".join(f"{t.get(y, float('nan')):.4g}" for t in wtraj.values()))
    drows = sorted({(r["row"], int(r["decade"])) for rows in data.values() for r in rows if r["kind"] == "T" and int(r["decade"]) > 1780})
    ds = [(dec - 1780) / 10.0 for _, dec in drows]
    dmax, dmin = max(ds), min(ds)
    w0 = 0.8 * whi / (dmax * (1 + WIDTH / 2))
    print(f"# d＞0 の行 {len(drows)} 本（登録は 40 本）・d_max {dmax}（登録 14.0）・d_min {dmin}（登録 2.0）・d の中央値 {med(ds)}")
    print(f"# W85 ＝ {w85:.6g}   W_hi（P90）＝ {whi:.6g}（W_hi/W85 ＝ {whi / w85:.3f}・最大/W85 ＝ {max(wmed.values()) / w85:.3f}）")
    print(f"# W0 ＝ 0.8×W_hi÷(d_max×(1＋w/2)) ＝ {w0:.6g}（W85 の {w0 / w85:.4f} 倍）・w ＝ {WIDTH}")
    av = []
    for s, rows in data.items():
        byrow = {}
        for r in rows:
            if r["kind"] != "T" or int(r["decade"]) <= 1780:
                continue
            byrow.setdefault((r["row"], int(r["decade"])), []).append(r)
        for (row, dec), lst in byrow.items():
            r = min(lst, key=lambda t: (abs(int(t["year"]) - (dec + 5)), int(t["year"])))
            if abs(int(r["year"]) - (dec + 5)) > 2:
                continue
            for col in NAMED:
                a = fl(r[col])
                if a == a and a > 0:
                    av.append(a)
    am = med(av) / K if av else float("nan")
    print(f"# Am ＝ median(A_n>0)/k ＝ {am:.6g}（A_n の中央値 {med(av):.6g}・件数 {len(av)}・k＝{K:.3f}）")
    print(f"# 腕 B の係数：DiscoveryClockWorldPerMonth={w0:.6g};DiscoveryOwnHalfPerMonth={am:.6g}（w は既定 0.1）")
    r1 = whi / w85
    s1_0 = [row for row, dec in drows if w85 > w0 * ((dec - 1780) / 10.0) * (1 - WIDTH / 2)]
    print(f"K1 W_hi/W85 ＝ {r1:.3f}（線 9.67）・{y0} 年に S＞0 の行 {len(s1_0)} 本（{'・'.join(s1_0)}）→ {'通過' if r1 >= 9.67 and not s1_0 else '停止（腕 B を回さない）'}")
    unreached = []
    for row, dec in drows:
        d = (dec - 1780) / 10.0
        if not any(wmed[y] >= w0 * d * (1 + WIDTH / 2) for y in years):
            unreached.append(row)
    print(f"K2 S＝1 に届かない行 ＝ {len(unreached)} 本（{'・'.join(unreached)}）→ {'通過' if not unreached else '停止（腕 B を回さない）'}")
    print(f"K3 W_hi/W85 ＝ {r1:.3f}（線 10）→ {'通過' if r1 >= 10 else '停止（腕 B を回さない）'}")
    print("読み c row\tdecade\td\tS>0 の年\tS=1 の年\t(S=1 の年 − 行の年代)")
    for row, dec in sorted(drows, key=lambda t: t[1]):
        d = (dec - 1780) / 10.0
        y_lo = next((y for y in years if wmed[y] > w0 * d * (1 - WIDTH / 2)), None)
        y_hi = next((y for y in years if wmed[y] >= w0 * d * (1 + WIDTH / 2)), None)
        print(f"読み c {row}\t{dec}\t{d:g}\t{y_lo}\t{y_hi}\t{'' if y_hi is None else y_hi - dec}")
    wv = []
    for s, rows in data.items():
        byrow = {}
        for r in rows:
            if r["kind"] != "T" or int(r["decade"]) <= 1780:
                continue
            byrow.setdefault((r["row"], int(r["decade"])), []).append(r)
        for (row, dec), lst in byrow.items():
            d = (dec - 1780) / 10.0
            r = min(lst, key=lambda t: (abs(int(t["year"]) - (dec + 5)), int(t["year"])))
            if abs(int(r["year"]) - (dec + 5)) > 2:
                continue
            w = fl(r["worldTotalPerMonth"])
            if w == w and w > 0:
                wv.append(w / (K * d))
    if wv:
        print(f"# 読み a（記録）：median(W(dec+5)/(k×d)) ＝ {med(wv):.6g}・最大/最小 {max(wv) / min(wv):.2f}")
    xs = [y for y in years if y0 <= y <= 1885 and wmed[y] > 0]
    if len(xs) >= 4:
        def fit(xv, yv):
            n = len(xv); mx_ = sum(xv) / n; my_ = sum(yv) / n
            sxx = sum((a - mx_) ** 2 for a in xv); sxy = sum((a - mx_) * (b - my_) for a, b in zip(xv, yv)); syy = sum((b - my_) ** 2 for b in yv)
            return sxy / sxx, (sxy * sxy) / (sxx * syy) if syy > 0 else float("nan")
        sl, r2log = fit(xs, [math.log(wmed[y]) for y in xs])
        print(f"# 読み b（{y0}〜1885）：ln W の傾き {sl:.4f}/年（年率 {100 * (math.exp(sl) - 1):.2f}%）・ln W の直線への R2 {r2log:.3f}")
    print("K4 計算量（C 行）：")
    for s, rows in data.items():
        cr = [r for r in rows if r["kind"] == "C"]
        if not cr:
            continue
        last = cr[-1]
        sec, vis = fl(last["worldTotalPerMonth"]), fl(last["holders"])
        q = int(last["quarter"]) + 1
        est = sec * 668.0 / q
        print(f"   seed {s}：最後の C 行 quarter {last['quarter']}（{last['year']} 年）・巡回の累計 {sec:.3f} 秒・訪れた生産者 {vis:.4g} → 1 生産者あたり {1e6 * sec / vis:.4f} マイクロ秒・668 四半期へ伸ばした推定 {est:.3f} 秒（線 8 秒）→ {'通過' if est <= 8 else '巡回を軽くする案を追記に書く'}")


if __name__ == "__main__":
    main()
