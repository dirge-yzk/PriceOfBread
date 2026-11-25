# (1361) c143td 設計の材料：効率化材の EffFill（1913 年）を〈閾値を通った割合〉×〈配給の率〉に割る紙。読むだけ・エンジンの関数は呼ばない。
# 生：keep_1355/base_ins（m1355 の M・種 1〜10）の nationgood・subst。
# 使い方：python -I decomp1361.py <base_ins のフォルダ> <出力の txt>
# 式の出所（main 1cb76c40c）：入る条件 EconomyStep.cs:1359（EB×p_out ≦ EK×p_in なら買わない）・最安の 1 つを選ぶ :1360〜:1361・
#   需要 effNeed＝Output×EK :1364〜:1366・残余の配給 :1631〜:1633・決済と EffFill の慣性 :1960〜:1969。
import sys, math, statistics

BASE, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
YEAR = 1913
NATS = ("ロシア", "清", "イギリス")
EB, EK = 0.3, 0.08                     # EffBoost・EffPerOutput（Coefficients.p02_borrow_polity_epidemic.cs:39・:38）
G = EB / EK                            # 3.75
STAPLE = ("米", "野菜", "ジャガイモ", "トウモロコシ")            # Tier＝Staple の RGO＝StapleEffTech の母集合（EconomyStep.cs:81）
CROPS = ("綿花", "麻", "麦", "果実", "染料", "葉煙草") + STAPLE     # Catalog.cs:178（肥料・馬）
GROUPS = {"作物": (CROPS, ("肥料", "馬")),
          "漁獲": (("漁獲",), ("帆船",)),
          "畜産": (("畜産",), ("飼料",)),
          "鉱物": (("鉄鉱石", "石炭", "銅鉱", "鉛鉱", "金", "硫黄", "石灰石", "珪砂", "粘土", "原油", "クロム", "錫鉱", "硝石", "銀"), ("鉄", "ダイナマイト"))}   # Catalog.cs:179〜:181

def rows(path, need):
    with open(path, encoding="utf-8") as fh:
        h = fh.readline().rstrip("\n").split("\t"); ix = [h.index(c) for c in need]
        for line in fh:
            v = line.rstrip("\n").split("\t"); yield [v[i] for i in ix]

def fl(s):
    try: return float(s)
    except ValueError: return float("nan")
def ok(*a): return all(x == x and not math.isinf(x) for x in a)
def med(a): a = [x for x in a if ok(x)]; return statistics.median(a) if a else float("nan")

per = {}
def add(k, v): per.setdefault(k, []).append(v)
world = {}
def wadd(k, v): world.setdefault(k, []).append(v)

