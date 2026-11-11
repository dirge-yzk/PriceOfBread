// GameViewModel.Trade.cs ＝ 市場・清算・貿易ページ＝財の値段の表・輸出入一覧・輸送サマリ・条約港・清算の線（輸出・輸入・収支の四半期の系列）。
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

// ===== 貿易タブ（Yキー・2026-07-24新設＝外国経済の可視化doc §3-6/§3-7）=====
// 第1部＝輸出一覧・輸入一覧（額の降順・世界シェア%/輸入依存度%つき）＋ヘッダ行（総額・収支・関税収入）。
// 第2部＝輸送サマリ（ヘッダから移設した鉄道/船舶＋海運の占有分解）。
// 全てデータ行のみ（UI恒久規律）。額は世界価格建て（決済実態と一致）・表示は月割（÷MonthsPerClearing＝関税予想 Fiscal.cs と同じ単位ヘルパー）。
// 貿易の行は本体の表（SimCore/Display/TradeTables）が渡し、共通の表（Controls/TableView）が描く（設計 §7-1 段1）。
// 作者指定2026-08-11＝材名／額(世界価格比)／依存度（輸出は世界シェア）／相手。★2026-09-23：量の列は畳んだ（量の情報は隣の列に相対形で残る）。
public sealed record TransportRow(string Label, string Value);
// ⚠撤去2026-08-23（読み手ゼロ）: LogisticsRow／LogisticsRows ＝州別物流表は axaml から外され、VM 側は宣言だけが残っていた（生成コードも無し）。

