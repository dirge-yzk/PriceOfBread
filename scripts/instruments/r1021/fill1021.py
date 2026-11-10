# (1021) c95cf 読む線：清算の着値の充填の分布（csc_s の fillsCount）と tradeskip_s の門の和（読むだけ）
# 使い方：python -X utf8 -I fill1021.py <出力ファイル> <名:生のフォルダ:種の始め:種の終わり> ...
import os, sys
from collections import defaultdict

def rows(path):
    with open(path, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            yield ix, ln.decode('utf-8').rstrip('\r\n').split('\t')

def csc(d, s0, s1):
    byC = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])   # cls -> rows, fills, dealtRows, deals
    byDec = defaultdict(lambda: defaultdict(float))   # decade -> cls -> fills
    byNat = defaultdict(float); rowsNat = defaultdict(float)
    for s in range(s0, s1 + 1):
        for ix, x in rows(os.path.join(d, f'csc_s{s}.tsv')):
            if x[ix['roleCode']] != '0': continue
            cl = int(x[ix['classCode']]); r = float(x[ix['rowsCount']]); fl = float(x[ix['fillsCount']])
            a = byC[cl]; a[0] += r; a[1] += fl; a[2] += float(x[ix['dealtRowsCount']]); a[3] += float(x[ix['dealsCount']])
            y = int(x[ix['year']]); byDec[(y // 20) * 20][cl] += fl
            byNat[x[ix['nation']]] += fl; rowsNat[x[ix['nation']]] += r if cl != 4 else 0
    return byC, byDec, byNat, rowsNat

TSC = ['rows', 'rowIdle', 'rowNoNeed', 'rowBudget0', 'rowScan', 'rowDealtCross', 'rowDealtDomOnly', 'rowNoDeal', 'xPairs', 'xSoldOut', 'xCap', 'xWedge', 'xWedgeA', 'xWedgeB', 'xWedgeC', 'xWedgeD', 'xPass', 'domPairs']
def ts(d, s0, s1):
    T = dict.fromkeys(TSC, 0.0)
    for s in range(s0, s1 + 1):
        for ix, x in rows(os.path.join(d, f'tradeskip_s{s}.tsv')):
            if x[ix['nation']] == 'ALL' or x[ix['good']] == 'ALL': continue
            for c in TSC: T[c] += float(x[ix[c]] or 0)
    return T

out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
def P(*a): print(*a, file=out)
for spec in sys.argv[2:]:
    name, rest = spec.split(':', 1); d, s0, s1 = rest.rsplit(':', 2)
    s0, s1 = int(s0), int(s1); n = s1 - s0 + 1
    byC, byDec, byNat, rowsNat = csc(d, s0, s1)
    totF = sum(v[1] for v in byC.values()); totR = sum(v[0] for k, v in byC.items() if k != 4)
    P(f'== {name}（{d}・種 {s0}〜{s1}）')
    P(f'csc 充填の和÷種 {totF / n:.6g}・走査した行の和÷種 {totR / n:.6g}・充填÷走査した行 {totF / totR:.4f}')
    for cl in sorted(byC):
        r, fl, dr, dl = byC[cl]
        P(f'  種類 {cl}: 行÷種 {r / n:.4g} 充填÷種 {fl / n:.4g} 充填の割合 {fl / totF:.4f} 充填÷行 {fl / r if r else float("nan"):.3f} 成約した行の割合 {dr / r if r else float("nan"):.3f}')
    first = sum(byC[k][1] for k in (0, 1) if k in byC); thin = sum(byC[k][1] for k in (2, 3, 5) if k in byC)
    P(f'  最初の清算（種類 0・1）の充填の割合 {first / totF:.4f}・2・3 月目（種類 2・3・5）{thin / totF:.4f}')
    P('  20 年ごとの充填÷種（種類 0+1 / 2+3+5）:')
    for dec in sorted(byDec):
        b = byDec[dec]; P(f'    {dec} {(b[0] + b[1]) / n:.4g} / {(b[2] + b[3] + b[5]) / n:.4g}')
    top = sorted(byNat.items(), key=lambda kv: -kv[1])[:15]
    P('  充填の上位 15 国（充填の割合・充填÷走査した行）:')
    for k, v in top: P(f'    {k} {v / totF:.4f} {v / rowsNat[k] if rowsNat[k] else float("nan"):.2f}')
    T = ts(d, s0, s1)
    P('  tradeskip_s の和（国×財の行・ALL を除く・調べる年の第 4 四半期の最初の清算）:')
    P('    ' + '  '.join(f'{c} {T[c]:.6g}' for c in TSC))
    P(f'    xPairs÷rowScan {T["xPairs"] / T["rowScan"]:.3f}・xPass÷rowScan {T["xPass"] / T["rowScan"]:.3f}・domPairs÷rowScan {T["domPairs"] / T["rowScan"]:.3f}')
    P(f'    越境の売り手の最初の門 sold {T["xSoldOut"]:.6g} cap {T["xCap"]:.6g} wedge {T["xWedge"]:.6g} passed {T["xPass"]:.6g}・楔 A {T["xWedgeA"]:.6g} B {T["xWedgeB"]:.6g} C {T["xWedgeC"]:.6g} D {T["xWedgeD"]:.6g}')
out.close()
