"""(651) an_mon.py の自己試験の合成入力。項目ごとに 1 つの極端を仕込んだ小さな TSV（6 種×4 国×668 四半期）を <出力>/ins に書く。
  仕込みと期待は selftest.py の EXPECT／ABSENT が持つ（生成と期待は別の場所に書く＝同語反復にしない）。python make_fixtures.py <出力フォルダ>"""
import os, sys

NATIONS = ['N1', 'N2', 'N3', 'N4']
NQ = 668
SEEDS = [1, 2, 3, 4, 5, 6]

def w(d, name, s, header, rows):
    with open(os.path.join(d, '%s_s%d.tsv' % (name, s)), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\t'.join(header) + '\n')
        for r in rows: f.write('\t'.join(str(x) for x in r) + '\n')

def gen(root):
    d = os.path.join(root, 'ins'); os.makedirs(d, exist_ok=True)
    for s in SEEDS:
        qs = range(NQ)
        yr = lambda q: 1783 + q // 4
        # ---- nation_s
        H = ['quarter', 'year', 'nation', 'populationK', 'nominalGdpPerYear', 'civilWarMonths', 'conscriptionLaw', 'mobilizedFlag', 'coreRegions', 'integratedRegions',
             'ledgerBorrowCumulative', 'ledgerDevalueCumulative', 'sovereignDefaultCumulativeCount', 'monopolyPushHitCount', 'priceLevel']
        rows = []
        for n in NATIONS:
            for q in qs:
                gdp = 1000
                if n == 'N3' and q >= 4 * 30: gdp = 0
                civ = 0
                if n == 'N2' and s == 3 and q == 462: civ = 3
                core = 10
                if n == 'N3' and s == 1 and q >= 491: core = 3
                pl = 1.0
                if n == 'N2' and q >= 468: pl = 5000.0
                if n == 'N3' and s == 1 and q >= 500: pl = 5000.0
                if n == 'N4' and s <= 5 and q >= 300: pl = 5000.0
                mob = 1 if (n == 'N1' and s in (1, 2) and q == 100) else 0
                borrow = q if n == 'N1' else 0
                rows.append((q, yr(q), n, 1000, gdp, civ, 1, mob, core, 0, borrow, 0, 0, 0, pl))
        w(d, 'nation', s, H, rows)
        # ---- scalars_s
        H = ['quarter', 'year', 'nation', 'polityCode', 'defeatShock', 'educationLaw', 'suffrage', 'censorship', 'nationalizedMaskCode']
        rows = []
        for n in NATIONS:
            for q in qs:
                y = yr(q)
                pol = 3
                if n == 'N3' and s in (3, 4) and 1900 <= y < 1910: pol = 6
                suf = 1
                if n == 'N2': suf = ((y - 1783) // 20) % 3
                de = 0.5 if (n == 'N2' and s == 2 and q == 460) else 0.0
                rows.append((q, y, n, pol, de, 2, suf, 0, 0))
        w(d, 'scalars', s, H, rows)
        w(d, 'labor', s, ['quarter', 'year', 'nation', 'lawCode', 'plainTinyPriceRegionShare', 'farmTinyPriceRegionShare'],
          [(q, yr(q), n, 0, 0.0, 0.9 if n == 'N1' else 0.0) for n in NATIONS for q in qs])
        w(d, 'reform', s, ['quarter', 'year', 'nation', 'legitimacyShare'], [(q, yr(q), n, 0.8) for n in NATIONS for q in qs if q % 4 == 3])
        w(d, 'pclamp', s, ['time', 'nation', 'year', 'kind'], [('q%d' % (10 + i), 'N2', 1790, 'floor') for i in range(20)] + [('q%d' % (10 + i), 'N3', 1790, 'floor') for i in range(5)])
        # ---- class_s
        rows = []
        cls = {'0': '貴族', '1': '農民', '10': '商人', '3': '資本家', '4': '技師'}
        for n in NATIONS:
            for i in range(168):
                q = 4 * i + 3
                if n == 'N4':
                    if i < 5: sz = {'1': 980, '0': 10, '10': 10, '3': 0, '4': 0}
                    elif i < 10: sz = {'1': 700, '0': 100, '10': 0.01, '3': 100, '4': 100}
                    else: sz = {'1': 800, '0': 100, '10': 0.01, '3': 60, '4': 40}
                else: sz = {'1': 980, '0': 10, '10': 10, '3': 0, '4': 0}
                for k, v in sz.items(): rows.append(('q%d' % q, n, yr(q), k, cls[k], v))
        for i in range(20):   # N5＝class_s にだけ居る短命の国（Pa が 8 年＝25% 以上だが 10 年に満たない）
            q = 4 * i + 3
            sz = {'1': 980, '0': 10, '10': 10, '3': 0, '4': 0} if i < 8 else {'1': 700, '0': 100, '10': 100, '3': 100, '4': 0}
            for k, v in sz.items(): rows.append(('q%d' % q, 'N5', yr(q), k, cls[k], v))
        rows.append(('open', 'N1', 1783, '1', '農民', 5)); rows.append(('w3', 'N1', 1783, '1', '農民', 5))
        w(d, 'class', s, ['time', 'nation', 'year', 'classId', 'className', 'sizeK'], rows)
        # ---- stock_s
        H = ['time', 'nation', 'year', 'soldierToPopulationShare', 'atWarFlag', 'treasuryStock', 'govDebtStock']
        rows = []
        for n in NATIONS:
            for i in range(168):
                q = 4 * i + 3
                sh = 0.2 if n in ('N2', 'N3') else 0.0
                war = 1 if n == 'N3' else 0
                tre = 2000 if n == 'N1' else 100
                debt = {'N1': 0, 'N2': 4000, 'N3': 0, 'N4': (0 if i < 100 else 100)}[n]
                rows.append(('q%d' % q, n, yr(q), sh, war, tre, debt))
        rows.append(('warm', 'N1', 1783, 0, 0, 0, 999))
        w(d, 'stock', s, H, rows)
        # ---- pxgoods_s
        H = ['time', 'nation', 'year', 'goodName', 'lastProducedQty', 'lastDemandQty', 'lastShortageQty', 'lastImportQty', 'stockQty', 'priceOverRefTimesLevelMedianRatio']
        rows = []
        def row(i, n, g, prod=100, dem=100, sh=0, imp=0, stk=100, pxr=1.0): rows.append(('q%d' % (i * 8 + 1), n, 1790, g, prod, dem, sh, imp, stk, pxr))
        for i in range(68):
            for n in ('N1', 'N2', 'N3'): row(i, n, 'パン', sh=60 if i < 30 else 0)
            row(i, 'N1', '米', stk=5000 if i < 30 else 100)
            row(i, 'N1', '茶', prod=0, stk=0)
            row(i, 'N1', '塩', prod=0, stk=500)
            if i < 40: row(i, 'N1', '紙')
            elif i < 60: row(i, 'N1', '紙', prod=0.5, dem=0.5, sh=0.5)
            else: row(i, 'N1', '紙')
            row(i, 'N1', 'キニーネ', pxr=30.0 if i < 40 else 1.0)
            row(i, 'N1', 'ビール', pxr=60.0 if i < 40 else 1.0)
            row(i, 'N1', '干物', pxr=0.01 if i < 40 else 1.0)
        rows.append(('w0', 'N1', 1790, 'パン', 100, 100, 100, 0, 100, 1.0))
        w(d, 'pxgoods', s, H, rows)
        # ---- 発見
        E = [('R1', 'N1', 0), ('R2', 'N1', 100), ('R2', 'N2', 100), ('R2', 'N3', 100), ('R3', 'N3', 120), ('R5', 'N1', 50), ('R6', 'N1', 60), ('R7', 'N1', 70),
             ('R8', 'N2', 80), ('R9', 'N4', 140), ('R10', 'N2', 150)]
        w(d, 'disc', s, ['kind', 'quarter', 'year', 'nation', 'row', 'transmitTerm'], [('E', q, yr(q), n, r, 0) for r, n, q in E] + [('T', 0, 1783, 'N1', 'R4', 0)])
        w(d, 'engineer', s, ['kind', 'quarter', 'year', 'nation', 'row'], [('T', 0, 1783, 'N1', 'R%d' % i) for i in range(1, 11)])
        # ---- 動詞
        w(d, 'aib', s, ['quarter', 'year', 'nation', 'startedByCategory'], [(10, 1790, 'N1', '軽工業:2' + (';重工業:1' if s == 1 else ''))])
        w(d, 'wars', s, ['startYear', 'civilWarFlag', 'sideA', 'sideB'], [(1791, 0, 'N1', 'N2'), (1850, 0, 'N2', 'N3')])
        w(d, 'invade', s, ['year', 'invader', 'target', 'submitted'], [(1791, 'N1', 'X', 1), (1792, 'N1', 'Y', 0)])
        w(d, 'canal', s, ['canalId', 'openedQuarter', 'openedYear'], [('suez', 157 if s in (1, 2) else -1, 1822)])
        w(d, 'epi', s, ['quarter', 'year', 'nation', 'region', 'deadK'], ([(464, 1899, 'N2', 'r', 100)] if s == 4 else [(10, 1785, 'N1', 'r', 0.001)]))
        # ---- ファシズム
        fq = 4 * (1900 + s - 1783)
        w(d, 'idn', s, ['quarter', 'year', 'nation', 'knownFas'], [(q, yr(q), n, 1 if (n == 'N1' and q >= fq) else 0) for n in NATIONS for q in qs])
        w(d, 'ideology', s, ['quarter', 'year', 'nation', 'ideoNationalFascismShare'], [(q, yr(q), n, 0.2 if (n == 'N2' and yr(q) >= 1910) else 0.0) for n in NATIONS for q in qs if q % 4 == 3])
    return d

if __name__ == '__main__':
    print(gen(sys.argv[1]))