public partial class GameViewModel
{
    [ObservableProperty] private TableSnapshot _priceTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _exportTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _importTable = TableSnapshot.Empty;
    // 清算の線＝四半期ごとの輸出・輸入・収支（月割）。名前つきの系列の倉に貯め（セーブは倉が名前と値の組で往復する）、画面へは点列の写しを渡す。
    private const string ClearingExportKey = "clearing.export", ClearingImportKey = "clearing.import", ClearingBalanceKey = "clearing.balance";
    // 市場のページの 2 面（2026-11-11 作者指示）：財の値段／貿易（輸出＝左・輸入＝右・輸送力。収支は枠の外）
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MarketPricesView))] private bool _marketTradeView;
    public bool MarketPricesView => !MarketTradeView;
    [CommunityToolkit.Mvvm.Input.RelayCommand] private void SetMarketView(string? p) => MarketTradeView = p == "trade";
    [ObservableProperty] private string _transportUseText = "—";
    [ObservableProperty] private Avalonia.Media.IBrush _transportUseBrush = Avalonia.Media.Brushes.Black;
    [ObservableProperty] private string _tradeExportTotal = "—";
    [ObservableProperty] private string _tradeImportTotal = "—";
    [ObservableProperty] private string _tradeBalance = "—";
    public string TreatyPortLine { get; private set; } = "";   // ★条約港（2026-08-04）。空＝非表示
    public bool HasTreatyPorts => TreatyPortLine.Length > 0;
    [ObservableProperty] private List<TransportRow> _transportRows = new();

    /// <summary>清算の線に 1 点足す（見えていない間も途切れさせない）。完了した四半期の数に点が足りない間だけ足す＝同じ四半期に何度呼んでも 1 点・ロード後も二重にならない。
    /// 決済の前（輸出入の台帳が無い）は足さない。</summary>
    private void SampleClearingSeries()
    {
        var w = _player;
        if (w.ExportsByGood is null || w.ImportsByGood is null) return;
        int quarters = (int)_simDays / Calendar.DaysPerQuarter;
        var ex = _series.Of(ClearingExportKey); var im = _series.Of(ClearingImportKey); var ba = _series.Of(ClearingBalanceKey);
        if (!(ex.Count < quarters)) return;
        // この系列を持たないセーブ（段3 より前の版）を読んだ直後：過ぎた四半期は測っていない＝数でない値で埋める（線は非有限の点を飛ばす）。現在値を過去の四半期へ積んで平らな偽の線にしない
        if (ex.Count == 0 && quarters > 1)
            for (int q = 1; q < quarters; q++) { ex.Add(double.NaN); im.Add(double.NaN); ba.Add(double.NaN); }
        var (e, i) = TradeTables.Totals(w);
        double mpc = w.Coef.MonthsPerClearing;
        ex.Add(e / mpc); im.Add(i / mpc); ba.Add((e - i) / mpc);
    }

    private void RefreshTrade()
    {
        SampleClearingSeries();
        if (!MarketShown) return;
        var w = _player;
        PriceTable = TableSnapshot.From(MarketTables.Prices.Read(_sim, w));

        // ---- 第1部：輸出入一覧（§3-4テレメトリ＝国際決済の国別×財別額。決済前/単一国は空） ----
        // 行は本体の貿易の表が組む（額の降順・世界価格比・依存度/世界シェア・相手）。額は世界価格建て（決済実態と一致）・表示は月割（÷MonthsPerClearing）。
        ExportTable = TableSnapshot.From(TradeTables.Exports.Read(_sim, w));
        ImportTable = TableSnapshot.From(TradeTables.Imports.Read(_sim, w));
        var (expTotal, impTotal) = TradeTables.Totals(w);
        double mpcTot = w.Coef.MonthsPerClearing;
        TradeExportTotal = $"{Fmt.Money(expTotal / mpcTot)}/月";
        TradeImportTotal = $"{Fmt.Money(impTotal / mpcTot)}/月";
        double bal = (expTotal - impTotal) / mpcTot;
        TradeBalance = $"{(bal >= 0 ? "＋" : "−")}{Fmt.Money(Math.Abs(bal))}/月";
        // ★2026-08-15作者フィードバック(10)：関税行（財政タブと重複）・開放度行は撤去。収支は総額の下の大型表示へ（財政タブと同レイアウト）
        // ★条約港（2026-08-04・作者仕様(4)の可視化）：開かされた港の数と関税自主権の実効低下。0港なら行ごと出さない。
        TreatyPortLine = Politics.ForeignPortShare(w) > 0   // setter は private プロパティへ直接・★(402) 関税が目減りしている間は出す（旧＝UnequalTreatyInForce の半分の線で 0〜0.25 の持分が見えなかった・反証役 rs〔bv〕低#1）
            ? $"条約港　外国持分 {Fmt.Share(Politics.ForeignPortShare(w))}　関税の実効 ×{InternationalMarket.TreatyPortTariffScale(w):F2}"
            : "";
        OnPropertyChanged(nameof(TreatyPortLine)); OnPropertyChanged(nameof(HasTreatyPorts));

        // ⚠撤去2026-08-23（読み手ゼロ）: 植民地直送の節（ColonialRows/HasColonialRows）＝TradeTab.axaml に束縛が無く、VM だけが毎月集計していた。

        // ---- 第2部：輸送サマリ（海運の占有分解＝InternationalMarket の shipEff 合成式と同じ順で表示） ----
        // 03手順7：通商破壊は容量でなくマージン（リスク料）に乗る＝shipEff合成式からraidを撤去しリスクを別行で表示
        double burden = Math.Max(0, w.ColonialShipBurden);
        double burdenLoss = w.ShipCapacity * (burden / (1.0 + burden));
        // 輸送容量は3〜4桁の素の数＝k化すると全て「0.x k」に潰れる。桁区切りだけを外す（作者裁定2026-08-10）
        // ★作者指定2026-08-11：陸上／海上の2節に分け、**指定の項目だけ**を出す。
        //   合成結果（実効海運）と1州あたりの割り算は落とした＝読み手が足し引きできる素の量だけを残す。
        // ★2026-08-15作者フィードバック(9)：「――陸上――」等のダッシュ区切りは廃止＝項目名だけで自明（鉄道=陸・海軍=海）
        var tRows = new List<TransportRow>
        {
            new("基本輸送力", Fmt.Qty(Transport.PreRailCapacity(w, w.Coef))),   // ★(166) 鉄道以前の陸の容量＝人口×基礎の目標（清算と同じ1本）
        };
        if (w.RailCapacity > 1e-9) tRows.Add(new TransportRow("鉄道輸送力", Fmt.Qty(w.RailCapacity)));   // 鉄道が無い間は出さない（0 の行を並べない・B21）
        tRows.Add(new TransportRow("海軍輸送力", Fmt.Qty(w.ShipCapacity)));
        if (burdenLoss > 1e-9) tRows.Add(new TransportRow("植民地補給", Fmt.Qty(-burdenLoss)));   // 差し引く量＝負号は「−」（Fmt.Qty）
        if (w.LastBlockadeRisk > 1e-9) tRows.Add(new TransportRow("リスク料（通商破壊）", $"{Fmt.Share(w.LastBlockadeRisk)}"));
        // 使用率＝運びたかった重量 ÷ 配られた輸送容量（州ごとの値を自国の活きている州で合算）。100% を超える＝輸送が交易を絞っている
        double pot = 0, cap = 0;
        foreach (var rg in w.Regions)
            if (rg.LastTransportCap > 1e-9 && rg.LastTransportCap < 1e15) { pot += Math.Max(0, rg.LastTradePotential); cap += rg.LastTransportCap; }
        tRows.Add(new TransportRow("使用量", Fmt.Qty(pot)));
        TransportRows = tRows;
        double use = cap > 1e-9 ? pot / cap : double.NaN;
        TransportUseText = double.IsFinite(use) ? $"{Fmt.Share(use)}" : "—";
        TransportUseBrush = !double.IsFinite(use) ? Avalonia.Media.Brushes.Gray : use > 1.0 ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#B02418")) : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2118"));
    }

    // ⚠撤去2026-08-23（読み手ゼロ）: 弧3-1/3-2 の計器板（ArcGrowthText/ArcCreditMultText/ArcSubsidyText/ArcCrowdingText/ArcWelfareText/ArcFiscalCapacityText と NotifyArc）＝どの axaml も束縛していなかった。
}
