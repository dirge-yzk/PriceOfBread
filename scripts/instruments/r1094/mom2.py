# (1094) 結果の後の切り分け：便乗の勢いの飽和が Math.Max(1, 保持力) の床によるかを crownpc_s で分ける（4 腕）
# 使い方: python -X utf8 -I mom2.py <keep_dir> <out>
import os, sys
keep, out = sys.argv[1], sys.argv[2]
ARMS = [("M", "base_ins", range(1, 11)), ("N1", "n1", range(1, 11)), ("N2", "n2", range(1, 11)), ("S11", "a11", range(11, 21))]
def fl(x):
    try: return float(x)
    except Exception: return float("nan")
with open(out, "w", encoding="utf-8", newline="\n") as o:
    o.write("arm\tpolEnabledRows\tcoerceLt1\tsat_coerceLt1\tsat_coerceGe1\tmomP50_coerceGe1\tmobOverCoerceP50_Ge1\tsatByPolity(rows:sat)\n")
    for arm, sub, seeds in ARMS:
        n = lt1 = slt = sge = nge = 0; mg = []; ratio = []; byp = {}
        for s in seeds:
            prev = {}
            with open(os.path.join(keep, sub, f"crownpc_s{s}.tsv"), "rb") as f:
                hdr = f.readline().decode("utf-8").rstrip("\r\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
                for line in f:
                    r = line.decode("utf-8").rstrip("\r\n").split("\t")
                    qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
                    mob, co = fl(r[ix["mobilizationIndex"]]), fl(r[ix["effectiveCoerceIndex"]])
                    en = r[ix["politicsEnabledFlag"]] == "1"; pc = r[ix["polityCode"]]
                    p = prev.get(nat)
                    prev[nat] = (qt, mob, co, en)
                    if not p or p[0] != qt - 1 or not p[3] or not en or not (p[1] == p[1] and p[2] == p[2]):
                        continue
                    n += 1
                    mom = min(1.0, max(0.0, p[1] / max(1.0, p[2]) * 0.45))
                    sat = mom >= 1.0
                    b = byp.setdefault(pc, [0, 0]); b[0] += 1; b[1] += sat
                    if p[2] < 1.0:
                        lt1 += 1; slt += sat
                    else:
                        nge += 1; sge += sat; mg.append(mom); ratio.append(p[1] / p[2])
        mg.sort(); ratio.sort()
        md = lambda v: v[len(v) // 2] if v else float("nan")
        o.write(f"{arm}\t{n}\t{lt1/n:.4f}\t{slt/max(1,lt1):.4f}\t{sge/max(1,nge):.4f}\t{md(mg):.4f}\t{md(ratio):.4f}\t" + " ".join(f"{k}:{v[0]}:{v[1]}" for k, v in sorted(byp.items())) + "\n")
print("ok")
