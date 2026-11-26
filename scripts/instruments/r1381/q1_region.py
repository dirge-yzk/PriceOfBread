# (1381) 読む線：1 州の region_s の四半期の並び（工場の産出ほか）。使い方 python q1_region.py <ins> <種> <州> <年の始め> <年の終わり> <出力>
import sys
d, sd, reg, y0, y1, out = sys.argv[1], int(sys.argv[2]), sys.argv[3], int(sys.argv[4]), int(sys.argv[5]), sys.argv[6]
cols = ['quarter','year','populationK','employmentShare','foodFillShare','wageFactor','earnedPerMonth','factoryOutputRefValuePerMonth','extractionOutputRefValuePerMonth','factoryActiveCount','factoryBankruptCount','extractionActiveCount','extractionBankruptCount','migrationOutKPerQuarter','migrationInKPerQuarter','warIntensity','advLaborShortage','opportunityIndex','holdingFactor']
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh, open(out, 'w', encoding='utf-8', newline='\n') as fo:
    h = fh.readline().rstrip('\n').split('\t'); ix = [h.index(c) for c in cols]; ir = h.index('region'); iy = h.index('year')
    fo.write('\t'.join(cols) + '\n'); last = None
    for line in fh:
        c = line.rstrip('\n').split('\t')
        if c[ir] != reg or not (y0 <= int(c[iy]) <= y1) or c[0] == last: continue
        last = c[0]
        fo.write('\t'.join(c[i] for i in ix) + '\n')
