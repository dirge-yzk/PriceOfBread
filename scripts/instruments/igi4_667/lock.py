import csv, collections
COLS = ['educationLaw','suffrage','censorship','bureaucracy','socialSecurityLawCode','antitrustLawCode','womenRightsCode','childLaborLegal','workHoursValue']
def run(d, S):
    cnt = collections.Counter(); tot = 0
    for s in S:
        f = open(f'{d}/scalars_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        idx = {c: h.index(c) for c in COLS}
        rows = []
        for x in r:
            rows.append(x)
        mx = {c: max(float(x[idx[c]]) for x in rows) for c in COLS}
        for x in rows:
            tot += 1
            for c in COLS:
                if float(x[idx[c]]) == mx[c] and mx[c] > min(float(y[idx[c]]) for y in rows[:1]): cnt[c] += 1
        f.close()
    return cnt, tot
res = {}
for lab, d, a in [('m655','D:/PoB_work/keep_655/m655_ins',1),('N1','D:/PoB_work/keep_655/n1',1),('N2','D:/PoB_work/keep_655/n2',1),('B','D:/PoB_work/igi4_667/out_B_10/ins',1),('A21','D:/PoB_work/igi4_667/out_A21_10_s21/ins',21),('B21','D:/PoB_work/igi4_667/out_B21_10_s21/ins',21)]:
    res[lab] = run(d, range(a, a + 10))
for lab,(c,t) in res.items(): print(lab, t, ' '.join(f'{k[:8]}={c[k]/t:.4f}' for k in COLS))
for arm, refs in [('B',['m655','N1','N2']),('B21',['A21'])]:
    worst = 0
    for k in COLS:
        mxref = max(res[r][0][k]/res[r][1] for r in refs)
        v = res[arm][0][k]/res[arm][1]
        worst = max(worst, v/mxref if mxref > 0 else 0)
    print(arm, '最大の段に張り付く割合 腕÷基準の最大(列ごとの最悪)=%.2f（2 以上で該当）' % worst)
