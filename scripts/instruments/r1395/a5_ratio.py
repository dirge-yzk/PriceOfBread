# (1395) A5：国をまたぐ売り手×買い手の selP/lp の分布（tradeskip の ratioBin・10 種）＝危険が最大 +1（主食は +1.5）で着値が lp を超えるか
import csv, sys, collections
RAW, OUT = sys.argv[1], sys.argv[2]
edges = ['<0.5', '0.5-0.8', '0.8-0.9', '0.9-1.0', '1.0-1.1', '1.1-1.25', '1.25-1.5', '1.5-2', '2-4', '>=4']
agg = collections.defaultdict(lambda: [0.0] * 10)
for s in range(1, 11):
    with open(f'{RAW}/tradeskip_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            y = int(r['year']); era = '1783-1829' if y < 1830 else '1830-1879' if y < 1880 else '1880-1950'
            for b in range(10): agg[era][b] += float(r[f'ratioBin{b}'] or 0)
o = open(f'{OUT}/a5_ratio_bins.tsv', 'w', encoding='utf-8', newline='\n')
o.write('era\t' + '\t'.join(edges) + '\ttotal\tshareBelow0.5\tshareBelow0.8\n')
for era in sorted(agg):
    v = agg[era]; t = sum(v)
    o.write(era + '\t' + '\t'.join(f'{x / t:.4f}' for x in v) + f'\t{t:.0f}\t{v[0] / t:.4f}\t{(v[0] + v[1]) / t:.4f}\n')
