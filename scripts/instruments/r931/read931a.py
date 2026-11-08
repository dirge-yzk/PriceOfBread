# (931) 読むだけ：州の産出（工場＋採取の参照価格の値）がちょうど 0 の続き（人口 100 千人以上・4 四半期以上）を分ける。
#   deadreg916.py と同じ定義（region_s の factoryOutputRefValuePerMonth＋extractionOutputRefValuePerMonth＝0・populationK≧100・続き 4 四半期以上）。
#   使い方: python -X utf8 -I read931a.py <ins フォルダ> <種の起点 1|11> <名札> [書き先のフォルダ＝既定はこの台本のあるフォルダ]
#   出力: 区間と州×四半期の数・持ち主・格・長さの分布・入り方・終わり方・州×四半期の生産者の状態（居ない／全部倒産／居て 0）・
#         cearn_s（州のお金・物価・自己費用の割合）と subst_s（自給）の中央を、同じ腕の〈0 でない植民地格の州〉と並べる。
import sys, os, statistics as st, collections as C

M = sys.argv[1]; SB = int(sys.argv[2]); TAG = sys.argv[3]
POP = 100.0; K = 4

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

agg = C.defaultdict(list)       # 欄 -> 値（0 の続きの州×四半期）
ref = C.defaultdict(list)       # 欄 -> 値（0 でない植民地格の州×四半期・人口 100 以上）
owner = C.Counter(); tierc = C.Counter(); lens = []; startc = C.Counter(); endc = C.Counter(); prodst = C.Counter()
regc = C.Counter(); runs_all = []; yearc = C.Counter(); popratio = []; reign = []; ownchg = []; soft = [0, 0]
before = C.defaultdict(list); after = C.defaultdict(list)
for s in range(SB, SB + 10):
    p = os.path.join(M, f'region_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    rows = C.defaultdict(dict)   # region -> q -> tuple
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        q = int(r[ix['quarter']])
        rows[r[ix['region']]][q] = (r[ix['nationIndex']] + ':' + r[ix['nation']], r[ix['tier']], fl(r[ix['populationK']]), fl(r[ix['ruralPopulationK']]),
            fl(r[ix['factoryOutputRefValuePerMonth']]) + fl(r[ix['extractionOutputRefValuePerMonth']]),
            fl(r[ix['factoryActiveCount']]), fl(r[ix['factoryBankruptCount']]), fl(r[ix['extractionActiveCount']]), fl(r[ix['extractionBankruptCount']]),
            fl(r[ix['earnedPerMonth']]), fl(r[ix['foodFillShare']]), int(r[ix['year']]), r[ix['nationIndex']], fl(r[ix['landFactor']]), fl(r[ix['holdingFactor']]), fl(r[ix['arableLand']]))
    f.close()
    zkeys = set(); rkeys = set()
    for reg, d in rows.items():
        qs = sorted(d); run = []
        def close(run):
            if len(run) >= K:
                q0, q1 = run[0], run[-1]
                for q in run: zkeys.add((reg, q))
                prev = d.get(q0 - 1); nxt = d.get(q1 + 1)
                if q0 == 0: sc = '開幕から'
                elif prev is None: sc = '前の行なし'
                elif prev[2] < POP: sc = '人口が 100 を超えて入った'
                else: sc = '産出が落ちて入った'
                if nxt is None: ec = '終わりまで' if q1 == max(qs) and q1 >= 667 else '州の行が消えた'
                elif nxt[12] != d[q1][12]: ec = '持ち主の変化'
                elif nxt[2] < POP: ec = '人口が 100 を割った'
                elif nxt[4] > 0: ec = '再点火'
                else: ec = 'その他'
                ows = {d[q][0] for q in run}
                pr = d[q1][2] / d[q0][2] if d[q0][2] > 0 else float('nan'); popratio.append(pr)
                if ec == '再点火': reign.append((reg, q1 + 1, s))
                if ec == '持ち主の変化': ownchg.append((reg, q1 + 1, s))
                runs_all.append((len(run), s, d[q0][11], reg, ','.join(sorted(ows)), d[q0][1], round(d[q1][2], 1), sc, ec))
                lens.append(len(run)); startc[sc] += 1; endc[ec] += 1; regc[reg] += len(run)
                for q in run:
                    v = d[q]; owner[v[0]] += 1; tierc[v[1]] += 1; yearc[(v[11] // 25) * 25] += 1
                    act = v[5] + v[7]; bank = v[6] + v[8]
                    prodst['居て 0（稼働の数＞0）' if act > 0 else ('全部倒産' if bank > 0 else '稼働 0・倒産 0（全て眠りか Scale 0）')] += 1
                    agg['pop'].append(v[2]); agg['ruralShare'].append(v[3] / v[2] if v[2] > 0 else float('nan'))
                    agg['earnedPerCapPerMonth'].append(v[9] / v[2] if v[2] > 0 else float('nan')); agg['foodFill'].append(v[10])
                    agg['extAct'].append(v[7]); agg['facAct'].append(v[5]); agg['extBank'].append(v[8])
                    agg['landFactor'].append(v[13]); agg['holdingFactor'].append(v[14]); agg['arableLand'].append(v[15])
        for q in qs:
            v = d[q]
            if v[4] == 0.0 and v[2] >= POP:
                if run and run[-1] == q - 1: run.append(q)
                else:
                    close(run); run = [q]
            else:
                close(run); run = []
                if v[1] == 'Colonial' and v[2] >= POP and v[4] > 0:
                    rkeys.add((reg, q))
                    ref['pop'].append(v[2]); ref['ruralShare'].append(v[3] / v[2] if v[2] > 0 else float('nan'))
                    ref['earnedPerCapPerMonth'].append(v[9] / v[2] if v[2] > 0 else float('nan')); ref['foodFill'].append(v[10])
                    ref['extAct'].append(v[7]); ref['facAct'].append(v[5]); ref['extBank'].append(v[8])
                    ref['landFactor'].append(v[13]); ref['holdingFactor'].append(v[14]); ref['arableLand'].append(v[15])
        close(run)
        rs = []
        for q in qs:
            v = d[q]
            if v[4] < 1e-3 and v[2] >= POP:
                if rs and rs[-1] == q - 1: rs.append(q)
                else:
                    if len(rs) >= K: soft[0] += 1; soft[1] += len(rs)
                    rs = [q]
            else:
                if len(rs) >= K: soft[0] += 1; soft[1] += len(rs)
                rs = []
        if len(rs) >= K: soft[0] += 1; soft[1] += len(rs)
    del rows
    bk = {(r_, q_ - 1): 'b' for r_, q_, s_ in reign if s_ == s}; ak = {(r_, q_): 'a' for r_, q_, s_ in reign if s_ == s}
    obk = {(r_, q_ - 1): 'ob' for r_, q_, s_ in ownchg if s_ == s}; oak = {(r_, q_): 'oa' for r_, q_, s_ in ownchg if s_ == s}
    # cearn_s
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if os.path.exists(p):
        f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if len(r) < len(h): continue
            k = (r[ix['region']], int(r[ix['quarter']]))
            for dct, lab in ((bk, '再点火の前'), (ak, '再点火の四半期'), (obk, '持ち主の変化の前'), (oak, '持ち主の変化の四半期')):
                if k in dct:
                    sp_, sm_ = fl(r[ix['staplePrice']]), fl(r[ix['stapleMarginalCost']])
                    before[lab + ':costPriceLevel'].append(fl(r[ix['costPriceLevel']])); before[lab + ':regionPriceLevel'].append(fl(r[ix['regionPriceLevel']]))
                    before[lab + ':localPriceMultiplier'].append(fl(r[ix['localPriceMultiplier']])); before[lab + ':staplePriceOverMc'].append(sp_ / sm_ if sm_ > 0 else float('nan'))
            tgt = agg if k in zkeys else (ref if k in rkeys else None)
            if tgt is None: continue
            pop = fl(r[ix['popK']])
            tgt['savingsPerCap'].append(fl(r[ix['savings']]) / pop if pop > 0 else float('nan'))
            tgt['inMarketPerCapPerQ'].append(fl(r[ix['inMarketPerQuarter']]) / pop if pop > 0 else float('nan'))
            tgt['outMarketPerCapPerQ'].append(fl(r[ix['outMarketPerQuarter']]) / pop if pop > 0 else float('nan'))
            tgt['inStatePerCapPerQ'].append(fl(r[ix['inStatePerQuarter']]) / pop if pop > 0 else float('nan'))
            tgt['outColonialRentPerCapPerQ'].append(fl(r[ix['outColonialRentPerQuarter']]) / pop if pop > 0 else float('nan'))
            tgt['localPriceMultiplier'].append(fl(r[ix['localPriceMultiplier']]))
            tgt['atFloor'].append(fl(r[ix['localPriceAtFloorFlag']]))
            tgt['regionPriceLevel'].append(fl(r[ix['regionPriceLevel']]))
            tgt['costPriceLevel'].append(fl(r[ix['costPriceLevel']]))
            sp, sm = fl(r[ix['staplePrice']]), fl(r[ix['stapleMarginalCost']])
            tgt['staplePriceOverMc'].append(sp / sm if sm > 0 else float('nan'))
            tgt['belowSelfCostShare'].append(fl(r[ix['belowSelfCostShare']]))
            tgt['frozenFlag'].append(fl(r[ix['frozenFlag']]))
            if tgt is agg: tgt['stapleGood:' + r[ix['stapleGood']]].append(1)
        f.close()
    # subst_s
    p = os.path.join(M, f'subst_s{s}.tsv')
    if os.path.exists(p):
        f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if len(r) < len(h): continue
            k = (r[ix['region']], int(r[ix['quarter']]))
            tgt = agg if k in zkeys else (ref if k in rkeys else None)
            if tgt is None: continue
            sp = fl(r[ix['subPopK']])
            tgt['subPopK'].append(sp)
            tgt['engineSubOutputPerMonth'].append(fl(r[ix['engineSubOutputPerMonth']]))
            tgt['farmCapPerMonth'].append(fl(r[ix['farmCapPerMonth']]))
            tgt['farmSoldPerMonth'].append(fl(r[ix['farmSoldPerMonth']]))
            tgt['farmProducedPerMonth'].append(fl(r[ix['farmProducedPerMonth']]))
            tgt['producedZeroFlag'].append(fl(r[ix['producedZeroFlag']]))
        f.close()

out = []
P = out.append
P(f'== {TAG}  {M}')
P(f'区間 {len(lens)}  州×四半期 {sum(lens)}  格 {dict(tierc)}')
P('長さの分布（四半期）: ' + ' '.join(f'{a}-{b}:{sum(1 for x in lens if a <= x <= b)}' for a, b in [(4, 7), (8, 19), (20, 39), (40, 79), (80, 159), (160, 999)]))
P(f'長さの中央 {med(lens)}  最長 {max(lens) if lens else 0}')
P(f'人口の終÷始の中央 {med(popratio):.4g}  0.9 以上の区間 {sum(1 for x in popratio if x >= 0.9)}/{len(popratio)}  0.5 未満 {sum(1 for x in popratio if x < 0.5)}')
P(f'記録：柔らかい定義（産出＜1e-3/月）の続き 区間 {soft[0]}  州×四半期 {soft[1]}')
P('入り方: ' + ' '.join(f'{k}:{v}' for k, v in startc.most_common()))
P('終わり方: ' + ' '.join(f'{k}:{v}' for k, v in endc.most_common()))
P('州×四半期の生産者の状態: ' + ' '.join(f'{k}:{v}' for k, v in prodst.most_common()))
P('持ち主（州×四半期・上位 12）: ' + ' '.join(f'{k}:{v}' for k, v in owner.most_common(12)))
P('年の帯（25 年・州×四半期）: ' + ' '.join(f'{k}:{v}' for k, v in sorted(yearc.items())))
P('州（州×四半期・上位 12）: ' + ' '.join(f'{k}:{v}' for k, v in regc.most_common(12)))
sg = sorted(((k[11:], len(v)) for k, v in agg.items() if k.startswith('stapleGood:')), key=lambda x: -x[1])
P('主食（州×四半期）: ' + ' '.join(f'{k}:{v}' for k, v in sg[:6]))
P('欄\t0 の続きの中央\t0 でない植民地格の中央')
for k in ['landFactor', 'holdingFactor', 'arableLand', 'pop', 'ruralShare', 'earnedPerCapPerMonth', 'foodFill', 'facAct', 'extAct', 'extBank', 'savingsPerCap', 'inMarketPerCapPerQ', 'outMarketPerCapPerQ',
          'inStatePerCapPerQ', 'outColonialRentPerCapPerQ', 'localPriceMultiplier', 'atFloor', 'regionPriceLevel', 'costPriceLevel', 'staplePriceOverMc',
          'belowSelfCostShare', 'frozenFlag', 'subPopK', 'engineSubOutputPerMonth', 'farmCapPerMonth', 'farmSoldPerMonth', 'farmProducedPerMonth', 'producedZeroFlag']:
    a = agg.get(k, []); b = ref.get(k, [])
    am = sum(1 for x in a if x == x)
    P(f'{k}\t{med(a):.6g} (n={am}, 平均 {(sum(x for x in a if x == x) / am) if am else float("nan"):.4g})\t{med(b):.6g}')
P('再点火と持ち主の変化の前後（cearn・中央）')
for k in sorted(before): P(f'  {k}	{med(before[k]):.5g} (n={len(before[k])})')
runs_all.sort(reverse=True)
P('最長の区間（上位 15）: 長さ 種 始めの年 州 持ち主 格 人口(終) 入り方 終わり方')
for r in runs_all[:15]: P('  ' + ' '.join(str(x) for x in r))
# (977) 書き先は 4 つ目の引数（フォルダ）。無ければ従来どおりこの台本のあるフォルダ。
OUTDIR = sys.argv[4] if len(sys.argv) > 4 else os.path.dirname(os.path.abspath(__file__))
os.makedirs(OUTDIR, exist_ok=True)
open(os.path.join(OUTDIR, f'read931a_{TAG}_out.txt'), 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print('\n'.join(out[:12]))
