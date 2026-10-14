"""(323) 流れF の切り分け（窃盗だけ＝T・置き直し＋AI の応答＝D1）の読み手（読むだけ・挙動は変えない）。事前登録 measurements/fy_20261014_prereg.txt の [3][4][5][6]。
  python -X utf8 an_fy323.py <出力.md> A=<ins> T=<ins> D1=<ins> N1=<ins> N2=<ins> [NG<腕>=<ngset192 の要約テキスト> …] [REFNG<名前>=<要約テキスト> …] [BAND<腕>=<帯の報告〔同名.cost を隣に読む〕> …] [SEEDS=<n>]
入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル,清 bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
      （nation_s・fiscal_s・class_s・region_s〔★(323) 窃盗の列を含む〕・stock_s・pxgoods_s）。最初の腕＝基線 A。ヌル腕は名前が N で始まる腕（雑音の床の相手）。
出す物（英・種の中央〔最小〜最大〕）：
  §1 腕ごとの英の表（兌換停止・不履行の種の数の全窓／払えない月／歳入÷年GDP／Q40 末の債務÷年GDP／大衆の Paid÷Earned／専売の段）・§2 窃盗（英の州の窃盗の額÷大衆の Paid・窓ごと）・
  §3 世界（人口100以上の千件級の補給崩壊の種の数・専売の段の往復と段5・空洞化の件数〔an_h2.py〕・英の技術水準）・§4 雑音の床の表（床＝max(|N1−A|,|N2−A|)・最小の床は種の数の量で 1）・
  §5 NG の分野ごとの正味（新しい−外れた）〔要約テキストを渡した腕だけ〕・§6 空洞化した州の流れの表（窃盗の額÷州の所得を含む）。
限界の自己申告（計器が一番バグる）：
  ・兌換停止＝fiscal_s の onGold が 0.5 以上から 0.5 未満へ・不履行＝defaultCount＞0。四半期の番号は monthIndex 0,1→Q1・以後 3 か月ずつ（an_fx319_arms.py と同じ定義）。
  ・窃盗の額÷大衆の Paid＝region_s の英の州の theftPoolPerMonth の和（総額＝盗まれた額）÷class_s の英の農民・工場労働者・職人の paidPerMonth の和。受け手は大衆に限らない（困窮×不満の重みの全階級）＝大衆への純受け取りではない
    〔flow_s の give−take とは別の量〕。州の窃盗は州の中で閉じる（盗む側も受ける側も同じ州）。
  ・実質の指数＝英の pxgoods_s（計器が選んだ財）を A の同じ種・同じ四半期の価格で重みづけた売れた量（Laspeyres・Q5〜Q40・N と T と D1 を A で割る）。選んだ財が国の消費の全てではない。
  ・空洞化の件数は hol/an_h2.py -v4 の出力の『ピークの1/10 未満: N 件』（移民が主因でなくても数える＝件数と原因を並べて出す）。
  ・NG の正味は ngset192.py の出力の『新しい NG』『外れた NG』の分野ごとの件数の差（ヌル腕でも出る＝§4 の床の相手）。基準の集合が違う表どうしは比べない。
  ・床は種の数・件数の量では最小 1（1 種の違いは雑音）。連続量は max(|N1−A|,|N2−A|) のまま（0 のときは 0）。"""
import sys, os, re, statistics, collections, subprocess
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds, num

OUT = sys.argv[1]
ARMS = collections.OrderedDict(); NGP = {}; REFNG = {}; BANDP = {}; SEEDCAP = 0
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'SEEDS': SEEDCAP = int(v)
    elif k.startswith('REFNG'): REFNG[k[5:]] = v
    elif k.startswith('NG'): NGP[k[2:]] = v
    elif k.startswith('BAND'): BANDP[k[4:]] = v
    else: ARMS[k] = v
UK = 'イギリス'
W = [8, 40, 80, 160, 240, 10 ** 6]; WN = ['Q8', 'Q40', 'Q80', 'Q160', 'Q240', '全期']
MASS = ('農民', '工場労働者', '職人')
HERE = os.path.dirname(os.path.abspath(__file__))
AN_H2 = os.path.join(HERE, '..', 'hol', 'an_h2.py')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    xs = [x for x in xs if x == x]
    return '—' if not xs else f'{statistics.median(xs):.{nd}f} ({min(xs):.{nd}f}〜{max(xs):.{nd}f})'


def qof(m):
    return 0 if m < 2 else 1 + (m - 2) // 3


