# (1360) c143hr 読む（コミットしない・門にしない・エンジンの関数を呼ばない）：
#   守られた法の後退を 1 件ずつ型に分ける（monlaw_s と lrlock_s だけで・全ての国）。
#     政体＝同じ四半期に政体が変わった／窓＝断絶の窓（ruptureWindowQuarters>0）／戦時＝atWarFlag=1／
#     取消＝同じ政体で、直前の同じ軸の変化が前進で、その間が 4 四半期以内／他＝それ以外。（判定はこの順の最初に当たった物）
#   時短は、取消の間の四半期の分布と、前進→後退の段の組も出す。
#   大国（scalars が四半期ごと）では、取消の前進の四半期の闘争度の前後（前の四半期・その四半期・次の四半期）と与党（sf）を集める。
#   使い方：python q4_classify.py <名> <ins> [<名> <ins> ...]
import sys, os, glob, re, statistics
AX = ['ShorterHours', 'ChildLabor', 'Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage']
UP = {'ShorterHours', 'ChildLabor'}
def seed(p): return int(re.search(r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
def rows(p):
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            yield ix, line.rstrip(b'\r\n').decode('utf-8').split('\t')
a = sys.argv[1:]
print('腕\t軸\t後退\t政体\t窓\t戦時\t取消\t他\t取消の間(1/2/3/4 四半期)\t取消の段の組\t大国の取消\t闘争度 前/当/次 の中央\t当の闘争度>0.5 の割合\t次÷当 の中央\t与党(0保/1自/2社/他)')
for name, ins in zip(a[0::2], a[1::2]):
    cls = {x: {'政体': 0, '窓': 0, '戦時': 0, '取消': 0, '他': 0} for x in AX}
    gap = {1: 0, 2: 0, 3: 0, 4: 0}; pairs = {}; mil = []; party = {}
    for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
        s = seed(p)
        lr = {}
        for ix, c in rows(os.path.join(ins, f'lrlock_s{s}.tsv')):
            lr[(c[ix['quarter']], c[ix['nationUid']])] = (c[ix['atWarFlag']] == '1', float(c[ix['ruptureWindowQuarters']]) > 0)
        sc = {}
        for ix, c in rows(os.path.join(ins, f'scalars_s{s}.tsv')):
            sc.setdefault(c[ix['nation']], {})[int(c[ix['quarter']])] = float(c[ix['militancyMean']])
        gp = {}
        sfp = os.path.join(ins, f'sf_s{s}.tsv')
        if os.path.exists(sfp):
            for ix, c in rows(sfp): gp[(c[ix['nation']], int(c[ix['quarter']]))] = c[ix['governingParty']]
        prev = {}; ppol = {}; last = {}
        for ix, c in rows(p):
            u = c[ix['nationUid']]; nm = c[ix['nation']]; q = int(c[ix['quarter']]); po = c[ix['polityCode']]
            w, rw = lr.get((c[ix['quarter']], u), (False, False))
            for x in AX:
                if x not in ix: continue
                v = int(float(c[ix[x]])); k = (u, x)
                if k in prev and v != prev[k]:
                    reg = (v > prev[k]) == (x in UP)
                    if reg:
                        lc = last.get(k)
                        if ppol.get(u) != po: t = '政体'
                        elif rw: t = '窓'
                        elif w: t = '戦時'
                        elif lc and not lc[1] and q - lc[0] <= 4 and lc[2] == po: t = '取消'
                        else: t = '他'
                        cls[x][t] += 1
                        if t == '取消' and x == 'ShorterHours':
                            gap[q - lc[0]] += 1
                            kk = f'{v}→{prev[k]}→{v}'; pairs[kk] = pairs.get(kk, 0) + 1
                            m = sc.get(nm, {})
                            q0 = lc[0]
                            if q0 in m and q0 - 1 in m and q0 + 1 in m and (q0 - 1) in m and len(m) > 200:
                                mil.append((m[q0 - 1], m[q0], m[q0 + 1]))
                                g = gp.get((nm, q), '?'); party[g] = party.get(g, 0) + 1
                    last[k] = (q, reg, po)
                prev[k] = v
            ppol[u] = po
    for x in AX:
        d = cls[x]; tot = sum(d.values())
        if tot == 0: continue
        extra = ['', '', '', '', '', '', '']
        if x == 'ShorterHours':
            extra[0] = '/'.join(str(gap[i]) for i in (1, 2, 3, 4))
            extra[1] = ' '.join(f'{k}:{n}' for k, n in sorted(pairs.items(), key=lambda t: -t[1]))
            extra[2] = str(len(mil))
            if mil:
                extra[3] = '/'.join(f'{statistics.median(z[i] for z in mil):.3f}' for i in range(3))
                extra[4] = f'{sum(1 for z in mil if z[1] > 0.5) / len(mil):.2f}'
                extra[5] = f'{statistics.median(z[2] / z[1] for z in mil if z[1] > 0):.3f}'
                extra[6] = ' '.join(f'{k}:{n}' for k, n in sorted(party.items()))
        print('\t'.join([name, x, str(tot)] + [str(d[t]) for t in ('政体', '窓', '戦時', '取消', '他')] + extra))
