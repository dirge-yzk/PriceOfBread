# 読むだけ (1182) c117rs：11-12 の S11 の 37 行を、台帳の id（S11-Hnnn＋枝の字）・今の木 c590d21ba の錨・写しと読み直しの別で 12 列の TSV に書く。
#  (1176) の mk_tsv_s10.py の形を写した。入力：11-12 の tsv（in_S11_1112.tsv）・out_cmp_S11.tsv。出力：ledger_S11_2026-11-16.tsv（12 列）と side_S11_2026-11-16.tsv（写しの別）。
#  倍率の一掃（11-11）・天井と床・セーブの往復の穴の行は、12 列目に印をつけるだけ（処置はその線で決める）。
# 使い方: python -X utf8 -I mk_tsv_s11.py <作業フォルダ>
import sys, os, re, collections
W = sys.argv[1]
OLD, NEW = '9b18137a', 'c590d21ba'
MAP = {'H001': [1, 2, 3, 4], 'H002': [5, 6], 'H003': [7, 8, 10, 11], 'H004': [9], 'H005': [12, 13], 'H006': [14], 'H007': [15, 16],
       'H008': [17, 18, 19], 'H009': [20, 21], 'H010': [22, 23], 'H011': [24], 'H012': [36], 'H013': [37],
       'H014': [25, 26, 27, 28, 29, 30, 32, 33, 34, 35], 'H015': [31]}
def newid(k):
    for h, ks in MAP.items():
        if k in ks: return 'S11-' + h + ('' if len(ks) == 1 else 'abcdefghijklm'[ks.index(k)])
    raise KeyError(k)