def load(D):
    sds = sorted(set(iseeds(D, 'nation')) & set(iseeds(D, 'fiscal')) & set(iseeds(D, 'class')))
    if SEEDCAP: sds = [s for s in sds if s <= SEEDCAP]
    R = dict(path=D, seeds=sds, nat=collections.defaultdict(dict), fis=collections.defaultdict(list), cls=collections.defaultdict(lambda: collections.defaultdict(dict)),
             tw=collections.defaultdict(list), gd=collections.defaultdict(dict), theft=collections.defaultdict(lambda: collections.defaultdict(float)),
             tech=collections.defaultdict(dict), collapse=collections.Counter())
    for sd, r in irows(D, 'nation'):
        if sd not in sds: continue
        q = int(r['quarter'])
        tv = num(r.get('monopolyTierEndStage', 'nan'))
        if tv == tv: R['tw'][(sd, r.get('nationUid', r['nation']))].append((q, tv))
        if r['nation'] == UK: R['nat'][sd][q] = r
        s_, p_ = num(r.get('supplyFillShare', 'nan')), num(r['populationK'])
        if s_ == s_ and s_ < 0.3 and p_ >= 100: R['collapse'][sd] += 1
    for sd, r in irows(D, 'fiscal'):
        if sd in sds and r['nation'] == UK: R['fis'][sd].append(r)
    for sd, r in irows(D, 'pxgoods'):
        if sd in sds and r['nation'] == UK and r['time'].startswith('q'):
            q = int(r['time'][1:])
            if 4 <= q < 40: R['gd'][sd][(q, r['goodName'])] = (num(r['lastSoldQty']), num(r['priceMean']))
    for sd, r in irows(D, 'class'):
        if sd in sds and r['nation'] == UK and r['time'].startswith('q') and r['className'] in MASS:
            R['cls'][sd].setdefault(int(r['time'][1:]), []).append((num(r['earnedPerMonth']), num(r['paidPerMonth']), num(r['savingsStock'])))
    if iseeds(D, 'region'):
        for sd, r in irows(D, 'region'):
            if sd in sds and r['nation'] == UK and 'theftPoolPerMonth' in r:
                R['theft'][sd][int(r['quarter'])] += num(r['theftPoolPerMonth'])
    for sd, r in irows(D, 'stock'):
        if sd in sds and r.get('nation') == UK and r['time'].startswith('q') and int(r['time'][1:]) % 4 == 3 and 'techLevelIndex' in r:
            R['tech'][sd][int(r['time'][1:])] = num(r['techLevelIndex'])
    for sd in sds: R['fis'][sd].sort(key=lambda r: int(r['monthIndex']))
    return R


def susp_def(R):
    out = {w: [0, 0, 0] for w in W}
    for sd in R['seeds']:
        prev = None; dq = {w: 0 for w in W}; sq = {w: 0 for w in W}
        for r in R['fis'][sd]:
            q = qof(int(r['monthIndex'])); g = num(r['onGold']); dcv = num(r['defaultCount'])
            for w in W:
                if q < w:
                    if dcv > 0: dq[w] = 1
                    if prev is not None and prev >= .5 and g < .5: sq[w] += 1
            prev = g
        for w in W:
            out[w][0] += dq[w]; out[w][1] += 1 if sq[w] > 0 else 0; out[w][2] += sq[w]
    return out


def susp_between(R, lo, hi):
    """区間 [lo, hi) 四半期の兌換停止のある種の数・不履行〔defaultCount の増え〕のある種の数（累計でなく区間・(323) [9] の④の記録）"""
    sus = dfl = 0
    for sd in R['seeds']:
        prev = None; pdc = None; s1 = d1 = 0
        for r in R['fis'][sd]:
            q = qof(int(r['monthIndex'])); g = num(r['onGold']); dcv = num(r['defaultCount'])
            if lo <= q < hi:
                if prev is not None and prev >= .5 and g < .5: s1 = 1
                if pdc is not None and dcv > pdc: d1 = 1
            prev = g; pdc = dcv
        sus += s1; dfl += d1
    return sus, dfl


def short_months(R, mmax):
    a, b = [], []
    for sd in R['seeds']:
        x = y = 0
        for r in R['fis'][sd]:
            if int(r['monthIndex']) >= mmax: continue
            if num(r['payRateShare']) < 1.0: x += 1
            if num(r['borrowShortfallPerMonth']) > 0 and r['borrowBinder'] == 'full': y += 1
        a.append(x); b.append(y)
    return a, b


def per_seed(R, fn):
    return [fn(R['nat'][sd]) for sd in R['seeds']]


def rev_gdp(nat):
    xs = [num(nat[q]['revenueBaseMeanPerMonth']) * 12 / num(nat[q]['nominalGdpPerYear']) for q in range(4, 40) if q in nat]
    return statistics.mean(xs) if xs else float('nan')


def debt_gdp(nat):
    r = nat.get(39)
    return num(r['govDebtStock']) / num(r['nominalGdpPerYear']) if r else float('nan')


def mass_pe(R, sd):
    xs = []
    for q in range(4, 40):
        c = R['cls'][sd].get(q)
        if c and len(c) == len(MASS):
            e = sum(x[0] for x in c); p = sum(x[1] for x in c)
            if e > 0: xs.append(p / e)
    return statistics.mean(xs) if xs else float('nan')


