# (1059) c100ci 設計の材料：D1〈治安維持力を州に届く人の数対州の大衆で測る〉の紙の数え（読むだけ・腕 0）
# 引数：生のフォルダ 種の始め 種の終わり 出力のファイル 組の名
# 読む表：secession_s（州の行）・nation_s（国の兵の数 soldierK）・cpop_s（植民地格の州の兵士の POP＝className 兵士）
# 変種（AI の案・確証なし）
#   old：今の式 治安維持力の項＝SecessionHoldCoercion×SecessionThreshold×c÷(c＋PoliticalReferencePop)（c＝coerceIndex）
#   A0：単位だけ直す（兵は大衆に比例と置く）＝x÷(x＋need)・x＝c×N_r÷N_nat・need＝N_r×CrimeOrderPerPop
#   A1：D1＝兵の居場所で割り振る＝x＝c×S_r÷S_nat（S_r が表に無い州は、国の残りの兵を残りの大衆に比例で置く）
#   A2：A1 の治安維持力の項に投射を掛けない（威信の項だけ投射）
# 限界（この台本が自分で申告する）
#   ①首都の州は secession_s に行が無い＝首都の大衆は 総人口−行の人口 に行の大衆の割合を掛けて置く（近似）
#   ②州の兵の数は cpop_s の植民地格の州だけ（挙げた国だけ）＝本国の州は国の残りの兵を大衆の比で置く（近似）・表に無い国は A1＝A0
#   ③soldierK は四半期の末・cpop_s は月の頭・secession_s は CheckSecession の瞬間＝時点が違う
#   ④支配度の積分の予言は、今の行の圧力と共有分母のままで新の引き留めを積んだ物＝分離が起きた後の帰結は入らない（最初に 0 に届く州だけを数える）
#   ⑤挙げていない国は年末の行だけ＝4 四半期を同じ margin で積む
import sys, os, statistics as st
from collections import defaultdict

RAW, S0, S1, OUT, TAG = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
THR, KHC, REF, COP, HPP, SPD = 1.43, 1.0, 1000.0, 0.8, 0.000005, 0.025
SA = "ヌエバ・エスパーニャ銀地帯 中央メキシコ キューバ ペルー ヌエバ・グラナダ ラ・プラタ プエルトリコ カラカス ルイジアナ・フロリダ チリ サンタフェ エルパソ ロレート レオン ボゴタ キト イキトス ラパス トゥクマン アスンシオン カルメン・デ・パタゴネス プンタアレナス チロエ".split()
BR = "ブラジル アマゾン マナウス サルヴァドール サンルイス ゴイアス クイアバ".split()
SD = "北部サン=ドマング 南部サン=ドマング".split()
GROUP = {}
for n in SA: GROUP[n] = ("SA", "スペイン")
for n in BR: GROUP[n] = ("BR", "ポルトガル")
for n in SD: GROUP[n] = ("SD", "フランス")
SOL = "\t兵士\t".encode("utf-8")
VARS = ("old", "A0", "A1", "A2")

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float("nan")

def sat(x, need):
    return x / (x + need) if (x + need) > 0 else 0.0

