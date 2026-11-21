# (1279/1280) c131b2 の紙の影：poppref_s から〈経済の側を保有で読んだ時の党の選好の差〉を国×四半期で数える。
# 読むだけ（エンジンを呼ばない・生は keep_1274/base_ins）。AI の台本・確証なし。
# 使い方: python -X utf8 -I b2shadow.py <生のフォルダ> <出力フォルダ>
#
# 導き（poppref の列だけから・PopPreference.cs と PolicyPreferences.cs の式の形を読んで書いた）：
#   旧 IncomeTax(b)      = mass_b - 3 W_b                     → W_b（ブロックの富の割合）
#   旧 SocialSecurity(b) = mass_b - 2 W_b + O_b               → O_b（高齢の項）
#   新 meanPi Inc(b)     = Bm_b - 3 W_b/P_b
#   新 meanPi SS(b)      = Bm_b - 2 W_b/P_b + O_b             → R_b=W_b/P_b・P_b（人口の割合）・Bm_b（非地代の所得の割合の人口の平均）
#   旧 CornLaws(b)       = rp Rent_b - (b!=0)                 → Rent_b（rp=1 と仮定・自己検査 ΣRent=1 で確かめる）
#   新 meanPi CL(b)      = rp Rent_b/P_b - (1 - RI_b)         → RI_b（地代÷所得の人口の平均）
# 保有で読んだブロックの選好（割合の物差しのまま・札の定数だけを保有の量へ）：
#   H_Inc(b) = Bm_b - 3 W_b ・ H_SS(b) = Bm_b - 2 W_b + O_b ・ H_CL(b) = Rent_b - (1 - RI_b)
#   差＝H - 旧：Inc と SS は Bm_b - mass_b、CL は RI_b - (b==0)
# 党 k の選好は今の木では Pref(a, k)（党の添字＝ブロックの添字）＝党の選好の差＝ブロック k の差。
# 議会の項の差＝Σ_k PartySeats_k × 差(a,k)（ParliamentLawWeight を掛ける前の単位）。
import sys, os, math, csv

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
SEEDS = range(1, 11)
AX = ["IncomeTax", "SocialSecurity", "CornLaws", "Tariff", "LandTax", "PollTax"]

def fl(s):
    try:
        v = float(s)
        return v
    except ValueError:
        return float("nan")

def q(vals, p):
    v = sorted(x for x in vals if not math.isnan(x))
    if not v:
        return float("nan")
    i = (len(v) - 1) * p
    lo = int(math.floor(i)); hi = min(lo + 1, len(v) - 1)
    return v[lo] + (v[hi] - v[lo]) * (i - lo)

def fmt(x):
    return "nan" if (isinstance(x, float) and math.isnan(x)) else (f"{x:.4g}" if isinstance(x, float) else str(x))

# 政体（center_s の polityCode・3=立憲王政・4=共和政＝HasLegislature）
pol = {}
for s in SEEDS:
    p = os.path.join(src, f"center_s{s}.tsv")
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r); iq, inm, ip = h.index("quarter"), h.index("nation"), h.index("polityCode")
        for row in r:
            pol[(s, int(row[iq]), row[inm])] = int(row[ip])

A = {}   # (s,q,nation) -> {(axis,b): (meanPi, old, opp, cnt)}
B = {}   # (s,q,nation) -> (shadowSeats[3], seats[3], year)
YEAR = {}
for s in SEEDS:
    p = os.path.join(src, f"poppref_s{s}.tsv")
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        for row in r:
            key = (s, int(row[1]), row[3]); YEAR[key] = int(row[2])
            if row[0] == "A":
                A.setdefault(key, {})[(row[4], int(row[5]))] = (fl(row[6]), fl(row[7]), fl(row[10]), int(row[11]))
            elif row[0] == "B":
                B[key] = ([fl(row[12]), fl(row[13]), fl(row[14])], [fl(row[15]), fl(row[16]), fl(row[17])])

