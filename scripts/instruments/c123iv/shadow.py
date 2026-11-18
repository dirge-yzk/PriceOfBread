# 使い方: python -X utf8 -I shadow.py <rawdir> <seeds 1-10> <out> [flag]
# (1223) c123iv の段 0 の紙の影（静的・読むだけ）。旗 1＝私的な建設の予算を投資家の前の月の稼ぎで縛る形の影を、
# nation_s（constructionPlanned/Desired/Budget の月あたりの額）と class_s（資本家 classId 3 の earnedPerMonth・savingsStock）で出す。
# flag=0 は今の規則の影（予算＝記録した constructionBudget）＝計画が変わらないことの自己検査（台本の門が旗 0 で落ちることの確かめ）。
# 影の単位：国×年（class_s の年 1 行と、その年の nation_s の四半期の平均）。Σ_州 min(希望, E_州) ≦ min(Σ希望, ΣE)＝影は縛りの下限（甘い側）。
import sys, math, statistics as st
raw, sr, out = sys.argv[1], sys.argv[2], sys.argv[3]
flag = int(sys.argv[4]) if len(sys.argv) > 4 else 1
s0, s1 = int(sr.split('-')[0]), int(sr.split('-')[1])
def fl(x):
    try: return float(x)
    except: return float("nan")
rows = []   # (seed, nation, year, E, S, size, planned, desired, budget)
ext = []    # (seed, nation, peakYear, extYear, Speak, excessSum, cumPlanned)
for s in range(s0, s1 + 1):
    try: f = open(f"{raw}/nation_s{s}.tsv", encoding="utf-8")
    except FileNotFoundError: continue
    ny = {}
    with f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            k = (c[ix["nation"]], int(c[ix["year"]]))
            a = ny.setdefault(k, [0, 0.0, 0.0, 0.0])
            a[0] += 1; a[1] += fl(c[ix["constructionPlannedMoneyPerMonth"]]); a[2] += fl(c[ix["constructionDesiredMoneyPerMonth"]]); a[3] += fl(c[ix["constructionBudgetMoneyPerMonth"]])
    cap = {}
    with open(f"{raw}/class_s{s}.tsv", encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            if c[0] in ("open", "warm") or int(c[3]) != 3: continue
            n, y = c[1], int(c[2])
            cap.setdefault(n, []).append((y, fl(c[ix["earnedPerMonth"]]), fl(c[ix["savingsStock"]]), fl(c[ix["sizeK"]])))
    for n, ser in cap.items():
        ser.sort()
        for (y, E, S, z) in ser:
            a = ny.get((n, y))
            if not a or a[0] == 0: continue
            P, D, B = a[1] / a[0], a[2] / a[0], a[3] / a[0]
            if not (P == P) or not (E == E): continue
            rows.append((s, n, y, E, S, z, P, D, B))
        pk = max(ser, key=lambda t: t[3] if t[3] == t[3] else -1)
        ey = None
        for (y, E, S, z) in ser:
            if y > pk[0] and z == z and z < pk[3] / 1000: ey = y; break
        speak = max((t[2] for t in ser if t[2] == t[2]), default=0.0)
        exs = 0.0; cum = 0.0
        for (y, E, S, z) in ser:
            if ey is not None and y >= ey: break
            a = ny.get((n, y))
            if not a or a[0] == 0: continue
            P, D, B = a[1] / a[0], a[2] / a[0], a[3] / a[0]
            Bn = (E if E > 0 else 0.0) if flag == 1 else B
            newP = min(D, Bn) if D == D else 0.0
            if not (newP == newP): newP = 0.0
            exs += 12.0 * max(0.0, P - newP); cum += 12.0 * P
        ext.append((s, n, pk[0], ey, speak, exs, cum))
o = open(out, "w", encoding="utf-8", newline="\n")
o.write(f"(1223) c123iv 段0 の紙の影 flag={flag} raw={raw} seeds={sr} 国×年 {len(rows)} 行\n")
o.write("== A 縛りの影：kept＝min(希望, 予算の新)÷今の計画（国×年・資本家の在る年だけ＝size>0）・十年ごと ==\n")
o.write("decade\tn\tbindShare(新の予算<今の計画)\tkeptMedian\tkeptP25\tE/計画の中央\t今の予算/計画の中央\n")
dec = {}
for (s, n, y, E, S, z, P, D, B) in rows:
    if not (z > 0) or not (P > 0): continue
    Bn = (E if E > 0 else 0.0) if flag == 1 else B
    newP = min(D, Bn)
    dec.setdefault(y // 10 * 10, []).append((newP / P, Bn < P * (1 - 1e-9), (E / P) if E == E else float("nan"), B / P))
for d in sorted(dec):
    v = dec[d]; k = sorted(t[0] for t in v)
    o.write(f"{d}\t{len(v)}\t{sum(t[1] for t in v)/len(v):.3f}\t{st.median(k):.3f}\t{k[len(k)//4]:.3f}\t{st.median(t[2] for t in v):.3f}\t{st.median(t[3] for t in v):.1f}\n")
o.write("== B 消滅した国×種：峰の前後から消滅の年までに、新の予算が今の計画を削る額の和（12×月の額）÷資本家の貯蓄の最大 ==\n")
ex = [e for e in ext if e[3] is not None]
o.write(f"消滅 {len(ex)}／{len(ext)}（国×種・class_s の資本家の峰の 1/1000 未満）\n")
r = sorted(e[5] / e[4] for e in ex if e[4] > 0)
if r:
    o.write(f"削る額÷貯蓄の最大：中央 {st.median(r):.3f}・P25 {r[len(r)//4]:.3f}・P75 {r[3*len(r)//4]:.3f}・1 以上 {sum(1 for x in r if x >= 1)}／{len(r)}\n")
    c2 = sorted(e[5] / e[6] for e in ex if e[6] > 0)
    o.write(f"削る額÷今の建設の和：中央 {st.median(c2):.3f}・P25 {c2[len(c2)//4]:.3f}・P75 {c2[3*len(c2)//4]:.3f}\n")
ye = sorted(e[3] for e in ex)
if ye: o.write(f"消滅の年：中央 {st.median(ye)}・P25 {ye[len(ye)//4]}・P75 {ye[3*len(ye)//4]}\n")
for name in ("ネパール", "ハノーファー", "イギリス", "フランス", "プロイセン", "清"):
    for e in ext:
        if e[1] == name and e[0] in (7, 8, 1):
            o.write(f"  {name} 種{e[0]} 峰{e[2]} 消滅{e[3]} 貯蓄の最大{e[4]:.0f} 削る額{e[5]:.0f} 建設の和{e[6]:.0f}\n")
o.close()
print(open(out, encoding="utf-8").read()[:3000])
