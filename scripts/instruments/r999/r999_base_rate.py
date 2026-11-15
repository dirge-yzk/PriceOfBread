# (999) 底の率：植民地格の州×四半期で、その四半期か前の四半期に流入（移民・越境・入植）が正の割合
# 使い方: python -X utf8 -I r999_base_rate.py <出力> <名前>=<insフォルダ> ...
import csv, glob, os, sys
out = open(sys.argv[1], 'w', encoding='utf-8')
for arg in sys.argv[2:]:
    name, d = arg.split('=', 1)
    n = hit = 0
    for p in glob.glob(os.path.join(d, 'region_s*.tsv')):
        R = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                if x[ix['tier']] != 'Colonial' or float(x[ix['populationK']]) <= 0: continue
                R[(int(x[0]), x[ix['region']])] = float(x[ix['migrationInKPerQuarter']]) + float(x[ix['crossBorderInKPerQuarter']]) + (float(x[ix['settlerInKPerQuarter']]) if 'settlerInKPerQuarter' in ix else 0.0)
        for (q, reg), v in R.items():
            n += 1
            if v > 0 or R.get((q - 1, reg), 0) > 0: hit += 1
    out.write(f'{name} 植民地格の州×四半期 {n}  その四半期か前に流入＞0 {hit}  割合 {hit/max(1,n):.4f}\n')
out.close(); print('ok')
