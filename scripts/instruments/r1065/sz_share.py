# (1065) 兵 0 の州の徴募元（農民＋工場労働者）が国の徴募元の何割か〈近似〉＝空席の大きさの桁（読むだけ）。呼び方：python -X utf8 -I sz_share.py <生> <種の始め> <種の終わり>
# 分母＝国の populationK（nation_s・全州）。分子＝兵 0 の植民地格の州の徴募元の人数（cpop_s）。RuleDelivery と年齢の構成は掛けない（近似＝空席の上限の桁）。
import sys, os, statistics as st
from collections import defaultdict
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
K = {"兵士".encode(): "S", "農民".encode(): "A", "工場労働者".encode(): "B"}
out = defaultdict(list)
for s in range(S0, S1 + 1):
    popn = {}
    with open(os.path.join(RAW, f"nation_s{s}.tsv"), "rb") as f:
        h = f.readline().decode().rstrip("\r\n").split("\t"); iq, ino, ip = h.index("quarter"), h.index("nation"), h.index("populationK")
        for line in f:
            t = line.decode().rstrip("\r\n").split("\t")
            if int(t[iq]) % 4 == 3: popn[(int(t[iq]), t[ino])] = float(t[ip])
    S = {}; A = defaultdict(float)
    with open(os.path.join(RAW, f"cpop_s{s}.tsv"), "rb") as f:
        f.readline()
        for line in f:
            t = line.split(b"\t", 9)
            c = K.get(t[6])
            if c is None or int(t[0]) % 4 != 3: continue
            k = (int(t[0]), t[2].decode(), t[3].decode())
            if c == "S": S[k] = float(t[8])
            else: A[k] += float(t[8])
    z = defaultdict(float)
    for k, v in S.items():
        if v == 0: z[(k[0], k[1])] += A[k]
    for (q, n), a in z.items():
        if popn.get((q, n), 0) > 0: out[n].append(a / popn[(q, n)])
for n, xs in sorted(out.items(), key=lambda z: -len(z[1]))[:8]:
    print(f"{n}\t国×年={len(xs)}\t中央={st.median(xs):.4f}\t最大={max(xs):.4f}")
