# (1219) 合流：00一覧・実装ログ・CURRENT・README へ (1213)〜(1219) の行を入れる（バイトで読み書き・CR の数を保つ）。
#   python -X utf8 -I scripts/instruments/m1219/docs1219.py <木の根> <フルスイートの文> <待ちの回数>
import sys
R, SUITE, WAITS = sys.argv[1:4]
W = 'D:/PoB_work/m1219_work/'
def rd(p): return open(p, 'rb').read().decode('utf-8').strip('\n')
def fill(s): return s.replace('@@SUITE@@', SUITE).replace('@@WAITS@@', WAITS).replace('@@BASE@@', '782ca9375').replace('@@STAGE@@', '7f9e4fde3')
def load(p):
    b = open(p, 'rb').read(); nl = '\r\n' if b.count(b'\r\n') > b.count(b'\n') // 2 else '\n'
    return b.decode('utf-8').split(nl), nl
def save(p, L, nl): open(p, 'wb').write(nl.join(L).encode('utf-8'))
def idx(L, head):
    h = [i for i, l in enumerate(L) if l.startswith(head)]; assert len(h) == 1, (head, len(h)); return h[0]

# 00一覧
p = R + '/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'; L, nl = load(p)
i = idx(L, '| **★(1212) 合流（波121'); L.insert(i + 1, fill(rd(W + 'merge_row.txt')))
i = idx(L, '| **次の波（波122〜・(1212) の推奨')
old = L[i]; head_end = old.index(' | ') + 3; tail_start = old.rindex(' | ')
body = old[head_end:tail_start]
L[i] = fill(rd(W + 'next_wave.txt')) + body + ' | 実装ログ (1213)〜(1219) |'
L.insert(i, '| ~~**次の波（波122〜・(1212) の推奨・AI・作者の承認は未確認）**~~〔2026-11-19 ★(1219) で波122 完了＝挙動 0/2（c122bd は ㉜ で止めた・c122nr は反証役 v2 の高で入れない）・恒等 2/2（c122ts〔T2 だけ〕・c122cr）・読む 1（c122rs）・設計の材料 1（c122r2）〕 | 中身は ★(1219) の行と下の〈次の波（波123〜）〉へ運んだ | 実装ログ (1212) |')
i = idx(L, '| **★(1212) 作者に聞く物（波121')
L[i] = L[i].replace('| **★(1212) 作者に聞く物（波121 の合流・本当に迷う物だけ・推奨つき）** |', '| ~~**★(1212) 作者に聞く物（波121 の合流・本当に迷う物だけ・推奨つき）**~~〔2026-11-19 ① の A は c122cr で消した＝確認待ちは ★(1219) へ運んだ〕 |', 1)
L.insert(i + 1, fill(rd(W + 'ask_row.txt')))
i = idx(L, '| **採用率の表（★(685)')
k = '（★(1212)）。採らなかった物の多くは'
assert L[i].count(k) == 1
L[i] = L[i].replace(k, '（★(1212)）。**波122 挙動 0/2（c122bd〔英の国債の不履行の入口〕は旗 1 で旧の規則を固定する番人が赤＝㉜ で腕 B なし・c122nr〔民族主義の根〕は門を通ったが反証役 v2 の高〔戦争の数が史実の桁の線を越える〕で入れない＝3 回の規則の 1 回目）・恒等 2/2（c122ts〔T2 だけ〕・c122cr）・読む 1（c122rs）・設計の材料 1（c122r2）**（★(1219)）。採らなかった物の多くは')
save(p, L, nl); print('00一覧')

# 実装ログ
p = R + '/docs/202609/99_実装ログ_20260903.md'; L, nl = load(p)
while L and L[-1] == '': L.pop()
L += fill(rd(W + 'log_lines.md')).split('\n') + fill(rd(W + 'log_merge.md')).split('\n') + ['']
save(p, L, nl); print('実装ログ')

# CURRENT
p = R + '/docs/CURRENT.md'; L, nl = load(p)
i = idx(L, '| ★ **最終更新（main へ合流済み）**')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1212)） |', 1)
L.insert(i, fill(rd(W + 'current_row.txt')))
save(p, L, nl); print('CURRENT')

# README
p = R + '/docs/README.md'; L, nl = load(p)
i = idx(L, '- ★(1212)：')
L.insert(i + 1, rd(W + 'readme_row.txt'))
save(p, L, nl); print('README')
