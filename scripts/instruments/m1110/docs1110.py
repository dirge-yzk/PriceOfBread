# (1110) 合流：00一覧・実装ログ・CURRENT へ書く（(1107) c106dc の新しい置き場に合わせて新しく書いた・旧パス〔書庫〕へは追記しない）。バイトで読み書き。
#   python -X utf8 docs1110.py <木> <作業フォルダ>   （作業フォルダに one_rows.md・log_lines.md・log_merge.md・current_row.txt）
import sys
T, W = sys.argv[1:3]
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def piece(name):
    t = rd(W + '/one_rows.md')
    k = t.index('@@' + name + '\n') + len('@@' + name + '\n')
    e = t.find('\n@@', k)
    return t[k:] if e < 0 else t[k:e]
# --- 00一覧 ---
p = T + '/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(p)
assert '\r' not in s
L = s.split('\n')
def find(prefix):
    h = [i for i, l in enumerate(L) if l.startswith(prefix)]
    assert len(h) == 1, (prefix, len(h))
    return h[0]
i = find('| **★(1103) 合流（波105')
L.insert(i + 1, piece('MERGEROW').rstrip('\n'))
i = find('| **次の波（波106〜・(1103) の推奨')
old = L[i]
a = old.index('| 基準は **m1103**') + 2
tail = ' | 実装ログ (1097)〜(1103) |'
assert old.endswith(tail)
body = old[a:len(old) - len(tail)]
L[i] = piece('OLDNEXT').rstrip('\n')
L.insert(i + 1, piece('NEXTHEAD').rstrip('\n') + body + ' | 実装ログ (1103)〜(1110) |')
i = find('| **★(1103) 作者に聞く物')
L.insert(i, piece('ASK').rstrip('\n'))
i = find('| **採用率の表（★(685)')
key = '（★(1103)）。採らなかった物の多くは'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1103)）' + piece('RATE').strip('\n') + '。採らなかった物の多くは')
wr(p, '\n'.join(L))
# --- 実装ログ（当月の回だけのファイルの末尾へ） ---
p = T + '/docs/202609/99_実装ログ_20260903.md'
b = open(p, 'rb').read()
add = (rd(W + '/log_lines.md').rstrip('\n') + '\n\n' + rd(W + '/log_merge.md').rstrip('\n') + '\n').encode('utf-8')
if not b.endswith(b'\n'): b += b'\n'
if not b.endswith(b'\n\n'): b += b'\n'
open(p, 'wb').write(b + add)
# --- CURRENT（最終更新を最上段へ・前の更新は 3 本・あふれた行は書庫の見出しの次へ） ---
p = T + '/docs/CURRENT.md'; pa = T + '/docs/CURRENT_書庫.md'
s = rd(p); L = s.split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** |')]
assert len(i) == 1; i = i[0]
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1103)） |', 1)
L.insert(i, rd(W + '/current_row.txt').rstrip('\n'))
prev = [k for k, l in enumerate(L) if l.startswith('| 前の更新（(')]
assert len(prev) == 4, len(prev)
moved = L.pop(prev[-1])
wr(p, '\n'.join(L))
sa = open(pa, 'rb').read()
la = sa.split(b'\n')
assert la[0].startswith('# CURRENT の書庫'.encode('utf-8')) and la[1] == b''
la.insert(2, moved.encode('utf-8'))
open(pa, 'wb').write(b'\n'.join(la))
print('ok', moved[:40])
