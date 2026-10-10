"""(171) 線M 後半の読み手（読むだけ）＝inj_war171 の w<seed>.txt（WS/WE）と q<seed>.txt（Q）・inj_mil の hist/a<seed>.txt（A 行：GDP・人口・兵士）。
  ① 戦争の型ごとの長さと終わり方（時代＝1784-1849／1850-1913／1914-1950）
  ② 大戦の型（同じ時期に重なり当事国を共有する戦争の塊＝列強〔開戦時のランク＝列強〕が3以上）の長さ・終わり方・動員・戦死の率・戦費・装備の充足
  ③ 厭戦の中身（終わりの崩壊の線と項の内訳）・戦勝点（押しの項と運）
  ④ 国×四半期の軍の財布（交戦の種類ごと）＝器・給与・装備の上限・請求・必要額・r・装備の充足
python an_war171.py <out_dir（w171・hist・cas を含む）> [ラベル] [--seeds N]"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; LAB = args[1] if len(args) > 1 else D
ERAS = [(1784, 1849), (1850, 1913), (1914, 1950)]
GREAT = 3
RS = {0: '国の除去', 1: '決定的', 2: '崩壊', 3: '崩壊', 4: '両方の崩壊', 5: '双方の厭戦', 6: '調停', 7: '他', -1: '続行中'}


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def era(y):
    for a, b in ERAS:
        if a <= y <= b: return f'{a}-{b}'
    return None


# ---- 読み込み ----
wars = []   # dict
for f in sorted(glob.glob(os.path.join(D, 'w171', 'w*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    ws = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'WS':
            ws[int(t[1])] = dict(sd=sd, id=int(t[1]), t0=int(t[2]), y0=int(t[3]), depth=int(t[4]), civil=int(t[5]), sec=int(t[6]), A=t[7], B=t[8],
                                 aNat=int(t[9]), bNat=int(t[10]), aRank=int(t[11]), bRank=int(t[12]), aSold0=fl(t[13]), bSold0=fl(t[14]),
                                 aMob0=int(t[15]), bMob0=int(t[16]), aLaw=int(t[17]), bLaw=int(t[18]), aStage0=fl(t[19]), bStage0=fl(t[20]))
        elif t[0] == 'WE':
            r = ws[int(t[1])]
            r.update(ended=int(t[2]), yEnd=int(t[3]), months=int(t[4]), reason=int(t[5]), score=fl(t[6]), pushA=fl(t[7]), pushB=fl(t[8]),
                     pushTerm=fl(t[9]), luck=fl(t[10]), luckAbs=fl(t[11]), exA=fl(t[12]), exB=fl(t[13]), cutA=fl(t[14]), cutB=fl(t[15]),
                     deadA=fl(t[16]), deadB=fl(t[17]), aExd=[fl(x) for x in t[18].split(',')], bExd=[fl(x) for x in t[19].split(',')],
                     aSoldE=fl(t[20]), bSoldE=fl(t[21]), aSide=[fl(x) for x in t[22].split(',')], bSide=[fl(x) for x in t[23].split(',')])
    wars.extend(ws.values())
seeds = sorted({w['sd'] for w in wars})
A = {}   # (sd, q, name) -> (gdp, pop, sold)
for f in sorted(glob.glob(os.path.join(D, 'hist', 'a*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'A': continue
        A[(sd, int(t[1]), t[4])] = (fl(t[6]), fl(t[7]), fl(t[8]), int(t[5]))
Q = {}   # (sd, q, name) -> list
for f in sorted(glob.glob(os.path.join(D, 'w171', 'q*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    prev = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'Q': continue
        q = int(t[1]); name = t[4]; v = [fl(x) for x in t[5:]]
        led = v[23:28]   # borrow, devalue, army, navy, tax（累計）
        p = prev.get(name)
        dled = [led[k] - (p[k] if p else 0) for k in range(5)]
        prev[name] = led
        Q[(sd, q, name)] = dict(yr=int(t[2]), m=v[0], env=v[1], payFull=v[2], regFull=v[3], payFill=v[4], equipRoom=v[5], equipClaim=v[6], need=v[7],
                               trainNeed=v[8], trainClaim=v[9], claims=v[10], payable=v[11], r=v[12], eq=v[13], wm=v[14], sold=v[15], warM=v[16], cwM=v[17],
                               mobM=v[18], stage=v[19], rev=v[20], treas=v[21], debt=v[22], dBorrow=dled[0], dDeval=dled[1], dArmy=-dled[2], dNavy=-dled[3], dTax=dled[4],
                               pd=v[28] if len(v) > 32 else float('nan'), ew=v[29] if len(v) > 32 else float('nan'), eg=v[30] if len(v) > 32 else float('nan'), wr=v[31] if len(v) > 32 else float('nan'), af=v[32] if len(v) > 32 else float('nan'))

print(f'######## {LAB}（種 {len(seeds)}・戦争 {len(wars)} 本）')


def kind(w):
    if w['civil']: return '内戦'
    if not (w['aNat'] and w['bNat']): return '対外・抽象の相手'
    if w['aRank'] == GREAT and w['bRank'] == GREAT: return '列強どうし'
    if w['aRank'] == GREAT or w['bRank'] == GREAT: return '列強と非列強'
    return '非列強どうし'


# ---- ① 型ごとの長さと終わり方 ----
print('\n## ① 戦争の型ごとの長さ（月・中央〔四分位〕）と終わり方（％）＝終わった戦争だけ・開戦の年で時代を分ける')
print('時代 | 型 | 本数（種あたり） | 月数の中央〔25-75%〕 | 決定的 | 崩壊 | 双方の厭戦 | 調停 | 国の除去 | 戦死の中央（千人）')
for a, b in ERAS:
    for k in ['列強どうし', '列強と非列強', '非列強どうし', '対外・抽象の相手', '内戦']:
        v = [w for w in wars if w.get('ended') and a <= w['y0'] <= b and kind(w) == k]
        if not v: continue
        ms = sorted(w['months'] for w in v)
        q1 = ms[len(ms) // 4]; q3 = ms[(3 * len(ms)) // 4]
        c = collections.Counter(w['reason'] for w in v); n = len(v)
        pc = lambda *ks: 100 * sum(c[x] for x in ks) / n
        print(f'{a}-{b} | {k} | {n}（{n / len(seeds):.1f}） | {med(ms):.0f}〔{q1}-{q3}〕 | {pc(1):.0f} | {pc(2, 3, 4):.0f} | {pc(5):.0f} | {pc(6):.0f} | {pc(0):.0f} | {med([w["deadA"] + w["deadB"] for w in v]):.1f}')

# ---- 塊（大戦の型）----
par = list(range(len(wars)))


def find(x):
    while par[x] != x:
        par[x] = par[par[x]]; x = par[x]
    return x


bysd = collections.defaultdict(list)
for i, w in enumerate(wars):
    if w.get('civil'): continue
    w['t1'] = w['t0'] + max(1, w.get('months', 0))
    bysd[w['sd']].append(i)
for sd, idx in bysd.items():
    idx.sort(key=lambda i: wars[i]['t0'])
    for x in range(len(idx)):
        for y in range(x + 1, len(idx)):
            wi, wj = wars[idx[x]], wars[idx[y]]
            if wj['t0'] >= wi['t1']:
                continue
            if {wi['A'], wi['B']} & {wj['A'], wj['B']}:
                par[find(idx[x])] = find(idx[y])
cl = collections.defaultdict(list)
for sd, idx in bysd.items():
    for i in idx: cl[find(i)].append(i)
clusters = []
for root, mem in cl.items():
    ws_ = [wars[i] for i in mem]
    gp = set()
    for w in ws_:
        if w['aRank'] == GREAT and w['aNat']: gp.add(w['A'])
        if w['bRank'] == GREAT and w['bNat']: gp.add(w['B'])
    t0 = min(w['t0'] for w in ws_); t1 = max(w['t1'] for w in ws_)
    parts = set()
    for w in ws_: parts.add(w['A']); parts.add(w['B'])
    clusters.append(dict(sd=ws_[0]['sd'], wars=ws_, gp=gp, t0=t0, t1=t1, y0=min(w['y0'] for w in ws_), months=t1 - t0, parts=parts,
                         dead=sum(w.get('deadA', 0) + w.get('deadB', 0) for w in ws_)))


def gpmonths(c):   # 列強の交戦の月（列強ごとの交戦期間の和）
    per = collections.defaultdict(set)
    for w in c['wars']:
        for side in ('A', 'B'):
            if w[side] in c['gp']:
                for t in range(w['t0'], w['t1']): per[w[side]].add(t)
    return sum(len(s) for s in per.values()), per


print('\n## ② 塊（同じ時期に重なり当事国を共有する対外戦争の和集合）＝列強の数ごとの長さ（月・中央）と戦死（千人・中央）・種あたりの数')
print('時代 | 列強の数 | 塊（種あたり） | 塊の長さの中央〔最大〕 | 列強1国あたりの交戦の月の中央 | 塊の戦死の中央（千人）〔最大〕 | 当事国の数の中央')
for a, b in ERAS:
    for g in [(0, 0), (1, 1), (2, 2), (3, 99)]:
        v = [c for c in clusters if a <= c['y0'] <= b and g[0] <= len(c['gp']) <= g[1]]
        if not v: continue
        gm = []
        for c in v:
            tot, per = gpmonths(c)
            gm.extend(len(s) for s in per.values())
        print(f'{a}-{b} | {g[0]}{"+" if g[1] > 3 else ""} | {len(v)}（{len(v) / len(seeds):.1f}） | {med([c["months"] for c in v]):.0f}〔{max(c["months"] for c in v)}〕 | {med(gm):.0f} | {med([c["dead"] for c in v]):.1f}〔{max(c["dead"] for c in v):.0f}〕 | {med([len(c["parts"]) for c in v]):.0f}')

big = [c for c in clusters if len(c['gp']) >= 3]
print('\n## ②-2 大戦の型（列強3以上の塊）の一覧（種・開戦の年・長さ・列強・戦死・終わり方）＝1850 年以後')
for c in sorted(big, key=lambda c: (c['sd'], c['t0'])):
    if c['y0'] < 1850: continue
    rs = collections.Counter(RS[w.get('reason', -1)] for w in c['wars'])
    gm, per = gpmonths(c)
    print(f"  種{c['sd']} {c['y0']}〜 {c['months']}か月・列強 {len(c['gp'])}（{'・'.join(sorted(c['gp']))}）・列強の交戦の月 {'/'.join(str(len(per[g])) for g in sorted(per))}・戦死 {c['dead']:.0f}千・戦争 {len(c['wars'])}本（{dict(rs)}）")

# 大戦の型の列強の側の量（Q と A から・交戦の四半期）
print('\n## ②-3 大戦の型（列強3以上の塊）の列強の側（列強×交戦の月の中央）＝時代ごと')
print('時代 | 列強の戦争の本数 | 月数の中央 | 終わり方（決定的/崩壊/厭戦/調停）| 兵士÷人口%（開戦→終わり） | 総動員の月の割合 | 装備の充足 | r | 段 | 戦死÷兵士の月（%／月） | 厭戦の中身（基礎・経済・封鎖・財政・空襲・化学・戦死の割合%）| 崩壊の線 cut | 押しの項÷|運|')
for a, b in [(1784, 1849), (1850, 1913), (1914, 1950)]:
    sides = []
    for c in big:
        if not (a <= c['y0'] <= b): continue
        for w in c['wars']:
            if not w.get('ended'): continue
            for s in ('A', 'B'):
                if w[s] not in c['gp']: continue
                o = w['aSide'] if s == 'A' else w['bSide']
                ex = w['aExd'] if s == 'A' else w['bExd']
                m = max(1, w['months'])
                q0 = None
                sides.append(dict(w=w, s=s, months=w['months'], reason=w['reason'], sold0=w['aSold0'] if s == 'A' else w['bSold0'], soldE=w['aSoldE'] if s == 'A' else w['bSoldE'],
                                  soldAvg=o[0] / m, mob=o[1] / m, eq=o[2] / m, r=o[3] / m, stage=o[4] / m, ex=ex, cut=w['cutA'] if s == 'A' else w['cutB'],
                                  dead=w['deadA'] if s == 'A' else w['deadB'], sd=w['sd'], name=w[s], y0=w['y0'], y1=w['yEnd']))
    if not sides: continue
    # 人口（開戦の年の A 行）
    def pop_of(sd, name, yr):
        for (s2, q, n), v in A.items():
            pass
        return float('nan')
    rc = collections.Counter(x['reason'] for x in sides); n = len(sides)
    exs = [sum(x['ex'][:7]) for x in sides]
    comp = []
    for k in range(7):
        tot = sum(x['ex'][k] for x in sides); comp.append(tot)
    T = sum(comp) or 1
    cr = [x['dead'] / max(1e-9, x['soldAvg'] * max(1, x['months'])) * 100 for x in sides if x['soldAvg'] > 0]
    pr = [abs(x['w']['pushTerm']) / max(1e-9, x['w']['luckAbs']) for x in sides]
    print(f"{a}-{b} | {n} | {med([x['months'] for x in sides]):.0f} | {100*rc[1]/n:.0f}/{100*(rc[2]+rc[3]+rc[4])/n:.0f}/{100*rc[5]/n:.0f}/{100*rc[6]/n:.0f} | — | {med([x['mob'] for x in sides]):.2f} | {med([x['eq'] for x in sides]):.2f} | {med([x['r'] for x in sides]):.2f} | {med([x['stage'] for x in sides]):.2f} | {med(cr):.2f} | "
          + '・'.join(f'{100*c/T:.0f}' for c in comp) + f" | {med([x['cut'] for x in sides]):.2f} | {med(pr):.2f}")

# 人口の索引（種・名・年 → 人口・GDP・兵士）
popi = {}
for (sd, q, n), v in A.items():
    popi[(sd, n, q)] = v
qmax = max((k[1] for k in A), default=0)


def at(sd, name, yr, end=False):
    q = (yr - 1783) * 4 + (3 if end else 0)
    for dq in range(0, 8):
        for qq in ((q - dq, q + dq) if not end else (q - dq,)):
            v = popi.get((sd, name, qq))
            if v: return v
    return None


print('\n## ②-4 大戦の型の列強の動員（兵士÷人口%）＝開戦の四半期と終わりの四半期（A 行）・戦死÷戦前の兵士')
for a, b in [(1850, 1913), (1914, 1950)]:
    s0, s1, dr = [], [], []
    for c in big:
        if not (a <= c['y0'] <= b): continue
        for g in c['gp']:
            ws_ = [w for w in c['wars'] if g in (w['A'], w['B']) and w.get('ended')]
            if not ws_: continue
            y0 = min(w['y0'] for w in ws_); y1 = max(w['yEnd'] for w in ws_)
            v0 = at(c['sd'], g, y0); v1 = at(c['sd'], g, y1, True)
            if v0 and v0[1] > 0: s0.append(100 * v0[2] / v0[1])
            if v1 and v1[1] > 0: s1.append(100 * v1[2] / v1[1])
            dd = sum((w['deadA'] if w['A'] == g else w['deadB']) for w in ws_)
            if v0 and v0[2] > 0: dr.append(dd / v0[2])
    if s0:
        print(f'{a}-{b}：列強×塊 {len(s0)}・兵士÷人口% 開戦 {med(s0):.2f}〔最大 {max(s0):.2f}〕→ 終わり {med(s1):.2f}〔最大 {max(s1) if s1 else float("nan"):.2f}〕・戦死÷開戦の兵士 {med(dr):.3f}')

# ---- ③ 列強どうしの戦争（塊を問わない）の厭戦と押し ----
print('\n## ③ 列強どうしの対外戦争（終わった戦争）の終わりの厭戦と崩壊の線・厭戦の中身（%）・戦勝点（押しの項の累計／運の累計／|運|の累計）')
for a, b in ERAS:
    v = [w for w in wars if w.get('ended') and a <= w['y0'] <= b and kind(w) == '列強どうし']
    if not v: continue
    comp = [0.0] * 7
    for w in v:
        for k in range(7): comp[k] += w['aExd'][k] + w['bExd'][k]
    T = sum(comp) or 1
    capm = sum(w['aExd'][7] + w['bExd'][7] for w in v); mm = sum(2 * max(1, w['months']) for w in v)
    print(f"{a}-{b} | {len(v)}本 | 月数の中央 {med([w['months'] for w in v]):.0f} | 終わりの厭戦（負け側・勝ち側）{med([max(w['exA'], w['exB']) for w in v]):.2f}・{med([min(w['exA'], w['exB']) for w in v]):.2f} | cut の中央 {med([min(w['cutA'], w['cutB']) for w in v if w['cutA'] >= 0]):.2f} | 中身 "
          + '・'.join(f'{100*c/T:.0f}' for c in comp) + f" | 押しの項 {med([abs(w['pushTerm']) for w in v]):.2f}・運 {med([abs(w['luck']) for w in v]):.2f}・|運| {med([w['luckAbs'] for w in v]):.2f} | 戦死の上限に張り付いた月 {100*capm/mm:.1f}%")

# ---- ④ 国×四半期の軍の財布 ----
print('\n## ④ 国×四半期の軍の財布（月の和の比・国×四半期の中央）＝器÷必要額（装備の需要に対する器）・給与の満額÷器・装備の上限÷必要額・装備の請求÷必要額・r・装備の充足・戦時の倍率・段')
print('時代 | 種類 | 国×四半期 | 器÷(給与＋必要額) | 給与の満額÷器 | 給与の充足 | 装備の上限÷必要額 | 請求÷必要額 | r | 装備の充足 | 戦時の倍率 | 段 | 軍事費÷年GDP%（台帳の陸＋海） | 借入÷年GDP%（台帳） | 調達の充足 afford（日の平均） | 市場の配給 ration（必要量の重み） | 買えた÷必要量（日の和）')
for a, b in ERAS:
    for k in ['平時', '対外・総動員なし', '対外・総動員', '内戦']:
        rows = []
        for (sd, q, name), v in Q.items():
            if not (a <= v['yr'] <= b) or v['m'] < 2.5 or v['m'] > 3.5: continue
            ar = A.get((sd, q, name))
            if not ar or ar[3] != 1: continue
            if v['cwM'] > 0: kk = '内戦'
            elif v['warM'] > 0: kk = '対外・総動員' if v['mobM'] >= 1.5 else '対外・総動員なし'
            else: kk = '平時'
            if kk != k: continue
            gdpq = ar[0] / 4 if ar[0] > 0 else float('nan')
            rows.append(dict(e_n=v['env'] / max(1e-9, v['payFull'] + v['need']), p_e=v['payFull'] / max(1e-9, v['env']), pf=v['payFill'] / v['m'],
                             er_n=v['equipRoom'] / max(1e-9, v['need']), c_n=v['equipClaim'] / max(1e-9, v['need']), r=v['r'] / v['m'], eq=v['eq'] / v['m'],
                             wm=v['wm'] / v['m'], st=v['stage'] / v['m'], mg=100 * (v['dArmy'] + v['dNavy']) / gdpq if gdpq == gdpq else float('nan'),
                             bg=100 * v['dBorrow'] / gdpq if gdpq == gdpq else float('nan'),
                             af=v['af'] / v['pd'] if v['pd'] > 0 else float('nan'), ra=v['wr'] / v['ew'] if v['ew'] > 0 else float('nan'), gw=v['eg'] / v['ew'] if v['ew'] > 0 else float('nan')))
        if not rows: continue
        g = lambda key: med([x[key] for x in rows if x[key] == x[key] and abs(x[key]) < 1e9])
        print(f"{a}-{b} | {k} | {len(rows)} | {g('e_n'):.2f} | {g('p_e'):.2f} | {g('pf'):.2f} | {g('er_n'):.2f} | {g('c_n'):.2f} | {g('r'):.2f} | {g('eq'):.2f} | {g('wm'):.1f} | {g('st'):.2f} | {g('mg'):.1f} | {g('bg'):.2f} | {g('af'):.2f} | {g('ra'):.2f} | {g('gw'):.2f}")

# 主要国の戦時の軍事費÷年GDP（交戦の四半期・国ごとの中央と最大）
print('\n## ④-2 主要国の交戦の四半期（対外）の軍事費÷年GDP%（台帳の陸＋海÷四半期の GDP）・借入÷年GDP%・装備の充足＝時代 1850-1913／1914-1950（種の中央の中央と最大）')
for n in ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '日本', 'オーストリア', '清']:
    out = []
    for a, b in [(1850, 1913), (1914, 1950)]:
        per = collections.defaultdict(list)
        for (sd, q, name), v in Q.items():
            if name != n or not (a <= v['yr'] <= b) or v['warM'] <= 0 or v['cwM'] > 0: continue
            ar = A.get((sd, q, name))
            if not ar or ar[0] <= 0: continue
            per[sd].append((100 * (v['dArmy'] + v['dNavy']) / (ar[0] / 4), 100 * v['dBorrow'] / (ar[0] / 4), v['eq'] / max(1, v['m'])))
        if per:
            ms = [med([x[0] for x in l]) for l in per.values()]; mx = max(max(x[0] for x in l) for l in per.values())
            bs = [med([x[1] for x in l]) for l in per.values()]; es = [med([x[2] for x in l]) for l in per.values()]
            out.append(f'{a}-{b} 軍 {med(ms):.1f}（最大 {mx:.1f}）・借入 {med(bs):.1f}・充足 {med(es):.2f}・種 {len(per)}')
        else:
            out.append(f'{a}-{b} —')
    print(f'{n}：' + '｜'.join(out))
