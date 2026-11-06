# (901) c77cs 段0 の紙（腕の前・読むだけ）。(896) paper896.py の型を、今の盤面 keep_899/base_ins で取り直した。
# 足した物：(v) PC＝1 を EPS1 1e-9 と 1e-6 の 2 値で数える・(iii) G2 の実際の側＝エンジンの pcNow−pcPrev から他の項（巻き返し・①・②・②′）を引いた量
#   （計器の summonTermShare を使わない）・エンジンだけ何もしない M1 の紙（旗 0 の生から旧の召集を引く＝実際の側は残差だけ）・仏 1783–1829 の利払い÷歳入（記録）。
# 使い方: python -X utf8 paper901.py <base_ins> <n1> <n2> <a11> <out.txt>
# 門の閾値は事前登録の文（measurements/m901_20261106/gates_before_paper.txt）に先に書いた物＝この台本は閾値を読まない（数を出すだけ）。
import csv, sys, math, os
from collections import defaultdict

RATE = 0.02          # SummonParliamentRate（Coefficients.cs:2960）
MAXS = 0.67          # MaxInterestShareOfRevenue（Coefficients.cs:462）
EPSS = (1e-9, 1e-6)  # PC＝1 の判定（PC ≥ 1−EPS）
TOLS = (1e-12, 1e-9, 1e-6)
NAT4 = ["イギリス", "清", "フランス", "ロシア"]
NAT9 = NAT4 + ["プロイセン", "オーストリア", "オスマン", "アメリカ", "日本"]
PERIODS = [(1783, 1830), (1830, 1880), (1880, 1914), (1914, 1951)]
OTHERS = ("rollbackTermShare", "term1Share", "term2Share", "term2pShare")

def fnum(s):
    try: return float(s)
    except ValueError: return math.nan

def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for l in r: yield ix, l

def clamp01(v): return 0.0 if not (v > 0) else (1.0 if v > 1 else v)

def seeds(d, stem, sb=1):
    return [(k, os.path.join(d, f"{stem}_s{k}.tsv")) for k in range(sb, sb + 10) if os.path.exists(os.path.join(d, f"{stem}_s{k}.tsv"))]

def fm_share(d, sb=1):
    n = defaultdict(int); one = {e: defaultdict(int) for e in EPSS}; trans = 0
    for k, p in seeds(d, "fm", sb):
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
    return n, one, trans

def pct(a, b): return a / b * 100 if b else float("nan")

