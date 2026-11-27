# -*- coding: utf-8 -*-
"""(1407) c150rp 紙の台本：線路と機関車の財の調達を同じ国の州の在庫へ（旗 RailProcureNational）の腕の前の読み。

使い方：python paper_rp.py <生のフォルダ（trrail_s<種>.tsv・region_s<種>.tsv・sellr_s<種>.tsv）> [種の並び 1-10]
読む物（列名で引く）：
  trrail_s：year・nation・region・phiEnd・needEnd・boughtCapPerYear・procureGotUnitsPerYear・procureStockZeroGoodMonths・railTrackEnd
  region_s：year・nation・region・tier（Core/Integrated＝本国格・Colonial は除く）
  sellr_s：year・region・ownerNation・good（線路・機関車）・stock（10 年ごとの写し）
出す物：
  B 型（バーミンガム型）＝本国格の州で φ＜0.2・必要＞1,000・買った容量 0 の年が 10 年（40 四半期）以上続く州×種の数と名
  国の中から買った量（観測量）＝boughtCapPerYear÷TransportCapPerUnit(30)−procureGotUnitsPerYear（旗 0 では 0＝門の〈旗 0 で落ちる〉の確かめ）
  procureStockZeroGoodMonths の和（種ごと）
  英の州の線路の和（1850・1880）
  B 型の州×年のうち 10 年ごとの写しに当たる年で、同じ国の他の州に線路・機関車の在庫がある割合（段 0 の予言の材料）
"""
import sys, os, csv
from collections import defaultdict

CAP_PER_UNIT = 30.0
csv.field_size_limit(10**9)


def rows(path):
    with open(path, 'r', encoding='utf-8-sig', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        h = next(rd)
        ix = {k: i for i, k in enumerate(h)}
        for r in rd:
            yield ix, r


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def seed_list(s):
    if '-' in s:
        a, b = s.split('-')
        return list(range(int(a), int(b) + 1))
    return [int(x) for x in s.split(',')]


def one_seed(d, s):
    tier = {}
    for ix, r in rows(os.path.join(d, f'region_s{s}.tsv')):
        tier[(int(r[ix['year']]), r[ix['nation']], r[ix['region']])] = r[ix['tier']]
    tr = defaultdict(dict)  # (nation,region) -> year -> (phi,need,bought,got,zero,track)
    nat_units_max = 0.0
    nat_units_sum = 0.0
    zero_sum = 0
    uk = defaultdict(float)
    for ix, r in rows(os.path.join(d, f'trrail_s{s}.tsv')):
        y = int(r[ix['year']]); n = r[ix['nation']]; g = r[ix['region']]
        phi = fl(r[ix['phiEnd']]); need = fl(r[ix['needEnd']]); b = fl(r[ix['boughtCapPerYear']])
        got = fl(r[ix['procureGotUnitsPerYear']]); z = int(fl(r[ix['procureStockZeroGoodMonths']]))
        tk = fl(r[ix['railTrackEnd']])
        tr[(n, g)][y] = (phi, need, b, got, z, tk)
        nu = b / CAP_PER_UNIT - got
        if nu == nu:
            nat_units_sum += nu
            nat_units_max = max(nat_units_max, abs(nu))
        zero_sum += z
        if n == 'イギリス' and y in (1850, 1880):
            uk[y] += tk
    # B 型
    btype = []
    btype_years = []  # (nation, region, year)
    for (n, g), ys in tr.items():
        run = []
        best = []
        for y in sorted(ys):
            phi, need, b, got, z, tk = ys[y]
            t = tier.get((y, n, g), '')
            hit = t in ('Core', 'Integrated') and phi < 0.2 and need > 1000 and not (b > 0)
            if hit:
                run.append(y)
            else:
                if len(run) > len(best): best = run
                run = []
        if len(run) > len(best): best = run
        if len(best) >= 10:
            btype.append((n, g, best[0], best[-1]))
            btype_years += [(n, g, y) for y in best]
    # 同じ国の他の州の在庫（10 年ごとの写し）
    want_years = {y for (_, _, y) in btype_years}
    stock = defaultdict(float)   # (year, nation, good) -> 和
    stock_r = defaultdict(float)  # (year, nation, region, good)
    if want_years:
        for ix, r in rows(os.path.join(d, f'sellr_s{s}.tsv')):
            gd = r[ix['good']]
            if gd != '線路' and gd != '機関車':
                continue
            y = int(r[ix['year']])
            if y not in want_years:
                continue
            st = fl(r[ix['stock']])
            if not (st > 1e-9):
                continue
            n = r[ix['ownerNation']]; g = r[ix['region']]
            stock[(y, n, gd)] += st
            stock_r[(y, n, g, gd)] += st
    snap = 0; avail_rail = 0; avail_loco = 0; avail_units = []
    for (n, g, y) in btype_years:
        if y not in (1790, 1800, 1810, 1820, 1830, 1840, 1850, 1860, 1870, 1880, 1890, 1900, 1910, 1913, 1920, 1930, 1940, 1950):
            continue
        snap += 1
        o_r = stock[(y, n, '線路')] - stock_r[(y, n, g, '線路')]
        o_l = stock[(y, n, '機関車')] - stock_r[(y, n, g, '機関車')]
        if o_r > 1e-9: avail_rail += 1
        if o_l > 1e-9: avail_loco += 1
        avail_units.append(min(o_r, o_l))
    return dict(btype=btype, nat_max=nat_units_max, nat_sum=nat_units_sum, zero=zero_sum, uk=dict(uk),
                snap=snap, avail_rail=avail_rail, avail_loco=avail_loco,
                avail_med=(sorted(avail_units)[len(avail_units) // 2] if avail_units else float('nan')))


def main():
    d = sys.argv[1]
    seeds = seed_list(sys.argv[2]) if len(sys.argv) > 2 else list(range(1, 11))
    tot_b = 0; tot_seed_b = 0
    print('seed\tbtypeN\tnatUnitsMaxAbs\tnatUnitsSum\tstockZeroGoodMonths\tukRail1850\tukRail1880\tbtypeSnapYears\tavailRail\tavailLoco\tavailMinMedian')
    lists = []
    for s in seeds:
        o = one_seed(d, s)
        tot_b += len(o['btype']); tot_seed_b += 1 if o['btype'] else 0
        print(f"{s}\t{len(o['btype'])}\t{o['nat_max']:.6g}\t{o['nat_sum']:.6g}\t{o['zero']}\t{o['uk'].get(1850, float('nan')):.6g}\t{o['uk'].get(1880, float('nan')):.6g}\t{o['snap']}\t{o['avail_rail']}\t{o['avail_loco']}\t{o['avail_med']:.6g}")
        for (n, g, a, b) in o['btype']:
            lists.append(f'  s{s}\t{n}\t{g}\t{a}-{b}')
    print(f'TOTAL\tbtype={tot_b}\tseedsWithBtype={tot_seed_b}/{len(seeds)}')
    print('B 型の州×種（種・国・州・続いた年）：')
    for x in lists:
        print(x)


if __name__ == '__main__':
    main()
