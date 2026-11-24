# (1344) c141lr：守られた法 7 軸の後退の件数を monlaw_s*.tsv から数える（読むだけ・エンジンの関数を呼ばない）。
# 後退の向き：ShorterHours と ChildLabor は段が上がる側・他の 5 軸は段が下がる側（Government.Regressive と同じ向きを表から独立に書く）。
# lrlock_s（(1344) の計器）があれば、後退を〈旧の規則なら塞がれた〉（断裂の窓が前の四半期と今の四半期とも 0・時短と児童労働は今の四半期が平時）と〈それ以外〉に分ける。
# 児童労働の合法度：平時・法が合法（childLaborAllowedFlag 1）・合法度 < 0.999 の国×四半期を数え、そのうち前の四半期から合法度が上がった物を数える（旧の規則なら上がらない）。
# 使い方：python lr_count.py <ins フォルダ> [--years]
import sys, glob, os, re, statistics
AX_DOWN = ['Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage']
AX_UP = ['ShorterHours', 'ChildLabor']
SIX = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ']
NL = b'\r\n'

def rows(p):
    with open(p, 'rb') as fh:
        hdr = fh.readline().rstrip(NL).decode('utf-8').split('\t')
        ix = {h: i for i, h in enumerate(hdr)}
        for line in fh:
            yield ix, line.rstrip(NL).decode('utf-8').split('\t')

def lrlock(d, s):
    p = os.path.join(d, f'lrlock_s{s}.tsv')
    if not os.path.exists(p): return None
    m = {}
    for ix, c in rows(p):
        m[(int(c[ix['quarter']]), c[ix['nationUid']])] = (int(c[ix['ruptureWindowQuarters']]), int(c[ix['atWarFlag']]),
                                                           int(c[ix['childLaborAllowedFlag']]), float(c[ix['childLaborLegalShare']]))
    return m

def run(d):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'monlaw_s*.tsv')), key=lambda p: int(re.search(r'_s(\d+)', p).group(1))):
        s = int(re.search(r'_s(\d+)', f).group(1))
        lk = lrlock(d, s); oldblk = 0; oldblk6 = 0; clstuck = 0; clrise = 0
        prev = {}; prevq = {}
        per_ax = {a: 0 for a in AX_DOWN + AX_UP}; six = 0; six_ax = {a: 0 for a in AX_DOWN + AX_UP}; ev = []
        for ix, c in rows(f):
            uid = c[ix['nationUid']]; nm = c[ix['nation']]; q = int(c[ix['quarter']])
            vals = {a: int(float(c[ix[a]])) for a in AX_DOWN + AX_UP}
            pq = prevq.get(uid)
            cur = lk.get((q, uid)) if lk is not None else None
            prv = lk.get((pq, uid)) if lk is not None and pq is not None else None
            if cur is not None:
                rw, aw, ca, cl = cur
                if ca == 1 and aw == 0 and cl < 0.999:
                    clstuck += 1
                    if prv is not None and cl > prv[3] + 1e-12: clrise += 1
            if uid in prev:
                for a in AX_DOWN + AX_UP:
                    p = prev[uid][a]; v = vals[a]
                    reg = (v < p) if a in AX_DOWN else (v > p)
                    if not reg: continue
                    per_ax[a] += 1
                    if nm in SIX:
                        six += 1; six_ax[a] += 1; ev.append((c[ix['year']], nm, a, p, v))
                    if cur is not None and prv is not None and cur[0] == 0 and prv[0] == 0 and (a not in AX_UP or cur[1] == 0):
                        oldblk += 1
                        if nm in SIX: oldblk6 += 1
            prev[uid] = vals; prevq[uid] = q
        out[s] = (per_ax, six, six_ax, ev, (oldblk, oldblk6, clstuck, clrise) if lk is not None else None)
    return out

if __name__ == '__main__':
    d = sys.argv[1]; r = run(d)
    print('seed\tsixTotal\t' + '\t'.join('all:' + a for a in AX_DOWN + AX_UP) + '\t' + '\t'.join('six:' + a for a in AX_DOWN + AX_UP))
    for s, (pa, six, sa, ev, ob) in r.items():
        print(f'{s}\t{six}\t' + '\t'.join(str(pa[a]) for a in AX_DOWN + AX_UP) + '\t' + '\t'.join(str(sa[a]) for a in AX_DOWN + AX_UP))
    print('median_sixTotal\t' + (str(statistics.median([v[1] for v in r.values()])) if r else 'NA'))
    if any(v[4] is not None for v in r.values()):
        print('seed\toldRuleBlockedAll\toldRuleBlockedSix\tchildLegalBelowLawPeaceQ\tchildLegalRisePeaceQ')
        for s, v in r.items():
            if v[4] is not None: print(f'{s}\t' + '\t'.join(str(x) for x in v[4]))
    if '--years' in sys.argv:
        for s, v in r.items():
            for e in v[3]: print('EV', s, *e, sep='\t')
