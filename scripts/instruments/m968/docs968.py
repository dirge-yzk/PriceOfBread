# (968) 合流：生きた文書（実装ログ・00一覧・CURRENT・README）へ波86 の記録を書く（バイトで読み書き・行を消さない・docs961.py の写し・錨を 1 つ先へ）。
#   python -X utf8 docs968.py <リポジトリの根> <値の表 key=value の txt>
#   docs968_*.md の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
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
wr(p, s + '\n' + txt('docs968_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(961) 合流（波85')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = txt('docs968_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波86〜')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs968_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(961) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs968_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表（★(685)')]; assert len(i) == 1
a = '（★(961)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(961)） → ' + txt('docs968_rate.md') + '（★(968)）。')
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-08（(961) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(961)） |', 1)
L[i[0]:i[0]] = [txt('docs968_current.md')]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(961)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs968_readme.md')]
i = [k for k, l in enumerate(L) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261108_m961`＝')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('（今は `measurements/b5_20261108_m961`＝', '（今は `measurements/b5_20261108_m968`＝' + V['README_BASE'] + '・元は ', 1)
wr(p, '\n'.join(L))
print('ok')
