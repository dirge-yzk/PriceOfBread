# 読むだけ (1169)：S01-V01〈解禁の順の一貫性〉＝engineer_s の E 行（解禁した四半期）で、魚の缶詰が缶詰より先に（缶詰が未解禁のまま含む）解禁した国×種を数える。
# 使い方: python -X utf8 -I s01_v01.py <生のフォルダ> <種の数>
import sys, os, csv
B, NS = sys.argv[1], int(sys.argv[2])
both = fish_first = fish_only = can_first = same = 0; miss = 0
for s in range(1, NS + 1):
    p = os.path.join(B, f'engineer_s{s}.tsv')
    if not os.path.exists(p): miss += 1; continue
    first = {}
    for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
        if r['kind'] != 'E' or r['row'] not in ('魚の缶詰', '缶詰'): continue
        k = (r['nation'], r['row']); q = int(r['quarter'])
        if k not in first or q < first[k]: first[k] = q
    for n in {k[0] for k in first}:
        f, c = first.get((n, '魚の缶詰')), first.get((n, '缶詰'))
        if f is not None and c is None: fish_only += 1
        elif f is not None and c is not None:
            both += 1
            if f < c: fish_first += 1
            elif f == c: same += 1
            else: can_first += 1
tot = fish_first + fish_only + both - fish_first + 0
n_f = fish_first + fish_only + same + can_first
print(f'種 {NS - miss}/{NS}・魚の缶詰を解禁した国×種 {n_f}・缶詰より先 {fish_first}・缶詰は未解禁のまま {fish_only}・同じ四半期 {same}・缶詰が先 {can_first}・逆順（先＋未解禁）の割合 {(fish_first + fish_only) / max(n_f, 1):.1%}')
