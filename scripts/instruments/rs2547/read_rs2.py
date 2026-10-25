#!/usr/bin/env python
"""(547) 線 rs2 の読み手：事前登録 measurements/rs2547_20261025_prereg.txt の [5]（P1・M1・M2・M2b・M3）と [6]（R1〜R4・R6）を読む。
使い方：PYTHONUTF8=1 python scripts/instruments/rs2547/read_rs2.py <計器の出力フォルダ（腕 B の ins）> [--base <基線の ins>] [--a0 <旗 0 の計器つき腕 A0 の ins>] [--null <ヌル腕の ins をコンマ区切り>]
追記 3（事前登録 [11]）：P1 と M6 は --a0（旗 0＝処置していない世界）で読む。M5 は --base と --null の reform 表から閾値を作る。
入力：<ins>/rs2_s*.tsv（InstrumentsRs2.cs）と <ins>/reform_s*.tsv。--base があれば基線の reform_s*.tsv も同じ読みで並べる（R1・R2・R6）。
限界：連続した四半期の行がある系列（挙げた国）だけを読む（年末だけの国は 4 四半期おき＝連続でない）。閾値は事前登録のとおり（結果を見て直さない）。
"""
import csv, glob, os, sys, statistics as st
from collections import defaultdict

P1_MIN = 0.008; M1_MED = 0.004; M1_EACH = 0.008; M1_FRAC = 0.80; M2_MAX = 0.40; M2B_P99 = 0.10; MIN_RUN = 40


