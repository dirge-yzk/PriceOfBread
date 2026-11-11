# (981) 合流（docs975.py の写し・名前を m981 へ・台帳 §4-4 の注を足した）
# (975) 合流（docs968.py の写し・名前を m975 へ・§0-W の行と前の〈次の波〉の行の打ち消しを足した）
#   python -X utf8 docs975.py <リポジトリの根> <値の表 key=value の txt>
#   docs981_*.md の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
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
wr(p, s + '\n' + txt('docs981_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(975) 合流（波87')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = txt('docs981_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波88〜')]; assert len(i) == 1
a = '| **次の波（波88〜・(975) の推奨・AI・作者の承認は未確認）**'; assert L[i[0]].startswith(a)
L[i[0]] = '| ~~**次の波（波88〜・(975) の推奨・AI・作者の承認は未確認）**~~〔2026-11-09 ★(981) で波88 完了＝c88ts・c88sm・c88cs は入れた・c88rb は docs へ・c88hr は measurements へ〕' + L[i[0]][len(a):]
L[i[0] + 1:i[0] + 1] = [txt('docs981_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(975) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs981_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表')]; assert len(i) == 1
a = '（★(975)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(975)） → ' + txt('docs981_rate.md') + '（★(981)）。')
i = [k for k, l in enumerate(L) if l.startswith('| **★(784) m784 の値')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs981_w.md')]
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-09（(975) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(975)） |', 1)
L[i[0]:i[0]] = [txt('docs981_current.md')]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(975)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs981_readme.md')]
i = [k for k, l in enumerate(L) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261109_m975`＝')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('（今は `measurements/b5_20261109_m975`＝', '（今は `measurements/b5_20261109_m981`＝' + V['README_BASE'] + '・元は ', 1)
wr(p, '\n'.join(L))
p = T + '/機能の台帳.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- S05-H023 の注（(939)）')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs981_ledger.md')]
wr(p, '\n'.join(L))
print('ok')
