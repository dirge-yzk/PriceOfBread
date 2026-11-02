# r794: 挙げた 9 国の投資家の階級（3 資本家・10 商人・4 技師・9 医者）の貯蓄の書き手を 2 つに分ける：CapitalMarket.cs の行（国際金融）とそれ以外。
# 10 年ごと・国ごと・種の平均・金建て（nation_s の fxRate）。引数：<ins フォルダ> <種の数>
import sys, csv, collections
d = sys.argv[1]; N = int(sys.argv[2])
INV = {'3', '10', '4', '9'}
acc = collections.defaultdict(float)
for s in range(1, N + 1):
    fx = {}
    with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iq = h.index('quarter'); inn = h.index('nation'); ie = h.index('fxRate')
        for row in r: fx[(row[iq], row[inn])] = float(row[ie])
    with open(f'{d}/flow_s{s}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); next(r)
        for row in r:
            if row[3] not in INV: continue
            e = fx.get((row[0], row[2]))
            if e is None: continue
            v = float(row[8]) * 3 * e / N
            dec = int(row[1]) // 10 * 10
            if row[5] == 'CapitalMarket.cs':
                key = {'174': 'div', '172': 'flight', '158': 'domOut', '165': 'domIn', '187': 'back'}.get(row[6], 'cmOther')
            else:
                key = 'otherPos' if v > 0 else 'otherNeg'
            for n in (row[2], '9国計'):
                acc[(n, dec, key)] += v
keys = ['otherPos', 'otherNeg', 'domOut', 'domIn', 'flight', 'div', 'back', 'cmOther']
print('nation\tdec\t' + '\t'.join(keys) + '\t配当の払い÷それ以外の受け取り\t国際金融の総流れ÷それ以外の受け取り')
for n in sorted({k[0] for k in acc}, key=lambda x: (x == '9国計', x)):
    for dec in sorted({k[1] for k in acc}):
        op = acc[(n, dec, 'otherPos')]
        if op == 0 and acc[(n, dec, 'div')] == 0: continue
        gross = sum(abs(acc[(n, dec, k)]) for k in ('domOut', 'domIn', 'flight', 'div', 'back', 'cmOther'))
        print(f'{n}\t{dec}\t' + '\t'.join(f'{acc[(n, dec, k)]:.4g}' for k in keys) + f'\t{(-acc[(n, dec, "div")] / op if op > 0 else float("nan")):.3f}\t{(gross / op if op > 0 else float("nan")):.2f}')
