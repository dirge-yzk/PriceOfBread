# (1006) 合流（docs1001.py の写し・名前を m1006 へ・錨を (1001) の行へ）
#   python -X utf8 docs1006.py <リポジトリの根> <値の表 key=value の txt>
#   docs1006_*.md と docs1006_small.txt の {KEY} を値の表で埋めてから差し込む（埋め残しがあれば止まる）。
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
SM = {k: fill(v, k) for k, v in kv(os.path.join(H, 'docs1006_small.txt')).items()}
def one(L, pre):
    i = [k for k, l in enumerate(L) if l.startswith(pre)]; assert len(i) == 1, (pre[:40], len(i)); return i[0]
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs1006_lines.md') + '\n\n' + txt('docs1006_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = one(L, '| **★(1001) 合流（波91')
L[i + 1:i + 1] = txt('docs1006_rows.md').split('\n')
i = one(L, '| **次の波（波92〜・(1001) の推奨・AI・作者の承認は未確認）**')
L[i] = '| ~~**次の波（波92〜・(1001) の推奨・AI・作者の承認は未確認）**~~〔2026-11-10 ★(1006) で波92 完了＝c92gf・c92tg は入れた（恒等）・c92wr は measurements へ・c92tf は docs へ・挙動の線 0 本＝基線は m1001 のまま〕 | 〔畳んだ〕 | 実装ログ (1001) |'
L[i + 1:i + 1] = [txt('docs1006_next.md')]
i = one(L, '| **★(1001) 作者に聞く物')
L[i:i] = [txt('docs1006_ask.md')]
i = one(L, '| **採用率の表')
a = '（★(1001)）。'; assert L[i].count(a) == 1
L[i] = L[i].replace(a, '（★(1001)） → ' + SM['RATE'] + '（★(1006)）。')
i = one(L, '| **★(1001) 新しいヌル腕の 100 倍の参照の最大が 0')
L[i + 1:i + 1] = [SM['W']]
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = one(L, '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-10（(1001) 合流')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1001)） |', 1)
L[i:i] = [SM['CURRENT']]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = one(L, '- ★(1001)：')
L[i + 1:i + 1] = [SM['README']]
wr(p, '\n'.join(L))
p = T + '/機能の台帳.md'
L = rd(p).split('\n')
i = one(L, '- S05 B-1 公売（(1001)）')
L[i + 1:i + 1] = [SM['LEDGER']]
wr(p, '\n'.join(L))
print('ok')
