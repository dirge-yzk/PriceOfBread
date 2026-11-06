# (900) c77lg：monlaw_s と nation_s（atWarMonths・civilWarMonths）を国×四半期で結び、
#  労働時間の後退（段が上がる）と徴兵法の 2 段下げが戦争の四半期か戦争の終わりの四半期かを数える（読むだけ）。
# 使い方: python -X utf8 s19_war.py <生のフォルダ> [種の数]
import sys, os, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
out = collections.Counter()
for s in range(1, ns + 1):
    war = {}
    f = open(os.path.join(d, f'nation_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\n').split('\t')
        war[(r[ix['nationUid']], int(r[0]))] = (float(r[ix['atWarMonths']]), float(r[ix['civilWarMonths']]))
    f = open(os.path.join(d, f'monlaw_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    prev = {}
    for raw in f:
        r = raw.decode().rstrip('\n').split('\t')
        u = r[ix['nationUid']]; q = int(r[0]); pol = r[ix['polityCode']]
        sh = int(r[ix['ShorterHours']]); cl = int(r[ix['ConscriptionLaw']])
        pv = prev.get(u)
        if pv and pv[0] == q - 1:
            wc = war.get((u, q)); wp = war.get((u, q - 1))
            tag = 'nation_s に行なし' if wc is None or wp is None else ('戦争（今か前の四半期）' if (wc[0] > 0 or wp[0] > 0) else ('内戦のみ' if (wc[1] > 0 or wp[1] > 0) else '平時'))
            if sh > pv[2] and pv[1] == pol: out[('労働時間の後退・政体同じ', tag)] += 1
            if cl - pv[3] <= -2 and pv[1] == pol: out[('徴兵法の2段下げ・政体同じ', tag + ('・今は平時' if wc and wc[0] == 0 else ''))] += 1
            if cl - pv[3] >= 2: out[('徴兵法の2段上げ', tag)] += 1
        prev[u] = (q, pol, sh, cl)
for k, n in sorted(out.items()): print(k, n)
