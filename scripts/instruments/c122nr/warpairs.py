# (1216) 記録：同じ組の戦争の回数の上位（wars_s・内戦を除く）と、その組の平均の月数・死者。使い方: python warpairs.py タグ=<生> ...
import sys, os, collections, statistics as st
for arg in sys.argv[1:]:
    tag, raw = arg.split('=', 1); tot = 0; top = []; months = []; short = 0
    for s in range(1, 11):
        p = os.path.join(raw, f'wars_s{s}.tsv')
        if not os.path.exists(p): continue
        c = collections.Counter(); m = collections.defaultdict(list)
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                r = l.rstrip('\n').split('\t')
                if r[ix['civilWarFlag']] != '0': continue
                k = tuple(sorted((r[ix['sideA']], r[ix['sideB']]))); c[k] += 1; mo = float(r[ix['months']]); m[k].append(mo); months.append(mo); tot += 1
                if mo <= 3: short += 1
        if c:
            k, n = c.most_common(1)[0]; top.append((s, k, n, st.mean(m[k]), int(st.median([y for y in [0]]))))
    print(f'== {tag}: 戦争 {tot}・月数の中央 {st.median(months) if months else 0}・3 か月以下 {short}')
    for s, k, n, mm, _ in top: print(f'  種 {s} 最多の組 {k[0]}×{k[1]} {n} 回・平均 {mm:.1f} か月')
