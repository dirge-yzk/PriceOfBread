# (472) 段0の読み：m466 の計器つき 10 種の pop_s から、国×期間の粗出生・粗死亡（平常＋飢え＋他）・0〜9 歳の死を種の中央値で出す。
# 使い方: python -I pop466.py <計器の生のフォルダ> [出力のファイル]
import csv, sys, statistics as st, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める
NATIONS = ["イギリス", "フランス", "プロイセン", "ロシア", "日本", "清"]
D = sys.argv[1]
eras = [(1783, 1799), (1800, 1849), (1850, 1899), (1900, 1949)]
acc = collections.defaultdict(lambda: collections.defaultdict(list))  # (国, 期間) -> 列 -> 種ごとの値
names = set()
for s in range(1, 11):
    per = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(f"{D}/pop_s{s}.tsv", encoding="utf-8") as f:
        r = csv.DictReader(f, delimiter="\t")
        for row in r:
            if row["scope"] != "N":
                continue
            y = int(row["year"]); n = row["nation"]; names.add(n)
            for i, (a, b) in enumerate(eras):
                if a <= y <= b:
                    k = per[(n, i)]
                    k["pop"] += float(row["popK"] or 0) * float(row["monthsCount"] or 0) / 12.0
                    for c in ["birthsK", "deathsNormalK", "deathsFamineK", "deathsOtherK", "warDeadK", "deathsFrom0K"]:
                        try: k[c] += float(row[c] or 0)
                        except ValueError: pass
                    # band0 population approx: popK*share0to14*(10/15)
                    try: k["b0pop"] += float(row["popK"]) * float(row["share0to14"]) * (10/15) * float(row["monthsCount"]) / 12.0
                    except ValueError: pass
    for key, k in per.items():
        if k["pop"] <= 0: continue
        t = acc[key]
        t["cbr"].append(1000 * k["birthsK"] / k["pop"])
        tot = k["deathsNormalK"] + k["deathsFamineK"] + k["deathsOtherK"]
        t["cdr"].append(1000 * tot / k["pop"])
        t["fam"].append(1000 * k["deathsFamineK"] / k["pop"])
        t["oth"].append(1000 * k["deathsOtherK"] / k["pop"])
        t["mu0"].append(1000 * k["deathsFrom0K"] / k["b0pop"] if k["b0pop"] > 0 else float("nan"))
out = open(sys.argv[2], "w", encoding="utf-8", newline="\n") if len(sys.argv) > 2 else sys.stdout
for n in NATIONS:
    for i, (a, b) in enumerate(eras):
        t = acc.get((n, i))
        if not t: continue
        med = {c: st.median(v) for c, v in t.items()}
        print(f"{n}\t{a}-{b}\tCBR {med['cbr']:.1f}\tCDR {med['cdr']:.1f}\tfam {med['fam']:.2f}\toth {med['oth']:.2f}\tmu0(0-9) {med['mu0']:.1f}\tn={len(t['cbr'])}", file=out)
