# dsc6 後半：事前登録の追記 3（measurements/dsc6_20261027/prereg_dsc6.txt）の判定の読み手。gates_dsc6.py（前半・canary の読み手）の analyse をそのまま使い、T1★・T5 の定義だけ登録のとおり替えた。
# 使い方：PYTHONUTF8=1 python gates_dsc6b.py <ins ディレクトリ> <種の起点> <種の数>      （D:/… の形で渡す）
#         PYTHONUTF8=1 python gates_dsc6b.py --open-only <ins ディレクトリ> <種の起点> <種の数>   （disc_s だけ＝基線の生〔dscx なし〕でも読める。開いた行の数と〈開いた行×年〉だけ）
# ⚠限界（自己申告）：国は計器の NATIONS の 9 国／〈研究で開いた〉は開いた四半期の E 行の項（技術＋必要性 と 伝わり）の大小で分ける＝積み上げの全期間の寄与ではない／
#   開幕の既知は dscx の R 行の known（最初の標本）／制度と思想の行は判定に入れない（今の門のまま）。
import sys, os, csv, collections, random, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gates_dsc6 as g6
from gates_dsc6 import MAJOR, SPREAD
DRAWS = 2000
BLOCKS = (12, 20, 40)          # 記録の帰無（国の中で、ブロック内の件数を保って引き直す）の幅（四半期）
PRIMARY_BLOCK = 20


def mwin(xs):
    xs = sorted(xs); j = 0; b = 0
    for i in range(len(xs)):
        while xs[i] - xs[j] > 3: j += 1
        b = max(b, i - j + 1)
    return b


def events_terms(path):
    """disc_s の E 行（毎四半期の出来事）＝(国, 行) -> (四半期, 技術の項, 必要性の項, 伝わりの項)。最初の出来事だけ。"""
    ev = {}
    with open(path, encoding='utf-8') as fh:
        rd = csv.reader(fh, delimiter='\t'); next(rd)
        for r in rd:
            if r[0] != 'E': continue
            k = (r[3], r[4])
            if k in ev: continue
            f = lambda x: float(x) if x not in ('nan', '') else 0.0
            ev[k] = (int(r[1]), f(r[5]), f(r[6]), f(r[7]))
    return ev


def cells_rowswap(op, rng):
    """行ごとに、開いた国どうしで四半期を入れ替える帰無（世界の行ごとの開く時期を保ち、国ごとの固まりだけを見る）。op: {(国,行): 四半期}"""
    rows = collections.defaultdict(list)
    for (n, r), q in op.items(): rows[r].append((n, q))
    obs = {n: [q for (m, r), q in op.items() if m == n] for n in MAJOR}
    use = [n for n in MAJOR if len(obs[n]) >= 5]
    vs = {n: [] for n in use}
    for _ in range(DRAWS):
        ns = {n: [] for n in use}
        for r, l in rows.items():
            nat = [m for m, _ in l]; qs = [q for _, q in l]; rng.shuffle(qs)
            for m, q in zip(nat, qs):
                if m in ns: ns[m].append(q)
        for n in use: vs[n].append(mwin(ns[n]))
    out = []
    for n in use:
        v = sorted(vs[n]); out.append((n, mwin(obs[n]), sum(v) / len(v), v[int(0.95 * len(v)) - 1], len(obs[n])))
    return out


