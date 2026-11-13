# (1118) c108ps 紙（旗 0 の生）：G3 の 0→1 以上の上げのうち、同じ国が前に〈距離 1 の移行で上げた〉か〈四半期 0 に施行していた〉続きの後の再上げの数（読むだけ）。
import sys, os, re, collections
ELECT = {3, 4}
for a in sys.argv[1:]:
    name, d = a.split("=", 1)
    C = collections.Counter()
    for s in sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r"monlaw_s(\d+)\.tsv", f)] if m):
        by = collections.defaultdict(dict); kc = collections.Counter()
        with open(os.path.join(d, f"monlaw_s{s}.tsv"), encoding="utf-8") as f:
            h = f.readline().rstrip("\r\n").split("\t"); iq, inn, ip, im = h.index("quarter"), h.index("nation"), h.index("polityCode"), h.index("MinimumWage")
            for line in f:
                p = line.rstrip("\r\n").split("\t"); q = int(p[iq]); kc[(q, p[inn])] += 1; by[p[inn]][q] = (int(p[ip]), int(p[im]))
        for nat, m in by.items():
            prior_gap = False
            for q in sorted(m):
                if kc[(q, nat)] != 1: continue
                pol, mw = m[q]
                if q == 0 and mw >= 1: prior_gap = True
                pr = m.get(q - 1); p2 = m.get(q - 2)
                if pr is None or kc[(q - 1, nat)] != 1: continue
                if pr[1] == 0 and mw >= 1:
                    d1 = pr[0] in ELECT and p2 is not None and p2[0] not in ELECT
                    if d1: prior_gap = True; continue
                    C["G3"] += 1; C["G3_after_gap"] += prior_gap
    print(f"{name}：G3 {C['G3']}・うち同じ国が前に空白（距離 1 か四半期 0）で施行していた後の再上げ {C['G3_after_gap']}・それ以外 {C['G3'] - C['G3_after_gap']}")
