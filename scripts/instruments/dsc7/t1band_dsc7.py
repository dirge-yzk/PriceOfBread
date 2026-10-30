# dsc7（仮番号 678）：T1★（国ごとの固まり）の読み手。事前登録 measurements/dsc7_20261030/prereg_dsc7.txt [5] の門と、その門の出所の〈旗 0 の帯〉を同じ 1 本で読む。
# dsc6 の gates_dsc6b.py（枝 dsc6）の T1★ の定義（研究で開いた出来事・行ごとの国間入れ替えの帰無・2000 回・乱数 random.Random(20261027*1000+種)）を写し、
# disc_s の E 行だけで読めるようにした（dscx が無い旗 0 の生でも読める＝帯を作れる）。開幕の既知は q>0 の近似（dsc6 は dscx の R 行の known）。
# 使い方：PYTHONUTF8=1 python t1band_dsc7.py <ins ディレクトリ（D:/… の形）> <種の起点> <種の数> [--gate]
#   --gate を付けると、事前登録の門（(i) 平均差 ≦ +0.15・(ii) p95 超えのセルの割合 ≦ 0.10）で判定を 1 行出す。門の数は下の定数で、結果を見て変えない。
# ⚠限界（自己申告）：国は主要 9 国だけ／〈研究で開いた〉は開いた四半期の E 行の項（技術＋必要性 ≧ 伝わり）の大小で、積み上げの全期間の寄与ではない／
#   開幕の既知は q>0 の近似／制度と思想の行は除く／旗 0 は国あたり 5〜12 行・研究の割合 0.9 以上、旗つき（dsc6）は 45〜50 行・0.53＝母集団が違う（帯は門の高さの出所で、同じ母集団の比べではない）。
import sys, os, csv, collections, random, statistics as st

MAJOR = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', 'オーストリア', 'オスマン', '清', '日本']
SPREAD = {'特許', '株式会社', '社会主義', '共産主義', '帝国主義', 'ファシズム'}
DRAWS = 2000
GATE_MEAN_EXCESS = 0.15     # (i) 旗 0 の 4 つの腕（m675・N1・N2・種 11〜20）の最小 +0.153 の下＝帯の外（事前登録 [5]）
GATE_P95_SHARE = 0.10       # (ii) dsc6 のまま（番人＝旗 0 も dsc6 も通る・判別力は無い）
GATE_ALL_EXCESS = 0.20      # (i-b) 全ての出来事の入れ替え（rec_rowswap_all）の平均差の線＝旗 0 の 4 腕の最小 +0.203 の下（事前登録 追記 2）
GATE_MIN_CELLS = 80         # セルの数の床（10 種×9 国＝90・旗 0 は 89〜90）。母集団の側へ出来事を逃がして門を通ることを防ぐ（追記 2）
GATE_MIN_RESEARCH_SHARE = 0.40  # 研究で開いた割合の床（旗 0 は 0.90〜0.99・dsc6 は 0.53）。分母の崩れだけを捕まえる緩い床（追記 2）


def mwin(xs):
    xs = sorted(xs); j = 0; b = 0
    for i in range(len(xs)):
        while xs[i] - xs[j] > 3: j += 1
        b = max(b, i - j + 1)
    return b


def events_terms(path):
    """disc_s の E 行＝(国, 行) -> (四半期, 技術, 必要性, 伝わり)。最初の出来事だけ。"""
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
        v = sorted(vs[n]); out.append((n, mwin(obs[n]), sum(v) / len(v), v[int(0.95 * len(v)) - 1], len(obs[n]), st.pstdev(v)))
    return out


def cells_uniform(op, rng):
    out = []
    for n in MAJOR:
        qq = [q for (m, r), q in op.items() if m == n]
        if len(qq) < 5: continue
        lo, hi = min(qq), max(qq); v = []
        for _ in range(DRAWS): v.append(mwin([rng.randint(lo, hi) for _ in qq]))
        v.sort(); out.append((n, mwin(qq), sum(v) / len(v), v[int(0.95 * len(v)) - 1], len(qq), st.pstdev(v)))
    return out


