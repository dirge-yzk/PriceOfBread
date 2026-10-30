# (669) 線 navf3：狙いの数字の読み手（読むだけ）。事前登録 measurements/navf669_20261030_prereg.txt の定義のとおり。(606)/(614) の scripts/instruments/navf606/g_lines.py の後継（旗 1 本・本国の港だけ）。
# 追記 1（2026-10-30・腕の前）：G2 は記録だけ（G5 に入れない）・G1b は英が必須＋仏露墺のうち 2 国以上。
# 使い方：python -X utf8 -I scripts/instruments/navf669/g_lines.py <計器の出力フォルダ(…/ins)> [種の数=10] [種の起点=1] [--a <旗 0 の腕の計器の出力フォルダ>] [--a-start <腕 A の種の起点=種の起点>] [--events]
#   <計器の出力フォルダ> が旗 1 の腕（B＝種 1〜10／B′＝種 11〜20）。--a が旗 0 の対（A＝種 1〜5／A′＝種 11〜20）。旗 0 の腕や基線の生に通すと G2・艦の数・半減の事象の表だけが出る（G1・G7・G1b・G6 は旗 1 の列が要る）。
#   --events で半減の事象ごとの表（国×種：始まりの年・その時の艦・走っている最高値・最高値の年と行・戻ったか）を出す（事前登録 [2]）。
import csv, sys, os, statistics as st


def _pos():
    out = []; skip = False
    for a in sys.argv[1:]:
        if skip: skip = False; continue
        if a in ('--a', '--a-start'): skip = True; continue
        if a.startswith('--'): continue
        out.append(a)
    return out


def opt(name):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else None


ARMA = opt('--a'); EVENTS = '--events' in sys.argv
args = _pos()
D = args[0]
NS = int(args[1]) if len(args) > 1 else 10
SB = int(args[2]) if len(args) > 2 else 1
SEEDS = list(range(SB, SB + NS))
ASB = int(opt('--a-start')) if opt('--a-start') else SB
SEVEN = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本']
FOUR = ['イギリス', 'フランス', 'ロシア', 'オーストリア']
G2_FLOOR_WIDTH = 0.112   # m655 の木の旗なし 3 腕の G2 の広がり（0.292−0.180）＝ヌル腕の幅（事前登録 追記 1：G2 は記録だけ・採否に使わない）
G2_NULL_LOW = 0.180      # 旗なしの 3 腕（m655 0.250・N1 0.292・N2 0.180）の下端（記録）
NA = 5 if SB == 1 else NS   # 旗 0 の対の種の数（A＝種 1〜5・A′＝種 11〜20）


def fnum(x):
    try: return float(x)
    except Exception: return float('nan')


def table(name, s, d):
    p = os.path.join(d, f'{name}_s{s}.tsv')
    if not os.path.exists(p): return None, None
    fh = open(p, encoding='utf-8-sig'); rd = csv.reader(fh, delimiter='\t'); h = next(rd)
    return {n: i for i, n in enumerate(h)}, rd


def rows_of(d, s):
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


def fleet_series(s, d):
    rows = rows_of(d, s)
    if rows is None: return None
    out = {}
    for r in rows:
        t = r['time']
        if not (t.startswith('q') and t[1:].isdigit()): continue
        if r['nation'] not in SEVEN: continue
        out.setdefault(r['nation'], []).append((int(t[1:]), int(fnum(r['year'])), fnum(r['fleetUnitsStock'])))
    return out


def halving_event(ser, n_years, ymax=1815):
    """艦隊半減の事象（(594) ③・(606) の定義と同じ）：走っている最高値 P の半分を割った最初の四半期（始まりの年 ≦ ymax）を 1 つだけ。
    戻り値＝None（事象なし／観測が n_years に満たず戻っていない＝分母から外す）か、辞書（始まり・その時の艦・最高値・最高値の年と行・戻ったか）。"""
    f = [x[2] for x in ser]; yrs = [x[1] for x in ser]; L = len(f)
    if not f or max(f) <= 0: return None
    peak = 0.0; pk_i = 0
    for i in range(L):
        if f[i] > peak: peak = f[i]; pk_i = i
        if peak > 0 and f[i] < 0.5 * peak and yrs[i] <= ymax:
            back = None
            for j in range(i, L):
                if f[j] >= peak: back = j; break
            if i + 4 * n_years < L or back is not None:
                return dict(year=yrs[i], fleet=f[i], peak=peak, peak_year=yrs[pk_i], peak_row=pk_i, ok=(back is not None and back - i <= 4 * n_years))
            return None
    return None


