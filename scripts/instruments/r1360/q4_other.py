# (1360) 読む：q4_classify の〈他〉（同じ政体・窓なし・平時で、直前の変化が 4 四半期以内の前進でない後退）の内訳。
#   直前の同じ軸の変化の向き（前進/後退/無し）×間（≤4・5〜40・>40 四半期）×段の組。時短だけ。使い方：python q4_other.py <名> <ins> ...
import sys, os, glob, re
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
def rows(p):
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh: yield ix, line.rstrip(b'\r\n').decode('utf-8').split('\t')
a = sys.argv[1:]
for name, ins in zip(a[0::2], a[1::2]):
    acc = {}
    for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
        s = seed(p); lr = {}
        for ix, c in rows(os.path.join(ins, f'lrlock_s{s}.tsv')):
            lr[(c[ix['quarter']], c[ix['nationUid']])] = (c[ix['atWarFlag']] == '1', float(c[ix['ruptureWindowQuarters']]) > 0)
        prev = {}; ppol = {}; last = {}
        for ix, c in rows(p):
            u = c[ix['nationUid']]; q = int(c[ix['quarter']]); po = c[ix['polityCode']]; v = int(float(c[ix['ShorterHours']]))
            w, rw = lr.get((c[ix['quarter']], u), (False, False))
            if u in prev and v != prev[u]:
                reg = v > prev[u]; lc = last.get(u)
                if reg and ppol.get(u) == po and not rw and not w and not (lc and not lc[1] and q - lc[0] <= 4 and lc[2] == po):
                    if lc is None: kd = '無し'
                    else:
                        g = q - lc[0]; kd = ('前進' if not lc[1] else '後退') + ('≤4' if g <= 4 else '5-40' if g <= 40 else '>40') + ('' if lc[2] == po else '(政体違い)')
                    key = (kd, f'{prev[u]}→{v}'); acc[key] = acc.get(key, 0) + 1
                last[u] = (q, reg, po)
            prev[u] = v; ppol[u] = po
    tot = sum(acc.values())
    print(name, tot, ' / '.join(f'{k[0]} {k[1]}:{n}' for k, n in sorted(acc.items(), key=lambda t: -t[1])[:12]))
