# (1349) c142ds 計器 migcap_s の読み手（記録・門にしない）。python migcap_ds.py <ins> <種の起点> <種の数>
# 州×四半期の行のうち計画の流入＞0 の行で、受け入れの上限が効く行（計画＞上限）の割合を旧（行き先＝機会）と新（機会×食の充足）で数える。
# 〈2 乗の行〉＝上限が効き、かつ平滑の後の食の充足＜1 の行（食が上限と行き先の値打ちの両方に入る）。上限で縮めた計画の和 Σmin(計画, 上限) も旧と新で並べる。
import sys, os
ins, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
K = ['dom', 'xb']
T = {k: {'n_old': 0, 'n_new': 0, 'bind_old': 0, 'bind_new': 0, 'sq_new': 0, 'plan_old': 0.0, 'plan_new': 0.0, 'capd_old': 0.0, 'capd_new': 0.0} for k in K}
seen = 0
for s in range(s0, s0 + ns):
    p = os.path.join(ins, 'migcap_s%d.tsv' % s)
    if not os.path.exists(p): print('種', s, 'MISSING'); continue
    seen += 1
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
        for line in fh:
            v = line.rstrip('\n').split('\t')
            food = f(v[ix['foodFillSmoothed']])
            for k, pre, unit in (('dom', 'dom', 'KPerMonth'), ('xb', 'xb', 'KPerQuarter')):
                po, pn, cap = f(v[ix[pre + 'PlanOld' + unit]]), f(v[ix[pre + 'PlanNew' + unit]]), f(v[ix[pre + 'Cap' + unit]])
                t = T[k]
                if po > 0:
                    t['n_old'] += 1; t['plan_old'] += po; t['capd_old'] += min(po, cap) if cap == cap else 0
                    if po > cap: t['bind_old'] += 1
                if pn > 0:
                    t['n_new'] += 1; t['plan_new'] += pn; t['capd_new'] += min(pn, cap) if cap == cap else 0
                    if pn > cap:
                        t['bind_new'] += 1
                        if food < 1.0: t['sq_new'] += 1
print('種の数', seen)
print('区分\t計画＞0の行(旧)\t上限が効く割合(旧)\t計画＞0の行(新)\t上限が効く割合(新)\t2乗の行の割合(新)\t計画の和(旧)\t計画の和(新)\t新÷旧\t上限で縮めた和(旧)\t上限で縮めた和(新)\t新÷旧')
for k in K:
    t = T[k]
    r = lambda a, b: a / b if b else float('nan')
    print('%s\t%d\t%.4f\t%d\t%.4f\t%.4f\t%.1f\t%.1f\t%.4f\t%.1f\t%.1f\t%.4f' % (k, t['n_old'], r(t['bind_old'], t['n_old']), t['n_new'], r(t['bind_new'], t['n_new']), r(t['sq_new'], t['n_new']),
          t['plan_old'], t['plan_new'], r(t['plan_new'], t['plan_old']), t['capd_old'], t['capd_new'], r(t['capd_new'], t['capd_old'])))
print('単位：国内は 1 か月分の千人（四半期の末の盤面）・越境は 1 四半期分の千人。和は州×四半期の和（種をまたぐ）。')