def g2(d, seeds, n_years=8, tag='', events=False):
    ok = tot = ok2 = tot2 = 0; per = []; byn = {n: [0, 0] for n in SEVEN}
    for s in seeds:
        fs = fleet_series(s, d)
        if fs is None: continue
        po = pt = 0
        for n in SEVEN:
            if n not in fs: continue
            e = halving_event(fs[n], n_years)
            if e is None: continue
            pt += 1; po += e['ok']; byn[n][0] += e['ok']; byn[n][1] += 1
            if e['peak_row'] > 0: tot2 += 1; ok2 += e['ok']   # G2′：最高値が開幕の行でない事象だけ（記録の副読み・門にしない）
            if events:
                print(f'事象 {tag} 種{s} {n} 始まり{e["year"]} 艦{e["fleet"]:.0f} 最高値{e["peak"]:.0f}（{e["peak_year"]}年・{e["peak_row"]}行目）戻った={int(e["ok"])}')
        ok += po; tot += pt; per.append((po, pt))
    return ok, tot, per, byn, ok2, tot2


def share_other(d, s, n):
    cap = oth = 0.0
    rows = rows_of(d, s)
    if rows is None or not has_nav_cols(rows): return float('nan')
    for r in rows:
        if r['nation'] != n or not r['time'].startswith('q'): continue
        if not (fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth']) > 0): continue
        cap += fnum(r['capitalFleetGoodsOutputPerMonth']); oth += fnum(r['otherFleetGoodsOutputPerMonth'])
    return oth / (cap + oth) if cap + oth > 0 else float('nan')


def g1_g7():
    """G1（入口）：英仏露墺で、予算が正の国×四半期のうち首都以外の予算が正の割合 ≥0.5 の種が 8 種以上。G7（植民地へ流れない）：全ての行で navalBudgetColonialPerMonth が 0。"""
    g1 = {n: [] for n in FOUR}; bad = 0; nrows = 0; worst = (0.0, None)
    for s in SEEDS:
        rows = rows_of(D, s)
        if rows is None: continue
        if not has_nav_cols(rows):
            print('G1/G7 navalBudgetOtherPerMonth の列なし（旗 1 の腕の計器の生ではない）'); return None, None
        for r in rows:
            if not r['time'].startswith('q'): continue
            nrows += 1
            c = fnum(r.get('navalBudgetColonialPerMonth', 'nan'))
            if not (c == 0):   # NaN も外れ
                bad += 1
                if c == c and c > worst[0]: worst = (c, (s, r['nation'], r['year']))
        for n in FOUR:
            tot = oth = 0
            for r in rows:
                if r['nation'] != n or not r['time'].startswith('q'): continue
                if not (fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth']) > 0): continue
                tot += 1; oth += fnum(r['navalBudgetOtherPerMonth']) > 0
            g1[n].append(oth / tot if tot else float('nan'))
    for n in FOUR:
        ok = sum(1 for x in g1[n] if x >= 0.5)
        print(f'G1 {n} 首都以外の予算が正の割合≧0.5 の種 = {ok}/{len(g1[n])}（種ごと {[round(x, 2) for x in g1[n]]}）')
    p1 = all(sum(1 for x in g1[n] if x >= 0.5) >= 8 for n in FOUR)
    print('G1 判定（入口の番人・4 国とも 8 種以上）=', 'PASS' if p1 else 'FAIL')
    p7 = bad == 0 and nrows > 0
    print(f'G7 navalBudgetColonialPerMonth が 0 でない行 = {bad}/{nrows}（最大 {worst}）')
    print('G7 判定（植民地格の州〔首都を除く〕へ予算が流れない）=', 'PASS' if p7 else 'FAIL')
    return p1, p7


def g7_record_a():
    """追記 2 R10：旗 0 の腕（A・A′）の navalBudgetColonialPerMonth が正の行は記録だけ（旧の持ち越し・門にしない）。"""
    if not ARMA: return
    bad = []
    for s in range(ASB, ASB + NA):
        rows = rows_of(ARMA, s)
        if rows is None or not has_nav_cols(rows): continue
        for r in rows:
            if r['time'].startswith('q') and not (fnum(r.get('navalBudgetColonialPerMonth', 'nan')) == 0): bad.append((s, r['nation'], r['year']))
    print(f'記録 G7 旗 0 の対（A）で navalBudgetColonialPerMonth が 0 でない行 = {len(bad)}（{bad[:6]}）')


def g1b():
    """G1b（追記 1）：英が必須（B の中央値が A の種ごとの最大を上回る）かつ 仏・露・墺のうち 2 国以上。"""
    if not ARMA: print('G1b --a（旗 0 の腕の計器の出力）が無い'); return None
    up = {}
    for n in FOUR:
        bl = [x for x in (share_other(D, s, n) for s in SEEDS) if x == x]
        al = [x for x in (share_other(ARMA, s, n) for s in range(ASB, ASB + NA)) if x == x]
        if not bl or not al: print(f'G1b {n} 値なし'); up[n] = False; continue
        bm = st.median(bl); am = max(al); up[n] = bm > am
        print(f'G1b {n} 造船の産出のうち首都以外の割合 B 中央値={bm:.4f} A 最大={am:.4f} A 中央値={st.median(al):.4f}  B種ごと={[round(x, 3) for x in bl]} A種ごと={[round(x, 3) for x in al]}')
    others = sum(1 for n in FOUR if n != 'イギリス' and up.get(n))
    p = bool(up.get('イギリス')) and others >= 2
    print('G1b 判定（英が必須・仏露墺のうち 2 国以上で B の中央値が A の種ごとの最大を上回る）=', 'PASS' if p else 'FAIL', f'（英={up.get("イギリス")}・仏露墺={others} 国）')
    return p


def g6():
    """G6：英の portFleetGoodsStock（首都以外の本国格の港）の（1790 年末 ÷ 最初の行）が旗 1 で旗 0 より小さい種が、対にした先頭 5 種中 4 種以上。"""
    if not ARMA: print('G6 --a（旗 0 の腕の計器の出力）が無い'); return None

    def ratio(d, s):
        rows = rows_of(d, s)
        if rows is None or 'portFleetGoodsStock' not in rows[0]: return None
        uk = [r for r in rows if r['nation'] == 'イギリス']
        if not uk: return None
        first = fnum(uk[0]['portFleetGoodsStock'])
        y = [r for r in uk if int(fnum(r['year'])) == 1790 and r['time'].startswith('q')]
        if not y or not (first > 0): return None
        return fnum(y[-1]['portFleetGoodsStock']) / first
    ok = tot = 0; per = []
    for k in range(5):
        rb, ra = ratio(D, SB + k), ratio(ARMA, ASB + k)
        if rb is None or ra is None: continue
        tot += 1; ok += rb < ra; per.append((SB + k, round(rb, 3), round(ra, 3)))
    print(f'G6 英の本国格の港の州の艦の財の在庫 1790年末/最初の行 が B<A の種 = {ok}/{tot}（先頭 5 種・種ごと 種,B,A {per}）')
    p = tot >= 5 and ok >= 4
    print('G6 判定（先頭 5 種中 4 種以上）=', 'PASS' if p else 'FAIL')
    return p


def records():
    for tag, d, s0, n in (('B', D, SB, NS), ('A', ARMA, ASB, 5 if SB == 1 else NS)):
        if not d: continue
        for nat in SEVEN:
            bud = home = paid = 0.0; mvh = []; mvc = []
            for s in range(s0, s0 + n):
                rows = rows_of(d, s)
                if rows is None or not has_nav_cols(rows) or 'navyPaidPerMonth' not in rows[0]: continue
                for r in rows:
                    if r['nation'] != nat or not r['time'].startswith('q'): continue
                    b = fnum(r['navalBudgetCapitalPerMonth']) + fnum(r['navalBudgetOtherPerMonth'])
                    bud += b; home += fnum(r['navalBudgetOtherPerMonth']); paid += fnum(r['navyPaidPerMonth'])
                last = [r for r in rows if r['nation'] == nat]
                if last and 'fleetUnitsMovedOutHomeCum' in last[-1]:
                    mvh.append(fnum(last[-1]['fleetUnitsMovedOutHomeCum'])); mvc.append(fnum(last[-1]['fleetUnitsMovedOutColonialCum']))
            if bud > 0:
                m = lambda v: st.median(v) if v else float('nan')
                print(f'記録 {tag} {nat} 予算のうち首都以外の本国格の港へ = {home / bud:.4f}・海軍の支払い÷予算 = {paid / bud:.4f}・出ていった艦(本国格/植民地格)の種ごとの中央値 = {m(mvh):.1f}/{m(mvc):.1f}')


def main():
    n_seed = sum(1 for s in SEEDS if fleet_series(s, D) is not None)
    print(f'# {D} 種={n_seed}（{SEEDS[0]}〜{SEEDS[-1]}）')
    p1, p7 = g1_g7(); g7_record_a(); p1b = g1b(); p6 = g6()
    g2b = float('nan')
    for n in (5, 8):
        o, t, per, byn, o2, t2 = g2(D, SEEDS, n, tag='B', events=(EVENTS and n == 8))
        print(f'G2 艦隊半減からの回復 N={n}年 戻った/事象 = {o}/{t} = {o / t if t else float("nan"):.3f}（種ごと {per}）')
        if n == 8:
            print('G2 国ごと（戻った/事象）', {k: tuple(v) for k, v in byn.items()})
            print(f'G2′（記録・最高値が開幕の行でない事象だけ）= {o2}/{t2}' + (f' = {o2 / t2:.3f}' if t2 else ''))
            g2b = o / t if t else float('nan')
    floor_a = None
    if ARMA:
        o, t, per, byn, o2, t2 = g2(ARMA, range(ASB, ASB + NA), 8, tag='A', events=EVENTS)
        floor_a = o / t if t else float('nan')
        print(f'G2 旗 0 の対（種 {ASB}〜{ASB + NA - 1}）戻った/事象 = {o}/{t} = {floor_a:.3f}（種ごと {per}）')
        print(f'記録 G2（採否に使わない・追記 1）B {g2b:.3f} vs 対 − {G2_FLOOR_WIDTH} = {floor_a - G2_FLOOR_WIDTH:.3f}（B が対の幅の内か：', 'yes' if g2b >= floor_a - G2_FLOOR_WIDTH else 'no', '）')
    if SB == 1:
        print(f'記録 G2（採否に使わない・追記 1）種 1〜10 の旗なし 3 腕の下端（N2）= {G2_NULL_LOW:.3f} vs B {g2b:.3f}（B ≥ 下端：', 'yes' if g2b >= G2_NULL_LOW else 'no', '）')
    gate = [x for x in (p1, p7, p1b, p6) if x is not None]
    if not gate: print('G5 旗 1 の列が無い生＝判定なし')
    else: print('G5（G4 を除く・G2 は記録だけ・名乗りは〈入口は開いた・尾は未判定〉に固定）=', 'PASS' if all(gate) else f'FAIL（G1={p1} G7={p7} G1b={p1b} G6={p6}）')
    records()
    for y in (1790, 1800, 1815, 1850, 1913):
        vals = []
        for s in SEEDS:
            fs = fleet_series(s, D)
            if fs is None: continue
            for nat, ser in fs.items():
                v = [x[2] for x in ser if x[1] == y]
                if v: vals.append(v[-1])
        if vals: print(f'記録 艦の数 {y}年末 7国×種の中央値 = {st.median(vals):.1f}（n={len(vals)}）')


if __name__ == '__main__':
    main()
