# (1196) c119ra 問い2：飢えの 1/4 未満の 6 件（an_h2.py -v4 の M の読み）の州の軌道を、M と Amob の同じ種で並べる（読むだけ）。
#  加えて種ごとに、M と Amob の nation_s が最初に分かれる四半期と国（兵の数・人口）を出す（分かれた後は軌道が別＝州ごとの帰属は弱い）。
#  使い方: python -X utf8 -I q2_cases.py <M の ins> <Amob の ins> <種の起点> <種の終わり> <件の表 tsv（seed\tregion\tnation）>
import sys, csv, os, collections
sys.stdout.reconfigure(newline='\n')
M, A, sb, se, cases = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for c in r:
            if len(c) >= len(h): yield ix, c


def first_split(s):
    a = {};
    for ix, c in rd(os.path.join(M, f'nation_s{s}.tsv')):
        a[(c[ix['quarter']], c[ix['nation']])] = (c[ix['soldierK']], c[ix['totalPopulationK']], c[ix['year']])
    best = None
    for ix, c in rd(os.path.join(A, f'nation_s{s}.tsv')):
        k = (c[ix['quarter']], c[ix['nation']]); v = a.get(k)
        if v is None or v[0] != c[ix['soldierK']] or v[1] != c[ix['totalPopulationK']]:
            q = int(k[0])
            if best is None or q < best[0]: best = (q, k[1], c[ix['year']], v, (c[ix['soldierK']], c[ix['totalPopulationK']]))
    return best


for s in range(sb, se + 1):
    b = first_split(s)
    if b: print(f's{s} 最初に分かれる四半期 {b[0]}（{b[2]} 年）{b[1]}：M 兵 {b[3][0] if b[3] else "行なし"} 人口 {b[3][1] if b[3] else ""} → Amob 兵 {b[4][0]} 人口 {b[4][1]}')
    else: print(f's{s} 分かれない')
C = [l.rstrip('\n').split('\t') for l in open(cases, encoding='utf-8') if l.strip() and not l.startswith('#')]
for seed, reg, nat in C:
    s = int(seed)
    print(f'## s{s} {reg}（{nat}）')
    for nm, d in (('M', M), ('Amob', A)):
        rows = {}
        for ix, c in rd(os.path.join(d, f'region_s{s}.tsv')):
            if c[ix['region']] != reg: continue
            y = int(c[ix['year']])
            rows[y] = (c[ix['nation']], float(c[ix['populationK']]), c[ix['foodFillShare']], c[ix['warIntensity']])
        nat_rows = {}
        for ix, c in rd(os.path.join(d, f'nation_s{s}.tsv')):
            if c[ix['nation']] != nat: continue
            y = int(c[ix['year']])
            if y % 10 == 0 or y not in nat_rows:
                tp = float(c[ix['totalPopulationK']] or 0)
                nat_rows[y] = (float(c[ix['soldierK']]) / tp if tp > 0 else float('nan'), c[ix['atWarMonths']], c[ix['civilWarMonths']])
        peak = max(rows.items(), key=lambda kv: kv[1][1]) if rows else None
        line = []
        for y in range(1790, 1951, 10):
            if y in rows:
                r = rows[y]; n = nat_rows.get(y)
                line.append(f'{y}:{r[1]:.1f}k f{float(r[2]):.2f} w{float(r[3]):.2f}' + (f' 兵{n[0]*100:.1f}% 戦{n[1]} 内{n[2]}' if n else '') + ('' if r[0] == nat else f' 〔{r[0]}〕'))
        print(f'  {nm} ピーク {peak[0]} {peak[1][1]:.1f}k' if peak else f'  {nm} 行なし')
        print('   ' + ' | '.join(line))
