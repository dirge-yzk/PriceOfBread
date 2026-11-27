# (1416) c151fl 同じ盤面の新÷旧（記録）：腕と基線の fsx_s・fiscal_s を種ごとに並べる（生を読むだけ・列名で引く）。
# 使い方: python newold_fl.py <腕の ins> <基線の ins> [種 1-10]
import sys, statistics as st
A, B = sys.argv[1], sys.argv[2]
a, b = (sys.argv[3] if len(sys.argv) > 3 else '1-10').split('-')
def tab(p, filt):
    f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in f:
        r = l.rstrip('\n').split('\t')
        if filt(ix, r): yield ix, r
def fsx(D, s):
    done = fl = 0; debt = {1850: 0, 1900: 0, 1949: 0}; uk = {}
    for ix, r in tab(f'{D}/fsx_s{s}.tsv', lambda ix, r: r[ix['time']].startswith('q')):
        done += float(r[ix['repayDoneSum']]); fl += float(r[ix['repayFloorSum']])
        y = int(r[ix['year']])
        if y in debt: debt[y] += float(r[ix['govDebtEnd']])
        if r[ix['nation']] == 'イギリス' and y in debt: uk[y] = float(r[ix['govDebtEnd']])
    return done, fl, debt, uk
def ukint(D, s):
    v = [float(r[ix['interestToRevenueShareNow']]) for ix, r in tab(f'{D}/fiscal_s{s}.tsv', lambda ix, r: r[ix['nation']] == 'イギリス' and int(r[ix['year']]) >= 1850)]
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
print('seed\trepayDone新÷旧\t世界の国債1850新÷旧\t1900\t1949\t英の国債1900新÷旧\t1949\t英の利払い÷歳入の中央(1850〜)旧\t新')
def q(x, y): return f'{x / y:.3f}' if y else 'nan'
for s in range(int(a), int(b) + 1):
    dA, fA, gA, uA = fsx(A, s); dB, fB, gB, uB = fsx(B, s)
    print('\t'.join([str(s), q(dA, dB), q(gA[1850], gB[1850]), q(gA[1900], gB[1900]), q(gA[1949], gB[1949]), q(uA.get(1900, 0), uB.get(1900, 0)), q(uA.get(1949, 0), uB.get(1949, 0)), f'{ukint(B, s):.4f}', f'{ukint(A, s):.4f}']))
