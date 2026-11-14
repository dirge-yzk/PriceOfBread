# (1143) 合流（m1136 の同名の台本の写し）：00一覧に ★(1143) の行・〈次の波（波112〜）〉・§0-2 ★(1143)・採用率の列を足す（バイトで読み書き・CR の数を保つ）。
#   python -X utf8 -I docs1143_00.py <00一覧のパス> <本文の断片のフォルダ>
#   断片のフォルダ：row1143.txt（★(1143) の行）・next112_head.txt（〈次の波（波112〜）〉の行の頭＝セル 1 とセル 2 の頭）・q1143.txt（§0-2 の行）・adopt1143.txt（採用率に足す文）
import os, sys

p, frag = sys.argv[1:3]
b = open(p, 'rb').read()
s = b.decode('utf-8')
cr = s.count('\r')
L = s.split('\n')


def rd(n):
    return open(os.path.join(frag, n), 'rb').read().decode('utf-8').strip('\n')


def find1(pred, what):
    idx = [i for i, l in enumerate(L) if pred(l)]
    assert len(idx) == 1, (what, idx)
    return idx[0]


# 1) ★(1143) の行を ★(1136) の行の後へ
i = find1(lambda l: l.startswith('| **★(1136) 合流（波110'), '1136 row')
L.insert(i + 1, rd('row1143.txt'))
# 2) 〈次の波（波111〜）〉の行を打ち消して 1 行に畳み、本文を新しい行の後ろへ運ぶ
i = find1(lambda l: l.startswith('| **次の波（波111〜・(1136) の推奨'), 'next111')
old = L[i]
parts = old.split(' | ')
assert len(parts) == 3, len(parts)
body = parts[1]
tail = parts[2]
fold = ('| ~~**次の波（波111〜・(1136) の推奨・AI・作者の承認は未確認）**~~〔2026-11-15 ★(1143) で波111 完了＝挙動 3/3（rb・tp・lo の載せ直しを採った＝新しい基線 m1143）・'
        '恒等 2/2（c111tf・c111sm 入れた）・読む 1（c111s25）・設計の材料 1（c111cr）〕 | 中身は ★(1143) の行と下の〈次の波（波112〜）〉へ運んだ | 実装ログ (1136) |')
head = rd('next112_head.txt')
new = head + body + ' | 実装ログ (1137)〜(1143) |'
L[i] = fold
L.insert(i + 1, new)
# 3) §0-2 の ★(1143) を ★(1136) の後へ
i = find1(lambda l: l.startswith('| **★(1136) 作者に聞く物'), 'q1136')
L.insert(i + 1, rd('q1143.txt'))
# 4) 採用率の表に波111 の列
i = find1(lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1136)）。'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1136)） → ' + rd('adopt1143.txt') + '。')
s2 = '\n'.join(L)
assert s2.count('\r') == cr, (s2.count('\r'), cr)
open(p, 'wb').write(s2.encode('utf-8'))
print('ok', len(L))
