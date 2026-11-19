# (1233) 注：GuildMobilityDrag は c124kb (1227) で消えた＝字面で読む古い台本（回し直すと台帳 S12-H005b の〈消した〉を上書きするおそれ）
# 読むだけ (1189) c118dl：11-12 の S12 の 36 行を、台帳の id（S12-Hnnn＋枝の字）・今の木 ef7db3cc8 の錨・写しと読み直しの別で 12 列の TSV に書く。
#  (1182) の mk_tsv_s11.py の形を写した。入力：in_S12_1112.tsv・out_cmp_S12.tsv。出力：ledger_S12_2026-11-17.tsv（12 列）・side_S12_2026-11-17.tsv（写しの別）。
#  倍率の一掃（11-11）の行は 12 列目に印をつけるだけ（処置はその線で決める）。11-12 の反証（ledger_S12_2026-11-12_反証.md）の直しを入れた。
# 使い方: python -X utf8 -I mk_tsv_s12.py <作業フォルダ>
import sys, os, csv
W = sys.argv[1]
OLD, NEW = '9b18137a', 'ef7db3cc8'
MAP = {'H001': [1], 'H002': [2], 'H003': [3, 4, 5], 'H004': [7], 'H005': [8, 9], 'H006': [10, 11, 12, 13], 'H007': [14], 'H008': [15],
       'H009': [16], 'H010': [17, 18], 'H011': [19], 'H012': [20], 'H013': [21], 'H014': [22, 24, 25, 26, 27, 28, 30], 'H015': [31],
       'H016': [23, 29], 'H017': [6], 'H018': [32], 'H019': [33], 'H020': [34], 'H021': [35], 'H022': [36]}
def newid(k):
    for h, ks in MAP.items():
        if k in ks: return 'S12-' + h + ('' if len(ks) == 1 else 'abcdefghijklm'[ks.index(k)])
    raise KeyError(k)
