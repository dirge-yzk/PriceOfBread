#!/usr/bin/env python
"""(430) 発見の時計の形 C1：W0（DiscoveryWorldEngineerHalfPerMonth）と Am（DiscoveryOwnHalfPerMonth）の census の読み手と止める規則 S1〜S4。
事前登録 measurements/dw_20261019_prereg.txt [5] の規則どおり（census の前に固定・census を読んだ後に規則を改めない）。

入力：<計器の出力フォルダ>/clock_s<seed>.tsv（旗 0 の census の走り・列は InstrumentsClock.cs）。
規則：
  W85  ＝ 1785 年の W 行の worldTotalPerMonth の種の中央値。
  d_med＝ T 行に現れる d＞0 の行（40 本）の d の中央値（登録は 10.0）。
  W0   ＝ 9 × W85 ÷ d_med。
  Am   ＝ 各行（d＞0）×種×{英・仏・プロイセン}で A_n＞0 のもの（行の年代 +5 年の ±2 年以内で最も近い T 行）の A_n の中央値 ÷ k（k＝s÷(1−s)・s＝√(55÷70)＝7.80）。
  S1   ＝ G ＝ W(1885)÷W85（種の中央値）。G＜9 なら腕 B を回さない。9≦G＜13.5 は警告。
  S2   ＝ 種の中央値の W の軌道（1785〜1950）で、行ごとの S の最大。最大＜30÷70＝0.43 の行が 5 本を超えたら腕 B を回さない。
  S3   ＝ 最大の W ÷ W85 が 10 未満なら腕 B を回さない。
  S4   ＝ C 行（世界の合計の巡回の累計の秒と訪れた生産者）を 668 四半期へ伸ばした推定が 3.0 秒を超えるなら巡回を軽くする案を追記に書く。
記録だけ：枝 dd の規則（W(dec+5)÷(k×d) の中央値）・その行ごとの開き・ln W の直線性・S＝0.5 に届く年と行の年代の差。
使い方：PYTHONUTF8=1 python clock_census.py <計器の出力フォルダ> [種の番号...]
"""
import csv, glob, math, os, statistics, sys

S = math.sqrt(55.0 / 70.0)
K = S / (1.0 - S)
NAMED = {"britainPerMonth": "英", "francePerMonth": "仏", "prussiaPerMonth": "普"}


def fl(x):
    try:
        return float(x)
    except (ValueError, TypeError):
        return float("nan")


