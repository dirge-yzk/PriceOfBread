# (907) 追加：cpeace の repFactors（戦争ごとの 会議の削り repCut:条約港の倍率:支払い率 r）と、rule の条約港の外れ方（持ち主が同じ＝失効／変わった＝移転）
import sys, os, csv, collections
d = sys.argv[1]
def rows(t):
    for s in range(1, 21):
        p = os.path.join(d, f'{t}_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8', newline='') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                r['_seed'] = s; yield r
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
cut = []; tm = []
for r in rows('cpeace'):
    for w in r['repFactors'].split('/'):
        p = w.split(':')
        if len(p) == 3: cut.append(fl(p[0])); tm.append(fl(p[1]))
print('[cpeace repFactors] 戦争', len(cut), '会議の削り repCut>0', sum(1 for x in cut if x > 0), '=0', sum(1 for x in cut if x == 0), '最大', max(cut) if cut else '', '中央(>0)', sorted(x for x in cut if x > 0)[len([x for x in cut if x > 0])//2] if any(x > 0 for x in cut) else '')
print('[cpeace repFactors] 条約港の倍率 >1', sum(1 for x in tm if x > 1), '=1', sum(1 for x in tm if x == 1), '値', dict(collections.Counter(round(x, 3) for x in tm).most_common(4)))
TP = collections.defaultdict(list)
for r in rows('rule'):
    TP[(r['_seed'], r['region'])].append((int(r['quarter']), r['treatyPortInForceFlag'], r['nation']))
lapse = move = 0; lyear = []
for v in TP.values():
    v.sort()
    for (q0, f0, n0), (q1, f1, n1) in zip(v, v[1:]):
        if f0 == '1' and f1 != '1':
            if n0 == n1: lapse += 1
            else: move += 1
print('[rule] 条約港の外れ 持ち主が同じ（失効）', lapse, '持ち主が変わった（移転）', move)
