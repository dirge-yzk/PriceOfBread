# (855) c70ss 段0 の紙の台本（paper849.py の写し・定義は変えない・今の盤面 m852 とヌル腕 keep_852/n1・n2 で取り直す）＝元は (849) c69ss 段0 の紙の台本（読むだけ・シミュは回さない）：自給の段 2 S1-a の no-go の基準と門の素案の数を、基線とヌル腕の subst_s の生から取る。
# 呼び方：python paper849.py <基線の生> <ヌル腕1の生> <ヌル腕2の生> <出力>
# 定義（事前登録 measurements/m849_20261105_prereg_ss.txt §2 と同じ）：
#   U＝D1×能力（四半期の末の月の売れ残りの能力）・ρ_r＝d_p×必要量×担い手÷U0（その州が最初に現れた四半期 q0 で焼く・技術の比は 1＝EffFill の列は旧の木に無い）
#   旗 0 の食べる割合 s0＝engineSub÷(必要量×担い手)（エンジンの今の値・収穫係数つき）
#   旗 1 の食べる割合 s1＝ρ_r×U÷(必要量×担い手)×hf（hf＝engineSub÷nominal＝収穫係数）＝エンジンの形（GoodNeed×σ×収穫係数・min なし）
#   ヌル腕の幅 w＝同じ（種・四半期・州）の s0 の 3 本（基線・N1・N2）の最大−最小
#   母集合 P1＝D1＜D1_0 の州×四半期（q≧1）。外＝(s1−s0)＜−w。
# ⚠限界：①四半期の末の月の値（エンジンは前の月の U を読む＝1 か月ずれる）②技術の比 1③州の粒度（担い手の POP は州に 1 つ）④3 本のどれかに無い行は母集合から外す
import sys, os, collections

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def load(base, seed):
    """(seed, q, region) → (year, sp, rate, cap, d1, need, eng, nom, sold)＝種ごとに読む（記憶を抑える）"""
    D = {}
    if True:
        p = os.path.join(base, "subst_s%d.tsv" % seed)
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                sp = fnum(c[ix["subPopK"]])
                if not (sp > 0): continue
                D[(seed, int(c[ix["quarter"]]), c[ix["nation"]] + "/" + c[ix["region"]])] = (
                    int(c[ix["year"]]), sp, fnum(c[ix["subRateWeighted"]]), fnum(c[ix["farmCapPerMonth"]]), fnum(c[ix["unsoldD1Share"]]),
                    fnum(c[ix["stapleNeedPerCapita"]]), fnum(c[ix["engineSubOutputPerMonth"]]), fnum(c[ix["nominalSubPerMonth"]]),
                    fnum(c[ix["farmSoldPerMonth"]]))
    return D

def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"

def s0_of(r):
    y, sp, rate, cap, d1, need, eng, nom, sold = r
    den = need * sp
    return eng / den if den > 0 and eng == eng else float("nan")

def main(b, n1, n2, out):
    C = collections.Counter(); W = collections.defaultdict(list); bake = {}
    for seed in range(1, 11):
        A = [load(b, seed), load(n1, seed), load(n2, seed)]
        one_seed(A, bake, C, W)
        del A
    report(b, n1, n2, out, bake, C, W)

