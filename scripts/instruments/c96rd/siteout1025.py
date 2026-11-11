# (1025) c96rd 記録（門にしない・結果を見た後に足した読み＝名乗る）：足した (州, 財) の RGO の産出と能力と持ち主の国の新÷旧
#   使い方：python siteout1025.py <rows.tsv> <基準の生> <腕の生> <出力>
#   prodcap_s の年 1800・1850・1870・1913 の q4（無ければその年の最後の q）・isRgo=1・(region, good) ごとに output と capacity の和（種を合わせた和）・
#   持ち主が開幕と同じ種の数も数える（所有の変わった州は Output と producedQty が合わない＝c95pc の反証役）。
import sys, os, glob, collections

ROWS, BASE, ARM, OUT = sys.argv[1:5]
YEARS = {'1800', '1850', '1870', '1913'}
added = {}
for l in open(ROWS, encoding='utf-8'):
    if l.startswith('#') or l.startswith('nation\t'): continue
    c = l.rstrip('\n').split('\t')
    if c[-1] == '1': added[(c[2], c[1])] = c[0]

def load(d):
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0, 0])   # out, cap, seeds held, seeds
    for p in sorted(glob.glob(os.path.join(d, 'prodcap_s*.tsv'))):
        last = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip('\n').split('\t')
                y = c[ix['year']]
                if y not in YEARS or c[ix['isRgo']] != '1': continue
                k = (c[ix['region']], c[ix['good']])
                if k not in added: continue
                q = int(c[ix['q']])
                cur = last.get((y, k))
                if cur is None or q > cur[0]: last[(y, k)] = [q, 0.0, 0.0, c[ix['ownerNation']]]
                if last[(y, k)][0] == q:
                    last[(y, k)][1] += float(c[ix['output']]); last[(y, k)][2] += float(c[ix['capacity']]); last[(y, k)][3] = c[ix['ownerNation']]
        for (y, k), v in last.items():
            a = acc[(y, k)]; a[0] += v[1]; a[1] += v[2]; a[3] += 1
            if v[3] == added[k]: a[2] += 1
    return acc

b, a = load(BASE), load(ARM)
out = ['# siteout1025 base=%s arm=%s（結果を見た後に足した記録の読み）' % (BASE, ARM),
       'year\tprovince\tgood\tbaseOut\tarmOut\tout新÷旧\tbaseCap\tarmCap\tcap新÷旧\tbaseHeld\tarmHeld']
for y in sorted(YEARS):
    for k in sorted(added):
        x, z = b.get((y, k), [0, 0, 0, 0]), a.get((y, k), [0, 0, 0, 0])
        r1 = z[0] / x[0] if x[0] > 0 else float('nan'); r2 = z[1] / x[1] if x[1] > 0 else float('nan')
        out.append('%s\t%s\t%s\t%.4g\t%.4g\t%.3f\t%.4g\t%.4g\t%.3f\t%d/%d\t%d/%d' % (y, k[0], k[1], x[0], z[0], r1, x[1], z[1], r2, x[2], x[3], z[2], z[3]))
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
