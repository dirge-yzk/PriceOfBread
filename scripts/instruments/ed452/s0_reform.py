import csv, sys, statistics as st
# 段0：国家の教育の買い付けの現状（m451_ins の reform_s・class_s）。読むだけ。
base = 'D:/PoB_work/keep_451/m451_ins/'
NAT = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
P = {'P1': (0, 67), 'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667)}
def per(q):
    for k, (a, b) in P.items():
        if a <= q <= b: return k
    return None
rows = {}  # (nat, per, seed) -> lists
for s in range(1, 11):
    with open(base + f'reform_s{s}.tsv', encoding='utf-8') as f:
        r = csv.DictReader(f, delimiter='\t')
        for d in r:
            n = d['nation']
            if n not in NAT: continue
            q = int(d['quarter']); p = per(q)
            if p is None: continue
            rows.setdefault((n, p, s), []).append(d)
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
print('nation period | law(med) spendShare parishFund buyPerMonth literacy | share of quarters with buy>0')
for n in NAT:
    for p in P:
        agg = {k: [] for k in ('law', 'sp', 'pf', 'buy', 'lit', 'pos')}
        for s in range(1, 11):
            for d in rows.get((n, p, s), []):
                agg['law'].append(float(d['educationLawCode'])); agg['sp'].append(float(d['educationSpendingShare']))
                agg['pf'].append(float(d['parishFundingShare'])); agg['buy'].append(float(d['educationBuyPerMonth']))
                agg['lit'].append(float(d['literacyShare'])); agg['pos'].append(1.0 if float(d['educationBuyPerMonth']) > 0 else 0.0)
        print(f"{n:6s} {p} | law {med(agg['law']):.0f} sp {med(agg['sp']):.2f} pf {med(agg['pf']):.2f} buy {med(agg['buy']):9.2f} lit {med(agg['lit']):.2f} | buy>0 {sum(agg['pos'])/max(1,len(agg['pos'])):.2f}")
