# (857) c70ea Q3：M の種 8 の共和制デンマーク（州ペルー）の名目 GDP 0 の四半期の状態（読むだけ）
#   python -I q3_denmark.py <出力> <腕名=ins>...   種 8 だけ。州ペルーの持ち主の時系列・州のお金と流れ・POP の内訳・破綻の確かめ（NaN・残差）
import sys, os, math
from collections import defaultdict
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
NAT = '共和制デンマーク'; REG = 'ペルー'; S = 8
L = []
def P(s=''): L.append(s)
def rows(d, t, pred):
    fn = os.path.join(d, f'{t}_s{S}.tsv')
    if not os.path.exists(fn): return None, []
    with open(fn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); out = []
        for line in f:
            if pred(line): out.append(line.rstrip('\n').split('\t'))
    return h, out
for name, d in arms:
    P(f'==== {name} = {d}')
    # 州ペルーの持ち主の時系列（region_s）
    h, rs = rows(d, 'region', lambda l: f'\t{REG}\t' in l)
    if h:
        iq, inat, it, ipk, iact, iearn, ifood = [h.index(c) for c in ['quarter', 'nation', 'tier', 'populationK', 'activeFlag', 'earnedPerMonth', 'foodFillShare']]
        prev = None
        P('Q3a\tquarter\tyear\tnation\ttier\tpopulationK\tearnedPerMonth\tfoodFillShare\t(持ち主か格が変わった行と 40 四半期ごと)')
        for x in rs:
            key = (x[inat], x[it])
            if key != prev or int(x[iq]) % 40 == 0:
                P(f'Q3a\t{x[iq]}\t{1783 + int(x[iq]) // 4}\t{x[inat]}\t{x[it]}\t{x[ipk]}\t{x[iearn]}\t{x[ifood]}')
            prev = key
    # 国の行（nation_s）
    h, ns = rows(d, 'nation', lambda l: f'\t{NAT}\t' in l)
    if h and ns:
        ig = h.index('nominalGdpPerYear')
        z = [int(x[0]) for x in ns if float(x[ig]) == 0]
        runs = []; s0 = p = None
        for q in z:
            if s0 is None: s0 = q
            elif q != p + 1: runs.append((s0, p)); s0 = q
            p = q
        if s0 is not None: runs.append((s0, p))
        nonfin = sum(1 for x in ns for v in x if v.lower() in ('nan', 'inf', '-inf', '∞', '-∞', 'infinity', '-infinity'))
        P(f'Q3b\tnationRows\t{len(ns)}\tzeroGdpRows\t{len(z)}\truns\t{runs}\tnationIndexSet\t{sorted(set(x[2] for x in ns))}\tnonFiniteCells\t{nonfin}')
    else:
        P(f'Q3b\tnationRows\t0')
    # 州のお金（cearn_s）＝持ち主が NAT の四半期
    h, cs = rows(d, 'cearn', lambda l: f'\t{REG}\t' in l)
    if h and cs:
        cols = ['quarter', 'nation', 'tier', 'popK', 'savingsStart', 'savings', 'inMarketPerQuarter', 'outMarketPerQuarter', 'inStatePerQuarter', 'outTaxPerQuarter', 'inExportPerQuarter', 'outImportPerQuarter', 'residualPerQuarter', 'lateRegisteredPops', 'earnedGrossPerMonth', 'emptyPopSavings', 'estateMovedPerQuarter', 'regionPriceLevel', 'stapleGood', 'staplePrice']
        cols = [c for c in cols if c in h]   # 旗 0 の木（keep_845）は c69eh の末尾の列を持たない
        ix = [h.index(c) for c in cols]
        P('Q3c\t' + '\t'.join(cols))
        prev = None
        for x in cs:
            q = int(x[0]); own = x[2]
            if own == NAT or (prev == NAT) or q in (0, 100, 200, 300, 330, 339):
                P('Q3c\t' + '\t'.join(x[i] for i in ix))
            prev = own
        bad = [x[0] for x in cs if any(x[i].lower() in ('nan', 'inf', '-inf', '∞', '-∞') for i in ix[3:13])]
        P(f'Q3c\tnonFiniteRowsInMoneyCols\t{len(bad)}\t{bad[:10]}')
    # POP の内訳（cpop_s）＝持ち主が NAT の最初と最後の四半期と、その直前の四半期
    h, ps = rows(d, 'cpop', lambda l: f'\t{REG}\t' in l)
    if h and ps:
        iq, inat = h.index('quarter'), h.index('nation')
        qs = sorted(set(int(x[iq]) for x in ps if x[inat] == NAT))
        pick = set()
        if qs:
            pick = {qs[0] - 1, qs[0], qs[0] + 4, qs[min(len(qs) - 1, 25)], qs[-1], qs[-1] + 1}
        cols = ['quarter', 'nation', 'className', 'popK', 'savings', 'inMarketPerQuarter', 'outMarketPerQuarter', 'earnedSettlePerQuarter', 'paidSettlePerQuarter', 'subsistenceValuePerQuarter', 'subsistenceRate', 'unemploymentRate', 'foodFill', 'laborLines', 'laborOutput', 'laborRevenuePerMonth', 'topLaborGood', 'topLaborPrice', 'topLaborOutput']
        ix = [h.index(c) for c in cols]
        P('Q3d\t' + '\t'.join(cols))
        for x in ps:
            if int(x[iq]) in pick and float(x[h.index('popK')]) > 0: P('Q3d\t' + '\t'.join(x[i] for i in ix))
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
