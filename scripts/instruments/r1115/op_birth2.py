# (1115) c107op 生まれた選挙政体の国：誕生からの距離ごとの MinimumWage の上げの率と、sf_s の席が空の割合（読むだけ）。
# 距離 d＝その国の行の順（最初の行＝0）。上げ＝前の行より大きい。他の選挙政体の四半期の率（op_open.py の (c) と同じ定義の値）は引数 BASE で渡す。
import sys, os, re, csv, collections
sys.stdout.reconfigure(newline=chr(10))
ELECT = {3, 4}
def seeds(d, pre):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(pre + r"_s(\d+)\.tsv", f)] if m)
BINS = [(1, 1), (2, 2), (3, 4), (5, 8), (9, 16), (17, 10000)]
for a in sys.argv[1:]:
    name, rest = a.split("=", 1); d, base = rest.split("@"); base = float(base)
    opp = collections.Counter(); up = collections.Counter(); emp = collections.Counter(); tot = collections.Counter()
    for s in seeds(d, "monlaw"):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, iu, inn, ip, im = h.index("quarter"), h.index("nationUid"), h.index("nation"), h.index("polityCode"), h.index("MinimumWage")
            for row in r: rows[row[iu]].append((int(row[iq]), int(row[ip]), float(row[im]), row[inn]))
        names = {}
        for uid, seq in rows.items():
            seq.sort()
            if seq[0][0] == 0 or seq[0][1] not in ELECT: continue
            names[seq[0][3]] = seq[0][0]
            for k in range(1, len(seq)):
                if seq[k][1] not in ELECT: break
                b = next(x for x in BINS if x[0] <= k <= x[1])
                opp[b] += 1
                if seq[k][2] > seq[k - 1][2]: up[b] += 1
        sp = os.path.join(d, f"sf_s{s}.tsv")
        if os.path.exists(sp) and names:
            with open(sp, encoding="utf-8", newline="") as f:
                r = csv.reader(f, delimiter="\t"); h = next(r)
                iq, inn, a0, a1, a2 = h.index("quarter"), h.index("nation"), h.index("seatCons"), h.index("seatLib"), h.index("seatSoc")
                for row in r:
                    n = row[inn]
                    if n in names:
                        k = int(row[iq]) - names[n]
                        if k < 0: continue
                        b = (0, 0) if k == 0 else next(x for x in BINS if x[0] <= k <= x[1])
                        tot[b] += 1
                        if float(row[a0]) + float(row[a1]) + float(row[a2]) < 1e-9: emp[b] += 1
    print(f"== {name}（他の選挙政体の四半期の上げの率 {base}）")
    for b in BINS:
        rr = up[b] / max(1, opp[b])
        print(f"   距離 {b[0]}〜{b[1] if b[1] < 10000 else '末'}：四半期 {opp[b]}・上げ {up[b]}（率÷他 {rr / base:.1f}）・sf_s の席が空 {emp[b]}/{tot[b]}")
    print(f"   距離 0 の sf_s の席が空 {emp[(0, 0)]}/{tot[(0, 0)]}")