def summ(cells):
    if not cells: return None
    # 末尾の 6 つ目＝規模に依らない記録 (M−μ)÷σ の平均（σ が 0 のセルは除く）
    zs = [(m - mu) / sd for _, m, mu, _, _, sd in cells if sd > 0]
    return (len(cells), st.mean(c[1] for c in cells), st.mean(c[2] for c in cells),
            st.mean(c[1] - c[2] for c in cells), sum(1 for c in cells if c[1] > c[3]) / len(cells), st.mean(zs) if zs else float('nan'))


def main():
    ins = sys.argv[1]; s0 = int(sys.argv[2]); ns = int(sys.argv[3]); gate = '--gate' in sys.argv
    acc = {'T1star': [], 'rec_rowswap_all': [], 'rec_uniform_all': []}; fr = []; seen = 0
    for s in range(s0, s0 + ns):
        p = os.path.join(ins, 'disc_s%d.tsv' % s)
        if not os.path.exists(p): print('seed', s, 'なし'); continue
        seen += 1
        ev = events_terms(p)
        op = {k: v[0] for k, v in ev.items() if v[0] > 0 and k[1] not in SPREAD and k[0] in MAJOR}
        res = {k: q for k, q in op.items() if ev[k][1] + ev[k][2] >= ev[k][3]}
        rng = random.Random(20261027 * 1000 + s)
        c1 = cells_rowswap(res, rng); acc['T1star'] += c1
        acc['rec_rowswap_all'] += cells_rowswap(op, rng); acc['rec_uniform_all'] += cells_uniform(op, rng)
        fr.append(len(res) / len(op) if op else float('nan'))
        sm = summ(c1)
        print('seed', s, '出来事', len(op), '研究で開いた', len(res), '' if sm is None else 'T1★ セル %d 差 %+.2f p95超え %.3f' % (sm[0], sm[3], sm[4]))
    print('種の数', seen)
    for k, v in acc.items():
        sm = summ(v); print(k, 'セルが無い' if sm is None else 'セル %d 観測M %.2f 帰無 %.2f 差 %+.3f p95超え %.3f 規模に依らない差(M-μ)/σ %+.3f' % sm)
    print('研究で開いた割合（種ごと）', [round(x, 2) for x in fr])
    if gate:
        sm = summ(acc['T1star'])
        if sm is None or seen != ns: print('T1★ 判定：外れ（セルが無いか種が欠けた）'); return
        ok1 = sm[3] <= GATE_MEAN_EXCESS; ok2 = sm[4] <= GATE_P95_SHARE
        sa = summ(acc['rec_rowswap_all']); ok3 = sa is not None and sa[3] <= GATE_ALL_EXCESS
        ok4 = sm[0] >= GATE_MIN_CELLS
        sh = st.mean(fr) if fr else float('nan'); ok5 = sh >= GATE_MIN_RESEARCH_SHARE
        allok = ok1 and ok2 and ok3 and ok4 and ok5
        print('T1★ 判定：(i) 差 %+.3f ≦ %+.2f %s／(ii) p95 超え %.3f ≦ %.2f %s／(i-b) 全ての出来事の入れ替えの差 %s ≦ %+.2f %s／セル %d ≧ %d %s／研究で開いた割合 %.2f ≧ %.2f %s ＝ %s' % (
            sm[3], GATE_MEAN_EXCESS, '通る' if ok1 else '外れ', sm[4], GATE_P95_SHARE, '通る' if ok2 else '外れ',
            'なし' if sa is None else '%+.3f' % sa[3], GATE_ALL_EXCESS, '通る' if ok3 else '外れ',
            sm[0], GATE_MIN_CELLS, '通る' if ok4 else '外れ', sh, GATE_MIN_RESEARCH_SHARE, '通る' if ok5 else '外れ', '通る' if allok else '外れ'))


if __name__ == '__main__':
    main()
