# -*- coding: utf-8 -*-
"""(536) 線 ivf の読み手（追記 2 で全面に書き直した）：ivf_s<seed>.tsv（SimCore/InstrumentsIvf.cs）から事前登録 measurements/ivf536_20261024_prereg.txt の
G1〜G9・G5b・G9′ と記録 R1〜R4・R10〜R12 の値を読み、門の通り／外れを印字する。
⚠(550) 線 fb で F2（時代の組）を道ごと消した＝ivf_s の列 bundleEraOverRefMain／bundleEraMonths は bundleBuildingOverRefMain／bundleBuildingMonths（建造物の組）に替わった。
  この読み手は F2 の腕の生（列の名前が旧）を読むための物＝新しい生に当てるときは scripts/instruments/fb550/an_fb.py を使う。
使い方：PYTHONUTF8=1 python an_ivf.py <腕 B の計器の出力フォルダ> [<腕 A の計器の出力フォルダ>] [種 ...]
  例）python an_ivf.py D:/PoB_work/x/out_B/ins D:/PoB_work/x/out_A/ins 1 2 3 4 5 6 7 8 9 10
  腕 A を渡さないと G7 は読めない（B の値だけ印字）。種を渡さないと 1〜10。
窓と単位（追記 2 で固定・結果を読む前）：
  ・G5〜G7 の母集団＝本土（植民地を除く）の資本区画のある州×月（計器 IvfFold が本土だけを足す）。ファイルにある全ての国の行を足す（挙げた国は毎四半期・他の国は年末の行に年内の積算）。
  ・G5＝時代の組の月数÷組を求めた月数（1783〜1829 年・種ごとに全ての国をプール）の 10 種の中央値 ≧0.9。G5b＝同じ量を 1913 年の行で ≦0.5（中央値）。1850・1880・1913 は記録。
  ・G6＝種ごとに（切り替えの回数×12÷組を求めた月数・1783〜1829 年・全ての国をプール）の 10 種の平均 ≦1.0。国ごとの分布（月数 24 以上の国のうち 1.0 を超える割合）と全期間の値は記録。
  ・G7＝イギリス・フランス・プロイセン・アメリカの 1783〜1820 年の束の最低の配給率（和÷月数）を種ごとにプールし、腕 B ＞ 腕 A の種が 8 以上（10 種）。
  ・G8＝イギリスの資本財の能力の和（1800 年の最後の行÷四半期 0 の行）≧0.9 の種が 8 以上。
  ・G9＝鋳鉄の機械で Output が 1 を超える本土の州÷鋳鉄の機械のレシピがある本土の州（1783〜1829 年・全ての国をプール）の 10 種の中央値 ≧0.5（働いているか。算術に近い）。
  ・G9′＝本土の機械の産出のうち鋳鉄の機械の割合（1783〜1829 年・種ごとにプール）の 10 種の中央値 ≧0.5。1850・1880・1913 は記録（転炉鋼の後に下がるのが狙いの向き）。
  ・G1・G2・G3・G4＝四半期 0 の行（イギリス・ネーデルラント・スペイン）。G1・G2 は国平均を本土にした算術の確認（人口加重の平均が 1 に寄るのは作りの結果＝判定の力は弱いと追記 2 に書いた）。
⚠読むだけ。採否の判定は事前登録の段（機構→止める線→採否）で人が行う。"""
import csv, sys, os, statistics

def rd(p):
    return list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))

def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')

def isnum(x):
    return x == x

def med(v):
    v = [x for x in v if isnum(x)]
    return statistics.median(v) if v else float('nan')

def mean(v):
    v = [x for x in v if isnum(x)]
    return sum(v) / len(v) if v else float('nan')

def load(d, seeds):
    out = {}
    for sd in seeds:
        p = os.path.join(d, f'ivf_s{sd}.tsv')
        if os.path.exists(p):
            out[sd] = rd(p)
    return out

def win(rows, y0, y1):
    return [r for r in rows if y0 <= int(f(r['year'])) <= y1]

def pooled(rows, num, den, scale=1.0):
    n = sum(f(r[num]) for r in rows if isnum(f(r[num])))
    m = sum(f(r[den]) for r in rows if isnum(f(r[den])))
    return scale * n / m if m > 0 else float('nan')