def one_seed(A, bake, C, W):
    base = A[0]
    for (seed, q, reg), r in sorted(base.items(), key=lambda kv: kv[0][1]):
        k = (seed, reg)
        if k in bake: continue
        y, sp, rate, cap, d1, need, eng, nom, sold = r
        U = d1 * cap
        bake[k] = (rate * need * sp / U, d1, sold / cap if cap > 0 else float("nan"), q) if (q == 0 and U > 0 and rate > 0 and cap > 0) else None
    for (seed, q, reg), r in base.items():
        b0 = bake.get((seed, reg))
        if b0 is None or q < 1: continue
        rho, d10, soc0, _ = b0
        y, sp, rate, cap, d1, need, eng, nom, sold = r
        if not (cap > 0) or d1 != d1 or not (nom > 0): continue
        s0s = [s0_of(a[(seed, q, reg)]) for a in A if (seed, q, reg) in a]
        if len(s0s) < 3 or any(v != v for v in s0s): C["3 本のどれかに無い・nan（外した）"] += 1; continue
        w = max(s0s) - min(s0s)
        s0 = s0s[0]
        hf = eng / nom
        s1 = rho * d1 * cap / (need * sp) * hf
        d = s1 - s0
        wn = win(y)
        for key in (wn, "全期間 q≧1"):
            C[(key, "行")] += 1
            if d1 < d10:
                C[(key, "P1 D1＜D1_0")] += 1
                W[(key, "P1 d")].append(d); W[(key, "P1 w")].append(w)
                if d < -w: C[(key, "P1 外（d＜−w）")] += 1
                if d < 0: C[(key, "P1 d＜0")] += 1
            soc = sold / cap
            if abs(soc - soc0) <= 0.01:
                C[(key, "P5 対照 |売れた÷能力−開幕|≦0.01")] += 1
                W[(key, "P5 d")].append(d); W[(key, "P5 w")].append(w)
                if abs(d) <= w: C[(key, "P5 内（|d|≦w）")] += 1
            # E_p＞0（技術の比 1）＝ρU×hf＞必要量×担い手
            if rho * d1 * cap * hf > need * sp: C[(key, "E_p＞0（倍率 1）")] += 1
    # ヌル腕どうし（基線を腕と見なさない一つ抜き）：N1 を〈旗 1〉の代わりに置いた P1 の外の割合（旗 0 の対照＝門が空振りする率の目安）
    for (seed, q, reg), r in base.items():
        b0 = bake.get((seed, reg))
        if b0 is None or q < 1: continue
        rho, d10, soc0, _ = b0
        y, sp, rate, cap, d1, need, eng, nom, sold = r
        if not (cap > 0) or d1 != d1 or not (nom > 0) or not (d1 < d10): continue
        if (seed, q, reg) not in A[1] or (seed, q, reg) not in A[2]: continue
        a, bb, cc = s0_of(r), s0_of(A[1][(seed, q, reg)]), s0_of(A[2][(seed, q, reg)])
        if a != a or bb != bb or cc != cc: continue
        # 一つ抜き：腕＝N2・幅＝基線と N1 の差＝(N2−基線)＜−|N1−基線|
        w2 = abs(bb - a)
        C[("全期間 q≧1", "ヌル一つ抜き P1 行")] += 1
        if (cc - a) < -w2: C[("全期間 q≧1", "ヌル一つ抜き P1 外")] += 1

def report(b, n1, n2, out, bake, C, W):
    def med(xs):
        xs = sorted(x for x in xs if x == x); n = len(xs)
        return float("nan") if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])
    L = []; P = L.append
    P("(849) 段0 の紙（基線 %s・ヌル腕 %s・%s・10 種・シミュは回していない）" % (b, n1, n2))
    P("焼いた州（種×州）%d・焼けなかった（q0 に無い・U0＝0）%d" % (sum(1 for v in bake.values() if v), sum(1 for v in bake.values() if not v)))
    P("外した行：%d" % C["3 本のどれかに無い・nan（外した）"])
    for key in ("1786-99", "1800-49", "1850-99", "1900-50", "全期間 q≧1"):
        n = C[(key, "P1 D1＜D1_0")]; o = C[(key, "P1 外（d＜−w）")]
        P("-- %s：行 %d・P1 %d・P1 の外 %d（%.4f）・P1 の d＜0 %d（%.4f）・P1 の d 中央 %.4f・w 中央 %.4f" % (
            key, C[(key, "行")], n, o, o / n if n else float("nan"), C[(key, "P1 d＜0")], C[(key, "P1 d＜0")] / n if n else float("nan"),
            med(W[(key, "P1 d")]), med(W[(key, "P1 w")])))
        n5 = C[(key, "P5 対照 |売れた÷能力−開幕|≦0.01")]; i5 = C[(key, "P5 内（|d|≦w）")]
        P("   P5 対照 %d・内 %d（%.4f）・d 中央 %.4f・w 中央 %.4f・E_p＞0（倍率 1）%d" % (n5, i5, i5 / n5 if n5 else float("nan"),
            med(W[(key, "P5 d")]), med(W[(key, "P5 w")]), C[(key, "E_p＞0（倍率 1）")]))
    nn = C[("全期間 q≧1", "ヌル一つ抜き P1 行")]; no = C[("全期間 q≧1", "ヌル一つ抜き P1 外")]
    P("ヌル一つ抜き（腕＝N2・幅＝|N1−基線|）：P1 %d・外 %d（%.4f）＝旗 0 相当の空振りの率" % (nn, no, no / nn if nn else float("nan")))
    n = C[("全期間 q≧1", "P1 D1＜D1_0")]; o = C[("全期間 q≧1", "P1 外（d＜−w）")]
    P("no-go の判定（事前登録 §2：P1 の外が 1/2 未満なら no-go）：%s（%.4f）" % ("no-go" if not (n and o / n >= 0.5) else "go", o / n if n else float("nan")))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
    print("\n".join(L))

if __name__ == "__main__":
    main(*sys.argv[1:5])
