# (576) 補給崩壊の数（記録）：nation_s の supplyFillShare が 0.3 を割る国×四半期のうち populationK ≥100 の行の数（全ての種の和）。instr.py の頭の定義どおり。
import csv, glob, os, sys
for a in sys.argv[1:]:
    lab, d = a.split('=', 1); n = 0
    for p in glob.glob(os.path.join(d, 'nation_s*.tsv')):
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.DictReader(f, delimiter='\t')
            for x in r:
                try:
                    if float(x['supplyFillShare']) < 0.3 and float(x['populationK']) >= 100: n += 1
                except (KeyError, ValueError, TypeError): pass
    print(lab, n)
