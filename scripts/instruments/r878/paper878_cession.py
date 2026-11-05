# (878) c73ds 読むだけの紙の台本：割譲の事象ごとの支配の 4 区分と、形 C1（国家の分を在地へ移す）を m873 の盤面（州ごとの治安 P1-1＝RuleOrderByRegion 1）で当てた見込み
# 元：scripts/instruments/r844/paper844_cession.py（main 70b27755・(844) の台本）を写して書き足した（元は書き換えていない）。シミュは回さない。
# 使い方：python -X utf8 -I paper878_cession.py <盤面の名> <計器の生のフォルダ（cess_s・rule_s・secession_s・cpeace_s）> <出力.txt> [種の起点=1] [エンジンの規則 new|old＝RuleOrderByRegion 1|0]
# ⚠限界（台本が自分で名乗る）
#   ①rule_s は計器の 9 国だけ毎四半期・他の国は年末の行＝割譲の後の最初の行（受け手）は、受け手が計器の国なら割譲の四半期そのもの、そうでなければ最大 3 四半期の後。
#   ②四半期の中の順（ソース・MultiNationSim.cs:783 の StepDayClose・:811〜822 の共有層）：各国の四半期末（Politics.Step→CheckSecession→Rule のループ）の後に共有層の Diplomacy.Step×3（講和→CedeRegion）、
#     その後に SelfPlay.cs:2183 の Instruments.Quarter＝受け手が計器の国なら、割譲の四半期の rule_s の行は〈割譲の直後（Rehome と CededColonyMilitancy の後）・受け手の Rule のループの前〉の値。
#   ③目標の列（ungovTarget*）は四半期の末の m_r・m̄・R で数えた計器の値＝エンジンの CheckSecession の瞬間の値とは違う（InstrumentsRule.cs:24・(846)）＝全て近似。
#     瞬間の値は secession_s の P1 の列（CheckSecession の中・Rule のループの直前）を、割譲の後の最初の行で並べる（首都の行は無い）。
#   ④外国の目標 tForeign＝ForeignShareSum は列に無い＝ruleForeignShare（今の R2）で代える（近似）。外国持分の主体名は列に無い＝〈受け手自身の持分〉は数えられない（上界）。
#   ⑤C1 の経路は、Rule の書き手が CheckSecession のループ 1 本だけ（Politics.cs:1586〜1587・grep）で式が線形（寄せ・和は 1 のまま・0 の切りは凸結合で起きない）
#     ＝同じ目標の列に寄る 2 本の経路の差は 0.975^n で縮む、を使って今の経路から作る：R0_C1(n)＝R0(n)−R0a×0.975^n・R1_C1(n)＝R1(n)＋R0a×0.975^n（R0a＝割譲の直後の R0）。
#     C1 が無統治の目標そのものを変えない（目標は Rule を読まない）ことが前提＝間接の道（届き→国庫→満足→闘争度）は入らない。
#     例外：tState＝max(0, 1−tL−tF−tU) は tL＋tF＋tU＞1 のとき 0 に切られ Politics.cs:1587 の割り直しが効く＝外国持分のある州（tF＞0）では近似（件数は [9]）。
#   ⑥cess_s の行は講和で移った州 1 件（経路 ① は植民地の名＝州でない物は rule_s に無い＝〈前後の行が揃わない〉に入る）。
import sys, os, collections, math

SPEED = 0.025          # Coefficients.cs:2751 DominanceSpeed
KEEP = 1.0 - SPEED
D_DELIV = 0.7          # 1−PrivilegeTaxDiscount（Coefficients.cs:1853 の 0.3）
HIST = ("台北", "ヴェネツィア")   # 史実の型（台湾 1895・ヴェネト 1866）の州名＝名前が一致する州を並べるだけ（反実仮想の世界＝相手も年も違う）

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs); return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def qt(xs, p):
    xs = sorted(x for x in xs if x == x)
    return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else float("nan")

def mx(xs):
    xs = [x for x in xs if x == x]
    return max(xs) if xs else float("nan")

