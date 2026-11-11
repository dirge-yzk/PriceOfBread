# (903) c77cw 段0：内戦の月の数（nation_s の civilWarMonths・atWarMonths）と wars_s の内戦の件数・長さの中央を腕ごとに数える（読むだけ）
import sys, os, csv, statistics
def nation(d):
    out = {}
    for s in range(1, 11):
        p = os.path.join(d, f"nation_s{s}.tsv")
        if not os.path.exists(p): continue
        cw = aw = q = cwq = cwnoaw = 0
        with open(p, encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ic = h.index("civilWarMonths"); ia = h.index("atWarMonths")
            for row in r:
                c = int(row[ic]); a = int(row[ia]); cw += c; aw += a; q += 1
                if c > 0: cwq += 1
                if c > a: cwnoaw += c - a
        out[s] = (cw, aw, cwq, cwnoaw, q)
    return out
def wars(d):
    out = {}
    for s in range(1, 11):
        p = os.path.join(d, f"wars_s{s}.tsv")
        if not os.path.exists(p): continue
        L = []
        with open(p, encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ic = h.index("civilWarFlag"); im = h.index("months")
            for row in r:
                if row[ic] == "1": L.append(int(row[im]))
        out[s] = L
    return out
for d in sys.argv[1:]:
    n = nation(d); w = wars(d)
    print(f"== {d}")
    print("seed\tcivilWarMonths\tatWarMonths\tcivilQuarters\tcivilMonthsNotAtWar\tcivilWars\tcivilMedianMonths")
    allL = []; T = [0, 0, 0, 0]
    for s in sorted(n):
        cw, aw, cwq, cna, q = n[s]; L = w.get(s, []); allL += L
        T = [T[0] + cw, T[1] + aw, T[2] + cwq, T[3] + cna]
        print(f"{s}\t{cw}\t{aw}\t{cwq}\t{cna}\t{len(L)}\t{statistics.median(L) if L else 'nan'}")
    print(f"sum\t{T[0]}\t{T[1]}\t{T[2]}\t{T[3]}\t{len(allL)}\t{statistics.median(allL) if allL else 'nan'}")
    s5 = [s for s in sorted(w) if s <= 5]; L5 = [m for s in s5 for m in w[s]]
    print(f"seeds1-5\tcivilWars={len(L5)}\tmedianMonths={statistics.median(L5) if L5 else 'nan'}")
