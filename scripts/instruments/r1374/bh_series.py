# (1374) 読む線：1 州の region_s の時系列（種ごと）を書き出す。使い方 python bh_series.py <ins> <種> <州> <出力>
import sys, collections
d, sd, reg, out = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
cols = ['quarter','year','nation','tier','activeFlag','populationK','ruralPopulationK','employmentShare','wageFactor','pushFactor','housingFactor','holdingFactor','foodFillShare','migrationOutKPerQuarter','migrationInKPerQuarter','crossBorderOutKPerQuarter','crossBorderInKPerQuarter','warIntensity','earnedPerMonth','factoryActiveCount','factoryBankruptCount','soldierK','opportunityIndex']
with open('%s/region_s%d.tsv' % (d, sd), encoding='utf-8') as fh, open(out, 'w', encoding='utf-8', newline='\n') as fo:
    h = fh.readline().rstrip('\n').split('\t'); ix = [h.index(c) for c in cols]; ir = h.index('region')
    fo.write('\t'.join(cols) + '\tdupRow\n')
    last = None
    for line in fh:
        c = line.rstrip('\n').split('\t')
        if c[ir] != reg: continue
        dup = '1' if last == c[0] else '0'; last = c[0]
        fo.write('\t'.join(c[i] for i in ix) + '\t' + dup + '\n')
