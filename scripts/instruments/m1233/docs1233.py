# (1233) 合流の docs の書き込み（m1226/docs1226.py の型・バイトで読み書き＝LF と BOM を保つ）。
#   python -X utf8 -I docs1233.py <リポジトリ> <下書きのフォルダ>
#   下書き：log_lines.md（(1227)〜(1232)）・log_1233.md（(1233)）・next_wave.txt（次の波の節）・misc_1233.txt（@@README・@@CURRENT・@@ADOPT・@@Q・@@ROW・@@HEAD・@@TAIL）
import sys
R, W = sys.argv[1], sys.argv[2]

def rd(p):
    return open(p, 'rb').read().decode('utf-8').replace('\r\n', '\n')

def sections(s):
    out = {}; cur = None
    for l in s.split('\n'):
        if l.startswith('@@'):
            cur = l[2:].strip(); out[cur] = []
        elif cur is not None:
            out[cur].append(l)
    return {k: '\n'.join(v).strip('\n') for k, v in out.items()}

mis = sections(rd(f'{W}/misc_1233.txt'))

# 実装ログ
p = f'{R}/docs/202609/99_実装ログ_20260903.md'
b = open(p, 'rb').read()
add = rd(f'{W}/log_lines.md').strip('\n') + '\n\n' + rd(f'{W}/log_1233.md').strip('\n') + '\n'
if not b.endswith(b'\n'): b += b'\n'
open(p, 'wb').write(b + b'\n' + add.encode('utf-8'))

# 00一覧
p = f'{R}/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
def find(prefix):
    ix = [i for i, l in enumerate(L) if l.startswith(prefix)]
    assert len(ix) == 1, (prefix, len(ix)); return ix[0]
# ★(1233) の行（★(1226) の行の次）
i = find('| **★(1226) 合流（波123')
L.insert(i + 1, mis['ROW'])
# 次の波
i = find('| **次の波（波124〜・(1226) の推奨')
old = L[i]; parts = old.split(' | ')
assert len(parts) == 3, len(parts)
struck = ('| ~~**次の波（波124〜・(1226) の推奨・AI・作者の承認は未確認）**~~〔2026-11-19 ★(1233) で波124 完了＝挙動 1/3（c124bk を採った＝新しい基線 m1233・c124pf は段 0 で記録だけ・c124nr は H3 が赤＝3 回の規則の 2 回目）・'
          '恒等 1/1（c124kb）・読む 1（c124rd）・設計の材料 1（c124ef）〕 | 中身は ★(1233) の行と下の〈次の波（波125〜）〉へ運んだ | 実装ログ (1227)〜(1233) |')
nw = rd(f'{W}/next_wave.txt').strip('\n').split('\n')
new = mis['HEAD'] + '<br>' + '<br>'.join(nw) + '<br>' + mis['TAIL'] + '<br>' + parts[1] + ' | 実装ログ (1227)〜(1233) |'
L[i] = struck
L.insert(i + 1, new)
# §0-2
i = find('| **★(1226) 作者に聞く物（波123 の合流')
L[i] = L[i].replace('| **★(1226) 作者に聞く物（波123 の合流・本当に迷う物だけ・推奨つき）**',
                    '| ~~**★(1226) 作者に聞く物（波123 の合流・本当に迷う物だけ・推奨つき）**~~〔2026-11-19 ① は B-1 の旗 1 の 2 回目の段 0 へ運ぶ・② は (1232) で材料が出た〔甲は反証役 v の高で晴れていない〕・③ は ★(1233) ④ で keep_1219_rej を消した〕', 1)
L.insert(i + 1, mis['Q'])
# 採用率の表
i = find('| **採用率の表（★(685)')
key = '採らなかった物の多くは 5 テーマの繰り返し'
assert L[i].count(key) == 1
L[i] = L[i].replace(key, mis['ADOPT'] + key, 1)
open(p, 'wb').write('\n'.join(L).encode('utf-8'))

# CURRENT
p = f'{R}/docs/CURRENT.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
i = find('| ★ **最終更新（main へ合流済み）**')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1226)）', 1)
L.insert(i, mis['CURRENT'])
open(p, 'wb').write('\n'.join(L).encode('utf-8'))

# README（★(1226) の行の次）
p = f'{R}/docs/README.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
i = find('- ★(1226)：')
L.insert(i + 1, mis['README'])
open(p, 'wb').write('\n'.join(L).encode('utf-8'))
print('docs を書いた')
