# (959) c85mw 門の集計（事前登録 measurements/m959_20261108_prereg_mw.txt §3 の写し・結果を見て替えない）
#   使い方: python gates959.py <腕の ins> <基線の ins（keep_954/base_ins）>   （種 1〜10・SB=11 で 11〜20）
#   A1・A3 は cwmult_s の末尾の列（(959) で足した列）を読む＝列が無い生は〈列なし〉と書く。A2・A4〜A6 は既にある表だけ。
import sys, os, csv, statistics, collections
ARM, BASE = sys.argv[1], sys.argv[2]
SB = int(os.environ.get('SB', '1')); SEEDS = range(SB, SB + 10)
RATE = 0.002 / 0.2765   # A2 の代理の Rate（順位にしか効かない＝WMSS の選び方で結果は変わらない）

def rows(d, t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return []
    with open(p, encoding="utf-8") as f: return list(csv.DictReader(f, delimiter="\t"))

def take(d):
    o = collections.Counter(); ny = []; fw = []; cw = []; has_new = None; seeds = 0
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
                o["ctMis"] += int(r["contactRuleMismatchMonthsCount"]); o["zeroM"] += int(r["zeroContactWarMonthsCount"])
                o["zeroAct"] += float(r["warMultActualZeroContactSumIndex"]); o["ctSum"] += float(r["contactShareSumIndex"])
                o["soldCtK"] += sold.get((int(r["quarter"]), r["nation"]), 0.0) * float(r["contactShareSumIndex"])
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
    return o, ny, fw, cw, has_new, seeds

def a2(ny):
    if not ny: return float("nan")
    ny = sorted(ny, key=lambda t: t[0]); tot = sum(t[2] for t in ny); top = []; acc = 0
    for t in reversed(ny):
        if acc >= 0.1 * tot: break
        top.append(t); acc += t[2]
    mean = lambda L: sum(t[1] * t[2] for t in L) / max(1e-9, sum(t[2] for t in L))
    return mean(top) / mean(ny)

def ok(x): return "緑" if x else "赤"
a, nya, fwa, cwa, hn, sa = take(ARM)
b, nyb, fwb, cwb, _, sb = take(BASE)
print(f"# (959) 門（腕 {ARM}・基線 {BASE}・種 {SB}〜{SB+9}）")
print(f"腕：種 {sa}・交戦の月 {a['warMonths']}・内戦の月 {a['civil']}・warMult の平均 {a['actSum']/max(1,a['warMonths']):.4f}／基線：種 {sb}・交戦の月 {b['warMonths']}")
if hn:
    zm = a["zeroAct"] / a["zeroM"] if a["zeroM"] else float("nan")
    print(f"A1：接触 0 の交戦の月 {a['zeroM']}（≧100）・その warMult の平均 {zm:.9f}（|−1|≦1e-6）\t{ok(a['zeroM'] >= 100 and abs(zm - 1) <= 1e-6)}")
    lim = 0.001 * a["warMonths"]
    print(f"A3：新の規則（計器の 1＋κe）とのずれの月 {a['ctMis']} ≦ {lim:.1f}\t{ok(a['ctMis'] <= lim)}")
    print(f"記録：計器の接触の割合の月の平均 {a['ctSum']/max(1,a['warMonths']):.5f}・Σ兵K×e {a['soldCtK']:.6g}・κ（総量を保つ値・同じ盤面）{a['excessK']/a['soldCtK'] if a['soldCtK'] > 0 else float('nan'):.4f}")
else:
    print("A1・A3：cwmult_s に (959) の列なし")
r2 = a2(nya)
print(f"A2：接触の代理の上位 10% の warMult÷全体 {r2:.4f} ∈ [1.5, 3.5]\t{ok(1.5 <= r2 <= 3.5)}（基線 {a2(nyb):.4f}）")
r4 = a["excessK"] / b["excessK"] if b["excessK"] > 0 else float("nan")
print(f"A4：Σ兵K×(warMult−1) 腕÷基線 {r4:.4f} ∈ [0.5, 2.0]\t{ok(0.5 <= r4 <= 2.0)}（腕 {a['excessK']:.6g}・基線 {b['excessK']:.6g}）")
med = statistics.median(cwa) if cwa else float("nan")
print(f"A5 G2：内戦の長さの中央 {med}（内戦 {len(cwa)}）∈ [10, 13]\t{ok(10 <= med <= 13)}")
for g, kd, kp in (("弾薬", "ammoD", "ammoP"), ("火薬", "powD", "powP"), ("鉛", "leadD", "leadP")):
    print(f"A5 G3 {g}：内戦の月の需要の和 {a[kd]:.6g}・産出の和 {a[kp]:.6g}＞0\t{ok(a[kd] > 0 and a[kp] > 0)}")
if a["civil"] > 0 and b["civil"] > 0 and b["ammoD"] > 0:
    dB = a["ammoD"] / a["civil"]; d0 = b["ammoD"] / b["civil"]
    print(f"A5 G3 比：弾薬の内戦の月あたりの需要 d_B÷d_0 {dB/d0:.4f} ≧ 0.2\t{ok(dB/d0 >= 0.2)}")
fl = statistics.mean(fwa) if fwa else float("nan")
print(f"A6 1900 年以後の対外戦の本数 {a['fPost']} ∈ [877, 1708]\t{ok(877 <= a['fPost'] <= 1708)}（基線 {b['fPost']}）")
print(f"A6 その長さの平均 {fl:.2f} ∈ [14.59, 18.94]\t{ok(14.59 <= fl <= 18.94)}（基線 {statistics.mean(fwb) if fwb else float('nan'):.2f}）")
print(f"A6 1900 年以後の列強どうし {a['gpPost']} ∈ [178, 295]\t{ok(178 <= a['gpPost'] <= 295)}（基線 {b['gpPost']}）")
print(f"A6 1910 年代の戦死K {a['dead1910sK']:.6g} ∈ [7607, 10496]\t{ok(7607 <= a['dead1910sK'] <= 10496)}（基線 {b['dead1910sK']:.6g}）")
