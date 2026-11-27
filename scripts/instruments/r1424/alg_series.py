# (1424) 読む：名札 NAME を持つ nationUid の系列（最初と最後の四半期・人口・本国の州）を種ごとに出す。
import csv, sys, collections
d = sys.argv[1]; name = sys.argv[2]; seeds = sys.argv[3].split('-'); s0, s1 = int(seeds[0]), int(seeds[1])
for s in range(s0, s1 + 1):
    f = open(f'{d}/nation_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
    ser = collections.OrderedDict(); names = collections.defaultdict(list)
    for row in r:
        u = row[ix['nationUid']]; ser.setdefault(u, []).append(row)
        nm = row[ix['nation']]
        if not names[u] or names[u][-1][1] != nm: names[u].append((row[ix['quarter']], nm))
    for u, rows in ser.items():
        if not any(n == name for q, n in names[u]): continue
        pk = [float(x[ix['populationK']]) for x in rows]
        print(f's{s} uid={u} idx={rows[0][ix["nationIndex"]]} q={rows[0][ix["quarter"]]}..{rows[-1][ix["quarter"]]} n={len(rows)} popFirst={pk[0]:.0f} popMax={max(pk):.0f} popLast={pk[-1]:.0f} core={rows[0][ix["coreRegions"]]}->{rows[-1][ix["coreRegions"]]} names={names[u][:6]}')
