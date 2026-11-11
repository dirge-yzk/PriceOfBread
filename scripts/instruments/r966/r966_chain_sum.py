# (966) 鎖の表（<頭>_chain.tsv）の中央：各量の f0 の値÷f0−40 の値・f0+20÷f0（F で 1 本にまとめた組）。分かれた国とそれ以外を分ける。
import sys, statistics as st
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
for p in sys.argv[1:]:
    L = [l.rstrip('\n').split('\t') for l in open(p, encoding='utf-8')]; h = L[0]; ix = {k: i for i, k in enumerate(h)}
    for grp in ('分', '-'):
        R = [r for r in L[1:] if r[3] == grp]
        if not R: continue
        out = [p.split('_')[0], '分かれた国' if grp == '分' else 'それ以外', f'F {len(R)}']
        for k in ['fci', 'invSav', 'paid', 'sav', 'inMkt', 'P', 'gdp']:
            a = [f(r[ix[f'{k}@+0']]) / f(r[ix[f'{k}@-40']]) for r in R if f(r[ix[f'{k}@-40']]) > 0 and f(r[ix[f'{k}@+0']]) == f(r[ix[f'{k}@+0']])]
            b = [f(r[ix[f'{k}@+20']]) / f(r[ix[f'{k}@+0']]) for r in R if f(r[ix[f'{k}@+0']]) > 0 and f(r[ix[f'{k}@+20']]) == f(r[ix[f'{k}@+20']])]
            out.append(f'{k} 0/−40 {st.median(a):.3g}（{len(a)}）+20/0 {st.median(b):.3g}' if a and b else f'{k} -')
        # 純の流出÷貯蓄（f0−8 の 4 四半期の平均）
        nc = [f(r[ix['netCap@-8']]) / f(r[ix['sav@-8']]) for r in R if f(r[ix['sav@-8']]) > 0]
        ni = [f(r[ix['netImp@-8']]) / f(r[ix['sav@-8']]) for r in R if f(r[ix['sav@-8']]) > 0]
        pd = [f(r[ix['paid@-8']]) / f(r[ix['sav@-8']]) for r in R if f(r[ix['sav@-8']]) > 0 and f(r[ix['paid@-8']]) == f(r[ix['paid@-8']])]
        if nc: out.append(f'f0−8：純資本÷貯蓄 中央 {st.median(nc):.3g}・純輸入÷貯蓄 中央 {st.median(ni):.3g}・配当の払い÷貯蓄 中央 {st.median(pd):.3g}（{len(nc)}）')
        print(*out, sep='\t')
