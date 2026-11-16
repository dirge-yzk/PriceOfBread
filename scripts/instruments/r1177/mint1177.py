# (1177) c116bm：造幣の床の読み（mintreg_s・region_s の首都・money_s の被覆・nation_s の与信の上限）。
# 使い方：python -X utf8 -I mint1177.py <生のフォルダ> <ラベル> <出力フォルダ>
# エンジンの関数は呼ばない。計器が書かない行は数えない（0 と数えない）。
import sys, os, math, collections

src, label, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
SEEDS = range(1, 11)
SILVER6 = {"オムスク", "プラハ", "ヌエバ・エスパーニャ銀地帯", "ペルー", "ラ・プラタ", "ドレスデン"}

def era(y):
    return "1783-1829" if y < 1830 else "1830-1869" if y < 1870 else "1870-1913" if y < 1914 else "1914-1950"

def fnum(s):
    try:
        v = float(s)
        return v if math.isfinite(v) else None
    except ValueError:
        return None

def capitals(seed):
    p = os.path.join(src, f"region_s{seed}.tsv")
    if not os.path.exists(p):
        return None
    cap = {}
    with open(p, encoding="utf-8") as f:
        hdr = f.readline().rstrip("\n").split("\t")
        iq, inn, ir = hdr.index("quarter"), hdr.index("nation"), hdr.index("region")
        for line in f:
            c = line.split("\t", ir + 1)
            k = (int(c[iq]), c[inn])
            if k not in cap:
                cap[k] = c[ir]
    return cap

L = []
def P(s=""):
    L.append(s)

tot = collections.Counter()
# 母集合 U（law 1・2・本位の金属の市場がある月）
byLaw = collections.defaultdict(collections.Counter)
stockEra = collections.defaultdict(float)       # (era, ownerLaw) -> Σ stockAg（四半期末の写し）
demEra = collections.defaultdict(float)
qtyEra = collections.defaultdict(float)          # (era, kind) -> Σ qtyAg
prodEra = collections.defaultdict(float)
s6 = collections.defaultdict(collections.Counter)  # region -> law の四半期
s6q = collections.defaultdict(float)               # (region, law) -> Σ qtyAg
s6stock = collections.defaultdict(float); s6dem = collections.defaultdict(float)
rowsN = 0; rowsCap = 0; capMissing = 0
nq_out = open(os.path.join(out, f"natq_{label}.tsv"), "w", encoding="utf-8", newline="\n")
nq_out.write("seed\tyear\tq\tnation\tlaw\tregions\tmonths\tfiatM\tnoGoodM\tnoStockM\taboveM\tgapM\tboughtM\tnotPaidM\tatMarketM\tcoverShortM\tqtyAg\tqtyAu\tstockAg\tdemandAg\tprodAg\tqtyAgCap\tstockAgCap\n")
cols = None
for seed in SEEDS:
    p = os.path.join(src, f"mintreg_s{seed}.tsv")
    if not os.path.exists(p):
        P(f"# seed {seed}: mintreg_s なし"); continue
    cap = capitals(seed)
    agg = {}
    with open(p, encoding="utf-8") as f:
        hdr = f.readline().rstrip("\n").split("\t")
        ix = {n: i for i, n in enumerate(hdr)}
        cnt = ["months", "fiatM", "noGoodM", "noStockM", "aboveM", "gapM", "wouldBuyM", "boughtM", "notPaidM", "atMarketM", "coverShortM", "modelMissM"]
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(hdr):
                continue
            rowsN += 1
            y, q, nat, reg, law = int(c[ix["year"]]), int(c[ix["q"]]), c[ix["nation"]], c[ix["region"]], int(c[ix["law"]])
            v = {k: int(c[ix[k]]) for k in cnt}
            for k in cnt: tot[k] += v[k]
            qa, qu = fnum(c[ix["qtyAg"]]) or 0.0, fnum(c[ix["qtyAu"]]) or 0.0
            st, dm, pr = fnum(c[ix["stockAg"]]), fnum(c[ix["demandAg"]]), fnum(c[ix["prodAg"]])
            tot["qtyAg"] += qa; tot["qtyAu"] += qu
            iscap = None
            if cap is not None:
                cn = cap.get((q, nat))
                if cn is None: capMissing += 1
                else: iscap = (cn == reg)
            if iscap: rowsCap += 1
            if law in (1, 2):
                U = v["months"] - v["fiatM"] - v["noGoodM"]
                b = byLaw[law]
                b["U"] += U; b["noStock"] += v["noStockM"]; b["above"] += v["aboveM"]; b["gap"] += v["gapM"]
                b["bought"] += v["boughtM"]; b["notPaid"] += v["notPaidM"]; b["noGood"] += v["noGoodM"]; b["months"] += v["months"]
                if iscap:
                    b["Ucap"] += U; b["noStockCap"] += v["noStockM"]; b["boughtCap"] += v["boughtM"]
            e = era(y)
            if st is not None: stockEra[(e, law)] += st
            if dm is not None: demEra[(e, law)] += dm
            if pr is not None: prodEra[e] += pr
            kind = "cap" if iscap else ("prod" if (pr or 0) > 0 else "other")
            qtyEra[(e, kind)] += qa
            if reg in SILVER6:
                s6[reg][law] += 1; s6q[(reg, law)] += qa
                if st is not None: s6stock[(reg, law)] += st
                if dm is not None: s6dem[(reg, law)] += dm
            k = (y, q, nat)
            a = agg.get(k)
            if a is None:
                a = agg[k] = [law, 0] + [0] * 10 + [0.0] * 7
            a[1] += 1
            for j, kk in enumerate(["months", "fiatM", "noGoodM", "noStockM", "aboveM", "gapM", "boughtM", "notPaidM", "atMarketM", "coverShortM"]):
                a[2 + j] += v[kk]
            a[12] += qa; a[13] += qu; a[14] += st or 0.0; a[15] += dm or 0.0; a[16] += pr or 0.0
            if iscap: a[17] += qa; a[18] += st or 0.0
    for (y, q, nat), a in sorted(agg.items()):
        nq_out.write("\t".join([str(seed), str(y), str(q), nat] + [str(x) for x in a[:12]] + [repr(x) for x in a[12:]]) + "\n")
