# (973) c87mw 紙の台本（(959) paper959.py の写し・m968 の盤面へ・末尾にヌル腕の一つ抜きの空振りを足した）：今の盤面（旗 0）の生から κ・門の旗 0 の値・ヌル腕の幅を出す（読むだけ・エンジンを呼ばない）。
#   使い方: python paper973.py <ins>[,<ins>...]   （腕ごとに 1 ブロック・種 1〜10・SB=11 で 11〜20）
#   κ（総量を保つ）＝Σ兵K×(warMult−1)〔cwmult_s の実際の値×nation_s の兵〕÷Σ兵K×接触の割合〔casualty_s の戦死÷Rate＝接触×兵の月の和・上限と制空の増しの分だけ近似〕
import sys, os, csv, statistics, collections
SB = int(os.environ.get('SB', '1')); SEEDS = range(SB, SB + 10)
WMSS = (0.2731, 0.2765)   # Military.WorkingMenStdShare の候補（層の刻みで 2 通り＝κ の近似の幅として両方出す）

def rows(d, t, s):
    p = os.path.join(d, f"{t}_s{s}.tsv")
    if not os.path.exists(p): return []
    with open(p, encoding="utf-8") as f: return list(csv.DictReader(f, delimiter="\t"))

def arm(d):
    o = collections.Counter(); ny = []; gp_post = []; fw_len = []; cw_len = []
    for s in SEEDS:
        sold = {(int(r["quarter"]), r["nation"]): float(r["soldierK"]) for r in rows(d, "nation", s)}
        great = set()
        for r in rows(d, "score", s):
            if r["rankInForce"] == "Great": great.add((int(r["year"]), r["nation"]))
        wm = collections.defaultdict(lambda: [0, 0.0])   # (年, 国) → [交戦の月, warMult の和]
        for r in rows(d, "cwmult", s):
            m = int(r["civilWarMonthsCount"]) + int(r["foreignWarMonthsCount"])
            act = float(r["warMultActualCivilSumIndex"]) + float(r["warMultActualForeignSumIndex"])
            k = (int(r["quarter"]), r["nation"])
            sk = sold.get(k, 0.0)
            o["warMonths"] += m; o["actSum"] += act; o["excessK"] += sk * (act - m); o["soldWarK"] += sk * m
            a = wm[(int(r["year"]), r["nation"])]; a[0] += m; a[1] += act
        for r in rows(d, "casualty", s):
            y = int(r["year"]); n = r["nation"]; dk = float(r["deadK"]); smk = float(r["soldierMonthsK"]); mo = int(r["months"])
            o["deadK"] += dk; o["casMonths"] += mo
            if 1910 <= y <= 1919: o["dead1910sK"] += dk
            a = wm.get((y, n))
            if a and a[0] > 0 and smk > 0: ny.append((dk / smk, a[1] / a[0], a[0], dk == 0))
        for r in rows(d, "wars", s):
            y = int(r["startYear"]); mo = int(r["months"])
            if r["civilWarFlag"] == "1": cw_len.append(mo); continue
            o["foreign"] += 1
            if y >= 1900: o["foreignPost1900"] += 1; fw_len.append(mo)
            g = (y, r["sideA"]) in great and (y, r["sideB"]) in great
            if g: o["gpgp"] += 1
            if g and y >= 1900: o["gpgpPost1900"] += 1
    return o, ny, fw_len, cw_len

VALS = []
for d in sys.argv[1].split(","):
    o, ny, fw, cw = arm(d)
    VALS.append((d, {"fPost": o["foreignPost1900"], "fLen": statistics.mean(fw) if fw else float("nan"), "gpPost": o["gpgpPost1900"], "dead1910sK": o["dead1910sK"], "cwMed": statistics.median(cw) if cw else float("nan"), "excessK": o["excessK"]}))
    print(f"== {d}（種 {SB}〜{SB+9}）")
    print(f"交戦の国×月 {o['warMonths']}・warMult の平均 {o['actSum']/max(1,o['warMonths']):.4f}・Σ兵K×(warMult−1) {o['excessK']:.6g}・Σ兵K×交戦の月 {o['soldWarK']:.6g}")
    for w in WMSS:
        rate = 0.002 / w
        cont = o["deadK"] / rate
        print(f"  WMSS={w}：Σ兵K×接触 ≈ {cont:.6g}・κ ≈ {o['excessK']/cont:.4f}（接触の兵の重みの平均 ≈ {cont/max(1e-9,o['soldWarK']):.5f}）")
    # A2 の代理（国×年）：接触の割合の代理 e＝戦死÷(Rate×兵の月)・上位 10%（交戦の月の重み）の warMult の平均÷全体
    if ny:
        rate = 0.002 / WMSS[0]
        ny.sort(key=lambda t: t[0])
        tot = sum(t[2] for t in ny); top = []; acc = 0
        for t in reversed(ny):
            if acc >= 0.1 * tot: break
            top.append(t); acc += t[2]
        mean = lambda L, i: sum(t[i] * t[2] for t in L) / max(1e-9, sum(t[2] for t in L))
        r0 = mean(top, 1) / mean(ny, 1)
        kap = o["excessK"] / (o["deadK"] / rate)
        e_top = mean(top, 0) / rate; e_all = mean(ny, 0) / rate
        z = [t for t in ny if t[3]]
        print(f"  A2 代理（国×年 {len(ny)}・上位 10% {len(top)}）：旗 0 の比 {r0:.4f}・e の平均 上位 {e_top:.5f}・全体 {e_all:.5f}・旗 1 の見込みの比 (1+κe上)/(1+κe全)＝{(1+kap*e_top)/(1+kap*e_all):.4f}")
        print(f"  A1 代理：戦死 0 の交戦の国×年 {len(z)}（交戦の月 {sum(t[2] for t in z)}）・その warMult の平均 {mean(z,1) if z else float('nan'):.4f}")
    print(f"  A6：対外戦 {o['foreign']}・1900 年以後 {o['foreignPost1900']}（長さの平均 {statistics.mean(fw) if fw else float('nan'):.2f}・中央 {statistics.median(fw) if fw else float('nan')}）・列強どうし {o['gpgp']}（1900 年以後 {o['gpgpPost1900']}）・1910 年代の戦死K {o['dead1910sK']:.6g}")
    print(f"  G2：内戦 {len(cw)}・長さの中央 {statistics.median(cw) if cw else float('nan')}")

# ヌル腕の一つ抜き（3 腕のうち 1 本を腕と見なし、残り 2 本の最小〜最大を両側へ幅の分だけ広げた幅の外か）＝空振りの率が 1/3 以上なら記録へ下げる
if len(VALS) == 3:
    print("== 一つ抜き（両側へ幅の分）")
    for k in ("fPost", "fLen", "gpPost", "dead1910sK", "cwMed"):
        xs = [v[k] for _, v in VALS]; lo, hi = min(xs), max(xs); w = hi - lo
        miss = 0
        for i in range(3):
            ys = [xs[j] for j in range(3) if j != i]; l2, h2 = min(ys), max(ys); w2 = h2 - l2
            if not (l2 - w2 <= xs[i] <= h2 + w2): miss += 1
        print(f"  {k}：値 {xs}・全体の幅 [{lo - w:.6g}, {hi + w:.6g}]・一つ抜きの空振り {miss}/3" + ("＝記録へ" if miss >= 1 else ""))
    e0 = VALS[0][1]["excessK"]
    print("  A4 のヌル腕どうしの比（腕÷base）：" + "・".join(f"{v['excessK'] / e0:.4f}" for _, v in VALS[1:]))
