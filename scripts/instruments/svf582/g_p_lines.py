#!/usr/bin/env python
"""(582) 線 svf2：物価水準の天井の素通り（旗 PriceLevelCeilFree）の狙いの数字を、計器 money_s の既存の列から読む（足した列は無い）。
使い方：PYTHONUTF8=1 python g_p_lines.py <計器の出力フォルダ（ins）> [-v]
  列（money_s）＝time・nation・event・priceLevelBeforeIndex・priceLevelAfterEmaIndex・priceLevelAfterFiatIndex・priceLevelAfterIndex・fiatPressureBeforeShare・cyclicalRatio
H1 天井が残っていない：event=update の行で 〈増発の後の P（AfterFiat）が 4.0 を越えるのに最終の P（After）がそれより小さい〉行の数＝0（旗1で合格・旗0の基線では天井に触れた行がそのまま出る）
H2 隠れた増幅が無い：増発の圧が 0 の行で P（After）≦max(更新前の P, 循環比, 床 0.4)×(1+1e-9)（P の更新は P と循環比の凸結合＝正帰還が無い）。違反は機構の誤り
H3 NaN・無限：money_s の全ての数の列に nan（P・循環比の列）・inf が無い（基線でも q0 の趨勢の列に nan が出る行はある＝event=first は除く）
記録＝床に触れた行（AfterFiat＜0.4 かつ After＝0.4）・増発なしで P＞4 の (種, 国) の行数・最大値・増発ありで P＞4 の行数・最大の P
"""
import csv, glob, re, sys, math, collections

def f(x):
    try: return float(x)
    except Exception: return float('nan')

def main(d, verbose=False):
    files = sorted(glob.glob(f'{d}/money_s*.tsv'), key=lambda p: int(re.search(r'money_s(\d+)', p).group(1)))
    h1 = 0; h2 = 0; h3 = 0; floor = 0; rows_n = 0
    nofiat_gt4 = collections.defaultdict(lambda: [0, 0.0]); fiat_gt4 = [0, 0.0]
    h2_list = []; h1_list = []
    for p in files:
        seed = int(re.search(r'money_s(\d+)', p).group(1))
        for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
            if r['event'] != 'update': continue
            rows_n += 1
            pb, pe, pf, pa, fp, cyc = (f(r['priceLevelBeforeIndex']), f(r['priceLevelAfterEmaIndex']), f(r['priceLevelAfterFiatIndex']),
                                       f(r['priceLevelAfterIndex']), f(r['fiatPressureBeforeShare']), f(r['cyclicalRatio']))
            if any(math.isnan(x) or math.isinf(x) for x in (pb, pe, pf, pa, cyc)): h3 += 1
            if pf > 4.0 + 1e-9 and pa < pf - 1e-9:
                h1 += 1
                if len(h1_list) < 5: h1_list.append((seed, r['nation'], r['time'], pf, pa))
            if pf < 0.4 - 1e-9 and abs(pa - 0.4) < 1e-9: floor += 1
            if not (fp > 0):
                bound = max(pb, cyc, 0.4) * (1 + 1e-9)
                if not (pa <= bound):
                    h2 += 1
                    if len(h2_list) < 5: h2_list.append((seed, r['nation'], r['time'], pb, cyc, pa))
            if pa > 4.0 + 1e-9 or pf > 4.0 + 1e-9:
                m = max(pa, pf)
                if fp > 0:
                    fiat_gt4[0] += 1; fiat_gt4[1] = max(fiat_gt4[1], m)
                else:
                    k = (seed, r['nation']); nofiat_gt4[k][0] += 1; nofiat_gt4[k][1] = max(nofiat_gt4[k][1], m)
    print(f'rows(update)={rows_n} files={len(files)}')
    print(f'H1 天井が働いた行（旗1では 0）={h1}  {h1_list if verbose else ""}')
    print(f'H2 増発なしの行で P が max(更新前, 循環比, 床) を越えた行（0）={h2}  {h2_list if verbose else ""}')
    print(f'H3 nan・inf の行（0）={h3}')
    print(f'記録：床に触れた行={floor}／増発なしで P>4 の (種,国) の数={len(nofiat_gt4)}（行 {sum(v[0] for v in nofiat_gt4.values())}・最大 {max([v[1] for v in nofiat_gt4.values()] or [0]):.2f}）／増発ありで P>4 の行={fiat_gt4[0]}（最大 {fiat_gt4[1]:.2f}）')
    for k, v in sorted(nofiat_gt4.items()): print(f'  増発なし P>4：種{k[0]} {k[1]}  行{v[0]}  最大{v[1]:.2f}')

if __name__ == '__main__':
    main(sys.argv[1], '-v' in sys.argv)
