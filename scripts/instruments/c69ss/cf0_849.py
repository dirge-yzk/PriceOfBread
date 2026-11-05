# (849) 旗 0（ident の生）の反実仮想：計器の ρ で数えた (b′) と今の失業・計器の食べる割合と今の食べる割合の差（読むだけ）
import sys, os
d = sys.argv[1]; n = pos = negbad = neg = sd = 0
for s in range(1, 11):
    with open(os.path.join(d, "subst_s%d.tsv" % s), encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            if c[ix["s2RhoSource"]] != "1" or int(c[ix["quarter"]]) < 1: continue
            try: e = float(c[ix["s2EEnd"]]); ue = float(c[ix["s2UnempEngine"]]); se = float(c[ix["s2SigmaEngine"]]); si = float(c[ix["s2SigmaInst"]])
            except ValueError: continue
            if e != e or ue != ue: continue
            n += 1
            if e > 0: pos += 1
            else:
                neg += 1
                if ue > 1e-12: negbad += 1
            if si == si and abs(se - si) > 1e-6: sd += 1
print("旗 0 の反実仮想（計器が焼いた州・q≧1）：行 %d・E＞0 %d・E≦0 %d（うち今の失業＞1e-12 %d＝%.4f）・|σ_今−σ_計器|＞1e-6 の行 %d（%.4f）" % (n, pos, neg, negbad, negbad / neg if neg else float("nan"), sd, sd / n if n else float("nan")))
