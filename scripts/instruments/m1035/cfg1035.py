# (1035) 合流（m1027/cfg1027.py の型・挙動の線 0 本＝基線は m1027 のまま・INSRAW を合わせた木の生へ・(1011) の前例）
#   python -X utf8 cfg1035.py <ident.cfg> <測った木の短いコミット> <SOURCE-HASH>
import sys
p, H, SH = sys.argv[1:4]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
k0 = b.index('# 現在の基準＝基線 m1027（★(1027) 合流〔波96〕'); k1 = len('# 現在の基準＝')
b = b[:k0] + ('# 現在の基準＝基線 m1027 のまま（★(1035) 合流〔波97〕＝挙動の線 0 本・恒等の線 c97ws〔(1028) warentry_s の末尾 crossWeakWill・nationgood_s の末尾 outputSum・eventRegions〕・'
              'c97sp〔(1029) 清算の着値の下界で売り手を飛ばす・cscx_s の末尾 fillsSkippedLB・台帳の項 ClearingSkipsLB〕を入れた・腕 Z＝合わせた木そのもの〔ident m1035z〕〈判定: 恒等〉・'
              '測った木 ' + H + '・SOURCE-HASH ' + SH + '・成績表は m1027 の物のまま〔全行一致〕・INSRAW keep_1035/base_ins〔合わせた木の計器つき 10 種の生・keep_1027/base_ins とバイトで同じ表はハードリンク〕・'
              'BASE_COMMIT ' + H + '・TESTS_NONFULL 2651・NG_BASE ng1027）。もとは基線 m1027（') + b[k0 + k1 + len('基線 m1027（'):]
one('INSRAW=D:/PoB_work/keep_1027/base_ins\n', 'INSRAW=D:/PoB_work/keep_1035/base_ins\n')
one('BASE_COMMIT=df6c859f\n', 'BASE_COMMIT=' + H + '\n')
k = b.index('# (1027) から keep_1027/base_ins'); k = b.index('\n', k) + 1
b = b[:k] + ('# (1035) から INSRAW は keep_1035/base_ins（合わせた木 ws＋sp の ident m1035z の計器つき 10 種の生・木 ' + H + '・warentry_s・nationgood_s・cscx_s の末尾の列つき・csc_s と cscx_s の充填の列は c97sp で値が変わる）。'
             'ヌル腕 keep_1027/n1・n2・種 11〜20 keep_1027/a11・基線の M の生 keep_1027/base_ins は残す（軌道は同じ・古い keep_* を消すのは作者のセッション）。\n') + b[k:]
k = b.index('TESTS_NONFULL=2642\n')
b = b[:k] + '# (1035)：2642＋c97ws 4（WarInstrumentTests 2・NationgoodOutputEventTests 2）＋c97sp 5（ClearingLowerBoundTests ほか）＝2651（合わせた木の --list-tests で確かめた）。\n' + b[k:]
one('TESTS_NONFULL=2642\n', 'TESTS_NONFULL=2651\n')
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
