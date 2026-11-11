# (933) 合流：機能の台帳の行の更新を写す（線の報告の下書き・バイトで読み書き・列の数は変えない）。
#   python -X utf8 ledger933.py <台帳 md>
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
    print('upd', rid)
# 列（' | ' で割った添字）：0 '| 行'・1 系統・2 機能・3 期待・4 錨・5 持ち主・6 判定の型・7 最後に確かめた・8 判定・9 根拠・10 残りタスク・11 直す／消す／残す ' |'
def s1617(c):
    c[7] = '1af3e5d7'
    c[8] = '働く'
    c[9] = c[9] + '。★(933)：旗 CivilWarArmsDemandUnified の既定 1（(929) c81cw を採った）＝内戦も対外戦と同じ MilitaryWarMult×(1+WarMobilizationDepth×厭戦)・2.0 の枝は旗 0 のときだけ（Simulation.cs:2123）。M の門 G1 a_c 10.796・newRuleMismatch 0・S11 a_c 10.786（measurements/m933_20261107/armM・armS11 の cw_gates.txt）'
def s18h018(c):
    c[6] = '効き'; c[7] = '11c35e54'; c[8] = '一部だけ働く'
    c[9] = c[9] + '；(932) c81ld の材料（docs/202611/20261107/20261107_地主の政治の導き直しの段C-0′＋D-0_盤面m919_材料.md）'
    c[11] = '直す＝LP-1（(932)） |'
def s18h019(c):
    c[6] = '効き'; c[7] = '11c35e54'; c[8] = '一部だけ働く'
    c[9] = c[9] + '；(932) c81ld の材料（同上）'
    c[11] = '直す＝C-0′＋D-0→C-1→D-1（(932)） |'
def s18h026(c):
    c[6] = '効き'; c[7] = '11c35e54'; c[8] = '一部だけ働く'
    c[9] = c[9] + '；(932) c81ld の材料（同上）・PolicyPreferences.cs:72 の注〈既定1.5〉は古い'
    c[11] = '直す＝PolicyPreferences.cs:72 の注は恒等の小物・関税の項は C-1 の後の別の線（(932)） |'
def s20h005(c):
    c[9] = c[9] + '；(931) c81co：Fiscal.cs:119 の人頭税が植民地格の州のお金を吸い切る（0 の続きの入り口の前 12 四半期の流出の 93%・税÷期首の貯蓄 1.0）＝残す（★(859) ② の見張り）'
upd('S16-17', s1617)
upd('S18-H018', s18h018)
upd('S18-H019', s18h019)
upd('S18-H026', s18h026)
upd('S20-H005', s20h005)
open(p, 'wb').write('\n'.join(lines).encode('utf-8'))
