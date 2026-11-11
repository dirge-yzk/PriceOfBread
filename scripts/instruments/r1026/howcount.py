import csv, collections, sys
csv.field_size_limit(1 << 30)
res = collections.Counter(); evn = collections.Counter()
for pre, src, seeds in [('base', 'D:/PoB_work/keep_1022/base_ins', range(1, 11)), ('a11', 'D:/PoB_work/keep_1022/a11', range(11, 21))]:
    how = {}
    for s in seeds:
        for row in csv.DictReader(open(f'{src}/cederule_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            q = int(row['quarter'])
            how[(s, q, row['receiver'], row['region'])] = row['how']
            if q % 4 == 3 and (lambda y: y % 10 == 0 or y == 1913)(1784 + q // 4): evn[row['how']] += 1
    for row in csv.DictReader(open(f'{pre}_mismatch.tsv', encoding='utf-8'), delimiter='\t'):
        if row['class'] != 'a': continue
        hs = {how.get((int(row['seed']), int(row['q']), row['nation'], rg)) for rg in row['movedRegions'].split('|')}
        hs.discard(None)
        res['+'.join(sorted(hs)) or 'founded-only'] += 1
open('q3_byhow.txt', 'w', encoding='utf-8', newline='\n').write(f'class a mismatched pairs by how (seeds 1-20): {dict(res)}\nevents at reading quarters by how: {dict(evn)}\n')
print(open('q3_byhow.txt', encoding='utf-8').read())