for sd in SEEDS:
    ng = {}
    for yr, nat, good, P, D, Sh, St, p in rows(f"{BASE}/nationgood_s{sd}.tsv",
            ["year", "nation", "good", "producedQty", "demandQty", "shortageQty", "stockQty", "priceMeanGold"]):
        if yr != str(YEAR): continue
        ng[(nat, good)] = tuple(map(fl, (P, D, Sh, St, p)))
    # 観測の EffFill（主食の RGO の産出加重を州ごとに持つ s2EffFillHead・州の重みは (1354) と同じ nN＝Q÷(sraw×f)）
    eff = {}
    for yr, nat, kind, e, Q, sraw, f in rows(f"{BASE}/subst_s{sd}.tsv",
            ["year", "nation", "s2BasisKind", "s2EffFillHead", "s3QInst", "s3SrawInst", "s3HarvestInst"]):
        if yr != str(YEAR) or nat not in NATS or kind != "2": continue
        e, Q, sraw, f = fl(e), fl(Q), fl(sraw), fl(f)
        if not ok(e, Q, sraw, f) or not (sraw > 0 and f > 0 and Q > 0): continue
        a = eff.setdefault(nat, [0.0, 0.0]); n = Q / (sraw * f); a[0] += n * e; a[1] += n
    nations = sorted({k[0] for k in ng})
    for nat in nations:
        def g(good): return ng.get((nat, good), (0.0, 0.0, 0.0, 0.0, float("nan")))
        # ---- 問い 1：作物の分解（国の平均の値段で閾値を読む＝州の値段の散らばりは見ない〈桁〉）
        boosters = [(b, g(b)) for b in ("肥料", "馬") if g(b)[0] > 0 and ok(g(b)[4]) and g(b)[4] > 0]
        if not boosters: continue
        pin = min(x[1][4] for x in boosters)
        qst = sum(g(c)[0] for c in STAPLE); qcr = sum(g(c)[0] for c in CROPS)
        pst = sum(g(c)[0] for c in STAPLE if ok(g(c)[4]) and G * g(c)[4] > pin)
        pcr = sum(g(c)[0] for c in CROPS if ok(g(c)[4]) and G * g(c)[4] > pin)
        Rst = (sum(g(c)[0] * g(c)[4] for c in STAPLE if ok(g(c)[4])) / qst / pin) if qst > 0 else float("nan")
        Epass = EK * pcr                                   # 閾値を通った作物の RGO が全て買う時の効率化材の需要
        Pf, Df, Shf, Stf, pf = g("肥料"); Ph, Dh, Shh, Sth, ph = g("馬")
        effDh = max(0.0, Epass - Df)                       # 肥料はコアの需要 0（Catalog に投入の行なし）＝肥料の需要は全て効率化材
        coreh = max(0.0, Dh - effDh)                       # 馬のコア＝軍（MilitaryGoods）＋畜力の労働（Simulation.cs:1107〜1108）
        soldf = max(0.0, Df - Shf); soldh = max(0.0, Dh - Shh)
        effSoldh = max(0.0, min(effDh, soldh - coreh))
        rf = soldf / Df if Df > 0 else float("nan")
        rh = effSoldh / effDh if effDh > 0 else float("nan")
        reff = (soldf + effSoldh) / (Df + effDh) if (Df + effDh) > 0 else float("nan")
        vol = (Pf + Ph) / Epass if Epass > 0 else float("nan")       # 産出の量だけで足りるか（コアも残余も問わない上限）
        volr = (Pf + max(0.0, Ph - coreh)) / Epass if Epass > 0 else float("nan")   # コアを先に引いた残りの産出
        idle = (Stf + Sth) / Epass if Epass > 0 else float("nan")    # 売れずに残った効率化材の在庫（需要の無い州に置かれた分）÷需要
        Pi = pst / qst if qst > 0 else float("nan")
        if nat in NATS:
            k = nat
            e = eff.get(nat); add((k, "EffFill 観測（主食の RGO）"), e[0] / e[1] if e and e[1] > 0 else float("nan"))
            add((k, "R＝p_主食÷p_安い効率化材"), Rst); add((k, "閾値を通った割合 Π（主食の産出）"), Pi)
            add((k, "閾値を通った割合（作物 10 の産出）"), pcr / qcr if qcr > 0 else float("nan"))
            add((k, "安い方が馬＝1・肥料＝0"), 1.0 if ph == pin else 0.0)
            add((k, "肥料の配給 r_f＝売れた÷需要"), rf); add((k, "馬の効率化の配給 r_h（コアの後）"), rh)
            add((k, "効率化材の配給 r（合わせて）"), reff); add((k, "予言の EffFill＝Π×r"), Pi * reff if ok(Pi, reff) else float("nan"))
            add((k, "産出÷需要（肥料＋馬）"), vol); add((k, "コアの後の産出÷需要"), volr); add((k, "売れ残りの在庫÷需要"), idle)
            add((k, "馬のコアの需要÷馬の需要"), coreh / Dh if Dh > 0 else float("nan"))
            add((k, "Epass（効率化材の需要の量）"), Epass); add((k, "肥料の産出"), Pf); add((k, "馬の産出"), Ph)
            add((k, "肥料の不足÷需要"), Shf / Df if Df > 0 else float("nan")); add((k, "肥料の在庫÷産出"), Stf / Pf if Pf > 0 else float("nan"))
        # ---- 問い 2：案 A の世界の桁（全ての国・全ての RGO の群・値段は国の平均）
        for grp, (outs, boos) in GROUPS.items():
            bl = [g(b)[4] for b in boos if g(b)[0] > 0 and ok(g(b)[4]) and g(b)[4] > 0]
            if not bl: continue
            pin2 = min(bl)
            sold_b = sum(max(0.0, g(b)[1] - g(b)[2]) for b in boos); dem_b = sum(g(b)[1] for b in boos)
            rub = sold_b / dem_b if dem_b > 0 else 0.0     # 効率化材の売れた÷需要（コアを含む）＝今の配給の上限の目安
            for c in outs:
                q, p = g(c)[0], g(c)[4]
                if not (q > 0 and ok(p) and p > 0): continue
                val = q * p; R = p / pin2; pas = G * R > 1
                Bnow = 1.0 + EB * (rub if pas else 0.0)              # 今（配給の上限の目安で）
                Bnow1 = 1.0 + EB * (1.0 if pas else 0.0)             # 今の天井
                Bfull = (G * R) ** (EB / (1.0 - EB)) if pas else 1.0 # 案 A・満（e*/k まで届く）
                x = rub if pas else 0.0                               # 案 A・届く量は今のまま（供給が縛る時）：B＝(1＋x)^EB
                Bsame = (1.0 + x) ** EB
                wadd((sd, grp, "val"), val); wadd((sd, grp, "full"), val * Bfull / Bnow); wadd((sd, grp, "same"), val * Bsame / Bnow)
                wadd((sd, grp, "pass"), val * (1.0 if pas else 0.0)); wadd((sd, grp, "ceil"), val * Bfull / Bnow1)
                wadd((sd, grp, "Rlog"), val * math.log(R))

