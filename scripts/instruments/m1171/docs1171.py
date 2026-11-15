# (1171) 合流（m1164/docs1164.py の型・リファクタの候補の行に波115 の WriterRatchetTests の取り直し〔線 sv・★(749)〕を足す）：00一覧・実装ログ・CURRENT・README へ断片を入れる（バイトで読み書き・CR の数を保つ）
#   python -X utf8 -I docs1171.py <リポジトリの木> <断片のフォルダ> <置き換えの tsv（鍵 TAB 値）>
#   断片：row1171.txt・next116_head.txt・q1171.txt・adopt1171.txt・log1171.md・current1171.txt・readme1171.txt。本文の {鍵} を置き換えの tsv の値へ。断片の CR は落とす。
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
i = find1(L, lambda l: l.startswith('| **★(1164) 合流（波114'), '1164 row'); L.insert(i + 1, rd('row1171.txt'))
i = find1(L, lambda l: l.startswith('| **次の波（波115〜・(1164) の推奨'), 'next115')
parts = L[i].split(' | '); assert len(parts) == 3, len(parts)
body = parts[1]
L[i] = ('| ~~**次の波（波115〜・(1164) の推奨・AI・作者の承認は未確認）**~~〔2026-11-16 ★(1171) で波115 完了＝挙動 1/2（sv を採った＝新しい基線 m1171・cr は紙の no-go＝3 回の規則で段 C′ を外した）・'
        '恒等 2/2（c115qc・c115sb 入れた＝段 1 で先に push）・読む 1（c115lg）・設計の材料 1（c115td）〕 | 中身は ★(1171) の行と下の〈次の波（波116〜）〉へ運んだ | 実装ログ (1164) |')
L.insert(i + 1, rd('next116_head.txt') + body + ' | 実装ログ (1165)〜(1171) |')
i = find1(L, lambda l: l.startswith('| **★(1164) 作者に聞く物'), 'q1164'); L.insert(i + 1, rd('q1171.txt'))
i = find1(L, lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1164)）。'; assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1164)） → ' + rd('adopt1171.txt') + '。')
i = find1(L, lambda l: l.startswith('| **〈(3) リファクタの候補〉'), 'refactor')
key = '合わせた木 d05c6bea9 では取り直さず緑'; assert L[i].count(key) == 1
L[i] = L[i].replace(key, key + '・波115（挙動の線 c115sv の旗 1 の木 bea18d4f5）：ClaimProducers 1,796→1,938・DeadProdBankrupt 2,334→4,476（+92%）・DiploHegemonScans 3,362,000→2,225,644（−34%）＝線の担当が取り直した（★(749)・予言〈±2% の内〉は外れた）・合わせた木 dcdd69b51 では取り直さず緑')
save(p, '\n'.join(L), bom, cr)
p = os.path.join(T, 'docs/202609/99_実装ログ_20260903.md')
s, bom, cr = load(p)
s = s.rstrip('\n') + '\n\n' + rd('log1171.md') + '\n'
save(p, s, bom, cr)
# CURRENT（最終更新を差し替え・前の更新は 3 本まで・超えた分は書庫の先頭へ）
p = os.path.join(T, 'docs/CURRENT.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('| ★ **最終更新（main へ合流済み）**'), 'cur')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1164)）', 1)
L.insert(i, rd('current1171.txt'))
prev = [j for j, l in enumerate(L) if l.startswith('| 前の更新（(')]
move = prev[3:]
moved = [L[j] for j in move]
for j in reversed(move): del L[j]
save(p, '\n'.join(L), bom, cr)
p2 = os.path.join(T, 'docs/CURRENT_書庫.md'); s2, bom2, cr2 = load(p2); L2 = s2.split('\n')
k = next(j for j, l in enumerate(L2) if l.startswith('| 前の更新（('))
for m in reversed(moved): L2.insert(k, m)
save(p2, '\n'.join(L2), bom2, cr2)
# README（★(1164) の行の後へ）
p = os.path.join(T, 'docs/README.md'); s, bom, cr = load(p); L = s.split('\n')
i = find1(L, lambda l: l.startswith('- ★(1164)：'), 'readme1164'); L.insert(i + 1, rd('readme1171.txt'))
save(p, '\n'.join(L), bom, cr)
print('ok moved', len(moved))
