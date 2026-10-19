#!/usr/bin/env python3
# (446) 線 md：事前登録 measurements/md446_20261020_prereg.txt [8] の狙い a1〜a8 を計器つき 10 種から読む。読むだけ。
# 使い方： python an_md446.py <B の ins フォルダ> <A（m443_ins）の ins フォルダ> [種の数=10]
#   B の ins に med_s<seed>.tsv・spend_s・class_s があること。A（基線）は spend_s・class_s を使う（med_s は無くてよい）。
# 判定：10 種のうち 7 種以上＝当たり・4〜6＝部分・3 以下＝外れ。採る条件＝a1・a2・a7・a8・a9 が当たりで a3〜a6 に外れが無い（a9＝追記1）。
# 限界（自己申告）：期の値は〈期の中の四半期の中央値→種どうしの中央値〉／a4 は class_s の q<N> 行（挙げた国のみ毎四半期）／
#   a8 の指数は spend_s の窓（bucket 3〜6＝1793〜1849）の qty÷(0.05×窓の人口の平均)で crowding を含む。
import csv, sys, statistics as st, collections, math
sys.stdout.reconfigure(encoding='utf-8')
B = sys.argv[1].rstrip('/') + '/'
A = sys.argv[2].rstrip('/') + '/'
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
UK, QING, PRU = 'イギリス', '清', 'プロイセン'
NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
P = {'P1': (0, 67), 'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667), 'OPEN': (13, 47)}


def rd(path):
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {n: i for i, n in enumerate(h)}
        for row in r:
            yield ix, row


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


def med_s(seed):
    """seed の med_s を {(nation, period, col): median} にする"""
    acc = collections.defaultdict(list)
    for ix, row in rd(B + 'med_s%d.tsv' % seed):
        q = int(row[ix['quarter']])
        n = row[ix['nation']]
        for pn, (a, b) in P.items():
            if a <= q <= b:
                for c in ix:
                    if c not in ('quarter', 'year', 'nation'):
                        acc[(n, pn, c)].append(fl(row[ix[c]]))
    return {k: med(v) for k, v in acc.items()}


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


def spend_index(base, seed, nation):
    size = collections.defaultdict(list)
    for ix, row in rd(base + 'class_s%d.tsv' % seed):
        if row[ix['nation']] != nation:
            continue
        t = row[ix['time']]
        if t.startswith('q'):
            size[(row[ix['className']], 1783 + int(t[1:]) // 4)].append(fl(row[ix['sizeK']]))
    sz = {k: st.mean(v) for k, v in size.items()}
    num = 0.0
    den = 0.0
    for ix, row in rd(base + 'spend_s%d.tsv' % seed):
        if row[ix['nation']] != nation or row[ix['slotName']] != '医療' or row[ix['kind']] != 'good':
            continue
        if int(row[ix['bucket']]) not in (3, 4, 5, 6) or row[ix['className']] not in ('農民', '工場労働者'):
            continue
        fq = int(row[ix['firstQuarter']])
        lq = int(row[ix['lastQuarter']])
        ys = range(1783 + fq // 4, 1783 + lq // 4 + 1)
        pk = [sz[(row[ix['className']], y)] for y in ys if (row[ix['className']], y) in sz]
        if not pk:
            continue
        pk = st.mean(pk)
        w = fl(row[ix['monthsCount']]) if 'monthsCount' in ix else 1.0
        num += fl(row[ix['qtyPerMonth']]) / (0.05 * pk) * pk * w
        den += pk * w
    return num / den if den > 0 else float('nan')


def spearman(x, y):
    def rk(v):
        o = sorted(range(len(v)), key=lambda i: v[i])
        r = [0.0] * len(v)
        i = 0
        while i < len(o):
            j = i
            while j + 1 < len(o) and v[o[j + 1]] == v[o[i]]:
                j += 1
            for k in range(i, j + 1):
                r[o[k]] = (i + j) / 2.0
            i = j + 1
        return r
    rx, ry = rk(x), rk(y)
    mx, my = st.mean(rx), st.mean(ry)
    sx = math.sqrt(sum((a - mx) ** 2 for a in rx))
    sy = math.sqrt(sum((b - my) ** 2 for b in ry))
    return sum((a - mx) * (b - my) for a, b in zip(rx, ry)) / (sx * sy) if sx > 0 and sy > 0 else float('nan')


def lit_p4(base, seed, nation):
    """reform_s の literacyShare の P4（Q468〜Q667）の中央値"""
    v = []
    for ix, row in rd(base + 'reform_s%d.tsv' % seed):
        if row[ix['nation']] != nation:
            continue
        try:
            q = int(row[ix['quarter']])
        except Exception:
            continue
        if 468 <= q <= 667:
            v.append(fl(row[ix['literacyShare']]))
    return med(v)


def tier(k):
    # 10 種で 7 以上＝当たり・4〜6＝部分・3 以下＝外れ（種の数が違うときは割合で読む）
    return '当たり' if k * 10 >= 7 * NS else '部分' if k * 10 >= 4 * NS else '外れ'


res = collections.defaultdict(list)
rec = collections.defaultdict(list)
for s in range(1, NS + 1):
    m = med_s(s)

    def g(n, p, c):
        return m.get((n, p, c), float('nan'))

    q4 = g(UK, 'P4', 'doctorCareShare')
    res['a1'].append(q4 >= 0.25 and q4 >= g(UK, 'P2', 'doctorCareShare'))
    res['a2'].append(g(UK, 'P4', 'massCareShare') >= 0.30)
    res['a3'].append(g(QING, 'P4', 'doctorCareShare') <= 0.5 * q4)
    b34 = doc_per1000(B, s, UK, 268, 667)
    a34 = doc_per1000(A, s, UK, 268, 667)
    res['a4'].append(b34 / a34 >= 1.3 if a34 == a34 and a34 > 0 else False)
    rec['a4_ratio'].append(b34 / a34 if a34 else float('nan'))
    op = g(UK, 'OPEN', 'priceMean')
    pr = [g(UK, p, 'priceMean') / op for p in ('P2', 'P3', 'P4')]
    res['a5'].append(all(x <= 1.5 for x in pr))
    rec['a5_max'].append(max(pr))
    res['a6'].append(all(g(n, p, 'soldOverProducedShare') >= 0.8 for n in (UK, PRU) for p in ('P3', 'P4')))
    ml = [g(n, 'P4', 'massLiteracyShare') for n in NAT8]
    mc = [g(n, 'P4', 'massCareShare') for n in NAT8]
    ok = [i for i in range(len(NAT8)) if ml[i] == ml[i] and mc[i] == mc[i]]
    rho = spearman([ml[i] for i in ok], [mc[i] for i in ok])
    res['a7'].append(rho >= 0.5)
    rec['a7_rho'].append(rho)
    bi = spend_index(B, s, UK)
    ai = spend_index(A, s, UK)
    res['a8'].append(bi >= 2 * ai if ai == ai and ai > 0 else False)
    rec['a8_B'].append(bi)
    rec['a8_A'].append(ai)
    ratios = [lit_p4(B, s, n) / lit_p4(A, s, n) for n in (UK, 'フランス', PRU)]
    res['a9'].append(all(r >= 0.9 for r in ratios))
    rec['a9_min_ratio'].append(min(ratios))
    for p in ('P1', 'P2', 'P3', 'P4'):
        rec['uk_Q_' + p].append(g(UK, p, 'doctorCareShare'))
        rec['uk_massQ_' + p].append(g(UK, p, 'massCareShare'))
        rec['uk_price_' + p].append(g(UK, p, 'priceMean'))
        rec['uk_held_' + p].append(g(UK, p, 'heldBudgetShare'))
        rec['uk_massNeed_' + p].append(g(UK, p, 'massNeedFillShare'))
        rec['qing_Q_' + p].append(g(QING, p, 'doctorCareShare'))

names = {
    'a1': '英の Q（P4 が 0.25 以上かつ P4 が P2 以上）',
    'a2': '英の大衆の Q（P4 が 0.30 以上）',
    'a3': '清の Q が英の 0.5 倍以下（P4）',
    'a4': '英の医者千人あたり B÷A が 1.3 以上（P3〜P4）',
    'a5': '英の医療の値段が開幕期の 1.5 倍以下（P2〜P4）',
    'a6': '英・普の稼働が 0.8 以上（P3〜P4）',
    'a7': '識字と大衆の Q の順位相関が 0.5 以上（P4・8 国）',
    'a8': '英の谷の指数 B が A の 2 倍以上',
    'a9': '英・仏・普の P4 の識字が基線の 0.9 倍以上（教育を削らない）',
}
print('# (446) md 狙い a1〜a8（種', NS, '）')
tiers = {}
for a in sorted(res):
    k = sum(1 for x in res[a] if x)
    tiers[a] = tier(k)
    print(f'{a} {names[a]}: {k}/{len(res[a])} → {tiers[a]}')
adopt = all(tiers[a] == '当たり' for a in ('a1', 'a2', 'a7', 'a8', 'a9')) and all(tiers[a] != '外れ' for a in ('a3', 'a4', 'a5', 'a6'))
print('採る条件（a1・a2・a7・a8・a9 が当たりで a3〜a6 に外れ無し）:', '満たす' if adopt else '満たさない')
print('# 付記（採否に使わない・種どうしの中央値）')
for k in sorted(rec):
    print(f'{k}: {med(rec[k]):.4g}')
