# (1395) B：運河の池と BuildCost の比・門から開通まで・交戦の停止（keep_1390/base_ins を列名で読む・読むだけ）
import csv, sys, collections
RAW = sys.argv[1]; OUT = sys.argv[2]
REQ = {'suez': '汽船', 'panama': 'ダイナマイト'}; COST = {'suez': 150000.0, 'panama': 400000.0}; HOST = {'suez': 'カイロ', 'panama': 'パナマ'}
FRAC = 0.03
def rows(name, s):
    with open(f'{RAW}/{name}_s{s}.tsv', encoding='utf-8', newline='') as f:
        yield from csv.DictReader(f, delimiter='\t')
def fl(x):
    try: return float(x)
    except: return float('nan')
out = open(OUT + '/b_canal_seed.tsv', 'w', encoding='utf-8', newline='\n')
ser = open(OUT + '/b_canal_series.tsv', 'w', encoding='utf-8', newline='\n')
out.write('seed\tcanal\treq\tgateQuarter\tgateYear\tfirstNation\topenQuarter\topenYear\tlagQuarters\tknowersAtOpen\tpoolLocalAtGate\tpoolGoldAtGate\tratioGoldAtGate\tratioLocalAtGate\twarExcludedAtGate\thostOwnerAtGate\tpoolGold1783AllNations\tratioGold1783\n')
ser.write('seed\tcanal\tquarter\tyear\tknowers\twarExcluded\tpoolLocal\tpoolGold\tratioGold\tcumGold\tcumLocal\n')
for s in range(1, 11):
    opened = {r['canalId']: (int(r['openedQuarter']), int(r['openedYear'])) for r in rows('canal', s)}
    first = {}; knowQ = collections.defaultdict(dict)
    for r in rows('disc', s):
        if r['kind'] != 'E': continue
        for cid, rq in REQ.items():
            if r['row'] == rq:
                q = int(r['quarter']); n = r['nation']
                if n not in knowQ[cid] or q < knowQ[cid][n]: knowQ[cid][n] = q
                if cid not in first or q < first[cid][0]: first[cid] = (q, int(r['year']), n)
    sav = collections.defaultdict(dict)
    for r in rows('capinv', s): sav[int(r['quarter'])][r['nation']] = fl(r['capitalistSavingsStock'])
    fx = collections.defaultdict(dict)
    for r in rows('nation', s): fx[int(r['quarter'])][r['nation']] = fl(r['fxRate'])
    owner = collections.defaultdict(dict)
    for r in rows('region', s):
        if r['region'] in HOST.values(): owner[r['region']][int(r['quarter'])] = r['nation']
    wars = []
    for r in rows('wars', s):
        try: q0 = int(r['q']); m = fl(r['months'])
        except: continue
        wars.append((q0, q0 + max(1, int(round(m / 3.0))), set(r['sideA'].split('|')), set(r['sideB'].split('|'))))
    def atwar(a, b, q):
        for q0, q1, A, B in wars:
            if q0 <= q < q1 and ((a in A and b in B) or (a in B and b in A)): return True
        return False
    for cid in ('suez', 'panama'):
        if cid not in first or cid not in opened: out.write(f'{s}\t{cid}\t{REQ[cid]}\t-\n'); continue
        g, gy, gn = first[cid]; o, oy = opened[cid]
        cum = 0.0; cuml = 0.0; gate = None
        for q in range(g, o + 1):
            ow = owner[HOST[cid]].get(q, '?')
            kn = [n for n, kq in knowQ[cid].items() if kq <= q]
            ex = [n for n in kn if ow != '?' and n != ow and atwar(n, ow, q)]
            pl = sum(FRAC * max(0.0, sav[q].get(n, 0.0)) for n in kn if n not in ex)
            pg = sum(FRAC * max(0.0, sav[q].get(n, 0.0)) * fx[q].get(n, float('nan')) for n in kn if n not in ex)
            cum += pg; cuml += pl
            ser.write(f'{s}\t{cid}\t{q}\t{1783 + q // 4}\t{len(kn)}\t{len(ex)}\t{pl:.1f}\t{pg:.1f}\t{pg / COST[cid]:.3f}\t{cum:.1f}\t{cuml:.1f}\n')
            if gate is None: gate = (pl, pg, len(ex), ow)
        all83 = sum(FRAC * max(0.0, v) * fx[0].get(n, float('nan')) for n, v in sav[0].items())
        kno = sum(1 for n, kq in knowQ[cid].items() if kq <= o)
        out.write(f'{s}\t{cid}\t{REQ[cid]}\t{g}\t{gy}\t{gn}\t{o}\t{oy}\t{o - g}\t{kno}\t{gate[0]:.1f}\t{gate[1]:.1f}\t{gate[1] / COST[cid]:.3f}\t{gate[0] / COST[cid]:.3f}\t{gate[2]}\t{gate[3]}\t{all83:.1f}\t{all83 / COST[cid]:.3f}\n')