def q0(rows, nation):
    for r in rows:
        if r['nation'] == nation and r['quarter'] == '0':
            return r
    return None

def verdict(ok):
    return 'PASS' if ok else 'FAIL'

EU = ('フランス', 'プロイセン', 'ロシア', 'オスマン', 'オーストリア')

def main():
    args = sys.argv[1:]
    dB = args[0]
    dA = None
    rest = args[1:]
    if rest and not rest[0].isdigit():
        dA = rest[0]; rest = rest[1:]
    seeds = [int(a) for a in rest] or list(range(1, 11))
    B = load(dB, seeds)
    A = load(dA, seeds) if dA else {}
    print(f'腕 B {dB} 種 {sorted(B)}' + (f'／腕 A {dA} 種 {sorted(A)}' if dA else '／腕 A なし'))

    # ---- 四半期 0 の行：G1〜G4・R1〜R4・R2・R10
    print('\n[四半期 0 の行（種ごと）] G1 lpmMain（英・蘭・西）／G2 wagePremMain（英）／G3 coalOwnCost（英）／G4 英÷他の欧州の中央値')
    g1 = []; g2 = []; g3 = []; g4 = []
    for sd, rows in sorted(B.items()):
        row = []
        for n in ('イギリス', 'ネーデルラント', 'スペイン'):
            r = q0(rows, n)
            v = f(r['lpmMain']) if r else float('nan'); g1.append(v); row.append(f'{n}={v:.4f}')
        r = q0(rows, 'イギリス')
        w = f(r['wagePremMain']) if r else float('nan'); g2.append(w)
        c = f(r['coalOwnCostMain']) if r else float('nan'); g3.append(c)
        eu = [f(q0(rows, n)['coalOwnCostMain']) for n in EU if q0(rows, n)]
        g4v = c / statistics.median(eu) if eu and isnum(c) else float('nan'); g4.append(g4v)
        print(f'  s{sd}: lpm {" ".join(row)} 割増 {w:.4f} 石炭 {c:.4f} G4 {g4v:.3f}')
    print(f'G1（算術）本土の物価の乗数 0.95〜1.05：{sum(1 for v in g1 if isnum(v) and 0.95 <= v <= 1.05)}/{len([v for v in g1 if isnum(v)])}  {verdict(all(0.95 <= v <= 1.05 for v in g1 if isnum(v)) and any(isnum(v) for v in g1))}')
    print(f'G2（算術）英本土の割増 0.8〜1.25：中央値 {med(g2):.4f}  {verdict(0.8 <= med(g2) <= 1.25)}')
    print(f'G3 英本土の石炭の自己費用 0.30〜0.36：中央値 {med(g3):.4f}  {verdict(0.30 <= med(g3) <= 0.36)}')
    print(f'G4 英÷他の欧州の中央値 ≦1.15：中央値 {med(g4):.3f}  {verdict(med(g4) <= 1.15)}')

    print('\n[記録 R1・R2・R3・R4・R10（四半期 0・種の中央値）]')
    for n in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', '清', 'マラータ同盟'):
        rs = [q0(rows, n) for rows in B.values()]
        rs = [r for r in rs if r]
        if not rs: continue
        m = lambda k: med([f(r[k]) for r in rs])
        print(f'  {n}: R1 蒸気÷賃金 {m("steamMcOverWageMain"):.3f}／R4 労働の産出÷需要 {m("laborProdOverDemandMain"):.3f}／労働の値段÷参照 {m("laborPriceOverRefMain"):.3f}／R2 束の値段÷参照 鋼 {m("bundleSteelOverRefMain"):.2f} 時代 {m("bundleEraOverRefMain"):.2f}'
              f'／R3 鉄 需要÷産出 {m("ironDemand")/max(1e-12, m("ironProd")):.2f} 鋼 {m("steelDemand")/max(1e-12, m("steelProd")):.2f} 機械 {m("machDemand")/max(1e-12, m("machProd")):.2f}／植民地の割増 {m("wagePremCol"):.3f} 下限の人口割合 {m("colWagePremFloorShare"):.3f}')

    # ---- G5・G5b・G6
    print('\n[G5・G5b・G6（種ごと・全ての国をプール）]')
    g5 = []; g6 = []; yr = {1850: [], 1880: [], 1913: []}; g6all = []
    for sd, rows in sorted(B.items()):
        w = win(rows, 1783, 1829)
        a5 = pooled(w, 'bundleEraMonths', 'bundleMonths'); g5.append(a5)
        a6 = pooled(w, 'bundleFlips', 'bundleMonths', 12.0); g6.append(a6)
        g6all.append(pooled(rows, 'bundleFlips', 'bundleMonths', 12.0))
        for y in yr: yr[y].append(pooled(win(rows, y, y), 'bundleEraMonths', 'bundleMonths'))
        print(f'  s{sd}: G5 {a5:.3f}  G6 {a6:.2f}（全期間 {g6all[-1]:.2f}）  時代の組の割合 1850 {yr[1850][-1]:.3f} 1880 {yr[1880][-1]:.3f} 1913 {yr[1913][-1]:.3f}')
    print(f'G5 時代の組の割合（1783〜1829）の中央値 ≧0.9：{med(g5):.3f}  {verdict(med(g5) >= 0.9)}')
    print(f'G5b 1913 年の時代の組の割合の中央値 ≦0.5：{med(yr[1913]):.3f}  {verdict(med(yr[1913]) <= 0.5)}（記録 1850 {med(yr[1850]):.3f}・1880 {med(yr[1880]):.3f}）')
    print(f'G6 組の切り替え÷（州・年）の平均 ≦1.0：{mean(g6):.3f}  {verdict(mean(g6) <= 1.0)}（全期間の平均 {mean(g6all):.3f}）')
    # 国ごとの分布（記録）
    nat_flip = {}
    for rows in B.values():
        for r in win(rows, 1783, 1829):
            d = nat_flip.setdefault(r['nation'], [0.0, 0.0])
            d[0] += f(r['bundleFlips']); d[1] += f(r['bundleMonths'])
    big = {n: 12 * d[0] / d[1] for n, d in nat_flip.items() if d[1] >= 24 * len(B)}
    over = sorted([(v, n) for n, v in big.items() if v > 1.0], reverse=True)
    print(f'  記録：月数が十分な国 {len(big)} のうち切り替え÷（州・年）が 1.0 を超える国 {len(over)}  上位 ' + ' '.join(f'{n}={v:.2f}' for v, n in over[:8]))

    # ---- G7
    print('\n[G7 束の最低の配給率（英・仏・普・米・1783〜1820・種ごとにプール）]')
    nats7 = ('イギリス', 'フランス', 'プロイセン', 'アメリカ')
    def g7(rows):
        w = [r for r in win(rows, 1783, 1820) if r['nation'] in nats7]
        return pooled(w, 'bottleneckRationSum', 'bottleneckMonths')
    wins = 0; both = 0
    for sd in sorted(B):
        b = g7(B[sd]); a = g7(A[sd]) if sd in A else float('nan')
        if isnum(a) and isnum(b):
            both += 1; wins += (b > a)
        print(f'  s{sd}: B {b:.3f}' + (f'  A {a:.3f}  B>A {b > a}' if isnum(a) else '  A なし'))
    if both: print(f'G7 B>A の種 {wins}/{both}（≧8/10）  {verdict(wins >= 8 and both == 10)}（種が 10 に満たないときは FAIL 表示＝判定できない）')
    else: print('G7 読めない（腕 A の値なし）')

    # ---- G8
    print('\n[G8 英の資本財の能力（1800 年の最後の行÷四半期 0）]')
    g8 = []
    for sd, rows in sorted(B.items()):
        uk = [r for r in rows if r['nation'] == 'イギリス']
        c0 = next((f(r['capGoodsCapacity']) for r in uk if r['quarter'] == '0'), float('nan'))
        y1800 = [r for r in uk if int(f(r['year'])) == 1800]
        c1800 = f(max(y1800, key=lambda r: int(r['quarter']))['capGoodsCapacity']) if y1800 else float('nan')
        mn = min((f(r['capGoodsCapacity']) for r in uk if int(f(r['year'])) <= 1800 and isnum(f(r['capGoodsCapacity']))), default=float('nan'))
        mx = max((f(r['capGoodsCapacity']) for r in uk if int(f(r['year'])) <= 1800 and isnum(f(r['capGoodsCapacity']))), default=float('nan'))
        g8.append(c1800 / c0 if c0 > 0 else float('nan'))
        print(f'  s{sd}: Q0 {c0:.0f} 1800 年末 {c1800:.0f} 比 {g8[-1]:.3f}（1800 年までの最小 {mn / c0:.3f}・最大 {mx / c0:.3f}）')
    print(f'G8 英の 1800 年末÷開幕 ≧0.9 の種 {sum(1 for v in g8 if isnum(v) and v >= 0.9)}/{len(g8)}（≧8/10）  {verdict(sum(1 for v in g8 if isnum(v) and v >= 0.9) >= 8 and len(g8) == 10)}')

    # ---- G9・G9'
    print('\n[G9・G9′ 鋳鉄の機械]')
    g9 = []; g9p = []; sh = {1850: [], 1880: [], 1913: []}
    for sd, rows in sorted(B.items()):
        w = win(rows, 1783, 1829)
        g9.append(pooled(w, 'castMachOutput', 'castMachRegions'))
        g9p.append(pooled(w, 'machOutCastMain', 'machOutAllMain'))
        for y in sh: sh[y].append(pooled(win(rows, y, y), 'machOutCastMain', 'machOutAllMain'))
        print(f'  s{sd}: G9 {g9[-1]:.3f}  G9′ 鋳鉄の機械の割合 {g9p[-1]:.3f}  1850 {sh[1850][-1]:.3f} 1880 {sh[1880][-1]:.3f} 1913 {sh[1913][-1]:.3f}')
    print(f'G9 働いている州の割合の中央値 ≧0.5：{med(g9):.3f}  {verdict(med(g9) >= 0.5)}')
    print(f'G9′ 機械の産出のうち鋳鉄の機械の割合（1783〜1829）の中央値 ≧0.5：{med(g9p):.3f}  {verdict(med(g9p) >= 0.5)}（記録 1850 {med(sh[1850]):.3f}・1880 {med(sh[1880]):.3f}・1913 {med(sh[1913]):.3f}）')

    # ---- 記録 R10・R11・R12
    print('\n[記録 R10 植民地（1783〜1829・全ての国をプールした人口加重の近似＝行の colPopK で重み）]')
    cw = 0.0; cf = 0.0; cl = 0.0
    for rows in B.values():
        for r in win(rows, 1783, 1829):
            p = f(r['colPopK'])
            if p > 0 and isnum(f(r['colWagePremFloorShare'])):
                cw += p; cf += p * f(r['colWagePremFloorShare']); cl += p * f(r['colLpmFloorShare'])
    print(f'  植民地の人口のうち賃金の割増が下限（0.25）の割合 {cf / cw if cw else float("nan"):.3f}／物価の乗数が下限の割合 {cl / cw if cw else float("nan"):.3f}')
    print('[記録 R11 資本財の能力の上側（1783〜1800・各国 max÷Q0・種の中央値）]')
    for n in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', '清'):
        v = []
        for rows in B.values():
            nr = [r for r in rows if r['nation'] == n and int(f(r['year'])) <= 1800]
            c0 = next((f(r['capGoodsCapacity']) for r in nr if r['quarter'] == '0'), float('nan'))
            if nr and c0 > 0: v.append(max(f(r['capGoodsCapacity']) for r in nr) / c0)
        print(f'  {n}: {med(v):.3f}')
    print('[記録 R12 早期の退出（1800 年までに行が終わる国・種ごと）]')
    for sd, rows in sorted(B.items()):
        last = {}
        for r in rows: last[r['nation']] = max(last.get(r['nation'], 0), int(f(r['year'])))
        gone = sorted(n for n, y in last.items() if y < 1800)
        print(f'  s{sd}: 全 {len(last)} 国のうち 1800 年より前に終わる {len(gone)}  ' + ' '.join(gone[:8]))

if __name__ == '__main__':
    main()
