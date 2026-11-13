# (1108) 移行の直後の四半期（議席が空の 1 期）に法の段が動く率を、法ごとに他の選挙政体の四半期と比べる（読むだけ）。
# 併せて最低賃金法：移行の直後（距離 0〜1）に上げた段が続いた施行の行の数。
import sys, os, re, csv, collections
ELECT = {3, 4}
SKIP = {"quarter", "year", "nationUid", "nationIndex", "nation", "polityCode", "blockadeRiskShare"}
def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m)
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    ch1 = collections.Counter(); chElse = collections.Counter(); opp1 = 0; oppElse = 0; up1 = collections.Counter(); upElse = collections.Counter()
    mwRowsFromVac = 0; mwRows = 0
    for s in seeds(d):
        rows = collections.defaultdict(list)
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r)
            iq, inn, ip = h.index("quarter"), h.index("nation"), h.index("polityCode")
            laws = [(k, i) for i, k in enumerate(h) if k not in SKIP and not k.startswith("lawGateOpen") and not k.endswith("Target")]
            for row in r: rows[row[inn]].append((int(row[iq]), int(row[ip]), [row[i] for _, i in laws]))
        im = [k for k, _ in laws].index("MinimumWage")
        for nat, seq in rows.items():
            seq.sort(); lastTrans = None; prev = None; vacSpell = False
            for (q, pol, vals) in seq:
                if prev is not None and pol != prev[1] and prev[1] not in ELECT and pol in ELECT: lastTrans = q
                if pol not in ELECT: lastTrans = None
                st = float(vals[im])
                if prev is not None and pol in ELECT and prev[1] in ELECT:
                    isVac = lastTrans is not None and q - lastTrans == 1
                    if isVac: opp1 += 1
                    else: oppElse += 1
                    for j, (k, _) in enumerate(laws):
                        if vals[j] != prev[2][j]:
                            (ch1 if isVac else chElse)[k] += 1
                            try:
                                if float(vals[j]) > float(prev[2][j]): (up1 if isVac else upElse)[k] += 1
                            except ValueError: pass
                if prev is not None:
                    pst = float(prev[2][im])
                    if st > pst: vacSpell = lastTrans is not None and q - lastTrans <= 1
                    if st == 0: vacSpell = False
                if st > 0:
                    mwRows += 1
                    if vacSpell: mwRowsFromVac += 1
                prev = (q, pol, vals)
    print(f"== {name}：選挙政体が続く四半期の対のうち、移行の直後の 1 期 {opp1}・それ以外 {oppElse}")
    print(f"   最低賃金：施行の行 {mwRows}・うち移行の直後（距離 0〜1）に上げた段が続いた行 {mwRowsFromVac}（{mwRowsFromVac/max(1,mwRows):.3f}）")
    out = []
    for k in set(ch1) | set(chElse):
        r1 = ch1[k] / max(1, opp1); r0 = chElse[k] / max(1, oppElse)
        out.append((r1 / r0 if r0 > 0 else float("inf"), k, ch1[k], chElse[k], up1[k], upElse[k]))
    out.sort(reverse=True)
    print("   法\t直後の率÷他の率\t直後の変化\t他の変化\t直後の上げ\t他の上げ")
    for t in out[:14]: print(f"   {t[1]}\t{t[0]:.1f}\t{t[2]}\t{t[3]}\t{t[4]}\t{t[5]}")
