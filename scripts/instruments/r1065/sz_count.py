# (1065) c101sz 州の兵士 POP が 0 の数え（読むだけ・腕 0）。呼び方：python -X utf8 -I sz_count.py <生のフォルダ> <種の始め> <種の終わり> <出力> <組の名> <hit の一覧 or ->
# 読む表：cpop_s（植民地格の州の POP の行＝兵士・農民・工場労働者だけを拾う）・region_s（全州の格と人口・出どころの分類）・rule_s（州の支配の 4 区分＝RuleDelivery が 0 か）・nation_s（徴兵法）
# 定義（事前登録 prereg.txt）：兵 0＝cpop_s の兵士の popK が厳密に 0。州×年＝quarter%4==3 の行。
# 出どころ：I＝quarter 0 に同じ国の下で在る州／P＝途中で初めて現れた州名／C＝前に別の国の下に在った州名（Cn＝受け手が quarter 0 に居ない国＝建国・内戦の後の国）／T＝同じ国の下で格が他から変わった
# 限界（台本が自分で申告する）：①cpop_s は格が Colonial の州だけ＝本国格・統合格の兵は読めない ②州名で州を同定（同名の州の吸収は 1 本の系列）③rule_s の行が無い州×四半期は RuleDelivery を〈不明〉
#   ④RuleDelivery は ruleState＋ruleLocal＝0 のときだけ 0 と読む（AdminFill は読まない＝0 か否かには効かない）⑤四半期の末の 1 時点
import sys, os
from collections import defaultdict, Counter

RAW, S0, S1, OUT, TAG, HITS = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5], sys.argv[6]
CLS = {"兵士".encode(): "S", "農民".encode(): "A", "工場労働者".encode(): "B"}
KEYS = [b"\t" + k + b"\t" for k in CLS]

def hdr(f):
    return f.readline().decode("utf-8").rstrip("\r\n").split("\t")

def lawname(v):
    return {"0": "志願", "1": "選抜", "2": "皆兵"}.get(v, "不明")

def poolbin(x):
    if x != x: return "不明"
    if x == 0: return "0"
    if x < 0.1: return "<0.1K"
    if x < 10: return "0.1-10K"
    return ">=10K"

tot = Counter()          # (区分の名, 値) → 州×年
zero = Counter()
zeroTiny = Counter()
trans = Counter()        # 遷移の種類
runs = Counter()         # 0 の続き（四半期）の長さの区分 × 法
q0 = defaultdict(lambda: [0, 0])   # 国 → [quarter 0 の植民地格の州, その中の兵 0]
natZero = Counter(); natTot = Counter()
neverLeft = Counter()    # 兵 0 の州×年のうち、その系列（国×州）で一度も兵＞0 にならなかった物
hitRows = []
hits = defaultdict(list)
if HITS != "-" and os.path.exists(HITS):
    f = False
    for line in open(HITS, encoding="utf-8"):
        if line.startswith("# A1 で 0 に届く州の一覧"): f = True; continue
        if f and line.strip():
            s, n, r, y, t = line.rstrip("\n").split("\t"); hits[int(s)].append((n, r, int(y), t))
