# (1044) 合流：列が末尾に増えた表（頭を名指し）について、基準の生の列の数までの先頭の列を〈行末を LF にそろえて〉sha1 する（読むだけ・基準の生は CRLF なので hash1044.py の全体の sha1 とは比べない）。
#   python -X utf8 -I prefix1044.py <ins フォルダ> <出力 tsv> <基準の ins フォルダ> <表の頭,...>
#   python -X utf8 -I prefix1044.py --cmp <a.tsv> <b.tsv>
import sys, os, hashlib
if sys.argv[1] == '--cmp':
    A = dict(l.rstrip('\n').split('\t', 1) for l in open(sys.argv[2], encoding='utf-8'))
    B = dict(l.rstrip('\n').split('\t', 1) for l in open(sys.argv[3], encoding='utf-8'))
    com = [k for k in A if k in B]; same = [k for k in com if A[k] == B[k]]
    print(f'先頭の列：共通 {len(com)}・一致 {len(same)}・違い {len(com) - len(same)}')
    for k in com:
        if A[k] != B[k]: print('違い', k)
    sys.exit(0)
D, O, BASE, heads = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4].split(',')
with open(O, 'w', encoding='utf-8', newline='\n') as f:
    for n in sorted(os.listdir(D)):
        if not any(n.startswith(h + '_s') or n.startswith(h) for h in heads): continue
        bp = os.path.join(BASE, n)
        if not os.path.isfile(bp): continue
        with open(bp, 'rb') as g: k = len(g.readline().rstrip(b'\r\n').split(b'\t'))
        h = hashlib.sha1()
        with open(os.path.join(D, n), 'rb') as g:
            for line in g: h.update(b'\t'.join(line.rstrip(b'\r\n').split(b'\t')[:k]) + b'\n')
        f.write(f'{n}\t{k}\t{h.hexdigest()}\n')
print('ok', O)
