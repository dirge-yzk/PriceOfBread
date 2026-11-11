# (1052) c99lb 設計の材料：盤面 m1044（keep_1044/base_ins）の労働の材料を数える（読むだけ・エンジンの関数を呼ばない）。
# 使い方: python labor_mat.py <生のフォルダ> <出力の .txt>
# 表：国の群 × 年（その年の全四半期の平均）× 種の中央値。
#  T1 lwk_s：工場労働者（role の bit1）の m・参加率、農民（bit2）の参加率、行の数
#  T2 labor_s：工場の実質賃金の指数・失業の割合・紙の〈女の 1 時間の稼ぎの指数〉＝実質賃金×(手 0.6＋0.4×m)（AI の紙・確証なし）
#  T3 svc_s：使用人÷人口
#  T4 recipeout_s：労働力のレシピごとの産出の割合と、労働力（既定）の生産者の数の世界の和（＝工場労働者の POP の数）
import csv, sys, os, statistics as st
from collections import defaultdict

RAW, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
YEARS = [1790, 1830, 1870, 1900, 1913, 1930, 1950]
GROUPS = [("英", ("イギリス",)), ("仏", ("フランス",)), ("独", ("プロイセン", "ドイツ")), ("米", ("アメリカ",)), ("露", ("ロシア", "ソ連", "ソビエト")), ("日", ("日本",))]
WF_HAND = 0.6  # Coefficients.FemaleHandLaborWeight（m1044 の値・紙の式の写し）

def grp(n):
    for g, keys in GROUPS:
        if any(k in n for k in keys): return g
    return None

def med(xs): return st.median(xs) if xs else float("nan")

def rd(name, s):
    p = os.path.join(RAW, f"{name}_s{s}.tsv")
    with open(p, encoding="utf-8", newline="") as f:
        yield from csv.DictReader(f, delimiter="\t")

def fnum(x):
    try:
        v = float(x); return v if v == v else None
    except Exception: return None

out = []
# T1 + T2
m_by = defaultdict(list); pw_by = defaultdict(list); pp_by = defaultdict(list); cnt_by = defaultdict(list)
rw_by = defaultdict(list); un_by = defaultdict(list); ern_by = defaultdict(list); frw_by = defaultdict(list)
for s in SEEDS:
    acc = defaultdict(lambda: [0.0, 0, 0.0, 0, 0.0, 0]); rows_q = defaultdict(int)
    for r in rd("lwk", s):
        y = int(r["year"])
        if y not in YEARS: continue
        g = grp(r["nation"])
        if not g: continue
        role = int(r["role"]); a = acc[(g, y)]
        m, pt = fnum(r["mShare"]), fnum(r["partShare"])
        if role & 1 and m is not None and pt is not None:
            a[0] += m; a[1] += 1; a[2] += pt; a[3] += 1
            rows_q[(g, y, r["quarter"])] += 1
        if role & 2 and pt is not None:
            a[4] += pt; a[5] += 1
    for (g, y), a in acc.items():
        if a[1]: m_by[(g, y)].append(a[0] / a[1])
        if a[3]: pw_by[(g, y)].append(a[2] / a[3])
        if a[5]: pp_by[(g, y)].append(a[4] / a[5])
    qs = defaultdict(list)
    for (g, y, q), c in rows_q.items(): qs[(g, y)].append(c)
    for k, v in qs.items(): cnt_by[k].append(sum(v) / len(v))
    lac = defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    for r in rd("labor", s):
        y = int(r["year"])
        if y not in YEARS: continue
        g = grp(r["nation"])
        if not g: continue
        rw, un, frw = fnum(r["plainRealPriceIndex"]), fnum(r["plainUnemploymentShare"]), fnum(r["farmRealPriceIndex"])
        if rw is None or un is None or frw is None: continue
        a = lac[(g, y)]; a[0] += rw; a[1] += un; a[2] += frw; a[3] += 1
    for (g, y), a in lac.items():
        if a[3]:
            rw_by[(g, y)].append(a[0] / a[3]); un_by[(g, y)].append(a[1] / a[3]); frw_by[(g, y)].append(a[2] / a[3])
    # 紙の稼ぎ（同じ種の m と実質賃金の積）
    for (g, y), a in lac.items():
        if a[3] and acc[(g, y)][1]:
            mm = acc[(g, y)][0] / acc[(g, y)][1]
            ern_by[(g, y)].append(a[0] / a[3] * (WF_HAND + (1 - WF_HAND) * mm))