def theft_ratio(R, sd, lo, hi):
    """英の州の窃盗の額（月平均の和）÷大衆の Paid（月平均）の窓 [lo,hi) の平均（四半期ごとの比の平均）"""
    xs = []
    for q in range(lo, hi):
        c = R['cls'][sd].get(q)
        if not c or len(c) != len(MASS) or q not in R['theft'][sd]: continue
        p = sum(x[1] for x in c)
        if p > 0: xs.append(R['theft'][sd][q] / p)
    return statistics.mean(xs) if xs else float('nan')


def sav_months(R, sd, lo, hi):
    """英の大衆の貯蓄÷月収（月数）の窓 [lo,hi) の平均（四半期ごとの 貯蓄の和÷所得の和）"""
    xs = []
    for q in range(lo, hi):
        c = R['cls'][sd].get(q)
        if c and len(c) == len(MASS):
            e = sum(x[0] for x in c); s = sum(x[2] for x in c)
            if e > 0: xs.append(s / e)
    return statistics.mean(xs) if xs else float('nan')


def m1_seed(R, sd):
    """(数に入る, 除外された)＝Q2 の末の段≧3 のうち、払えない月が先に無かった種（重さだけで上がったと読める＝(319) 事前登録 [8]①）"""
    n = R['nat'][sd]
    if 0 not in n or 1 not in n: return (False, False)
    t1, t2 = num(n[0]['monopolyTierEndStage']), num(n[1]['monopolyTierEndStage'])
    if not t2 >= 3: return (False, False)
    early = any(num(r['payRateShare']) < 1.0 for r in R['fis'][sd] if int(r['monthIndex']) < 2)
    if t1 >= 3 or not early: return (True, False)
    return (False, True)


def tier_series(R, sd, lo=0, hi=40):
    return [num(R['nat'][sd][q]['monopolyTierEndStage']) for q in range(lo, hi) if q in R['nat'][sd]]


def tier5_q(R, sd, lo, hi):
    return sum(1 for t in tier_series(R, sd, lo, hi) if t >= 5)


def rev7_series(t):
    return sum(1 for i in range(2, len(t)) if t[i - 1] > t[i - 2] and t[i] < t[i - 1])


def world_tier(R):
    wrev = wmax = wn = 0
    for key, ser in R['tw'].items():
        ser.sort()
        wrev += rev7_series([tv for _, tv in ser])
        for _, tv in ser:
            wn += 1
            if tv >= 5: wmax += 1
    return wrev, wmax, wn


def real_index(RA, RX):
    """英 Q5〜Q40・A の同じ種の同じ四半期の価格で重みづけた売れた量の指数 X÷A の種ごとの窓の平均（Laspeyres）"""
    qs = []
    for sd in RX['seeds']:
        if sd not in RA['gd']: continue
        qq = []
        for q in range(4, 40):
            nq = dq = 0.0
            for (q_, g), (soldA, pxA) in RA['gd'][sd].items():
                if q_ != q: continue
                dd = RX['gd'][sd].get((q, g))
                if dd is None or not (soldA == soldA and pxA == pxA and dd[0] == dd[0]): continue
                nq += dd[0] * pxA; dq += soldA * pxA
            if dq > 0: qq.append(nq / dq)
        if qq: qs.append(statistics.mean(qq))
    return qs


def hollow(D):
    """an_h2.py -v4 の出力から (ピークの1/10 未満の件数, 原因の内訳の行, 1/10 の事象の行[(seed, 州, 格, 国)])"""
    try:
        out = subprocess.run([sys.executable, '-X', 'utf8', AN_H2, D, '-v4'], capture_output=True, text=True, encoding='utf-8', timeout=900).stdout
    except Exception as e:
        return float('nan'), f'失敗 {e}', []
    n = float('nan'); cause = ''; ev = []
    for ln in out.splitlines():
        m = re.match(r'## ピークの1/10 未満: (\d+) 件 (.*)', ln)
        if m: n = int(m.group(1)); cause = m.group(2)
        m = re.match(r'\s*\[(\S+)・1/10\] s(\d+) (\S+)（(\S+?)・(\w+)）', ln)
        if m: ev.append((int(m.group(2)), m.group(3), m.group(5), m.group(4), m.group(1)))
    return n, cause, ev


def parse_ng(path):
    """ngset192 の出力から {'新': {分野: 件数}, '外': {...}} と総数"""
    t = open(path, encoding='utf-8').read().splitlines()
    res = {'新': collections.Counter(), '外': collections.Counter()}; tot = {}
    cur = None
    for ln in t:
        m = re.match(r'## (新しい|外れた|前からの) NG[^:]*: (\d+)', ln)
        if m:
            cur = {'新しい': '新', '外れた': '外'}.get(m.group(1)); tot[m.group(1)] = int(m.group(2)); continue
        m = re.match(r'\s*\[(\w+)\] (\d+):', ln)
        if m and cur: res[cur][m.group(1)] += int(m.group(2))
    return res, tot


