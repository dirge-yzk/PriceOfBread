# (884) c74ds 読むだけの紙の台本 4/4：開幕の基数の比の崩れの分解（R と V のどちらが動いたか）と、開幕に凍結したときの実質の重さ B＝R(1783 q0)÷R(今) の見込み
# 使い方：python -X utf8 -I paper884d.py <base_ins> <出力.txt>
# 定義：R＝Σ max(0, rentToOthers)＋max(0, rentToFarmers)（subst_s の行の州・国の R と中央 1.000 で同じ＝paper884b [R]）・V＝Σ engineSub×主食の値段（名目・その国の通貨）。
#   (種,国) ごとに窓の四半期あたりの平均を取り、1783 q0 の値で割る→種の中央。物価は nation_s の priceLevel（窓の平均÷q0）。
import sys, os, collections

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def ff(x, d=2): return ("%." + str(d) + "f") % x if x == x else "nan"

WINDOWS = [(1786, 1799, "1786-99"), (1800, 1849, "1800-49"), (1850, 1899, "1850-99"), (1900, 1950, "1900-50")]
def wins(y): return [w[2] for w in WINDOWS if w[0] <= y <= w[1]]

def main():
    base, opath = sys.argv[1], sys.argv[2]
    q0 = {}; acc = collections.defaultdict(lambda: [0.0, 0.0, 0])   # (seed,nat,win) → V, R, 四半期の数（州の行の和を四半期ごとに）
    qsum = collections.defaultdict(lambda: [0.0, 0.0])   # (seed,nat,q) → V, R
    qyear = {}
    for seed in range(1, 11):
        with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if not (fnum(c[ix["subPopK"]]) > 0): continue
                qq = int(c[ix["quarter"]]); nat = c[ix["nation"]]
                eng = fnum(c[ix["engineSubOutputPerMonth"]]); px = fnum(c[ix["stapleNeedWeightedPrice"]])
                ro = fnum(c[ix["rentToOthersPerMonth"]]); rf = fnum(c[ix["rentToFarmersPerMonth"]])
                k = qsum[(seed, nat, qq)]
                k[0] += eng * px if (eng == eng and px == px) else 0.0
                k[1] += max(0.0, ro if ro == ro else 0.0) + max(0.0, rf if rf == rf else 0.0)
                qyear[(seed, qq)] = int(c[ix["year"]])
    for (seed, nat, qq), (V, R) in qsum.items():
        if qq == 0: q0[(seed, nat)] = (V, R)
        for wn in wins(qyear[(seed, qq)]):
            a = acc[(seed, nat, wn)]; a[0] += V; a[1] += R; a[2] += 1
    pl0 = {}; plw = collections.defaultdict(lambda: [0.0, 0])
    for seed in range(1, 11):
        with open(os.path.join(base, "nation_s%d.tsv" % seed), encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                nat = c[ix["nation"]]; qq = int(c[ix["quarter"]]); y = int(c[ix["year"]]); pl = fnum(c[ix["priceLevel"]])
                if not (pl == pl and pl > 0): continue
                if qq == 0: pl0[(seed, nat)] = pl
                for wn in wins(y): a = plw[(seed, nat, wn)]; a[0] += pl; a[1] += 1
    nats = sorted({n for (_, n) in q0})
    pick = ["イギリス", "清", "ネーデルラント", "スペイン", "ポルトガル", "ロシア", "フランス", "プロイセン", "オーストリア", "日本", "アメリカ", "オスマン",
            "ハワイ王国", "コンゴ王国", "オマーン", "朝鮮", "ポーランド", "ベンガル"]
    out = []; P = out.append
    P("(884) c74ds 紙の数 4/4（台本 paper884d.py・腕 %s・シミュは回していない）" % base)
    P("[K] 窓の四半期あたりの平均÷1783 q0（10 種の中央）：R（今の土地税の基数・名目）・V（自給の収穫の値・名目）・物価水準 priceLevel・B＝R(q0)÷R(窓)＝開幕に凍結した定額の実質でない重さ（名目の基数の比）")
    P("   国\t" + "\t".join("R %s" % w[2] for w in WINDOWS) + "\t" + "\t".join("V %s" % w[2] for w in WINDOWS) + "\t" + "\t".join("物価 %s" % w[2] for w in WINDOWS))
    allR = collections.defaultdict(list); allV = collections.defaultdict(list); allP = collections.defaultdict(list)
    for nat in nats:
        rr = []; vv = []; pp = []
        for _, _, wn in WINDOWS:
            r_ = []; v_ = []; p_ = []
            for s in range(1, 11):
                a = acc.get((s, nat, wn)); z = q0.get((s, nat))
                if a and z and a[2] > 0:
                    if z[1] > 0: r_.append(a[1] / a[2] / z[1])
                    if z[0] > 0: v_.append(a[0] / a[2] / z[0])
                b = plw.get((s, nat, wn)); p0 = pl0.get((s, nat))
                if b and p0 and b[1] > 0: p_.append(b[0] / b[1] / p0)
            rr.append(med(r_)); vv.append(med(v_)); pp.append(med(p_))
            allR[wn].append(rr[-1]); allV[wn].append(vv[-1]); allP[wn].append(pp[-1])
        if nat in pick:
            P("   %s\t%s\t%s\t%s" % (nat, "\t".join(ff(x) for x in rr), "\t".join(ff(x) for x in vv), "\t".join(ff(x) for x in pp)))
    P("   全ての国の中央\t%s\t%s\t%s" % ("\t".join(ff(med(allR[w[2]])) for w in WINDOWS), "\t".join(ff(med(allV[w[2]])) for w in WINDOWS), "\t".join(ff(med(allP[w[2]])) for w in WINDOWS)))
    P("   ⚠B＝1÷(R の比)。物価で割った実質の重さ＝B×(物価の比)⁻¹ ではなく、定額の請求÷その時の収穫比例の請求＝B そのもの（名目どうしの比）。")
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main()
