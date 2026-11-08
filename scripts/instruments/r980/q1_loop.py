# (980) c88hr 問い 1・2：輸入の代金を払い手と〈手形→B1→決済①→門 0→新しい手形〉の型で分ける（読むだけ・エンジンを呼ばない・旗を読まない）
#   python -X utf8 -I q1_loop.py --ins <ins フォルダ> --seeds <a> <b> --out <出力ファイル> [--trace 種:国:四半期 ...]
# 行＝cearn_s の州×四半期。輸入の額＝importPaidPerQuarter を払いの時点の払い手の人数で分けた 3 列（Le1e12＝人の居ない払い手＝kind 2 の手形の払い・Mid・Gt1e9＝人の居る払い手）。
# 人の居ない払い手の額 E を、同じ行の印で分ける：
#   E1 型そのもの：estateDebtMovedPerQuarter ＞0（B1 が相続人へ移した）・heirDebtSettledPerQuarter ＞0（相続人が決済①で返した）・billWithHeirDebtClearings＝0（① の後に相続人の債務 ＞1e-9 が 1 度も無い＝(c1) の門は 1 度も閉じない）
#   E2 型＋門が閉じた清算あり：E1 と同じで billWithHeirDebtClearings ＞0
#   E3 移したが返していない：moved ＞0・heirSettled＝0
#   E4 移していない：moved＝0（人の居ない商人が自分の貯蓄で返した・最初の手形・旗 0 の盤面の全て）
# 範囲：全て・tier＝Colonial・F の区間（nation_s の priceLevel ≦0.4＋1e-6 が 4 四半期以上続く・gate971.py と同じ定義）の国×四半期・U5 の底の四半期（区間ごとに州の貯蓄の和が最小の四半期）
# 純＝outImport−inExport（入ってくる側を引く）。
import sys, os
a = sys.argv[1:]
def opt(k, n=1):
    i = a.index(k); v = a[i + 1:i + 1 + n]; del a[i:i + 1 + n]; return v
ins = opt('--ins')[0]; s0, s1 = map(int, opt('--seeds', 2)); out = opt('--out')[0]
tr = []
if '--trace' in a:
    i = a.index('--trace'); tr = a[i + 1:]; del a[i:]
tr = [(int(x.split(':')[0]), x.split(':')[1], int(x.split(':')[2])) for x in tr]
FLOOR = 0.4 + 1e-6; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def z(x): return x if x == x else 0.0
def runs(qs, k):
    o = []; st = pv = None
    for q in qs:
        if st is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: o.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: o.append((st, pv))
    return o
SC = ['all', 'colonial', 'F', 'Fbottom']
KEYS = ['outImport', 'inExport', 'paid', 'L', 'E', 'E1', 'E2', 'E3', 'E4', 'moved', 'heirSettled', 'heirSettled_E12', 'moved_E12', 'outOther', 'inCredit', 'oiE12', 'oiE1', 'rows', 'rowsE', 'rowsE1', 'rowsE2']
tot = {s: {k: 0.0 for k in KEYS} for s in SC}
perseed = {}
lines = []
TRC = ['quarter', 'year', 'nation', 'region', 'tier', 'popK', 'savingsStart', 'savings', 'outImportPerQuarter', 'inExportPerQuarter', 'inCreditPerQuarter', 'outOtherPerQuarter',
       'importPayerKind', 'importPaidPerQuarter', 'importPaidPayerSizeLe1e12PerQuarter', 'importPaidPayerSizeMidPerQuarter', 'importPaidPayerSizeGt1e9PerQuarter',
       'merchantDebt', 'estateDebtMovedPerQuarter', 'heirDebtSettledPerQuarter', 'heirDebtAfterSettleSum', 'billWithHeirDebtClearings', 'billBlockedClearings', 'emptyPopSavings']
