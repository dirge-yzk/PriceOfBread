"""(822) cearn_s の閉じの自己検査（読むだけ）：全ての州×四半期で |残差|÷(Σ流入＋Σ流出＋|始まり|＋|終わり|) の分布と、登録の遅れのある行の数・凍結の行の数。
使い方: python -I r822_closure.py <cearn の生のディレクトリ> <出力.tsv>"""
import sys, os, math
def fl(x):
    try: return float(x)
    except: return float('nan')
raw, outp = sys.argv[1], sys.argv[2]
INS = ['inMarketPerQuarter','inExportPerQuarter','inStatePerQuarter','inCreditPerQuarter','inFromPopsPerQuarter','inMigrationPerQuarter','inOtherPerQuarter']
OUTS = ['outMarketPerQuarter','outTaxPerQuarter','outBondPerQuarter','outDuesPerQuarter','outColonialRentPerQuarter','outCapitalPerQuarter','outImportPerQuarter','outMigrationPerQuarter','outOtherPerQuarter']
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\trows\tnanStart\tlateRows\tfrozenRows\tfrozenColonialRows\trelResidMax\trelResidP999\trowsOver1e-8\trowsOver1e-6\tworstRegion\tworstQuarter\n')
for sd in range(1, 21):
    p = os.path.join(raw, 'cearn_s%d.tsv' % sd)
    if not os.path.exists(p): continue
    n = nan = late = frz = frzc = over8 = over6 = 0; rel = []; worst = (0, '', '')
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t'); n += 1
            s0, s1, res = fl(a[ix['savingsStart']]), fl(a[ix['savings']]), fl(a[ix['residualPerQuarter']])
            if not math.isfinite(s0): nan += 1; continue
            if int(a[ix['lateRegisteredPops']]) > 0: late += 1
            if a[ix['frozenFlag']] == '1':
                frz += 1
                if a[ix['tier']] == 'Colonial': frzc += 1
            sc = sum(abs(fl(a[ix[c]])) for c in INS + OUTS) + abs(s0) + abs(s1)
            r = abs(res) / sc if sc > 0 else 0.0
            rel.append(r)
            if r > 1e-8: over8 += 1
            if r > 1e-6: over6 += 1
            if r > worst[0]: worst = (r, a[ix['region']], a[ix['quarter']])
    rel.sort()
    p999 = rel[int(0.999 * (len(rel) - 1))] if rel else float('nan')
    o.write('\t'.join(map(str, [sd, n, nan, late, frz, frzc, '%.3g' % (rel[-1] if rel else float('nan')), '%.3g' % p999, over8, over6, worst[1], worst[2]])) + '\n')
o.close()
print(open(outp, encoding='utf-8').read())
