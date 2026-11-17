# (1191) 合流（m1184/cfg1184.py の型・基線を m1184→m1191 へ）。バイトで読み書き。
#   python -X utf8 cfg1191.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1184（★(1184)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1191（★(1191) 合流〔波118〕＝挙動の線 c118nc〔(1186) 国民意識の廃止の 1 段目・係数 3 本を 0 へ〕と c118ur〔(1188) 統一戦争の AI の待ちと 30% の乱数を外す・係数 2 本〕を採った・'
              '恒等の線 c118hc〔(1185) hd の後片付け・試験の穴・README の門の決まり〕を入れた・挙動の線 c118mb〔(1187)〕は反証役 v2 の高 2 件で入れていない・'
              '腕 Z〔ident m1191z・hc＋nc＋ur＋nc と ur の係数を旧へ・段 1・計器つき〕〈判定: 恒等（段 1）〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261118_m1191・B10INS b10_20261118_m1191_ins・MON mon_20261118_m1191.txt・INSRAW keep_1191/base_ins〔M の計器つき 10 種の生・全表〕・'
              'ヌル腕は N1 の 1 本〔keep_1191/n1〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2903〔Tests 2669＋Tests.Ui 234〕・NG_BASE ng1191）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261116_m1184\n', 'B5=measurements/b5_20261118_m1191\n')
one('B10INS=measurements/b10_20261116_m1184_ins\n', 'B10INS=measurements/b10_20261118_m1191_ins\n')
one('MON=measurements/mon_20261116_m1184.txt\n', 'MON=measurements/mon_20261118_m1191.txt\n')
one('INSRAW=D:/PoB_work/keep_1184/base_ins\n', '# (1191) から keep_1191/base_ins（合わせた木 hc＋nc＋ur の M の計器つき 10 種の生・木 ' + H + '・全表＝線 nc の腕 B と軌道が同じ〔表ごとの sha1 で一致 1511・違う 50＝既知の揺れ 20＋呼び出し位置の行番号だけ 30〕）。ヌル腕は keep_1191/n1 の 1 本（絞った表・POB_CEARN=0・2 本を求める読み手には n1 を 2 回渡す）・種 11〜20 keep_1191/a11。\nINSRAW=D:/PoB_work/keep_1191/base_ins\n')
one('BASE_COMMIT=ba65aacf1\n', '# (1191) 新しい基線の測った木（INSRAW keep_1191/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2897\n', '# (1191)：2897＋c118nc 3（NationalConsciousnessReadersOffTests）＋c118ur 3（UnificationAiNoWaitTests）＋c118hc 0＝2903＝Tests 2669＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2903\n')
one('NG_BASE=rd:measurements/ng1184_20261116:m1184\n', 'NG_BASE=rd:measurements/ng1191_20261118:m1191\n')
one('STAGE_BASE=ba65aacf197b5b7f76385b3721da4fe53e860ea3\n', '# (1191) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
