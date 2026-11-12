# (1087) 合流：docs の更新（実装ログ・00一覧・CURRENT・README・機能の台帳）。バイトで読み書き（CR と BOM を保つ）。m1078/docs1078.py の型。
#   python -X utf8 scripts/instruments/m1087/docs1087.py   （wt_m1087 の根で）
W = 'D:/PoB_work/m1087_work/'
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def frag(n): return rd(W + n).replace('\r\n', '\n')
# 実装ログ
L = 'docs/202609/99_実装ログ_20260903.md'
s = rd(L); assert '(1087) 合流' not in s
wr(L, s.rstrip('\n') + '\n\n' + frag('log_lines.md').strip('\n') + '\n' + frag('log_merge.md').rstrip('\n') + '\n')
# 00一覧
O = 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(O); assert '★(1087)' not in s
nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl)
def idx(pref):
    r = [i for i, l in enumerate(lines) if l.startswith(pref)]; assert len(r) == 1, (pref, len(r)); return r[0]
i = idx('| **採用率の表（★(685)')
old = '（★(1078)）。採らなかった物の多くは'
assert lines[i].count(old) == 1
lines[i] = lines[i].replace(old, '（★(1078)） → **波103 挙動 2/2（c103dm〔威信と巨大建造物を消す〕・c103cr〔21 州の文化と宗教のデータ〕採った＝新しい基線 m1087）・恒等 2/2（c103tr・c103st 入れた）・読む 3（c103rb〔支配の力の段 C′ の前〕・c103ld〔休眠 50 本と台帳の写し〕・c103s18〔S18 選挙と政党〕）・設計の材料 1（c103rm〔宗教と移民の段 0〕）・計画 0**（★(1087)）。採らなかった物の多くは', 1)
i = idx('| **★(1078) 1783 年から国民皆兵が標準')
lines[i + 1:i + 1] = frag('sec0w.md').strip('\n').split('\n')
i = idx('| **★(1078) 作者に聞く物')
lines.insert(i, frag('q_row.md').strip('\n'))
i = idx('| **次の波（波103〜・(1078) の推奨')
lines[i] = '| ~~**次の波（波103〜・(1078) の推奨・AI・作者の承認は未確認）**~~〔2026-11-13 ★(1087) で波103 完了＝挙動 2/2（c103dm・c103cr 採った＝新しい基線 m1087）・恒等 2/2（c103tr・c103st 入れた）・c103rb・c103ld・c103s18 は measurements と台帳へ・c103rm は docs/202611/20261113 へ〕 | 〔畳んだ〕 | 実装ログ (1078) |'
lines.insert(i + 1, frag('next_row.md').strip('\n'))
i = idx('| **★(1078) 合流（波102')
lines.insert(i + 1, frag('sec00.md').strip('\n'))
wr(O, nl.join(lines))
# CURRENT
C = 'docs/CURRENT.md'
s = rd(C)
pre = '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-12（(1078) 合流'
assert s.count(pre) == 1
LF = chr(10); CR = chr(13)
k = s.index(pre); e = s.index(LF, k); nl2 = (CR + LF) if s[e - 1] == CR else LF
s = s[:k] + frag('current_row.md').strip(LF) + nl2 + s[k:].replace('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-12（(1078) 合流', '| 前の更新（(1078)） | ⚠ **2026-11-12（(1078) 合流', 1)
wr(C, s)
# README（★(1078) の次）
R = 'docs/README.md'
s = rd(R); nl3 = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl3)
i = [k for k, l in enumerate(lines) if l.startswith('- ★(1078)：')]; assert len(i) == 1
lines.insert(i[0] + 1, frag('readme_add.md').strip('\n'))
# README の〈今の基線〉の行（★ 5種の腕は5種の基線と比べる）：(1069) で書き換え漏れ＝m1053 のままだった
k5 = [k for k, l in enumerate(lines) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261112_m1053`')]; assert len(k5) == 1
lines[k5[0]] = lines[k5[0]].replace('（今は `measurements/b5_20261112_m1053`＝合流 (1053) の新しい基線 m1053',
    '（今は `measurements/b5_20261113_m1087`＝合流 (1087) の新しい基線 m1087〔波103・挙動の線 c103dm・c103cr を採った・恒等の線 c103tr・c103st・INSRAW `D:/PoB_work/keep_1087/base_ins`・ヌル腕 keep_1087/n1・n2・種 11〜20 keep_1087/a11〕・その前は (1069) の m1069（この行は (1069) で書き換え漏れだった）と合流 (1053) の新しい基線 m1053', 1)
wr(R, nl3.join(lines))
# 機能の台帳（ledger1087.sh の出力＝写しと錨の写しの後）
T = 'docs/機能の台帳.md'
new = rd(W + 'ledger_re.md')
assert '## 2f. S18' in new
wr(T, new if new.endswith('\n') else new + '\n')
print('ok')
