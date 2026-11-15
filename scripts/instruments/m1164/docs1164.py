# (1164) 合流（m1157/docs1157.py の型・リファクタの候補の行は触らない＝WriterRatchetTests を取り直していない）：00一覧・実装ログ・CURRENT・README へ断片を入れる（バイトで読み書き・CR の数と BOM を保つ）。
#   python -X utf8 -I docs1164.py <リポジトリの木> <断片のフォルダ> <置き換えの tsv（鍵\t値）>
#   断片：row1164.txt・next115_head.txt・q1164.txt・adopt1164.txt・log_lines.md・log1164.md・current1164.txt・readme1164.txt。本文の {鍵} を置き換えの tsv の値へ。断片の CR は落とす。
import os, sys

T, frag, subs = sys.argv[1:4]
SUB = {}
for ln in open(subs, encoding='utf-8'):
    ln = ln.rstrip('\n').rstrip('\r')
    if ln and '\t' in ln:
        k, v = ln.split('\t', 1); SUB[k] = v


def rd(n):
    s = open(os.path.join(frag, n), 'rb').read().decode('utf-8').replace('\r', '').strip('\n')
    for k, v in SUB.items(): s = s.replace('{' + k + '}', v)
    for k in SUB: assert ('{' + k + '}') not in s, (n, k)
    import re
    left = re.findall(r'\{[A-Z0-9_]+\}', s)
    assert not left, (n, left[:5])
    return s


def load(p):
    b = open(p, 'rb').read(); bom = b.startswith(b'\xef\xbb\xbf')
    s = (b[3:] if bom else b).decode('utf-8'); return s, bom, s.count('\r')


def save(p, s, bom, cr):
    assert s.count('\r') == cr, (p, s.count('\r'), cr)
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


def find1(L, pred, what):
    idx = [i for i, l in enumerate(L) if pred(l)]
    assert len(idx) == 1, (what, idx)
    return idx[0]

# 00一覧
p = os.path.join(T, 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md')
s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('| **★(1157) 合流（波113'), '1157 row'); L.insert(i + 1, rd('row1164.txt'))
i = find1(L, lambda l: l.startswith('| **次の波（波114〜・(1157) の推奨'), 'next114')
parts = L[i].split(' | '); assert len(parts) == 3, len(parts)
body = parts[1]
L[i] = ('| ~~**次の波（波114〜・(1157) の推奨・AI・作者の承認は未確認）**~~〔2026-11-16 ★(1164) で波114 完了＝挙動 1/2（qn を採った＝新しい基線 m1164・t1 は段 T1-0 だけ恒等として入れた＝旗 1 と腕 B は回していない）・'
        '恒等 1/1（c114sr 入れた）・読む 2（c114s22・c114k1）・設計の材料 1（c114c3）〕 | 中身は ★(1164) の行と下の〈次の波（波115〜）〉へ運んだ | 実装ログ (1157) |')
L.insert(i + 1, rd('next115_head.txt') + body + ' | 実装ログ (1158)〜(1164) |')
i = find1(L, lambda l: l.startswith('| **★(1157) 作者に聞く物'), 'q1157'); L.insert(i + 1, rd('q1164.txt'))
i = find1(L, lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1157)）。'; assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1157)） → ' + rd('adopt1164.txt') + '。')
save(p, '\n'.join(L), bom, cr)
# 実装ログ（末尾に足す）
p = os.path.join(T, 'docs/202609/99_実装ログ_20260903.md')
s, bom, cr = load(p)
s = s.rstrip('\n') + '\n' + rd('log_lines.md') + '\n\n' + rd('log1164.md') + '\n'
save(p, s, bom, cr)
# CURRENT（最終更新を差し替え・前の更新は 3 本まで・超えた分は書庫の先頭へ）
p = os.path.join(T, 'docs/CURRENT.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('| ★ **最終更新（main へ合流済み）**'), 'cur')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1157)）', 1)
L.insert(i, rd('current1164.txt'))
prev = [j for j, l in enumerate(L) if l.startswith('| 前の更新（(')]
move = prev[3:]
moved = [L[j] for j in move]
for j in reversed(move): del L[j]
save(p, '\n'.join(L), bom, cr)
p2 = os.path.join(T, 'docs/CURRENT_書庫.md'); s2, bom2, cr2 = load(p2); L2 = s2.split('\n')
k = next(j for j, l in enumerate(L2) if l.startswith('| 前の更新（('))
for m in reversed(moved): L2.insert(k, m)
save(p2, '\n'.join(L2), bom2, cr2)
# README（★(1157) の行の後へ）
p = os.path.join(T, 'docs/README.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('- ★(1157)：'), 'readme1157'); L.insert(i + 1, rd('readme1164.txt'))
save(p, '\n'.join(L), bom, cr)
print('ok moved', len(moved))
