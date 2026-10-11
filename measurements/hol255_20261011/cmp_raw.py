"""旧の注入の g<seed>.txt（G5）と新の region_s／migration_s（G10）を行ごとに突き合わせる。python cmp_raw.py <旧 ghost フォルダ> <新 ins フォルダ> [seed...]
 鍵＝(q, idx, region)・新の値を G5 へ丸めてから旧と比べる。M は (q, from, to) で旧の 3 クラスの和と新の量。"""
import sys, os, math, collections
old, new = sys.argv[1], sys.argv[2]
seeds = [int(x) for x in sys.argv[3:]] or sorted(int(f[1:-4]) for f in os.listdir(old) if f.startswith('g') and f.endswith('.txt'))
RC = 'q yr idx nation nreg region tier state pop closed sold crowd peas opp prod ind wage land emp push hous best mOut mIn cOut cIn xOut xIn dPop dom hold pres mil war food wprem arable rural keep'.split()
MAP = {'populationK': 'pop', 'ruralPopulationK': 'rural', 'opportunityIndex': 'opp', 'holdingFactor': 'keep', 'wageFactor': 'wage', 'landFactor': 'land', 'employmentShare': 'emp',
       'pushFactor': 'push', 'housingFactor': 'hous', 'wagePremium': 'wprem', 'arableLand': 'arable', 'migrationOutKPerQuarter': 'mOut', 'migrationInKPerQuarter': 'mIn',
       'settlerOutKPerQuarter': 'cOut', 'settlerInKPerQuarter': 'cIn', 'crossBorderOutKPerQuarter': 'xOut', 'crossBorderInKPerQuarter': 'xIn', 'warIntensity': 'war', 'foodFillShare': 'food'}
TIER = {'Colonial': 'col', 'Core': 'core', 'Integrated': 'int'}
def fl(x):
    try: return float(x)
    except: return float('nan')
def same(newv, oldv):
    if math.isnan(newv) and math.isnan(oldv): return True
    if math.isnan(newv) or math.isnan(oldv): return False
    if oldv == 0: return abs(newv) < 1e-300 or abs(newv) < 5e-5 * 1e-300
    # 旧は G5（5 桁）＝新（G10）との差は 5 桁目の半分以内（G10 側の丸めの境界を許す）
    half = 0.5 * 10 ** (math.floor(math.log10(abs(oldv))) - 4)
    return abs(newv - oldv) <= half * (1 + 1e-6)
def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            yield dict(zip(head, line.rstrip('\n').split('\t')))
tot = collections.Counter(); bad = collections.defaultdict(list); nameDiff = collections.Counter(); nameEx = []
for sd in seeds:
    O = {}
    OM = collections.defaultdict(float)
    for line in open(os.path.join(old, 'g%d.txt' % sd), encoding='utf-8'):
        if line[0] == 'R':
            p = line.rstrip('\n').split('\t')[1:]
            r = dict(zip(RC, p)); O[(int(r['q']) - 1, r['idx'], r['region'])] = r
        elif line[0] == 'M':
            p = line.rstrip('\n').split('\t')
            OM[(int(p[1]) - 1, p[3], p[4])] += fl(p[5]) + fl(p[6]) + fl(p[7])
    seen = set()
    for row in table(os.path.join(new, 'region_s%d.tsv' % sd)):
        key = (int(row['quarter']), row['nationIndex'], row['region'])
        o = O.get(key)
        tot['R行（新）'] += 1
        if o is None: bad['旧に無い行'].append((sd, key)); continue
        seen.add(key)
        for col, k in MAP.items():
            tot['R値'] += 1
            if not same(fl(row[col]), fl(o[k])): bad[k].append((sd, key, row[col], o[k]))
        if TIER.get(row['tier'], '?') != o['tier']: bad['tier'].append((sd, key, row['tier'], o['tier']))
        if ('a' if row['activeFlag'] == '1' else 'l') != o['state']: bad['state'].append((sd, key))
        if int(row['year']) != int(o['yr']): bad['yr'].append((sd, key))
        if row['nation'] != o['nation']:
            nameDiff[(row['nation'], o['nation'])] += 1
    for key in O:
        if key not in seen: bad['新に無い行'].append((sd, key))
    NM = {}
    mg = os.path.join(new, 'migration_s%d.tsv' % sd)
    for row in table(mg):
        NM[(int(row['quarter']), row['fromRegion'], row['toRegion'])] = fl(row['movedKPerQuarter']); tot['M行（新）'] += 1
    for k, v in NM.items():
        if k not in OM: bad['M 旧に無い'].append((sd, k, v)); continue
        tot['M値'] += 1
        if abs(v - OM[k]) > 1e-4 * max(abs(v), 1e-12): bad['M値'].append((sd, k, v, OM[k]))   # 旧は3クラスをそれぞれ G5 に丸めた和＝相対 1e-4 まで
    for k, v in OM.items():
        if k not in NM and v > 0: bad['M 新に無い'].append((sd, k, v))
print('照合：', dict(tot))
print('国の名前の違い（新＝BaseName・旧＝Name）：', sum(nameDiff.values()), '行', len(nameDiff), '通り', list(nameDiff.items())[:8])
for k, v in bad.items(): print('不一致', k, len(v), v[:3])
print('全行一致（名前を除く）' if not bad else '⚠ 不一致あり')
