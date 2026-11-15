# 診断（門ではない）：短い走りの CTL の上側の赤を、ヌル腕の同じ窓（種 1〜2・四半期 0〜79）と並べる。表を読むだけ。
import sys, os, collections, statistics as st
sys.path.insert(0, 'D:/PoB_work/wt_c113cr/scripts/instruments/c113cr')
import onset_cr
fl = onset_cr.fl
K = 'D:/PoB_work/keep_1150'; CTL = 'D:/PoB_work/c113cr_work/mut/CTL/ins'
def onset_window(folder, seeds, qmax):
    out = []; pairs = collections.Counter()
    for s in seeds:
        p = os.path.join(folder, f'rponset_s{s}.tsv')
        byq = collections.Counter(); granted = set(); adjonset = set()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            for line in f:
                c = line.rstrip('\n').split('\t'); q = int(c[ix['quarter']])
                if q > qmax: break
                if c[ix['rowKind']] != 'pair': continue
                key = (c[ix['region']], c[ix['foreignKey']])
                if c[ix['classHead']] == '4': granted.add(key)
                if c[ix['classHead']] == '3' and fl(c[ix['sHead']]) == 0: adjonset.add(key)
                if c[ix['tailFlag']] == '1' and c[ix['dataStateFlag']] == '0':
                    byq[q] += fl(c[ix['tailMass']])
                    src = 'grant' if key in granted else ('adj' if key in adjonset else 'other')
                    pairs[(src, c[ix['nation']], c[ix['tier']])] += fl(c[ix['tailMass']])
        out.append(sum(byq.values()) / (qmax + 1))
    return out, pairs
for lb, fo in (('M', K + '/base_ins'), ('N1', K + '/n1'), ('N2', K + '/n2'), ('CTL', CTL)):
    v, pairs = onset_window(fo, (1, 2), 79)
    tot = sum(pairs.values())
    top = sorted(pairs.items(), key=lambda x: -x[1])[:6]
    print(lb, 'C15 窓 0-79 種 1-2', [round(x, 6) for x in v], '中央', round(st.median(v), 6), '出どころ', {k: round(sum(x for kk, x in pairs.items() if kk[0] == k) / max(tot, 1e-12), 3) for k in ('grant', 'adj', 'other')}, '上位', [(k, round(x / 80, 6)) for k, x in top])
# C5〈壊さない〉・C9′ の型を、ヌル腕 N1 の種 1〜2 を〈腕〉と見て数える（同じ c5_series の帯・同じ定義＝影の O）
ser = collections.defaultdict(dict)
with open('D:/PoB_work/wt_c113cr/measurements/m1151_20261115/paper/c5_series.tsv', encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t')
    for line in f:
        c = line.rstrip('\n').split('\t'); ser[int(c[0])] = {h[j]: fl(c[j]) for j in range(1, len(h))}
def series(folder, seeds, col):
    natq = collections.defaultdict(lambda: collections.defaultdict(dict))
    for s in seeds:
        with open(os.path.join(folder, f'rulepower_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
            ic = ix[col] if col in ix else ix['oNationIndex']
            for line in f:
                c = line.rstrip('\n').split('\t'); q = int(c[ix['quarter']])
                if q > 79: break
                n = c[ix['nation']]
                if n not in natq[q][s]: natq[q][s][n] = (fl(c[ix['lNationIndex']]), fl(c[ic]))
    res = {}
    for q, bys in natq.items():
        a, b = [], []
        for s, nat in bys.items():
            L = sum(v[0] for v in nat.values()); Os = [v[1] for v in nat.values()]; so = sum(Os)
            if L > 0: a.append(so / L); b.append(max(Os) / so if so > 0 else 0.0)
        res[q] = (st.median(a), st.median(b))
    return res
pairs = [('N1', 'N2'), ('M', 'N2'), ('M', 'N1')]
for lb, fo, col in (('N1 種1-2（影の O）', K + '/n1', 'oNationIndex'), ('M 種1-2（影の O）', K + '/base_ins', 'oNationIndex'), ('CTL（エンジンの O）', CTL, 'oEngNationIndex'), ('CTL（影の O）', CTL, 'oNationIndex')):
    r = series(fo, (1, 2), col)
    for i, k in enumerate(('sumOsumL', 'maxOshare')):
        per = []
        for pr in pairs:
            n = 0
            for q, v in r.items():
                lo = min(ser[q][f'{b}_{k}_min'] for b in pr); hi = max(ser[q][f'{b}_{k}_max'] for b in pr)
                n += not (lo <= v[i] <= hi)
            per.append(n)
        print(lb, k, '幅の外の四半期', per)
