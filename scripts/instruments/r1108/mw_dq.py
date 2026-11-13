# (1108) 段を上げた回（MinimumWage が前の四半期より上がった）の、選挙政体への移行からの四半期の距離の分布（読むだけ）。
import sys, collections, importlib.util, os
spec = importlib.util.spec_from_file_location("ms", os.path.join(os.path.dirname(os.path.abspath(__file__)), "mw_split.py"))
ELECT = {3, 4}
import csv, re
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    hist = collections.Counter(); transCount = 0; transWithEnact = 0; base = collections.Counter()
    for s in seeds(d):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, inn, ip, im = (h.index(k) for k in ("quarter", "nation", "polityCode", "MinimumWage"))
            for row in r: rows[row[inn]].append((int(row[iq]), int(row[ip]), int(float(row[im]))))
        for nat, seq in rows.items():
            seq.sort(); lastTrans = None; prevPol = None; prevSt = 0; hit = set()
            for (q, pol, st) in seq:
                if prevPol is not None and pol != prevPol and prevPol not in ELECT and pol in ELECT:
                    lastTrans = q; transCount += 1
                if pol not in ELECT: lastTrans = None
                if st > prevSt and lastTrans is not None:
                    dq = q - lastTrans
                    hist[dq if dq <= 8 else ("9-16" if dq <= 16 else "17+")] += 1
                    if dq <= 16 and lastTrans not in hit: transWithEnact += 1; hit.add(lastTrans)
                prevPol = pol; prevSt = st
    keys = [k for k in range(0, 9)] + ["9-16", "17+"]
    print(name, "移行（非選挙→選挙）", transCount, "うち 16 四半期以内に段を上げた移行", transWithEnact)
    print("   距離の分布", [(k, hist.get(k, 0)) for k in keys])
