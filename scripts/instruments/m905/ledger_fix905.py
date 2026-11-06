# (905) 合流：c77lg の台帳と概要の写しに当てる文書の直し（バイトで読み書き・CR と BOM を保つ）。
#   python -X utf8 ledger_fix905.py <台帳 in> <台帳 out> <概要 in> <概要 out>
import sys
li, lo, oi, oo = sys.argv[1:5]
def rep(b, old, new, n):
    o, w = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    assert c == n, (old, c, n)
    return b.replace(o, w)
b = open(li, 'rb').read()
b = rep(b, '働かない 5 行のうち 4 行は既定 0 の器', '働かない 6 行（(905) 合流の反証役の数え直しで 5→6 に直した）のうち 4 行は既定 0 の器', 1)
b = rep(b, 'S25 0（Landmass.cs・数え直すと 0・§1 の表は 1・差の理由は読んでいない）',
        "S25 2（Landmass.cs・(905) 合流の反証役：`--since=2026-09-26` は時刻を今の時刻で補う＝'2026-09-26 00:00:00' まで書いて数え直すと 2〔f7120308・b7fd1c8f〕・S19 はどちらの書き方でも 15・§1 の表は 1）", 1)
b = rep(b, 'TreatyRatificationWeight', 'TreatyRatificationControlThreshold', 0)   # 台帳の錨の改名は reanchor905.py が先に済ませた（4 行）＝ここでは 0 件であることだけ確かめる
open(lo, 'wb').write(b)
o = open(oi, 'rb').read()
o = rep(o, 'TreatyRatificationWeight', 'TreatyRatificationControlThreshold', 5)
open(oo, 'wb').write(o)
print('ok')
