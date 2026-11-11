# (907) c78lg：(900) mk_fixtures900.py を写して、門 6 のもう片方（判定を埋めた行に〈最後に確かめたコミット〉が無い）の台帳を 2 本足した（元の r900/ は書き換えない）。
# 使い方: python -X utf8 mk_fixtures907.py <台帳 after907> <出力フォルダ>
#  ledger_gate6last.md   S17-P07（§4・確かめた系統の外・判定〈働く〉・最後に確かめた 11db605f）の〈最後に確かめた〉を〈—〉にした＝門 6 だけが落ちるはず
#  ledger_gate6last_v.md S25-01（確かめた系統 S25・判定〈働く〉）の〈最後に確かめた〉を〈—〉にした＝門 5 と門 6 が一緒に落ちる（記録・確かめた系統の行では門 6 だけを落とせない）
#  ledger_empty.md     空の台帳（門 4 ほか）
#  ledger_gate6.md     S15-01（判定〈働く〉・最後に確かめた 731d3de1）の錨を〈—〉にした＝門 6 だけが落ちるはず（★(899) 反証役〈門 6 はどの台帳でも赤にならない〉）
#  ledger_dup.md       §3 の S10 の塊を行の途中で 2 つの見出しに割った（行の中身・id は同じ）＝門 8 だけが落ちるはず
#  ledger_nocol.md     S15-02 の行の 12 列目を消した（11 列）＝門 7 だけが落ちるはず
#  ledger_kesu.md      S15-32 の 12 列目から読み手の数を消した＝門 9 だけが落ちるはず
#  ledger_after.md     そのまま（rc=0 のはず）
import sys, os
src, od = sys.argv[1:3]
os.makedirs(od, exist_ok=True)
b = open(src, 'rb').read().decode('utf-8')
assert '\r' not in b
def w(name, text): open(os.path.join(od, name), 'wb').write(text.encode('utf-8'))
def one(a, c):
    assert b.count(a) == 1, (b.count(a), a[:60])
    return b.replace(a, c)
w('ledger_empty.md', '')
a = '| S15-01 | S15 | 正統性のストック（侵食と回復） | 前期値から侵食を引き、上限 1 へ回復率×(1−正統性) で戻る（L2593） | '
k = b.index(a) + len(a); e = b.index(' | ', k)
anchor = b[k:e]
assert anchor.startswith('Politics.cs:') and 'LegitimacyRegenRate' in anchor, anchor
w('ledger_gate6.md', b[:k] + '—' + b[e:])
HDR = '| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク | 直す／消す／残す |'
SEP = '|---|---|---|---|---|---|---|---|---|---|---|---|'
i3 = b.index('\n## 3. ')
k = b.index('\n| S10-H010 |', i3)
w('ledger_dup.md', b[:k] + '\n\n### S10 人口動態と暮らし\n\n' + HDR + '\n' + SEP + b[k:])
k = b.index('| S15-02 |'); e = b.index('\n', k); row = b[k:e]
assert row.endswith(' — |'), row[-20:]
w('ledger_nocol.md', b[:k] + row[:-len(' — |')] + b[e:])
k = b.index('| S15-32 |'); e = b.index('\n', k); row = b[k:e]
assert '消す（i・読み手 0' in row
w('ledger_kesu.md', b[:k] + row.replace('消す（i・読み手 0＝届かない枝・試験の呼び手 1＝ConservationTests.cs:682）', '消す（i・届かない枝）') + b[e:])
def blank_last(rid, expect):
    k = b.index('| ' + rid + ' |'); e = b.index(chr(10), k); row = b[k:e]
    c = row.strip().strip('|').split('|')
    assert len(c) == 12 and c[7].strip() == expect and c[8].strip() == '働く', (rid, c[7], c[8])
    c[7] = ' — '
    return b[:k] + '|' + '|'.join(c) + '|' + b[e:]
w('ledger_gate6last.md', blank_last('S17-P07', '11db605f'))
w('ledger_gate6last_v.md', blank_last('S25-01', 'fc9aeb4c'))
w('ledger_after.md', b)
print('fixtures', sorted(os.listdir(od)))
