# (1081) c103dm：DM-B の記録（門にしない・事前登録 §5）。読むだけ。
#   python -X utf8 -I rec_dm.py B=<ins> M=<ins> N1=<ins> N2=<ins>
#   ①灌漑の持ち主 7 国の人口（anchor_s の popK・capacity・arableSum・年のラベル 1800・1850・1900・1949 の最後の行・10 種の中央）
#   ②ポーランドの 1800 年の工業化（eq_s の indMean・同上）
#   ③正統性の窓（scalars_s の legitimacy・1785〜1805 のラベル・選んだ 9 国・国×四半期の中央と 0.95 超の割合）
#   各腕の種の中央を並べ、B÷M と N1÷M・N2÷M（ヌル腕の幅）を出す。
import sys, os, csv, statistics
csv.field_size_limit(1 << 30)
arms = dict(a.split('=', 1) for a in sys.argv[1:])
SEEDS = range(1, 11)
NAT7 = ['オスマン', 'デリー', 'シク連邦', 'マイソール', 'サルデーニャ＝ピエモンテ', 'トスカーナ', '阮氏']
YEARS = [1800, 1850, 1900, 1949]
def last_by(path, keycols, want):
    out = {}
    if not os.path.exists(path): return out
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: h.index(c) for c in keycols + want}
        for row in r:
            k = tuple(row[ix[c]] for c in keycols)
            out[k] = {c: row[ix[c]] for c in want}
    return out
def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')
def num(v):
    try: return float(v)
    except: return float('nan')
res = {}
for nm, d in arms.items():
    pop = {}; ind = []; leg = []
    for s in SEEDS:
        a = last_by(os.path.join(d, f'anchor_s{s}.tsv'), ['year', 'nation'], ['popK', 'capacity', 'arableSum'])
        for n in NAT7:
            for y in YEARS:
                v = a.get((str(y), n))
                if v: pop.setdefault((n, y), []).append(tuple(num(v[c]) for c in ('popK', 'capacity', 'arableSum')))
        e = last_by(os.path.join(d, f'eq_s{s}.tsv'), ['year', 'nation'], ['indMean'])
        v = e.get(('1800', 'ポーランド'))
        if v: ind.append(num(v['indMean']))
        p = os.path.join(d, f'scalars_s{s}.tsv')
        if os.path.exists(p):
            with open(p, encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); iy, il = h.index('year'), h.index('legitimacy')
                for row in r:
                    if 1785 <= int(row[iy]) <= 1805: leg.append(num(row[il]))
    res[nm] = (pop, ind, leg)
print('== ① 灌漑の持ち主 7 国（anchor_s・10 種の中央：popK／capacity／arableSum）')
for n in NAT7:
    for y in YEARS:
        cells = []
        for nm in arms:
            vals = res[nm][0].get((n, y), [])
            cells.append(f'{nm} {med([v[0] for v in vals]):.4g}/{med([v[1] for v in vals]):.4g}/{med([v[2] for v in vals]):.4g}(n{len(vals)})')
        b = med([v[0] for v in res['B'][0].get((n, y), [])]); m = med([v[0] for v in res['M'][0].get((n, y), [])])
        nul = [med([v[0] for v in res[k][0].get((n, y), [])]) / m - 1 for k in arms if k.startswith('N') and m == m and m]
        print(f'  {n} {y}：' + '・'.join(cells) + f'　popK の B÷M−1 {b / m - 1:+.3f}・ヌル腕 ' + '・'.join(f'{x:+.3f}' for x in nul))
print('== ② ポーランドの 1800 年の工業化（eq_s indMean・10 種の中央）')
print('  ' + '・'.join(f'{nm} {med(res[nm][1]):.4g}(n{len(res[nm][1])})' for nm in arms))
print('== ③ 正統性の窓 1785〜1805（scalars_s・選んだ 9 国×四半期と年末の行）')
for nm in arms:
    L = res[nm][2]
    print(f'  {nm}：行 {len(L)}・中央 {med(L):.4f}・0.95 超 {sum(1 for x in L if x > 0.95) / max(1, len(L)):.3f}')
