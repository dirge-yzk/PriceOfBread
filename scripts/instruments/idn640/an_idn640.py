# -*- coding: utf-8 -*-
"""(640) 線 idn の読み手：計器の表 idn_s（新しい強度の部品と判定が使った 3 項）を読む。
事前登録 measurements/idn640_20261028_prereg.txt の [4]（G1・G2・G3・G5）と R4・R7。
使い方：python an_idn640.py <ins のフォルダ> [first_q_open=31]
  G1＝行ごとに 種のうち 3 種以上で どれかの国×四半期の新しい強度が 0.15 以上（旗 0 の腕 A の idn_s で読む＝静的な前検査）
  R8（旧 G2）＝1783〜1790 年（q0〜q31）の主要 9 国の候補の強度から作る必要性の項が 10 未満のセルの割合（記録）。新 G2＝q≤31 に開いた国×種が 0 件（disc_s の E 行から別に数える）。
  G3＝主要 9 国×全四半期の s_soc と s_com の Pearson 相関（種ごと・0.9 未満が 8 種以上）
  G5＝旗を入れた腕で 主要 9 国のどれかが行を開いた種の数（disc_s の E 行から）
出力は標準出力。ファイル名は idn_s<種>.tsv。
"""
import csv, glob, math, re, sys, collections

sys.stdout.reconfigure(encoding="utf-8")
MAJ = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "日本", "清", "オスマン"]
HALF = 0.2
GATE = 0.15
NEAR0 = 10.0


def seed_of(path):
    return int(re.search(r"s(\d+)\.tsv$", path).group(1))


def pearson(xs, ys):
    n = len(xs)
    if n < 3:
        return float("nan")
    mx, my = sum(xs) / n, sum(ys) / n
    sxx = sum((x - mx) ** 2 for x in xs); syy = sum((y - my) ** 2 for y in ys)
    sxy = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    if sxx <= 1e-18 or syy <= 1e-18:
        return float("nan")
    return sxy / math.sqrt(sxx * syy)


def load(d):
    out = {}
    for f in sorted(glob.glob(d + "/idn_s*.tsv")):
        s = seed_of(f)
        rows = []
        for r in csv.DictReader(open(f, encoding="utf-8"), delimiter="\t"):
            rr = {k: (float(v) if k not in ("nation",) and v not in ("nan", "inf", "-inf") else (float("nan") if v == "nan" else v)) for k, v in r.items()}
            rr["nation"] = r["nation"]
            rows.append(rr)
        out[s] = rows
    return out


