"""(546) 線 iwf の読み手：事前登録 measurements/iwf546_20261025_prereg.txt の M1〜M3 と記録 R1〜R9 を、計器つきの生（<出力>/ins）から読む。
使い方：PYTHONUTF8=1 python scripts/instruments/iwf546/judge.py <A の ins> [<B の ins>]
  引数 1 つ＝その腕の値だけ（種ごと・10 種の中央値）。引数 2 つ＝A と B を並べ、M1〜M3 の判定（B の値と A の前提）を出す。
読むだけ。シミュは回さない。種はフォルダの iwf_s<種>.tsv から拾う。中央値＝昇順の真ん中（偶数個は中 2 つの平均）。
"""
import csv, glob, os, re, statistics, sys

NATS = ["清", "マラータ同盟"]
EN = "イギリス"


def load(d, f, seed):
    p = os.path.join(d, f"{f}_s{seed}.tsv")
    if not os.path.exists(p):
        return []
    with open(p, encoding="utf-8", newline="") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))


def seeds_of(d):
    return sorted(int(re.search(r"iwf_s(\d+)\.tsv", p).group(1)) for p in glob.glob(os.path.join(d, "iwf_s*.tsv")))


def fl(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float("nan")


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float("nan")


def wmean(rows, key):
    num = sum(fl(r[key]) * fl(r["regionMonths"]) * fl(r["avgPopK"]) for r in rows)
    den = sum(fl(r["regionMonths"]) * fl(r["avgPopK"]) for r in rows)
    return num / den if den > 0 else float("nan")


def per_seed(d):
    """腕 d の種ごとの値の辞書。"""
    out = {}
    for s in seeds_of(d):
        iw = load(d, "iwf", s); lab = load(d, "labor", s); ivf = load(d, "ivf", s)
        disc = load(d, "disc", s); cls = load(d, "class", s); pop = load(d, "pop", s)
        v = {}
        for n in NATS:
            w = [r for r in iw if r["phase"] == "w" and r["nation"] == n and r["tier"] == "M"]
            g = [r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "M" and int(r["t"]) <= 11]
            v[f"M1_{n}"] = wmean(w, "closedShare")
            v[f"M3_{n}"] = wmean(g, "urbanFill")
            g40 = [r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "M" and int(r["t"]) <= 39]
            v[f"M3b_{n}"] = wmean(g40, "urbanFill")
            q0 = [r for r in lab if r["nation"] == n and r["quarter"] == "0"]
            v[f"M2_{n}"] = fl(q0[0]["plainPriceOverMcIndex"]) if q0 else float("nan")
            v[f"R_capdem_{n}"] = (fl(q0[0]["plainCapPerMonth"]) / fl(q0[0]["plainDemandPerMonth"])) if q0 and fl(q0[0]["plainDemandPerMonth"]) > 0 else float("nan")
            v[f"R_warmUp_{n}"] = sum(fl(r["movedUpK"]) for r in iw if r["phase"] == "w" and r["nation"] == n)
            v[f"R_urbSh0_{n}"] = (lambda rr: fl(rr[0]["urbanShare"]) if rr else float("nan"))([r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "M" and r["t"] == "0"])
            inc = [r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "M" and r["t"] == "0"]
            v[f"R9_incRatio_{n}"] = fl(inc[0]["incUrban"]) / fl(inc[0]["incRural"]) if inc and fl(inc[0]["incRural"]) > 0 else float("nan")
        for n in NATS + [EN]:
            q0 = [r for r in ivf if r["nation"] == n and r["quarter"] == "0"]
            v[f"R1_steamWage_{n}"] = fl(q0[0]["steamMcOverWageMain"]) if q0 else float("nan")
            lp = [(int(r["quarter"]), fl(r["laborPriceOverRefMain"])) for r in ivf if r["nation"] == n and int(r["quarter"]) <= 80]
            base = [x for q, x in lp if q == 0]
            v[f"R5_labPxMinOverQ0_{n}"] = (min(x for q, x in lp) / base[0]) if lp and base and base[0] > 0 else float("nan")
        v["R1_enLtQing"] = 1.0 if v["R1_steamWage_" + EN] < v["R1_steamWage_清"] else 0.0
        v["R1_enLtMar"] = 1.0 if v["R1_steamWage_" + EN] < v["R1_steamWage_マラータ同盟"] else 0.0
        # R3：助走の合計で農村→都市の流れが 0 の国
        tot = {}
        for r in iw:
            if r["phase"] == "w":
                tot[r["nation"]] = tot.get(r["nation"], 0.0) + fl(r["movedUpK"])
        v["R3_zeroInflowNations"] = float(sum(1 for x in tot.values() if x <= 0)); v["R3_nations"] = float(len(tot))
        # R2：蒸気機関の解禁（q11 までに見つけた国の数）
        ev = [r for r in disc if r["kind"] == "E" and r["row"] == "蒸気機関"]
        v["R2_steamBy11"] = float(sum(1 for r in ev if int(r["quarter"]) <= 11)); v["R2_steamAll"] = float(len(ev))
        # R5：清の農民の貯蓄が q0 の 5% 未満になる最初の四半期
        sv = sorted((int(r["time"][1:]), fl(r["savingsStock"])) for r in cls if r["nation"] == "清" and r["className"] == "農民" and r["time"].startswith("q"))
        first = float("nan")
        if sv and sv[0][1] > 0:
            for q, x in sv:
                if x < 0.05 * sv[0][1]:
                    first = float(q); break
        v["R5_savingsExhaustQ"] = first
        # R6：植民地の格の流れ（q≦39 の和）と飢えの死（国別・pop_s scope N の和）
        for n in [EN, "スペイン"]:
            cc = [r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "C" and int(r["t"]) <= 39]
            v[f"M4a_colFill_{n}"] = wmean(cc, "urbanFill")
        for n in [EN, "スペイン", "清"]:
            c = [r for r in iw if r["phase"] == "q" and r["nation"] == n and r["tier"] == "C" and int(r["t"]) <= 39]
            v[f"R6_colBlocked_{n}"] = sum(fl(r["blockedK"]) for r in c); v[f"R6_colExempt_{n}"] = sum(fl(r["exemptK"]) for r in c); v[f"R6_colMovedUp_{n}"] = sum(fl(r["movedUpK"]) for r in c)
        for n in [EN, "スペイン", "清", "マラータ同盟", "日本"]:
            v[f"R6_famineDeathsK_{n}"] = sum(fl(r["deathsFamineK"]) for r in pop if r["scope"] == "N" and r["nation"] == n)
        # 発散の物差し：労働の値段が潰れる州の割合の最大
        tiny = [fl(r["plainTinyPriceRegionShare"]) for r in lab if r["plainTinyPriceRegionShare"] not in ("nan", "")]
        v["DIV_tinyMax"] = max(tiny) if tiny else float("nan")
        out[s] = v
    return out


def col(ps, key):
    return [ps[s][key] for s in sorted(ps)]


def fmt(x):
    return "nan" if x != x else f"{x:.4g}"


def main(argv):
    if len(argv) < 2:
        print(__doc__); return 1
    A = per_seed(argv[1]); B = per_seed(argv[2]) if len(argv) > 2 else None
    keys = sorted(next(iter(A.values())).keys()) if A else []
    print(f"# 種 A={sorted(A)}" + (f" B={sorted(B)}" if B else ""))
    for k in keys:
        a = col(A, k)
        line = f"{k:34s} A 中央値 {fmt(med(a)):>9s}  種ごと {' '.join(fmt(x) for x in a)}"
        if B:
            b = col(B, k)
            line += f"\n{'':34s} B 中央値 {fmt(med(b)):>9s}  種ごと {' '.join(fmt(x) for x in b)}"
        print(line)
    if B:
        print("\n# 判定（事前登録 [4]・追記 3）")
        ok = True
        prereq_fail = []
        for n in NATS:
            a1, b1 = col(A, f"M1_{n}"), col(B, f"M1_{n}")
            need = max(1, -(-9 * len(b1) // 10))   # 10 種なら 9 種以上（種が少ない煙試験では 9/10 の切り上げ）
            p = med(a1) >= 0.8; g = med(b1) <= 0.2 and sum(1 for x in b1 if x <= 0.2) >= need
            print(f"M1 {n}: 前提 A 中央値 {fmt(med(a1))} ≧0.8 → {'成立' if p else '不成立'}／B 中央値 {fmt(med(b1))} ≦0.2 かつ {need} 種以上（{sum(1 for x in b1 if x <= 0.2)} 種）→ {'通る' if g else '外れ'}"); ok &= g
            a2, b2 = col(A, f"M2_{n}"), col(B, f"M2_{n}")
            p = med(a2) >= 1.15; g = med(b2) <= 1.05
            print(f"M2 {n}: 前提 A 中央値 {fmt(med(a2))} ≧1.15 → {'成立' if p else '不成立'}／B 中央値 {fmt(med(b2))} ≦1.05 → {'通る' if g else '外れ'}"); ok &= g
            b3 = col(B, f"M3_{n}"); g = med(b3) >= 0.6
            print(f"M3 {n}: B 中央値 {fmt(med(b3))} ≧0.6 → {'通る' if g else '外れ'}（A {fmt(med(col(A, f'M3_{n}')))}）"); ok &= g
            a3b, b3b = med(col(A, f"M3b_{n}")), med(col(B, f"M3b_{n}"))
            g = b3b >= a3b - 0.10
            print(f"M3b {n}（本土 t≦39）: B 中央値 {fmt(b3b)} ≧ A 中央値 {fmt(a3b)} − 0.10 → {'通る' if g else '外れ'}"); ok &= g
            if not (med(a1) >= 0.8 and med(a2) >= 1.15):
                prereq_fail.append(n)
        for n in [EN, "スペイン"]:
            a4, b4 = med(col(A, f"M4a_colFill_{n}")), med(col(B, f"M4a_colFill_{n}"))
            g = b4 >= a4 - 0.10 if (a4 == a4 and b4 == b4) else True
            print(f"M4a {n}（植民地の格 t≦39 の都市の充足）: B 中央値 {fmt(b4)} ≧ A 中央値 {fmt(a4)} − 0.10 → {'通る' if g else '外れ'}（nan＝その国に植民地の格の州の行が無い＝判定対象外）"); ok &= g
            a5, b5 = med(col(A, f"R6_famineDeathsK_{n}")), med(col(B, f"R6_famineDeathsK_{n}"))
            g = b5 <= 2.0 * a5 if a5 > 0 else (b5 <= 0)
            print(f"M4b {n}（国の飢えの死の累計 千人）: B 中央値 {fmt(b5)} ≦ 2×A 中央値 {fmt(2.0 * a5)} → {'通る' if g else '外れ'}"); ok &= g
        print("M1〜M4 全て通る" if ok else "M1〜M4 に外れあり")
        if prereq_fail:
            print(f"判定不能＝採らない：前提（A の M1≧0.8・M2≧1.15）が不成立の国 {prereq_fail}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