seedsRead = 0
for seed in range(S0, S1 + 1):
    cp = os.path.join(RAW, f"cpop_s{seed}.tsv")
    have = os.path.exists(cp)
    # 徴兵法
    law = {}
    with open(os.path.join(RAW, f"nation_s{seed}.tsv"), "rb") as f:
        h = hdr(f); iq, ino, il = h.index("quarter"), h.index("nation"), h.index("conscriptionLaw")
        for line in f:
            t = line.decode("utf-8").rstrip("\r\n").split("\t"); law[(int(t[iq]), t[ino])] = t[il]
    nat0 = {n for (q, n) in law if q == 0}
    # 州の格と出どころ
    first = {}   # 州名 → (最初の四半期, 国)
    owners = defaultdict(list)   # 州名 → [(q, 国, 格)]
    with open(os.path.join(RAW, f"region_s{seed}.tsv"), "rb") as f:
        h = hdr(f); iq, ino, ire, iti = h.index("quarter"), h.index("nation"), h.index("region"), h.index("tier")
        for line in f:
            t = line.decode("utf-8").rstrip("\r\n").split("\t")
            owners[t[ire]].append((int(t[iq]), t[ino], t[iti]))
    origin = {}   # (国, 州, q) → 出どころ（その国の下での系列の始まりで決める）
    seriesStart = {}
    for reg, lst in owners.items():
        lst.sort()
        prev = None
        for (q, n, ti) in lst:
            if prev is None:
                o = "I" if q == 0 else "P"
            elif prev[1] != n:
                o = "C" if n in nat0 else "Cn"
            elif prev[2] != ti and ti == "Colonial" and seriesStart[(n, reg)][1] != "T":
                o = "T"
            else:
                o = seriesStart[(n, reg)][1]
            if prev is None or prev[1] != n: seriesStart[(n, reg)] = (q, o)
            elif o == "T" and seriesStart[(n, reg)][1] != "T": seriesStart[(n, reg)] = (q, "T")
            origin[(n, reg, q)] = o
            prev = (q, n, ti)
    # 支配の区分
    rd0 = {}
    rp = os.path.join(RAW, f"rule_s{seed}.tsv")
    if os.path.exists(rp):
        with open(rp, "rb") as f:
            h = hdr(f); iq, ino, ire, ist, ilo = h.index("quarter"), h.index("nation"), h.index("region"), h.index("ruleStateShare"), h.index("ruleLocalShare")
            for line in f:
                t = line.decode("utf-8").rstrip("\r\n").split("\t")
                try: rd0[(int(t[iq]), t[ino], t[ire])] = (float(t[ist]) + float(t[ilo])) <= 0
                except ValueError: pass
    if not have:
        # a11 など：分類の母数だけ（植民地格の州×年）
        for reg, lst in owners.items():
            for (q, n, ti) in lst:
                if q % 4 == 3 and ti == "Colonial":
                    tot[("母数のみ・出どころ", origin[(n, reg, q)])] += 1
                    tot[("母数のみ・法", lawname(law.get((q, n), "")))] += 1
        continue
    seedsRead += 1
    S = {}; A = defaultdict(float)
    with open(cp, "rb") as f:
        f.readline()
        for line in f:
            if not any(k in line for k in KEYS): continue
            t = line.split(b"\t")
            c = CLS.get(t[6])
            if c is None: continue
            key = (int(t[0]), t[2].decode("utf-8"), t[3].decode("utf-8"))
            v = float(t[8])
            if c == "S": S[key] = v
            else: A[key] += v
    ser = defaultdict(list)
    for (q, n, r), v in S.items(): ser[(n, r)].append((q, v))
    for (n, r), lst in ser.items():
        lst.sort()
        everPos = any(v > 0 for _, v in lst)
        prevq, prevv = None, None
        run, runLaw = 0, set()
        for (q, v) in lst:
            L = law.get((q, n), "")
            if q == 0:
                q0[n][0] += 1; q0[n][1] += 1 if v == 0 else 0
            if prevq is not None and q == prevq + 1:
                L0 = law.get((prevq, n), "")
                if prevv == 0 and v > 0: trans[("0→正", lawname(L0) + "→" + lawname(L))] += 1
                if prevv > 0 and v == 0: trans[("正→0", lawname(L0) + "→" + lawname(L))] += 1
            elif prevq is not None:
                trans[("系列の切れ目", "")] += 1
            if v == 0 and prevq is not None and q == prevq + 1 and prevv == 0:
                run += 1; runLaw.add(L)
            else:
                if run: runs[(("1-4" if run <= 4 else "5-40" if run <= 40 else ">40"), "+".join(sorted(lawname(x) for x in runLaw)))] += 1
                run, runLaw = (1, {L}) if v == 0 else (0, set())
            prevq, prevv = q, v
            if q % 4 != 3: continue
            o = origin.get((n, r, q), "?")
            pool = A.get((q, n, r), float("nan"))
            rd = rd0.get((q, n, r))
            rdn = "不明" if rd is None else ("0" if rd else "正")
            dims = [("全体", ""), ("出どころ", o), ("法", lawname(L)), ("徴募元の人数", poolbin(pool)), ("RuleDelivery", rdn),
                    ("出どころ×法", o + "/" + lawname(L))]
            for d in dims:
                tot[d] += 1
                if v == 0: zero[d] += 1
                if v < 0.001: zeroTiny[d] += 1
            natTot[n] += 1
            if v == 0:
                natZero[n] += 1
                if not everPos: neverLeft[("一度も正にならない系列", "")] += 1
                else: neverLeft[("正の時期がある系列", "")] += 1
        if run: runs[(("1-4" if run <= 4 else "5-40" if run <= 40 else ">40"), "+".join(sorted(lawname(x) for x in runLaw)))] += 1
    # 問い 3：A1 の hit の前の兵
    for (n, r, y, ti) in hits.get(seed, []):
        qh = (y - 1783) * 4
        pre = [S.get((q, n, r)) for q in range(qh - 12, qh)]
        preY = [S.get((q, n, r)) for q in range(qh - 12, qh + 4)]
        got = [x for x in pre if x is not None]
        gotY = [x for x in preY if x is not None]
        kind = "表に無い" if not got else ("前の12四半期すべて0" if len(got) == 12 and all(x == 0 for x in got) else ("一部0" if any(x == 0 for x in got) else "兵あり"))
        kindY = "表に無い" if not gotY else ("y-3〜y の全て0" if all(x == 0 for x in gotY) else "それ以外")
        pool = A.get((qh - 1, n, r), float("nan"))
        hitRows.append((seed, n, r, y, ti, kind, kindY, len(got), lawname(law.get((qh - 1, n), "")), poolbin(pool), origin.get((n, r, qh - 1), "?")))

