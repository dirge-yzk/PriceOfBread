# (1313) 案 A′（稼ぎの比）の近似：生まれの国の州（region_s の生まれの四半期）が、前の四半期に親の国で持っていた earnedPerMonth の比。読むだけ。
# 使い方：python -X utf8 -I q4_earn.py <q4_all.tsv> <keep の根> 名前=<下の名> ...（下の名＝base_ins・n1・n2・a11）
import sys, csv
q4, root = sys.argv[1], sys.argv[2]; m = dict(x.split('=') for x in sys.argv[3:])
rows = [x for x in csv.DictReader(open(q4, encoding='utf-8'), delimiter='\t') if x['parent'] and float(x['bornT']) > 0 and float(x['priv0']) / float(x['bornT']) < 0.1]
for x in rows:
    d = f"{root}/{m[x['arm']]}"; q0 = int(x['bornQ']); n = x['nation'].split('#')[0]; par = x['parent']
    regs = set(); pe = {}; tot = 0.0
    with open(f"{d}/region_s{x['seed']}.tsv", encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for row in r:
            q = int(row[0])
            if q == q0 and row[ix['nation']] == n: regs.add(row[ix['region']])
            if q == q0 - 1 and row[ix['nation']] == par:
                e = float(row[ix['earnedPerMonth']]); pe[row[ix['region']]] = e; tot += e
    es = sum(pe.get(g, 0.0) for g in regs) / tot if tot > 0 else float('nan')
    tE = float(x['parentT_prev']) * es
    print(f"{x['arm']}\t{x['seed']}\t{x['nation']}\tregs={len(regs)}\tparentRegs={len(pe)}\tpopShare={float(x['popShareImplied']):.4g}\tearnShare={es:.4g}\tT_pop={float(x['bornT']):.6g}\tT_earn={tE:.6g}\tT_money={float(x['T_money']):.6g}\tpriv0={x['priv0']}\tpriv0/T_earn={(float(x['priv0'])/tE if tE>0 else float('nan')):.4g}")
