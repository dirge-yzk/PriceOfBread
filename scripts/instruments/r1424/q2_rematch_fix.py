# (1425) 合流が写した：c152rw の反証役 v の数え直し（wars_s の q は始めの四半期）。使い方: python q2_rematch_fix.py <生> <種の範囲 1-10>
# 反証：wars_s の q は始めの四半期（InstrumentsWar.cs:91 WarNoteStart）＝始め=q*3・終わり=始め+月数 で数え直す
import sys, csv, collections
d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-'))
T = collections.defaultdict(collections.Counter)
for s in range(s0, s1+1):
    r = csv.reader(open(f'{d}/wars_s{s}.tsv', encoding='utf-8'), delimiter='\t'); h = next(r); ix = {c:i for i,c in enumerate(h)}
    ws = []
    for x in r:
        st = int(x[ix['q']])*3; ws.append((frozenset((x[ix['sideA']], x[ix['sideB']])), st, st+int(x[ix['months']]), x[ix['verdictCode']]))
    for p, st, en, v in ws:
        if v == '-1': continue
        nxt = [w[1]-en for w in ws if w[0]==p and w[1] >= en-2 and w[1] > st]
        g = min(nxt) if nxt else None
        for k in ('ALL', 'blank' if v=='0' else 'won'):
            T[k]['ended'] += 1; T[k]['re12'] += (g is not None and g <= 12); T[k]['re60'] += (g is not None and g <= 60)
for k,t in sorted(T.items()): print(d, k, t['ended'], t['re12'], t['re60'], '%.3f %.3f' % (t['re12']/t['ended'], t['re60']/t['ended']))
