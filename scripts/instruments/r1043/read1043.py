# (1043) c98cp 設計の材料（読むだけ・腕 0 本）：crownpc_s の生から、王権と議会の (i)〜(iv) の数え直しと、(2)・(3a)・(3b) の紙の量を数える。
#   python -X utf8 -I read1043.py <生のフォルダ（crownpc_s<種>.tsv）> [国の名…]
#   エンジンの関数は呼ばない。式は CrownParliament.cs の現物（行は material.md）を、この台本が自分で書く。数は全ての種の和。
#   AI の読み・確証なし。紙の項は〈前の四半期の末の断面〉で当てる（計器の限界①と同じずれを持つ）。
import sys, os, glob, csv
from collections import defaultdict, Counter

csv.field_size_limit(1 << 30)
CROWN_POL = {0, 1, 2}           # 絶対・専制・幕府（Model.cs:21）
RATE = 0.02                     # SummonParliamentRate（Coefficients.cs:2967）＝巻き返しの率も同じ係数を読む（CrownParliament.cs:52）
ARMY_PC = 0.02                  # ArmyPcPressure（Coefficients.cs:1743）
LEG_MIN, ARMY_MIN = 0.7, 0.3    # CrownParliament.cs:15-16


def num(s):
    return float("nan") if s in ("nan", "") else float(s)


def pct(a, b):
    return f"{100.0 * a / b:.1f}%" if b else "—"


def q(xs, p):
    if not xs:
        return float("nan")
    xs = sorted(xs)
    return xs[min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))]


