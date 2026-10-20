"""(459) 線 lw 段0 の読み手＝労働力の値段の決まり方（読むだけ・シミュは回さない）。
  python an_lw459.py <計器つき10種の生のフォルダ（例 D:/PoB_work/keep_457/m457_ins）> > report.txt
  入力（標準の計器・INS=1）：factory_s<seed>.tsv（労働3財の能力・需要・産出）・engineer_s<seed>.tsv（蒸気機関の行の T 行＝代替する物の値段 replaced〔労働力の平滑化した実質の値段〕・
        新しいレシピの費用 cost・必要性の項 needTerm）・money_s<seed>.tsv（費用の物価水準÷物価水準）・pop_s<seed>.tsv（階級の所得と籠）・class_s<seed>.tsv。
  表：
    [U]  労働3財の使われ方＝産出÷能力・需要÷能力・能力÷担い手千人（供給曲線のどこで取引されているか）
    [P]  労働力の実質の値段÷人力の自己費用（0.35×費用物価水準÷物価水準）＝1 に近いほど〈隠れた床〉に張り付いている
    [S]  蒸気機関の行：代替する物の値段（replaced）・新しいレシピの費用・必要性が正の T 行の割合（国×期）
    [I]  階級（農民・工場労働者）の所得と籠＝英と清で 1.7 倍以上違う量の探し（労働力の単位の値段は同じでも所得は 2〜3 倍違う）
    [E]  弾力の見積り＝均衡の式 p＝mc×(1＋SupplySlope×u)（u＝需要÷能力）から ε＝d ln p／d ln D＝s·u／(1＋s·u)
  限界の申告：①T 行は 8 四半期ごと・②factory_s の量は〈挙げた国〉だけ（アメリカは money_s に無く [P] が空）・③replaced は州の需要加重の平均を物価水準で割った値＝人力の自己費用と州の乗数（LocalPriceMultiplier）の差は入らない。
"""
import csv, sys, io, statistics as st, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
B = (sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_457/m457_ins').rstrip('/') + '/'
NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '清', '日本', 'オスマン']
PER = [(1790, 1809), (1810, 1839), (1840, 1869), (1870, 1899), (1900, 1950)]
PN = ['1790-1809', '1810-1839', '1840-1869', '1870-1899', '1900-1950']
SEEDS = range(1, 11)
SUPPLY_SLOPE = 0.5          # Coefficients.cs:30（読んだ値・この読み手は係数を読まない）
SELF_COST = 0.35            # Simulation.cs:451・1162（人力の MarginalCost）


def rows(path):
    with open(path, encoding='utf-8') as f:
        yield from csv.DictReader(f, delimiter='\t')


def per(y):
    for i, (a, b) in enumerate(PER):
        if a <= y <= b:
            return i
    return -1


def med(v):
    return st.median(v) if v else float('nan')


def fmt(x, f='%.3f'):
    return f % x if x == x else '-'


# ---------------------------------------------------------------- [U]
U = collections.defaultdict(list)
for s in SEEDS:
    for r in rows(B + 'factory_s%d.tsv' % s):
        n = r['nation']
        if n not in NAT:
            continue
        i = per(int(r['year']))
        if i < 0:
            continue
        for g in ('plainLabor', 'advLabor', 'farmLabor'):
            cap = float(r[g + 'SupplyCapacityUnitsPerMonth']); dem = float(r[g + 'MarketDemandUnitsPerMonth'])
            prod = float(r[g + 'MarketProducedUnitsPerMonth']); pk = float(r[g + 'SupplierPopK'])
            if cap > 0:
                U[(g, n, i)].append((prod / cap, dem / cap, cap / pk if pk > 0 else float('nan'), dem / pk if pk > 0 else float('nan')))
print('== [U] 労働3財の使われ方（国×期の中央値・10種）  産出÷能力 | 需要÷能力 | 能力÷担い手千人 | 需要÷担い手千人')
for g in ('plainLabor', 'advLabor', 'farmLabor'):
    print('-- %s' % g)
    print('国'.ljust(8) + ' '.join(p.center(34) for p in PN))
    for n in NAT:
        print(n.ljust(8) + ' '.join(('  %s %s %6.1f %6.2f' % (fmt(med([x[0] for x in U[(g, n, i)]]), '%.3f'), fmt(med([x[1] for x in U[(g, n, i)]]), '%.3f'),
                                                          med([x[2] for x in U[(g, n, i)]]), med([x[3] for x in U[(g, n, i)]]))).ljust(34) if U[(g, n, i)] else '-'.center(34) for i in range(len(PER))))

# ---------------------------------------------------------------- [P][S]
P = collections.defaultdict(list); S = collections.defaultdict(list); C = collections.defaultdict(list); N = collections.defaultdict(list); R = collections.defaultdict(list)
for s in SEEDS:
    ml = {}
    for r in rows(B + 'money_s%d.tsv' % s):
        try:
            ml[(r['nation'], int(r['year']))] = float(r['costPriceLevelAfterIndex']) / float(r['priceLevelAfterIndex'])
        except Exception:
            pass
    for r in rows(B + 'engineer_s%d.tsv' % s):
        if r['kind'] != 'T' or r['row'] != '蒸気機関' or r['nation'] not in NAT:
            continue
        y = int(r['year']); i = per(y)
        if i < 0:
            continue
        n = r['nation']; rep = float(r['replaced']); cost = float(r['cost'])
        R[(n, i)].append(rep); C[(n, i)].append(cost); N[(n, i)].append(1 if float(r['needTerm']) > 0 else 0)
        if (n, y) in ml:
            P[(n, i)].append(rep / (SELF_COST * ml[(n, y)]))
