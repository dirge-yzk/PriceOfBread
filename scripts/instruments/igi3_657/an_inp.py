# (657) 線 igi3：計器 inpf_s の読み手（読むだけ）。年代窓 4 つ×国（と全 9 国の和）で、工場×月の失い lossRef を理由別に割った割合を、種ごとに出して種の中央値と幅を並べる。
# 使い方：python an_inp.py <ins フォルダ> [種の数=10] [種の起点=1]
import sys, csv, statistics, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
WIN = [(1783, 1829), (1830, 1869), (1870, 1909), (1910, 1950)]
def win(y):
    for i, (a, b) in enumerate(WIN):
        if a <= y <= b: return i
    return None
def cls(r):
    if r.startswith('zeroDesired.'): return 'zero'
    if r in ('ok', 'dropped'): return r
    return r
REASONS = ['labor', 'svcAlloc', 'worldUnreleased', 'nationUnreleased', 'worldNoSeller', 'reachLack', 'noOneAsks', 'contested.f', 'contested.p', 'contested.i', 'contested.m', 'dropped']
res = collections.defaultdict(lambda: collections.defaultdict(list))   # (win, nation|'ALL') -> metric -> [per seed]
c2bad = c2n = 0
for s in range(s0, s0 + ns):
    acc = collections.defaultdict(lambda: collections.defaultdict(float))
    with open(f"{d}/inpf_s{s}.tsv", encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for r in rd:
            if r[ix['reason']] == 'c2Mismatch': c2bad += float(r[ix['months']]); c2n += float(r[ix['desiredRef']]); continue
            w = win(int(r[ix['year']]))
            if w is None: continue
            rs = cls(r[ix['reason']])
            for nat in (r[ix['nation']], 'ALL'):
                a = acc[(w, nat)]
                a['months_' + rs] += float(r[ix['months']]); a['loss_' + rs] += float(r[ix['lossRef']]); a['des_' + rs] += float(r[ix['desiredRef']])
                a['unsup'] += float(r[ix['unsupportedRef']]); a['out'] += float(r[ix['outputRef']]); a['ext_' + rs] += float(r[ix['nExtreme']])
    for key, a in acc.items():
        des = sum(v for k, v in a.items() if k.startswith('des_') and not k.endswith('_zero'))
        loss = sum(a['loss_' + x] for x in REASONS)
        mon = sum(v for k, v in a.items() if k.startswith('months_') and k != 'months_zero')
        m = res[key]
        m['lossOverDesired'].append(loss / des if des > 0 else float('nan'))
        m['otherLossOverDesired'].append((loss - a['loss_labor']) / des if des > 0 else float('nan'))
        m['unsupOverOut'].append(a['unsup'] / a['out'] if a['out'] > 0 else float('nan'))
        m['lowShare'].append((mon - a['months_ok']) / mon if mon > 0 else float('nan'))
        m['zeroShare'].append(a['months_zero'] / (mon + a['months_zero']) if mon + a['months_zero'] > 0 else float('nan'))
        for x in REASONS: m['lossShare_' + x].append(a['loss_' + x] / loss if loss > 0 else 0.0)
        lowm = mon - a['months_ok']
        m['extremeShare'].append(sum(a['ext_' + x] for x in REASONS) / mon if mon > 0 else float('nan'))
def med(v):
    v = [x for x in v if x == x]; return statistics.median(v) if v else float('nan')
def rng(v):
    v = [x for x in v if x == x]; return (min(v), max(v)) if v else (float('nan'), float('nan'))
print(f"# inpf 読み手 ins={d} 種 {s0}..{s0+ns-1}  C2 食い違い {c2bad:.0f} / 見た工場×月 {c2n:.0f}")
nats = sorted({k[1] for k in res if k[1] != 'ALL'})
for w in range(4):
    print(f"\n== 窓 {WIN[w][0]}-{WIN[w][1]}（種の中央値［最小,最大］）")
    for nat in ['ALL'] + nats:
        m = res.get((w, nat))
        if not m: continue
        top = sorted(REASONS, key=lambda x: -med(m['lossShare_' + x]))[:4]
        print(f"{nat:8s} loss/des {med(m['lossOverDesired']):.3f} other/des {med(m['otherLossOverDesired']):.3f} unsup/out {med(m['unsupOverOut']):.3f} low {med(m['lowShare']):.3f} ext {med(m['extremeShare']):.3f} zero {med(m['zeroShare']):.3f} | "
              + ' '.join(f"{x}={med(m['lossShare_'+x]):.2f}" for x in top))
