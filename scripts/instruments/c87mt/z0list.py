# 生まれた国の区間で最初の行の実質 GDP が 0 の物を列挙（r966_birth0 と同じ区間の取り方）
import sys, os
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f: yield ix, l.rstrip('\n').split('\t')
d, a0, a1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
for s in range(a0, a1 + 1):
    ser = {}
    for ix, x in rd(os.path.join(d, f'nation_s{s}.tsv')):
        ser.setdefault(x[ix['nation']], []).append((int(x[ix['quarter']]), float(x[ix['realGdpPerYear']]), float(x[ix['populationK']]), x[ix.get('nominalGdpPerYear', ix['realGdpPerYear'])]))
    q00 = min(v[0][0] for v in ser.values())
    for n, xs in ser.items():
        st = 0
        for i in range(1, len(xs) + 1):
            if i == len(xs) or xs[i][0] != xs[i - 1][0] + 1:
                seg = xs[st:i]
                if seg[0][0] != q00 and seg[0][1] == 0:
                    z = 0
                    while z < len(seg) and seg[z][1] == 0: z += 1
                    print(s, n, 'q0=%d' % seg[0][0], 'zeros=%d' % z, 'popK=%.0f' % seg[0][2], 'len=%d' % len(seg), sep='\t')
                st = i
