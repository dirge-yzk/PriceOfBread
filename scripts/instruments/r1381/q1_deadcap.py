# (1381) 読む線：州の労働の POP ごとに、能力（capacity÷productivity＝頭数×boost の近似）のうち投入の充足が 0.05 未満か産出 0 の生産者に割り当たった割合（〈死んだ能力〉）を年ごとに出す。使い方 python q1_deadcap.py <ins> <種> <州> <出力>
import sys, collections
d, sd, reg, out = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4]
f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
T = collections.defaultdict(float); D = collections.defaultdict(float); G = collections.defaultdict(lambda: collections.defaultdict(float))
with open('%s/prodcap_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t')
        if c[I['region']] != reg or c[I['isRgo']] != '0' or c[I['good']] in ('労働力', '農業労働力', '高度労働力'): continue
        prod = f(c[I['productivity']])
        if prod <= 0: continue
        k = (int(c[I['year']]), c[I['laborPopIndex']]); x = f(c[I['capacity']]) / prod
        T[k] += x
        if f(c[I['lastInputFill']]) < 0.05 or f(c[I['output']]) <= 1e-9:
            D[k] += x; G[k][c[I['good']] + '/' + c[I['recipe']]] += x
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\tlaborPop\tcapOverProd\tdeadCapOverProd\tdeadShare\ttopDead\n')
    for k in sorted(T):
        top = sorted(G[k].items(), key=lambda t: -t[1])[:4]
        fo.write('%d\t%s\t%.1f\t%.1f\t%.3f\t%s\n' % (k[0], k[1], T[k], D[k], D[k] / T[k] if T[k] > 0 else 0, ','.join('%s:%.1f' % t for t in top)))
