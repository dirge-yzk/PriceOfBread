# (994) 合流（docs988.py の写し・名前を m994 へ・線の段 docs994_lines.md を合流の段の前に足す・README の基線の行は m988 のままなので触らない）
#   python -X utf8 docs994.py <リポジトリの根> <値の表 key=value の txt>
#   docs994_*.md と docs994_small.txt の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
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
SM = {k: fill(v, k) for k, v in kv(os.path.join(H, 'docs994_small.txt')).items()}
def one(L, pre):
    i = [k for k, l in enumerate(L) if l.startswith(pre)]; assert len(i) == 1, (pre[:40], len(i)); return i[0]
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs994_lines.md') + '\n\n' + txt('docs994_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = one(L, '| **★(988) 合流（波89')
L[i + 1:i + 1] = txt('docs994_rows.md').split('\n')
i = one(L, '| **次の波（波90〜・(988) の推奨・AI・作者の承認は未確認）**')
L[i] = '| ~~**次の波（波90〜・(988) の推奨・AI・作者の承認は未確認）**~~〔2026-11-09 ★(994) で波90 完了＝c90tr・c90px は入れた・c90sl は採らず旗 0 の器だけ入れた・c90bc は docs へ・c90si は measurements へ〕 | 〔畳んだ〕 | 実装ログ (988) |'
L[i + 1:i + 1] = [txt('docs994_next.md')]
i = one(L, '| **★(988) 作者に聞く物')
L[i:i] = [txt('docs994_ask.md')]
i = one(L, '| **採用率の表')
a = '（★(988)）。'; assert L[i].count(a) == 1
L[i] = L[i].replace(a, '（★(988)） → ' + SM['RATE'] + '（★(994)）。')
i = one(L, '| **★(988) 新しいヌル腕 N2 のシャム')
L[i + 1:i + 1] = [SM['W']]
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = one(L, '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-09（(988) 合流')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(988)） |', 1)
L[i:i] = [SM['CURRENT']]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = one(L, '- ★(988)：')
L[i + 1:i + 1] = [SM['README']]
wr(p, '\n'.join(L))
p = T + '/機能の台帳.md'
L = rd(p).split('\n')
i = one(L, '- S07-P02 の注（(988)）')
L[i + 1:i + 1] = [SM['LEDGER']]
wr(p, '\n'.join(L))
print('ok')
