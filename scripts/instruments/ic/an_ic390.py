# (390) 工場の能力の内訳（factory_s）と思想の割合（ideology_s）の読み手。使い方：PYTHONUTF8=1 python an_ic390.py <ins フォルダ> [種の最大=全て] [国=挙げた国のみ|全て]
# ⚠限界：laborSupplyCapacityUnitsPerMonth（人の側の器）は労働力の生産者の Capacity の和＝純粋な頭数でなく 工業化度の倍率×発見の門×働ける人口の割合 を含む。
#   断面は四半期の末の 1 か月（LastInputFill は次月用の値＝産出を決めた前月の値と 1 か月ずれる・産出/充足が 100% を超えるのはこのずれと在庫循環・慣性）。
# 読むだけ。段0＝〈工場の能力を律速しているのが頭数か投入の充足か〉を時代別・国別に出す。数字の意味は SimCore/InstrumentsCapacity.cs の頭のコメント。
import sys, glob, csv, os, re, collections
d = sys.argv[1]; smax = int(sys.argv[2]) if len(sys.argv) > 2 else 10**9
ERAS = [(1783, 1829), (1830, 1869), (1870, 1913), (1914, 1950)]
def era(y):
    for i, (a, b) in enumerate(ERAS):
        if a <= y <= b: return i
    return len(ERAS) - 1
def rows(kind):
    out = []
    for p in sorted(glob.glob(os.path.join(d, kind + '_s*.tsv'))):
        m = re.search(r'_s(\d+)\.tsv$', p); s = int(m.group(1))
        if s > smax: continue
        with open(p, encoding='utf-8') as f:
            rd = csv.DictReader(f, delimiter='\t')
            for r in rd:
                r['_seed'] = s
                for k, v in r.items():
                    if k in ('nation', '_seed'): continue
                    try: r[k] = float(v)
                    except: r[k] = float('nan')
                out.append(r)
    return out
fr = rows('factory'); ir = rows('ideology')
print(f'factory_s 行 {len(fr)}・ideology_s 行 {len(ir)}・種 {sorted({r["_seed"] for r in fr})}')
# 国ごとの行の数で「挙げた国」（毎四半期）と「年末だけ」の国を分ける
cnt = collections.Counter((r['_seed'], r['nation']) for r in fr)
mx = collections.defaultdict(int)
for (s, n), v in cnt.items(): mx[s] = max(mx[s], v)
full = {k for k, v in cnt.items() if v >= 0.9 * mx[k[0]]}   # 毎四半期の国＝その種の最大の行数の 9 割以上（年末だけの国は 1/4）
picked = sorted({k[1] for k in full})
print('毎四半期の国（挙げた国）:', ' '.join(picked))
def agg(rs):
    S = collections.defaultdict(float)
    for r in rs:
        for k in ('capacityRefValuePerMonth', 'desiredRefValuePerMonth', 'filledRefValuePerMonth', 'materialFilledRefValuePerMonth', 'outputRefValuePerMonth',
                  'notInputBoundDesiredRefValuePerMonth', 'laborBoundDesiredRefValuePerMonth', 'otherBoundDesiredRefValuePerMonth', 'unmatchedBoundDesiredRefValuePerMonth',
                  'laborBoundLossRefValuePerMonth', 'otherBoundLossRefValuePerMonth', 'unmatchedBoundLossRefValuePerMonth',
                  'laborSupplyCapacityUnitsPerMonth', 'laborNeedAtCapacityUnitsPerMonth', 'laborNeedAtDesiredUnitsPerMonth', 'laborMarketDemandUnitsPerMonth', 'laborMarketProducedUnitsPerMonth',
                  'factoryLaborPopK', 'factoryActiveCount', 'factoryCount', 'factoryBankruptCount', 'factoryIdleCount'):
            v = r[k]
            if v == v: S[k] += v
        S['n'] += 1
    return S
