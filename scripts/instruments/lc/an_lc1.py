# (365) 後半: 建設・保守の機関車の需要の門の腕 A・B・N1・N2（計器つき10種）を事前登録 lc_20261016_prereg.txt の線で読む。読むだけ。
# 使い方: PYTHONUTF8=1 python an_lc1.py <作業フォルダ(例 D:/PoB_work/lc)> <出力txt> [腕の並び=A,B,N1,N2]
#   各腕の ins フォルダ = <作業フォルダ>/out_<腕>_10/ins
# 線: M1a・M2・H1・H3（採否）／M1b・H2・H4・H5・R1・R2・R4・R5・R6・補給崩壊・上位1国の寄与（記録）。
import csv, sys, os, collections, statistics as st

W = sys.argv[1]
OUT = sys.argv[2]
ARMS = sys.argv[3].split(',') if len(sys.argv) > 3 else ['A', 'B', 'N1', 'N2']
SEEDS = range(1, 11)
HIST = {  # 国: (最初の鉄道の年, 確かさ)  S=出典あり  M=AI の記憶（線に使わない）
    'イギリス': (1825, 'S'), 'フランス': (1827, 'S'), 'アメリカ': (1830, 'M'), 'プロイセン': (1838, 'M'), 'バイエルン': (1835, 'S'),
    'ネーデルラント': (1839, 'S'), 'ロシア': (1837, 'S'), 'オーストリア': (1837, 'S'), 'スウェーデン': (1856, 'S'), 'デンマーク': (1847, 'M'),
    'スペイン': (1848, 'S'), 'ポルトガル': (1856, 'M'), 'オスマン': (1856, 'M'), '日本': (1872, 'S'), '清': (1876, 'S'), 'シャム': (1893, 'S'),
}
SNAT = [n for n, v in HIST.items() if v[1] == 'S']
INDUSTRIAL = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', 'ネーデルラント', 'スペイン', 'オスマン']
UK = 'イギリス'
OPEN_YEAR_NONE = 1951


def path(arm, name, s):
    return f"{W}/out_{arm}_10/ins/{name}_s{s}.tsv"


