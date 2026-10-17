# (394) 段0 の読み手：工場の能力を律速しているのは頭数・設備・投入のどれか（A の世界・計器 factory_s の既存の列だけ）。
# 使い方：PYTHONUTF8=1 python an_fp0.py <ins フォルダ> [種の最大=全て]      ⚠-I は付けない（-I は PYTHONUTF8 を無視して日本語が化ける）
# 読むだけ。新しい列は足さない（計器の列は線 ic2 の担当）。数字の意味は SimCore/InstrumentsCapacity.cs の頭のコメント。
# ⚠限界（この読み手が自分で申告する）：
#   ①〈設備〉の側は A の世界では構造的に 0：Producer.Capacity の式（EconomyStep.cs の `region.Pops[lp].Size * fraction * s.Productivity * boost * …`）に
#      BuiltScale も Scale も入っていない（Scale は労働の按分の重み prodW=LaborWeight×Scale に入るだけで、按分の総和 Σfraction=1 は Scale に依らない）。
#      ここでは設備の実測として 操業規模/棟（Scale の平均）と BuiltScale の割合を出す＝能力の源ではなく〈設備の欄が能力に効いていない〉ことの裏づけ。
#   ②〈頭数が実際に律速する量〉（能力・採算・充足が全て満ちた量）の列は無い。上限として 投入に縛られない量（notInputBound）の割合を出す（採算で削られる前の量）。
#   ③Q-PW1 の型（州の工場の産出がピークの 1/10 未満 かつ 担い手 POP がピークの 5% 未満）は州の列が factory_s に無いので国の水準で近似する
#      （国の能力がそれまでのピークの 1/10 未満 かつ 国の担い手 POP がそれまでのピークの 5% 未満）。州の水準は線 ic2 への依頼（読み手は変えない）。
#   ④挙げた国（毎四半期）以外は年末の行だけ＝減りの露出は年ごとの比で出す（毎四半期の国は同じ年末の行を使い、条件を揃える）。
import sys, glob, csv, os, re, collections

d = sys.argv[1]; smax = int(sys.argv[2]) if len(sys.argv) > 2 else 10**9
ERAS = [(1783, 1829), (1830, 1869), (1870, 1913), (1914, 1950)]
DEP_M = 0.003                      # Coefficients.IndDepreciation（月）
DEP_Y = 1.0 - (1.0 - DEP_M) ** 12  # 年の減価


def era(y):
    for i, (a, b) in enumerate(ERAS):
        if a <= y <= b: return i
    return len(ERAS) - 1


def num(v):
    try: return float(v)
    except Exception: return float('nan')


