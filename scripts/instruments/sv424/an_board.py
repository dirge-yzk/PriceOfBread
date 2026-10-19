# (424) 線 sv：使用人の賄いの読み手（読むだけ）。計器つきの走りの board_s<種>.tsv と spend_s<種>.tsv から、事前登録 measurements/sv424_20261019_prereg.txt の [4] と追記2 [11] の
# 狙い T1〜T4・T6 を、挙げた国×窓（Q8-39／Q40-159／Q160-399）の種の中央として判定する。
# 使い方：PYTHONUTF8=1 python an_board.py <旗1の計器の出力(ins)> <旗0の計器の出力(ins)> [<出力ファイル>]
#   旗0の出力は T4（使用人÷人口の旗 0 比）と T3(b)・T6 の比べる側。基線 m420_ins（10 種）か、canary なら旗 0 の 1 種。種は board_s<種>.tsv の添字で突き合わせる（旗 1 の側にある種だけを読む）。
# 判定（事前登録のとおり。解釈を足した所は名乗る）：
#   T1：窓 Q40-159 の国の中央で、使用人の水準が 農民・工場労働者・職人の最小 以上 かつ 0.10 以上。8 国のうち 6 国以上。≤0.05 に張り付く国が 5 国以上＝働いていない。
#       過剰の目印（採否でなく切り分け）：使用人の水準が農民・工場労働者・職人の最大を上回る国が 4 国以上。
#   T2：(a) 使用人のいる国×四半期（Q8 以降）のうち boardPaidPerMonth>0 が 95% 以上（挙げた国の合算）／(b) 賄い÷賃金（boardPaid÷boardServantWage）の国の中央（窓 Q8-399・国ごとに種の中央→国をまたいで中央）が 1.0 以上 3.0 以下／
#       (c) 不足（owed−paid）÷owed が 20% 以下（Q8-399 の国×種の合計の中央）。
#   T3：(a) 窓 Q40-159 の国の中央で 使用人の主食の充足 − 農民・工場労働者の最小 を出し、国をまたいだ中央が 0 以上（解釈：事前登録は国×種の中央としか書いていない＝国の数も添える）／
#       (b) spend_s の使用人の日用品の割合（窓 Q40-159 の国の中央）の国をまたいだ中央が 10% 以上。
#   T4：窓 Q40-159 の使用人÷人口（spend_s の階級 12 の人数÷全階級の人数）の旗 1／旗 0 が国の中央で 0.5〜2.0 の国が 6 国以上（解釈：事前登録は〈国の中央〉としか書いていない）。
#   T6（記録）：貴族・資本家の貯蓄÷人数の旗 1／旗 0（board_s）。旗 0 側に board_s があるときだけ。
# 力学の値を 1 つも書かない（読み手）。バイトで書く（改行は LF）。
import csv, glob, os, re, statistics, sys, collections

src = sys.argv[1]
base = sys.argv[2]
outp = sys.argv[3] if len(sys.argv) > 3 else None
WINDOWS = {"Q8-39": (8, 39), "Q40-159": (40, 159), "Q160-399": (160, 399)}
BUCKETS = {"Q8-39": ["2"], "Q40-159": ["3"], "Q160-399": ["4", "5", "6", "7"]}   # spend_s の bucket（a0_servants.py と同じ）
SERVANT = 12

def fl(x):
    try:
        return float(x)
    except Exception:
        return float("nan")

def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float("nan")

def rows(path):
    with open(path, encoding="utf8", newline="") as f:
        return list(csv.DictReader(f, delimiter="\t"))

def seeds_of(d, prefix):
    out = {}
    for p in glob.glob(os.path.join(d, prefix + "_s*.tsv")):
        m = re.search(r"_s(\d+)\.tsv$", p)
        if m:
            out[int(m.group(1))] = p
    return out

def load_board(d, seeds):
    """(nation, window, col) -> {種: 窓の中央}／ (nation, seed) -> Q8 以降の行"""
    res = collections.defaultdict(dict)
    qrows = collections.defaultdict(list)
    bd = seeds_of(d, "board")
    for s in seeds:
        if s not in bd:
            continue
        by = collections.defaultdict(list)
        for r in rows(bd[s]):
            by[r["nation"]].append(r)
        for n, rr in by.items():
            if len(rr) < 100:   # 挙げた国だけ（他の国は年末の行しか無い）
                continue
            for w, (q0, q1) in WINDOWS.items():
                x = [r for r in rr if q0 <= int(r["quarter"]) <= q1]
                if len(x) < 0.9 * (q1 - q0 + 1):
                    continue
                for c in x[0].keys():
                    if c in ("quarter", "year", "nation"):
                        continue
                    res[(n, w, c)][s] = med([fl(r[c]) for r in x])
            qrows[(n, s)] = [r for r in rr if int(r["quarter"]) >= 8]
    return res, qrows

