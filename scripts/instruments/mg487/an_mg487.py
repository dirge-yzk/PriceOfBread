# (487) 線 mg2 の判定の読み手：事前登録 measurements/mg487_20261021_prereg.txt の [8] と追記 3 の GD1・GD2・G2（と記録）。
# 使い方：PYTHONUTF8=1 python an_mg487.py <B の ins フォルダ> <m484 の ins フォルダ> [種の数=10]
#   B＝データ（農奴制の初期値）を直した木＋旗 SerfGateCoversMigration=1 の計器つき 10 種（nation_s・mgdom_s・mg_s・lawgate_s・region_s を持つ）。比べる側＝基準 m484 の計器つき 10 種（mgdom_s と mg_s は持たない）。
# 読むだけ。判定は 7 種以上＝当たり・4〜6＝部分・3 以下＝外れ。判定の式は腕の前に固定した（追記 3）。各線の〈弱い線〉の注は事前登録に書いた。標準ライブラリだけ。
import csv, sys, collections

if hasattr(sys.stdout, "reconfigure"): sys.stdout.reconfigure(encoding="utf-8")
B = sys.argv[1]; A = sys.argv[2]; NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
SEEDS = list(range(1, NS + 1))
Q_END = 667
P = {"P1": (0, 67), "P2": (68, 267), "P3": (268, 467), "P4": (468, 667)}

# 手で書いた期待の表（事前登録 [3]・番人 SerfInitialDataTests と同じ・世界データから作らない）。表に無い国は全面。段の符号は nation_s の serfEmancipation（0＝なし・1＝限定・2＝全面）。
NONE = ["ロシア", "プロイセン", "ポーランド"]
LIMITED = ["フランス", "日本", "オスマン", "オーストリア", "スペイン", "サルデーニャ＝ピエモンテ", "ポルトガル",
           "デンマーク", "ライン諸邦", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク",
           "両シチリア", "教皇領", "ビルマ", "シャム", "カンボジア", "ラオス", "エチオピア"]
FULL_EXPLICIT_OLD = ["イギリス", "アメリカ", "ネーデルラント", "スウェーデン"]
LIMITED_OLD = ["フランス", "オスマン", "オーストリア", "スペイン", "サルデーニャ＝ピエモンテ", "ポルトガル"]
OLD_EXPLICIT = set(FULL_EXPLICIT_OLD + LIMITED_OLD)   # m484 の木で明示されていた 10 国（残りの 51 国は既定のなし）
FOUNDED_PARENT = {"セイロン共和国": "ネーデルラント", "バタヴィア共和国": "ネーデルラント"}   # 建国の国は親の値を継ぐ（親の値を明示して渡す＝MultiNationSim の建国）


def expect(name):
    return 0 if name in NONE else (1 if name in LIMITED else 2)


def rows(path):
    with open(path, encoding="utf-8") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            yield r


def fl(x):
    try: return float(x)
    except Exception: return float("nan")


def verdict(n):
    return "当たり" if n >= 7 else ("部分" if n >= 4 else "外れ")


res = collections.defaultdict(list)       # 線 -> 種ごとの真偽（None＝その種では対象なしで除く）
detail = collections.defaultdict(dict)
rec = collections.defaultdict(dict)       # 記録（採否に使わない）

CHANGED_LIMITED = [n for n in LIMITED if n not in OLD_EXPLICIT]   # 限定のうち m484 の木で明示でなかった国（なし→限定）


def nation_levels(base, sd):
    """国×四半期の (serfEmancipation, conscriptionLaw)"""
    lv = {}
    for r in rows(f"{base}/nation_s{sd}.tsv"):
        lv[(r["nation"], int(r["quarter"]))] = (int(fl(r["serfEmancipation"])), int(fl(r["conscriptionLaw"])))
    return lv


def region_out(base, sd):
    """国×四半期の (越境の出, 越境の入, 人口)"""
    out = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
    for r in rows(f"{base}/region_s{sd}.tsv"):
        a = out[(r["nation"], int(r["quarter"]))]
        a[0] += fl(r["crossBorderOutKPerQuarter"]); a[1] += fl(r["crossBorderInKPerQuarter"]); a[2] += fl(r["populationK"])
    return out


def rate(tot, nation, p, idx=0):
    a, b = P[p]; s = pop = 0.0
    for q in range(a, b + 1):
        v = tot.get((nation, q))
        if v: s += v[idx]; pop += v[2]
    return s / pop * 4 * 100 if pop > 0 else 0.0


