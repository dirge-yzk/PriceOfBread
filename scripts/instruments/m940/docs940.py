# (940) 合流：生きた文書（00一覧・実装ログ・CURRENT・README・ident.cfg・機能の台帳）へ書く（バイトで読み書き・改行と BOM を保つ・1 度だけ当てる）。
#   python -X utf8 docs940.py <木> <フルスイートの要約の 1 行> <D: の空き GB>
import sys, os
T = sys.argv[1]; SUITE = sys.argv[2]
H = os.path.dirname(os.path.abspath(__file__))
DFREE = sys.argv[3]; D75 = str(round(float(DFREE) - 75, 1))
TX = open(os.path.join(H, 'docs940_text.md'), 'rb').read().decode('utf-8')
TX += '\n@@ log\n' + open(os.path.join(H, 'docs940_log.md'), 'rb').read().decode('utf-8')
TX = TX.replace('{SUITE}', SUITE).replace('{DFREE75}', D75).replace('{DFREE}', DFREE)
blk = {}
cur = None
for ln in TX.split('\n'):
    if ln.startswith('@@ '):
        cur = ln[3:].strip(); blk[cur] = []
    elif cur is not None:
        blk[cur].append(ln)
B = {k: '\n'.join(v).strip('\n') for k, v in blk.items()}

def rw(rel, fn):
    p = os.path.join(T, rel); b = open(p, 'rb').read()
    crlf = b.count(b'\r\n') > b.count(b'\n') // 2
    s = b.decode('utf-8')
    if crlf: s = s.replace('\r\n', '\n')
    n = fn(s)
    assert n != s, rel
    if crlf: n = n.replace('\n', '\r\n')
    open(p, 'wb').write(n.encode('utf-8')); print('wrote', rel, len(b), '->', len(n.encode('utf-8')))

def once(s, old, new):
    assert s.count(old) == 1, (old[:40], s.count(old)); return s.replace(old, new)

# 00一覧
def ichiran(s):
    L = s.split('\n')
    i667 = next(i for i, l in enumerate(L) if l.startswith('| **次の波（波81〜・(926) の推奨'))
    assert L[i667 - 1].startswith('| **★(933) 合流')
    i668 = i667 + 1
    assert L[i668].startswith('| **次の波（波82〜・(933) の推奨・AI・作者の承認は未確認）** |')
    L[i668] = L[i668].replace('| **次の波（波82〜・(933) の推奨・AI・作者の承認は未確認）** |',
                              '| **次の波（波82〜・(933) の推奨・AI・作者の承認は未確認）** ' + B['ann668'] + ' |', 1)
    L.insert(i668 + 1, B['next83'])
    for r in reversed(B['rows'].split('\n')):
        L.insert(i667, r)
    s = '\n'.join(L)
    s = once(s, '| **★(933) 作者に聞く物（波81 の合流・本当に迷う物だけ・推奨つき）** |', B['ask940'] + '\n| **★(933) 作者に聞く物（波81 の合流・本当に迷う物だけ・推奨つき）** |')
    s = once(s, '・計画 0**（★(933)）。', '・計画 0**（★(933)） → ' + B['rate'] + '。')
    return s
rw('docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md', ichiran)

# 実装ログ（末尾へ）
def log(s):
    return s.rstrip('\n') + '\n\n' + B['log'] + '\n'
rw('docs/202609/99_実装ログ_20260903.md', log)

# CURRENT §1 の先頭の行
def current(s):
    old = '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-07（(933) 合流（波81）'
    return once(s, old, B['current'] + '\n| 前の更新（(933)） | ⚠ **2026-11-07（(933) 合流（波81）')
rw('docs/CURRENT.md', current)

# README：89 行の基線の行と ★(940)
def readme(s):
    s = once(s, '（今は `measurements/b5_20261107_m933`＝2026-11-07 (933) の合流〔', '（今は `measurements/b5_20261107_m933`＝' + B['readme89'] + '・元は 2026-11-07 (933) の合流〔')
    return once(s, '\n- ★(933)：**恒等の線を 2 本以上重ねた腕 Z', '\n' + B['readme'] + '\n- ★(933)：**恒等の線を 2 本以上重ねた腕 Z')
rw('docs/README.md', readme)

# ident.cfg の注
def cfg(s):
    return once(s, '# 現在の基準＝基線 m933（★(933) 合流', '# 現在の基準＝基線 m933（' + B['cfg'] + '）。もとは基線 m933（★(933) 合流')
rw('scripts/instruments/ident/ident.cfg', cfg)

# 機能の台帳（末尾へ §4-4）
def ledger(s):
    return s.rstrip('\n') + '\n\n' + B['ledger'] + '\n'
rw('docs/機能の台帳.md', ledger)
