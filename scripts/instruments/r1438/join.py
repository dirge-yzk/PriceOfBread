# (1438) 札ごとの出力を鍵で並べる。使い方: python join.py <出力のフォルダ> <q1|q3|q4> 札...
import sys
d, q, tags = sys.argv[1], sys.argv[2], sys.argv[3:]
T = {}
for t in tags:
    for l in open(f'{d}/{q}_{t}.tsv', 'rb').read().decode().splitlines():
        if l.startswith('#'): continue
        p = l.split('\t'); T.setdefault(p[0], {})[t] = '|'.join(p[1:])
print('key\t' + '\t'.join(tags))
for k in sorted(T): print(k + '\t' + '\t'.join(T[k].get(t, '-') for t in tags))
