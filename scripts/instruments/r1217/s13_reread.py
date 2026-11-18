# 読むだけ (1217) c122rs：台帳 S13 の読み直す行を keep_1212 の生で数える（エンジンの閾値の式で数える・★(1136)）。
#  使い方: python -X utf8 -I s13_reread.py <生のフォルダ> <種 1-10> <出力 txt>
#  母集合を名乗る：monlaw＝全ての国×四半期・urxpd＝計器の国の本国の州×月・region＝全ての国の州×四半期・migration＝国内の組×四半期・xmig＝越境の組×四半期。
import sys, collections, math
raw, rng, out = sys.argv[1], sys.argv[2], sys.argv[3]
s0, s1 = map(int, rng.split('-')); seeds = range(s0, s1 + 1)
o = open(out, 'w', encoding='utf-8', newline='\n')
def P(*a): o.write(' '.join(str(x) for x in a) + '\n')
def rows(t, s):
    with open(f'{raw}/{t}_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f: yield ix, l.rstrip('\n').split('\t')
def fl(x):
    try: return float(x)
    except: return float('nan')
# --- monlaw：移民制限法（ImmigrationRestriction 0 開放・1 選別・2 閉鎖）・技術者の出国禁止法（SpecialistExitBan）
tight = opn = pairs = 0; init = collections.Counter(); seb_nz = seb_all = 0; open_ns = collections.Counter(); opened = []
for s in seeds:
    last = {}
    for ix, c in rows('monlaw', s):
        q = int(c[ix['quarter']]); u = c[ix['nationUid']]; v = int(fl(c[ix['ImmigrationRestriction']])); b = fl(c[ix['SpecialistExitBan']])
        seb_all += 1; seb_nz += (b != 0)
        if q == 0 and s == s0: init[(c[ix['nation']], v)] += 1
        if u in last:
            pairs += 1
            if v > last[u]: tight += 1
            if v < last[u]: opn += 1; opened.append((s, q, c[ix['nation']], last[u], v))
        last[u] = v
P('S13-02 開幕 1783 q0 の移民制限法（種', s0, '）：開放でない国', sorted(k for k in init if k[1] != 0))
P('S13-05 締める遷移（段が上がる）', tight, '／ 続いた国×四半期の組', pairs, '（種', rng, '）')
P('S13-06 開く遷移（段が下がる）', opn, '件・国×種', len(set((x[0], x[2]) for x in opened)))
P('S13-51 技術者の出国禁止法が 0 でない国×四半期', seb_nz, '／', seb_all)
# 開いた四半期に条約港の旗（rule_s の treatyPortInForceFlag・その国のどれかの州）
want = {(x[0], x[2], x[1]) for x in opened}
hit = set()
for s in seeds:
    qs = {q for (ss, n, q) in want if ss == s}
    if not qs: continue
    for ix, c in rows('rule', s):
        q = int(c[ix['quarter']])
        if q in qs and (s, c[ix['nation']], q) in want and fl(c[ix['treatyPortInForceFlag']]) > 0: hit.add((s, c[ix['nation']], q))
P('   開く遷移のうち同じ四半期に条約港の旗がある', len(hit), '／', len(want), '・例', sorted(opened)[:6])
# --- urxpd：都市化の門（計器の国の本国の州×月）
tot = fam = serf = flip = nopair = 0
for s in seeds:
    for ix, c in rows('urxpd', s):
        m = fl(c[ix['regionMonths']]); tot += m; fam += fl(c[ix['famineGated']]); serf += fl(c[ix['serfGated']]); flip += fl(c[ix['flowFlipRegionMonths']]); nopair += fl(c[ix['noPair']])
P('S13-12 飢えの門が閉じた州×月', round(fam / tot, 4), '・農奴の門', round(serf / tot, 4), '・向きの反転', round(flip / tot, 4), '・対なし', round(nopair / tot, 4), '（州×月', int(tot), '）')
# --- region：機会・持ち分・賃金・住居・押し・窃盗・流出
n = 0; wf = []; hf_lo = 0; push_lo = 0; wage_lo = 0; opp_max = 0; stay_min = 1e9; theft_pos = 0; theft_sum = 0; earn_sum = 0
cb_out_all = 0; cb_out_hold = 0; hold_le = 0; out_frac_max = 0; out_frac_arg = None
uk = collections.defaultdict(lambda: [0.0, 0.0])
for s in seeds:
    for ix, c in rows('region', s):
        if c[ix['activeFlag']] not in ('1', '1.0', 'True'): pass
        n += 1
        opp = fl(c[ix['opportunityIndex']]); hold = fl(c[ix['holdingFactor']]); w = fl(c[ix['wageFactor']]); hs = fl(c[ix['housingFactor']]); pu = fl(c[ix['pushFactor']])
        pop = fl(c[ix['populationK']])
        if opp > opp_max: opp_max = opp
        if opp * hold < stay_min and pop > 0: stay_min = opp * hold
        if hs < 0.1: hf_lo += 1
        if pu < 0.1: push_lo += 1
        if w < 0.1: wage_lo += 1
        if len(wf) < 2000000: wf.append(w)
        t = fl(c[ix['theftPoolPerMonth']]); e = fl(c[ix['earnedPerMonth']])
        if t > 0: theft_pos += 1
        if math.isfinite(t) and math.isfinite(e): theft_sum += t; earn_sum += e
        cbo = fl(c[ix['crossBorderOutKPerQuarter']])
        if math.isfinite(cbo):
            cb_out_all += cbo
            if hold <= 1.01: cb_out_hold += cbo
        if hold <= 1.01: hold_le += 1
        mo = fl(c[ix['migrationOutKPerQuarter']])
        if pop > 1 and mo / pop > out_frac_max: out_frac_max = mo / pop; out_frac_arg = (s, c[ix['quarter']], c[ix['nation']], c[ix['region']])
        if c[ix['nation']] == 'イギリス' and c[ix['tier']] == 'Core' and math.isfinite(cbo):
            k = (s, c[ix['region']]); uk[k][0] += cbo; uk[k][1] += pop
wf.sort()
P('S13-16 賃金の因子 wageFactor の分位（州×四半期', n, '）：1%', round(wf[len(wf)//100], 3), '中央', round(wf[len(wf)//2], 3), '99%', round(wf[len(wf)*99//100], 3), '・0.1 未満', wage_lo)
P('S13-20 床 0.1 の下：住居', hf_lo, '・押し（計器の式・近似）', push_lo, '・賃金', wage_lo, '／', n)
P('S13-27 上限 0.2 の届き：RegionMobility 0.03×gMax≧0.2 は gMax≧6.67 が要る。機会の最大', round(opp_max, 3), '・残る値打ちの最小', round(stay_min, 3), '＝gMax の上界', round(opp_max - stay_min - 0.15, 3), '・州の四半期の流出÷人口の最大', round(out_frac_max, 4), out_frac_arg)
P('S13-29 持ち分≦1.01 の州×四半期', round(hold_le / n, 4), '・越境の流出のうちその州から', round(cb_out_hold / cb_out_all, 4) if cb_out_all > 0 else 'nan')
P('S13-19 窃盗の溜まりが正の州×四半期', round(theft_pos / n, 4), '・窃盗÷所得（和の比）', f'{theft_sum / earn_sum:.6f}' if earn_sum else 'nan')
ukr = []
for (s, r), (cbo, popsum) in uk.items():
    if popsum > 0: ukr.append((s, r, cbo / popsum * 4))   # 四半期の流出÷人口の平均×4＝年率（近似）
ukr.sort(key=lambda x: -x[2])
P('S13-36 英の本国格の州の越境の流出の年率（近似＝四半期の流出の和÷人口の和×4）上位', [(a, b, f'{c:.5f}') for a, b, c in ukr[:6]], '・州×種', len(ukr))
# --- migration：国内の組
mv = 0; far = 0
for s in seeds:
    for ix, c in rows('migration', s):
        m = fl(c[ix['movedKPerQuarter']]); km = fl(c[ix['routeKm']])
        if math.isfinite(m): mv += m; far += m if km > 2000 else 0
P('S13-26・28 国内の移民の総量（千人）', round(mv), '・経路 2000km 超の割合', round(far / mv, 4) if mv else 'nan')
# --- xmig：越境の組
for s in seeds:
    inn = collections.Counter(); outn = collections.Counter()
    for ix, c in rows('xmig', s):
        m = fl(c[ix['movedKPerQuarter']])
        if not math.isfinite(m): continue
        inn[c[ix['toNation']]] += m; outn[c[ix['fromNation']]] += m
    top = inn.most_common(1)[0] if inn else ('—', 0)
    P('S13-35 種', s, '米の流入', round(inn.get('アメリカ', 0), 1), '流出', round(outn.get('アメリカ', 0), 1), '・最大の受け手', top[0], round(top[1], 1))
o.close()
