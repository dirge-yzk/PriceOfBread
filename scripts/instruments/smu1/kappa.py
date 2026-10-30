# (701) smu1：資本の大きさ κ（工場の資本の値打ち÷工場の年の付加価値・年）を基線の生から読む（読むだけ・C# を変えない・シミュを回さない）。
# 設計書 docs/202610/20261030/20261030_資本の在庫と投資_設計.md §3-9（U1）の列 capva_s は main に無い＝既存の計器 factory_s・nation_s・money_s・sbu_s から作り直した代用。
# 使い方：python scripts/instruments/smu1/kappa.py <keep のフォルダ> <goods.json> <出力> [open]（open＝§4 の開幕の全ての国だけを出す）
#   keep のフォルダ＝D:/PoB_work/keep_694（m694_ins・n1・n2 は種 1〜10、a11 は種 11〜20）。
# 単位の規約（README §1-9）：頭数は POP の Size（千人の単位）。β＝1 は〈Size の 1 単位（千人）に建屋の組 1 単位〉＝コードが掛ける単位（(677) s0.py と同じ）。
# 定義（全て参照価格＝1783 年の名目・実質）：
#   K_β1＝factoryLaborPopK×BundleRef（建屋の組の参照価格・材料だけ）。K_β1L＝×(1＋ConstructionLaborWageFrac 0.67)（建設の工賃込み）。機械の在庫は main に無い＝建屋だけ。
#   産出の単位 u＝plainLaborNeedAtDesired÷LaborPerOutput(0.5)×(outputRef÷desiredRef)。付加価値 VA＝u×FactoryVa(0.06)＋（労働の必要量×労働の参照価格）×(outputRef÷desiredRef)
#     ＝参照価格の連鎖（Catalog.ComputeRefPrices）で投入の材料を引いた残り＝自己費用＋労働＋技師。VA_cap は capacityRef÷desiredRef で同じ形（投入が全て届いたときの上限）。
#   κ＝K÷(12×VA)。π_ref＝12×FactoryVa×u÷K（参照価格のときの持ち主の取り分＝工場の RentShare 1.0・労働は買う投入）。i＝bondRateMeanPerYear。
#   照合：工場の VA×物価水準×12÷名目 GDP（桁の自己検査）・sbu_s の vaSum（実価格の付加価値＝持ち主の取り分・組合の法が禁止でない国の適格な工場だけ）÷（参照価格の取り分×物価水準）。
# 限界（自己申告）：①頭数は POP の Size 全体（HeadsLastMonth＝Size×fraction はどの計器にも無い）＝K を過大・κ を過大に読む側。②factory_s は四半期の末の 1 か月の断面。
#   ③u は既定のレシピの労働の係数 0.5 で逆算＝労働を節約するレシピの工場では u を過小（自己費用の項だけ・VA の 1〜2 割）。④参照価格の VA は実価格の利潤の差（売値−参照価格）を含まない＝照合の列で読む。
#   ⑤factory_s の母集団は BuysLabor の生産者（動力の生産者を含む）。⑥sbu_s の vaSum は va＞0 の工場だけ（正の側へ偏る）。
import csv, sys, json, statistics as st, collections as C
ROOT, GOODS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY_OPEN = len(sys.argv) > 4 and sys.argv[4] == "open"
NAT = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"]
ARMS = [("m694_ins", range(1, 11)), ("n1", range(1, 11)), ("n2", range(1, 11)), ("a11", range(11, 21))]
FACTORY_VA, LABOR_PER_OUTPUT, WAGE_FRAC = 0.06, 0.5, 0.67
REF_LABOR, REF_ADV, REF_FARM = 0.5, 0.8, 0.5

# --- 参照価格の再計算（Catalog.ComputeRefPrices の写し・BundleRef の 3.7104 を確かめる） ---
goods = json.load(open(GOODS, encoding="utf-8-sig"))
ADV = {78: 0.15}
for i in (46, 47, 71, 76, 77, 79, 90, 95, 96, 112, 113, 115): ADV[i] = 0.10
for i in (27, 28, 29, 30, 31, 32, 33, 128, 34): ADV[i] = 0.05
def svc(i): return 0.5 if i in (84, 88, 92) else 0.8 if i == 91 else 0.6
rp = [0.0] * len(goods)
for g in goods:
    rp[g["id"]] = svc(g["id"]) if g["tier"] == "Service" else (0.34 + 0.4 * svc(92) if not g["inputs"] else FACTORY_VA)
for _ in range(24):
    for g in goods:
        if g["tier"] == "Service" or not g["inputs"]: continue
        c = FACTORY_VA + LABOR_PER_OUTPUT * svc(84) + ADV.get(g["id"], 0.02) * svc(91)
        for inp in g["inputs"]:
            if 0 <= inp["goodId"] < len(rp): c += inp["qtyPerOutput"] * rp[inp["goodId"]]
        rp[g["id"]] = c
