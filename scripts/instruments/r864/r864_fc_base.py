# (864) 旗 0 の基線（keep_852/base_ins）で cap÷人数（cap÷subPopK を同じ州の q0 と比べる）が P5b の行で同じように漂うかを数える（読むだけ）。
# 加えて、旗 0 の盤面で計器が数える反実仮想の σ（s2SigmaInst）÷d0 の中央を窓ごとに出す＝形が旗 0 の盤面で対照に出す直の効きの紙。
# 呼び方：python -X utf8 -I r864_fc_base.py <出力>
import sys, os, collections
B = "D:/PoB_work/keep_852/base_ins"
NAN = float("nan")
def fnum(s):
    try: return float(s)
    except ValueError: return NAN
def win(y):
    return "1783-85" if y <= 1785 else "1786-99" if y <= 1799 else "1800-49" if y <= 1849 else "1850-99" if y <= 1899 else "1900-50"
def med(xs):
    xs = sorted(x for x in xs if x == x); n = len(xs)
    return NAN if n == 0 else xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2])
L = collections.defaultdict(list)
for seed in range(1, 11):
    D = {}
    with open(os.path.join(B, "subst_s%d.tsv" % seed), encoding="utf-8") as f:
        h = f.readline().rstrip("\r\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            sp = fnum(c[ix["subPopK"]])
            if not (sp > 0): continue
            D[(int(c[ix["quarter"]]), c[ix["nation"]] + "/" + c[ix["region"]])] = (int(c[ix["year"]]), sp, fnum(c[ix["farmCapPerMonth"]]), fnum(c[ix["farmSoldPerMonth"]]),
                fnum(c[ix["unsoldD1Share"]]), fnum(c[ix["s2SigmaInst"]]), fnum(c[ix["s2D0Inst"]]), int(c[ix["s2RhoSource"]]), fnum(c[ix["s2TechHead"]]), fnum(c[ix["s2Tech0Inst"]]))
    o0 = {k[1]: r for k, r in D.items() if k[0] == 0}
    for k, r in D.items():
        if k[0] == 0: continue
        o = o0.get(k[1])
        if o is None or r[4] != r[4] or o[4] != o[4] or not (r[2] > 0 and o[2] > 0): continue
        if abs(r[3] / r[2] - o[3] / o[2]) > 0.01: continue
        wn = win(r[0])
        L[(wn, "fC")].append((r[2] / r[1]) / (o[2] / o[1]))
        if r[7] == 1 and r[6] > 0: L[(wn, "sig/d0")].append(r[5] / r[6])
        if r[9] > 0: L[(wn, "T/T0")].append(r[8] / r[9])
    del D
out = ["(864) 旗 0 の基線 keep_852/base_ins の P5b（基線の q0・|soc−soc0|≦0.01・N1・N2 の鍵は問わない）：cap÷人数の比と反実仮想の σ÷d0（計器 s2SigmaInst）"]
for wn in ("1783-85", "1786-99", "1800-49", "1850-99", "1900-50"):
    out.append("  %s：行 %d・fC 中央 %.4f・反実仮想の σ/d0 中央 %.4f（行 %d）・T/T0 中央 %.4f" % (wn, len(L[(wn, "fC")]), med(L[(wn, "fC")]), med(L[(wn, "sig/d0")]), len(L[(wn, "sig/d0")]), med(L[(wn, "T/T0")])))
txt = "\n".join(out) + "\n"
with open(sys.argv[1], "w", encoding="utf-8", newline="\n") as f:
    f.write(txt)
print(txt)
