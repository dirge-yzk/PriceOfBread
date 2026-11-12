# (1077) c102bk 設計の材料：階級ごとの所得の組み立て（地代・領主の取り分・十分の一税）とブロックの境のずれ。読むだけ。
# 使い方: python blocinc.py <base_ins のフォルダ> <出力 tsv>
# 分子＝landshare_s の landRentActual{Noble,Clergy,Peasant,Capitalist,Other}PerQuarter（非植民地格の州だけ・四半期の和）
#        flow_s の giveIncome Fiscal.cs:110（領主の取り分）・Fiscal.cs:132（十分の一税）（四半期の月平均）
# 分母＝class_s の earnedPerMonth（同じ四半期の月平均・全ての州＝植民地格を含む）×月数
# ⚠限界：①分子の地代は植民地格の州を含まない・分母は含む＝植民地を持つ国の地代の割合は下に偏る
#        ②利潤（工業レント）と賃金と俸給は本土の州では表に無い＝残り（所得−地代−取り分−十分の一税）として一括
#        ③class_s は 8 四半期ごと＋年末の行だけ＝その四半期だけを使う ④Other＝貴族・聖職・農民・資本家以外の受け手（階級の番号は分からない）
import sys, os, csv, collections
base, out = sys.argv[1], sys.argv[2]
BLOC = {0: 0, 6: 0, 3: 1, 4: 1, 7: 1, 8: 1, 9: 1, 10: 1, 13: 1, 1: 2, 2: 2, 11: 2, 12: 2, 14: 2, 5: 2}  # BlocOf：Closed→0・IsModernElite→1・他→2（兵士 5 も 2）
LS = {'Noble': 0, 'Clergy': 6, 'Peasant': 1, 'Capitalist': 3}
EPOCH = [(1783, 1799), (1820, 1839), (1850, 1869), (1880, 1899), (1910, 1929), (1935, 1950)]
def ep(y):
    for a, b in EPOCH:
        if a <= y <= b: return f"{a}-{b}"
    return None
