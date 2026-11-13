# (1115) c107op 開幕の空白の対照（読むだけ）：選挙政体の国ごとに、MinimumWage が最初に ≥1 になった四半期と、四半期 0 で上げなかった種での最初の四半期。
# 使い方：python -X utf8 -I op_us.py 名=フォルダ ...
import sys, os, re, csv, collections
sys.stdout.reconfigure(newline=chr(10))
ELECT = {3, 4}
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    first = collections.defaultdict(list); gate0 = collections.Counter(); el0 = set(); polq0 = collections.defaultdict(set)
    for s in seeds(d):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, inn, ip, im, igate = h.index("quarter"), h.index("nation"), h.index("polityCode"), h.index("MinimumWage"), h.index("lawGateOpen:MinimumWage")
            for row in r: rows[row[inn]].append((int(row[iq]), int(row[ip]), float(row[im]), row[igate]))
        for nat, seq in rows.items():
            seq.sort()
            if seq[0][0] != 0 or seq[0][1] not in ELECT: continue
            el0.add(nat); polq0[nat].add(seq[0][1])
            if seq[0][3] not in ("0", "0.0", "False", "false", ""): gate0[nat] += 1
            fq = next((q for (q, pol, mw, gate) in seq if mw >= 1), None)
            # 最初の上げの四半期の政体が選挙のままか
            first[nat].append(fq)
    print(f"== {name}")
    for nat in sorted(el0):
        v = first[nat]
        early = sorted(x for x in v if x is not None)
        print(f"  {nat}（四半期 0 の政体 {sorted(polq0[nat])}・門が開いた種 {gate0[nat]}/{len(v)}）：最初の施行の四半期 " + ",".join("-" if x is None else str(x) for x in v))
