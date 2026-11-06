# (896) c76cs 段0 の紙（腕の前・読むだけ）。旗 1 の召集の式を旗 0 の生（crownpc_s・fm_s）に当てた〈一歩だけの紙〉＝軌道は旗 0 のまま＝近似。
# 使い方: python -X utf8 paper896.py <base_ins> <n1> <n2> <a11> <out.txt>
# 門の閾値は事前登録の文（prereg_cs.txt）に先に書いた物を、この台本が読み直さずに写す（結果を見て替えない）。
import csv, sys, math, os
from collections import defaultdict

RATE = 0.02          # SummonParliamentRate（Coefficients.cs）
FLOOR = 0.45         # ConstitutionalControlFloor
MAXS = 0.67          # MaxInterestShareOfRevenue
EPS1 = 1e-9          # PC＝1 の判定（PC ≥ 1−1e-9）
NAT4 = ["イギリス", "清", "フランス", "ロシア"]
PERIODS = [(1783, 1830), (1830, 1880), (1880, 1914), (1914, 1951)]

def fnum(s):
    try:
        return float(s)
    except ValueError:
        return math.nan

def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t")
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for line in r:
            yield ix, line

def clamp01(v):
    return 0.0 if not (v > 0) else (1.0 if v > 1 else v)

def target(form, pc_start, x, dg):
    s = 0.0 if not (x > 0) else clamp01(x / MAXS)
    if form == "A": T = FLOOR
    elif form == "B": T = dg
    else: T = max(FLOOR, dg)
    return RATE * s * max(0.0, T - pc_start)

def ptarget(code):
    return {4: 1.0, 3: 0.65, 5: 0.30, 6: 0.20, 7: 0.10}.get(code, 0.0)

def seeds(d, stem):
    out = []
    for k in range(1, 21):
        p = os.path.join(d, f"{stem}_s{k}.tsv")
        if os.path.exists(p): out.append((k, p))
    return out

NANFM = {}
def pc1_share_fm(d):
    """fm_s：立憲（polityCode 3）の四半期のうち PC≥1−1e-9 の割合（国ごと・全ての種の和）。"""
    n = defaultdict(int); one = defaultdict(int); trans = 0; trans_by = defaultdict(int); nanfm = [0]
    for k, p in seeds(d, "fm"):
        prev = {}
        for ix, l in rows(p):
            nat = l[ix["nation"]]; cf = fnum(l[ix["polityCode"]]); pc = fnum(l[ix["parliamentControlShare"]])
            if not (cf == cf): nanfm[0] += 1; continue
            code = int(cf)
            uid = l[ix["nationUid"]]
            if code == 3:
                n[nat] += 1
                if pc >= 1 - EPS1: one[nat] += 1
            pv = prev.get(uid)
            if pv is not None and pv in (0, 1, 2) and code == 3:
                trans += 1; trans_by[nat] += 1
            prev[uid] = code
    NANFM[d] = nanfm[0]
    return n, one, trans, trans_by

