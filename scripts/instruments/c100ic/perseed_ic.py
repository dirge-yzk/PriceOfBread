# (1056) c100ic 記録の台本（腕 B の後に足した・門ではない）：events_cg を種ごとに、最初の飛ばした月の前と後に分けて数える。
#   gate_ic.py の数え方を種ごとに写した物（events＝（月・敗者）・events_cg＝事象の最初の矯正が cg を通った物）。
#   新しい列の無い表（基線・ヌル腕）は cpeace の nGrouped と shares で cg を近似する（gate_ic.py と同じ）。
#   使い方: python perseed_ic.py <名>=<ins> ... --skipfrom <腕の名>   （--skipfrom の腕の種ごとの最初の飛ばした月で前後を分ける）
import sys, os, collections
args = [a for a in sys.argv[1:] if not a.startswith('--') and '=' in a]
sf = next((sys.argv[i + 1] for i, a in enumerate(sys.argv) if a == '--skipfrom'), None)

def rd(p):
    if not os.path.exists(p): return None, []
    L = open(p, encoding='utf-8').read().splitlines()
    return {k: i for i, k in enumerate(L[0].split('\t'))}, [r for r in L[1:] if r]

def seed_events(ins, s):
    ih, irows = rd(os.path.join(ins, f'imp_s{s}.tsv')); ch, crows = rd(os.path.join(ins, f'cpeace_s{s}.tsv'))
    new = 'groupIdIndex' in ih
    grp = {}
    for r in crows:
        f = r.split('\t')
        if int(f[ch['nGrouped']]) >= 2: grp[(f[ch['month']], f[ch['loser']])] = set(x.split(':')[0] for x in f[ch['shares']].split('/') if x)
    ev = {}; skipm = None
    for r in irows:
        f = r.split('\t'); m, lo, wi = f[ih['month']], f[ih['loser']], f[ih['winner']]
        if new and f[ih['skippedByGroupFlag']] == '1':
            skipm = int(m) if skipm is None else min(skipm, int(m)); continue
        incg = (f[ih['groupIdIndex']] != '0') if new else ((m, lo) in grp and wi in grp[(m, lo)])
        if (m, lo) not in ev: ev[(m, lo)] = incg
    return ev, skipm

arms = [a.split('=', 1) for a in args]
skips = {}
if sf:
    p = dict(arms)[sf]
    for s in range(1, 11): skips[s] = seed_events(p, s)[1]
print('種\t最初の飛ばした月\t' + '\t'.join(f'{n} 前/後/計' for n, _ in arms))
tot = collections.Counter(); bylo = {n: collections.Counter() for n, _ in arms}
for s in range(1, 11):
    lim = skips.get(s) if skips.get(s) is not None else 10 ** 9
    cells = []
    for n, p in arms:
        ev, _ = seed_events(p, s)
        pre = sum(1 for (m, lo), v in ev.items() if v and int(m) < lim); post = sum(1 for (m, lo), v in ev.items() if v and int(m) >= lim)
        tot[(n, 'pre')] += pre; tot[(n, 'post')] += post
        for (m, lo), v in ev.items():
            if v: bylo[n][lo] += 1
        cells.append(f'{pre}/{post}/{pre + post}')
    print(f'{s}\t{skips.get(s)}\t' + '\t'.join(cells))
print('計\t-\t' + '\t'.join(f'{tot[(n, "pre")]}/{tot[(n, "post")]}/{tot[(n, "pre")] + tot[(n, "post")]}' for n, _ in arms))
los = sorted(set().union(*[set(c) for c in bylo.values()]), key=lambda x: -sum(bylo[n][x] for n, _ in arms))
print('敗者ごとの events_cg（上位 15）\t' + '\t'.join(n for n, _ in arms))
for lo in los[:15]: print(lo + '\t' + '\t'.join(str(bylo[n][lo]) for n, _ in arms))
