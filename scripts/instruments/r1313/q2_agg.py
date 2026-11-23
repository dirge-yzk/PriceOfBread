# (1313) q2_allborn の集計：官僚 0 か否か・民間÷国庫の区分ごとに、20 四半期の出口（支出の科目の和）÷生まれの国庫・行政の支出が 0 の割合。
import sys, csv, statistics as st
rows = []
for p in sys.argv[1:]:
    with open(p, encoding='utf-8') as f:
        r = csv.DictReader(f, delimiter='\t'); rows += list(r)
OUT = ['Army', 'Navy', 'Admin', 'Education', 'Welfare', 'Pension', 'Modernization', 'AntiCorruption', 'Interest', 'DebtRepay', 'Subsidy', 'ColonialUpkeep', 'TributeOut', 'OtherOut']
INC = ['Tax', 'Tariff', 'StateMonopoly', 'Borrow', 'Devalue', 'StateFactory', 'TributeIn', 'OtherIn']
def f(x):
    try: return float(x)
    except: return float('nan')
def grp(name, sel):
    g = [r for r in rows if sel(r) and f(r['bornT']) > 0 and f(r['pop0K']) >= 100]
    if not g: print(name, 0); return
    spend = [-sum(f(r[c]) for c in OUT) / f(r['bornT']) for r in g]
    inc = [sum(f(r[c]) for c in INC) / f(r['bornT']) for r in g]
    adm0 = sum(1 for r in g if f(r['Admin']) == 0)
    army = [-f(r['Army']) / f(r['bornT']) for r in g]
    tt = [f(r['T20']) / f(r['bornT']) for r in g]
    pr = [f(r['priv20']) / max(1e-12, f(r['T20'])) for r in g]
    q = lambda v: f"{st.median(v):.3g}[{sorted(v)[len(v)//10]:.3g}..{sorted(v)[(len(v)*9)//10]:.3g}]"
    print(f"{name}\tn={len(g)}\tAdmin0={adm0}\tspend20/T0={q(spend)}\tarmy20/T0={q(army)}\tinc20/T0={q(inc)}\tT20/T0={q(tt)}\tpriv20/T20={q(pr)}")
for arm in sorted(set(r['arm'] for r in rows)) + ['ALL']:
    A = (lambda r: True) if arm == 'ALL' else (lambda r, a=arm: r['arm'] == a)
    grp(arm + ' all', A)
    grp(arm + ' bur0=0', lambda r: A(r) and f(r['bur0K']) == 0)
    grp(arm + ' bur0>0', lambda r: A(r) and f(r['bur0K']) > 0)
    grp(arm + ' priv0/T<0.1', lambda r: A(r) and f(r['bornT']) > 0 and f(r['priv0']) / f(r['bornT']) < 0.1)
    grp(arm + ' priv0/T>=1', lambda r: A(r) and f(r['bornT']) > 0 and f(r['priv0']) / f(r['bornT']) >= 1)
