# 読むだけ (1245) c126s28：11-12 の S28 の 33 行（木 7bb40c77）を、今の木 3cb2210a0 の錨・写しと読み直しの別で 12 列の TSV に書く（(1224) mk_tsv_s26.py の形を写した）。
#  入力：q/ledger_S28_2026-11-12.tsv・q/s28_cmp.tsv（cmp_rows.py）・q/s28_reread_m1233.txt・q/s28_weather_q3_m1233.txt
#  出力：q/ledger_S28_2026-11-19.tsv（12 列）・q/side_S28_2026-11-19.tsv（写した判定と読み直した判定の列を分ける）
#  文書の行（L）は今の木の docs/ゲーム概要と実装機能.md の行（7bb40c77 から S28 の節の上に 1 行増えた＝+1・節の中身は字が同じ）。
#  使い方: python -X utf8 -I mk_tsv_s28.py <作業フォルダ>
import sys, os, csv, re, collections
W = sys.argv[1]; NEW = '3cb2210a0'
RAW = '（生 keep_1233/base_ins＝m1233・種 1〜10・q/s28_reread_m1233.txt）'
RAWQ3 = '（生 keep_1233/base_ins＝m1233・種 1〜10・年の最後の四半期だけ＝11-12 と同じ取り方・q/s28_weather_q3_m1233.txt）'
def rd(p): return list(csv.reader(open(os.path.join(W, p), encoding='utf-8'), delimiter='\t'))
old = rd('q/ledger_S28_2026-11-12.tsv'); hdr, rows = old[0], old[1:]
cmp = {r[0]: r for r in rd('q/s28_cmp.tsv')[1:]}
FIX = {'S28-05': 'Simulation.cs:121:SetCapital(World.Regions[0])',
       'S28-16': 'Climate.cs:20:StapleSelfSufficient;Simulation.cs:768:StapleSelfSufficient',
       'S28-20': 'Simulation.cs:669:ApplyEndowment;Simulation.cs:732:ProvinceResources',
       'S28-22': 'Hz8IdentityTests.cs:22:RentScarcityElasticity',
       'S28-23': 'Megaprojects.cs:23:CanStart;Simulation.cs:2164:arableSum',
       'S28-28': 'Coefficients.cs:34:ScaleMax;EconomyStep.cs:2439:ScaleMax;EconomyStep.cs:2442:RgoScaleSpeedFactor;Coefficients.p10_ai_silver_overrides.cs:47:RgoScaleSpeedFactor;DormantIdentityTests.cs:272:MineralScaleCap',
       'S28-31': 'Coefficients.p06_rank_discovery.cs.md:989:PollutionCapacityWeight'}
