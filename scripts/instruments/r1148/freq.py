# (1148) S26 植民地の経営：台帳の行ごとの頻度（届いた割合）を生の表から数える（読むだけ・エンジンの閾値の式で）
# 使い方: python -X utf8 -I freq.py <生のフォルダ> <ラベル> <リポジトリ>
# 計器が書かない行は 0 と数えない＝表が無ければ「表なし」と書く。
import sys, os, json, statistics as st
D, LAB, REPO = sys.argv[1], sys.argv[2], sys.argv[3]
sys.stdout.reconfigure(newline='\n')
SIM = os.path.join(REPO, 'PriceOfBread.SimCore')
goods = json.load(open(os.path.join(SIM, 'goods.json'), encoding='utf-8-sig'))
goods = goods if isinstance(goods, list) else goods['goods']
COLG = {g['name'] for g in goods if g.get('isColonial')}
W = json.load(open(os.path.join(SIM, 'world.json'), encoding='utf-8'))
QNAMES = {c['name'] for t in W['abstractTargets'] if t.get('quinine') for c in t['colonies']}
UNREST_W = 0.006; FULL_COL = 3.0   # Coefficients.p06_rank_discovery.cs:81・p05_port_insurance.cs:19（grep で写した値）

def rd(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    f = open(p, encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    return h, f
def q(v, ps=(5, 50, 95)):
    if not v: return 'n=0'
    v = sorted(v); n = len(v)
    return 'n=%d ' % n + '/'.join('%.4g' % v[min(n - 1, int(p / 100 * n))] for p in ps)
def fl(x):
    try: return float(x)
    except: return float('nan')
out = []
def P(*a): out.append('\t'.join(str(x) for x in a))
seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(D, f'nation_s{s}.tsv'))]
P('# arm', LAB, 'seeds', ','.join(map(str, seeds)), 'quinineColonies', len(QNAMES), 'colonialGoods', len(COLG))
agg = {}
def A(k, v): agg.setdefault(k, []).append(v)
for s in seeds:
    # nation：植民地格の州の和（開幕・最後）・格の州×四半期
    r = rd('nation', s)
    if r:
        h, f = r; iq, icr, iint, icore, inat, iy = h.index('quarter'), h.index('colonialRegions'), h.index('integratedRegions'), h.index('coreRegions'), h.index('nation'), h.index('year')
        iam = h.index('adminFiscalMeanMul')
        cr0 = 0; byq = {}; tierq = [0, 0, 0]; amC = []; amN = []; colnq = 0; nq = 0
        for l in f:
            x = l.rstrip('\n').split('\t'); qq = int(x[iq]); c = fl(x[icr])
            byq[qq] = byq.get(qq, 0) + c
            tierq[0] += fl(x[icore]); tierq[1] += fl(x[iint]); tierq[2] += c
            nq += 1
            if c > 0: colnq += 1
            y = int(fl(x[iy]))
            if y in (1790, 1850, 1900):
                a = fl(x[iam])
                if a == a and a > 0: (amC if c > 0 else amN).append(a)
        last = max(byq)
        P(s, 'S26-01/35 colonialRegions q0', byq.get(0), 'last', byq[last], 'max', max(byq.values()), 'nationQuarterWithColonies', colnq, 'of', nq)
        P(s, 'S26-11 regionQuarters core/integrated/colonial', *['%d' % v for v in tierq])
        P(s, 'S26-13 adminFiscalMeanMul(1790/1850/1900) withCol', q(amC), 'noCol', q(amN))
        A('cr0', byq.get(0)); A('crlast', byq[last]); A('crmax', max(byq.values())); A('colnq', colnq / max(1, nq))
        for v in amC: A('amC', v)
        for v in amN: A('amN', v)
    else: P(s, 'nation', '表なし')
    # rule：開幕の植民地格の支配度
    r = rd('rule', s)
    if r:
        h, f = r; iq, it, idm = h.index('quarter'), h.index('tier'), h.index('dominanceShare')
        n = one = 0; q0seen = False
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[iq] != '0':
                if q0seen: break
                continue
            q0seen = True
            if x[it] == 'Colonial':
                n += 1; one += fl(x[idm]) >= 0.999999
        P(s, 'S26-03 q0 colonial dominance==1', one, 'of', n)
        A('dom1', one / max(1, n))
    # region：開幕の植民地格の州の名・マラリア地（キニーネの標的の名）を持つ国×四半期・移民の受け
    r = rd('region', s)
    colq0 = set(); malnq = set(); colTierRegQ = {}
    if r:
        h, f = r; iq, it, ir, inat, ip = h.index('quarter'), h.index('tier'), h.index('region'), h.index('nation'), h.index('populationK')
        iact = h.index('activeFlag')
        allnq = set(); colPopByQ = {}; homePopByQ = {}
        for l in f:
            x = l.rstrip('\n').split('\t'); qq = int(x[iq])
            if x[iact] not in ('1', 'True', 'true'): continue
            allnq.add((qq, x[inat]))
            if x[it] == 'Colonial':
                colTierRegQ.setdefault(qq, set()).add(x[ir])
                if qq == 0: colq0.add(x[ir])
                if x[ir] in QNAMES: malnq.add((qq, x[inat]))
                colPopByQ[qq] = colPopByQ.get(qq, 0) + fl(x[ip])
            else: homePopByQ[qq] = homePopByQ.get(qq, 0) + fl(x[ip])
        last = max(colPopByQ) if colPopByQ else 0
        yrs = {}
        P(s, 'S26-20 nationQuarters holding a quinine-target colonial region', len(malnq), 'of active nationQuarters', len(allnq), 'firstQ', min((a for a, _ in malnq), default='-'))
        P(s, 'S26-35 colonialTierPopK q0', '%.0f' % colPopByQ.get(0, 0), 'last', '%.0f' % colPopByQ.get(last, 0), 'homePopK q0', '%.0f' % homePopByQ.get(0, 0))
        A('maln', len(malnq) / max(1, len(allnq))); A('malany', 1 if malnq else 0)
    # cpop：開幕の植民地格の州の階級の人口
    r = rd('cpop', s)
    if r and colq0:
        h, f = r; iq, ir, icn, ipk = h.index('quarter'), h.index('region'), h.index('className'), h.index('popK')
        bycls = {}; q0seen = False
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[iq] != '0':
                if q0seen: break
                continue
            q0seen = True
            if x[ir] in colq0: bycls[x[icn]] = bycls.get(x[icn], 0) + fl(x[ipk])
        tot = sum(bycls.values())
        P(s, 'S26-04 q0 colonial classes popK', ' '.join('%s=%.1f' % (k, v) for k, v in sorted(bycls.items(), key=lambda z: -z[1]) if v > 0), 'peasantShare', '%.4f' % (bycls.get('農民', 0) / tot if tot else float('nan')))
        A('peas', bycls.get('農民', 0) / tot if tot else float('nan'))
    # landshare：植民地の地代と送金
    r = rd('landshare', s)
    if r:
        h, f = r; irc, irem, ifc = h.index('rentColonialPerQuarter'), h.index('colonialRemitPerQuarter'), h.index('rgoFieldColonialCount')
        n = rc = rm = fc = 0; ratio = []
        for l in f:
            x = l.rstrip('\n').split('\t'); n += 1
            a, b, c = fl(x[irc]), fl(x[irem]), fl(x[ifc])
            if c > 0: fc += 1
            if a > 0: rc += 1
            if b != 0 and b == b: rm += 1
            if a > 1e-9 and b == b: ratio.append(b / a)
        P(s, 'S26-07/08 landshare rows', n, 'rgoFieldColonial>0', fc, 'rentColonial>0', rc, 'remit!=0', rm, 'remit/rentColonial', q(ratio))
        A('ls_fc', fc); A('ls_rc', rc); A('ls_rm', rm)
        for v in ratio: A('ls_ratio', v)
    # fiscal：植民地の維持費の請求（燃料・キニーネ）
    r = rd('fiscal', s)
    if r:
        h, f = r; iy, icl, inat = h.index('year'), h.index('claimColonialUpkeepPerMonth'), h.index('nation')
        ilt = h.index('claimTotalPerMonth')
        n = pos = 0; byper = {}; first = None; share = []
        for l in f:
            x = l.rstrip('\n').split('\t'); y = int(fl(x[iy])); a = fl(x[icl]); n += 1
            per = '1783-1829' if y < 1830 else ('1830-1869' if y < 1870 else ('1870-1909' if y < 1910 else '1910-'))
            b = byper.setdefault(per, [0, 0]); b[1] += 1
            if a > 0:
                pos += 1; b[0] += 1; first = y if first is None else min(first, y)
                t = fl(x[ilt])
                if t > 0: share.append(a / t)
        P(s, 'S26-16 fiscal months', n, 'colonialUpkeep>0', pos, 'firstYear', first, ' '.join('%s:%d/%d' % (k, v[0], v[1]) for k, v in sorted(byper.items())), 'share of claimTotal', q(share))
        A('fis_pos', pos / max(1, n)); A('fis_first', first if first else float('nan'))
        for v in share: A('fis_share', v)
    # canal：開通の年
    r = rd('canal', s)
    if r:
        h, f = r; yrs = {x.split('\t')[0]: int(x.split('\t')[2]) for x in f if x.strip()}
        P(s, 'S26-19 canal open year', 'suez', yrs.get('suez', '-'), 'panama', yrs.get('panama', '-'))
        if 'suez' in yrs: A('suez', yrs['suez'])
        if 'panama' in yrs: A('panama', yrs['panama'])
    # secession：反帝世論の月の加算の見積もり（植民地格の州の不満の人口加重×帝国の規模）・参政権の算入
    r = rd('secession', s)
    if r:
        h, f = r; iq, it, inat, ipk, imil, isuf = h.index('quarter'), h.index('tier'), h.index('nation'), h.index('popK'), h.index('militancyMeanIndex'), h.index('suffrageInclusionShare')
        acc = {}; sufpos = sufn = 0
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[it] != 'Colonial': continue
            k = (x[iq], x[inat]); a = acc.setdefault(k, [0.0, 0.0, 0])
            pk, m = fl(x[ipk]), fl(x[imil])
            if pk == pk and m == m: a[0] += pk * m; a[1] += pk; a[2] += 1
            sv = fl(x[isuf]); sufn += 1; sufpos += sv > 1e-9
        hp = [UNREST_W * (a[0] / a[1]) * min(1.0, a[2] / FULL_COL) for a in acc.values() if a[1] > 1e-9]
        P(s, 'S26-23 antiImperial monthly add (approx: popK weight, colonies=colonial-tier rows of the instrumented nation)', q(hp, (5, 50, 95, 99)), 'quarter12xMonths', '%.4g' % (3 * st.median(hp) if hp else float('nan')))
        P(s, 'S26-45/46 colonial-tier rows suffrageInclusion>0', sufpos, 'of', sufn)
        for v in hp: A('hp', v)
        A('suf', sufpos / max(1, sufn))
    # migration：植民地格の州への国内の移民
    r = rd('migration', s)
    if r:
        h, f = r; iq, ito, imv = h.index('quarter'), h.index('toRegion'), h.index('movedKPerQuarter')
        tot = toc = 0.0; n = nc = 0
        for l in f:
            x = l.rstrip('\n').split('\t'); m = fl(x[imv]); qq = int(x[iq])
            if not m == m: continue
            tot += m; n += 1
            if x[ito] in colTierRegQ.get(qq, ()): toc += m; nc += 1
        P(s, 'S26-29 domestic migration rows', n, 'movedK', '%.0f' % tot, 'toColonialTier rows', nc, 'movedK', '%.0f' % toc)
        A('mig', tot); A('migc', toc)
    # xmig：国家間の移民
    r = rd('xmig', s)
    if r:
        h, f = r; imv, igc = h.index('movedKPerQuarter'), h.index('greatCircleKm')
        tot = 0.0; wk = []
        for l in f:
            x = l.rstrip('\n').split('\t'); m = fl(x[imv])
            if m == m: tot += m; wk.append((fl(x[igc]), m))
        wk.sort(); half = tot / 2; cum = 0; med = float('nan')
        for k, m in wk:
            cum += m
            if cum >= half: med = k; break
        P(s, 'S26-30 crossBorder movedK', '%.0f' % tot, 'weightedMedianKm', '%.0f' % med)
        A('xmig', tot)
    # nationgood：植民地財の越境の輸出額の割合
    r = rd('nationgood', s)
    if r:
        h, f = r; iy, ig, ie = h.index('year'), h.index('good'), h.index('crossExportValue')
        byy = {}
        for l in f:
            x = l.rstrip('\n').split('\t'); y = int(fl(x[iy]))
            if y not in (1790, 1850, 1900, 1940): continue
            e = fl(x[ie])
            if not e == e: continue
            b = byy.setdefault(y, [0.0, 0.0]); b[1] += e
            if x[ig] in COLG: b[0] += e
        P(s, 'S26-28 colonialGoods crossExport share', ' '.join('%d:%.3f' % (y, b[0] / b[1] if b[1] else float('nan')) for y, b in sorted(byy.items())))
        for y, b in byy.items():
            if b[1]: A('cg%d' % y, b[0] / b[1])
P('# --- aggregate over seeds')
for k, v in agg.items():
    v = [z for z in v if z == z]
    if not v: continue
    v.sort()
    P('agg', k, 'n=%d' % len(v), 'min %.4g' % v[0], 'median %.4g' % st.median(v), 'max %.4g' % v[-1])
print('\n'.join(out))
