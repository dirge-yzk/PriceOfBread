# (975) 合流：機能の台帳の行を (973) c87mw の採用で直す（ledger968.py の型）。
#   python -X utf8 ledger975.py <台帳 md 入力（reanchor の出力）> <台帳 md 出力> <合わせた木の短いコミット>
import sys
src, dst, H = sys.argv[1], sys.argv[2], sys.argv[3]
lines = open(src, 'rb').read().decode('utf-8').split('\n')
def upd(rid, f):
    hit = [i for i, l in enumerate(lines) if l.startswith('| ' + rid + ' |')]
    assert len(hit) == 1, (rid, len(hit))
    c = lines[hit[0]].split(' | '); n0 = len(c); f(c); assert len(c) == n0
    lines[hit[0]] = ' | '.join(c)
def s1617(c):
    c[4] = 'Simulation.cs:2120:MilitaryWarMultFromContact'
    c[7] = H
    c[9] = c[9] + '（2026-11-09 (973) c87mw ⑰：内戦の 2.0 の枝と旗 CivilWarArmsDemandUnified を旧の枝ごと消した＝内戦も対外戦も同じ式〔旗 MilitaryWarMultFromContact 1 では 1＋κ×接触の割合・κ＝WarArmsDemandPerContact 42〕。(975) 合流で採った＝錨を新しい warMult の行へ）'
def h024(c):
    c[9] = c[9] + '（2026-11-09 (973) c87mw を (975) で採った：旗 MilitaryWarMultFromContact 1 では Simulation は MilitaryWarMult・WarMobilizationDepth を読まない＝器と計器だけ〔読み手は旗 0 の枝・計器・WarThemeTests〕）'
upd('S16-17', s1617)
upd('S23-H024', h024)
open(dst, 'wb').write('\n'.join(lines).encode('utf-8'))
print('ok')
