# (1375) rw：開戦の門（WarWearyThreshold 0.5・6 か所）を K の形ごとに置いた時、平時に門が閉じる国の数（読むだけ）
# F＝nation.payRateMeanShare（四半期の支払い率の平均）・MR＝stock.militaryReadinessShare（stock の行がある四半期だけ）
# RP＝clamp01(crownpc.mobilizationIndex / max(1, effectiveCoerceIndex))・平時＝nation.atWarMonths==0・今の門＝scalars.warExhaustion>0.5（平時）
# 使い方: python peace_k.py <base_ins>
import sys, glob, os, collections
d = sys.argv[1]
def fl(x):
    try: return float(x)
    except: return float('nan')
def load(t, seed, keyf, cols):
    out = {}
    with open(os.path.join(d, f'{t}_s{seed}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {c: h.index(c) for c in cols}
        for ln in fh:
            v = ln.rstrip('\n').split('\t'); k = keyf(h, v)
            if k is None: continue
            out[k] = {c: v[i] for c, i in ix.items()}
    return out
def kq(h, v): return (int(v[h.index('quarter')]), v[h.index('nation')])
def ks(h, v):
    t = v[0]
    return (int(t[1:]), v[1]) if t.startswith('q') else None
tot = collections.Counter(); nations = collections.defaultdict(set); wartimes = collections.defaultdict(list)
for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
    seed = f.rsplit('_s', 1)[1].split('.')[0]
    na = load('nation', seed, kq, ['payRateMeanShare', 'atWarMonths', 'totalPopulationK'])
    cp = load('crownpc', seed, kq, ['mobilizationIndex', 'effectiveCoerceIndex', 'politicsEnabledFlag'])
    st = load('stock', seed, ks, ['militaryReadinessShare'])
    sc = load('scalars', seed, kq, ['warExhaustion', 'atWar'])
    for k, v in na.items():
        F = fl(v['payRateMeanShare']); peace = fl(v['atWarMonths']) == 0
        c = cp.get(k)
        RP = max(0.0, min(1.0, fl(c['mobilizationIndex']) / max(1.0, fl(c['effectiveCoerceIndex'])))) if c else float('nan')
        tag = 'peace' if peace else 'war'
        if c and c['politicsEnabledFlag'] != '1': tag += '_polOff'
        if RP == RP:
            tot[tag + '_rows_rp'] += 1
            if RP > 0.5: tot[tag + '_RP>0.5'] += 1; nations[(tag + '_RP>0.5', seed)].add(k[1])
            wartimes[tag + '_RP'].append(RP)
            X, C = fl(c['mobilizationIndex']), fl(c['effectiveCoerceIndex']); K2 = 1.0 if not (X > 0) else C / (C + X)
            wartimes[tag + '_Kprime'].append(K2)
            if K2 < 0.5: tot[tag + '_Kprime<0.5'] += 1; nations[(tag + '_Kprime<0.5', seed)].add(k[1])
        if F == F:
            tot[tag + '_rows_F'] += 1
            if F < 0.5: tot[tag + '_F<0.5'] += 1; nations[(tag + '_F<0.5', seed)].add(k[1])
        s = st.get(k)
        if s and F == F:
            MR = fl(s['militaryReadinessShare'])
            if MR == MR:
                tot[tag + '_rows_FMR'] += 1
                if F * MR < 0.5: tot[tag + '_FxMR<0.5'] += 1; nations[(tag + '_FxMR<0.5', seed)].add(k[1])
                if RP == RP:
                    if F * MR * (1 - RP) < 0.5: tot[tag + '_FxMRx(1-RP)<0.5'] += 1; nations[(tag + '_FxMRx(1-RP)<0.5', seed)].add(k[1])
        e = sc.get(k)
        if e and peace:
            tot['peace_rows_scalars'] += 1
            if fl(e['warExhaustion']) > 0.5: tot['peace_WE>0.5(now)'] += 1; nations[('peace_WE>0.5(now)', seed)].add(k[1])
for k in sorted(tot): print(k, tot[k])
agg = collections.defaultdict(list)
for (tag, seed), s in nations.items(): agg[tag].append(len(s))
for tag in sorted(agg): print('nations per seed', tag, 'mean', round(sum(agg[tag]) / 10, 1), 'seeds', len(agg[tag]))
for tag, a in wartimes.items():
    a.sort(); q = lambda p: a[min(len(a) - 1, int(p * len(a)))]
    print(tag, 'n', len(a), 'p10', round(q(.1), 4), 'med', round(q(.5), 4), 'p90', round(q(.9), 4), 'p99', round(q(.99), 4))
