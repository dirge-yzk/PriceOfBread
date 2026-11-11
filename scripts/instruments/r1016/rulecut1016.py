# (1016) 紙 2：官僚制度の〈次の一段で失う仲介者の取り分の割合〉の近似（読むだけ）
# 使い方: python -X utf8 -I rulecut1016.py <出力ファイル> <名>=<生のフォルダ> ...（rule_s・class_s・burblock_s を読む）
# 式（エンジンの写し・計器と同じく自分で書く＝エンジンの関数を呼ばない）：
#  身分の度合い E：猟官 1・資格任用 0.5・専門 0（Privileges.cs EstateDegree）。土地改革の度合い L：なし 1・限定 0.5・全面 0。
#  仲介の度合い I＝max(E, L)（Privileges.cs IntermediaryDegree）。官僚制度の次の段 E'＝E(段+1)（専門の次は専門）。I'＝max(E', L)。
#  在地の目標＝RuleInit[1]×I（Politics.cs 在地の目標）。取り分 cut＝(R1＋R0×(1−f))×PrivilegeTaxDiscount×査定（Fiscal.cs TaxScan）。
#  上限：失う取り分 ≦ cut×(I−I')/I（州の取り分の全てが在地の区分から来るとみなす・R0×(1−f) の分も落ちると数える＝上限）。
#  中：在地の区分 R1 の分だけが動き、動いた R1 は国家の区分へ移って (1−f) は仲介者に残る＝cut×R1×(I−I')/I×f÷(R1＋R0×(1−f))。f は州の欄に無い＝国の adminFillMeanShare（nation_s）。
#  分母＝旧支配層の所得（class_s の貴族＋聖職者・年 1 回の断面）。
import sys, os, statistics as st

EST = [1.0, 0.5, 0.0]
LRD = [1.0, 0.5, 0.0]
PERIODS = [(1783, 1830), (1831, 1870), (1871, 1910), (1911, 1951)]


