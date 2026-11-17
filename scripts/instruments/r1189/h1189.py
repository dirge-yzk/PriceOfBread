# (1189) 読む線 c118dl 問い3：hd（StockpilePanicWeight 0.5→0）の副作用＝主食の不足の行が増えたのに飢えの死が減った。
#  同じ木・同じ種の対（hd 0 と hd 0.5）で、不足の深さの分布と飢えの死を並べる。
#   国×四半期：nation_s の foodMetShare（Σ FoodMet÷Σ FoodNeed・その四半期の最後の月の値）を深さの刻みに分け、行数と人口の重みを数える。
#   飢えの死：pop_s の scope N の deathsFamineK（四半期の和）を、同じ国×四半期の foodMetShare の刻みへ帰す。
#   州：region_s の foodFillShare（州の Σ FoodMet÷Σ FoodNeed・最後の月）に、エンジンの式（Simulation.cs:2303〜:2306＝充足 f＜0.8 のとき
#       月の超過死亡＝0.9÷12×((0.8−f)/0.8)^4×人口）を当てた〈州の粒の写し〉を 3 か月分数える（POP の粒ではない＝州の中の散らばりは見えない）。
#  使い方: python -X utf8 -I h1189.py <名> <ins> <種の起点> <種の終わり> [...4 つ組を並べる]
import csv, math, os, sys, collections
sys.stdout.reconfigure(newline='\n')
csv.field_size_limit(10**8)
def qn(v): return int(str(v).lstrip('q'))
def fl(v):
    try: return float(v)
    except (ValueError, TypeError): return float('nan')
BINS = [(0.0, 0.5), (0.5, 0.7), (0.7, 0.8), (0.8, 0.9), (0.9, 9.0)]
def bn(f):
    for i, (a, b) in enumerate(BINS):
        if a <= f < b: return i
    return len(BINS) - 1
FLOOR, RATE, EXP = 0.8, 0.9, 4.0   # Coefficients.p02 :68〜:70 の既定（ef7db3cc8）
def eng(f, popK): return 3 * RATE / 12 * ((FLOOR - f) / FLOOR) ** EXP * popK if f < FLOOR else 0.0
a = sys.argv[1:]
for i in range(0, len(a), 4):
    name, B, sb, se = a[i], a[i + 1], int(a[i + 2]), int(a[i + 3])
    rows = collections.Counter(); popw = collections.Counter(); famB = collections.Counter(); famWP = collections.Counter(); famMiss = collections.Counter()
    defW = collections.Counter(); reg = collections.Counter(); regPop = collections.Counter(); engD = collections.Counter(); seeds = 0
    cls = collections.Counter()
    for s in range(sb, se + 1):
        f = os.path.join(B, f'nation_s{s}.tsv')
        if not os.path.exists(f): continue
        seeds += 1
        nat = {}
        for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
            fm, pk, aw = fl(r.get('foodMetShare')), fl(r.get('populationK')), fl(r.get('atWarMonths'))
            if not (math.isfinite(fm) and pk >= 10 and aw == aw): continue
            w = 'war' if aw > 0 else 'peace'
            k = bn(fm); nat[(qn(r['quarter']), r['nation'])] = (w, k)
            rows[(w, k)] += 1; popw[(w, k)] += pk; defW[w] += pk * max(0.0, 0.9 - fm)
        pf = os.path.join(B, f'pop_s{s}.tsv')
        if os.path.exists(pf):
            for r in csv.DictReader(open(pf, encoding='utf-8'), delimiter='\t'):
                d = fl(r['deathsFamineK'])
                if not (d == d): continue
                if r['scope'] == 'C':
                    cls[r['className']] += d; continue
                if r['scope'] != 'N': continue
                key = nat.get((qn(r['quarter']), r['nation']))
                if key is None: famMiss['nat_row_missing'] += d; continue
                famB[key] += d; famWP[key[0]] += d
        rf = os.path.join(B, f'region_s{s}.tsv')
        if os.path.exists(rf):
            for r in csv.DictReader(open(rf, encoding='utf-8'), delimiter='\t'):
                ff, pk = fl(r['foodFillShare']), fl(r['populationK'])
                if not (math.isfinite(ff) and pk > 0): continue
                key = nat.get((qn(r['quarter']), r['nation']))
                w = key[0] if key else 'unk'
                k = bn(ff); reg[(w, k)] += 1; regPop[(w, k)] += pk; engD[w] += eng(ff, pk)
    print(f'## {name}\t{B}\t種 {sb}..{se}（読めた {seeds}）')
    for w in ('war', 'peace'):
        n = sum(rows[(w, k)] for k in range(len(BINS)))
        print(f'  国×四半期 {w}: 行 {n}\t' + '\t'.join(f'{BINS[k][0]}-{BINS[k][1] if BINS[k][1] < 9 else ""}:{rows[(w, k)]}({rows[(w, k)] / max(n, 1):.4f})' for k in range(len(BINS))) + f'\t<0.9 計 {sum(rows[(w, k)] for k in range(4)) / max(n, 1):.4f}')
        print(f'    飢えの死K {w}: 計 {famWP[w]:.1f}\t' + '\t'.join(f'b{k}:{famB[(w, k)]:.1f}' for k in range(len(BINS))) + f'\t人口×(0.9−f)の和 {defW[w]:.1f}')
    for w in ('war', 'peace', 'unk'):
        n = sum(reg[(w, k)] for k in range(len(BINS)))
        if n == 0: continue
        print(f'  州×四半期 {w}: 行 {n}\t' + '\t'.join(f'b{k}:{reg[(w, k)]}({reg[(w, k)] / n:.4f})' for k in range(len(BINS))) + f'\tエンジンの式（州の粒）K {engD[w]:.1f}')
    print('  階級ごとの飢えの死K（scope C・全期）: ' + ' '.join(f'{c}:{v:.1f}' for c, v in cls.most_common(8)) + f'\t帰せない {dict(famMiss)}')
