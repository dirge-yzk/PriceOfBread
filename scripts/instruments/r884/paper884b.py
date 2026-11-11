# (884) c74ds 読むだけの紙の台本 2/2：段 A-0 の列と段 A-2 の門の母集合の見込み（盤面 m873・シミュを回さない・エンジンの関数を呼ばない）
# 使い方：python -X utf8 -I paper884b.py <出力.txt> <腕の名=フォルダ> [<腕の名=フォルダ> ...]
# [S] subst_s の s2 の列（S1-a の後に実測で取れる量）：s2BasisKind の分布・s2SigmaEngine と subRateWeighted の差（担い手が 1 POP の州）・
#     作った量が必要量を超える州×四半期（s2EEnd＞0＝余りの労働が正＝E_p＞0 の見込み）・(b′) の失業が正（s2UnempB＞0）
# [R] 今の土地税の基数 R の 2 つの読み：subst_s の行の州だけの和（paper884 の R）と、cornint_s の国の rentDomTotalPerMonth（国の全ての州）の比
# [D] 段 A-2 の〈働いた〉の門 G8 の母集合の見込み：nation_s の priceLevel で、4 四半期先の物価が 2% 超下がる／上がる国×四半期の割合と、
#     8 四半期続けて前の四半期より下がった区間の数（定額が実質で重くなる局面）
import sys, os, collections

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def qt(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    return xs[min(len(xs) - 1, int(p * len(xs)))]

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def ff(x, d=3):
    return ("%." + str(d) + "f") % x if x == x else "nan"

WINDOWS = [(1783, 1785, "1783-85"), (1786, 1799, "1786-99"), (1800, 1849, "1800-49"), (1850, 1899, "1850-99"), (1900, 1950, "1900-50")]
def wins(y): return [w[2] for w in WINDOWS if w[0] <= y <= w[1]]

def arm_s(base):
    c = collections.defaultdict(collections.Counter); diff = collections.defaultdict(list)
    rsub = collections.defaultdict(float)   # (seed,q,nat) → R（subst の行）
    for seed in range(1, 11):
        p = os.path.join(base, "subst_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                v = line.rstrip("\n").split("\t")
                y = int(v[ix["year"]]); qq = int(v[ix["quarter"]]); nat = v[ix["nation"]]
                ro = fnum(v[ix["rentToOthersPerMonth"]]); rf = fnum(v[ix["rentToFarmersPerMonth"]])
                rsub[(seed, qq, nat)] += max(0.0, ro if ro == ro else 0.0) + max(0.0, rf if rf == rf else 0.0)
                sp = fnum(v[ix["subPopK"]])
                if not (sp > 0): continue
                kind = v[ix["s2BasisKind"]]; e = fnum(v[ix["s2EEnd"]]); ub = fnum(v[ix["s2UnempB"]])
                se = fnum(v[ix["s2SigmaEngine"]]); sw = fnum(v[ix["subRateWeighted"]])
                for wn in wins(y):
                    C = c[wn]; C["rows"] += 1; C["kind" + kind] += 1
                    if e == e:
                        C["eDef"] += 1
                        if e > 0: C["ePos"] += 1
                    if ub == ub and ub > 0: C["ubPos"] += 1
                    if kind == "2" and se == se and sw == sw: diff[wn].append(abs(se - sw))
    return c, diff, rsub

def arm_r(base, rsub):
    ratio = collections.defaultdict(list)
    for seed in range(1, 11):
        p = os.path.join(base, "cornint_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                v = line.rstrip("\n").split("\t")
                y = int(v[ix["year"]]); qq = int(v[ix["quarter"]]); nat = v[ix["nation"]]
                dom = fnum(v[ix["rentDomTotalPerMonth"]]); un = sum(x for x in (fnum(v[ix["rentDomUnattrPerMonth%d" % b]]) for b in range(3)) if x == x)
                rs = rsub.get((seed, qq, nat))
                if rs is None or not (dom == dom) or not (dom + un > 0): continue
                for wn in wins(y): ratio[wn].append(rs / (dom + un))
    return ratio

def arm_d(base):
    out = collections.defaultdict(collections.Counter); runs = collections.Counter()
    for seed in range(1, 11):
        p = os.path.join(base, "nation_s%d.tsv" % seed)
        if not os.path.exists(p): continue
        ser = collections.defaultdict(list)
        with open(p, encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                v = line.rstrip("\n").split("\t")
                ser[v[ix["nation"]]].append((int(v[ix["quarter"]]), int(v[ix["year"]]), fnum(v[ix["priceLevel"]])))
        for nat, s in ser.items():
            s.sort(); d = {qq: (y, pl) for qq, y, pl in s}
            run = 0
            for qq, y, pl in s:
                if qq + 4 in d and pl == pl and pl > 0:
                    p4 = d[qq + 4][1]
                    if p4 == p4:
                        for wn in wins(y):
                            O = out[wn]; O["n"] += 1
                            if p4 / pl < 0.98: O["down"] += 1
                            elif p4 / pl > 1.02: O["up"] += 1
                            else: O["flat"] += 1
                if qq - 1 in d and pl == pl and d[qq - 1][1] == d[qq - 1][1] and pl < d[qq - 1][1]: run += 1
                else:
                    if run >= 8: runs[seed] += 1
                    run = 0
            if run >= 8: runs[seed] += 1
    return out, runs

def main():
    opath = sys.argv[1]; arms = [a.split("=", 1) for a in sys.argv[2:]]
    out = []; P = out.append
    P("(884) c74ds 紙の数 2/2（台本 paper884b.py・盤面 m873・シミュは回していない）")
    P("腕：" + "・".join("%s=%s" % (a, b) for a, b in arms))
    P("")
    for name, base in arms:
        c, diff, rsub = arm_s(base)
        P("[S] subst_s の s2 の列（腕 %s・担い手の人数＞0 の州×四半期）" % name)
        for _, _, wn in WINDOWS:
            C = c[wn]
            if not C["rows"]: continue
            P("   %s：行 %d・s2BasisKind 2（生産性を焼いた）%d・1（助走の割合）%d・0（開幕の割合）%d・-1（担い手なし）%d・s2EEnd が数として出た行 %d のうち＞0 %d（%.2f%%）・s2UnempB＞0 %d（%.2f%%）・|s2SigmaEngine−subRateWeighted| の中央 %s・p99 %s（焼いた州）" % (
                wn, C["rows"], C["kind2"], C["kind1"], C["kind0"], C["kind-1"], C["eDef"], C["ePos"], 100.0 * C["ePos"] / max(1, C["eDef"]), C["ubPos"], 100.0 * C["ubPos"] / C["rows"],
                ff(med(diff[wn]), 4), ff(qt(diff[wn], .99), 4)))
        ratio = arm_r(base, rsub)
        P("[R] R（subst の行の州の和）÷国の全ての国内の地代（cornint の rentDomTotal＋未帰属）（腕 %s・国×四半期）" % name)
        for _, _, wn in WINDOWS:
            if not ratio[wn]: continue
            P("   %s：国×四半期 %d・中央 %s・p10 %s・p1 %s・最小 %s" % (wn, len(ratio[wn]), ff(med(ratio[wn])), ff(qt(ratio[wn], .1)), ff(qt(ratio[wn], .01)), ff(min(ratio[wn]))))
        o, runs = arm_d(base)
        P("[D] 物価水準（nation_s priceLevel）の 4 四半期先の比（腕 %s・国×四半期）・8 四半期以上続けて下がった区間の数（種ごと）" % name)
        for _, _, wn in WINDOWS:
            O = o[wn]
            if not O["n"]: continue
            P("   %s：国×四半期 %d・2%% 超下がる %d（%.1f%%）・2%% 超上がる %d（%.1f%%）・±2%% の内 %d（%.1f%%）" % (wn, O["n"], O["down"], 100.0 * O["down"] / O["n"], O["up"], 100.0 * O["up"] / O["n"], O["flat"], 100.0 * O["flat"] / O["n"]))
        P("   8 四半期以上続けて下がった区間：種ごと %s（合計 %d）" % ("・".join("%d:%d" % (s, runs[s]) for s in range(1, 11)), sum(runs.values())))
        P("")
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main()
