# (902) c77ce 記録：名目 GDP が正でない四半期の続き（人口 10 千人以上）を、国の系列の最初の行から始まる物（建国の直後の型）とそれ以外に分けて、長さの分布を数える（gdp_zero10 と同じ列・同じ人口の床）。
#   python -X utf8 -I gdpzero_born902.py <ins> [<ins>...]
import sys, os, glob, csv, collections
for d in sys.argv[1:]:
    born = collections.Counter(); other = collections.Counter(); ex = []
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        seed = os.path.basename(f)[len('nation_s'):-4]
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iq, inn, ig, ip = h.index('quarter'), h.index('nation'), h.index('nominalGdpPerYear'), h.index('populationK')
        rows = collections.defaultdict(list)
        for x in r:
            try: rows[x[inn]].append((int(x[iq]), float(x[ig]), float(x[ip])))
            except (ValueError, IndexError): pass
        for nm, v in rows.items():
            v.sort(); run = 0; start = None; first = v[0][0]
            for q, g, p in v + [(None, 1.0, 0.0)]:
                if q is not None and not (g > 0) and p >= 10:
                    if run == 0: start = q
                    run += 1
                else:
                    if run > 0:
                        (born if start == first else other)[run] += 1
                        if run >= 4: ex.append('種%s %s q%d〜 %d四半期%s' % (seed, nm, start, run, '（建国の直後）' if start == first else ''))
                    run = 0
    print(d)
    print('  建国の直後から（長さ:件数）', dict(sorted(born.items())), ' それ以外', dict(sorted(other.items())))
    if ex: print('  4 四半期以上：' + '・'.join(ex))
