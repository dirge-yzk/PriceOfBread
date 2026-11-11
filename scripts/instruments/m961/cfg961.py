# (961) 合流：ident.cfg を新しい基線 m961 へ書き換える（cfg947.py の型・バイトで読み書き）。
#   python -X utf8 cfg961.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH>
import sys
p, H, SH = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m947（★(954) 合流〔波84〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m961（★(961) 合流〔波85〕＝挙動の線 c85kp〔(957) K＝開幕の一括昇格の植民地の支配度を 1.0 から・旗 OpeningColonyDominanceFull 既定 1〕を採った新しい基線・'
              '恒等の線 c85hs〔(956) HeldSinceFromData・CoreHeldYears・heldSince の削除〕・試験だけの c85sl〔(955)〕を入れた・c85lt〔(958)〕は kp と重ねた木のフルスイートの赤で外した・腕 Z〔ident m961z・木 c114968f〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表 B5 b5_20261108_m961・B10INS b10_20261108_m961_ins・MON mon_20261108_m961.txt・INSRAW keep_961/base_ins〔M の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2431・NG_BASE ng961）'
              '。もとは基線 m947（') + b[k0 + k1 + len('基線 m947（'):]
one('B5=measurements/b5_20261108_m947\n', 'B5=measurements/b5_20261108_m961\n')
one('B10INS=measurements/b10_20261108_m947_ins\n', 'B10INS=measurements/b10_20261108_m961_ins\n')
one('MON=measurements/mon_20261108_m947.txt\n', 'MON=measurements/mon_20261108_m961.txt\n')
one('INSRAW=D:/PoB_work/keep_954/base_ins\n', 'INSRAW=D:/PoB_work/keep_961/base_ins\n')
one('BASE_COMMIT=3c3747bb\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (947) から keep_947/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (961) から keep_961/base_ins（lt を外した合わせた木 ' + H + ' の M〔c85kp の旗 1〕の計器つき 10 種の生）。ヌル腕 keep_961/n1・n2・種 11〜20 keep_961/a11（POB_CEARN=0）。'
             'keep_947・keep_954 は残す（古い keep_* を消すのは作者のセッション）。採らなかった腕の生は keep_961_rej（kp と lt を重ねた木の M・N1・N2・S11 と c85lt の腕 B・S11 を門と止める線の表へ縮めた物）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2425\n')
b = b[:k] + '# (961)：2425＋c85hs 1（OpeningProvinceHeldTests）＋c85kp 5（OpeningColonyDominanceTests ほか）＋c85sl 0＝2431。\n' + b[k:]
one('TESTS_NONFULL=2425\n', 'TESTS_NONFULL=2431\n')
one('NG_BASE=rd:measurements/ng947_20261108:m947\n', 'NG_BASE=rd:measurements/ng961_20261108:m961\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
