# (889) c75cp 読み（記録・採否でない）：crownpc_s の生から事前登録 [5] の (i)〜(v) を数える。
#   python -X utf8 -I read889.py <生のフォルダ（crownpc_s<種>.tsv）> [国の名…]
#   国の名を渡さなければ、行の多い順に全ての国を並べる。数は全ての種の和。
import sys, os, glob, csv
from collections import defaultdict, Counter

csv.field_size_limit(1 << 30)
POL = ["絶対", "専制", "幕府", "立憲", "共和", "ブル独裁", "プロ独裁", "軍政", "無政府"]
CROWN_POL = {0, 1, 2}


def num(s):
    return float("nan") if s in ("nan", "") else float(s)


def pct(a, b):
    return f"{100.0 * a / b:.1f}%" if b else "—"


def quant(xs, p):
    if not xs:
        return float("nan")
    xs = sorted(xs)
    i = min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))
    return xs[i]


def main():
    d = sys.argv[1]
    want = sys.argv[2:]
    files = sorted(glob.glob(os.path.join(d, "crownpc_s*.tsv")))
    rise = defaultdict(lambda: [0, 0, 0, 0.0, 0.0, 0.0])   # 上がった四半期・召集が正・① が正・上がりの和・召集の和・① の和
    cm = defaultdict(lambda: [0, 0])                       # 立憲の四半期・pcNow=1
    rb = Counter(); coop = []; army = []; coopCm = []
    trans = Counter(); prevrows = {}
    expl = Counter(); drift = Counter(); driftNat = defaultdict(Counter); res9 = []; resMask = defaultdict(list); explNat = defaultdict(Counter)
    nrows = Counter(); seeds = 0
    terms = defaultdict(lambda: [0.0] * 6 + [0] * 7)       # 事象の無い行の項の和（召集・①・②・②′・巻き返し・残差）と 0 でない四半期の数・行の数
    for path in files:
        seeds += 1
        last = {}
        with open(path, encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                n = r["nation"]; nrows[n] += 1
                key = (r["nationUid"])
                expl[r["explainCode"]] += 1; explNat[n][r["explainCode"]] += 1
                if r["firstRowFlag"] == "1":
                    last[key] = r
                    continue
                pp, pn = num(r["pcPrevShare"]), num(r["pcNowShare"])
                ev = r["regimeEventFlag"] == "1"
                s, t1, t2 = num(r["summonTermShare"]), num(r["term1Share"]), num(r["term2Share"])
                if r["explainCode"] == "9":
                    m = int(r["driftMaskCode"]); drift[m] += 1; driftNat[n][m] += 1; res9.append(abs(num(r["residualShare"]))); resMask[m].append(abs(num(r["residualShare"])))
                if not ev and pn > pp:
                    a = rise[n]; a[0] += 1; a[3] += pn - pp
                    if s > 0: a[1] += 1; a[4] += s
                    if t1 > 0: a[2] += 1; a[5] += t1
                pc_code = int(r["polityCode"])
                if pc_code == 3:
                    cm[n][0] += 1
                    if pn == 1.0: cm[n][1] += 1
                if int(r["crownPrevCode"]) != 0:
                    rb["王冠あり"] += 1
                    for g in ("rollbackCoopGateFlag", "rollbackLegitimacyGateFlag", "rollbackArmyGateFlag"):
                        if r[g] == "1": rb[g] += 1
                    if all(r[g] == "1" for g in ("rollbackCrownGateFlag", "rollbackCoopGateFlag", "rollbackLegitimacyGateFlag", "rollbackArmyGateFlag")): rb["全て開いた"] += 1
                    if num(r["rollbackTermShare"]) < 0: rb["項が負"] += 1
                    coop.append(num(r["coopParliamentCrownPrevIndex"])); army.append(num(r["armyAllegiancePrevIndex"]))
                    if int(r["polityPrevCode"]) == 3: coopCm.append(num(r["coopParliamentCrownPrevIndex"]))
                if r["explainCode"] in ("0", "9"):   # 事象・下賜・床の無い行（残差は数え直しのずれだけ）
                    t = terms[n]
                    for i, col in enumerate(("summonTermShare", "term1Share", "term2Share", "term2pShare", "rollbackTermShare", "residualShare")):
                        v = num(r[col])
                        if v == v and v != 0: t[i] += v; t[6 + i] += 1
                    t[12] += 1
                prv = int(r["polityPrevCode"])
                if prv == 3 and pc_code in CROWN_POL:
                    trans["立憲→王権の政体"] += 1
                    trans["同じ四半期に②が負"] += t2 < 0
                    trans["その遷移が再ラベル（事象なし）"] += r["relabelFlag"] == "1"
                    if r["relabelFlag"] == "1":
                        trans["再ラベルの四半期に②′が負"] += num(r["term2pShare"]) < 0
                        trans["再ラベルの四半期に巻き返しが負"] += num(r["rollbackTermShare"]) < 0
                        trans["再ラベルの四半期の残差が負"] += num(r["residualShare"]) < 0
                    else:
                        trans[f"事象の区分{r['explainCode']}"] += 1
                    lp = last.get(key)
                    if lp is not None and lp["firstRowFlag"] == "0":
                        trans["前の四半期に②が負"] += num(lp["term2Share"]) < 0
                    trans[f"国:{n}"] += 1
                last[key] = r
    print(f"種 {seeds}・行 {sum(nrows.values())}")
    names = want or [n for n, _ in nrows.most_common()]
    print("\n(i) PC が上がった四半期（事象・最初の行を除く）のうち召集の項＞0 の割合と ① の項＞0 の割合・上がりの和に占める召集と ① の和")
    print("国\t上がった四半期\t召集＞0\t①＞0\t召集の和の割合\t①の和の割合")
    for n in names:
        a = rise.get(n)
        if not a or a[0] == 0: continue
        print(f"{n}\t{a[0]}\t{pct(a[1], a[0])}\t{pct(a[2], a[0])}\t{pct(a[4], a[3]) if a[3] else '—'}\t{pct(a[5], a[3]) if a[3] else '—'}")
    print("\n(ii) 立憲の四半期のうち pcNow＝1 の割合")
    print("国\t立憲の四半期\tpcNow=1")
    for n in names:
        b = cm.get(n)
        if not b or b[0] == 0: continue
        print(f"{n}\t{b[0]}\t{pct(b[1], b[0])}")
    print("\n(iii) 巻き返しの門（前の四半期の末に王冠のある国×四半期）")
    for k in ("王冠あり", "rollbackCoopGateFlag", "rollbackLegitimacyGateFlag", "rollbackArmyGateFlag", "全て開いた", "項が負"):
        print(f"{k}\t{rb[k]}\t{pct(rb[k], rb['王冠あり'])}")
    print(f"協力度（王冠あり）\t最小 {min(coop) if coop else float('nan'):.4g}\t下位10% {quant(coop, 0.1):.4g}\t中央 {quant(coop, 0.5):.4g}\t下位1% {quant(coop, 0.01):.4g}")
    print(f"協力度（前の四半期が立憲）\t件数 {len(coopCm)}\t最小 {min(coopCm) if coopCm else float('nan'):.4g}\t下位10% {quant(coopCm, 0.1):.4g}\t中央 {quant(coopCm, 0.5):.4g}")
    print(f"軍の帰属（王冠あり）\t最大 {max(army) if army else float('nan'):.4g}\t上位10% {quant(army, 0.9):.4g}\t中央 {quant(army, 0.5):.4g}\t0.3超の割合 {pct(sum(1 for x in army if x > 0.3), len(army))}")
    print("\n(iv) 立憲→絶対・専制・幕府の遷移")
    for k, v in sorted(trans.items(), key=lambda kv: (kv[0].startswith("国:"), -kv[1])):
        print(f"{k}\t{v}")
    print("\n(vi) explainCode 0 と 9 の行（事象・下賜・幕府の床の無い行）の項の和（全ての種の和・単位は PC）と 0 でない四半期の割合")
    print("国\t行\t召集の和\t①の和\t②の和\t②′の和\t巻き返しの和\t残差の和\t召集≠0\t①≠0\t②≠0\t②′≠0\t別の区分の行")
    for n in names:
        t = terms.get(n)
        if not t or t[12] == 0: continue
        e = explNat[n]; nev = e["3"] + e["4"] + e["5"]
        print(f"{n}\t{t[12]}\t" + "\t".join(f"{t[i]:.4g}" for i in range(6)) + "\t" + "\t".join(pct(t[6 + i], t[12]) for i in range(4)) + f"\t下賜{e['6']}・事象{nev}・床{e['7']}")
    print("\n(v) 説明の区分（explainCode：0 一致・1 最初の行・3 事象 Step の後・4 事象 Step の前・5 事象で説明できない・6 下賜・7 幕府の床・9 説明できない）")
    tot = sum(expl.values())
    for k in sorted(expl): print(f"{k}\t{expl[k]}\t{pct(expl[k], tot)}")
    print("driftMaskCode（9 の行・1 ①・2 ②・4 ②′・8 巻き返し・0 門が全て閉じていた）")
    for k in sorted(drift): print(f"{k}\t{drift[k]}\t|残差| 中央 {quant(resMask[k], 0.5):.3g}・上位10% {quant(resMask[k], 0.9):.3g}・最大 {max(resMask[k]):.3g}")
    if res9:
        print(f"9 の行の |残差|\t中央 {quant(res9, 0.5):.3g}\t上位10% {quant(res9, 0.9):.3g}\t最大 {max(res9):.3g}")
    print("9 の行の多い国（上位 10）")
    for n, c in sorted(driftNat.items(), key=lambda kv: -sum(kv[1].values()))[:10]:
        print(f"{n}\t{sum(c.values())}\t" + "・".join(f"{k}:{v}" for k, v in sorted(c.items())))


if __name__ == "__main__":
    sys.exit(main())