def prov_table(RA, RX, sd, region, label):
    """州の流れの表：region_s の (seed, 州) の行を、人口のピークの 8 四半期前から谷の 4 四半期後まで 4 四半期おき（A の同じ州を並べる）"""
    out = []
    def rd(D):
        rr = []
        for s, r in irows(D, 'region'):
            if s == sd and r['region'] == region: rr.append(r)
        return rr
    rx, ra = rd(RX['path']), rd(RA['path'])
    if not rx: return [f'（{label} s{sd} {region}：region_s に行が無い）']
    pops = [num(r['populationK']) for r in rx]
    pk = max(range(len(pops)), key=lambda i: pops[i]); tr = min(range(pk, len(pops)), key=lambda i: pops[i])
    lo, hi = max(0, pk - 8), min(len(rx) - 1, tr + 4)
    out.append(f'\n### {label} s{sd} {region}（ピーク Q{int(rx[pk]["quarter"]) + 1}・谷 Q{int(rx[tr]["quarter"]) + 1}）\n')
    out.append('| Q | 人口 千 | 窃盗÷州の所得 | 窃盗÷受け手の所得 | 移民の出 千/Q | 機会 | 賃金 | 雇用 | 引き留め | 主食 | A の人口 千 | A の窃盗÷州の所得 |')
    out.append('|---|---|---|---|---|---|---|---|---|---|---|---|')
    A_by = {int(r['quarter']): r for r in ra}
    for i in range(lo, hi + 1, 4):
        r = rx[i]; q = int(r['quarter']); e = num(r['earnedPerMonth'])
        tp = num(r.get('theftPoolPerMonth', 'nan')); tre = num(r.get('theftRecipientsEarnedPerMonth', 'nan'))
        a = A_by.get(q); ae = num(a['earnedPerMonth']) if a else float('nan'); atp = num(a.get('theftPoolPerMonth', 'nan')) if a else float('nan')
        out.append(f'| {q + 1} | {f(num(r["populationK"]), 2)} | {f(tp / e if e > 0 else float("nan"), 4)} | {f(tp / tre if tre > 0 else float("nan"), 3)} | {f(num(r["migrationOutKPerQuarter"]), 2)} | '
                   f'{f(num(r["opportunityIndex"]))} | {f(num(r["wageFactor"]))} | {f(num(r["employmentShare"]))} | {f(num(r["holdingFactor"]))} | {f(num(r["foodFillShare"]))} | '
                   f'{f(num(a["populationK"]), 2) if a else "—"} | {f(atp / ae if ae and ae > 0 else float("nan"), 4)} |')
    return out


D = {a: load(p) for a, p in ARMS.items()}
names = list(ARMS); base = names[0]
nulls = [a for a in names if a.startswith('N')]
tests = [a for a in names if a != base and a not in nulls]
lines = [f'# (323) 流れF の切り分け：読み（英・種 {D[base]["seeds"]}・種の中央〔最小〜最大〕）\n', f'腕：{" / ".join(names)}（基線＝{base}・ヌル腕＝{",".join(nulls) or "なし"}）\n']

# §0 機構の確認・帯（ヌル腕が Q0 から別の軌道か／帯の ★赤・exact・near・推定の計算量）
lines.append('## §0 機構の確認と帯\n')
def q0_rows(R):
    out = {}
    for sd, r in irows(R['path'], 'nation'):
        if sd in R['seeds'] and int(r['quarter']) == 0: out.setdefault(sd, []).append(tuple(sorted(r.items())))
    return out
Q0 = {a: q0_rows(D[a]) for a in names}
for a in names:
    if a == base: continue
    dif = sum(1 for sd in D[base]['seeds'] if Q0[a].get(sd) != Q0[base].get(sd))
    lines.append(f'- {a} の nation_s の Q1（quarter＝0）の行が {base} と違う種：{dif}/{len(D[base]["seeds"])}' + ('（ヌル腕は ≧9/10 でなければ雑音の相手にならない）' if a in nulls else '（試験の腕は係数の効きが Q1 から出る向き）'))
def first_diff_q(Ra, Rx):
    """種ごとの nation_s の行が最初に基線と違う四半期（違わなければ None）"""
    def grab(R):
        g = collections.defaultdict(dict)
        for s_, r in irows(R['path'], 'nation'):
            if s_ in R['seeds']: g[s_][int(r['quarter'])] = tuple(sorted(r.items()))
        return g
    ga, gx = grab(Ra), grab(Rx); out = {}
    for sd in Ra['seeds']:
        fq = None
        for q in sorted(ga[sd]):
            if gx[sd].get(q) != ga[sd][q]: fq = q; break
        out[sd] = fq
    return out
for a in nulls:
    fq = first_diff_q(D[base], D[a]); n_ = len(D[base]['seeds'])
    by4 = sum(1 for v in fq.values() if v is not None and v <= 3)
    lines.append(f'- G4（代わりの線・[9]）{a} が nation_s で最初に {base} と分かれる四半期（種ごと・Q1＝0）：{fq}・Q4（≦3）までに分かれた種 {by4}/{n_}' + ('（可）' if by4 >= 9 else '（**外れ**＝[9] の代わりの N3 へ）'))
