# (1023) 床 F1〜F3 と新しい列の数え直し（ident の計器つき 10 種の生・読むだけ）。使い方：python floors.py <ins のフォルダ>
import sys, glob, os, re
d = sys.argv[1]
def rows(p):
    L = open(p, encoding='utf-8').read().split('\n'); h = L[0].split('\t')
    return h, [dict(zip(h, l.split('\t'))) for l in L[1:] if l]
tot = {}
for p in sorted(glob.glob(os.path.join(d, 'warentry_s*.tsv')), key=lambda x: int(re.findall(r'(\d+)\.tsv', x)[0])):
    seed = re.findall(r'(\d+)\.tsv', p)[0]
    h, R = rows(p)
    S = lambda k: sum(float(r[k]) for r in R)
    inv = len(rows(os.path.join(d, 'invade_s%s.tsv' % seed))[1]) if os.path.exists(os.path.join(d, 'invade_s%s.tsv' % seed)) else -1
    o = dict(evals=S('evals'), F1_will=S('r_will'), F2_invade=inv, F3_shcalls=S('sh_calls'), tauPos=S('tauPos'), newOk=S('newOk'), oldOk=S('oldOk'), newOnly=S('newOnly'), rowsTau=sum(1 for r in R if float(r['tauPos']) > 0))
    print('seed', seed, ' '.join('%s=%g' % kv for kv in o.items()))
    for k, v in o.items(): tot[k] = tot.get(k, 0) + v
print('total', ' '.join('%s=%g' % kv for kv in tot.items()))
