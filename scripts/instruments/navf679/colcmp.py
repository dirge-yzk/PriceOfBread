#!/usr/bin/env python
"""(679) 線 navf4：計器の生の表を、列の名前で旧と比べる（新しい列は無視・旧の列だけ全行比べる）。恒等の証明の読み手（読むだけ）。
使い方：python -X utf8 -I colcmp.py <新の ins> <旧の ins> <新の種の起点> <旧の種の起点> <種の数>
  壁時計の表（burscan_s・clock_s）は比べない。flow_s は sourceLine 列を除く。新にだけある列（stock_s の新しい列）は数えるだけ。
  出力：一致した表の数・違った表（列名と違った行の数）・旧にだけある表・新にだけある表。"""
import csv, filecmp, os, sys
csv.field_size_limit(10**9)
new, old = sys.argv[1].rstrip('/'), sys.argv[2].rstrip('/')
ns, os_, n = int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
SKIP = ('burscan_s', 'clock_s')
IGN = {'flow_s': {'sourceLine'}}


def load(p):
    with open(p, encoding='utf-8-sig', newline='') as f:
        r = list(csv.reader(f, delimiter='\t'))
    return r[0], r[1:]


same = 0; diff = []; onlyold = []; onlynew = []; newcols = {}
for k in range(n):
    sn, so = ns + k, os_ + k
    fn = {f[:-len(f'_s{sn}.tsv')] for f in os.listdir(new) if f.endswith(f'_s{sn}.tsv')}
    fo = {f[:-len(f'_s{so}.tsv')] for f in os.listdir(old) if f.endswith(f'_s{so}.tsv')}
    onlyold += [f'{t}_s{so}' for t in sorted(fo - fn)]; onlynew += [f'{t}_s{sn}' for t in sorted(fn - fo)]
    for t in sorted(fn & fo):
        if t in SKIP: continue
        pn, po_ = os.path.join(new, f'{t}_s{sn}.tsv'), os.path.join(old, f'{t}_s{so}.tsv')
        if os.path.getsize(pn) == os.path.getsize(po_) and filecmp.cmp(pn, po_, shallow=False):
            same += 1; continue   # バイトで同じ表は列の比べを省く（速い道）
        hn, rn = load(os.path.join(new, f'{t}_s{sn}.tsv')); ho, ro = load(os.path.join(old, f'{t}_s{so}.tsv'))
        ign = IGN.get(t, set())
        cols = [c for c in ho if c not in ign]
        extra = [c for c in hn if c not in ho]
        if extra: newcols.setdefault(t, set()).update(extra)
        if len(rn) != len(ro):
            diff.append((f'{t}_s{sn}', f'行数 新{len(rn)} 旧{len(ro)}', {})); continue
        ixn = {c: i for i, c in enumerate(hn)}; ixo = {c: i for i, c in enumerate(ho)}
        bad = {}
        miss = [c for c in cols if c not in ixn]
        for a, b in zip(rn, ro):
            for c in cols:
                if c in ixn and (a[ixn[c]] != b[ixo[c]]): bad[c] = bad.get(c, 0) + 1
        if bad or miss: diff.append((f'{t}_s{sn}', f'新に無い旧の列 {miss}' if miss else '', bad))
        else: same += 1
print(f'旧の列が全行一致した表 = {same}・違った表 = {len(diff)}・旧にだけある表 = {len(onlyold)}・新にだけある表 = {len(onlynew)}')
for t, note, bad in diff[:30]: print(f'  違い {t} {note} 違った列と行数={bad}')
if onlyold: print('  旧にだけある:', onlyold[:10])
if onlynew: print('  新にだけある:', onlynew[:10])
print('  新しい列（旧に無い）:', {t: sorted(v) for t, v in newcols.items()})