nq_out.close()

P(f"# mint1177 {label}  src={src}  行={rowsN}  首都の行={rowsCap}  首都が引けない行={capMissing}")
P("## 1. 10 種の和（mintreg_s の数の列）")
P("  " + "  ".join(f"{k}={tot[k]}" for k in ["months", "fiatM", "noGoodM", "noStockM", "aboveM", "gapM", "wouldBuyM", "boughtM", "notPaidM", "atMarketM", "coverShortM", "modelMissM"]))
P(f"  qtyAg={tot['qtyAg']:.6g}  qtyAu={tot['qtyAu']:.6g}")
P("## 2. 母集合 U（本位の金属の市場がある州×月）の理由の割合（law 1＝銀・2＝金）")
for law in (1, 2):
    b = byLaw[law]; U = b["U"]
    if U <= 0: continue
    withStock = U - b["noStock"]
    P(f"  law{law}: U={U}  F_noStock={b['noStock']/U:.4f}  F_above(在庫のある月のうち)={b['above']/withStock if withStock>0 else float('nan'):.4f}  F_gap={b['gap']/U:.4f}  F_bought={b['bought']/U:.4f}  notPaid={b['notPaid']}  noGood(U の外)={b['noGood']}/{b['months']}")
    if b["Ucap"] > 0:
        P(f"        首都の州: U={b['Ucap']}  F_noStock={b['noStockCap']/b['Ucap']:.4f}  F_bought={b['boughtCap']/b['Ucap']:.4f}")
P("## 3. 銀の在庫（四半期末の写しの和）を持ち主の本位で分ける・在庫÷需要（Σ÷Σ）")
for e in ["1783-1829", "1830-1869", "1870-1913", "1914-1950"]:
    s = {law: stockEra[(e, law)] for law in (0, 1, 2)}; d = {law: demEra[(e, law)] for law in (0, 1, 2)}
    S = sum(s.values())
    if S <= 0: continue
    sout = (s[0] + s[2]) / S
    rr = "  ".join(f"law{law}: 在庫 {s[law]:.4g} 在庫÷需要 {s[law]/d[law] if d[law]>0 else float('nan'):.3g}" for law in (0, 1, 2))
    P(f"  {e}: S_out={sout:.4f}  {rr}")
P("## 4. 造幣が買った銀の量の置き場（首都・産地・その他）と産出（四半期末の月の産出の和×3 で割る＝近似）")
for e in ["1783-1829", "1830-1869", "1870-1913", "1914-1950"]:
    qs = {k: qtyEra[(e, k)] for k in ("cap", "prod", "other")}; Q = sum(qs.values())
    if Q <= 0 and prodEra[e] <= 0: continue
    P(f"  {e}: 買い {Q:.4g}  首都 {qs['cap']/Q if Q>0 else float('nan'):.4f}  産地 {qs['prod']/Q if Q>0 else float('nan'):.4f}  その他 {qs['other']/Q if Q>0 else float('nan'):.4f}  買い÷(産出×3) {Q/(3*prodEra[e]) if prodEra[e]>0 else float('nan'):.4f}")
P("## 5. 銀の賦存の 6 州：持ち主の本位ごとの四半期・買った量・在庫÷需要")
for reg in sorted(SILVER6):
    parts = []
    for law in (0, 1, 2):
        n = s6[reg][law]
        if n == 0: continue
        d = s6dem[(reg, law)]
        parts.append(f"law{law} 四半期 {n} 買い {s6q[(reg, law)]:.4g} 在庫÷需要 {s6stock[(reg, law)]/d if d>0 else float('nan'):.3g}")
    P(f"  {reg}: " + " ／ ".join(parts))
with open(os.path.join(out, f"mint1177_{label}.txt"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(L) + "\n")
print("\n".join(L))
