# (994) 合流：読む線 r992（c90si）と設計の材料の線 r993（c90bc）の作業フォルダを、リポジトリへ写す（copyr988.py の型）。
#   中身は書き換えない（改行だけ CRLF→LF にそろえる）。反証役の指摘は末尾に節を足す（本文は直さない）。
#   r993 の材料の題の行だけ、状態の語を下書きと同じ〈設計の材料と下書き・AI の案・作者の確認待ち〉へそろえる。
#   python -X utf8 -I copyr994.py <リポジトリの根>
import os, sys
R = sys.argv[1]
def rd(p):
    return open(p, 'rb').read().replace(b'\r\n', b'\n')
def wr(dst, b):
    d = os.path.join(R, dst); os.makedirs(os.path.dirname(d), exist_ok=True)
    open(d, 'wb').write(b); print('cp', dst, len(b))
TAIL = {}
TAIL['r992'] = '''

---
## 反証役の指摘（(994) 合流が末尾に足した・本文は直していない）
- 中：案A の区分の条件は共通の州の実質の産出の比だけ（samereg.py の `moved = 0.1 <= cr <= 10`）で、州の集合が変わったことを条件にしていない＝州の集合が同じで物価だけが 100 倍動いた名目の跳びも〈州の出入り〉に入り〈それ以外〉から消える。次の恒等の線の素案に (a) 〈前と今の四半期で州の集合が違う〉（と〈全体の比は [1/10,10] の外・共通の比は内〉）を条件に足す (b) 行に名目÷実質の比（物価の比）を出す (c) 〈同じ州・物価だけ 200 倍〉の合成の表を G3 に足す (d) 突然変異 M6〈集合の変化の条件を外す〉で G3 が赤になることを紙で確かめる＋M5 の〈要確認〉を腕の前に決める。この線の結論（エンジンの実際の落ち・破綻ではない）は変わらない。
- 低：止める線（gdp_jump906 の〈それ以外〉）が数えたのは q445 の上がり（211.7 倍・人口の比 1.1598）。q444 の落ち（204.0 倍）は gdp_fall949 の記録の節にだけある（stop906_n2.txt:8・:16・:25・stop949_m981nulls_n2.txt:8〜25・:63）。同じ事象の鏡像。
- 低：vfall.py の V 字は〈次の 1 四半期で戻る〉物しか数えない。2 四半期で戻る S11 s16 ポルトガル（q546→q548・99.5 倍）と N2 s9 ライン諸邦（q486→q488・49.9 倍）は 6 件に入っていない（1〜2 四半期なら 8 件・どちらも 100 倍未満）。型の名は〈1 四半期だけ〉。
- 低：副所見③の起点 1886 の 14,736.8 は 1 年だけの山（1885 5,332・1887 4,774・1883〜1889 の平常 3,400〜5,900）。平常から 1893 の 42/月へは約 1/120。食料の 0.53〜0.59 は 1890（0.526）と 1893（0.588）の点で、1883〜1889 は 0.86〜0.94。bengal.py のコメントは q%40 でコードは q%60。1886〜1893 の年ごとの値は bengal_out.txt に無い。
- 低：[1/10,10] の共通の比の物差しは事前登録（prereg.txt 17:09）に無く、samereg.py（17:13）で vfall の結果を見た後に作った＝〈100 倍以上の 6 件は全て当たる〉は作った物差しを同じ盤面に当てはめた結果。次の線では〈m988 の盤面を見た後に作った区分〉と名乗り、G2 は予言ではなく〈当てはめの確かめ〉と書く。prereg の〈植民地の人口の割合〉はシャム以外で出していない。
'''
TAIL['r993'] = '''

---
## 反証役の指摘（(994) 合流が末尾に足した・本文は直していない）
要点：K3 は〈ほぼ何もしない〉という結論は、反証役のソースの読みと再集計でも崩れなかった（全て〈低〉）。
- 低：§3 の Mi4〈drawn を due−have で数える〉は、have を補助（:1141）の後の貯蓄（:1144）で取る限り Debt の増え（:1145 Bank.Lend(due−have)）と同じ額＝元と同じ値を出す突然変異で C1 は緑。〈have を補助の前の貯蓄で数える〉と特定するか表から外す。
- 低：§3 の Mi3〈LastImport の累積を読む〉は担保を大きく数え excess は 0 の側へ寄る＝C3（excess＝0・drawn＞0）は赤にならない。赤になり得るのは C1 だけ。
- 低：§2・material §4〈m.Price には床が直接は掛からない〉は〈直接は〉現物どおりだが、値段の帯の錨は rp＝RefPrice×region.PriceLevel（MarketFlow.cs:134・:242 付近）＝国の床 0.4 は錨を通して m.Price に間接に効く。ρ の向きには効かない。
- 低：〈方針の無い財では全ての成約で ρ＜1〉は fac2≧1 が前提。売り手の国の奨励率 sellCheap（:913・旗 TradeCheapSellSubsidy・既定 0）が 1 なら buyDear＝0 でも fac2＜1 になり得る＝〈旗 TradeCheapSellSubsidy＝0（既定）のとき〉と条件を足す。
- 低：material §2〈商品を担保にした貸しは担保の時価より低い額まで（掛け目）＝慣行として確証あり〉は書名が無い＝〈出典未確認〉〈推〉へ下げる。
- 低：〈F の区間の純の輸入は負〉は和÷和の話。U5 の最大の区間（種 7 イギリス 537〜582）では Σ純の輸入÷ΣoutCapital が＋0.748（q993_base_ins.txt）。
- 低：〈99.75% に 1 枚目の手形の債務が残る〉の〈1 枚目〉は推し量り（列 merchantDebt は四半期の末の断面）＝〈推〉。
- 低：§0〈数える前に固定した〉は draft.md の中にあり最終更新 17:25（台本の出力 17:16 の後）＝外から確かめる印が無い（q993.py と §0 の食い違いは無い）。次からは §0 を別のファイルとして台本の前に置く。
'''
# r992
W = 'D:/PoB_work/r992_work'; M = 'measurements/m992_20261109/'
wr(M + 'results.txt', rd(W + '/results.txt') + TAIL['r992'].encode('utf-8'))
for f in ['prereg.txt', 'plan.txt', 'next_prereg_draft.txt', 'siam_n2.tsv', 'rebel_n2.tsv', 'pl_check.tsv', 'vfall_out.txt', 'samereg_out.txt', 'bengal_out.txt', 'nation_cols.txt']:
    wr(M + f, rd(W + '/' + f))
for f in ['rows.py', 'vfall.py', 'samereg.py', 'bengal.py']:
    wr('scripts/instruments/r992/' + f, rd(W + '/' + f))
# r993
W = 'D:/PoB_work/r993_work'; D = 'docs/202611/20261109/20261109_B5のc2_K3手形の額を積荷の値で縛る_盤面m988_'
wr(D + '下書き.md', rd(W + '/draft.md') + TAIL['r993'].encode('utf-8'))
m = rd(W + '/material.md'); old = '【設計の材料・AI の読み・作者の確認待ち】'.encode('utf-8')
assert m.count(old) == 1; m = m.replace(old, '【設計の材料と下書き・AI の案・作者の確認待ち】'.encode('utf-8'))
wr(D + '材料.md', m + TAIL['r993'].encode('utf-8'))
for f in ['q993.py', 'q993b.py']:
    wr('scripts/instruments/r993/' + f, rd(W + '/' + f))
for f in ['q993_base_ins.txt', 'q993_n1.txt', 'q993_n2.txt', 'q993b_base_ins.txt', 'q3_base988.txt']:
    wr('measurements/m993_20261109/' + f, rd(W + '/' + f))
