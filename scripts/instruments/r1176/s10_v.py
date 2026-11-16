# 読むだけ (1176) c116rl：台帳 S10 の読み直す行を、今の生（keep_1171）でエンジンの閾値の式のまま数える。
#  S10-03：人口＞扶養力（anchor_s の popK と capacity）＝Simulation.cs:2229 の net の符号が負の国×四半期。
#  S10-06：扶養力の床＝capacity ≦ 0.3×anchorUsed（Simulation.cs:2211 の Math.Max(baseline*0.3, …)）。0 か非有限の行は別に数える。
#  S10-11/15：capBase の決め手＝LandAnchorCapacityWeight=1（Coefficients）なので capBase＝max(食料の錨の混ぜ, 耕地)。
#             耕地が決める＝|capBase−arableSum| が相対 1e-9 以内。錨より下＝capBase＜anchorUsed。
#  S10-10：workHoursValue（scalars_s）の 12 時間からの離れ（WorkHoursCapacity＝Simulation.cs:2360）。
#  S10-20/48：womenRightsCode（scalars_s）の段の度数。
#  S10-02/64：sf_s の年の最後の四半期の 1950 年（無ければその国の最後の行）で soldierElder60K÷elder60K・pensionLevel＞0 の国。
#  S10-05/28：pop_s の scope=N の行の recoveringMonths／monthsCount・deathsFamineK÷(Normal＋Famine＋Other)。
#  S10-73：dcont_s の milFamineMeanShare＞0。
# 使い方: python -X utf8 -I s10_v.py <生のフォルダ> <名> [pop の種の数]   計器が書かない表は〈表なし〉と印字する（0 と数えない）
import sys, os, math, statistics, collections
B, NAME = sys.argv[1], sys.argv[2]; NPOP = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def rows(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    f = open(p, encoding='utf-8', newline='')
    h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    def gen():
        for l in f:
            c = l.rstrip('\r\n').split('\t')
            yield ix, c
        f.close()
    return gen()
def fl(x):
    try: return float(x)
    except: return float('nan')
def q(xs, p): xs = sorted(xs); return xs[min(len(xs) - 1, int(p * len(xs)))] if xs else float('nan')
print(f'== {NAME}  {B}')
# anchor
n = gt = floor = bad = land = food = below = eq = 0; seeds = 0
for s in range(1, 11):
    g = rows('anchor', s)
    if g is None: continue
    seeds += 1
    for ix, c in g:
        pk, cap, base, cb, ar = (fl(c[ix[k]]) for k in ('popK', 'capacity', 'anchorUsed', 'capBase', 'arableSum'))
        n += 1
        if not (cap > 1e-9): bad += 1; continue
        gt += pk > cap
        floor += cap <= 0.3 * base * (1 + 1e-9)
        if abs(cb - ar) <= 1e-9 * max(1.0, abs(cb)): land += 1
        else: food += 1
        if cb < base * (1 - 1e-9): below += 1
        elif abs(cb - base) <= 1e-9 * max(1.0, abs(cb)): eq += 1
if seeds:
    m = n - bad
    print(f'S10-03/06/11/15 anchor_s 種 {seeds}・国×四半期 {n}・扶養力 0 か非有限 {bad}・人口＞扶養力 {gt}/{m}={gt / m:.2%}・床に届いた {floor}・capBase を耕地が決める {land / m:.2%}・食料の錨が決める {food / m:.2%}・錨より下 {below / m:.2%}（{below}）・錨と等しい {eq}')
else: print('S10-03/06/11/15 anchor_s 表なし')
# scalars
wh = []; d05 = d1 = up1 = lo1 = 0; wr = collections.Counter(); wrs = collections.defaultdict(collections.Counter); ns = 0; nr = 0
for s in range(1, 11):
    g = rows('scalars', s)
    if g is None: continue
    ns += 1
    for ix, c in g:
        nr += 1
        h = fl(c[ix['workHoursValue']])
        if h == h:
            wh.append(h); d05 += abs(h - 12) >= 0.5; d1 += abs(h - 12) >= 1.0; up1 += h - 12 >= 1.0; lo1 += 12 - h >= 1.0
        k = c[ix['womenRightsCode']]; wr[k] += 1; wrs[k][s] += 1
if ns:
    print(f'S10-10 scalars_s 種 {ns}・国×四半期 {nr}・労働時間 5%点 {q(wh, .05):.2f}・中央 {q(wh, .5):.2f}・95%点 {q(wh, .95):.2f}・12 時間から 0.5 時間以上 {d05 / len(wh):.1%}・1 時間以上 {d1 / len(wh):.1%}（超 {up1 / len(wh):.1%}・未満 {lo1 / len(wh):.1%}）')
    print('S10-20/48 womenRightsCode の度数 ' + ' '.join(f'{k}:{v}({v / nr:.2%})・種 ' + ','.join(f'{s}:{x}' for s, x in sorted(wrs[k].items())) for k, v in sorted(wr.items())))
else: print('S10-10/20/48 scalars_s 表なし')
# sf
rat = []; mxn = (0, ''); pen = []; ns = 0; firstpen = []
for s in range(1, 11):
    g = rows('sf', s)
    if g is None: continue
    ns += 1; last = {}; fp = {}
    for ix, c in g:
        y = int(c[ix['year']]); nat = c[ix['nation']]; qq = int(c[ix['quarter']])
        if nat not in last or qq >= last[nat][0]: last[nat] = (qq, y, fl(c[ix['elder60K']]), fl(c[ix['soldierElder60K']]), fl(c[ix['pensionLevel']]))
        if fl(c[ix['pensionLevel']]) > 0 and nat not in fp: fp[nat] = y
    L = [v for v in last.values() if v[1] == 1950]
    e = sum(v[2] for v in L); so = sum(v[3] for v in L)
    rat.append(so / e if e > 0 else float('nan'))
    for nat, v in last.items():
        if v[1] == 1950 and v[2] > 0 and v[3] / v[2] > mxn[0]: mxn = (v[3] / v[2], f'{nat}・種 {s}')
    pen.append((sum(1 for v in L if v[4] > 0), len(L))); firstpen += fp.values()
if ns:
    print(f'S10-02 sf_s 種 {ns}・1950 年の世界の 60 歳以上のうち兵士 {min(rat):.2%}〜{max(rat):.2%}・国の最大 {mxn[0]:.1%}（{mxn[1]}）')
    print(f'S10-64 sf_s 1950 年に年金の段が正の国 ' + ' '.join(f'{a}/{b}' for a, b in pen) + (f'・初めて正の年 {min(firstpen)}〜{max(firstpen)}' if firstpen else '・正の国 0'))
else: print('S10-02/64 sf_s 表なし')
# pop (scope N)
rec = mc = fam = tot = 0; ny = nyfam = 0; ns = 0
for s in range(1, NPOP + 1):
    g = rows('pop', s)
    if g is None: continue
    ns += 1
    for ix, c in g:
        if c[ix['scope']] != 'N': continue
        rec += fl(c[ix['recoveringMonths']]); mc += fl(c[ix['monthsCount']])
        f_ = fl(c[ix['deathsFamineK']]); a = fl(c[ix['deathsNormalK']]) + f_ + fl(c[ix['deathsOtherK']])
        fam += f_; tot += a; ny += 1; nyfam += f_ > 0
if ns: print(f'S10-05/28 pop_s(N) 種 {ns}・行 {ny}・回復期の月 {rec:.0f}/{mc:.0f}={rec / mc:.2%}・飢えの死÷粗死亡 {fam / tot:.2%}・飢えの死が正の行 {nyfam / ny:.2%}')
else: print('S10-05/28 pop_s 表なし')
# dcont
v = []; nr = 0; ns = 0
for s in range(1, 11):
    g = rows('dcont', s)
    if g is None: continue
    ns += 1
    for ix, c in g:
        nr += 1; x = fl(c[ix['milFamineMeanShare']])
        if x > 0: v.append(x)
if ns: print(f'S10-73 dcont_s 種 {ns}・国×四半期 {nr}・milFamineMeanShare＞0 {len(v) / nr:.1%}・正の行の中央 {q(v, .5):.4f}・95%点 {q(v, .95):.3f}')
else: print('S10-73 dcont_s 表なし')
