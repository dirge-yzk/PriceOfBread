"""(1085) 与党と議席の食い違い（読むだけ）。sf_s と scalars_s を (seed, quarter, nation) で結ぶ。
使い方: python -X utf8 -I mismatch.py <out>
"""
import sys, os, csv, collections

ARMS = [
    ("m1069", "D:/PoB_work/keep_1078/base_ins", range(1, 11)),
    ("n1", "D:/PoB_work/keep_1069/n1", range(1, 11)),
    ("n2", "D:/PoB_work/keep_1069/n2", range(1, 11)),
    ("s11", "D:/PoB_work/keep_1069/a11", range(11, 21)),
]
ELECTED = {3, 4}
RULING = {5: 1, 6: 2}  # Parliament.cs:416 RulingParty: 5 Bourgeois->Liberal, 6 Proletarian->Socialist, else Conservative
WIN = 16


def read(path, cols):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = [h.index(c) for c in cols]
        for row in r:
            yield [row[i] for i in ix]


def argmax3(a):
    b = 0
    if a[1] > a[b]: b = 1
    if a[2] > a[b]: b = 2
    return b


def main():
    out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
    w = lambda s: out.write(s + "\n")
    w("arm\tclass\trows\tmismatch\tshare\tempty\tnote")
    detail = collections.Counter()
    for arm, d, seeds in ARMS:
        c = collections.Counter()
        for s in seeds:
            pol = {}
            for q, n, pc in read(f"{d}/scalars_s{s}.tsv", ["quarter", "nation", "polityCode"]):
                pol[(int(q), n)] = int(float(pc))
            hist = collections.defaultdict(dict)
            for q, n, pc in [(k[0], k[1], v) for k, v in pol.items()]:
                hist[n][q] = pc
            for q, n, a, b, cc, g in read(f"{d}/sf_s{s}.tsv", ["quarter", "nation", "seatCons", "seatLib", "seatSoc", "governingParty"]):
                q = int(q); seats = [float(a), float(b), float(cc)]; gp = int(float(g))
                pc = pol.get((q, n))
                if pc is None:
                    c[("nopolity", "all")] += 1; continue
                yearend = q % 4 == 3
                quarterly = len(hist[n]) > 600
                el = pc in ELECTED
                empty = sum(seats) <= 1e-9
                mm = (not empty) and gp != argmax3(seats)
                # P2a 予言の比較: 非選挙で governing==RulingParty か
                if not el and gp != RULING.get(pc, 0) and quarterly:
                    prev = hist[n].get(q - 1, pc)
                    c[("nonelected_gp_not_ruling", "lag" if prev != pc else "same")] += 1
                if el and quarterly and mm and not any(hist[n].get(q - k, pc) not in ELECTED for k in range(1, WIN + 1)):
                    prev = hist[n].get(q - 1, pc)
                    c[("normal_mm", "lag" if prev != pc else "same")] += 1
                keys = []
                if yearend:
                    keys.append(("yearend_elected" if el else "yearend_nonelected"))
                if quarterly:
                    if el:
                        trans = any(hist[n].get(q - k, pc) not in ELECTED for k in range(1, WIN + 1))
                        keys.append("q9_elected_trans" if trans else "q9_elected_normal")
                    else:
                        keys.append("q9_nonelected")
                for k in keys:
                    c[(k, "rows")] += 1
                    if empty: c[(k, "empty")] += 1
                    if mm:
                        c[(k, "mm")] += 1
                        if k == "yearend_nonelected":
                            detail[(arm, pc, gp, argmax3(seats))] += 1
        for k in ["yearend_elected", "yearend_nonelected", "q9_elected_normal", "q9_elected_trans", "q9_nonelected"]:
            r = c[(k, "rows")]; m = c[(k, "mm")]; e = c[(k, "empty")]
            w(f"{arm}\t{k}\t{r}\t{m}\t{(m / r if r else 0):.4f}\t{e}\t")
        w(f"{arm}\tnonelected_gp_not_ruling_q9\t-\tlag={c[('nonelected_gp_not_ruling','lag')]} same={c[('nonelected_gp_not_ruling','same')]}\t-\t-\tP2a")
        w(f"{arm}\telected_normal_mm_q9\t-\tlag={c[('normal_mm','lag')]} same={c[('normal_mm','same')]}\t-\t-\tP2b")
        w(f"{arm}\tnopolity\t{c[('nopolity','all')]}\t\t\t\t結べなかった行")
    w("")
    w("# 非選挙政体の年末の食い違いの内訳: arm polity governing seatArgmax count")
    for k, v in sorted(detail.items()):
        w("\t".join(map(str, k)) + f"\t{v}")
    out.close()


main()
