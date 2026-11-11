# (1000) c91ld 読むだけの台本：★(899) ② (c)〈着値の係数 A・B を毎月組み直す〉の費用を、生の行の数と前提の単価から見積もる（計っていない＝見積もり）。
#  国の数＝cornint_s の国×四半期の行÷四半期（種ごと）・州の数＝landbasereg_s の州×四半期の行÷四半期（下限）と world.json の州の数 227（上限）。
#  単価（前提・計っていない）：州の市場の欄の冷えた読み 5〜20 ns・掛けて足す 1 ns・大圏の距離 1 回 60 ns。財は 7（Tier Staple 5＋GrainChain 2・Catalog.cs）。
#  Build の呼び数（国×四半期）は grep の 7 か所から 4〜12 と置いた（前提）。
# 使い方：python -X utf8 -I cost1000.py <生のフォルダ> <出力のファイル>
import sys, os
d, out = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11); G = 7; REG_UP = 227
QUARTERS = 668; MONTHS = QUARTERS * 3
BASE_SEC, CAP_SEC = 211.5, 248.5   # m988 の推定の計算量と上限（README の成績表から写した）

def rows_per_q(p):
    with open(p, "rb") as fh:
        L = fh.read().decode("utf-8").splitlines()[1:]
    qs = {}
    for ln in L:
        q = ln.split("\t", 1)[0]; qs[q] = qs.get(q, 0) + 1
    return sum(qs.values()) / max(1, len(qs)), max(qs.values())

nat = []; reg = []
for s in SEEDS:
    pc = os.path.join(d, f"cornint_s{s}.tsv"); pr = os.path.join(d, f"landbasereg_s{s}.tsv")
    if os.path.exists(pc): nat.append(rows_per_q(pc))
    if os.path.exists(pr): reg.append(rows_per_q(pr))
N = sum(a for a, _ in nat) / len(nat); Nmax = max(b for _, b in nat)
R = sum(a for a, _ in reg) / len(reg)
o = [f"## (c) の費用の見積もり  生={d}  種={len(nat)}",
     f"- 国の数（四半期の平均）{N:.1f}・最大 {Nmax}／州の数（landbasereg_s の平均）{R:.1f}・上限 {REG_UP}",
     f"- 基準：m988 の推定の計算量 {BASE_SEC} 秒・上限 {CAP_SEC} 秒＝余り {CAP_SEC-BASE_SEC:.1f} 秒・国×月あたり {BASE_SEC/(MONTHS*N)*1e3:.2f} ms", "",
     "| 形 | 1 回の中身 | 回数 | 演算（読み・距離・掛け足し） | 推定の秒（安い単価〜高い単価） | 余りに対する割合 |", "|---|---|---|---|---|---|"]

def line(name, what, calls, reads, dists, madds):
    lo = calls * (reads * 5e-9 + dists * 60e-9 + madds * 1e-9)
    hi = calls * (reads * 20e-9 + dists * 60e-9 + madds * 1e-9)
    slack = CAP_SEC - BASE_SEC
    o.append(f"| {name} | {what} | {calls:,.0f} | {reads:,.0f}・{dists:,.0f}・{madds:,.0f} | {lo:.2f}〜{hi:.2f} | {lo/slack:.1%}〜{hi/slack:.1%} |")

for Rx, tag in [(R, "州=平均"), (REG_UP, "州=上限")]:
    # (c-月) 買い手ごと・毎月：全ての国の州の市場（7 財×5 欄）を読み、売り手ごとに距離 1 回と 7 財の掛け足し
    line(f"(c-月) 買い手が毎月全ての州を読む（{tag}）", "全州×7 財×5 欄＋売り手の距離＋7×売り手", N * MONTHS, Rx * G * 5, N, G * N * 2)
    # (c-呼) Build の呼びごと（国×四半期×K）
    for K in (4, 12):
        line(f"(c-呼) Build の呼びごと K={K}（{tag}）", "同上", N * QUARTERS * K, Rx * G * 5, N, G * N * 2)
# (c-共有) 共有の段で毎月 1 回だけ全ての州を読み、買い手ごとに売り手の和
line("(c-共有) 共有の段で毎月 1 回（州=上限）", "全州×7×5 を 1 回＋買い手×売り手", MONTHS, REG_UP * G * 5 + 0, N * N, G * N * N * 2)
# (c‴) 判定の段の世界の市場（既に組まれている）から A・B を Build の呼びごとに
for K in (4, 12):
    line(f"(c‴) 判定の段の世界の市場から Build ごと K={K}", "売り手の距離＋7×売り手", N * QUARTERS * K, 0, N, G * N * 2)
# (a′) 書き手は四半期の末の共有の段で 1 回
line("(a′) 共有の段で四半期に 1 回書く", "買い手×売り手の距離＋7×売り手", QUARTERS, 0, N * N, G * N * N * 2)
o += ["", "- 名乗り：単価は計っていない前提（冷えた読みの幅は (高速化の段) の〈生産の段は冷えた読みが律速〉から 5〜20 ns と置いた）。回数と演算の数は生とソースから。確証＝見積もり。",
      "- (c-月) と (c-呼) は〈どの断面を読むか〉が国の並びに依る（四半期の末の読み手）＝費用の前に形の問題がある（下書き §3）。"]
os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
with open(out, "wb") as fh:
    fh.write(("\n".join(o) + "\n").encode("utf-8"))