def period(y):
    for a, b, nm in ((1783, 1819, "1783-1819"), (1820, 1859, "1820-59"), (1860, 1899, "1860-99"), (1900, 1950, "1900-50")):
        if a <= y <= b:
            return nm
    return "other"

rows = []          # 国×四半期×ブロック の導いた量
chk = {"sumW": [], "sumP": [], "sumRent": [], "BmRI": []}
parl = []          # 国×四半期×軸 の議会の項の差
for key, d in A.items():
    s, qq, nm = key
    der = {}
    for b in range(3):
        mass = 1.0 if b == 2 else 0.0
        try:
            mi, oi, oppi, ci = d[("IncomeTax", b)]
            ms, os_, opps, cs = d[("SocialSecurity", b)]
            mc, oc, oppc, cc = d[("CornLaws", b)]
        except KeyError:
            continue
        if ci == 0 or math.isnan(mi) or math.isnan(oi):
            continue
        W = (mass - oi) / 3.0
        O = os_ - mass + 2.0 * W
        R = ms - mi - O
        P = W / R if R > 1e-12 else float("nan")
        Bm = mi + 3.0 * R
        Rent = oc + (1.0 if b != 0 else 0.0)
        RI = 1.0 + mc - (Rent / P if P > 0 else float("nan"))
        der[b] = dict(W=W, O=O, R=R, P=P, Bm=Bm, Rent=Rent, RI=RI, mass=mass, oldInc=oi, oldSS=os_, oldCL=oc,
                      dInc=Bm - mass, dCL=RI - (1.0 if b == 0 else 0.0),
                      rawInc=mi - oi, rawCL=mc - oc, oppInc=oppi, oppCL=oppc, cnt=ci)
        rows.append((key, b, der[b]))
    if len(der) == 3:
        chk["sumW"].append(sum(der[b]["W"] for b in der))
        if all(not math.isnan(der[b]["P"]) for b in der):
            chk["sumP"].append(sum(der[b]["P"] for b in der))
        chk["sumRent"].append(sum(der[b]["Rent"] for b in der))
    for b in der:
        chk["BmRI"].append(der[b]["Bm"] + der[b]["RI"])
    if key in B and len(der) == 3:
        seats = B[key][1]
        st = sum(seats)
        pc = pol.get(key, -1)
        if st > 1e-9 and pc in (3, 4):
            for ax, dk, ok in (("IncomeTax", "dInc", "oldInc"), ("SocialSecurity", "dInc", "oldSS"), ("CornLaws", "dCL", "oldCL")):
                old = sum(seats[k] * der[k][ok] for k in range(3))
                dlt = sum(seats[k] * der[k][dk] for k in range(3))
                parl.append((key, ax, old, dlt, old + dlt))

with open(os.path.join(out, "b2_selfcheck.tsv"), "w", encoding="utf-8", newline="\n") as f:
    f.write("check\tn\tp01\tp50\tp99\n")
    for k, v in chk.items():
        f.write(f"{k}\t{len(v)}\t{fmt(q(v,.01))}\t{fmt(q(v,.5))}\t{fmt(q(v,.99))}\n")

with open(os.path.join(out, "b2_bloc.tsv"), "w", encoding="utf-8", newline="\n") as f:
    f.write("period\tbloc\tn\tP_p50\tW_p50\tBm_p10\tBm_p50\tBm_p90\tdInc_p10\tdInc_p50\tdInc_p90\tRI_p50\tdCL_p10\tdCL_p50\tdCL_p90\t"
            "rawInc_p50\trawCL_p50\toppInc_p50\toppInc_p90\toppCL_p50\toppCL_p90\n")
    for per in ("1783-1819", "1820-59", "1860-99", "1900-50", "all"):
        for b in range(3):
            sel = [d for (k, bb, d) in rows if bb == b and (per == "all" or period(YEAR[k]) == per)]
            col = lambda n: [d[n] for d in sel]
            f.write("\t".join([per, str(b), str(len(sel))] + [fmt(x) for x in (
                q(col("P"), .5), q(col("W"), .5), q(col("Bm"), .1), q(col("Bm"), .5), q(col("Bm"), .9),
                q(col("dInc"), .1), q(col("dInc"), .5), q(col("dInc"), .9), q(col("RI"), .5),
                q(col("dCL"), .1), q(col("dCL"), .5), q(col("dCL"), .9), q(col("rawInc"), .5), q(col("rawCL"), .5),
                q(col("oppInc"), .5), q(col("oppInc"), .9), q(col("oppCL"), .5), q(col("oppCL"), .9))]) + "\n")

