# 読むだけ (1196) c119ra：11-12 の S09 の 48 行を、今の木 93fc4a967 の錨・写しと読み直しの別で 12 列の TSV に書く（(1189) mk_tsv_s12.py の形を写した）。
#  入力：ledger_S09_2026-11-12.tsv（11-12・木 7bb40c77）・cmp_S09.tsv（cmp_rows.py の出力）。出力：ledger_S09_2026-11-18.tsv（12 列）・side_S09_2026-11-18.tsv（写しの別）。
#  12 列目の〈作者「全て推奨で」で決定〉の書き方は〈消すと決まった〉と読めるので〈AI の推奨（作者「全て推奨で」の下の候補）〉へ直した（(1189) 反証 v の指摘と同じ）。
#  使い方: python -X utf8 -I mk_tsv_s09.py <作業フォルダ q3>
import sys, os, csv, re
W = sys.argv[1]
OLD, NEW = '7bb40c77', '93fc4a967'
CUR = '（生 keep_1191/base_ins 種 1〜3・台本 r1196 の s09_v.py・s09_v_m1191_s1_3.txt）'
RUN = 'PriceOfBread.Runner/Program.cs の同じ行番号で字が同じ（cmp_rows.py の〈取り直せない〉は Program.cs の名が App と Runner に 2 つある取り違え＝(1196) で行ごとに確かめた）'
GONE = '〈読み直し・ソース〉名は木から消えた（墓標の注も注の圧縮〔履歴〕で消えた・93fc4a967 で git grep 0 件）＝錨は無い。文書の見出しは古いまま'
# 読み直した行：(錨の上書き or None, 判定 or None, 根拠, 12 列目の足し or None)
RE = {
 1: (None, None, '〈読み直し・生〉T（tradeskip_s の財×国×年・good≠ALL）種 1〜3：楔で飛んだ越境の売り手 6,690,526・6,771,138・6,675,417 のうち関税を抜けば通る（xWedgeAT・InstrumentsTradeskip.cs:202）2,111,307（31.6%）・2,025,064（29.9%）・1,943,878（29.1%）＝上界（反証 §8-5）。TN の関税の中央 0.239・0.240・0.224（1,131・1,125・1,119 国×年）' + CUR, None),
 4: (None, '働かない', '〈読み直し・生〉F（sf_s の国×四半期）種 1〜3：負の関税で安く売る財＞0 0／14,823・0／14,773・0／14,724。G（fiscal_s の国×月）関税の台帳＜0 0／18,027（3 種とも）。X（scalars_s）関税の目標＜0 420（2.8%）・492（3.3%）・653（4.4%）＝目標は出るが実際の関税は 0 を割らない（11-12 と同じ形）。囲む関数 MonthSettle の差は駐屯の調達と巨大建造物の削除で、負の関税の枝の字は同じ' + CUR, None),
 5: (None, '一部だけ働く（関税≧0.5 は国×年の 2.0〜2.7%）', '〈読み直し・生〉TN 種 1〜3：関税≧0.5 31／1,131・27／1,125・22／1,119（2.7%・2.4%・2.0%）・≧0.8 は各 1・負 0（反証 §8-5 の型）' + CUR, None),
 8: ('InternationalMarket.cs:107:TreatyPortTariffScale', None, '〈読み直し・ソース＋生〉11-12 の錨 Politics.cs:1743 は ForeignPortShare の頭の注の行（注の圧縮で消えた）＝錨は InternationalMarket.cs:107 の 1 本。TN 種 1〜3：treatyScale＜0.99 70・40・70・＜0.9 49・22・42・＜0.5 21・18・21（11-12 は＜0.5 が 0）＝目減りの深い国×年が出るようになった〈推：11-12 の後の条約港の変更〉' + CUR, None),
 13: (None, None, '〈読み直し・生〉C（secession_s の州×四半期・計器の国）種 1〜3：tariffInterestIndex≠0 119,276／119,276・121,025／121,025・113,582／113,582（首都の州は表に無い）。SecessionConsciousnessWeight 1→0（(1186) nc）で consciousnessFactorShare は全ての行で 1' + CUR, None),
 14: (None, None, '〈読み直し・生〉C 種 1〜3：関税の項 tariffTermIndex≠0 は全ての行・p50 0.055・0.055・0.060・p90 0.316・0.307・0.315・最大 2.47・2.28・2.44。国民意識の因子が 1 に固定された後も関税の項は分離の圧力の乗算に残る（Politics.cs:885）' + CUR, None),
 16: (None, '働く', '〈読み直し・生〉L（monlaw_s・全ての国×四半期）種 1〜3：保有の禁令（OpiumPossession≠None＝BilateralMarket.cs:323 の門）2,928（7.1%）・4,274（10.4%）・3,871（9.5%）。O（opium_s の国の行）：禁令の四半期のうち輸入＞0 2,296（78%）・3,238（76%）・2,828（73%）＝漏れで続く。StepBilateral の差は距離の入口（DropKm）で錨の字は同じ' + CUR, None),
 17: (None, '働く', '〈読み直し・生〉L 種 1〜3：輸出禁止 1,009（2.4%）・2,588（6.3%）・1,238（3.0%）。O：禁止の四半期のうち輸出＞0 183（18%）・273（11%）・336（27%）' + CUR, None),
 19: (None, None, '〈読み直し・ソース〉錨の字は同じ。口の値の列は今も生に無い。反証 §8-2：直す #2・#3 は回 4-2 の〈組の門〉の 1 本の線に畳む（別々に直さない）', '（反証 §8-2：組の門の線へ畳む）'),
 21: ('BilateralMarket.cs:775:NonTradeable', '働く', '〈読み直し・ソース〉旧の錨 BilateralMarket.cs:732（TradeColonialGoods の墓標の注）は注の圧縮で消えた＝錨を交易できない財の門（:775）へ。植民地財を除く門は今も無い', None),
 22: (None, None, '〈読み直し・生〉X 種 1〜3：交戦中の国×四半期 2,214（14.9%）・1,893（12.8%）・2,254（15.3%）。成約ごとの危険料の列は今も無い。反証 §8-1：消す候補 I は作者 2026-09-26 の回 4-2 と 11-11 の問い5 を引いて Q1′ に入れる' + CUR, None),
 23: (None, None, '〈読み直し・ソース〉錨の字は同じ。危険料の列は今も無い。反証 §2：回 4-2 手順 2 の直し（売り手の国の敵対度も見る）は〈組の門〉の線と同じ回', None),
 24: ('World.cs:1105:LastBlockadeRisk', '働かない', '〈読み直し・ソース＋生〉書き手は今も 0（SimCore の代入なし・試験 WarThemeTests.cs:1131 だけ）。L 種 1〜3：blockadeRiskShare≠0 0／41,248・0／41,059・0／40,853。読み手は 4 本（拿捕 Diplomacy.cs:725・厭戦 :2242・保険金 MultiNationSim.cs:1250・計器 InstrumentsMon.cs:228）＝貿易タブの読み手（旧 GameViewModel.Trade.cs:99）は画面の作り直しで消えた' + CUR, None),
 25: (None, '働かない', '〈読み直し・ソース〉StepWars の差は他の項（11-12 の後の変更）で、封鎖の項（Diplomacy.cs:2242）と配給の緩和（:2244）の字は同じ＝入力 LastBlockadeRisk が 0（S09-24）＝項は常に 0', None),
 28: (None, None, '〈読み直し・生〉X 種 1〜3：交戦中の国×四半期 14.9%・12.8%・15.3%。禁輸の数の列は今も無い。錨の字は同じ（StepBilateral の差は距離の入口）' + CUR, None),
 30: (None, None, '〈読み直し・ソース〉錨の字は同じ・比重の列は今も無い。反証 §8-6：比重は 11-11 の A', None),
 33: (None, '働く', '〈読み直し・ソース〉売れる量は今も国内の前期の需要 LastDemand（EconomyStep.cs:852）＝輸出を含まない。Run の差は畑と鉱山の上限の削除（FarmScaleCap・MineralScaleCap）で、この行の字は同じ', None),
 34: (None, None, '〈読み直し・生〉F 種 1〜3：高く買う財＞0 14,819／14,823・14,771／14,773・14,712／14,724・平均 71.8・71.5・71.0 財（11-12 のとおり）。反証 §3：非対称は在るが小さい・直す #1(a) は wedgeHome と対で直す形へ' + CUR, None),
 36: ('GameViewModel.Trade.cs:46:RefreshTrade', '未確かめ（危険の行は画面の作り直しで消えた）', '〈読み直し・ソース〉旧の錨 GameViewModel.Trade.cs:99（危険の行）は画面の作り直しで消えた＝錨を RefreshTrade（:46）へ。他の行は撮影を読んでいない', None),
 38: (None, None, '〈読み直し・ソース〉' + RUN, None),
 39: (None, None, '〈読み直し・ソース〉' + RUN, None),
 40: (None, None, '〈読み直し・ソース〉' + RUN, None),
 41: (None, None, '〈読み直し・ソース〉' + RUN, None),
 42: (None, None, '〈読み直し・ソース〉' + RUN, None),
 43: ('—', '文書が古い', GONE, None),
 44: ('—', '文書が古い', GONE, None),
 45: (None, None, '〈読み直し・ソース〉燃料の式の字は同じ（Transport.Step の差は植民地の石炭の時代の項 EraColonyCoal の削除）。燃料の量の列は今も無い', None),
 47: ('—', '文書が古い', GONE, None),
 48: ('—', '文書が古い', GONE, None),
}
# 消す行の読み手の数（93fc4a967 の git grep の行・宣言を含む・E＝SimCore〔係数を除く〕・K＝係数・T＝試験・A＝App・R＝Runner）
READERS = {
 5: '〈読み手 5〉（IsolationExportWeight E3・K2・T0／IsolationExportFactor E2）',
 22: '〈読み手 5〉（BlockadeStapleBias E2・K1・T2）',
 29: '〈読み手 7〉（HegemonContainmentTrade E2・K1／HegemonContainmentMax E2・K1・T0）',
 34: '〈読み手 21〉（buyDear E4・T1・R1／TryTradeBias E2／TradeBiasStrength E10・K1・T2）',
 41: '〈読み手 2〉（tradewhy T1・R1）',
 26: '〈読み手 2〉（StepMarineInsurance E2）',
 35: '〈読み手 8〉（TradeCheapSellSubsidy E2・K1・T5）',
}
DEC = re.compile(r'〈作者「全て推奨で」で決定（2026-11-12）〉')
rows = list(csv.reader(open(os.path.join(W, 'ledger_S09_2026-11-12.tsv'), encoding='utf-8'), delimiter='\t'))
hdr, rows = rows[0], rows[1:]
cmp = {r[0]: r for r in list(csv.reader(open(os.path.join(W, 'cmp_S09.tsv'), encoding='utf-8'), delimiter='\t'))[1:]}
out = open(os.path.join(W, 'ledger_S09_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n')
side = open(os.path.join(W, 'side_S09_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n')
out.write('\t'.join(hdr) + '\n')
side.write('行\t区分\tコードの差（注と空白を除く・錨を囲む関数）\t11-12 の判定\t今の判定\t確証\n')
for r in rows:
    if len(r) < 12: continue
    k = int(r[0].split('-')[1]); c = cmp.get(r[0])
    oldj = r[8]
    if c is not None and c[2].strip():
        # 要素ごと：取り直せた錨は新しい行・〈?〉の錨（Runner の Program.cs）は 11-12 の行のまま（字が同じ）
        oa = r[4].split(';'); na = c[2].split(';')
        if len(oa) == len(na): r[4] = ';'.join(o if ':?:' in n else n for o, n in zip(oa, na))
        elif '?' not in c[2]: r[4] = c[2]
    r[11] = DEC.sub('〈AI の推奨（作者「全て推奨で」の下の候補・2026-11-12）〉', r[11])
    if k in READERS and r[11].startswith('消す') and not re.search(r'読み手[^0-9０-９]{0,12}[0-9]', r[11]): r[11] = r[11] + READERS[k]
    if k in RE:
        anc, j, basis, add12 = RE[k]
        if anc: r[4] = anc
        if j: r[8] = j
        r[7] = NEW
        r[9] = basis + '〔11-12 の根拠〕' + r[9]
        if add12: r[11] = r[11] + add12
        kind = '読み直し'; conf = 'ソース（今の木 93fc4a967）' + ('と生（keep_1191 種 1〜3）' if '生' in basis[:10] else '')
    else:
        r[7] = OLD; r[9] = '〈写し・11-12 の読み（木 7bb40c77）〉' + r[9]
        kind = '写し'; conf = '11-12 の読み＝今の生では数え直していない・錨を囲む関数は今の木まで字が同じ（cmp_rows.py）'
    diff = ('変わった：' + c[3]) if c is not None and c[1] == '変わった' else '変わらない'
    side.write('\t'.join([r[0], kind, diff, oldj, r[8], conf]) + '\n')
    out.write('\t'.join(x.replace('\t', ' ').replace('|', '／') for x in r) + '\n')
print('行', len(rows))
