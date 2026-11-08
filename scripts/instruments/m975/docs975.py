# (975) 合流（docs968.py の写し・名前を m975 へ・§0-W の行と前の〈次の波〉の行の打ち消しを足した）
#   python -X utf8 docs975.py <リポジトリの根> <値の表 key=value の txt>
#   docs975_*.md の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
import os, sys, re
R = sys.argv[1]; T = R + '/docs'
H = os.path.dirname(os.path.abspath(__file__))
V = dict(l.split('=', 1) for l in open(sys.argv[2], 'rb').read().decode('utf-8').split('\n') if '=' in l)
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def txt(name):
    s = rd(os.path.join(H, name)).rstrip('\n')
    for k, v in V.items(): s = s.replace('{' + k + '}', v)
    left = re.findall(r'\{[A-Z_0-9]+\}', s); assert not left, (name, left)
    return s
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs975_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(968) 合流（波86')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = txt('docs975_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波87〜')]; assert len(i) == 1
a = '| **次の波（波87〜・(968) の推奨・AI・作者の承認は未確認）**'; assert L[i[0]].startswith(a)
L[i[0]] = '| ~~**次の波（波87〜・(968) の推奨・AI・作者の承認は未確認）**~~〔2026-11-09 ★(975) で波87 完了＝c87ot・c87bx・c87mt・c87mw は入れた・c87hd は採らない・c87lm は docs へ〕' + L[i[0]][len(a):]
L[i[0] + 1:i[0] + 1] = [txt('docs975_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(968) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs975_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表')]; assert len(i) == 1
a = '（★(968)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(968)） → ' + txt('docs975_rate.md') + '（★(975)）。')
i = [k for k, l in enumerate(L) if l.startswith('| **★(784) m784 の値')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs975_w.md')]
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-08（(968) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(968)） |', 1)
L[i[0]:i[0]] = [txt('docs975_current.md')]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(968)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs975_readme.md')]
i = [k for k, l in enumerate(L) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261108_m968`＝')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('（今は `measurements/b5_20261108_m968`＝', '（今は `measurements/b5_20261109_m975`＝' + V['README_BASE'] + '・元は ', 1)
wr(p, '\n'.join(L))
print('ok')
