# (1145) c112hy 壊さない門の材料：warband_s の開戦の数（start_* の和・内戦を除く）と講和の置き直しの数（peaceResets）を 10 種で合計し、比（開戦÷講和）を出す。
# 使い方：python scripts/instruments/c112hy/warpeace.py <生のフォルダ> <ラベル> [種の始め 1] [種の終わり 10]
import sys, os
D, LAB = sys.argv[1], sys.argv[2]
S0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
S1 = int(sys.argv[4]) if len(sys.argv) > 4 else 10
st = pe = civ = 0; per = []
for s in range(S0, S1 + 1):
    p = os.path.join(D, f'warband_s{s}.tsv')
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
    ix = {n: i for i, n in enumerate(h)}
    sc = [i for n, i in ix.items() if n.startswith('start_') and n != 'start_civil']
    a = b = 0
    for l in f:
        r = l.rstrip('\n').split('\t')
        a += sum(int(r[i]) for i in sc); b += int(r[ix['peaceResets']]); civ += int(r[ix['start_civil']])
    st += a; pe += b; per.append((s, a, b))
print(f'{LAB}\tstarts={st}\tpeace={pe}\tratio={st/max(pe,1):.6f}\tcivil={civ}\tper_seed=' + ' '.join(f'{s}:{a}/{b}' for s, a, b in per))
