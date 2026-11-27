# (1425) 合流が写した：c152rw の反証役 v の数え直し（入口の突き合わせを q＝始めの四半期で）。使い方: python q2_entry_fix.py <生> <種の範囲 1-10>
# (1424) 問い2 の補：崩壊で終わった後 12 か月以内の同じ組の再戦を、次の戦争の入口（wars_s entryCode）で分ける（読むだけ）。
import sys, os, collections, csv
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lk_gate import load, groups, endpath
d = sys.argv[1]; s0, s1 = map(int, sys.argv[2].split('-')); cnt = collections.Counter(); prevep = collections.Counter()
for s in range(s0, s1 + 1):
    ix, rows = load(f'{d}/wexit_s{s}.tsv'); ws = []
    for g, rs in groups(ix, rows).items():
        ws.append((frozenset((g[0], g[1])), int(rs[0][ix['month']]), int(rs[-1][ix['month']]), endpath(ix, rs[-1]), g[2]))
    r = csv.reader(open(f'{d}/wars_s{s}.tsv', encoding='utf-8'), delimiter='\t'); h = next(r); jx = {c: i for i, c in enumerate(h)}
    ent = collections.defaultdict(list)
    for x in r:
        ent[frozenset((x[jx['sideA']], x[jx['sideB']]))].append((int(x[jx['q']]) * 3, x[jx['entryCode']]))
    for p, st, en, ep, cv in ws:
        if ep != 'collapse' or en >= 2003: continue
        nx = sorted((w for w in ws if w[0] == p and w[1] > en), key=lambda w: w[1])
        if not nx or nx[0][1] - en > 12: continue
        st2 = nx[0][1]; c = [e for m, e in ent[p] if abs(m - st2) <= 3]
        cnt[(c[0] if c else '?') + ('/civil' if nx[0][4] == '1' else '')] += 1
print(d, dict(cnt.most_common()))
