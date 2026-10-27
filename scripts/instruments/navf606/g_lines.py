# (606) 線 navf：狙いの数字の読み手（読むだけ）。事前登録 measurements/navf606_20261028_prereg.txt の定義のとおり。
# 使い方：PYTHONUTF8=1 python -I scripts/instruments/navf606/g_lines.py <計器の出力フォルダ(…/ins か keep_*/…)> [種の数=10] [種の起点=1] [--base <基線の計器の出力フォルダ>] [--a <腕 A の計器の出力フォルダ>]
#   G2 は <計器の出力フォルダ> だけで出る。G1・G1b は navalBudget… の列（腕 B／腕 A）。G3 は --base（keep_600/m600_ins）と比べる。G1b・G6 は --a（腕 A の種 1〜5）と比べる。
# 基線 D:/PoB_work/keep_600/m600_ins とヌル腕 D:/PoB_work/keep_592/n1・n2 に通すと基線の値が出る（nav_s が要る G1・G3・Gv は基線では〈nav_s なし〉）。
import csv, sys, os, statistics as st

def _pos():
    out = []; skip = False
    for a in sys.argv[1:]:
        if skip: skip = False; continue
        if a in ('--base', '--a'): skip = True; continue
        out.append(a)
    return out
def opt(name):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else None
BASE = opt('--base'); ARMA = opt('--a')
args = _pos()
D = args[0]
NS = int(args[1]) if len(args) > 1 else 10
SB = int(args[2]) if len(args) > 2 else 1
SEEDS = range(SB, SB + NS)
SEVEN = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本']   # 計器の挙げた 9 国のうち清とオスマンを除く＝(594) ③の主要国のうち計器に載る 7 国
NINE = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']


def fnum(x):
    try: return float(x)
    except Exception: return float('nan')


def table(name, s, d=None):
    p = os.path.join(d or D, f'{name}_s{s}.tsv')
    if not os.path.exists(p): return None, None
    fh = open(p, encoding='utf-8-sig'); rd = csv.reader(fh, delimiter='\t'); h = next(rd)
    return {n: i for i, n in enumerate(h)}, rd


def fleet_series(s):
    """国 -> [(通し番号, 年, 艦の数)]（q0 以降の四半期の行だけ。open・warm の行は除く）。"""
    ix, rd = table('stock', s)
    if ix is None: return None
    out = {}
    for r in rd:
        t = r[ix['time']]
        if not (t.startswith('q') and t[1:].isdigit()): continue
        n = r[ix['nation']]
        if n not in SEVEN: continue
        out.setdefault(n, []).append((int(t[1:]), int(r[ix['year']]), fnum(r[ix['fleetUnitsStock']])))
    return out


def halving_events(series, n_years, ymax=1815):
    """艦隊半減の事象（(594) ③の再現）：国×種ごとに〈走っている最高値 P の半分を割った最初の四半期〉（始まりの年 ≦ ymax）を 1 つだけ数え、
    n_years 年以内（4×n_years 四半期）に f≧P へ戻ったかを返す。観測が n_years 年に満たない始まりで戻っていないものは分母から外す（打ち切り）。戻り値＝(戻った数, 分母の数)。"""
    ok = tot = 0
    for nat, ser in series.items():
        f = [x[2] for x in ser]; yrs = [x[1] for x in ser]
        if not f or max(f) <= 0: continue
        peak = 0.0; L = len(f)
        for i in range(L):
            if f[i] > peak: peak = f[i]
            if peak > 0 and f[i] < 0.5 * peak and yrs[i] <= ymax:
                back = None
                for j in range(i, L):
                    if f[j] >= peak: back = j; break
                if i + 4 * n_years < L or back is not None:
                    tot += 1
                    if back is not None and back - i <= 4 * n_years: ok += 1
                break
    return ok, tot


def rows_of(d, s):
    """計器の stock 表の四半期の行（q 始まり）を列名の辞書にして返す。表が無い／新しい列が無い＝None。"""
    ix, rd = table('stock', s, d)
    if ix is None: return None
    out = []
    for r in rd:
        t = r[ix['time']]
        if not (t.startswith('q') and t[1:].isdigit()) and t not in ('open', 'warm'): continue
        out.append({n: r[i] for n, i in ix.items()})
    return out


def has_nav_cols(rows):
    return bool(rows) and 'navalBudgetOtherPerMonth' in rows[0]


