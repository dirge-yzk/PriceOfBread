# (1404) 合流：生きた文書へ書く（実装ログ・00一覧・README・CURRENT）。断片は <断片のフォルダ> の txt（バイトで読み書き・CR と BOM を保つ）。
#   python -X utf8 -I docs1404.py <木> <断片のフォルダ>
import sys, os
R, D = sys.argv[1], sys.argv[2]
def frag(n): return open(os.path.join(D, n), 'rb').read().decode('utf-8').rstrip('\n')
def rw(p, f):
    p = os.path.join(R, p); b = open(p, 'rb').read(); s = b.decode('utf-8'); s2 = f(s)
    if s2 == s: print('skip', p); return
    open(p, 'wb').write(s2.encode('utf-8')); print('ok', p)
L = '\r\n'
def nl(s): return '\r\n' if '\r\n' in s else '\n'
# 00一覧
def f00(s):
    if '★(1404) 合流（波149' in s: return s
    n = nl(s); lines = s.split(n)
    i822 = [i for i, l in enumerate(lines) if l.startswith('| **★(1397) 合流（波148')]; assert len(i822) == 1
    lines.insert(i822[0] + 1, frag('row1404.txt'))
    i891 = [i for i, l in enumerate(lines) if l.startswith('| **次の波（波149〜・(1397)')]; assert len(i891) == 1
    lines[i891[0]] = frag('fold149.txt'); lines.insert(i891[0] + 1, frag('next150.txt'))
    i963 = [i for i, l in enumerate(lines) if l.startswith('| **★(1397) 作者に聞く物')]; assert len(i963) == 1
    lines.insert(i963[0] + 1, frag('q1404.txt'))
    ia = [i for i, l in enumerate(lines) if l.startswith('| **採用率の表')]; assert len(ia) == 1
    a = lines[ia[0]]; k = a.rindex(' | 実装ログ (685)'); lines[ia[0]] = a[:k] + frag('adopt.txt') + a[k:]
    ir = [i for i, l in enumerate(lines) if l.startswith('| **〈(3) リファクタの候補〉')]; assert len(ir) == 1
    r = lines[ir[0]]; k = r.rindex(' | 実装ログ (764) |'); lines[ir[0]] = r[:k] + frag('refac.txt') + r[k:]
    return n.join(lines)
rw('docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md', f00)
# README
def frd(s):
    if '- ★(1404)' in s: return s
    n = nl(s); lines = s.split(n)
    ix = [i for i, l in enumerate(lines) if l.startswith('- ★(1397)：効率化（提案）')]; assert len(ix) == 1
    for j, t in enumerate(frag('readme1404.txt').split('\n')): lines.insert(ix[0] + 1 + j, t)
    return n.join(lines)
rw('docs/README.md', frd)
# 実装ログ
def flog(s):
    if '(1404) 合流（波149' in s: return s
    n = nl(s); t = frag('log_entry.md').replace('\n', n)
    if not s.endswith(n): s += n
    return s + t.lstrip(n) + n
rw('docs/202609/99_実装ログ_20260903.md', flog)
# CURRENT（最終更新の行を最上段に・前の更新は 3 本まで・超えた分は書庫の先頭へ）
cur = os.path.join(R, 'docs/CURRENT.md'); arc = os.path.join(R, 'docs/CURRENT_書庫.md')
s = open(cur, 'rb').read().decode('utf-8'); n = nl(s); lines = s.split(n)
if '(1404) 合流（波149）' in s: print('skip CURRENT'); sys.exit(0)
i0 = [i for i, l in enumerate(lines) if l.startswith('| ★ **最終更新（main へ合流済み）**')]; assert len(i0) == 1
old = lines[i0[0]].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1397)）', 1)
lines[i0[0]] = frag('current.txt'); lines.insert(i0[0] + 1, old)
open(cur, 'wb').write(n.join(lines).encode('utf-8')); print('ok CURRENT')
# 前の更新の行を書庫へ移す段は入れていない（今の CURRENT は前の更新が 30 本＝前の回から移していない・(1404) も前の行に揃えた）