def main():
    d = sys.argv[1]
    q_open = int(sys.argv[2]) if len(sys.argv) > 2 else 31
    data = load(d)
    if not data:
        print("idn_s が無い:", d); return
    seeds = sorted(data)
    print("# 種:", seeds, "行数:", {s: len(v) for s, v in data.items()})
    rows_def = [("社会主義", "wageUnmet", "needSoc"), ("共産主義", "sComNew", "needCom"), ("ファシズム", "sFasNew", "needFas")]
    print("\n== G1（静的な前検査）：新しい強度の最大（全ての国×四半期）と 0.15 に届いた種の数／今の強度の最大と比べる")
    for name, col, _ in rows_def:
        reach = 0; line = []
        for s in seeds:
            mx = max((r[col] for r in data[s] if not math.isnan(r[col])), default=float("nan"))
            old = max((r["workerMil"] if col != "sFasNew" else r["sFasOld"] for r in data[s]), default=float("nan"))
            if mx >= GATE: reach += 1
            line.append(f"s{s}:{mx:.3f}(旧{old:.3f})")
        print(f"  {name}: 届いた種 {reach}/{len(seeds)}  " + " ".join(line))
    print("\n== R8（旧 G2 の降格・追記 1）：q0〜q%d の主要 9 国の候補の強度から作る必要性の項 70×s÷(s+%g) が %g 未満のセルの割合（前提待ち・危機 0 の影響を受けない）" % (q_open, HALF, NEAR0))
    for name, col, _ in rows_def:
        cells = [70.0 * r[col] / (r[col] + HALF) if r[col] > 0 else 0.0 for s in seeds for r in data[s] if r["nation"] in MAJ and r["quarter"] <= q_open]
        if cells:
            print(f"  {name}: {sum(1 for x in cells if x < NEAR0)}/{len(cells)} = {sum(1 for x in cells if x < NEAR0) / len(cells):.3f}  中央 {sorted(cells)[len(cells) // 2]:.2f}  最大 {max(cells):.2f}")
    print("  （新 G2＝門が閉じている：q≤%d に社会主義・共産主義・ファシズムの行を開いた国×種が 0 件〔disc_s の E 行〕。基線 m627／N1／N2 はいずれも 0 件）" % q_open)
    print("\n== G3：主要 9 国×全四半期の s_soc と s_com の相関（種ごと）／ 今の強度では同じ量")
    for s in seeds:
        xs = [r["wageUnmet"] for r in data[s] if r["nation"] in MAJ]; ys = [r["sComNew"] for r in data[s] if r["nation"] in MAJ]
        print(f"  s{s}: r={pearson(xs, ys):.3f}  (n={len(xs)})")
    print("\n== 部品の分布（主要 9 国の中央・年別）")
    hdr = ["wageShare", "wageUnmet", "workerSupplyFill", "ownerTake", "sComNew", "workerMil", "colonialShare", "crisis", "popMil", "decline", "sFasNew"]
    print("  年\t" + "\t".join(hdr))
    yrs = sorted({int(r["year"]) for s in seeds for r in data[s]})
    for y in [1783, 1790, 1800, 1820, 1840, 1860, 1880, 1900, 1920, 1940]:
        if y not in yrs: continue
        vals = []
        for h in hdr:
            xs = sorted(r[h] for s in seeds for r in data[s] if r["nation"] in MAJ and int(r["year"]) == y and not math.isnan(r[h]))
            vals.append(f"{xs[len(xs) // 2]:.3f}" if xs else "-")
        print(f"  {y}\t" + "\t".join(vals))
    print("\n== 国別（種を通した最大）：wageUnmet／sComNew／workerMil（今の強度）")
    for nat in MAJ:
        a = [r for s in seeds for r in data[s] if r["nation"] == nat]
        if a:
            print(f"  {nat}: wageUnmet 最大 {max(r['wageUnmet'] for r in a):.3f}  sComNew 最大 {max(r['sComNew'] for r in a):.3f}  workerMil 最大 {max(r['workerMil'] for r in a):.3f}  workerSupplyFill 中央 {sorted(r['workerSupplyFill'] for r in a if not math.isnan(r['workerSupplyFill']))[len(a)//2]:.3f}")
    print("\n== 一貫性：旗 0 のとき needSoc は 70×workerMil÷(workerMil+%g) と一致するか（計器の旧の強度と本体の強度の同一性）" % HALF)
    bad = 0; tot = 0
    for s in seeds:
        for r in data[s]:
            if r["knownSoc"] == 1: continue
            if r["techSoc"] == 0 and r["needSoc"] == 0 and r["transSoc"] == 0: continue   # 前提待ちなど判定に入らない行
            tot += 1
            exp = 70.0 * r["workerMil"] / (r["workerMil"] + HALF) if r["workerMil"] > 0 else 0.0
            if abs(exp - r["needSoc"]) > 1e-6: bad += 1
    print(f"  不一致 {bad}/{tot}（旗 0 の腕でだけ 0 のはず）")
    print("\n== R4 帝国主義：開幕（q0）の colonialShare＝0 の国の割合（全国）")
    q0 = [r for s in seeds for r in data[s] if r["quarter"] <= 3]
    if q0:
        z = sum(1 for r in q0 if r["colonialShare"] <= 0)
        print(f"  q≤3 の行 {len(q0)} 件のうち colonialShare＝0： {z} 件 ({z / len(q0):.3f})")


main()
