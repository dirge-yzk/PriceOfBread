# r794: flow_s（挙げた 9 国の投資家でない階級も含む全階級）から CapitalMarket.cs の行ごとの額を国×10 年で金建てに直して足す（四半期の額＝月平均×3・金建て＝×nation_s の fxRate）。
# 引数：<ins フォルダ> <種の数>
import sys, csv, collections
d = sys.argv[1]; N = int(sys.argv[2])
LN = {'158': 'domOut', '165': 'domIn', '172': 'flightPaid', '174': 'divPaid', '187': 'backRecv', '223': 'fdiSent', '234': 'fdiRecv', '273': 'bondRecv'}
tot = collections.defaultdict(float)
for s in range(1, N + 1):
    fx = {}
    with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iq = h.index('quarter'); inn = h.index('nation'); ie = h.index('fxRate')
        for row in r: fx[(row[iq], row[inn])] = float(row[ie])
    with open(f'{d}/flow_s{s}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for row in r:
            if row[5] != 'CapitalMarket.cs' or row[6] not in LN: continue
            e = fx.get((row[0], row[2]))
            if e is None: continue
            dec = int(row[1]) // 10 * 10
            v = float(row[8]) * 3 * e / N   # 種の平均・金建て・四半期の額
            tot[(row[2], dec, LN[row[6]])] += v
            tot[('9国計', dec, LN[row[6]])] += v
cols = list(LN.values())
nats = sorted({k[0] for k in tot}, key=lambda x: (x == '9国計', x))
decs = sorted({k[1] for k in tot})
print('nation\tdecade\t' + '\t'.join(cols))
for n in nats:
    for dd in decs:
        print(n + '\t' + str(dd) + '\t' + '\t'.join(f'{tot[(n, dd, c)]:.4g}' for c in cols))
