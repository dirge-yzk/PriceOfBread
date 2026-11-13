# (1118) 記録：開幕の選挙政体の席の組み＝旗 1 の四半期 0 の保守の席の割合 と 旗 0 の四半期 1 の保守の席の割合（同じ種・同じ nationUid・読むだけ）
import sys, os, collections
A, B = sys.argv[1], sys.argv[2]
def rd(d, s, q):
    out = {}
    with open(os.path.join(d, f"parlseat_s{s}.tsv"), encoding="utf-8") as f:
        h = f.readline().rstrip("\r\n").split("\t")
        for line in f:
            r = dict(zip(h, line.rstrip("\r\n").split("\t")))
            if int(r["quarter"]) == q and float(r["seatSumEnd"]) > 1e-9: out[(r["nationUid"], r["nation"])] = float(r["seatConsEnd"]) / float(r["seatSumEnd"])
    return out
by = collections.defaultdict(list)
for s in range(1, 11):
    a = rd(A, s, 0); b = rd(B, s, 1)
    for k, v in a.items():
        if k in b: by[k[1]].append((v, b[k]))
for n, l in sorted(by.items()):
    d = [x - y for x, y in l]
    print(f"{n}：種 {len(l)}・旗 1 の q0 の保守の席 平均 {sum(x for x, _ in l) / len(l):.3f}・旗 0 の q1 {sum(y for _, y in l) / len(l):.3f}・差の最大の絶対値 {max(abs(x) for x in d):.3f}")