for a, pth in BANDP.items():
    if not os.path.exists(pth): continue
    tx = open(pth, encoding='utf-8').read().splitlines()
    pick = [l.strip() for l in tx if l.strip().startswith('★赤') or '異常総数' in l or '政体exact:' in l or '近い' in l and '参照' in l and '前回比' in l]
    lines.append(f'- 帯 {a}（{os.path.basename(pth)}）：' + '／'.join(pick[:8]))
    cp = pth + '.cost'
    if os.path.exists(cp):
        for l in open(cp, encoding='utf-8').read().splitlines():
            if l.startswith('推定の計算量') or l.startswith('判定:'): lines.append(f'  - {a} .cost：{l[:140]}')

# §1
lines.append('## §1 腕ごとの英の表\n')
S = {}   # 腕 → スカラーの辞書（§4 の床の表の入力）
for a in names:
    R = D[a]; sd_ = susp_def(R); n = len(R['seeds'])
    lines.append(f'\n### {a}\n')
    lines.append('| 窓 | ' + ' | '.join(WN) + ' |\n|---|' + '---|' * len(WN))
    lines.append('| 兌換停止のある種 | ' + ' | '.join(f'{sd_[w][1]}/{n}' for w in W) + ' |')
    lines.append('| 　総回数 | ' + ' | '.join(str(sd_[w][2]) for w in W) + ' |')
    lines.append('| 不履行のある種 | ' + ' | '.join(f'{sd_[w][0]}/{n}' for w in W) + ' |')
    sm, fm = short_months(R, 24); sm40, fm40 = short_months(R, 120)
    rg = per_seed(R, rev_gdp); dg = per_seed(R, debt_gdp); pe = [mass_pe(R, sd) for sd in R['seeds']]
    lines.append(f'\n- 英の払えない月：Q1〜Q8 種の中央 {f(med(sm), 1)}・最大 {max(sm)}／Q1〜Q40 の和 {sum(sm40)}・満額借りても足りない月（binder＝full）{sum(fm40)}')
    lines.append(f'- 英の歳入÷年GDP（Q5〜Q40）：{cell(rg, 4)}／Q40 末の債務÷年GDP：{cell(dg)}／大衆の Paid÷Earned（Q5〜Q40）：{cell(pe)}')
    t5 = [tier5_q(R, sd, 4, 40) for sd in R['seeds']]; r7 = [rev7_series(tier_series(R, sd, 0, 40)) for sd in R['seeds']]
    lines.append(f'- 英の専売の段：Q2 末の段（種ごと）{[num(R["nat"][sd][1]["monopolyTierEndStage"]) if 1 in R["nat"][sd] else None for sd in R["seeds"]]}・段5 の四半期数（Q5〜Q40）{cell(t5, 1)}・往復（Q1〜Q40・種の和）{sum(r7)}')
    sv1 = [sav_months(R, sd, 0, 1) for sd in R['seeds']]; sv8 = [sav_months(R, sd, 4, 8) for sd in R['seeds']]
    m1s = [m1_seed(R, sd) for sd in R['seeds']]
    t80 = [R['tech'][sd].get(79, float('nan')) for sd in R['seeds']]; t160 = [R['tech'][sd].get(159, float('nan')) for sd in R['seeds']]
    lines.append(f'- 英の大衆の貯蓄÷月収（月数）：Q1 {cell(sv1, 1)}・Q5〜Q8 {cell(sv8, 1)}（置き直しの機構の確認＝B は 2.2〜2.9・A は約 90）／技術水準 Q80 {cell(t80)}・Q160 {cell(t160)}')
    lines.append(f'- M1 英の専売の段が Q2 の末までに上がった種（除外規則つき）：{sum(1 for x, y in m1s if x)}/{n}（除外 {sum(1 for x, y in m1s if y)}）')
    sb = susp_between(R, 40, 240)
    lines.append(f'- 区間 Q41〜Q240 の兌換停止のある種 {sb[0]}/{n}・不履行（defaultCount の増え）のある種 {sb[1]}/{n}（記録＝D の同じ窓の値は無い）')
    S[a] = dict(susp41_240=sb[0], def41_240=sb[1], m1=sum(1 for x, y in m1s if x), tech80=med(t80), tech160=med(t160), sav1=med(sv1), susp40=sd_[40][1], susp_all=sd_[10 ** 6][1], def40=sd_[40][0], def_all=sd_[10 ** 6][0], short40=sum(sm40), revgdp=med(rg), debt=med(dg), pe=med(pe), rev7=sum(r7), t5=med(t5))

# §2
lines.append('\n## §2 窃盗（英の州の窃盗の額÷大衆の Paid・総額＝受け手は大衆に限らない）\n')
lines.append('| 腕 | Q1〜Q8 | Q5〜Q40 | Q41〜Q160 | Q161〜Q240 |\n|---|---|---|---|---|')
for a in names:
    R = D[a]
    lines.append(f'| {a} | ' + ' | '.join(cell([theft_ratio(R, sd, lo, hi) for sd in R['seeds']], 4) for lo, hi in ((0, 8), (4, 40), (40, 160), (160, 240))) + ' |')
    S[a]['theft540'] = med([theft_ratio(R, sd, 4, 40) for sd in R['seeds']])
