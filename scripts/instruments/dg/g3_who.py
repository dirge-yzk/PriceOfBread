import sys, collections
sys.path.insert(0, 'D:/PoB_work/wt_g3/scripts/instruments/dg')
from an_dg2 import Arm, fl
B = Arm('D:/PoB_work/g3/out_g3B_10/ins'); A = Arm('D:/PoB_work/g3/out_g3idins_10/ins')
cb = collections.Counter(); ca = collections.Counter()
for sd in B.seeds:
    for n in B.steam_nations(sd, 1784): cb[n] += 1
    for n in A.steam_nations(sd, 1784): ca[n] += 1
print('B 1783-84 解禁の国（種の数/10）:', len(cb))
print(' '.join(f'{n}{c}' for n, c in cb.most_common()))
print('B で 8 種以上:', [n for n, c in cb.items() if c >= 8])
print('A 1783-84 の国:', len(ca), ' A で解禁・B で 0 種:', len([n for n in ca if cb[n] == 0]))
print('B で 1783-84 に解禁した国 5 種以上の国の A の数', sum(1 for n,c in cb.items() if c>=5), [ (n, ca[n]) for n,c in cb.items() if c>=5][:40])
# 解禁の四半期と needTerm/techTerm/transmit in B for those
import statistics
rows=[]
for sd in B.seeds:
    for n in B.steam_nations(sd,1784):
        r=B.E[(sd,n,'蒸気機関')]
        rows.append((fl(r['needTerm']),fl(r['transmitTerm']),fl(r['techTerm']),int(r['quarter'])))
print('B 1783-84 の E 行 n=',len(rows),'need 中央値',statistics.median(r[0] for r in rows),'transmit 中央値',statistics.median(r[1] for r in rows),'tech',statistics.median(r[2] for r in rows),'Q0',sum(1 for r in rows if r[3]==0))
print('transmit>0 の割合', sum(1 for r in rows if r[1]>0)/len(rows), 'need>0', sum(1 for r in rows if r[0]>0)/len(rows))
