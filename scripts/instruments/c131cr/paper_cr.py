# (1278) c131cr の紙：基準の生 region_s の州の人口で、国の主流の札（Diplomacy.MajorityCultureReligion）を
# 旧（植民地＝昇格の時の持ち主の首都の札）・新（データの札・全ての州）・新＋本国格（Colonial 格を数えない）で数え、
# 旧と違う国×四半期・首都の札と違う国×四半期・空の札の国×四半期を出す。エンジンの関数を呼ばない（読む州を自分で選ぶ）。
# 使い方: python -I paper_cr.py <生のフォルダ> <world.json> <colony_labels.tsv> [種...]
import sys, csv, json, collections
sys.stdout.reconfigure(encoding='utf-8')
raw, wpath, lpath = sys.argv[1], sys.argv[2], sys.argv[3]
seeds = [int(s) for s in sys.argv[4:]] or list(range(1, 11))
w = json.load(open(wpath, encoding='utf-8-sig'))
prov = {}
for n in w['nations']:
    for p in n['provinces']:
        prov[p['city']] = (p.get('culture') or '', p.get('religion') or '')
lab = {}
with open(lpath, encoding='utf-8') as f:
    for r in csv.DictReader(f, delimiter='\t'):
        lab[r['name']] = (r['culture'], r['religion'])
csv.field_size_limit(10**8)

def best(t):
    b, bv = ('', 0.0)
    for k, v in t.items():       # 挿入順＝州の順（エンジンの辞書と同じ先勝ち）
        if v > bv: bv, b = v, k
    return b

tot = collections.Counter(); unknown = set(); per_seed = []
for s in seeds:
    path = f"{raw}/region_s{s}.tsv"
    oldlab = {}          # 植民地の旧の札（昇格の時の持ち主の首都の旧の札）
    rows_by_q = collections.defaultdict(list)
    with open(path, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
        for c in rd:
            if len(c) < len(h): continue
            if c[ix['activeFlag']] != '1': continue
            rows_by_q[int(c[ix['quarter']])].append((c[ix['nation']], c[ix['region']], c[ix['tier']], float(c[ix['populationK']])))
    cnt = collections.Counter()
    natq = set(); ex1 = {}
    for q in sorted(rows_by_q):
        by_n = collections.OrderedDict()
        for (nat, reg, tier, pk) in rows_by_q[q]:
            by_n.setdefault(nat, []).append((reg, tier, pk))
        for nat, regs in by_n.items():
            cap = regs[0][0]
            def old_of(reg):
                if reg in prov: return prov[reg]
                if reg in oldlab: return oldlab[reg]
                return None
            # 昇格の時の持ち主の首都の旧の札を、初めて見えた四半期で決める
            for (reg, tier, pk) in regs:
                if reg not in prov and reg not in oldlab:
                    if reg not in lab: unknown.add(reg)
                    co = old_of(cap)
                    oldlab[reg] = co if co is not None else ('', '')
            for (reg, tier, pk) in regs:
                if reg not in prov and reg not in lab: unknown.add(reg)
            ct = {'old': ({}, {}), 'new': ({}, {}), 'newhome': ({}, {})}
            for (reg, tier, pk) in regs:
                if not (pk > 0): continue
                o = old_of(reg); nw = prov.get(reg) or lab.get(reg) or ('', '')
                for key, l, use in (('old', o, True), ('new', nw, True), ('newhome', nw, tier != 'Colonial')):
                    if not use: continue
                    if l[0]: ct[key][0][l[0]] = ct[key][0].get(l[0], 0) + pk
                    if l[1]: ct[key][1][l[1]] = ct[key][1].get(l[1], 0) + pk
            m = {k: (best(v[0]), best(v[1])) for k, v in ct.items()}
            capnew = prov.get(cap) or lab.get(cap) or ('', '')
            cnt['nq'] += 1
            for k in ('new', 'newhome'):
                if m[k][0] != m['old'][0]: cnt[f'{k}_cult_flip'] += 1
                if m[k][1] != m['old'][1]: cnt[f'{k}_rel_flip'] += 1
                if m[k] != m['old']:
                    cnt[f'{k}_any_flip'] += 1; natq.add((k, nat)); ex1.setdefault((k, nat), (q, m['old'], m[k], 0))
                    e = ex1[(k, nat)]; ex1[(k, nat)] = (e[0], e[1], e[2], e[3] + 1)
                if m[k][0] != capnew[0]: cnt[f'{k}_cult_ne_cap'] += 1
                if m[k][0] == '': cnt[f'{k}_cult_empty'] += 1
            if m['old'][0] != capnew[0]: cnt['old_cult_ne_cap'] += 1
    nats = collections.Counter(k for (k, _) in natq)
    print(f"seed {s} " + ' '.join(f"{k}={v}" for k, v in sorted(cnt.items())) + f" nations_new={nats['new']} nations_newhome={nats['newhome']}")
    ex = sorted(n for (k, n) in natq if k == 'new')
    print(f"  new_flip_nations: {' '.join(ex)}")
    print(f"  newhome_flip_nations: {' '.join(sorted(n for (k, n) in natq if k == 'newhome'))}")
    for (k, nat), e in sorted(ex1.items()): print(f"  ex {k} {nat} firstQ={e[0]} old={e[1][0]}/{e[1][1]} new={e[2][0]}/{e[2][1]} quarters={e[3]}")
    tot.update(cnt)
print("total " + ' '.join(f"{k}={v}" for k, v in sorted(tot.items())))
print("unknown_regions " + ' '.join(sorted(unknown)))
