# (1065) 志願の道が兵＞0 の植民地格の州で働いているか（読むだけ）。志願→志願 の続く四半期で、兵が前の四半期より増えた／減った／同じの件数と、増えた量の中央。
# 呼び方：python -X utf8 -I sz_volinc.py <生> <種の始め> <種の終わり>
# 限界：増えた分が志願か、州の外から運ばれた分（割譲は系列が切れるので入らない）かは分けられない＝増えるのは志願（FillJobs）か年齢の層の入れ替わりだけ〈推〉。
import sys, os, statistics as st
from collections import defaultdict, Counter
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
SOL = "\t兵士\t".encode()
cnt = Counter(); rel = []; natInc = Counter(); natAll = Counter()
for s in range(S0, S1 + 1):
    law = {}
    with open(os.path.join(RAW, f"nation_s{s}.tsv"), "rb") as f:
        h = f.readline().decode().rstrip("\r\n").split("\t"); iq, ino, il = h.index("quarter"), h.index("nation"), h.index("conscriptionLaw")
        for line in f:
            t = line.decode().rstrip("\r\n").split("\t"); law[(int(t[iq]), t[ino])] = t[il]
    S = {}
    with open(os.path.join(RAW, f"cpop_s{s}.tsv"), "rb") as f:
        f.readline()
        for line in f:
            if SOL not in line: continue
            t = line.split(b"\t", 9); S[(int(t[0]), t[2].decode(), t[3].decode())] = float(t[8])
    for (q, n, r), v in S.items():
        pv = S.get((q - 1, n, r))
        if pv is None or not (pv > 0): continue
        if law.get((q - 1, n)) != "0" or law.get((q, n)) != "0": continue
        natAll[n] += 1
        if v > pv * (1 + 1e-6): cnt["増えた"] += 1; rel.append(v / pv - 1); natInc[n] += 1
        elif v < pv * (1 - 1e-6): cnt["減った"] += 1
        else: cnt["同じ"] += 1
print("志願→志願・兵＞0 の州×四半期：", dict(cnt), " 増えた時の増分の割合の中央=", f"{st.median(rel):.4f}" if rel else "-")
for n, a in natAll.most_common(6): print(f"{n}\t州×四半期={a}\t増えた={natInc[n]}")
