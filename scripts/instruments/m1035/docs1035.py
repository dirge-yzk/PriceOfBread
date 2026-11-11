# (1035) 合流（m1027/docs1027.py の型・錨を (1027) の行へ）：生きた文書へ (1028)〜(1035) の段と行を差し込む（バイトで読み書き・改行と BOM を保つ）。
#   python -X utf8 docs1035.py <リポジトリの根> <作業フォルダ（docs_*.md・docs_small.txt・docs_fill.txt）>
import os, sys, re
R, W = sys.argv[1], sys.argv[2]; T = R + '/docs'
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def kv(p): return dict(l.split('=', 1) for l in rd(p).split('\n') if '=' in l and not l.startswith('#'))
FILL = kv(W + '/docs_fill.txt') if os.path.exists(W + '/docs_fill.txt') else {}
def fill(s, name):
    for k, v in FILL.items(): s = s.replace('{' + k + '}', v)
    left = re.findall(r'\{[A-Z_0-9]+\}', s); assert not left, (name, left)
    return s
def txt(name): return fill(rd(W + '/' + name).rstrip('\n'), name)
SM = {k: fill(v, k) for k, v in kv(W + '/docs_small.txt').items()}
def one(L, pre):
    i = [k for k, l in enumerate(L) if l.startswith(pre)]; assert len(i) == 1, (pre[:40], len(i)); return i[0]
for p in [T + '/202609/99_実装ログ_20260903.md', T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md', T + '/CURRENT.md', T + '/README.md', T + '/機能の台帳.md']:
    print(p[-24:], 'CRLF', open(p, 'rb').read().count(b'\r\n'))
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
wr(p, s + '\n' + txt('docs_lines.md') + '\n\n' + txt('docs_log.md') + '\n')
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
L = rd(p).split('\n')
i = one(L, '| **★(1027) 合流（波96')
L[i + 1:i + 1] = txt('docs_rows.md').split('\n')
i = one(L, '| **次の波（波97〜・(1027) の推奨・AI・作者の承認は未確認）**')
L[i] = '| ~~**次の波（波97〜・(1027) の推奨・AI・作者の承認は未確認）**~~〔2026-11-11 ★(1035) で波97 完了＝c97ws・c97sp は入れた（恒等）・c97eb は腕 B の前に止めた（3 回の規則の 1 回目）・c97mg は measurements へ・c97dp・c97lw・c97op は docs/202611/20261111 へ〕 | 〔畳んだ〕 | 実装ログ (1027) |'
L[i + 1:i + 1] = [txt('docs_next.md')]
i = one(L, '| **★(1027) 作者に聞く物')
L[i:i] = [SM['ASK']]
i = one(L, '| **採用率の表')
a = '（★(1027)）。'; assert L[i].count(a) == 1
L[i] = L[i].replace(a, '（★(1027)） → ' + SM['RATE'] + '（★(1035)）。')
i = one(L, '| **★(1027) 計算量の余りは 約 17.5 秒')
L[i + 1:i + 1] = [SM['W']]
i = one(L, '| **〈(3) リファクタの候補〉')
a = '231.1→231.0 秒・11 波目〕'; assert L[i].count(a) == 1
L[i] = L[i].replace(a, a + SM['REFAC'])
wr(p, '\n'.join(L))
p = T + '/CURRENT.md'
L = rd(p).split('\n')
i = one(L, '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-10（(1027) 合流')
L[i] = L[i].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1027)） |', 1)
L[i:i] = [SM['CURRENT']]
wr(p, '\n'.join(L))
p = T + '/README.md'
L = rd(p).split('\n')
i = one(L, '- ★(1027)：')
L[i + 1:i + 1] = [SM['README']]
wr(p, '\n'.join(L))
p = T + '/機能の台帳.md'
L = rd(p).split('\n')
i = one(L, '- 資源のデータ 10 行（(1027)）')
L[i + 1:i + 1] = [SM['LEDGER'], SM['LEDGER2'], SM['LEDGER3']]
wr(p, '\n'.join(L))
print('ok')
