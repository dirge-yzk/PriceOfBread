# (1060) 合流の docs の書き足し（m1053/docs1053.py の役・バイトで読み書き＝行末と BOM を保つ）
#   python -X utf8 docs1060.py
R = 'C:/Users/dirge/Documents/PriceOfBread/'
W = 'D:/PoB_work/m1060_work/'
def rd(p): return open(R + p, 'rb').read()
def wr(p, b): open(R + p, 'wb').write(b)

# 1) 実装ログ：線の段 (1054)〜(1059) と合流 (1060) を末尾へ
L = 'docs/202609/99_実装ログ_20260903.md'
b = rd(L)
add = open(W + 'log_lines.md', 'rb').read().replace(b'\r\n', b'\n') + open(W + 'log_1060.md', 'rb').read().replace(b'\r\n', b'\n')
if not b.endswith(b'\n'): b += b'\n'
wr(L, b + b'\n' + add.lstrip(b'\n'))

# 2) 00一覧
F = 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(F).decode('utf-8')
assert '\r\n' not in s
lines = s.split('\n')
def idx(prefix):
    k = [i for i, l in enumerate(lines) if l.startswith(prefix)]
    assert len(k) == 1, (prefix, len(k)); return k[0]
t = {}
for blk in open(W + 'rows1060.txt', 'rb').read().decode('utf-8').replace('\r\n', '\n').split('\n@@'):
    blk = blk.strip('\n')
    if not blk: continue
    k, v = blk.split('\n', 1)
    t[k.strip('@ ')] = v.strip('\n')
# 2a) ★(1060) の合流の行（★(1053) の合流の行の後）
i = idx('| **★(1053) 合流（波99・挙動の線 c99db を採った＝新しい基線 m1053）**')
lines.insert(i + 1, t['merge'])
# 2b) 次の波：波100 の行を畳み、波101 の行を足す
i = idx('| **次の波（波100〜・(1053) の推奨・AI・作者の承認は未確認）**')
lines[i] = t['fold']
lines.insert(i + 1, t['next'])
# 2c) §0-2 ★(1060) 作者に聞く物（★(1053) の行の前）
i = idx('| **★(1053) 作者に聞く物（波99 の合流・本当に迷う物だけ・推奨つき）**')
lines.insert(i, t['ask'])
# 2d) §0-W ★(1060)（★(1053) の英の行の前）
i = idx('| **★(1053) 英が 10 種とも矯正講和で政体を押し付けられる')
lines.insert(i, t['watch'])
# 2e) 採用率の表に 1 列
i = idx('| **採用率の表（★(685)・RULE45 の診断を数で残す）**')
old = '（★(1053)）。採らなかった物の多くは'
assert lines[i].count(old) == 1
lines[i] = lines[i].replace(old, '（★(1053)）・' + t['adopt'] + '。採らなかった物の多くは')
wr(F, '\n'.join(lines).encode('utf-8'))

# 3) CURRENT：最終更新の行を足し、前の行を〈前の更新〉へ
C = 'docs/CURRENT.md'
s = rd(C).decode('utf-8'); lines = s.split('\n')
i = idx('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-12（(1053) 合流（波99）')
lines[i] = lines[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1053)） |', 1)
lines.insert(i, t['current'])
wr(C, '\n'.join(lines).encode('utf-8'))

# 4) README：★(1060) の教訓（★(1053) の行の後）
M = 'docs/README.md'
s = rd(M).decode('utf-8'); lines = s.split('\n')
i = idx('- ★(1053)：')
lines.insert(i + 1, t['readme'])
wr(M, '\n'.join(lines).encode('utf-8'))
print('ok')
