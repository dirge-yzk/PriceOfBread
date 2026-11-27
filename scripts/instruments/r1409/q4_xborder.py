# (1409) 問い 4：越境の移民の合計（region_s の crossBorderOutKPerQuarter の和・千人）を種ごと・時代ごとに。旗 1 と旗 0 の生で同じ台本。
import csv, os, sys, statistics as st
D, SB, N = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
ERAS = [(1783, 1800), (1800, 1830), (1830, 1870), (1870, 1914), (1914, 1951)]
tot = []; era = [[] for _ in ERAS]; first40 = []
for s in range(SB, SB + N):
    p = os.path.join(D, f'region_s{s}.tsv')
    if not os.path.exists(p): continue
    t = 0.0; e = [0.0] * len(ERAS); f40 = 0.0
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iq = h.index('quarter'); iy = h.index('year'); ix = h.index('crossBorderOutKPerQuarter')
        for row in r:
            try: v = float(row[ix])
            except: continue
            if v != v: continue
            t += v; y = int(float(row[iy]))
            if int(row[iq]) < 40: f40 += v
            for k, (a, b) in enumerate(ERAS):
                if a <= y < b: e[k] += v
    tot.append(t); first40.append(f40)
    for k in range(len(ERAS)): era[k].append(e[k])
    print(f's{s}\ttotal={t:.0f}\tfirst40q={f40:.1f}\t' + '\t'.join(f'{x:.0f}' for x in e))
print(f'median\ttotal={st.median(tot):.0f}\tfirst40q={st.median(first40):.1f}\t' + '\t'.join(f'{st.median(x):.0f}' for x in era))
print('# 列：' + ' '.join(f'{a}-{b - 1}' for a, b in ERAS))
