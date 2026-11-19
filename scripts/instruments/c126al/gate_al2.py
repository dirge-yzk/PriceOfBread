# (1242) c126al 腕 B の史実の桁の線の門と記録（紙の台本・腕と基準を同じ台本で読む）。
# 使い方: python gate_al2.py <腕の生 ins> <基準の生 ins> [種の起点=1] [種の数=10]
# G-B1（門）甜菜糖の発見：engineer_s の行〈甜菜糖〉で knownFlag が 1 になった最初の年（どの国でも）。
#        門＝1950 年までに発見された種の数が、腕で基準より少ない＝赤（旗 1 で発見が永久に 0 になる形を捕まえる）。年は記録（史実 1801 年〈推〉）。
# 記録 R1：甜菜糖の行の親の技師（engineer_s の engineerPerMonth・1785 年の四半期の国の中央）の種の中央。
# 記録 R2：イギリスのパンの〈値段÷(基準×州の物価)〉の州の中央（pxgoods の列 priceOverRefTimesLevelMedianRatio・1783〜1795）の種の中央。
# 記録 R3：植民地格÷本国格の工場の産出の 1 人あたりの比（region・10 年ごと・種の中央）。
import sys, os, statistics as st
arm, base = sys.argv[1], sys.argv[2]
sb = int(sys.argv[3]) if len(sys.argv) > 3 else 1
n = int(sys.argv[4]) if len(sys.argv) > 4 else 10
seeds = range(sb, sb + n)

def rows(d, t, s):
    with open(os.path.join(d, f"{t}_s{s}.tsv"), encoding="utf-8") as f:
        h = next(f).rstrip("\n").split("\t")
        for ln in f:
            c = ln.rstrip("\n").split("\t")
            if len(c) == len(h): yield dict(zip(h, c))

def beet(d):
    first, eng = {}, {}
    for s in seeds:
        e = []
        for r in rows(d, "engineer", s):
            if r["row"] != "甜菜糖": continue
            if r["knownFlag"] == "1" and s not in first: first[s] = int(r["year"])
            if r["year"] == "1785":
                try: e.append(float(r["engineerPerMonth"]))
                except ValueError: pass
        if e: eng[s] = st.median(e)
    return first, eng

def bread(d):
    out = {}
    for s in seeds:
        v = []
        for r in rows(d, "pxgoods", s):
            if r["nation"] == "イギリス" and r["goodName"] == "パン" and r["time"].startswith("q"):
                try: v.append(float(r["priceOverRefTimesLevelMedianRatio"]))
                except ValueError: pass
        if v: out[s] = st.median(v)
    return out

def colratio(d):
    per = {}
    for s in seeds:
        acc = {}
        for r in rows(d, "region", s):
            if r["activeFlag"] != "1": continue
            try: dec, p, o = int(r["year"]) // 10 * 10, float(r["populationK"]), float(r["factoryOutputRefValuePerMonth"])
            except ValueError: continue
            if not (p == p and o == o): continue
            a = acc.setdefault(dec, [0.0, 0.0, 0.0, 0.0])
            if r["tier"] == "Colonial": a[0] += o; a[1] += p
            elif r["tier"] == "Core": a[2] += o; a[3] += p
        for dec, a in acc.items():
            if a[1] > 0 and a[3] > 0 and a[2] > 0: per.setdefault(dec, []).append((a[0] / a[1]) / (a[2] / a[3]))
    return {k: st.median(v) for k, v in sorted(per.items())}

fa, ea = beet(arm); fb, eb = beet(base)
na, nb = sum(1 for s in seeds if s in fa and fa[s] <= 1950), sum(1 for s in seeds if s in fb and fb[s] <= 1950)
med = lambda m: st.median(m.values()) if m else float("nan")
print(f"G-B1 甜菜糖の発見のあった種 腕 {na}/{n}・基準 {nb}/{n}（門＝腕＜基準で赤）")
print(f"  記録 最初の年 腕 {[fa.get(s) for s in seeds]} 中央 {med(fa)}・基準 {[fb.get(s) for s in seeds]} 中央 {med(fb)}（史実 1801〈推〉）")
print(f"  記録 R1 甜菜糖の親の技師（1785 の国の中央・種の中央）腕 {med(ea):.4g}・基準 {med(eb):.4g}・腕で 0 の種 {sum(1 for v in ea.values() if not v > 0)}")
ba, bb = bread(arm), bread(base)
print(f"記録 R2 イギリスのパンの州の中央の比（1783〜1795・種の中央）腕 {med(ba):.4g}・基準 {med(bb):.4g}")
ra, rb = colratio(arm), colratio(base)
print("記録 R3 植民地格÷本国格の工場の産出の 1 人あたり（10 年・種の中央）", " ".join(f"{k}:{rb[k]:.3g}→{ra.get(k, float('nan')):.3g}" for k in rb))
print("判定:", "門 赤" if na < nb else "門 緑")
