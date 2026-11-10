# (1025) c96rd 記録（門にしない）：N3〈足した国のその財の世界の産出の取り分〉と〈足した財の越境の輸出÷産出〉の新÷旧
#   使い方：python record1025.py <rows.tsv> <出力> 名前=<生> [名前=<生> ...]   （最初の名前が基準・生＝nationgood_s<種>.tsv のあるフォルダ・種 1〜10）
#   N3：年 1850・1870・1913、足した (国, 財) ごとに〈国の producedQty ÷ 全ての国の producedQty〉を種ごとに出し、種の中央値。
#   越境：足した財ごとに〈全ての国の crossExportQty の和 ÷ producedQty の和〉（種を合わせた和÷和）。
import sys, os, glob, statistics, collections

ROWS, OUT = sys.argv[1], sys.argv[2]
arms = [a.split('=', 1) for a in sys.argv[3:]]
YEARS = ['1850', '1870', '1913']
pairs = []
for l in open(ROWS, encoding='utf-8'):
    if l.startswith('#') or l.startswith('nation\t'): continue
    c = l.rstrip('\n').split('\t')
    if c[-1] == '1' and (c[0], c[1]) not in pairs: pairs.append((c[0], c[1]))
goods = sorted({g for _, g in pairs})

def load(d):
    share = collections.defaultdict(list); xs = collections.defaultdict(lambda: [0.0, 0.0])
    for p in sorted(glob.glob(os.path.join(d, 'nationgood_s*.tsv'))):
        tot = collections.defaultdict(float); nat = collections.defaultdict(float)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip('\n').split('\t')
                y, n, g = c[ix['year']], c[ix['nation']], c[ix['good']]
                if y not in YEARS or g not in goods: continue
                q = float(c[ix['producedQty']]); tot[(y, g)] += q; nat[(y, n, g)] += q
                xs[(y, g)][0] += float(c[ix['crossExportQty']]); xs[(y, g)][1] += q
        for y in YEARS:
            for n, g in pairs:
                t = tot.get((y, g), 0.0)
                if t > 0: share[(y, n, g)].append(nat.get((y, n, g), 0.0) / t)
    return {k: statistics.median(v) for k, v in share.items()}, {k: (v[0] / v[1] if v[1] > 0 else float('nan')) for k, v in xs.items()}

res = [(name, load(d)) for name, d in arms]
b = res[0][1]
out = ['# record1025 ' + ' '.join('%s=%s' % (n, d) for n, d in arms),
       '# N3 取り分（種の中央値）・新÷旧は最初の名前を旧とする',
       'kind\tyear\tnation\tgood\t' + '\t'.join(n for n, _ in res) + '\t' + '\t'.join('%s/%s' % (n, res[0][0]) for n, _ in res[1:])]
for y in YEARS:
    for n, g in pairs:
        v = [r[0].get((y, n, g), float('nan')) for _, r in res]
        out.append('N3\t%s\t%s\t%s\t' % (y, n, g) + '\t'.join('%.4f' % x for x in v) + '\t' + '\t'.join(('%.4f' % (x / v[0]) if v[0] > 0 else 'nan') for x in v[1:]))
for y in YEARS:
    for g in goods:
        v = [r[1].get((y, g), float('nan')) for _, r in res]
        out.append('xexp\t%s\t-\t%s\t' % (y, g) + '\t'.join('%.5f' % x for x in v) + '\t' + '\t'.join(('%.4f' % (x / v[0]) if v[0] > 0 else 'nan') for x in v[1:]))
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
