# (1042) 問2：EthnicDraw の項 dstA=(1+0.7*(1-EU))*clamp(lit/0.45) の近似（計器の 9 国・B 10 種の種の中央）
# EU≈secession_s の coreShareMeanShare を popK で重みを付けた平均（首都を除く＝近似・首都は中核が高いはず＝EU は低めに出る＝dstA は高めに出る）
# lit≈lawgate_s literacyHome（兵士を含む・本国だけ＝エンジンの AvgLiteracy は植民地を含む＝植民地を持つ国は近似が高めに出る）
import statistics, collections
D = 'D:/PoB_work/keep_1035/base_ins'
YEARS = [1785, 1820, 1850, 1880, 1910, 1940]
eu = collections.defaultdict(list); lit = collections.defaultdict(list); da = collections.defaultdict(list)
for s in range(1, 11):
    num = collections.defaultdict(float); den = collections.defaultdict(float)
    with open(f'{D}/secession_s{s}.tsv', 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t'); y = int(x[ix['year']]); q = int(x[0])
            if y not in YEARS or q % 4 != 3: continue
            pk = float(x[ix['popK']]); c = float(x[ix['coreShareMeanShare']])
            if pk > 0 and c == c: num[(x[ix['nation']], y)] += pk * c; den[(x[ix['nation']], y)] += pk
    L = {}
    with open(f'{D}/lawgate_s{s}.tsv', 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t'); y = int(x[ix['year']]); q = int(x[0])
            if y in YEARS and q % 4 == 3: L[(x[ix['nation']], y)] = float(x[ix['literacyHome']])
    for k in den:
        e = num[k] / den[k]; eu[k].append(e)
        if k in L:
            l = L[k]; lit[k].append(l); da[k].append((1 + 0.7 * (1 - e)) * min(1.0, max(0.0, l / 0.45)))
nats = sorted({k[0] for k in eu})
print('国\t' + '\t'.join(str(y) for y in YEARS) + '\t（各欄＝EU/識字/dstA の種の中央）')
for n in nats:
    cells = []
    for y in YEARS:
        k = (n, y)
        if eu.get(k) and lit.get(k): cells.append(f'{statistics.median(eu[k]):.2f}/{statistics.median(lit[k]):.2f}/{statistics.median(da[k]):.2f}')
        else: cells.append('-')
    print(n + '\t' + '\t'.join(cells))
