# (901) c77cs 門 G1〜G4 と記録の読み手（読むだけ）。閾値は measurements/m901_20261106/gates_before_paper.txt の写し（結果を見て替えない）。
# (896) gates896.py の型から：G1 を EPS 1e-9 と 1e-6 の 2 値で（両方で通る）・G2 の実際の側をエンジンの pcNow−pcPrev から他の項を引いた量へ（計器の summonTermShare を使わない）・
#   政変の四半期を G2 の分母から外す・記録に実際の側の時期ごとの和・残差の行の読み・仏 1783–1829 の利払い÷歳入を足した。
# 使い方: python -X utf8 gates901.py <腕の ins> <腕の基準の ins（種 1〜10＝keep_899/base_ins・種 11〜20＝keep_873/a11）> <幅の base> <N1> <N2> <種の起点 1|11> <out.txt>
import csv, sys, os, math
from collections import defaultdict

EPSS = (1e-9, 1e-6)
MAXS = 0.67
TOL = 1e-12
NAT4 = ["イギリス", "清", "フランス", "ロシア"]
PERIODS = [(1783, 1830), (1830, 1880), (1880, 1914), (1914, 1951)]
OTHERS = ("rollbackTermShare", "term1Share", "term2Share", "term2pShare")

def fnum(s):
    try: return float(s)
    except ValueError: return math.nan

def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for l in r: yield ix, l

def files(d, stem, sb):
    return [(k, os.path.join(d, f"{stem}_s{k}.tsv") if os.path.exists(os.path.join(d, f"{stem}_s{k}.tsv")) else None) for k in range(sb, sb + 10)]

def fm_stats(d, sb):
    n = defaultdict(int); one = {e: defaultdict(int) for e in EPSS}; trans = 0; missing = []
    for k, p in files(d, "fm", sb):
        if p is None: missing.append(k); continue
        prev = {}
        for ix, l in rows(p):
            cf = fnum(l[ix["polityCode"]])
            if not (cf == cf): continue
            code = int(cf); nat = l[ix["nation"]]; uid = l[ix["nationUid"]]; pc = fnum(l[ix["parliamentControlShare"]])
            if code == 3:
                n[nat] += 1
                for e in EPSS:
                    if pc >= 1 - e: one[e][nat] += 1
            pv = prev.get(uid)
            if pv is not None and pv in (0, 1, 2) and code == 3: trans += 1
            prev[uid] = code
    return n, one, trans, missing

def crown_stats(d, sb):
    st = defaultdict(float); missing = []; per = defaultdict(float); res = []; fr = []
    for k, p in files(d, "crownpc", sb):
        if p is None: missing.append(k); continue
        for ix, l in rows(p):
            if l[ix["firstRowFlag"]] == "1" or l[ix["politicsEnabledFlag"]] != "1": continue
            st["rows"] += 1
            rev = fnum(l[ix["revenuePerMonth"]]); it = fnum(l[ix["interestPerMonth"]])
            x = 0.0 if not (rev > 0) else it / rev
            sm = fnum(l[ix["summonTermShare"]])
            pcp = fnum(l[ix["pcPrevShare"]]); pcn = fnum(l[ix["pcNowShare"]])
            ev = l[ix["regimeEventFlag"]] == "1"
            act = pcn - pcp - sum(fnum(l[ix[c]]) for c in OTHERS)
            nat = l[ix["nation"]]; yr = int(fnum(l[ix["year"]]))
            if nat == "フランス" and yr < 1830: fr.append(x)
            if x >= MAXS and not ev:
                st["hi"] += 1
                if act > TOL: st["hiact"] += 1
                if sm > 0: st["hipos"] += 1
                rr = fnum(l[ix["residualShare"]])
                if abs(rr) > TOL: res.append(rr)
            if sm > 0: st["pos"] += 1
            if sm == sm: st["sum"] += sm
            if l[ix["summonClampHitFlag"]] == "1": st["clamp"] += 1
            prevc = fnum(l[ix["polityPrevCode"]]); code = int(fnum(l[ix["polityCode"]]))
            if l[ix["relabelFlag"]] == "1" and prevc in (0, 1, 2) and code == 3: st["up"] += 1
            if l[ix["relabelFlag"]] == "1" and prevc == 3 and code in (0, 1, 2):
                st["down"] += 1
                if fnum(l[ix["term2pShare"]]) < 0: st["down2p"] += 1
            if l[ix["explainCode"]] == "9": st["ex9"] += 1
            if nat in NAT4:
                for a, b in PERIODS:
                    if a <= yr < b:
                        if sm == sm: per[(nat, a, "ins")] += sm
                        if not ev and act == act and l[ix["grantFlag"]] != "1" and l[ix["floorHitFlag"]] != "1": per[(nat, a, "act")] += act
                        elif not ev: st["perskip"] += 1
    return st, per, res, fr, missing

def share(n, one, nat): return one[nat] / n[nat] * 100 if n[nat] else float("nan")