def cells_block(op, rng, block):
    out = []
    for n in MAJOR:
        qq = [q for (m, r), q in op.items() if m == n]
        if len(qq) < 5: continue
        cnt = collections.Counter(q // block for q in qq); v = []
        for _ in range(DRAWS):
            xs = []
            for b, c in cnt.items(): xs += [rng.randint(b * block, b * block + block - 1) for _ in range(c)]
            v.append(mwin(xs))
        v.sort(); out.append((n, mwin(qq), sum(v) / len(v), v[int(0.95 * len(v)) - 1], len(qq)))
    return out


def cells_uniform(op, rng):
    out = []
    for n in MAJOR:
        qq = [q for (m, r), q in op.items() if m == n]
        if len(qq) < 5: continue
        lo, hi = min(qq), max(qq); v = []
        for _ in range(DRAWS): v.append(mwin([rng.randint(lo, hi) for _ in qq]))
        v.sort(); out.append((n, mwin(qq), sum(v) / len(v), v[int(0.95 * len(v)) - 1], len(qq)))
    return out


def summ(cells):
    if not cells: return None
    return (len(cells), st.mean(m for _, m, _, _, _ in cells), st.mean(mu for _, _, mu, _, _ in cells),
            st.mean(m - mu for _, m, mu, _, _ in cells), sum(1 for _, m, _, p, _ in cells if m > p) / len(cells))


def analyse_seed(ins, seed):
    a = g6.analyse(ins, seed)
    ev = events_terms(os.path.join(ins, 'disc_s%d.tsv' % seed))
    start = a['start_rows']
    # 判定に入れる出来事：主要 9 国・制度と思想を除く・開幕の既知の行を除く（開幕の既知の国×行は analyse が除外済み）
    op = {k: q for k, q in a['ev'].items() if k[1] not in SPREAD}
    research = {}; transm = {}
    for k, q in op.items():
        t = ev.get(k)
        if t is None: research[k] = q; continue
        (research if (t[1] + t[2]) >= t[3] else transm)[k] = q
    rng = random.Random(20261027 * 1000 + seed)
    res = dict(seed=seed, n_all=len(op), n_research=len(research), n_transm=len(transm))
    res['T1star'] = cells_rowswap(research, rng)                    # 判定：研究で開いた出来事・行ごとの国間入れ替え
    res['rec_rowswap_all'] = cells_rowswap(op, rng)                  # 記録：伝わりで開いた出来事も含む
    res['rec_uniform_all'] = cells_uniform(op, rng)                  # 記録：前半の帰無（一様）
    for b in BLOCKS: res['rec_block%d' % b] = cells_block(op, rng, b)
    # T5（登録 追記 3）：制度と思想を除く・ランダムの期待値と並べる
    firsts = {}
    for k, q in op.items():
        r = k[1]
        if r in start: continue
        if r not in firsts or q < firsts[r][0]: firsts[r] = (q, {k[0]})
        elif q == firsts[r][0]: firsts[r][1].add(k[0])
    notmax = 0; exp = 0.0; tot = 0; cn = []
    for r, (q, ns) in firsts.items():
        qe = q if q % 2 == 0 else q - 1
        eng = {m: a['N'].get((qe, m), {}).get('engAll', 0) for m in MAJOR}
        best = max(eng, key=lambda m: eng[m])
        tot += 1; notmax += (best not in ns); exp += 1 - len(ns) / len(MAJOR)
        if '清' in ns: cn.append(r)
    res['T5'] = (notmax, tot, exp)
    res['cn_first'] = cn
    # 全ての国の開いた行の数（計算量の帰属）：disc_s の E 行全部（開幕の既知を除く）
    allev = {k: v for k, v in ev.items() if v[0] > 0 and k[1] not in SPREAD}
    nat_rows = collections.Counter(n for (n, r) in allev)
    res['open_rows_per_nation_median'] = st.median(nat_rows.values()) if nat_rows else 0
    res['row_quarters_open'] = sum(668 - v[0] for v in allev.values())   # 〈開いた行×四半期〉＝開いてから終わりまでの四半期の和
    # 前半の gates（T2・T3・T8 ほか）
    gg = g6.gates(a)
    for k in ('T2', 'steam1850', 'steam1900', 'loco1850', 'T8', 'Nshare', 'Nspread', 'rho_s', 'YG', 'key'): res[k] = gg[k]
    res['T4'] = gg['T4']
    return res


def open_only(ins, s0, ns):
    for s in range(s0, s0 + ns):
        p = os.path.join(ins, 'disc_s%d.tsv' % s)
        if not os.path.exists(p): print('seed', s, 'なし'); continue
        ev = events_terms(p)
        allev = {k: v for k, v in ev.items() if v[0] > 0 and k[1] not in SPREAD}
        nat_rows = collections.Counter(n for (n, r) in allev)
        print('seed', s, '国あたり開いた行（中央）', st.median(nat_rows.values()) if nat_rows else 0, ' 開いた行×四半期', sum(668 - v[0] for v in allev.values()))


def main():
    if sys.argv[1] == '--open-only':
        open_only(sys.argv[2], int(sys.argv[3]), int(sys.argv[4])); return
    ins = sys.argv[1]; s0 = int(sys.argv[2]); ns = int(sys.argv[3])
    res = []
    for s in range(s0, s0 + ns):
        if not os.path.exists(os.path.join(ins, 'disc_s%d.tsv' % s)): print('seed', s, 'なし'); continue
        r = analyse_seed(ins, s); res.append(r)
        print('== seed', s, ' 出来事（制度と思想を除く・開幕の既知を除く）全', r['n_all'], ' 研究で開いた', r['n_research'], ' 伝わりで開いた', r['n_transm'])
        for k in ('T1star', 'rec_rowswap_all', 'rec_uniform_all') + tuple('rec_block%d' % b for b in BLOCKS):
            sm = summ(r[k]); print('  ', k, '' if sm is None else 'セル %d 観測M %.2f 帰無 %.2f 差 %+.2f p95超え %.3f' % sm)
        print('  T2', r['T2'], ' T3 蒸気機関1850', r['steam1850'], '1900', r['steam1900'], ' 機関車1850', r['loco1850'], ' T5', r['T5'], ' 清が最初', r['cn_first'])
        print('  国あたり開いた行（中央）', r['open_rows_per_nation_median'], ' 開いた行×四半期', r['row_quarters_open'], ' T8', r['T8'], ' N の張り付き', r['Nshare'], ' rho_s', r['rho_s'])
        for rr, v in r['key'].items(): print('   記録', rr, v)
    if not res: return
    n = len(res)
    print('\n## 集計（%d 種）' % n)
    allc = [c for r in res for c in r['T1star']]
    sm = summ(allc)
    if sm:
        ok1 = sm[4] <= 0.10; ok2 = sm[3] <= 0.25
        print(' T1★ セル', sm[0], ' 観測M %.2f 帰無 %.2f 差 %+.3f (<=+0.25 %s)  p95 超え %.3f (<=0.10 %s)' % (sm[1], sm[2], sm[3], '通る' if ok2 else '外れ', sm[4], '通る' if ok1 else '外れ'))
    else: print(' T1★ セルが無い（外れ扱い）')
    for k in ('rec_rowswap_all', 'rec_uniform_all') + tuple('rec_block%d' % b for b in BLOCKS):
        sm2 = summ([c for r in res for c in r[k]])
        if sm2: print(' 記録', k, 'セル %d 観測M %.2f 帰無 %.2f 差 %+.3f p95超え %.3f' % sm2)
    print(' T2 全ての種で 0:', all(r['T2'] == 0 for r in res))
    print(' T3 蒸気機関1850>=3 の割合', sum(r['steam1850'] >= 3 for r in res) / n, '(>=0.6)  1900>=6 の割合', sum(r['steam1900'] >= 6 for r in res) / n, '(>=0.8)  機関車1850>=4 の割合', sum(r['loco1850'] >= 4 for r in res) / n, '(>=0.6)   ※床の確かめ（追う国の道がある）・見分ける力は弱い')
    nm = sum(r['T5'][0] for r in res); tt = sum(r['T5'][1] for r in res); ex = sum(r['T5'][2] for r in res)
    print(' T5 技師最大の国でない行の割合', round(nm / tt, 3) if tt else None, '(>=0.75)  ランダムの期待値', round(ex / tt, 3) if tt else None)
    nr = sum(r['n_research'] for r in res); na = sum(r['n_all'] for r in res)
    print(' 記録 研究で開いた出来事の割合', round(nr / na, 3) if na else None)
    print(' 記録 清が最初の発見者の行の数 種ごと', [len(r['cn_first']) for r in res])
    print(' 記録 国あたり開いた行（種ごとの中央）', [r['open_rows_per_nation_median'] for r in res], ' 開いた行×四半期（種ごと）', [r['row_quarters_open'] for r in res])
    t8 = [r['T8'] for r in res if r['T8'] is not None]; print(' 記録 T8 中央', round(st.median(t8), 1) if t8 else None)


if __name__ == '__main__':
    main()