print()
print('== [P] 労働力の平滑化した実質の値段 ÷ 人力の自己費用（0.35×費用物価水準÷物価水準）  国×期の中央値・10種')
print('国'.ljust(8) + ' '.join(p.center(11) for p in PN))
for n in NAT:
    print(n.ljust(8) + ' '.join(fmt(med(P[(n, i)])).center(11) for i in range(len(PER))))
print()
print('== [S] 蒸気機関の行（T 行）  代替する物の値段 replaced＝労働力の実質の値段 | 蒸気の新しいレシピの費用 cost | 必要性の項 > 0 の割合')
print('国'.ljust(8) + ' '.join(p.center(26) for p in PN))
for n in NAT:
    print(n.ljust(8) + ' '.join(('  %s %s %3d%%' % (fmt(med(R[(n, i)])), fmt(med(C[(n, i)])), round(100 * sum(N[(n, i)]) / len(N[(n, i)])))).ljust(26) if N[(n, i)] else '-'.center(26) for i in range(len(PER))))
print('最大÷最小（replaced・主要 8 国の中央値の比）： ' + ' '.join('%s=%s' % (PN[i], fmt(max(med(R[(n, i)]) for n in NAT if R[(n, i)]) / min(med(R[(n, i)]) for n in NAT if R[(n, i)]), '%.2f')) for i in range(len(PER))))
print('清÷英（replaced）： ' + ' '.join('%s=%s' % (PN[i], fmt(med(R[('清', i)]) / med(R[('イギリス', i)]), '%.2f')) for i in range(len(PER))))
print('清÷英（replaced÷自己費用）： ' + ' '.join('%s=%s' % (PN[i], fmt(med(P[('清', i)]) / med(P[('イギリス', i)]), '%.2f')) for i in range(len(PER))))

# ---------------------------------------------------------------- [I]
INC = collections.defaultdict(list)
for s in SEEDS:
    pl = {}
    for r in rows(B + 'money_s%d.tsv' % s):
        try:
            pl[(r['nation'], int(r['year']))] = float(r['priceLevelAfterIndex'])
        except Exception:
            pass
    for r in rows(B + 'pop_s%d.tsv' % s):
        if r['scope'] != 'C' or r['className'] not in ('農民', '工場労働者') or r['nation'] not in NAT:
            continue
        y = int(r['year']); i = per(y)
        if i < 0 or (r['nation'], y) not in pl:
            continue
        try:
            ya = float(r['yAdultPerMonth']); bk = float(r['basketPerMonth'])
        except Exception:
            continue
        if bk > 0:
            INC[(r['className'], r['nation'], i)].append((ya / pl[(r['nation'], y)], bk / pl[(r['nation'], y)], ya / bk))
print()
print('== [I] 階級の所得（大人 1 人・月・物価水準で割る）| 籠（1 人・月・物価水準で割る）| 所得÷籠  国×期の中央値')
for cl in ('農民', '工場労働者'):
    print('-- ' + cl)
    for n in NAT:
        print(n.ljust(8) + ' '.join(('  %5.2f %5.2f %5.2f' % tuple(med([x[k] for x in INC[(cl, n, i)]]) for k in range(3))).ljust(22) if INC[(cl, n, i)] else '-'.center(22) for i in range(len(PER))))
print('清÷英（工場労働者の所得÷物価水準）： ' + ' '.join('%s=%s' % (PN[i], fmt(med([x[0] for x in INC[('工場労働者', '清', i)]]) / med([x[0] for x in INC[('工場労働者', 'イギリス', i)]]), '%.2f')) for i in range(len(PER))))
print('清÷英（農民の所得÷物価水準）： ' + ' '.join('%s=%s' % (PN[i], fmt(med([x[0] for x in INC[('農民', '清', i)]]) / med([x[0] for x in INC[('農民', 'イギリス', i)]]), '%.2f')) for i in range(len(PER))))
print('清÷英（籠÷物価水準）： ' + ' '.join('%s=%s' % (PN[i], fmt(med([x[1] for x in INC[('工場労働者', '清', i)]]) / med([x[1] for x in INC[('工場労働者', 'イギリス', i)]]), '%.2f')) for i in range(len(PER))))
print('清÷英（需要÷能力＝緊張 u）： ' + ' '.join('%s=%s' % (PN[i], fmt(med([x[1] for x in U[('plainLabor', '清', i)]]) / med([x[1] for x in U[('plainLabor', 'イギリス', i)]]), '%.2f')) for i in range(len(PER))))

# ---------------------------------------------------------------- [E]
print()
print('== [E] 今の供給曲線の弾力：均衡 p＝mc×(1＋s·u)（s＝SupplySlope %.2f・u＝需要÷能力）  ε＝d ln p／d ln D＝s·u／(1＋s·u)  と 価格の幅 p／mc' % SUPPLY_SLOPE)
for g in ('plainLabor', 'farmLabor'):
    us = [x[1] for n in NAT for i in range(len(PER)) for x in U[(g, n, i)]]
    print('%s：u の最小 %.3f・中央値 %.3f・最大 %.3f  ⇒ ε の中央値 %.3f・p／mc の中央値 %.3f・最大 %.3f' % (
        g, min(us), med(us), max(us), SUPPLY_SLOPE * med(us) / (1 + SUPPLY_SLOPE * med(us)), 1 + SUPPLY_SLOPE * med(us), 1 + SUPPLY_SLOPE * max(us)))
print('（u を倍にしても値段は %.1f％ しか動かない。英と清の u の比 1.1〜1.6 倍は、値段の比 1.01〜1.03 倍にしかならない）' % (100 * SUPPLY_SLOPE * 0.05))