def main():
    arm, armbase, wb, n1, n2, sb, out = sys.argv[1:8]; sb = int(sb)
    o = []; P = o.append
    P(f"(901) 門 G1〜G4（腕={arm}・腕の基準={armbase}・幅={wb}・{n1}・{n2}・種 {sb}〜{sb + 9}）")
    W = {nm: fm_stats(d, 1) for nm, d in (("base", wb), ("N1", n1), ("N2", n2))}
    A = fm_stats(arm, sb); B = fm_stats(armbase, sb)
    if A[3] or B[3]: P(f"⚠欠けた種：腕 {A[3]}・基準 {B[3]}")
    ok = True
    P("== G1 働いた：立憲の四半期の PC≥1−EPS の割合（fm_s）≤ 基準 − 幅（EPS 1e-9 と 1e-6 の両方）==")
    for e in EPSS:
        for nat in ("イギリス", "清"):
            v = [share(W[k][0], W[k][1][e], nat) for k in ("base", "N1", "N2")]
            w = max(abs(v[0] - v[1]), abs(v[0] - v[2]), abs(v[1] - v[2]))
            b = share(B[0], B[1][e], nat); a = share(A[0], A[1][e], nat); line = b - w
            g = a <= line; ok &= g
            P(f"EPS {e:g}\t{nat}\t腕 {a:.2f}%\t基準 {b:.2f}%\t幅 {w:.2f}pt\t線 {line:.2f}%\t{'通る' if g else '落ちる'}")
    P("== G1b 壊さない：立憲の四半期の数 ≥ 基準×0.5 ==")
    for nat in ("イギリス", "清"):
        g = A[0][nat] >= 0.5 * B[0][nat]; ok &= g
        P(f"{nat}\t腕 {A[0][nat]}\t基準 {B[0][nat]}\t下限 {0.5 * B[0][nat]:.1f}\t{'通る' if g else '落ちる'}")
    P("== 記録：立憲の四半期の PC＝1 の割合（4 国・fm_s・EPS 1e-9／1e-6）==")
    for nat in NAT4:
        P(f"{nat}\t腕 {A[0][nat]}・{share(A[0], A[1][1e-9], nat):.2f}%／{share(A[0], A[1][1e-6], nat):.2f}%\t基準 {B[0][nat]}・{share(B[0], B[1][1e-9], nat):.2f}%／{share(B[0], B[1][1e-6], nat):.2f}%")
    cs, per, res, fr, cm = crown_stats(arm, sb)
    if cm: P(f"⚠腕の crownpc_s の欠けた種 {cm}")
    P("== G2 壊さない：利払い÷歳入≥0.67 の行（政変の四半期を除く）のうち、実際の側（pcNow−pcPrev−他の項）＞1e-12 の割合 ≥10%・件数＞0（crownpc_s）==")
    hi = cs["hi"]; ha = cs["hiact"]; r2 = ha / hi * 100 if hi else float("nan")
    g = hi > 0 and ha > 0 and r2 >= 10; ok &= g
    P(f"分母 {int(hi)}\t実際の側＞1e-12 {int(ha)}\t{r2:.2f}%\t{'通る' if g else '落ちる'}\t（記録：計器の項＞0 {int(cs['hipos'])}・{(cs['hipos'] / hi * 100 if hi else float('nan')):.2f}%）")
    have_base_crown = all(p is not None for _, p in files(armbase, "crownpc", sb))
    P("== G3 壊さない：絶対・専制・幕府→立憲（基準の 0.25 倍以上・4 倍以下）==")
    if have_base_crown:
        bs = crown_stats(armbase, sb)[0]; a3, b3 = cs["up"], bs["up"]; P("母集合＝crownpc_s の再ラベル（relabelFlag 1・全ての国）")
    else:
        a3, b3 = A[2], B[2]; P("母集合＝fm_s の遷移（9 国・事象と再ラベルを分けない）")
    g = 0.25 * b3 <= a3 <= 4 * b3; ok &= g
    P(f"腕 {int(a3)}\t基準 {int(b3)}\t[{0.25 * b3:.1f}, {4 * b3:.1f}]\t{'通る' if g else '落ちる'}")
    P("== G4 記録：summonClampHitFlag＝1 の行 ==")
    P(f"腕 {int(cs['clamp'])}" + (f"\t基準 {int(bs['clamp'])}" if have_base_crown else ""))
    P("== 記録 ==")
    P(f"立憲→王権の再ラベル {int(cs['down'])}（そのうち ②′ が負 {int(cs['down2p'])}）" + (f"\t基準 {int(bs['down'])}（②′ 負 {int(bs['down2p'])}）" if have_base_crown else ""))
    P(f"計器の召集の項＞0 の行 {int(cs['pos'])}・項の和 {cs['sum']:.3f}・explainCode 9 の行 {int(cs['ex9'])}" + (f"\t基準 {int(bs['pos'])}・{bs['sum']:.3f}・{int(bs['ex9'])}" if have_base_crown else ""))
    res.sort(key=abs)
    P(f"G2 の分母の残差の行（|残差|＞1e-12）{len(res)}・正 {sum(1 for v in res if v > 0)}・負 {sum(1 for v in res if v < 0)}" + (f"・中央 {abs(res[len(res) // 2]):.3g}・上位 10% {abs(res[int(len(res) * 0.9)]):.3g}・最大 {abs(res[-1]):.3g}" if res else ""))
    P(f"召集の項の時期ごとの和（腕・計器の項／実際の側＝政変・下賜・床の行を除く〔4 国で除いた行 {int(cs['perskip'])}〕・実際の側は残差を含む）")
    for nat in NAT4:
        P(nat + "\t" + "\t".join(f"{a}–{b - 1} {per[(nat, a, 'ins')]:.3f}／{per[(nat, a, 'act')]:.3f}" for a, b in PERIODS))
    fr.sort()
    if fr: P(f"仏 1783–1829 の利払い÷歳入：{len(fr)} 行・中央 {fr[len(fr) // 2]:.3f}・上位 10% {fr[int(len(fr) * 0.9)]:.3f}・最大 {fr[-1]:.3f}・≥0.67 {sum(1 for v in fr if v >= MAXS)}")
    P(f"判定：門 G1（2 値）・G1b・G2・G3 は {'全て通る' if ok else '落ちた物がある'}")
    with open(out, "wb") as f: f.write(("\n".join(o) + "\n").encode("utf-8"))

main()