IDS = {k: newid(k) for k in range(1, 38)}
assert len(set(IDS.values())) == 37
ANCH = {  # cmp_rows で取り直せなかった物・係数の宣言の名の取り違え
 'S11-34': 'Coefficients.p10_ai_silver_overrides.cs:60:EraFactor;InternationalMarket.cs:114:FreightRevolution;BilateralMarket.cs:673:FreightRevolution',
}
CUR = '（今の生 M＝keep_1178/base_ins・N1／N2＝keep_1178/n1・n2〔絞った表〕・N1′／N2′＝keep_1150/n1・n2〔全表のヌル腕・木 d05c6bea9〕・母集合 A＝年末の全ての国〔q%4==3〕・B＝計器の国の毎四半期・monlaw は全ての国の毎四半期・台本 r1182 の s11_v.py）'
AI = '〈AI の推奨・確証なし〉'
MULT = '印：倍率の一掃（2026-11-11 の取りまとめ・技術水準 15 本は区分 A〈大〉＝AI の分類・EliteForFullTech は問い10・IrreversibleLaws は個別の 37 本）＝処置はその線で決める。11-12 の読みの処置：'
CEIL = '印：天井と床・倍率の一掃＝処置はその線で決める。11-12 の読みの処置：'
MARK = {**{f'S11-{k:02d}': MULT for k in (17, 18, 19, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 33, 35)}, 'S11-32': CEIL}
# 読み直した行：(判定の型 or None, 判定, 根拠, 12 列目 or None＝元のまま)
RE = {
 'S11-01': (None, '働く', '〈読み直し〉囲む関数 EconomyStep.Run の差は大きさ 0 の POP を飛ばす枝（EconomyStep.cs:1612 の skipZeroPop＝必要量×0 と同じ）ほか教育の外。生 ed の needPerMonth が 0 以下：M A 0/102,515・B 0/60,120・N1′ 0/102,237・N2′ 0/102,858。必要量は年齢の層 AgeShare(5, 15) を読む（ソース）', None),
 'S11-05': ('届かない門', '一部だけ働く（本番 0・開幕の前の助走の 20 四半期だけ）',
   '〈読み直し〉本番：reform の教育法 1（宗教学校）の行 M A 0/102,515・B 0/60,120・N1 A 0/102,639・N2 A 0/102,415（全て公教育 2）。壁の一歩（wallPassAxes に EducationLaw）は 1783 年の中だけ M A 450・B 60（11-12 は 464・60）。助走：Simulation.cs:1857 の担い手の切り替えは WarmupFrozen で止まらない（Simulation.cs の WarmupFrozen の行に教育の区画は無い）・MultiNationSim.cs:434〜435 が WarmupEconomy の後に WarmupTech＝11-12 の反証 2-2 の読みは今の木でも成り立つ（助走は計器が書かない＝生では確かめていない）', None),
 'S11-06': ('両側の門', '一部だけ働く（公教育の枝だけ・学校なしの枝 0）',
   '〈読み直し〉reform の教育法は全て 2（S11-H002a）。生 ed の供給÷必要の 25 年ごとの平均 M A 0.179・0.269・0.444・0.424・0.376・0.213・0.156／B 0.184・0.202・0.253・0.319・0.367・0.235・0.216（N1′ A 0.178〜0.443・N2′ A 0.172〜0.434）＝教師の産出が市場に出る。⚠ 1 つの生産者の担い手と投入を毎月書き換える形（文書 pop-18・dorm-11）', None),
 'S11-07': ('両側の門', '一部だけ働く（義務教育の満額の枝 0・公教育の 2 割の枝だけ）',
   '〈読み直し〉生 ed：国庫の買いたい分＞0 M A 98.9%・B 98.9%／取れた分＞0 A 76.9%・B 91.2%（N1′ A 77.8%・N2′ A 77.2%）・国庫の取れた÷必要の 25 年ごとの平均 A 0.002→0.051。monlaw（10 種・全ての国の毎四半期 410,034 行）で教育法→目標が 2→2 の行が全て＝義務教育を目標に置いた行 0（N1 410,530・N2 409,636 も全て 2→2）。根（11-12 の反証 2-1 を今の木で確かめた）：義務教育を目標に置く行は SelfPlay.cs:933〜934 の 1 か所で case Stance.Modernize（SelfPlay.cs:931）の中＝modsig の姿勢 0（近代化）は M A 0/102,515・B 0/60,120（N1′ 0・N2′ 0）＝国庫＞緩衝×CompulsoryEduBufferMult の門は一度も評価されない', None),
 'S11-08': ('効き', '働く（国庫の割合は必要の 0.2〜5%）',
   '〈読み直し〉reform の公教育の行で parishFundingShare＞0：M A 101,357/102,515・B 59,474/60,120（N1 A 101,481・N2 A 101,255）。大きさ：生 ed の国庫の取れた÷必要は A 0.002〜0.051（25 年ごと）。〈宗教学校も 2 割〉の枝は本番 0（S11-H002a）・助走だけ。⚠ 文書は〈公教育でも〉だけ', None),
 'S11-09': ('効き', '文書が古い', '〈読み直し〉囲む関数 MonthSettle の差は巨大建造物の請求と駐屯の調達の削除（教育の外）＝Simulation.cs:2009 で r.CompulsoryEduFunding に、:2010 で HigherEduFunding に軍事費・行政費と同じ r を掛ける＝働いている。請求の和（Simulation.cs:1930）から MonumentBuildQueue が消えた＝r の分母が変わった（教育の請求の扱いは同じ）。文書の見出しは消えた器 EduFundingCashClamp の名のまま', None),
 'S11-10': ('効き', '文書が古い', '〈読み直し〉ソース：Simulation.cs:1835 で常に RuleReach を掛ける（(399) で旗を物理削除）。囲む関数の差は教育の外（S11-H004 と同じ）。文書の〈休眠〉は古い', None),
 'S11-11': ('効き', '働かない', '〈読み直し〉係数 EduReachFromAdmin 0（Coefficients.cs:71）＝EconomyStep.cs:1780 の枝は ×1.0。囲む関数 Run の差は教育の外。読み手：エンジン 1（EconomyStep.cs:1780）＋宣言・計器 0・試験 3 ファイル・App 0',
   '消す〈AI の推奨・確認待ち〉恒等の線：読み手 エンジン 1（EconomyStep.cs:1780）・計器 0・試験 3 ファイル・App 0。失う物＝無し（係数 0）。消し方＝係数と枝を消し試験は ★(743) ① で消す。行政の効き目は法の浸透 StateReach と税の届けに既にある'),
 'S11-12': ('効き', '働かない', '〈読み直し〉係数 EduProducersCoexist 0（Coefficients.p02_borrow_polity_epidemic.cs:31）＝EconomyStep.cs:928・Simulation.cs:1122・:1841 の枝は通らない。〔11-12 の反証 2-6 の理由の訂正〕崖（法の 1 段で聖職者の教育の収入が全額↔0）は宗教学校で始まる国の全てで開幕の 1 年の中に 1 度起きる（S11-H002a）＝〈本番で起きない〉は誤り。常設にしても使わない側の Scale は 0（Simulation.cs:1841 の後）',
   '残す（S11-D1 の答えの後に判断）〈AI の推奨・確認待ち〉：公立学校と教区学校の並立は 19 世紀の史実の形＝消す側に入れない。読み手 エンジン 3 ファイル 4 か所・計器 0・試験 2 ファイル・App 0'),
 'S11-13': ('効き', '働かない', '〈読み直し〉係数 TeacherQualityWeight 0（Coefficients.cs:70）＝Simulation.cs:1849・:1870 は不読。高等教育の必要量 0（HigherEduNeedPerEducated 0・生 ed の高等教育の買いたい分＞0 が M A 0/102,515・B 0/60,120・N1′ 0・N2′ 0）＝点火すると q＝1−w で教師の生産性を下げるだけ（式の形から）',
   '消す〈AI の推奨・確認待ち〉恒等の線：読み手 エンジン 1 ファイル 2 か所（Simulation.cs:1849・:1870）・計器 0・試験 1・App 0。失う物＝点火していない器（作者 2026-09-06 に採った）。消し方＝係数と 2 か所の枝。高等教育の線を点火する時に要るなら作り直す'),
 'S11-15': ('効き', '働く', '〈読み直し〉エンジンの式（Simulation.cs:1715：毎月 Toward(x, 目標, 0.15×(締める向きなら StateReach・緩めるなら 1))）で、計器の国の続いた四半期（法の段が同じ）から 1 か月の詰まり k＝1−(1−四半期の詰まり)^(1/3) を出し 0.15 で割った：締める向き M 15,150 件・中央 0.264（下位 1 割 0.024・上位 1 割 0.600）＝admrule の stateReachShare との差の中央 −0.000（下位 1 割 −0.010・上位 1 割 0.005）＝行の一致（N1 15,408 件・N2 15,055 件も差の中央 −0.000）。11-12 の中央 0.105（7,603 件）は台本が残っておらず数え方を再現できない（未確かめ）。文書の変数 LawEnforcementAdminGate・WorkHoursFollowSpeed は削除済み＝今は WomenRightsFollowSpeed 0.15 を 3 本の法で共有', None),
 'S11-16': ('片側だけの門', '働く', '〈読み直し〉同じ数え方で緩める向き M 1,194 件・中央 1.000（下位 1 割 0.999）・N1 1,203 件・N2 922 件も中央 1.000（11-12 は 334 件で全て 1.000）', None),
 'S11-17': ('両側の門', '一部だけ働く（社会保障だけ）',
   '〈読み直し〉lawgate2（段の法の施行の最後の結果・真偽の軸は覆わない）で符号 5〈不可逆で固定〉の行：M 1,332 行・国×種×軸 19（全て社会保障・理由 20）・N1′ 1,874 行・34・N2′ 1,245 行・22。参政権・女性の権利・労働時間・児童労働は 0（11-12 の反証 2-4〈社会保障だけ 2,084 行・31〉と同じ形）。lawgate2 の理由の符号 20・21 はこの錠の結果を読む計器の読み手＝計器 1', None),
 'S11-18': ('届かない門', '働かない（M で錠に届く場面 0）',
   '〈読み直し〉Simulation.cs:1724 の錠は法が許可で合法度の方が低い（childRegress）時だけ評価。monlaw（全ての国の毎四半期）の児童労働の法の移り：M 禁止へ 35・許可へ 0／N1 36・0／N2 41・2（N2 の 2 件で錠が止めたかは計器に列が無い＝未確かめ）。11-12 の反証 2-5〈届かない門〉を今の生で確かめた', None),
 'S11-22': ('効き', '一部だけ働く（1783 年の後は水路 1・助走の間は宗教学校で始まる国が 0.1）',
   '〈読み直し〉本番は全国が公教育（S11-H002a）＝担い手は教師＝水路 1＝Elite は平均識字。助走の 20 四半期は担い手が聖職者（S11-H002a の読み直し）＝EliteMultiplier(Religious)＝0.1・その差を WarmupTech（MultiNationSim.cs:435）が開幕の技術水準の差にする（11-12 の反証 2-2）。Elite の読み手：エンジン TechEducation.cs（GrowTech・AbsorptionOf・FrontierCapable）と建国の写し・Runner 2', None),
 'S11-23': ('効き', '働く', '〈読み直し〉読み手は発見の特許の行の必要性 1 本（Discovery.cs:159 `if (row == PatentRow) s = w.EliteCore`）。〔11-12 の反証 2-7 の訂正〕他の制度の行の必要性は工業化度・闘争度・植民地の割合などで識字ではない＝直す向きは〈EliteCore を PoliticalQuantities.AvgLiteracy(w, coreOnly: true)〔PoliticalQuantities.cs:50〕へ置き換える〉だけ（1783 年の後は水路 1＝ほぼ恒等）。特許の解禁の数（11-12 の disc 種1〜3 で 177 件）は数え直していない', None),
 'S11-24': ('効き', '文書が古い', '〈読み直し〉囲む関数 Run の差は教育の外。AbsorptionOf（TechEducation.cs:13）の読み手 3＝後追いの天井（TechEducation.cs:55）・外資の与信（CapitalMarket.cs:63）・私的投資（EconomyStep.cs:1070）。行政の脚は (383) で物理削除＝今は 2 項', None),
 'S11-28': ('両側の門', '一部だけ働く（1783 年の後は公教育の脚が常に真・WarmupTech の時は宗教学校で始まる国で偽）',
   '〈読み直し〉公教育の脚（TechEducation.cs:126）：本番の教育法は全て公教育（S11-H002a）＝判定は自由かエリートの代替だけで決まる。助走の後の WarmupTech の時は宗教学校の担い手のまま＝脚が偽（11-12 の反証 2-2）。開く／閉じるの割合（11-12 の近似で 75,950／71,223）は Freedom・FrontierCapable の列が計器に無いので数え直していない', None),
 'S11-33': ('効き', '働く', '〈読み直し〉囲む関数の差は教育の外。括弧の旗 TechProductivityBracketOff 0（Coefficients.p08_audit_soldiers.cs:134）＝今も掛かる。TechProductivity の生きた読み手：工場の能力（Simulation.cs:2011）と私的投資の収益の梃子（EconomyStep.cs:2578 の InvestmentReturn の productivityMultiplier）・計器 modsig 1（InstrumentsModernize.cs:22）。App の表示（11-12 の GameViewModel.Fiscal.cs:116）は今の木に無い＝App 0', None),
 'S11-34': ('読まれない欄', '働かない', '〈読み直し〉EraFactor は常に 1.0（Coefficients.p10_ai_silver_overrides.cs:60）・FreightRevolution も常に 1.0（InternationalMarket.cs:114〜115）。11-12 の後に (1160) で呼び手が減った（Colonial.cs:32・Transport.cs:17 は定数 1.0 へ・World.cs:1956 の注）＝今の呼び手：EraFactor はエンジン 3 ファイル 4 か所（Territory.cs:70・:73・Military.cs:179・MultiNationSim.cs:1065）・App 1（GameViewModel.Politics.cs:636）／FreightRevolution はエンジン 2（BilateralMarket.cs:673・MultiNationSim.cs:523）・計器 1（InstrumentsScalars.cs:41）。WorldTechLevel の生きた読み手は後追いの天井（TechEducation.cs:60）だけ',
   '消す〈AI の推奨・確認待ち〉恒等の線：読み手 エンジン 3＋2 ファイル（EraFactor 4 か所・FreightRevolution 2 か所）・計器 1（scalars の freightRevolution 列＝常に 1）・試験 5＋2 ファイル・App 1（艦の石炭の表示）。失う物＝無し（常に 1.0）。消し方＝呼び手を 1.0 の定数へ畳み（Colonial.cs・Transport.cs と同じ形）関数と Era* の係数を消す・計器の列は消すか〈全表の一致は取れない〉と名乗る'),
 'S11-35': ('効き', '働く', '〈読み直し〉ソース：助走の後に呼ぶ（MultiNationSim.cs:435 WarmupTech(StartWarmupTechIterations 8)）。差の種は助走の間の宗教学校の水路（S11-H010a・11-12 の反証 2-2）', None),
}
CMP = {}
for l in open(os.path.join(W, 'out_cmp_S11.tsv'), encoding='utf-8').read().split('\n')[1:]:
    c = l.split('\t')
    if len(c) >= 4: CMP[c[0]] = c
