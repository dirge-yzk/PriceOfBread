# (844) c68ds 読むだけの紙の台本 3/3：割譲の事象ごとの支配の 4 区分（今＝踏襲）と、形 C1 を当てたときの見込み
# 入力：m839 の計器つき 10 種の生（keep_839/base_ins の cess_s・rule_s・secession_s）。シミュは回さない。
# 使い方：python -I paper844_cession.py <base_ins のフォルダ> <出力.txt>
# ⚠限界：①rule_s は計器の国だけ毎四半期・他の国は年末＝割譲の前後の行は最大 1 年離れる（離れた四半期の数を列で出す）
#   ②rule_s は外国持分の主体名を出さない（foreignRulerCount だけ）＝〈受け手自身の持分〉の穴は数えられない＝候補（持分の主体が 1 つ以上の州）の上界だけ
#   ③AdminFill の列が無い＝C1 の瞬間の税の届きの落ち R0×(1−d)×AdminFill は AdminFill＝1 の上界（d＝1−PrivilegeTaxDiscount 0.3＝0.7）
#   ④cess_s の行＝割譲の 1 州（Diplomacy.CedeRegion の呼び出しに対応するかは cpeace_s の cedeRealCount の和と突き合わせて名乗る）
import sys, os, collections, math

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def med(xs):
    xs = sorted(x for x in xs if x == x)
    if not xs: return float("nan")
    n = len(xs); return xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])

def qt(xs, p):
    xs = sorted(x for x in xs if x == x)
    return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else float("nan")

