# (965) 読むだけ：植民地格の州の輸出が閉じる根。区間の定義は deadreg916 と同じ（region_s の工場＋採取の産出＝0・人口 100 千人以上・4 四半期以上）。
#   使い方: python -X utf8 -I read965b.py <ins フォルダ> <種の起点 1|11> <名札> <出力フォルダ>
#   読む表: region_s・cearn_s（inExportPerQuarter ほか）・trroute_s（10 年ごとの年の終わり・上位 3000 組＋残り 1 行）・port_s。
#   出力: <名札>_sum.txt（問い 2〜4 の表）・<名札>_close.tsv（輸出が閉じた出来事 1 行ずつ）・<名札>_dead.tsv（0 の続き 1 行ずつ）。
import sys, os, statistics as st, collections as C

M, SB, TAG, OUT = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
POP = 100.0; K = 4; LOOK = 80
GM = {}
for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'goodmap.tsv'), encoding='utf-8'):
    i, n, t = l.rstrip('\n').split('\t'); GM[int(i)] = (n, t)
GRAIN = {15, 42}   # 麦・小麦粉（Colonial.ExportCrop の GrainChain＝主食の側）

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

GN = {n: (i, t) for i, (n, t) in GM.items()}
def kind(g):
    if g not in GN: return 'none'
    i, t = GN[g]
    if i in GRAIN or t == 'Staple': return 'staple'
    return 'cash'

def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')

S = C.Counter(); D = C.defaultdict(list)
fc = open(os.path.join(OUT, f'{TAG}_close.tsv'), 'w', encoding='utf-8')
fc.write('seed\tregion\tq\tyear\tnationBefore\tnationAfter\toutAtClose\tdeadStartLag\texpPcBefore\tsavPcBefore\tsavPcAfter\t'
         'top1Before\ttop1KindBefore\tratioTop1Before\tratioTop1After\tratioStapleBefore\tratioStapleAfter\tlpmBefore\tlpmAfter\tatFloorAfter\t'
         'rplBefore\trplAfter\ttaxPcBefore\ttaxPcAfter\tmktPcBefore\tmktPcAfter\tpopRatio\tportBefore\tportAfter\ttrSellerYb\ttrSellerYa\ttrValYb\ttrCutYb\ttrCrossYb\ttrFreightShareYb\n')
fd = open(os.path.join(OUT, f'{TAG}_dead.tsv'), 'w', encoding='utf-8')
freg = open(os.path.join(OUT, f'{TAG}_reg.tsv'), 'w', encoding='utf-8')
freg.write('\t'.join('seed region nq nExpQ nPortQ nDeadQ nation top1Kind ratioTop1 ratioStaple rpl savPc mktPc expPc trSellerSnaps trSnapsInRange qFirst qLast soldShareExpQ soldShareNoExpQ subRate'.split()) + '\n')
fd.write('seed\tregion\tq0\tlen\tnation\tlastExpLag\tlastExpSubLag\texpQuartersIn80\toutAtLastExp\tsavPcAtLastExp\tsavPcAtQ0\ttop1AtLastExp\n')

