# (1210) ① 国×年の 11 の思想の国民の取り分（種の中央）と意識×声の平均（1783 を 1 とした比）。用法: natmix.py <生> <種の始め> <種の終わり> <出力> <国,国,...>
import sys, csv, statistics as st
raw, s0, s1, out, nats = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5].split(',')
YEARS = [1783, 1800, 1820, 1840, 1848, 1860, 1871, 1900, 1913, 1950]
IDEO = ['Traditionalism', 'Reactionism', 'Liberalism', 'Republicanism', 'Nationalism', 'Socialism', 'Communism', 'Anarchism', 'Imperialism', 'Fascism', 'Royalism']
acc = {}
for s in range(s0, s1 + 1):
    with open(f'{raw}/ideology_s{s}.tsv', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        last = {}
        for x in r:
            n = x[ix['nation']]
            if n not in nats: continue
            y = int(x[ix['year']])
            if y not in YEARS: continue
            last[(n, y)] = x   # 年の最後の行
        for (n, y), x in last.items():
            pk = float(x[ix['popK']]); ws = float(x[ix['nationalWeightSumIndex']])
            v = [float(x[ix['ideoNational' + k + 'Share']]) for k in IDEO] + [ws / pk if pk > 0 else float('nan')]
            acc.setdefault((n, y), []).append(v)
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# raw={raw} seeds={s0}..{s1}・年の最後の行・種の中央・cvRel＝（重みの和÷人口）を同じ国の 1783 年で割った比\n')
    o.write('nation\tyear\tn\t' + '\t'.join(k[:5] for k in IDEO) + '\tcvRel\n')
    for n in nats:
        base = None
        for y in YEARS:
            vs = acc.get((n, y))
            if not vs: continue
            m = [st.median([v[i] for v in vs]) for i in range(len(IDEO) + 1)]
            if base is None: base = m[-1]
            o.write(f'{n}\t{y}\t{len(vs)}\t' + '\t'.join(f'{z:.3f}' for z in m[:-1]) + f'\t{m[-1] / base:.2f}\n')
print('ok', out)
