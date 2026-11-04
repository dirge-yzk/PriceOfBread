# (841) c68uh 段0 の紙：都市の持ち分を出る側に足したとき、最良の差 g の符号が正→負へ返る四半期の割合（読むだけ・腕なし）
#   python -X utf8 -I paper_flip.py <events.tsv> <hol_common のあるディレクトリ>
#   events.tsv＝set<TAB>dir<TAB>seed<TAB>region<TAB>peakYear<TAB>troughYear<TAB>label（an_h2 -v4 の移民が主の事象）
#   g（国内・Territory.Step と同じ形）＝opp_d − stay_s − TF − EF(km_d)・EF＝TF×km÷(10000×Era)・Era∈[1,2]（EraMigrationScale=1 の時代の係数の範囲）
#   stay_s（旧）＝opportunityIndex×holdingFactor（region_s の列＝エンジンの Opportunity と Holding そのもの）
#   都市の持ち分の上限の形（在庫の選び方に依らない上側の見積り）：都市の在庫は事象のピークの四半期の値で動かない＝1 人あたり ρ＝都市人口_peak÷都市人口_t（ピークで 1）
#     U＝max(1,ρ)^e（e＝0.7〔LandDiminishExponent〕と 1）・新しい stay＝旧 stay×U^u（u＝州の都市の割合＝1−農村÷人口）
#   数えるのは、ピーク〜底の四半期で国内の出（migrationOut）＞0 かつ 旧の最良の g＞0 の四半期。そのうち新しい最良の g≦0 の割合。
import sys, os, math, collections
sys.path.insert(0, sys.argv[2])
from hol_common import dist
TF = 0.15
def fl(x):
    try: return float(x)
    except: return float('nan')
ev = [l.rstrip('\n').split('\t') for l in open(sys.argv[1], encoding='utf-8') if l.strip() and not l.startswith('#')]
cache = {}
def load(d, seed):
    key = (d, seed)
    if key in cache: return cache[key]
    rows = collections.defaultdict(list)   # quarter -> list of (nationIndex, region, opp, keep, pop, rural, mOut)
    with open(os.path.join(d, f'region_s{seed}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for line in fh:
            f = line.rstrip('\n').split('\t')
            rows[int(f[ix['quarter']])].append((f[ix['nationIndex']], f[ix['region']], fl(f[ix['opportunityIndex']]), fl(f[ix['holdingFactor']]),
                fl(f[ix['populationK']]), fl(f[ix['ruralPopulationK']]), fl(f[ix['migrationOutKPerQuarter']]), int(f[ix['year']])))
    cache.clear(); cache[key] = rows
    return rows
def bestg(stay, s_region, s_nat, qrows, era):
    best = -1e300
    for nat, reg, opp, keep, pop, rural, mo, yr in qrows:
        if nat != s_nat or reg == s_region or not math.isfinite(opp): continue
        g = opp - stay - TF
        if not g > 0: continue
        km = dist(s_region, reg)
        if not math.isfinite(km): km = 0.0
        g -= TF * km / (10000.0 * era)
        if g > best: best = g
    return best
print('set\tseed\tregion\tlabel\tquarters\tuMed\tflip_e07_era1\tflip_e07_era2\tflip_e1_era1\tflip_e1_era2\trhoMaxInWindow\tneedU_med')
tot = collections.Counter()
for st, d, seed, region, py, ty, label in ev:
    rows = load(d, seed)
    qs = sorted(rows)
    # この州の系列
    ser = {}
    for q in qs:
        for r in rows[q]:
            if r[1] == region: ser[q] = r
    win = [q for q in qs if q in ser and int(py) <= ser[q][7] <= int(ty)]
    if not win: print(f'{st}\t{seed}\t{region}\t{label}\t0'); continue
    # 事象のピーク＝窓の中の人口の最大の四半期・その都市人口
    qpk = max(win, key=lambda q: ser[q][4])
    upk = ser[qpk][4] - ser[qpk][5]
    n = 0; fl_ = collections.Counter(); us = []; need = []; rhomax = 0
    for q in win:
        nat, reg, opp, keep, pop, rural, mo, yr = ser[q]
        if q < qpk or not (mo > 0) or not (pop > 0): continue
        stay = opp * keep
        g1 = bestg(stay, region, nat, rows[q], 1.0)
        g2 = bestg(stay, region, nat, rows[q], 2.0)
        if not g2 > 0: continue   # 旧でも差が負＝国内の出は前の月の盤面か越境（数えない）
        n += 1
        u = max(0.0, 1.0 - rural / pop); us.append(u)
        urb = pop - rural
        rho = upk / urb if urb > 1e-9 else float('inf'); rhomax = max(rhomax, rho)
        # 返るのに要る U（Era=2＝差が最も大きい側）：stay×U^u ≧ stay＋g2 → U ≧ (1＋g2/stay)^(1/u)
        need.append((1 + g2 / stay) ** (1 / u) if u > 1e-9 and stay > 0 else float('inf'))
        for e in (0.7, 1.0):
            U = max(1.0, rho) ** e
            ns = stay * U ** u
            for era, tag in ((1.0, 'era1'), (2.0, 'era2')):
                if not bestg(ns, region, nat, rows[q], era) > 0: fl_[(e, tag)] += 1
    need.sort(); us.sort()
    med = lambda a: a[len(a)//2] if a else float('nan')
    fr = lambda k: f'{fl_[k]}/{n}'
    print(f'{st}\t{seed}\t{region}\t{label}\t{n}\t{med(us):.3f}\t{fr((0.7,"era1"))}\t{fr((0.7,"era2"))}\t{fr((1.0,"era1"))}\t{fr((1.0,"era2"))}\t{rhomax:.3g}\t{med(need):.3g}')
    tot['n'] += n
    for k, v in fl_.items(): tot[k] += v
    sys.stdout.flush()
print(f'# 計 四半期 {tot["n"]}　e=0.7 Era1 {tot[(0.7,"era1")]}　Era2 {tot[(0.7,"era2")]}　e=1 Era1 {tot[(1.0,"era1")]}　Era2 {tot[(1.0,"era2")]}')
for k in [(0.7,'era1'),(0.7,'era2'),(1.0,'era1'),(1.0,'era2')]:
    print(f'# 割合 e={k[0]} {k[1]}：{tot[k]/max(1,tot["n"]):.3f}')
