# (968) 合流：ident.cfg を新しい基線 m968 へ書き換える（cfg961.py の型・バイトで読み書き）。
#   python -X utf8 cfg968.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH>
import sys
p, H, SH = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m961（★(961) 合流〔波85〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m968（★(968) 合流〔波86〕＝挙動の線 c86lt〔(964) A-2〈定額の土地税〉・旗 LandTaxFixedBasis 既定 1〕を採った新しい基線・'
              '恒等の線 c86sm〔(963) 小物の束・cearn_s の末尾 2 列〕・試験だけの c86oc〔(962)〕を入れた・腕 Z〔ident m968z・木 900a762a〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261108_m968・B10INS b10_20261108_m968_ins・MON mon_20261108_m968.txt・INSRAW keep_968/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2454・NG_BASE ng968）'
              '。もとは基線 m961（') + b[k0 + k1 + len('基線 m961（'):]
one('B5=measurements/b5_20261108_m961\n', 'B5=measurements/b5_20261108_m968\n')
one('B10INS=measurements/b10_20261108_m961_ins\n', 'B10INS=measurements/b10_20261108_m968_ins\n')
one('MON=measurements/mon_20261108_m961.txt\n', 'MON=measurements/mon_20261108_m968.txt\n')
one('INSRAW=D:/PoB_work/keep_961/base_ins\n', 'INSRAW=D:/PoB_work/keep_968/base_ins\n')
one('BASE_COMMIT=05983574\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (961) から keep_961/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (968) から keep_968/base_ins（合わせた木 oc＋sm＋lt の M〔LandTaxFixedBasis 1〕の計器つき 10 種の生・木 f25aa61d＝' + H + ' とエンジンは同じ）。ヌル腕 keep_968/n1・n2・種 11〜20 keep_968/a11（POB_CEARN=0）。'
             'keep_947・keep_954・keep_961・keep_961_rej は残す（古い keep_* を消すのは作者のセッション）。採らなかった腕は無い＝keep_968_rej は作っていない。\n') + b[k:]
k = b.index('TESTS_NONFULL=2431\n')
b = b[:k] + '# (968)：2431＋c86sm 4（HeirDebtAfterSettleColumnsTests ほか）＋c86lt 19（LandTaxFixedBasisTests・LandfixInstrumentTests ほか）＋c86oc 0＝2454。\n' + b[k:]
one('TESTS_NONFULL=2431\n', 'TESTS_NONFULL=2454\n')
one('NG_BASE=rd:measurements/ng961_20261108:m961\n', 'NG_BASE=rd:measurements/ng968_20261108:m968\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
