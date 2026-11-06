# (896) c76cs 門 G1〜G4 と記録の読み手（読むだけ）。閾値は事前登録 measurements/m896_20261106_prereg_cs.txt §3 の写し（gates_before_paper.txt・結果を見て替えない）。
# 使い方: python -X utf8 gates896.py <腕の ins> <腕の基準の ins（種 1〜10＝keep_892/base_ins・種 11〜20＝keep_873/a11）> <幅の base> <N1> <N2> <種の起点 1|11> <out.txt>
#   G3 は腕の基準に crownpc_s があれば crownpc_s の再ラベル（全ての国）、無ければ fm_s の遷移（9 国）で、腕も同じ母集合で数える。
import csv, sys, os, math
from collections import defaultdict

EPS1 = 1e-9
MAXS = 0.67
NAT4 = ["イギリス", "清", "フランス", "ロシア"]
PERIODS = [(1783, 1830), (1830, 1880), (1880, 1914), (1914, 1951)]

def fnum(s):
    try: return float(s)
    except ValueError: return math.nan

def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for l in r: yield ix, l

def files(d, stem, sb):
    out = []
    for k in range(sb, sb + 10):
        p = os.path.join(d, f"{stem}_s{k}.tsv")
        out.append((k, p if os.path.exists(p) else None))
    return out

def fm_stats(d, sb):
    n = defaultdict(int); one = defaultdict(int); trans = 0; missing = []
    for k, p in files(d, "fm", sb):
        if p is None: missing.append(k); continue
        prev = {}
        for ix, l in rows(p):
            cf = fnum(l[ix["polityCode"]])
            if not (cf == cf): continue
            code = int(cf); nat = l[ix["nation"]]; uid = l[ix["nationUid"]]; pc = fnum(l[ix["parliamentControlShare"]])
            if code == 3:
                n[nat] += 1
                if pc >= 1 - EPS1: one[nat] += 1
            pv = prev.get(uid)
            if pv is not None and pv in (0, 1, 2) and code == 3: trans += 1
            prev[uid] = code
    return n, one, trans, missing

def crown_stats(d, sb):
    st = defaultdict(float); missing = []
    per = defaultdict(float)
    res16 = []
    for k, p in files(d, "crownpc", sb):
        if p is None: missing.append(k); continue
        for ix, l in rows(p):
            if l[ix["firstRowFlag"]] == "1" or l[ix["politicsEnabledFlag"]] != "1": continue
            st["rows"] += 1
            rev = fnum(l[ix["revenuePerMonth"]]); it = fnum(l[ix["interestPerMonth"]])
            x = 0.0 if not (rev > 0) else it / rev
            sm = fnum(l[ix["summonTermShare"]])
            if x >= MAXS:
                st["hi"] += 1
                if sm > 0: st["hipos"] += 1
            if sm > 0: st["pos"] += 1
            if sm == sm: st["sum"] += sm
            if l[ix["summonClampHitFlag"]] == "1": st["clamp"] += 1
            prevc = fnum(l[ix["polityPrevCode"]]); code = int(fnum(l[ix["polityCode"]]))
            if l[ix["relabelFlag"]] == "1" and prevc in (0, 1, 2) and code == 3: st["up"] += 1
            if l[ix["relabelFlag"]] == "1" and prevc == 3 and code in (0, 1, 2):
                st["down"] += 1
                if fnum(l[ix["term2pShare"]]) < 0: st["down2p"] += 1
            if l[ix["explainCode"]] == "9": st["ex9"] += 1
            dm = l[ix["driftMaskCode"]]
            if dm.isdigit() and int(dm) & 16:
                st["dm16"] += 1; res16.append(abs(fnum(l[ix["residualShare"]])))
            nat = l[ix["nation"]]
            if nat in NAT4 and sm == sm:
                yr = int(fnum(l[ix["year"]]))
                for a, b in PERIODS:
                    if a <= yr < b: per[(nat, a)] += sm
    res16.sort()
    return st, per, res16, missing

def share(n, one, nat): return one[nat] / n[nat] * 100 if n[nat] else float("nan")

