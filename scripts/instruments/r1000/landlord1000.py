# (1000) c91ld 読むだけの台本：盤面 m988 で地主の所得（地代のシェア）の項がどう変わったかを読む。
#  A 地代の持ち分のデータ（world.json の landOwnership）がある 8 国と米・清：cornint_s の engineRentShare0..2（Build の Rent[b] の正規化＝エンジンの穀物法の項の入力）の中央
#  B 同じ国の穀物の地代（rentGrainDom＋rentGrainExp）のブロックの割合＝和÷和（10 種×期間）
#  C landshare_s：実際に付いた地代の層ごとの割合（landRentActual*・和÷和）と国家の枠（crownActual）の割合（÷(層の和＋国家)）
#  D 生の名乗り：行の数・種の数
# 使い方：python -X utf8 -I landlord1000.py <生のフォルダ> <出力のファイル>
import sys, os, math
SEEDS = range(1, 11)
PER = [(1783, 1846), (1847, 1913), (1914, 1950)]
NAT = ["フランス", "日本", "イギリス", "ロシア", "プロイセン", "スペイン", "デンマーク", "ポーランド", "清", "アメリカ"]
CLS = ["Noble", "Clergy", "Peasant", "Capitalist", "Other"]

def rd(p):
    with open(p, "rb") as fh:
        L = fh.read().decode("utf-8").splitlines()
    h = L[0].split("\t"); ix = {n: i for i, n in enumerate(h)}
    return ix, [ln.split("\t") for ln in L[1:] if ln]

def fl(x):
    try: return float(x)
    except ValueError: return float("nan")

def q(v, p):
    v = sorted(x for x in v if not math.isnan(x))
    return v[int((len(v) - 1) * p)] if v else float("nan")

def per_of(y):
    return next((p for p in PER if p[0] <= y <= p[1]), None)

d, out = sys.argv[1], sys.argv[2]
shr = {}; grain = {}; ls = {}; nrow = 0; nseed = 0
for s in SEEDS:
    pc = os.path.join(d, f"cornint_s{s}.tsv"); pl = os.path.join(d, f"landshare_s{s}.tsv")
    if not (os.path.exists(pc) and os.path.exists(pl)): continue
    nseed += 1
    ix, R = rd(pc)
    for c in R:
        n = c[ix["nation"]]; pr = per_of(int(c[ix["year"]]))
        if pr is None or n not in NAT: continue
        nrow += 1
        if fl(c[ix["rentDomTotalPerMonth"]]) > 1e-9:
            a = shr.setdefault((n, pr), [[], [], []])
            for b in range(3): a[b].append(fl(c[ix[f"engineRentShare{b}"]]))
        g = grain.setdefault((n, pr), [0.0, 0.0, 0.0])
        for b in range(3):
            v = fl(c[ix[f"rentGrainDomPerMonth{b}"]]) + fl(c[ix[f"rentGrainExpPerMonth{b}"]])
            if not math.isnan(v): g[b] += v
    ix, R = rd(pl)
    for c in R:
        n = c[ix["nation"]]; pr = per_of(int(c[ix["year"]]))
        if pr is None or n not in NAT: continue
        a = ls.setdefault((n, pr), [0.0] * 6)
        for k, cl in enumerate(CLS):
            v = fl(c[ix[f"landRentActual{cl}PerQuarter"]])
            if not math.isnan(v): a[k] += v
        v = fl(c[ix["crownActualPerQuarter"]])
        if not math.isnan(v): a[5] += v

o = [f"## {d}  種={nseed}  国×四半期の行（cornint_s・10 国）={nrow}", "",
     "### A エンジンの穀物法の項の入力 engineRentShare（Build の Rent[b]÷Σ・国×四半期の中央・地代の正の行だけ）",
     "| 国 | 時期 | 旧支配層 0 | 近代 1 | 大衆 2 |", "|---|---|---|---|---|"]
for n in NAT:
    for p in PER:
        a = shr.get((n, p))
        if a: o.append(f"| {n} | {p[0]}〜{p[1]} | {q(a[0],.5):.3f} | {q(a[1],.5):.3f} | {q(a[2],.5):.3f} |")
o += ["", "### B 穀物の地代（国内＋輸出）のブロックの割合（和÷和・10 種）", "| 国 | 時期 | 0 | 1 | 2 |", "|---|---|---|---|---|"]
for n in NAT:
    for p in PER:
        g = grain.get((n, p)); t = sum(g) if g else 0
        if t > 0: o.append(f"| {n} | {p[0]}〜{p[1]} | {g[0]/t:.3f} | {g[1]/t:.3f} | {g[2]/t:.3f} |")
o += ["", "### C 実際に付いた地代の層の割合（landshare_s の landRentActual*・和÷和）と国家の枠 crownActual÷(層の和＋国家)",
      "| 国 | 時期 | 貴族 | 聖職者 | 農民 | 資本家 | 他 | 国家の枠 |", "|---|---|---|---|---|---|---|---|"]
for n in NAT:
    for p in PER:
        a = ls.get((n, p))
        if not a: continue
        t = sum(a[:5]); tc = t + a[5]
        if t > 0: o.append(f"| {n} | {p[0]}〜{p[1]} | " + " | ".join(f"{a[k]/t:.3f}" for k in range(5)) + f" | {a[5]/tc:.3f} |")
os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
with open(out, "wb") as fh:
    fh.write(("\n".join(o) + "\n").encode("utf-8"))
