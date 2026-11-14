# (1128) T1 の上側の門の候補 R4（月の版）と R5（在庫の余り）をヌル腕 3 本で数える（記録・門の閾値の下見）。
#   エンジンの関数は呼ばない。生の nationgood_s と sellr_s を自分で読む。
#   R4a: 国×財で crossImportQty > 2*(demandQty + 0)                 （再輸出 0＝件数の上界）
#   R4b: 国×財で crossImportQty > 2*(demandQty + min(輸入, 輸出))    （再輸出を上から見積もる＝件数の下界）
#        母集合 demandQty > 0。nationgood_s は年の最後の月の 1 回の清算の量（NgHead で 0 戻し）。
#   R5 : 州×財の (stock - targetStock) / lastDemand。sellr_s（年の最後の四半期の終わりの写し）。
#        母集合 (i) lastDemand > 0 / (ii) そのうち prevImportAll > 0。統計＝D の重みつき 99 分位（主）・重みなし 99 分位（記録）。
# 使い方: python -X utf8 -I r45.py 基線=<dir> N1=<dir> N2=<dir> [--years 1870,1913]
import os, re, sys, math, collections as C


def fl(x):
    try:
        v = float(x)
        return v if math.isfinite(v) else float('nan')
    except Exception:
        return float('nan')


def seeds(d, stem):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(stem + r'_s(\d+)\.tsv', f)] if m)


def wq(pairs, q):
    """重みつき分位：値の昇順で重みの累積が q に届いた最初の値。"""
    pairs = [(v, w) for v, w in pairs if w > 0 and v == v]
    if not pairs: return float('nan')
    pairs.sort()
    tot = sum(w for _, w in pairs); acc = 0.0
    for v, w in pairs:
        acc += w
        if acc >= q * tot: return v
    return pairs[-1][0]


def uq(vals, q):
    vals = sorted(v for v in vals if v == v)
    if not vals: return float('nan')
    k = min(len(vals) - 1, max(0, int(math.ceil(q * len(vals))) - 1))
    return vals[k]


def r4(d):
    out = C.defaultdict(lambda: [0, 0, 0])   # 年 -> [母集合, R4a, R4b]
    for s in seeds(d, 'nationgood'):
        with open(os.path.join(d, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                dem = fl(v[ix['demandQty']]); imp = fl(v[ix['crossImportQty']]); exp_ = fl(v[ix['crossExportQty']])
                if not (dem > 0): continue
                imp = imp if imp == imp else 0.0; exp_ = exp_ if exp_ == exp_ else 0.0
                t = out[v[0]]; t[0] += 1
                if imp > 2 * dem: t[1] += 1
                if imp > 2 * (dem + min(imp, exp_)): t[2] += 1
    return out


def r5(d):
    rows = C.defaultdict(lambda: ([], []))   # 年 -> (全 (i) の (比, D), 輸入した行 (ii))
    for s in seeds(d, 'sellr'):
        with open(os.path.join(d, 'sellr_s%d.tsv' % s), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iD, iS, iT, iI = ix['lastDemand'], ix['stock'], ix['targetStock'], ix['prevImportAll']
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                D = fl(v[iD])
                if not (D > 0): continue
                r = (fl(v[iS]) - fl(v[iT])) / D
                if r != r: continue
                a, b = rows[v[0]]
                a.append((r, D))
                if fl(v[iI]) > 0: b.append((r, D))
    return rows


def main(argv):
    arms = [a.split('=', 1) for a in argv if '=' in a and not a.startswith('--')]
    if len(arms) != 3: print('ヌル腕 3 本（基線・N1・N2）が要る'); return 2
    ss = seeds(arms[0][1], 'sellr')
    for n, d in arms:
        if seeds(d, 'sellr') != ss or seeds(d, 'nationgood') != ss: print('種の不一致', n); return 2
    print('種:', ' '.join(map(str, ss)))
    res4 = {n: r4(d) for n, d in arms}
    years = sorted(res4[arms[0][0]])
    print('R4（月の版・国×財・10 種の和）  年  ' + '  '.join('%s[母集合 R4a R4b]' % n for n, _ in arms))
    for y in years:
        print('R4 %s  ' % y + '  '.join('%s %d %d %d' % (n, *res4[n][y]) for n, _ in arms))
    for k, nm in ((1, 'R4a'), (2, 'R4b')):
        mx = {n: max(res4[n][y][k] for y in years) for n, _ in arms}
        print('%s 年ごとの最大  ' % nm + '  '.join('%s %d' % (n, mx[n]) for n, _ in arms) + '  ヌル腕の最大 %d' % max(mx.values()))
    print('R5 (S-TS)/D  年  腕  (i)全[行 重み99 重みなし99 最大]  (ii)輸入した行[行 重み99 重みなし99 最大]')
    agg = {n: {'i_w': [], 'ii_w': [], 'i_u': [], 'ii_u': []} for n, _ in arms}
    for n, d in arms:
        res = r5(d)
        for y in sorted(res):
            a, b = res[y]
            iw, iu = wq(a, 0.99), uq([r for r, _ in a], 0.99)
            jw, ju = wq(b, 0.99), uq([r for r, _ in b], 0.99)
            agg[n]['i_w'].append((y, iw)); agg[n]['ii_w'].append((y, jw)); agg[n]['i_u'].append((y, iu)); agg[n]['ii_u'].append((y, ju))
            print('R5 %s %s  (i) %d %.4f %.4f %.3g  (ii) %d %.4f %.4f %.3g' % (
                y, n, len(a), iw, iu, max((r for r, _ in a), default=float('nan')), len(b), jw, ju, max((r for r, _ in b), default=float('nan'))))
    for key in ('ii_w', 'i_w', 'ii_u', 'i_u'):
        mx = {n: max(agg[n][key], key=lambda t: t[1]) for n, _ in arms}
        print('R5 %s 年ごとの最大  ' % key + '  '.join('%s %.4f(%s)' % (n, mx[n][1], mx[n][0]) for n, _ in arms) +
              '  ヌル腕の最大 %.4f' % max(t[1] for t in mx.values()))
        for y in ('1870', '1913'):
            vals = [dict(agg[n][key])[y] for n, _ in arms]
            print('R5 %s %s  ' % (key, y) + '  '.join('%s %.4f' % (n, v) for (n, _), v in zip(arms, vals)) + '  ヌル腕の最大 %.4f' % max(vals))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
