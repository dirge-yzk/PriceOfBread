# (900) c77lg：戦時配給法（WartimeRationing＝1）の行と、その唯一の効き（封鎖の厭戦×RationingBlockadeRelief）の入力 blockadeRiskShare＞0 の行を数える（monlaw_s・読むだけ）。
# 使い方: python -X utf8 s19_ration.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]; c = collections.Counter()
for s in range(1, 11):
    f = open(os.path.join(d, f'monlaw_s{s}.tsv'), 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\r\n').split('\t'); on = r[ix['WartimeRationing']] == '1'
        try: br = float(r[ix['blockadeRiskShare']])
        except ValueError: br = float('nan')
        c['行'] += 1; c['配給あり'] += on; c['封鎖の危険>0'] += (br > 0); c['配給あり×封鎖の危険>0'] += (on and br > 0)
print(dict(c))
