#!/usr/bin/env python
"""(558) 線 rs3 の読み手：事前登録 measurements/rs3558_20261025_prereg.txt の [5]（G1〜G5）と [6]（R1〜R7 の一部）を読む。
使い方：PYTHONUTF8=1 python scripts/instruments/rs3558/read_rs3.py <計器の出力フォルダ D:/…（腕 B の ins）> [--base <基線の ins D:/…>] [--null <ヌル腕の ins をコンマ区切り D:/…>] [--a3 <腕 A（旗 0・INS=1・種 1〜5）の ins D:/…＝危機の記録の基線>]
入力：<ins>/rs3_s*.tsv（InstrumentsRs3.cs）・rs2_s*.tsv（InstrumentsRs2.cs）・reform_s*.tsv。--base があれば基線の rs2_s*.tsv・reform_s*.tsv を記録の比べに使う（基線には rs3 の表が無い）。
G5 の M1・M2・M2b・M3 は scripts/instruments/rs2547/read_rs2.py をそのまま呼ぶ（閾値は変えない）。M5 はここで 412（幅の下限 10）で読む。
注意：パスは D:/… の形で渡す（/d/… だと Windows の Python は空を読む）。閾値は事前登録のとおり（結果を見て直さない）。
"""
import csv, glob, os, subprocess, statistics as st, sys
from collections import defaultdict

TOL_DEC = 1e-6; TOL_INC = 1e-9
G3_MIN = 0.70
G4_MED = 0.004; G4_EACH = 0.008; G4_FRAC = 0.80; G4_MIN_SERIES = 20; G4_PREMISE = 0.008
RUN_ROWS = 10; RUN_SPAN_Q = 40; GAP_Q = 4
M5_THR = 412
G7_THR = 0.0881   # 事前登録 [11](d)：0.0641＋2×0.0120
PICKED = ('清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ')
SS = 2.0