KIND = {'S28-01': '頻度', 'S28-11': '頻度', 'S28-12': '頻度', 'S28-14': '頻度', 'S28-15': '頻度', 'S28-21': '頻度', 'S28-24': '頻度', 'S28-25': '頻度', 'S28-28': '頻度', 'S28-32': '頻度'}
# 読み直した行：(判定, 根拠の頭に足す文, 12 列目〔None＝そのまま〕, 残りタスク〔None＝そのまま〕)
D = {
 'S28-01': ('働く', f'読み直し（頻度）：国×年の天候（州の平均）の標準偏差 0.146〜0.152・0.8 未満 9.2〜14.1%（凶作の年表に載る年）・計器の 9 国×年 1,503／種 {RAWQ3}。国×四半期で州の間の幅 0 の割合 98.4〜99.4%＝国の全ての州が同じ値（D3 のまま）{RAW}。', None, None),
 'S28-02': ('一部だけ働く（国×年の歪度 −0.154〜+0.013＝弱い）', f'読み直し：国×年の歪度 −0.076・−0.051・−0.154・−0.037・−0.050・−0.019・+0.013・−0.003・−0.012・−0.098（種 1〜10）{RAWQ3}。', '消す（K102・挙動の線）：読み手 エンジン 4〔Simulation.cs:40・:43・:44・:1681〕・係数 1〔p07:140〕・試験 6・App 0・Runner 0。失う物＝局地の 1 個の外れの非対称（積んだ後の歪度は種の揺れの内）', None),
 'S28-03': ('働く', f'読み直し：前年との相関 0.302〜0.386（種 1〜10・局地 0.35 と帯 0.35 の積）{RAWQ3}。', None, None),
 'S28-04': ('働く', f'読み直し：同じ帯の国の対の相関 0.177〜0.306（16 対）・違う帯 −0.061〜+0.031（20 対）（国×年＝州×四半期の平均・帯＝world.json の 1 番目の州の緯度÷15 の切り捨て）{RAW}。州の緯度は読まない（S28-01 の幅 0 が 98.4〜99.4%）。', None, None),
 'S28-05': ('文書が古い', '読み直し（ソース）：旗 CenterIsFirstProvince は (1089) で焼かれ、Simulation.cs:121 が SetCapital(World.Regions[0]) を無条件に呼ぶ（エンジンの読み手 0・DormantIdentityTests.cs:273 の RetiredFields に名）。文書 L6532 の変数の行は旗の名と値 1.0 を残す。', '直す（文書：変数の行から CenterIsFirstProvince を消す）＝11-12 の K1 は (1089) で済んだ', '—'),
 'S28-08': ('一部だけ働く（天候は届く・麦の病気は自給のパンに届かない）', '読み直し（ソース）：StapleHarvestFactor（EconomyStep.cs:147〜151）は今も主食の財そのものの CropHealth[gi] を読む＝パンは病気の作物でない（Catalog.cs:218 IsDiseaseProneCrop）＝D2 は未了。錨の近くの差は計器の 1 行（SatNeeds）だけ。', None, None),
 'S28-11': ('文書が古い', f'読み直し（頻度）：内部の発生 741〜822 件／種・国あたり（発生＋伝播）中央 26〜29 件・最大 36〜51 件（61〜66 国・1783〜1950）{RAW}＝どの国でも 6 年に 1 回ほど起きる。', None, None),
 'S28-12': ('文書が古い', f'読み直し（頻度）：1 件の死者÷襲われた州の人口（発生＋伝播）中央 2.15〜2.49%・90% 点 3.57〜3.90%・最大 5.63〜7.42%・5% を超えた件 0.38〜1.51%（種 1〜10）{RAW}＝致死率の帯は狭く、爆発的な流行（大きな尾）は出ない。作者 2026-10-20「「高確率で頻繁に発生するが、ほとんどは広がらずに収束する」「複数の条件がかみ合った際に、爆発的な流行となる」方が正しそう」の形とは違う（頻度の差は死の側の段3 の対象）。', None, '死の側の段3 の疫病（616 行 ⑥・残りタスクの対象）'),
 'S28-14': ('文書が古い（機構は働く＝S10-H028a）', f'読み直し（頻度）：国内の二次の波 1,030〜1,108 件／種＝発生＋伝播の 0.61〜0.65 倍（州が 2 つ以上ある国だけで起きる）{RAW}。', None, None),
 'S28-15': ('一部だけ働く（3 つの経路は働く・時代の伸びは常に 1 倍）', f'読み直し（頻度）：国際の伝播 833〜961 件／種＝内部の発生の 1.10〜1.28 倍＝疫病の過半は飛び火で起きる {RAW}。時代の伸びは EraFactor が常に 1.0（Coefficients.p10_ai_silver_overrides.cs:59）＝MultiNationSim.cs:1052 の EraEpidemicRange は何も変えない。', '消す（K55・S11-H014i の EraFactor の削除に畳む＝新しい番号にしない・恒等）：読み手 エンジン 1〔MultiNationSim.cs:1052〕・係数 1・試験 0', None),
 'S28-16': ('働く', '読み直し（ソース）：BuildProvince の差は賦存の割り算を 2 つの引数へ分けた行（ApplyEndowment の capMul と costDiv・Simulation.cs:752・:758）と ScarceMineralGate の焼き込みだけ＝主食の気候の門（Simulation.cs:768・Climate.cs:20）は同じ字。', None, None),
 'S28-17': ('働く', '読み直し（ソース）：米の門（Simulation.cs:741・Climate.cs:11）は同じ字。BuildProvince の差は S28-16 と同じ。', None, None),
 'S28-18': ('働く', '読み直し（ソース）：綿花の門（Simulation.cs:743・Climate.cs:17）は同じ字。BuildProvince の差は S28-16 と同じ。', None, None),
 'S28-19': ('働く', '読み直し（ソース）：気候ゲート財の削り（Simulation.cs:736・Catalog.cs:220）は同じ字。BuildProvince の差は S28-16 と同じ。', None, None),
 'S28-20': ('未確かめ（設計③で書き直す）', '読み直し（ソース）：ApplyEndowment（Simulation.cs:669）は産出の乗数 capMul と費用の割り算 costDiv を別の引数で受ける形へ変わった＝希少鉱物は能力を希少の賦存で掛け、費用はふつうの賦存で割る（:758）。賦存の表そのもの（ProvinceResources・Simulation.cs:732）は同じ。', None, None),
 'S28-21': ('一部だけ働く（土地係数＜1 の州×四半期 4.2〜8.1%・1925〜 8.7〜19.3%＝混んだ州だけで効く設計どおり）', f'読み直し（頻度）：全ての国の活きた州×四半期で landFactor＜1 の割合 4.22〜8.07%（＜0.9 2.14〜5.08%）・1783〜1849 1.65〜2.29%・1850〜1924 4.79〜9.21%・1925〜1950 8.66〜19.28%（region_s・種 1〜10）{RAW}。', None, None),
 'S28-23': ('働かない（消した＝(1081)）', '読み直し（ソース）：megaprojects.json は木に無い・耕地を足す ApplyEffect は無い・Megaprojects.cs:23 の CanStart は常に偽（画面と Runner のための空の口＝K50）。扶養力の耕地の和（Simulation.cs:2164）は州の ArableLand だけを読む。文書 L6645〜6646 は〈稼働中・11 件〉のまま。', '直す（文書の見出しを〈消した〉へ）・口は K50（⑲ の束）', '—'),
 'S28-24': ('一部だけ働く（品位が床の産出 1850 年 91〜98%＝在庫の差が州を分けない）', f'読み直し（頻度）：鉱物の RGO の産出のうち品位が床（0.15）以下の割合 1800 年 0.1〜1.0%・1825 年 13.5〜45.7%・1850 年 91.0〜98.3%・1900 年 96.9〜99.3%・1949 年 92.5〜99.0%（遍在の 3 財〔粘土・珪砂・石灰石〕も同じ形）・産出加重の品位 1850 年 0.158〜0.199（minecap_s・全ての国・年末・種 1〜10）{RAW}＝19 世紀の半ばから全ての鉱山に一様な ×0.15 が掛かるだけ（11-12 の推し・★(1184) ① の m1178 と同じ形）。', '直す（鉱床の線・★(1184) ① の推奨＝案 S〔在庫は開いた産地の生産者だけ〕・第 3 の形の静的な推しと門を設計の材料の線で先に）＝残りタスクの対象', '鉱床（616 行 ⑨・残りタスクの対象）・★(1184) ①'),
 'S28-25': ('一部だけ働く（年代を読まない）', f'読み直し（頻度）：開いた産地の年（種 1〜10）：ニルギリ・ジャワ・セイロンのキナ 1783〜1789（10/10）・九州とルイジアナの硫黄 1787〜1788（10/10）・ヴィクトリアの金 1793〜1797（10/10・史実 1851）・カリフォルニアの金 1852〜1900（10/10・開いた時の持ち主は日本 5・清 3・アメリカ 2）・ラントの金 1862〜1919（9/10・持ち主はマラータ同盟 3・ソコト 2・アメリカ 2・日本 1・南アフリカ共和国 1）・クロンダイクの金 1825〜1909（6/10）{RAW}。州の持ち主が遠い国の種が多い＝S27（領土の移転）の側の記録。', None, None),
 'S28-26': ('休眠', '写し（ソース）：SilverRushEnabled＝0.0（Coefficients.p04_trade.cs:109）・読み手 Simulation.cs:692 だけ・DormantIdentityTests.DormantZeroFields に名（:95）。', None, None),
 'S28-27': ('休眠', '写し（ソース）：MineralCostFollowsGrade＝0.0（Coefficients.p04_trade.cs:110）・読み手 EconomyStep.cs:2786 だけ・DormantZeroFields に名（:96）。', None, None),
 'S28-28': ('文書が古い（上限は工場と同じ ScaleMax 3.0・速さだけ 1/4）', '読み直し（ソース＋頻度）：MineralScaleCap は (1132) で消え FarmScaleCap は (1089) で焼かれた＝RGO の上限は工場と同じ ScaleMax 3.0（EconomyStep.cs:2439）・速さだけ ×0.25（:2442）。prodcap_s（種 1〜2）の RGO の行で scale≥3.0 は賦存の無い希少鉱物の行（class Z）9.2〜12.5%（>1.5 は 82〜84%）・他の区分 0%（生 keep_1233/base_ins・種 1〜2・q/s28_reread_m1233.txt の F）。文書 L6671〜6672 は〈+50% まで〉と消えた係数の名を残す。', '直す（文書）・残る天井 ScaleMax は S05 の側（設計思想⑧）', None),
 'S28-30': ('文書が古い', '読み直し（ソース）：乱数の系列は 8 本・引く行は 13（q/s28_rng.tsv）。主系列（Simulation.Rng）を引くのは豊凶の 1 行（Simulation.cs:1680）と単一国の世界の外交（:2058 が渡す）だけ＝文書 L6695 の〈鉱脈の発見（と到達しない確率の枯渇）は主系列〉は古い（:1702 の注＝枯渇 2・発見 2 の主系列の乱数は消えた）。', None, None),
 'S28-32': ('一部だけ働く（不作の深さは国の局地の 1 個だけ）', '読み直し（成績表）：m1233 の 10 種の成績表（measurements/b10_20261119_m1233 の 8 行目）〈恐慌の原因: 信用過熱先行 81%　不作先行 33%〉＝両側が出る。不作の深さは 1−w.HarvestFactor（SelfPlay.cs:1422）＝帯の天候と作物の病気を読まない（D7 のまま）。', None, None),
}
COPYSRC = {'S28-09': 'S10-H019（(1176)・b9570c8a7・働く）', 'S28-13': 'S10-H027', 'S28-22': '—'}
out = []; side = [['行', '区分', '写した判定（11-12・7bb40c77）', '読み直した判定（3cb2210a0）', '錨の比べ（cmp_rows）']]
for r in rows:
    k = r[0]; c = cmp.get(k)
    na = FIX.get(k) or (c[2] if c and c[2] and '?' not in c[2] else r[4] if r[4] else '—')
    nr = list(r); nr[4] = na if na else '—'
    nr[3] = re.sub(r'L(6\d\d\d)', lambda m: 'L' + str(int(m.group(1)) + 1), nr[3])
    if k in KIND: nr[6] = KIND[k]
    if k in D:
        j, why, act, rem = D[k]; nr[7] = NEW; nr[8] = j; nr[9] = why + ' ｜11-12：' + r[9]
        if act is not None: nr[11] = act
        if rem is not None: nr[10] = rem
        side.append([k, '読み直し', r[8], j, c[1] if c else '—'])
    else:
        nr[9] = r[9] + f' ｜錨は 3cb2210a0 で取り直した（cmp_rows：{c[1] if c else "—"}）。'
        side.append([k, '写し', r[8], '—', c[1] if c else '—'])
    out.append(nr)