lines.append(f'\n- 注意：T の式は min(前の月の所得×率, 貯蓄)＝貯蓄が月収より小さい POP では旧の式（貯蓄×率）より大きくなりうる＝〈A より下がる〉は貯蓄が月収より大きい POP（A の大衆は貯蓄が月収の約 80 か月分）について作りで決まる。')

# §3
lines.append('\n## §3 世界・英の技術\n')
lines.append('| 腕 | 補給崩壊（人口100以上・千件級の種／件数の最大） | 世界の専売の段の往復 | 段5 の国×四半期 | 英の技術水準 Q80 | Q160 |\n|---|---|---|---|---|---|')
HOL = {}
for a in names:
    R = D[a]; c = R['collapse']; big = sum(1 for v in c.values() if v >= 1000)
    wrev, wmax, wn = world_tier(R)
    t80 = [R['tech'][sd].get(79, float('nan')) for sd in R['seeds']]; t160 = [R['tech'][sd].get(159, float('nan')) for sd in R['seeds']]
    n, cause, ev = hollow(R['path']); HOL[a] = (n, cause, ev)
    lines.append(f'| {a} | {big}/{len(R["seeds"])}／{max(c.values()) if c else 0} | {wrev} | {wmax}/{wn} | {cell(t80)} | {cell(t160)} |')
    S[a].update(collapse=big, wrev=wrev, hollow=n, hollow_mig=sum(1 for e in ev if e[4] == '移民'), hollow_seeds=len({e[0] for e in ev}))
lines.append('\n空洞化（ピークの 1/10 未満・an_h2.py -v4）：')
for a in names:
    n, cause, ev = HOL[a]
    lines.append(f'- {a}：{n} 件 {cause}　' + '；'.join(f's{s} {r}（{nat}・{tier}・{why}）' for s, r, tier, nat, why in ev))

# §4
lines.append('\n## §4 雑音の床の表（床＝max(|N−A|)・種の数・件数の量は最小 1）\n')
METRICS = [('兌換停止のある種 Q40', 'susp40', 'int', +1), ('兌換停止のある種 全期', 'susp_all', 'int', +1), ('不履行のある種 Q40', 'def40', 'int', +1), ('不履行のある種 全期', 'def_all', 'int', +1),
           ('英の払えない月 Q1〜Q40（和）', 'short40', 'cnt', +1), ('英の歳入÷年GDP（Q5〜Q40）', 'revgdp', 'cont', -1), ('英の大衆の Paid÷Earned（Q5〜Q40）', 'pe', 'cont', -1),
           ('英の専売の段の往復（Q1〜Q40・和）', 'rev7', 'cnt', +1), ('補給崩壊の千件級の種', 'collapse', 'int', +1), ('世界の専売の段の往復', 'wrev', 'cnt', +1), ('空洞化（1/10 未満）の件数', 'hollow', 'int', +1), ('空洞化のうち移民が原因の件数', 'hollow_mig', 'int', +1), ('空洞化のある種の数', 'hollow_seeds', 'int', +1), ('兌換停止のある種 Q41〜Q240（区間）', 'susp41_240', 'int', +1), ('不履行のある種 Q41〜Q240（区間）', 'def41_240', 'int', +1), ('英の技術水準 Q80', 'tech80', 'cont', -1), ('英の技術水準 Q160', 'tech160', 'cont', -1)]
hdr = '| 量 | ' + ' | '.join(names) + ' | 床 | ' + ' | '.join(f'{t}−{base} は床を超えて悪化' for t in tests) + ' |'
lines.append(hdr); lines.append('|---|' + '---|' * (len(names) + 1 + len(tests)))
FLOORS = {}
for label, key, kind, bad in METRICS:
    vals = [S[a].get(key, float('nan')) for a in names]
    fl = max([abs(S[n][key] - S[base][key]) for n in nulls if S[n].get(key, float('nan')) == S[n].get(key, float('nan'))] or [0.0])
    if kind in ('int', 'cnt'): fl = max(fl, 1.0)
    FLOORS[key] = fl
    flags = []
    for t in tests:
        d = (S[t].get(key, float('nan')) - S[base].get(key, float('nan'))) * bad
        flags.append('—' if d != d else (f'**はい**（超過 {d:.4g}・床 {fl:.4g}・{d / fl:.2f} 倍）' if d > fl and fl > 0 else (f'**はい**（超過 {d:.4g}・床 0）' if d > fl else 'いいえ')))
    fmt = (lambda v: f'{v:.0f}') if kind in ('int', 'cnt') else (lambda v: f'{v:.4f}')
    lines.append(f'| {label} | ' + ' | '.join('—' if v != v else fmt(v) for v in vals) + f' | {fmt(fl)} | ' + ' | '.join(flags) + ' |')
