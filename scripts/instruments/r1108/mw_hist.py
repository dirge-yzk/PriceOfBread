# (1108) 最低賃金法の施行の行を〈移行の直後（距離 0〜1）に上げた段の続き〉と〈それ以外〉に分け、年代の分布を並べる（読むだけ・史実の比較は記録）。
import sys, os, re, csv, collections, statistics
ELECT = {3, 4}
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
BINS = [(1783, 1850), (1850, 1894), (1894, 1909), (1909, 1938), (1938, 1951)]
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    rows_v = collections.Counter(); rows_o = collections.Counter(); ev_v = []; ev_o = []
    for s in seeds(d):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, iy, inn, ip, im = (h.index(k) for k in ("quarter", "year", "nation", "polityCode", "MinimumWage"))
            for row in r: rows[row[inn]].append((int(row[iq]), int(row[iy]), int(row[ip]), float(row[im])))
        for nat, seq in rows.items():
            seq.sort(); lastTrans = None; prevPol = None; prevSt = 0.0; vac = False
            for (q, y, pol, st) in seq:
                if prevPol is not None and pol != prevPol and prevPol not in ELECT and pol in ELECT: lastTrans = q
                if pol not in ELECT: lastTrans = None
                if st > prevSt:
                    if prevSt == 0: vac = lastTrans is not None and q - lastTrans <= 1; (ev_v if vac else ev_o).append(y)
                if st == 0: vac = False
                if st > 0:
                    b = next(i for i, (lo, hi) in enumerate(BINS) if lo <= y < hi)
                    (rows_v if vac else rows_o)[b] += 1
                prevPol = pol; prevSt = st
    lab = [f"{lo}-{hi-1}" for lo, hi in BINS]
    print(f"== {name}")
    print("   施行の行（直後の続き）", [(lab[i], rows_v[i]) for i in range(len(BINS))], "計", sum(rows_v.values()))
    print("   施行の行（それ以外）  ", [(lab[i], rows_o[i]) for i in range(len(BINS))], "計", sum(rows_o.values()))
    for nm, ev in (("直後", ev_v), ("それ以外", ev_o)):
        if ev: print(f"   0→施行の回（{nm}）{len(ev)}・年の中央 {statistics.median(ev)}・1894 年より前 {sum(1 for y in ev if y < 1894)}")
