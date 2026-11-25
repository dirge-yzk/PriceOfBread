# (1374) 読む線：1 国の国の量を年ごと（年末の四半期）に。使い方 python bh_uk.py <ins> <種> <国>
import sys
d, sd, nat = sys.argv[1], int(sys.argv[2]), sys.argv[3]
C = ['year','totalPopulationK','populationK','soldierK','realGdpPerYear','atWarMonths','civilWarMonths','foodMetShare','bondDebtToGdpYearMeanRatio','sovereignDefaultCumulativeCount','polityCode']
YS = {1790,1800,1810,1820,1830,1840,1845,1850,1855,1860,1870,1880,1890,1900,1910,1920,1930,1940,1949}
with open('%s/nation_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = [h.index(c) for c in C]; inat = h.index('nation')
    print('\t'.join(C))
    for line in fh:
        c = line.rstrip('\n').split('\t')
        if c[inat] != nat or int(c[0]) % 4 != 3 or int(c[1]) not in YS: continue
        print('\t'.join(c[i] for i in ix))
