# 読むだけ (1169)：11-12 の S01・S02 の行を、台帳の id（Sxx-Hnnn＋枝の字）・今の木 c3c4de36a の錨・写しと読み直しの別で 12 列の TSV に書く。
#  入力：11-12 の tsv（scratchpad）・cmp_rows の出力（out_cmp_<系統>.tsv）。出力：ledger_<系統>_2026-11-16.tsv（12 列）と side_<系統>.tsv（写しの別）。
# 使い方: python -X utf8 -I mk_tsv.py <scratchpad> <作業フォルダ> <repo>
import sys, os, subprocess, collections
SP, W, REPO = sys.argv[1:4]
OLD, NEW = '9b18137a', 'c3c4de36a'
MAP = {
 'S02': {'H001': [1, 2, 3, 4, 5], 'H002': [6, 7, 8], 'H003': [9], 'H004': [10, 11], 'H005': [12], 'H006': [13, 14], 'H007': [15, 16],
         'H008': [17, 18], 'H009': [19, 20], 'H010': [21], 'H011': [22, 23]},
 'S01': {'H001': [39], 'H002': [40], 'H003': [41], 'H004': [1, 2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 42], 'H005': [9, 10, 11, 15, 16],
         'H006': [17, 18, 19, 20], 'H007': [21, 22, 23, 24], 'H008': [25], 'H009': [26], 'H010': [27], 'H011': [28], 'H012': [29, 30],
         'H013': [31], 'H014': [32], 'H015': [33], 'H016': [34], 'H017': [35], 'H018': [36], 'H019': [37], 'H020': [38]},
}
def newid(sysn, k):
    for h, ks in MAP[sysn].items():
        if k in ks: return f'{sysn}-{h}' + ('' if len(ks) == 1 else 'abcdefghijklm'[ks.index(k)])
    raise KeyError(k)
