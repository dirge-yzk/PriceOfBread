# (966) c86ef 読むだけ：鎖の順〈外資の流入 → 配当 → 投資家の貯蓄 → 需要 → 価格水準の床〉を、組ごとに F の始まり f0 を 0 とした時刻で並べる。
#   r966_ef.py の <頭>_pairs.tsv の全ての組を読む（ef[0] だけを読まない）。同じ (盤面,種,国,F) の組は F で 1 本にまとめる（F の始まりが時刻の 0）。
#   列：fci（divhost）・invSav（divhost・投資家の貯蓄）・savings（cearn・国の州の和＝全ての POP の貯蓄）・inMarket（cearn・月末の精算の所得の側＝国内の需要の受け取り）・
#       outCapital−inOther（純資本）・outImport−inExport（純輸入）・paid（divhost の払い）・priceLevel・名目 GDP（nation_s）・cyc（pclamp の床の行だけ）。
#   時刻：f0−40・f0−20・f0−8・f0・f0+8・f0+20（無い行は空）。各量の 4 四半期の平均（その時刻から 4 つ）。エンジンの関数を呼ばない。
#   python -X utf8 -I r966_chain.py <pairs.tsv> <出力> <名>=<ins フォルダ> ...
#   1 国の時系列：python -X utf8 -I r966_chain.py --case <ins> <種> <国> <q0> <q1> <刻み> <出力>
import sys, os
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def load(d, s, names):
    S = {}
    def put(n, q, k, v): S.setdefault((n, q), {})[k] = v
    for ix, x in rd(os.path.join(d, f'nation_s{s}.tsv')):
        n = x[ix['nation']]
        if n in names: q = int(x[ix['quarter']]); put(n, q, 'P', fl(x[ix['priceLevel']])); put(n, q, 'gdp', fl(x[ix['nominalGdpPerYear']])); put(n, q, 'popK', fl(x[ix['populationK']]))
    alive = {}; q00 = None
    for ix, x in rd(os.path.join(d, f'nation_s{s}.tsv')):
        q = int(x[ix['quarter']]); q00 = q if q00 is None else min(q00, q); alive.setdefault(x[ix['nation']], []).append((q, fl(x[ix['priceLevel']])))
    SEG = {}
    for nn, xs in alive.items():
        qs = [y[0] for y in xs]; sg = []; st0 = qs[0]
        for i in range(1, len(qs)):
            if qs[i] != qs[i - 1] + 1: sg.append((st0, qs[i - 1])); st0 = qs[i]
        sg.append((st0, qs[-1])); SEG[nn] = sg
    Pq = {(nn, y[0]): y[1] for nn, xs in alive.items() for y in xs}
    def MAPQ(nn, c):   # r966_ef.py と同じ写し方（区間ごとに番号が 0 から・床に当たる区間を選ぶ）
        cand = [((0 if a == q00 else a + 1) + c, a, b) for a, b in SEG[nn]]
        cand = [q for q, a, b in cand if a <= q <= b]
        hit = [q for q in cand if Pq.get((nn, q), 9) <= 0.4 + 1e-6]
        return hit[-1] if hit else (cand[-1] if cand else None)
    calls = {}
    for ix, x in rd(os.path.join(d, f'divhost_s{s}.tsv')):
        n = x[ix['nation']]
        if n not in names: continue
        q = int(x[ix['quarter']]); c = int(x[ix['call']])
        if calls.get((n, q), -1) > c: continue
        calls[(n, q)] = c
        for k, cn in (('fci', 'fci'), ('invSav', 'invSav'), ('req', 'divReqIndex'), ('paid', 'paidIndex'), ('nInv', 'investorCount')): put(n, q, k, fl(x[ix[cn]]))
    pp = os.path.join(d, f'pclamp_s{s}.tsv')
    if os.path.exists(pp):
        for ix, x in rd(pp):
            t = x[ix['time']]
            if t.startswith('q') and x[ix['kind']] == 'floor' and x[ix['nation']] in names:
                nn = x[ix['nation']]; mq = MAPQ(nn, int(t[1:]))   # pclamp の time は国ごとの通し番号
                if mq is not None: put(nn, mq, 'cyc', fl(x[ix['cyclicalRatio']]))
    pc = os.path.join(d, f'cearn_s{s}.tsv')
    if os.path.exists(pc):
        cols = [('sav', 'savings'), ('inMkt', 'inMarketPerQuarter'), ('oCap', 'outCapitalPerQuarter'), ('iOth', 'inOtherPerQuarter'), ('oImp', 'outImportPerQuarter'), ('iExp', 'inExportPerQuarter')]
        for ix, x in rd(pc):
            n = x[ix['nation']]
            if n not in names: continue
            r = S.setdefault((n, int(x[ix['quarter']])), {})
            for k, cn in cols:
                v = fl(x[ix[cn]]); r[k] = r.get(k, 0.0) + (v if v == v else 0.0)
    for r in S.values():
        if 'oCap' in r: r['netCap'] = r['oCap'] - r['iOth']; r['netImp'] = r['oImp'] - r['iExp']
    return S
KEYS = ['fci', 'invSav', 'paid', 'sav', 'inMkt', 'netCap', 'netImp', 'P', 'gdp']
def avg(S, n, q, k, w=4):
    v = [S[(n, t)][k] for t in range(q, q + w) if (n, t) in S and k in S[(n, t)] and S[(n, t)][k] == S[(n, t)][k]]
    return sum(v) / len(v) if v else float('nan')
def g(v): return format(v, '.3g') if v == v else ''
if sys.argv[1] == '--case':
    d, s, n, q0, q1, st, out = sys.argv[2], int(sys.argv[3]), sys.argv[4], int(sys.argv[5]), int(sys.argv[6]), int(sys.argv[7]), sys.argv[8]
    S = load(d, s, {n})
    with open(out, 'w', encoding='utf-8', newline='') as fo:
        ks = ['popK', 'P', 'cyc', 'gdp', 'fci', 'invSav', 'nInv', 'req', 'paid', 'sav', 'inMkt', 'oCap', 'iOth', 'netCap', 'oImp', 'iExp', 'netImp']
        fo.write('\t'.join(['quarter'] + ks) + '\n')
        for q in range(q0, q1 + 1, st):
            r = S.get((n, q), {}); fo.write('\t'.join([str(q)] + [g(r.get(k, float('nan'))) for k in ks]) + '\n')
    sys.exit(0)
pairs, out = sys.argv[1], sys.argv[2]; dirs = dict(a.split('=', 1) for a in sys.argv[3:])
want = {}
with open(pairs, encoding='utf-8') as f:
    f.readline()
    for line in f:
        x = line.rstrip('\n').split('\t')
        if x[0] not in dirs: continue
        f0 = int(x[6].split('〜')[0]); want.setdefault((x[0], int(x[1])), {}).setdefault((x[2], f0), x)
OFF = [-40, -20, -8, 0, 8, 20]
with open(out, 'w', encoding='utf-8', newline='') as fo:
    fo.write('\t'.join(['盤面', '種', '国', '分', 'F', 'E(組の最初)'] + [f'{k}@{o:+d}' for k in KEYS for o in OFF]) + '\n')
    for (nm, s), dd in sorted(want.items()):
        S = load(dirs[nm], s, {n for n, _ in dd})
        for (n, f0), x in sorted(dd.items()):
            fo.write('\t'.join([nm, str(s), n, x[3], x[6], x[5]] + [g(avg(S, n, f0 + o, k)) for k in KEYS for o in OFF]) + '\n')