BUNDLE_REF = 2.3969 * rp[129] + 0.15 * rp[46]   # Coefficients.ConsBuilding×RefPrices[建造物]＋ConsMachine×RefPrices[機械]（BuildingGoodBundle＝1）
# 工場の財の参照価格のうち VA（自己費用＋労働＋技師）の割合（既定のレシピ）＝財ごとの散らばりの記録
vashare = {g["name"]: (FACTORY_VA + 0.25 + ADV.get(g["id"], 0.02) * 0.8) / rp[g["id"]] for g in goods if g["tier"] != "Service" and g["inputs"]}

def dec(y): return 1783 if y < 1790 else (y // 10) * 10
def fnum(x):
    try:
        v = float(x); return v if v == v else None
    except: return None

def read_arm(arm, seeds):
    # 戻り値：(nation, decade) → 指標 → 種ごとの値のリスト
    res = C.defaultdict(lambda: C.defaultdict(list))
    for s in seeds:
        base = f"{ROOT}/{arm}"
        bond = {}; gdp = {}
        with open(f"{base}/nation_s{s}.tsv", encoding="utf-8") as f:
            for x in csv.DictReader(f, delimiter="\t"):
                if x["nation"] not in NAT: continue
                k = (int(x["quarter"]), x["nation"])
                bond[k] = fnum(x["bondRateMeanPerYear"]); gdp[k] = fnum(x["nominalGdpPerYear"])
        pl = {}
        with open(f"{base}/money_s{s}.tsv", encoding="utf-8") as f:
            for x in csv.DictReader(f, delimiter="\t"):
                if x["nation"] not in NAT or not x["time"].startswith("q"): continue
                pl[(int(x["time"][1:]), x["nation"])] = fnum(x["priceLevelAfterIndex"])
        sbu = {}
        with open(f"{base}/sbu_s{s}.tsv", encoding="utf-8") as f:
            for x in csv.DictReader(f, delimiter="\t"):
                if x["nation"] not in NAT: continue
                v = fnum(x["vaSum"]); n = fnum(x["eligN"])
                if v and n and n > 0: sbu[(int(x["quarter"]), x["nation"])] = v / 3.0   # 四半期の 3 か月の和→月
        acc = C.defaultdict(lambda: C.defaultdict(list))   # 種の中：十年の四半期の値を集める
        with open(f"{base}/factory_s{s}.tsv", encoding="utf-8") as f:
            for x in csv.DictReader(f, delimiter="\t"):
                if x["nation"] not in NAT: continue
                q = int(x["quarter"]); y = int(x["year"]); n = x["nation"]; k = (dec(y), n)
                heads = fnum(x["factoryLaborPopK"]); outR = fnum(x["outputRefValuePerMonth"]); desR = fnum(x["desiredRefValuePerMonth"]); capR = fnum(x["capacityRefValuePerMonth"])
                pn = fnum(x["plainLaborNeedAtDesiredUnitsPerMonth"]) or 0.0; an = fnum(x["advLaborNeedAtDesiredUnitsPerMonth"]) or 0.0; fn = fnum(x["farmLaborNeedAtDesiredUnitsPerMonth"]) or 0.0
                if not heads or not desR or desR <= 0 or outR is None: continue
                uDes = pn / LABOR_PER_OUTPUT
                vaDes = uDes * FACTORY_VA + pn * REF_LABOR + an * REF_ADV + fn * REF_FARM
                vaOut = vaDes * outR / desR; vaCap = vaDes * (capR or 0) / desR; uOut = uDes * outR / desR
                K = heads * BUNDLE_REF
                if not vaOut > 0: continue
                a = acc[k]
                a["K"].append(K); a["vaOut"].append(vaOut); a["vaCap"].append(vaCap); a["profRef"].append(FACTORY_VA * uOut); a["heads"].append(heads)
                a["vaOverOut"].append(vaOut / outR if outR > 0 else float("nan"))
                b = bond.get((q, n));
                if b and b > 0: a["i"].append(b)
                p = pl.get((q, n)); g = gdp.get((q, n))
                if p and g and g > 0: a["vaGdp"].append(12 * vaOut * p / g)
                sv = sbu.get((q, n))
                if sv and p and uOut > 0: a["sbuRatio"].append(sv / (FACTORY_VA * uOut * p))
        for k, a in acc.items():
            K = st.mean(a["K"]); vaO = st.mean(a["vaOut"]); vaC = st.mean(a["vaCap"]); pr = st.mean(a["profRef"])
            r = res[k]
            r["headsK"].append(st.mean(a["heads"]))
            r["kappa_out"].append(K / (12 * vaO)); r["kappa_owner"].append(K / (12 * pr) if pr > 0 else float("nan")); r["kappa_outL"].append(K * (1 + WAGE_FRAC) / (12 * vaO)); r["kappa_cap"].append(K / (12 * vaC) if vaC > 0 else float("nan"))
            r["va_per_headK_yr"].append(12 * vaO / st.mean(a["heads"]))
            r["vaOverOutRef"].append(st.mean(a["vaOverOut"]))
            pi = 12 * pr / K; r["pi_ref"].append(pi)
            if a["i"]: ii = st.mean(a["i"]); r["i"].append(ii); r["pi_over_i"].append(pi / ii)
            if a["vaGdp"]: r["vaGdp"].append(st.mean(a["vaGdp"]))
            if a["sbuRatio"]: r["sbuRatio"].append(st.median(a["sbuRatio"]))
    return res

def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float("nan")
def rng(v):
    v = sorted(x for x in v if x == x); return (v[0], v[-1]) if v else (float("nan"), float("nan"))
def g3(x): return "nan" if x != x else f"{x:.3g}"

R = {} if ONLY_OPEN else {arm: read_arm(arm, seeds) for arm, seeds in ARMS}

def read_open_all(arm, seeds):
    # factory_s は挙げた国を毎四半期・他の国を年末に 1 行出す（keep_694 で確かめた：他の国の行は四半期 3・7・11…で、年の欄は次の年〔四半期 3 は 1784〕）
    # ＝最初の年の末の行（四半期 3）を全ての国から読み、国ごとの κ_out と千人あたりの年の付加価値
    out = C.defaultdict(lambda: C.defaultdict(list))
    for s in seeds:
        with open(f"{ROOT}/{arm}/factory_s{s}.tsv", encoding="utf-8") as f:
            for x in csv.DictReader(f, delimiter="	"):
                if int(x["quarter"]) != 3: continue
                n = x["nation"]
                heads = fnum(x["factoryLaborPopK"]); outR = fnum(x["outputRefValuePerMonth"]); desR = fnum(x["desiredRefValuePerMonth"])
                pn = fnum(x["plainLaborNeedAtDesiredUnitsPerMonth"]) or 0.0; an = fnum(x["advLaborNeedAtDesiredUnitsPerMonth"]) or 0.0; fn = fnum(x["farmLaborNeedAtDesiredUnitsPerMonth"]) or 0.0
                if not heads or not desR or desR <= 0 or not outR: continue
                vaO = (pn / LABOR_PER_OUTPUT * FACTORY_VA + pn * REF_LABOR + an * REF_ADV + fn * REF_FARM) * outR / desR
                if not vaO > 0: continue
                d = out[n]; d["last"] = [heads, vaO]   # 1783 年の末の行（四半期 3）
        for n, d in out.items():
            if "last" in d:
                h, v = d.pop("last"); d["kappa_out"].append(h * BUNDLE_REF / (12 * v)); d["vaPerHeadK"].append(12 * v / h); d["heads"].append(h)
    return out
L = []
L.append("# (701) smu1 κ の表（読むだけ・基線 m694 の生 D:/PoB_work/keep_694・書いたモデル Claude Opus 5.5・読み手 scripts/instruments/smu1/kappa.py）")
L.append(f"# BundleRef（goods.json から再計算）＝{BUNDLE_REF:.4f}（Coefficients.cs 1204 の注の 3.7104 と照合）・RefPrices[建造物]＝{rp[129]:.4f}・RefPrices[機械]＝{rp[46]:.4f}")
vs = sorted(vashare.values())
L.append(f"# 工場の財の参照価格のうち付加価値（自己費用＋労働＋技師・既定のレシピ）の割合：財 {len(vs)} 個・最小 {vs[0]:.3f}・中央 {st.median(vs):.3f}・最大 {vs[-1]:.3f}")
L.append("# 単位：頭数は Size（千人）。β＝1＝Size 1 単位（千人）に建屋の組 1 単位。κ は年。π は年あたり。値は種ごとの十年の平均の、種の中央値（括弧は種の最小..最大）。")
L.append("# 列：headsK＝工場の担い手の POP（千人）・va/headK＝千人あたりの年の付加価値（参照価格）・VA/out＝付加価値÷産出（参照価格）・κ_out＝K_β1÷年の VA（産出の実績）・κ_outL＝建設の工賃込みの K・κ_cap＝能力どおりの VA で割った下限・κ_owner＝K_β1÷年の持ち主の取り分（参照価格の自己費用 0.06×産出＝1/π_ref・コードの EconomyStep の valueAdded〔売上−投入〈労働を含む〉〕に当たる側）・π_ref＝参照価格の持ち主の取り分÷K・i＝国債金利・π/i・VA/GDP＝工場の付加価値×物価水準÷名目GDP・sbu＝実価格の持ち主の取り分÷参照価格の取り分（組合の法が禁止でない四半期だけ）")
L.append("")
if not ONLY_OPEN:
    cols = ["headsK", "va_per_headK_yr", "vaOverOutRef", "kappa_out", "kappa_outL", "kappa_cap", "kappa_owner", "pi_ref", "i", "pi_over_i", "vaGdp", "sbuRatio"]
    L.append("## 1. 基線 m694（種 1〜10）・国×十年")
    L.append("nation\tdecade\t" + "\t".join(cols) + "\tκ_out(種の最小..最大)")
    m = R["m694_ins"]
    for n in NAT:
        for d in sorted({k[0] for k in m if k[1] == n}):
            r = m[(d, n)]; lo, hi = rng(r["kappa_out"])
            L.append(f"{n}\t{d}\t" + "\t".join(g3(med(r[c])) for c in cols) + f"\t{g3(lo)}..{g3(hi)}")
    L.append("")
    L.append("## 2. 腕どうしの比べ（κ_out の種の中央値・m694／ヌル腕 N1／N2／種 11〜20）")
    L.append("nation\tdecade\tm694\tN1\tN2\ta11")
    for n in NAT:
        for d in sorted({k[0] for k in m if k[1] == n}):
            L.append(f"{n}\t{d}\t" + "\t".join(g3(med(R[a][(d, n)]["kappa_out"])) if (d, n) in R[a] else "nan" for a, _ in ARMS))
    L.append("")
    # 世界（9 国）の要約：1800〜1913 の国×十年の κ_out の分布
    allk = [med(m[k]["kappa_out"]) for k in m if 1800 <= k[0] <= 1910]
    allp = [med(m[k]["pi_over_i"]) for k in m if 1800 <= k[0] <= 1910]
    L.append("## 3. 要約（基線 m694・9 国×十年 1800〜1910）")
    q = sorted(x for x in allk if x == x)
    L.append(f"κ_out：件数 {len(q)}・最小 {q[0]:.3g}・四分位 {q[len(q)//4]:.3g}・中央 {st.median(q):.3g}・四分位 {q[3*len(q)//4]:.3g}・最大 {q[-1]:.3g}")
    q = sorted(x for x in allp if x == x)
    L.append(f"π_ref÷i：件数 {len(q)}・最小 {q[0]:.3g}・中央 {st.median(q):.3g}・最大 {q[-1]:.3g}")
OA = read_open_all("m694_ins", range(1, 11))
L.append("")
L.append("## 4. 開幕 1783 年の全ての国（1783 年の末＝四半期 3 の行・種 1〜10 の中央値・世界）＝β の式の材料（§3-9：β＝κ×開幕の世界の工場の 1 人あたり年の付加価値の中央値÷建屋の組の参照価格）")
vk = sorted(med(d["kappa_out"]) for d in OA.values()); vv = sorted(med(d["vaPerHeadK"]) for d in OA.values())
wv = [(med(d["vaPerHeadK"]), med(d["heads"])) for d in OA.values()]
L.append(f"国の数 {len(vk)}・κ_out：最小 {vk[0]:.3g}・四分位 {vk[len(vk)//4]:.3g}・中央 {st.median(vk):.3g}・四分位 {vk[3*len(vk)//4]:.3g}・最大 {vk[-1]:.3g}")
L.append(f"千人あたりの年の付加価値（参照価格）：最小 {vv[0]:.4g}・中央 {st.median(vv):.4g}・最大 {vv[-1]:.4g}・頭数加重の平均 {sum(a*b for a,b in wv)/sum(b for _,b in wv):.4g}")
L.append(f"β＝1 の千人あたりの建屋の値打ち＝BundleRef {BUNDLE_REF:.4f}＝世界の中央の κ_out（β＝1）＝{BUNDLE_REF/st.median(vv):.3g} 年")
L.append("β の要る倍率＝κ_出典÷(BundleRef÷中央の千人あたりの年の付加価値)：κ_出典 0.5／1／2 年〈推〉で " + "・".join(f"{k*st.median(vv)/BUNDLE_REF:.3g}" for k in (0.5, 1.0, 2.0)) + " 倍")
L.append("nation	kappa_out_1783	vaPerHeadK_yr	headsK")
for n in sorted(OA, key=lambda n: -med(OA[n]["heads"])):
    d = OA[n]; L.append(f"{n}	{g3(med(d['kappa_out']))}	{g3(med(d['vaPerHeadK']))}	{g3(med(d['heads']))}")
open(OUT, "wb").write(("\n".join(L) + "\n").encode("utf-8"))
print("\n".join(L[:8])); print(f"wrote {OUT}")
