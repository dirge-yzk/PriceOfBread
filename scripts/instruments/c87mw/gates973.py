# (973) c87mw 門の集計（事前登録 measurements/m973_20261108_prereg_mw.txt §3 の写し・結果を見て替えない）。(959) gates959.py の写しを m968 の盤面へ。
#   使い方: python gates973.py <腕の ins> <基線の ins（keep_968/base_ins）>   （種 1〜10・SB=11 で 11〜20）
#   A1・A2c・A3 は cwmult_s の末尾 5 列（(959) で足した列）を読む＝列が無い生は〈列なし〉と書く。A2・A4 と記録は既にある表だけ。
#   門＝A1・A2・A2c・A3・A4（最後の行に GATES PASS か GATES FAIL）。R1〜R6 は記録（採否に使わない・幅の外なら〈注視〉と書く）。
import sys, os, csv, statistics, collections
ARM, BASE = sys.argv[1], sys.argv[2]
SB = int(os.environ.get('SB', '1')); SEEDS = range(SB, SB + 10)

def rows(d, t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return []
    with open(p, encoding="utf-8") as f: return list(csv.DictReader(f, delimiter="\t"))

def take(d):
    o = collections.Counter(); ny = []; fw = []; cw = []; rc = []; has_new = None; seeds = 0
    for s in SEEDS:
        cwr = rows(d, "cwmult", s)
        if not cwr: continue
        seeds += 1
        sold = {(int(r["quarter"]), r["nation"]): float(r["soldierK"]) for r in rows(d, "nation", s)}
        great = {(int(r["year"]), r["nation"]) for r in rows(d, "score", s) if r["rankInForce"] == "Great"}
        wm = collections.defaultdict(lambda: [0, 0.0])
        for r in cwr:
            c = int(r["civilWarMonthsCount"]); fo = int(r["foreignWarMonthsCount"]); m = c + fo
            act = float(r["warMultActualCivilSumIndex"]) + float(r["warMultActualForeignSumIndex"])
            o["warMonths"] += m; o["civil"] += c; o["actSum"] += act
            o["excessK"] += sold.get((int(r["quarter"]), r["nation"]), 0.0) * (act - m)
            o["ammoD"] += float(r["ammunitionDemandCivilQty"]); o["ammoP"] += float(r["ammunitionProducedCivilQty"])
            o["powD"] += float(r["gunpowderDemandCivilQty"]); o["powP"] += float(r["gunpowderProducedCivilQty"])
            o["leadD"] += float(r["leadDemandCivilQty"]); o["leadP"] += float(r["leadProducedCivilQty"])
            if "contactRuleMismatchMonthsCount" in r:
                has_new = True
                ct = float(r["contactShareSumIndex"])
                o["ctMis"] += int(r["contactRuleMismatchMonthsCount"]); o["zeroM"] += int(r["zeroContactWarMonthsCount"])
                o["zeroAct"] += float(r["warMultActualZeroContactSumIndex"]); o["ctSum"] += ct
                o["soldCtK"] += sold.get((int(r["quarter"]), r["nation"]), 0.0) * ct
                if m > 0: rc.append((ct / m, act / m, m))
            else:
                has_new = False if has_new is None else has_new
            a = wm[(int(r["year"]), r["nation"])]; a[0] += m; a[1] += act
        for r in rows(d, "casualty", s):
            y = int(r["year"]); dk = float(r["deadK"]); smk = float(r["soldierMonthsK"])
            if 1910 <= y <= 1919: o["dead1910sK"] += dk
            a = wm.get((y, r["nation"]))
            if a and a[0] > 0 and smk > 0: ny.append((dk / smk, a[1] / a[0], a[0]))
        for r in rows(d, "wars", s):
            y = int(r["startYear"]); mo = int(r["months"])
            if r["civilWarFlag"] == "1": cw.append(mo); continue
            if y >= 1900:
                o["fPost"] += 1; fw.append(mo)
                if (y, r["sideA"]) in great and (y, r["sideB"]) in great: o["gpPost"] += 1
    return o, ny, fw, cw, rc, has_new, seeds

def top10(L):
    # L＝(並べる量, 読む量, 重み)。並べる量の上位 10%（重みで）の読む量の平均÷全体の平均、と上位の並べる量の平均÷全体の平均
    if not L: return float("nan"), float("nan")
    L = sorted(L, key=lambda t: t[0]); tot = sum(t[2] for t in L); top = []; acc = 0
    for t in reversed(L):
        if acc >= 0.1 * tot: break
        top.append(t); acc += t[2]
    mean = lambda X, i: sum(t[i] * t[2] for t in X) / max(1e-9, sum(t[2] for t in X))
    return mean(top, 1) / mean(L, 1), (mean(top, 0) / mean(L, 0) if mean(L, 0) > 0 else float("nan"))

def ok(x): return "緑" if x else "赤"
def rec(x): return "幅の中" if x else "注視（幅の外）"
a, nya, fwa, cwa, rca, hn, sa = take(ARM)
b, nyb, fwb, cwb, _, _, sb = take(BASE)
gates = []
print(f"# (973) 門（腕 {ARM}・基線 {BASE}・種 {SB}〜{SB+9}）")
print(f"腕：種 {sa}・交戦の月 {a['warMonths']}・内戦の月 {a['civil']}・warMult の平均 {a['actSum']/max(1,a['warMonths']):.4f}／基線：種 {sb}・交戦の月 {b['warMonths']}")
if hn:
    zm = a["zeroAct"] / a["zeroM"] if a["zeroM"] else float("nan")
    g = a["zeroM"] >= 100 and abs(zm - 1) <= 1e-6; gates.append(g)
    print(f"A1：接触 0 の交戦の月 {a['zeroM']}（≧100）・その warMult の平均 {zm:.9f}（|−1|≦1e-6）\t{ok(g)}")
    r2c, eratio = top10(rca)
    g = 1.5 <= r2c <= eratio; gates.append(g)
    print(f"A2c：計器の接触の割合の上位 10%（国×四半期・月の重み）の warMult÷全体 {r2c:.4f} ∈ [1.5, 上位の接触÷全体の接触 {eratio:.4f}]\t{ok(g)}")
    lim = 0.001 * a["warMonths"]
    g = a["ctMis"] <= lim; gates.append(g)
    print(f"A3：新の規則（計器の 1＋κe）とのずれの月 {a['ctMis']} ≦ {lim:.1f}\t{ok(g)}")
    print(f"記録：計器の接触の割合の月の平均 {a['ctSum']/max(1,a['warMonths']):.5f}・Σ兵K×e {a['soldCtK']:.6g}・κ（総量を保つ値・同じ盤面）{a['excessK']/a['soldCtK'] if a['soldCtK'] > 0 else float('nan'):.4f}")
else:
    gates.append(False)
    print("A1・A2c・A3：cwmult_s に (959) の列なし\t赤")
r2, _ = top10(nya); r2b, _ = top10(nyb)
g = 1.5 <= r2 <= 3.5; gates.append(g)
print(f"A2：接触の代理（戦死÷兵の月）の上位 10% の warMult÷全体 {r2:.4f} ∈ [1.5, 3.5]\t{ok(g)}（基線 {r2b:.4f}）")
r4 = a["excessK"] / b["excessK"] if b["excessK"] > 0 else float("nan")
g = 0.5 <= r4 <= 2.0; gates.append(g)
print(f"A4：Σ兵K×(warMult−1) 腕÷基線 {r4:.4f} ∈ [0.5, 2.0]\t{ok(g)}（腕 {a['excessK']:.6g}・基線 {b['excessK']:.6g}）")
print("--- 記録（採否に使わない・幅は m968 の base・n1・n2 の最小〜最大を両側へ幅の分だけ広げた物・一つ抜きの空振り 1/3 で門から下げた）")
med = statistics.median(cwa) if cwa else float("nan")
print(f"R1 内戦の長さの中央 {med}（内戦 {len(cwa)}）∈ [9, 12]\t{rec(9 <= med <= 12)}（基線 {statistics.median(cwb) if cwb else float('nan')}）")
for gname, kd, kp in (("弾薬", "ammoD", "ammoP"), ("火薬", "powD", "powP"), ("鉛", "leadD", "leadP")):
    print(f"R2 {gname}：内戦の月の需要の和 {a[kd]:.6g}・産出の和 {a[kp]:.6g}（基線 {b[kd]:.6g}・{b[kp]:.6g}）\t{rec(a[kd] > 0 and a[kp] > 0)}")
if a["civil"] > 0 and b["civil"] > 0 and b["ammoD"] > 0:
    dB = a["ammoD"] / a["civil"]; d0 = b["ammoD"] / b["civil"]
    print(f"R2 比：弾薬の内戦の月あたりの需要 d_B÷d_0 {dB/d0:.4f}\t{rec(dB/d0 >= 0.2)}")
fl = statistics.mean(fwa) if fwa else float("nan")
print(f"R3 1900 年以後の対外戦の本数 {a['fPost']} ∈ [894, 1410]\t{rec(894 <= a['fPost'] <= 1410)}（基線 {b['fPost']}）")
print(f"R4 その長さの平均 {fl:.2f} ∈ [15.69, 19.62]\t{rec(15.69 <= fl <= 19.62)}（基線 {statistics.mean(fwb) if fwb else float('nan'):.2f}）")
print(f"R5 1900 年以後の列強どうし {a['gpPost']} ∈ [159, 306]\t{rec(159 <= a['gpPost'] <= 306)}（基線 {b['gpPost']}）")
print(f"R6 1910 年代の戦死K {a['dead1910sK']:.6g} ∈ [0, 18299]\t{rec(0 <= a['dead1910sK'] <= 18299)}（基線 {b['dead1910sK']:.6g}）")
print("GATES PASS" if all(gates) else "GATES FAIL")
