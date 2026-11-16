# (1178) 合流：読む線 c116rl (1176) の台帳の行（r1176/mk_testmd_s10.py の出力〈試し〉）を docs/機能の台帳.md へ差し込む（m1171/ledger1171.py の型）。
#   ①節の題の〈・試し〉を外す ②反証役 v の訂正を行の最後の列の末尾に〈(1178) v の訂正〉として足す（中身は書き換えない）
#   ③この波の線が名や値を変えた行（S02-H004a・S02-H007a・S02-H008b・S01-H015）の最後の列の末尾に〈(1178)：…〉を足し、消した名の錨を〈—〉へ（ledger_anchor1171.py の型）
#   python -X utf8 -I ledger1178.py <mk_testmd_s10.py の出力 test_ledger.md> <書き出す台帳>
import sys
src, dst = sys.argv[1:3]
b = open(src, 'rb').read(); nl = '\r\n' if b'\r\n' in b else '\n'
s = b.decode('utf-8')
h = '## 2k. S10 人口動態と暮らし（(1176) c116rl で確かめた・試し）'
assert s.count(h) == 1, h
s = s.replace(h, h.replace('・試し）', '）'))
POP = '頻度の母集合が混ざっている（anchor_s・scalars_s・dcont_s は計器の約 9 国を毎四半期、他の国を年末〔q%4==3〕だけ書く＝InstrumentsAnchor.cs:10）'
FIX = {
    'S10-H003e': POP + '＝食料の錨が決める行は年末の全ての国で 41.25%・計器の国の年末以外で 12.36%（混ぜた値 32.42%）。11-12 の値を〈誤り〉と書くのは 11-12 の母集合を確かめてから（確かめられなければ〈母集合が違う可能性〉）',
    'S10-H006': POP + '＝食料の錨が決める行は年末の全ての国で 41.25%・計器の国の年末以外で 12.36%（混ぜた値 32.42%）。11-12 の 3.9% との比較は 11-12 の母集合を確かめてから',
    'S10-H044': POP + '＝飢えの怒りが正の行は年末の全ての国で 46.72%・計器の国だけで 87.03%（混ぜた値 59.0%）',
    'S10-H002a': POP + '＝人口が扶養力を超える行は年末の全ての国で 16.67%・計器の国で 15.54%（混ぜた値 16.32%）',
    'S10-H003d': POP + '＝労働時間が 1 時間以上離れた行は年末の全ての国で 30.17%・計器の国で 28.84%（混ぜた値 29.8%）',
    'S10-H002d': '〈働かない（届かない門）〉は標本の行で届いた 0（標本は計器の国が四半期に 1 か月・他の国が年に 1 か月）。極端な飢饉（FoodFill 0・耕地が小さい・都市化 1）では床の前の値が約 0.24×baseline まで下がりうる〈推〉＝床を外す線の恒等は測った種での一致',
}
# この波の線が名や値を変えた行：(錨の置き換え〔旧, 新〕か None, 足す注)
WAVE = {
    'S02-H004a': (None, '(1174) c116hd（係数 StockpilePanicWeight を 0.5→0）は自分の門と止める線を通ったが、合流の番人の束で cl＋t1＋k1＋hd を重ねたときだけ InstrumentsTests 3 本が赤＝後から入れた hd を外した（★(961)・枝 origin/c116hd に残す・次の波で試験を直してから載せ直す）＝この行は今のまま。張り付きの頻度の母集合（(1176) v）＝年末の全ての国で約 74%・計器の国で約 78%'),
    'S02-H007a': (('MarketFlow.cs:166:coef.PriceLawSeesExports > 0 ? totalDemand + m.LastExport : totalDemand;Coefficients.cs:29:PriceLawSeesExports',
                   'MarketFlow.cs:165:totalDemand + m.LastExport'), None),
    'S02-H008b': (('MarketFlow.cs:86:bool bandFloorOff = coef.LaborBandFloorOffGate > 0;', 'MarketFlow.cs:86:bool laborClosed = h > 1;'), None),
    'S01-H015': (('Coefficients.p08_audit_soldiers.cs:124:NiterBedEnabled;Simulation.cs:1204:if (coef.NiterBedEnabled > 0)', '—（硝石丘の枝と NiterBedEnabled・NiterBedProductivity は消した＝(1175) c116cl）'), None),
}
NOTE = {
    'S02-H007a': '(1175) c116cl で旗 PriceLawSeesExports（既定 1）を焼き込み、旗を消した・番人〈輸出は値段の法の需要の側に映る〉を足した',
    'S02-H008b': '(1175) c116cl で旗 LaborBandFloorOffGate（既定 1）を焼き込み、旗を消した（LaborMarketFormTests の旗 0 の腕を今の盤面へ移した・上側を見る Assert が無い＝(1175) v）',
    'S01-H015': '(1175) c116cl で枝と係数 2 本を消した（硝石を産地にする回で戻す）',
}
L = s.split(nl)
hit = set()
for i, l in enumerate(L):
    for k, v in FIX.items():
        if l.startswith('| ' + k + ' |'):
            assert l.rstrip().endswith('|'), k
            L[i] = l.rstrip()[:-1].rstrip() + f'〔(1178) v の訂正：{v}〕 |'
            hit.add(k)
    for k, (an, note) in WAVE.items():
        if l.startswith('| ' + k + ' |'):
            c = L[i].split(' | ')
            if an is not None:
                o, n = an
                assert c[4].startswith(o), (k, c[4][:160])
                c[4] = c[4].replace(o, n, 1)
                note = NOTE[k]
            l2 = ' | '.join(c).rstrip()
            assert l2.endswith('|'), k
            L[i] = l2[:-1].rstrip() + f'〔(1178)：{note}〕 |'
            hit.add(k)
want = set(FIX) | set(WAVE)
assert hit == want, want - hit
open(dst, 'wb').write(nl.join(L).encode('utf-8'))
print('ok', len(hit))
