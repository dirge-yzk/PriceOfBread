# (867) c72gt 持ち主が分かれた州の振り分け（読むだけ・事前登録 measurements/m867_20261106_prereg_gt.txt [4]）。holclass839.py は書き換えない（その上に区分を足す）。
#   python -X utf8 -I holsplit867.py <腕の an_h2 -v4 の出力> <腕の ins> <出力> --null <M′の an> <M′の ins> --null <N1 の an> <N1 の ins> --null <N2 の an> <N2 の ins> [--mode peak|late|nocmp]
#   区分：L0 例外の名簿・L1 元からの弱さ（基線かヌル腕の同じ種・同じ州が 1/4 未満）・L2 軌道の分かれの型（記録）・L3 線が原因（止める線）。
#   L2＝腕でピークの時点（腕の行のピークの年の最初の四半期）の持ち主 ≠ 腕で最初の行の持ち主 かつ、同じ種・同じ州でヌル 3 本のその同じ四半期の持ち主が 3 本とも同じではない。
#   ヌルに行が無い州は割れと数えず L3 のまま（件数を名乗る）。--mode late/nocmp は突然変異の口（late＝ピークでなく最後の四半期で読む・nocmp＝ヌルの割れの条件を外す）。
#   名乗る限界：ヌルどうしで持ち主が割れる州では、線の効きと雑音を分けられない。L2 は〈ヌルの幅の内で説明がつく〉の意味に限る（線の寄与が 0 と言えるのではない）。
#   VERDICT：pass＝例外の名簿を除く 1/10 の行のうち L3 が 0 件。
import sys, re, os, collections
pat = re.compile(r'^\s*\[([^\]・]+)・(1/10|1/4)(・例外の名簿)?\]\s+s(\d+)\s+(\S+?)（')
pk = re.compile(r'\s([0-9.]+)\((\d{4})\)→([0-9.]+)\((\d{4})\)')

def cases(path):
    out = []
    for line in open(path, encoding="utf-8", errors="replace"):
        m = pat.match(line)
        if not m: continue
        k = pk.search(line)
        out.append(dict(level=m.group(2), exempt=m.group(3) is not None, seed=int(m.group(4)), region=m.group(5), cause=m.group(1),
                        peak_year=int(k.group(2)) if k else None, text=line.strip()[:200]))
    return out

def read_owner(ins, seed, region):
    """region_s<seed>.tsv から州の (四半期 → (年, nationIndex)) を返す。無ければ None。"""
    p = os.path.join(ins, "region_s%d.tsv" % seed)
    if not os.path.exists(p): return None
    D = {}
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\r\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        iq, iy, ir, ii = ix["quarter"], ix["year"], ix["region"], ix["nationIndex"]
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            if c[ir] == region: D[int(c[iq])] = (int(c[iy]), c[ii])
    return D

def owner_at(D, q):
    return None if D is None or q not in D else D[q][1]

def classify(arm_path, arm_ins, nulls, mode="peak"):
    """nulls＝[(an の出力, ins), ...]（3 本）。戻り：行ごとの (区分, 理由, 行の文字列) の一覧と数え。"""
    base14 = set()
    for an, _ in nulls:
        for c in cases(an): base14.add((c["seed"], c["region"]))
    res = []; C = collections.Counter()
    for c in cases(arm_path):
        if c["level"] != "1/10": continue
        C["1/10 の行"] += 1
        if c["exempt"]: lab, why = "L0", "例外の名簿（件数の外）"
        elif (c["seed"], c["region"]) in base14: lab, why = "L1", "元からの弱さ（基線かヌル腕の同じ種・同じ州が 1/4 未満）"
        else:
            A = read_owner(arm_ins, c["seed"], c["region"])
            if A is None or not A or c["peak_year"] is None:
                lab, why = "L3", "線が原因（腕の region_s かピークの年が読めない＝割れと数えない）"; C["読めない"] += 1
            else:
                q0 = min(A)
                qp = min((q for q, (y, _) in A.items() if y == c["peak_year"]), default=None)
                if mode == "late": qp = max(A)
                if qp is None:
                    lab, why = "L3", "線が原因（腕にピークの年の行が無い）"; C["読めない"] += 1
                else:
                    split = A[q0][1] != A[qp][1]
                    ns = [owner_at(read_owner(ins, c["seed"], c["region"]), qp) for _, ins in nulls]
                    missing = sum(1 for x in ns if x is None)
                    fork = missing == 0 and len(set(ns)) > 1
                    if missing: C["ヌルに行が無い（割れと数えない）"] += 1
                    if mode == "nocmp": fork = True
                    if split and fork: lab, why = "L2", "軌道の分かれの型（ピークの前に持ち主が替わり、ヌル 3 本の持ち主も割れる＝ヌルの幅の内・線の寄与が 0 とは言えない）"
                    elif split: lab, why = "L3", "線が原因（ピークの前に持ち主が替わったが、ヌル 3 本は同じ持ち主＝持ち主の分かれ自体が線の結果と読む）"
                    else: lab, why = "L3", "線が原因（ピークの前に持ち主は替わっていない）"
        C[lab] += 1; res.append((lab, why, c["text"], c))
    return res, C

