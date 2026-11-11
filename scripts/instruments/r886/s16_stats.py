# 読むだけ：S16（蜂起と内戦と運動）の判定の材料を計器の生から数える。引数＝生のフォルダ（例 D:/PoB_work/keep_885/base_ins）
import csv, sys, collections, os
d = sys.argv[1]
def rows(t, s):
    p = os.path.join(d, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return []
    with open(p, encoding='utf-8', newline='') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))
seeds = range(1, 11)
out = []
def P(*a): out.append('\t'.join(str(x) for x in a))
# 1) coupgate の経路
rc = collections.Counter(); rcs = collections.defaultdict(set)
for s in seeds:
    for r in rows('coupgate', s): rc[r['route']] += 1; rcs[r['route']].add(s)
P('coupgate_route', 'route', 'rows', 'seeds_with')
for k in sorted(rc, key=int): P('coupgate_route', k, rc[k], len(rcs[k]))
# 2) 運動体（dcont）
mut_q = 0; gate_q = 0; strong_q = 0; rq = 0; revolts = 0; strike_q = 0; nat_mut = set(); nat_gate=set(); nat_strong=set(); nat_rev=set()
maxcoh = 0.0
for s in seeds:
    last = {}
    for r in rows('dcont', s):
        rq += 1
        if int(r['factionsMutatedCount']) > 0: mut_q += 1; nat_mut.add(r['nation'])
        if int(r['facCohGateCount']) > 0: gate_q += 1; nat_gate.add(r['nation'])
        if int(r['facStrongCount']) > 0: strong_q += 1; nat_strong.add(r['nation'])
        if r['strikeRegions'] not in ('', '0'): strike_q += 1
        maxcoh = max(maxcoh, float(r['factionsMaxCohesionIndex']))
        last[r['nation']] = int(r['factionRevoltsCum'])
    for n, v in last.items():
        revolts += v
        if v > 0: nat_rev.add(n)
P('dcont', 'rows', rq); P('dcont', 'q_mutated>0', mut_q, 'nations', len(nat_mut)); P('dcont', 'q_cohGate>0', gate_q, 'nations', len(nat_gate))
P('dcont', 'q_strong>0', strong_q, 'nations', len(nat_strong)); P('dcont', 'factionRevoltsCum_sum_final(計器国)', revolts, 'nations', len(nat_rev)); P('dcont', 'maxCohesion', maxcoh); P('dcont', 'q_strikeRegions>0', strike_q)
# 3) prdf の蜂起
tc = collections.Counter(); rn = 0
for s in seeds:
    for r in rows('prdf', s):
        n = int(r['revoltN']); rn += n
        if n > 0: tc[r['revoltTarget']] += 1
P('prdf', 'revoltN_sum', rn); P('prdf', 'revoltTarget(行)', dict(tc))
# 4) 戦争の表の内戦
cw = 0; cwm = []; allw = 0
for s in seeds:
    for r in rows('wars', s):
        allw += 1
        if r['civilWarFlag'] == '1': cw += 1; cwm.append(float(r['months']))
cwm.sort()
P('wars', 'all', allw, 'civil', cw, 'months_median', cwm[len(cwm)//2] if cwm else '-', 'months_max', cwm[-1] if cwm else '-')
# 5) 講和の内訳（cpmed）
cb = collections.Counter()
for s in seeds:
    for r in rows('cpmed', s):
        if r['civilFlag'] == '1': cb[('civil', 'blank' if r['blankFlag'] == '1' else 'nonblank', 'forced' if r['forcedFlag'] == '1' else '-')] += 1
for k, v in sorted(cb.items()): P('cpmed_civil', *k, v)
# 6) nation の内戦の月
cm = 0; cmq = 0; natc = set()
for s in seeds:
    for r in rows('nation', s):
        v = int(float(r['civilWarMonths']));
        if v > 0: cmq += 1; cm += v; natc.add(r['nation'])
P('nation', 'q_civilWarMonths>0', cmq, 'months_sum', cm, 'nations', len(natc))
print('\n'.join(out))