def med(v):
    return statistics.median(v) if v else float("nan")


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
    # W の軌道（種ごと・year → W）
    wtraj = {s: {int(r["year"]): fl(r["worldTotalPerMonth"]) for r in rows if r["kind"] == "W"} for s, rows in data.items()}
    years = sorted({y for t in wtraj.values() for y in t})
    wmed = {y: med([t[y] for t in wtraj.values() if y in t and t[y] == t[y]]) for y in years}
    w85 = med([t[1785] for t in wtraj.values() if 1785 in t])
    print(f"# 種 {sorted(data)}・標本の年 {years[0]}〜{years[-1]}（{len(years)} 点）")
    print("year\tW(種の中央値)\tW/W85\t種ごと")
    for y in years:
        if y % 10 in (3, 5) or y in (1785, 1885, 1950):
            print(f"{y}\t{wmed[y]:.6g}\t{wmed[y] / w85:.4g}\t" + " ".join(f"{t.get(y, float('nan')):.4g}" for t in wtraj.values()))
    # d の中央値
    decs = sorted({int(r["decade"]) for rows in data.values() for r in rows if r["kind"] == "T"})
    drows = sorted({(r["row"], int(r["decade"])) for rows in data.values() for r in rows if r["kind"] == "T" and int(r["decade"]) > 1780})
    ds = [(dec - 1780) / 10.0 for _, dec in drows]
    dmed = med(ds)
    w0 = 9.0 * w85 / dmed
    print(f"# d＞0 の行 {len(drows)} 本・d の中央値 {dmed}（登録は 40 本・10.0）")
    print(f"# W85 ＝ {w85:.6g}   W0 ＝ 9×W85÷d_med ＝ {w0:.6g}")
    # Am
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
    # S1
    g = wmed.get(1885, float("nan")) / w85
    verdict = "停止（G<9）" if g < 9 else ("警告（9≦G<13.5）" if g < 13.5 else "通過")
    print(f"S1 G ＝ W(1885)/W85 ＝ {g:.3f} → {verdict}")
    # S3
    peak = max(wmed.values())
    ypk = [y for y in years if wmed[y] == peak][0]
    print(f"S3 最大の W/W85 ＝ {peak / w85:.2f}（{ypk} 年）→ {'停止（<10）' if peak / w85 < 10 else '通過'}")
    # S2: 行ごとの S の最大（種の中央値の W の軌道）
    print("S2 row\tdecade\td\tmaxS\tfirstS>=0.43\tfirstS>=0.5\tS(1785)\tS(dec+5)")
    never = 0; lows = []
    for row, dec in sorted(drows, key=lambda t: t[1]):
        d = (dec - 1780) / 10.0
        ss = {y: (wmed[y] / (wmed[y] + w0 * d)) if wmed[y] > 0 else 0.0 for y in years}
        mx = max(ss.values())
        y43 = next((y for y in years if ss[y] >= 0.43), None)
        y50 = next((y for y in years if ss[y] >= 0.5), None)
        yy = min(years, key=lambda y: abs(y - (dec + 5)))
        if mx < 30.0 / 70.0:
            never += 1; lows.append(row)
        print(f"S2 {row}\t{dec}\t{d:g}\t{mx:.3f}\t{y43}\t{y50}\t{ss[1785]:.3f}\t{ss[yy]:.3f}")
    print(f"S2 max S ＜ 0.43 の行 ＝ {never} 本（{'・'.join(lows)}）→ {'停止（>5）' if never > 5 else '通過'}")
    # 記録：枝 dd の規則
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
        print(f"# 記録（枝 dd の規則）：median(W(dec+5)/(k×d)) ＝ {med(wv):.6g}・最小 {min(wv):.4g}・最大 {max(wv):.4g}・最大/最小 {max(wv) / min(wv):.2f}（読み a）")
    # 読み b：ln W の直線性（1785〜1885）
    xs = [y for y in years if 1785 <= y <= 1885 and wmed[y] > 0]
    if len(xs) >= 4:
        def fit(xv, yv):
            n = len(xv); mx_ = sum(xv) / n; my_ = sum(yv) / n
            sxx = sum((a - mx_) ** 2 for a in xv); sxy = sum((a - mx_) * (b - my_) for a, b in zip(xv, yv)); syy = sum((b - my_) ** 2 for b in yv)
            slope = sxy / sxx
            return slope, (sxy * sxy) / (sxx * syy) if syy > 0 else float("nan")
        sl, r2log = fit(xs, [math.log(wmed[y]) for y in xs])
        sl2, r2lin = fit(xs, [wmed[y] for y in xs])
        print(f"# 読み b（1785〜1885）：ln W の傾き {sl:.4f}/年（年率 {100 * (math.exp(sl) - 1):.2f}%）・ln W の直線への R2 {r2log:.3f}・W の直線への R2 {r2lin:.3f}")
    # S4
    print("S4 計算量（C 行）：")
    for s, rows in data.items():
        cr = [r for r in rows if r["kind"] == "C"]
        if not cr:
            continue
        last = cr[-1]
        sec, vis = fl(last["worldTotalPerMonth"]), fl(last["holders"])
        q = int(last["quarter"]) + 1
        print(f"   seed {s}：最後の C 行 quarter {last['quarter']}（{last['year']} 年）・巡回の累計 {sec:.3f} 秒・訪れた生産者 {vis:.4g} → 1 生産者あたり {1e6 * sec / vis:.4f} マイクロ秒・668 四半期へ伸ばした推定 {sec * 668.0 / q:.3f} 秒")


if __name__ == "__main__":
    main()