def ld(path):
    with open(path, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        rows = [line.rstrip("\n").split("\t") for line in f]
    return ix, rows

def targets(row, ix, which):
    # 目標の 4 区分（Politics.cs:1580〜1584 の式を計器の列で書き直す・近似④）
    tU = fnum(row[ix["ungovTargetNewShare" if which == "new" else "ungovTargetOldShare"]])
    init = fnum(row[ix["ruleInitLocalShare"]]); inter = fnum(row[ix["intermediaryDegreeIndex"]])
    tL = min(1.0 - tU, init * inter) if tU == tU and init == init and inter == inter else float("nan")
    tF = fnum(row[ix["ruleForeignShare"]])
    tS = max(0.0, 1.0 - tL - tF - tU) if tL == tL else float("nan")
    return tS, tL, tF, tU

def first_n(seq, cond):
    for n, v in seq:
        if cond(n, v): return n
    return None

def main(name, base, opath, s0=1, eng="new"):
    out = []; P = out.append
    D = collections.defaultdict(list); C = collections.Counter(); per_seed = []; hist = []
    for seed in range(s0, s0 + 10):
        rix, rrows = ld(os.path.join(base, "rule_s%d.tsv" % seed))
        reg = collections.defaultdict(list); natpop = collections.defaultdict(float); quarterly = set(); natq = collections.defaultdict(set)
        for c in rrows:
            qq = int(c[rix["quarter"]]); nat = c[rix["nation"]]
            reg[c[rix["region"]]].append((qq, int(c[rix["year"]]), nat, c))
            pop = fnum(c[rix["popK"]]); natpop[(qq, nat)] += pop if pop == pop else 0
            natq[nat].add(qq)
        for nat, qs in natq.items():
            if len(qs) > 600: quarterly.add(nat)
        for v in reg.values(): v.sort(key=lambda t: t[0])
        six, srows = ld(os.path.join(base, "secession_s%d.tsv" % seed))
        go = collections.defaultdict(list); sec_at = {}
        for c in srows:
            rg = c[six["region"]]
            if c[six["goFlag"]] == "1": go[rg].append(int(c[six["year"]]))
            sec_at[(rg, int(c[six["quarter"]]), c[six["nation"]])] = c
        creal = 0
        cix, crows = ld(os.path.join(base, "cpeace_s%d.tsv" % seed))
        for c in crows: creal += int(fnum(c[cix["cedeRealCount"]]))
        eix, erows = ld(os.path.join(base, "cess_s%d.tsv" % seed))
        ncess = len(erows); goall = sum(len(v) for v in go.values())
        for c in erows:
            y = int(c[eix["year"]]); win = c[eix["winner"]]; los = c[eix["loser"]]; rg = c[eix["region"]]; tier = c[eix["tier"]]
            rows = reg.get(rg, [])
            before = [r for r in rows if r[2] == los and r[1] <= y]
            after = [r for r in rows if r[2] == win and r[1] >= y and (not before or r[0] > before[-1][0])]
            if not before or not after:
                C["[0] 前後の行が揃わない（経路 ① の植民地の名・計器の国でない・年末の前に再び動いた）"] += 1; continue
            b = before[-1][3]; a = after[0][3]; qa = after[0][0]; qb = before[-1][0]
            exact = win in quarterly
            C["[1] 前後の行が揃った"] += 1
            if exact: C["[1a] うち受け手が計器の国（割譲の四半期の行＝割譲の直後・受け手の寄せの前）"] += 1
            if tier == "Colonial": C["[1b] うち植民地格"] += 1
            else: C["[1c] うち本国の州（編入）"] += 1
            Rb = [fnum(b[rix[k]]) for k in ("ruleStateShare", "ruleLocalShare", "ruleForeignShare", "ruleUngovShare")]
            Ra = [fnum(a[rix[k]]) for k in ("ruleStateShare", "ruleLocalShare", "ruleForeignShare", "ruleUngovShare")]
            D["gapQ"].append(qa - qb)
            for k, nm in enumerate(("R0", "R1", "R2", "R3")):
                D[nm + "_b"].append(Rb[k]); D[nm + "_a"].append(Ra[k])
                if exact: D[nm + "_a_exact"].append(Ra[k])
            D["dR0"].append(Ra[0] - Rb[0])
            # ②の順の確かめ：送り手も受け手も計器の国で隣の四半期なら、a＝b から送り手の目標へ 1 歩（受け手の寄せはまだ）の見込み
            if exact and los in quarterly and qa - qb == 1:
                tSb = targets(b, rix, "new")[0]
                pred1 = Rb[0] + (tSb - Rb[0]) * SPEED
                tSa = targets(a, rix, "new")[0]
                pred2 = pred1 + (tSa - pred1) * SPEED
                D["res_one_step"].append(abs(Ra[0] - pred1)); D["res_two_step"].append(abs(Ra[0] - pred2)); D["res_none"].append(abs(Ra[0] - Rb[0]))
                C["[2] 順の確かめの母集合（送り手・受け手とも計器の国・隣の四半期）"] += 1
            R0a, R1a = Ra[0], Ra[1]
            # 休眠の述語 Military.cs:273（Rule[1] > Rule[0]）
            if R1a > R0a: C["[3a] 割譲の直後に在地＞国家（今でも述語が真）"] += 1
            if R0a + R1a > 0: C["[3b] C1 で述語が真になる（R0＋R1＞0）"] += 1
            if R0a > 0: C["[3c] 参考：R0＞0（(844) の数え方）"] += 1
            if Ra[2] > 0: C["[9] 割譲の直後に R2（外国）＞0＝限界⑤の分解が近似になりうる州"] += 1
            # 目標（P1-1 の新・旧の規則を同じ盤面の同じ行で）
            tSn, tLn, tFn, tUn = targets(a, rix, "new"); tSo, tLo, tFo, tUo = targets(a, rix, "old")
            D["tS_new"].append(tSn); D["tS_old"].append(tSo); D["tU_new"].append(tUn); D["tU_old"].append(tUo); D["tL_new"].append(tLn)
            D["tS_new_minus_old"].append(tSn - tSo)
            D["tU_new_minus_old"].append(tUn - tUo)
            tSbn = targets(b, rix, "new")[0]
            D["tS_jump"].append(tSn - tSbn)   # 送り手の下の最後の行の目標→受け手の最初の行の目標
            D["tU_jump"].append(tUn - targets(b, rix, "new")[3])
            mr_a = fnum(a[rix["militancyRegionIndex"]]); mbar_a = fnum(a[rix["militancyNationIndex"]]); mr_b = fnum(b[rix["militancyRegionIndex"]])
            D["mr_a"].append(mr_a); D["mr_b"].append(mr_b); D["mr_ratio_a"].append(mr_a / mbar_a if mbar_a and mbar_a == mbar_a and mbar_a > 0 else float("nan"))
            D["orderR_a"].append(fnum(a[rix["orderRegionIndex"]])); D["orderOld_a"].append(fnum(a[rix["publicOrderIndex"]]))
            D["R0a_minus_tSn"].append(R0a - tSn)
            if tSn == tSn and tSn < R0a: C["[4a] 受け手の新しい目標の国家区分＜割譲の直後の R0（今の経路でも R0 は下がる側）"] += 1
            if tSo == tSo and tSo < R0a: C["[4b] 同じ行を旧の規則（国の秩序）で＝目標＜R0"] += 1
            # 割譲の後の最初の CheckSecession の瞬間（secession_s・受け手）
            nxt = [r for r in rows if r[2] == win and r[0] > qa]
            sq = None
            for r in rows:
                if r[2] == win and r[0] > qa:
                    if (rg, r[0], win) in sec_at: sq = sec_at[(rg, r[0], win)]; break
            if sq is not None:
                tUi = fnum(sq[six["ungovTargetNewShare"]]); tUio = fnum(sq[six["ungovTargetOldShare"]])
                D["tU_inst_new"].append(tUi); D["tU_inst_old"].append(tUio); D["tU_inst_new_minus_old"].append(tUi - tUio)
                D["orderR_inst"].append(fnum(sq[six["orderRegionNowIndex"]]))
                C["[5] 割譲の後の最初の CheckSecession の行がある（secession_s）"] += 1
            # 経路（同じ受け手の下にいる間・最大 160 四半期）
            path = []
            for r in rows:
                if r[0] < qa: continue
                if r[2] != win: break
                n = r[0] - qa
                if n > 160: break
                c2 = r[3]
                R0 = fnum(c2[rix["ruleStateShare"]]); R1 = fnum(c2[rix["ruleLocalShare"]])
                tS = targets(c2, rix, eng)[0]
                g = R0a * KEEP ** n
                path.append((n, (R0, R1, R0 - g, R1 + g, tS)))
            last_n = path[-1][0] if path else 0
            D["followQ"].append(last_n)
            def stat(key, cond):
                n = first_n(path, cond)
                if n is None: C["[6] %s：打切り（受け手の下にいる間に届かない）" % key] += 1; D[key + "_cens"].append(last_n)
                else: D[key].append(n)
            stat("nHalf_C1", lambda n, v: v[4] == v[4] and v[2] >= 0.5 * v[4])
            stat("n90_C1", lambda n, v: v[4] == v[4] and v[2] >= 0.9 * v[4])
            stat("nPredOff_C1", lambda n, v: not (v[3] > v[2]))
            if R1a > R0a: stat("nPredOff_now", lambda n, v: not (v[1] > v[0]))
            for nn in (4, 12, 28, 40):
                hit = [v for n, v in path if nn <= n <= nn + 3]
                if hit:
                    D["R0_now_%d" % nn].append(hit[0][0] - R0a); D["R0_C1_%d" % nn].append(hit[0][2])
                    D["tS_%d" % nn].append(hit[0][4])
            # 受け手の国への影響（AI の算術・上界）
            tot = natpop.get((qa, win), float("nan")); pa = fnum(a[rix["popK"]])
            ps = pa / tot if tot and tot > 0 else float("nan")
            D["popShare"].append(ps)
            D["taxDropUpper"].append(ps * R0a * (1 - D_DELIV) if ps == ps else float("nan"))
            D["reachDrop"].append(ps * R0a if ps == ps else float("nan"))
            # 外国持分の穴の候補（上界）
            if int(fnum(b[rix["foreignRulerCount"]]) or 0) > 0:
                C["[7] 割譲の前に外国持分の主体が 1 つ以上（穴の候補の上界）"] += 1
                if a[rix["treatyPortInForceFlag"]] == "1": C["[7a] うち受け手の条約港の旗が割譲の後に 1"] += 1
            gs = [gy for gy in go.get(rg, []) if y <= gy <= y + 10]
            if gs: C["[8] 割譲の後 10 年以内にその州で独立の戦争の開始（goFlag）"] += 1
            if rg in HIST:
                hist.append("  種 %d・%d 年・%s→%s・%s・%s：R0 %.3f→C1 0・R1 %.3f→%.3f・新しい目標の国家区分 %.3f（旧の規則 %.3f）・C1 で目標の半分まで %s 四半期・9 割まで %s 四半期" % (
                    seed, y, los, win, rg, tier, R0a, R1a, R0a + R1a, tSn, tSo,
                    first_n(path, lambda n, v: v[4] == v[4] and v[2] >= 0.5 * v[4]), first_n(path, lambda n, v: v[4] == v[4] and v[2] >= 0.9 * v[4])))
        per_seed.append((seed, ncess, creal, goall))
    P("(878) 割譲の事象ごとの支配の 4 区分と形 C1 の見込み・盤面 %s（%s・台本 paper878_cession.py）" % (name, base))
    P("種ごと：種・cess_s の行（割譲の州）・cpeace_s の cedeRealCount の和・secession_s の goFlag の総数")
    for s in per_seed: P("  %d\t%d\t%d\t%d" % s)
    P("割譲の行の合計 %d・cedeRealCount の和 %d・goFlag の総数 %d" % (sum(s[1] for s in per_seed), sum(s[2] for s in per_seed), sum(s[3] for s in per_seed)))
    for k, v in sorted(C.items()): P("  %s：%d" % (k, v))
    P("")
    P("[A] 割譲の前後の 4 区分（前＝送り手の下の最後の行・後＝受け手の下の最初の行）")
    P("  行の離れ（四半期）：中央 %.0f・p90 %.0f" % (med(D["gapQ"]), qt(D["gapQ"], .9)))
    for nm in ("R0", "R1", "R2", "R3"):
        P("  %s 前 中央 %.3f（p10 %.3f・p90 %.3f）・後 中央 %.3f・後（受け手が計器の国だけ）中央 %.3f" % (nm, med(D[nm + "_b"]), qt(D[nm + "_b"], .1), qt(D[nm + "_b"], .9), med(D[nm + "_a"]), med(D[nm + "_a_exact"])))
    P("  国家区分の差（後−前）：中央 %.4f・p10 %.4f・p90 %.4f" % (med(D["dR0"]), qt(D["dR0"], .1), qt(D["dR0"], .9)))
    P("  順の確かめ（%d 件）：|後−前| 中央 %.2e／|後−〈前から送り手の目標へ 1 歩〉| 中央 %.2e／|後−〈さらに受け手の目標へ 1 歩〉| 中央 %.2e" % (
        len(D["res_one_step"]), med(D["res_none"]), med(D["res_one_step"]), med(D["res_two_step"])))
    P("")
    P("[B] 割譲の直後の目標（四半期の末の計器の列・近似③④）＝P1-1 の新の規則と旧の規則（国の秩序）を同じ行で")
    P("  無統治の目標：新 中央 %.3f（p90 %.3f・最大 %.3f）・旧 中央 %.3f（p90 %.3f）・新−旧 中央 %.4f（p10 %.4f・p90 %.4f）" % (
        med(D["tU_new"]), qt(D["tU_new"], .9), mx(D["tU_new"]), med(D["tU_old"]), qt(D["tU_old"], .9), med(D["tU_new_minus_old"]), qt(D["tU_new_minus_old"], .1), qt(D["tU_new_minus_old"], .9)))
    P("  国家区分の目標：新 中央 %.3f（p10 %.3f・p90 %.3f）・旧 中央 %.3f・新−旧 中央 %.4f（p10 %.4f）・在地の目標（新）中央 %.3f" % (
        med(D["tS_new"]), qt(D["tS_new"], .1), qt(D["tS_new"], .9), med(D["tS_old"]), med(D["tS_new_minus_old"]), qt(D["tS_new_minus_old"], .1), med(D["tL_new"])))
    P("  目標の跳び（送り手の下の最後の行→受け手の最初の行・新の規則）：国家区分 中央 %.4f（p10 %.4f）・無統治 中央 %.4f（p90 %.4f）" % (
        med(D["tS_jump"]), qt(D["tS_jump"], .1), med(D["tU_jump"]), qt(D["tU_jump"], .9)))
    P("  州の闘争度 m_r：前 中央 %.3f・後 中央 %.3f（CededColonyMilitancy +0.3 は Diplomacy.cs:3390・Rehome の後）・後の m_r÷m̄ 中央 %.2f（p90 %.2f）・州の秩序 order_r 後 中央 %.3f（p10 %.3f）・国の秩序 中央 %.3f" % (
        med(D["mr_b"]), med(D["mr_a"]), med(D["mr_ratio_a"]), qt(D["mr_ratio_a"], .9), med(D["orderR_a"]), qt(D["orderR_a"], .1), med(D["orderOld_a"])))
    P("  割譲の直後の R0−新しい目標の国家区分：中央 %.3f（p10 %.3f・p90 %.3f）" % (med(D["R0a_minus_tSn"]), qt(D["R0a_minus_tSn"], .1), qt(D["R0a_minus_tSn"], .9)))
    P("  瞬間（割譲の後の最初の CheckSecession・secession_s・%d 件）：無統治の目標 新 中央 %.3f（p90 %.3f）・旧 中央 %.3f・新−旧 中央 %.4f（p90 %.4f）・order_r 中央 %.3f（p10 %.3f）" % (
        len(D["tU_inst_new"]), med(D["tU_inst_new"]), qt(D["tU_inst_new"], .9), med(D["tU_inst_old"]), med(D["tU_inst_new_minus_old"]), qt(D["tU_inst_new_minus_old"], .9), med(D["orderR_inst"]), qt(D["orderR_inst"], .1)))
    P("")
    P("[C] 戻り（今の DominanceSpeed 0.025 のまま・目標は各行の〈エンジンが使う規則＝%s〉の目標・C1 の経路は限界⑤の式）" % eng)
    P("  解析：目標が一定なら C1 の差は 0.975^n で縮む＝半減 %.1f 四半期（%.1f 年）＝目標の値に依らない" % (math.log(2) / -math.log(KEEP), math.log(2) / -math.log(KEEP) / 4))
    for key, lab in (("nHalf_C1", "C1 の R0 が目標の国家区分の半分に届く"), ("n90_C1", "C1 の R0 が目標の 9 割に届く"), ("nPredOff_C1", "C1 で述語（在地＞国家）が偽に戻る"), ("nPredOff_now", "今（C1 なし）で述語が偽に戻る（割譲の直後に真だった州だけ）")):
        P("  %s：%d 件・中央 %s・p10 %s・p90 %s 四半期／打切り %d 件（受け手の下にいた四半期の中央 %s）" % (
            lab, len(D[key]), f(med(D[key])), f(qt(D[key], .1)), f(qt(D[key], .9)), len(D[key + "_cens"]), f(med(D[key + "_cens"]))))
    P("  受け手の下で追えた四半期：中央 %s・p10 %s" % (f(med(D["followQ"])), f(qt(D["followQ"], .1))))
    for nn in (4, 12, 28, 40):
        P("  n≈%d 四半期：今の R0−割譲の直後の R0 中央 %.4f（p10 %.4f・p90 %.4f）・C1 の R0 中央 %.3f・目標の国家区分 中央 %.3f（%d 件）" % (
            nn, med(D["R0_now_%d" % nn]), qt(D["R0_now_%d" % nn], .1), qt(D["R0_now_%d" % nn], .9), med(D["R0_C1_%d" % nn]), med(D["tS_%d" % nn]), len(D["R0_now_%d" % nn])))
    P("")
    P("[D] 受け手の国への瞬間の影響（AI の算術・上界）")
    P("  割譲の州の人口÷受け手の国の人口：中央 %.4f・p90 %.4f・最大 %.4f" % (med(D["popShare"]), qt(D["popShare"], .9), mx(D["popShare"])))
    P("  税の届きの落ち（人口の割合×R0×0.3・AdminFill＝1 の上界）：中央 %.5f・p90 %.5f・最大 %.5f" % (med(D["taxDropUpper"]), qt(D["taxDropUpper"], .9), mx(D["taxDropUpper"])))
    P("  公共財の到達（RuleReach＝R0）の落ち＝人口の割合×R0：中央 %.5f・p90 %.5f・最大 %.5f" % (med(D["reachDrop"]), qt(D["reachDrop"], .9), mx(D["reachDrop"])))
    P("  C1 で在地へ移る量（割譲の直後の R0）：中央 %.3f・移した後の在地（R0＋R1）中央 %.3f" % (med(D["R0_a"]), med([x + y for x, y in zip(D["R0_a"], D["R1_a"])])))
    P("")
    P("[E] 史実の型の州名（反実仮想の世界＝相手も年も史実と違う・並べるだけ）")
    for h in hist: P(h)
    if not hist: P("  （該当なし）")
    with open(opath, "wb") as fo:
        fo.write(("\n".join(out) + "\n").encode("utf-8"))

def f(x):
    return "nan" if x != x else ("%.0f" % x)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]) if len(sys.argv) > 4 else 1, sys.argv[5] if len(sys.argv) > 5 else "new")
