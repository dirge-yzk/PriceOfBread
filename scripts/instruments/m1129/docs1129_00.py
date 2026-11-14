# (1129) 合流：00一覧に ★(1129) の行・〈次の波（波110〜）〉・§0-2 ★(1129)・採用率の列を足す（バイトで読み書き・CR の数を保つ）。
#   python -X utf8 -I docs1129_00.py <00一覧のパス> <本文の断片のフォルダ>
#   断片のフォルダ：row1129.txt（★(1129) の行）・next110_head.txt（〈次の波（波110〜）〉の行の頭＝セル 1 とセル 2 の頭）・q1129.txt（§0-2 の行）・adopt1129.txt（採用率に足す文）
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


# 1) ★(1129) の行を ★(1122) の行の後へ
i = find1(lambda l: l.startswith('| **★(1122) 合流（波108'), '1122 row')
L.insert(i + 1, rd('row1129.txt'))
# 2) 〈次の波（波109〜）〉の行を打ち消して 1 行に畳み、本文を新しい行の後ろへ運ぶ
i = find1(lambda l: l.startswith('| **次の波（波109〜・(1122) の推奨'), 'next109')
old = L[i]
parts = old.split(' | ')
assert len(parts) == 3, len(parts)
body = parts[1]
tail = parts[2]
fold = ('| ~~**次の波（波109〜・(1122) の推奨・AI・作者の承認は未確認）**~~〔2026-11-14 ★(1129) で波109 完了＝挙動 0/1（c109rc は腕 B で自分の門 4 本が赤＝段 C′ の 1 回目・記録だけ）・'
        '恒等 2/2（c109t0・c109rs 入れた）・読む 2（c109mw・c109s05）・設計の材料 1（c109t1）〕 | 中身は ★(1129) の行と下の〈次の波（波110〜）〉へ運んだ | 実装ログ (1122) |')
head = rd('next110_head.txt')
new = head + '<br>**以下は前の行（波109〜）の本文をそのまま運んだ物**（閉じた物＝0 節 ③ A2 の許し〔rs〕・④ rs・1 節 ③ T0a〔t0〕・2 節 の〈最低賃金の施行が後の時期でも半分〉〔mw〕・1 節 ① 段 C′ の 2 回目は〈採らなかった＝1 回目〉へ。上の 0〜3 が並べ方を上書きする・中身は削らない）：<br>' + body + ' | 実装ログ (1122)〜(1129) |'
L[i] = fold
L.insert(i + 1, new)
# 3) §0-2 の ★(1129) を ★(1122) の後へ
i = find1(lambda l: l.startswith('| **★(1122) 作者に聞く物'), 'q1122')
L.insert(i + 1, rd('q1129.txt'))
# 4) 採用率の表に波109 の列
i = find1(lambda l: l.startswith('| **採用率の表（★(685)'), 'adopt')
key = '（★(1122)）。'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, '（★(1122)） → ' + rd('adopt1129.txt') + '。')
s2 = '\n'.join(L)
assert s2.count('\r') == cr, (s2.count('\r'), cr)
open(p, 'wb').write(s2.encode('utf-8'))
print('ok', len(L))