IDS = {k: newid(k) for k in range(1, 37)}
assert len(set(IDS.values())) == 36
CUR = '（今の生 M＝keep_1184/base_ins〔計器つき 10 種・全表〕・N1／N2＝keep_1184/n1・n2〔絞った表＝class・scalars だけ〕・母集合は行ごとに名乗る・台本 r1189 の s12_v.py・s12_art.py）'
SRC = '〈読み直し・ソース〉錨の行は今の木 ef7db3cc8 にある。囲む関数の差（注と空白を除く・cd_*.txt）は S12 の外＝Meritocracy.Step と FillJobs は兵の空の席の所得（旗 SoldierVacancyFromZero 1・(1166)）と計器の口だけ・EconomyStep.Run／Simulation.MonthSettle／Colonial.Step の差に社会移動・失業・農奴・職人・商人・聖職の語 0＝11-12 の読み（ソース）を保つ。'
SV = '〔(1166) の後：プールの着地の候補に、人数 0 の兵の席が入る＝EmptySeatIncome（Meritocracy.cs:441〜:447・国の兵が受け取った率×徴兵の賃金）で所得を見る＝着地の重みの母集合が 11-12 と違う〕'
AI = '〈AI の推奨・確認待ち〉'
MULT = '印：倍率の一掃（2026-11-11 の取りまとめ）＝処置はその線で決める。'
# 読み直した行：(判定の型 or None, 判定 or None, 根拠, 残りタスク or None, 12 列目 or None＝元のまま)
RE = {
 1: (None, None, SRC, '00-BUFF 順 3・10-18 精査 人13', MULT + 'MeritGroupWeight＝A（§4 の A の一覧）・政体の表 PolityModernity は文書の外。11-12 の反証 F3：Q1 は〈00-BUFF 順 3 で M を代用なしで外すか、代用を設計するか〉の問い'),
 2: (None, None, SRC, None, None),
 3: (None, None, SRC, None, None),
 4: (None, None, '〈読み直し・生〉scalars_s の教育法（X＝全ての国×四半期）：公教育（2）以外 M 0/147,395・N1 0/148,275・N2 0/148,057＝水路は常に 1.0・宗教学校の 0.05 となしの 0 は届かない（S11-H002a・S11-D2 と同じ根＝近代化の姿勢が 0 回）' + CUR, None, None),
 5: (None, None, SRC, None, None),
 6: (None, None, SRC, '00-BUFF 順 3・10-18 精査 人13', MULT + 'MinorityPenaltyModernBlend＝A（17）。11-12 の推奨〈消す（iii）〉は M（S12-H001）と同じ腕にする（反証 3-2）'),
 7: (None, '一部だけ働く', '〈読み直し・生〉merch_s（M＝計器の生の全ての州×四半期・10 種 2,181,870 行）：lockBOpen＞0 9.50%（11-12 8.5〜12.2%）・空の職人で止まる lockABlockedArtisan＞0 4.27%・職人が空 31.7%・職人への入口 artisanPettyIn＞0 1.11%・和 4,945.5K（10 種＝種あたり 0.49K・11-12 は種あたり 451〜764K＝約 1/1000・単位の変更か挙動の変化かは未確かめ）・職人の転落の和 579,925K（種あたり 58.0M）・商人への入口の和 40,442.5K（種あたり 4.04M）・商人の転落の和 545,973K（種あたり 54.6M）＝入口と出口の両側は出る・職人の入口は出口の 1/10⁵' + CUR, None,
     '直す' + AI + '：元手の門を転落の錨と同じ FallAnchor で書く（新しい数を作らない・〈入る＝資産が MiddleFallThreshold×錨を超える〉）＝入口と出口の対・プールの着地の職人の資格（JobGate）と同じ式に揃える（片方だけ直すと分子と分母の非対称）・空の職人の行き先を開くと魅力が 1 に張り付く跳ねを先に読む（11-12 の反証 F1）'),
 8: (None, None, SRC, None, None),
 9: (None, None, SRC + '係数 GuildMobilityDrag 0・主フラグ GuildLawGate 0（Meritocracy.cs:122 の門が閉じる）', '10-18 精査 政4',
     '消す（i・恒等の線）：GuildMobilityDrag だけ（10-18 精査 政4〈消してよい〉）・法の軸（GuildLawGate・GuildRentPremium）は史実で重要（同業組合の廃止）＝残す〈AI の推奨〉（読み手 エンジン 1〔Meritocracy.cs:122〕・試験 2〔GuildLawTests・DormantIdentityTests〕）＝問い 1 の表 K10′'),
 10: (None, None, SRC + SV, None, None),
 11: (None, None, SRC + SV, None, None),
 12: (None, '未確かめ', '〈読み直し・ソース＋生〉ソースの形（残りは所得の最良の 1 つへ全量）は 11-12 のとおり。強制の残りに届いた割合を分ける列は無い（計器 mobility_s 待ち）。症状の数（記録）：class_s（K＝計器の国×時点・open を除く）の医者が人口の 1% 超 M 6,247/148,005（4.2%）・2% 超 1,171（N1 6,858・1,199／N2 7,357・1,733）＝11-12 の 2.6% より多い。11-12 の反証 2-2 の直し（〈一部だけ働く〉→〈未確かめ〉）を写した' + CUR, '(540)', None),
 13: (None, None, '〈読み直し・ソース〉UrbanHeadroom（Meritocracy.cs:330〜:337）は旗 UrbanCapOff の焼き込みで常に +∞（組の人数 0 の州だけ 0）＝今の木でも同じ。生の urxpd_s の capBound 0（M 0/1,976,850 州×月）は計器の中で偽に固定（11-12 の反証 2-2）＝証拠にしない', None, None),
 14: (None, None, SRC, None, None),
 15: (None, None, SRC, None, None),
 16: (None, '一部だけ働く', '〈読み直し・生〉svc_s（S＝計器の国×四半期・作った＞0 の 60,120 行）で、エンジンの式 unsold＝1−売れた÷作った（EconomyStep.cs:2527・州ごと）を国の和で近似：使用人の売れ残り＞0 24,706（41.1%）・＞0.1 529（0.88%）（11-12 は 1.1〜2.3%）＝失業として立つ量は小さい。教師・医者・官僚の側は列が無い（未確かめ）。11-12 の反証 2-2 の直し（〈働く〉→〈一部だけ働く〉）' + CUR, None,
     '消す（i・恒等の線＝旗の焼き込み）：ServiceUnemployment＝1.0 は満額の旗（×1.0）＝焼いて式だけ残す（読み手 エンジン 2〔EconomyStep.cs:2519・:2528〕・試験 0）＝問い 1 の表 K12'),
 17: (None, None, '〈読み直し・生〉urxpd_s（U＝計器の国の州×月の和 1,976,850）：農奴の栓で縮んだ州×月 266,262（13.5%・11-12 は 11.3〜12.2%）＝縛られる側と自由な側が両方出る（計器は同じ式を組み直す）' + CUR, None, '—'),
 19: (None, None, SRC + '係数 SerfGateCoversPool 0＝Meritocracy.cs:301 の門が閉じる（文書どおりの休眠）', None,
     '直す' + AI + '：点火の前に形を直す＝栓は〈強制でない流れ〉の農村の送り手→都市の職の着地だけ（FillJobs の srcRural && t.IsUrban の位置）・今の :301〜:304 は流れの全量（強制の転落・兵の復員まで）を縛る＝片方向の錠になる（11-12 の反証 F4）・その後に点火して旗を焼く（読み手 エンジン 2〔Meritocracy.cs:301・:304〕）'),
 20: (None, None, '〈読み直し・生〉scalars_s の教育法（X）：宗教学校の国×四半期 M 0/147,395・N1 0/148,275・N2 0/148,057＝門の手前で止まる（S12-H003b と同じ根）。聖職者の推移は 11-12 の読み（出生の配分の凍結分母 H25 の推測）のまま' + CUR, None,
     '残す（上流待ち）：教育法が動けば効く・動かないと決まれば消す（iii＝聖職への入口は所得の選択から出す）（読み手の直し：ClergyAttractThreshold 試験 0・ClergyJoinRatePerQuarter 試験 2〔UnitTypeTests〕＝11-12 の反証 3-1）'),
 21: (None, None, SRC, None, None),
 22: (None, None, '〈読み直し・生〉専門職の道の列は無い。merch_s の商人→資本家 merchCapRiseOut の和 27,675.5K（10 種＝種あたり 2.77M・11-12 は種あたり 1.66〜2.58M）＝資本家へ入る道の大きさ（記録）' + CUR, '(540)', None),
 23: (None, '一部だけ働く', '〈読み直し・生〉merch_s（M）：商人→資本家 merchCapRiseOut＞0 258,738/2,181,870（11.9%・11-12 11.3〜12.8%）・和 種あたり 2.77M＝商人の側は門の両側が出る。職人の側は職人が空の州×四半期 31.7%・英の職人は 1800 年に 0.7K（中央）＝届く相手がいない。11-12 の反証 2-2 の直し（〈働く〉→〈一部だけ働く〉）' + CUR, None,
     '消す（i・恒等の線＝旗の焼き込み）：MerchantArtisanCapitalRise＝1.0 は満額の旗＝焼いて式だけ残す・MobilityTests.cs が旗を切る道具に使う＝試験の書き直しが要る（読み手 エンジン 3〔Meritocracy.cs:178・:189・:248〕・試験 1）＝問い 1 の表 K14'),
 24: (None, None, SRC, '(540)', None),
 25: (None, None, '〈読み直し・生〉class_s（K＝計器の国×時点・open を除く）で、開幕に聖職者が 0.2% 以上の国の国×時点 134,190 のうち 0.2% を割る行 M 13,283（9.9%）・N1 13,644・N2 12,643（11-12 は 12.3%・反証 12.0%）＝下限は ④ だけ・②\'\' の道で割れる形は保つ' + CUR, '棚卸し 灰・人口の段C（人4）の後',
     MULT + 'ClergyFloorShare＝A（25）。11-12 の反証の推奨：今は消さない（教会は消えないは史実で重要・人口の段C の人4 InitialFertileShare の後・MobilityTests.cs:145 が下限に頼る）'),
 26: (None, None, SRC, '(540)', None),
 27: (None, None, SRC + '係数 PopDebtDriven・DebtRuinRatio は初期化子が無い＝0（cmp_rows の〈係数が消えた〉は初期化子の無い宣言の読み落とし）＝Meritocracy.cs:214 は常に偽', None,
     '残す：個人債務の器の持ち主は S22（S22-H048）＝S22 の線が点火か削除を決める（読み手 エンジン 7 行〔Simulation.cs 3・EconomyStep.cs 2・Meritocracy.cs 2〕・試験 4）'),
 28: (None, None, SRC, '(540)', None),
 29: (None, None, '〈読み直し・生〉merch_s（M）：商人の転落 merchFallOut＞0 587,433（26.9%・11-12 24.8〜30.6%）・門の内 merchFallGateIn＞0 591,608（27.1%）・和 種あたり 54.6M＝入口（種あたり 4.04M）の 13.5 倍（11-12 は 9〜18 倍）＝両側が出る' + CUR, None, None),
 30: (None, None, '〈読み直し・生〉merch_s（1783〜1800 の職人の転落の和・種あたり・開幕の職人の州和）：英 1,135K（1,126K）・仏 1,256K（1,424K）・清 10,600K（14,687K）・日 871K（1,059K）・普 340K（369K）・露 477K（499K）・同じ期間の入口 0〜4.4K＝種に依らない。class_s（英・10 種の中央）：職人の 1 人あたり月の所得 4.33（1790）・5.48（1795）・5.66（1800）＝工場労働者の 2.35・3.34・3.86 倍・職人の人数 163K→7.2K→0.7K＝所得が労働者の数倍あっても資産の門（錨の 7 割が州の平均）で落ちる＝工場に負けて落ちるのではない（11-12 の反証 2-2 の根拠の直し・10 種で確かめた）' + CUR, '10-18 精査 人14（一括の再較正 9-3）と (455)・S04', None),
 31: (None, None, '〈読み直し・生〉職人の転落の和 種あたり 58.0M に対し入口 0.49K（S12-H004）＝狙いの〈格差が没落を製造する因果を断つ〉は職人で届かない。錨は 0.7×州の平均＋0.3×自分の年所得のまま（ソース）' + CUR, '10-18 精査 人14（一括の再較正 9-3）',
     MULT + 'FallAnchorReform は〈物差しの混ぜ率と閾値〉の問い。11-12 の推奨：S12-H014g（職人の ⑥）の扱いは人14 と S04 の後に読み直す（11-12 の反証 F5・§3-2）'),
 32: (None, None, SRC, None, None),
 36: (None, None, '〈読み直し・生〉ed_s（E＝全ての国×四半期）：高等教育の補助＞0 0/147,395・高等教育の買いたい分＞0 0（M）。器は (590)(597) の奨学金の法（既定 ScholarshipTopStage 0）' + CUR, None, None),
}
rows = list(csv.reader(open(os.path.join(W, 'in_S12_1112.tsv'), encoding='utf-8'), delimiter='\t'))
hdr, rows = rows[0], rows[1:]
cmp = {r[0]: r for r in list(csv.reader(open(os.path.join(W, 'out_cmp_S12.tsv'), encoding='utf-8'), delimiter='\t'))[1:]}
out = open(os.path.join(W, 'ledger_S12_2026-11-17.tsv'), 'w', encoding='utf-8', newline='\n')
side = open(os.path.join(W, 'side_S12_2026-11-17.tsv'), 'w', encoding='utf-8', newline='\n')
out.write('\t'.join(hdr) + '\n')
side.write('行\t11-12 の行\t区分\tコードの差（注と空白を除く・錨を囲む関数）\t11-12 の判定\t今の判定\t確証\n')
order = []
for h, ks in MAP.items():
    for k in ks: order.append(k)
