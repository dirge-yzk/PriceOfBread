# (1066) 全 61 国の人口 0 の POP の数の下界（読むだけ）。2026-11-12 改め（反証の指摘：旧版は兵士の階級 5 を 0 と数え、植民地格の州だけの cpop で比を掛けた＝下界が真値を超えた）。
# 下界＝国の階級の人口が 0（pop_s scope C に popK>0 の行が無い）×その国の州の数（region_s）。階級は pop_s scope C に出る 12 階級だけ（兵士＝classId 5 は scope C に出ない＝数えない＝不明）。
# 国の和が 0 なら全ての州で 0（人数は非負）＝下界として成り立つ。上界は出せない（州×階級の人数の表が全ての州には無い）。
# 照合：cpop_s（植民地格の州だけ＝(836) の母集合）で、同じ 12 階級の真の 0 の数と、同じ州の集合の下界を並べる（下界≦真値を確かめる）。
import sys, statistics, collections
D = sys.argv[1]
YQ = {1784: 3, 1800: 67, 1850: 267, 1900: 467, 1950: 667}
out = collections.defaultdict(list); tavg_list = []
for s in range(1, 11):
    regs = collections.defaultdict(set)
    with open(f"{D}/region_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); iq, inat, ireg = h.index("quarter"), h.index("nation"), h.index("region")
        qs = set(YQ.values())
        for line in f:
            c = line.split("\t", 6)
            q = int(c[iq])
            if q in qs: regs[(q, c[inat])].add(c[ireg])
    clsPos = collections.defaultdict(set); known = set(); present = set()   # present＝scope C に行がある（国, 四半期）。行の無い国は不明＝下界に数えない（2026-11-12 反証の 2 回目）
    with open(f"{D}/pop_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); isc, iq, inat, icl, ik = (h.index(k) for k in ("scope", "quarter", "nation", "classId", "popK"))
        for line in f:
            c = line.split("\t", 10)
            if c[isc] != "C": continue
            known.add(c[icl])
            q = int(c[iq])
            if q in YQ.values(): present.add((q, c[inat]))
            if q in YQ.values() and float(c[ik]) > 0: clsPos[(q, c[inat])].add(c[icl])
    K = len(known)
    col_true = collections.Counter(); col_regs = collections.defaultdict(set); col_pops = collections.Counter()
    with open(f"{D}/cpop_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); iq, inat, ireg, icl, ik = (h.index(k) for k in ("quarter", "nation", "region", "classId", "popK"))
        for line in f:
            c = line.split("\t", 10)
            q = int(c[iq])
            if q not in YQ.values() or c[icl] not in known: continue
            col_regs[q].add((c[inat], c[ireg])); col_pops[q] += 1
            if float(c[ik]) == 0: col_true[q] += 1
    shs = {}
    for y, q in YQ.items():
        lb = 0; nreg = 0; miss_n = 0; miss_r = 0
        for (qq, n), rs in regs.items():
            if qq != q: continue
            nreg += len(rs)
            if (q, n) not in present: miss_n += 1; miss_r += len(rs); continue
            lb += (K - len(clsPos.get((q, n), ()))) * len(rs)
        lb_col = sum(K - len(clsPos.get((q, n), ())) for (n, r) in col_regs[q] if (q, n) in present)
        SL = 13   # 台帳の件数は州×13 枠（兵士を含む）＝割合の分母も 13 枠（兵士は 0 と数えない＝下界のまま）
        out[(y, "K")].append(K); out[(y, "regions")].append(nreg); out[(y, "slots")].append(nreg * SL); out[(y, "lb")].append(lb)
        out[(y, "missNations")].append(miss_n); out[(y, "missRegions")].append(miss_r)
        shs[y] = lb / (nreg * SL)
        out[(y, "col_slots")].append(col_pops[q]); out[(y, "col_lb")].append(lb_col); out[(y, "col_true")].append(col_true[q])
        out[(y, "col_lb_le_true")].append(1 if lb_col <= col_true[q] else 0)
        if lb_col > col_true[q]: print(f"# 下界が真値を超えた：種 {s}・{y} 年・下界 {lb_col}・真値 {col_true[q]}")
        if miss_n: print(f"# scope C に行の無い国：種 {s}・{y} 年・{miss_n} 国・{miss_r} 州（下界に数えない）")
    ys = sorted(shs); ta = sum((shs[a] + shs[b]) / 2 * (b - a) for a, b in zip(ys, ys[1:])) + shs[ys[0]] * 5
    tavg_list.append(ta / (ys[-1] - ys[0] + 5))
keys = ["K", "regions", "slots", "lb", "missNations", "missRegions", "col_slots", "col_lb", "col_true", "col_lb_le_true"]
print("year\t" + "\t".join(keys) + "\tlb_share\tcol_true_share（10種の中央値・col_lb_le_true は 10 種の和）")
for y in YQ:
    m = {k: statistics.median(out[(y, k)]) for k in keys}; m["col_lb_le_true"] = sum(out[(y, "col_lb_le_true")])
    sh = [a / b for a, b in zip(out[(y, "lb")], out[(y, "slots")])]
    print(str(y) + "\t" + "\t".join(f"{m[k]:.0f}" for k in keys) + f"\t{m['lb']/m['slots']:.3f}\t{m['col_true']/max(1,m['col_slots']):.3f}\t# 種ごとの lb_share 最小 {min(sh):.3f}・最大 {max(sh):.3f}")
print("# 種ごとの下界の割合の時間平均（13 枠の分母）：最小 %.4f・中央 %.4f・最大 %.4f" % (min(tavg_list), statistics.median(tavg_list), max(tavg_list)))
