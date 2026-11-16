# (1173) 周辺の点検の数え（読むだけ）：包摂を 1 にする 3 法が立った行の数（sf_s・monlaw_s）
import sys, os, csv
from collections import Counter
for tag, d in (("base", "D:/PoB_work/keep_1171/base_ins"), ("n1", "D:/PoB_work/keep_1171/n1"), ("n2", "D:/PoB_work/keep_1171/n2"), ("a11", "D:/PoB_work/keep_1171/a11")):
    cs = Counter(); mg = Counter(); su = Counter(); seeds = 0
    for s in range(1, 21):
        f = os.path.join(d, f"sf_s{s}.tsv")
        if os.path.exists(f):
            seeds += 1
            with open(f, encoding="utf-8") as fh:
                r = csv.reader(fh, delimiter="\t"); h = next(r); a = h.index("colonialSuffrageLaw"); b = h.index("migrantNativeSuffrageLaw")
                for x in r: cs[int(float(x[a]))] += 1; mg[int(float(x[b]))] += 1
        f = os.path.join(d, f"monlaw_s{s}.tsv")
        if os.path.exists(f):
            with open(f, encoding="utf-8") as fh:
                r = csv.reader(fh, delimiter="\t"); h = next(r); a = h.index("Suffrage")
                for x in r: su[int(float(x[a]))] += 1
    print(tag, "sf の種", seeds, "植民地参政権", dict(sorted(cs.items())), "移民原住民参政権", dict(sorted(mg.items())), "参政権の段", dict(sorted(su.items())))
