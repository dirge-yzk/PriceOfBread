# (478) 線 dz：age_s（B 行・N 行）を読んで事前登録 [8] の G1-0・G1-1・G1-4・G1-6 を出す。層の数が 11 のときの 0 歳の層を読む（8 層は乳児の列が nan）。
# 使い方: PYTHONUTF8=1 python -I an_dz478.py <計器の生のフォルダ> [出力のファイル]    （種の中央値＝フォルダにある age_s<seed>.tsv 全部の中央値）
import csv, sys, glob, re, statistics as st, collections
NATIONS = ["イギリス", "フランス", "プロイセン", "ロシア", "日本", "清"]
ERAS = [("P1", 1783, 1799), ("P2", 1800, 1849), ("P3", 1850, 1899), ("P4", 1900, 1949)]
D = sys.argv[1]
out = open(sys.argv[2], "w", encoding="utf-8", newline="\n") if len(sys.argv) > 2 else sys.stdout
files = sorted(glob.glob(f"{D}/age_s*.tsv"))
per_seed = []
g10_bad = 0; g10_rows = 0
for fp in files:
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(fp, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            n = r["nation"]; y = int(r["year"])
            if r["scope"] == "N":
                g10_rows += 1
                pk = float(r["popK"])
                if abs(float(r["sumBandsMinusPopK"])) > 1e-9 * pk: g10_bad += 1
            if n not in NATIONS: continue
            for pname, a, b in ERAS:
                if a <= y <= b:
                    k = acc[(n, pname)]
                    if r["scope"] == "B":
                        bi = int(r["bandIndex"]); k["bands"] = max(k["bands"], bi + 1)
                        k[f"dead{bi}"] += float(r["deathsK"]); k["dead"] += float(r["deathsK"])
                        if bi == 0: k["births"] += float(r["birthsK"]); k["exp0"] += float(r["personYearsK"]); k["stock0"] += float(r["countK"])
                    else:
                        w = float(r["popK"]); k["pop"] += w; k["w"] += 1
                        k["pop0share"] += (float(r["infantShare"]) * w) if r["infantShare"] not in ("nan", "") else 0
    per_seed.append(acc)
out.write(f"G1-0: N 行 {g10_rows} 行のうち Σ層−人口 が 1e-9×人口 を超えた行 {g10_bad}（0 であること）  種 {len(files)}\n")
out.write("国\t期間\t層の数\t0歳の層の割合 s0\t粗出生 cbr\t乳児死亡(0歳の層の死÷出生)\tcbr×(1−乳児死亡/2)\ts0÷それ(G1-1: 0.8〜1.2)\t0〜5歳の死の割合(G1-6)\n")
for n in NATIONS:
    for pname, a, b in ERAS:
        rows = []
        for acc in per_seed:
            k = acc.get((n, pname))
            if not k or k["w"] == 0 or k["bands"] != 11 or k["births"] <= 0: continue
            # 窓の合計が四半期の行ごとの積み上げ＝期間全体の割合：s0＝期間の 0 歳の層の在庫の人口加重平均、cbr＝出生÷人年
            s0 = k["pop0share"] / k["pop"]
            imr = k["dead0"] / k["births"]
            rows.append((s0, imr, (k["dead0"] + k["dead1"]) / k["dead"] if k["dead"] > 0 else float("nan"), k["births"], k["pop"]))
        if not rows: continue
        s0 = st.median(r[0] for r in rows); imr = st.median(r[1] for r in rows); d05 = st.median(r[2] for r in rows)
        # 粗出生は N 行の popK（四半期の人口の和）でなく、四半期の人口の平均×四半期数が要る＝ここでは出生÷（人口の和÷4）で年あたり
        cbrs = [r[3] / (r[4] / 4.0) for r in rows]; cbr = st.median(cbrs)
        exp = cbr * (1 - imr / 2)
        out.write(f"{n}\t{pname}\t11\t{s0:.4f}\t{cbr:.4f}\t{imr:.4f}\t{exp:.4f}\t{s0 / exp:.3f}\t{d05:.3f}\n")