def read_tsv(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').split('\n')
    h = lines[0].rstrip('\r').split('\t')
    rows = [l.rstrip('\r').split('\t') for l in lines[1:] if l.strip()]
    return h, rows


def fnum(x):
    try:
        v = float(x)
        return v if v == v else 0.0
    except ValueError:
        return 0.0


def qs(xs):
    if not xs: return "n=0"
    s = sorted(xs)
    g = lambda p: s[min(len(s) - 1, int(round(p * (len(s) - 1))))]
    return f"n={len(s)} p10={g(.1):.4f} 中央={g(.5):.4f} p90={g(.9):.4f} p99={g(.99):.4f} 最大={s[-1]:.4f}"


def main():
    out = sys.argv[1]
    L = ["# (1016) 官僚制度：次の一段で失う仲介者の取り分の割合（近似）", ""]
    for name, d in (a.split('=', 1) for a in sys.argv[2:]):
        pw_moves = 0.0; pw_all = 0.0          # 人口の重み：官僚制度の次の段で I が下がる州
        pw_lr_moves = 0.0                      # 土地改革の次の段で I が下がる州
        pw_tie = 0.0                           # E＝L（どちらの段 1 つでも I が下がらない）で I＞0 の州
        idx_mismatch = 0; rows_r = 0
        up = {p: [] for p in PERIODS}; mid = {p: [] for p in PERIODS}
        cut_over_I0 = []
        sumUp = 0.0; sumMid = 0.0; sumI0 = 0.0
        nat_moves = 0; nat_rows = 0
        need_rows = 0; need_up = 0; bb_rows = 0
        for s in range(1, 11):
            pr = os.path.join(d, f"rule_s{s}.tsv"); pc = os.path.join(d, f"class_s{s}.tsv"); pn = os.path.join(d, f"nation_s{s}.tsv")
            if not (os.path.exists(pr) and os.path.exists(pc)): continue
            fill = {}
            if os.path.exists(pn):
                hn, rn = read_tsv(pn)
                a, b, c = hn.index("quarter"), hn.index("nation"), hn.index("adminFillMeanShare")
                for r in rn: fill[(int(r[a]), r[b])] = fnum(r[c])
            hc, rc = read_tsv(pc)
            it, inn, icl, iea, iy = (hc.index(c) for c in ("time", "nation", "classId", "earnedPerMonth", "year"))
            I0 = {}; yr = {}
            for r in rc:
                if not r[it].startswith('q'): continue
                if int(r[icl]) in (0, 6):
                    k = (int(r[it][1:]), r[inn]); I0[k] = I0.get(k, 0.0) + fnum(r[iea]); yr[k] = int(r[iy])
            h, rows = read_tsv(pr)
            ix = {c: h.index(c) for c in ("quarter", "year", "nation", "popK", "ruleStateShare", "ruleLocalShare", "ruleInitLocalShare",
                                           "bureaucracyLawCode", "landReformLawCode", "intermediaryDegreeIndex", "ruleCutPerMonth")}
            natUp = {}; natMid = {}; natMove = {}
            for r in rows:
                rows_r += 1
                k = (int(r[ix["quarter"]]), r[ix["nation"]])
                pop = fnum(r[ix["popK"]]); bl = int(fnum(r[ix["bureaucracyLawCode"]])); lr = int(fnum(r[ix["landReformLawCode"]]))
                E = EST[min(bl, 2)]; Lr = LRD[min(lr, 2)]; I = max(E, Lr)
                if abs(I - fnum(r[ix["intermediaryDegreeIndex"]])) > 1e-9: idx_mismatch += 1
                E2 = EST[min(bl + 1, 2)]; I2 = max(E2, Lr)
                L2 = LRD[min(lr + 1, 2)]; I3 = max(E, L2)
                init1 = fnum(r[ix["ruleInitLocalShare"]])
                pw_all += pop
                if init1 > 0 and I - I2 > 1e-12: pw_moves += pop; natMove[k] = 1
                if init1 > 0 and I - I3 > 1e-12: pw_lr_moves += pop
                if init1 > 0 and I > 0 and abs(E - Lr) < 1e-12: pw_tie += pop
                cut = fnum(r[ix["ruleCutPerMonth"]])
                if cut > 0 and I > 1e-12 and I - I2 > 1e-12:
                    fr = (I - I2) / I
                    natUp[k] = natUp.get(k, 0.0) + cut * fr
                    R1 = fnum(r[ix["ruleLocalShare"]]); R0 = fnum(r[ix["ruleStateShare"]]); f = fill.get(k, 0.0)
                    den = R1 + R0 * (1 - f)
                    if den > 1e-12: natMid[k] = natMid.get(k, 0.0) + cut * R1 * fr * f / den
                elif k not in natUp:
                    natUp.setdefault(k, 0.0); natMid.setdefault(k, 0.0)
            # ルールの表は全ての四半期・class は年 1 回＝class のある四半期だけ割る
            for k, i0 in I0.items():
                if k not in natUp or i0 <= 1e-9: continue
                y = yr[k]; per = next(p for p in PERIODS if p[0] <= y <= p[1])
                up[per].append(natUp[k] / i0); mid[per].append(natMid[k] / i0)
                sumUp += natUp[k]; sumMid += natMid[k]; sumI0 += i0
                nat_rows += 1; nat_moves += natMove.get(k, 0)
            # 官僚制度の〈要るから上げる〉（SelfPlay.cs の BurObserve）
            pb = os.path.join(d, f"burblock_s{s}.tsv")
            if os.path.exists(pb):
                hb, rb = read_tsv(pb)
                ib, io, inn2 = hb.index("bur"), hb.index("burOut"), hb.index("need")
                for r in rb:
                    bb_rows += 1
                    if int(fnum(r[inn2])) > int(fnum(r[ib])): need_rows += 1
                    if int(fnum(r[io])) > int(fnum(r[ib])): need_up += 1
        L.append(f"## 腕 {name}")
        L.append(f"州×四半期の行 {rows_r}・I の写しと intermediaryDegreeIndex の食い違い {idx_mismatch}")
        L.append(f"- 人口の重みで、官僚制度の次の段で I が下がる州（RuleInit[1]＞0）の割合 {pw_moves/pw_all:.4f}")
        L.append(f"- 同じく土地改革の次の段で I が下がる州の割合 {pw_lr_moves/pw_all:.4f}")
        L.append(f"- E＝L で I＞0（どちらの 1 段でも I が下がらない）州の割合 {pw_tie/pw_all:.4f}")
        L.append(f"- 国×年の断面で、官僚制度の次の段で I が下がる州を 1 つでも持つ割合 {nat_moves}/{nat_rows}")
        for p in PERIODS: L.append(f"- 上限 ÷旧支配層の所得 {p[0]}–{p[1]}: {qs(up[p])}")
        for p in PERIODS: L.append(f"- 中 ÷旧支配層の所得 {p[0]}–{p[1]}: {qs(mid[p])}")
        L.append(f"- 上限の形が 1 を超えた国×断面 {sum(1 for p in PERIODS for x in up[p] if x > 1.0)}（分母が痩せると跳ねる）")
        L.append(f"- 和÷和：上限 {sumUp/sumI0:.5f}・中 {sumMid/sumI0:.5f}" if sumI0 > 0 else "- 和÷和 nan")
        L.append(f"- burblock：行 {bb_rows}・要る段＞今 {need_rows}・出した目標＞今 {need_up}")
        allup = [x for p in PERIODS for x in up[p]]
        L.append(f"- 案 (A) 底あり −2×(0.5＋上限) の値: {qs([-2*(0.5+x) for x in allup])}（今の札 −1）")
        L.append("")
    with open(out, 'wb') as f:
        f.write(('\n'.join(L) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