def main():
    base, n1, n2, a11, out = sys.argv[1:6]
    o = []; P = o.append
    P("(901) c77cs 段0 の紙＝旗 1 の召集の式（形 (B)）を旗 0 の生に一歩だけ当てた近似（軌道は旗 0 のまま）")
    P(f"生：base={base}・N1={n1}・N2={n2}・S11 基準={a11}")
    P(f"定数：Rate={RATE}・MaxInterestShareOfRevenue={MAXS}・PC＝1 は PC≥1−EPS（EPS＝{EPSS[0]:g} と {EPSS[1]:g}）")
    P("")
    P("== (1) fm_s：立憲（polityCode 3）の四半期の数と PC＝1 の割合（種 1〜10 の和・S11 基準は種 11〜20）==")
    R = {"base": fm_share(base), "N1": fm_share(n1), "N2": fm_share(n2), "S11基準": fm_share(a11, 11)}
    for e in EPSS:
        P(f"-- EPS {e:g}")
        P("国\t" + "\t".join(R))
        for nat in NAT9:
            P(nat + "\t" + "\t".join(f"{R[k][0][nat]}・{pct(R[k][1][e][nat], R[k][0][nat]):.2f}%" for k in R))
    P("絶対・専制・幕府→立憲の遷移（fm_s・9 国）\t" + "\t".join(f"{k} {R[k][2]}" for k in R))
    P("")
    P("== (2) G1 の線（英・清）＝base−幅・幅＝base・N1・N2 の対の差の最大（pt）==")
    for e in EPSS:
        for nat in ("イギリス", "清"):
            v = [pct(R[k][1][e][nat], R[k][0][nat]) for k in ("base", "N1", "N2")]
            w = max(abs(v[0] - v[1]), abs(v[0] - v[2]), abs(v[1] - v[2]))
            s11 = pct(R["S11基準"][1][e][nat], R["S11基準"][0][nat])
            P(f"EPS {e:g}\t{nat}\tbase {v[0]:.2f}%\tN1 {v[1]:.2f}%\tN2 {v[2]:.2f}%\t幅 {w:.2f}pt\t線 {v[0] - w:.2f}%\tS11 基準 {s11:.2f}%・S11 の線（幅を借りる＝近似）{s11 - w:.2f}%")
    P("G1b の下限（base の 0.5 倍）\t" + "\t".join(f"{nat} {R['base'][0][nat]}→{0.5 * R['base'][0][nat]:.1f}" for nat in ("イギリス", "清"))
             + "\tS11 " + "\t".join(f"{nat} {R['S11基準'][0][nat]}→{0.5 * R['S11基準'][0][nat]:.1f}" for nat in ("イギリス", "清")))
    P("")
    # ---- (3) crownpc_s：G2 の実際の側 ----
    st = defaultdict(int); sums = defaultdict(float); per = defaultdict(float)
    resv = []; fr = []; up = 0; clamp = 0
    for k, p in seeds(base, "crownpc"):
        for ix, l in rows(p):
            if l[ix["firstRowFlag"]] == "1" or l[ix["politicsEnabledFlag"]] != "1": continue
            prevc = fnum(l[ix["polityPrevCode"]]); code = int(fnum(l[ix["polityCode"]]))
            if l[ix["relabelFlag"]] == "1" and prevc in (0, 1, 2) and code == 3: up += 1
            if l[ix["summonClampHitFlag"]] == "1": clamp += 1
            rev = fnum(l[ix["revenuePerMonth"]]); it = fnum(l[ix["interestPerMonth"]])
            x = 0.0 if not (rev > 0) else it / rev
            nat = l[ix["nation"]]; yr = int(fnum(l[ix["year"]]))
            if nat == "フランス" and yr < 1830: fr.append(x)
            pcp = fnum(l[ix["pcPrevShare"]]); pcn = fnum(l[ix["pcNowShare"]]); dg = fnum(l[ix["debtGripShare"]])
            old = fnum(l[ix["summonTermShare"]]); res = fnum(l[ix["residualShare"]])
            newb = RATE * clamp01(x / MAXS) * max(0.0, dg - pcp)
            if nat in NAT4:
                for a, b in PERIODS:
                    if a <= yr < b: per[(nat, a, "old")] += old; per[(nat, a, "B")] += newb
            if not (x >= MAXS): continue
            st["hi"] += 1
            if l[ix["regimeEventFlag"]] == "1": st["hi_ev"] += 1; continue
            st["den"] += 1
            oth = sum(fnum(l[ix[c]]) for c in OTHERS)
            act0 = pcn - pcp - oth          # 旗 0 の生そのもの（旧の召集のエンジンの動き＋残差）
            actB = act0 - old + newb        # 紙：エンジンが形 (B) で動いた（旧の召集を引き、新しい項を足す）
            actM1 = act0 - old              # 紙：エンジンだけ何もしない M1（計器は旗 1 のまま）＝残差だけ
            resv.append(res)
            for t in TOLS:
                if act0 > t: st[f"act0>{t:g}"] += 1
                if actB > t: st[f"actB>{t:g}"] += 1
                if actM1 > t: st[f"actM1>{t:g}"] += 1
            if old > 0: st["old>0"] += 1
            if newb > 0: st["newB>0"] += 1     # 旧の G2 の形（計器の項）＝M1 でも計器は旗 1 で同じ値を出す
            sums["act0"] += act0; sums["actB"] += actB; sums["actM1"] += actM1; sums["newB"] += newb
            if abs(actM1 - res) > 1e-9: st["m1_ne_res"] += 1
    den = st["den"]
    P("== (3) G2 の実際の側（crownpc_s・base・最初の行と政治の止まった行と政変の四半期を除く・利払い÷歳入≥0.67）==")
    P(f"≥0.67 の行 {st['hi']}・そのうち政変の四半期 {st['hi_ev']}（数えない）・分母 {den}")
    P("量＝pcNowShare−pcPrevShare−(rollbackTermShare＋term1Share＋term2Share＋term2pShare)（エンジンの動きから他の項を引いた残り＝召集＋残差）")
    P("盤面\t" + "\t".join(f">{t:g}" for t in TOLS) + "\t和")
    for nm, key in (("旗 0 の生（旧の召集）", "act0"), ("紙：形 (B)（旧を引き新を足す）", "actB"), ("紙：エンジンだけの M1（旧を引くだけ）", "actM1")):
        P(nm + "\t" + "\t".join(f"{st[f'{key}>{t:g}']}（{pct(st[f'{key}>{t:g}'], den):.2f}%）" for t in TOLS) + f"\t{sums[key]:.4f}")
    P(f"参考：旧の G2 の形（計器の項＞0）＝旗 0 の生 {st['old>0']}（{pct(st['old>0'], den):.2f}%）・紙 (B) と M1 の計器の項＞0 {st['newB>0']}（{pct(st['newB>0'], den):.2f}%）＝M1 でも計器は同じ＝旧の G2 は M1 を落とせない")
    P(f"検算：紙の M1 の量と residualShare の差が 1e-9 を超える行 {st['m1_ne_res']}（0 なら M1 の量＝残差）")
    resv.sort(key=abs); nz = [v for v in resv if abs(v) > 1e-12]
    P(f"残差の行の読み（分母の行の residualShare）：|残差|＞1e-12 の行 {len(nz)}（{pct(len(nz), den):.2f}%）・正 {sum(1 for v in nz if v > 0)}・負 {sum(1 for v in nz if v < 0)}"
      + (f"・|残差| 中央 {abs(nz[len(nz) // 2]):.3g}・上位 10% {abs(nz[int(len(nz) * 0.9)]):.3g}・最大 {abs(nz[-1]):.3g}" if nz else ""))
    P("")
    P("== (4) G3 の旗 0 の値・G4 の旗 0 の値（crownpc_s・base・全ての国）==")
    P(f"絶対・専制・幕府→立憲の再ラベル（relabelFlag 1）{up}（線 [{0.25 * up:.1f}, {4 * up:.1f}]）・summonClampHitFlag＝1 の行 {clamp}")
    P("")
    P("== (5) 記録：時期ごとの召集の項の和（一歩の紙・全ての種の和・旧＝計器の旗 0 の項・B＝紙）==")
    for nat in NAT4:
        P(nat + "\t" + "\t".join(f"{a}–{b - 1} 旧 {per[(nat, a, 'old')]:.3f}・B {per[(nat, a, 'B')]:.3f}" for a, b in PERIODS))
    fr.sort()
    if fr:
        P(f"仏 1783–1829 の利払い÷歳入（crownpc_s の行・{len(fr)} 行）：中央 {fr[len(fr) // 2]:.3f}・上位 10% {fr[int(len(fr) * 0.9)]:.3f}・最大 {fr[-1]:.3f}・≥0.67 の行 {sum(1 for v in fr if v >= MAXS)}（{pct(sum(1 for v in fr if v >= MAXS), len(fr)):.2f}%）")
    with open(out, "wb") as f: f.write(("\n".join(o) + "\n").encode("utf-8"))

main()
