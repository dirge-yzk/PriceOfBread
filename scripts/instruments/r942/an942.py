# (942) 読む：probe_<tag>.tsv を四半期×州へ畳み、PAY（決済の cost の和）・ROW（州の表の Paid）・SPQ（支払いの表の建設）を突き合わせる
import sys, collections
p = sys.argv[1]
pay = collections.defaultdict(float); payn = collections.defaultdict(float); row = collections.defaultdict(float); spq = {}
inregion = collections.Counter(); wid = {}
regs_pay = collections.defaultdict(float); regs_row = collections.defaultdict(float)
for ln in open(p, encoding='utf-8'):
    f = ln.rstrip('\n').split('\t')
    if f[0] == 'PAY':
        q, nat, w, reg, rh, cost, pk, act, uid, cont = int(f[1]), f[3], f[4], f[5], f[6], float(f[7]), f[8], f[9], f[10], f[11]
        payn[(q, nat, w)] += cost
        if nat == 'フランス':
            pay[q] += cost; regs_pay[(q, reg, rh)] += cost
            if cont != 'True': inregion[(q, reg)] += 1
    elif f[0] == 'ROW':
        q, nat, w, reg, rh, pd, cm = int(f[1]), f[3], f[4], f[5], f[6], float(f[7]), f[8]
        row[q] += pd; regs_row[(q, reg, rh)] += pd
    elif f[0] == 'SPQ':
        q, nat, w, cm, uid, pk, nreg, cs, b = int(f[1]), f[2], f[3], f[4], f[5], f[6], f[7], float(f[8]), f[9]
        if nat == 'フランス': spq[q] = (cs, cm, uid, pk, nreg, b, w)
print('q\tbucket\tpayFR\trowFR\tspendFR\trow-spend\tpay-spend\tcm\tuid\tnreg\tw')
for q in sorted(set(pay) | set(row) | set(spq)):
    cs = spq.get(q, (float('nan'),) * 7)
    print(f"{q}\t{cs[5]}\t{pay.get(q,0):.4f}\t{row.get(q,0):.4f}\t{cs[0]:.4f}\t{row.get(q,0)-cs[0]:.4f}\t{pay.get(q,0)-cs[0]:.4f}\t{cs[1]}\t{cs[2]}\t{cs[4]}\t{cs[6]}")
print('PAY in France world but region not in w.Regions:', dict(inregion))
d = [(k, regs_pay.get(k, 0) - regs_row.get(k, 0)) for k in set(regs_pay) | set(regs_row)]
d = [x for x in d if abs(x[1]) > 1e-6]
print('state x quarter where pay != row:', len(d))
for k, v in sorted(d, key=lambda x: -abs(x[1]))[:20]: print(k, round(v, 4))
# 仏の名を持つ世界の数
ws = collections.defaultdict(set)
for (q, nat, w), v in payn.items(): ws[nat].add(w)
print({k: len(v) for k, v in ws.items()})