def pct(a, b): return f'{100 * a / b:6.1f}' if b > 0 else '   nan'
def line(label, S):
    c, de, fi, ou = S['capacityRefValuePerMonth'], S['desiredRefValuePerMonth'], S['filledRefValuePerMonth'], S['outputRefValuePerMonth']
    print(f'{label:<22}{int(S["n"]):>6} | 採算 {pct(de, c)} | 投入 {pct(fi, de)} (素材 {pct(S["materialFilledRefValuePerMonth"], de)}) | 産出/充足 {pct(ou, fi)} | '
          f'縛られない {pct(S["notInputBoundDesiredRefValuePerMonth"], de)} 労働 {pct(S["laborBoundDesiredRefValuePerMonth"], de)} 他投入 {pct(S["otherBoundDesiredRefValuePerMonth"], de)} 不一致 {pct(S["unmatchedBoundDesiredRefValuePerMonth"], de)} | '
          f'失った量（作りたい量比）労働 {pct(S["laborBoundLossRefValuePerMonth"], de)} 他投入 {pct(S["otherBoundLossRefValuePerMonth"], de)} | 労働の需要/器 {pct(S["laborNeedAtDesiredUnitsPerMonth"], S["laborSupplyCapacityUnitsPerMonth"])} 市場需要/器 {pct(S["laborMarketDemandUnitsPerMonth"], S["laborSupplyCapacityUnitsPerMonth"])} 市場産出/器 {pct(S["laborMarketProducedUnitsPerMonth"], S["laborSupplyCapacityUnitsPerMonth"])} 市場産出/需要 {pct(S["laborMarketProducedUnitsPerMonth"], S["laborMarketDemandUnitsPerMonth"])}')
print('\n① 段0＝工場の能力を律速しているのは頭数か投入の充足か（作りたい量の参照価格加重の割合%・採算＝作りたい量/能力・投入＝充足後/作りたい量）')
print('   縛られない＝LastInputFill が 1（能力と採算で決まる＝頭数の側）／労働＝最小の配給率が労働財／他投入＝労働以外の投入／不一致＝計器の限界')
print('   失った量＝分類ごとの作りたい量×(1−LastInputFill)＝縛られた〈量〉でなく縛られて実際に失った〈量〉（分類の割合は 1 未満でもわずかなら入るので、律速の強さはこちらで読む）')
print('   労働の需要/器＝作りたい量に要る労働（財）÷労働力の生産者の能力の和（人の側の器）／市場需要・市場産出＝労働力の市場の前月の需要と産出（産出/器が低い＝器でなく値段〔供給の曲線〕が産出を決めている）')
sel = [r for r in fr if (r['_seed'], r['nation']) in full]
for e, (a, b) in enumerate(ERAS):
    line(f'全国（挙げた国）{a}-{b}', agg([r for r in sel if era(r['year']) == e]))
print()
for nm in picked:
    for e, (a, b) in enumerate(ERAS):
        rs = [r for r in sel if r['nation'] == nm and era(r['year']) == e]
        if rs: line(f'{nm} {a}-{b}', agg(rs))
print('\n② 頭数と1人あたりの能力（能力＝担い手の人数×1人あたり。年ごとの国の中央値の変化を時代の始めと終わりで比べる）')
for nm in picked:
    by = collections.defaultdict(list)
    for r in sel:
        if r['nation'] == nm and r['quarter'] % 4 == 3 and r['factoryLaborPopK'] == r['factoryLaborPopK'] and r['factoryLaborPopK'] > 0:
            by[(r['year'], )].append((r['capacityRefValuePerMonth'], r['factoryLaborPopK']))
    ys = sorted(by)
    def med(xs): xs = sorted(xs); return xs[len(xs) // 2]
    print(f'{nm}: ' + ' '.join(f'{int(y[0])}:能力 {med([c for c, p in by[y]]):.0f}・人 {med([p for c, p in by[y]]):.0f}K・1人 {med([c / p for c, p in by[y]]):.1f}' for y in ys if int(y[0]) in (1790, 1830, 1870, 1914, 1950)))
print('\n③ 思想の割合（国民の集計 National と 人口加重の素の平均・種と四半期の平均）')
NAMES = [k[len('ideoNational'):-len('Share')] for k in ir[0] if k.startswith('ideoNational')] if ir else []
selI = [r for r in ir if (r['_seed'], r['nation']) in full]
for y0, y1 in ((1783, 1789), (1845, 1855), (1910, 1914), (1930, 1938), (1946, 1950)):
    print(f'-- {y0}-{y1}')
    for nm in picked:
        rs = [r for r in selI if r['nation'] == nm and y0 <= r['year'] <= y1]
        if not rs: continue
        nat = {k: sum(r['ideoNational' + k + 'Share'] for r in rs) / len(rs) for k in NAMES}
        pm = {k: sum(r['ideoPopMean' + k + 'Share'] for r in rs) / len(rs) for k in NAMES}
        top = sorted(NAMES, key=lambda k: -nat[k])[:4]
        print(f'  {nm:<10} 反動 国民 {100*nat["Reactionism"]:5.1f}% 素 {100*pm["Reactionism"]:5.1f}% | ' + ' '.join(f'{k[:6]} {100*nat[k]:4.1f}/{100*pm[k]:4.1f}' for k in top))
