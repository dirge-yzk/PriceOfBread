# (1066) prodcap_s の年末の全生産者を、中身の無い物（capacity==0 かつ output==0）の種類で数える。読むだけ。
# 種類: svc＝役務の財（SleepState が眠らせない）・site＝siteRow>=0（眠らせない）・other＝それ以外（眠りか、倒産・工事・目覚めの月＝prodcap の列では分けられない）
import sys, statistics, collections
SVC = set("労働力 教育 医療 行政 兵力 高度労働力 農業労働力 上下水道 電化インフラ 家事奉仕 高等教育".split())
D = sys.argv[1]
YEARS = [1784, 1800, 1850, 1900, 1950]
res = collections.defaultdict(list)  # (year,key) -> list over seeds
for s in range(1, 11):
    cnt = collections.Counter()
    with open(f"{D}/prodcap_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t")
        iy, ig, ic, io, isr, isf, irgo = (h.index(k) for k in ("year", "good", "capacity", "output", "siteRow", "siteFound", "isRgo"))
        for line in f:
            c = line.rstrip("\n").split("\t")
            y = int(c[iy])
            if y not in YEARS: continue
            cnt[(y, "all")] += 1
            cap = float(c[ic]); out = float(c[io])
            svc = c[ig] in SVC; site = int(c[isr]) >= 0
            if cap == 0 and out == 0:
                k = "svc" if svc else ("site" if site else "other")
                cnt[(y, "e_" + k)] += 1
                if site: cnt[(y, "e_site_found" if c[isf] == "1" else "e_site_unfound")] += 1
                cnt[(y, "e_rgo" if c[irgo] == "1" else "e_fac")] += 1
            elif out == 0:
                cnt[(y, "out0_capPos")] += 1
            elif cap == 0:
                cnt[(y, "cap0_outPos")] += 1
            if svc: cnt[(y, "svc_all")] += 1
            if site: cnt[(y, "site_all")] += 1
    for k, v in cnt.items(): res[k].append(v)
keys = ["all", "e_svc", "e_site", "e_site_found", "e_site_unfound", "e_other", "e_rgo", "e_fac", "out0_capPos", "cap0_outPos", "svc_all", "site_all"]
print("year\t" + "\t".join(keys) + "\t(10種の中央値)")
for y in YEARS:
    print(str(y) + "\t" + "\t".join(str(statistics.median(res[(y, k)] + [0] * (10 - len(res[(y, k)])))) for k in keys))
