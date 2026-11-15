# (1148) 観戦 U5 の形：英の植民地格の州の数・総人口と本国の人口を年ごとに（読むだけ）
import sys, os
D = sys.argv[1]; NAT = sys.argv[2] if len(sys.argv) > 2 else 'イギリス'
sys.stdout.reconfigure(newline='\n')
YRS = (1783, 1820, 1850, 1880, 1900, 1925, 1949)
print('seed\t' + '\t'.join(f'{y}:col/totK/homeK' for y in YRS))
for s in range(1, 21):
    p = os.path.join(D, f'nation_s{s}.tsv')
    if not os.path.exists(p): continue
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
    iy, inat, icr, it, ip = h.index('year'), h.index('nation'), h.index('colonialRegions'), h.index('totalPopulationK'), h.index('populationK')
    got = {}
    for l in f:
        x = l.rstrip('\n').split('\t')
        if x[inat] != NAT: continue
        y = int(float(x[iy]))
        if y in YRS and y not in got: got[y] = '%d/%.0f/%.0f' % (float(x[icr]), float(x[it]), float(x[ip]))
    print(str(s) + '\t' + '\t'.join(got.get(y, '-') for y in YRS))
