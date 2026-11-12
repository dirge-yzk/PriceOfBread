# (1077) c102bk 反証役の指摘への読み直し（読むだけ）。
# (a) 資本家の地代のうち植民地からの送金（landshare_s の colonialRemitPerQuarter）の割合（期ごとの世界の和）
# (b) 1 人あたり所得の逆転を、両方の階級が sizeK>=FLOOR かつ所得>0 の組だけで数え直す（落ちた組の数も）
# (c) 世界の和の 1 人あたり所得を大衆（ブロック 2・兵士を除く）の平均で割った値
# 使い方: python blocfix.py <base_ins> <出力 tsv>
import sys, os, csv, collections
base, out = sys.argv[1], sys.argv[2]
FLOOR = 1.0  # 千人
EPOCH = [(1783, 1799), (1820, 1839), (1850, 1869), (1880, 1899), (1910, 1929), (1935, 1950)]
def ep(y):
    for a, b in EPOCH:
        if a <= y <= b: return f"{a}-{b}"
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
cap = collections.defaultdict(float); rem = collections.defaultdict(float); rentAll = collections.defaultdict(float)
for s in range(1, 11):
    with open(os.path.join(base, f'landshare_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for row in r:
            e = ep(int(row[ix['year']]))
            if e is None: continue
            cap[e] += fl(row[ix['landRentActualCapitalistPerQuarter']]); rem[e] += fl(row[ix['colonialRemitPerQuarter']])
            rentAll[e] += sum(fl(row[ix[f'landRentActual{k}PerQuarter']]) for k in ('Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other'))
MASS = {1, 2, 11, 12, 14}
inv = collections.defaultdict(lambda: [0, 0, 0]); W = collections.defaultdict(lambda: collections.defaultdict(float)); names = {}
for s in range(1, 11):
    with open(os.path.join(base, f'class_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        grp = collections.defaultdict(dict)
        for row in r:
            t = row[ix['time']]
            if not t.startswith('q'): continue
            e = ep(int(row[ix['year']]))
            if e is None: continue
            c = int(row[ix['classId']]); names[c] = row[ix['className']]
            z = fl(row[ix['sizeK']]); x = max(0.0, fl(row[ix['earnedPerMonth']]))
            grp[(e, t, row[ix['nation']])][c] = (x, z)
            W[e][('E', c)] += x; W[e][('Z', c)] += z
        for (e, t, n), d in grp.items():
            for m in (1, 2, 11):
                for k in (4, 7, 8, 9):
                    if m not in d or k not in d: continue
                    key = (e, m, k); inv[key][2] += 1
                    (xm, zm), (xk, zk) = d[m], d[k]
                    if zm < FLOOR or zk < FLOOR or xm <= 0 or xk <= 0: continue
                    inv[key][1] += 1
                    if xm / zm > xk / zk: inv[key][0] += 1
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('# (a) 資本家の地代と植民地の送金（期ごとの世界の和）\nepoch\tcapitalistRent\tcolonialRemit\tremitOverCapRent\tcapRentOverAllRent\n')
    for a, b in EPOCH:
        e = f'{a}-{b}'
        f.write(f"{e}\t{cap[e]:.4g}\t{rem[e]:.4g}\t{rem[e] / cap[e] if cap[e] else float('nan'):.3f}\t{cap[e] / rentAll[e] if rentAll[e] else float('nan'):.3f}\n")
    f.write(f'\n# (b) 逆転（両方 sizeK>={FLOOR}・所得>0 の組だけ）\nepoch\tmass\tmodern\tshare\tkept\tall\n')
    for (e, m, k), (a1, kept, al) in sorted(inv.items()):
        f.write(f"{e}\t{names[m]}\t{names[k]}\t{a1 / kept if kept else float('nan'):.3f}\t{kept}\t{al}\n")
    f.write('\n# (c) 世界の和の 1 人あたり所得÷大衆（兵士を除く）の 1 人あたり所得\nepoch\t' + '\t'.join(names[c] for c in sorted(names)) + '\n')
    for a, b in EPOCH:
        e = f'{a}-{b}'
        mz = sum(W[e][('Z', c)] for c in MASS); mx = sum(W[e][('E', c)] for c in MASS)
        mp = mx / mz if mz else float('nan')
        f.write(e + '\t' + '\t'.join(f"{(W[e][('E', c)] / W[e][('Z', c)]) / mp:.2f}" if W[e][('Z', c)] > 0 else 'nan' for c in sorted(names)) + '\n')
print('ok')
