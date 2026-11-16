# (1178) 合流（m1164/cfg1164.py の型・基線を m1171→m1178 へ）。バイトで読み書き。
#   python -X utf8 cfg1178.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1171（★(1171)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1178（★(1178) 合流〔波116〕＝挙動の線 c116t1〔(1172) T1 の 3 回目・段 T1-0′ の計器 t1buy_s の末尾 13 列・旗 TradeBuyBySupplyCurve 既定 1〕を採った・'
              '恒等の線 c116cl〔(1175) sv の後片付け・PriceLawSeesExports と LaborBandFloorOffGate の焼き込み・硝石丘の枝の削除〕を入れた・'
              '挙動の線 c116k1・c116hd は合わせた木で落ちて外した・腕 Z〔ident m1178z・cl＋t1＋k1＋旗 0・段 2・計器つき〕と腕 Z′〔ident m1178y・cl＋t1＋旗 0・--no-ins〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261116_m1178・B10INS b10_20261116_m1178_ins・MON mon_20261116_m1178.txt・INSRAW keep_1178/base_ins〔M′ の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2895〔Tests 2661＋Tests.Ui 234〕・NG_BASE ng1178）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261116_m1171\n', 'B5=measurements/b5_20261116_m1178\n')
one('B10INS=measurements/b10_20261116_m1171_ins\n', 'B10INS=measurements/b10_20261116_m1178_ins\n')
one('MON=measurements/mon_20261116_m1171.txt\n', 'MON=measurements/mon_20261116_m1178.txt\n')
one('INSRAW=D:/PoB_work/keep_1171/base_ins\n', '# (1178) から keep_1178/base_ins（合わせた木 cl＋t1 の M′ の計器つき 10 種の生・木 ' + H + '・全表・t1buy_s は末尾 13 列・cearn_s・cpop_s・flow_s の経路の行番号は合わせた木の物）。ヌル腕 keep_1178/n1・n2（絞った表＝a11 の名簿＋rponset_s・rule_s・secession_s・cessp_s・nationgood_s・tb_s・cederule_s・armsfill_s・soldiervac_s・mintreg_s・admrule_s・t1buy_s・pop_s・POB_CEARN=0）・種 11〜20 は keep_1178/a11・旗 0 の門の表は a11z。\nINSRAW=D:/PoB_work/keep_1178/base_ins\n')
one('BASE_COMMIT=26fe1c6d7\n', '# (1178) 新しい基線の測った木（INSRAW keep_1178/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2894\n', '# (1178)：2894＋c116t1 1（TradeT1RowAccTests）＋c116cl 0（輸出の番人 +1・確かめの試験 −1）＝2895＝Tests 2661＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2895\n')
one('NG_BASE=rd:measurements/ng1171_20261116:m1171\n', 'NG_BASE=rd:measurements/ng1178_20261116:m1178\n')
one('STAGE_BASE=26fe1c6d77fca6570f5ac16836b10a2b45c58257\n', '# (1178) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
