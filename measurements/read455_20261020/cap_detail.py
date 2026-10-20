import csv, os, sys, collections
PREF = ("共和制", "立憲王政")
def base(n):
    for p in PREF:
        if n.startswith(p): return n[len(p):]
    return n
def load(root, s, cls):
    ser = collections.defaultdict(dict); tot = collections.defaultdict(float)
    with open(os.path.join(root, f"class_s{s}.tsv"), encoding="utf-8") as fh:
        for r in csv.DictReader(fh, delimiter="\t"):
            t = r["time"]
            if not t.startswith("q"): continue
            q = int(t[1:]); n = base(r["nation"]); sz = float(r["sizeK"])
            tot[(n, q)] += sz
            if int(r["classId"]) == cls:
                ser[n][q] = ser[n].get(q, 0) + sz
    big = collections.defaultdict(float)
    for (n, q), v in tot.items(): big[n] = max(big[n], v)
    return ser, big
def stats(d):
    qs = sorted(d); v = [d[q] for q in qs]
    pk = max(v); ip = v.index(pk); after = v[ip:]; mn = min(after); im = ip + after.index(mn)
    return dict(peak=pk, qpk=qs[ip], mn=mn, qmn=qs[im], end=v[-1], qend=qs[-1],
                at = {y: d.get(y) for y in (68, 268, 468, 524, 600, 660)})
root0, root1, s = sys.argv[1], sys.argv[2], int(sys.argv[3]); cls = int(sys.argv[4]) if len(sys.argv) > 4 else 3
a, bigA = load(root0, s, cls); b, bigB = load(root1, s, cls)
names = sorted(set(a) | set(b), key=lambda n: -max(bigA.get(n, 0), bigB.get(n, 0)))
print("国\t大国\tF0 ピーク(q)\tF0 最小/ピーク\tF0 終わり/ピーク\tF0 終わり(千人)\t|\tF1 ピーク(q)\tF1 最小/ピーク\tF1 終わり/ピーク\tF1 終わり(千人)")
c = collections.Counter()
for n in names:
    if n not in a or n not in b: continue
    x = stats(a[n]); y = stats(b[n])
    if max(x["peak"], y["peak"]) < 5: continue
    bg = max(bigA.get(n, 0), bigB.get(n, 0)) >= 5000
    if bg:
        c["n"] += 1; c["f0fell"] += x["mn"] < .05 * x["peak"]; c["f1fell"] += y["mn"] < .05 * y["peak"]
        c["f0end"] += x["end"] < .05 * x["peak"]; c["f1end"] += y["end"] < .05 * y["peak"]
    print(f"{n}\t{'大' if bg else ''}\t{x['peak']:.0f}({x['qpk']})\t{x['mn']/x['peak']:.3f}\t{x['end']/x['peak']:.3f}\t{x['end']:.1f}\t|\t{y['peak']:.0f}({y['qpk']})\t{y['mn']/y['peak']:.3f}\t{y['end']/y['peak']:.3f}\t{y['end']:.1f}")
print(f"# 大国 {c['n']} 国：経路の最小<5% F0 {c['f0fell']}・F1 {c['f1fell']}／終わり<5% F0 {c['f0end']}・F1 {c['f1end']}")
