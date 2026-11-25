# (1377) c146wy 紙の台本と腕 B の門の読み手（同じ台本で基線と腕 B を数える）。
#   python -X utf8 -I paper_wy.py <ins の道> [種の起点=1] [種の数=10]
#   読む表：scalars_s（atWar・warExhaustion・milWarMean・dmWar・dynMandate・legitimacy）・center_s（ignitionViolentFlag・coupFlag・inCivilWarFlag）・cwend_s（行の数）。
#   エンジンの関数は呼ばない。保持力の戦時の乗数は旧の式（係数の今の値＝WarRallyCoercion 0.3・WarRallyDecayPoint 0.4・WarCoercionDrain 0.15）で表から数え直す。
#   門 G1＝scalars の milWarMean が全ての行で 0（エンジンが書いた p.MilWar の人口の加重平均を計器が数える＝本物の経路）。旗 0 の盤面では 0 でない行が出る＝門が落ちる。
import sys, os, csv
csv.field_size_limit(10**9)
INS = sys.argv[1]; SB = int(sys.argv[2]) if len(sys.argv) > 2 else 1; N = int(sys.argv[3]) if len(sys.argv) > 3 else 10
RALLY, DECAY, DRAIN = 0.3, 0.4, 0.15

def rows(name, s):
    p = os.path.join(INS, f"{name}_s{s}.tsv")
    if not os.path.exists(p): return None, []
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); return h, list(r)

def num(v):
    try: return float(v)
    except Exception: return float('nan')

def q(xs, f):
    if not xs: return float('nan')
    xs = sorted(xs); return xs[min(len(xs) - 1, int(f * len(xs)))]

tot = dict(rows=0, war=0, milNZ=0, milNZwar=0, dmNZ=0, multGt1=0, multLt1=0, multEq1=0, multGt1war=0, multLt1war=0, peaceExh=0)
milWarWar = []; dmWarWar = []; exhWar = []; multWar = []; multPeace = []; milMeanWar = []; milMeanPeace = []; dmMean = []
coeW = []; coeP = []
ign = coup = cw = ignWar = ignPeace = ignUnk = 0; seeds = 0; per = []
for s in range(SB, SB + N):
    h, R = rows('scalars', s)
    if h is None: print(f"種 {s}: scalars_s が無い"); continue
    seeds += 1
    ix = {k: i for i, k in enumerate(h)}
    war_at = {}
    sm = dict(rows=0, war=0, milNZ=0)
    for r in R:
        aw = r[ix['atWar']] == '1'; we = num(r[ix['warExhaustion']]); mw = num(r[ix['milWarMean']]); dm = num(r[ix['dmWar']])
        war_at[(r[ix['quarter']], r[ix['nation']])] = aw
        tot['rows'] += 1; sm['rows'] += 1
        mult = 1.0 + (RALLY * max(0.0, 1.0 - we / DECAY) if aw else 0.0) - DRAIN * we
        if mw != 0: tot['milNZ'] += 1; sm['milNZ'] += 1
        if dm != 0: tot['dmNZ'] += 1
        if mult > 1: tot['multGt1'] += 1
        elif mult < 1: tot['multLt1'] += 1
        else: tot['multEq1'] += 1
        dmMean.append(num(r[ix['dynMandate']]))
        if aw:
            tot['war'] += 1; sm['war'] += 1
            milWarWar.append(mw); dmWarWar.append(dm); exhWar.append(we); multWar.append(mult); milMeanWar.append(num(r[ix['militancyMean']]))
            if mw != 0: tot['milNZwar'] += 1
            if mult > 1: tot['multGt1war'] += 1
            elif mult < 1: tot['multLt1war'] += 1
        else:
            multPeace.append(mult); milMeanPeace.append(num(r[ix['militancyMean']]))
            if we > 0: tot['peaceExh'] += 1
    per.append((s, sm['rows'], sm['war'], sm['milNZ']))
    hc, C = rows('center', s)
    if hc is not None:
        jc = {k: i for i, k in enumerate(hc)}
        for r in C:
            iv = r[jc['ignitionViolentFlag']] == '1'
            if r[jc['coupFlag']] == '1': coup += 1
            if iv:
                ign += 1
                k = (r[jc['quarter']], r[jc['nation']])
                if k not in war_at: ignUnk += 1
                elif war_at[k]: ignWar += 1
                else: ignPeace += 1
    hp, P = rows('crownpc', s)
    if hp is not None:
        jp = {k: i for i, k in enumerate(hp)}
        for r in P:
            v = num(r[jp['effectiveCoerceIndex']]); k = (r[jp['quarter']], r[jp['nation']])
            if v == v and k in war_at: (coeW if war_at[k] else coeP).append(v)
    hw, W = rows('cwend', s)
    if hw is not None: cw += len(W)

def m(xs): xs = [x for x in xs if x == x]; return sum(xs) / len(xs) if xs else float('nan')
print(f"# paper_wy {INS} 種 {SB}..{SB+N-1}（読めた種 {seeds}）")
print(f"scalars の行 {tot['rows']}・交戦中 {tot['war']}・平時で厭戦＞0 {tot['peaceExh']}")
print(f"G1 milWarMean≠0 の行 {tot['milNZ']}（交戦中 {tot['milNZwar']}）＝旗 1 の門は 0")
print(f"交戦中の milWarMean 平均 {m(milWarWar):.4f}・中央 {q(milWarWar,0.5):.4f}・90% {q(milWarWar,0.9):.4f}・最大 {max(milWarWar) if milWarWar else float('nan'):.4f}")
print(f"交戦中の militancyMean 平均 {m(milMeanWar):.4f}・平時 {m(milMeanPeace):.4f}")
print(f"dmWar≠0 の行 {tot['dmNZ']}・交戦中の dmWar 平均 {m(dmWarWar):.4f}（記録＝計器が旗を通して自分で数える列）")
print(f"交戦中の厭戦 中央 {q(exhWar,0.5):.4f}・75% {q(exhWar,0.75):.4f}・90% {q(exhWar,0.9):.4f}・最大 {max(exhWar) if exhWar else float('nan'):.4f}")
print(f"保持力の戦時の乗数（旧の式）：＞1 {tot['multGt1']}（交戦中 {tot['multGt1war']}）・＜1 {tot['multLt1']}（交戦中 {tot['multLt1war']}）・＝1 {tot['multEq1']}")
print(f"  交戦中の乗数 平均 {m(multWar):.4f}・中央 {q(multWar,0.5):.4f}・10% {q(multWar,0.1):.4f}・90% {q(multWar,0.9):.4f}／平時 平均 {m(multPeace):.4f}・最小 {min(multPeace) if multPeace else float('nan'):.4f}")
print(f"crownpc effectiveCoerceIndex 平均 交戦中 {m(coeW):.4f}（{len(coeW)}）・平時 {m(coeP):.4f}（{len(coeP)}）")
print(f"天命 dynMandate の平均 {m(dmMean):.4f}")
print(f"center_s 暴力の点火 {ign}（交戦中 {ignWar}・平時 {ignPeace}・scalars に行の無い四半期 {ignUnk}）・coupFlag {coup}・cwend_s の行 {cw}")
for s, a, b, c in per: print(f"  種 {s}: 行 {a}・交戦中 {b}・milWar≠0 {c}")
