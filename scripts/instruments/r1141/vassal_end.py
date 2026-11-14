# (1141) 終局の従属（dep_s の張りの行から組み立てた近似＝離反と国の消滅は行が無い＝上限）と、warentry_s の最後の四半期のランクを突き合わせる（読むだけ）
import sys, os
from collections import Counter
D, LAB, S0, S1, OUT = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
out = open(OUT, 'w', encoding='utf-8', newline='\n'); out.write(f'# (1141) vassal_end.py arm={LAB} dir={D} seeds {S0}..{S1}\n種\t終局の国\t属国（近似）\t割合\t列強のうち属国\t列強の宗主を持つ属国の宗主のランク\t最終行の張り\n')
for s in range(S0, S1 + 1):
    pd, pe = os.path.join(D, f'dep_s{s}.tsv'), os.path.join(D, f'warentry_s{s}.tsv')
    if not (os.path.exists(pd) and os.path.exists(pe)): continue
    L = {}; fin = ''
    for l in open(pd, encoding='utf-8'):
        r = l.rstrip('\n').split('\t')
        if r[0] == '2': fin = r[15]
        if r[0] != '1': continue
        if r[12]: L.pop(r[5], None)
        L[r[6]] = r[5]
    rows = [l.rstrip('\n').split('\t') for l in open(pe, encoding='utf-8')][1:]
    lastq = max(int(r[0]) for r in rows)
    rank = {r[2]: r[3] for r in rows if int(r[0]) == lastq}
    live = {n for n in rank}
    deps = [d for d in L if d in live and L[d] in live]
    gp = [d for d in deps if rank.get(d) == 'Great']
    sr = Counter(rank.get(L[d], '?') for d in gp)
    out.write(f'{s}\t{len(live)}\t{len(deps)}\t{len(deps)/len(live):.2f}\t{len(gp)}\t{dict(sr)}\t{fin}\n')