per_seed = []   # 種ごとの集計
hitList = []    # A1 で 0 に届いた (種, 国, 州, 年, tier)
for seed in range(S0, S1 + 1):
    sec = os.path.join(RAW, f"secession_s{seed}.tsv")
    if not os.path.exists(sec): continue
    # 国の兵の数
    snat = {}
    with open(os.path.join(RAW, f"nation_s{seed}.tsv"), "rb") as f:
        h = f.readline().decode("utf-8").rstrip("\r\n").split("\t")
        iq, ino, isk = h.index("quarter"), h.index("nation"), h.index("soldierK")
        for line in f:
            t = line.decode("utf-8").rstrip("\r\n").split("\t")
            snat[(int(t[iq]), t[ino])] = fnum(t[isk])
    # 州の兵の数（植民地格の州だけ）
    sreg = {}
    cp = os.path.join(RAW, f"cpop_s{seed}.tsv")
    if os.path.exists(cp):
        with open(cp, "rb") as f:
            f.readline()
            for line in f:
                if SOL not in line: continue
                t = line.decode("utf-8").rstrip("\r\n").split("\t")
                sreg[(int(t[0]), t[2], t[3])] = fnum(t[8])
    # 州の行を国×四半期で束ねる
    rows = defaultdict(list)
    with open(sec, "rb") as f:
        f.readline()
        for line in f:
            t = line.decode("utf-8").rstrip("\r\n").split("\t")
            rows[(int(t[0]), t[2])].append(t)
    g = defaultdict(lambda: defaultdict(list))   # 群→量→値の列（年末の行）
    neg = defaultdict(lambda: [0, 0, 0, 0, 0])   # 群→[行, old 負, A0 負, A1 負, A2 負]（年末の行）
    gapmax = 0.0
    series = defaultdict(list)   # (国, 州)→[(四半期, 支配度, 各変種の margin, tier, proj, goFlag)]
    for (q, nat), rs in rows.items():
        sp = sum(fnum(t[13]) for t in rs); sc = sum(fnum(t[14]) for t in rs)
        if not (sp > 0): continue
        P = fnum(rs[0][6]); capPop = P - sp
        Nnat = sc + (capPop * sc / sp if capPop > 0 else 0.0)
        Snat = snat.get((q, nat), float("nan"))
        known = {t[3]: sreg[(q, nat, t[3])] for t in rs if (q, nat, t[3]) in sreg}
        sK = sum(known.values()); nK = sum(fnum(t[14]) for t in rs if t[3] in known)
        restS = Snat - sK; restN = Nnat - nK
        for t in rs:
            y = int(t[1]); reg = t[3]; tier = t[4]
            L, c, hb, sd = fnum(t[5]), fnum(t[7]), fnum(t[8]), fnum(t[11])
            N = fnum(t[14]); p = fnum(t[27]); eff = fnum(t[30]); m_eng = fnum(t[32])
            proj = eff / hb if hb > 0 else 1.0
            legit = L / (1 + P * HPP)
            tOld = KHC * THR * c / (c + REF)
            need = N * COP
            tA0 = KHC * THR * sat(c * N / Nnat, need) if Nnat > 0 else tOld
            if Snat == Snat and Snat > 0:
                if reg in known: Sr = known[reg]
                else: Sr = N * restS / restN if (restS > 0 and restN > 0) else 0.0
                tA1 = KHC * THR * sat(c * Sr / Snat, need)
            else:
                tA1 = tA0
            mOld = (legit + tOld) * proj / sd - p
            gapmax = max(gapmax, abs(mOld - m_eng))
            ms = {"old": m_eng, "A0": (legit + tA0) * proj / sd - p, "A1": (legit + tA1) * proj / sd - p,
                  "A2": (legit * proj + tA1) / sd - p}
            series[(nat, reg)].append((q, fnum(t[33]), ms, tier, proj, t[34]))
            if q % 4 != 3: continue
            grp = []
            if reg in GROUP and GROUP[reg][1] == nat and 1800 <= y <= 1830: grp.append(GROUP[reg][0])
            if proj < 0.6: grp.append("far")
            if tier == "Core" and proj >= 0.9: grp.append("homeCore")
            grp.append("all")
            for gg in grp:
                d = g[gg]
                d["termOld"].append(tOld); d["termA0"].append(tA0); d["termA1"].append(tA1)
                for v in VARS: d["m_" + v].append(ms[v])
                d["proj"].append(proj); d["p"].append(p)
                nn = neg[gg]; nn[0] += 1
                for i, v in enumerate(VARS): nn[i + 1] += 1 if ms[v] < 0 else 0
    # 支配度の積分の予言（最初に 0 に届く州）
    hits = {v: defaultdict(int) for v in VARS}
    goReal = defaultdict(int)
    hitNames = {v: defaultdict(int) for v in ("A1",)}
    for (nat, reg), ser in series.items():
        ser.sort(key=lambda x: x[0])
        D = {v: None for v in VARS}; prevq = None; done = {v: False for v in VARS}
        for (q, dom, ms, tier, proj, go) in ser:
            key = "far" if proj < 0.6 else ("homeCore" if tier == "Core" and proj >= 0.9 else "mid")
            if go == "1": goReal[(tier, key)] += 1
            if prevq is None or q - prevq > 4:
                for v in VARS:
                    D[v] = dom
                prevq = q; continue
            dq = q - prevq; prevq = q
            for v in VARS:
                if done[v]: continue
                d = D[v] + ms[v] * SPD * dq
                d = 0.0 if not (d > 0) else (1.0 if d > 1 else d)
                D[v] = d
                if d == 0.0:
                    done[v] = True; hits[v][(tier, key)] += 1
                    if v == "A1":
                        hitNames["A1"][nat] += 1
                        hitList.append((seed, nat, reg, q // 4 + 1783, tier))
    per_seed.append((seed, g, neg, gapmax, hits, goReal, hitNames))

with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1059) D1 の紙の数え 組={TAG} 生={RAW} 種={S0}〜{S1}（読めた種 {len(per_seed)}）\n")
    o.write("# 年末の行・SA/BR/SD は 1800〜1830 の保持中・far＝投射<0.6・homeCore＝Core かつ投射≥0.9・all＝全ての州の行。値は種ごとの中央の、種の中央\n")
    o.write(f"# 検算：今の式で組み直した margin と本体の marginIndex の差の最大（種の最大）＝{max(x[3] for x in per_seed):.3g}\n")
    for gg in ("SA", "BR", "SD", "far", "homeCore", "all"):
        vals = {}
        for k in ("termOld", "termA0", "termA1", "m_old", "m_A0", "m_A1", "m_A2", "proj", "p"):
            vals[k] = med([med(x[1][gg][k]) for x in per_seed if x[1][gg][k]])
        rowsN = med([x[2][gg][0] for x in per_seed])
        shares = []
        for i in range(4):
            ss = [x[2][gg][i + 1] / x[2][gg][0] for x in per_seed if x[2][gg][0] > 0]
            shares.append((med(ss), max(ss) if ss else float("nan")))
        o.write(f"{gg}\t行/種={rowsN:.0f}\t項 old={vals['termOld']:.3f} A0={vals['termA0']:.3f} A1={vals['termA1']:.3f}\t"
                f"margin old={vals['m_old']:.3f} A0={vals['m_A0']:.3f} A1={vals['m_A1']:.3f} A2={vals['m_A2']:.3f}\t投射={vals['proj']:.3f} 圧力={vals['p']:.3f}\t"
                + "負の行の割合(中央/最大) " + " ".join(f"{v}={a:.4f}/{b:.4f}" for v, (a, b) in zip(VARS, shares)) + "\n")
    o.write("# 支配度の積分の予言（最初に 0 に届く州の数・種ごと）＝鍵 (tier, far/mid/homeCore)\n")
    keys = sorted({k for x in per_seed for v in VARS for k in x[4][v]} | {k for x in per_seed for k in x[5]})
    for k in keys:
        o.write(f"{k[0]}/{k[1]}\t実際の goFlag 行=" + ",".join(str(x[5].get(k, 0)) for x in per_seed)
                + "\t" + "\t".join(f"{v}=" + ",".join(str(x[4][v].get(k, 0)) for x in per_seed) for v in VARS) + "\n")
    tot = defaultdict(int)
    for x in per_seed:
        for n, c in x[6]["A1"].items(): tot[n] += c
    o.write("# A1 で 0 に届く州の多い国（種の和・上位 15）\n")
    for n, c in sorted(tot.items(), key=lambda z: -z[1])[:15]:
        o.write(f"{n}\t{c}\n")
    o.write("# A1 で 0 に届く州の一覧（種, 国, 州, 年, tier）\n")
    for h in sorted(hitList):
        o.write("\t".join(str(x) for x in h) + "\n")
