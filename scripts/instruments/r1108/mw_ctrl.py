# (1108) 対照：最低賃金法の段を上げる率を、移行の種類（非選挙→選挙＝議席が空になる／選挙→選挙＝議席は空にならない）と距離（1・2〜4）で、他の選挙政体の四半期と比べる（読むだけ）。
import sys, os, re, csv, collections
ELECT = {3, 4}
LAWS = ("MinimumWage", "Censorship", "LaborUnion")
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    opp = collections.Counter(); up = collections.Counter()
    for s in seeds(d):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, inn, ip = h.index("quarter"), h.index("nation"), h.index("polityCode")
            il = [h.index(k) for k in LAWS]
            for row in r: rows[row[inn]].append((int(row[iq]), int(row[ip]), [float(row[i]) for i in il]))
        for nat, seq in rows.items():
            seq.sort(); last = None; kind = None; prev = None
            for (q, pol, v) in seq:
                if prev is not None and pol != prev[1] and pol in ELECT:
                    last = q; kind = "非選挙→選挙" if prev[1] not in ELECT else "選挙→選挙"
                if pol not in ELECT: last = None
                if prev is not None and pol in ELECT and prev[1] in ELECT:
                    dq = q - last if last is not None else None
                    cell = (kind, "1") if dq == 1 else (kind, "2-4") if dq is not None and 2 <= dq <= 4 else ("他", "")
                    opp[cell] += 1
                    for j, k in enumerate(LAWS):
                        if v[j] > prev[2][j]: up[(cell, k)] += 1
                prev = (q, pol, v)
    print(f"== {name}")
    base = {k: up[(("他", ""), k)] / max(1, opp[("他", "")]) for k in LAWS}
    for cell in [("非選挙→選挙", "1"), ("非選挙→選挙", "2-4"), ("選挙→選挙", "1"), ("選挙→選挙", "2-4"), ("他", "")]:
        parts = [f"{k} 上げ {up[(cell, k)]}（率÷他 {(up[(cell, k)] / max(1, opp[cell])) / base[k]:.1f}）" if base[k] > 0 else f"{k} 上げ {up[(cell, k)]}" for k in LAWS]
        print(f"   {cell[0]} 距離 {cell[1] or '-'}：四半期 {opp[cell]}・" + "・".join(parts))