def main():
    base, n1, n2, a11, out = sys.argv[1:6]
    o = []
    P = o.append
    P("(896) c76cs 段0 の紙＝旗 1 の召集の式を旗 0 の生に一歩だけ当てた近似（軌道は旗 0 のまま）")
    P(f"生：base={base}・N1={n1}・N2={n2}・S11 基準={a11}")
    P(f"定数：Rate={RATE}・床={FLOOR}・MaxInterestShareOfRevenue={MAXS}・PC＝1 は PC≥1−{EPS1}")
    P("形：s＝Clamp01(利払い÷歳入÷0.67)（分母の守り＝歳入が正でなければ 0）・項＝Rate×s×max(0, T−PC)。(A) T＝床 0.45・(B) T＝DebtGrip・(C) T＝max(床, DebtGrip)")
    P("")
    # ---- (1) ヌル腕の幅：fm_s の立憲の四半期の PC＝1 の割合 ----
    P("== (1) fm_s：立憲の四半期の PC＝1 の割合（種 1〜10 の和）と絶対・専制・幕府→立憲の遷移の数（事象と再ラベルを分けない）==")
    res = {}
    for name, d in (("base", base), ("N1", n1), ("N2", n2), ("S11基準", a11)):
        res[name] = pc1_share_fm(d)
    P("国\t" + "\t".join(f"{k}(立憲の四半期・PC=1)" for k in res))
    for nat in NAT4 + ["プロイセン", "オーストリア", "オスマン", "アメリカ", "日本"]:
        cells = []
        for k in res:
            n, one, _, _ = res[k]
            cells.append(f"{n[nat]}・{(one[nat] / n[nat] * 100 if n[nat] else float('nan')):.2f}%")
        P(nat + "\t" + "\t".join(cells))
    P("王権→立憲の遷移\t" + "\t".join(str(res[k][2]) for k in res))
    P("polityCode が nan の行（数えない）\t" + "\t".join(str(NANFM[d]) for d in (base, n1, n2, a11)))
    P("")
    P("== G1 の幅（英・清）：base・N1・N2 の 3 つの値の対の差の最大（pt）==")
    for nat in ("イギリス", "清"):
        v = []
        for k in ("base", "N1", "N2"):
            n, one, _, _ = res[k]; v.append(one[nat] / n[nat] * 100 if n[nat] else float("nan"))
        w = max(abs(v[0] - v[1]), abs(v[0] - v[2]), abs(v[1] - v[2]))
        P(f"{nat}\tbase {v[0]:.2f}%\tN1 {v[1]:.2f}%\tN2 {v[2]:.2f}%\t幅 {w:.2f}pt\tG1 の線＝base−幅 {v[0] - w:.2f}%")
    n, one, _, _ = res["S11基準"]
    for nat in ("イギリス", "清"):
        P(f"S11 基準 {nat}\t{one[nat] / n[nat] * 100 if n[nat] else float('nan'):.2f}%（種 11〜20 のヌル腕は無い＝幅は種 1〜10 の物を借りる＝近似）")
    P("")
    # ---- (2) 一歩だけの紙：crownpc_s ----
    tot = 0; hi = 0
    cnt = {f: defaultdict(int) for f in ("old", "A", "B", "C")}
    per = {f: defaultdict(float) for f in ("old", "A", "B", "C")}
    sumterm = {f: 0.0 for f in ("old", "A", "B", "C")}
    nanrev = 0
    relabel_up = 0; relabel_up_seed = defaultdict(int)
    xs_dist = []
    for k, p in seeds(base, "crownpc"):
        for ix, l in rows(p):
            if l[ix["firstRowFlag"]] == "1" or l[ix["politicsEnabledFlag"]] != "1": continue
            prevc = fnum(l[ix["polityPrevCode"]]); code = int(fnum(l[ix["polityCode"]]))
            if l[ix["relabelFlag"]] == "1" and prevc in (0, 1, 2) and code == 3:
                relabel_up += 1; relabel_up_seed[k] += 1
            pc0 = fnum(l[ix["pcPrevShare"]])
            if pc0 < 0: pc0 = ptarget(int(prevc))
            interest = fnum(l[ix["interestPerMonth"]]); rev = fnum(l[ix["revenuePerMonth"]])
            x = 0.0 if not (rev > 0) else interest / rev
            if not (rev > 0): nanrev += 1
            dg = fnum(l[ix["debtGripShare"]])
            nat = l[ix["nation"]]; yr = int(fnum(l[ix["year"]]))
            terms = {"old": fnum(l[ix["summonTermShare"]])}
            for f in ("A", "B", "C"): terms[f] = target(f, pc0, x, dg)
            tot += 1
            ishi = x >= MAXS
            if ishi: hi += 1
            for f, t in terms.items():
                if not (t == t): continue
                sumterm[f] += t
                if t > 0: cnt[f]["pos"] += 1
                if ishi and t > 0: cnt[f]["hipos"] += 1
                if pc0 + t >= 1 - EPS1: cnt[f]["end1"] += 1
                if pc0 + t >= 1 - EPS1 and pc0 < 1 - EPS1: cnt[f]["reach1"] += 1
                if pc0 < FLOOR and pc0 + t >= FLOOR: cnt[f]["cross"] += 1
                if nat in NAT4:
                    for a, b in PERIODS:
                        if a <= yr < b: per[f][(nat, a)] += t
    P("== (2) 一歩だけの紙（crownpc_s・最初の行と政治の止まった行を除く）==")
    P(f"行 {tot}・利払い÷歳入≥0.67 の行 {hi}（{hi / tot * 100:.2f}%）・歳入が正でない行 {nanrev}")
    P("形\t召集の項>0 の行\t≥0.67 の行のうち項>0\t始まり+項≥1−1e-9\tそのうち始まり<1（1 へ届く）\t床 0.45 を下から越える\t項の和")
    for f in ("old", "A", "B", "C"):
        c = cnt[f]
        P(f"{f}\t{c['pos']}（{c['pos'] / tot * 100:.2f}%）\t{c['hipos']}（{(c['hipos'] / hi * 100 if hi else float('nan')):.2f}%）\t{c['end1']}\t{c['reach1']}\t{c['cross']}\t{sumterm[f]:.3f}")
    P("")
    P("== (3) 英・清・仏・露の時期ごとの召集の項の和（一歩の紙・全ての種の和・単位 PC）==")
    P("国\t時期\told\tA\tB\tC")
    for nat in NAT4:
        for a, b in PERIODS:
            P(f"{nat}\t{a}–{b - 1}\t" + "\t".join(f"{per[f][(nat, a)]:.3f}" for f in ("old", "A", "B", "C")))
    P("")
    P(f"== (4) G3 の旗 0 の値：絶対・専制・幕府→立憲の再ラベル（relabelFlag＝1）＝{relabel_up}（種ごと {dict(sorted(relabel_up_seed.items()))}）==")
    with open(out, "wb") as f:
        f.write(("\n".join(o) + "\n").encode("utf-8"))

main()
