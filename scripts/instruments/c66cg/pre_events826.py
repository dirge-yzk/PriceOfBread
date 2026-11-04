"""(829) 腕の前の出来事の選び（群の表は読まない・cearn_s だけ）：m826 の生で、frozenFlag の最長の連続が 40 四半期以上の州×種を全て挙げ、
入り（最長の連続の最初の四半期）の前 8 四半期の主の出口（(822) r822_onset.py と同じ 9 つの出口＝8 列と市場の純の出口）を書く。主の出口が outTaxPerQuarter の行が出来事。
使い方: python -I pre_events826.py <cearn の生のフォルダ> <出力.tsv>"""
import sys, os, glob, re
raw, outp = sys.argv[1], sys.argv[2]
OUTS = ['outTaxPerQuarter','outBondPerQuarter','outDuesPerQuarter','outColonialRentPerQuarter','outCapitalPerQuarter','outImportPerQuarter','outMigrationPerQuarter','outOtherPerQuarter']
def fl(x):
    try: return float(x)
    except: return float('nan')
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tregion\tnation\tlongestRunQ\tonsetQ\tonsetYear\tpreQ\ttopExit\ttopExitAmount\toutTaxPre\tsavStartPre\tsavEndPre\tevent\n')
seeds = sorted(int(re.search(r'cearn_s(\d+)\.tsv$', p).group(1)) for p in glob.glob(os.path.join(raw, 'cearn_s*.tsv')))
for sd in seeds:
    by = {}
    with open(os.path.join(raw, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            a = line.rstrip('\n').split('\t'); by.setdefault(a[3], []).append(dict(zip(h, a)))
    for reg in sorted(by):
        rr = sorted(by[reg], key=lambda d: int(d['quarter']))
        best, bs, cur, cs, prevq = 0, None, 0, None, None
        for d in rr:
            q = int(d['quarter'])
            if d['frozenFlag'] == '1':
                if cur > 0 and prevq == q - 1: cur += 1
                else: cur, cs = 1, q
                if cur > best: best, bs = cur, cs
            else: cur = 0
            prevq = q
        if best < 40: continue
        at = next(d for d in rr if int(d['quarter']) == bs)
        pre = [d for d in rr if bs - 8 <= int(d['quarter']) <= bs - 1]
        S = lambda c: sum(fl(d[c]) for d in pre)
        outs = {c: S(c) for c in OUTS}; outs['marketNet'] = max(0.0, S('outMarketPerQuarter') - S('inMarketPerQuarter'))
        big = max(outs, key=lambda k: outs[k]) if pre else '-'
        o.write('\t'.join(map(str, [sd, reg, at['nation'], best, bs, at['year'], len(pre), big, '%.4g' % outs.get(big, float('nan')), '%.4g' % outs['outTaxPerQuarter'],
                                    pre[0]['savingsStart'] if pre else '-', pre[-1]['savings'] if pre else '-', 1 if big == 'outTaxPerQuarter' and len(pre) == 8 else 0])) + '\n')
o.close(); print(open(outp, encoding='utf-8').read())
