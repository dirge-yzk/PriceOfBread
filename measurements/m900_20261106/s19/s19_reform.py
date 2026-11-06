# (900) c77lg：reform_s の内戦の講和の譲歩（concedeSpendIndex>0）と、凍結中に止められた数（blockedWhileFrozenCount>0）の行を数える（読むだけ）。
# 使い方: python -X utf8 s19_reform.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]; c = collections.Counter(); seeds = collections.Counter()
for s in range(1, 11):
    f = open(os.path.join(d, f'reform_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\r\n').split('\t'); c['行'] += 1
        def v(k):
            try: return float(r[ix[k]])
            except ValueError: return float('nan')
        if v('concedeSpendIndex') > 0: c['譲歩の支出>0'] += 1; seeds[s] += 1
        if v('blockedWhileFrozenCount') > 0: c['凍結中に止めた>0'] += 1
print(dict(c), '譲歩の種ごと', dict(sorted(seeds.items())))
