// GameViewModel.Refresh.cs ＝ 毎月の全体更新 Refresh()＝全タブの表示値をまとめて作り直す唯一の入口。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 国選択モーダル（SelectNation/CloseDetailModal/RefreshSelectedInfo/MapPolJa）＝モーダル廃止後の残骸を削除（監査2026-07-12 死35：地図クリックはEllipse判定で捨てられ到達不能・XAML参照ゼロ）。州クリックに機能を持たせる案は別件。

    private void Refresh(bool includeMap = true)
    {
        UpdateDate();
        RefreshDaily();   // 国庫の1行は日次と同じ1本（2026-09-11 #19d）＝物価/為替/詳細パネルの価格も同時に最新化
        TreasuryHover = StatG("", "国庫", _histTreasury, Money);   // 国庫の年次推移（ヘッダー国庫のホバー）
        // 技術水準の年次推移（_histTech）は**財政タブの技術効率の行**が読む＝GameViewModel.Fiscal.cs の TechHover（2026-08-15作者裁定の移設先）。
        // ⚠ここで同じものを二度作らない（同じ量を2つの名前で呼ばない）。採取は SampleYearStats の _histTech.Add の1本だけ。
        RefreshTrade();   // 貿易タブ（2026-07-24）：開いている時のみ集計（閉＝即return）
        // 金融（財政タブ）：「ラベル｜値」の行に分解（旧FinanceText＝1本の連結文字列は折返し位置が不定・ホバー対象が曖昧だった）。
        InterestLeverLine = $"政策金利 {_player.Policy.InterestRateTarget:P0}　実効 {_player.InterestRate:P0}";   // B1：他11レバーと同じ「目標／実効」の1行ペア
        LoanRateText = $"{_player.LoanRateAnnual:P1}";                                                          // B1：年率換算（唯一の換算地点＝World.LoanRateAnnual）
        if (_player.Bank is { } bank)
        {
            CoverText = $"{(_player.BroadMoney > 1e-9 ? _player.BaseMoney / _player.BroadMoney : 1.0):P0}";
            HouseholdLoanText = Money(bank.HouseholdLoans);   // ★A-03：POPの個人債務の残高
        }
        else { CoverText = "—"; HouseholdLoanText = "—"; }   // 銀行なし＝兌換系は該当なし
        LossFinancedText = Money(_player.LossFinanced);   // ★A-03：損失の負債化（累積グロス）＝硬性ゲートの分子
        OnPropertyChanged(nameof(CoverTip));   // 兌換カバー率ホバーの内訳（基礎/広義貨幣）も追従

        // 旧・上部バー用の DebtText/DebtVisible と RegionInfo はXAML未バインドの死構築だったため削除（監査2026-07-12 死14/16）。
        // 財政劣化の連鎖（対GDP比・対外債務・緊縮・切下げ累計）は財政タブの DebtLine（UpdateForecast）へ統合。

        if (includeMap) RefreshMap();                  // 地図中心UI：全国の州○を所有国色で再配置（重い＝四半期のみ。月次はスキップ）

        // マップ：国カード（国力順・自国/緊張/戦争・政体色）
        // ★並びはランクが第一キー（作者指摘2026-08-11「ランクと順位が一致しない」）。
        //   原因＝一覧は**今の国力**で並べ、ランクは年1回の再分類（RankUpdateIntervalQuarters=4）の結果なので、
        //   その間に国力が入れ替わると「中堅なのに列強より上」が起こる。ランクで束ねてから国力で並べれば定義上ずれない。
        var powers = _sim.Diplomacy.Powers
            .OrderByDescending(p => (int)_sim.Diplomacy.RankOf(p))
            .ThenByDescending(p => p.Nation != null ? NationalScore.Of(p.Nation) : -1).ToList();
        var me = _sim.Diplomacy.Powers[_playerIndex];
        var myRank = _sim.Diplomacy.RankOf(me);
        // 自国が交戦中の戦争と、その相手国の集合（カード表示・後段の戦争タブ集計の両方が読む＝二重走査をやめる）
        var myWars = new List<War>();
        var warEnemyIds = new HashSet<long>();
        foreach (var war0 in _sim.Diplomacy.Wars) if (war0.A == me || war0.B == me) { myWars.Add(war0); warEnemyIds.Add((war0.A == me ? war0.B : war0.A).Id); }
        // P4 統一戦争（主フラグONのみ）：狙える変体と必要州の保有国リスト（このrefresh内で1回だけ計算）
        var unifRule = _player.Coef.UnificationWarEnabled > 0.5 ? _player.UnificationRule : null;
        var unifTargets = unifRule != null ? Formations.UnificationTargets(_sim.Diplomacy, me, unifRule) : null;
        UnifLine = unifRule != null ? $"統一の機運　目標 {unifRule.NewName}" : "";   // 外交タブ上部の状態表示（機運なし＝非表示）
        var cards = new List<NationCard>(powers.Count);   // 据え置き同期（SyncRows）：行の Button/ツールチップを毎月破棄しない
        for (int oi = 0; oi < powers.Count; oi++)
        {
            var pow = powers[oi];
            bool isMe = pow == me;
            bool war = warEnemyIds.Contains(pow.Id);
            var powRank = _sim.Diplomacy.RankOf(pow);
            string rank = RankJa(powRank);   // 国家ランク（列強/中堅/弱小・改修2026-06-28）
            // ★導出政体（2026-08-21・作者裁定「権力円グラフから政体を算出し、外交パネルでの表示のみに使う」）：
            //   ラベル（w.Polity）でなく PowerAffinity.PolityFromShares（権力シェアの読み出し）を表示する。
            //   忠実度ハーネスの照合と同じ物差し＝画面と帯が同じものを見る。⚠月次リフレッシュ×61カ国×自国POP走査＝軽い。
            Polity? derived = pow.Nation is { } dnw ? PowerAffinity.PolityFromShares(dnw) : (Polity?)null;
            string name = SecessionNames.DisplayName(pow, derived, _sim.Diplomacy.Wars);   // 政体で変わる国号（イギリス共和国 等）・政体をめぐる内戦の間は両陣営が頭書き＋国名（表は SimCore の1か所）
            string score = pow.Nation != null ? NationalScore.Of(pow.Nation).ToString("F0") : "—";
            string polity = PolityRegimeJa(derived);   // ★導出政体（同上）
            // 低い＝多民族/多宗派（移民を引く・分離圧力）。宗教統一は民族統一と対称の派生スカラー＝州宗教の最大シェア（#62で露出）
            string unity = pow.Nation is { EthnicEnabled: true } nn ? $"民族統一 {nn.EthnicUnity:P0}　宗教統一 {nn.ReligiousUnity:P0}" : "";
            // 緊張：自国は「—」、それ以外は**開戦までの近さ**（100%＝宣戦の閾値・TensionPct）。交戦はバッジで示す。
            string tension = isMe ? "—" : $"{TensionPct(_sim.Diplomacy.Tension(me, pow)):P0}";
            // 関係バッジ（優先順）：戦争中＞属国/宗主（自国）＞他国の属国＞同盟＞領土なし（帰属の明記・2026-07-19）
            var powSuz = _sim.Diplomacy.Suzerain(pow);
            string badge = ""; IBrush badgeBrush = BadgeDefault;
            if (isMe) { }
            else if (war) { badge = "戦争中"; badgeBrush = NewsRed; }
            else if (powSuz == me) { badge = "自国の属国"; badgeBrush = BadgeOwnVassal; }
            else if (_sim.Diplomacy.Suzerain(me) == pow) { badge = "自国の宗主"; badgeBrush = BadgeOwnSuzerain; }
            // ★2026-08-01：宗主名を入れると外交タブの行が折り返して太くなる。宗主名はホバーの詳細に残る
            // ★2026-09-23：バッジ本文は**宗主名だけ**にした（旧＝「○○の属国」・作者指示2026-08-03）。
            //   実写で MaxWidth 66px（DiploTab.axaml のバッジ）に収まらず「オヨ帝国の属」＝**「の属」という存在しない語**が出ていた。
            //   固有名詞は途中で切れても固有名詞のままだが「○○の属国」は末尾が落ちると別の語になる。属国であることは色（BadgeOtherVassal）が担う。
            //   ⚠全文（「○○の属国」）はホバーの詳細の「関係:」行に元から残っている（下の rel.Add）。
            else if (powSuz is not null) { badge = powSuz.Name; badgeBrush = BadgeOtherVassal; }
            else if (_sim.Diplomacy.HasAlliance(me, pow)) { badge = "同盟"; badgeBrush = NewsGreen; }
            else if (pow.Nation is null && pow.ColonyList.Count == 0) { badge = "領土なし"; badgeBrush = BadgeNoTerritory; }   // 全て接収済みの抽象標的（侵略しても空振り＝ボタンは不可理由つきで灰色）
            // 外交アクションの可否：同ランク＝同盟（緊張の増減は#106でポップアップへ移行）／弱小への侵略（実国家間の開戦＝緊張の創発のみ）。
            // ★2026-08-23作者裁定：植民地化と侵略は**1本**（侵略）。標的が弱小なら抽象標的でも実在国でも同じボタン＝
            //   自国のランクでボタンが出たり消えたりしない（エンジンの World.CanInvade にもランク条件は無い＝AIと同じ動詞・同じ可否）。
            // 同盟の可否判定（canTension/canAlly）は削除（2026-08-10 死コード掃除）＝同盟は自動化済みでボタンが無く、算出値の行き先が無かった。
            bool showInvade = !isMe && powRank == PowerRank.Minor;
            string invReason = "";
            bool canInvade = showInvade && _player.CanInvade(pow, out invReason);
            if (!showInvade || canInvade) invReason = "";
            bool showUnif = unifTargets != null && unifTargets.Contains(pow);   // P4：必要州の保有国にのみ表示
            string unifReason = "";
            bool canUnif = showUnif && _player.CanUnificationWar(pow, out unifReason);
            if (!showUnif || canUnif) unifReason = "";
            double willCost = showInvade ? _player.ExpansionCostFor(pow) : 0;   // ゲート・実消費・表示は同一の式（国力比・2026-08-23）
            double wcap = _player.Coef.ExpansionWillCap;
            // 不可の理由（グレーアウト時のツールチップ）：不足系は「必要量（現在値）」で言い換え。
            string? ReasonTip(string reason) => reason switch
            {
                "" => null,
                "余剰戦力が不足" => $"戦力余剰が{willCost / wcap:P0}必要(現在 {_player.ExpansionWill / wcap:P0})",   // ⚠case の文字列は**エンジンの理由**（World.CanInvade:1880）＝変えない。変えたのは表示だけ（2026-09-23「余剰戦力」→「戦力余剰」）
                "属国が上限" => $"属国 {_sim.Diplomacy.DependentCount(me)}/{_player.Coef.MaxDependents}",
                "兵力が不足" => $"兵力が標的の{_player.Coef.InvadeMilitaryFactor:F1}倍必要(自国 {me.Military * _player.MilitaryReadiness:F2}/標的 {pow.Military:F2})",
                _ => reason,
            };
            // 緊張の色：低=青→中=黄→高=赤
            double tVal = isMe ? 0 : TensionPct(_sim.Diplomacy.Tension(me, pow)) * 100.0;   // 色も同じ物差し（100=開戦）
            IBrush tBrush = isMe ? Brushes.Gray : tVal < 35 ? TensionLow : tVal < 70 ? TensionMid : TensionHigh;

            // ホバー詳細：スコアの内訳（軍事/富/金融/技術）＋緊張が高まっている要因。
            string detail = $"{name}({rank})　{polity}";
            if (pow.Nation is { } nw)
            {
                detail += $"\n列強スコア:{score} 軍事:{NationalScore.Military(nw):F0} 生産:{NationalScore.Wealth(nw):F0} 金融:{NationalScore.Finance(nw):F0} 技術:{NationalScore.Tech(nw):F0}";
                if (unity.Length > 0) detail += "\n" + unity;
                // 主流の文化/宗教（州ラベルの人口加重多数派）＝下の「民族の隔たり/宗教の隔たり」の素（#62）
                if (pow.Culture.Length > 0 || pow.Religion.Length > 0) detail += $"\n文化: {pow.Culture}　宗教: {pow.Religion}";
                if (!isMe && me.Nation is not null)
                {
                    // 緊張要因＝UIで再導出せず、エンジンの理由別台帳（TensionBreakdown）をそのまま出す（二重真理の解消）。
                    // 表示名は Diplomacy.TensionReasonJa（多対一）＝同名チャネルは束ねてから並べる（緊張モーダル＝Popups.cs と同じ規約）。
                    var merged = new List<(string Name, double Value)>();
                    foreach (var (reason, v) in _sim.Diplomacy.TensionBreakdown(me, pow))
                    {
                        string rname = Diplomacy.TensionReasonJa(reason);
                        int at = merged.FindIndex(x => x.Name == rname);
                        if (at >= 0) merged[at] = (rname, merged[at].Value + v);
                        else merged.Add((rname, v));
                    }
                    var factors = merged.Where(f => TensionPct(f.Value) >= 0.005).OrderByDescending(f => f.Value).ToList();   // 0%に丸まる寄与は出さない（緊張モーダルと同じ足切り）
                    if (factors.Count > 0)
                    {
                        detail += "\n緊張要因";
                        foreach (var f in factors)
                            detail += $"\n・{f.Name} +{TensionPct(f.Value):P0}";
                    }
                }
                // 自国との関係（同盟・禁輸・属国/宗主）＝意思決定情報
                var rel = new List<string>();
                if (_sim.Diplomacy.HasAlliance(me, pow)) rel.Add("防衛同盟");
                if (_sim.Diplomacy.HasEmbargo(me, pow)) rel.Add("禁輸中");
                if (powSuz == me) rel.Add("自国の属国");
                else if (powSuz is not null) rel.Add($"{powSuz.Name}の属国");   // 帰属の明記（2026-07-19）：他国の勢力圏もホバーで判る
                if (_sim.Diplomacy.Suzerain(me) == pow) rel.Add("自国の宗主");
                if (rel.Count > 0) detail += "\n関係: " + string.Join("　", rel);
                // 経済データ行（外国経済の可視化doc §3-3・2026-07-24）：19世紀でも新聞で公知のマクロ量のみ（帳簿の全公開はしない）
                if (pow.Nation is { } enw)
                {
                    detail += $"\n経済: GDP {Money(enw.NominalGdp)}　工業化 {enw.MeanIndustrialization:F1}　物価 {enw.PriceLevel:F2}"
                            + $"\n金本位 {(enw.OnGoldStandard ? "○" : "×")}　恐慌 {(enw.GdpPcDrawdown > DepressionSevereDrawdown ? "○" : "×")}　関税率 {enw.Tariff:P0}";   // 「恐慌」の閾値は通知・年代記と同じ 0.25（2026-08-23）
                }
            }
            else detail += $"\n残る領土 {pow.ColonyList.Count}";   // 抽象標的＝未接収の植民地数（0=領土なし・奪う物が無い）
            // 通商条約の行（ShowTreaty/CanTreaty/TreatyTip）は廃止（2026-09-07作者指示）＝締結は Diplomacy.TradeTreaties が四半期に自動で行う（プレイヤーもAIと同じ掃引）
            if (oi == 0 || _sim.Diplomacy.RankOf(powers[oi - 1]) != powRank)
                cards.Add(new NationCard { Header = rank, PowerName = "＿見出し＿" + rank });   // 区切り（PowerNameは同期キー＝国名と衝突しない印）
            cards.Add(new NationCard
            {
                Order = (oi + 1).ToString(),
                Name = name,
                PowerName = pow.Name,
                Tension = tension,
                Detail = detail,   // ホバー詳細（スコア内訳＋緊張の要因）
                Color = PolityColor(derived),   // 国号・政体名（name/polity）と同じ導出政体（PolityFromShares）＝色と文字が別の政体を指さない
                Badge = badge,
                BadgeBrush = badgeBrush,
                RowBrush = isMe ? SelfRowHighlight : Brushes.Transparent,   // ライトテーマの自国ハイライト
                ShowInvade = showInvade,
                CanInvade = canInvade,
                ShowUnif = showUnif,
                CanUnif = canUnif,
                UnifTip = ReasonTip(unifReason),
                InvadeLabel = "侵略",   // ★2026-08-23作者裁定：植民地化と1本化＝弱小への動詞はこれ1つ
                InvadeTip = ReasonTip(invReason),
                TensionBrush = tBrush,
                InvadeCmd = DiploInvadeCommand,
                UnifCmd = DiploUnifWarCommand,
                CenterCmd = CenterNationCommand,
            });
        }
        // 2段組（2026-08-10）：前半を左段・後半を右段へ。奇数なら左段が1つ多い＝上から詰まって見える。
        int half = (cards.Count + 1) / 2;
        SyncRows(NationsLeft, cards.GetRange(0, half), c => c.PowerName, (t, s) => t.CopyFrom(s));
        SyncRows(NationsRight, cards.GetRange(half, cards.Count - half), c => c.PowerName, (t, s) => t.CopyFrom(s));

        double willCap = _player.Coef.ExpansionWillCap;
        ExpansionText = $"戦力余剰:{_player.ExpansionWill / willCap:P0}";   // ★2026-09-23作者指示「余剰戦力」→「戦力余剰」。好戦世論はホバー内訳の先頭へ統合（ヘッダーの類似数値の削減・2026-07-16）
        // ★政治資本（MandateSpent）のヘッダー表示は撤去（2026-08-15作者確認＝選挙梯子E5で廃止予定・表示部分を前倒し。内部は「紛争記憶」への改名待ち＝E5本体）
        OnPropertyChanged(nameof(ExpansionTip));   // 戦力余剰・好戦世論のホバー内訳を再計算
        // ★防衛同盟の枠数はヘッダーから撤去（2026-08-23作者指示）＝同盟の有無は外交タブの国カードのバッジ「同盟」で読む。

        // 概況（上部バー2行目）：人口・GDP・生産・技術力・識字率・満足/闘争度・汚職・正統性・行政力・軍事力。一部はホバーで年次推移グラフ。
        double popTotal = 0, litSum = 0;
        foreach (var rp in _player.AllPops()) { popTotal += rp.Size; litSum += rp.Size * rp.Literacy; }
        double lit = popTotal > 1e-9 ? litSum / popTotal : 0;
        double corrRate = EffectiveCorruption();
        // 技術力＝世界前線比（自国技術÷各国最高＝0〜100%）。開始盤面は技術の助走（WarmupTech・2026-07-13）で前線t≈0.23まで育つ＝
        // 全国ゼロの「団子」を脱し、識字×世俗教育の差が前線比で見える（前線<0.05のみ「揺籃期」＝助走を通らない単一国/古いセーブの安全弁）。
        // 旧表示「技術力:{TechLevel:P0}」は0〜3の絶対値を%扱い（後期300%超）＋「遅れ」を別スケールの前線比で出す不整合＝初期に0%/98%と矛盾表示になっていた（2026-07-12修正）。
        // ★技術力の対最先端比はヘッダーから撤去（2026-08-15＝財政タブの技術効率へ移設）
        // ★軍事力（順位＋即応度）はヘッダーから撤去（2026-08-24作者指示）＝戦争タブの「理想の軍隊」の節見出しへ移設（ArmyIdealHeader）。
        // 並び＝**GDPが先頭**（2026-08-02ユーザー指示）。以降は論理グループ順：経済（GDP・ジニ係数・技術力）→
        // 人的（人口・識字率・満足度・闘争度）→統治（汚職・治安・行政力・正統性・国民意識）→軍事力。
        // GDPと1人あたりGDPの世界順位（2026-08-23作者指示）＝自国より上の国の数＋1。物差しは軍事力の順位（milPos）と同じ。
        // ★1人あたりは**人単位**（TotalPopulation の内部単位は千人＝×1000で人に直す）。GDPは月次フロー＝1人あたりも同じ期間。
        double myGdp = _player.Gdp, myPop = _player.TotalPopulation;
        double myGdpPc = myPop > 1e-9 ? myGdp / (myPop * 1000.0) : 0;
        int gdpPos = 1, gdpPcPos = 1;
        // ★2026-09-23：上位5カ国の一人当たりGDPの一覧（pcList）は撤去＝ホバーが折れ線だけになり読み手が消えた（毎月61カ国ぶんの整形も消える）
        foreach (var q in powers)
        {
            if (q == me || q.Nation is not { } qw) continue;
            double qPop = qw.TotalPopulation;
            double qPc = qPop > 1e-9 ? qw.Gdp / (qPop * 1000.0) : 0;
            if (qw.Gdp > myGdp) gdpPos++;
            if (qPc > myGdpPc) gdpPcPos++;
        }
        // ★2026-09-23（作者指示「詳細表示は全てグラフのみ」）：ヘッダーのホバーは**折れ線だけ**になった。
        //   ⚠例外は1行だけ＝一人当たりGDPと世界順位（GDPの量なので置き場はGDPのグラフのホバーしかない・作者指示）。
        //   上位5カ国の一人当たりGDPの一覧とティア内訳（主食/日用品/…/素材）は撤去した。
        string gdpHover = $"GDP　一人当たり {MoneyPerHead(myGdpPc)}　世界{gdpPcPos}位";
        // ★2026-09-23（作者指示「文字数固定」）：各項目の箱の最小幅（画素）。この行は FontBody=13。
        //   下の数値は「1900年前後に出うる最長の文字列」の実寸を2の倍数へ切り上げたもの。
        //     地位:列強12位 84（★2026-09-23に1行目から移設） ／ GDP:£999.99M(61位) 130 ／ 人口:1234.56M人 104 ／
        //     識字率:100% 76（満足度・不満度・闘争度・行政力・正統性も同じ） ／ 工業化:1000% 84 ／
        //     外資信用:100% 90 ／ 国民意識:-100% 96。統一の機運は**末尾**なので0（右に押すものが無い）。
        //   合計＝1044px＋項目間の余白 9px×12＝1152px（全画面1920なら可用 1896px に収まる）。実測（1783全画面）で末尾のインク右端は1142px。
        // 地位：列強は順位付き「列強3位」・それ以外は「準列強」「中堅」「弱小」。★2026-09-23に1行目（国名の隣）から概況行へ移したので、
        // 下の stats が読む前にここで作る（旧はSyncRowsの後で作っており、そのまま stats に入れると1ヶ月前の値が出る）。
        // 順位＝国力スコア（NationalScore）で自国より上の列強の数＋1（旧＝Powersリストの生成順インデックスは無関係で、
        // 仏が32番目に生成されると「列強32位」と誤表示していた不具合の修正・2026-07-12）。
        int myPos = 1;
        if (myRank == PowerRank.Great)
        {
            double myScore = NationalScore.Of(_player);
            foreach (var q in powers)
            {
                if (q == me || q.Nation is not { } qw || _sim.Diplomacy.RankOf(q) != PowerRank.Great) continue;
                double qs = NationalScore.Of(qw);
                if (qs > myScore || (qs == myScore && q.Id < me.Id)) myPos++;   // 同点は Id で全順序（ComputeRanks と同規約）
            }
        }
        PlayerName = _player.SelfPower?.Name ?? _player.Region.Name;
        PlayerRankText = myRank == PowerRank.Great ? $"列強{myPos}位" : myRank == PowerRank.Secondary ? "準列強" : myRank == PowerRank.Middle ? "中堅" : "弱小";
        OnPropertyChanged(nameof(PlayerScoreTip));   // 国名ホバーのスコア4分解を再計算
        var stats = new List<HeaderStat>
        {
            // ★2026-09-23：地位は国名の隣から**この行**へ移した＝1行目が2段になる退行の是正（算術は GameView.axaml の1行目のコメント）。
            //   順位もランクも世界が動かす量＝この行の他11項と同類。1行目に残すと 76+12px を食い、最長の国名
            //   「サルデーニャ＝ピエモンテ」（実測171px）が1行目に入らず戦力余剰が2段目へ落ちていた（撮影で確認）。
            //   最長「地位:列強12位」＝13pxで実測84px。
            StatPlain($"地位:{PlayerRankText}", "地位", 84),
            // ★2026-09-23作者指示：「GDPはnk」→「GDP:nk」（この1行だけ「は」で他は全部「名前:値」だった）
            StatG($"GDP:{Money(myGdp)}({gdpPos}位)", "GDP", _histGdp, Money, 130, gdpHover),   // 後退（ドローダウン）付記は削除（2026-07-18ユーザー指示＝不要）
            // ★2026-09-23作者指示：ジニ係数は撤去（採取 _histGini は残す＝セーブの系列配列に居る）。
            //   ⚠その空きに技術水準を置くのは**差し戻した**＝2026-08-15作者裁定「技術力はヘッダーから撤去＝財政タブの技術効率へ移設」が生きており、
            //     その撤去の記録がすぐ上の行にコメントで残ったまま再設置していた。年次グラフは TechHover で財政タブへ渡す（_histTech の採取は継続）。
            StatG($"人口:{PopFmt(popTotal)}", "人口", _histPop, PopFmt, 104),   // 内訳（移民の流入/流出・子供比/老年比）はPOPタブへ移送（2026-09-23）
            StatG($"識字率:{lit:P0}", "識字率", _histLit, v => v.ToString("P0"), 76),
            StatG($"満足度:{NationalScore.MeanSatisfaction(_player):P0}", "満足度", _histSat, v => v.ToString("P0"), 76),
            // ★2026-09-23作者指示「識字率の右に満足度・不満度・闘争度の順」＝足りないのは不満度1つ。
            //   値はエンジンの集計 Politics.AvgMassDiscontent をそのまま呼ぶ（App側に式を書き写さない）。
            //   ★2026-09-25：満足度（NationalScore.MeanSatisfaction）も「大衆のみ」（Politics.IsMassPop）へ揃えた＝3つの数字が同じ母集団を見る。
            //   ⚠年次グラフの _histSat はセーブされる系列＝2026-09-25 より前のセーブを読むと、その年までは全POPの定義の値が混ざる。
            StatG($"不満度:{Politics.AvgMassDiscontent(_player):P0}", "不満度", _histDis, v => v.ToString("P0"), 76),
            StatG($"闘争度:{Politics.AvgMassMilitancy(_player):P0}", "闘争度", _histMil, v => v.ToString("P0"), 76),
            // 生産はヘッダーから撤去（GDP＋役務とほぼ同値・2026-07-16）。_histProd の採取とセーブ枠は互換のため保持。
            // ★2026-08-13：国力スコア7項のうちヘッダーに無かった3つ（工業化・対外信用・金融）を追加。
            //   工業化はスコア最大の重み(×9)で本作の主題そのものなのに、**自国の全体値を出す場所がUIに1つも無かった**。
            //   対外信用は外資の流入・引き上げ・配当を全部決めるのに同じく不在（作者指摘）。
            StatG($"工業化:{_player.MeanIndustrialization:P0}", "工業化", _histInd, v => v.ToString("P0"), 84),   // ★2026-08-31：％表示
            // ★2026-08-15作者指示：「対外信用」は国家信用とも読める＝実体（外資が付くか/逃げるかの係数）に合わせ「外資信用」へ改名
            StatG($"外資信用:{_player.CreditRating:P0}", "外資信用", _histCredit, v => v.ToString("P0"), 90),   // 4項の内訳（国債残高/兌換/為替/利回り）は財政タブへ移送（2026-09-23）
            // ★2026-08-31作者指示：金融(x/6)と汚職はヘッダーから撤去（_histFin/_histCorr の採取はセーブ互換のため継続）
            // ★2026-08-15作者指示(5)：治安はヘッダーから撤去＝州ホバーへ移設済み（Map.cs・悪化州のみ表示。SecurityTermsも削除済み）
            // ★2026-09-23：行政力は年次系列を持たない＝グラフが作れない＝ホバーを持たない素の項目にする（内訳は財政タブへ移送）
            StatPlain($"行政力:{_player.AdminPowerSmoothed:P0}", "行政力", 76),
            StatG($"正統性:{_player.Legitimacy:P0}", "正統性", _histLeg, v => v.ToString("F3"), 76),        // 説明API（#63）の内訳は政治タブへ移送（2026-09-23）
            StatG($"国民意識:{_player.NationalConsciousnessDisplay:P0}", "国民意識", _histCons, v => v.ToString("P0"), 96),   // ★2026-08-02：開始時から値がある（World 側で初期値を読み側に持たせた）
            // 政治資本（MandateSpent）はヘッダー上段の戦力余剰の隣へ移動（2026-07-20ユーザー指示）＝CoercionHeaderText。年次履歴 _histMandate は互換のため採取継続。
        };
        // ★2026-09-23：統一の機運も1行目から**この行の末尾**へ（1行目の2段化の是正）。1行目では最大265px を食い、しかも
        //   機運が立つのは長い国名の国（サルデーニャ＝ピエモンテ→イタリア・プロイセン→ドイツ）＝国名と同時に溢れる組み合わせだった。
        //   末尾＝右に押すものが無いので箱の最小幅は要らない（0）。文言は UnifLine の一本（Fiscal.cs が作る）をそのまま読む。
        if (UnifLine.Length > 0) stats.Add(StatPlain(UnifLine, "統一の機運", 0));
        // ★王権支持度（天命×有効度＋世論の支持）のヘッダー表示は撤去（2026-08-23作者指示）。エンジン側の天命（DynasticMandate）は稼働のまま＝表示だけを畳む。
        // ★制度疲労のヘッダー表示は撤去（2026-08-15作者指示＝機構ごと廃止予定：汚職V2の制度疲労ループが受け皿・台本を創発へdocの残項）
        SyncRows(HeaderStats, stats, h => h.Key, (t, s) => t.CopyFrom(s));   // 据え置き同期＝ヘッダー2行目が点滅しない（キーは GraphTitle でなく Key＝空にならない）

        // 戦争タブ（レビュー2026-07-19で改構）：犠牲者＝エンジンの戦争別台帳 War.DeadA/DeadB（兵士＋将校）と CivDeadA/CivDeadB（民間人）＝内戦・粛清の混入なし、
        // 戦費＝エンジンが実際に払った軍の月次の支出（_monthWarCost＝台帳の陸軍・海軍・戦費・GameViewModel.cs の OnTick が取る）を戦線数で均等按分した累計、
        // 開戦日＝war.Months から導出、勝敗＝講和ログを一次判定。
        var wrows = new List<WarRow>();
        var nowEnemies = new HashSet<long>();
        bool newEnemy = false;
        bool seeded = _warSeeded; _warSeeded = true;
        int monthStamp = (int)(_simDays / (double)Calendar.DaysPerMonth + 1e-9);
        // ★2026-09-23 戦費按分の分母を「その月に抱えていた戦線数」で固定する。旧＝継続中は myWars.Count、終戦した戦争は
        //   myWars.Count+1 を各自が使っていたので、**同じ月にN件が終戦すると分母が 0+1=1 になり、N件それぞれが
        //   その月の国全体の戦費を丸ごと受け取っていた**（思想⑨の二重計上）。終戦月も軍の支出は立つ＝継続分＋終戦分＝その月の全戦線。
        foreach (var w0 in myWars) nowEnemies.Add((w0.A == me ? w0.B : w0.A).Id);
        int endedThisRefresh = 0;
        foreach (var kv in _warTracks) if (!nowEnemies.Contains(kv.Key)) endedThisRefresh++;
        int frontsThisMonth = myWars.Count + endedThisRefresh;
        bool accrue = myWars.Count > 0 && monthStamp != _warCostStamp;   // 月が変わった最初のRefreshだけ按分（ボタン起因の再Refreshで二重加算しない）
        if (accrue) _warCostStamp = monthStamp;
        double costShareMonth = frontsThisMonth > 0 ? _monthWarCost / frontsThisMonth : 0;
        foreach (var war in myWars)
        {
            bool meIsA = war.A == me;
            var enemy = meIsA ? war.B : war.A;
            double sc = meIsA ? war.Score : -war.Score;
            if (!_warTracks.TryGetValue(enemy.Id, out var tr))
            {
                _warTracks[enemy.Id] = tr = new WarTrack { Won0 = me.WarsWon, Lost0 = me.WarsLost };
                if (seeded) newEnemy = true;   // 初回Refresh（新規開始/ロード直後）は既存の戦争で自動オープンしない
            }
            if (accrue) tr.CostShare += costShareMonth;
            tr.Civil = war.CivilWarParent is not null; tr.Secession = war.CivilWarSecession; tr.MeParent = war.CivilWarParent == me;
            tr.Demand = tr.Civil && war.CivilWarParent!.Nation?.Polity == war.CivilWarPolity;   // 同じ政体の要求戦
            tr.EnemyName = SecessionNames.WarName(enemy, _sim.Diplomacy.Wars);   // 政体をめぐる内戦の政府側は戦争の間だけ頭書き＋国名（反乱側は名そのものが頭書きつき）
            tr.LastScore = sc;
            tr.LastMonths = war.Months;
            tr.LastDeadSelf = meIsA ? war.DeadA : war.DeadB;
            tr.LastDeadEnemy = meIsA ? war.DeadB : war.DeadA;
            tr.LastCivDeadSelf = meIsA ? war.CivDeadA : war.CivDeadB;
            tr.LastCivDeadEnemy = meIsA ? war.CivDeadB : war.CivDeadA;
            wrows.Add(new WarRow
            {
                Key = enemy.Id.ToString(),
                Enemy = tr.EnemyName,
                Score = $"戦勝点 {sc:+0%;-0%;0%}",   // ★2026-09-23：第3節（ゼロ節）を足す＝2節のままだと0へ丸まる負が `-+0%`（符号2つ）で出る（.NET 10 実測）
                ScoreBrush = sc > 0.05 ? TermPlus : sc < -0.05 ? TermMinus : TermNeutral,
                Info = $"{DateAt(_simDays - war.Months * (double)Calendar.DaysPerMonth)} 開戦　{war.Months}ヶ月経過",
                Forces = $"自軍 {Troops(me)}　即応度 {_player.MilitaryReadiness:P0}　敵軍 {Troops(enemy)}",   // ★実兵数（2026-08-08）
                // ★2026-09-23：兵士の戦死と民間人の死を**別の行**で出す（民間は兵の17〜25倍＝1本に足すと兵の戦死が桁で消える）
                Losses = $"戦死 自国 {PopFmt(tr.LastDeadSelf)}" + (enemy.Nation is not null ? $"　相手 {PopFmt(tr.LastDeadEnemy)}" : "")
                       + $"\n民間人の死 自国 {PopFmt(tr.LastCivDeadSelf)}" + (enemy.Nation is not null ? $"　相手 {PopFmt(tr.LastCivDeadEnemy)}" : ""),
                Cost = $"戦費 {Money(tr.CostShare)}",
                Exhaustion = $"厭戦 自国 {me.WarExhaustion:P0}　相手 {enemy.WarExhaustion:P0}",
                Terms = ExplainTerms("Exhaustion", v => v.ToString("P0")),   // 自国の厭戦の内訳（説明API#63）
            });
        }
        SyncRows(WarRows, wrows, r => r.Key, (t, s) => t.CopyFrom(s));
        // 終わった戦争：追跡中だが Wars に無い＝講和済み。エンジンの終戦記録（EndedWars＝終戦月の最終値）を一次ソースに、
        // 無い場合のみ勝敗回数の増分でフォールバック（レビュー2026-07-19：ログ文字列照合は包含国名で誤爆するため廃止）。
        foreach (var (id, tr) in _warTracks.Where(kv => !nowEnemies.Contains(kv.Key)).ToList())
        {
            var ep = _sim.Diplomacy.Powers.FirstOrDefault(p => p.Id == id);
            EndedWarRecord? rec = null;
            for (int ri = _sim.Diplomacy.EndedWars.Count - 1; ri >= 0; ri--)
            {
                var r = _sim.Diplomacy.EndedWars[ri];
                if ((r.AId == me.Id && r.BId == id) || (r.BId == me.Id && r.AId == id)) { rec = r; break; }   // 同ペアの最新＝いま終わった戦争（終戦は単一経路）
            }
            double scEnd = rec is null ? tr.LastScore : rec.AId == me.Id ? rec.Score : -rec.Score;
            double deadSelf = rec is null ? tr.LastDeadSelf : rec.AId == me.Id ? rec.DeadA : rec.DeadB;
            double deadEnemy = rec is null ? tr.LastDeadEnemy : rec.AId == me.Id ? rec.DeadB : rec.DeadA;
            double civSelf = rec is null ? tr.LastCivDeadSelf : rec.AId == me.Id ? rec.CivDeadA : rec.CivDeadB;
            double civEnemy = rec is null ? tr.LastCivDeadEnemy : rec.AId == me.Id ? rec.CivDeadB : rec.CivDeadA;
            int months = rec?.Months ?? (tr.LastMonths + 1);
            string result = rec is not null
                ? (Math.Abs(rec.Score) < _player.Coef.WhitePeaceScore ? "白紙講和" : scEnd > 0 ? "勝利" : "敗北")
                : me.WarsWon > tr.Won0 && me.WarsLost == tr.Lost0 ? "勝利"
                : me.WarsLost > tr.Lost0 && me.WarsWon == tr.Won0 ? "敗北" : "白紙講和";
            tr.CostShare += costShareMonth;   // 終戦月の戦費（軍の支出は終戦月も立つ＝取りこぼし補填）。分母は継続分＋終戦分＝その月の全戦線
            PastWarRows.Insert(0, new PastWarRow
            {
                Key = $"{id}@{(int)_simDays}",
                EndDay = _simDays,
                Title = $"{ep?.Name ?? (tr.EnemyName.Length > 0 ? tr.EnemyName : "—")}　{result}",
                Detail = $"{DateAt(_simDays - months * (double)Calendar.DaysPerMonth)}〜{EventDate()}　{months}ヶ月"
                       + $"\n戦死 自国 {PopFmt(deadSelf)}" + (ep?.Nation is not null ? $"　相手 {PopFmt(deadEnemy)}" : "")
                       + $"\n民間人の死 自国 {PopFmt(civSelf)}" + (ep?.Nation is not null ? $"　相手 {PopFmt(civEnemy)}" : "")
                       + $"\n戦費 {Money(tr.CostShare)}",
            });
            // #106 終戦ポップアップ：双方の戦死・戦費・得失を表示（同四半期に複数終戦なら最初の1件）
            if (!WarEndPopupOpen)
            {
                WarEndPopupBody = $"{ep?.Name ?? (tr.EnemyName.Length > 0 ? tr.EnemyName : "—")} との戦争が終結した。\n\n"
                    + $"結果:{result}(戦勝点 {scEnd:+0%;-0%;0%})　{months}ヶ月\n"   // ★2026-09-23：第3節（ゼロ節）＝0へ丸まる負の `-+0%` を止める
                    + $"戦死　自国 {PopFmt(deadSelf)}" + (ep?.Nation is not null ? $"/相手 {PopFmt(deadEnemy)}" : "") + "\n"
                    + $"民間人の死　自国 {PopFmt(civSelf)}" + (ep?.Nation is not null ? $"/相手 {PopFmt(civEnemy)}" : "") + "\n"
                    + $"戦費　{Money(tr.CostShare)}\n\n"
                    + (tr.Civil ? CivilWarOutcome(tr, scEnd)
                       : Math.Abs(scEnd) < _player.Coef.WhitePeaceScore ? "得たもの:なし(白紙講和)"
                       : scEnd > 0 ? "得たもの:賠償金"
                       : "失ったもの:賠償金");
                WarEndPopupOpen = true;
            }
            _warTracks.Remove(id);
        }
        for (int pi2 = PastWarRows.Count - 1; pi2 >= 0; pi2--)
            if (_simDays - PastWarRows[pi2].EndDay > Calendar.DaysPerYear * PastWarKeepYears) PastWarRows.RemoveAt(pi2);
        PlayerAtWar = WarRows.Count > 0;
        if (WarRows.Count == 0 && _player.Mobilized) { _player.Mobilized = false; OnPropertyChanged(nameof(PlayerMobilized)); }   // #106：講和で自動的に動員解除（総動員は戦時のみ）
        NoActiveWars = WarRows.Count == 0;
        HasPastWars = PastWarRows.Count > 0;
        // ★他国の戦争への調停（2026-09-06）：自国が当事国でない戦争を列挙し、AIと同じ述語で可否と理由（データのみ）を出す。
        {
            var mrows = new List<MediationRow>();
            var cM = _player.Coef;
            foreach (var war in _sim.Diplomacy.Wars)
            {
                if (war.A == me || war.B == me) continue;
                if (war.A.Nation is null && war.B.Nation is null) continue;
                bool ok = _sim.Diplomacy.CanOfferPeace(me, war, cM, out var why);
                double sc = war.Score;
                // 政体をめぐる内戦の政府側は戦争の間だけ頭書き＋国名（戦争タブの自国の戦争と同じ表＝SimCore の1か所）
                string aName = SecessionNames.WarName(war.A, _sim.Diplomacy.Wars), bName = SecessionNames.WarName(war.B, _sim.Diplomacy.Wars);
                string lead = System.Math.Abs(sc) < 0.05 ? "拮抗" : (sc > 0 ? $"{aName}が優勢 {sc:+0%}" : $"{bName}が優勢 {-sc:+0%}");
                mrows.Add(new MediationRow
                {
                    Key = $"{war.A.Id}|{war.B.Id}",
                    Title = $"{aName} ⚔ {bName}",
                    Info = $"{war.Months}ヶ月経過　{lead}" + (ok ? $"　戦力余剰 {WillPct(_sim.Diplomacy.InterventionWillCost(me, war, cM))}" : $"　{why}"),   // ★2026-09-23：語を「戦力余剰」へ統一
                    CanMediate = ok, Reason = why,
                });
            }
            SyncRows(MediationRows, mrows, r => r.Key, (t, s2) => t.CopyFrom(s2));
            NoMediation = MediationRows.Count == 0;
        }
        WarTabVisible = true;   // 戦争タブは常時表示（国内勢力を統合＝戦争がなくても勢力の閲覧/鎮圧に使う・2026-07-20）
        if (newEnemy && !ProdOpen) ActiveTab = "戦争";   // 開戦＝戦争タブを自動で開く（生産タブ操作中は邪魔しない）

        // 外交ログ（開戦・講和・同盟・植民地化・独立…）を事件ログへ（新規分のみ）。
        // 日付は yyyy/mm/dd（事件の発生日）。発生地＝本文に最初に現れる国の首都＝遠い事件ほど遅れて届く（QueueNews と同じ距離ラグ）。
        var dlog = _sim.Diplomacy.Log;
        for (int li = _diploLogSeen; li < dlog.Count; li++)
        {
            string raw = dlog[li];
            string body = raw;
            int sp = raw.IndexOf(' ');
            if (sp > 0 && int.TryParse(raw[..sp], out _)) body = raw[(sp + 1)..];   // 先頭の年トークンを除去（yyyy/mm/dd に置換）
            bool warLine = body.Contains("開戦") || body.Contains("宣戦");
            string icon = warLine ? "⚔" : body.Contains("同盟") ? "🤝" : body.Contains("講和") ? "🕊" : body.Contains("侵略") || body.Contains("属国") ? "🏴" : body.Contains("成立を宣言") || body.Contains("から独立") ? "👑" : body.Contains("座礁") ? "🏭" : body.Contains("職人が没落") ? "🔨" : "🌐";   // 座礁/職人の没落（通知・2026-09-05）
            // 発生地＝本文で最初に名前が出る国。**自国が当事者（本文に自国名）なら距離ラグなしで即時**＝宣戦布告される・属国化される等の重大事を見逃さない。
            bool mentionsMe = body.Contains(Me.Name, StringComparison.Ordinal);
            Power? src = null; int best = int.MaxValue;
            foreach (var p in _sim.Diplomacy.Powers)
            {
                int ix = body.IndexOf(p.Name, StringComparison.Ordinal);
                if (ix < 0 && p.BaseName != p.Name) ix = body.IndexOf(p.BaseName, StringComparison.Ordinal);   // 変名直前に書かれたログ行は旧名のまま＝旧名でも解決
                if (ix >= 0 && ix < best) { best = ix; src = p; }
            }
            double sLat = _player.Latitude, sLon = _player.Longitude; bool isMine = true;
            if (src is not null && src != Me && !mentionsMe)
            {
                isMine = false;
                if (src.Nation is { } sw) { sLat = sw.Latitude; sLon = sw.Longitude; }
                else if (src.ColonyList.Count > 0) { sLat = src.ColonyList[0].Latitude; sLon = src.ColonyList[0].Longitude; }
            }
            var lineBrush = warLine && mentionsMe ? NewsRed : NewsBlue;   // 自国が絡む開戦＝赤で強調
            QueueNewsText(icon, body, lineBrush, sLat, sLon, isMine);   // 日付は配達時に付与（届いた日＋遅延ヶ月）
        }
        _diploLogSeen = dlog.Count;

        // 教育費（義務教育の国庫負担）・専売益・外資フローの実績＝州の累積カウンタの四半期差分→月平均（市場依存で毎tick予測が難しい項目）。
        // 監査2026-07-11（他7）：Refreshは月次でも呼ばれる＝毎回/3で割ると教育費・専売が実際の約1/3表示・外資は3ヶ月のうち2ヶ月£0点滅だった→
        // 四半期境界を跨いだ時だけ差分を取り月平均を更新（境界間は前回値を保持）。
        int qStamp = _player.Year * Calendar.QuartersPerYear + _player.Quarter;
        if (qStamp != _prevQuarterStamp)
        {
            double eduCum = 0, stateMonopCum = 0;
            foreach (var rg0 in _player.Regions) { eduCum += rg0.CumEducationCost; stateMonopCum += rg0.CumStateMonopolyRevenue; }
            double fNet = _player.ForeignCapitalIn - _player.CapitalAbroad;   // 外資の純フロー：正＝流入（収入側）・負＝流出（支出側）。片方は£0
            if (_prevQuarterStamp >= 0)
            {
                _eduMonthly = System.Math.Max(0, eduCum - _prevEduCum) / (double)Calendar.MonthsPerQuarter;
                _stateMonopMonthly = System.Math.Max(0, stateMonopCum - _prevStateMonopCum) / (double)Calendar.MonthsPerQuarter;
                double fFlow = (fNet - _prevForeignNet) / (double)Calendar.MonthsPerQuarter;
                _fInMonthly = System.Math.Max(0, fFlow);
                _fOutMonthly = System.Math.Max(0, -fFlow);
            }
            _prevEduCum = eduCum; _prevStateMonopCum = stateMonopCum; _prevForeignNet = fNet;
            _prevQuarterStamp = qStamp;
        }

        UpdateForecast();   // 予算予想（支出/収入・実効%・予想額）＝毎tickも更新されるがRefresh時にも即時反映

        // POP＝上流（貴族・聖職・資本家）／中流（専門職・官僚・職人・商人）／下流（労働者・農民・兵士・使用人 他）の3層で表示。
        // 全州のpopを ClassId で集約（国民全体）。州別の人数はツールチップに内訳表示（パリ:n人 リール:n人…）。数値はすべて「:」で繋ぐ。
        var popsUpper = new List<PopRow>(); var popsMiddle = new List<PopRow>(); var popsLower = new List<PopRow>();   // 据え置き同期（ホバー中のツールチップを毎月壊さない）
        var order = new List<int>();
        var agg = new Dictionary<int, List<(Region Rg, Pop P)>>();
        foreach (var rg in _player.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            foreach (var p in rg.Pops)
            {
                if (!agg.TryGetValue(p.ClassId, out var l)) { agg[p.ClassId] = l = new(); order.Add(p.ClassId); }
                l.Add((rg, p));
            }
        }
        bool foodShort = _player.Regions.Any(r => r.State == RegionState.Active && r.FoodFill < FillWarnThreshold);
        double taxBurden = _player.TaxRate + _player.PollTax * 0.5;
        // ★増減の矢印（作者指示2026-08-16(2)(3)）：四半期境界でのみ規模/失業者の差分を確定（状態は GameViewModel.Pop.cs）。初回は基準化のみ＝矢印なし
        int popQStamp = _player.Year * Calendar.QuartersPerYear + _player.Quarter;
        bool popQBoundary = popQStamp != _prevPopQuarterStamp;
        foreach (int cid in order)
        {
            var members = agg[cid];
            var first = members[0].P;
            double size = 0, sav = 0, earned = 0, satW = 0, milW = 0, litW = 0, femW = 0, childW = 0, oldW = 0, unempW = 0;
            double leanC = 0, leanL = 0, leanS = 0;   // 政治意識のある人数の族別内訳（千人・作者指示2026-08-16(1)）＝残りが「なし」
            double mLiving = 0, mVoice = 0, mWar = 0, mNation = 0, mFamine = 0;   // ★段2＝闘争度の5項（生活の失望/法の隔たり/厭戦/民族/飢餓）
            double debt = 0;   // ★2026-08-02：個人債務も集計（作者指示「POPに貯蓄と合わせて借金も表示」）
            // ★満足度セルのホバー（作者指示2026-08-15(2)）：主食と主食以外の充足。需要量はpop規模に比例＝量の総和がそのままsize加重
            double fMet = 0, fNeed = 0, nMet = 0, nTot = 0;
            foreach (var (rg, p) in members)
            {
                size += p.Size; sav += p.Savings; earned += p.Earned; debt += p.Debt;
                fMet += p.FoodMet; fNeed += p.FoodNeed; nMet += p.NeedMet; nTot += p.NeedTotal;
                satW += p.Satisfaction * p.Size; milW += p.Militancy * p.Size; litW += p.Literacy * p.Size;
                femW += p.FemaleShare * p.Size; childW += p.ChildShare * p.Size; oldW += p.OldShare * p.Size; unempW += p.UnemploymentRate * p.Size;
                mLiving += p.MilLiving * p.Size; mVoice += p.MilVoice * p.Size; mWar += p.MilWar * p.Size;
                mNation += p.MilNation * p.Size; mFamine += p.MilFamine * p.Size;   // ★段2で失業/群衆/格差/中間/革命の波は項ごと削除（設計メモ §7-3）
                // ★4区分の畳み込み（作者指示2026-08-16(1)）＝政治意識のある人数を族比で割る（PopIdeologyHeads・GameViewModel.Pop.cs）。
                //   単位は人数（千人）＝ここで足し込んだ3族と「なし」の合計がその階級の規模に一致する。
                var (headC, headL, headS) = PopIdeologyHeads(p);
                leanC += headC; leanL += headL; leanS += headS;
            }
            double inv = size > 1e-9 ? 1.0 / size : 0;
            double sat = satW * inv, mil = milW * inv, litAvg = litW * inv, fem = femW * inv, child = childW * inv, old = oldW * inv, unemp = unempW * inv;
            double perCap = size > 1e-9 ? sav / size : 0;
            // ★満足度セルのホバー（作者指示2026-08-15(2)）：主食＝FoodMet/FoodNeed・主食以外＝NeedMet/NeedTotal。
            //   エンジンと同じくクランプ（EconomyStep の Satisfaction=Clamp(NeedMet/NeedTotal) と同族）。
            //   ⚠アヘン・インフラ・福祉等の後段の加減点は診断フィールドが無い＝今回は出さない（将来課題）
            double foodFill = fNeed > 1e-9 ? System.Math.Clamp(fMet / fNeed, 0, 1) : 1.0;
            // ★階層3分割（作者裁定2026-09-20・2026-09-22 既定化）＝満足度の台帳（NeedMet/NeedTotal）には食料が載っていない＝台帳そのものが「主食以外」。
            //   ⚠旧の引き算（nTot−fNeed／nMet−fMet）は分子と分母で別の規則（重み付き台帳−重みなしの食料）だった＝分割後に引くと分子が負になる。
            double othNeed = nTot, othMet = nMet;
            double othFill = othNeed > 1e-9 ? System.Math.Clamp(othMet / othNeed, 0, 1) : 1.0;
            string satTip = $"主食の充足:{foodFill:P0}\n主食以外の充足:{othFill:P0}";
            // ★増減の向き（作者指示2026-08-16(2)(3)）：四半期境界でのみ確定（StampPopDelta・GameViewModel.Pop.cs）＝規模と失業者で同じ物差し。
            double unempK = size * unemp;   // 失業者数（千人）
            var (sizeArrow, sizeArrowBrush) = PopTrend(StampPopDelta(_prevPopSize, _popSizeDelta, cid, size, popQBoundary));
            var (unempArrow, unempArrowBrush) = PopTrend(StampPopDelta(_prevPopUnemp, _popUnempDelta, cid, unempK, popQBoundary));
            string party = first.Closed ? "保守党" : first.IsModernElite ? "自由党" : "社会党";   // 支持政党＝利益ブロック
            bool parliamentActive = HasParliament;   // 項9（2026-07-26）：議会（議席）が無い段階では支持政党を出さない（ParliamentTextと同じ判定＝HasParliament。政体条件込み・2026-08-04）
            // 所得・貯蓄 → 州別の人数(失業込み)を1州1行 → 人口構成 → 失業率/支持政党（すべて「:」区切り）
            string tip = first.ClassName
                       + $"\n所得:{Money(earned)}　貯蓄:£{perCap:F1}　借金:£{(size > 1e-9 ? debt / size : 0):F1}";   // ★借金＝1人当たり（貯蓄と同じ物差し・2026-08-02）
            foreach (var (rg, p) in members.Where(m => m.P.Size > 0.5).OrderByDescending(m => m.P.Size))
            {
                double un = p.Size * p.UnemploymentRate;
                tip += $"\n{rg.Name}:{PopFmtK(p.Size)}" + (un > 0.5 ? $"(失業{PopFmtK(un)})" : "");   // 内部単位=千人（他10）。POPタブはk統一（項10）
            }
            tip += $"\n女性:{fem:P0}　子供:{child:P0}　老年:{old:P0}"
                 + $"\n失業率:{unemp:P0}" + (parliamentActive ? $"　支持政党:{party}" : "");
            // 闘争度の要因内訳：全国size加重の各寄与ぶん（多い順・0.5%未満は省略）。生活水準には操作可能な主因をヒント
            var milb = new List<(string Label, double V)>
            {
                ("生活水準の不足", mLiving * inv), ("政治的疎外", mVoice * inv), ("厭戦", mWar * inv),
                ("民族的地位", mNation * inv), ("飢餓", mFamine * inv),
            };
            var causes = new List<string>();
            if (foodShort) causes.Add("主食不足");
            if (!first.Closed && !first.IsSoldier && taxBurden > 0.30) causes.Add("重税");
            if (!first.Closed && !first.IsModernElite && _player.WorkHoursValue >= 12 && first.IsUrban) causes.Add("長時間労働");
            var shown = milb.Where(x => x.V > 0.005).OrderByDescending(x => x.V).ToList();
            // ★闘争度の要因は闘争度セルのホバーへ移動（作者指示2026-08-15(2)）＝階級名セルの tip からは出さない。
            //   1本も無い（全要因0.5%以下）なら null＝ツールチップ自体を出さない（空文字だと空の吹き出し）
            string? milTipText = null;
            foreach (var (label, v) in shown)
            {
                string extra = label == "生活水準の不足" && causes.Count > 0 ? $"({string.Join("、", causes)})" : "";
                milTipText = (milTipText is null ? "" : milTipText + "\n") + $"・{label}:+{v:P0}{extra}";
            }
            // ★4区分（作者指示2026-08-16(1)）：なし＝規模−（保守+自由+社会）＝Σ size×(1−Consciousness)。差で取る＝4列の合計は常に規模と一致する。
            double leanNone = System.Math.Max(0, size - (leanC + leanL + leanS));
            var privChips = PopPrivilegeChips(cid);   // ★特権の1行（作者指示2026-09-07(7)(8)）＝法・政体・工業化から導く観測量。先頭が「なし」
            var row = new PopRow
            {
                Class = first.ClassName,
                ClassId = cid,
                // ★規模と失業者は別の列（作者フィードバック2026-08-10）。POPタブはk統一（項10）。
                //   規模は全国人口比を併記「5,430K(12%)」（作者指示2026-08-15(3)・分母=Refresh冒頭のpopTotal）＋増減の矢印（2026-08-16(2)）
                Size = popTotal > 1e-9 ? PopFmtK(size) + $"({size / popTotal:P0})" : PopFmtK(size),
                SizeArrow = sizeArrow,
                SizeArrowBrush = sizeArrowBrush,
                // ★失業者も規模と同型「543K(10%)↑」（作者指示2026-08-16(3)）＝()内はこの階級の失業率。丸めて0%の階級は「—」（矢印も出さない）
                Unemployed = unemp > 0.005 ? PopFmtK(unempK) + $"({unemp:P0})" : "—",
                UnemployedArrow = unemp > 0.005 ? unempArrow : "",
                UnemployedArrowBrush = unempArrowBrush,
                Satisfaction = sat.ToString("P0"),
                Militancy = mil.ToString("P0"),
                Literacy = litAvg.ToString("P0"),
                // ★なし/保守/自由/社会の4区分（作者指示2026-08-16(1)・旧「政治意識」列は「なし」の裏返しなので撤去）＝合計100%。
                //   意識は識字×教育法×都市×報道で伸びる遅い変数（時定数約17年）＝「なし」が減る速さがそのままレバーの効き。
                LeanNone = (leanNone * inv).ToString("P0"),
                // ★族ごとに1列（作者フィードバック2026-08-10）＝縦に並ぶので階級間で同じ族を比較できる。
                LeanConservative = (leanC * inv).ToString("P0"),
                LeanLiberal = (leanL * inv).ToString("P0"),
                LeanSocialist = (leanS * inv).ToString("P0"),
                Tip = tip,
                SatTip = satTip,
                MilTip = milTipText,
            };
            foreach (var ch in privChips) row.Privileges.Add(ch);
            bool upper = first.Closed || first.ClassName == CapitalistClass;
            bool middle = !upper && (first.IsModernElite || first.ClassName is BureaucratClass or ArtisanClass or MerchantClass);
            (upper ? popsUpper : middle ? popsMiddle : popsLower).Add(row);
        }
        if (popQBoundary) _prevPopQuarterStamp = popQStamp;   // 全階級の基準化/確定が済んでからスタンプを進める
        SyncRows(PopsUpper, popsUpper, r => r.Class, (t, s) => t.CopyFrom(s));
        SyncRows(PopsMiddle, popsMiddle, r => r.Class, (t, s) => t.CopyFrom(s));
        SyncRows(PopsLower, popsLower, r => r.Class, (t, s) => t.CopyFrom(s));

        BuildComposition();   // 文化・宗教の構成（#62 表示レイヤー）
        BuildParliament();    // 議会の実体（議席・得票・支持基盤・2026-08-04）

        // 利益団体（旧支配層/近代エリート/大衆の政治勢力＝富×参政権。POP由来＝政治の主役・革命の駆動）
        var (trad, modern, mass) = InterestGroups.Blocs(_player);
        double bsum = trad + modern + mass; if (bsum < 1e-9) bsum = 1;
        Blocs.Clear();
        // ★力の源泉ホバー（2026-08-04）：旧支配層の力＝流動資産＋土地（地代の資本化）。何が力を支えているかの分解（データのみ）
        double nobSav = 0, nobLand = 0;
        foreach (var pp in _player.AllPops())
            if (pp.Closed) { nobSav += System.Math.Max(0, pp.Savings); nobLand += _player.Coef.LandedPowerCapitalization * System.Math.Max(0, pp.LandRent); }
        double nobTot = nobSav + nobLand;
        // ★括弧書きの内訳（貴族と聖職／ブルジョワと専門職／労働者と農民）は削除（2026-08-23作者指示）
        Blocs.Add(new BlocRow { Name = "旧支配層", Percent = (trad / bsum).ToString("P0"),
            Tip = nobTot > 1e-9 ? $"力の源泉　流動資産 {nobSav / nobTot:P0}/土地(地代の資本化) {nobLand / nobTot:P0}" : "" });
        Blocs.Add(new BlocRow { Name = "近代エリート", Percent = (modern / bsum).ToString("P0") });
        Blocs.Add(new BlocRow { Name = "大衆", Percent = (mass / bsum).ToString("P0") });

        // 政体別実績ストック（信認⇔失望∈[-1,+1]・4系統）＝固着⇔カオスのドライバ。正=緑/負=赤のバーで符号を色分け。
        RegimeRecords.Clear();
        // ★系統2の表示名を「強権」→「独裁制」へ（2026-08-23作者指摘）。実体は CrownParliament.RegimeFamily の系統2＝軍事独裁とブルジョワ独裁。
        //   撤去済みのヘッダー指標「強権」（MandateSpent）と同語だったため、そちらの残骸に見えていた。
        string[] regimeNames = { "君主制", "議会制", "独裁制", "急進" };
        for (int ri = 0; ri < 4 && ri < _player.RegimeTrackRecord.Length; ri++)
        {
            double v = _player.RegimeTrackRecord[ri];
            RegimeRecords.Add(new RegimeRow
            {
                Name = regimeNames[ri],
                Value = $"{(v >= 0 ? "＋" : "−")}{Math.Abs(v):P0}",
                BarWidth = Math.Min(1.0, Math.Abs(v)) * 130,
                Brush = v >= 0 ? TermPlus : TermMinus,
            });
        }

        // 予算レバーの現在値を通知（ロード時/再描画時）
        OnPropertyChanged(nameof(ArmyUpkeepLine)); OnPropertyChanged(nameof(NavyUpkeepLine));
        // ⚠撤去2026-09-23：NavalShare/NavalShareLine への通知＝比のレバーごと廃止（地理が置く）。陸海それぞれの**金額**は上の2行が名乗る。
        OnPropertyChanged(nameof(Tariff));
        RefreshLaws();   // 法律タブの段階ボタン行（E8）＋財政の段（R1/R2）：現行段階・改正可否・政治資本コストを毎期更新
        UpdateLawAlert();     // 法律タブの通知＝改革の一歩が「通らない→通る」へ変わったら点灯
        OnPropertyChanged(nameof(PolityText));
        OnPropertyChanged(nameof(CrownText)); OnPropertyChanged(nameof(ParliamentControlText));
        OnPropertyChanged(nameof(CoopParliamentText));
        OnPropertyChanged(nameof(ArmyAllegianceText));   // 王権と議会の観測軸（政治タブ）。政治資本(MandateSpent)・世論の王権支持率はヘッダーへ移動
        OnPropertyChanged(nameof(PolityAttractorText)); OnPropertyChanged(nameof(PolityAttractorTip)); OnPropertyChanged(nameof(ChallengeText)); OnPropertyChanged(nameof(ChallengeTip)); OnPropertyChanged(nameof(ChallengeBrush));   // 反体制派の強さ比は戦争タブ（色は国内勢力行と同一規則）。分離リスクは撤去2026-08-16＝国内勢力の一覧が同じことを行ごとに出す
        OnPropertyChanged(nameof(PolityEconControlText)); OnPropertyChanged(nameof(PolityBondDiscountText));
        OnPropertyChanged(nameof(PolityMobilizationText)); OnPropertyChanged(nameof(PolityRollbackText));   // 政体パネル（2026-07-24）。総動員時の兵士割合
        OnPropertyChanged(nameof(ParliamentText));
        OnPropertyChanged(nameof(PriceLevelText)); OnPropertyChanged(nameof(ExchangeRateText));
        OnPropertyChanged(nameof(MobilizeButtonText)); OnPropertyChanged(nameof(CoverVisible));
        OnPropertyChanged(nameof(LoanRateVisible)); OnPropertyChanged(nameof(HouseholdLoanVisible));   // 金融の条件付きの2行（2026-09-26）
        OnPropertyChanged(nameof(InterestLever));
        OnPropertyChanged(nameof(InterestLeverEnabled));   // ★トリレンマ（2026-08-13）：兌換維持＋資本自由へ入った/出た瞬間に掴める/掴めないが切り替わる
        OnPropertyChanged(nameof(ModernizationInvest));
        // 法律の現行値/支持団体の8本への通知は撤去（2026-08-10）＝2026-08-02の「議会立法」ブロック撤去で購読者が0件になっていた
        // （現行段は法律一覧の濃塗り・賛否は段ボタンのホバーが持つ）。行そのものの追従は上の RefreshLaws が担う。
        // ⚠撤去2026-09-12/2026-09-20: 社会保障・税・教育費のスライダーへの通知＝段ボタンへ一本化して購読者が0件（段ボタンの追従は上の RefreshLaws）。
        OnPropertyChanged(nameof(AdminSpending));
        // 制度（法律タブ）：解禁は四半期判定＝状態行を毎期読み直す
        OnPropertyChanged(nameof(Institutions));   // 制度3行（施行/未施行）＝法律と同じ形（2026-08-11）

        if (ProdOpen) RefreshProduction();   // 生産タブが開いていれば四半期ごとに統計を追従
        if (DiscoveryOpen) RefreshDiscovery();   // 発見パネル（F）も同じ追従
        if (TabWar) RefreshFactions();   // 戦争タブが開いていれば国内勢力を四半期ごとに追従（2026-07-20移設・国内勢力は戦争タブが正位置）
        // 巨大建造物は生産タブの左下（2026-08-10移設）＝上の RefreshProduction が BuildMegaRows を含む＝ここで別に組まない
    }
}
