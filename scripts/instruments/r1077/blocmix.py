# (1077) c102bk：ブロックの中の階級の混ざり（所得・貯蓄・人口の割合）。class_s だけを読む。読むだけ。
# 使い方: python blocmix.py <base_ins> <出力 tsv>
# 値＝期ごとの世界の和（種 1〜10・class_s の q 行）。ブロック内の割合＝階級の和÷そのブロックの和。
import sys, os, csv, collections
base, out = sys.argv[1], sys.argv[2]
BLOC = {0: 0, 6: 0, 3: 1, 4: 1, 7: 1, 8: 1, 9: 1, 10: 1, 13: 1, 1: 2, 2: 2, 11: 2, 12: 2, 14: 2, 5: 2}
EPOCH = [(1783, 1799), (1820, 1839), (1850, 1869), (1880, 1899), (1910, 1929), (1935, 1950)]
def ep(y):
    for a, b in EPOCH:
        if a <= y <= b: return f"{a}-{b}"
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
A = collections.defaultdict(lambda: collections.defaultdict(float)); names = {}
# 1 人あたりの所得の逆転：同じ国×四半期で、大衆の階級の 1 人あたり所得が近代の階級の 1 人あたり所得を上回る組の割合
inv = collections.defaultdict(lambda: [0, 0])
for s in range(1, 11):
    with open(os.path.join(base, f'class_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        cur = {}; curk = None
        def flush():
            if not cur: return
            e = ep(cur['_y'])
            if e is None: return
            pc = {c: v[0] / v[1] for c, v in cur.items() if c != '_y' and v[1] > 1e-6}
            for m in (1, 2, 11):
                for n in (7, 9, 4, 8):
                    if m in pc and n in pc:
                        inv[(e, m, n)][1] += 1
                        if pc[m] > pc[n]: inv[(e, m, n)][0] += 1
        for row in r:
            t = row[ix['time']]
            if not t.startswith('q'): continue
            k = (t, row[ix['nation']])
            if k != curk: flush(); cur = {'_y': int(row[ix['year']])}; curk = k
            c = int(row[ix['classId']]); names[c] = row[ix['className']]
            e = ep(int(row[ix['year']]))
            if e is None: continue
            earn = max(0.0, fl(row[ix['earnedPerMonth']])); sz = fl(row[ix['sizeK']]); sv = max(0.0, fl(row[ix['savingsStock']]))
            A[e][('E', c)] += earn; A[e][('Z', c)] += sz; A[e][('S', c)] += sv
            cur[c] = (earn, sz)
        flush()
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('epoch\tbloc\tclassId\tclassName\tpopShareInBloc\tincomeShareInBloc\tsavingsShareInBloc\tincomePerCapRelBloc\n')
    for a, b in EPOCH:
        e = f'{a}-{b}'
        for bl in range(3):
            cs = [c for c in sorted(names) if BLOC.get(c, 2) == bl]
            Z = sum(A[e][('Z', c)] for c in cs); E = sum(A[e][('E', c)] for c in cs); S = sum(A[e][('S', c)] for c in cs)
            for c in cs:
                z, x, sv = A[e][('Z', c)], A[e][('E', c)], A[e][('S', c)]
                rel = (x / z) / (E / Z) if z > 0 and Z > 0 and E > 0 else float('nan')
                f.write(f"{e}\t{bl}\t{c}\t{names[c]}\t{z / Z if Z else 0:.3f}\t{x / E if E else 0:.3f}\t{sv / S if S else 0:.3f}\t{rel:.2f}\n")
    f.write('\n# 逆転：同じ国×四半期で 大衆の階級 m の 1 人あたり所得 > 近代の階級 n の割合（組の数）\nepoch\tmass\tmodern\tshare\tn\n')
    for (e, m, n), (k, t) in sorted(inv.items()):
        f.write(f"{e}\t{names.get(m, m)}\t{names.get(n, n)}\t{k / t if t else float('nan'):.3f}\t{t}\n")
print('ok')
