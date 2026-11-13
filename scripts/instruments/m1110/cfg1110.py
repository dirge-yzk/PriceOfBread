# (1110) 合流（m1103/cfg1103.py の型・基線は m1103 のまま・BASE_COMMIT・STAGE_BASE・TESTS_NONFULL だけ）。バイトで読み書き。
#   python -X utf8 cfg1110.py <ident.cfg> <合わせた木の短いコミット> <合わせた木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1103（★(1103)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1103 のまま（★(1110) 合流〔波106〕＝挙動の線 0 本・恒等の線 c106dc〔(1107) 文書だけ〕・c106ui〔(1104) uifix2 3900efcb4 まで〕・'
              'c106im〔(1105) 計器 86 本を SimCore/Instruments/ へ〕・c106cf〔(1106) Coefficients.cs を部品 10 本へ〕を入れた・'
              '腕 Z〔ident m1110z・合わせた木 ' + H + '〕〈判定: 恒等〉＝素の 5 種・計器つき 10 種が m1103 と全行一致・SOURCE-HASH ' + SH + '・'
              '成績表と NG の基準と INSRAW〔keep_1103/base_ins〕は m1103 のまま・BASE_COMMIT ' + H + '・TESTS_NONFULL 2823）。もとは') + b[k0 + k1:]
one('BASE_COMMIT=92ee76dc\n', '# (1110) 合わせた木（.cs は 62693feb6 と同じ）。\nBASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2808\n')
b = b[:k] + '# (1110)：2808＋c106ui 14（LawStakesTests ほか uifix2 の試験）＋c106im 1（InstrumentsPlacementTests）＝2823（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2808\n', 'TESTS_NONFULL=2823\n')
one('STAGE_BASE=92ee76dc42c22c1b7a507f1e855c43c643c452d0\n', '# (1110) STAGE_BASE は合わせた木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
