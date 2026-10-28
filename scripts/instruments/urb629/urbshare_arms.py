"""(629) 事前登録 [9] ⓖ：英・仏・普の本国（Core）の都市の割合の種の中央値を、基線の幅〔m627・N1・N2 の中央値の最小−2SE〜最大＋2SE〕の下端と比べる。
python urbshare_arms.py 名=ディレクトリ ...（比べる腕。幅は基線 3 本だけから作る）。読む四半期は q268・q388・q520 に固定。"""
import csv, sys, collections, statistics
base = {'m627': 'D:/PoB_work/keep_627/m627_ins', 'n1': 'D:/PoB_work/keep_627/n1', 'n2': 'D:/PoB_work/keep_627/n2'}
test = dict(a.split('=', 1) for a in sys.argv[1:])
nats = ['イギリス', 'フランス', 'プロイセン']
qs = {1850: 268, 1880: 388, 1913: 520}
def load(d, seeds):
    res = collections.defaultdict(dict)
    for s in seeds:
        try: f = open(f'{d}/region_s{s}.tsv', encoding='utf-8')
        except Exception: continue
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        for row in csv.DictReader(f, delimiter='\t'):
            q = int(row['quarter'])
            if q in qs.values() and row['nation'] in nats and row['tier'] == 'Core':
                k = (row['nation'], q); acc[k][0] += float(row['populationK']); acc[k][1] += float(row['ruralPopulationK'])
        for k, (p, r) in acc.items():
            if p > 0: res[k][s] = 1 - r / p
    return res
B = {a: load(d, range(1, 11)) for a, d in base.items()}
T = {a: load(d.split('@')[0], range(*map(int, d.split('@')[1].split('-')))) if '@' in d else load(d, range(1, 11)) for a, d in test.items()}
for n in nats:
    for y, q in qs.items():
        meds = [statistics.median(B[a][(n, q)].values()) for a in B]
        sd = statistics.pstdev(B['m627'][(n, q)].values()); se2 = 2 * sd / 10 ** 0.5
        lo, hi = min(meds) - se2, max(meds) + se2
        out = []
        for a in T:
            v = list(T[a][(n, q)].values())
            if v:
                m = statistics.median(v)
                out.append('%s med %.3f %s' % (a, m, 'OK' if m >= lo else 'BELOW'))
        print(n, y, 'band [%.3f,%.3f]' % (lo, hi), ' | '.join(out))
