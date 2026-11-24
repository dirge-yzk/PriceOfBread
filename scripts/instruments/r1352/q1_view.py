# (1352) q1_series.py の出力から、州 A と行き先 B を並べて間引いて出す
import sys, csv
src, arm, A, B, step = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], int(sys.argv[5])
rows = {}
with open(src, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'):
        if r['arm'] != arm: continue
        rows[(int(r['quarter']), r['region'])] = r
qs = sorted({q for q, _ in rows})
print('q\tyr\tpopA\toppA\tstayA\tfoodA\tempA\tlandA\twageA\thousA\tholdA\toutA\tinA\txoutA\tfacA\toppB\tfoodB\thousB\tpopB\tgap')
def g(r, c): return float(r[c])
for q in qs:
    if q % step: continue
    a, b = rows.get((q, A)), rows.get((q, B))
    if not a or not b: continue
    gap = g(b,'opportunityIndex') - g(a,'stayApprox') - 0.15
    print('\t'.join([str(q), a['year']] + ['%.0f'%g(a,'populationK')] + ['%.3f'%g(a,c) for c in ['opportunityIndex','stayApprox','foodFillShare','employmentShare','landFactor','wageFactor','housingFactor','holdingFactor']] + ['%.1f'%g(a,c) for c in ['migrationOutKPerQuarter','migrationInKPerQuarter','crossBorderOutKPerQuarter']] + ['%.0f'%g(a,'factoryOutputRefValuePerMonth'), '%.3f'%g(b,'opportunityIndex'), '%.3f'%g(b,'foodFillShare'), '%.3f'%g(b,'housingFactor'), '%.0f'%g(b,'populationK'), '%.3f'%gap]))
