#!/usr/bin/env python3
# (446) 線 md：事前登録 追記2 [追2-2] の記録の線 R1〜R5 を計器つき 10 種から読む。読むだけ・採否に使わない。
# 使い方： python an_md446_rec.py <B の ins フォルダ> <A（m443_ins）の ins フォルダ> [種の数=10]
# R1 低識字国（清・ロシア・オスマン・日本）の P4 の識字 B÷A／R2 清の医者千人あたり（P4）B÷A／R3 教育の買えた量の指数（spend_s の 教育 qty÷popK・農民と工場労働者の人口加重・P2〜P4）B÷A（8 国）／
# R4 8 国の 1900 年・1949 年の totalPopulationK（nation_s）B÷A／R5 healthcareAbstractShare（med_s・B だけ。A の med_s は m443_ins に無い＝B÷A は出せない）。
# 値は〈種ごとの B÷A〉の種どうしの中央値（と最小・最大）。限界（自己申告）：期の値は期の中の四半期の中央値／R3 は spend_s の窓（bucket 3〜6＝1793〜1849・7＝1850〜82・8＝1883〜1949）／nation_s は年の中の挙げた四半期の中央値。
import csv, sys, statistics as st, collections
sys.stdout.reconfigure(encoding='utf-8')
B = sys.argv[1].rstrip('/') + '/'
A = sys.argv[2].rstrip('/') + '/'
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
LOW4 = ['清', 'ロシア', 'オスマン', '日本']
P = {'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667)}
EDUB = {'1793-1849': (3, 4, 5, 6), '1850-1882': (7,), '1883-1949': (8,)}   # spend_s の窓（bucket）。期 P2〜P4 とは窓が違う


_CACHE = {}


def rd(path):
    # 同じ表を国×期ごとに読み直すと遅い＝表ごとに 1 回だけ読んで覚える（spend_s は教育の行だけ）
    if path not in _CACHE:
        with open(path, encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t')
            h = next(r)
            ix = {n: i for i, n in enumerate(h)}
            keep = 'spend_s' in path
            rows = [row for row in r if not keep or (row[ix['slotName']] == '教育' and row[ix['kind']] == 'good')]
        _CACHE[path] = (ix, rows)
    ix, rows = _CACHE[path]
    for row in rows:
        yield ix, row


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def col_med(base, table, seed, nation, col, qa, qb, qcol='quarter'):
    v = []
    for ix, row in rd(base + '%s%d.tsv' % (table, seed)):
        if row[ix['nation']] != nation:
            continue
        try:
            q = int(row[ix[qcol]])
        except Exception:
            continue
        if qa <= q <= qb:
            v.append(fl(row[ix[col]]))
    return med(v)


def doc_per1000(base, seed, nation, qa, qb):
    tot = collections.defaultdict(float)
    doc = {}
    for ix, row in rd(base + 'class_s%d.tsv' % seed):
        if row[ix['nation']] != nation:
            continue
        t = row[ix['time']]
        if not t.startswith('q'):
            continue
        q = int(t[1:])
        if not (qa <= q <= qb):
            continue
        tot[q] += fl(row[ix['sizeK']])
        if row[ix['className']] == '医者':
            doc[q] = fl(row[ix['sizeK']])
    return med([doc[q] / tot[q] * 1000 for q in doc if tot[q] > 0])


def class_size(base, seed, nation):
    size = collections.defaultdict(list)
    for ix, row in rd(base + 'class_s%d.tsv' % seed):
        if row[ix['nation']] != nation:
            continue
        t = row[ix['time']]
        if t.startswith('q'):
            size[(row[ix['className']], 1783 + int(t[1:]) // 4)].append(fl(row[ix['sizeK']]))
    return {k: st.mean(v) for k, v in size.items()}


def edu_index(base, seed, nation, buckets, sz):
    num = den = 0.0
    for ix, row in rd(base + 'spend_s%d.tsv' % seed):
        if row[ix['nation']] != nation or row[ix['slotName']] != '教育' or row[ix['kind']] != 'good':
            continue
        if row[ix['className']] not in ('農民', '工場労働者'):
            continue
        if int(row[ix['bucket']]) not in buckets:
            continue
        fq = int(row[ix['firstQuarter']])
        lq = int(row[ix['lastQuarter']])
        if lq < 0:
            lq = 667   # 最後の窓は終わりが開いている
        ys = range(1783 + fq // 4, 1783 + lq // 4 + 1)
        pk = [sz[(row[ix['className']], y)] for y in ys if (row[ix['className']], y) in sz]
        if not pk:
            continue
        pk = st.mean(pk)
        w = fl(row[ix['monthsCount']]) if 'monthsCount' in ix else 1.0
        num += fl(row[ix['qtyPerMonth']]) / pk * pk * w
        den += pk * w
    return num / den if den > 0 else float('nan')


def pop_year(base, seed, nation, year):
    v = []
    for ix, row in rd(base + 'nation_s%d.tsv' % seed):
        if row[ix['nation']] == nation and int(row[ix['year']]) == year:
            v.append(fl(row[ix['totalPopulationK']]))
    return med(v)


def show(title, d):
    print('#', title)
    for k, v in d.items():
        vv = [x for x in v if x == x]
        if not vv:
            print(f'  {k}: 値なし'); continue
        print(f'  {k}: 中央 {st.median(vv):.3f}（最小 {min(vv):.3f}・最大 {max(vv):.3f}・{len(vv)} 種）')


r1 = collections.defaultdict(list)
r1b = collections.defaultdict(list)
r1a = collections.defaultdict(list)
r2 = []
r2b = []
r2a = []
r3 = collections.defaultdict(list)
r4 = collections.defaultdict(list)
r5 = collections.defaultdict(list)
for s in range(1, NS + 1):
    _CACHE.clear()
    for n in NAT8:
        b = col_med(B, 'reform_s', s, n, 'literacyShare', *P['P4'])
        a = col_med(A, 'reform_s', s, n, 'literacyShare', *P['P4'])
        r1[n].append(b / a if a else float('nan')); r1b[n].append(b); r1a[n].append(a)
    b = doc_per1000(B, s, '清', *P['P4']); a = doc_per1000(A, s, '清', *P['P4'])
    r2.append(b / a if a else float('nan')); r2b.append(b); r2a.append(a)
    for n in NAT8:
        szB = class_size(B, s, n); szA = class_size(A, s, n)
        for p, bk in EDUB.items():
            b = edu_index(B, s, n, bk, szB); a = edu_index(A, s, n, bk, szA)
            r3[(n, p)].append(b / a if a and a == a else float('nan'))
        for y in (1900, 1949):
            b = pop_year(B, s, n, y); a = pop_year(A, s, n, y)
            r4[(n, y)].append(b / a if a and a == a else float('nan'))
        for p, (qa, qb) in P.items():
            r5[(n, p)].append(col_med(B, 'med_s', s, n, 'healthcareAbstractShare', qa, qb))

print('# (446) md 記録の線 R1〜R5（種', NS, '・採否に使わない）')
show('R1 識字 P4 の B÷A（8 国・うち低識字 4 国＝清・ロシア・オスマン・日本）', {n: r1[n] for n in NAT8})
print('  （B の識字の中央 / A の識字の中央）', {n: (round(med(r1b[n]), 3), round(med(r1a[n]), 3)) for n in NAT8})
show('R2 清の医者千人あたり P4 の B÷A', {'清': r2})
print('  （B の中央 / A の中央）', round(med(r2b), 3), round(med(r2a), 3))
show('R3 教育の買えた量の指数 B÷A（農民と工場労働者の人口加重）', {f'{n} {p}': r3[(n, p)] for n in NAT8 for p in EDUB})
show('R4 人口 B÷A', {f'{n} {y}': r4[(n, y)] for n in NAT8 for y in (1900, 1949)})
print('# R5 healthcareAbstractShare（B のみ・期の中央・種の中央）')
for n in NAT8:
    print('  ', n, {p: round(med(r5[(n, p)]), 3) for p in P})