for s in range(SB, SB + 10):
    pr = os.path.join(M, f'region_s{s}.tsv'); pc = os.path.join(M, f'cearn_s{s}.tsv')
    if not (os.path.exists(pr) and os.path.exists(pc)): print('欠け', s); continue
    # region_s
    reg = C.defaultdict(dict)
    f = open(pr, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        try: q = int(r[ix['quarter']])
        except ValueError: continue
        reg[r[ix['region']]][q] = (r[ix['nation']], r[ix['tier']], fl(r[ix['populationK']]),
                                   fl(r[ix['factoryOutputRefValuePerMonth']]) + fl(r[ix['extractionOutputRefValuePerMonth']]), int(r[ix['year']]))
    # cearn_s
    ce = C.defaultdict(dict)
    f = open(pc, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    cols = ['popK', 'savings', 'inExportPerQuarter', 'inMarketPerQuarter', 'outTaxPerQuarter', 'staplePrice', 'stapleMarginalCost',
            'top1Price', 'top1MarginalCost', 'localPriceMultiplier', 'localPriceAtFloorFlag', 'regionPriceLevel', 'costPriceLevel']
    ci = [ix[c] for c in cols]; it1 = ix['top1Good']; isg = ix['stapleGood']; iq = ix['quarter']; ire = ix['region']
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        try: q = int(r[iq])
        except ValueError: continue
        v = [fl(r[i]) for i in ci]
        ce[r[ire]][q] = (v, r[it1])
    # port_s
    port = set()
    pp = os.path.join(M, f'port_s{s}.tsv')
    if os.path.exists(pp):
        f = open(pp, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            try: port.add((r[ix['region']], int(r[ix['quarter']])))
            except (ValueError, IndexError): pass
    # trroute（10 年ごと）
    trs = C.defaultdict(lambda: C.defaultdict(lambda: [0.0, 0.0, 0])); cut = {}
    pt = os.path.join(M, f'trroute_s{s}.tsv')
    f = open(pt, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    mins = C.defaultdict(lambda: float('inf'))
    for l in f:
        r = l.rstrip('\n').split('\t')
        try: y = int(r[0])
        except ValueError: continue
        if r[1].startswith('('): continue
        val = fl(r[ix['valueGoldPerYear']]); fr = fl(r[ix['freightGoldPerYear']])
        a = trs[y][r[1]]; a[0] += val; a[1] += fr; a[2] += 1 if r[ix['sellerNation']] != r[ix['buyerNation']] else 0
        if val < mins[y]: mins[y] = val
    years = sorted(trs.keys())

    def pc_(v, i):   # 1 千人あたり
        return v[0][i] / v[0][0] if v[0][0] > 0 else float('nan')

    def ratio(v, ip, im):
        d = v[0][im] * v[0][12] * v[0][9]
        return v[0][ip] / d if d > 0 else float('nan')

    # 0 でない植民地格の州×四半期の基準（問い 4）と 0 の続き
    deadq = set()
    for rn, ser in reg.items():
        qs = sorted(ser); run = []
        def flush():
            if len(run) >= K:
                for qq in run: deadq.add((rn, qq))
                D['runs'].append((rn, run[0], len(run)))
        for q in qs:
            nat, tier, pop, out, yr = ser[q]
            if out == 0.0 and pop >= POP:
                if run and run[-1] != q - 1: flush(); run = []
                run.append(q)
            else:
                flush(); run = []
        flush()
    runs = [x for x in D['runs']]; D['runs'] = []
    expref = []
    for rn, ser in reg.items():
        cs = ce.get(rn, {})
        for q, (nat, tier, pop, out, yr) in ser.items():
            if tier != 'Colonial' or pop < POP or (rn, q) in deadq: continue
            v = cs.get(q)
            if v is None: continue
            S[f'ref_rq'] += 1
            if v[0][2] > 0: S['ref_rq_exp'] += 1; expref.append(pc_(v, 2))
    refmed = med(expref); sub = 0.01 * refmed if refmed == refmed else 0.0
    D['refmed'].append(refmed)
    # 0 の続きごと（問い 2 の前半）
    for rn, q0, ln in runs:
        cs = ce.get(rn, {}); ser = reg[rn]
        last = None; lastsub = None; nq80 = 0
        for q in sorted(cs):
            if q >= q0: break
            v = cs[q]
            if v[0][2] > 0:
                last = q
                if q >= q0 - LOOK: nq80 += 1
                if pc_(v, 2) >= sub: lastsub = q
        lag = q0 - last if last is not None else -1; lagsub = q0 - lastsub if lastsub is not None else -1
        oat = ser[last][3] if last is not None and last in ser else float('nan')
        sl = pc_(cs[last], 1) if last is not None else float('nan'); s0 = pc_(cs[q0], 1) if q0 in cs else float('nan')
        t1 = cs[last][1] if last is not None else '-'
        fd.write(f'{s}\t{rn}\t{q0}\t{ln}\t{ser[q0][0]}\t{lag}\t{lagsub}\t{nq80}\t{oat}\t{sl}\t{s0}\t{t1}\n')
        S['dead_runs'] += 1; S['dead_rq'] += ln
        b = 'never' if lag < 0 else ('1' if lag == 1 else ('2-4' if lag <= 4 else ('5-20' if lag <= 20 else ('21-80' if lag <= 80 else '>80'))))
        S['deadlag_' + b] += 1
        bs = 'never' if lagsub < 0 else ('1' if lagsub == 1 else ('2-4' if lagsub <= 4 else ('5-20' if lagsub <= 20 else ('21-80' if lagsub <= 80 else '>80'))))
        S['deadlagsub_' + bs] += 1
        if last is not None: S['dead_outpos_at_lastexp'] += 1 if oat > 0 else 0
        D['dead_nq80'].append(nq80)
    runstart = C.defaultdict(list)
    for rn, q0, ln in runs: runstart[rn].append(q0)
    # 輸出が閉じた出来事（問い 2 の後半・問い 3）：植民地格・人口 100 以上・前の四半期に輸出＞0（1% の塵より上）・続く 4 四半期が全て 0
    for rn, cs in ce.items():
        ser = reg.get(rn, {})
        qs = sorted(cs)
        for i in range(1, len(qs)):
            q = qs[i]
            if qs[i - 1] != q - 1: continue
            vb = cs[q - 1]
            if not (pc_(vb, 2) >= sub) or not (vb[0][2] > 0): continue
            if any((q + k) not in cs or cs[q + k][0][2] > 0 for k in range(K)): continue
            if q not in ser or ser[q][1] != 'Colonial' or ser[q][2] < POP: continue
            S['close_n'] += 1
            nb = ser.get(q - 4, ser.get(q - 1, ser[q]))[0]; na = ser.get(q + 3, ser[q])[0]
            if nb != na: S['close_owner_change'] += 1
            outc = ser[q][3]
            if outc > 0: S['close_outpos'] += 1
            ds = [x - q for x in runstart.get(rn, []) if -LOOK <= x - q <= LOOK]
            lagd = min(ds, key=abs) if ds else 9999
            if ds and lagd >= 0: S['close_dead_after_80'] += 1
            if ds and lagd < 0: S['close_dead_before_80'] += 1
            pre = [cs[q - k] for k in range(1, K + 1) if (q - k) in cs]; post = [cs[q + k] for k in range(K) if (q + k) in cs]
            t1 = vb[1]; kd = kind(t1); S['close_top1_' + kd] += 1
            pb = (rn, q - 1) in port; pa = (rn, q) in port
            yb = max([y for y in years if y <= ser[q][4] - 1], default=None); ya = min([y for y in years if y >= ser[q][4] + 1], default=None)
            tb = trs[yb].get(rn) if yb is not None else None; ta = trs[ya].get(rn) if ya is not None else None
            if yb is not None: S['close_tr_yb_known'] += 1; S['close_tr_seller_yb'] += 1 if tb else 0
            if ya is not None: S['close_tr_ya_known'] += 1; S['close_tr_seller_ya'] += 1 if ta else 0
            row = [s, rn, q, ser[q][4], nb, na, outc, lagd, pc_(vb, 2), mean([pc_(v, 1) for v in pre]), mean([pc_(v, 1) for v in post]),
                   t1, kd, mean([ratio(v, 7, 8) for v in pre]), mean([ratio(v, 7, 8) for v in post]),
                   mean([ratio(v, 5, 6) for v in pre]), mean([ratio(v, 5, 6) for v in post]), mean([v[0][9] for v in pre]), mean([v[0][9] for v in post]),
                   max([v[0][10] for v in post], default=float('nan')), mean([v[0][11] for v in pre]), mean([v[0][11] for v in post]),
                   mean([pc_(v, 4) for v in pre]), mean([pc_(v, 4) for v in post]), mean([pc_(v, 3) for v in pre]), mean([pc_(v, 3) for v in post]),
                   (ser.get(q + 3, ser[q])[2] / ser[q - 1][2]) if (q - 1) in ser and ser[q - 1][2] > 0 else float('nan'),
                   int(pb), int(pa), int(bool(tb)), int(bool(ta)), tb[0] if tb else 0, mins.get(yb, float('nan')) if yb is not None else float('nan'),
                   tb[2] if tb else 0, (tb[1] / tb[0]) if tb and tb[0] > 0 else float('nan')]
            fc.write('\t'.join(str(x) for x in row) + '\n')
            D['close_rows'].append(row)
    # subst_s：農の売った量÷作った量（自給の残りを市場へ出す割合）
    sb = {}
    psb = os.path.join(M, f'subst_s{s}.tsv')
    if os.path.exists(psb):
        f = open(psb, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        a1, a2, a3 = ix['farmSoldPerMonth'], ix['farmProducedPerMonth'], ix['subRateWeighted']
        for l in f:
            r = l.rstrip('\n').split('\t')
            try: q = int(r[0])
            except ValueError: continue
            if (r[ix['region']] in ce):
                pv = fl(r[a2]); sb[(r[ix['region']], q)] = (fl(r[a1]) / pv if pv > 0 else float('nan'), fl(r[a3]))
    # 州ごとの静的な比べ（問い 4）：植民地格・人口 100 以上の四半期だけ
    for rn, cs in ce.items():
        ser = reg.get(rn, {}); qs = [q for q in sorted(cs) if q in ser and ser[q][1] == 'Colonial' and ser[q][2] >= POP]
        if not qs: continue
        nexp = sum(1 for q in qs if cs[q][0][2] > 0); npt = sum(1 for q in qs if (rn, q) in port)
        nd = sum(1 for q in qs if (rn, q) in deadq); kinds = C.Counter(kind(cs[q][1]) for q in qs if (rn, q) not in deadq)
        nats = C.Counter(ser[q][0] for q in qs)
        trn = sum(1 for y in years if rn in trs[y]); trk = sum(1 for y in years if any(ser[q][4] == y for q in qs))
        freg.write('\t'.join(str(x) for x in [s, rn, len(qs), nexp, npt, nd, nats.most_common(1)[0][0], kinds.most_common(1)[0][0] if kinds else 'none',
            med([ratio(cs[q], 7, 8) for q in qs]), med([ratio(cs[q], 5, 6) for q in qs]), med([cs[q][0][11] for q in qs]), med([pc_(cs[q], 1) for q in qs]),
            med([pc_(cs[q], 3) for q in qs]), med([pc_(cs[q], 2) for q in qs]), trn, trk, qs[0], qs[-1],
            med([sb[(rn, q)][0] for q in qs if (rn, q) in sb and cs[q][0][2] > 0]), med([sb[(rn, q)][0] for q in qs if (rn, q) in sb and not (cs[q][0][2] > 0)]),
            med([sb[(rn, q)][1] for q in qs if (rn, q) in sb])]) + '\n')
    print(f'種 {s} 読んだ', flush=True)

fc.close(); fd.close(); freg.close()
o = open(os.path.join(OUT, f'{TAG}_sum.txt'), 'w', encoding='utf-8')
P = lambda *a: print(*a, file=o)
P(f'# {TAG}  {M}  種 {SB}〜{SB + 9}')
P('0 でない植民地格の州×四半期', S['ref_rq'], '輸出＞0 の割合', round(S['ref_rq_exp'] / max(1, S['ref_rq']), 4), '輸出＞0 の 1 千人あたりの中央（種ごと）', [round(x, 4) for x in D['refmed']])
P('0 の続き 区間', S['dead_runs'], '州×四半期', S['dead_rq'])
P('入り口 q0 から見た最後の輸出＞0 までの四半期（区間の数）', {k[8:]: v for k, v in S.items() if k.startswith('deadlag_')})
P('同（1% の塵より上の輸出）', {k[11:]: v for k, v in S.items() if k.startswith('deadlagsub_')})
P('最後の輸出の四半期に産出＞0 の区間', S['dead_outpos_at_lastexp'], '入り口の前 80 四半期の輸出＞0 の四半期数の中央', med(D['dead_nq80']))
cr = D['close_rows']; n = max(1, S['close_n'])
P('輸出が閉じた出来事', S['close_n'], '閉じた四半期に産出＞0', S['close_outpos'], f'({S["close_outpos"] / n:.3f})', '持ち主の替わり（前 4〜後 3）', S['close_owner_change'], f'({S["close_owner_change"] / n:.3f})')
P('閉じた後 80 四半期内に 0 の続きが始まる', S['close_dead_after_80'], f'({S["close_dead_after_80"] / n:.3f})', '閉じる前 80 四半期内に 0 の続きが始まっていた', S['close_dead_before_80'])
P('閉じる前の top1 の種類', {k[11:]: v for k, v in S.items() if k.startswith('close_top1_')})
P('trroute：前の 10 年の区切りで売り手に居た', S['close_tr_seller_yb'], '/', S['close_tr_yb_known'], '後の区切りで売り手に居た', S['close_tr_seller_ya'], '/', S['close_tr_ya_known'])
def colmed(i, sel=lambda r: True): return med([r[i] for r in cr if sel(r)])
names = {9: '貯蓄/千人 前', 10: '貯蓄/千人 後', 13: 'top1 値段÷自己費用 前', 14: 'top1 値段÷自己費用 後', 15: '主食 値段÷自己費用 前', 16: '主食 値段÷自己費用 後',
         17: '州の物価の倍率 前', 18: '州の物価の倍率 後', 20: '州の物価水準 前', 21: '州の物価水準 後', 22: '税/千人 前', 23: '税/千人 後', 24: '市場の所得/千人 前', 25: '市場の所得/千人 後', 26: '人口 後÷前', 34: '運賃÷額（前の区切り）'}
for i, nm in names.items(): P(f'  {nm}\t中央 {colmed(i):.4g}\t産出＞0 で閉じた {colmed(i, lambda r: r[6] > 0):.4g}\t後に 0 の続き {colmed(i, lambda r: 0 <= r[7] <= LOOK):.4g}')
P('  閉じた後に州の物価の倍率が下限に着いた', sum(1 for r in cr if r[19] == 1), '港 前→後', sum(r[27] for r in cr), '→', sum(r[28] for r in cr))
P('  前の区切りで売り手に居た出来事の額÷上位 3000 の最小の中央', med([r[31] / r[32] for r in cr if r[29] and r[32] == r[32] and r[32] > 0]))
yc = C.Counter((r[3] // 25) * 25 for r in cr); P('  閉じた年（25 年刻み）', dict(sorted(yc.items())))
oc = C.Counter(r[5] for r in cr); P('  持ち主（後）の上位', oc.most_common(8))
tc = C.Counter(r[11] for r in cr); P('  top1 の財の上位', tc.most_common(10))
o.close()
print(open(os.path.join(OUT, f'{TAG}_sum.txt'), encoding='utf-8').read())
