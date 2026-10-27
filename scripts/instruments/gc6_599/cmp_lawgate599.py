"""(599) gc6：lawgate_s*.tsv を、消した 4 列〔flagLevel・goldWishOld・antiTargetOld・guildTargetOld〕を基準の側から落として今の生とバイトで比べる。
使い方：python -I -X utf8 cmp_lawgate599.py <基準の生のフォルダ> <今の生のフォルダ> [種の数=10]
基準（m592）の見出しは 18 列・今は 14 列。基準から 4 列を落とした見出しと全行が今と一致し、今の見出しに 4 列が無いことを確かめる。"""
import sys
A, B = sys.argv[1], sys.argv[2]
n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
DROP = [b'flagLevel', b'goldWishOld', b'antiTargetOld', b'guildTargetOld']
bad = 0
for s in range(1, n + 1):
    def rd(p):
        L = [x.rstrip(b'\r').split(b'\t') for x in open(p, 'rb').read().split(b'\n') if x.strip(b'\r') != b'']
        return L[0], L[1:]
    ha, ra = rd(f'{A}/lawgate_s{s}.tsv'); hb, rb = rd(f'{B}/lawgate_s{s}.tsv')
    keep = [i for i, c in enumerate(ha) if c not in DROP]
    ha2 = [ha[i] for i in keep]; ra2 = [[r[i] for i in keep] for r in ra]
    ok = (ha2 == hb and ra2 == rb and len(ha) == 18 and len(hb) == 14 and all(c not in hb for c in DROP)
          and all(len(r) == 18 for r in ra) and all(len(r) == 14 for r in rb))
    print(s, 'lawgate-eq-without-4-columns' if ok else 'DIFF', len(ra), len(rb)); bad += (not ok)
print('LAWGATEBAD', bad)
sys.exit(1 if bad else 0)
