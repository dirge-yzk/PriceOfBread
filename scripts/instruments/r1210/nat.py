# (1210) ① 民族主義の取り分の動き（読むだけ）。用法: python -X utf8 -I nat.py <生のフォルダ> <種の始め> <種の終わり> <出力>
# ideology_s は picked の国だけ四半期ごと・他は年末だけ＝picked の国の四半期の行と、年末の行（全ての国）を分けて読む。
import sys, csv, statistics as st
raw, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
YEARS = [1783, 1790, 1800, 1810, 1820, 1830, 1840, 1848, 1855, 1860, 1866, 1871, 1880, 1900, 1913, 1930, 1950]
NATS = ['プロイセン', 'サルデーニャ', 'フランス', 'オーストリア', 'イギリス', 'ロシア', 'オスマン', '清', '日本', 'アメリカ']

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        for x in r:
            yield dict(zip(h, x))

def fl(x):
    try:
        v = float(x)
        return v if v == v else None
    except Exception:
        return None

data = {}   # (nat, seed) -> {year: (natShare, popMeanShare, consVoice, rows_in_year)} 年末の値（その年の最後の行）
qmax = {}   # (nat, seed) -> (max share, year) 四半期の行での最大（picked の国だけ年の中の頂点が見える）
win = {}    # (nat, seed) -> (min, max) 1848〜1871
cnt = {}
sc = {}     # (nat, seed) -> {year: (defeatShock, externalPressure, militancyMean, suffrage, popK)}
for s in range(s0, s1 + 1):
    for d in rows(f'{raw}/ideology_s{s}.tsv'):
        n = d['nation']
        if n not in NATS: continue
        y = int(d['year']); v = fl(d['ideoNationalNationalismShare']); pm = fl(d['ideoPopMeanNationalismShare'])
        pk = fl(d['popK']); ws = fl(d['nationalWeightSumIndex'])
        if v is None: continue   # 計器が書かない行を 0 と数えない
        k = (n, s)
        cnt[k] = cnt.get(k, 0) + 1
        data.setdefault(k, {})[y] = (v, pm, (ws / (pk * 1000.0)) if (ws is not None and pk) else None)
        m = qmax.get(k)
        if m is None or v > m[0]: qmax[k] = (v, y)
        if 1848 <= y <= 1871:
            a = win.get(k)
            win[k] = (v, v) if a is None else (min(a[0], v), max(a[1], v))
    for d in rows(f'{raw}/scalars_s{s}.tsv'):
        n = d['nation']
        if n not in NATS: continue
        y = int(d['year'])
        sc.setdefault((n, s), {})[y] = (fl(d['defeatShock']), fl(d['externalPressure']), fl(d['militancyMean']), d['suffrage'], fl(d['popK']))

def med(xs):
    xs = [x for x in xs if x is not None]
    return st.median(xs) if xs else None

def f3(x):
    return '' if x is None else f'{x:.3f}'

with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# raw={raw} seeds={s0}..{s1}\n# 表1 国×年＝国民の民族主義の取り分（意識×声の重み）の種の中央 [最小〜最大]・素の人口平均の中央・意識×声の平均の中央（重みの和÷人口）\n')
    o.write('nation\tyear\tnSeeds\tnatShareMed\tnatShareMin\tnatShareMax\tpopMeanMed\tconsVoiceMed\tdefeatShockMed\textPressMed\tmilitancyMed\n')
    for n in NATS:
        for y in YEARS:
            vs = [data[(n, s)][y] for s in range(s0, s1 + 1) if (n, s) in data and y in data[(n, s)]]
            ss = [sc[(n, s)][y] for s in range(s0, s1 + 1) if (n, s) in sc and y in sc[(n, s)]]
            if not vs: continue
            nv = [v[0] for v in vs]
            o.write('\t'.join([n, str(y), str(len(vs)), f3(med(nv)), f3(min(nv)), f3(max(nv)), f3(med([v[1] for v in vs])), f3(med([v[2] for v in vs])),
                               f3(med([x[0] for x in ss])), f3(med([x[1] for x in ss])), f3(med([x[2] for x in ss]))]) + '\n')
    o.write('\n# 表2 国×種＝行の数・全期間の最大と年・1848〜1871 の最小〜最大・0.25 以上の行の数\n')
    o.write('nation\tseed\trows\tmax\tmaxYear\twinMin\twinMax\n')
    for n in NATS:
        for s in range(s0, s1 + 1):
            k = (n, s)
            if k not in cnt: continue
            w = win.get(k, (None, None))
            o.write('\t'.join([n, str(s), str(cnt[k]), f3(qmax[k][0]), str(qmax[k][1]), f3(w[0]), f3(w[1])]) + '\n')
print('ok', out)
