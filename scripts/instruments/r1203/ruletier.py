# (1203) 問い② 兵の席は州の男×RuleDelivery（Politics.cs:1096＝Rule[0]×(…AdminFill)＋Rule[1]×(1−PrivilegeTaxDiscount)）に比例する。
#  rule_s の ruleStateShare・ruleLocalShare を州の格ごとに人口で重みづけて並べる（RuleDelivery の上界の目安＝AdminFill と割引を掛けない）
# 使い方: python -X utf8 -I ruletier.py <生> <種0> <種1> <国名,…> <年の下限> <出力>
import sys, csv, collections
raw, s0, s1, nats, y0, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4].split(','), int(sys.argv[5]), sys.argv[6]
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return None
A = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
for s in range(s0, s1 + 1):
    with open(f'{raw}/rule_s{s}.tsv', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] not in nats or int(r['year']) < y0: continue
            p, st, lo = num(r['popK']), num(r['ruleStateShare']), num(r['ruleLocalShare'])
            if None in (p, st, lo) or p <= 0: continue
            a = A[(r['nation'], r['tier'])]; a[0] += p * st; a[1] += p * lo; a[2] += p; a[3] += 1
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('nation\ttier\trows\tstateShare_w\tlocalShare_w\tstate_plus_local\n')
    for k in sorted(A):
        a = A[k]; o.write(f"{k[0]}\t{k[1]}\t{a[3]}\t{a[0]/a[2]:.3f}\t{a[1]/a[2]:.3f}\t{(a[0]+a[1])/a[2]:.3f}\n")
