# (1354) 州の工業化度（cons_s・1823 年まで）を自給の必要量×人数で加重した値と、国の人口加重（score_s の meanIndustrialization）の比＝K の上ずれの読み。読むだけ。
# 使い方：python -I indbias1354.py <base_ins> <出力>
import sys, statistics
BASE, OUT = sys.argv[1], sys.argv[2]
NATS = ("ロシア", "清", "イギリス"); YRS = (1800, 1810, 1820)
def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t"); yield [v[i] for i in ix]
def fl(s):
    try: return float(s)
    except ValueError: return float("nan")
def im(I): return 1.0 + 1.2 * I / (1.0 + I / 2.5)
res = {}
for sd in range(1, 11):
    ci = {}
    for q, y, nat, reg, pi, ti in rows(f"{BASE}/cons_s{sd}.tsv", ["quarter", "year", "nation", "region", "productiveIndustrializationIndex", "industrializationIndex"]):
        if nat in NATS: ci[(q, reg)] = (fl(pi), fl(ti))
    sc = {}
    for y, nat, I in rows(f"{BASE}/score_s{sd}.tsv", ["year", "nation", "meanIndustrialization"]):
        if nat in NATS: sc[(int(y), nat)] = fl(I)
    acc = {}
    for q, y, nat, reg, kind, Q, sraw, f in rows(f"{BASE}/subst_s{sd}.tsv", ["quarter", "year", "nation", "region", "s2BasisKind", "s3QInst", "s3SrawInst", "s3HarvestInst"]):
        y = int(y)
        if nat not in NATS or kind != "2" or y not in YRS: continue
        c = ci.get((q, reg)); Q, x = fl(Q), fl(sraw) * fl(f)
        if c is None or not (x > 0) or not (Q > 0): continue
        nN = Q / x; a = acc.setdefault((nat, y), [0.0, 0.0, 0.0])
        a[0] += nN; a[1] += nN * c[0]; a[2] += nN * im(c[0])
    for (nat, y), a in acc.items():
        I_sub = a[1] / a[0]; I_nat = sc.get((y, nat), float("nan"))
        res.setdefault((nat, y), []).append((I_sub, I_nat, (a[2] / a[0]) / im(I_nat)))
out = ["# 国×年：自給の加重の州の ProductiveIndustrialization・国の meanIndustrialization・indMult の比（自給の加重÷国の値）＝種の中央〔最小〜最大〕"]
for k in sorted(res):
    v = res[k]; cols = []
    for i, nm in enumerate(("I 自給の州", "I 国", "indMult 比")):
        s = sorted(t[i] for t in v); cols.append(f"{nm} {statistics.median(s):.3g}〔{s[0]:.3g}〜{s[-1]:.3g}〕")
    out.append(f"{k[0]} {k[1]}: " + "・".join(cols) + f" n{len(v)}")
open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
print("書いた", len(out))