def read_owner_many(ins, seed, regions, at_q):
    """一回の走査で、regions の州の (四半期 → (年, nationIndex)) と、全ての州の at_q の持ち主を返す。"""
    p = os.path.join(ins, "region_s%d.tsv" % seed)
    D = {r: {} for r in regions}; ALL = {}
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\r\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        iq, iy, ir, ii = ix["quarter"], ix["year"], ix["region"], ix["nationIndex"]
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            r = c[ir]
            if r in D: D[r][int(c[iq])] = (int(c[iy]), c[ii])
            if int(c[iq]) == at_q: ALL[r] = c[ii]
    return D, ALL

def calib(nulls, out, at_q=400, nseed=10):
    """較正の記録（門にしない）：ヌル 3 本の 1/4 未満の事象（例外を除く）のうち、持ち主がヌルどうしで割れる州にある割合と、全ての州（at_q の四半期）のうち割れる州の割合。"""
    ev = collections.defaultdict(list)
    for k, (an, _) in enumerate(nulls):
        for c in cases(an):
            if c["level"] == "1/4" and not c["exempt"] and c["peak_year"] is not None: ev[c["seed"]].append((k, c))
    n_ev = fk_ev = n_all = fk_all = 0
    for seed in range(1, int(nseed) + 1):
        regs = {c["region"] for _, c in ev[seed]}
        got = [read_owner_many(ins, seed, regs, at_q) for _, ins in nulls]
        regs_all = set(got[0][1]) & set(got[1][1]) & set(got[2][1])
        for r in regs_all:
            n_all += 1; fk_all += len({g[1][r] for g in got}) > 1
        for k, c in ev[seed]:
            qp = min((q for q, (y, _) in got[k][0][c["region"]].items() if y == c["peak_year"]), default=None)
            if qp is None: continue
            ns = [g[0][c["region"]].get(qp, (0, None))[1] for g in got]
            if any(x is None for x in ns): continue
            n_ev += 1; fk_ev += len(set(ns)) > 1
    L = ["(867) 較正の記録（ヌル 3 本・門にしない・件数が小さい）：1/4 未満の事象（例外を除く）%d 件のうち、ピークの四半期にヌル 3 本の持ち主が割れた州 %d 件（%.4f）" % (n_ev, fk_ev, fk_ev / n_ev if n_ev else float("nan")),
         "  全ての州（四半期 %d・種 1〜%d・3 本に在る州）%d 件のうち、持ち主が割れる州 %d 件（%.5f）" % (at_q, nseed, n_all, fk_all, fk_all / n_all if n_all else float("nan"))]
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0

def main():
    a = sys.argv[1:]
    if a and a[0] == "--calib":
        nulls = [(a[1], a[2]), (a[3], a[4]), (a[5], a[6])]; return calib(nulls, a[7], 400, int(a[8]) if len(a) > 8 else 10)
    mode = "peak"
    if "--mode" in a:
        i = a.index("--mode"); mode = a[i + 1]; a = a[:i] + a[i + 2:]
    arm_an, arm_ins, out = a[0], a[1], a[2]; nulls = []
    i = 3
    while i < len(a):
        if a[i] == "--null": nulls.append((a[i + 1], a[i + 2])); i += 3
        else: raise SystemExit("引数が分からない：" + a[i])
    if len(nulls) != 3: raise SystemExit("ヌル 3 本（M′・N1・N2）が要る")
    res, C = classify(arm_an, arm_ins, nulls, mode)
    L = []
    for lab, why, txt, _ in res: L.append("%s\t%s\t%s" % (lab, why, txt))
    holclass = C["L2"] + C["L3"]
    L.append("(867) 振り分け（モード %s）：1/10 の行 %d（例外の名簿を含む）＝L0 %d・L1 %d・L2 %d・L3 %d・読めない %d・ヌルに行が無い %d" % (mode, C["1/10 の行"], C["L0"], C["L1"], C["L2"], C["L3"], C["読めない"], C["ヌルに行が無い（割れと数えない）"]))
    L.append("holclass839 の数（線が原因と読む行＝L2＋L3）：%d" % holclass)
    L.append("VERDICT hol %s L3 が %d 件（L2 %d 件はヌルの幅の内として記録・〈近似〉）" % ("pass" if C["L3"] == 0 else "fail", C["L3"], C["L2"]))
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0

if __name__ == "__main__":
    sys.exit(main())
