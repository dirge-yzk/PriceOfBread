# (966) 生まれた国（開幕の後に nation_s に現れた区間）のうち、最初の行の実質 GDP が 0 の区間を数え、その区間に cyc ＜1e-3 の床の行があるかを並べる（(D) の型の確かめ）。
import sys, os
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f: yield ix, l.rstrip('\n').split('\t')
for arg in sys.argv[1:]:
    name, rest = arg.split('=', 1); d, a0, a1 = rest.rsplit(':', 2)
    tot = dict(born=0, z0=0, z0deep=0, pos0deep=0, zq=[])
    for s in range(int(a0), int(a1) + 1):
        ser = {}
        for ix, x in rd(os.path.join(d, f'nation_s{s}.tsv')):
            ser.setdefault(x[ix['nation']], []).append((int(x[ix['quarter']]), float(x[ix['realGdpPerYear']]), float(x[ix['populationK']])))
        q00 = min(v[0][0] for v in ser.values())
        segs = []
        for n, xs in ser.items():
            st = 0
            for i in range(1, len(xs) + 1):
                if i == len(xs) or xs[i][0] != xs[i - 1][0] + 1:
                    if xs[st][0] != q00: segs.append((n, xs[st:i]))
                    st = i
        deep = {}
        pp = os.path.join(d, f'pclamp_s{s}.tsv')
        if os.path.exists(pp):
            for ix, x in rd(pp):
                t = x[ix['time']]
                if t.startswith('q') and x[ix['kind']] == 'floor' and float(x[ix['cyclicalRatio']]) < 1e-3: deep.setdefault(x[ix['nation']], set()).add(int(t[1:]))
        for n, xs in segs:
            if xs[0][2] < 100: continue
            tot['born'] += 1; L = len(xs); dp = any(c < L for c in deep.get(n, ()))
            z = 0
            while z < L and xs[z][1] == 0: z += 1
            if z > 0: tot['z0'] += 1; tot['z0deep'] += dp; tot['zq'].append(z)
            elif dp: tot['pos0deep'] += 1
    zq = sorted(tot['zq'])
    print(name, f'生まれた区間（人口 100 千以上）{tot["born"]}', f'最初の行の実質 GDP が 0 {tot["z0"]}（0 の続きの中央 {zq[len(zq)//2] if zq else "-"}・最大 {zq[-1] if zq else "-"}）', f'そのうち cyc＜1e-3 {tot["z0deep"]}', f'最初が正で cyc＜1e-3 {tot["pos0deep"]}', sep='\t')
