# (479) 線 mg の判定の読み手：事前登録 measurements/mg479_20261021_prereg.txt の [8] と追記 2（[13]）の G0〜G6。
# 使い方：PYTHONUTF8=1 python an_mg479.py <B の ins フォルダ> <m475 の ins フォルダ> [種の数=10]
#   B＝旗 2 本とも 1 の計器つき 10 種（mg_s を持つ）・比べる側＝基準の計器つき 10 種（mg_s を持たない）。
# 読むだけ。判定は 7 種以上＝当たり・4〜6＝部分・3 以下＝外れ。自明な線（G2 の厳密側・perCap≦fare の厳密 0）は番人に任せ、ここでは検査だけして記録に出す。
import csv, sys, statistics as st, collections

if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")
B = sys.argv[1]; A = sys.argv[2]; NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
SEEDS = list(range(1, NS + 1))
Q_END = 667
P = {"P1": (0, 67), "P2": (68, 267), "P3": (268, 467), "P4": (468, 667)}


def rows(path):
    with open(path, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            yield r


def fl(x):
    try: return float(x)
    except Exception: return float("nan")


def region_totals(base, sd):
    """国×四半期の (出, 入, 人口) と 全国の四半期ごとの出の和"""
    out = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
    for r in rows(f"{base}/region_s{sd}.tsv"):
        k = (r["nation"], int(r["quarter"]))
        a = out[k]
        a[0] += fl(r["crossBorderOutKPerQuarter"]); a[1] += fl(r["crossBorderInKPerQuarter"]); a[2] += fl(r["populationK"])
    return out


def rate(tot, nation, p, idx=0):
    a, b = P[p]
    s = pop = 0.0
    for q in range(a, b + 1):
        v = tot.get((nation, q))
        if v: s += v[idx]; pop += v[2]
    return s / pop * 4 * 100 if pop > 0 else 0.0


def sum_all(tot, idx, q0=0, q1=Q_END):
    return sum(v[idx] for (n, q), v in tot.items() if q0 <= q <= q1)


def mg_totals(base, sd):
    """mg_s の合計行（srcNation='-1'）の四半期ごとの値と、組の行"""
    tot = {}; pairs = []
    for r in rows(f"{base}/mg_s{sd}.tsv"):
        d = {k: (fl(v) if k not in ("srcNation", "dstNation") else v) for k, v in r.items()}
        if r["srcNation"] == "-1": tot[int(r["quarter"])] = d
        else: pairs.append(d)
    return tot, pairs


def verdict(n):
    return "当たり" if n >= 7 else ("部分" if n >= 4 else "外れ")


res = collections.defaultdict(list)   # G名 -> 種ごとの真偽
note = []
detail = collections.defaultdict(dict)

for sd in SEEDS:
    tb = region_totals(B, sd); ta = region_totals(A, sd)
    mt, mp = mg_totals(B, sd)
    # G0
    inB = sum_all(tb, 1); inA = sum_all(ta, 1)
    ratio = inB / inA if inA > 0 else float("inf")
    uk = rate(tb, "イギリス", "P2") * 0.5 + rate(tb, "イギリス", "P3") * 0.5   # P2〜P3 の年あたり（200 四半期ずつ・単純平均）
    res["G0下"].append(ratio >= 10); res["G0上"].append(uk <= 7.0)
    detail["G0"][sd] = f"B入合計 {inB:.0f}千 / m475 {inA:.0f}千 = {ratio:.1f}倍 ・英の国外へ(P2〜P3年率) {uk:.3f}% (m475 {0.5*(rate(ta,'イギリス','P2')+rate(ta,'イギリス','P3')):.3f}%)"
    # G1
    pre = sum(mt[q]["preAffordK"] for q in mt if q <= 267); blk = sum(mt[q]["blockedByFareK"] for q in mt if q <= 267)
    g1 = blk / pre if pre > 0 else 0.0
    res["G1"].append(g1 >= 0.05); detail["G1"][sd] = f"P1・P2 の運賃で止まった割合 {g1*100:.1f}% (blockedByFare {blk:.0f}千 / preAfford {pre:.0f}千)"
    # G2
    serfBlocked = {}
    for p in mp: serfBlocked[p["srcNation"]] = serfBlocked.get(p["srcNation"], 0.0) + p["blockedBySerfK"]
    nBlockedNations = sum(1 for v in serfBlocked.values() if v > 0)
    strict_bad = sum(1 for p in mp if p["serfFactor"] == 0 and p["cls"] == 1 and p["postCapK"] > 0)
    # 国内の移民：nation_s の serfEmancipation==0 の国の出
    def dom_none(base):
        em = {}
        for r in rows(f"{base}/nation_s{sd}.tsv"):
            em[(r["nation"], int(r["quarter"]))] = r["serfEmancipation"]
        reg2n = {}
        for r in rows(f"{base}/region_s{sd}.tsv"): reg2n[r["region"]] = r["nation"]
        tot = 0.0
        for r in rows(f"{base}/migration_s{sd}.tsv"):
            n = reg2n.get(r["fromRegion"]); q = int(r["quarter"])
            if n is not None and em.get((n, q)) == "0": tot += fl(r["movedKPerQuarter"])
        return tot
    dB = dom_none(B); dA = dom_none(A)
    res["G2"].append(nBlockedNations >= 1 and dB < dA)
    detail["G2"][sd] = f"農奴の門で止まった国 {nBlockedNations} か国 ・厳密側の違反行 {strict_bad} ・国内の移民(解放なしの国の出・全期) B {dB:.0f}千 / m475 {dA:.0f}千"
    # G3
    qb = None
    for q in sorted(mt):
        if mt[q]["preAffordK"] > 0 and mt[q]["passageFactor"] < 0.8: qb = q; break
    if qb is None:
        res["G3"].append(False); detail["G3"][sd] = "pf が 0.8 を下回らない"
    else:
        # 四半期ごとの全国の出の和
        def per_q(tot):
            d = collections.defaultdict(float)
            for (n, q), v in tot.items(): d[q] += v[0]
            return d
        pb = per_q(tb); pa = per_q(ta)
        def mean(d, q0, q1):
            qs = range(max(0, q0), min(Q_END, q1) + 1)
            return sum(d.get(q, 0.0) for q in qs) / max(1, len(list(qs)))
        rB = mean(pb, qb, qb + 99) / mean(pb, qb - 100, qb - 1) if mean(pb, qb - 100, qb - 1) > 0 else float("inf")
        rA = mean(pa, qb, qb + 99) / mean(pa, qb - 100, qb - 1) if mean(pa, qb - 100, qb - 1) > 0 else float("inf")
        res["G3"].append(rB >= 1.2 * rA)
        detail["G3"][sd] = f"境 Q{qb} ({int(mt[qb]['year'])}年) ・B 後÷前 {rB:.2f} ・m475 同じ窓 {rA:.2f}"
    # G4
    pa_ = sum(mt[q]["postAffordK"] for q in mt); lo = sum(mt[q]["postAffordLowGapK"] for q in mt)
    g4 = lo / pa_ if pa_ > 0 else 0.0
    res["G4"].append(g4 >= 0.30); detail["G4"][sd] = f"旧の閾値の下の差の組の計画の流量 {g4*100:.1f}% ({lo:.0f} / {pa_:.0f}千)"
    # G5
    usB = 0.5 * (rate(tb, "アメリカ", "P2") + rate(tb, "アメリカ", "P3")); usA = 0.5 * (rate(ta, "アメリカ", "P2") + rate(ta, "アメリカ", "P3"))
    usInB = 0.5 * (rate(tb, "アメリカ", "P2", 1) + rate(tb, "アメリカ", "P3", 1)); usInA = 0.5 * (rate(ta, "アメリカ", "P2", 1) + rate(ta, "アメリカ", "P3", 1))
    res["G5"].append(usB <= usA); detail["G5"][sd] = f"米の国外へ(P2〜P3年率) B {usB:.3f}% / m475 {usA:.3f}% ・米の国外から B {usInB:.3f}% / m475 {usInA:.3f}%"
    # G6
    ok = 0; txt = []
    for nat in ("清", "プロイセン", "ロシア"):
        pr = sum(p["preAffordK"] for p in mp if p["srcNation"] == nat and p["cls"] == 1); po = sum(p["postAffordK"] for p in mp if p["srcNation"] == nat and p["cls"] == 1)
        af = po / pr if pr > 0 else float("nan")
        txt.append(f"{nat} {af:.2f}")
        if af == af and 0.2 <= af <= 0.8: ok += 1
    res["G6"].append(ok >= 2); detail["G6"][sd] = "農民の afford: " + " / ".join(txt)

print("# (479) 判定（B の計器つき 10 種 × m475）")
for g in ["G0下", "G0上", "G1", "G2", "G3", "G4", "G5", "G6"]:
    n = sum(res[g]); print(f"{g}: {n}/{len(res[g])} 種 → {verdict(n)}  {''.join('o' if x else '.' for x in res[g])}")
print()
for g in ["G0", "G1", "G2", "G3", "G4", "G5", "G6"]:
    print(f"[{g}]")
    for sd in SEEDS: print(f"  種{sd}: {detail[g].get(sd, '')}")
