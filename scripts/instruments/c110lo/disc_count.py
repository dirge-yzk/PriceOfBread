# (1130) c110lo 記録：機関車を知る国の数（railspend_s の discEnd・四半期 160/268/400/667）と disc_s の E 行〈機関車〉の数（腕・旗 0・ヌル腕）。
#   python -X utf8 -I disc_count.py <腕の ins> <旗 0 の railspend の生> <基準の ins> <N1> <N2>
import sys, os, statistics
def known(d):
    out = []
    for s in range(1, 11):
        last = {}
        with open(os.path.join(d, f"railspend_s{s}.tsv"), encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.split("\t"); last.setdefault(int(c[1]), []).append(c[3])
        res = []
        for qq in (160, 268, 400, 667):
            q = max(k for k in last if k <= qq); v = last[q]; res.append(sum(1 for x in v if x == "1"))
        out.append(res)
    return out
def erows(d):
    n = []
    for s in range(1, 11):
        k = 0
        with open(os.path.join(d, f"disc_s{s}.tsv"), encoding="utf-8") as f:
            next(f)
            for ln in f:
                c = ln.split("\t")
                if c[0] == "E" and c[4] == "機関車": k += 1
        n.append(k)
    return n
arm, b0, base, n1, n2 = sys.argv[1:6]
print("機関車を知る国の数（種 1〜10・四半期 160〔1823〕/268〔1850〕/400〔1883〕/667〔1950〕）")
for nm, d in (("arm", arm), ("flag0", b0)):
    k = known(d); print(nm, " ".join("/".join(map(str, x)) for x in k), "中央 q400", statistics.median(x[2] for x in k), "q667", statistics.median(x[3] for x in k))
print("disc_s の E 行〈機関車〉の数（種 1〜10）")
for nm, d in (("arm", arm), ("base", base), ("n1", n1), ("n2", n2)):
    e = erows(d); print(nm, e, "和", sum(e))
