# PYTHONIOENCODING=utf-8 で呼ぶ（Windows の既定では標準出力が化ける）
# (1059) A1 の予言で 0 に届いた州の、届いた年の州の人口（popK）と大衆（crowdK）・兵（読むだけ）
import sys, os
RAW, OUTTXT = sys.argv[1], sys.argv[2]
hits = []
f = False
for line in open(OUTTXT, encoding="utf-8"):
    if line.startswith("# A1 で 0 に届く州の一覧"): f = True; continue
    if f and line.strip():
        s, n, r, y, t = line.rstrip("\n").split("\t"); hits.append((int(s), n, r, int(y)))
by = {}
for s, n, r, y in hits: by.setdefault(s, set()).add((n, r, y))
res = []
for s, want in sorted(by.items()):
    p = os.path.join(RAW, f"secession_s{s}.tsv")
    got = {}
    with open(p, "rb") as fh:
        fh.readline()
        for line in fh:
            t = line.decode("utf-8").split("\t")
            k = (t[2], t[3], int(t[1]))
            if k in want: got[k] = (float(t[13]), float(t[14]))
    for k in sorted(want):
        res.append((s,) + k + got.get(k, (float("nan"), float("nan"))))
small = sum(1 for x in res if x[4] < 0.1); mid = sum(1 for x in res if 0.1 <= x[4] < 10)
print(f"予言の件数 {len(res)}・人口 0.1K 未満 {small}・0.1〜10K {mid}・10K 以上 {len(res)-small-mid}")
for x in sorted(res, key=lambda z: z[4])[:12]: print("\t".join(str(v) if not isinstance(v, float) else f"{v:.3g}" for v in x))
