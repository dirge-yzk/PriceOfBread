# (1004) 同じ国の中で交戦の四半期と交戦なしの四半期の R（陸の大きさ÷(陸の必要量＋内の守り)）を比べる（9 国・毎四半期の行）。
# 使い方：python -X utf8 -I wr1004_warpeace.py <keep のルート> <出力のフォルダ>
import sys, os
ROOT, OUT = sys.argv[1], sys.argv[2]
NINE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
SEEDS = {'base_ins': range(1, 11), 'n1': range(1, 11), 'n2': range(1, 11), 'a11': range(11, 21)}
def med(xs):
    xs = sorted(xs); n = len(xs)
    return float('nan') if n == 0 else (xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2]))
out = []
for b, ss in SEEDS.items():
    R = {n: ([], []) for n in NINE}
    for s in ss:
        L = open(f'{ROOT}/{b}/fm_s{s}.tsv', 'rb').read().decode('utf-8').splitlines()
        h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in L[1:]:
            c = l.split('\t'); n = c[ix['nation']]
            if n not in R: continue
            try:
                lp = float(c[ix['landPotentialIndex']]); need = float(c[ix['needLandIndex']]) + float(c[ix['needInternalIndex']]); wm = float(c[ix['atWarMonthsCount']])
            except ValueError: continue
            if not (need > 0): continue
            R[n][1 if wm > 0 else 0].append(lp / need)
    out.append(f'{b}: ' + '・'.join(f'{n} 平時 {med(R[n][0]):.2f}({len(R[n][0])}) 交戦 {med(R[n][1]):.2f}({len(R[n][1])})' for n in NINE))
os.makedirs(OUT, exist_ok=True)
open(os.path.join(OUT, 'wr1004_warpeace.txt'), 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('ok')
