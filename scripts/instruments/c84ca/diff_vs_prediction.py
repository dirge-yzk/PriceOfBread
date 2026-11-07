# (948) c84ca：cons_s・consf_s の基準と腕の行の差が、紙の予言（州の名の集合が前の四半期と変わった (種, 国, 四半期)）の中だけかを数える。
import csv, collections, sys
base, arm = sys.argv[1], sys.argv[2]
def load(d, name, s):
    rows = list(csv.reader(open(f'{d}/{name}{s}.tsv', encoding='utf-8'), delimiter='\t')); return rows[0], rows[1:]
tot = collections.Counter()
for s in range(1, 11):
    h, rb = load(base, 'cons_s', s)
    iq, inat, ir = h.index('quarter'), h.index('nation'), h.index('region')
    sets = collections.defaultdict(dict)
    for r in rb: sets[r[inat]].setdefault(int(r[iq]), set()).add(r[ir])
    ev = set()
    for n, d in sets.items():
        prev = None
        for q in sorted(d):
            if prev is not None and (q != prev + 1 or d[q] != d[prev]): ev.add((n, q))
            prev = q
    for name in ('cons_s', 'consf_s'):
        h, rb = load(base, name, s); h2, ra = load(arm, name, s); assert h == h2
        iq, inat = h.index('quarter'), h.index('nation')
        cb, ca = collections.Counter(map(tuple, rb)), collections.Counter(map(tuple, ra))
        diff = (cb - ca) + (ca - cb)
        keys = {(r[inat], int(r[iq])) for r in diff}
        out = {k for k in keys if k not in ev}
        tot[name + ' 差の行'] += sum(diff.values()); tot[name + ' 差の (国,四半期)'] += len(keys); tot[name + ' 予言の外'] += len(out)
        if name == 'cons_s': tot['予言の件数'] += len(ev)
        if out: print('予言の外', name, s, sorted(out)[:5])
for k, v in tot.items(): print(k, v)
