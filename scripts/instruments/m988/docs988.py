# (988) 合流（docs981.py の写し・名前を m988 へ・小さな差し込みは docs988_small.txt の key=value に寄せた）
#   python -X utf8 docs988.py <リポジトリの根> <値の表 key=value の txt>
#   docs988_*.md と docs988_small.txt の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
import os, sys, re
R = sys.argv[1]; T = R + '/docs'
H = os.path.dirname(os.path.abspath(__file__))
def kv(p): return dict(l.split('=', 1) for l in open(p, 'rb').read().decode('utf-8').split('\n') if '=' in l)
V = kv(sys.argv[2])
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def fill(s, name):
    for k, v in V.items(): s = s.replace('{' + k + '}', v)
    left = re.findall(r'\{[A-Z_0-9]+\}', s); assert not left, (name, left)
    return s
def txt(name): return fill(rd(os.path.join(H, name)).rstrip('\n'), name)
SM = {k: fill(v, k) for k, v in kv(os.path.join(H, 'docs988_small.txt')).items()}
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs988_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(981) 合流（波88')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = txt('docs988_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波89〜')]; assert len(i) == 1
a = '| **次の波（波89〜・(981) の推奨・AI・作者の承認は未確認）**'; assert L[i[0]].startswith(a)
L[i[0]] = '| ~~**次の波（波89〜・(981) の推奨・AI・作者の承認は未確認）**~~〔2026-11-09 ★(988) で波89 完了＝c89tb・c89ls・c89dd は入れた・c89tc は docs へ・c89sd・c89ci は measurements へ〕' + L[i[0]][len(a):]
L[i[0] + 1:i[0] + 1] = [txt('docs988_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(981) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs988_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表')]; assert len(i) == 1
a = '（★(981)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(981)） → ' + SM['RATE'] + '（★(988)）。')
i = [k for k, l in enumerate(L) if l.startswith('| **★(784) m784 の値')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [SM['W']]
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-09（(981) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(981)） |', 1)
L[i[0]:i[0]] = [SM['CURRENT']]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(981)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [SM['README']]
i = [k for k, l in enumerate(L) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261109_m981`＝')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('（今は `measurements/b5_20261109_m981`＝', '（今は `measurements/b5_20261109_m988`＝' + SM['README_BASE'] + '・元は ', 1)
wr(p, '\n'.join(L))
p = T + '/機能の台帳.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- S05-H023・S05-P01・S05-P02 の注（(981)）')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [SM['LEDGER']]
wr(p, '\n'.join(L))
print('ok')