# 12 列目（処置）を全ての行に書く（(1245) の問い 3）。None＝上で決めたまま。
ACT = {'S28-01': '残す（働く・州の座標で引く直しは S28-04 の D3）', 'S28-03': '残す（働く）',
       'S28-04': '直す（D3・挙動：帯を州の座標で引く・州と開幕の植民地 308 のうち 103 が首都と違う帯〈推・world.json と places.json の静的な数〉）',
       'S28-06': '残す（未確かめ・計器 I1 が先）', 'S28-07': '残す（史実で重要＝ジャガイモ胴枯病・さび病・フィロキセラ・未確かめ・計器 I1 が先）',
       'S28-08': '直す（D2・挙動：StapleHarvestFactor〔EconomyStep.cs:147〕で主食の財からレシピの投入をたどり病気の作物の CropHealth を読む）',
       'S28-09': '残す（S10-H019 の側で働く）', 'S28-11': '直す（文書）・発生の形は死の側の段3＝残りタスクの対象',
       'S28-12': '直す（文書）・爆発的な流行の尾が無いのは死の側の段3＝残りタスクの対象', 'S28-13': '残す（働く）',
       'S28-14': '直す（文書）・機構は残す（二次の波は発生＋伝播の 0.61〜0.65 倍）', 'S28-16': '残す（働く）', 'S28-17': '残す（働く）',
       'S28-18': '残す（働く）', 'S28-19': '残す（働く）', 'S28-25': '直す（S01-21 の側：開く年が史実より 50〜60 年早い）',
       'S28-27': '残す（DormantZeroFields:96 の休眠の器・設計③ §6）', 'S28-29': '残す（働く）',
       'S28-30': '直す（文書：主系列の行）・乱数の系列 8 本と引く行 13 は q/s28_rng.tsv', 'S28-32': '残す（成績表の 1 行＝恒等の判定が読む）・直すなら D7（帯と作物も数える）',
       'S28-33': '残す（App は計測の外・D6 は App の束）'}
REM = {'S28-28': 'S05（残る天井 ScaleMax 3.0・設計思想⑧）'}
for x in out:
    if x[0] in ACT: x[11] = ACT[x[0]]
    if x[0] in REM: x[10] = REM[x[0]]
for x in out:
    for i, v in enumerate(x): assert '|' not in v and '\t' not in v and '\n' not in v, (x[0], i)
with open(os.path.join(W, 'q/ledger_S28_2026-11-19.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('\t'.join(hdr) + '\n'); [f.write('\t'.join(x) + '\n') for x in out]
with open(os.path.join(W, 'q/side_S28_2026-11-19.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    [f.write('\t'.join(x) + '\n') for x in side]
print('判定', dict(collections.Counter(x[8].split('（')[0] for x in out))); print('区分', dict(collections.Counter(x[1] for x in side[1:])))