q_idx = {a: med(real_index(D[base], D[a])) for a in names}
lines.append(f'\n- 英の財の実質の指数（{base} の価格で重みづけた売れた量・Q5〜Q40・種を対にして{base}で割る）：' + '・'.join(f'{a} {f(q_idx[a])}' for a in names) +
             f'／床 {f(max([abs(1 - q_idx[n]) for n in nulls if q_idx[n] == q_idx[n]] or [0.0]), 4)}（悪化＝床より下）：' + '・'.join(f'{t} {"**はい**" if (1 - q_idx[t]) > max([abs(1 - q_idx[n]) for n in nulls if q_idx[n] == q_idx[n]] or [0.0]) else "いいえ"}' for t in tests))

# §5
if NGP:
    lines.append('\n## §5 NG（史実比較の桁）の分野ごとの正味（新しい−外れた・基準の集合は全腕同じ）\n')
    allp = dict(NGP); allp.update({('参考:' + k): v for k, v in REFNG.items()})
    P = {a: parse_ng(p) for a, p in allp.items()}
    doms = sorted({d for a in P for d in set(P[a][0]['新']) | set(P[a][0]['外'])})
    lines.append('| 腕 | 新しい | 外れた | 正味 | ' + ' | '.join(doms) + ' |\n|---|---|---|---|' + '---|' * len(doms))
    for a, (res, tot) in P.items():
        nn, gg = sum(res['新'].values()), sum(res['外'].values())
        lines.append(f'| {a} | {nn} | {gg} | {nn - gg:+d} | ' + ' | '.join(f'{res["新"][d] - res["外"][d]:+d}' for d in doms) + ' |')
    nets = {a: sum(P[a][0]['新'].values()) - sum(P[a][0]['外'].values()) for a in P if a in NGP}
    for a in NGP: S[a]['ngnet'] = nets[a]
    if base in nets:
        fl = max([abs(nets[n] - nets[base]) for n in nulls if n in nets] or [0]); fl = max(fl, 1)
        lines.append(f'\n- 正味の床（max|N−{base}|・最小 1）{fl}：' + '・'.join(f'{t} {nets[t] - nets[base]:+d}（{"**床を超えて悪化**" if nets[t] - nets[base] > fl else "床の内"}）' for t in tests if t in nets))

    # (323) [9]：分野ごとの床＝N の分野ごとの正味の max|N−A|（最小 1）・T と D1 は床を超えて悪化したか
    def dnet(a, d): return P[a][0]['新'][d] - P[a][0]['外'][d]
    if base in NGP and nulls:
        lines.append('\n分野ごとの床（max|N−A|・最小 1）と、試験の腕の分野ごとの正味（A に対する差・悪化＝正味が増える向き）：\n')
        lines.append('| 分野 | A の正味 | 床 | ' + ' | '.join(f'{t} の正味（床を超えて悪化?）' for t in tests if t in NGP) + ' |\n|---|---|---|' + '---|' * len([t for t in tests if t in NGP]))
        NGDOM = {}
        for d in doms:
            fl_d = max([abs(dnet(n, d) - dnet(base, d)) for n in nulls if n in NGP] or [0]); fl_d = max(fl_d, 1)
            NGDOM[d] = fl_d
            lines.append(f'| {d} | {dnet(base, d):+d} | {fl_d} | ' + ' | '.join(f'{dnet(t, d) - dnet(base, d):+d}（{"**はい**" if dnet(t, d) - dnet(base, d) > fl_d else "いいえ"}）' for t in tests if t in NGP) + ' |')
        S['_ngdom'] = NGDOM
    S['_ngP'] = P; S['_doms'] = doms

# §6
lines.append('\n## §6 空洞化した州の流れの表（試験の腕で 1/10 未満になった州・A の同じ州を並べる）\n')
shown = 0
for t in tests:
    for (s, reg, tier, nat, why) in HOL[t][2]:
        lines += prov_table(D[base], D[t], s, reg, f'{t}・{nat}・{tier}・{why}'); shown += 1
if not shown: lines.append('（試験の腕に空洞化の件数が無い）')
# §7 止める線の判定（事前登録 [3]・[9]：§4 の旗ではなく生の列から読む。ヌル腕は事実として併記し線は変えない）
lines.append('\n## §7 止める線（変えない・各腕）\n')
lines.append('| 腕 | 移民による空洞化（1 件で止める） | 空洞化（全原因） | 千件級の補給崩壊の種（≧2/10 で止める） | NG 新しい／外れた／新しい−外れた（＞6 で止める） | 推定の計算量（＞248.5 秒で止める） |\n|---|---|---|---|---|---|')
P_ = S.get('_ngP')
def est_cost(a):
    pth = BANDP.get(a)
    if not pth or not os.path.exists(pth + '.cost'): return float('nan')
    for l in open(pth + '.cost', encoding='utf-8').read().splitlines():
        m = re.match(r'推定の計算量（1試合）: ([0-9.]+) 秒', l)
        if m: return float(m.group(1))
    return float('nan')
