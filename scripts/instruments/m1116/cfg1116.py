# (1116) 合流（m1110/cfg1110.py の型・基線は m1103 のまま・INSRAW を keep_1116/base_ins〔合わせた木の腕 Z の生〕へ・BASE_COMMIT・STAGE_BASE・TESTS_NONFULL）。バイトで読み書き。
#   python -X utf8 cfg1116.py <ident.cfg> <合わせた木の短いコミット> <合わせた木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1103 のまま（★(1110)'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1103 のまま（★(1116) 合流〔波107〕＝挙動の線 0 本・恒等の線 c107di〔(1111) 表示の層を PriceOfBread.Display へ・試験を 2 プロジェクトへ〕・'
              'c107ha〔(1112) 注を履歴へ 組 A〕・c107hb〔(1113) 組 B〕・c107hc〔(1114) 係数の注〕を入れた・'
              '腕 Z〔ident m1116z・合わせた木 ' + H + '〕〈判定: 恒等〉＝素の 5 種・計器つき 10 種が m1103 と全行一致・SOURCE-HASH ' + SH + '・'
              '成績表と NG の基準は m1103 のまま・INSRAW は keep_1116/base_ins〔経路の行は合わせた木の座標〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2826）。もとは') + b[k0 + k1:]
one('INSRAW=D:/PoB_work/keep_1103/base_ins\n',
    '# (1116) から keep_1116/base_ins（合わせた木 di＋ha＋hb＋hc の腕 Z〔ident m1116z・PRUNE=0〕の計器つき 10 種の生・木 ' + H + '・全表 1437 本・cearn_s・cpop_s の経路の行番号と dep_s の callerLine は合わせた木の物・'
    'keep_1103/base_ins とバイトで同じ 1377 本はハードリンク＝hlink1116.py・別の物 60 本＝burscan・cearn・clock・cpop・dep・flow）。ヌル腕 keep_1103/n1・n2・種 11〜20 は keep_1103/a11 のまま（KEEPNULL）・keep_1103/base_ins は止める線の基準の生として残す。\n'
    'INSRAW=D:/PoB_work/keep_1116/base_ins\n')
one('# (1110) 合わせた木（.cs は 62693feb6 と同じ）。\nBASE_COMMIT=62693feb6\n', '# (1110) 合わせた木（.cs は 62693feb6 と同じ）。\n# (1116) 合わせた木（INSRAW keep_1116/base_ins を出した木・.cs は最終の頭と同じ）。\nBASE_COMMIT=' + H + '\n')
k = b.index('TESTS_NONFULL=2823\n')
b = b[:k] + '# (1116)：2823＋c107di 3（DisplayUnitTypeTests 2・SourceScanCoverageTests 1）＝2826＝エンジン PriceOfBread.Tests 2594＋画面 PriceOfBread.Tests.Ui 232（ident.sh が 2 プロジェクトの --list-tests の和を数える・合わせた木で確かめた）。FullLength は Tests 3＋Tests.Ui 0。\n' + b[k:]
one('TESTS_NONFULL=2823\n', 'TESTS_NONFULL=2826\n')
one('# (1110) STAGE_BASE は合わせた木。\nSTAGE_BASE=62693feb676e724e1f3addba6bc59cba08bd36e8\n', '# (1110) STAGE_BASE は合わせた木。\n# (1116) STAGE_BASE は合わせた木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
