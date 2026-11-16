# 読むだけ (1182) c117rs：台帳 S11 の読み直す行（変わった行と 11-12 の反証の指摘の行）の数を、今の生（m1178）とヌル腕で数え直す。
#  母集合を混ぜない（(1176) v）：A＝年末の全ての国（q%4==3）・B＝計器の国（q%4!=3 にも行がある国）の毎四半期・monlaw は全ての国の毎四半期（その表の母集合）。
#  計器が書かない表は〈表なし〉と印字する（0 と数えない）。エンジンを呼ばない。
# 使い方: python -X utf8 -I s11_v.py <生のフォルダ> <名> [種の数=10]
import sys, os, csv, collections, statistics
B, TAG = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def rd(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def per(y): return f'{(y - 1783) // 25 * 25 + 1783}'
out = []
def P(*a): print(' '.join(str(x) for x in a), flush=True)
P(f'# {TAG} 生={B} 種=1〜{NS}')
def picked_of(rows):
    return {r['nation'] for r in rows if int(r['quarter']) % 4 != 3}
def popsplit(rows, pk):
    A = [r for r in rows if int(r['quarter']) % 4 == 3]
    Bp = [r for r in rows if r['nation'] in pk]
    return A, Bp
# ---- ed ----
acc = collections.defaultdict(lambda: collections.defaultdict(list)); cnt = collections.Counter(); have = 0
for s in range(1, NS + 1):
    ed = rd('ed', s)
    if ed is None: continue
    have += 1; pk = picked_of(ed)
    for g, rows in zip('AB', popsplit(ed, pk)):
        for r in rows:
            need = fl(r['needPerMonth']); cnt[g, 'n'] += 1
            cnt[g, 'need<=0'] += not (need > 0)
            cnt[g, 'stateWant>0'] += fl(r['stateWantPerMonth']) > 0
            cnt[g, 'stateTake>0'] += fl(r['stateTakePerMonth']) > 0
            cnt[g, 'higherWant>0'] += fl(r['higherWantPerMonth']) > 0
            if need > 0:
                y = per(int(r['year']))
                acc[g, 'avail/need'][y].append(fl(r['availablePerMonth']) / need)
                acc[g, 'take/need'][y].append(fl(r['stateTakePerMonth']) / need)
                acc[g, 'priv/need'][y].append(fl(r['privateDemandPerMonth']) / need)
if have:
    for g in 'AB':
        n = cnt[g, 'n']
        P(f'ed[{g}] 種 {have} 行 {n}・必要≦0 {cnt[g, "need<=0"]}・国庫の買いたい＞0 {cnt[g, "stateWant>0"]}（{cnt[g, "stateWant>0"] / n:.1%}）・取れた＞0 {cnt[g, "stateTake>0"]}（{cnt[g, "stateTake>0"] / n:.1%}）・高等教育の買いたい＞0 {cnt[g, "higherWant>0"]}')
        for k in ('avail/need', 'take/need', 'priv/need'):
            d = acc[g, k]; P(f'  {k} 25 年ごとの平均 ' + ' '.join(f'{y}:{statistics.fmean(d[y]):.3f}' for y in sorted(d)))
else: P('ed 表なし')
# ---- reform ----
cnt = collections.Counter(); have = 0
for s in range(1, NS + 1):
    rf = rd('reform', s)
    if rf is None: continue
    have += 1; pk = picked_of(rf)
    for g, rows in zip('AB', popsplit(rf, pk)):
        for r in rows:
            cnt[g, 'n'] += 1; e = r['educationLawCode']; cnt[g, 'edu' + e] += 1
            if e == '2' and fl(r['parishFundingShare']) > 0: cnt[g, 'parish>0|edu2'] += 1
            ax = r.get('wallPassAxes', '')
            if 'EducationLaw' in ax:
                cnt[g, 'wallEdu'] += 1
                if int(r['quarter']) <= 3: cnt[g, 'wallEdu1783'] += 1
if have:
    for g in 'AB':
        n = cnt[g, 'n']
        P(f'reform[{g}] 種 {have} 行 {n}・教育法 ' + ' '.join(f'{k[1][3:]}:{v}' for k, v in sorted(cnt.items()) if k[0] == g and k[1].startswith('edu')) +
          f'・公教育で教区の割合＞0 {cnt[g, "parish>0|edu2"]}・壁の一歩に教育法 {cnt[g, "wallEdu"]}（1783 年＝q≦3 {cnt[g, "wallEdu1783"]}）')
else: P('reform 表なし')
# ---- modsig ----
cnt = collections.Counter(); have = 0
for s in range(1, NS + 1):
    ms = rd('modsig', s)
    if ms is None: continue
    have += 1; pk = picked_of(ms)
    for g, rows in zip('AB', popsplit(ms, pk)):
        for r in rows: cnt[g, r['stance']] += 1; cnt[g, 'n'] += 1
if have:
    for g in 'AB': P(f'modsig[{g}] 種 {have} 行 {cnt[g, "n"]}・姿勢 ' + ' '.join(f'{k[1]}:{v}' for k, v in sorted(cnt.items()) if k[0] == g and k[1] != 'n'))
else: P('modsig 表なし')
# ---- monlaw（全ての国の毎四半期）----
cnt = collections.Counter(); have = 0
for s in range(1, NS + 1):
    ml = rd('monlaw', s)
    if ml is None: continue
    have += 1; prev = {}
    for r in ml:
        cnt['n', '', ''] += 1; cnt['edu', r['EducationLaw'], r['EducationLawTarget']] += 1
        n = r['nation']; ch = r['ChildLabor']
        if n in prev and prev[n] != ch: cnt['child', prev[n], ch] += 1
        prev[n] = ch; cnt['childv', ch, ''] += 1
if have:
    P(f'monlaw 種 {have} 行 {cnt["n", "", ""]}・教育法→目標 ' + ' '.join(f'{k[1]}→{k[2]}:{v}' for k, v in sorted(cnt.items()) if k[0] == 'edu') +
      '・児童労働の値 ' + ' '.join(f'{k[1]}:{v}' for k, v in sorted(cnt.items()) if k[0] == 'childv') +
      '・児童労働の移り ' + ' '.join(f'{k[1]}→{k[2]}:{v}' for k, v in sorted(cnt.items()) if k[0] == 'child'))
else: P('monlaw 表なし')
# ---- lawgate2 ----
cnt = collections.Counter(); have = 0; nat5 = set()
for s in range(1, NS + 1):
    lg = rd('lawgate2', s)
    if lg is None: continue
    have += 1
    for r in lg:
        cnt[r['engineCode'], r['axis'], r['reasonCode']] += 1
        if r['engineCode'] == '5': nat5.add((s, r['nation'], r['axis']))
if have: P(f'lawgate2 種 {have}・(符号,軸,理由) ' + ' '.join(f'{k}:{v}' for k, v in sorted(cnt.items())) + f'・符号 5 の 国×種×軸 {len(nat5)}')
else: P('lawgate2 表なし')
# ---- 労働時間の浸透（scalars×monlaw×admrule・計器の国の続いた四半期）----
H = {'0': 8.0, '1': 10.0, '2': 12.0, '3': 14.0}
tight = []; diff = []; loose = []; have = 0; adm_have = 0
for s in range(1, NS + 1):
    sc, ml = rd('scalars', s), rd('monlaw', s)
    if sc is None or ml is None: continue
    have += 1; ad = rd('admrule', s)
    law = {(int(r['quarter']), r['nation']): r['ShorterHours'] for r in ml}
    reach = {}
    if ad is not None:
        adm_have += 1
        for r in ad: reach[(int(r['quarter']), r['nation'])] = fl(r['stateReachShare'])
    pk = picked_of(sc); v = {}
    for r in sc:
        if r['nation'] in pk: v[(int(r['quarter']), r['nation'])] = fl(r['workHoursValue'])
    for (q, n), x in v.items():
        if (q - 1, n) not in v: continue
        l0, l1 = law.get((q - 1, n)), law.get((q, n))
        if l0 is None or l0 != l1: continue
        t = H[l1]; x0 = v[(q - 1, n)]
        if abs(x0 - t) < 1e-6: continue
        f = (x0 - x) / (x0 - t)
        if not (0 <= f <= 1): continue
        k = 1 - (1 - f) ** (1 / 3)
        if t < x0:
            e = k / 0.15; tight.append(e)
            if (q, n) in reach: diff.append(e - reach[(q, n)])
        else: loose.append(k / 0.15)
def qs(a):
    if not a: return '0 件'
    a = sorted(a); return f'{len(a)} 件・中央 {a[len(a) // 2]:.3f}・下位 1 割 {a[len(a) // 10]:.3f}・上位 1 割 {a[len(a) * 9 // 10]:.3f}'
if have:
    P(f'労働時間 種 {have}（admrule {adm_have}）・締める向きの 1 か月の詰まり÷0.15 {qs(tight)}・stateReach との差 {qs(diff)}・緩める向き {qs(loose)}')
else: P('労働時間 表なし')