def main():
    d = sys.argv[1]
    want = sys.argv[2:]
    files = sorted(glob.glob(os.path.join(d, "crownpc_s*.tsv")))
    nrows = Counter()
    # A：上がり（事象と最初の行を除く）の正の寄与＝召集・①・下賜の行の跳び・その他の正の残差
    up = defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0.0])
    # B：立憲の四半期の張り付き
    cm = defaultdict(lambda: [0, 0, 0, 0, 0, [], []])     # 行・PC=1・|PC−DG|<1e-3・<1e-6・PC≧DG−1e-9・PC の列・DG の列
    clampHit = Counter()
    # C：巻き返しの門
    rb = Counter(); rbNat = defaultdict(Counter)
    # D：立憲→王権の政体
    tr = defaultdict(Counter); relabPrevPc = defaultdict(list)
    # 立憲の行（前の四半期が立憲・事象なし）の項の和
    cmTerms = defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
    # 0 行・1 召集・2 ①・3 ②・4 ②′・5 巻き返し・6 残差・7 紙(3a-1)・8 紙(3a-2)・9 紙(3a-3)・10 紙(3b)・11 ②′ の行で PC<0.5 の旧
    # E：② の門と鏡の門
    g2 = Counter(); diff = []; diffCm = []
    seeds = 0
    for path in files:
        seeds += 1
        with open(path, encoding="utf-8", newline="") as f:
            for r in csv.DictReader(f, delimiter="\t"):
                n = r["nation"]; nrows[n] += 1
                if r["firstRowFlag"] == "1":
                    continue
                pp, pn = num(r["pcPrevShare"]), num(r["pcNowShare"])
                ev = r["regimeEventFlag"] == "1"
                ex = r["explainCode"]
                s, t1, t2, t2p, rbk, res = (num(r[k]) for k in ("summonTermShare", "term1Share", "term2Share", "term2pShare", "rollbackTermShare", "residualShare"))
                pol, prv = int(r["polityCode"]), int(r["polityPrevCode"])
                crownPrev = int(r["crownPrevCode"]) != 0
                seizPrev = int(r["seizurePrevCode"]) != 0
                # A
                if not ev and pn > pp:
                    a = up[n]; a[0] += 1
                    if s > 0: a[1] += s
                    if t1 > 0: a[2] += t1
                    if ex == "6":
                        known = sum(v for v in (s, t1, t2, t2p, rbk) if v == v)
                        a[3] += max(0.0, pn - pp - known)
                    elif res == res and res > 0:
                        a[4] += res
                # B
                if pol == 3:
                    b = cm[n]; b[0] += 1
                    dg = num(r["debtGripShare"])
                    if pn == 1.0: b[1] += 1
                    if dg == dg:
                        if abs(pn - dg) < 1e-3: b[2] += 1
                        if abs(pn - dg) < 1e-6: b[3] += 1
                        if pn >= dg - 1e-9: b[4] += 1
                        b[6].append(dg)
                    b[5].append(pn)
                if r["summonClampHitFlag"] == "1": clampHit[n] += 1
                # C
                coop, leg, army = num(r["coopParliamentCrownPrevIndex"]), num(r["legitimacyIndex"]), num(r["armyAllegiancePrevIndex"])
                gc, gl, ga = r["rollbackCoopGateFlag"] == "1", r["rollbackLegitimacyGateFlag"] == "1", r["rollbackArmyGateFlag"] == "1"
                if crownPrev:
                    rb["王冠あり"] += 1
                    rb["協力度の門"] += gc; rb["正統性の門"] += gl; rb["軍の門"] += ga
                    rb["協力度＋正統性"] += gc and gl; rb["協力度＋軍"] += gc and ga; rb["正統性＋軍"] += gl and ga
                    rb["三つ"] += gc and gl and ga
                    rb["項が負"] += rbk < 0
                    if prv == 3:
                        rb["王冠あり・前が立憲"] += 1; rb["前が立憲・正統性＋軍"] += gl and ga; rb["前が立憲・協力度の門"] += gc
                    rbNat[n]["王冠あり"] += 1; rbNat[n]["協力度の門"] += gc; rbNat[n]["正統性の門"] += gl; rbNat[n]["軍の門"] += ga; rbNat[n]["正統性＋軍"] += gl and ga
                    rbNat[n]["協力度の最小×1e4"] = min(rbNat[n].get("協力度の最小×1e4", 10 ** 9), int(round(coop * 1e4))) if coop == coop else rbNat[n].get("協力度の最小×1e4", 10 ** 9)
                # D
                if prv == 3 and pol in CROWN_POL:
                    t = tr[n]; t["立憲→王権"] += 1
                    if r["relabelFlag"] == "1":
                        t["再ラベル"] += 1
                        t["再ラベル・②′が負"] += t2p < 0
                        t["再ラベル・召集が正"] += s > 0
                        t["再ラベル・巻き返しが負"] += rbk < 0
                        t["再ラベル・②が負"] += t2 < 0
                        relabPrevPc[n].append(pp)
                    else:
                        t[f"事象の区分{ex}"] += 1
                # 立憲の行の項の和と紙
                if prv == 3 and not ev and ex in ("0", "9"):
                    c = cmTerms[n]; c[0] += 1
                    for i, v in enumerate((s, t1, t2, t2p, rbk, res), start=1):
                        if v == v: c[i] += v
                    if crownPrev and coop == coop and leg == leg and army == army:
                        hostile = max(0.0, -coop)
                        # (3a-1)：−0.5 の門を比例に（正統性と軍の門は残す）＝−率×max(0,−協力度)
                        if leg > LEG_MIN and army > ARMY_MIN: c[7] -= RATE * hostile
                        # (3a-2)：門を全て比例に＝−率×max(0,−協力度)×正統性×max(0,軍の帰属)
                        c[8] -= RATE * hostile * leg * max(0.0, army)
                        # (3a-3)：予算の闘争の量（Government.cs:919 の積 PC×max(0,−協力度)）で比例・正統性と軍の門は残す
                        if leg > LEG_MIN and army > ARMY_MIN: c[9] -= RATE * max(0.0, pp) * hostile
                    lean, ap = num(r["soldierLeanIndex"]), num(r["armyPowerIndex"])
                    if army == army and ap == ap and army > 0:
                        c[10] -= ARMY_PC * army * ap      # (3b) 記録：②′ の向きを前の四半期の軍の帰属から
                # E
                if crownPrev and not seizPrev:
                    rep, cur, crown = num(r["repSupportIndex"]), num(r["curSupportIndex"]), num(r["crownSupportIndex"])
                    inert, mob, coer = num(r["inertiaIndex"]), num(r["mobilizationIndex"]), num(r["effectiveCoerceIndex"])
                    if crown == crown and rep == rep:
                        g2["行"] += 1; diff.append(crown - rep)
                        g2["crown＞rep"] += crown > rep
                        g2["crown−rep＞慣性"] += (crown - rep) > inert
                        g2["鏡の門（保持力＞動員）"] += coer > mob
                        g2["①の支持の門（rep−cur＞慣性）"] += (rep - cur) > inert
                        g2["①の保持力の門（動員＞保持力）"] += mob > coer
                        g2["②の門（記録の列）"] += r["term2GateFlag"] == "1"
                        if prv == 3:
                            g2["前が立憲"] += 1; diffCm.append(crown - rep)
                            g2["前が立憲・鏡の門"] += coer > mob
                            g2["前が立憲・crown−cur＞0"] += crown > cur
    names = want or [n for n, _ in nrows.most_common()]
    print(f"種 {seeds}・行 {sum(nrows.values())}")
    print("\n[A] PC が上がった四半期（事象・最初の行を除く）の正の寄与の割合（召集・①・下賜の行の跳び・その他の正の残差）")
    print("国\t上がった四半期\t召集\t①\t下賜\t正の残差")
    for n in names:
        a = up.get(n)
        if not a or a[0] == 0: continue
        tot = a[1] + a[2] + a[3] + a[4]
        print(f"{n}\t{a[0]}\t{pct(a[1], tot)}\t{pct(a[2], tot)}\t{pct(a[3], tot)}\t{pct(a[4], tot)}")
    print("\n[B] 立憲の四半期の張り付き（PC＝1・PC と DebtGrip の差）と summonClampHitFlag")
    print("国\t立憲の四半期\tPC=1\t|PC−DG|<1e-3\t<1e-6\tPC≧DG（召集が止まる側）\tPC の中央\tDG の中央\t切りの印")
    for n in names:
        b = cm.get(n)
        if not b or b[0] == 0: continue
        print(f"{n}\t{b[0]}\t{pct(b[1], b[0])}\t{pct(b[2], b[0])}\t{pct(b[3], b[0])}\t{pct(b[4], b[0])}\t{q(b[5], 0.5):.3f}\t{q(b[6], 0.5):.3f}\t{clampHit[n]}")
    print(f"切りの印（全ての国）\t{sum(clampHit.values())}")
    print("\n[C] 巻き返しの門（前の四半期の末に王冠のある国×四半期）")
    for k in ("王冠あり", "協力度の門", "正統性の門", "軍の門", "協力度＋正統性", "協力度＋軍", "正統性＋軍", "三つ", "項が負", "王冠あり・前が立憲", "前が立憲・協力度の門", "前が立憲・正統性＋軍"):
        base = rb["王冠あり・前が立憲"] if k.startswith("前が立憲") else rb["王冠あり"]
        print(f"{k}\t{rb[k]}\t{pct(rb[k], base)}")
    print("国\t王冠あり\t協力度の門\t正統性の門\t軍の門\t正統性＋軍\t協力度の最小")
    for n in names:
        c = rbNat.get(n)
        if not c or c["王冠あり"] == 0: continue
        m = c.get("協力度の最小×1e4", 10 ** 9)
        print(f"{n}\t{c['王冠あり']}\t{c['協力度の門']}\t{pct(c['正統性の門'], c['王冠あり'])}\t{pct(c['軍の門'], c['王冠あり'])}\t{pct(c['正統性＋軍'], c['王冠あり'])}\t{m / 1e4 if m < 10 ** 9 else '—'}")
    print("\n[D] 立憲→絶対・専制・幕府（国ごと）")
    print("国\t立憲→王権\t再ラベル\t②′が負\t召集が正\t巻き返しが負\t②が負\t事象\t再ラベルの前の PC の中央")
    allt = Counter()
    for n in sorted(tr, key=lambda k: -tr[k]["立憲→王権"]):
        t = tr[n]; allt.update(t)
        if want and n not in want: continue
        evn = sum(v for k, v in t.items() if k.startswith("事象"))
        print(f"{n}\t{t['立憲→王権']}\t{t['再ラベル']}\t{t['再ラベル・②′が負']}\t{t['再ラベル・召集が正']}\t{t['再ラベル・巻き返しが負']}\t{t['再ラベル・②が負']}\t{evn}\t{q(relabPrevPc[n], 0.5):.3f}")
    evn = sum(v for k, v in allt.items() if k.startswith("事象"))
    print(f"全ての国\t{allt['立憲→王権']}\t{allt['再ラベル']}\t{allt['再ラベル・②′が負']}\t{allt['再ラベル・召集が正']}\t{allt['再ラベル・巻き返しが負']}\t{allt['再ラベル・②が負']}\t{evn}\t—")
    print("\n[F] 前の四半期が立憲・事象なし（explainCode 0・9）の行の項の和（単位 PC・種の和）と紙の項（AI の案・確証なし）")
    print("国\t行\t召集\t①\t②\t②′\t巻き返し\t残差\t紙3a-1\t紙3a-2\t紙3a-3\t紙3b\t紙3b÷②′")
    for n in names:
        c = cmTerms.get(n)
        if not c or c[0] == 0: continue
        ratio = f"{c[10] / c[4]:.3f}" if c[4] else "—"
        print(f"{n}\t{c[0]}\t" + "\t".join(f"{c[i]:.4g}" for i in range(1, 11)) + f"\t{ratio}")
    tot = [sum(c[i] for c in cmTerms.values()) for i in range(12)]
    print("全ての国\t" + f"{tot[0]}\t" + "\t".join(f"{tot[i]:.4g}" for i in range(1, 11)) + (f"\t{tot[10] / tot[4]:.3f}" if tot[4] else "\t—"))
    print("\n[E] ② の門と鏡の門（前の四半期の末に王冠があり奪取なしの行）")
    for k in ("行", "crown＞rep", "crown−rep＞慣性", "②の門（記録の列）", "鏡の門（保持力＞動員）", "①の支持の門（rep−cur＞慣性）", "①の保持力の門（動員＞保持力）", "前が立憲", "前が立憲・鏡の門", "前が立憲・crown−cur＞0"):
        base = g2["前が立憲"] if k.startswith("前が立憲") and k != "前が立憲" else g2["行"]
        print(f"{k}\t{g2[k]}\t{pct(g2[k], base)}")
    if diff:
        print(f"crown−rep（王冠あり）\t最大 {max(diff):.4g}\t上位1% {q(diff, 0.99):.4g}\t中央 {q(diff, 0.5):.4g}")
    if diffCm:
        print(f"crown−rep（前が立憲）\t最大 {max(diffCm):.4g}\t上位1% {q(diffCm, 0.99):.4g}\t中央 {q(diffCm, 0.5):.4g}")


if __name__ == "__main__":
    sys.exit(main())