# 錨の手直し（cmp_rows で取り直せなかった物・注の中の名だった物）
ANCH = {
 'S01-23': 'Discovery.cs:927:OpenSite;Discovery.cs:935:Simulation.ApplyEndowment(p, c.GoldRushEndowment, siteCostDiv, c)',
 'S01-31': 'Simulation.cs:1060:RecipeName = "黄鉄鉱法";Coefficients.p04_trade.cs:108:SulfurFromPyriteProductivity',
 'S01-32': 'Simulation.cs:1202:RecipeName = "堆肥";Coefficients.p08_audit_soldiers.cs:121:CompostProductivity',
 'S01-35': 'Simulation.cs:1159:RecipeName = "蒸気タービン";Coefficients.p04_trade.cs:116:SteamTurbineProductivity',
 'S01-36': 'Simulation.cs:920:d.Id == Catalog.CrudeOilId;Simulation.cs:921:coef.OilMachineryInput',
 'S01-15': 'Coefficients.p06_rank_discovery.cs:103:DiscoveryTechPerDecade;Discovery.cs:15:c.DiscoveryTechPerDecade',
 'S02-09': 'MarketFlow.cs:70:double supply = System.Math.Max(flowIn, System.Math.Min(flowIn + m.Stock, totalDemand))',
 'S02-19': 'Coefficients.p06_rank_discovery.cs:119:UnionBargaining',
}
# 読み直した行（最後に確かめた＝今の木）。値＝(判定, 根拠, 12 列目 or None＝元のまま)
CUR = '（今の生 M＝keep_1164/base_ins・N1／N2＝keep_1164/n1・n2・台本 r1169）'
RE = {
 'S02-09': ('働く', '〈読み直し・ソース〉供給＝max(流入, min(流入＋在庫, 需要))（MarketFlow.cs:70・今の木で字が同じ）。pxgoods_s の輸入と輸出の列が 0 になる理由はソースの順で確かめられる（11-12 反証の訂正を写した）', None),
 'S02-10': ('一部だけ働く（国×月の 76.4% 以上で満点に張り付く）',
            '〈読み直し〉満点＝緊張≧RivalTensionThreshold 40＋StockpileTensionSpan 60（Model.cs:676）。warpair_s の monthsGt100 を国ごとに組の最大で取った（年末の四半期・labor_s の生きた国）＝下限（warpair は 100 以上か出来事のある組だけを書く）：M 76.4%・N1 77.6%・N2 78.9%・3 か月とも M 76.0%・時代別 M 1783-1849 78.0%／1850-1913 80.9%／1914-1950 66.9%（11-12 の m1053 は 73.0%）。張り付きの根は国の最大の緊張（全ての組の最大・Simulation.cs:1738）。文書の変数 StockpileTensionThreshold は (736) で削除済み＝文書が古い' + CUR,
            '消す（iii）〈AI の推奨・確証なし〉：ほぼ常に満点＝主食と軍需の在庫の目標を 1.5 倍にする定数と同じ。読み手 エンジン 4 か所（Model.cs:672〜680 の欄と関数・MarketFlow.cs:67・Simulation.cs:1819）＋係数 3。間接の読み手＝TargetStock を読む BilateralMarket.cs:815・897〜911・1063〜1068・EconomyStep.cs:830・1634〜1636・SelfPlay.cs:776（腕は貿易の量・買い占め・AI の貿易の傾きを見る）。作者への問いは〈消す／緊張の側を直して信号に戻す／残す〉の 3 択'),
 'S02-11': ('一部だけ働く（一致するのは満点でない国×月の 23.6% 以下）',
            '〈読み直し〉生産の側 EconomyStep.cs:2774（DesiredInventoryCover×LastDemand）は買いだめの倍率を見ず、値段の側 MarketFlow.cs:67 だけが掛ける（今の木で字が同じ）。満点でない国×月 M 23.6%・N1 22.4%・N2 21.1% 以下（S02-10 の補数）。効きの上限は産出の目標の 2%（11-12 反証・ソース）' + CUR,
            '直す：S02-10 の線に畳む（消せば一致・残すなら TargetStock の読み手 5 か所以上を同じ定義へ）〈AI の推奨〉'),
 'S02-13': ('働く', '〈読み直し〉SoftBandLn（MarketFlow.cs:210）は今の木で字が同じ。pxgoods_s は 1783〜1794 の 12 年だけを書く＝全期間の頻度として読まない（11-12 反証）', None),
 'S02-14': ('働く（供給の無い財の値段は帯の不動点に座る）',
            '〈読み直し〉pxgoods_s 種 1〜3（1783〜1794 だけ）の産出 0・在庫 0・需要＞0 の行 M 4,629・N1 4,638・N2 4,583。参照×物価水準に対する比の最多 49.16（M 2,013・N1 2,036・N2 1,943 行）＝財に依らず揃う。財はコーヒー・茶・防水布・米・キニーネ・医薬品。4,629 は上限で確かなのは 49.16 に座る行（11-12 反証）' + CUR, None),
 'S02-15': ('働く', '〈読み直し・ソース〉需要＋LastExport（MarketFlow.cs:166）。輸出の無い財に足さないのは正しい振る舞い（11-12 反証）',
            '直す（恒等）：0／1 の構造の旗の焼き込み・読み手 SimCore 1 行（MarketFlow.cs:166）＋係数 1・試験 TradeClearingTests.cs:62 は確かめの Assert（注ではない＝焼くときに消す・★(743) ④）'),
 'S02-16': ('働かない（国家の調達が価格則に 2 回入る）',
            '〈読み直し〉Procure の国家の枝は LastDemand（InternationalMarket.cs:94）と LastExport（:101）の両方に記帳し、価格則は需要＋輸出（MarketFlow.cs:166）＝今の木で字が同じ。呼び手は Colonial.cs:41（燃料）・:43（キニーネ＝(1164) c114qn で係数 0＝今は買わない）・World.cs:1866〜1874（遠征の帆船・汽船・携行糧食・キニーネ）。量：fiscal_s の植民地の維持の請求＞0 は国×月の M 54.1%（N1 54.1%・N2 56.4%）だが請求の合計に対する割合は M の 90 分位で 0.007%＝量はほぼ 0。11-12 反証の〈28.3%・中央 2.4%〉は m1053（キニーネ 6.0）の数' + CUR,
            '直す（挙動の線・小）〈AI の推奨〉：(1058) c100pq が読んだ・形 Y（LastSold へ記帳）は (1063) c101py で手続きの不採用・量がほぼ 0 になった＝優先は低い'),
 'S02-17': ('働く',
            '〈読み直し〉Toward は比例の式＝下がる速さは上がる速さの 1/4（MarketFlow.cs:96〜98・11-12 反証）。labor_s 10 種の年末：工場労働の失業 5% 超 M 57.9%（N1 61.2%・N2 58.7%）・20% 超 6.7%（8.1%・7.8%）・実質の値段÷参照の中央 0.30（0.31・0.30）・2 倍超 0.00%・農業労働 0.5 倍未満 99.6%（99.5%・99.5%）。記録：中央は m1053 の 0.69 から 0.30 へ・農業 83.3% から 99.6% へ（m1053→m1164 の間の挙動の線の結果〈推〉・帰属は読んでいない）' + CUR, None),
 'S01-07': ('一部だけ働く（費用が立たない 79.4%）',
            '〈読み直し〉disc_s の 1880 年以後の機関車の T 行で cost＜0（RailCostIndex の NoPrice・Discovery.cs:745）：M 79.4%・N1 78.6%・N2 79.4%（11-12 の m1053 は 76%）。立った行の置き換える÷費用の中央 M 20.2・N1 17.8・N2 19.1＝立てば必要性はほぼ満点。(1130) c110lo・(1152) c113lc の機関車の門は需要と払いの側（EconomyStep.cs:1050）＝必要性の式は変えていない。持ち主 Transport.cs の差は段 R（(1097) c105rr）＝RailCostIndex は Transport.cs を読まない〈ソース〉' + CUR, None),
 'S01-23': ('働く',
            '〈読み直し〉OpenSite の差は (1079) c103tr（R1 (a)(b)＝費用を割る数を能力の倍率から分ける）＝門 EndowmentCostFollowsAbundance・Scarcity が 0 で〈判定: 恒等〉。site_s の O 行（M）：九州とルイジアナの硫黄・ニルギリとジャワとセイロンのキナ・ヴィクトリアとカリフォルニアとラントの金が 10/10 種・クロンダイクの金 6/10（11-12 の m1053 は 9 本が 10/10）。富鉱の産出の量は読んでいない' + CUR, None),
}
# 写した行の 12 列目の手直し（線の番号・済みの印・門 9 の読み手の数）
FIX = {
 'S01-27': '消す〈作者 2026-11-12「全て推奨で」で推奨を採った・線は未起票＝実装ログに無い〉：労働力の財の蒸気機関と電動のレシピ・読み手 SimCore 2 行（Simulation.cs:1114・:1116）＋試験 RecipeCostTests.cs ほか・発見の行（Catalog.cs:425・:440）は他の読み手があるので残す〈推〉',
 'S01-17': '直す：年代を読む形へ〈作者 2026-11-12「全て推奨で」で推奨を採った・線は未起票＝実装ログに無い・挙動の線〉',
 'S01-21': '直す：見つける力に年代を戻す（発見の表と同じ時計を読む・新しい係数なし）〈作者 2026-11-12「全て推奨で」で推奨を採った・線は未起票＝実装ログに無い・挙動の線〉',
 'S01-31': '—（旗の 0 側＝消す候補 ④ は (1089) c104fb で焼いた）',
 'S01-32': '直す：過リン酸石灰の必要性（S01-04）の側（旗の 0 側＝消す候補 ④ は (1089) c104fb で焼いた）',
 'S01-35': '直す：電力の鎖の側（旗の 0 側＝消す候補 ④ は (1089) c104fb で焼いた）',
 'S01-36': '—（旗の 0 側＝消す候補 ④ は (1089) c104fb で焼いた）',
 'S01-07': '直す（材料）：費用が立たない理由（線路の値段か機関車の費用か）を計器で割ってから〈AI の推奨〉',
 'S01-24': '消す：休眠の旗（恒等の線）・読み手 SimCore 3 行（Coefficients.p04_trade.cs:109・Simulation.cs:693・InstrumentsRecipeout.cs:19）＋試験 4（DiscoveryScoreTests.cs:735・:757・DormantIdentityTests.cs:97・TradePhase0Tests.cs:90）＝消す候補 ①',
 'S01-33': '消す：休眠（恒等）・読み手 SimCore 2 行（Coefficients.p08_audit_soldiers.cs:123・Simulation.cs:1205）＋試験 1（DormantIdentityTests.cs:82）＝消す候補 ①',
 'S01-15': '消す：Y と技術水準の枝（恒等）＝消す候補 ③・読み手 SimCore 2 行（Coefficients.p06_rank_discovery.cs:103・Discovery.cs:15）＋試験 1（DiscoveryScoreTests.cs:51）',
 'S02-18': None,  # 元のまま（後で関連の線を足す）
}
ADD12 = {'S02-18': '・関連：(1108) c106mw・(1126) c109mw（最低賃金の施行の読み）'}
CMP = {}
for s in ('S01', 'S02'):
    for l in open(os.path.join(W, f'out_cmp_{s}.tsv'), encoding='utf-8').read().split('\n')[1:]:
        c = l.split('\t')
        if len(c) >= 4: CMP[c[0]] = c