def table(title, d, fmt="{:.3f}"):
    out.append(f"## {title}")
    out.append("群\t" + "\t".join(map(str, YEARS)))
    for g, _ in GROUPS:
        out.append(g + "\t" + "\t".join((fmt.format(med(d[(g, y)])) if d[(g, y)] else "-") for y in YEARS))
    out.append("")

table("T1a 工場労働者の動力の割合 m（mShare・行の平均→種の中央値）", m_by)
table("T1b 工場労働者の女性の参加率（partShare）", pw_by)
table("T1c 農民の女性の参加率（partShare）", pp_by)
table("T1d 工場労働者の行の数（四半期の平均）", cnt_by, "{:.1f}")
table("T2a 工場の実質賃金の指数（plainRealPriceIndex）", rw_by)
table("T2b 農業の実質賃金の指数（farmRealPriceIndex）", frw_by)
table("T2c 工場の失業の割合（plainUnemploymentShare）", un_by)
table("T2d 紙：女の 1 時間の稼ぎの指数＝実質賃金×(0.6＋0.4m)（AI の紙・確証なし）", ern_by)

# T3 使用人÷人口
sv_by = defaultdict(list)
for s in SEEDS:
    acc = defaultdict(lambda: [0.0, 0.0])
    for r in rd("svc", s):
        y = int(r["year"])
        if y not in YEARS: continue
        g = grp(r["nation"])
        if not g: continue
        sk, tp = fnum(r["servantK"]), fnum(r["totalPopulationK"])
        if sk is None or not tp: continue
        acc[(g, y)][0] += sk; acc[(g, y)][1] += tp
    for k, a in acc.items():
        if a[1] > 0: sv_by[k].append(a[0] / a[1])
table("T3 使用人÷人口（svc_s）", sv_by, "{:.4f}")

# T4 労働力のレシピの割合（世界の和）と工場労働者の POP の数
rec_by = defaultdict(list); npop_by = defaultdict(list); recnames = set()
for s in SEEDS:
    acc = defaultdict(float); tot = defaultdict(float); npop = defaultdict(int)
    for r in rd("recipeout", s):
        y = int(r["year"])
        if y not in YEARS or r["good"] != "労働力": continue
        o = fnum(r["outputSum"]) or 0.0
        acc[(r["recipe"], y)] += o; tot[y] += o; recnames.add(r["recipe"])
        if r["recipe"] == "(既定)": npop[y] += int(r["producers"])
    for (rc, y), o in acc.items():
        if tot[y] > 0: rec_by[(rc, y)].append(o / tot[y])
    for y, n in npop.items(): npop_by[y].append(n)
out.append("## T4a 労働力のレシピごとの産出の割合（世界の和・種の中央値）")
out.append("レシピ\t" + "\t".join(map(str, YEARS)))
for rc in sorted(recnames):
    out.append(rc + "\t" + "\t".join((f"{med(rec_by[(rc, y)]):.4f}" if rec_by[(rc, y)] else "-") for y in YEARS))
out.append("")
out.append("## T4b 労働力（既定）の生産者の数の世界の和（＝工場労働者の POP の数・種の中央値）")
out.append("\t".join(f"{y}:{med(npop_by[y]):.0f}" if npop_by[y] else f"{y}:-" for y in YEARS))
with open(OUT, "wb") as f:
    f.write(("\n".join(out) + "\n").encode("utf-8"))
print("ok", OUT)
