# (1034) c97op 読む台本：tradeskip_s を最初に当たった門で数え直す（読むだけ・エンジンの関数を呼ばない）
# 使い方: python op_count.py <pre のフォルダ> <keep のフォルダ> <出力 tsv>
#   pre に ts_<腕>_s<種>.tsv（1870・1913 の行だけを grep した物）・keep に <腕>/nationgood_s1.tsv（財→層の対応だけを読む）
# 3 つの分母を混ぜない：
#   行の段  rows = rowIdle + rowNoNeed + rowBudget0 + rowScan（検算する）
#   走査の段 rowScan = rowDealtCross + rowDealtDomOnly + rowNoDeal（検算する）
#   組の段  xPairs = xSoldOut + xCap + xWedge + xPass・xWedge = A+B+C+D（検算する）
# 売り手の段（売り手の国の州×財）：sellerUnlinked・sellerNoStock・sellerOtherGate・sellerListed
# 残り（問い2 の補い）：xPass>0 なのに越境の成約 0 の国×財のます＝楔の後（着値・X・Y・予算・容量の途中切れ）で落ちた分
import sys, os, statistics as st
from collections import defaultdict

PRE, KEEP, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ARMS = ['base_ins', 'n1', 'n2']
SEEDS = range(1, 11)
YEARS = ['1870', '1913']
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']

# 列（0 始まり・見出しで引く）
def cols(h):
    ix = {n: i for i, n in enumerate(h)}
    return ix

tier = {}
with open(os.path.join(KEEP, 'base_ins', 'nationgood_s1.tsv'), encoding='utf-8') as f:
    hh = f.readline().rstrip('\n').split('\t'); gi, ti = hh.index('good'), hh.index('tier')
    for ln in f:
        p = ln.rstrip('\n').split('\t')
        tier.setdefault(p[gi], p[ti])

with open(os.path.join(KEEP, 'base_ins', 'tradeskip_s1.tsv'), encoding='utf-8') as f:
    H = f.readline().rstrip('\n').split('\t')
C = cols(H)
NUM = ['rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan', 'rowDealtCross', 'rowDealtDomOnly', 'rowNoDeal',
       'xPairs', 'xSoldOut', 'xCap', 'xWedge', 'xWedgeA', 'xWedgeB', 'xWedgeC', 'xWedgeD', 'xPass', 'domPairs',
       'sellerUnlinked', 'sellerNoStock', 'sellerOtherGate', 'sellerListed']
CAPLOW = ['xCapBin%d' % k for k in range(4)]     # 売り手の値段÷自州の値段 < 1.0 の組（容量で止まった・値段は安い）
PASSLOW = ['xPassBin%d' % k for k in range(4)]

def scopes(nation):
    s = ['全ての国']
    if nation in MAJ: s += ['6大国', nation]
    return s

# acc[(arm,seed,year,scope,tierkey)][name] = 和
acc = defaultdict(lambda: defaultdict(float))
chk = []   # 検算の外れ
ledger = {}
for arm in ARMS:
    for s in SEEDS:
        p = os.path.join(PRE, 'ts_%s_s%d.tsv' % (arm, s))
        with open(p, encoding='utf-8') as f:
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                y, nat, g = v[0], v[1], v[2]
                if y not in YEARS: continue
                if nat == 'ALL':
                    if g == 'ALL': ledger[(arm, s, y)] = float(v[C['rowsLedger']])
                    else: acc[(arm, s, y, '全ての国', '*')]['goodNoSellers'] += float(v[C['goodNoSellers']])
                    continue
                d = {n: float(v[C[n]]) for n in NUM}
                # 検算（行の排他・組の排他）
                if abs(d['rows'] - d['rowIdle'] - d['rowNoNeed'] - d['rowBudget0'] - d['rowScan']) > 0.5: chk.append(('rows', arm, s, y, nat, g))
                if abs(d['rowScan'] - d['rowDealtCross'] - d['rowDealtDomOnly'] - d['rowNoDeal']) > 0.5: chk.append(('scan', arm, s, y, nat, g))
                if abs(d['xPairs'] - d['xSoldOut'] - d['xCap'] - d['xWedge'] - d['xPass']) > 0.5: chk.append(('pairs', arm, s, y, nat, g))
                if abs(d['xWedge'] - d['xWedgeA'] - d['xWedgeB'] - d['xWedgeC'] - d['xWedgeD']) > 0.5: chk.append(('wedge', arm, s, y, nat, g))
                caplow = sum(float(v[C[n]]) for n in CAPLOW); passlow = sum(float(v[C[n]]) for n in PASSLOW)
                for sc in scopes(nat):
                    for tk in ('*', tier.get(g, '?')):
                        a = acc[(arm, s, y, sc, tk)]
                        for n in NUM: a[n] += d[n]
                        a['xCapLow'] += caplow; a['xPassLow'] += passlow
                        a['cells'] += 1
                        if d['rowScan'] > 0: a['cellsScan'] += 1
                        if d['rowDealtCross'] > 0: a['cellsCross'] += 1
                        if d['xPass'] > 0 and d['rowDealtCross'] == 0:
                            a['cellsPassNoCross'] += 1; a['xPassNoCross'] += d['xPass']
                        if d['xPass'] > 0: a['cellsPass'] += 1

