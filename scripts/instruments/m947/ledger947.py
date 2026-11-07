# (947) 合流：機能の台帳の行の更新を写す（線の報告と反証役の訂正・バイトで読み書き・列の数は変えない・ledger933.py の型）。
#   python -X utf8 ledger947.py <台帳 md>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
lines = b.split('\n')
def upd(rid, f):
    hit = [i for i, l in enumerate(lines) if l.startswith('| ' + rid + ' |')]
    assert len(hit) == 1, (rid, len(hit))
    i = hit[0]
    c = lines[i].split(' | ')
    n0 = len(c)
    f(c)
    assert len(c) == n0
    lines[i] = ' | '.join(c)
def h010(c):
    assert c[9].endswith('＝残す'), c[9][-10:]
    c[9] = c[9][:-len('＝残す')] + '＝~~残す~~（2026-11-08 (946) c83hs：開幕の一括昇格の呼び口〔MultiNationSim.cs:174〕だけ 1.0 から始める案 K＝直す側の材料・AI の案・確証なし・作者の確認待ち・反証役の訂正 4）'
    c[10] = c[10] + '・(946) 案 K（00一覧 §00 波84）'
def h011(c):
    assert c[6] == '—' and c[7] == '—' and c[8] == 'この波の線が埋める (888) c75ce', c[6:9]
    c[6] = '効き'; c[7] = 'd313515b'; c[8] = '働かない'
    c[9] = c[9] + '。(946) c83hs（盤面 m933・紙の積分し直し・腕 0 本）：29 州の四半期 0 の margin は +0.70〜+1.86・1 未満で始まる 6 州は 9 年以内に 1.0 へ重なり 0 に届く州×種は 0＝旗 1 にしても成績表は全行一致の予言（ペルシャ〔バクー〕は年の分解能の近似）＝点火しても力学が動かない'
    c[11] = '消す（読み手 2〔Simulation.cs:938・:939〕＋休眠の表 DormantIdentityTests.cs:103 の 1 行・(946) の段 H0＝恒等の線） |'
upd('S27-H010', h010)
upd('S27-H011', h011)
b = '\n'.join(lines)
add = '''
## 4-5. 波83 の線が埋めた行（(947) の合流が線の記録から写した・二度確かめない）

- 写した元：(943) c83ic の事前登録 `measurements/m943_20261107_prereg_ic.txt` と腕 B・S11 の門（`measurements/m943_20261107/`）・(947) の M・S11 の門（`measurements/m947_20261108/armM`・`armS11`）。(946) c83hs の下書き §4（`docs/202611/20261107/20261107_割譲の支配度の残り_開幕の州の支配度の初期値_盤面m933_下書き.md`・反証役の訂正 4 を当てた＝新しい行 S27-P06 は作らず S27-H010 を更新・S27-H011 は〈判定〉の列を埋めた）。
- 錨は d313515b→e8e2d4ae へ写した（reanchor905.py・写した 454・`measurements/m947_20261108/reanchor947.tsv`・門 1〜9 通る `ledger_check_947.txt`）。Coefficients.cs:2018 の 1 行と Diplomacy.cs の矯正講和の塊（+34／−9）で後ろの行が動いた。
- c83hd の旗 EmptyPopDebtToHeirs と c83lt の旗 LandTaxFixedBasis は main に無い（★(940)・枝 origin/c83hd・origin/c83lt に残した）＝行を立てない。c83cc の直し（InstrumentsTests.cs の :2119 の試験の ①）は試験＝行を立てない。
- 判定は線の担当と反証役の言い方のまま（和らげない）。

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク | 直す／消す／残す |
|---|---|---|---|---|---|---|---|---|---|---|---|
| S17-P09 | S17 | 矯正講和の君主系の押し付けの王冠（ImposeCrownFromLoser・形 F1） | 勝者が君主系なら、講和の段の頭で控えた敗者の王冠から写し替える（無→絶対・絶対→絶対・専制→専制・幕府→幕府）（(943)・AI の案・確証なし） | Coefficients.cs:2018:ImposeCrownFromLoser | Diplomacy.cs | 両側の門 | e8e2d4ae | 働く | (943) の門 G1・G1s・G3・GC が旗 0 の 5〜72 から 0（腕 B・S11・(947) の M・S11 で同じ）・GR 0・G2 544／456（線の事前登録の幅 204〜1060 の内・指示の文の幅 407〜515 なら外）・(ii) を拾う数〈imposedPolityCode≠講和の段の頭の王冠の写し〉M 0/375・S11 0/303（旗 0 の値 62〜72） | — | 残す |
'''
if not b.endswith('\n'):
    b += '\n'
b += chr(10) + add.lstrip(chr(10))
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