def g1_g1b():
    """G1（入口が開いたことを見る番人）：英・仏・露・墺で、海軍の予算が正の国×四半期のうち首都以外の予算が正の割合 ≥0.5 の種の数（4 国とも ≥8/10）。
    G1b（追記 2 の定義）：予算が正の国×四半期の造船の産出（3 財の和）のうち首都以外の割合。B の種 2〜10 の中央値が A の種 2〜5 の**種ごとの最大**を上回る国が 4 国のうち 3 国以上。戻り値＝(G1 の合否, G1b の合否)。"""
    nats = ['イギリス', 'フランス', 'ロシア', 'オーストリア']
    g1 = {n: [] for n in nats}

    def share_other(d, s, n):
        cap = oth = 0.0
        rows = rows_of(d, s)
        if rows is None or not has_nav_cols(rows): return float('nan')
        for r in rows:
            if r['nation'] != n or not r['time'].startswith('q'): continue
            if not (fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth']) > 0): continue
            cap += fnum(r['capitalFleetGoodsOutputPerMonth']); oth += fnum(r['otherFleetGoodsOutputPerMonth'])
        return oth / (cap + oth) if cap + oth > 0 else float('nan')
    for s in SEEDS:
        rows = rows_of(D, s)
        if rows is None: continue
        if not has_nav_cols(rows):
            print('G1 navalBudgetOtherPerMonth の列なし（基線の計器の生には無い列）'); return None, None
        for n in nats:
            tot = oth = 0
            for r in rows:
                if r['nation'] != n or not r['time'].startswith('q'): continue
                if not (fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth']) > 0): continue
                tot += 1; oth += fnum(r['navalBudgetOtherPerMonth']) > 0
            g1[n].append(oth / tot if tot else float('nan'))
    for n in nats:
        ok = sum(1 for x in g1[n] if x >= 0.5)
        print(f'G1 {n} 首都以外の予算が正の割合≧0.5 の種 = {ok}/{len(g1[n])}（種ごと {[round(x, 2) for x in g1[n]]}）')
    p1 = all(sum(1 for x in g1[n] if x >= 0.5) >= 8 for n in nats)
    print('G1 判定（入口の番人・4 国とも 8 種以上）=', 'PASS' if p1 else 'FAIL')
    if not ARMA:
        print('G1b --a（腕 A の計器の出力）が無い'); return p1, None
    up = 0
    for n in nats:
        bl = [x for x in (share_other(D, s, n) for s in range(2, 11)) if x == x]
        al = [x for x in (share_other(ARMA, s, n) for s in range(2, 6)) if x == x]
        if not bl or not al: print(f'G1b {n} 値なし'); continue
        bm = st.median(bl); am = max(al); up += bm > am
        print(f'G1b {n} 造船の産出のうち首都以外の割合 B 中央値(種2〜10)={bm:.4f} A 最大(種2〜5)={am:.4f} A 中央値={st.median(al):.4f}  B種ごと={[round(x, 3) for x in bl]} A種ごと={[round(x, 3) for x in al]}')
    p1b = up >= 3
    print('G1b 判定（B の中央値が A の種ごとの最大を上回る国が 3 国以上）=', 'PASS' if p1b else 'FAIL', f'（{up} 国）')
    return p1, p1b


def g3():
    """G3（入口の番人）：英・仏の navyUpkeepShare の 1783〜1830 年の四半期の中央値が B で基線（--base の同じ種）以上の種の数（両国とも ≥8/10）。"""
    if not BASE: print('G3 --base（基線の計器の出力）が無い'); return None
    res = []
    for n in ('イギリス', 'フランス'):
        ok = tot = 0; per = []
        for s in SEEDS:
            a = rows_of(D, s); b = rows_of(BASE, s)
            if a is None or b is None: continue
            def med(rows):
                v = [fnum(r['navyUpkeepShare']) for r in rows if r['nation'] == n and r['time'].startswith('q') and 1783 <= int(fnum(r['year'])) <= 1830]
                v = [x for x in v if x == x]
                return st.median(v) if v else float('nan')
            mb, ma = med(a), med(b)
            tot += 1; ok += mb >= ma; per.append((round(mb, 4), round(ma, 4)))
        print(f'G3 {n} navyUpkeepShare 中央値 B≧基線 の種 = {ok}/{tot}（種ごと B,基線 {per}）')
        res.append(ok >= 8 and tot >= 10)
    p = all(res)
    print('G3 判定（入口の番人・英仏とも 8 種以上）=', 'PASS' if p else 'FAIL')
    return p


def g6():
    """G6（入口の番人）：英の portFleetGoodsStock の（1790 年末 ÷ 最初の行）が B で A より小さい種（種 1〜5 の対で ≥4/5、かつ種 1 を外した種 2〜5 で ≥3/4＝追記 2）。"""
    if not ARMA: print('G6 --a（腕 A の計器の出力）が無い'); return None
    def ratio(d, s):
        rows = rows_of(d, s)
        if rows is None or not has_nav_cols(rows): return None
        uk = [r for r in rows if r['nation'] == 'イギリス']
        if not uk: return None
        first = fnum(uk[0]['portFleetGoodsStock'])
        y = [r for r in uk if int(fnum(r['year'])) == 1790 and r['time'].startswith('q')]
        if not y or not (first > 0): return None
        return fnum(y[-1]['portFleetGoodsStock']) / first
    ok = tot = ok2 = tot2 = 0; per = []
    for s in range(1, 6):
        rb, ra = ratio(D, s), ratio(ARMA, s)
        if rb is None or ra is None: continue
        tot += 1; ok += rb < ra; per.append((s, round(rb, 3), round(ra, 3)))
        if s >= 2: tot2 += 1; ok2 += rb < ra
    print(f'G6 英の港の州の艦の財の在庫 1790年末/最初の行 が B<A の種 = {ok}/{tot}（種1〜5）・{ok2}/{tot2}（種2〜5）（種ごと 種,B,A {per}）')
    p = tot >= 5 and ok >= 4 and tot2 >= 4 and ok2 >= 3
    print('G6 判定（入口の番人・種1〜5 で 4/5 以上かつ 種2〜5 で 3/4 以上）=', 'PASS' if p else 'FAIL')
    return p


def records():
    """記録（採否に使わない）：予算の植民地格の割合・海軍の区分の支払い÷予算（B と A）・艦ごと移った隻数。"""
    for tag, d in (('B', D), ('A', ARMA)):
        if not d: continue
        for n in ['イギリス', 'フランス', 'ロシア', 'オーストリア', 'アメリカ', '日本', 'プロイセン']:
            bud = col = paid = 0.0; mv = []
            for s in range(1, 11):
                rows = rows_of(d, s)
                if rows is None or not has_nav_cols(rows) or 'navyPaidPerMonth' not in rows[0]: continue
                for r in rows:
                    if r['nation'] != n or not r['time'].startswith('q'): continue
                    bud += fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth'])
                    col += fnum(r['navalBudgetColonialPerMonth']); paid += fnum(r['navyPaidPerMonth'])
                last = [r for r in rows if r['nation'] == n]
                if last: mv.append(fnum(last[-1]['fleetUnitsMovedByTransferCum']) - fnum(last[0]['fleetUnitsMovedByTransferCum']))
            if bud > 0:
                print(f'記録 {tag} {n} 予算のうち植民地格の州へ = {col / bud:.4f}・海軍の支払い÷予算 = {paid / bud:.4f}・艦ごと移った隻数(州の移し替え)の種ごとの中央値 = {st.median(mv) if mv else float("nan"):.2f}')


def main():
    res = {5: [0, 0], 8: [0, 0]}; per = {5: [], 8: []}
    n_seed = 0
    for s in SEEDS:
        fs = fleet_series(s)
        if fs is None: continue
        n_seed += 1
        for n in (5, 8):
            o, t = halving_events(fs, n)
            res[n][0] += o; res[n][1] += t
            per[n].append((o, t))
    print(f'# {D} 種={n_seed}')
    p1, p1b = g1_g1b(); p3 = g3(); p6 = g6()
    for n in (5, 8):
        o, t = res[n]
        print(f'G2 艦隊半減からの回復 N={n}年 戻った/事象 = {o}/{t} = {o / t if t else float("nan"):.3f}（種ごと {per[n]}）')
    o, t = res[8]; g2 = o / t if t else float('nan')
    tier = '採らない(0.51 未満)' if g2 < 0.51 else ('入口は開いた・尾の効きは判定不能(0.51〜0.74)' if g2 <= 0.74 else '直った(0.74 超)')
    print(f'G2 判定（N=8 年）= {g2:.3f} → {tier}')
    print('G5 〈機構が狙いどおり働いたか〉(G1・G3・G6・G1b が PASS かつ G2≧0.51 かつ G4 非該当[別に確かめる]) =',
          'PASS（G4 を除く）' if (p1 and p3 and p6 and p1b and g2 >= 0.51) else f'FAIL（G1={p1} G3={p3} G6={p6} G1b={p1b} G2={g2:.3f}）')
    records()
    # 記録：艦の数の年代（7 国の種の中央値）
    for y in (1790, 1800, 1815, 1850, 1913):
        vals = []
        for s in SEEDS:
            fs = fleet_series(s)
            if fs is None: continue
            for nat, ser in fs.items():
                v = [x[2] for x in ser if x[1] == y]
                if v: vals.append(v[-1])
        if vals: print(f'記録 艦の数 {y}年末 7国×種の中央値 = {st.median(vals):.1f}（n={len(vals)}）')


if __name__ == '__main__':
    main()
