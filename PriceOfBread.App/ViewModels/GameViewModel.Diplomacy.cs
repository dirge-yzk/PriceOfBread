// GameViewModel.Diplomacy.cs ＝ 外交と戦争のページ＝国の一覧（共通の表）・カメラ移動・侵略/統一戦争の確認モーダル。
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
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

/// <summary>国の一覧の色（旧の外交タブと同じ固定のパレット・「#RRGGBB」）。関係のバッジの背景・政体の色の丸・緊張の青／黄／赤。表の側（TableView）が文字列を筆へ直す。</summary>
internal static class NationInk
{
    public const string War = "#A0432D", Ally = "#2E7D32", OwnVassal = "#3A5A8A", OwnSuzerain = "#8A6A2A", OtherVassal = "#7A6A55", NoTerritory = "#8A8070", Default = "#555555";

    /// <summary>緊張の色（開戦までの近さ。低＝青・中＝黄・高＝赤）。</summary>
    public static string Tension(double share) => !(share >= 0.35) ? "#1F5FA8" : share < 0.70 ? "#B8860B" : "#A0432D";   // 否定形＝NaN は低い側

    /// <summary>政体の色（導出した政体。立憲君主制は自国の属国と、ブルジョア独裁は自国の宗主と同じ色）。</summary>
    public static string PolityInk(Polity? p) => p switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => "#7A3030",
        Polity.ConstitutionalMonarchy => OwnVassal,
        Polity.Republic => "#2E6E5E",
        Polity.MilitaryDictatorship => "#5A3A6A",
        Polity.BourgeoisDictatorship => OwnSuzerain,
        Polity.ProletarianDictatorship => "#E0402A",   // 絶対君主制の暗い赤と見分けられる明るい赤（V10）
        _ => Default,
    };
}

public partial class GameViewModel
{
    // 国の一覧は共通の表（Controls/TableView）。行は国（鍵＝国名）で、ランクごとの群・順位・国名・関係・緊張・動詞（宣戦布告。統一戦争ができる相手は押し釦の色を変える）。
    // 列は下の NationColumns が名乗る。行は鍵で値だけ流す（SyncRows の規律＝押し釦とホバーを毎月壊さない）。
    // ★国の行の組み立ては App の状態（確かめの窓・狙い先・地図のカメラ）に依るので、本体の目録（PoliticsTables）ではなくここで TableData を組む。
    [ObservableProperty] private TableSnapshot _nationTable = TableSnapshot.Empty;