def main(base, opath):
    out = []; P = out.append
    D = collections.defaultdict(list); C = collections.Counter()
    per_seed = []
    for seed in range(1, 11):
        # rule_s
        reg = collections.defaultdict(list); natpop = collections.defaultdict(float); natflag = {}
        with open(os.path.join(base, "rule_s%d.tsv" % seed), encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                qq = int(c[ix["quarter"]]); y = int(c[ix["year"]]); nat = c[ix["nation"]]; rg = c[ix["region"]]
                pop = fnum(c[ix["popK"]])
                R = [fnum(c[ix[k]]) for k in ("ruleStateShare", "ruleLocalShare", "ruleForeignShare", "ruleUngovShare")]
                reg[rg].append((qq, y, nat, R, int(c[ix["foreignRulerCount"]]), fnum(c[ix["dominanceShare"]]), pop, c[ix["tier"]]))
                natpop[(qq, nat)] += pop if pop == pop else 0
                natflag[(qq, nat)] = c[ix["treatyPortInForceFlag"]]
        # secession_s の goFlag
        go = collections.defaultdict(list)
        with open(os.path.join(base, "secession_s%d.tsv" % seed), encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                if c[ix["goFlag"]] == "1": go[c[ix["region"]]].append(int(c[ix["year"]]))
        # cpeace の cedeRealCount の和
        creal = 0
        with open(os.path.join(base, "cpeace_s%d.tsv" % seed), encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t"); creal += int(fnum(c[ix["cedeRealCount"]]))
        ncess = 0; goall = sum(len(v) for v in go.values())
        with open(os.path.join(base, "cess_s%d.tsv" % seed), encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                c = line.rstrip("\n").split("\t")
                ncess += 1
                y = int(c[ix["year"]]); win = c[ix["winner"]]; los = c[ix["loser"]]; rg = c[ix["region"]]; tier = c[ix["tier"]]
                rows = reg.get(rg, [])
                before = [r for r in rows if r[2] == los and r[1] <= y]
                after = [r for r in rows if r[2] == win and r[1] >= y]
                if not before or not after:
                    C["前後の行が揃わない（計器の国でない・年末の行の前に再び動いた）"] += 1; continue
                b = before[-1]; a = after[0]
                C["前後の行が揃った"] += 1
                D["gapQ"].append(a[0] - b[0])
                for k, nm in enumerate(("R0", "R1", "R2", "R3")):
                    D[nm + "_before"].append(b[3][k]); D[nm + "_after"].append(a[3][k])
                D["dR0"].append(a[3][0] - b[3][0]); D["dDom"].append(a[5] - b[5])
                if a[0] - b[0] <= 1: D["dR0_adj"].append(a[3][0] - b[3][0]); C["隣の四半期で揃った"] += 1
                tot = natpop.get((a[0], win), float("nan"))
                ps = a[6] / tot if tot and tot > 0 else float("nan")
                D["popShare"].append(ps)
                D["taxDropUpper"].append(ps * b[3][0] * 0.3 if ps == ps else float("nan"))
                D["reachDrop"].append(ps * b[3][0] if ps == ps else float("nan"))
                D["R0R1_before"].append(b[3][0] + b[3][1])
                if tier == "Colonial": C["植民地格"] += 1
                else: C["本国の州（編入）"] += 1
                if b[4] > 0:
                    C["割譲の前に外国持分の主体が 1 つ以上（穴の候補の上界）"] += 1
                    fa = natflag.get((a[0], win)); fb = natflag.get((b[0], win))
                    if fa == "1": C["その候補のうち受け手の条約港の旗が割譲の後に 1"] += 1
                    if fa == "1" and fb == "0": C["その候補のうち受け手の旗が 0→1"] += 1
                gs = [gy for gy in go.get(rg, []) if y <= gy <= y + 10]
                if gs: C["割譲の後 10 年以内にその州で独立の戦争の開始（goFlag）"] += 1
                if b[3][1] > b[3][0]: C["割譲の前に在地＞国家（休眠の述語が今でも真）"] += 1
                if b[3][0] > 0: C["C1 で休眠の述語 Military.cs:273 が真になる（R0＞0 の州）"] += 1
        per_seed.append((seed, ncess, creal, goall))
    P("(844) 割譲の事象ごとの支配の 4 区分（m839＝keep_839/base_ins の 10 種・台本 paper844_cession.py）")
    P("種ごと：種・cess_s の行（割譲の州）・cpeace_s の cedeRealCount の和・secession_s の goFlag の総数")
    for s in per_seed: P("  %d\t%d\t%d\t%d" % s)
    P("割譲の行の合計 %d・cedeRealCount の和 %d" % (sum(s[1] for s in per_seed), sum(s[2] for s in per_seed)))
    for k, v in sorted(C.items()): P("  %s：%d" % (k, v))
    P("")
    P("前後の行の離れ（四半期）：中央 %.0f・p90 %.0f" % (med(D["gapQ"]), qt(D["gapQ"], .9)))
    for nm in ("R0", "R1", "R2", "R3"):
        P("  %s 割譲の前 中央 %.3f（p10 %.3f・p90 %.3f）・後 中央 %.3f" % (nm, med(D[nm + "_before"]), qt(D[nm + "_before"], .1), qt(D[nm + "_before"], .9), med(D[nm + "_after"])))
    P("  国家区分の差（後−前）：中央 %.4f・p10 %.4f・p90 %.4f／隣の四半期で揃った行だけ 中央 %.4f（%d 件）" % (med(D["dR0"]), qt(D["dR0"], .1), qt(D["dR0"], .9), med(D["dR0_adj"]), len(D["dR0_adj"])))
    P("  支配度の差（後−前）：中央 %.4f（割譲は支配度を動かさない＝Territory.cs:271〜275）" % med(D["dDom"]))
    P("  割譲の州の人口÷受け手の国の人口：中央 %.4f・p90 %.4f・最大 %.4f" % (med(D["popShare"]), qt(D["popShare"], .9), max([x for x in D["popShare"] if x == x] or [float('nan')])))
    P("  C1 の瞬間の受け手の国の税の届きの落ち（上界＝人口の割合×R0×0.3）：中央 %.5f・p90 %.5f・最大 %.5f" % (med(D["taxDropUpper"]), qt(D["taxDropUpper"], .9), max([x for x in D["taxDropUpper"] if x == x] or [float('nan')])))
    P("  C1 の瞬間の受け手の国の公共財の到達（人口加重の RuleReach）の落ち＝人口の割合×R0：中央 %.5f・p90 %.5f" % (med(D["reachDrop"]), qt(D["reachDrop"], .9)))
    P("  C1 で在地へ移る量（割譲の前の R0）：中央 %.3f・在地の後の値（R0＋R1）中央 %.3f" % (med(D["R0_before"]), med(D["R0R1_before"])))
    P("  戻り：DominanceSpeed 0.025／四半期で目標へ寄せる＝半減 ln2÷−ln(0.975)＝%.1f 四半期（%.1f 年）" % (math.log(2) / -math.log(0.975), math.log(2) / -math.log(0.975) / 4))
    open(opath, "wb").write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
