# (533) 記録：工場の産出 outputRefValuePerMonth（factory_s・10 種の中央値・年の最後の四半期）。1 番目を分母に比。
import csv, glob, os, sys, statistics as st
NATIONS = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
YEARS = [1800, 1820, 1850, 1880, 1900, 1913, 1930, 1950]
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def read(d):
    last = {}
    for p in glob.glob(os.path.join(d, 'factory_s*.tsv')):
        s = int(os.path.basename(p)[9:-4])
        for x in rows(p):
            try: k = (s, x['nation'], int(x['year'])); q = int(x['quarter']); v = float(x['outputRefValuePerMonth'])
            except ValueError: continue
            if k not in last or q > last[k][0]: last[k] = (q, v)
    return {k: v for k, (q, v) in last.items()}
arms = [(a.split('=', 1)[0], read(a.split('=', 1)[1])) for a in sys.argv[1:]]
print(f"# 工場の産出 outputRefValuePerMonth（10 種の中央値・年の最後の四半期）。比は {arms[0][0]} を分母")
for n in NATIONS:
    print(f"## {n}")
    print("年\t" + "\t".join(k for k, _ in arms) + "\t" + "\t".join(f"{k}/{arms[0][0]}" for k, _ in arms[1:]))
    for y in YEARS:
        vals = []
        for _, t in arms:
            vs = [v for (s, nn, yy), v in t.items() if nn == n and yy == y and v == v]
            vals.append((st.median(vs) if vs else float('nan'), len(vs)))
        b = vals[0][0]
        print(f"{y}\t" + "\t".join(f"{v:.4g}(n{c})" for v, c in vals) + "\t" + "\t".join((f"{v / b:.3f}" if b == b and b and v == v else "nan") for v, _ in vals[1:]))
