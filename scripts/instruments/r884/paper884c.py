# (884) c74ds 読むだけの紙の台本 3/3：国ごとの時期の表（取り分 r×V_eng÷R・輸出の地代の割合）と m852 の開幕の輸出の地代の割合（measurements/m865_20261105/landshare_paper865.txt）との並べ
# 使い方：python -X utf8 -I paper884c.py <world.json> <base_ins> <landshare_paper865.txt> <出力.txt>
# 定義は paper884.py と同じ（V_eng・R・r）。(種,国,窓) ごとに四半期の和の比→種の中央。
import sys, os, json, collections

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs)
    return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def ff(x, d=2): return ("%." + str(d) + "f") % x if x == x else "nan"

SE_CAP, SERF_FLOOR = 0.5, 0.15
def rentcap(level, full): return full if level == "Full" else (full + (1 - full) * 0.5 if level == "Limited" else 1.0)
WINDOWS = [(1783, 1785, "1783-85"), (1786, 1799, "1786-99"), (1800, 1849, "1800-49"), (1850, 1899, "1850-99"), (1900, 1950, "1900-50")]
def wins(y): return [w[2] for w in WINDOWS if w[0] <= y <= w[1]]

def main():
    wpath, base, p865, opath = sys.argv[1:5]
    world = json.load(open(wpath, encoding="utf-8")); reff = {}
    for n in world["nations"]:
        fls = n.get("farmLordShare", 0.6); se = n.get("initialSerfEmancipation") or "Full"
        reff[n["name"]] = min(max(0.3, min(fls, 1 - SERF_FLOOR)), min(1.0, rentcap(se, SE_CAP)))
    acc = collections.defaultdict(lambda: [0.0, 0.0])   # (seed,nat,win) → [V_eng, R]
    for seed in range(1, 11):
        with open(os.path.join(base, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if not (fnum(c[ix["subPopK"]]) > 0): continue
                y = int(c[ix["year"]]); nat = c[ix["nation"]]
                eng = fnum(c[ix["engineSubOutputPerMonth"]]); px = fnum(c[ix["stapleNeedWeightedPrice"]])
                ro = fnum(c[ix["rentToOthersPerMonth"]]); rf = fnum(c[ix["rentToFarmersPerMonth"]])
                ve = eng * px if (eng == eng and px == px) else 0.0
                R = max(0.0, ro if ro == ro else 0.0) + max(0.0, rf if rf == rf else 0.0)
                for wn in wins(y): a = acc[(seed, nat, wn)]; a[0] += ve; a[1] += R
    exq = collections.defaultdict(lambda: [0.0, 0.0])
    ex0 = collections.defaultdict(list)
    for seed in range(1, 11):
        with open(os.path.join(base, "cornint_s%d.tsv" % seed), encoding="utf-8") as f:
            hdr = f.readline().rstrip("\n").split("\t"); ix = {h: i for i, h in enumerate(hdr)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                y = int(c[ix["year"]]); nat = c[ix["nation"]]
                dom = fnum(c[ix["rentDomTotalPerMonth"]]); dom = max(0.0, dom) if dom == dom else 0.0
                ex = max(0.0, sum(v for v in (fnum(c[ix["rentExpPerMonth%d" % b]]) for b in range(3)) if v == v))
                for wn in wins(y): a = exq[(seed, nat, wn)]; a[0] += dom; a[1] += ex
                if c[ix["quarter"]] == "0": ex0[nat].append((dom, ex))
    m852 = {}
    with open(p865, encoding="utf-8") as f:
        for line in f:
            s = line.rstrip("\n").split("\t")
            if len(s) == 9 and s[0] != "nation":
                d, e = fnum(s[1]), fnum(s[2])
                if d == d and e == e and d + e > 0: m852[s[0]] = (e / (d + e), fnum(s[6]))
    pick = ["ハワイ王国", "イギリス", "コンゴ王国", "ネーデルラント", "オマーン", "ラヘジ", "ボルヌ帝国", "スペイン", "ポルトガル", "ポーランド", "オヨ帝国", "教皇領", "ラオス", "朝鮮",
            "清", "フランス", "ロシア", "プロイセン", "オーストリア", "日本", "アメリカ", "オスマン", "シャム"]
    out = []; P = out.append
    P("(884) c74ds 紙の数 3/3（台本 paper884c.py・腕 %s・シミュは回していない）" % base)
    P("[W] 国ごとの取り分 r×V_eng÷R（自給の収穫を取り分で土地税の基数へ広げたときの基数の増え）の時期（10 種の中央）・輸出の地代÷全ての地代（cornint の近似・同じ窓）")
    P("   国\tr\t" + "\t".join(w[2] for w in WINDOWS) + "\t輸出の割合 " + "\t".join(w[2] for w in WINDOWS) + "\t開幕の輸出の割合 m852（(865)）\tm873\t開幕の年の地代÷名目GDP m852")
    for nat in pick:
        if nat not in reff: continue
        r = reff[nat]; vals = []; exs = []
        for _, _, wn in WINDOWS:
            vals.append(med([r * acc[(s, nat, wn)][0] / acc[(s, nat, wn)][1] for s in range(1, 11) if acc[(s, nat, wn)][1] > 0]))
            exs.append(med([exq[(s, nat, wn)][1] / (exq[(s, nat, wn)][0] + exq[(s, nat, wn)][1]) for s in range(1, 11) if (exq[(s, nat, wn)][0] + exq[(s, nat, wn)][1]) > 0]))
        e0 = med([e / (d + e) for d, e in ex0[nat] if d + e > 0])
        o = m852.get(nat, (float("nan"), float("nan")))
        P("   %s\t%.3f\t%s\t%s\t%s\t%s\t%s" % (nat, r, "\t".join(ff(v) for v in vals), "\t".join(ff(v, 3) for v in exs), ff(o[0], 3), ff(e0, 3), ff(o[1], 3)))
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main()
