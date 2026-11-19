# (1238) 読むだけ：プローブの T（段ごとの出口）を種×区間（助走 1-10・11-20・月 1-12）で足し、どの段で止まったかを並べる。U（ジャマイカの容量を使った財）の上位も。引数：出力のフォルダ
import sys, csv, collections
d = sys.argv[1]
def rows(p): return [r for r in csv.reader(open(p, encoding="utf-8"), delimiter="\t") if r and not r[0].startswith("#")]
for F in (0, 1):
    T = rows(f"{d}/f{F}_T.tsv"); h = T[0]; T = T[1:]
    print(f"== 旗 SellOfferExcessStock={F}")
    for s in sorted({r[0] for r in T}):
        for lab, sel in (("助走1-10", lambda r: r[1]=="warm" and int(r[2])<=10), ("助走11-20", lambda r: r[1]=="warm" and int(r[2])>10), ("月1-12", lambda r: r[1]=="m" and int(r[2])>=1)):
            acc = collections.Counter(); last = None
            for r in T:
                if r[0]==s and sel(r):
                    for i in range(3, 31): acc[h[i]] += int(r[i])
                    last = r
            keys = ["clears","inSellers","availLe0","stockZero","capLeftZeroAtSellers","rowsJamInList","rowsJamLbBelowLp","rowsJamCapLeft0","rowsJamLeft0","candJam","iterJamBest","iterX0","iterY0","iterBudget0","iterCap0","iterOk","deals"]
            print(f"s{s} {lab}\t" + " ".join(f"{k}={acc[k]}" for k in keys) + (f"\t末 jamCap={float(last[h.index('jamCap')]):.3g} pot={float(last[h.index('jamPotential')]):.3g} thr={float(last[h.index('jamThrottle')]):.3g}" if last else ""))
    U = rows(f"{d}/f{F}_U.tsv")[1:]
    for s in sorted({r[0] for r in U}):
        g = collections.Counter()
        for r in U:
            if r[0]==s and r[1]=="m": g[r[4]] += float(r[5]) + float(r[6])
        print(f"  s{s} 月1-12 のジャマイカの容量を使った財（売り＋買い×Wg）上位: " + ", ".join(f"{k} {v:.3g}" for k, v in g.most_common(6)))
