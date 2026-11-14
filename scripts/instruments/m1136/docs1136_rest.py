# (1136) 合流：README に ★(1136) の行（★(1129) の行の後）・CURRENT の最終更新の行の差し替え・実装ログの末尾に (1130)〜(1136) を足す（バイトで読み書き・CR の数を保つ）。
#   python -X utf8 -I docs1136_rest.py <リポジトリの根> <断片のフォルダ>
#   断片：frag/readme1136.txt・frag/current1136.txt・log_lines.md（(1130)〜(1135)）・log_merge.md（(1136)）
import os, sys

root, w = sys.argv[1:3]


def rd(p):
    return open(p, 'rb').read().decode('utf-8').strip('\n')


def edit(p, fn):
    s = open(p, 'rb').read().decode('utf-8')
    cr = s.count('\r')
    s2 = fn(s)
    assert s2.count('\r') == cr, (p, s2.count('\r'), cr)
    open(p, 'wb').write(s2.encode('utf-8'))


def readme(s):
    L = s.split('\n')
    idx = [i for i, l in enumerate(L) if l.startswith('- ★(1129)：')]
    assert len(idx) == 1, idx
    L.insert(idx[0] + 1, rd(os.path.join(w, 'frag', 'readme1136.txt')))
    return '\n'.join(L)


def current(s):
    L = s.split('\n')
    idx = [i for i, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** |')]
    assert len(idx) == 1, idx
    L[idx[0]] = rd(os.path.join(w, 'frag', 'current1136.txt'))
    return '\n'.join(L)


def log(s):
    add = rd(os.path.join(w, 'log_lines.md')) + '\n\n' + rd(os.path.join(w, 'log_merge.md')) + '\n'
    return s.rstrip('\n') + '\n\n' + add


edit(os.path.join(root, 'docs', 'README.md'), readme)
edit(os.path.join(root, 'docs', 'CURRENT.md'), current)
edit(os.path.join(root, 'docs', '202609', '99_実装ログ_20260903.md'), log)
print('ok')