def rows(arm, name, s):
    with open(path(arm, name, s), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        idx = {c: i for i, c in enumerate(h)}
        for line in r:
            yield idx, line


def med(v):
    v = list(v)
    return st.median(v) if v else None


def derived(n):
    return n.startswith(('共和制', '立憲王政', '統領制'))


out = []
P = out.append


def f(x, spec='.1f'):
    return '-' if x is None else format(x, spec)


# ---- 読み込み（腕ごと）----
D = {}
for arm in ARMS:
    a = {'open': {}, 'steam': {}, 'locorow': {}, 'rail_first': {}, 'rail_q': {}}
    for s in SEEDS:
        for idx, ln in rows(arm, 'disc', s):
            if ln[idx['kind']] == 'E':
                key = (s, ln[idx['nation']])
                rw = ln[idx['row']]
                if rw == '機関車':
                    a['open'][key] = (int(ln[idx['quarter']]), int(ln[idx['year']]))
                elif rw == '蒸気機関':
                    a['steam'][key] = (int(ln[idx['quarter']]), int(ln[idx['year']]))
        for idx, ln in rows(arm, 'stock', s):
            t = ln[idx['time']]
            if t.startswith('q') and float(ln[idx['railCapacityStock']]) > 0:
                key = (s, ln[idx['nation']])
                if key not in a['rail_first']:
                    a['rail_first'][key] = int(ln[idx['year']])
    D[arm] = a


def ycy(arm, n, kind='open'):
    """H1 の規則: 10 種の中央値・開かない種は 1951。"""
    return med([D[arm][kind][(s, n)][1] if (s, n) in D[arm][kind] else OPEN_YEAR_NONE for s in SEEDS])


allnat = sorted({n for arm in ARMS for (_, n) in D[arm]['open']} | {n for arm in ARMS for (_, n) in D[arm]['steam']})
allnat = [n for n in allnat if not derived(n)]

P('# (365) 後半: 機関車の需要の門の腕を事前登録の線で読む（計器つき 10 種・読むだけ）')
P('腕: ' + ' / '.join(ARMS) + '（入力 ' + W + '/out_<腕>_10/ins）')
P('')

# ---- 床 ----
A = 'A'
floor_h1 = {}
floor_h3 = {}
for n in set(SNAT) | set(INDUSTRIAL):
    for kind, tbl, mn in (('open', floor_h1, 5.0), ('steam', floor_h3, 3.0)):
        if 'N1' in D and 'N2' in D:
            dd = max(abs(ycy('N1', n, kind) - ycy('A', n, kind)), abs(ycy('N2', n, kind) - ycy('A', n, kind)))
        else:
            dd = 0.0
        tbl[n] = max(mn, 2 * dd)

# ---- H1 ----
P('## H1（採否）出典つき 11 国の最初の鉄道の年（機関車の行・10 種の中央・開かない種は 1951）。d_c=|y_c−史実|。悪化＝d_c(腕)−d_c(A) が床を超える')
P('国\t史実\ty(A)\ty(B)\ty(N1)\ty(N2)\td(A)\td(B)\tB−A(d)\t床\t悪化\t開いた種 A/B\t悪化の余地(最大)')
H1 = {}
worse = []
norooms = []
for arm in ARMS:
    if arm in ('A', 'N1', 'N2'):
        continue
    H1[arm] = []
for n in SNAT:
    h = HIST[n][0]
    yA, yB = ycy('A', n), ycy('B', n) if 'B' in D else None
    yN1 = ycy('N1', n) if 'N1' in D else None
    yN2 = ycy('N2', n) if 'N2' in D else None
    dA, dB = abs(yA - h), (abs(yB - h) if yB is not None else None)
    fl = floor_h1[n]
    maxd = max(abs(yA - h), abs(OPEN_YEAR_NONE - h))   # B が後ろへだけ動くと見たときの最大の d
    room = maxd - dA
    wr = (dB - dA) > fl if dB is not None else None
    if wr:
        worse.append(n)
    if room <= fl:
        norooms.append(n)
    nA = sum(1 for s in SEEDS if (s, n) in D['A']['open'])
    nB = sum(1 for s in SEEDS if (s, n) in D['B']['open']) if 'B' in D else 0
    P(f'{n}\t{h}\t{f(yA)}\t{f(yB)}\t{f(yN1)}\t{f(yN2)}\t{f(dA)}\t{f(dB)}\t{f(dB - dA) if dB is not None else "-"}\t{fl:.1f}\t{"悪化" if wr else "-"}\t{nA}/{nB}\t{room:.1f}')
P(f'H1 の判定（B）: 悪化した国 {len(worse)}（{",".join(worse) or "なし"}）→ {"合格(≤3)" if len(worse) <= 3 else "外れ(>3)"}')
P(f'悪化の余地が床以下の国（実効の分母から外れる）: {len(norooms)} {",".join(norooms)}')
if 'B' in D:
    dAs = [abs(ycy('A', n) - HIST[n][0]) for n in SNAT]
    dBs = [abs(ycy('B', n) - HIST[n][0]) for n in SNAT]
    eA = [ycy('A', n) - HIST[n][0] for n in SNAT]
    eB = [ycy('B', n) - HIST[n][0] for n in SNAT]
    P(f'H2（記録）符号つき誤差の中央 y−史実: A {med(eA):+.1f}・B {med(eB):+.1f}／|.| の中央: A {med(dAs):.1f}・B {med(dBs):.1f}／N1 {med([abs(ycy("N1", n) - HIST[n][0]) for n in SNAT]):.1f}・N2 {med([abs(ycy("N2", n) - HIST[n][0]) for n in SNAT]):.1f}')
P('')

# ---- H3 ----
P('## H3（採否）工業国の蒸気機関の行の最初の年（10 種の中央・開かない種は 1951）。B が A より床(max(3, 2×max|N−A|))を超えて遅れた国が 0')
P('国\ty(A)\ty(B)\ty(N1)\ty(N2)\tB−A\t床\t遅れ')
h3_late = []
for n in INDUSTRIAL:
    yA, yB = ycy('A', n, 'steam'), ycy('B', n, 'steam')
    fl = floor_h3[n]
    late = (yB - yA) > fl
    if late:
        h3_late.append(n)
    P(f'{n}\t{f(yA)}\t{f(yB)}\t{f(ycy("N1", n, "steam"))}\t{f(ycy("N2", n, "steam"))}\t{yB - yA:+.1f}\t{fl:.1f}\t{"遅れ" if late else "-"}')
P(f'H3 の判定（B）: 遅れた国 {len(h3_late)}（{",".join(h3_late) or "なし"}）→ {"合格(0)" if not h3_late else "外れ"}')
P('')

# ---- locogate_s: M1a・M2・R2・R6 ----
Q = {}   # (arm,s) -> list of dict rows
def load_lg(arm, s):
    res = {}
    for idx, ln in rows(arm, 'locogate', s):
        g = lambda c: float(ln[idx[c]])
        res[(ln[idx['nation']], int(ln[idx['quarter']]))] = {c: g(c) for c in ('ownLocoRow', 'ownGridRow', 'regionMonths', 'locoWantUnits', 'locoGotUnits', 'locoPaid', 'locoBlockedUnits',
                                                           'railWantUnits', 'railGotUnits', 'railPaid', 'railBlockedUnits', 'gridWantUnits', 'gridGotUnits', 'maintenanceUnits')}
    return res

P('## M1a（採否・形の確かめ）B の locogate_s: ownLocoRow＝0 の行の locoWantUnits・locoGotUnits・locoPaid が厳密に 0')
LG = {arm: {s: load_lg(arm, s) for s in SEEDS} for arm in ('A', 'B')}
rowsB0 = [r for s in SEEDS for r in LG['B'][s].values() if r['ownLocoRow'] == 0.0]
rowsBm = [r for s in SEEDS for r in LG['B'][s].values() if 0.0 < r['ownLocoRow'] < 1.0]
sw = sum(r['locoWantUnits'] for r in rowsB0); sg = sum(r['locoGotUnits'] for r in rowsB0); sp = sum(r['locoPaid'] for r in rowsB0)
bl = sum(r['locoBlockedUnits'] for r in rowsB0)
P(f'B の ownLocoRow＝0 の国×四半期 {len(rowsB0)} 件（中間の割合 {len(rowsBm)} 件は数えない）: locoWantUnits {sw:.6g}・locoGotUnits {sg:.6g}・locoPaid {sp:.6g}（blocked＝消えた量 {bl:.6g}）→ {"合格(厳密0)" if sw == 0.0 and sg == 0.0 and sp == 0.0 else "外れ＝形のバグ"}')
P('')
P('## M2（採否）A で ownLocoRow＝0 の国×四半期の機関車の支払いが正（何もしない機構が緑にならない）')
rowsA0 = [r for s in SEEDS for r in LG['A'][s].values() if r['ownLocoRow'] == 0.0]
pA0 = sum(r['locoPaid'] for r in rowsA0); pAall = sum(r['locoPaid'] for s in SEEDS for r in LG['A'][s].values())
P(f'A の ownLocoRow＝0 の国×四半期 {len(rowsA0)} 件: locoPaid {pA0:.6g}（A の全期間の locoPaid {pAall:.6g}・割合 {pA0 / pAall:.1%}）→ {"合格(>0)" if pA0 > 0 else "外れ"}')
rA0r = sum(r['railPaid'] for r in rowsA0); rAr = sum(r['railPaid'] for s in SEEDS for r in LG['A'][s].values())
P(f'（記録）A の ownLocoRow＝0 の railPaid {rA0r:.6g}（A 全体 {rAr:.6g}・割合 {rA0r / rAr:.1%}）')
P('')

# 同じ (seed,nation,quarter) で A の ownLocoRow=0 だった行を B で比べる
P('## R6（記録）A で ownLocoRow＝0 だった同じ国×四半期の B との比（保守の量＝運行要員の需要の元・機関車の需要）')
keysA0 = [(s, k) for s in SEEDS for k, r in LG['A'][s].items() if r['ownLocoRow'] == 0.0]
for col in ('maintenanceUnits', 'locoWantUnits', 'locoPaid', 'railWantUnits', 'railPaid'):
    sa = sum(LG['A'][s][k][col] for s, k in keysA0)
    sb = sum(LG['B'][s][k][col] for s, k in keysA0 if k in LG['B'][s])
    P(f'  {col}: A {sa:.6g}・B {sb:.6g}・B÷A {sb / sa if sa else float("nan"):.3f}')
P('  （機関士＝保守×0.4・火夫＝保守×0.3 を掛ける前の量。B÷A>1 なら門で要員の需要が増えた）')
P('')
P('## R2（記録）A の送電網: ownGridRow＝0 の国×四半期の購入（自国の電力の解禁に縛られているか）')
g0 = [r for s in SEEDS for r in LG['A'][s].values() if r['ownGridRow'] == 0.0]
ga = [r for s in SEEDS for r in LG['A'][s].values()]
P(f'ownGridRow＝0 の国×四半期 {len(g0)}/{len(ga)}: gridWantUnits {sum(r["gridWantUnits"] for r in g0):.6g}・gridGotUnits {sum(r["gridGotUnits"] for r in g0):.6g}（A 全体 want {sum(r["gridWantUnits"] for r in ga):.6g}・got {sum(r["gridGotUnits"] for r in ga):.6g}）')
P('')

# ---- M1b: 接触 ----
P('## M1b（記録）disccontact_s の行＝機関車で買い手が機関車の行を未解禁の四半期の接触（10 種の合計）')
cont = {}
uk_sell = {}
for arm in ('A', 'B'):
    tot = pre = 0.0; uks = 0.0
    for s in SEEDS:
        for idx, ln in rows(arm, 'disccontact', s):
            if ln[idx['row']] != '機関車':
                continue
            v = float(ln[idx['valuePerQuarter']]); b = ln[idx['buyer']]; q = int(ln[idx['quarter']])
            tot += v
            oq = D[arm]['open'].get((s, b))
            if oq is None or q < oq[0]:
                pre += v
            if ln[idx['seller']] == UK:
                uks += v
    cont[arm] = (tot, pre); uk_sell[arm] = uks
P(f'A: 全体 {cont["A"][0]:.0f}・未解禁の買い手 {cont["A"][1]:.0f}／B: 全体 {cont["B"][0]:.0f}・未解禁の買い手 {cont["B"][1]:.0f}（B÷A {cont["B"][1] / cont["A"][1]:.3%}）→ {"5% 以下" if cont["B"][1] <= 0.05 * cont["A"][1] else "5% 超＝残りの運び手が多い(機関車の財以外・形のバグではない)"}')
P(f'R5（記録）英が売り手の機関車の接触の合計: A {uk_sell["A"]:.0f}・B {uk_sell["B"]:.0f}')
P('')

# ---- H4/R4/H5 ----
P('## H4・R4（記録）全ての国: 機関車の行の年(10 種の中央・開かない種は 1951)と開かない種の数。A・B・N1・N2')
P('国\t' + '\t'.join(f'y({a})\t開いた種({a})' for a in ARMS))
for n in allnat:
    ln = [n]
    for arm in ARMS:
        ln += [f(ycy(arm, n)), f'{sum(1 for s in SEEDS if (s, n) in D[arm]["open"])}/10']
    P('\t'.join(ln))
P('')
P('R4: 機関車の行が開いた国×種: ' + '・'.join(f'{a} {len(D[a]["open"])}' for a in ARMS))
P('H4: 1950 年まで蒸気機関を解禁しない国×種（disc_s に現れた国の中で開かなかった）: ' + '・'.join(f'{a} {sum(1 for n in allnat for s in SEEDS if (s, n) in {k for k in D[a]["open"]} and (s, n) not in D[a]["steam"])}' for a in ARMS))
P('')
P('H4: 種ごとの最初に機関車の行を開いた国と年（A／B）')
for s in SEEDS:
    r = []
    for arm in ('A', 'B'):
        c = [(v[0], k[1], v[1]) for k, v in D[arm]['open'].items() if k[0] == s]
        c.sort()
        r.append(f'{arm}: {c[0][1]} {c[0][2]}（Q{c[0][0]}）' if c else f'{arm}: -')
    P(f'  種{s}\t' + '\t'.join(r))
P('')
P('H5（記録・出典つき 11 国）鉄道容量の最初の年(10 種の中央・出ない種は 1951)と行の年との差（A／B）')
for n in SNAT:
    r = []
    for arm in ('A', 'B'):
        rc = med([D[arm]['rail_first'].get((s, n), OPEN_YEAR_NONE) for s in SEEDS])
        r.append(f'{arm}: 容量 {rc} 行 {ycy(arm, n)} 差 {rc - ycy(arm, n):+.1f}')
    P(f'  {n}\t' + '\t'.join(r))
P('')

# ---- R1: cons_s の窓 ----
P('## R1（記録）建設の単位 builtUnitsPerMonth（cons_s・国の合計・四半期）: B の行が開いた四半期を 0 として ±4 四半期の窓（A と同じ国×種×四半期の比）')
def load_cons(arm, s):
    t = collections.defaultdict(float)
    for idx, ln in rows(arm, 'cons', s):
        v = float(ln[idx['builtUnitsPerMonth']])
        if v == v:
            t[(ln[idx['nation']], int(ln[idx['quarter']]))] += v
    return t
CB = {arm: {s: load_cons(arm, s) for s in SEEDS} for arm in ('A', 'B')}
maxq = max((k[1] for arm in CB for s in SEEDS for k in CB[arm][s]), default=0)
P(f'cons_s の最後の四半期 Q{maxq}（計器の出力の範囲＝それより後に行が開いた国は窓に入らない）')
P('オフセット\t件数\tB÷A の中央\tB 平均\tA 平均')
for k in range(-4, 5):
    rat = []; bs = []; as_ = []
    for (s, n), (q0, y0) in D['B']['open'].items():
        q = q0 + k
        b = CB['B'][s].get((n, q)); a = CB['A'][s].get((n, q))
        if b is not None and a is not None and a > 0:
            rat.append(b / a); bs.append(b); as_.append(a)
    P(f'{k:+d}\t{len(rat)}\t{f(med(rat), ".3f")}\t{f(sum(bs) / len(bs) if bs else None, ".3f")}\t{f(sum(as_) / len(as_) if as_ else None, ".3f")}')
closedB = [(s, n, q) for s in SEEDS for (n, q) in CB['B'][s] if ((s, n) not in D['B']['open'] or q < D['B']['open'][(s, n)][0])]
sb = sum(CB['B'][s][(n, q)] for s, n, q in closedB); sa = sum(CB['A'][s].get((n, q), 0.0) for s, n, q in closedB)
P(f'B の行が開く前の国×四半期（cons_s の範囲内）{len(closedB)} 件: 建設の単位の合計 B {sb:.6g}・A {sa:.6g}・B÷A {sb / sa if sa else float("nan"):.3f}')
P('')

# ---- 補給崩壊 ----
P('## 補給崩壊（人口 100 以上の国×四半期・supplyFillShare＜0.3・10 種の合計）と千件級の種の数・種ごとの上位 1 国の寄与')
coll = {}
for arm in ARMS:
    per = {}
    top = {}
    for s in SEEDS:
        c = collections.Counter()
        for idx, ln in rows(arm, 'nation', s):
            try:
                fill = float(ln[idx['supplyFillShare']]); pop = float(ln[idx['populationK']])
            except ValueError:
                continue
            if fill == fill and fill < 0.3 and pop >= 100:
                c[ln[idx['nation']]] += 1
        per[s] = sum(c.values())
        top[s] = c.most_common(1)[0] if c else ('-', 0)
    coll[arm] = per
    P(f'{arm}: 合計 {sum(per.values())}・千件級(≥1000)の種 {sum(1 for v in per.values() if v >= 1000)}/10・種ごと ' + ' '.join(str(per[s]) for s in SEEDS))
    P('    上位1国（種ごと）: ' + ' '.join(f'{top[s][0]}{top[s][1]}' for s in SEEDS))
tA, tB = sum(coll['A'].values()), sum(coll['B'].values())
nulls = [sum(coll[a].values()) for a in ('N1', 'N2') if a in coll]
P(f'B÷A {tB / tA if tA else float("nan"):.2f}（倍以上の線＝2）・B÷最大のヌル {tB / max(nulls) if nulls else float("nan"):.2f}（記録）')
P('')

open(OUT, 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print('\n'.join(out))