byk = {int(r[0].split('-')[1]): r for r in rows}
for k in order:
    r = list(byk[k]); old = r[0]; c = cmp.get(old)
    nid = IDS[k]; r[0] = nid
    if c is not None and c[2].strip(): r[4] = c[2]
    # 表の区切りの字（|）を含む錨は file:line:名 の形へ（名は行にある係数の名・ledger_check の門 1 が引ける形）
    r[4] = ';'.join(a if '|' not in a else ':'.join(a.split(':')[:2]) + ':InsolventThreshold' for a in r[4].split(';'))
    oldj = r[8]
    if k in RE:
        ty, j, basis, rest, col12 = RE[k]
        if ty: r[6] = ty
        if j: r[8] = j
        r[7] = NEW
        r[9] = basis if basis.startswith('〈読み直し・生〉') or basis.startswith('〈読み直し・ソース〉UrbanHeadroom') or k == 12 else basis + '〔11-12 の根拠〕' + r[9]
        if rest: r[10] = rest
        if col12 is not None: r[11] = col12
        kind = '読み直し'; conf = 'ソース（今の木 ef7db3cc8）' + ('と生（keep_1184）' if '生' in basis[:9] or k == 12 else '')
    else:
        r[7] = OLD; r[9] = '〈写し・11-12 の読み（木 9b18137a・生 keep_1053）〉' + r[9]
        kind = '写し'; conf = '11-12 の読み（keep_1053）＝今の生では数え直していない・錨を囲む関数は今の木まで字が同じ（cmp_rows.py）'
    if r[8] not in ('働かない', '一部だけ働く') and not r[11].startswith('印') and not any(x in r[2] for x in ('休眠',)) and r[11] not in ('—',) and k not in (15, 20, 27, 36):
        r[11] = '—'
    diff = ('変わった：' + c[3]) if c is not None and c[1] == '変わった' else '変わらない'
    side.write('\t'.join([nid, old, kind, diff, oldj, r[8], conf]) + '\n')
    out.write('\t'.join(x.replace('\t', ' ').replace('|', '／') for x in r) + '\n')
print('行', len(order))
