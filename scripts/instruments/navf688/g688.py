# (688) 線 navf5：狙いの数字の読み手（読むだけ）。事前登録 measurements/navf688_20261030_prereg.txt の [3] G1・G2 と [4] の記録。
# 使い方：python -X utf8 -I scripts/instruments/navf688/g688.py <計器の出力フォルダ(…/ins)> [種の数=10] [種の起点=1] [--col <事象の列名>] [--recv <受け手の列名>]
#   事象の列（国〔World〕ごとの累計）の増分が正の四半期＝割譲で艦が動いた事象。送り手の艦の数（fleetUnitsStock）の四半期の落ち（前の四半期 − その四半期）÷ 増分 を比 ρ とする。
#   旗 0 の腕（A）：--col fleetUnitsMovedOutCededCum（割譲で艦ごと出た隻数）。旗 1 の腕（B）：--col fleetUnitsReturnedToCapitalCum（元の国の首都の州へ戻した隻数）。
#   受け手の列（旗 0 の腕のみ）：--recv fleetUnitsReceivedCededCum＝受け手の艦の数の四半期の増え ÷ 受け手の増分。
#   基線 m685 の生には新しい列が無い＝--col fleetUnitsMovedOutHomeCum（割譲＋分離独立などの全て）で動作確認だけできる（段0）。
import csv, sys, collections, statistics as st


def fnum(x):
    try: return float(x)
    except Exception: return float('nan')


def opt(name, dflt=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else dflt


def pos():
    out = []; skip = False
    for a in sys.argv[1:]:
        if skip: skip = False; continue
        if a in ('--col', '--recv'): skip = True; continue
        out.append(a)
    return out


args = pos(); D = args[0]; NS = int(args[1]) if len(args) > 1 else 10; SB = int(args[2]) if len(args) > 2 else 1
COL = opt('--col', 'fleetUnitsMovedOutCededCum'); RECV = opt('--recv')
SEVEN = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本']


def load(s):
    fh = open(f'{D}/stock_s{s}.tsv', encoding='utf-8-sig'); rd = csv.DictReader(fh, delimiter='\t')
    byq = collections.defaultdict(dict)
    for r in rd:
        t = r['time']
        if t.startswith('q') and t[1:].isdigit(): byq[int(t[1:])][r['nation']] = r
    return byq


rho = []; rrho = []; ev = []; paid_pre = []; paid_post = []
for s in range(SB, SB + NS):
    try: byq = load(s)
    except FileNotFoundError: continue
    qs = sorted(byq); prev = {}
    for q in qs:
        cur = byq[q]
        for n, r in cur.items():
            if COL not in r: print('列が無い:', COL); sys.exit(1)
            c = fnum(r[COL]); pp = prev.get(n)
            if pp is not None:
                p, pst = pp; d = c - p
                if d > 0.5:
                    drop = pst - fnum(r['fleetUnitsStock'])
                    ev.append((s, q, int(fnum(r['year'])), n, round(d, 1), round(drop, 1), round(drop / d, 3)))
                    rho.append(drop / d)
                    if RECV and RECV in r:
                        pass
            prev[n] = (c, fnum(r['fleetUnitsStock']))
    if RECV:
        prevr = {}
        for q in qs:
            cur = byq[q]
            for n, r in cur.items():
                if RECV not in r: print('列が無い:', RECV); sys.exit(1)
                c = fnum(r[RECV]); pp = prevr.get(n)
                if pp is not None and c - pp[0] > 0.5:
                    rrho.append((fnum(r['fleetUnitsStock']) - pp[1]) / (c - pp[0]))
                prevr[n] = (c, fnum(r['fleetUnitsStock']))
print(f'事象の列 {COL}：事象 {len(ev)} 件（種 {SB}〜{SB + NS - 1}）')
if rho:
    rs = sorted(rho); print(f'送り手の艦の落ち÷増分 ρ：中央値 {st.median(rs):.3f}・四分位 {rs[len(rs) // 4]:.3f}〜{rs[3 * len(rs) // 4]:.3f}・ρ≥0.5 の事象 {sum(1 for x in rs if x >= 0.5)}/{len(rs)}')
if rrho:
    rs = sorted(rrho); print(f'受け手の艦の増え÷増分：中央値 {st.median(rs):.3f}・四分位 {rs[len(rs) // 4]:.3f}〜{rs[3 * len(rs) // 4]:.3f}（{len(rs)} 件）')
by = collections.defaultdict(lambda: [0, 0.0]); 
for e in ev: by[e[3]][0] += 1; by[e[3]][1] += e[4]
for n in SEVEN: print(n, '事象', by[n][0], '隻数', round(by[n][1]))
