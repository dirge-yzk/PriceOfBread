# (954) 合流：機能の台帳の行の更新を写す（線の報告の文・バイトで読み書き・列の数は変えない・ledger947.py の型）。
#   python -X utf8 ledger954.py <台帳 md>
import sys
p = sys.argv[1]
lines = open(p, 'rb').read().decode('utf-8').split('\n')
def upd(rid, f):
    hit = [i for i, l in enumerate(lines) if l.startswith('| ' + rid + ' |')]
    assert len(hit) == 1, (rid, len(hit))
    c = lines[hit[0]].split(' | '); n0 = len(c); f(c); assert len(c) == n0
    lines[hit[0]] = ' | '.join(c)
def h010(c):
    assert c[9].endswith('反証役の訂正 4）'), c[9][-12:]
    c[9] = c[9] + '（2026-11-08 (952) c84kp：案 K を旗 OpeningColonyDominanceFull で 1 回目・器は ident〈判定: 恒等〉・旗 1 のフルスイートで SecessionLaborLawInstrumentTests.cs:87 の対照の件数 7 で止めた・腕 B なし・3 回の規則に数えない・器と点火は枝 origin/c84kp に残す＝★(940)）'
def h006(c):
    assert c[10] == '土地税の算定の基準 A1（612 行 ⑤⑧）', c[10]
    c[10] = c[10] + '・(953) c84tb 土地税の算定の基準 A-2b〈基準を選ぶ X〉の材料（盤面 m947・腕 0 本）＝推奨 X1（土地税の軸の 2 行目の段の法）・K1（行政の能力の門と査定の費用を足さない）・査定は切り替えの月の地代・新しい状態は凍結の額と基準の目標・前提 P1〜P7（A-2 の 2 回目しだい）・反証役の訂正 9（施行は MandateGate でなく Move＝〈同じ門・同じ代価〉は成り立たない ほか）・下書き docs/202611/20261108/20261108_土地税の算定の基準A1の段A-2b_基準を選ぶ_盤面m947_下書き.md（AI の案・確証なし・作者の確認待ち）'
upd('S27-H010', h010)
upd('S20-H006', h006)
open(p, 'wb').write('\n'.join(lines).encode('utf-8'))
print('ok')