# 量の定義（分母は自分の段だけ）
def q(a):
    r = a['rows'] or float('nan'); sc = a['rowScan'] or float('nan'); xp = a['xPairs'] or float('nan')
    xw = a['xWedge'] or float('nan'); xc = a['xCap'] or float('nan')
    sel = a['sellerUnlinked'] + a['sellerNoStock'] + a['sellerOtherGate'] + a['sellerListed'] or float('nan')
    xps = a['xPass'] or float('nan'); cp = a['cellsPass'] or float('nan')
    return {
        '行:R0眠り': a['rowIdle'] / r, '行:需要なし(xNeed≦0ほか)': a['rowNoNeed'] / r, '行:R4予算0': a['rowBudget0'] / r, '行:走査': a['rowScan'] / r,
        '走査:越境の成約': a['rowDealtCross'] / sc, '走査:国内だけ': a['rowDealtDomOnly'] / sc, '走査:成約0': a['rowNoDeal'] / sc,
        '組:R1売り切れ': a['xSoldOut'] / xp, '組:R3容量': a['xCap'] / xp, '組:R2楔': a['xWedge'] / xp, '組:楔を通る': a['xPass'] / xp,
        '楔:A関税を抜けば通る': a['xWedgeA'] / xw, '楔:B運賃を抜けば通る': a['xWedgeB'] / xw, '楔:C両方抜けば通る': a['xWedgeC'] / xw, '楔:D両方抜いても通らない': a['xWedgeD'] / xw,
        '容量:値段は安い(比<1)': a['xCapLow'] / xc,
        '売り手:連結なし': a['sellerUnlinked'] / sel, '売り手:R1在庫0か値段0': a['sellerNoStock'] / sel, '売り手:その他の門(在庫≦目標ほか)': a['sellerOtherGate'] / sel, '売り手:表に載る': a['sellerListed'] / sel,
        '残り:楔を通る組のうち越境の成約0のますの分': a['xPassNoCross'] / xps, '残り:楔を通る組のあるますのうち越境の成約0': a['cellsPassNoCross'] / cp,
        'ます:越境の成約あり': a['cellsCross'] / (a['cells'] or float('nan')),
        '件数:行': a['rows'], '件数:組': a['xPairs'],
    }

ORDER = None
rows_out = []
keys = sorted({(k[2], k[3], k[4]) for k in acc})
for (y, sc, tk) in keys:
    if tk != '*' and sc not in ('6大国', '全ての国'): continue
    per = {arm: [q(acc[(arm, s, y, sc, tk)]) for s in SEEDS if (arm, s, y, sc, tk) in acc] for arm in ARMS}
    if not per['base_ins']: continue
    ORDER = ORDER or list(per['base_ins'][0].keys())
    for m in ORDER:
        def med(L):
            L = [x[m] for x in L if x[m] == x[m]]
            return (st.median(L), min(L), max(L)) if L else (float('nan'),) * 3
        b = med(per['base_ins']); n1 = med(per['n1']); n2 = med(per['n2'])
        rows_out.append((y, sc, tk, m, b[0], b[1], b[2], n1[0], n2[0]))

with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('year\tscope\ttier\tmeasure\tbase_med\tbase_min\tbase_max\tn1_med\tn2_med\n')
    for r in rows_out:
        f.write('\t'.join(r[:4]) + '\t' + '\t'.join('%.4g' % x for x in r[4:]) + '\n')
# 検算：Σrows（ALL を除く）と台帳 rowsLedger
with open(OUT + '.check.txt', 'w', encoding='utf-8', newline='\n') as f:
    f.write('排他の検算の外れ %d 件\n' % len(chk))
    for c in chk[:20]: f.write('\t'.join(map(str, c)) + '\n')
    for (arm, s, y), L in sorted(ledger.items()):
        tot = acc[(arm, s, y, '全ての国', '*')]['rows']
        f.write('%s\ts%d\t%s\tΣrows=%d\trowsLedger=%d\t差=%d\n' % (arm, s, y, tot, L, tot - L))
print('done', len(rows_out), 'chk', len(chk))
