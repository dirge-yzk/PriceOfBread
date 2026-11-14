# (1126) 政体の行き来：主要 9 国で、選挙政体→非選挙→選挙の往復の数と、非選挙の続きの長さ（四半期）の分布。非選挙の政体の符号の内訳。
#   python -X utf8 -I q5.py <名=生 ins> [...]
import sys, os, csv, collections
sys.stdout.reconfigure(newline='\n')
ELECT = {3, 4}
K9 = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']
for a in sys.argv[1:]:
    name, d = a.split('=', 1)
    seeds = sorted(int(f[len('monlaw_s'):-4]) for f in os.listdir(d) if f.startswith('monlaw_s') and f.endswith('.tsv'))
    per = collections.defaultdict(lambda: collections.Counter())
    for s in seeds:
        by = collections.defaultdict(list)
        with open(os.path.join(d, f'monlaw_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if len(x) >= len(h) and x[ix['nation']] in K9:
                    by[x[ix['nation']]].append((int(x[ix['quarter']]), int(x[ix['polityCode']])))
        for n, L in by.items():
            L.sort(); c = per[n]; i = 1
            while i < len(L):
                if L[i - 1][1] in ELECT and L[i][1] not in ELECT:
                    j = i
                    while j < len(L) and L[j][1] not in ELECT: j += 1
                    ln = j - i; c['ne_' + str(L[i][1])] += 1
                    if j < len(L): c['round'] += 1; c['short' if ln <= 4 else 'long'] += 1
                    i = j
                else: i += 1
    print(name + '\t' + '  '.join(f"{n}:往復{c['round']}(≤4期 {c['short']})[" + ','.join(f'{k[3:]}:{v}' for k, v in sorted(c.items()) if k.startswith('ne_')) + ']' for n, c in per.items()))