def read_tsv(path):
    with open(path, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def load(d, prefix):
    out = {}
    for fp in glob.glob(os.path.join(d, prefix + '_s*.tsv')):
        out[int(os.path.basename(fp)[len(prefix) + 2:-4])] = read_tsv(fp)
    return dict(sorted(out.items()))


def series(rows):
    """国 -> 四半期順の行。同じ国名で四半期が重複する行は後ろを捨てる。"""
    d = defaultdict(dict)
    for r in rows:
        d[r['nation']].setdefault(int(r['quarter']), r)
    return {n: [v[q] for q in sorted(v)] for n, v in d.items()}


def runs(rows, key):
    """連続した四半期（差 1）で key(行) が同じ値の区間に分ける。"""
    cur = []
    for r in rows:
        if cur and (int(r['quarter']) != int(cur[-1]['quarter']) + 1 or key(r) != key(cur[-1])):
            yield cur
            cur = []
        cur.append(r)
    if cur:
        yield cur


def slope(xs, ys):
    n = len(xs)
    if n < 2:
        return float('nan')
    mx = sum(xs) / n
    my = sum(ys) / n
    den = sum((x - mx) ** 2 for x in xs)
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / den if den > 0 else float('nan')


def fl(r, k):
    try:
        return float(r[k])
    except ValueError:
        return float('nan')


def pctl(v, p):
    v = sorted(v)
    if not v:
        return float('nan')
    return v[min(len(v) - 1, int(round(p * (len(v) - 1))))]


def first_q(rows, key, thr):
    for r in rows:
        if int(r[key]) >= thr:
            return int(r['quarter'])
    return None


def reform_report(tag, refd):
    print(f'[R1/R2/R6] {tag}')
    none_q = tot_q = 0
    polity_at = defaultdict(int)
    land_first = []
    for nat in ('ロシア', 'プロイセン', 'オスマン'):
        lim, full = [], []
        for s, rows in refd.items():
            ser = series(rows).get(nat)
            if not ser:
                continue
            lim.append('初期' if int(ser[0]['serfLawCode']) >= 1 else first_q(ser, 'serfLawCode', 1))
            full.append(first_q(ser, 'serfLawCode', 2))
        print(f'    {nat} 最初の限定の四半期 {lim}  最初の全面 {full}')
    for s, rows in refd.items():
        for n, ser in series(rows).items():
            for a, b in zip(ser, ser[1:]):
                if int(a['serfLawCode']) == 0 and int(b['serfLawCode']) >= 1:
                    polity_at[b['polityCode']] += 1
                if int(a['landReformLawCode']) == 0 and int(b['landReformLawCode']) >= 1:
                    land_first.append((s, n, int(b['quarter'])))
            for r in ser:
                tot_q += 1
                none_q += int(r['serfLawCode']) == 0
    if tot_q:
        print(f'    農奴解放なしの四半期の割合（挙げた国の全行）{none_q / tot_q:.3f}（{none_q}/{tot_q}）')
    print(f'    解放が〈なし〉から動いた時の polityCode の度数 {dict(polity_at)}')
    print(f'    土地改革の最初の段が通った件数 {len(land_first)}  例 {land_first[:6]}')


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 2
    d = args[0]
    base = args[args.index('--base') + 1] if '--base' in args else None
    rs2 = load(d, 'rs2')
    ref = load(d, 'reform')
    if not rs2:
        print('rs2_s*.tsv が無い')
        return 2
    print(f'== 読み手 read_rs2.py  入力={d}  種={list(rs2)}')
    bad = sum(1 for rows in rs2.values() for r in rows if fl(r, 'flagLevel') != 1.0)
    tot = sum(len(rows) for rows in rs2.values())
    print(f'[M3] flagLevel!=1 の行 {bad}/{tot} -> {"通る" if bad == 0 else "外れ"}')
    sl_old, sl_new, sl_rti, labels = [], [], [], []
    for s, rows in rs2.items():
        for n, ser in series(rows).items():
            for run in runs(ser, key=lambda r: r['serfLawCode']):
                if run[0]['serfLawCode'] != '0' or len(run) < MIN_RUN:
                    continue
                xs = [int(r['quarter']) / 4 for r in run]
                sl_old.append(slope(xs, [fl(r, 'oppOldIndex') for r in run]))
                sl_new.append(slope(xs, [fl(r, 'oppSerfNewIndex') for r in run]))
                sl_rti.append(slope(xs, [fl(r, 'lordRentToIncomeShare') for r in run]))
                labels.append((s, n, len(run)))
    print(f'[系列] 農奴解放が〈なし〉で連続 {MIN_RUN} 四半期以上の系列 {len(labels)} 本')
    if labels:
        mo = st.median(sl_old)
        mn = st.median(abs(x) for x in sl_new)
        frac = sum(1 for x in sl_new if abs(x) <= M1_EACH) / len(sl_new)
        print(f'[P1] 今の式の傾き(oppOldIndex/年)の中央値 {mo:+.5f}（前提＝+{P1_MIN}以上） -> {"届く" if mo >= P1_MIN else "届かない＝M1は判定不能"}')
        print(f'[M1] 新しい式の傾きの|中央値| {mn:.5f}（≦{M1_MED}）・|傾き|≦{M1_EACH} の系列の割合 {frac:.2f}（≧{M1_FRAC}） -> {"通る" if mn <= M1_MED and frac >= M1_FRAC else "外れ"}')
        print(f'[記録] lordRentToIncomeShare の傾きの中央値 {st.median(sl_rti):+.5f}/年（M1 を外したとき〈形〉か〈比の推移〉かを名指す材料）')
        for (s, n, ln), a, b, c in zip(labels, sl_old, sl_new, sl_rti):
            print(f'    s{s} {n} {ln}q  old {a:+.5f}  new {b:+.5f}  rentToIncome {c:+.5f}')
    else:
        print('[P1] 系列が無い -> 判定不能')
    worst = 0.0
    seen = 0
    for s, rows in rs2.items():
        for r in rows:
            if r['nation'] == 'ロシア' and int(r['quarter']) <= 3:
                worst = max(worst, abs(fl(r, 'oppSerfNewIndex') - fl(r, 'oppOldIndex')))
                seen += 1
    print(f'[M2] ロシア q0〜q3 の |新-旧| の最大 {worst:.4f}（≦{M2_MAX}）行数 {seen} -> {"通る" if seen and worst <= M2_MAX else "外れ/行なし"}')
    steps = []
    for s, rows in rs2.items():
        for n, ser in series(rows).items():
            for run in runs(ser, key=lambda r: (r['serfLawCode'], r['landReformLawCode'])):
                for a, b in zip(run, run[1:]):
                    steps.append(abs(fl(b, 'oppSerfNewIndex') - fl(a, 'oppSerfNewIndex')))
    if steps:
        p99 = pctl(steps, 0.99)
        print(f'[M2b] 法の段が同じ連続の |Δ新| 件数 {len(steps)}・99分位 {p99:.4f}（≦{M2B_P99}）・最大 {max(steps):.4f} -> {"通る" if p99 <= M2B_P99 else "外れ"}')
    print('[R4] 年代別の中央値（国×10年）：oppOld / oppSerfNew / oppLandNew / rentToIncome / lordWealth')
    for nat in ('ロシア', 'プロイセン', 'オスマン'):
        by = defaultdict(list)
        for rows in rs2.values():
            for r in rows:
                if r['nation'] == nat:
                    by[int(r['year']) // 10 * 10].append(r)
        for dec in sorted(by):
            rr = by[dec]

            def m(k):
                return st.median(fl(r, k) for r in rr)
            print(f'    {nat} {dec}s n={len(rr)}  {m("oppOldIndex"):.3f} / {m("oppSerfNewIndex"):.3f} / {m("oppLandNewIndex"):.3f} / {m("lordRentToIncomeShare"):.3f} / {m("lordWealthShare"):.3f}')
    reform_report('腕 B（入力）', ref)
    if base:
        reform_report('基線（--base）', load(base, 'reform'))
    extra(d, rs2, ref, base, args)
    return 0


def land_transitions(refd):
    """土地改革の法が 0 から 1 以上へ動いた件数（種×国の系列の隣り合う行・年末だけの国の行も含む）。"""
    n = 0
    for rows in refd.values():
        for ser in series(rows).values():
            for a, c in zip(ser, ser[1:]):
                if int(a['landReformLawCode']) == 0 and int(c['landReformLawCode']) >= 1:
                    n += 1
    return n


def none_runs(rs2):
    out = []
    for s, rows in rs2.items():
        for n, ser in series(rows).items():
            for run in runs(ser, key=lambda r: r['serfLawCode']):
                if run[0]['serfLawCode'] == '0' and len(run) >= MIN_RUN:
                    out.append((s, n, run))
    return out


def extra(d, rs2, ref, base, args):
    a0 = args[args.index('--a0') + 1] if '--a0' in args else None
    nulls = args[args.index('--null') + 1].split(',') if '--null' in args else []
    # ---- P1（追記 3：処置していない世界 A0 で読む）----
    if a0:
        r0 = load(a0, 'rs2')
        sl = []
        for s, n, run in none_runs(r0):
            xs = [int(r['quarter']) / 4 for r in run]
            sl.append(slope(xs, [fl(r, 'oppOldIndex') for r in run]))
        if sl:
            mo = st.median(sl)
            print(f'[P1/A0] 旗 0 の腕の〈なし〉連続 {MIN_RUN}q 以上の系列 {len(sl)} 本・今の式の傾きの中央値 {mo:+.5f}/年（前提＝+{P1_MIN}以上） -> {"届く" if mo >= P1_MIN else "届かない＝M1は判定不能"}')
        else:
            print('[P1/A0] 系列が無い -> 判定不能')
    # ---- M5（土地改革の第 1 段が通る件数）----
    cb = land_transitions(ref)
    cs = [land_transitions(load(x, 'reform')) for x in ([base] if base else []) + nulls]
    if cs:
        thr = max(cs) + 2 * (max(cs) - min(cs))
        print(f'[M5] 土地改革 0->1以上 の件数 腕 B {cb}・基線とヌル腕 {cs}・閾値 {thr}（最大＋幅の2倍） -> {"通る" if cb <= thr else "外れ"}')
    else:
        print(f'[M5] 腕 B の件数 {cb}（--base/--null が無く閾値なし）')
    # ---- M6（旧支配層の政治力×反対の強さ＝門の純支持の旧支配層の項の傾き）----
    def pull_slopes(rs):
        out = []
        for s, n, run in none_runs(rs):
            pts = [(int(r['quarter']) / 4, fl(r, 'pullSerfUsed')) for r in run if r.get('pullSerfUsed', '') != '']
            if len(pts) >= MIN_RUN // 2:
                out.append(slope([x for x, _ in pts], [y for _, y in pts]))
        return out
    sb = pull_slopes(rs2)
    sa = pull_slopes(load(a0, 'rs2')) if a0 else []
    if sb and sa:
        mb, ma = st.median(sb), st.median(sa)
        verdict = '判定不能（A0 の傾きが正でない）' if not ma > 0 else ('通る' if mb <= ma else '外れ')
        print(f'[M6] 政治力×反対の強さ(pullSerfUsed)の傾きの中央値 腕 B {mb:+.5f}（{len(sb)}本）・A0 {ma:+.5f}（{len(sa)}本）/年 -> {verdict}')
    else:
        print(f'[M6] 系列不足（B {len(sb)}・A0 {len(sa)}）-> 判定不能')
    # ---- 記録：地代÷所得 > 1 の行・旧支配層の要求の上位・解放が全面の国の要求 ----
    over = sum(1 for rows in rs2.values() for r in rows if fl(r, 'lordRentToIncomeShare') > 1.0)
    allr = sum(len(rows) for rows in rs2.values())
    print(f'[記録] lordRentToIncomeShare>1 の行 {over}/{allr}')

    def dem_freq(rs):
        tot = defaultdict(int)
        hit = defaultdict(int)
        for rows in rs.values():
            for r in rows:
                if r.get('dem0Top', '') == '' and r.get('power0', '') == '':
                    continue
                dec = int(r['year']) // 10 * 10
                tot[dec] += 1
                tops = r.get('dem0Top', '').split(';')
                if any(t.startswith('SerfEmancipation') for t in tops):
                    hit[(dec, 'serf')] += 1
                if any(t.startswith('LandReform') for t in tops):
                    hit[(dec, 'land')] += 1
        return tot, hit
    tb, hb = dem_freq(rs2)
    ta, ha = dem_freq(load(a0, 'rs2')) if a0 else ({}, {})
    print('[記録] 旧支配層の要求の上位に 農奴解放/土地改革 が入った行の割合（年代別）B | A0')
    for dec in sorted(tb):
        fb = lambda h, t, k: (h.get((dec, k), 0) / t[dec]) if t.get(dec) else float('nan')
        print(f'    {dec}s n={tb[dec]}  serf {fb(hb, tb, "serf"):.2f} | {fb(ha, ta, "serf"):.2f}   land {fb(hb, tb, "land"):.2f} | {fb(ha, ta, "land"):.2f}')


if __name__ == '__main__':
    sys.exit(main())
