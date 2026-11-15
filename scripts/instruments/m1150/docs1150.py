# (1150) 合流（m1143/docs1143_00.py の型）：00一覧・実装ログ・CURRENT・README へ断片を入れる（バイトで読み書き・CR の数と BOM を保つ）。
#   python -X utf8 -I docs1150.py <リポジトリの木> <断片のフォルダ> <置き換えの tsv（鍵\t値）>
#   断片：row1150.txt・next113_head.txt・q1150.txt・adopt1150.txt・log1150.md・current1150.txt・readme1150.txt。本文の {鍵} を置き換えの tsv の値へ。
import os, sys

T, frag, subs = sys.argv[1:4]
SUB = {}
for ln in open(subs, encoding='utf-8'):
    ln = ln.rstrip('\n')
    if ln and '\t' in ln:
        k, v = ln.split('\t', 1); SUB[k] = v


def rd(n):
    s = open(os.path.join(frag, n), 'rb').read().decode('utf-8').strip('\n')
    for k, v in SUB.items(): s = s.replace('{' + k + '}', v)
    assert '{' not in s.replace('{}', '') or all(('{' + k + '}') not in s for k in SUB), n
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
i = find1(L, lambda l: l.startswith('| **★(1143) 合流（波111'), '1143 row'); L.insert(i + 1, rd('row1150.txt'))
i = find1(L, lambda l: l.startswith('| **次の波（波112〜・(1143) の推奨'), 'next112')
parts = L[i].split(' | '); assert len(parts) == 3, len(parts)
body = parts[1]
L[i] = ('| ~~**次の波（波112〜・(1143) の推奨・AI・作者の承認は未確認）**~~〔2026-11-15 ★(1150) で波112 完了＝挙動 1/2（hy を採った＝新しい基線 m1150・t1 は外した）・'
        '恒等 2/2（c112tt・c112cm 入れた）・読む 1（c112s26）・設計の材料 1（c112il）〕 | 中身は ★(1150) の行と下の〈次の波（波113〜）〉へ運んだ | 実装ログ (1143) |')
L.insert(i + 1, rd('next113_head.txt') + body + ' | 実装ログ (1144)〜(1150) |')
i = find1(L, lambda l: l.startswith('| **★(1143) 作者に聞く物'), 'q1143'); L.insert(i + 1, rd('q1150.txt'))
i = find1(L, lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1143)）。'; assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1143)） → ' + rd('adopt1150.txt') + '。')
i = find1(L, lambda l: l.startswith('| **〈(3) リファクタの候補〉'), 'refactor')
key = 'item 34 は合わせた木で許しの内〕）＝挙動の線のたびに'; assert L[i].count(key) == 1
L[i] = L[i].replace(key, 'item 34 は合わせた木で許しの内〕〔★(1150) 波112（挙動の線 c112hy の旗 1 の木 a41503e6f）：ConstructionSeen 2,134→2,195・DeadProdBankrupt 2,169→2,334・DiploHegemonScans 2,817,356→3,362,000（+19.3%）＝線の担当が取り直した（★(749)）・合わせた木 d05c6bea9 では取り直さず緑〕）＝挙動の線のたびに')
save(p, '\n'.join(L), bom, cr)
# 実装ログ（末尾に足す）
p = os.path.join(T, 'docs/202609/99_実装ログ_20260903.md')
s, bom, cr = load(p)
s = s.rstrip('\n') + '\n' + rd('log1150.md') + '\n'
save(p, s, bom, cr)
# CURRENT（最終更新を差し替え・前の更新は 3 本まで・超えた分は書庫の先頭へ）
p = os.path.join(T, 'docs/CURRENT.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('| ★ **最終更新（main へ合流済み）**'), 'cur')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1143)）', 1)
L.insert(i, rd('current1150.txt'))
prev = [j for j, l in enumerate(L) if l.startswith('| 前の更新（(')]
move = prev[3:]
moved = [L[j] for j in move]
for j in reversed(move): del L[j]
save(p, '\n'.join(L), bom, cr)
p2 = os.path.join(T, 'docs/CURRENT_書庫.md'); s2, bom2, cr2 = load(p2); L2 = s2.split('\n')
k = next(j for j, l in enumerate(L2) if l.startswith('| 前の更新（('))
for m in reversed(moved): L2.insert(k, m)
save(p2, '\n'.join(L2), bom2, cr2)
# README（★(1143) の行の後へ）
p = os.path.join(T, 'docs/README.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('- ★(1143)：'), 'readme1143'); L.insert(i + 1, rd('readme1150.txt'))
save(p, '\n'.join(L), bom, cr)
print('ok moved', len(moved))
