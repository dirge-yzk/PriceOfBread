# (878) c73ds 読むだけの紙の台本 2/2：cess_s の無い盤面（keep_859_rej/c70ssB_10＝m873 の木で RuleOrderByRegion だけ 0 の盤面と 8 表がバイトで同じ＝(873) [4] aSS）で、
#   州の持ち主が替わった行から割譲（と再併合）を拾い、受け手の目標の国家区分と割譲の後の R0 の動きを、同じ拾い方で m873 と並べる＝1 旗だけの対照（近似）。
# 使い方：python -X utf8 -I paper878_detect.py <盤面の名> <rule_s のフォルダ> <エンジンの規則 new|old> <出力.txt>
# ⚠限界：①拾うのは〈前の持ち主の下の行の次の行が、その種で前から居た別の国の下〉＝割譲と再併合（Returned）が混ざる・建国（新しい国の名）は外す。
#   ②計器の 9 国以外は年末の行＝受け手が計器の国の物だけを〈割譲の直後〉と数える列を分ける。③目標は四半期の末の計器の列（近似・paper878_cession.py の限界③④と同じ）。
import sys, os, collections

KEEP = 0.975

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

def tstate(c, ix, which):
    tU = fnum(c[ix["ungovTargetNewShare" if which == "new" else "ungovTargetOldShare"]])
    init = fnum(c[ix["ruleInitLocalShare"]]); inter = fnum(c[ix["intermediaryDegreeIndex"]])
    tL = min(1.0 - tU, init * inter)
    return max(0.0, 1.0 - tL - fnum(c[ix["ruleForeignShare"]]) - tU), tU

def main(name, base, eng, opath):
    out = []; P = out.append; D = collections.defaultdict(list); C = collections.Counter()
    for seed in range(1, 11):
        with open(os.path.join(base, "rule_s%d.tsv" % seed), encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            rows = [l.rstrip("\n").split("\t") for l in f]
        first_seen = {}; natq = collections.defaultdict(set); reg = collections.defaultdict(list)
        for c in rows:
            q = int(c[ix["quarter"]]); n = c[ix["nation"]]
            if n not in first_seen or q < first_seen[n]: first_seen[n] = q
            natq[n].add(q); reg[c[ix["region"]]].append((q, n, c))
        quarterly = {n for n, s in natq.items() if len(s) > 600}
        for rg, lst in reg.items():
            lst.sort(key=lambda t: t[0])
            for i in range(1, len(lst)):
                qb, nb, b = lst[i - 1]; qa, na, a = lst[i]
                if na == nb or first_seen.get(na, 1 << 30) >= qb: continue
                C["拾った持ち主の替わり（割譲か再併合）"] += 1
                if na in quarterly: C["うち受け手が計器の国"] += 1
                R0a = fnum(a[ix["ruleStateShare"]])
                tSn, tUn = tstate(a, ix, "new"); tSo, tUo = tstate(a, ix, "old")
                tSe = tSn if eng == "new" else tSo
                D["R0a"].append(R0a); D["tS_eng"].append(tSe); D["tU_eng"].append(tUn if eng == "new" else tUo)
                D["tS_new"].append(tSn); D["tS_old"].append(tSo)
                if tSe < R0a: C["エンジンの規則の目標の国家区分＜割譲の直後の R0"] += 1
                for nn in (12, 40):
                    hit = [(q, c2) for q, n2, c2 in lst[i:] if nn <= q - qa <= nn + 3 and n2 == na]
                    stay = all(n2 == na for q, n2, _ in lst[i:] if q - qa <= nn)
                    if hit and stay:
                        q2, c2 = hit[0]; R0 = fnum(c2[ix["ruleStateShare"]])
                        D["dR0_%d" % nn].append(R0 - R0a); D["R0C1_%d" % nn].append(R0 - R0a * KEEP ** (q2 - qa))
                        D["R3_%d" % nn].append(fnum(c2[ix["ruleUngovShare"]]))
    P("(878) 持ち主の替わりから拾った割譲（と再併合）・盤面 %s（%s・エンジンの規則 %s・台本 paper878_detect.py）" % (name, base, eng))
    for k, v in sorted(C.items()): P("  %s：%d" % (k, v))
    P("  割譲の直後の R0 中央 %.3f・エンジンの規則の目標の国家区分 中央 %.3f（p10 %.3f・p90 %.3f）・無統治の目標 中央 %.3f（p90 %.3f）" % (
        med(D["R0a"]), med(D["tS_eng"]), qt(D["tS_eng"], .1), qt(D["tS_eng"], .9), med(D["tU_eng"]), qt(D["tU_eng"], .9)))
    P("  同じ行の目標の国家区分：新の規則 中央 %.3f・旧の規則 中央 %.3f" % (med(D["tS_new"]), med(D["tS_old"])))
    for nn in (12, 40):
        P("  n≈%d 四半期（受け手の下に居続けた %d 件）：今の R0−直後 中央 %.4f（p10 %.4f・p90 %.4f）・C1 の R0 中央 %.3f・無統治 R3 中央 %.3f（p90 %.3f）" % (
            nn, len(D["dR0_%d" % nn]), med(D["dR0_%d" % nn]), qt(D["dR0_%d" % nn], .1), qt(D["dR0_%d" % nn], .9), med(D["R0C1_%d" % nn]), med(D["R3_%d" % nn]), qt(D["R3_%d" % nn], .9)))
    with open(opath, "wb") as fo:
        fo.write(("\n".join(out) + "\n").encode("utf-8"))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
