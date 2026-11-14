# (1141) c111s25 読む：台帳 S25 外交の行ごとの頻度（両側・届いた割合）を、生の計器の表から数える（読むだけ）。
# 使い方：python -X utf8 -I freq.py <生のフォルダ> <ラベル> <種の始め> <種の終わり> <出力>
# 閾値はエンジンの係数の値をそのまま写す（Coefficients.p0x の行は out の頭に名乗る）。計器が書かない行は 0 と数えない（表が無い種は「表なし」）。
import sys, os, math, statistics
from collections import defaultdict, Counter

D, LAB, S0, S1, OUT = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
# エンジンの閾値（03d192514 の値）
WAR_DECLARE = 140.0        # p04_trade.cs:122 WarDeclareTension
HIGH_BAND = 100.0          # p04_trade.cs:91 HighTensionBand
SHOCK_MIN = 0.3            # p06_rank_discovery.cs:61 DefeatShockProtectMin
WEARY = 0.5                # p04_trade.cs:132 WarWearyThreshold
MAXDEP = 8                 # p05_port_insurance.cs:62 MaxDependents

def rd(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        rows = [l.rstrip('\n').split('\t') for l in f if l.strip()]
    ix = {k: i for i, k in enumerate(h)}
    return ix, rows

def fl(x):
    try: return float(x)
    except: return float('nan')

per = defaultdict(dict)   # 指標 → 種 → 値
def put(k, s, v): per[k][s] = v

for s in range(S0, S1 + 1):
    # ---- warband：入口ごとの開戦・屈服・講和の置き直し・緊張の帯
    t = rd('warband', s)
    if t:
        ix, R = t
        for col in ['start_main', 'start_subjugate', 'start_declare', 'start_ally', 'start_suzerain', 'start_civil', 'start_mediation', 'start_secession', 'start_ultimatum', 'start_calltoarms', 'start_unknown', 'submits', 'peaceResets', 'secessionResets']:
            put('wb.' + col, s, sum(int(r[ix[col]]) for r in R))
        tot = sum(int(r[ix['pairs']]) for r in R)
        hi = sum(int(r[ix['b100_140']]) + int(r[ix['b140']]) for r in R)
        put('wb.pairq_ge100_share', s, hi / tot if tot else float('nan'))
        put('wb.pairq_ge140_share', s, sum(int(r[ix['b140']]) for r in R) / tot if tot else float('nan'))
    # ---- scalars：敗戦の衝撃・厭戦（国×四半期）
    shock = {}
    t = rd('scalars', s)
    if t:
        ix, R = t
        n = len(R)
        put('sc.weary_gt05_share', s, sum(1 for r in R if fl(r[ix['warExhaustion']]) > WEARY) / n)
        put('sc.shock_ge03_share', s, sum(1 for r in R if fl(r[ix['defeatShock']]) >= SHOCK_MIN) / n)
        for r in R: shock[(int(r[ix['quarter']]), r[ix['nation']])] = fl(r[ix['defeatShock']])
    # ---- dep：従属の張り（呼び手ごと）・交戦中に張った・輪
    links = {}   # dependent → suzerain（計器の行から組み立てた近似＝離反と国の消滅は行が無い＝上限）
    depev = []
    t = rd('dep', s)
    if t:
        ix, R = t
        cnt = Counter(); atw = Counter(); cyc = 0; cycAfterMax = 0; finalLinks = None
        for r in R:
            if r[0] == '2': finalLinks = int(r[ix['linksAfterCount']]); continue
            who = r[ix['callerMember']] + ':' + r[ix['callerLine']]
            cnt[who] += 1
            if r[ix['atWarFlag']] == '1': atw[who] += 1
            if r[ix['cycleFlag']] == '1': cyc += 1
            cycAfterMax = max(cycAfterMax, int(r[ix['cyclesAfterCount']]))
            q = int(r[ix['month']]) // 3
            depev.append((q, r[ix['suzerain']], r[ix['dependent']], r[ix['cutSuzerain']]))
        for k, v in cnt.items(): put('dep.n.' + k, s, v)
        for k, v in atw.items(): put('dep.atWar.' + k, s, v)
        put('dep.n.all', s, sum(cnt.values())); put('dep.atWar.all', s, sum(atw.values()))
        put('dep.cycleFlag', s, cyc); put('dep.cyclesAfterMax', s, cycAfterMax)
        if finalLinks is not None: put('dep.finalLinks', s, finalLinks)
        for q, su, de, cut in depev:
            if cut: links.pop(su, None)
            links[de] = su
        put('dep.reconLinksEnd', s, len(links))
        mx = Counter(links.values()).most_common(1)
        put('dep.reconMaxDependents', s, mx[0][1] if mx else 0)
    def suz_at(q):
        L = {}
        for qq, su, de, cut in depev:
            if qq > q: break
            if cut: L.pop(su, None)
            L[de] = su
        return L
    # ---- wars：入口の札・従属の組の交戦（V02 の入口 1・2 の近似）
    t = rd('wars', s)
    if t:
        ix, R = t
        c = Counter(r[ix['entryCode']] for r in R)
        for k, v in c.items(): put('wars.' + k, s, v)
        put('wars.all', s, len(R))
        if depev:
            dpw = Counter()
            for r in R:
                q = int(r[ix['q']]); a, b = r[ix['sideA']], r[ix['sideB']]
                L = suz_at(q)
                if L.get(a) == b or L.get(b) == a: dpw[r[ix['entryCode']]] += 1
            for k, v in dpw.items(): put('wars.depPair.' + k, s, v)
            put('wars.depPair.all', s, sum(dpw.values()))
    # ---- warpair：時計が開かない側・門の閉じた月・遺恨・高い帯の冷め方の枝
    t = rd('warpair', s)
    if t:
        ix, R = t
        ge3_noopen = sum(1 for r in R if int(r[ix['monthsGe140']]) >= 3 and int(r[ix['starts']]) == 0 and int(r[ix['submits']]) == 0)
        ge3 = sum(1 for r in R if int(r[ix['monthsGe140']]) >= 3)
        put('wp.ge140_3m_rows', s, ge3); put('wp.ge140_3m_noOpen', s, ge3_noopen)
        g140 = sum(int(r[ix['monthsGe140']]) for r in R); gate = sum(int(r[ix['gateOpenMonths']]) for r in R)
        put('wp.ge140_months', s, g140); put('wp.ge140_gateClosed_share', s, (g140 - gate) / g140 if g140 else float('nan'))
        put('wp.submits', s, sum(int(r[ix['submits']]) for r in R))
        put('wp.peaceResets', s, sum(int(r[ix['peaceResets']]) for r in R))
        put('wp.secessionResets', s, sum(int(r[ix['secessionResets']]) for r in R))
        hi = [r for r in R if fl(r[ix['tensionEnd']]) >= HIGH_BAND]
        put('wp.endGe100_rows', s, len(hi))
        put('wp.endGe100_grudgePos', s, sum(1 for r in hi if fl(r[ix['grudge']]) > 0))
        put('wp.endGe100_grudgeGe05', s, sum(1 for r in hi if fl(r[ix['grudge']]) >= 0.5))
        if shock:
            hy = 0; known = 0
            for r in hi:
                q = int(r[ix['quarter']]); a, b = r[ix['nationA']], r[ix['nationB']]
                sa, sb = shock.get((q, a)), shock.get((q, b))
                if sa is None and sb is None: continue
                known += 1
                if max(x for x in (sa, sb) if x is not None) >= SHOCK_MIN: hy += 1
            put('wp.endGe100_shockKnown', s, known); put('wp.endGe100_hyena', s, hy)
        put('wp.grudgePos_rows', s, sum(1 for r in R if fl(r[ix['grudge']]) > 0))
        # 遺恨の薄れ：同じ組が続く四半期に書かれ、どちらも遺恨が正で、その間に新しい講和の置き直しが無い行の比
        prev = {}; ratios = []
        for r in R:
            q = int(r[ix['quarter']]); k = (r[ix['nationA']], r[ix['nationB']]); g = fl(r[ix['grudge']])
            pv = prev.get(k)
            if pv and pv[0] == q - 1 and pv[1] > 0 and g > 0 and int(r[ix['peaceResets']]) == 0 and g <= pv[1]:
                ratios.append(g / pv[1])
            prev[k] = (q, g)
        if ratios:
            med = statistics.median(ratios)
            put('wp.grudge_keepPerQ_med', s, med)
            put('wp.grudge_halfLifeYears_med', s, math.log(0.5) / math.log(med) / 4 if 0 < med < 1 else float('inf'))
            put('wp.grudge_pairsCounted', s, len(ratios))
    # ---- invade：屈服か開戦か
    t = rd('invade', s)
    if t:
        ix, R = t
        put('inv.rows', s, len(R)); put('inv.submitted', s, sum(1 for r in R if r[ix['submitted']] == '1'))
        put('inv.toNation', s, sum(1 for r in R if r[ix['targetIsNation']] == '1'))
        put('inv.toNation_submitted', s, sum(1 for r in R if r[ix['targetIsNation']] == '1' and r[ix['submitted']] == '1'))
    # ---- warentry：可否の理由（宗主の上限）・ランクの段
    t = rd('warentry', s)
    if t:
        ix, R = t
        for col in ['calls', 'evals', 'r_ok', 'r_maxDep', 'r_dependent', 'r_suzerainOf', 'r_atWar', 'r_will', 'r_mil']:
            put('we.' + col, s, sum(int(r[ix[col]]) for r in R))
        rk = Counter(r[ix['rank']] for r in R)
        for k, v in rk.items(): put('we.rank.' + k, s, v)
    # ---- cpmed：調停の白紙（ForcedPeace）
    t = rd('cpmed', s)
    if t:
        ix, R = t
        put('cp.rows', s, len(R)); put('cp.forced', s, sum(1 for r in R if r[ix['forcedFlag']] == '1'))
        put('cp.blank', s, sum(1 for r in R if r[ix['blankFlag']] == '1'))
    t = rd('imp', s)
    if t: put('imp.rows', s, len(t[1]))
    # ---- opium：禁令の緊張（国×四半期）
    t = rd('opium', s)
    if t:
        ix, R = t
        N = [r for r in R if r[0] == 'N']
        put('op.N_rows', s, len(N))
        put('op.tensionPos', s, sum(1 for r in N if fl(r[ix['tensionAddedPerQuarter']]) > 0))
        put('op.possessionBan', s, sum(1 for r in N if r[ix['possessionLawCode']] not in ('0', '')))
        put('op.exportBan', s, sum(1 for r in N if r[ix['exportBanFlag']] == '1'))
    # ---- monai：AI が法を動かしたか（軍需の輸出・阿片の輸出・戦時配給）
    t = rd('monai', s)
    if t:
        ix, R = t
        for fld in ['MilitaryExportBan', 'OpiumExportBan', 'TariffTarget']:
            rs = [r for r in R if r[ix['field']] == fld]
            put('ai.' + fld + '.nations', s, len(rs))
            put('ai.' + fld + '.changed', s, sum(1 for r in rs if int(r[ix['changedQuarters']]) > 0))
            put('ai.' + fld + '.lastOn', s, sum(1 for r in rs if r[ix['lastValue']] in ('1', 'True')))
    # ---- fiscal：貢納の月の行
    t = rd('fiscal', s)
    if t:
        ix, R = t
        if 'ledgerTributeInPerMonth' in ix:
            put('fs.tributeIn_pos_rows', s, sum(1 for r in R if fl(r[ix['ledgerTributeInPerMonth']]) > 0))
            put('fs.rows', s, len(R))
    # ---- rule：条約港が効いている州×四半期
    t = rd('rule', s)
    if t:
        ix, R = t
        put('ru.tp_inForce', s, sum(1 for r in R if r[ix['treatyPortInForceFlag']] == '1'))
        put('ru.portForeignPos', s, sum(1 for r in R if fl(r[ix['portForeignShare']]) > 0))
        put('ru.rows', s, len(R))

with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write(f'# (1141) freq.py  arm={LAB}  dir={D}  seeds {S0}..{S1}\n')
    f.write('# 閾値：WarDeclareTension 140・HighTensionBand 100・DefeatShockProtectMin 0.3・WarWearyThreshold 0.5・MaxDependents 8（03d192514）\n')
    f.write('指標\t種の数\t最小\t中央\t最大\t和\n')
    for k in sorted(per):
        v = [per[k][s] for s in sorted(per[k])]
        fin = [x for x in v if isinstance(x, (int, float)) and not math.isnan(x)]
        if not fin: f.write(f'{k}\t{len(v)}\t-\t-\t-\t-\n'); continue
        def F(x): return f'{x:.4g}' if isinstance(x, float) else str(x)
        f.write(f'{k}\t{len(v)}\t{F(min(fin))}\t{F(statistics.median(fin))}\t{F(max(fin))}\t{F(sum(fin))}\n')
