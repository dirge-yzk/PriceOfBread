# (1237) c125al 史実の桁の線の門（紙の台本・腕 B と基準を同じ台本で読む）。
# 使い方: python gate_al.py <腕の生 ins> <基準の生 ins> [種の数=10]
# Q1＝食品の加工の 10 財×計器の国の〈値段÷(基準×州の物価) の州の中央〉（pxgoods の列 12）の四半期の中央→種の中央。
#     門 G-M1：基準で [0.1, 10] の内で、腕で外へ出た（国×財）の組の数＝0。
# Q2＝植民地格の州の工場の産出÷人口 を 本国格（Core）の同じ量で割った比（region の列 28・列 8）の 10 年ごとの値→種の中央。
#     門 G-M2：基準で [0.01, 1]〈推〉の内で、腕で外へ出た 10 年の数＝0。
# 記録：イギリスのパンの州の最大の比（pxgoods の列 13）の 1785・1790・1795 の種の中央。
# ⚠ pxgoods は助走 20 と本番の最初の 48 四半期（1783〜1795）だけ＝Q1 は開幕の 12 年の量。
import sys, os, statistics as st
FOOD = ["小麦粉", "パン", "肉", "砂糖", "加工肉", "干物", "ビール", "蒸留酒", "チーズ", "チョコレート"]

def q1(d, n):
    per = {}
    for s in range(1, n + 1):
        acc = {}
        with open(os.path.join(d, f"pxgoods_s{s}.tsv"), encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                if len(c) < 13 or c[3] not in FOOD or not c[0].startswith("q"): continue
                try: v = float(c[11])
                except ValueError: continue
                if v == v: acc.setdefault((c[1], c[3]), []).append(v)
        for k, v in acc.items(): per.setdefault(k, []).append(st.median(v))
    return {k: st.median(v) for k, v in per.items()}

def brit_max(d, n):
    out = {}
    for s in range(1, n + 1):
        with open(os.path.join(d, f"pxgoods_s{s}.tsv"), encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                if len(c) < 13 or c[1] != "イギリス" or c[3] != "パン" or not c[0].startswith("q"): continue
                y = int(c[2])
                if y in (1785, 1790, 1795):
                    try: out.setdefault(y, {}).setdefault(s, []).append(float(c[12]))
                    except ValueError: pass
    return {y: st.median([st.median(v) for v in m.values()]) for y, m in sorted(out.items())}

def q2(d, n):
    per = {}
    for s in range(1, n + 1):
        acc = {}
        with open(os.path.join(d, f"region_s{s}.tsv"), encoding="utf-8") as f:
            hdr = next(f).rstrip("\n").split("\t")
            iT, iP, iF, iY, iA = hdr.index("tier"), hdr.index("populationK"), hdr.index("factoryOutputRefValuePerMonth"), hdr.index("year"), hdr.index("activeFlag")
            for ln in f:
                c = ln.rstrip("\n").split("\t")
                if len(c) <= iF or c[iA] != "1": continue
                try: dec, p, o = int(c[iY]) // 10 * 10, float(c[iP]), float(c[iF])
                except ValueError: continue
                if not (p == p and o == o): continue
                a = acc.setdefault(dec, [0.0, 0.0, 0.0, 0.0])
                if c[iT] == "Colonial": a[0] += o; a[1] += p
                elif c[iT] == "Core": a[2] += o; a[3] += p
        for dec, a in acc.items():
            if a[1] > 0 and a[3] > 0 and a[2] > 0: per.setdefault(dec, []).append((a[0] / a[1]) / (a[2] / a[3]))
    return {k: st.median(v) for k, v in sorted(per.items())}

arm, base = sys.argv[1], sys.argv[2]
n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
qa, qb = q1(arm, n), q1(base, n)
newly1 = [(k, qb[k], qa[k]) for k in qb if k in qa and 0.1 <= qb[k] <= 10 and not (0.1 <= qa[k] <= 10)]
print(f"G-M1 食品の加工の財の州の中央の比 [0.1,10] の外へ新しく出た組={len(newly1)}（比べた組 {len(qb)}）")
for k, b, a in newly1: print("  外へ", k, f"基準 {b:.4g} 腕 {a:.4g}")
out_b = sorted(k for k in qb if not (0.1 <= qb[k] <= 10))
print(f"  記録：基準で外の組 {len(out_b)}・腕で外の組 {sum(1 for k in qa if not (0.1 <= qa[k] <= 10))}")
for k in sorted(qb):
    if k[1] in ("パン", "小麦粉"): print(f"  記録 {k[0]} {k[1]} 基準 {qb[k]:.4g} 腕 {qa.get(k, float('nan')):.4g}")
ra, rb = q2(arm, n), q2(base, n)
newly2 = [(k, rb[k], ra[k]) for k in rb if k in ra and 0.01 <= rb[k] <= 1 and not (0.01 <= ra[k] <= 1)]
print(f"G-M2 植民地格÷本国格の工場の産出の 1 人あたりの比 [0.01,1]〈推〉の外へ新しく出た 10 年={len(newly2)}（比べた 10 年 {len(rb)}）")
for k in rb: print(f"  記録 {k} 基準 {rb[k]:.4g} 腕 {ra.get(k, float('nan')):.4g}")
print("記録 イギリスのパンの州の最大の比（種の中央）基準", {k: round(v, 3) for k, v in brit_max(base, n).items()}, "腕", {k: round(v, 3) for k, v in brit_max(arm, n).items()})
print("判定:", "門 緑" if not newly1 and not newly2 else "門 赤")
