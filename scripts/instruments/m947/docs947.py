# (947) 合流：生きた文書（実装ログ・00一覧・CURRENT・README）へ波83 の記録を書く（バイトで読み書き・行を消さない）。
#   python -X utf8 docs947.py
import os
T = 'D:/PoB_work/wt_m947/docs'
H = os.path.dirname(os.path.abspath(__file__))
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
def txt(name): return rd(os.path.join(H, name)).rstrip('\n')

# 実装ログ：末尾に足す
p = T + '/202609/99_実装ログ_20260903.md'
s = rd(p)
if not s.endswith('\n'): s += '\n'
s += '\n' + txt('docs947_log.md') + '\n'
wr(p, s)

# 00一覧
p = T + '/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = rd(p); L = s.split('\n')
rows = txt('docs947_rows.md').split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| **★(940) 合流（波82')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = rows
i = [k for k, l in enumerate(L) if l.startswith('| **次の波（波83〜')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs947_next.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **★(940) 作者に聞く物')]; assert len(i) == 1
L[i[0]:i[0]] = [txt('docs947_ask.md')]
i = [k for k, l in enumerate(L) if l.startswith('| **採用率の表（★(685)')]; assert len(i) == 1
a = '（★(940)）。'; assert L[i[0]].count(a) == 1
L[i[0]] = L[i[0]].replace(a, '（★(940)） → ' + txt('docs947_rate.md') + '（★(947)）。')
wr(p, '\n'.join(L))

# CURRENT：§1 の先頭の行を足し、前の行の名を〈前の更新（(940)）〉へ
p = T + '/CURRENT.md'
s = rd(p); L = s.split('\n')
i = [k for k, l in enumerate(L) if l.startswith('| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-07（(940) 合流')]; assert len(i) == 1
L[i[0]] = L[i[0]].replace('| ★ **最終更新（main へ合流済み）** |', '| 前の更新（(940)） |', 1)
L[i[0]:i[0]] = [txt('docs947_current.md')]
wr(p, '\n'.join(L))

# README：★(940) の行の後に ★(947)
p = T + '/README.md'
s = rd(p); L = s.split('\n')
i = [k for k, l in enumerate(L) if l.startswith('- ★(940)：')]; assert len(i) == 1
L[i[0] + 1:i[0] + 1] = [txt('docs947_readme.md')]
wr(p, '\n'.join(L))
print('ok')