def read_tsv(path):
    with open(path, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def load(d, prefix):
    out = {}
    for fp in glob.glob(os.path.join(d, prefix + '_s*.tsv')):
        out[int(os.path.basename(fp)[len(prefix) + 2:-4])] = read_tsv(fp)
    return dict(sorted(out.items()))


def fl(r, k):
    try:
        return float(r[k])
    except (ValueError, KeyError):
        return float('nan')


def slope(xs, ys):
    n = len(xs)
    if n < 2:
        return float('nan')
    mx = sum(xs) / n; my = sum(ys) / n
    den = sum((x - mx) ** 2 for x in xs)
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / den if den > 0 else float('nan')


def capS(l):  # 農奴解放の地代の上限（手書き）
    return 1.0 if l <= 0 else (0.75 if l == 1 else 0.5)


def capL(l):  # 土地改革の地代の上限（手書き）
    return 1.0 if l <= 0 else (0.65 if l == 1 else 0.3)


def eff(c, s, l):
    return min(c, min(capS(s), capL(l)))


def expect(r):
    """列から手の式で組んだ (減らす向きの強さ 農奴, 土地, 進める向きの強さ 農奴, 土地)。"""
    s = int(r['serfLawCode']); l = int(r['landReformLawCode']); c = fl(r, 'customaryRentShare'); ratio = fl(r, 'lordRentToIncomeShare')
    now = eff(c, s, l)
    def g(prev):
        return (prev - now) / now * ratio if now > 1e-9 else 0.0
    def loss(nxt):
        return (now - nxt) / now * ratio if now > 1e-9 else 0.0
    dS = SS * (0.5 + g(eff(c, max(s - 1, 0), l)))
    dL = SS * (0.5 + g(eff(c, s, max(l - 1, 0))))
    iS = SS * (0.5 + loss(eff(c, min(s + 1, 2), l)))
    iL = SS * (0.5 + loss(eff(c, s, min(l + 1, 2))))
    return dS, dL, iS, iL, now


def runs(rows, keyf, okf):
    cur = []
    for r in rows:
        q = int(r['quarter'])
        if okf(r) and cur and (q - int(cur[-1]['quarter']) > GAP_Q or keyf(r) != keyf(cur[-1])):
            yield cur
            cur = []
        if okf(r):
            cur.append(r)
        elif cur:
            yield cur
            cur = []
    if cur:
        yield cur


def parse_axes():
    import re
    here = os.path.dirname(os.path.abspath(__file__))
    t = open(os.path.join(here, '..', '..', '..', 'PriceOfBread.SimCore', 'PolicyPreferences.cs'), encoding='utf-8').read()
    m = re.search(r'public enum PolicyAxis\s*\{(.*?)\n\}', t, re.S)
    body = re.sub(r'//[^\n]*', '', m.group(1))
    return re.findall(r'[A-Za-z_]\w*', body)


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 2
    d = args[0]
    base = args[args.index('--base') + 1] if '--base' in args else None
    nulls = args[args.index('--null') + 1].split(',') if '--null' in args else []
    rs3 = load(d, 'rs3'); rs2 = load(d, 'rs2'); ref = load(d, 'reform')
    if not rs3:
        print('rs3_s*.tsv が無い')
        return 2
    print(f'== 読み手 read_rs3.py  入力={d}  種={list(rs3)}')
    allrows = [r for rows in rs3.values() for r in rows]
    # ---- G2 ----
    bad1 = sum(1 for r in allrows if fl(r, 'flagLevel') != 1.0)
    bad2 = sum(1 for r in allrows if fl(r, 'flagLevel2') != 1.0)
    print(f'[G2] flagLevel!=1 の行 {bad1}・flagLevel2!=1 の行 {bad2}（全 {len(allrows)} 行） -> {"通る" if bad1 == 0 and bad2 == 0 else "外れ"}')
    # ---- G1 ----
    nbad = 0; nbadI = 0; nbadJ = 0; njoin = 0
    rs2idx = {s: {(r['quarter'], r['nation']): r for r in rows} for s, rows in rs2.items()}
    for s, rows in rs3.items():
        for r in rows:
            dS, dL, iS, iL, now = expect(r)
            if abs(dS - fl(r, 'oppSerfDecUsed')) > TOL_DEC or abs(dL - fl(r, 'oppLandDecUsed')) > TOL_DEC:
                nbad += 1
            if abs(iS - fl(r, 'oppSerfIncUsed')) > TOL_DEC or abs(iL - fl(r, 'oppLandIncUsed')) > TOL_DEC:
                nbadI += 1
            j = rs2idx.get(s, {}).get((r['quarter'], r['nation']))
            if j is not None:
                njoin += 1
                if abs(fl(j, 'oppSerfNewIndex') - fl(r, 'oppSerfIncUsed')) > TOL_INC or abs(fl(j, 'oppLandNewIndex') - fl(r, 'oppLandIncUsed')) > TOL_INC:
                    nbadJ += 1
    n = len(allrows)
    print(f'[G1] 減らす向きの強さが手の式と合わない行 {nbad}/{n}・進める向きが手の式と合わない行 {nbadI}/{n}・rs2 の列と合わない行 {nbadJ}/{njoin} -> {"通る" if nbad == 0 and nbadI == 0 and nbadJ == 0 else "外れ"}')
    # ---- G3 ----
    def left_floor(axcode, col):
        rows = [r for r in allrows if int(r[axcode]) == 2]
        if not rows:
            return float('nan'), 0
        return sum(1 for r in rows if fl(r, col) > 1.0 + 1e-9) / len(rows), len(rows)
    fL, nL = left_floor('landReformLawCode', 'oppLandDecUsed')
    fS, nS = left_floor('serfLawCode', 'oppSerfDecUsed')
    print(f'[G3] 土地改革が全面の行で減らす向きの強さが床(1.0)を離れた割合 {fL:.3f}（{nL} 行・≧{G3_MIN}） -> {"通る" if fL >= G3_MIN else "外れ"}   〔記録〕農奴解放が全面の行 {fS:.3f}（{nS} 行・基線の読み 0.026）')
    # ---- G4 ----
    def series_slopes(gaincol, deccol):
        out = []
        for s, rows in rs3.items():
            byn = defaultdict(list)
            for r in rows:
                byn[r['nation']].append(r)
            for nat, rr in byn.items():
                rr.sort(key=lambda r: int(r['quarter']))
                for run in runs(rr, lambda r: (r['serfLawCode'], r['landReformLawCode']), lambda r: fl(r, gaincol) > 1e-9):
                    if len(run) >= RUN_ROWS and int(run[-1]['quarter']) - int(run[0]['quarter']) >= RUN_SPAN_Q:
                        xs = [int(r['quarter']) / 4 for r in run]
                        out.append((s, nat, len(run), slope(xs, [fl(r, deccol) for r in run]), slope(xs, [fl(r, 'oppOldIndex') for r in run])))
        return out
    for ax, gc, dc in (('土地改革', 'gainLandShare', 'oppLandDecUsed'), ('農奴解放', 'gainSerfShare', 'oppSerfDecUsed')):
        sl = series_slopes(gc, dc)
        if not sl:
            print(f'[G4/{ax}] 系列が無い -> 判定不能')
            continue
        mo = st.median(x[4] for x in sl)
        mn = st.median(abs(x[3]) for x in sl)
        fr = sum(1 for x in sl if abs(x[3]) <= G4_EACH) / len(sl)
        prem = mo >= G4_PREMISE
        if ax == '土地改革':
            if len(sl) < G4_MIN_SERIES or not prem:
                verdict = f'判定不能（系列 {len(sl)} 本・前提 今の式の傾き {mo:+.5f}）'
            else:
                verdict = '通る' if mn <= G4_MED and fr >= G4_FRAC else '外れ'
            print(f'[G4/{ax}] 系列 {len(sl)} 本・今の式(oppOldIndex)の傾きの中央値 {mo:+.5f}（前提 ≧+{G4_PREMISE}）・減らす向きの傾きの|中央値| {mn:.5f}（≦{G4_MED}）・|傾き|≦{G4_EACH} の割合 {fr:.2f}（≧{G4_FRAC}） -> {verdict}')
        else:
            print(f'[G4/{ax}・記録のみ] 系列 {len(sl)} 本・今の式の傾きの中央値 {mo:+.5f}・減らす向きの傾きの|中央値| {mn:.5f}・|傾き|≦{G4_EACH} の割合 {fr:.2f}')
    # ---- G5 ----
    here = os.path.dirname(os.path.abspath(__file__))
    r2 = os.path.join(here, '..', 'rs2547', 'read_rs2.py')
    cmd = [sys.executable, r2, d] + (['--base', base] if base else []) + (['--null', ','.join(nulls)] if nulls else [])
    env = dict(os.environ); env['PYTHONUTF8'] = '1'
    res = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', env=env)
    print('[G5] read_rs2.py（変更なし）の M1・M2・M2b・M3・M5 の行：')
    for line in res.stdout.splitlines():
        if line.startswith(('[M1]', '[M2]', '[M2b]', '[M3]', '[M5]', '[P1]')):
            print('    ' + line)
    import importlib.util
    spec = importlib.util.spec_from_file_location('read_rs2', r2)
    mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
    cb = mod.land_transitions(ref)
    print(f'[G5/M5] 土地改革 0->1以上 の件数 {cb}（閾値 {M5_THR}＝392＋2×幅の下限10） -> {"通る" if cb <= M5_THR else "外れ"}')
    # ---- 記録 ----
    def law_moves(refd):
        dec = defaultdict(int); roundtrip = defaultdict(int); inc = defaultdict(int); where = []
        for s, rows in refd.items():
            byn = defaultdict(list)
            for r in rows:
                byn[r['nation']].append(r)
            for nat, rr in byn.items():
                rr.sort(key=lambda r: int(r['quarter']))
                for k in ('serfLawCode', 'landReformLawCode'):
                    seen_dec = False
                    for a, b in zip(rr, rr[1:]):
                        va, vb = int(a[k]), int(b[k])
                        if vb < va:
                            dec[k] += 1; seen_dec = True
                            where.append((s, nat, k, int(b['quarter']), b.get('polityCode', '')))
                        elif vb > va:
                            inc[k] += 1
                            if seen_dec:
                                roundtrip[k] += 1
        return dec, inc, roundtrip, where
    for tag, refd in (('腕 B', ref), ('基線', load(base, 'reform') if base else {}), *[(f'ヌル{i+1}', load(x, 'reform')) for i, x in enumerate(nulls)]):
        if not refd:
            continue
        dec, inc, rt, where = law_moves(refd)
        print(f'[R1] {tag}：法が戻った回数 {dict(dec)}・上がった回数 {dict(inc)}・戻った後にまた上がった回数 {dict(rt)}')
        if tag == '腕 B':
            print(f'    戻った例（種,国,法,四半期,polityCode）{where[:12]}')
            nat_cnt = defaultdict(int)
            for s, nat, k, q, p in where:
                nat_cnt[(nat, k)] += 1
            print(f'    戻った国×法の度数 {dict(sorted(nat_cnt.items(), key=lambda x: -x[1])[:15])}')
    def dem_share(rs):
        tot = defaultdict(int); hit = defaultdict(int)
        for rows in rs.values():
            for r in rows:
                if r.get('dem0Top', '') == '':
                    continue
                dec = int(r['year']) // 10 * 10
                tot[dec] += 1
                t = r['dem0Top'].split(';')
                if 'SerfEmancipation-' in t: hit[(dec, 's')] += 1
                if 'LandReform-' in t: hit[(dec, 'l')] += 1
        return tot, hit
    tb, hb = dem_share(rs2)
    tz, hz = dem_share(load(base, 'rs2')) if base else ({}, {})
    print('[R2] 旧支配層の要求の上位に 農奴解放を減らす/土地改革を減らす が入った行の割合（年代別）腕 B | 基線')
    for dec in sorted(tb):
        f = lambda h, t, k: (h.get((dec, k), 0) / t[dec]) if t.get(dec) else float('nan')
        print(f'    {dec}s n={tb[dec]}  serf- {f(hb, tb, "s"):.3f} | {f(hz, tz, "s"):.3f}   land- {f(hb, tb, "l"):.3f} | {f(hz, tz, "l"):.3f}')
    print('[R3] 減らす向きの強さの年代別の中央値（国×10年）：oppSerfDec / oppLandDec / oppOld')
    for nat in ('ロシア', 'プロイセン', 'オスマン', 'イギリス'):
        by = defaultdict(list)
        for rows in rs3.values():
            for r in rows:
                if r['nation'] == nat:
                    by[int(r['year']) // 10 * 10].append(r)
        for dec in sorted(by):
            rr = by[dec]
            m = lambda k: st.median(fl(r, k) for r in rr)
            print(f'    {nat} {dec}s n={len(rr)}  {m("oppSerfDecUsed"):.3f} / {m("oppLandDecUsed"):.3f} / {m("oppOldIndex"):.3f}')
    # ---- G7（事前登録 [11](d)）----
    def land_dem_share(rs):
        tot = 0; hit = 0
        for rows in rs.values():
            for r in rows:
                if r.get('dem0Top', '') == '':
                    continue
                tot += 1
                if 'LandReform-' in r['dem0Top'].split(';'):
                    hit += 1
        return (hit / tot if tot else float('nan')), tot, hit
    g7, g7n, g7h = land_dem_share(rs2)
    print(f'[G7] 旧支配層の要求の最上位に 土地改革を減らす が入った行の割合 {g7:.4f}（{g7h}/{g7n} 行・閾値 ＞{G7_THR}） -> {"通る" if g7 > G7_THR else "外れ"}')
    for tag, d0 in [('基線 m552', base)] + [(f'ヌル{i+1}', x) for i, x in enumerate(nulls)]:
        if d0:
            v, tn, th = land_dem_share(load(d0, 'rs2'))
            print(f'     比べる側 {tag}: {v:.4f}（{th}/{tn}）')
    def serf_dem_share(rs):
        tot = 0; hit = 0
        for rows in rs.values():
            for r in rows:
                if r.get('dem0Top', '') == '':
                    continue
                tot += 1
                if 'SerfEmancipation-' in r['dem0Top'].split(';'):
                    hit += 1
        return (hit / tot if tot else float('nan'))
    print(f'     〔記録〕農奴解放を減らす の割合 腕 B {serf_dem_share(rs2):.4f}' + ''.join(f' | {t} {serf_dem_share(load(d0, "rs2")):.4f}' for t, d0 in [("基線", base)] + [(f"ヌル{i+1}", x) for i, x in enumerate(nulls)] if d0))
    # ---- 記録(c)：危機・蜂起・内戦（rs3 の追加 4 列）----
    axes = parse_axes()
    LAND = axes.index('LandReform') + 1; SERF = axes.index('SerfEmancipation') + 1
    def crisis_stats(rs, seeds):
        eps = defaultdict(int); rows_lord = 0; revolts = 0; rev_eps = 0; nrows = 0
        for s in seeds:
            rows = rs.get(s)
            if not rows:
                continue
            byn = defaultdict(list)
            for r in rows:
                if r['nation'] in PICKED:
                    byn[r['nation']].append(r)
            for nat, rr in byn.items():
                rr.sort(key=lambda r: int(r['quarter']))
                prev_bloc = -1; prev_top = 0; prev_rev = -1; prev_cum = None
                for r in rr:
                    nrows += 1
                    bl = int(r['pendingCrisisBloc']); tp = int(r['pendingCrisisTop']); rv = int(r['revoltingBloc']); cum = int(r['factionRevoltsCum'])
                    if bl == 0:
                        rows_lord += 1
                        if prev_bloc != 0:
                            key = 'land-' if tp == -LAND else ('serf-' if tp == -SERF else ('land+' if tp == LAND else ('serf+' if tp == SERF else 'other')))
                            eps[key] += 1; eps['all'] += 1
                    if rv == 0 and prev_rev != 0:
                        rev_eps += 1
                    if prev_cum is not None and cum > prev_cum:
                        revolts += cum - prev_cum
                    prev_bloc = bl; prev_top = tp; prev_rev = rv; prev_cum = cum
        return dict(eps), rows_lord, revolts, rev_eps, nrows
    def show_crisis(tag, rs, seeds):
        eps, rl, rv, rve, nr = crisis_stats(rs, seeds)
        print(f'[R1c] {tag}（種 {min(seeds)}〜{max(seeds)}・挙げた国の行 {nr}）：旧支配層の危機の始まり {eps}・旧支配層の危機が立っていた行 {rl}・蜂起の累計の増分 {rv}・旧支配層が主体の内乱の始まり {rve}')
    seeds_b = sorted(rs3)
    show_crisis('腕 B', rs3, seeds_b)
    show_crisis('腕 B（種 1〜5）', rs3, [s for s in seeds_b if s <= 5])
    a3 = args[args.index('--a3') + 1] if '--a3' in args else None
    if a3:
        ra = load(a3, 'rs3')
        show_crisis('腕 A（旗 0）', ra, sorted(ra))
    over = sum(1 for r in allrows if fl(r, 'lordRentToIncomeShare') > 1.0)
    print(f'[R7] lordRentToIncomeShare>1 の行 {over}/{n}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