for a in names:
    hm = S[a]['hollow_mig']; ho = S[a]['hollow']; cl = S[a]['collapse']
    if P_ and a in NGP:
        nn = sum(P_[a][0]['新'].values()); gg = sum(P_[a][0]['外'].values()); ngc = f'{nn}／{gg}／{nn - gg:+d}' + ('（**当たる**）' if nn > gg + 6 else '')
    else: ngc = '—'
    ec = est_cost(a)
    lines.append(f'| {a}{"（ヌル腕・事実として併記）" if a in nulls else ""} | {hm}{"（**当たる**）" if hm >= 1 else ""} | {ho} | {cl}/{len(D[a]["seeds"])}{"（**当たる**）" if cl >= 2 else ""} | {ngc} | {f(ec, 1)}{"（**当たる**）" if ec == ec and ec > 248.5 else ""} |')
lines.append('\n- 破綻（クラッシュ・NaN・保存則・savetest・進行不能）は読み手の外：ms_<腕>.log の rc／帯の ★赤 異常の中身／leakhunt／savetest で読む。')

# §8 帰属（事前登録 [5]・[9]）
lines.append('\n## §8 世界側の副作用の帰属（[5]・[9]：起因＝床を超えて悪化かつ D の記録との差の 1/3 以上）\n')
DREC = {'空洞化の件数': 3, 'NG navy': 28, 'NG law': 16, 'NG rail': 12, 'NG fin': 6, '専売の段の往復（世界）': 491, '兌換停止のある種 Q40': 8, '不履行のある種 Q40': 4}
def get_x(a, label):
    if label == '空洞化の件数': return S[a]['hollow']
    if label == '専売の段の往復（世界）': return S[a]['wrev']
    if label == '兌換停止のある種 Q40': return S[a]['susp40']
    if label == '不履行のある種 Q40': return S[a]['def40']
    if label.startswith('NG ') and P_ and a in NGP:
        d = label[3:]; return P_[a][0]['新'][d] - P_[a][0]['外'][d]
    return float('nan')
def floor_x(label):
    if label == '空洞化の件数': return FLOORS['hollow']
    if label == '専売の段の往復（世界）': return FLOORS['wrev']
    if label == '兌換停止のある種 Q40': return FLOORS['susp40']
    if label == '不履行のある種 Q40': return FLOORS['def40']
    if label.startswith('NG '): return S.get('_ngdom', {}).get(label[3:], 1)
    return float('nan')
tt = [t for t in ('T', 'D1') if t in tests]
lines.append('| X | D の記録 | A | 床 | ' + ' | '.join(f'{t}（Y−A・区分）' for t in tt) + ' | 帰属 |\n|---|---|---|---|' + '---|' * len(tt) + '---|')
for label, dval in DREC.items():
    Av = get_x(base, label); fl = floor_x(label)
    Dd = dval - (Av if Av == Av else 0)   # 記録の D−A（NG は A＝基準なので 0）
    single = (label == '空洞化の件数')      # D の 3 件は全て種 6 の 1 回の出来事（[9]）
    cats = {}; cells = []
    for t in tt:
        Yv = get_x(t, label)
        if Yv != Yv: cats[t] = '—'; cells.append('—'); continue
        d = Yv - Av
        if d > fl and Dd > 0 and d >= Dd / 3: c = '起因'
        elif d > fl: c = '小さい寄与'
        elif d > 0: c = '床の内（正）'
        else: c = 'なし'
        cats[t] = c; cells.append(f'{d:+.0f}・{c}')
    ok = [t for t in tt if cats[t] == '起因']
    if any(cats[t] == '—' for t in tt): verdict = '—（NG の要約が無い腕がある）'
    elif len(ok) == 2:
        dT, dD = get_x('T', label) - Av, get_x('D1', label) - Av
        verdict = f'両方に分かれる（T {dT / (dT + dD):.0%}・D1 {dD / (dT + dD):.0%}）' if dT + dD > 0 else '両方'
    elif len(ok) == 1: verdict = f'{ok[0]} 起因'
    elif any(cats[t] == '小さい寄与' for t in tt): verdict = '小さい寄与のみ（単独では説明しない）'
    elif Dd <= fl or single: verdict = '雑音の内（判定できない）' + ('＝D の空洞化は種 6 の 1 回の出来事だけ' if single and Dd > fl else '＝D の記録が床の内')
    else: verdict = '組み合わせ（どちらの単独でも出ない）＝D2 の候補'
    lines.append(f'| {label} | {dval}（D−A {Dd:+.0f}） | {f(Av, 0)} | {fl:.0f} | ' + ' | '.join(cells) + f' | {verdict} |')
lines.append('\n- ③（世界の専売の段の往復）が D1 起因のときは、モード 2 の符号の段差（重さ＞0 で国庫が潤沢でも広げる）が候補として並ぶ＝AI の応答の採否の根拠にはしない。')
lines.append('- ④の窓は Q40 の累計（D の記録がある窓）。Q41〜Q240 の区間は §1 と §4 に記録だけ（D の同じ窓の値が無いので 1/3 の条件は組めない）。')
lines.append('- D2（置き直し＋窃盗の基数）を回すのは〈組み合わせ〉の行があるときだけ（〈雑音の内〉・〈小さい寄与のみ〉では回さない）。')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
print('done', OUT)