trrows = []
for s in range(s0, s1 + 1):
    pc = os.path.join(ins, f'cearn_s{s}.tsv'); pn = os.path.join(ins, f'nation_s{s}.tsv')
    if not os.path.exists(pc): lines.append(f'種 {s}：cearn_s 無し'); continue
    Fset = set(); bottoms = set()
    byn = {}
    with open(pn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if fl(x[ix['priceLevel']]) <= FLOOR: byn.setdefault(x[ix['nation']], []).append(int(x[ix['quarter']]))
    ivs = []
    for n, qs in byn.items():
        for st, en in runs(sorted(qs), K):
            ivs.append((n, st, en))
            for q in range(st, en + 1): Fset.add((n, q))
    sv = {}
    rows = []
    with open(pc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            n = x[ix['nation']]; q = int(x[ix['quarter']])
            if (n, q) in Fset: sv[(n, q)] = sv.get((n, q), 0.0) + z(fl(x[ix['savings']]))
            if any(t[0] == s and t[1] == n and t[2] - 2 <= q <= t[2] + 2 for t in tr):
                trrows.append((s, [x[ix[c]] for c in TRC]))
            rows.append((n, q, x[ix['tier']], z(fl(x[ix['outImportPerQuarter']])), z(fl(x[ix['inExportPerQuarter']])), z(fl(x[ix['importPaidPerQuarter']])),
                         z(fl(x[ix['importPaidPayerSizeLe1e12PerQuarter']])), z(fl(x[ix['importPaidPayerSizeMidPerQuarter']])) + z(fl(x[ix['importPaidPayerSizeGt1e9PerQuarter']])),
                         z(fl(x[ix['estateDebtMovedPerQuarter']])), z(fl(x[ix['heirDebtSettledPerQuarter']])), int(x[ix['billWithHeirDebtClearings']]),
                         z(fl(x[ix['outOtherPerQuarter']])), z(fl(x[ix['inCreditPerQuarter']]))))
    for n, st, en in ivs:
        best = None
        for q in range(st, en + 1):
            v = sv.get((n, q))
            if v is None or not (v > 0): continue
            if best is None or v < best[0]: best = (v, q)
        if best: bottoms.add((n, best[1]))
    ps = {k: 0.0 for k in KEYS}
    for (n, q, tier, oi, ie, paid, E, L, mv, hs, bw, oo, ic) in rows:
        cls = None
        if E > 0:
            cls = 'E4' if not (mv > 0) else ('E3' if not (hs > 0) else ('E1' if bw == 0 else 'E2'))
        scs = ['all'] + (['colonial'] if tier == 'Colonial' else []) + (['F'] if (n, q) in Fset else []) + (['Fbottom'] if (n, q) in bottoms else [])
        for sc in scs + (['_seed'] if True else []):
            t = ps if sc == '_seed' else tot[sc]
            t['outImport'] += oi; t['inExport'] += ie; t['paid'] += paid; t['L'] += L; t['E'] += E; t['moved'] += mv; t['heirSettled'] += hs; t['outOther'] += oo; t['inCredit'] += ic; t['rows'] += 1
            if cls: t[cls] += E; t['rowsE'] += 1
            if cls == 'E1': t['rowsE1'] += 1
            if cls == 'E2': t['rowsE2'] += 1
            if cls in ('E1', 'E2'): t['heirSettled_E12'] += hs; t['moved_E12'] += mv; t['oiE12'] += oi * E / paid if paid > 0 else 0.0
            if cls == 'E1': t['oiE1'] += oi * E / paid if paid > 0 else 0.0
    perseed[s] = ps
def pct(x, d): return f'{x / d:.4f}' if d > 0 else 'nan'
lines.insert(0, f'# (980) q1_loop.py・ins {ins}・種 {s0}〜{s1}')
for sc in SC:
    t = tot[sc]
    lines.append(f'[{sc}] 行 {int(t["rows"])}・outImport {t["outImport"]:.6g}・inExport {t["inExport"]:.6g}・純 {t["outImport"] - t["inExport"]:.6g}・paid {t["paid"]:.6g}（paid÷outImport {pct(t["paid"], t["outImport"])}）')
    lines.append(f'    払い手の人数で分けた額：人の居ない E {t["E"]:.6g}（÷paid {pct(t["E"], t["paid"])}）・人の居る L {t["L"]:.6g}（÷paid {pct(t["L"], t["paid"])}）')
    lines.append(f'    E の内訳（÷paid）：E1 型そのもの {pct(t["E1"], t["paid"])}・E2 型＋門の閉じあり {pct(t["E2"], t["paid"])}・E3 移したが返さず {pct(t["E3"], t["paid"])}・E4 移していない {pct(t["E4"], t["paid"])}（E の行 {int(t["rowsE"])}・E1 {int(t["rowsE1"])}・E2 {int(t["rowsE2"])}）')
    lines.append(f'    outImport を分母（行ごとに oi×E÷paid で配った）：(E1+E2) {pct(t["oiE12"], t["outImport"])}・E1 {pct(t["oiE1"], t["outImport"])}')
    lines.append(f'    Σ moved {t["moved"]:.6g}・Σ heirSettled {t["heirSettled"]:.6g}・E1+E2 の行で heirSettled÷moved {pct(t["heirSettled_E12"], t["moved_E12"])}・Σ inCredit {t["inCredit"]:.6g}・Σ outOther {t["outOther"]:.6g}')
lines.append('[種ごと] 種・paid・E÷paid・(E1+E2)÷paid・E1÷paid')
for s, t in perseed.items():
    lines.append(f'    {s}・{t["paid"]:.6g}・{pct(t["E"], t["paid"])}・{pct(t["E1"] + t["E2"], t["paid"])}・{pct(t["E1"], t["paid"])}')
if trrows:
    lines.append('[追跡] ' + '\t'.join(['seed'] + TRC))
    for s, r in trrows: lines.append('\t'.join([str(s)] + r))
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
print(f'書いた {out}・{len(lines)} 行')