with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1065) 兵 0 の数え 組={TAG} 生={RAW} 種={S0}〜{S1} cpop_s を読めた種={seedsRead}\n")
    o.write("# 州×年＝cpop_s の兵士の行のうち quarter%4==3（植民地格の州だけ）。兵0＝popK==0・兵<1人＝popK<0.001\n")
    o.write("区分\t値\t州×年\t兵0\t兵0の割合\t兵<1人\n")
    for d in sorted(tot):
        n = tot[d]
        o.write(f"{d[0]}\t{d[1]}\t{n}\t{zero[d]}\t{(zero[d] / n if n else float('nan')):.4f}\t{zeroTiny[d]}\n")
    o.write("# 兵 0 の州×年の系列（国×州）の種類\n")
    for k, v in sorted(neverLeft.items()): o.write(f"{k[0]}\t{v}\n")
    o.write("# 四半期ごとの遷移（同じ国の下の続く四半期）＝(種類, 前の四半期の法→その四半期の法)\n")
    for k, v in sorted(trans.items()): o.write(f"{k[0]}\t{k[1]}\t{v}\n")
    o.write("# 兵 0 の続き（四半期の数）×その間の法\n")
    for k, v in sorted(runs.items()): o.write(f"{k[0]}\t{k[1]}\t{v}\n")
    o.write("# quarter 0 の植民地格の州（国の和・種の和）：州・その中の兵0\n")
    for n, (a, b) in sorted(q0.items(), key=lambda z: -z[1][0]): o.write(f"{n}\t{a}\t{b}\n")
    o.write("# 国ごとの兵 0 の州×年（上位 15）：兵0／州×年\n")
    for n, c in natZero.most_common(15): o.write(f"{n}\t{c}\t{natTot[n]}\n")
    if hitRows:
        o.write("# 問い3：A1 で 0 に届く州（r1059 の一覧）の、届く前の兵（cpop_s）\n")
        c1 = Counter(x[5] for x in hitRows); c2 = Counter(x[6] for x in hitRows)
        o.write("前の12四半期\t" + "\t".join(f"{k}={v}" for k, v in sorted(c1.items())) + f"\t計={len(hitRows)}\n")
        o.write("y-3〜y\t" + "\t".join(f"{k}={v}" for k, v in sorted(c2.items())) + "\n")
        c3 = Counter((x[5], x[8]) for x in hitRows); c4 = Counter((x[5], x[9]) for x in hitRows); c5 = Counter((x[5], x[10]) for x in hitRows)
        o.write("×法\t" + "\t".join(f"{a}/{b}={v}" for (a, b), v in sorted(c3.items())) + "\n")
        o.write("×徴募元\t" + "\t".join(f"{a}/{b}={v}" for (a, b), v in sorted(c4.items())) + "\n")
        o.write("×出どころ\t" + "\t".join(f"{a}/{b}={v}" for (a, b), v in sorted(c5.items())) + "\n")
        c6 = Counter((x[1], x[5]) for x in hitRows)
        o.write("×国\t" + "\t".join(f"{a}/{b}={v}" for (a, b), v in sorted(c6.items(), key=lambda z: -z[1])[:16]) + "\n")
