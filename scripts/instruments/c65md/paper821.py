# (821) c65md 紙の台本：門の台本 gate821.py が〈何もしない実装・間違った実装〉で落ちることを、手で書いた cpmed_s・cpeace_s で確かめる（腕の前）。
#   使い方: python -I -X utf8 paper821.py <作業フォルダ>   （<作業フォルダ>/paper_<盤面>/ を作って gate821.py を回し、盤面ごとの総合の行を出す）
import sys, os, subprocess
w = sys.argv[1]
here = os.path.dirname(os.path.abspath(__file__))
H = 'seq\tmonth\tyear\tsideA\tsideB\tcivilFlag\tforcedFlag\tcollapsedIterFlag\texA\texB\tscoreIndex\tblankFlag\toverwriteMatchFlag\thitFlag\trepMovedIndex\tledgerAbsIndex\tcapturedCount\tcededFlag\tregionsMovedCount\trepWouldOldIndex'
CH = 'month\tyear\tloser\tnCollapse\tnOther\tnGrouped\tu2Count\tu3Count\tsaMaxIndex\trepOldIndex\trepNewIndex\trepRealIndex\tbudOldIndex\tbudNewIndex\tbudRealIndex\tcapturedCount\tcedeEligCount\tcedeRealCount\tshares\trepMovedIndex\trepExpectIndex\trepMovedOverExpect\trepFactors'
def row(forced, coll, match, hit, rep, led, would, civil=0, cap=0, ced=0, reg=0):
    return '\t'.join(str(x) for x in (1, 61, 1800, 'A', 'B', civil, forced, coll, 1, 0.2, 1.5 if match else 0.05, 0 if match else 1, match, hit, rep, led, cap, ced, reg, would))
def cprow(moved, expect):
    return '\t'.join(str(x) for x in (61, 1800, 'B', 1, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 'A:1:nan', moved, expect, moved / expect, '0:1:1'))
boards = {
    # 正しい旗 1：調停の戦争は白紙（賠償 0）・反実仮想は正・調停でない崩壊は上書きのまま・期待と実際が一致
    'ok_flag1': ([row(1, 1, 0, 0, 0, 0, 100.0), row(0, 1, 1, 1, 50.0, 90.0, 0)], [cprow(50.0, 50.0)]),
    # 旗 0（今の盤面）＝調停の戦争が崩壊で払う → G1 が落ちる
    'flag0': ([row(1, 1, 1, 1, 100.0, 180.0, 100.0), row(0, 1, 1, 1, 50.0, 90.0, 0)], [cprow(100.0, 100.0), cprow(50.0, 50.0)]),
    # 何もしない計器（行が無い）→ G0・G2a・G2b が落ちる
    'empty': ([], []),
    # M1・M2（分けを外す・満額で上書き）＝旗 1 でも調停の戦争が払う → G1
    'M1M2': ([row(1, 1, 1, 1, 100.0, 180.0, 100.0), row(0, 1, 1, 1, 50.0, 90.0, 0)], [cprow(100.0, 100.0), cprow(50.0, 50.0)]),
    # M4（調停の戦争を持つ敗者の戦争を前走査で全て数えない）＝同じ敗者の 2 本が満額 → 実際÷期待 2 → G2a
    'M4': ([row(1, 1, 0, 0, 0, 0, 100.0), row(0, 1, 1, 1, 50.0, 90.0, 0), row(0, 1, 1, 1, 50.0, 90.0, 0)], [cprow(100.0, 50.0)]),
    # M7（分けを調停でない戦争にも当てる）＝調停でない崩壊の講和が上書きの式と一致しない → G2b
    'M7': ([row(1, 1, 0, 0, 0, 0, 100.0), row(0, 1, 0, 0, 0, 0, 0)], [cprow(50.0, 50.0)]),
    # 植民地だけ動く誤り（賠償は 0 だが割譲の枝が走る）→ G1
    'colony_only': ([row(1, 1, 0, 1, 0, 0, 100.0, cap=1, reg=1), row(0, 1, 1, 1, 50.0, 90.0, 0)], [cprow(50.0, 50.0)]),
}
for name, (rs, cs) in boards.items():
    d = os.path.join(w, 'paper_' + name); os.makedirs(d, exist_ok=True)
    with open(os.path.join(d, 'cpmed_s1.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(H + '\n' + ''.join(r + '\n' for r in rs))
    with open(os.path.join(d, 'cpeace_s1.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(CH + '\n' + ''.join(r + '\n' for r in cs))
    out = subprocess.run([sys.executable, '-I', '-X', 'utf8', os.path.join(here, 'gate821.py'), d], capture_output=True, text=True, encoding='utf-8').stdout
    fails = [l.split('\t')[1].split('：')[0].split(' ')[0] for l in out.splitlines() if l.startswith('落ちる')]
    print(f'{name}\t' + ('全て通る' if not fails else '落ちる ' + ','.join(fails)))