def rd(p):
    with open(p, 'r', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for row in r: yield dict(zip(h, row))
def fl(x):
    try:
        v = float(x); return v if v == v else 0.0
    except: return 0.0
# 鍵＝(種, 四半期, 国)
earn = {}; size = {}
seeds = range(1, 11)
for s in seeds:
    for d in rd(os.path.join(base, f'class_s{s}.tsv')):
        t = d['time']
        if not t.startswith('q'): continue
        k = (s, int(t[1:]), d['nation']); c = int(d['classId'])
        earn.setdefault(k, {})[c] = fl(d['earnedPerMonth']); size.setdefault(k, {})[c] = fl(d['sizeK'])
keys = set(earn)
rent = {}; year = {}; mon = {}
for s in seeds:
    for d in rd(os.path.join(base, f'landshare_s{s}.tsv')):
        k = (s, int(d['quarter']), d['nation'])
        if k not in keys: continue
        m = fl(d['monthsInQuarter']) or 3.0
        mon[k] = m; year[k] = int(d['year'])
        rent[k] = {nm: fl(d[f'landRent Actual{nm}PerQuarter'.replace(' ', '')]) for nm in ['Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other']}
dues = collections.defaultdict(lambda: collections.defaultdict(float)); tithe = collections.defaultdict(lambda: collections.defaultdict(float))
for s in seeds:
    for d in rd(os.path.join(base, f'flow_s{s}.tsv')):
        if d['kind'] != 'giveIncome' or d['sourceFile'] != 'Fiscal.cs': continue
        k = (s, int(d['quarter']), d['nation'])
        if k not in keys: continue
        if d['sourceLine'] == '110': dues[k][int(d['classId'])] += fl(d['amountPerMonth'])
        elif d['sourceLine'] == '132': tithe[k][int(d['classId'])] += fl(d['amountPerMonth'])
lord = {}
for s in seeds:
    for d in rd(os.path.join(base, f'lordstat_s{s}.tsv')):
        k = (s, int(d['quarter']), d['nation'])
        if k in keys: lord[k] = fl(d['d2Rent0OverIncome0'])
# 集計：期×（世界の和）と国ごとの比の中央値
agg = collections.defaultdict(lambda: collections.defaultdict(float))
per = collections.defaultdict(lambda: collections.defaultdict(list))
xchk = collections.defaultdict(list)
for k in keys:
    if k not in rent: continue
    e = ep(year[k]);
    if e is None: continue
    m = mon[k]; E = {c: v * m for c, v in earn[k].items()}; Z = size[k]
    R = {0: rent[k]['Noble'], 6: rent[k]['Clergy'], 1: rent[k]['Peasant'], 3: rent[k]['Capitalist']}
    Ro = rent[k]['Other']
    D = {c: v * m for c, v in dues[k].items()}; T = {c: v * m for c, v in tithe[k].items()}
    bE = [0.0] * 3; bZ = [0.0] * 3; bR = [0.0] * 3; bF = [0.0] * 3
    for c, v in E.items():
        b = BLOC.get(c, 2); bE[b] += max(0, v); bZ[b] += Z.get(c, 0)
    for c, v in R.items(): bR[BLOC[c]] += v
    for c, v in list(D.items()) + list(T.items()): bF[BLOC.get(c, 2)] += v
    A = agg[e]; A['n'] += 1
    for b in range(3): A[f'E{b}'] += bE[b]; A[f'Z{b}'] += bZ[b]; A[f'R{b}'] += bR[b]; A[f'F{b}'] += bF[b]
    A['Ro'] += Ro
    for c in (0, 6, 1, 3, 10, 11):
        A[f'Ec{c}'] += max(0, E.get(c, 0)); A[f'Rc{c}'] += R.get(c, 0); A[f'Fc{c}'] += D.get(c, 0) + T.get(c, 0)
    P = per[e]
    if bE[0] > 0: P['feudal0'].append((bR[0] + bF[0]) / bE[0])
    if bE[1] > 0: P['rent1'].append(bR[1] / bE[1])
    if bE[2] > 0: P['rent2'].append(bR[2] / bE[2])
    if E.get(0, 0) > 0: P['nobleRent'].append((R[0] + D.get(0, 0)) / E[0])
    tot = sum(bR) + Ro
    if tot > 0: P['rentOutside0'].append((bR[1] + bR[2]) / tot)
    if bE[0] > 0 and k in lord and lord[k] > 0: xchk[e].append((bR[0] / bE[0]) / lord[k])
def med(a):
    a = sorted(a); n = len(a)
    return float('nan') if n == 0 else (a[n // 2] if n % 2 else 0.5 * (a[n // 2 - 1] + a[n // 2]))
def q(a, p):
    a = sorted(a); return float('nan') if not a else a[min(len(a) - 1, int(p * len(a)))]
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('epoch\trows\tpop0\tpop1\tpop2\tinc0\tinc1\tinc2\tworldFeudalShare0\tworldRentShare1\tworldRentShare2\trentToBloc1\trentToBloc2\trentOther\t'
            'medFeudal0\tp10Feudal0\tmedRent1\tp90Rent1\tmedRent2\tp90Rent2\tmedNobleRentDues\tp10NobleRentDues\tshareNobleBelow05\tmedRentOutside0\txchkMedRatio\txchkN\t'
            'clergyFeudal\tcapitalistRent\tpeasantRent\n')
    for a, b in EPOCH:
        e = f'{a}-{b}'; A = agg[e]; P = per[e]
        if A['n'] == 0: continue
        Zt = A['Z0'] + A['Z1'] + A['Z2']; Et = A['E0'] + A['E1'] + A['E2']; Rt = A['R0'] + A['R1'] + A['R2'] + A['Ro']
        nb = P['nobleRent']; below = sum(1 for x in nb if x < 0.5) / len(nb) if nb else float('nan')
        dv = lambda x, y: x / y if y > 0 else float('nan')
        row = [e, int(A['n']), dv(A['Z0'], Zt), dv(A['Z1'], Zt), dv(A['Z2'], Zt), dv(A['E0'], Et), dv(A['E1'], Et), dv(A['E2'], Et),
               dv(A['R0'] + A['F0'], A['E0']), dv(A['R1'], A['E1']), dv(A['R2'], A['E2']), dv(A['R1'], Rt), dv(A['R2'], Rt), dv(A['Ro'], Rt),
               med(P['feudal0']), q(P['feudal0'], 0.1), med(P['rent1']), q(P['rent1'], 0.9), med(P['rent2']), q(P['rent2'], 0.9),
               med(nb), q(nb, 0.1), below, med(P['rentOutside0']), med(xchk[e]), len(xchk[e]),
               dv(A['Fc6'] + A['Rc6'], A['Ec6']), dv(A['Rc3'], A['Ec3']), dv(A['Rc1'], A['Ec1'])]
        f.write('\t'.join(str(x) if isinstance(x, (int, str)) else f'{x:.3f}' for x in row) + '\n')
print('ok', len(keys))
