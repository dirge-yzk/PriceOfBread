import csv, glob, os, statistics as st, sys
# (649) urxpd_s の読み手（読むだけ・事前登録 [4]④）。使い方：
#   python readpd.py <腕0の計器フォルダ> [<腕1の計器フォルダ>|-] [Qまで=388] [国,国,...]
# 四半期を 40 本（10 年）ずつの区間に束ね、種の中央値で出す。pair の割合と州全体の割合のずれ（B1）・天井の切れ・門・駆動の向き。
NATS = ['イギリス', 'プロイセン', 'フランス', 'ネーデルラント']

def load(d):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'urxpd_s*.tsv'))):
        seed = int(os.path.basename(f)[7:-4])
        rows = {}
        with open(f, encoding='utf-8') as fh:
            r = csv.DictReader(fh, delimiter='\t')
            for row in r: rows[(row['nation'], int(row['quarter']))] = {k: float(v) for k, v in row.items() if k not in ('nation',)}
        out[seed] = rows
    return out

def stats(rows, nat, lo, hi):
    acc = {}
    for q in range(lo, hi):
        r = rows.get((nat, q))
        if not r: continue
        for k, v in r.items():
            if k in ('quarter', 'year'): continue
            acc[k] = acc.get(k, 0.0) + v
    if not acc or acc.get('regionMonths', 0) <= 0 or acc.get('sumTotal', 0) <= 0: return None
    T = acc['sumTotal']
    return {
        'pairUrbanOfRegionUrban': acc['sumPairUrban'] / acc['sumRegionUrban'] if acc['sumRegionUrban'] > 0 else float('nan'),
        'pairCur': acc['wCur'] / T,
        'regionUrbanShare': acc['sumRegionUrban'] / acc['sumRegionPop'] if acc['sumRegionPop'] > 0 else float('nan'),
        'pairShareOfRegionPop': T / acc['sumRegionPop'] if acc['sumRegionPop'] > 0 else float('nan'),
        'target': acc['wTargetPre'] / T, 'targetCapped': acc['wTargetCapped'] / T, 'cap': acc['wCap'] / T,
        'incGap': (acc['wIncU'] - acc['wIncR']) / T,
        'capBoundRate': acc['capBound'] / acc['regionMonths'], 'serfRate': acc['serfGated'] / acc['regionMonths'], 'famineRate': acc['famineGated'] / acc['regionMonths'],
        'noPairRate': acc['noPair'] / (acc['noPair'] + acc['regionMonths']),
        'netFlowPerMonth': (acc['sumFlowPos'] - acc['sumFlowNeg']) / max(1.0, (hi - lo) * 3),
    }

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

def main():
    d0 = sys.argv[1]; d1 = sys.argv[2] if len(sys.argv) > 2 and sys.argv[2] != '-' else None
    Q = int(sys.argv[3]) if len(sys.argv) > 3 else 388
    nats = sys.argv[4].split(',') if len(sys.argv) > 4 else NATS
    A0 = load(d0); A1 = load(d1) if d1 else None
    seeds = sorted(set(A0) & set(A1)) if A1 else sorted(A0)
    keys = ['pairUrbanOfRegionUrban', 'pairCur', 'regionUrbanShare', 'pairShareOfRegionPop', 'target', 'targetCapped', 'cap', 'incGap', 'capBoundRate', 'serfRate', 'famineRate', 'noPairRate', 'netFlowPerMonth']
    print(f'# urxpd 腕0={d0} 腕1={d1} 種={seeds}（10 年ごと・種の中央値）')
    for nat in nats:
        print(f'\n== {nat}')
        print('  区間（四半期）  ' + '  '.join(f'{k[:10]:>10}' for k in keys))
        for lo in range(0, Q + 1, 40):
            hi = min(lo + 40, Q + 1)
            def row(A):
                ss = [stats(A[s], nat, lo, hi) for s in seeds]
                return {k: med([x[k] for x in ss if x]) for k in keys}
            r0 = row(A0)
            print(f'  [{lo:3d},{hi:3d}) K0 ' + '  '.join(f'{r0[k]:10.4f}' for k in keys))
            if A1:
                r1 = row(A1)
                print(f'  [{lo:3d},{hi:3d}) K1 ' + '  '.join(f'{r1[k]:10.4f}' for k in keys))
                print(f'  [{lo:3d},{hi:3d}) 差 ' + '  '.join(f'{r1[k]-r0[k]:+10.4f}' for k in keys))

if __name__ == '__main__': main()
