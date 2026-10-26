#!/usr/bin/env python
"""(582) 線 svf2：物価水準の天井の素通り（旗 PriceLevelCeilFree）の狙いの数字を、計器 money_s の既存の列から読む（足した列は無い）。
使い方：PYTHONUTF8=1 python g_p_lines.py <計器の出力フォルダ（ins）> [-v]
  列（money_s）＝time・nation・event・priceLevelBeforeIndex・priceLevelAfterEmaIndex・priceLevelAfterFiatIndex・priceLevelAfterIndex・costPriceLevelAfterIndex・expectedInflationPerYear・fiatPressureBeforeShare・cyclicalRatio
H1 天井が残っていない：event=update の行で 〈増発の後の P（AfterFiat）が 4.0 を越えるのに最終の P（After）がそれより小さい〉行の数＝0（旗1で合格・旗0の基線では天井に触れた行がそのまま出る）【コードの検査】
H2 隠れた増幅が無い：増発の圧が 0 の行で P（After）≦max(更新前の P, 循環比, 床 0.4)×(1+1e-9)（P の更新は P と循環比の凸結合＝正帰還が無い）【更新の式の恒等の検査】
H3 NaN・無限（守りの後の列）：P（After）・自己費用の物価・循環比・期待インフレ年率に nan・inf が無い（0）。増発の後・守りの前の P（AfterFiat）の無限は守りが働いた行として記録へ（不合格に数えない＝追記 2）
H7 ハイパーインフレの観測（追記 2）：増発の圧が正の行で P（max(After, AfterFiat)）が 4.0 を越えた (種, 国) の数・最大・最初の時刻。平時（nation_s の atWarMonths・civilWarMonths が直前 8 四半期ともに 0）の行で P≧10 があれば不合格（極端の印が無い桁の暴走）
記録＝床に触れた行（AfterFiat＜0.4 かつ After＝0.4）・増発なしで P＞4 の (種, 国) の行数・最大値・増発ありで P＞4 の行数・最大の P・為替の目標が床に触れうる行（After が 1/ExchangeRateFloor＝3.33 を越える行・近似＝為替の bias は読まない）
"""
import csv, glob, re, sys, math, collections

FX_FLOOR_P = 1.0 / 0.3   # ExchangeRateFloor 0.3 の逆数（ppp＝1/P が床を割る P の目安・bias ±0.2 は読まない近似）


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def bad(x):
    return math.isnan(x) or math.isinf(x)


def war_quarters(d, seed):
    """nation_s から {(nation, quarter): 戦中か} を読む（atWarMonths>0 または civilWarMonths>0）。無ければ空。"""
    out = {}
    try:
        for r in csv.DictReader(open(f'{d}/nation_s{seed}.tsv', encoding='utf-8'), delimiter='\t'):
            out[(r['nation'], int(r['quarter']))] = (f(r['atWarMonths']) > 0) or (f(r['civilWarMonths']) > 0)
    except FileNotFoundError:
        pass
    return out


