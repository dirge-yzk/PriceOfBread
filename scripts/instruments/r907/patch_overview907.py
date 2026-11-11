# (907) c78lg：docs/ゲーム概要と実装機能.md の S25 の節（§3-8 外交・L5348〜L5872）だけを直した写しを作る
#  （行の数は変えない＝台帳の L の番号を動かさない・(900) patch_overview900.py の型）。App の座標（WarTab.axaml ほか）は確かめていない＝そのまま残す。
#  L263・L300（S19 の節の外・c78sm (d) の持ち分）には触れない＝assert で範囲を固定する。
# 使い方: python -X utf8 patch_overview907.py <今の文書> <s25_rows.py> <repo> <木> <出力>
import sys, re, subprocess, importlib.util
src, spec, repo, tree, out = sys.argv[1:6]
sp = importlib.util.spec_from_file_location('rows', spec); M = importlib.util.module_from_spec(sp); sp.loader.exec_module(M)
import os; sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_anchor; build_anchor.init(repo, tree)
b = open(src, 'rb').read().decode('utf-8')
assert '\r' not in b
L = b.split('\n'); L0 = list(L)
LO, HI = 5348, 5872
assert L[LO - 1].startswith('## 3-8. 外交') and L[HI].startswith('## 3-9.'), (L[LO - 1][:20], L[HI][:20])
T = '（2026-11-07 (907) に木 ' + tree + ' で取り直した・台帳 {}）'
touched = set()
def sub(n, a, c):
    assert LO <= n <= HI, n
    s = L[n - 1]
    assert s.count(a) == 1, (n, a, s[:80])
    L[n - 1] = s.replace(a, c); touched.add(n)
def line(n, start, new):
    assert LO <= n <= HI, n
    assert L[n - 1].startswith(start), (n, L[n - 1][:80])
    L[n - 1] = new; touched.add(n)
G = '（2026-11-07 (907)・台帳 {}）'
# ---- 文書が古い（§2d-2）----
sub(5413, '脅威の緊張率(ThreatTensionRate)=0.10', '脅威の緊張(ThreatTensionPerMonth)=0.10' + G.format('S25-03'))
sub(5425, '思想の緊張率(IdeologyTensionRate)=0.22', '思想の緊張(IdeologyTensionPerMonth)=0.22' + G.format('S25-05'))
sub(5431, '文明の緊張の有効化(EnableCivilizationTension)=有効', '~~文明の緊張の有効化(EnableCivilizationTension)=有効~~（2026-10-23 に焼き込み＝常に走る）')
sub(5431, '民族の緊張率(EthnicTensionRate)=0.06、法の緊張率(LawTensionRate)=0.05、宗教の緊張率(ReligionTensionRate)=0.03', '民族の緊張(EthnicTensionPerMonth)=0.06、法の緊張(LawTensionPerMonth)=0.05、宗教の緊張(ReligionTensionPerMonth)=0.03' + G.format('S25-06'))
sub(5437, '保護の緊張率(ProtectorTensionRate)=0.02', '保護の緊張(ProtectorTensionPerMonth)=0.02' + G.format('S25-07'))
sub(5442, '交易路依存の緊張率(RouteDependenceTensionRate)=0.10', '交易路依存の緊張(RouteDependenceTensionPerMonth)=0.10' + G.format('S25-08'))
sub(5448, '禁令の緊張率(OpiumBanTensionRate)=2.5', '禁令の緊張(OpiumBanTensionPerMonth)=2.5' + G.format('S25-09'))
sub(5459, '高緊張の減衰(HighTensionDecayRate)=1.0', '~~高緊張の減衰(HighTensionDecayRate)=1.0~~（(736) で削除＝×1.0 と門を外した・Coefficients.cs:1256 の注）' + G.format('S25-11'))
sub(5490, '束の併合(RankBracketsMerged)=1.0', '~~束の併合(RankBracketsMerged)=1.0~~（2026-10-18 に焼き込み）')
sub(5535, '到達を要求するか(WarNeedsReach)=1.0', '~~到達を要求するか(WarNeedsReach)=1.0~~（焼き込み）')
sub(5541, '同盟を数える(SubmitCountsAllies)=1.0', '~~同盟を数える(SubmitCountsAllies)=1.0~~（焼き込み）')
sub(5601, '拿捕率(PrizeMoneyRate)=0.004', '拿捕率(PrizeMoneyRatePerMonth)=0.004' + G.format('S25-35'))
sub(5607, '損失の按分(PrizeLossProrated)=1.0', '~~損失の按分(PrizeLossProrated)=1.0~~（(739) で焼き込み）' + G.format('S25-36'))
sub(5621, '従属の有効化(EnableDependency)=有効', '~~従属の有効化(EnableDependency)=有効~~（2026-10-23 に焼き込み）' + G.format('S25-38'))
sub(5626, '貢納率(VassalTributeRate)=0.015', '貢納(VassalTributeMonthGdpPerQuarter)=0.015' + G.format('S25-39'))
sub(5651, '介入の有効化(PeaceInterventionEnabled)=1.0', '~~介入の有効化(PeaceInterventionEnabled)=1.0~~（2026-10-18 に焼き込み）')
sub(5672, '植民地奪取の射程ゲート(ColonyCaptureReachGate)=1.0', '~~植民地奪取の射程ゲート(ColonyCaptureReachGate)=1.0~~（焼き込み）')
sub(5681, '⚠ 条約港1つにつき関税自主権が1段失われ、', '⚠ ~~条約港1つにつき関税自主権が1段失われ~~（(399)：関税が効かない割合＝港州の外国持分の人口加重平均・InternationalMarket.cs:166' + G.format('S25-49') + '）、')
sub(5683, '1港あたりの関税喪失(TreatyPortTariffLoss)=0.25', '~~1港あたりの関税喪失(TreatyPortTariffLoss)=0.25~~（(399) で削除＝関税が効かない割合は港州の外国持分）')
sub(5683, '条約港の持分(ForeignRuleFromTreaty)=0.0（休眠）', '条約港の持分(ForeignRuleFromTreaty)=0.5' + G.format('S25-49'))
sub(5686, '国力（軍事＋生産）が最強列強比で一定以上へ回復した国の条約港が、4年ごとの一斉検査で1港ずつ風化する。',
    '国力（世界の総量に対する割合）が列強の中央値に近づくほど、外国の持分が毎四半期その比÷0.5 に比例して風化する（★(416)(449)(456)・4年の刻みと1港ずつは無い' + G.format('S25-50') + '）。')
