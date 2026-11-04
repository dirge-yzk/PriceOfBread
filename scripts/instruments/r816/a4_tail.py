# (816) 旗 1 の腕 B の本走の rel の尾を、(a) 全て (b) 1786〜1790 を除く (c) 1 州の票の割合 usedMaxShare ≦0.1 の注入だけ、で並べる。同じ盤面の等重み relEq と旗 0 の盤面の rel も並べる。
F1 = 'D:/PoB_work/keep_813/rej/c12svB_10/ins'; F0 = 'D:/PoB_work/keep_813/base_ins'
def rd(p):
    with open(p, 'rb') as f: t = f.read().decode('utf-8').splitlines()
    h = t[0].split('\t'); return [dict(zip(h, l.split('\t'))) for l in t[1:]]
def q(v, p): v = sorted(v); return v[min(len(v)-1, int(p*len(v)))]
def row(name, v):
    return f'{name}\t{len(v)}\t{q(v,0.01):.3f}\t{q(v,0.05):.3f}\t{q(v,0.5):.3f}\t{q(v,0.95):.3f}\t{q(v,0.99):.3f}\t{min(v):.3f}\t{max(v):.3f}'
A=[];B=[];C=[];E=[];Z=[];ms=[]
for s in range(1, 11):
    for r in rd(f'{F1}/silverwt_s{s}.tsv'):
        if r['frozen'] != '0': continue
        u = float(r['usedRelIndex']); y = int(r['year']); m = float(r['usedMaxShare'])
        A.append(u); E.append(float(r['relEqualIndex'])); ms.append(m)
        if not (1786 <= y <= 1790): B.append(u)
        if m <= 0.1: C.append(u)
    for r in rd(f'{F0}/silverwt_s{s}.tsv'):
        if r['frozen'] == '0': Z.append(float(r['usedRelIndex']))
out = ['set\tn\tp1\tp5\tp50\tp95\tp99\tmin\tmax', row('flag1_used_all', A), row('flag1_used_excl1786-1790', B), row('flag1_used_maxShare<=0.1', C), row('flag1_board_equal', E), row('flag0_used', Z)]
out.append(f'flag1_usedMaxShare_quantiles\tp50 {q(ms,0.5):.3f}\tp90 {q(ms,0.9):.3f}\tp99 {q(ms,0.99):.3f}\tshare>0.2 {sum(1 for m in ms if m>0.2)/len(ms):.3f}\tshare>0.1 {sum(1 for m in ms if m>0.1)/len(ms):.3f}')
open('D:/PoB_work/r816_work/out/a4_tail.tsv', 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
