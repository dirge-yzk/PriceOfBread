# (1022) 合流：recipeout_s の先頭の列（siteFound を除く）を 2 つの生で突き合わせる（読むだけ）。
#   python -X utf8 -I rocols1022.py <生 a（列が少ない側）> <生 b（siteFound つき）> <種の起点> <種の数>
import sys, hashlib
A, B, s0, n = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
ok = 0
for s in range(s0, s0 + n):
    ha, hb = hashlib.sha1(), hashlib.sha1(); na = nb = 0
    with open(f'{A}/recipeout_s{s}.tsv', 'rb') as f:
        for l in f: ha.update(l.rstrip(b'\r\n') + b'\n'); na += 1
    with open(f'{B}/recipeout_s{s}.tsv', 'rb') as f:
        for l in f: hb.update(l.rstrip(b'\r\n').rsplit(b'\t', 1)[0] + b'\n'); nb += 1
    same = ha.digest() == hb.digest(); ok += same
    print(f'種 {s}: 行 {na}／{nb}・siteFound を除いて {"一致" if same else "違う"}')
print(f'一致 {ok}/{n}')