with open(OUT, "w", encoding="utf-8", newline="\n") as fo:
    fo.write(f"# (1361) 1913 年の EffFill の分解（m1355 の M・種 1〜10・種の中央〔最小〜最大〕）・生 {BASE}\n")
    fo.write("# 国の平均の値段と国の合計の量で読む＝州の散らばりを見ない〈桁〉。nationgood は年に 1 行の月の写し、EffFill は慣性 0.25 の積もり。\n")
    for nat in NATS:
        fo.write(f"\n## {nat}\n")
        for (k, name), v in per.items():
            if k != nat: continue
            vv = [x for x in v if ok(x)]
            fo.write(f"{name}\t{med(v):.4g}\t〔{min(vv):.4g}〜{max(vv):.4g}〕\tn={len(vv)}\n" if vv else f"{name}\tnan\n")
    fo.write("\n## 案 A の世界の桁（全ての国・RGO の産出の値の比＝産出の倍率の変わり・値段は据え置きの部分均衡）\n")
    fo.write("群\t値の割合\t閾値を通った値の割合\t満÷今（配給の目安）\t満÷今の天井 1.3\t届く量は今のまま÷今\tR の幾何平均\n")
    tot = {}
    for sd in SEEDS:
        for grp in GROUPS:
            if (sd, grp, "val") in world: tot.setdefault(sd, 0.0); tot[sd] += sum(world[(sd, grp, "val")])
    allv = {sd: {"val": 0.0, "full": 0.0, "same": 0.0, "ceil": 0.0} for sd in SEEDS}
    for grp in GROUPS:
        sh, ps, fu, ce, sa, rg = [], [], [], [], [], []
        for sd in SEEDS:
            V = sum(world.get((sd, grp, "val"), [0.0]))
            if not V > 0: continue
            sh.append(V / tot[sd]); ps.append(sum(world[(sd, grp, "pass")]) / V)
            fu.append(sum(world[(sd, grp, "full")]) / V); ce.append(sum(world[(sd, grp, "ceil")]) / V); sa.append(sum(world[(sd, grp, "same")]) / V)
            rg.append(math.exp(sum(world[(sd, grp, "Rlog")]) / V))
            for kk in ("val", "full", "same", "ceil"): allv[sd][kk] += sum(world[(sd, grp, kk)])
        fo.write(f"{grp}\t{med(sh):.3f}\t{med(ps):.3f}\t{med(fu):.3f}〔{min(fu):.3f}〜{max(fu):.3f}〕\t{med(ce):.3f}\t{med(sa):.4f}\t{med(rg):.3g}\n")
    fu = [allv[sd]["full"] / allv[sd]["val"] for sd in SEEDS if allv[sd]["val"] > 0]
    ce = [allv[sd]["ceil"] / allv[sd]["val"] for sd in SEEDS if allv[sd]["val"] > 0]
    sa = [allv[sd]["same"] / allv[sd]["val"] for sd in SEEDS if allv[sd]["val"] > 0]
    fo.write(f"全ての RGO\t1\t-\t{med(fu):.3f}〔{min(fu):.3f}〜{max(fu):.3f}〕\t{med(ce):.3f}\t{med(sa):.4f}\t-\n")
