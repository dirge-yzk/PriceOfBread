# (1177) c116bm：被覆 coverage＝base÷broad の分布（money_s・主要 9 国）と与信の上限のどちらが効いたか（nation_s）。
# 使い方：python -X utf8 -I cover1177.py <生のフォルダ> <ラベル> <出力フォルダ>
# money_s の行は計器が書いた行だけ（first と update）。計器が書かない国は数えない。
import sys, os, math, collections, statistics

src, label, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
L = []
def P(s=""): L.append(s)

def fnum(s):
    try:
        v = float(s); return v if math.isfinite(v) else None
    except ValueError:
        return None

cov = []; lo = []; tr = []; byNat = collections.defaultdict(list); lowRows = []
nmoney = 0
for seed in range(1, 11):
    p = os.path.join(src, f"money_s{seed}.tsv")
    if not os.path.exists(p): continue
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(h): continue
            br, ba, t, l = (fnum(c[ix[k]]) for k in ("broadMoneyStock", "baseMoneyStock", "treasuryStock", "outstandingLoansStock"))
            if br is None or ba is None or not br > 1e-9: continue
            nmoney += 1
            cv = ba / br; cov.append(cv); byNat[c[ix["nation"]]].append(cv)
            if l is not None: lo.append(l / br)
            if t is not None: tr.append(t / br)
            if cv < 0.5: lowRows.append((cv, seed, c[ix["time"]], c[ix["nation"]], c[ix["year"]], l / br if l is not None else None, t / br if t is not None else None))
P(f"# cover1177 {label}  src={src}  money_s の行={nmoney}")
if cov:
    s = sorted(cov)
    def q(a, x): return a[min(len(a) - 1, int(x * len(a)))]
    P(f"## 1. coverage＝base÷broad：最小 {s[0]:.6f}  1% {q(s,0.01):.6f}  中央 {q(s,0.5):.6f}  最大 {s[-1]:.6f}")
    P(f"   ＜0.325 {sum(1 for v in cov if v < 0.325)} 行  ＜0.357 {sum(1 for v in cov if v < 0.357)}  ＜0.5 {sum(1 for v in cov if v < 0.5)}  ＜0.9 {sum(1 for v in cov if v < 0.9)}  ／ {len(cov)}")
    sl = sorted(lo); st = sorted(tr)
    P(f"   loans÷broad：中央 {q(sl,0.5):.3g}  99% {q(sl,0.99):.3g}  最大 {sl[-1]:.3g}   treasury÷broad：最小 {st[0]:.3g}  中央 {q(st,0.5):.3g}")
    P("   国ごとの coverage の最小と中央：" + "  ".join(f"{n} {min(v):.4f}/{statistics.median(v):.4f}" for n, v in sorted(byNat.items())))
    for r in sorted(lowRows)[:8]:
        P(f"   低い行：cov {r[0]:.4f} 種 {r[1]} {r[2]} {r[3]} {r[4]} loans÷broad {r[5]} treasury÷broad {r[6]}")

# nation_s：与信の上限＝準備の上限と返せる額の小さい方（Credit.cs:40・:52）。どちらが効いた日か。
J = B = 0; ratio = []; nrow = 0
for seed in range(1, 11):
    p = os.path.join(src, f"nation_s{seed}.tsv")
    if not os.path.exists(p): continue
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
        need = ["creditJudgedDaysCount", "creditRepayableBoundDaysCount", "creditReserveCapMeanStock", "creditRepayableCapMeanStock"]
        if any(k not in ix for k in need): break
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < len(h): continue
            j = fnum(c[ix[need[0]]]); b = fnum(c[ix[need[1]]])
            if j is None or b is None or j <= 0: continue
            nrow += 1; J += j; B += b
            rc, rp = fnum(c[ix[need[2]]]), fnum(c[ix[need[3]]])
            if rc is not None and rp is not None and rc > 1e-9: ratio.append(rp / rc)
P(f"## 2. 与信の上限（nation_s・全ての国）：判定の行 {nrow}  返せる額が効いた日の割合 {B/J if J>0 else float('nan'):.4f}")
if ratio:
    s = sorted(ratio)
    P(f"   返せる額÷準備の上限：10% {s[len(s)//10]:.4g}  中央 {s[len(s)//2]:.4g}  90% {s[9*len(s)//10]:.4g}")
with open(os.path.join(out, f"cover1177_{label}.txt"), "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(L) + "\n")
print("\n".join(L))
