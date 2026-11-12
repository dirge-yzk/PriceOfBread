# (1078) 合流：docs の更新（実装ログ・00一覧・CURRENT・README・機能の台帳）。バイトで読み書き（CR と BOM を保つ）。
import sys, csv, io
W = 'D:/PoB_work/m1078_work/'
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def frag(n): return rd(W + n).replace('\r\n', '\n')
# 実装ログ
L = 'docs/202609/99_実装ログ_20260903.md'
s = rd(L); assert '(1078) 合流' not in s
wr(L, s.rstrip('\n') + '\n' + frag('log_lines.md') + frag('log_merge.md'))
# 00一覧
O = 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(O); assert '★(1078)' not in s
nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl)
def idx(pref):
    r = [i for i, l in enumerate(lines) if l.startswith(pref)]; assert len(r) == 1, (pref, len(r)); return r[0]
# 採用率の表
i = idx('| **採用率の表（★(685)')
old = '（★(1069)）。採らなかった物の多くは'
assert lines[i].count(old) == 1
lines[i] = lines[i].replace(old, '（★(1069)） → **波102 挙動 0・恒等 3/4（c102ui・c102rp・c102em 入れた・c102tr は手続きで採らない〔T4 の書き換えが ★(743) の外〕）・読む 3（c102sy〔S23 軍備と動員〕・c102tx〔試験の秒〕・c102fl〔英の艦隊〕）・設計の材料 1（c102bk〔3 つのブロック〕）・計画 0**（★(1078)）。採らなかった物の多くは')
# §0-W（★(1069) の行の次）
i = idx('| **★(1069) 列強が小国の属国になる呼び')
lines.insert(i + 1, frag('sec0w.md').strip('\n'))
# §0-2（表の先頭＝★(1069) 作者に聞く物の前）
i = idx('| **★(1069) 作者に聞く物')
lines.insert(i, frag('q_row.md').strip('\n'))
# 次の波：波102 の行を畳んで、新しい行を足す
i = idx('| **次の波（波102〜・(1069) の推奨')
lines[i] = '| ~~**次の波（波102〜・(1069) の推奨・AI・作者の承認は未確認）**~~〔2026-11-12 ★(1078) で波102 完了＝挙動 0・恒等 3/4（c102ui・c102rp・c102em 入れた・c102tr は手続きで採らない）・c102sy・c102tx・c102fl は measurements へ・c102bk は docs/202611/20261112 へ〕 | 〔畳んだ〕 | 実装ログ (1069) |'
lines.insert(i + 1, frag('next_row.md').strip('\n'))
# §00（★(1069) 合流の行の次）
i = idx('| **★(1069) 合流（波101')
lines.insert(i + 1, frag('sec00.md').strip('\n'))
wr(O, nl.join(lines))
# CURRENT（改行が混ざる＝行の頭の文字列で置き換える）
C = 'docs/CURRENT.md'
s = rd(C)
pre = '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-12（(1069) 合流'
assert s.count(pre) == 1
LF = chr(10); CR = chr(13)
k = s.index(pre); e = s.index(LF, k); nl = (CR + LF) if s[e - 1] == CR else LF
s = s[:k] + frag('current_row.md').strip(LF) + nl + s[k:].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(1069)） |', 1)
wr(C, s)
# README（★(1069) の次）
R = 'docs/README.md'
s = rd(R); nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl)
i = [k for k, l in enumerate(lines) if l.startswith('- ★(1069)：')]; assert len(i) == 1
lines.insert(i[0] + 1, frag('readme_add.md').strip('\n'))
wr(R, nl.join(lines))
# 機能の台帳：§2e S23（§3 の前）
T = 'docs/機能の台帳.md'
s = rd(T); nl = '\r\n' if '\r\n' in s else '\n'
assert '## 2e. S23' not in s
rows = list(csv.reader(io.StringIO(rd('measurements/m1074_20261112/ledger_S23.tsv')), delimiter='\t'))
tab = ['| ' + ' | '.join(c.replace('|', '／').replace('\n', ' ') for c in r) + ' |' for r in rows]
tab.insert(1, '|' + '---|' * len(rows[0]))
sec = frag('ledger_sec.md').rstrip('\n').split('\n')
k = sec.index('@@TABLE@@'); sec[k:k + 1] = tab
lines = s.split(nl)
i = [k for k, l in enumerate(lines) if l.startswith('## 3. 他の系統の仮の行')]; assert len(i) == 1
lines[i[0]:i[0]] = sec + ['']
wr(T, nl.join(lines))
print('ok', len(rows) - 1)
