"""(822) 記録の表（門ではない・形の判定 r822_read.py を変えない）：凍結の入りの窓（最初に frozenFlag＝1 になった四半期を終わりとする 8 四半期）の流出の内訳。
事前登録の窓 W（10 年前〜凍結の終わり）は入りの直前の崖を薄める＝次の波の狙いの数字の材料として入りの窓を別に書く（事前登録の後に足した記録・2026-11-04）。
使い方: python -I r822_onset.py <cearn の生> <zero_all.tsv> <出力.tsv>"""
import sys, os, math, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, zero_all, outp = sys.argv[1], sys.argv[2], sys.argv[3]
OUTS = ['outTaxPerQuarter','outBondPerQuarter','outDuesPerQuarter','outColonialRentPerQuarter','outCapitalPerQuarter','outImportPerQuarter','outMigrationPerQuarter','outOtherPerQuarter']
targets = []
with open(zero_all, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in f:
        a = line.rstrip('\n').split('\t')
        if a[ix['set']] == 'base' and int(a[ix['longestRunQ']]) >= 40:
            targets.append((int(a[ix['seed']]), a[ix['region']], int(a[ix['runStartYear']])))
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tregion\tonsetQ\tonsetYear\towner\tsavStart\tsavEnd\tmarketNet\t' + '\t'.join(OUTS) + '\tinState\tstateMarketSpend\tstateMarketTake\tpollClaim\tearnedGross\tlargest\ttopRoutes\n')
for sd in sorted({t[0] for t in targets}):
    by = collections.defaultdict(list)
    with open(os.path.join(raw, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            d = dict(zip(h, line.rstrip('\n').split('\t')))
            by[d['region']].append(d)
    for (s, reg, y0) in [t for t in targets if t[0] == sd]:
        rr = sorted(by.get(reg, []), key=lambda d: int(d['quarter']))
        # 入り＝runStartYear の前後で最初に frozenFlag が 1 になった四半期（runStartYear−2 年より後）
        on = next((i for i, d in enumerate(rr) if d['frozenFlag'] == '1' and int(d['year']) >= y0 - 2), None)
        if on is None: o.write('%d\t%s\t-\n' % (sd, reg)); continue
        W = rr[max(0, on - 7): on + 1]
        S = lambda c: sum(fl(d[c]) for d in W)
        mk = max(0.0, S('outMarketPerQuarter') - S('inMarketPerQuarter'))
        outs = {c: S(c) for c in OUTS}; outs['marketNet'] = mk
        big = max(outs, key=lambda k: outs[k])
        rt = collections.Counter()
        for d in W:
            for k, a in (('outTop1Route', 'outTop1PerQuarter'), ('outTop2Route', 'outTop2PerQuarter')):
                if d[k] != '-': rt[d[k]] += fl(d[a])
        tops = ';'.join('%s=%.3g' % (k, v) for k, v in rt.most_common(3))
        o.write('\t'.join([str(sd), reg, W[-1]['quarter'], W[-1]['year'], W[-1]['nation'], W[0]['savingsStart'], W[-1]['savings'], '%.4g' % mk]
                          + ['%.4g' % outs[c] for c in OUTS] + ['%.4g' % S('inStatePerQuarter'), '%.4g' % S('stateMarketSpendPerQuarter'), '%.4g' % S('stateMarketTakePerQuarter'),
                          '%.4g' % (3 * S('pollClaimPerMonth')), '%.4g' % (3 * S('earnedGrossPerMonth')), big, tops]) + '\n')
o.close()
print(open(outp, encoding='utf-8').read())
