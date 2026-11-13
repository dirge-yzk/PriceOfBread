# (1108) 挙げた国（sf_s が毎四半期の国）で、非選挙→選挙の移行の前後の議席の和と与党（sf_s の seat* と governingParty）を並べる（読むだけ）。
import sys, os, re, csv, collections
ELECT = {3, 4}
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    tab = collections.defaultdict(lambda: collections.Counter()); n = 0
    for f in sorted(os.listdir(d)):
        m = re.fullmatch(r"sf_s(\d+)\.tsv", f)
        if not m: continue
        s = m.group(1)
        pol = {}
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as g:
            r = csv.reader(g, delimiter="\t"); h = next(r); iq, inn, ip = h.index("quarter"), h.index("nation"), h.index("polityCode")
            for row in r: pol[(row[inn], int(row[iq]))] = int(row[ip])
        seats = {}
        with open(os.path.join(d, f), encoding="utf-8", newline="") as g:
            r = csv.reader(g, delimiter="\t"); h = next(r); iq, inn = h.index("quarter"), h.index("nation")
            ic, il, iso = h.index("seatCons"), h.index("seatLib"), h.index("seatSoc")
            for row in r: seats[(row[inn], int(row[iq]))] = float(row[ic]) + float(row[il]) + float(row[iso])
        for (nat, q), p in pol.items():
            pp = pol.get((nat, q - 1))
            if pp is None or pp in ELECT or p not in ELECT: continue
            if not all((nat, q + k) in seats for k in (-1, 0, 1, 2)): continue
            n += 1
            for k in (-1, 0, 1, 2):
                tab[k]["空" if seats[(nat, q + k)] < 1e-9 else "有"] += 1
    print(name, "挙げた国の移行（四半期の表がそろう物）", n)
    for k in (-1, 0, 1, 2): print("   移行から", k, "四半期：議席", dict(tab[k]))
