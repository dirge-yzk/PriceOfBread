#!/usr/bin/env python
"""(430) 腕 B の判定の読み手（事前登録 measurements/dw_20261019_prereg.txt [7] の定義どおり・枝 dd の judge.py に T2b・T7・T8 を足した）。腕を回す前に書いてコミットする（判定の定義を腕の後に書かない）。シミュは回さない。

入力：<計器の出力フォルダ>/disc_s<seed>.tsv（E 行＝解禁）・engineer_s<seed>.tsv（T 行＝8 四半期ごとの技術力、E 行＝解禁した国の A_n と技術力）・nation_s<seed>.tsv（開幕の人口）。
定義（腕の前に固定）：
  ・対象の行＝非制度・非思想の行（合成染料は旗の正負で E の有無が違うので除く）。制度と思想の行は disc_s の E に出るが数えない（SPREAD）。
  ・最初の発見者＝(種, 行) ごとに disc_s の E 行で (year, quarter) が最小の国。同じ (year, quarter) に複数の国が開いたら全員を最初の発見者とし、1/k ずつの重みで数える（(種, 行) の重みの和は 1）。
  ・小国＝その種の開幕（nation_s の quarter 0）の人口が、その種の国々の中央値より下の国。開幕に居ない国（独立・建国）は小国の比の分子にも分母にも入れず別に数える。T4 の比の分母には入れる。
  ・T1＝(種) ごとに、最初の解禁の year が 1783 の行の数（decade 1780＝d≦0 の行も入れる＝(415) の 11.5・m412 の 1 と同じ数え方）。種の平均と最大。d≦0 の行の数も別に出す。
  ・T2＝転炉鋼・機関車・汽船・加硫ゴムの最初の解禁の year の種の中央値。10 種のどれでも開かない種は ∞ として中央値に入れる（中央値が ∞ なら〈開かない〉）。
  ・T3 前半＝最初の発見者の A_n（engineer_s の E 行の engineerPerMonth）が Am 未満の件の重みの比。A_n が読めない件（engineer_s の E 行が無い）は別に数える。
  ・T4＝最初の発見者の重みの国別の比（清・最大の 1 国）。
  ・T5＝10 種（または与えた種の数）のどれでも開かない行の数と名前。行の全体＝engineer_s の T 行に現れる非制度・非思想の行の和集合。
  ・T0＝engineer_s の T 行（year≦1785・knownFlag＝0・d>0 の行だけ）の techTerm≧55（0.786×70）の国×行の数と国別。
追加（枝 dw）：
  ・T2b＝転炉鋼・機関車・汽船の 3 行とも 10 種のうち 8 種以上で開き、3 行の最初の解禁の年の中央値が 1900 年以前。
  ・T7＝clock_s の W 行の 1849 年と 1899 年の W の種の中央値が census（--census=<旗 0 の census の出力フォルダ>）の同じ年の 0.5 倍以上・最大の W÷W85 が 10 以上。
  ・T8＝行ごとの最初の解禁の年の種をまたぐ四分位幅（IQR）が 2 年未満の行の比（解禁した行のうち）が 75% 以下。外れは名乗りの段（採否に使わない）。
追記 3（腕の前に足した・採否の定義は変えない）：
  ・--rows=<clock_s のあるフォルダ> ＝行の全体（T5 の分母）をそのフォルダの clock_s の T 行から取る（m428 の生には clock_s が無い＝canary の出力などを渡す）。
  ・--base=<m428 の計器つき 10 種の生> ＝T5 を 2 本で印字する（①10 種の和の本数 ②基準の 10 種の和との差＝増えた行・減った行の名前）。
  ・T7 の W85 は --census の 1785 年の種の中央値（登録の W85）。腕自身の 1785 年の W ではない。
  ・T0・T2 は合否を印字する。T6（計算量）はこの読み手の外＝.cost を手で読む。T8 は名乗りの段（採否に使わない）。
  ・記録だけ（採否に使わない）：R-a W の種をまたぐ変動係数（標本年ごと）／R-b 同じ標本年・同じ行で A_n＞0 の国（計器の 8 国だけ）の techTerm のばらつき／R-c 最初の発見者の解禁時の A_n の分布と〈世界の取り分〉。
    全 61 国の開幕の工業の大きさは計器に無い（engineer_s は計器の 8 国だけ）＝反証役の求めた〈開幕の工業の下位半分〉は R-c で代える（nation_s の開幕の名目 GDP の下位半分も併記する）。
使い方：PYTHONUTF8=1 python judge.py <計器の出力フォルダ> [種の数=10] [--am=17.51] [--census=<census の出力フォルダ>] [--rows=<clock_s のあるフォルダ>] [--base=<基準の計器つき 10 種の生>]
"""
import collections, csv, glob, math, os, statistics, sys

