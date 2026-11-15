# (1171) 合流：読む線 c115lg (1169) の台帳の行を docs/機能の台帳.md へ差し込む（r1169/mk_testmd.py の出力〈試し〉を元に・節の題の〈・試し〉を外し・反証役 v の訂正を行の最後の列の末尾に〈(1171) v の訂正〉として足す＝中身は書き換えない）
#   python -X utf8 -I ledger1171.py <mk_testmd.py の出力 test_ledger.md> <書き出す台帳>
import sys
src, dst = sys.argv[1:3]
b = open(src, 'rb').read(); nl = b'\r\n' if b'\r\n' in b else b'\n'
s = b.decode('utf-8')
for h in ('## 2i. S02 値段と賃金の形成（(1169) c115lg で確かめた・試し）', '## 2j. S01 財と製法と発見（(1169) c115lg で確かめた・試し）'):
    assert s.count(h) == 1, h
    s = s.replace(h, h.replace('・試し）', '）'))
FIX = {
    'S01-H005d': '読み手は今の木で SimCore 3（Coefficients.p06:103・Discovery.cs:15・Discovery.cs:897 の SiteTerms〔旗 DiscoveryEngineerHalfPerMonth＞0 で届かない〕）＋計器 1（Instruments.cs:2367 の site_s の難易度の列）＋Runner 2（Program.cs:3391・:3576）＋試験 4（DiscoveryScoreTests.cs:51・:54・:1076・DiscoveryEngineerTests.cs:198）＝恒等で消すと Runner のビルドと試験が落ちる・失う物に site_s の難易度の列と Runner の discprobe の Y の表示',
    'S01-H019': '読み手は SimCore 2（Catalog.cs:433・Simulation.cs:1232）・試験 0（megaprojects.json は今の木に無い）',
    'S22-H035': '読み手に試験 2（CreditTests.cs:73・IndustrializationReaderTests.cs:162 が w.Explain.Get("CreditTarget") を読む）＝書き手を消すと 2 本が落ちる',
    'S02-H010': '錨は World.cs:1731（12 列目の World.cs:2071 は 11-12 の木の行番号）',
    'S01-H010': '史実の中心の〈動力の革命〉を消す側に置いた例外＝別の道で残る理由（工場の蒸気動力の石炭を焚く門・電化）の行番号は 9b18137a の物で、今の木の行は取り直していない',
    'S01-H007c': '数は M（base_ins）だけ＝N1・N2 は数えていない・クロンダイクが 11-12 の 10/10 から 6/10 に下がった帰属は読んでいない',
}
L = s.split(nl.decode())
hit = set()
for i, l in enumerate(L):
    for k, v in FIX.items():
        if l.startswith('| ' + k + ' |'):
            assert l.rstrip().endswith('|'), k
            body = l.rstrip()[:-1].rstrip()
            L[i] = body + f'〔(1171) v の訂正：{v}〕 |'
            hit.add(k)
assert hit == set(FIX), set(FIX) - hit
open(dst, 'wb').write(nl.decode().join(L).encode('utf-8'))
print('ok', len(hit))
