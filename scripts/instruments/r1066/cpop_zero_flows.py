# (1066) 人口 0 の POP（cpop_s の popK==0）が、お金を持つか・お金が動くかを数える（読むだけ）。
# 列ごとに「人口 0 の行のうち値が 0 でない行の数」と「その値の絶対値の和」を出す（種 1〜10・q%4==3 の全ての年）。
import sys, collections
D = sys.argv[1]
nz = collections.Counter(); sm = collections.Counter(); rows = 0; rows_all = 0
cls_sav = collections.Counter()
for s in range(1, 11):
    with open(f"{D}/cpop_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        ik = h.index("popK"); iq = h.index("quarter"); icl = h.index("className")
        cols = [i for i, k in enumerate(h) if k and i > ik]
        for line in f:
            c = line.rstrip("\n").split("\t")
            if int(c[iq]) % 4 != 3: continue
            rows_all += 1
            if float(c[ik]) != 0: continue
            rows += 1
            for i in cols:
                try: v = float(c[i])
                except ValueError: continue
                if v != 0: nz[h[i]] += 1; sm[h[i]] += abs(v)
            if float(c[h.index("savings")]) != 0: cls_sav[c[icl]] += 1
print(f"人口 0 の行 {rows} / 全行 {rows_all}（q%4==3・10 種）")
for k in sorted(nz, key=lambda k: -nz[k]):
    print(f"{k}\t非零の行 {nz[k]}\t割合 {nz[k]/rows:.4f}\t絶対値の和 {sm[k]:.6g}")
print("savings 非零の階級:", dict(cls_sav))