rows = []
for p in sorted(glob.glob(os.path.join(d, 'factory_s*.tsv'))):
    s = int(re.search(r'_s(\d+)\.tsv$', p).group(1))
    if s > smax: continue
    with open(p, encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            o = {'_seed': s, 'nation': r['nation']}
            for k, v in r.items():
                if k != 'nation': o[k] = num(v)
            rows.append(o)
seeds = sorted({r['_seed'] for r in rows})
cnt = collections.Counter((r['_seed'], r['nation']) for r in rows)
mx = collections.defaultdict(int)
for (s, n), v in cnt.items(): mx[s] = max(mx[s], v)
full = {k for k, v in cnt.items() if v >= 0.9 * mx[k[0]]}
picked = sorted({k[1] for k in full})
print(f'factory_s 行 {len(rows)}・種 {seeds}・毎四半期の国 {" ".join(picked)}')
sel = [r for r in rows if (r['_seed'], r['nation']) in full]


def pct(a, b): return f'{100 * a / b:6.1f}' if b > 0 else '   nan'


def agg(rs, keys):
    S = collections.defaultdict(float)
    for r in rs:
        for k in keys:
            v = r[k]
            if v == v: S[k] += v
        S['n'] += 1
    return S


K1 = ['capacityRefValuePerMonth', 'desiredRefValuePerMonth', 'filledRefValuePerMonth', 'notInputBoundDesiredRefValuePerMonth',
      'laborBoundDesiredRefValuePerMonth', 'otherBoundDesiredRefValuePerMonth', 'laborBoundLossRefValuePerMonth', 'otherBoundLossRefValuePerMonth',
      'builtScaleSum', 'operatingScaleSum', 'factoryActiveCount', 'factoryLaborPopK']
print('\n① 頭数・設備・投入（挙げた国・参照価格加重・作りたい量比%）')
print('   頭数の上限＝投入に縛られない量（採算で削られる前）／設備＝操業規模/棟と BuiltScale の割合（A の能力の式に設備の項は無い＝構造的に 0）／投入＝労働・他の投入の失った量')
for e, (a, b) in enumerate(ERAS):
    S = agg([r for r in sel if era(r['year']) == e], K1)
    print(f'  {a}-{b} 行 {int(S["n"]):>6} | 採算 {pct(S["desiredRefValuePerMonth"], S["capacityRefValuePerMonth"])} 投入の充足 {pct(S["filledRefValuePerMonth"], S["desiredRefValuePerMonth"])}'
          f' | 頭数の上限（縛られない） {pct(S["notInputBoundDesiredRefValuePerMonth"], S["desiredRefValuePerMonth"])}'
          f' | 失った量 労働 {pct(S["laborBoundLossRefValuePerMonth"], S["desiredRefValuePerMonth"])} 他投入 {pct(S["otherBoundLossRefValuePerMonth"], S["desiredRefValuePerMonth"])}'
          f' | 設備 操業規模/棟 {S["operatingScaleSum"] / S["factoryActiveCount"] if S["factoryActiveCount"] > 0 else float("nan"):5.2f}'
          f' BuiltScale/操業規模 {pct(S["builtScaleSum"], S["operatingScaleSum"])}')

print('\n② 能力の増え方＝頭数×1人あたり（国ごと・時代の始めと終わりの年末の値の比・挙げた国・種の中央値）')


def med(xs):
    xs = sorted(x for x in xs if x == x)
    return xs[len(xs) // 2] if xs else float('nan')


for nm in picked:
    out = []
    for e, (a, b) in enumerate(ERAS):
        ratios = []
        for s in seeds:
            ye = {int(r['year']): r for r in sel if r['_seed'] == s and r['nation'] == nm and int(r['quarter']) % 4 == 3}
            ya, yb = max(a, 1783), min(b, 1950)
            ya2 = min((y for y in ye if y >= ya), default=None); yb2 = max((y for y in ye if y <= yb), default=None)
            if ya2 is None or yb2 is None or ya2 >= yb2: continue
            ra, rb = ye[ya2], ye[yb2]
            if ra['factoryLaborPopK'] > 0 and rb['factoryLaborPopK'] > 0 and ra['capacityRefValuePerMonth'] > 0:
                ratios.append((rb['capacityRefValuePerMonth'] / ra['capacityRefValuePerMonth'], rb['factoryLaborPopK'] / ra['factoryLaborPopK'],
                               (rb['capacityRefValuePerMonth'] / rb['factoryLaborPopK']) / (ra['capacityRefValuePerMonth'] / ra['factoryLaborPopK'])))
        if ratios:
            out.append(f'{a}-{b} 能力×{med([x[0] for x in ratios]):.2f}=頭数×{med([x[1] for x in ratios]):.2f}・1人×{med([x[2] for x in ratios]):.2f}')
    print(f'  {nm}: ' + ' | '.join(out))

print('\n③ B（設備の減価だけで減る形）が A と違いうる局面の露出＝担い手 POP の年ごとの減りが年の減価', f'{100 * DEP_Y:.2f}%', 'より速い国×年の割合（年末の行・全ての国・100 以上の千人）')
print('   B の設備は担い手 POP が減っても月に IndDepreciation=0.003 でしか減らない＝それより速く頭数が減る年だけ B は A と違う。減り幅の階級別も出す。')
bycell = collections.defaultdict(dict)   # (seed,nation) -> {year: row}
for r in rows:
    if int(r['quarter']) % 4 == 3 and r['factoryLaborPopK'] == r['factoryLaborPopK']:
        bycell[(r['_seed'], r['nation'])][int(r['year'])] = r
for e, (a, b) in enumerate(ERAS):
    tot = fast = deep = vdeep = 0
    for k, ye in bycell.items():
        for y, r in ye.items():
            if era(y) != e or y - 1 not in ye: continue
            p0 = ye[y - 1]['factoryLaborPopK']; p1 = r['factoryLaborPopK']
            if not (p0 >= 100): continue
            tot += 1
            ch = p1 / p0 - 1.0
            if ch < -DEP_Y: fast += 1
            if ch < -0.20: deep += 1
            if ch < -0.50: vdeep += 1
    print(f'  {a}-{b} 国×年 {tot:>6} | 減りが減価より速い {pct(fast, tot)}% | 年に−20%超 {pct(deep, tot)}% | −50%超 {pct(vdeep, tot)}%')

print('\n④ Q-PW1 の型（国の水準の近似）＝国の能力がそれまでのピークの 1/10 未満 かつ 国の担い手 POP がそれまでのピークの 5% 未満の国×年（ピークの担い手 POP が 100 千人以上）')
hit = collections.Counter(); hit2 = collections.Counter(); hit3 = collections.Counter()
events = []
for (s, nm), ye in bycell.items():
    pk_c = pk_p = 0.0
    for y in sorted(ye):
        r = ye[y]
        c = r['capacityRefValuePerMonth']; p = r['factoryLaborPopK']
        pk_c = max(pk_c, c); pk_p = max(pk_p, p)
        if pk_p < 100: continue
        e = era(y)
        if c < 0.1 * pk_c and p < 0.05 * pk_p: hit[(s, e)] += 1; events.append((s, nm, y, c / pk_c, p / pk_p))
        if c < 0.5 * pk_c and p < 0.5 * pk_p: hit2[(s, e)] += 1
        if c < 0.5 * pk_c and p >= 0.5 * pk_p: hit3[(s, e)] += 1
for e, (a, b) in enumerate(ERAS):
    print(f'  {a}-{b} 1/10 かつ 5%：{sum(v for (s, ee), v in hit.items() if ee == e):>4} 国×年（種ごと {[hit.get((s, e), 0) for s in seeds]}）'
          f' | 緩い型（能力と担い手がともにピークの半分未満）{sum(v for (s, ee), v in hit2.items() if ee == e):>4}'
          f' | 能力だけがピークの半分未満（担い手は残る）{sum(v for (s, ee), v in hit3.items() if ee == e):>4}')
print('  1/10 かつ 5% の国×年（種・国・年・能力/ピーク・担い手/ピーク）：')
for ev in sorted(events)[:40]:
    print(f'    種{ev[0]} {ev[1]} {ev[2]} 能力 {ev[3]:.3f} 担い手 {ev[4]:.3f}')
print(f'    （全 {len(events)} 件・先頭 40 件）')

print('\n⑤ 労働が足りない局面の頻度（挙げた国の四半期）＝作りたい量に要る労働÷労働の器 が 1 を超える割合・労働の配給率の需要加重平均が 0.9 未満の割合')
for e, (a, b) in enumerate(ERAS):
    rs = [r for r in sel if era(r['year']) == e and r['laborSupplyCapacityUnitsPerMonth'] > 0]
    n1 = sum(1 for r in rs if r['laborNeedAtDesiredUnitsPerMonth'] / r['laborSupplyCapacityUnitsPerMonth'] > 1.0)
    n2 = sum(1 for r in rs if r['laborRationMeanShare'] == r['laborRationMeanShare'] and r['laborRationMeanShare'] < 0.9)
    print(f'  {a}-{b} 行 {len(rs):>6} | 労働の需要が器を超える {pct(n1, len(rs))}% | 労働の配給率 0.9 未満 {pct(n2, len(rs))}%')