def main(d, verbose=False):
    files = sorted(glob.glob(f'{d}/money_s*.tsv'), key=lambda p: int(re.search(r'money_s(\d+)', p).group(1)))
    h1 = 0; h2 = 0; h3 = 0; floor = 0; rows_n = 0; fx = 0; pf_nonfinite = 0
    nofiat_gt4 = collections.defaultdict(lambda: [0, 0.0])
    fiat_gt4 = collections.defaultdict(lambda: [0, 0.0, None])
    h2_list = []; h1_list = []; h3_list = []; h7_peace = []
    for p in files:
        seed = int(re.search(r'money_s(\d+)', p).group(1))
        war = war_quarters(d, seed)
        for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
            if r['event'] != 'update':
                continue
            rows_n += 1
            pb, pe, pf, pa, fp, cyc = (f(r['priceLevelBeforeIndex']), f(r['priceLevelAfterEmaIndex']), f(r['priceLevelAfterFiatIndex']),
                                       f(r['priceLevelAfterIndex']), f(r['fiatPressureBeforeShare']), f(r['cyclicalRatio']))
            cp, ei = f(r.get('costPriceLevelAfterIndex', 'nan')), f(r.get('expectedInflationPerYear', 'nan'))
            if any(bad(x) for x in (pa, cyc, cp, ei, pb, pe)):
                h3 += 1
                if len(h3_list) < 5:
                    h3_list.append((seed, r['nation'], r['time'], pa, cyc, cp, ei))
            if bad(pf):
                pf_nonfinite += 1
            if pf > 4.0 + 1e-9 and pa < pf - 1e-9:
                h1 += 1
                if len(h1_list) < 5:
                    h1_list.append((seed, r['nation'], r['time'], pf, pa))
            if pf < 0.4 - 1e-9 and abs(pa - 0.4) < 1e-9:
                floor += 1
            if pa > FX_FLOOR_P:
                fx += 1
            if not (fp > 0):
                bound = max(pb, cyc, 0.4) * (1 + 1e-9)
                if not (pa <= bound):
                    h2 += 1
                    if len(h2_list) < 5:
                        h2_list.append((seed, r['nation'], r['time'], pb, cyc, pa))
            if pa > 4.0 + 1e-9 or pf > 4.0 + 1e-9:
                m = max(pa, pf)
                if fp > 0:
                    k = (seed, r['nation'])
                    v = fiat_gt4[k]
                    v[0] += 1
                    v[1] = max(v[1], m)
                    if v[2] is None:
                        v[2] = r['time']
                    mt = re.fullmatch(r'q(\d+)', r['time'])
                    if m >= 10 and mt:
                        q = int(mt.group(1))
                        if war and not any(war.get((r['nation'], qq), False) for qq in range(max(0, q - 8), q + 1)):
                            h7_peace.append((seed, r['nation'], r['time'], m))
                else:
                    k = (seed, r['nation'])
                    nofiat_gt4[k][0] += 1
                    nofiat_gt4[k][1] = max(nofiat_gt4[k][1], m)
    print(f'rows(update)={rows_n} files={len(files)}')
    print(f'H1 天井が働いた行（旗1では 0）={h1}  {h1_list if verbose else ""}')
    print(f'H2 増発なしの行で P が max(更新前, 循環比, 床) を越えた行（0）={h2}  {h2_list if verbose else ""}')
    print(f'H3 守りの後の列に nan・inf の行（0）={h3}  {h3_list if verbose else ""}')
    print(f'  （記録）増発の後・守りの前の P が非有限の行（守りが働いた）={pf_nonfinite}')
    print(f'H7 増発ありで P>4 の (種,国) の数={len(fiat_gt4)}（行 {sum(v[0] for v in fiat_gt4.values())}・最大 {max([v[1] for v in fiat_gt4.values()] or [0]):.2f}）／平時に P≧10 の行（0）={len(h7_peace)}  {h7_peace[:5] if verbose else ""}')
    for k, v in sorted(fiat_gt4.items()):
        print(f'  増発あり P>4：種{k[0]} {k[1]}  行{v[0]}  最大{v[1]:.2f}  最初{v[2]}')
    print(f'記録：床に触れた行={floor}／為替の目標が床に触れうる行（P>{FX_FLOOR_P:.2f}・近似）={fx}／増発なしで P>4 の (種,国) の数={len(nofiat_gt4)}（行 {sum(v[0] for v in nofiat_gt4.values())}・最大 {max([v[1] for v in nofiat_gt4.values()] or [0]):.2f}）')
    for k, v in sorted(nofiat_gt4.items()):
        print(f'  増発なし P>4：種{k[0]} {k[1]}  行{v[0]}  最大{v[1]:.2f}')


if __name__ == '__main__':
    main(sys.argv[1], '-v' in sys.argv)