def main():
    arm, armbase, wb, n1, n2, sb, out = sys.argv[1:8]; sb = int(sb)
    o = []; P = o.append
    P(f"(896) 門 G1〜G4（腕={arm}・腕の基準={armbase}・幅={wb}・{n1}・{n2}・種 {sb}〜{sb + 9}）")
    # 幅（種 1〜10 の base・N1・N2）
    W = {}
    for nm, d in (("base", wb), ("N1", n1), ("N2", n2)):
        W[nm] = fm_stats(d, 1)
    A = fm_stats(arm, sb); B = fm_stats(armbase, sb)
    if A[3] or B[3]: P(f"⚠欠けた種：腕 {A[3]}・基準 {B[3]}")
    ok_all = True
    P("== G1 働いた：立憲の四半期の PC≥1−1e-9 の割合（fm_s）＜ 基準 − 幅 ==")
    for nat in ("イギリス", "清"):
        v = [share(W[k][0], W[k][1], nat) for k in ("base", "N1", "N2")]
        w = max(abs(v[0] - v[1]), abs(v[0] - v[2]), abs(v[1] - v[2]))
        b = share(B[0], B[1], nat); a = share(A[0], A[1], nat); line = b - w
        g = a < line; ok_all &= g
        P(f"{nat}\t腕 {a:.2f}%\t基準 {b:.2f}%\t幅 {w:.2f}pt\t線 {line:.2f}%\t{'通る' if g else '落ちる'}")
    P("== G1b 壊さない：立憲の四半期の数 ≥ 基準×0.5 ==")
    for nat in ("イギリス", "清"):
        g = A[0][nat] >= 0.5 * B[0][nat]; ok_all &= g
        P(f"{nat}\t腕 {A[0][nat]}\t基準 {B[0][nat]}\t下限 {0.5 * B[0][nat]:.1f}\t{'通る' if g else '落ちる'}")
    P("== 記録：立憲の四半期の PC＝1 の割合（4 国・fm_s）==")
    for nat in NAT4:
        P(f"{nat}\t腕 {A[0][nat]}・{share(A[0], A[1], nat):.2f}%\t基準 {B[0][nat]}・{share(B[0], B[1], nat):.2f}%")
    cs, per, res16, cm = crown_stats(arm, sb)
    if cm: P(f"⚠腕の crownpc_s の欠けた種 {cm}")
    P("== G2 壊さない：利払い÷歳入≥0.67 の行のうち召集の項＞0 の割合 ≥10%・件数＞0（crownpc_s）==")
    hi = cs["hi"]; hp = cs["hipos"]; r2 = hp / hi * 100 if hi else float("nan")
    g = hi > 0 and hp > 0 and r2 >= 10; ok_all &= g
    P(f"≥0.67 の行 {int(hi)}\t項＞0 {int(hp)}\t{r2:.2f}%\t{'通る' if g else '落ちる'}")
    have_base_crown = all(p is not None for _, p in files(armbase, "crownpc", sb))
    P("== G3 壊さない：絶対・専制・幕府→立憲（基準の 0.25 倍以上・4 倍以下）==")
    if have_base_crown:
        bs, _, _, _ = crown_stats(armbase, sb)
        a3, b3 = cs["up"], bs["up"]; P("母集合＝crownpc_s の再ラベル（relabelFlag 1・全ての国）")
    else:
        a3, b3 = A[2], B[2]; P("母集合＝fm_s の遷移（9 国・事象と再ラベルを分けない）")
    g = 0.25 * b3 <= a3 <= 4 * b3; ok_all &= g
    P(f"腕 {int(a3)}\t基準 {int(b3)}\t[{0.25 * b3:.1f}, {4 * b3:.1f}]\t{'通る' if g else '落ちる'}")
    P("== G4 記録：summonClampHitFlag＝1 の行 ==")
    P(f"腕 {int(cs['clamp'])}" + (f"\t基準 {int(bs['clamp'])}" if have_base_crown else ""))
    P("== 記録 ==")
    P(f"立憲→王権の再ラベル {int(cs['down'])}（そのうち ②′ が負 {int(cs['down2p'])}）" + (f"\t基準 {int(bs['down'])}（②′ 負 {int(bs['down2p'])}）" if have_base_crown else ""))
    P(f"召集の項＞0 の行 {int(cs['pos'])}・項の和 {cs['sum']:.3f}・explainCode 9 の行 {int(cs['ex9'])}" + (f"\t基準 {int(bs['pos'])}・{bs['sum']:.3f}・{int(bs['ex9'])}" if have_base_crown else ""))
    if res16:
        P(f"driftMaskCode の 16（旗 1 で召集の項≠0 の 9 の行）{len(res16)}・|残差| 中央 {res16[len(res16) // 2]:.3g}・上位 10% {res16[int(len(res16) * 0.9)]:.3g}・最大 {res16[-1]:.3g}")
    else:
        P("driftMaskCode の 16 の行 0")
    P("召集の項の時期ごとの和（腕）")
    for nat in NAT4:
        P(nat + "\t" + "\t".join(f"{a}–{b - 1} {per[(nat, a)]:.3f}" for a, b in PERIODS))
    P(f"判定：門 G1・G1b・G2・G3 は {'全て通る' if ok_all else '落ちた物がある'}")
    with open(out, "wb") as f: f.write(("\n".join(o) + "\n").encode("utf-8"))

main()
