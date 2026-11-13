# (1096) 合流：機能の台帳の更新（バイトで読み書き・LF）。
#   python -X utf8 ledger1096.py
#   ① s14 (1094) の 38 行を §2g として §3 の前に足す。§3 の S14 の仮の行（同じ id）は §2g への参照に畳む（担当の推奨・id の重なりを避ける）。
#   ② 反証役 v の高で S14-38・S14-H021・S14-H034 の判定を〈開幕の 1 四半期だけ働く（計器は 0 を書いた後に読む）〉へ直す（直した旨は根拠の頭に書く）。他の指摘は §2g-1 へ和らげずに写す。
#   ③ §1 の S14 の行の状態を〈(1094) で確かめた（§2g・木 23aa9d11a）〉へ。
import csv, io
R = 'D:/PoB_work/wt_m1096/'
P = R + 'docs/機能の台帳.md'
s = open(P, 'rb').read().decode('utf-8'); assert '\r' not in s
L = s.split('\n')
def find(pref):
    r = [i for i, l in enumerate(L) if l.startswith(pref)]; assert len(r) == 1, (pref, len(r)); return r[0]
t = open('D:/PoB_work/r1094_work/ledger_S14_2026-11-13.tsv', 'rb').read().decode('utf-8').replace('\r\n', '\n')
rows = [r for r in csv.reader(io.StringIO(t), delimiter='\t') if r]
assert all(len(r) == 12 for r in rows)
n_fix = 0
FIX = '開幕の 1 四半期だけ働く（計器 scalars_s は MultiNationSim.cs:859 が 0 を書いた後に読む＝開幕の 0.3 は見えない）'
for r in rows:
    if r[0] in ('S14-38', 'S14-H021', 'S14-H034'):
        r[9] = '(1096) 合流が反証役 v の高で判定を直した（元の判定〈' + r[8] + '〉）：各国の最初の四半期の Politics.Step は NationConfig.cs:114 の 0.3 を読む（Politics.cs:551・Ideology.cs:32）＝0 を書く :859 はその後の段。' + r[9]
        r[8] = FIX; n_fix += 1
tab = ['| ' + ' | '.join(c.replace('|', '／').replace('\n', ' ') for c in r) + ' |' for r in rows]
tab.insert(1, '|' + '---|' * len(rows[0]))
sec = '''## 2g. S14 不満と思想（(1094) c104s14 で確かめた・木 23aa9d11a・(1096) の合流が写した）

- 範囲：文書 docs/ゲーム概要と実装機能.md L2401〜L2592 の 37 見出し＋新しい行 S14-38（外圧の鎖）。§3 の S14 の仮の行はこの節への参照に畳んだ（id が同じ）。表は `measurements/m1094_20261113/ledger_S14_2026-11-13.tsv`・まとめは同じフォルダの `ledger_S14_2026-11-13.md`・台本は `scripts/instruments/r1094/`。
- 生（4 腕）：M＝D:/PoB_work/keep_1087/base_ins（種 1〜10）・N1／N2＝keep_1087/n1・n2・S11＝keep_1087/a11。表＝dcont_s・ideology_s・scalars_s・reform_s・idn_s・monlaw_s・crownpc_s・prds_s・disc_s。
- 判定の数（担当の値）：働く 18・一部だけ働く 7・働かない 1・休眠 2・廃止を確かめた 8・未確かめ 2（38 行）。(1096) の合流が反証役 v の高で 3 行（S14-38・H021・H034）を〈開幕の 1 四半期だけ働く〉へ直した＝働かない 0・一部だけ 6・働く 17・開幕の 1 四半期だけ 3。
- 主な所見：外圧は開幕の後の全ての行で 0（MultiNationSim.cs:859）・便乗の勢いの飽和 27.6%（立憲と共和で約 42%）・大きい闘争度は厭戦・飢え・民族から（法の項の 90% 点 0.024）・最も早い社会主義は 10 種ともアメリカ 1785〜88 年・組合の非合法の行 86.7%・秘密投票は 3 つの枝が全て出る・文書が古い所 14 か所。

@@TABLE@@

### 2g-1. 反証役 v の訂正（合流 (1096) が足した・上の 3 行の判定の直しの他は表の行を書き換えていない・和らげずに写した）

- **高**：外圧は〈4 腕の全ての行で 0＝効いていない〉ではない。各国の Politics.Step は MultiNationSim.cs:776 の StepDayClose の中で走り（Simulation.cs:1765 の QuarterEnd から :1935）、0 を書く MultiNationSim.cs:859 はその後の段＝最初の四半期は NationConfig.cs:114 の 0.3 を Simulation.cs:92 経由で読む（Politics.cs:551 の改革派 ×1.15・Ideology.cs:32 の国民主義 ＋0.3×0.3）。計器 ScalarsFold（Instruments.cs:1846）は書いた後に読む＝〈計器が書かない値を 0 と数えた〉型。事前登録の追記 A〈最初の四半期に 0.3 が残る〉は〈外れ〉でなく〈計器では見えない〉。**K1（外圧の鎖を消す）は開幕から軌道を変える＝恒等の線にならない**（推測・腕は回していない）＝F2 群4 への申し送りに〈消すと最初の四半期が変わる＝挙動の線か、先に開幕の値を 0 にする恒等でない変更が要る〉を足す。
- 中：K1 の失う物に〈単国の全ての盤面で全期間 0.3 の追い風（単国の Simulation には 0 を書く書き手がいない・ToScenario は 121 ファイル 357 か所）〉と〈黒船と明治・清の外圧の唯一の入口〉が無い。Q-s14-1 の推奨〈消す〉は史実の損失を名乗った上で作者の問いに残す。導き直しの候補（港・条約港・海軍の到達など・AI の案・確証なし）を並べていない。
- 低：H019 王党派・H026 思想の到達の〈一部だけ働く（量が小さい）〉は事前登録の定義（入力が 0 でない行が 5% 未満か門で止まる）では〈働く〉（0.001 を超える行 97%・伝わりの項が正の行 92%）。〈量が小さい〉は結果の後の基準＝H008・H031 と基準が揃っていない。
- 低：H036 の partOldK＞0 の 44.6%・大衆に占める 6.8%・H025 の〈最も早い社会主義は 10 種ともアメリカ 1785〜88 年〉は台本も出力も無く mk_ledger.py に手で書いた値＝〈台本なし・反証役が M と S11 で再現（44.61%・6.78%・アメリカ 1785〜88）〉。
'''.split('\n')
j = sec.index('@@TABLE@@'); sec[j:j + 1] = tab
# ③ §1 の S14 の行
i1 = find('| S14 | 不満と思想 |')
c = L[i1].split(' | '); assert c[-1] == '未確かめ |', c[-1]
c[-1] = '(1094) で確かめた（§2g・木 23aa9d11a） |'; L[i1] = ' | '.join(c)
# ① §3 の S14 の仮の行を畳む
a = find('### S14 不満と思想'); b = find('### S15 ')
old = [l for l in L[a + 1:b] if l.startswith('| S14-')]
L[a + 1:b] = ['', f'→ §2g（(1094) c104s14 で確かめた 38 行・id は同じ）。仮の行 {len(old)} 行は (1096) の合流が畳んだ（中身は §2g の表が持つ）。', '']
i3 = find('## 3. 他の系統の仮の行')
L[i3:i3] = sec + ['']
open(P, 'wb').write('\n'.join(L).encode('utf-8'))
print('ok', 'S14 行', len(rows) - 1, '直した行', n_fix, '畳んだ仮の行', len(old), '全行', len(L))
