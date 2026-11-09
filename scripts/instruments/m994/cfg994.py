# (994) 合流：基線は m988 のまま（挙動の線は 0 本）。ident.cfg の INSRAW を合わせた木の計器つき 10 種の生（landsale_s が +11 列）へ・BASE_COMMIT・TESTS_NONFULL だけを替える。
#   python -X utf8 cfg994.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH>
import sys
p, H, SH = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m988（★(988) 合流〔波89〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m988 のまま（★(994) 合流〔波90〕＝挙動の線は 0 本・恒等の線 c90tr〔(989) 試験だけ〕・c90px〔(990) φ* の器・旗 LandTaxPhiMove 0〕・旗 0 の器 c90sl〔(991) 公売・旗 LandShareTaxSale 0・計器 landsale_s +11 列〕を入れた・'
              '腕 Z＝合わせた木そのもの〔ident m994z〕〈判定: 恒等〉・測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表は m988 の物のまま〔全行一致〕・INSRAW keep_994/base_ins〔合わせた木の計器つき 10 種の生〕・BASE_COMMIT ' + H + '・TESTS_NONFULL 2521・NG_BASE ng988）'
              '。もとは基線 m988（') + b[k0 + k1 + len('基線 m988（'):]
one('INSRAW=D:/PoB_work/keep_988/base_ins\n', 'INSRAW=D:/PoB_work/keep_994/base_ins\n')
one('BASE_COMMIT=43d9a62d\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (988) から keep_988/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (994) から keep_994/base_ins（合わせた木 tr＋px＋sl〔旗は全て 0〕の ident m994z の計器つき 10 種の生・木 ' + H + '・landsale_s が末尾 11 列増えた以外は keep_988/base_ins と恒等）。'
             'ヌル腕と種 11〜20 は keep_988/n1・n2・a11 のまま。keep_981・keep_988 は残す（古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2504\n')
b = b[:k] + '# (994)：2504＋c90px 5（LandTaxPhiTests）＋c90sl 12（LandSaleTests）＋c90tr 0＝2521（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2504\n', 'TESTS_NONFULL=2521\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
