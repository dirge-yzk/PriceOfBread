# (1066) prodcap_s の能力 0・産出 0 の生産者を、眠り（SleepState==1）に近い物と、回されるのに中身の無い物に分ける近似（読むだけ）。
# 近似の述語: 同じ年・同じ国・同じ財・同じレシピに能力＞0 の生産者が 1 本でもあれば〈門が開いている〉と見なす＝その能力 0 の生産者は眠らない（回される）。
#   1 本も無ければ〈門が閉じている〉と見なす＝眠りの候補（倒産・工事・国の設備は prodcap に列が無い＝眠りの側に寄る＝回される中身の無い物の下界）。
# 役務の財・siteRow>=0 は SleepState が眠らせない＝回される側（ソース EconomyStep.cs:787-789）。
# 併せて region_s の倒産の数（工場＋産地・全 61 国）を同じ四半期で数える。
import sys, statistics, collections
SVC = set("労働力 教育 医療 行政 兵力 高度労働力 農業労働力 上下水道 電化インフラ 家事奉仕 高等教育".split())
D = sys.argv[1]
YQ = {1784: 3, 1800: 67, 1850: 267, 1900: 467, 1950: 667}
res = collections.defaultdict(list)
for s in range(1, 11):
    rows = collections.defaultdict(list)  # (year,nation,good,recipe) -> list of (cap,out,svc,site)
    with open(f"{D}/prodcap_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        iy, inat, ig, irc, ic, io, isr = (h.index(k) for k in ("year", "ownerNation", "good", "recipe", "capacity", "output", "siteRow"))
        for line in f:
            c = line.rstrip("\n").split("\t")
            y = int(c[iy])
            if y not in YQ: continue
            rows[(y, c[inat], c[ig], c[irc])].append((float(c[ic]), float(c[io]), c[ig] in SVC, int(c[isr]) >= 0))
    cnt = collections.Counter()
    for (y, n, g, rc), lst in rows.items():
        open_ = any(cap > 0 for cap, _, _, _ in lst)
        for cap, out, svc, site in lst:
            cnt[(y, "all")] += 1
            if cap == 0 and out == 0:
                if svc or site: cnt[(y, "run_empty_svc_site")] += 1
                elif open_: cnt[(y, "run_empty_gateopen")] += 1
                else: cnt[(y, "asleep_like")] += 1
            elif out == 0: cnt[(y, "run_cap_pos_out0")] += 1
            else: cnt[(y, "run_active")] += 1
    bk = collections.Counter()
    with open(f"{D}/region_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        iq, ifb, ieb = h.index("quarter"), h.index("factoryBankruptCount"), h.index("extractionBankruptCount")
        qy = {q: y for y, q in YQ.items()}
        for line in f:
            c = line.rstrip("\n").split("\t")
            q = int(c[iq])
            if q in qy: bk[qy[q]] += float(c[ifb]) + float(c[ieb])
    for y in YQ: cnt[(y, "bankrupt_region_s")] = bk[y]
    for k, v in cnt.items(): res[k].append(v)
keys = ["all", "asleep_like", "run_empty_gateopen", "run_empty_svc_site", "run_cap_pos_out0", "run_active", "bankrupt_region_s"]
print("year\t" + "\t".join(keys) + "\trun\trun_empty/run（10種の中央値）")
for y in YQ:
    m = {k: statistics.median(res[(y, k)] + [0] * (10 - len(res[(y, k)]))) for k in keys}
    run = m["all"] - m["asleep_like"]
    print(str(y) + "\t" + "\t".join(f"{m[k]:.0f}" for k in keys) + f"\t{run:.0f}\t{(m['run_empty_gateopen'] + m['run_empty_svc_site']) / run:.3f}")
