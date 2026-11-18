# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
"""(1186) c118nc 紙の台本：信任帝政の床（Politics.cs:268-272）が立つ四半期を基線の生で数える。
条件 A＝polityCode==3（立憲君主）・crown!=0・dynMandate>=0.6（RedefinedMandateMandateMin）。
国民意識は scalars に列が無い＝secession_s の州の consciousnessMeanIndex を popK で加重平均した近似
（首都 region 0 と兵を除く＝近似と名乗る）。条件 A かつ近似 NC>=0.5 を〈床が立つ〉と数える。
使い方: python floor_count.py <ins の生のフォルダ> [種の数]"""
import sys, os, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
tot = collections.Counter()
for s in range(1, ns + 1):
    sc = os.path.join(d, f'scalars_s{s}.tsv'); se = os.path.join(d, f'secession_s{s}.tsv')
    if not os.path.exists(sc): print('missing', sc); continue
    nc = collections.defaultdict(lambda: [0.0, 0.0])
    if os.path.exists(se):
        with open(se, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); iq, inn, ip, ic = h.index('quarter'), h.index('nation'), h.index('popK'), h.index('consciousnessMeanIndex')
            for line in f:
                a = line.rstrip('\n').split('\t')
                try: p = float(a[ip]); v = float(a[ic])
                except ValueError: continue
                if v < 0 or not p > 0: continue
                k = (a[iq], a[inn]); nc[k][0] += p * v; nc[k][1] += p
    c = collections.Counter()
    with open(sc, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, inn, ipo, icr, idm = (h.index(x) for x in ('quarter', 'nation', 'polityCode', 'crown', 'dynMandate'))
        for line in f:
            a = line.rstrip('\n').split('\t'); c['rows'] += 1
            try: dm = float(a[idm])
            except ValueError: continue
            if a[ipo] == '3': c['constMon'] += 1
            if a[ipo] == '3' and a[icr] != '0': c['constMonCrown'] += 1
            if a[ipo] == '3' and a[icr] != '0' and dm >= 0.6:
                c['condA'] += 1
                k = (a[iq], a[inn])
                if k in nc and nc[k][1] > 0:
                    c['condA_ncKnown'] += 1
                    if nc[k][0] / nc[k][1] >= 0.5: c['floor'] += 1
    print(f's{s}', dict(c)); tot.update(c)
print('total', dict(tot))