SPREAD = {"特許", "株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"}
EXCLUDED = {"合成染料"}
T2ROWS = ["転炉鋼", "機関車", "汽船", "加硫ゴム"]


def rd(path):
    with open(path, encoding="utf-8") as fh:
        return list(csv.DictReader(fh, delimiter="\t"))


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    am = 17.51
    census = None
    rows_dir = None
    base = None
    for a in sys.argv[1:]:
        if a.startswith("--am="):
            am = float(a[5:])
        elif a.startswith("--census="):
            census = a[9:]
        elif a.startswith("--rows="):
            rows_dir = a[7:]
        elif a.startswith("--base="):
            base = a[7:]
    root = args[0]
    nseeds = int(args[1]) if len(args) > 1 else 10
    first = {}      # (seed,row) -> list of (year,quarter,nation)
    pop0 = {}
    gdp0 = {}
    aN = {}         # (seed,row,nation) -> engineerPerMonth at unlock
    allrows = set()
    d0rows = set()   # decade<=1780（d≦0）の行
    t0hits = collections.Counter(); t0n = 0
    tech_by = collections.defaultdict(list)   # (seed,row,year) -> techTerm の国別（A_n>0 の国だけ）
    share_at = {}   # (seed,row,nation) -> 解禁時の clock_s E 行の ownPerMonth÷worldTotalPerMonth
    for s in range(1, nseeds + 1):
        p0 = {}
        for r in rd(os.path.join(root, f"nation_s{s}.tsv")):
            if r["quarter"] == "0":
                p0[r["nation"]] = float(r["totalPopulationK"])
        pop0[s] = p0
        g0 = {}
        for r in rd(os.path.join(root, f"nation_s{s}.tsv")):
            if r["quarter"] == "0":
                try:
                    g0[r["nation"]] = float(r["nominalGdpPerYear"])
                except ValueError:
                    pass
        gdp0[s] = g0
        best = {}
        for r in rd(os.path.join(root, f"disc_s{s}.tsv")):
            if r["kind"] != "E" or r["row"] in SPREAD or r["row"] in EXCLUDED:
                continue
            t = (int(r["year"]), int(r["quarter"]))
            best.setdefault(r["row"], []).append((t, r["nation"]))
        for row, lst in best.items():
            tmin = min(t for t, _ in lst)
            first[(s, row)] = [(tmin[0], tmin[1], n) for t, n in lst if t == tmin]
        cf = os.path.join(root, f"clock_s{s}.tsv")
        if os.path.exists(cf):
            for r in rd(cf):
                if r.get("kind") == "E":
                    try:
                        wt = float(r["worldTotalPerMonth"]); op = float(r["ownPerMonth"])
                        share_at[(s, r["row"], r["ownNation"])] = (int(r["year"]), int(r["quarter"]), op, op / wt if wt > 0 else float("nan"))
                    except (ValueError, KeyError):
                        pass
        for r in rd(os.path.join(root, f"engineer_s{s}.tsv")):
            if r["row"] in SPREAD or r["row"] in EXCLUDED:
                continue
            allrows.add(r["row"])
            if int(r["decade"]) <= 1780:
                d0rows.add(r["row"])
            if r["kind"] == "E":
                try:
                    aN[(s, r["row"], r["nation"])] = float(r["engineerPerMonth"])
                except ValueError:
                    pass
            if r["kind"] == "T":
                try:
                    if float(r["engineerPerMonth"]) > 0 and r["knownFlag"] == "0":
                        tech_by[(s, r["row"], int(r["year"]))].append(float(r["techTerm"]))
                except (ValueError, KeyError):
                    pass
            if r["kind"] == "T" and int(r["year"]) <= 1785 and r["knownFlag"] == "0" and int(r["decade"]) > 1780:   # d>0 の行だけ（d≦0 の行は技術力＝70 の定義どおりで時計の試験にならない）
                try:
                    tt = float(r["techTerm"])
                except ValueError:
                    continue
                t0n += 1
                if tt >= 55.0:
                    t0hits[r["nation"]] += 1
    # 行の全体は clock_s の T 行（代表の生産者が居る全ての非制度・非思想の行＝前提の行が開かない子の行も入る）の和集合へ広げる（engineer_s の T 行は前提の行がまだの国では出ない＝(428) 反証役 dd 中：分母が 34 行だった）
    for f in glob.glob(os.path.join(rows_dir or root, "clock_s*.tsv")):
        for r in rd(f):
            if r.get("kind") == "T" and r["row"] not in SPREAD and r["row"] not in EXCLUDED:
                allrows.add(r["row"])
    rows_seen = {row for (_, row) in first}
    # T1
    per1783, per1783_d0 = [], []
    for s in range(1, nseeds + 1):
        c = c0 = 0
        for (ss, row), lst in first.items():
            if ss == s and lst[0][0] == 1783:
                c += 1
        per1783.append(c)
    d0open = [sum(1 for (ss, row), lst in first.items() if ss == s and lst[0][0] == 1783 and row in d0rows) for s in range(1, nseeds + 1)]
    print(f"T1 1783 年中に開く行：種ごと {per1783}・平均 {statistics.mean(per1783):.2f}・最大 {max(per1783)}（狙い＝平均≦3・最大≦6）・うち d≦0 の行（{len(d0rows)} 本：{'・'.join(sorted(d0rows))}）種ごと {d0open}")
    # T2
    print("T2 最初の解禁の year の種の中央値（開かない種は ∞）：")
    t2ok = []
    for row in T2ROWS:
        ys = [first[(s, row)][0][0] if (s, row) in first else math.inf for s in range(1, nseeds + 1)]
        m = statistics.median(ys)
        print(f"   {row}\t{'開かない' if math.isinf(m) else f'{m:.1f}'}\t（開いた種 {sum(1 for y in ys if not math.isinf(y))}/{nseeds}・年 {sorted(y for y in ys if not math.isinf(y))}）")
    # T3, T4
    wnat = collections.Counter(); wtot = 0.0
    small_w = small_tot = 0.0; unseeded = 0.0
    below_am = known_a = unread = 0.0
    for (s, row), lst in sorted(first.items()):
        k = len(lst)
        med = statistics.median(pop0[s].values()) if pop0[s] else 0.0
        for (_, _, n) in lst:
            w = 1.0 / k
            wtot += w; wnat[n] += w
            if n in pop0[s]:
                small_tot += w
                if pop0[s][n] < med:
                    small_w += w
            else:
                unseeded += w
            a = aN.get((s, row, n))
            if a is None or a != a:
                unread += w
            else:
                known_a += w
                if a < am:
                    below_am += w
    print(f"T3 前半 最初の発見者の A_n が Am({am}) 未満の比：{below_am / known_a:.3f}（{below_am:.2f}/{known_a:.2f}・A_n が読めない件 {unread:.2f}）（狙い＜0.10）" if known_a else "T3 前半 A_n が読めない")
    print(f"T3 後半 小国が最初の発見者になる比：{small_w / small_tot:.3f}（{small_w:.2f}/{small_tot:.2f}・開幕に居ない国 {unseeded:.2f} 件は別）（狙い 0.05〜0.40）" if small_tot else "T3 後半 該当なし")
    top = wnat.most_common(8)
    print(f"T4 清の比：{wnat.get('清', 0.0) / wtot:.3f}（狙い≦0.20）・最大の 1 国：{top[0][0]} {top[0][1] / wtot:.3f}（狙い≦0.35）・上位 " + "・".join(f"{n}{c / wtot:.3f}" for n, c in top) + f"（重みの合計 {wtot:.2f}）")
    # T5
    never = sorted(r for r in allrows if r not in rows_seen)
    print(f"T5 どの種でも開かない行：{len(never)} 本（狙い≦8）→ {'通過' if len(never) <= 8 else '外れ'}：{'・'.join(never)}（行の全体 {len(allrows)} 本{'・--rows= から' if rows_dir else ''}）")
    if base:
        bseen = set()
        for s in range(1, nseeds + 1):
            fp = os.path.join(base, f"disc_s{s}.tsv")
            if os.path.exists(fp):
                for r in rd(fp):
                    if r["kind"] == "E" and r["row"] not in SPREAD and r["row"] not in EXCLUDED:
                        bseen.add(r["row"])
        bnever = sorted(r for r in allrows if r not in bseen)
        print(f"T5② 基準（{base}）の 10 種の和で開かない行：{len(bnever)} 本：{'・'.join(bnever)}")
        print(f"T5② 基準との差：増えた行 {'・'.join(sorted(set(never) - set(bnever))) or '-'}（{len(set(never) - set(bnever))} 本）・減った行 {'・'.join(sorted(set(bnever) - set(never))) or '-'}（{len(set(bnever) - set(never))} 本）")
    # R: 各行の最初の解禁の年の種の中央値
    print("R 行ごと\t開いた種\t最初の解禁の年の中央値（開いた種だけ）")
    for row in sorted(allrows):
        ys = [first[(s, row)][0][0] for s in range(1, nseeds + 1) if (s, row) in first]
        print(f"   {row}\t{len(ys)}\t{statistics.median(ys):.0f}" if ys else f"   {row}\t0\t-")
    # T2b：転炉鋼・機関車・汽船が 8 種以上で開き、年の中央値が 1900 年以前
    ok2b = True
    meds2b = []
    for row in ["転炉鋼", "機関車", "汽船"]:
        ys = [first[(s, row)][0][0] for s in range(1, nseeds + 1) if (s, row) in first]
        if len(ys) < 8:
            ok2b = False
        meds2b.append(statistics.median(ys) if ys else math.inf)
        print(f"T2b {row}：開いた種 {len(ys)}/{nseeds}（狙い≧8）・年の中央値 {statistics.median(ys) if ys else '開かない'}")
    big = statistics.median(meds2b)
    if not big <= 1900:
        ok2b = False
    print(f"T2b 3 行の最初の解禁の年の中央値の中央 {big}（狙い≦1900）→ {'通過' if ok2b else '外れ'}")
    # T7：自己強化の錠（clock_s の W 行を census と比べる）
    def wtraj(folder):
        out = {}
        for f in sorted(glob.glob(os.path.join(folder, "clock_s*.tsv"))):
            sd = os.path.basename(f)[len("clock_s"):-4]
            with open(f, encoding="utf-8") as fh:
                for r in csv.DictReader(fh, delimiter="	"):
                    if r.get("kind") == "W":
                        try:
                            out.setdefault(sd, {})[int(r["year"])] = float(r["worldTotalPerMonth"])
                        except (ValueError, KeyError):
                            pass
        return out
    wa = wtraj(root)
    if wa:
        yrs = sorted({y for t in wa.values() for y in t})
        wm = {y: statistics.median([t[y] for t in wa.values() if y in t]) for y in yrs}
        w85 = wm.get(1785, float("nan"))
        w85_note = "腕自身の 1785 年"
        if census:
            _wc = wtraj(census)
            _c85 = [t[1785] for t in _wc.values() if 1785 in t]
            if _c85:
                w85 = statistics.median(_c85); w85_note = "census の 1785 年（登録の W85）"
        print(f"T7 腕の W の最大÷W85 ＝ {max(wm.values()) / w85:.2f}（W85＝{w85:.5g}・{w85_note}・狙い≧10）")
        if census:
            wc = wtraj(census)
            cm = {y: statistics.median([t[y] for t in wc.values() if y in t]) for y in sorted({y for t in wc.values() for y in t})}
            for y in (1849, 1899):
                if y in wm and y in cm:
                    print(f"T7 {y} 年の W：腕 {wm[y]:.5g}・census {cm[y]:.5g}・比 {wm[y] / cm[y]:.3f}（狙い≧0.5）")
        else:
            print("T7 census の出力フォルダ（--census=）が無い＝比べられない")
    else:
        print("T7 clock_s が無い")
    # T8：最初の解禁の年の種をまたぐ幅
    narrow = total = 0
    for row in sorted(allrows):
        ys = sorted(first[(s, row)][0][0] for s in range(1, nseeds + 1) if (s, row) in first)
        if len(ys) < 4:
            continue
        q = statistics.quantiles(ys, n=4)
        total += 1
        if q[2] - q[0] < 2:
            narrow += 1
    print(f"T8 最初の解禁の年の IQR が 2 年未満の行：{narrow}/{total}（{narrow / total:.2f}・狙い≦0.75・名乗りの段）" if total else "T8 該当なし")
    t0ok = sum(t0hits.values()) <= 2 and all(v < 2 for v in t0hits.values())
    print(f"T0 1785 年以前の T 行（knownFlag＝0・{t0n} 組）で技術力≧55 の国×行：{sum(t0hits.values())} 件・国別 {t0hits.most_common(6)}（狙い≦2・1 国で 2 件以上を占めない）→ {'通過' if t0ok else '外れ'}")
    t1ok = statistics.mean(per1783) <= 3 and max(per1783) <= 6
    print(f"T1 合否 → {'通過' if t1ok else '外れ'}")
    print("T6 計算量はこの読み手の外（.cost・clock_s の C 行の秒・台帳の行 23 を手で読む）")
    # 記録だけ
    print("--- 記録だけ（採否に使わない）---")
    if wa:
        for y in (1785, 1825, 1865, 1905, 1945):
            vs = [t[y] for t in wa.values() if y in t]
            if len(vs) >= 2 and statistics.mean(vs) > 0:
                print(f"R-a {y} 年の W の種をまたぐ変動係数：{statistics.pstdev(vs) / statistics.mean(vs):.3f}（種 {len(vs)}・平均 {statistics.mean(vs):.5g}）")
    cvs = collections.defaultdict(list)
    for (s, row, y), vs in tech_by.items():
        if len(vs) >= 2 and statistics.mean(vs) > 0:
            cvs[y].append(statistics.pstdev(vs) / statistics.mean(vs))
    for y in sorted(cvs):
        if y in (1805, 1825, 1865, 1905, 1945) or y == min(cvs):
            print(f"R-b {y} 年・同じ行で A_n＞0 の国（計器の国だけ）の techTerm の変動係数の中央値：{statistics.median(cvs[y]):.3f}（行×種 {len(cvs[y])} 組）")
    shs = []; ans = []
    for (s, row), lst in first.items():
        for (_, _, n) in lst:
            e = share_at.get((s, row, n))
            if e and e[3] == e[3]:
                shs.append(e[3]); ans.append(e[2])
    if shs:
        q = statistics.quantiles(shs, n=4); qa = statistics.quantiles(ans, n=4)
        c_lo = sum(1 for a in ans if a / (a + am) < 0.9) / len(ans)
        print(f"R-c 最初の発見者の解禁時の A_n の四分位 {qa[0]:.1f}・{qa[1]:.1f}・{qa[2]:.1f}（Am＝{am}）・C＝A_n÷(A_n＋Am) が 0.9 未満の比 {c_lo:.3f}・世界の取り分（A_n÷W）の四分位 {q[0]:.4f}・{q[1]:.4f}・{q[2]:.4f}（{len(shs)} 件）")
    sm = lo = 0.0
    for (s, row), lst in first.items():
        gm = statistics.median(gdp0[s].values()) if gdp0[s] else None
        for (_, _, n) in lst:
            if gm is not None and n in gdp0[s]:
                sm += 1.0 / len(lst)
                if gdp0[s][n] < gm:
                    lo += 1.0 / len(lst)
    if sm:
        print(f"R-c' 最初の発見者の開幕の名目 GDP が種の中央値未満の比：{lo / sm:.3f}（{lo:.2f}/{sm:.2f}・人口でなく経済の大きさで取った小国の読み）")


if __name__ == "__main__":
    main()
