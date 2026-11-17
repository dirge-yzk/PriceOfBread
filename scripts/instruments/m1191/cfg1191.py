# (1191) 合流（m1184/cfg1184.py の写し・木と作業フォルダを wt_m1191・m1191_work へ）
# (1184) 合流（m1178/cfg1178.py の型・基線を m1178→m1184 へ）。バイトで読み書き。
#   python -X utf8 cfg1184.py <ident.cfg> <測った木の短いコミット> <測った木の全体のコミット> <SOURCE-HASH>
import sys
p, H, HF, SH = sys.argv[1:5]
b = open(p, 'rb').read().decode('utf-8')
assert '\r' not in b
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1178（★(1178)'); k1 = b.index('\n', k0)
old_head = b[k0:k1]
b = b[:k0] + ('# 現在の基準＝基線 m1184（★(1184) 合流〔波117〕＝挙動の線 c116hd〔(1174) 戦時の買いだめを消す・StockpilePanicWeight 0.5→0.0・3 回の規則の 2 回目＝載せ直し〕を採った・'
              '恒等の線 c117tg〔(1181) 試験の盤面と上界・Meritocracy の否定形〕・c117b0〔(1180) 旗 MintBuysAtCapital 既定 0 の器・計器 mintcap_s〕を入れた・挙動の線 c117k1〔(1179)〕は線の門 G5 で採らない＝入れていない・'
              '腕 Z〔ident m1184z・tg＋b0＋hd＋StockpilePanicWeight 0.5・段 2・計器つき〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261116_m1184・B10INS b10_20261116_m1184_ins・MON mon_20261116_m1184.txt・INSRAW keep_1184/base_ins〔M の計器つき 10 種の生・全表〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2897〔Tests 2663＋Tests.Ui 234〕・NG_BASE ng1184）。前の行：' + old_head[2:]) + b[k1:]
one('B5=measurements/b5_20261116_m1178\n', 'B5=measurements/b5_20261116_m1184\n')
one('B10INS=measurements/b10_20261116_m1178_ins\n', 'B10INS=measurements/b10_20261116_m1184_ins\n')
one('MON=measurements/mon_20261116_m1178.txt\n', 'MON=measurements/mon_20261116_m1184.txt\n')
one('INSRAW=D:/PoB_work/keep_1178/base_ins\n', '# (1184) から keep_1184/base_ins（合わせた木 tg＋b0＋hd の M の計器つき 10 種の生・木 ' + H + '・全表・mintcap_s あり）。ヌル腕 keep_1184/n1・n2（絞った表＝a11 の名簿＋…・mintcap_s・sf_s・POB_CEARN=0）・種 11〜20 keep_1184/a11・旗 0〔StockpilePanicWeight 0.5〕の種 11〜20 keep_1184/a11z（門の表＝keep_1178/a11 の同じ表と sha1 で一致）。\nINSRAW=D:/PoB_work/keep_1184/base_ins\n')
one('BASE_COMMIT=d5f7a8f90\n', '# (1184) 新しい基線の測った木（INSRAW keep_1184/base_ins を出した木）。\nBASE_COMMIT=' + H + '\n')
one('TESTS_NONFULL=2895\n', '# (1184)：2895＋c117b0 2（MintcapInstrumentTests）＋c117tg 0＋c116hd 0＝2897＝Tests 2663＋Tests.Ui 234（合わせた木の --list-tests で確かめた）。\nTESTS_NONFULL=2897\n')
one('NG_BASE=rd:measurements/ng1178_20261116:m1178\n', 'NG_BASE=rd:measurements/ng1184_20261116:m1184\n')
one('STAGE_BASE=d5f7a8f906b77981d965965e9b1544a54155b2f3\n', '# (1184) STAGE_BASE は新しい基線の測った木。\nSTAGE_BASE=' + HF + '\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
