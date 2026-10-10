"""(185) 工場の設備と労働の器の読み手（読むだけ）。python an_cap185.py <cap の出力の dir> [窓の年=10]
10年の窓ごとに、窓の中の年を平均してから種の中央。実質＝名目÷物価水準 PL・銀建て＝労働の値段÷銀の値段（銀何単位）・金建て＝名目×為替。"""
import sys, os, glob, statistics as st
d = sys.argv[1]
WIN = int(sys.argv[2]) if len(sys.argv) > 2 else 10
NAT = ['イギリス', '清', 'フランス', 'プロイセン', 'アメリカ', 'ロシア', '日本', 'オーストリア']
rows = {}
hdr = None
for f in sorted(glob.glob(os.path.join(d, 't*.txt'))):
    seed = os.path.basename(f)[1:-4]
    for ln in open(f, encoding='utf-8'):
        if ln.startswith('#C'):
            hdr = ln.rstrip('\n').split('\t'); continue
        if not ln.startswith('C\t'): continue
        x = ln.rstrip('\n').split('\t')
        r = dict(zip(hdr, x))
        rows[(seed, r['nation'], int(r['yr']))] = r
seeds = sorted({k[0] for k in rows})

def fl(v):
    try:
        v = float(v); return v if v == v else None
    except: return None

def q(r, name):
    g = lambda k: fl(r[k])
    pl = g('PL') or 1.0
    lp = g('labPx'); sv = g('silverPx'); fx = g('fx')
    capH = g('labCapH'); dem = g('labDem'); wkN = g('wkN') or 0
    fac = g('facOutV') or 0; oth = g('otherOutV') or 0; pop = g('pop') or 0
    v = {
        '労働の需要÷器(人力)': dem / capH if capH else None,
        '労働の需要÷器(全レシピ)': dem / g('labCapAll') if g('labCapAll') else None,
        '労働の実質の値段': lp / pl if lp is not None else None,
        '労働の銀建て(銀の単位)': lp / sv if (lp is not None and sv) else None,
        '労働の金建て': lp * fx if (lp is not None and fx is not None) else None,
        '労働÷主食': (lp / g('foodPx')) if (lp is not None and g('foodPx')) else None,
        '工場労働者の失業': g('wkU'),
        '工場労働者1人の月収(実質)': (g('wkEarn') / wkN / pl) if wkN > 0 else None,
        '工場の稼働(産出÷能力)': (g('sumOut') / g('sumCap')) if g('sumCap') else None,
        '設備なしの生産÷工場の産出': (g('noKOutV') / fac) if fac > 0 else None,
        '値段の暴れ(四半期の|対数の変化|)': g('pxVol'),
        '工業の割合(工場÷産出)': fac / (fac + oth) if fac + oth > 0 else None,
        '1人あたり工場の産出': fac / pop if pop > 0 else None,
        '工場の設備(Σ Scale)': g('sumK'),
        '国営の割合(ΣB÷ΣK)': (g('sumB') / g('sumK')) if g('sumK') else None,
        '工場労働者÷人口': wkN / pop if pop > 0 else None,
    }
    return v.get(name)

NAMES = ['労働の需要÷器(人力)', '労働の需要÷器(全レシピ)', '労働の実質の値段', '労働の銀建て(銀の単位)', '労働の金建て', '労働÷主食', '工場労働者の失業', '工場労働者1人の月収(実質)',
         '工場の稼働(産出÷能力)', '設備なしの生産÷工場の産出', '値段の暴れ(四半期の|対数の変化|)', '工業の割合(工場÷産出)', '1人あたり工場の産出', '工場の設備(Σ Scale)', '国営の割合(ΣB÷ΣK)', '工場労働者÷人口']
if len(sys.argv) > 3: NAMES = sys.argv[3].split(',')
wins = list(range(1783, 1951, WIN))

def cell(n, w0, name):
    per = []
    for s in seeds:
        vals = [q(rows[(s, n, y)], name) for y in range(w0, w0 + WIN) if (s, n, y) in rows]
        vals = [v for v in vals if v is not None]
        if vals: per.append(sum(vals) / len(vals))
    return st.median(per) if per else None

print(f'種 {len(seeds)}（{",".join(seeds)}）・窓 {WIN}年・種の中央（窓の中の年の平均）')
for name in NAMES:
    print(f'\n## {name}')
    print('国\t' + '\t'.join(str(w) for w in wins))
    for n in NAT:
        cs = [cell(n, w0, name) for w0 in wins]
        print(n + '\t' + '\t'.join('' if c is None else f'{c:.3g}' for c in cs))
# 比（英÷清）と追いつき（÷英）
print('\n## 比 英÷清（種ごとの比の中央）')
for name in ['労働の実質の値段', '労働の銀建て(銀の単位)', '労働の金建て', '工場労働者1人の月収(実質)', '労働÷主食', '労働の需要÷器(人力)']:
    cs = []
    for w0 in wins:
        per = []
        for s in seeds:
            a = [q(rows[(s, 'イギリス', y)], name) for y in range(w0, w0 + WIN) if (s, 'イギリス', y) in rows]
            b = [q(rows[(s, '清', y)], name) for y in range(w0, w0 + WIN) if (s, '清', y) in rows]
            a = [v for v in a if v is not None]; b = [v for v in b if v is not None]
            if a and b and sum(b) > 0: per.append((sum(a) / len(a)) / (sum(b) / len(b)))
        cs.append(st.median(per) if per else None)
    print(name + '\t' + '\t'.join('' if c is None else f'{c:.3g}' for c in cs))
print('\n## 1人あたり工場の産出÷英（種ごとの比の中央）')
for n in NAT:
    cs = []
    for w0 in wins:
        per = []
        for s in seeds:
            a = [q(rows[(s, n, y)], '1人あたり工場の産出') for y in range(w0, w0 + WIN) if (s, n, y) in rows]
            b = [q(rows[(s, 'イギリス', y)], '1人あたり工場の産出') for y in range(w0, w0 + WIN) if (s, 'イギリス', y) in rows]
            a = [v for v in a if v is not None]; b = [v for v in b if v is not None]
            if a and b and sum(b) > 0: per.append((sum(a) / len(a)) / (sum(b) / len(b)))
        cs.append(st.median(per) if per else None)
    print(n + '\t' + '\t'.join('' if c is None else f'{c:.3g}' for c in cs))
