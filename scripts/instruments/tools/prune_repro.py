# (1046) (1044) の 30/30 を manifest から再現する：基準の先頭の列の sha1（measurements/m1044_20261111/hash/pre_base.tsv＝表・列数・sha1）と、
# prune.py の manifest（腕が基準と同じ見出しの表は sha1_full_LF、列が増えた表は sha1_prefix_LF）が全て一致するか。
#   python -X utf8 -I prune_repro.py <prune_manifest.tsv> <pre_base.tsv>
import sys
man = {}
for l in open(sys.argv[1], encoding='utf-8'):
    if l.startswith('#'): continue
    x = l.rstrip('\n').split('\t'); man[x[0]] = x
ok = n = 0
for l in open(sys.argv[2], encoding='utf-8'):
    name, k, h = l.rstrip('\n').split('\t'); n += 1
    x = man.get(name)
    got = None if x is None else (x[4] if x[3] != '-' else x[2])
    if got == h: ok += 1
    else: print('違い', name)
print(f'先頭の列：{ok}/{n}')
sys.exit(0 if ok == n and n else 1)