L = open(os.path.join(W, 'in_S11_1112.tsv'), 'rb').read().decode('utf-8').split('\n')
hdr = L[0].rstrip('\r').split('\t'); out = ['\t'.join(hdr)]
side = ['\t'.join(['行', '11-12 の行', '区分', 'コードの差（注と空白を除く・錨を囲む関数）', '11-12 の判定', '今の判定', '確証'])]
FIX11 = {}  # 11-12 の判定の写しで 12 列目を直す行（無し）
def ref_old(s):
    return re.sub(r'S11-(\d\d)\b', lambda m: IDS[int(m.group(1))] if 1 <= int(m.group(1)) <= 37 else m.group(0), s)
for l in L[1:]:
    c = l.rstrip('\r').split('\t')
    if len(c) < 12: continue
    old = c[0]; k = int(old.split('-')[1]); nid = IDS[k]
    cm = CMP.get(old)
    anch = ANCH.get(old, cm[2] if cm and cm[2] else c[4])
    oldj = c[8]; diff = (cm[1] + ('：' + cm[3] if len(cm) > 3 and cm[3] else '')) if cm else '—'
    if old in RE:
        kind_, j, basis, fx = RE[old]
        c[7] = NEW
        if kind_: c[6] = kind_
        c[8] = j; c[9] = basis + CUR
        if fx is not None: c[11] = fx
        kind = '読み直し'; conf = '生とソース（今の木 c590d21ba・keep_1178・keep_1150）'
    else:
        c[7] = OLD; c[9] = '〈写し・11-12 の読み（木 9b18137a・生 keep_1053）〉' + c[9]
        kind = '写し'; conf = '11-12 の読み（keep_1053）＝今の生では数え直していない・錨を囲む関数は今の木まで字が同じ（cmp_rows.py）'
    if old in MARK: c[11] = MARK[old] + c[11]
    c[0] = nid; c[4] = anch
    c[9] = ref_old(c[9]); c[11] = ref_old(c[11]); c[10] = ref_old(c[10])
    for i in range(12): c[i] = c[i].replace('|', '／').replace('\t', ' ').replace('\n', ' ')
    out.append('\t'.join(c))
    side.append('\t'.join([nid, old, kind, diff.replace('\t', ' ')[:300], oldj, c[8], conf]))
open(os.path.join(W, 'ledger_S11_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
open(os.path.join(W, 'side_S11_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(side) + '\n')
print('行', len(out) - 1, '区分', collections.Counter(s.split('\t')[2] for s in side[1:]), '判定', collections.Counter(x.split('\t')[8].split('（')[0] for x in out[1:]))
print('12 列目', collections.Counter(x.split('\t')[11][:2] for x in out[1:]))
