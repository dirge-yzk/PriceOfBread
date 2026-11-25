# (1376) lawstep_s の件数を書き手の別（writerCode 1〜6）ごとに 10 種で数える（ts の事前登録の〈記録〉の項）。python -I lawstep_count.py <生のフォルダ>
import sys, glob, os, collections
sys.stdout.reconfigure(encoding="utf-8")
d = sys.argv[1]; tot = collections.Counter(); ax6 = collections.Counter(); per = {}
for p in sorted(glob.glob(os.path.join(d, "lawstep_s*.tsv"))):
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); iw = h.index("writerCode"); ia = h.index("axis")
        c = collections.Counter()
        for ln in f:
            r = ln.rstrip("\n").split("\t"); c[r[iw]] += 1
            if r[iw] == "6": ax6[r[ia]] += 1
        per[os.path.basename(p)] = c; tot.update(c)
print("writerCode\t件数（10 種の和）")
for k in sorted(tot): print(f"{k}\t{tot[k]}")
print("和\t%d" % sum(tot.values()))
print("書き手 6 の軸の内訳：" + "・".join(f"{a} {n}" for a, n in ax6.most_common()))
for k, c in per.items(): print(k + "\t" + " ".join(f"{w}:{c[w]}" for w in sorted(c)))
