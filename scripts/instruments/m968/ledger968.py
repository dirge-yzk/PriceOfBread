# (968) 合流：機能の台帳の行の更新を写す（線の報告の文・バイトで読み書き・列の数は変えない・ledger961.py の型）。
#   python -X utf8 ledger968.py <台帳 md 入力（reanchor の出力）> <台帳 md 出力> <合わせた木の短いコミット>
import sys
src, dst, H = sys.argv[1], sys.argv[2], sys.argv[3]
lines = open(src, 'rb').read().decode('utf-8').split('\n')
def upd(rid, f):
    hit = [i for i, l in enumerate(lines) if l.startswith('| ' + rid + ' |')]
    assert len(hit) == 1, (rid, len(hit))
    c = lines[hit[0]].split(' | '); n0 = len(c); f(c); assert len(c) == n0
    lines[hit[0]] = ' | '.join(c)
def h006(c):
    c[9] = c[9] + '（2026-11-08 (964) c86lt：A-2〈定額の土地税〉を m961 の上で取り直した＝旗 LandTaxFixedBasis 既定 1・腕 B の門 G7 20・G4 0・G8a 0・G8b 0・清の G8c 0.4368〔窓 0.2468〜1.2565〕・止める線 0・S11 の清の G8c 0.5301。(968) 合流で採った＝合わせた木の M・S11 は線の腕 B・S11 と全行一致〔計器の生も cearn_s の 2 列と時計の表の外はバイト一致〕・3 回の規則の 2 回目〔(958) は数えない〕）'
upd('S20-H006', h006)
open(dst, 'wb').write('\n'.join(lines).encode('utf-8'))
print('ok')