def load_spend(d, seeds):
    """(nation, window, metric) -> {種: 値}。metric＝servantShare（階級 12 の人数÷全階級）・servantDailyShare（使用人の支払いに占める日用品）"""
    res = collections.defaultdict(dict)
    sd = seeds_of(d, "spend")
    for s in seeds:
        if s not in sd:
            continue
        sp = rows(sd[s])
        for w, bks in BUCKETS.items():
            size = collections.defaultdict(float); paid = collections.defaultdict(float); daily = collections.defaultdict(float)
            for r in sp:
                if r["bucket"] not in bks or r["pickedFlag"] != "1":
                    continue
                n, c, mo = r["nation"], int(r["classId"]), fl(r["monthsCount"])
                if r["kind"] == "class":
                    size[(n, c)] += fl(r["qtyPerMonth"]) * mo
                    if c == SERVANT:
                        paid[n] += fl(r["payPerMonth"]) * mo
                elif r["kind"] == "good" and c == SERVANT and r["tier"] == "Daily":
                    daily[n] += fl(r["payPerMonth"]) * mo
            nations = {k[0] for k in size}
            for n in nations:
                tot = sum(v for (nn, c), v in size.items() if nn == n)
                if tot > 0 and size.get((n, SERVANT), 0) > 0:
                    res[(n, w, "servantShare")][s] = size[(n, SERVANT)] / tot
                if paid.get(n, 0) > 0:
                    res[(n, w, "servantDailyShare")][s] = daily.get(n, 0.0) / paid[n]
    return res

seeds = sorted(seeds_of(src, "board"))
if not seeds:
    sys.exit("board_s が無い: " + src)
B1, Q1 = load_board(src, seeds)
B0, Q0 = load_board(base, seeds) if glob.glob(os.path.join(base, "board_s*.tsv")) else ({}, {})
S1 = load_spend(src, seeds)
S0 = load_spend(base, seeds)
nations = sorted({k[0] for k in B1})
L = [f"# board_s の読み（旗 1＝{src}・旗 0＝{base}・種 {seeds}）"]

def g(res, n, w, c):
    return med(list(res.get((n, w, c), {}).values()))

# ---------------- T1
t1_ok = {}
for w in WINDOWS:
    L.append(f"## T1 水準：窓 {w}（国ごとに種の中央）")
    L.append("\t".join(["nation", "servantSat", "farmerSat", "workerSat", "artisanSat", "lowMin", "highMax", "T1(>=min&>=0.10)", "stuck<=0.05", "over(>max)"]))
    ok = stuck = over = 0
    for n in nations:
        s = g(B1, n, w, "servantSatisfactionShare")
        three = [g(B1, n, w, c) for c in ("farmerSatisfactionShare", "workerSatisfactionShare", "artisanSatisfactionShare")]
        sats = [x for x in three if x == x]
        lo, hi = (min(sats), max(sats)) if sats else (float("nan"), float("nan"))
        a = s == s and lo == lo and s >= lo and s >= 0.10
        st = s == s and s <= 0.05
        ov = s == s and hi == hi and s > hi
        ok += a; stuck += st; over += ov
        L.append("\t".join([n, f"{s:.3f}", *[f"{x:.3f}" for x in three], f"{lo:.3f}", f"{hi:.3f}", "OK" if a else "NG", "stuck" if st else "-", "over" if ov else "-"]))
    L.append(f"T1 を満たす国: {ok}/{len(nations)}・≤0.05 に張り付く国: {stuck}・過剰の国: {over}")
    t1_ok[w] = (ok, stuck, over)
T1 = t1_ok["Q40-159"][0] >= 6 and t1_ok["Q40-159"][1] < 5

# ---------------- T2
tot_q = pos_q = 0
ratio_n = []; short_n = []
L.append("## T2 機構が回ったか")
L.append("\t".join(["nation", "paidQuarters", "paid>0share", "boardPaid/wage(Q8-399 seed median)", "shortfall/owed(Q8-399 seed median)", "liveInShare(Q40-159)", "payerShareRich(Q40-159)"]))
for n in nations:
    per_seed_ratio = []; per_seed_short = []; a = b = 0
    for s in seeds:
        rr = Q1.get((n, s))
        if not rr:
            continue
        rs = [r for r in rr if fl(r["servantK"]) > 0]
        a += len(rs); b += sum(1 for r in rs if fl(r["boardPaidPerMonth"]) > 0)
        q = [fl(r["boardPaidPerMonth"]) / fl(r["boardServantWagePerMonth"]) for r in rs if fl(r["boardServantWagePerMonth"]) > 0]
        per_seed_ratio.append(med(q))
        owed = sum(fl(r["boardOwedPerMonth"]) for r in rs); sf = sum(fl(r["boardShortfallPerMonth"]) for r in rs)
        per_seed_short.append(sf / owed if owed > 0 else float("nan"))
    tot_q += a; pos_q += b
    ratio_n.append(med(per_seed_ratio)); short_n.append(med(per_seed_short))
    L.append("\t".join([n, f"{b}/{a}", f"{(b / a if a else float('nan')):.3f}", f"{ratio_n[-1]:.3f}", f"{short_n[-1]:.3f}", f"{g(B1, n, 'Q40-159', 'boardLiveInShare'):.3f}", f"{g(B1, n, 'Q40-159', 'boardPayerShareRich'):.3f}"]))
