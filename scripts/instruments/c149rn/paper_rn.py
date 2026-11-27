# (1401) c149rn 紙の台本と腕の読み（記録）：種ごとに AI の植民の侵略・国どうしの戦争・侵略の試行（warentry の calls・evals）・変異した勢力の最大・独占の押しの当たり・同盟の数（rnd_s があれば）を数える。
# 使い方: python paper_rn.py <ins の生（D:/…）> <種の起点> <種の数>
import csv, os, sys
D, SB, N = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8') as f: return list(csv.DictReader(f, delimiter='\t'))
def num(x):
    try: return float(x)
    except: return 0.0
print('seed\tinvade\tsubmitted\twarsInterstate\twarsCivil\tsubjCalls\tsubjEvals\tsubjOk\tmutatedMax\tpushHits\talliancePairsMaxQ\talliancePairsMeanQ\tcropDiseasedRowsQ')
for s in range(SB, SB + N):
    inv = rows('invade', s) or []; wars = rows('wars', s) or []; we = rows('warentry', s) or []
    dc = rows('dcont', s) or []; na = rows('nation', s) or []; rn = rows('rnd', s)
    mut = max((num(r.get('factionsMutatedCount')) for r in dc), default=0)
    push = sum(num(r.get('monopolyPushHitCount')) for r in na)
    if rn is None: amax = amean = crop = 'NA'
    else:
        byq = {}
        for r in rn: byq[r['quarter']] = byq.get(r['quarter'], 0) + num(r['alliesCount'])
        pairs = [v / 2 for v in byq.values()]
        amax = f'{max(pairs, default=0):g}'; amean = f'{(sum(pairs) / len(pairs) if pairs else 0):.2f}'
        crop = str(sum(1 for r in rn if num(r['cropDiseasedCount']) > 0))
    print('\t'.join(str(x) for x in [s, len(inv), sum(r['submitted'] == '1' for r in inv), sum(r['civilWarFlag'] == '0' for r in wars), sum(r['civilWarFlag'] != '0' for r in wars),
        f"{sum(num(r['calls']) for r in we):g}", f"{sum(num(r['evals']) for r in we):g}", f"{sum(num(r['r_ok']) for r in we):g}", f'{mut:g}', f'{push:g}', amax, amean, crop]))
