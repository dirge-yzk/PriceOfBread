# (1136) 合流（m1129/docs1129_00.py の写し）：00一覧に ★(1136) の行・〈次の波（波111〜）〉・§0-2 ★(1136)・採用率の列を足す（バイトで読み書き・CR の数を保つ）。
#   python -X utf8 -I docs1136_00.py <00一覧のパス> <本文の断片のフォルダ>
#   断片のフォルダ：row1136.txt（★(1136) の行）・next111_head.txt（〈次の波（波111〜）〉の行の頭＝セル 1 とセル 2 の頭）・q1136.txt（§0-2 の行）・adopt1136.txt（採用率に足す文）
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


# 1) ★(1136) の行を ★(1129) の行の後へ
i = find1(lambda l: l.startswith('| **★(1129) 合流（波109'), '1129 row')
L.insert(i + 1, rd('row1136.txt'))
# 2) 〈次の波（波110〜）〉の行を打ち消して 1 行に畳み、本文を新しい行の後ろへ運ぶ
i = find1(lambda l: l.startswith('| **次の波（波110〜・(1129) の推奨'), 'next110')
old = L[i]
parts = old.split(' | ')
assert len(parts) == 3, len(parts)
body = parts[1]
tail = parts[2]
fold = ('| ~~**次の波（波110〜・(1129) の推奨・AI・作者の承認は未確認）**~~〔2026-11-14 ★(1136) で波110 完了＝挙動 0/1（c110lo は自分の門を通ったが合わせた木で tb の試験 T1 が赤＝★(961) の型で外した・枝に残す）・'
        '恒等 2/2（c110tb・c110sm 入れた）・読む 2（c110fs・c110s24）・設計の材料 1（c110rn）〕 | 中身は ★(1136) の行と下の〈次の波（波111〜）〉へ運んだ | 実装ログ (1129) |')
head = rd('next111_head.txt')
new = head + body + ' | 実装ログ (1130)〜(1136) |'
L[i] = fold
L.insert(i + 1, new)
# 3) §0-2 の ★(1136) を ★(1129) の後へ
i = find1(lambda l: l.startswith('| **★(1129) 作者に聞く物'), 'q1129')
L.insert(i + 1, rd('q1136.txt'))
# 4) 採用率の表に波110 の列
i = find1(lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1129)）。'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1129)） → ' + rd('adopt1136.txt') + '。')
s2 = '\n'.join(L)
assert s2.count('\r') == cr, (s2.count('\r'), cr)
open(p, 'wb').write(s2.encode('utf-8'))
print('ok', len(L))
