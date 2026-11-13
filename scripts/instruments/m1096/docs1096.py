# (1096) 合流：docs の更新（実装ログ・00一覧・CURRENT・README）。バイトで読み書き（CR と BOM を保つ）。m1087/docs1087.py の型。
#   python -X utf8 scripts/instruments/m1096/docs1096.py   （wt_m1096 の根で）
W = 'D:/PoB_work/m1096_work/'
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def frag(n): return rd(W + n).replace('\r\n', '\n')
# 実装ログ
L = 'docs/202609/99_実装ログ_20260903.md'
s = rd(L); assert '(1096) 合流' not in s
wr(L, s.rstrip('\n') + '\n\n' + frag('log_lines.md').strip('\n') + '\n\n' + frag('log_merge.md').rstrip('\n') + '\n')
# 00一覧
O = 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(O); assert '★(1096)' not in s
nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl)
def idx(pref):
    r = [i for i, l in enumerate(lines) if l.startswith(pref)]; assert len(r) == 1, (pref, len(r)); return r[0]
i = idx('| **採用率の表（★(685)')
old = '（★(1087)）。採らなかった物の多くは'
assert lines[i].count(old) == 1
lines[i] = lines[i].replace(old, '（★(1087)） → **波104 挙動 0・恒等 4/4（c104ui〔uifix 18d6253e8 まで〕・c104fb〔旗の焼き込み 群 1＋FactoryBuildRefIsReal＋MC0〕・c104cp〔王権 P1・B1 の影の計器〕・c104si〔支配の力の計器 S1〜S4・鉱山の計器 MCi〕入れた）・読む 3（c104ar〔開幕の陸軍力の落ち〕・c104al〔A-L は通らない〕・c104s14〔S14 不満と思想〕）・設計の材料 1（c104rc〔段 R と C′ の素案〕）・計画 0**（★(1096)）。採らなかった物の多くは', 1)
i = idx('| **★(1087) 作者に聞く物')
lines.insert(i, frag('q_row.md').strip('\n'))
i = idx('| **次の波（波104〜・(1087) の推奨')
lines[i] = '| ~~**次の波（波104〜・(1087) の推奨・AI・作者の承認は未確認）**~~〔2026-11-13 ★(1096) で波104 完了＝挙動 0・恒等 4/4（c104ui・c104fb・c104cp・c104si 入れた＝基線は m1087 のまま）・c104ar・c104al・c104s14 は measurements と台帳へ・c104rc は docs/202611/20261113 へ〕 | 〔畳んだ〕 | 実装ログ (1087) |'
lines.insert(i + 1, frag('next_row.md').strip('\n'))
i = idx('| **★(1087) 合流（波103')
lines.insert(i + 1, frag('sec00.md').strip('\n'))
wr(O, nl.join(lines))
# CURRENT
C = 'docs/CURRENT.md'
s = rd(C)
pre = '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-13（(1087) 合流'
assert s.count(pre) == 1
LF = chr(10); CR = chr(13)
k = s.index(pre); e = s.index(LF, k); nl2 = (CR + LF) if s[e - 1] == CR else LF
s = s[:k] + frag('current_row.md').strip(LF) + nl2 + s[k:].replace(pre, '| 前の更新（(1087)） | ⚠ **2026-11-13（(1087) 合流', 1)
wr(C, s)
# README（★(1087) の次）
R = 'docs/README.md'
s = rd(R); nl3 = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl3)
i = [k for k, l in enumerate(lines) if l.startswith('- ★(1087)：')]; assert len(i) == 1
lines.insert(i[0] + 1, frag('readme_add.md').strip('\n'))
k5 = [k for k, l in enumerate(lines) if l.startswith('| ★ 5種の腕は**5種の基線**と比べる（今は `measurements/b5_20261113_m1087`')]; assert len(k5) == 1
old5 = 'INSRAW `D:/PoB_work/keep_1087/base_ins`・ヌル腕 keep_1087/n1・n2・種 11〜20 keep_1087/a11〕'
assert lines[k5[0]].count(old5) == 1
lines[k5[0]] = lines[k5[0]].replace(old5, 'INSRAW は (1096) から `D:/PoB_work/keep_1096/base_ins`〔波104 の合流・挙動の線 0 本＝基線は m1087 のまま・恒等の線 c104ui・c104fb・c104cp・c104si の計器の表を含む〕・ヌル腕 keep_1087/n1・n2・種 11〜20 keep_1087/a11〕', 1)
wr(R, nl3.join(lines))
print('ok')