    /// <summary>国名の前の丸の色の凡例（政体の色の組＝<see cref="NationInk.PolityInk"/>）。絶対君主制・専制・幕府は同じ色。</summary>
    public static IReadOnlyList<LegendItem> PolityLegend { get; } =
    [
        new("絶対君主制・専制・幕府", Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.Absolutist))),
        new(PlayText.PolityRegimeJa(Polity.ConstitutionalMonarchy), Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.ConstitutionalMonarchy))),
        new(PlayText.PolityRegimeJa(Polity.Republic), Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.Republic))),
        new(PlayText.PolityRegimeJa(Polity.MilitaryDictatorship), Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.MilitaryDictatorship))),
        new(PlayText.PolityRegimeJa(Polity.BourgeoisDictatorship), Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.BourgeoisDictatorship))),
        new(PlayText.PolityRegimeJa(Polity.ProletarianDictatorship), Avalonia.Media.Brush.Parse(NationInk.PolityInk(Polity.ProletarianDictatorship))),
    ];

    /// <summary>統一戦争ができる相手の宣戦布告の押し釦の地の色（金＝統一の機運）。</summary>
    private const string UnificationFill = "#A8862B";

    private static readonly ColumnInfo[] NationColumns =
    [
        new("order", "順位", ColumnKind.Number, Unit.Count, false, null),
        new("name", "国名", ColumnKind.Text, null, false, null),
        new("relation", "関係", ColumnKind.Text, null, false, null),
        new("tension", "緊張", ColumnKind.Number, Unit.Share, false, null),
        new("declare", "宣戦布告", ColumnKind.Verb, null, false, null),
    ];

    /// <summary>外交と戦争のページが見えた瞬間の更新（見えている間は毎月の Refresh が追従する）。国の並びに要る物（国力・ランク・戦争の相手・統一戦争の標的）をここで作り直す。</summary>
    public void RefreshDiplomacyPage()
    {
        var scoreByPower = NationalScore.ScoresByPower(_sim.Diplomacy);
        var linesByPower = NationalScore.BreakdownsByPower(_sim.Diplomacy);
        var powers = _sim.Diplomacy.Powers
            .OrderByDescending(p => (int)_sim.Diplomacy.RankOf(p))
            .ThenByDescending(p => p.Nation != null ? scoreByPower[p.Id] : -1).ToList();
        var me = _sim.Diplomacy.Powers[_playerIndex];
        var warEnemyIds = new HashSet<long>();
        foreach (var war0 in _sim.Diplomacy.Wars) if (war0.A == me || war0.B == me) warEnemyIds.Add((war0.A == me ? war0.B : war0.A).Id);
        var unifRule = _player.UnificationRule;
        var unifTargets = unifRule != null ? Formations.UnificationTargets(_sim.Diplomacy, me, unifRule) : null;
        BuildNationTable(powers, scoreByPower, linesByPower, me, warEnemyIds, unifTargets);
        RefreshFactions();
    }

    /// <summary>国の一覧の表を組む。並び＝ランクが第一キー（作者指摘2026-08-11「ランクと順位が一致しない」）：一覧は今の国力で並べ、ランクは年 1 回の再分類の結果なので、
    /// その間に国力が入れ替わると「中堅なのに列強より上」が起こる。ランクで束ねてから国力で並べれば定義上ずれない（群＝ランク）。</summary>
    private void BuildNationTable(List<Power> powers, Dictionary<int, double> scoreByPower, Dictionary<int, NationalScore.ScoreLine[]> linesByPower,
                                  Power me, HashSet<long> warEnemyIds, List<Power>? unifTargets)
    {
        var rows = new List<RowData>(powers.Count);
        for (int oi = 0; oi < powers.Count; oi++)
        {
            var pow = powers[oi];
            bool isMe = pow == me;
            bool war = warEnemyIds.Contains(pow.Id);
            var powRank = _sim.Diplomacy.RankOf(pow);
            string rank = RankJa(powRank);   // 国家ランク（列強/中堅/弱小・改修2026-06-28）
            // 導出政体（2026-08-21・作者裁定「権力円グラフから政体を算出し、外交パネルでの表示のみに使う」）：
            // ラベル（w.Polity）でなく PowerAffinity.PolityFromShares（権力シェアの読み出し）を表示する。
            Polity? derived = pow.Nation is { } dnw ? PowerAffinity.PolityFromShares(dnw) : (Polity?)null;
            string name = SecessionNames.DisplayName(pow, derived, _sim.Diplomacy.Wars);   // 政体で変わる国号・政体をめぐる内戦の間は両陣営が頭書き＋国名
            string score = pow.Nation != null ? scoreByPower[pow.Id].ToString("F1") : "—";
            string polity = PolityRegimeJa(derived);
            // 低い＝多民族/多宗派（移民を引く・分離圧力）。宗教統一は民族統一と対称の派生スカラー＝州宗教の最大シェア（#62で露出）
            string unity = pow.Nation is { EthnicEnabled: true } nn ? $"民族統一 {Fmt.Share(nn.EthnicUnity)}　宗教統一 {Fmt.Share(nn.ReligiousUnity)}" : "";
            // 緊張：自国は「—」、それ以外は開戦までの近さ（100%＝宣戦の閾値・TensionPct）。交戦は関係の列で示す。
            double tensionShare = isMe ? double.NaN : TensionPct(_sim.Diplomacy.Tension(me, pow));
            // 関係（優先順）：戦争中＞属国/宗主（自国）＞他国の属国＞同盟＞領土なし（帰属の明記・2026-07-19）
            var powSuz = _sim.Diplomacy.Suzerain(pow);
            string badge = ""; string? badgeFill = null;
            if (isMe) { }
            else if (war) { badge = "戦争中"; badgeFill = NationInk.War; }
            else if (powSuz == me) { badge = "自国の属国"; badgeFill = NationInk.OwnVassal; }
            else if (_sim.Diplomacy.Suzerain(me) == pow) { badge = "自国の宗主"; badgeFill = NationInk.OwnSuzerain; }
            else if (powSuz is not null) { badge = $"{PowerLabel.Of(powSuz, _sim.Diplomacy.Wars)}の属国"; badgeFill = NationInk.OtherVassal; }   // 国名の列と同じ表（国号）から＝「マラーター帝国」と「マラーター王国」が食い違わない（B45）   // 宗主名だけ（長い「○○の属国」は末尾が落ちて別の語になる）。属国であることは色が担い、全文は行のホバーの「関係:」
            else if (_sim.Diplomacy.HasAlliance(me, pow)) { badge = "同盟"; badgeFill = NationInk.Ally; }
            else if (pow.Nation is null && pow.ColonyList.Count == 0) { badge = "領土なし"; badgeFill = NationInk.NoTerritory; }   // 全て接収済みの抽象標的（侵略しても空振り＝ボタンは不可理由つきで灰色）
            // 外交アクションの可否：弱小への侵略（実国家間の開戦＝緊張の創発のみ）。
            // ★2026-08-23作者裁定：植民地化と侵略は1本（侵略）。標的が弱小なら抽象標的でも実在国でも同じボタン＝エンジンの World.CanInvade にもランク条件は無い（AI と同じ動詞・同じ可否）。
            bool showInvade = !isMe && powRank == PowerRank.Minor;   // 宣戦布告の釦は弱小の行だけ（弱小でない行は釦も理由も出さない＝19 行の同じ理由が並ばない・B47 の直し直し）
            string invReason = "";
            bool canInvade = showInvade && _player.CanInvade(pow, out invReason);
            if (!showInvade || canInvade) invReason = "";
            bool showUnif = unifTargets != null && unifTargets.Contains(pow);   // P4：必要州の保有国にのみ表示
            string unifReason = "";
            bool canUnif = showUnif && _player.CanUnificationWar(pow, out unifReason);
            if (!showUnif || canUnif) unifReason = "";
            // 不可の理由（グレーアウトした動詞の下に常に出す短い名詞句）。閾値の数は出さない（作者裁定 2026-09-23）。case の文字列はエンジンの理由（World.CanInvade）＝変えない
            string? ReasonTip(string reason) => reason switch
            {
                "" => null,
                "余剰戦力が不足" => "戦力余剰が不足",   // 語を「戦力余剰」へ統一（2026-09-23）
                _ => Fmt.Zen(reason),   // 括弧とコロンは全角（V11）
            };

            // ホバー詳細：スコアの内訳（NationalScore.Breakdowns の 1 本＝6 項の割合の点）＋緊張が高まっている要因。
            string detail = $"{name}({rank})　{polity}";
            if (pow.Nation is { } nw)
            {
                detail += $"\n列強スコア：{score} " + string.Join(" ", linesByPower[pow.Id].Select(l => $"{l.Label}：{l.Points:F1}"));
                if (unity.Length > 0) detail += "\n" + unity;
                // 主流の文化/宗教（州ラベルの人口加重多数派）＝下の「民族の隔たり/宗教の隔たり」の素（#62）
                if (pow.Culture.Length > 0 || pow.Religion.Length > 0) detail += $"\n文化：{pow.Culture}　宗教：{pow.Religion}";
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
                            detail += $"\n・{f.Name} +{Fmt.Share(TensionPct(f.Value))}";
                    }
                }
                // 自国との関係（同盟・禁輸・属国/宗主）＝意思決定情報
                var rel = new List<string>();
                if (_sim.Diplomacy.HasAlliance(me, pow)) rel.Add("防衛同盟");
                if (_sim.Diplomacy.HasEmbargo(me, pow)) rel.Add("禁輸中");
                if (powSuz == me) rel.Add("自国の属国");
                else if (powSuz is not null) rel.Add($"{PowerLabel.Of(powSuz, _sim.Diplomacy.Wars)}の属国");   // 帰属の明記（2026-07-19）：他国の勢力圏もホバーで判る
                if (_sim.Diplomacy.Suzerain(me) == pow) rel.Add("自国の宗主");
                if (rel.Count > 0) detail += "\n関係：" + string.Join("　", rel);
                // 経済データ行（外国経済の可視化doc §3-3・2026-07-24）：19世紀でも新聞で公知のマクロ量のみ（帳簿の全公開はしない）
                if (pow.Nation is { } enw)
                {
                    detail += $"\n経済：GDP {Fmt.Money(enw.NominalGdpPerMonth.Value)}/月　工業化 {enw.MeanIndustrialization:F1}　物価 {enw.PriceLevel:F2}"
                            + $"\n通貨制度 {MonetaryLawName(enw.MonetaryLaw)}　恐慌 {(SelfPlayMatch.IsGreatDepression(enw.GdpPcDrawdown.Value) ? "○" : "×")}　関税率 {Fmt.Share(enw.Tariff)}";   // 「恐慌」の閾値は通知・年代記と同じ 0.25（2026-08-23）
                }
            }
            else detail += $"\n残る領土 {pow.ColonyList.Count}";   // 抽象標的＝未接収の植民地数（0=領土なし・奪う物が無い）
            // 通商条約の行は廃止（2026-09-07作者指示）＝締結は Diplomacy.TradeTreaties が四半期に自動で行う（プレイヤーもAIと同じ掃引）

            // 動詞：押すと確かめの窓（既存の DiploInvade／DiploUnifWar）が開く＝窓の中で実行を選ぶ（窓の本文は押した瞬間の値で組む）。この行に無い動詞は空のまま（前の月にあった釦は消える）。
            string powName = pow.Name;
            // 宣戦布告は 1 本（2026-11-11 作者指示）：統一戦争ができる相手は同じ押し釦の地の色を変え、押すと統一戦争の確かめの窓へ
            object? declare = showUnif && canUnif ? new VerbCell("宣戦布告", true, () => DiploUnifWarCommand.Execute(powName), null, null, UnificationFill)
                            : showInvade ? new VerbCell("宣戦布告", canInvade, () => DiploInvadeCommand.Execute(powName), null, ReasonTip(invReason))
                            : showUnif ? new VerbCell("宣戦布告", false, () => DiploUnifWarCommand.Execute(powName), null, ReasonTip(unifReason))
                            : null;
            rows.Add(new RowData(pow.Name, name, rank,
                [new NumberCell(oi + 1), new TextCell(name, null, Swatch: NationInk.PolityInk(derived)), new TextCell(badge, null, BadgeFill: badgeFill),
                 new NumberCell(tensionShare, Ink: isMe ? null : NationInk.Tension(tensionShare)), declare], detail, isMe));
        }
        NationTable = TableSnapshot.From(new TableData("diplomacy.nations", "国の一覧", NationColumns, rows, Page.DiplomacyWar));
    }

    /// <summary>通貨制度の名（法の表の通貨制度の段の名と同じ語）。兌換の維持の旗（OnGoldStandard）は金属の種類を見ない＝銀本位の国に金本位と出さないため、法の値で出す。</summary>
    private static string MonetaryLawName(MonetaryStandardLaw law) => law switch { MonetaryStandardLaw.Gold => "金本位", MonetaryStandardLaw.Silver => "銀本位", _ => "不換紙幣" };

    /// <summary>行クリック＝その国の中心（首都州／なければ最初の植民地）へカメラを移動（View が購読して translate を合わせる）。</summary>
    public event Action<double, double>? CenterMap;

    /// <summary>自国首都の仮想キャンバス座標（地図の初期センタリング用）。座標未登録なら null。</summary>
    public (double X, double Y)? PlayerCapitalCanvas()
        => MapData.CityCoords.TryGetValue(_player.Region.Name, out var c) ? MapData.Project(c.Lat, c.Lon) : null;

    [RelayCommand]
    private void CenterNation(string? name)
    {
        if (FindPower(name) is not { } t) return;
        (double Lat, double Lon)? co = null;
        if (t.Nation is { } w && MapData.CityCoords.TryGetValue(w.Region.Name, out var c0)) co = c0;   // 首都州の座標
        if (co is null)
            foreach (var col in t.ColonyList)
                if (System.Math.Abs(col.Latitude) > 1e-9 || System.Math.Abs(col.Longitude) > 1e-9) { co = (col.Latitude, col.Longitude); break; }
        if (co is null) return;
        var (x, y) = MapData.Project(co.Value.Lat, co.Value.Lon);
        CenterMap?.Invoke(x, y);
    }

    // 死コード掃除（2026-08-10）：DiploAlly（防衛同盟の確認モーダル）を削除＝2026-08-01の同盟自動化で廃止済みの残骸。
    // XAML に同盟ボタンは無く、行に埋めていた AllyCmd／可否 CanAlly／文言 AllyLabel も**どこからもバインドされていなかった**（一式を撤去）。
    // 同盟の締結は Diplomacy が自動で行い、UI は状態（国カードのバッジ「同盟」）だけを映す。
    // 禁輸ボタン（DiploEmbargo）は廃止（禁制品doc C1・2026-07-30）。禁輸は挑発に同梱され（AI も同じ動詞を押す）、戦時は強制・終戦で自動解除。
    // 状態としての表示（国カードの「禁輸中」・緊張要因の内訳）は残す＝データの表示であって操作ではない。

    // 確認モーダル（侵略・統一戦争・調停・ロード・タイトル復帰）
    [ObservableProperty] private bool _confirmOpen;
    // 表示中は時間停止（ConfirmBody/可否判定が開いた瞬間の値のまま陳腐化しない）。閉じても自動再開はしない。
    partial void OnConfirmOpenChanged(bool value) { if (value) PopupPause(); else PopupResume(); }   // ★2026-08-24：他のポップアップと同じ1本へ（旧＝止めるだけで**閉じても戻らない**）
    [ObservableProperty] private string _confirmTitle = "";
    [ObservableProperty] private string _confirmBody = "";
    [ObservableProperty] private string _confirmAcceptLabel = "";
    [ObservableProperty] private bool _confirmCan;
    [ObservableProperty] private string _expansionText = "";   // 遠征余力・主戦論（上部バーに常時表示）
    // ジニ係数のヘッダー枠（_giniHeaderText）は削除（2026-08-10）＝2026-08-02にヘッダー2行目（HeaderStats）へ移設して以来、
    // どこからもバインドされないまま毎 Refresh に空文字を書き込むだけの死コードだった。
    private int _diploLogSeen;                                  // 外交ログの既読数（新規分だけ事件ログへ）
    private Action? _confirmDo;

    [RelayCommand]
    private void ConfirmAccept()
    {
        _confirmDo?.Invoke();
        _confirmDo = null;
        ConfirmOpen = false;
        Refresh();
    }

    [RelayCommand]
    private void ConfirmCancel() { _confirmDo = null; ConfirmOpen = false; }

    /// <summary>共通の表の動詞（<see cref="PriceOfBread.SimCore.Display.VerbCell"/>）で確かめの文を持つ物の口。既存の確かめの窓に、題＝動詞の名前・本文＝確かめの文を出し、実行で動詞の中身を呼ぶ
    /// （設計 §6-3）。<see cref="Controls.TableView.DefaultConfirm"/> へ GameView が繋ぐ。</summary>
    public void ConfirmVerb(PriceOfBread.SimCore.Display.VerbCell vb)
    {
        ConfirmTitle = vb.Name;
        ConfirmBody = vb.ConfirmText ?? "";
        ConfirmAcceptLabel = vb.Name;
        ConfirmCan = vb.Enabled;
        _confirmDo = vb.Invoke;
        ConfirmOpen = true;
    }
}