t2a = tot_q > 0 and pos_q / tot_q >= 0.95
r_all = med(ratio_n); t2b = r_all == r_all and 1.0 <= r_all <= 3.0
s_all = med(short_n); t2c = s_all == s_all and s_all <= 0.20
L.append(f"T2(a) 賄いが走った四半期 {pos_q}/{tot_q}={(pos_q / tot_q if tot_q else float('nan')):.3f} （>=0.95）→ {'OK' if t2a else 'NG'}")
L.append(f"T2(b) 賄い÷賃金の国の中央 {r_all:.3f} （1.0〜3.0）→ {'OK' if t2b else 'NG'}")
L.append(f"T2(c) 不足÷出すべき額の国の中央 {s_all:.3f} （<=0.20）→ {'OK' if t2c else 'NG'}")
T2 = t2a and t2b and t2c

# ---------------- T3
L.append("## T3 使用人の暮らし（窓 Q40-159）")
L.append("\t".join(["nation", "servantFoodFill", "farmerFoodFill", "workerFoodFill", "servantFood-lowMin", "servantDailyShare(spend_s)"]))
diffs = []; dn = 0; dailies = []
for n in nations:
    sf = g(B1, n, "Q40-159", "servantFoodFillShare"); fa = g(B1, n, "Q40-159", "farmerFoodFillShare"); wo = g(B1, n, "Q40-159", "workerFoodFillShare")
    lo = min(x for x in (fa, wo) if x == x) if any(x == x for x in (fa, wo)) else float("nan")
    d = sf - lo if sf == sf and lo == lo else float("nan")
    diffs.append(d); dn += d == d and d >= 0
    dsh = g(S1, n, "Q40-159", "servantDailyShare"); dailies.append(dsh)
    L.append("\t".join([n, f"{sf:.3f}", f"{fa:.3f}", f"{wo:.3f}", f"{d:.3f}", f"{dsh:.4f}"]))
d_med = med(diffs); dl_med = med(dailies)
t3a = d_med == d_med and d_med >= 0
t3b = dl_med == dl_med and dl_med >= 0.10
L.append(f"T3(a) 使用人の主食の充足−農民・工場労働者の最小の国の中央 {d_med:.3f} （>=0）・国の数 {dn}/{len(nations)} → {'OK' if t3a else 'NG'}")
L.append(f"T3(b) 使用人の日用品の割合の国の中央 {dl_med:.4f} （>=0.10）→ {'OK' if t3b else 'NG'}")
T3 = t3a and t3b

# ---------------- T4
L.append("## T4 暴走しない（spend_s の使用人÷人口の旗 1／旗 0）")
L.append("\t".join(["nation", *[f"{w}:flag1/flag0 (share1,share0)" for w in WINDOWS]]))
in_range = 0
for n in nations:
    cells = []
    for w in WINDOWS:
        a, b = g(S1, n, w, "servantShare"), g(S0, n, w, "servantShare")
        r = a / b if b == b and b > 0 else float("nan")
        cells.append(f"{r:.2f} ({a:.4f},{b:.4f})")
        if w == "Q40-159" and r == r and 0.5 <= r <= 2.0:
            in_range += 1
    L.append("\t".join([n, *cells]))
T4 = in_range >= 6
L.append(f"T4 窓 Q40-159 で 0.5〜2.0 倍の国: {in_range}/{len(nations)} （>=6）→ {'OK' if T4 else 'NG'}")

# ---------------- T6（記録）
if B0:
    L.append("## T6（記録）貴族・資本家の貯蓄÷人数の旗 1／旗 0")
    for n in nations:
        cells = []
        for w in WINDOWS:
            a, b = g(B1, n, w, "employerSavingsPerHeadStock"), g(B0, n, w, "employerSavingsPerHeadStock")
            cells.append(f"{w}:{(a / b if b else float('nan')):.3f}")
        L.append(n + "\t" + "\t".join(cells))

L.append("## 判定（働いた＝T1・T2・T3・T4 の全て）")
L.append(f"T1={'OK' if T1 else 'NG'} T2={'OK' if T2 else 'NG'} T3={'OK' if T3 else 'NG'} T4={'OK' if T4 else 'NG'} → {'働いた' if T1 and T2 and T3 and T4 else '働いていない（採らない）'}")
text = "\n".join(L) + "\n"
if outp:
    with open(outp, "wb") as f:
        f.write(text.encode("utf8"))
print(text)
