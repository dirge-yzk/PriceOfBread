# 使い方: python -X utf8 -I q1_desire.py <腕の生> <基線の生> <種 a-b>
# (1231) 問い 1 の四：希望の落ちの内訳（cons_s・1783〜1823・Core と Integrated・希望が正の州×四半期）＝単位の数×束の値段・純利回り・ハードル・利得。比は 10 種の和（平均は州×四半期の単純平均）。
import sys
def fl(x):
    try: return float(x)
    except: return float("nan")
def rd(raw, s0, s1):
    out = {}
    for s in range(s0, s1 + 1):
        with open(f"{raw}/cons_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[ix["tier"]] not in ("Core", "Integrated"): continue
                k = (c[2], int(c[1]) // 10 * 10)
                g = out.setdefault(k, [0.0] * 8)
                du = fl(c[ix["desiredUnitsMean"]]); um = fl(c[ix["unitMoneyMean"]]); nr = fl(c[ix["netReturnMean"]]); hu = fl(c[ix["hurdleMean"]]); ga = fl(c[ix["gainWithTechMean"]]); fh = fl(c[ix["financialHeatMean"]])
                if not all(x == x for x in (du, um, nr, hu, ga)): continue
                g[0] += du; g[1] += du * um; g[2] += nr; g[3] += hu; g[4] += ga; g[5] += 1; g[6] += fh if fh == fh else 0; g[7] += fl(c[ix["desiredMoneyPerMonth"]])
    return out
s0, s1 = map(int, sys.argv[3].split("-"))
A = rd(sys.argv[1], s0, s1); B = rd(sys.argv[2], s0, s1)
print("nation\tdecade\tdesUnits a/b\tdesMoney a/b\tunitMoney a/b\tnetReturn a|b\thurdle a|b\tgain a|b\theat a|b")
for k in sorted(B):
    if k not in A: continue
    a, b = A[k], B[k]
    print(f"{k[0]}\t{k[1]}\t{a[0]/b[0]:.3f}\t{a[7]/b[7]:.3f}\t{(a[1]/a[0])/(b[1]/b[0]):.3f}\t{a[2]/a[5]:.4f}|{b[2]/b[5]:.4f}\t{a[3]/a[5]:.4f}|{b[3]/b[5]:.4f}\t{a[4]/a[5]:.4f}|{b[4]/b[5]:.4f}\t{a[6]/a[5]:.3f}|{b[6]/b[5]:.3f}")
