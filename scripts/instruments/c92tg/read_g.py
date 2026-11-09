# (1003) ident の生で G1〜G4 を読む。使い方：python read_g.py <計器の出力フォルダ> [種の一覧 1,2,...,10]
import sys, os, csv, statistics as st
d = sys.argv[1]; seeds = [int(x) for x in (sys.argv[2] if len(sys.argv) > 2 else '1,2,3,4,5,6,7,8,9,10').split(',')]
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); return h, list(r)
tot = dict(g1_rows=0, g1_bad=0, g1_maxrel=0.0, g2_bad=0, g2_rows=0, g3_bad=0, led_bad=0, led_years=0)
qs = {}; ratios = []; wedge_tot = dict(A=0, B=0, C=0, D=0); xtot = dict(sold=0, cap=0, wedge=0, passed=0)
uk = {}
for s in seeds:
    pn = f'{d}/nationgood_s{s}.tsv'; pt = f'{d}/tradeskip_s{s}.tsv'; pi = f'{d}/nation_s{s}.tsv'
    if not (os.path.exists(pn) and os.path.exists(pt)):
        print('seed', s, 'MISSING'); continue
    h, rows = rd(pn); ix = {n: i for i, n in enumerate(h)}
    # G1：Σ財 crossExportValue・crossImportValue と nation_s の exportsPerClearing・importsPerClearing（年末の四半期 q%4==3）
    nh, nrows = rd(pi); nix = {n: i for i, n in enumerate(nh)}
    ns = {}
    for r in nrows:
        q = int(r[nix['quarter']]) if r[nix['quarter']].lstrip('-').isdigit() else None
        if q is not None and q % 4 == 3: ns[(r[nix['year']], r[nix['nation']])] = (float(r[nix['exportsPerClearing']]), float(r[nix['importsPerClearing']]))
    agg = {}
    for r in rows:
        k = (r[ix['year']], r[ix['nation']]); a = agg.setdefault(k, [0.0, 0.0])
        a[0] += float(r[ix['crossExportValue']]); a[1] += float(r[ix['crossImportValue']])
        q = float(r[ix['crossImportQty']]); p = float(r[ix['priceMeanGold']]); v = float(r[ix['crossImportValue']])
        if q > 0 and v > 0 and p > 0: ratios.append(q * p / v)
        kk = (s, r[ix['year']], r[ix['good']]); c = qs.setdefault(kk, [0.0, 0.0, 0.0, 0.0]); c[0] += q; c[1] += float(r[ix['crossExportQty']]); c[2] += v; c[3] += float(r[ix['crossExportValue']])
    for k, (e, i) in agg.items():
        if k not in ns: continue
        tot['g1_rows'] += 1
        for mine, theirs in ((e, ns[k][0]), (i, ns[k][1])):
            rel = abs(mine - theirs) / max(1.0, abs(theirs)); tot['g1_maxrel'] = max(tot['g1_maxrel'], rel)
            if rel > 1e-9: tot['g1_bad'] += 1
    # G2・G3
    h, rows = rd(pt); ix = {n: i for i, n in enumerate(h)}
    f = lambda r, n: float(r[ix[n]])
    led = {}; rs = {}
    for r in rows:
        y, nat, g = r[ix['year']], r[ix['nation']], r[ix['good']]
        if nat == 'ALL':
            if g == 'ALL': led[y] = led.get(y, 0) + f(r, 'rowsLedger')
            continue
        tot['g2_rows'] += 1
        if f(r, 'rows') != f(r, 'rowIdle') + f(r, 'rowNoNeed') + f(r, 'rowBudget0') + f(r, 'rowScan') or f(r, 'rowScan') != f(r, 'rowDealtCross') + f(r, 'rowDealtDomOnly') + f(r, 'rowNoDeal'): tot['g2_bad'] += 1
        if f(r, 'xPairs') != f(r, 'xSoldOut') + f(r, 'xCap') + f(r, 'xWedge') + f(r, 'xPass') or f(r, 'xWedge') != f(r, 'xWedgeA') + f(r, 'xWedgeB') + f(r, 'xWedgeC') + f(r, 'xWedgeD'): tot['g3_bad'] += 1
        rs[y] = rs.get(y, 0) + f(r, 'rows')
        for c, n in (('A', 'xWedgeA'), ('B', 'xWedgeB'), ('C', 'xWedgeC'), ('D', 'xWedgeD')): wedge_tot[c] += f(r, n)
        xtot['sold'] += f(r, 'xSoldOut'); xtot['cap'] += f(r, 'xCap'); xtot['wedge'] += f(r, 'xWedge'); xtot['passed'] += f(r, 'xPass')
        if nat == 'イギリス' and y == '1913':
            u = uk.setdefault(g, [0] * 6)
            for j, n in enumerate(('rowScan', 'rowDealtCross', 'xPairs', 'xWedgeA', 'xWedgeB', 'xWedgeC')): u[j] += f(r, n)
            u.append(0)
    for y in led:
        tot['led_years'] += 1
        if led[y] != rs.get(y, -1): tot['led_bad'] += 1
print('G1 行', tot['g1_rows'], '不一致', tot['g1_bad'], '最大相対差', tot['g1_maxrel'])
print('G2 行', tot['g2_rows'], '行の結果の不一致', tot['g2_bad'], '台帳の不一致の年', tot['led_bad'], '/', tot['led_years'])
bad_q = sum(1 for c in qs.values() if abs(c[0] - c[1]) > 1e-9 * max(1.0, abs(c[1]))); bad_cif = sum(1 for c in qs.values() if c[2] < c[3] * (1 - 1e-9))
print('G4b 年×財×種', len(qs), '量の保存の不一致', bad_q, 'CIF<FOB', bad_cif)
print('G3 不一致', tot['g3_bad'])
ratios.sort(); n = len(ratios)
if n: print('G4 正の行', n, '中央値', ratios[n // 2], '0.1〜10 の割合', sum(1 for x in ratios if 0.1 < x < 10) / n)
print('R2 区分の合計 A,B,C,D', wedge_tot, '越境の売り手の最初の門', xtot)
print('英 1913 の財別（走査・越境成約の行・越境の売り手・A・B・C）の上位 8（xPairs 順）')
for g, u in sorted(uk.items(), key=lambda kv: -kv[1][2])[:8]: print(' ', g, [int(x) for x in u[:6]])
