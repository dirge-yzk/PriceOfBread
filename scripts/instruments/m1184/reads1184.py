# (1184) 合流：読む線 c117rs (1182) と設計の材料 c117t2 (1183) を写す（中身は書き換えない・反証役 v の訂正は末尾に足す）。
#   python -X utf8 -I reads1184.py <リポジトリの木>
import os, shutil, sys
R = sys.argv[1]
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True); shutil.copyfile(src, dst)
# --- c117rs (1182)
W = 'D:/PoB_work/r1182_work'
for f in os.listdir(W + '/r1182'):
    if f.endswith('.py'): cp(f'{W}/r1182/{f}', f'{R}/scripts/instruments/r1182/{f}')
for f in os.listdir(W):
    p = f'{W}/{f}'
    if not os.path.isfile(p) or f == 'test_ledger.md' or f.endswith('.done'): continue
    cp(p, f'{R}/measurements/m1182_20261116/{f}')
# --- c117t2 (1183)
T = 'D:/PoB_work/r1183_work'
for f in os.listdir(T + '/scripts'):
    if f.endswith('.py'): cp(f'{T}/scripts/{f}', f'{R}/scripts/instruments/r1183/{f}')
for f in os.listdir(T):
    if f.startswith('out_an'): cp(f'{T}/{f}', f'{R}/measurements/m1183_20261116/{f}')
cp('D:/PoB_work/d1183_work/prereg.txt', f'{R}/measurements/m1183_20261116/prereg.txt')
VT2 = '''

---
## (1184) 合流が足した：反証役 v の訂正（(1183) c117t2・中 2・低 6・本文は書き換えていない）
- 中：下書き §5 の門 G1・G2・E1・E2 と突然変異 μ1〜μ5 は、旗 1 でも州の品位（EconomyStep.cs:938 の mineralMul＝region.MineralGradeFactor）を能力に掛けたまま、計器だけを生産者の品位へ読み替える誤りを拾わない（G1 は T2-1 が書き換える計器 minecap_s の gradeBin0 を読む・E1 の下側 1÷1.020 は産出が A0 と同じでも通る・上側 6.67 は物理の最大で形に固有の誤りの量ではない＝★(773)）。→ prodcap_s の capacity÷productivity（州×財）で、ふつうの鉱物の生産者の 1 人あたりの能力を A0 と比べる〈実際に掛かった側〉の門を足し、突然変異〈州の mineralMul を残す〉で赤を紙で書く。
- 中：作者に聞く物〈採らない時：年数を大きくすると案 S とほぼ同じ・小さくすると床が残る〉〈1 本の年数で石炭と金の両方に合う値は無い〉は膝の形に依る推し。膝を比例へ直すのは S28-24 (b) で決まっている。中くらいの年数＋比例の品位（石炭は少しずつ痩せ・金はほぼ痩せない）の第 3 の形を評価していない＝偽の 2 択。年数を大きくすると産地も枯れずラッシュの崩れ（G2）が消える＝案 S との本当の差。→ 作者に聞く物の 3 行を直す（an1.py に prop＋年数の腕を足す）。
- 低：〈膝を比例にしても床の州の数は変わらない（ソースから）〉は〈いつか床に届く〉まで。動く盤面では床に届く年が遅れる向き＝静的な推し・確証なし。
- 低：閾値のヌル腕の幅は keep_1150 の n1・n2（m1150 の盤面）。同じ盤面（keep_1178 の n1・n2 の mintreg_s）で床の割合を数えると 1850 年 基線 0.8575・n1 0.8565・n2 0.8472＝幅 約 1.012。E1 の下側と格 N の記録の幅は腕の前に同じ盤面で取り直す。
- 低：〈石炭 ×20〜27・鉄鉱石 ×13〜16・粘土 ×1.2〜1.6〉は台本の出力に無い。反証役の 10 種の数え：1913÷1784 は石炭 15.0〜32.6・鉄鉱石 9.0〜24.0・粘土 0.9〜1.6。種 1 の 1850 年の石炭の生産者は E 12＋N 311＝323（324 ではない）。
- 低：下書き §2〈賦存の級で年数を分ける〉は S28-24 で退けた形〉の出典は台帳にも設計にも見つからない（台帳は〈今の在庫は既に賦存に比例〉）。
- 低：案 S の良い所〈炭田と鉄の州は 1950 年まで枯れない〉は言い過ぎ（スコットランドのブラックバンド鉄鉱石・コーンウォールの錫と銅・英の鉛・英は 1870 年代からスペインの鉱石を輸入〈反証役の一般の知識・頁は未確かめ〉）。案 S は〈地域の枯れ→輸入〉の経路を消す＝貿易の主の線の段なので知らせる物。
- 低：〈新しい係数 0・状態は減る〉は使う状態の数え方（生産者の欄 2 本は全ての生産者がセーブに持つ）。BonanzaDepositYears 60 は意味が〈州の初期の年数〉から〈産地が開いた月の産出の年数〉へ変わる＝実質は置き直しの係数。
'''
for kind in ('material', 'draft'):
    nm = '材料' if kind == 'material' else '下書き'
    b = open(f'{T}/{kind}.md', 'rb').read()
    nl = b'\r\n' if b'\r\n' in b else b'\n'
    add = VT2.replace('\n', nl.decode()).encode('utf-8')
    open(f'{R}/docs/202611/20261116/20261116_貿易のT2の前提_{nm}.md', 'wb').write(b.rstrip(b'\r\n') + add)
print('ok')