with open(os.path.join(out, "b2_parl.tsv"), "w", encoding="utf-8", newline="\n") as f:
    f.write("period\taxis\tn\told_p50\tdelta_p10\tdelta_p50\tdelta_p90\tabsDelta_p50\tabsDelta_p90\tflipShare\tflipToPlus\tflipToMinus\n")
    for per in ("1783-1819", "1820-59", "1860-99", "1900-50", "all"):
        for ax in ("IncomeTax", "SocialSecurity", "CornLaws"):
            sel = [(o, d, n) for (k, a, o, d, n) in parl if a == ax and (per == "all" or period(YEAR[k]) == per)]
            if not sel:
                f.write(f"{per}\t{ax}\t0" + "\tnan" * 9 + "\n"); continue
            fl_ = [1 for (o, d, n) in sel if (o > 0) != (n > 0)]
            fp = sum(1 for (o, d, n) in sel if o <= 0 < n); fm = sum(1 for (o, d, n) in sel if n <= 0 < o)
            f.write("\t".join([per, ax, str(len(sel))] + [fmt(x) for x in (
                q([o for o, _, _ in sel], .5), q([d for _, d, _ in sel], .1), q([d for _, d, _ in sel], .5), q([d for _, d, _ in sel], .9),
                q([abs(d) for _, d, _ in sel], .5), q([abs(d) for _, d, _ in sel], .9), len(fl_) / len(sel), fp, fm)]) + "\n")

# 影の議席（最も近い党・POP の好み）と今の議席の差：参考（物差しが違う＝下の注）
with open(os.path.join(out, "b2_seats.tsv"), "w", encoding="utf-8", newline="\n") as f:
    f.write("period\tn\tabsDiffSum_p50\tabsDiffSum_p90\tshadowCons_p50\tseatCons_p50\tshadowSoc_p50\tseatSoc_p50\n")
    for per in ("1783-1819", "1820-59", "1860-99", "1900-50", "all"):
        sel = []
        for k, (sh, se) in B.items():
            if per != "all" and period(YEAR[k]) != per:
                continue
            if sum(se) > 1e-9 and pol.get(k, -1) in (3, 4) and not any(math.isnan(x) for x in sh):
                sel.append((sh, se))
        if not sel:
            f.write(f"{per}\t0" + "\tnan" * 6 + "\n"); continue
        f.write("\t".join([per, str(len(sel))] + [fmt(x) for x in (
            q([sum(abs(a - b) for a, b in zip(sh, se)) for sh, se in sel], .5), q([sum(abs(a - b) for a, b in zip(sh, se)) for sh, se in sel], .9),
            q([sh[0] for sh, _ in sel], .5), q([se[0] for _, se in sel], .5), q([sh[2] for sh, _ in sel], .5), q([se[2] for _, se in sel], .5))]) + "\n")

with open(os.path.join(out, "b2_counts.txt"), "w", encoding="utf-8", newline="\n") as f:
    f.write(f"nationQuarters_A={len(A)}\nblocRows={len(rows)}\nparlRows(legislature,seats>0)={len(parl)}\nBrows={len(B)}\n")
print("ok", len(A), len(rows), len(parl))