def urban_home(base, sd):
    """lawgate_s の urbanHome：国→{四半期: 値}（挙げた国以外は年末の断面だけ＝最初の行は Q3）"""
    d = collections.defaultdict(dict)
    for r in rows(f"{base}/lawgate_s{sd}.tsv"): d[r["nation"]][int(r["quarter"])] = fl(r["urbanHome"])
    return d


for sd in SEEDS:
    lvB = nation_levels(B, sd); lvA = nation_levels(A, sd)
    q0B = {n: v for (n, q), v in lvB.items() if q == 0}
    q0A = {n: v for (n, q), v in lvA.items() if q == 0}
    # ---------------- GD1（データが入った）：Q0 の段が表と一致・下がった国は 0・建国の国は親の値を継ぐ ----------------
    base_names = [n for n in q0B if n not in FOUNDED_PARENT]
    wrong_up = []; wrong_down = []
    for n in base_names:
        e = expect(n); g = q0B[n][0]
        if g > e: wrong_up.append((n, e, g))
        elif g < e: wrong_down.append((n, e, g))
    founded = sorted({n for (n, q) in lvB if n not in q0B})
    founded_bad = []
    for n in founded:
        if n in FOUNDED_PARENT:
            qs = sorted(q for (m, q) in lvB if m == n)
            first = lvB[(n, qs[0])][0]; par = FOUNDED_PARENT[n]
            pq = lvB.get((par, qs[0]))
            if pq is not None and first != pq[0]: founded_bad.append((n, first, pq[0]))
    n_equal = sum(1 for n in base_names if q0B[n][0] == expect(n))
    res["GD1"].append(len(wrong_down) == 0 and not founded_bad)
    detail["GD1"][sd] = (f"表と一致 {n_equal}/{len(base_names)} 国 ・上へずれ(助走の間に法が進んだ) {len(wrong_up)} 国 {wrong_up[:6]} ・下へずれ {len(wrong_down)} 国 {wrong_down[:4]} ・"
                         f"Q0 に居ない国(建国など) {len(founded)} 国 {founded[:5]} ・親と食い違う建国 {founded_bad}")
    # 全面の国の段が下がった回数（逆行・記録）
    rev = 0
    for n in [n for n in base_names if expect(n) == 2]:
        prev = None
        for q in range(0, Q_END + 1):
            v = lvB.get((n, q))
            if v is None: continue
            if prev is not None and v[0] < prev: rev += 1
            prev = v[0]
    rec["全面の国の段の逆行の回数(B)"][sd] = rev

    # 記録：農奴解放の段が上がった回数（全国）と国民皆兵の法の段の分布（Q0・Q67・Q267）
    def ups(lv):
        cnt = 0; last = {}
        for (n, q) in sorted(lv, key=lambda k: (k[0], k[1])):
            v = lv[(n, q)][0]
            if n in last and v > last[n]: cnt += 1
            last[n] = v
        return cnt

    def dist(lv, q):
        c = collections.Counter(v[1] for (n, qq), v in lv.items() if qq == q)
        return " ".join(f"{k}:{c[k]}" for k in sorted(c))
    rec["農奴解放の段が上がった回数(全国)"][sd] = f"B {ups(lvB)} / m484 {ups(lvA)}"
    rec["国民皆兵の法の段の分布(Q0/Q67/Q267)"][sd] = f"B [{dist(lvB,0)}] [{dist(lvB,67)}] [{dist(lvB,267)}] / m484 [{dist(lvA,0)}] [{dist(lvA,67)}] [{dist(lvA,267)}]"
    rec["国の数(Q0/Q667)"][sd] = f"B {len(q0B)}/{sum(1 for (n,q) in lvB if q==Q_END)} ・m484 {len(q0A)}/{sum(1 for (n,q) in lvA if q==Q_END)}"
    rec["建国の国(B)"][sd] = f"{len(founded)} 国 {founded[:8]}"
    # ---------------- GD2（農村→都市の門が開いた）：表で なし→限定／全面 になった 48 国の urbanHome の増分（最初の行→Q67）----------------
    uB = urban_home(B, sd); uA = urban_home(A, sd)
    full_changed = [n for n in base_names if expect(n) == 2 and n not in OLD_EXPLICIT]
    c48 = CHANGED_LIMITED + full_changed
    bigger = tot = 0
    for n in c48:
        if n in uB and n in uA and uB[n] and uA[n]:
            qb0 = min(uB[n]); qa0 = min(uA[n])
            if 67 in uB[n] and 67 in uA[n]:
                dB = uB[n][67] - uB[n][qb0]; dA = uA[n][67] - uA[n][qa0]
                tot += 1; bigger += dB > dA
    res["GD2"].append(tot > 0 and bigger > tot / 2)
    detail["GD2"][sd] = f"増分が m484 より大きい国 {bigger}/{tot}（表の対象 {len(c48)} 国）"
    # ---------------- G2（農奴の門が働いた）----------------
    strict_bad = 0; blocked_nat = collections.defaultdict(float)
    for r in rows(f"{B}/mg_s{sd}.tsv"):
        if r["srcNation"] == "-1": continue
        if fl(r["serfFactor"]) == 0 and r["cls"] == "1" and fl(r["postCapK"]) > 0: strict_bad += 1
        blocked_nat[r["srcNation"]] += fl(r["blockedBySerfK"])
    dom_bad = 0; by_nat = collections.defaultdict(list)
    for r in rows(f"{B}/mgdom_s{sd}.tsv"):
        q = int(r["quarter"]); f = fl(r["serfFactorIfGate"]); o = fl(r["ruralOutK"])
        if f == 0 and o > 0: dom_bad += 1
        by_nat[r["nation"]].append((q, f, o))
    after_pos = after_tot = 0
    for n, lst in by_nat.items():
        lst.sort()
        zero_q = [q for q, f, o in lst if f == 0]
        if not zero_q: continue
        qopen = next((q for q, f, o in lst if f > 0 and q > zero_q[0]), None)
        if qopen is None: continue
        after_tot += 1
        if sum(o for q, f, o in lst if qopen <= q < qopen + 40) > 0: after_pos += 1
    gate_engaged = [n for n in NONE if blocked_nat.get(n, 0) > 0]
    res["G2"].append(strict_bad == 0 and dom_bad == 0 and (after_tot == 0 or after_pos == after_tot) and len(gate_engaged) >= 1)
    detail["G2"][sd] = f"厳密側の違反(越境) {strict_bad} ・(国内) {dom_bad} ・門が止めた国 {gate_engaged} ・0→正の国 {after_tot} のうち後の 10 年に農村の出が正 {after_pos}"
    # 弱い線 G2x（国をまたいだ比べ）：ロシア・プロイセンの serfFactor＝0 の四半期の越境の出の合計が m484 の同じ四半期より小さい
    roB = region_out(B, sd); roA = region_out(A, sd)
    sB = sA = 0.0
    for n in ("ロシア", "プロイセン"):
        for q in {q for q, f, o in by_nat.get(n, []) if f == 0}:
            sB += roB.get((n, q), [0, 0, 0])[0]; sA += roA.get((n, q), [0, 0, 0])[0]
    res["G2x"].append(sB < sA if sA > 0 else None)
    detail["G2x"][sd] = f"ロシア・プロイセンの段 0 の四半期の越境の出(千) B {sB:.0f} / m484 {sA:.0f}（弱い線＝世界が別）"
    # ---------------- 記録：英・米・清・露・アイルランドの国外へ（年率 %）----------------
    for nat in ("イギリス", "アメリカ", "清", "ロシア", "アイルランド"):
        rec[f"{nat}の国外へ(年率%) P2/P3"][sd] = f"B {rate(roB,nat,'P2'):.3f}/{rate(roB,nat,'P3'):.3f} ・m484 {rate(roA,nat,'P2'):.3f}/{rate(roA,nat,'P3'):.3f}"
    sr = su = 0.0
    for r in rows(f"{B}/mgdom_s{sd}.tsv"): sr += fl(r["ruralOutK"]); su += fl(r["urbanOutK"])
    rec["国内の移民の農村の出／都市の出(千・全期・B)"][sd] = f"{sr:.0f} / {su:.0f}"

print("# (487) 判定（B の計器つき 10 種 × m484）")
for g, name in (("GD1", "GD1 データが入った(下へずれ 0・建国は親を継ぐ)"), ("GD2", "GD2 農村→都市の門が開いた(増分が m484 より大)"),
                ("G2", "G2 農奴の門が働いた(厳密＋0→正＋門が止めた国)"), ("G2x", "G2x 国をまたいだ比べ(弱い線)")):
    vals = [x for x in res[g] if x is not None]
    n = sum(1 for x in vals if x)
    print(f"{name}: {n}/{len(vals)} 種 → {verdict(n)}  {''.join('o' if x else ('.' if x is not None else '-') for x in res[g])}")
print()
for g in ("GD1", "GD2", "G2", "G2x"):
    print(f"[{g}]")
    for sd in SEEDS: print(f"  種{sd}: {detail[g].get(sd, '')}")
print()
print("# 記録（採否に使わない）")
for k, d in rec.items():
    print(f"[{k}]")
    for sd in SEEDS: print(f"  種{sd}: {d.get(sd, '')}")
