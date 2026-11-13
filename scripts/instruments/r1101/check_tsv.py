# (1101) TSV の点検：12 列・行の id の重複 0・錨の行に名が含まれる（木の作業ツリー）。呼び方：python -X utf8 -I check_tsv.py <repo> <TSV>
import sys, os
repo, p = sys.argv[1], sys.argv[2]
L = open(p, encoding='utf-8').read().split('\n')[1:]
L = [x for x in L if x]
ids = [x.split('\t')[0] for x in L]
bad = 0; n = 0
for x in L:
    c = x.split('\t')
    if len(c) != 12: print('列', c[0], len(c)); bad += 1
    for a in c[4].split(';'):
        f, ln, nm = a.split(':')
        s = open(os.path.join(repo, 'PriceOfBread.SimCore', f), encoding='utf-8-sig').read().split('\n')[int(ln) - 1]
        n += 1
        if nm not in s: print('錨', a); bad += 1
        else: print('ok', a, '|', s.strip()[:90])
print('rows', len(L), 'dupIds', len(ids) - len(set(ids)), 'anchors', n, 'bad', bad)
