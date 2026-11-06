# (893) c76lg：docs/ゲーム概要と実装機能.md の S15 の節（L2593〜2796）だけを直した写しを作る（行の数は変えない＝台帳の L の番号を動かさない）。
# 使い方: python -X utf8 patch_overview893.py <今の文書> <出力>
import sys
src, out = sys.argv[1:3]
b = open(src, 'rb').read().decode('utf-8')
assert '\r' not in b
L = b.split('\n')
T = '（2026-11-06 (893) に木 731d3de1 で取り直した・台帳 {}）'
def sub(n, a, c):
    s = L[n - 1]
    assert s.count(a) == 1, (n, a, s[:80])
    L[n - 1] = s.replace(a, c)
def line(n, start, new):
    assert L[n - 1].startswith(start), (n, L[n - 1][:80])
    L[n - 1] = new
ST = '　**稼働中** ／ '
sub(2594, 'の差＋腐敗から出る', 'の差から出る（腐敗の項は (383) で削除）')
line(2595, ST, ST + '`Politics.cs:395 / Politics.cs:398 / Politics.cs:434 / Politics.cs:456 / Diplomacy.cs:3265 / Politics.cs:479`' + T.format('S15-01・S15-02・S15-03'))
sub(2596, '腐敗の侵食(CorruptionLegitimacyErosion)=0.015', '~~腐敗の侵食(CorruptionLegitimacyErosion)=0.015~~（(383) で削除）')
line(2601, ST, ST + '`Politics.cs:417`' + T.format('S15-04'))
line(2607, ST, ST + '`Politics.cs:423 / Politics.cs:425`' + T.format('S15-05'))
sub(2613, '（裁定S2＝「満タンなら必ず払える」床は無い）。', '（裁定S2＝「満タンなら必ず払える」床は無い）→ ★(404)・(409) 以後は満タン（S≦0）で正統性の残りの 2 乗が代価に届けば通る（壁の一歩・`Government.cs:590`・MandateOverStockLegitPower＝2）＝永久ではない（2026-11-06 (893)・台帳 S15-09）。')
line(2615, ST, ST + '`Government.cs:478（MandateMargin）`・`Government.cs:549（MandateGate）`・`Government.cs:578（PoliticalCapitalAffords）`・`Government.cs:893（Move）` ／ `PoliticalQuantities.cs:304（b）`・`PoliticalQuantities.cs:325（実行度）` ／ `Politics.cs:450 / Politics.cs:452（線形回復）`' + T.format('S15-06〜S15-09・S15-11'))
line(2622, ST, ST + '`Government.cs:478（MandateMargin が憲法級に0を返す）`・`Government.cs:492（IsConstitutional）`' + T.format('S15-10'))
sub(2633, '`Government.cs:218-226,447-469（', '`Government.cs:227 / Government.cs:512（')
L[2633 - 1] = L[2633 - 1] + T.format('S15-12')
line(2639, ST, ST + '`Politics.cs:353 / Politics.cs:354 / Politics.cs:355 / Politics.cs:358 / Politics.cs:373 / Politics.cs:388`' + T.format('S15-13・S15-14'))
sub(2640, '汚職の重み(DynMandateGraftWeight)=0.5', '~~汚職の重み(DynMandateGraftWeight)=0.5~~（(383) で削除）')
line(2645, ST, ST + '`Politics.cs:380 / Politics.cs:385`' + T.format('S15-15'))
line(2651, ST, ST + '`Politics.cs:460`' + T.format('S15-17'))
line(2655, '#### ● 制度疲労', '#### ~~● 制度疲労~~')
line(2658, ST, '　~~**稼働中**~~ **削除（2026-10-16）** ／ ~~`Politics.cs:534-591`~~')
line(2662, '#### ● 制度疲労のショックは何度でも開く', '#### ~~● 制度疲労のショックは何度でも開く~~')
line(2665, ST, '　~~**稼働中**~~ **削除（2026-10-16）** ／ ~~`Politics.cs:553-562`~~')
line(2670, ST, ST + '`Government.cs:297 / Politics.cs:492 / PoliticalQuantities.cs:13`' + T.format('S15-21'))
line(2676, ST, ST + '`Politics.cs:489`' + T.format('S15-22'))
line(2682, ST, ST + '`Politics.cs:571（Transition）`・`Politics.cs:702（ExecuteRegimeChange）`' + T.format('S15-23・S15-28・S15-29・S15-32〜S15-34'))
old2687 = L[2687 - 1]
assert old2687.startswith('⚠ **軍政への引力は') and '~~' not in old2687
L[2687 - 1] = '⚠ **(393) で置き換えた**＝兵士 POP の思想の離反の割合で保持力 κ を守る側と挑戦の側へ分ける（`Politics.SoldierHoldShare`・軍政の支持への足し算は消えた・2026-11-06 (893)・台帳 S15-26）。以下は旧の記述：~~' + old2687 + '~~'
line(2688, ST, ST + '`Politics.cs:614 / Politics.cs:616 / Politics.cs:1819`' + T.format('S15-26'))
sub(2689, '変数: 軍の政治力(MilitaryCoupWeight)', '変数: ~~軍の政治力(MilitaryCoupWeight)')
sub(2689, 'クーデタ支持(MilitaryCoupSupport)=0.6、', 'クーデタ支持(MilitaryCoupSupport)=0.6~~（(393) で削除）、')
line(2692, '#### ● 一党支配から独裁へ', '#### ~~● 一党支配から独裁へ~~')
line(2694, ST, '　~~**稼働中**~~ **削除（(384)・DominanceCoupWeight を物理削除）** ／ ~~`Politics.cs:633-650,675`~~')
sub(2695, '支配のクーデタ引力(DominanceCoupWeight)=2.5', '~~支配のクーデタ引力(DominanceCoupWeight)=2.5~~')
line(2700, ST, ST + '`Politics.cs:589 / Politics.cs:594`' + T.format('S15-27'))
line(2706, ST, ST + '`CrownParliament.cs:202 / CrownParliament.cs:210 / CrownParliament.cs:212` ／ `Politics.cs:605` ／ `World.cs:841`' + T.format('S15-24・S15-25'))
line(2712, '　**稼働中**（2026-09-29 (102)・(104)） ／ ', '　**稼働中**（2026-09-29 (102)・(104)） ／ `World.cs:848` ／ `CrownParliament.cs:207 / CrownParliament.cs:458` ／ `Politics.cs:557 / Politics.cs:467 / Politics.cs:471`' + T.format('S15-18・S15-19・S15-20'))
line(2717, ST, ST + '`Politics.cs:478（侵食が進行中＝この四半期に下がった） / Politics.cs:627 / Politics.cs:636 / Politics.cs:678 / Politics.cs:685`' + T.format('S15-16・S15-30・S15-31'))
line(2722, ST, ST + '`Politics.cs:741 / Politics.cs:768`' + T.format('S15-35'))
line(2728, ST, ST + '`Politics.cs:759`' + T.format('S15-36'))
line(2733, ST, ST + '`Politics.cs:725 / Politics.cs:732`' + T.format('S15-37'))
line(2739, ST, ST + '`Politics.cs:15` ／ `TechEducation.cs:207`' + T.format('S15-44'))
sub(2743, '24四半期の起債停止', '24か月の起債停止')
line(2744, ST, ST + '`Politics.cs:715 / Fiscal.cs:890`' + T.format('S15-38'))
sub(2745, '起債停止(DefaultCooldownQuarters)=24四半期', '起債停止(DefaultCooldownMonths)=24か月（旧名 DefaultCooldownQuarters）')
sub(2749, '失脚した集団は反革命の激しい不満を抱く（闘争度に直接+0.6）。', '~~失脚した集団は反革命の激しい不満を抱く（闘争度に直接+0.6）。~~（今の ResolveRevolt は闘争度に触れない・2026-11-06 (893)）')
sub(2749, '★ 勝者と中立の闘争度は半分になる。', '~~★ 勝者と中立の闘争度は半分になる。~~')
line(2750, ST, ST + '`Politics.cs:1919（ResolveRevolt） / Politics.cs:1924`' + T.format('S15-39'))
sub(2751, '失脚の不満(DeposedMilitancySpike)=0.6、勝者の緩和(ReformMilitancyRelief)=0.5', '~~失脚の不満(DeposedMilitancySpike)=0.6~~（宣言が無い）、勝者の緩和(ReformMilitancyRelief)=0.5（読み手は譲歩と鎮圧の 3 か所＝ResolveRevolt は読まない）')
line(2773, ST, ST + '`CrownParliament.cs:234 / CrownParliament.cs:238 / CrownParliament.cs:241 / CrownParliament.cs:252` ／ `Fiscal.cs:650-669`（旧の座標）' + T.format('S15-41'))
line(2779, '　**休眠** ／ `CrownParliament.cs:277-287,396-405`', '　**休眠** ／ `CrownParliament.cs:263 / CrownParliament.cs:346`' + T.format('S15-42') + ' ／ 変数: 導出化(NationConsciousnessDerived)=0（休眠）')
line(2784, ST, ST + '`CrownParliament.cs:411 / CrownParliament.cs:248`' + T.format('S15-43'))
sub(2785, '移民の運搬(MigrantCarriesConsciousness)=1.0', '~~移民の運搬(MigrantCarriesConsciousness)=1.0~~（2026-09-30 に焼き込みで削除＝墓標 Coefficients.cs:3163）')
line(2789, ST, ST + '`Politics.cs:709 / Politics.cs:1845`' + T.format('S15-40'))
line(2794, ST, ST + '`Politics.cs:711 / Politics.cs:852 / Politics.cs:201`（政体変更の実行部・決着の人と政治 `SettleRevolt`） ／ `Government.cs:946`' + T.format('S15-45'))
nb = '\n'.join(L)
assert len(nb.split('\n')) == len(b.split('\n'))
open(out, 'wb').write(nb.encode('utf-8'))
print('書いた', out, len(L), '行')
