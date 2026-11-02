# (793) プローブの木（旗0）の生の計器を基準の生（keep_790/base_ins）と種ごとに突き合わせる（読むだけ）
# 既知の揺れ：burscan_s:seconds・clock_s の kind=C 行の worldTotalPerMonth。flow_s は sourceFile/sourceLine を除いた行の多重集合。r793_s は新規＝除外。
import sys, os, glob, csv
from collections import Counter
base, arm = sys.argv[1], sys.argv[2]; seeds = sys.argv[3].split(',')
csv.field_size_limit(1 << 26)
ok = bad = miss = 0; bads = []
def rows(p):
    with open(p, encoding='utf-8', newline='') as f: return [l.rstrip('\n').split('\t') for l in f]
for s in seeds:
    for bp in sorted(glob.glob(os.path.join(base, f'*_s{s}.tsv'))):
        name = os.path.basename(bp); ap = os.path.join(arm, name)
        if not os.path.exists(ap): miss += 1; bads.append('欠け ' + name); continue
        a, b = open(ap, 'rb').read(), open(bp, 'rb').read()
        if a == b: ok += 1; continue
        tbl = name.rsplit('_s', 1)[0]
        ra, rb = rows(ap), rows(bp)
        h = rb[0]
        def norm(rs, drop):
            idx = [i for i, c in enumerate(h) if c not in drop]
            return [tuple(r[i] for i in idx if i < len(r)) for r in rs]
        if tbl == 'burscan':
            same = norm(ra, {'seconds'}) == norm(rb, {'seconds'})
        elif tbl == 'clock':
            k = h.index('kind') if 'kind' in h else None; w = h.index('worldTotalPerMonth') if 'worldTotalPerMonth' in h else None
            fx = lambda rs: [tuple(c for i, c in enumerate(r) if not (k is not None and r[k] == 'C' and i == w)) for r in rs]
            same = fx(ra) == fx(rb)
        elif tbl == 'flow':
            same = Counter(norm(ra, {'sourceFile', 'sourceLine'})) == Counter(norm(rb, {'sourceFile', 'sourceLine'}))
        else: same = False
        if same: ok += 1
        else: bad += 1; bads.append('不一致 ' + name)
extra = [os.path.basename(p) for s in seeds for p in glob.glob(os.path.join(arm, f'*_s{s}.tsv')) if not os.path.exists(os.path.join(base, os.path.basename(p)))]
print(f'種 {seeds}：一致 {ok}・不一致 {bad}・欠け {miss}・腕だけの表 {extra}')
for x in bads[:30]: print('  ', x)
