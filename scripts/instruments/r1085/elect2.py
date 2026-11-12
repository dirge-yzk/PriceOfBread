"""(1085) 選挙で与党の族が替わった回の内訳（移行の後の片づけか、通常の任期の後か）・歪みの大きさ・開院の議席（読むだけ）。
9 国の毎四半期の行。選挙＝選挙政体の 2 四半期続きの行で議席が動いた四半期（Parliament.cs:105）。
歪み：議席 s_i＝v_i^1.3／Σ（Parliament.cs:219〜224・ElectoralDistortion 0.3）を逆に解いて v_i ∝ s_i^(1/1.3)。年末の選挙政体の行（全ての国）。
使い方: python -X utf8 -I elect2.py <out>
"""
import sys, csv, collections

ARMS = [
    ("m1069", "D:/PoB_work/keep_1078/base_ins", range(1, 11)),
    ("n1", "D:/PoB_work/keep_1069/n1", range(1, 11)),
    ("n2", "D:/PoB_work/keep_1069/n2", range(1, 11)),
    ("s11", "D:/PoB_work/keep_1069/a11", range(11, 21)),
]
ELECTED = {3, 4}
E = 1.3


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def am(a):
    b = 0
    if a[1] > a[b]: b = 1
    if a[2] > a[b]: b = 2
    return b


def qt(x, p):
    x = sorted(x); return x[min(len(x) - 1, int(p * len(x)))] if x else float("nan")


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    for arm, d, seeds in ARMS:
        ch = collections.Counter(); prem = []; firstq = collections.Counter()
        for s in seeds:
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            rows = collections.defaultdict(list)
            for q, n, a, b, c, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
                q = int(q); pc = pol.get((q, n), -1); st = (float(a), float(b), float(c))
                rows[n].append((q, pc, st, int(float(g))))
                if q % 4 == 3 and pc in ELECTED and sum(st) > 1e-9:
                    r = [x ** (1 / E) for x in st]; t = sum(r)
                    v = [x / t for x in r]; k = am(st)
                    prem.append(st[k] - v[k])
            for n, rs in rows.items():
                if len(rs) < 600: continue
                rs.sort()
                stale = False; prev = None
                for q, pc, st, gp in rs:
                    el = pc in ELECTED
                    if prev is not None:
                        if el and prev[1] not in ELECTED:
                            stale = gp != am(st) and sum(st) > 1e-9
                        if el and prev[1] in ELECTED and any(abs(st[i] - prev[2][i]) > 1e-12 for i in range(3)):
                            kind = "opening" if not (sum(prev[2]) > 1e-9) else "cleanup" if stale else "normal"
                            ch[(kind, "elections")] += 1
                            if gp != prev[3]: ch[(kind, "changed")] += 1
                            stale = False
                    if el and sum(st) > 1e-9 and (prev is None or sum(prev[2]) <= 1e-9):
                        firstq[q if q < 4 else ">=4"] += 1
                    prev = (q, pc, st, gp)
        w(f"== {arm}")
        w(f"選挙（移行の後の最初＝片づけ）{ch[('cleanup','elections')]}・うち与党の族が替わった {ch[('cleanup','changed')]}")
        w(f"開院（前の四半期の議席が空）{ch[('opening','elections')]}・うち与党が替わった {ch[('opening','changed')]}")
        w(f"選挙（通常）{ch[('normal','elections')]}・うち与党の族が替わった {ch[('normal','changed')]}（{ch[('normal','changed')] / max(1, ch[('normal','elections')]):.4f}）")
        w(f"第一党の議席−逆に解いた得票（歪みの上乗せ）中央 {qt(prem, .5):.3f}・p90 {qt(prem, .9):.3f}")
        w("議席が空から埋まった四半期（9 国）: " + " ".join(f"{k}:{v}" for k, v in sorted(firstq.items(), key=lambda x: str(x[0]))))
    out.close()


main()