sub(5687, '⚠ 4年の刻みは、2026-09-06 の恒久方針が置き換え候補として名指しした時間の刻み（棚卸し econ-27・dip-05）。', '⚠ ~~4年の刻みは、2026-09-06 の恒久方針が置き換え候補として名指しした時間の刻み（棚卸し econ-27・dip-05）。~~ → 刻みは四半期（Diplomacy.cs:532）。')
sub(5689, '失効の刻み(TreatyPortLapseYears)=4.0年', '~~失効の刻み(TreatyPortLapseYears)=4.0年~~（削除・RuleSharesFormTests.cs:359 の消えた名）')
line(5734, '#### ● 遠征技術のゲート', '#### ~~● 遠征技術のゲート~~')
line(5736, '　**稼働中** ／ `Diplomacy.cs:1968-1976`', '　~~**稼働中**~~ **門は無い（ExpeditionLift は常に 1.0＝(613) 段3・TechMultipliersOff は (843) で焼き込み）** ／ `Diplomacy.cs:2278（ExpeditionLift）`' + T.format('S25-56'))
sub(5737, '遠征技術の水準(ExpeditionTechLevel)=0.8、遠征技術の床(ExpeditionTechFloor)=0.15', '~~遠征技術の水準(ExpeditionTechLevel)=0.8、遠征技術の床(ExpeditionTechFloor)=0.15~~（(843) で削除・DormantIdentityTests.cs:334）')
sub(5816, '押し付けの有効化(ImposeRegimeEnabled)=1.0', '~~押し付けの有効化(ImposeRegimeEnabled)=1.0~~（焼き込み）')
line(5845, '#### ○ 開戦の政治的帰結', '#### ~~○ 開戦の政治的帰結~~')
line(5848, '　**休眠** ／ ', '　~~**休眠**~~ **削除（(271) の休眠 8 本の削除・DormantIdentityTests.cs:263〈戦争の実体の被害で表す〉）** ／ ~~`Diplomacy.cs:1435-1444,1794-1796`~~' + G.format('S25-74'))
sub(5849, '開戦の政治スケール(WarStartPoliticsScale)=0.0', '~~開戦の政治スケール(WarStartPoliticsScale)=0.0~~')
# ---- 座標（2026-09-26〜10 月の座標を台帳の錨で取り直す）----
heads = [i for i in range(LO, HI + 1) if L0[i - 1].startswith('#### ')]
byL = {r[1]: r for r in M.ROWS}
SPAN = re.compile(r'`([A-Za-z]+\.cs):([0-9][0-9,\-]*)((?:（[^`）]*）)?)`')
coord_rows = []
for k, h in enumerate(heads):
    if h not in byL: continue
    rid = byL[h][0]; an = [build_anchor.resolve(a) for a in byL[h][4]]
    e = heads[k + 1] if k + 1 < len(heads) else HI + 1
    st = [n for n in range(h + 1, e) if L0[n - 1].startswith('　**') or L0[n - 1].startswith('　~~**')]
    if not st: continue
    n = st[0]
    if n in touched: continue
    s = L[n - 1]; hit = [False]
    def rep(m):
        fn = m.group(1)
        ln = [a.split(':')[1] for a in an if a.split(':')[0] == fn]
        if not ln: return m.group(0)
        hit[0] = True
        return '`' + ' / '.join(f'{fn}:{x}' for x in ln) + m.group(3) + '`'
    s2 = SPAN.sub(rep, s)
    if hit[0]:
        L[n - 1] = s2 + T.format(rid); touched.add(n); coord_rows.append((n, rid))
nb = '\n'.join(L)
assert len(nb.split('\n')) == len(b.split('\n'))
for i, (x, y) in enumerate(zip(L0, L), 1):
    if x != y: assert LO <= i <= HI, i
open(out, 'wb').write(nb.encode('utf-8'))
print('書いた', out, len(L), '行・直した行', len(touched), '・座標を取り直した行', len(coord_rows))
print('座標', ' '.join(f'L{n}={r}' for n, r in coord_rows))
