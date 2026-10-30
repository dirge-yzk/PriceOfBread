# (688) 線 navf5：狙いの数字の読み手（読むだけ）。事前登録 measurements/navf688_20261030_prereg.txt の [3] G1・G3 と [追記 3]。
# 使い方：python -X utf8 -I scripts/instruments/navf688/g688.py <計器の出力フォルダ(…/ins)> [種の数=10] [種の起点=1] --flag <0|1>
#   事象の列（国ごとの累計）の増分 d が 0.5 隻より大きい四半期＝割譲で艦が動いた事象（床 0.5 隻は事前登録の追記 3 で絶対値として書いた）。
#   ρ＝（送り手の fleetUnitsStock の、その国の前の行 − その行）÷ d。
#   --flag 0（腕 A・A11）：事象の列＝fleetUnitsMovedOutCededCum。判定 G1-A＝ρ の中央値 ≧ 0.75（全事象と大事象の両方）・事象 20 件以上。
#   --flag 1（腕 B・B11）：事象の列＝fleetUnitsReturnedToCapitalCum。判定 G1-B＝ρ の中央値 ≦ 0.35（全事象と大事象の両方）・事象 20 件以上。
#                         判定 G3＝fleetUnitsReceivedCededCum が全ての行で 0。
#   大事象＝d ÷ 送り手の前の行の fleetUnitsStock ≧ 0.10（四半期の摩耗 約 1.5% の 6〜7 倍以上＝摩耗と購入の雑音に埋もれにくい事象。追記 3）。
#   旗 0 の腕でも fleetUnitsReceivedCededCum の増分を数えて記録する（G3 は旗 0 では列挙のみ）。
#   --col <列名> で事象の列を上書きできる（基線 m685 の生には新しい列が無い＝段0 の動作確認は --col fleetUnitsMovedOutHomeCum）。
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
        if a in ('--col', '--flag'): skip = True; continue
        out.append(a)
    return out


args = pos(); D = args[0]; NS = int(args[1]) if len(args) > 1 else 10; SB = int(args[2]) if len(args) > 2 else 1
FLAG = opt('--flag')
COL = opt('--col', 'fleetUnitsReturnedToCapitalCum' if FLAG == '1' else 'fleetUnitsMovedOutCededCum')
RECV = 'fleetUnitsReceivedCededCum'
SEVEN = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本']
FLOOR = 0.5; BIG = 0.10; MINN = 20


def load(s):
    fh = open(f'{D}/stock_s{s}.tsv', encoding='utf-8-sig'); rd = csv.DictReader(fh, delimiter='\t')
    byq = collections.defaultdict(dict)
    for r in rd:
        t = r['time']
        if t.startswith('q') and t[1:].isdigit(): byq[int(t[1:])][r['nation']] = r
    return byq


ev = []; recv_pos_rows = 0; recv_rows = 0; seeds_read = 0
for s in range(SB, SB + NS):
    try: byq = load(s)
    except FileNotFoundError: continue
    seeds_read += 1
    prev = {}
    for q in sorted(byq):
        for n, r in byq[q].items():
            if COL not in r: print('列が無い:', COL); sys.exit(1)
            c = fnum(r[COL]); st_now = fnum(r['fleetUnitsStock']); rv = fnum(r[RECV]) if RECV in r else 0.0
            recv_rows += 1
            if rv > 0: recv_pos_rows += 1
            pp = prev.get(n)
            if pp is not None:
                p, pst = pp; d = c - p
                if d > FLOOR:
                    drop = pst - st_now
                    ev.append((s, q, int(fnum(r['year'])), n, round(d, 1), round(drop, 1), drop / d, d / pst if pst > 0 else float('inf')))
            prev[n] = (c, st_now)


def med(v): return st.median(v) if v else float('nan')


def quart(v):
    s = sorted(v); return (s[len(s) // 4], s[3 * len(s) // 4]) if s else (float('nan'), float('nan'))


allr = [e[6] for e in ev]; bigr = [e[6] for e in ev if e[7] >= BIG]
print(f'腕の旗 {FLAG}・事象の列 {COL}：種 {SB}〜{SB + NS - 1}（読めた種 {seeds_read}）・事象（d>{FLOOR}隻）{len(ev)} 件・大事象（d÷前の艦 ≧{BIG}）{len(bigr)} 件')
for lab, v in (('全事象', allr), ('大事象', bigr)):
    if v:
        q1, q3 = quart(v)
        print(f'  ρ {lab}：中央値 {med(v):.3f}・四分位 {q1:.3f}〜{q3:.3f}・ρ≥0.5 の事象 {sum(1 for x in v if x >= 0.5)}/{len(v)}')
print(f'受け手の列 {RECV}：全 {recv_rows} 行のうち正の行 {recv_pos_rows}')
if FLAG in ('0', '1'):
    ok_n = len(allr) >= MINN and len(bigr) >= MINN
    if FLAG == '0':
        g1 = med(allr) >= 0.75 and med(bigr) >= 0.75
        print(f'G1-A（ρ 中央値 ≧0.75・全事象と大事象）：{"通過" if g1 else "外れ"}・件数 {"足りる" if ok_n else "不足（判定不能）"}')
    else:
        g1 = med(allr) <= 0.35 and med(bigr) <= 0.35
        g3 = recv_pos_rows == 0
        print(f'G1-B（ρ 中央値 ≦0.35・全事象と大事象）：{"通過" if g1 else "外れ"}・件数 {"足りる" if ok_n else "不足（判定不能）"}')
        print(f'G3（受け手の列が全行 0）：{"通過" if g3 else "外れ"}')
by = collections.defaultdict(lambda: [0, 0.0])
for e in ev: by[e[3]][0] += 1; by[e[3]][1] += e[4]
for n in SEVEN: print(n, '事象', by[n][0], '隻数', round(by[n][1]))
if '--list' in sys.argv:
    for e in ev: print(e)