names_changed = set(p.split('/')[-1] for p in subprocess.run(['git', '-C', REPO, 'diff', '--name-only', '9b18137a2', 'c3c4de36a'], capture_output=True, text=True).stdout.split('\n') if p)
for s in ('S02', 'S01'):
    L = open(os.path.join(SP, f'ledger_{s}_2026-11-12.tsv'), 'rb').read().decode('utf-8').split('\n')
    hdr = L[0].split('\t'); out = ['\t'.join(hdr)]; side = ['\t'.join(['行', '11-12 の行', '区分', '持ち主のファイルの差（ファイル単位）', 'コードの差（注と空白を除く・錨を囲む関数）', '11-12 の判定', '今の判定', '確証'])]
    for l in L[1:]:
        c = l.split('\t')
        if len(c) < 12: continue
        old = c[0]; k = int(old.split('-')[1]); nid = newid(s, k)
        cm = CMP.get(old)
        anch = ANCH.get(old, cm[2] if cm else c[4])
        owners = c[5].split()
        fch = sorted(set(owners) & names_changed)
        oldj = c[8]
        if old in RE:
            j, basis, fx = RE[old]
            c[7] = NEW; c[8] = j; c[9] = basis
            if fx is not None: c[11] = fx
            kind = '読み直し'; conf = '生とソース（今の木・keep_1164）'
        else:
            c[7] = OLD; c[9] = '〈写し・11-12 の読み（木 9b18137a・生 keep_1053）〉' + c[9]
            kind = '写し'; conf = '11-12 の読み（keep_1053）＝今の生では数え直していない'
        if old in FIX and FIX[old] is not None: c[11] = FIX[old]
        if old in ADD12: c[11] = c[11] + ADD12[old]
        c[0] = nid; c[4] = anch
        for i in range(12): c[i] = c[i].replace('|', '／').replace('\t', ' ').replace('\n', ' ')
        out.append('\t'.join(c[:12]))
        side.append('\t'.join([nid, old, kind, ' '.join(fch) or 'なし', (cm[3] if cm else '') or 'なし', oldj, c[8], conf]))
    open(os.path.join(W, f'ledger_{s}_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
    open(os.path.join(W, f'side_{s}_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write('\n'.join(side) + '\n')
    print(s, len(out) - 1, collections.Counter(x.split('\t')[2] for x in side[1:]))
