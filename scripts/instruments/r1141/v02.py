# (1141) V02〈宗主と属国は交戦しない〉の入口ごとの近似：dep_s の張りの行から従属の表を組み立て（離反・国の消滅は行が無い＝上限）、
# wars_s の開戦の四半期にその組が宗主と属国だったかを、入口の札（entryCode）ごとに数える。張りと同じ四半期の開戦は〈同じ四半期〉として別に数える。
# 使い方：python -X utf8 -I v02.py <生のフォルダ> <ラベル> <種の始め> <種の終わり> <出力>
import sys, os
from collections import Counter
D, LAB, S0, S1, OUT = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
out = open(OUT, 'w', encoding='utf-8', newline='\n')
out.write(f'# (1141) v02.py arm={LAB} dir={D} seeds {S0}..{S1}\n')
tot = Counter(); same = Counter(); old = Counter(); allw = Counter(); ex = []
for s in range(S0, S1 + 1):
    pd, pw = os.path.join(D, f'dep_s{s}.tsv'), os.path.join(D, f'wars_s{s}.tsv')
    if not (os.path.exists(pd) and os.path.exists(pw)): continue
    ev = []
    with open(pd, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if r[0] != '1': continue
            ev.append((int(r[ix['month']]) // 3, r[ix['suzerain']], r[ix['dependent']], r[ix['cutSuzerain']]))
    with open(pw, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        W = [l.rstrip('\n').split('\t') for l in f if l.strip()]
    for r in W:
        q = int(r[ix['q']]); a, b, code = r[ix['sideA']], r[ix['sideB']], r[ix['entryCode']]
        allw[code] += 1
        L = {}; setq = {}
        for qq, su, de, cut in ev:
            if qq > q: break
            if cut: L.pop(su, None)
            L[de] = su; setq[de] = qq
        hit = None
        if L.get(a) == b: hit = a
        elif L.get(b) == a: hit = b
        if hit is None: continue
        tot[code] += 1
        age = q - setq[hit]
        if age == 0: same[code] += 1
        elif age >= 8: old[code] += 1
        if len(ex) < 12 and code == 'ally': ex.append(f'種{s} q{q} {a}×{b}（属国 {hit}・張って {age} 四半期）')
out.write('入口\t戦争の数\t従属の組（近似）\tうち同じ四半期に張った\tうち張って 8 四半期以上\t割合\n')
for k in sorted(allw):
    out.write(f'{k}\t{allw[k]}\t{tot[k]}\t{same[k]}\t{old[k]}\t{tot[k]/allw[k]:.3%}\n')
out.write('例（ally）：' + '・'.join(ex) + '\n')
