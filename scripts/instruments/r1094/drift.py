# (1094) H031 現政体の参加の軸のずれ（DriftedPolityCoord）が 0 でない国×四半期の割合と大きさ（monlaw_s の polityCode・Suffrage・全ての国・毎四半期）
# 使い方: python -X utf8 -I drift.py <keep_dir> <out>
import os, sys
keep, out = sys.argv[1], sys.argv[2]
ARMS = [("M", "base_ins", range(1, 11)), ("N1", "n1", range(1, 11)), ("N2", "n2", range(1, 11)), ("S11", "a11", range(11, 21))]
BASE0 = [0.00, 0.05, 0.00, 0.60, 0.95, 0.30, 0.30, 0.15, 0.70]   # Ideology.cs:248〜256 の参加の軸
OPEN = [0.00, 0.55, 0.80, 0.95]                                  # Ideology.cs:311〜314 Openness
W = 0.4                                                          # Coefficients.cs:372 PolityCoordDriftWeight
with open(out, "w", encoding="utf-8", newline="\n") as o:
    o.write("arm\trows\tdriftNonzero\tabsDriftP50\tabsDriftP90\tbyPolity(rows:nonzero)\n")
    for arm, sub, seeds in ARMS:
        n = nz = 0; mags = []; byp = {}
        for s in seeds:
            with open(os.path.join(keep, sub, f"monlaw_s{s}.tsv"), "rb") as f:
                hdr = f.readline().decode("utf-8").rstrip("\r\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
                for line in f:
                    r = line.decode("utf-8").rstrip("\r\n").split("\t")
                    try:
                        pc = int(float(r[ix["polityCode"]])); su = int(float(r[ix["Suffrage"]]))
                    except Exception:
                        continue
                    if not (0 <= pc < 9 and 0 <= su < 4):
                        continue
                    d = W * (OPEN[su] - BASE0[pc]); n += 1
                    b = byp.setdefault(pc, [0, 0]); b[0] += 1
                    if abs(d) > 1e-12:
                        nz += 1; mags.append(abs(d)); b[1] += 1
        mags.sort()
        p = lambda q: mags[int(q * (len(mags) - 1))] if mags else float("nan")
        o.write(f"{arm}\t{n}\t{nz/n:.4f}\t{p(0.5):.3f}\t{p(0.9):.3f}\t" + " ".join(f"{k}:{v[0]}:{v[1]}" for k, v in sorted(byp.items())) + "\n")
print("ok")
