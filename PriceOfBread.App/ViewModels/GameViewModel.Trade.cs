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
// 全てデータ行のみ（UI恒久規律）。額は世界価格建て（決済実態と一致）・表示は月割（÷MonthsPerClearing＝関税予想 Fiscal.cs と同じ単位ヘルパー）。
// 貿易の行は本体の表（SimCore/Display/TradeTables）が渡し、共通の表（Controls/TableView）が描く（設計 §7-1 段1）。
// 作者指定2026-08-11＝材名／額(世界価格比)／依存度（輸出は世界シェア）／相手。★2026-09-23：量の列は畳んだ（量の情報は隣の列に相対形で残る）。
// ⚠撤去2026-08-23（読み手ゼロ）: LogisticsRow／LogisticsRows ＝州別物流表は axaml から外され、VM 側は宣言だけが残っていた（生成コードも無し）。

public partial class GameViewModel
{
    [ObservableProperty] private TableSnapshot _priceTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _exportTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _importTable = TableSnapshot.Empty;
    // 清算の線＝四半期ごとの輸出・輸入・収支（月割）。名前つきの系列の倉に貯め（セーブは倉が名前と値の組で往復する）、画面へは点列の写しを渡す。
    private const string ClearingExportKey = "clearing.export", ClearingImportKey = "clearing.import", ClearingBalanceKey = "clearing.balance";
    [ObservableProperty] private string _tradeExportTotal = "—";
    [ObservableProperty] private string _tradeImportTotal = "—";

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
    }

    // ⚠撤去2026-08-23（読み手ゼロ）: 弧3-1/3-2 の計器板（ArcGrowthText/ArcCreditMultText/ArcSubsidyText/ArcCrowdingText/ArcWelfareText/ArcFiscalCapacityText と NotifyArc）＝どの axaml も束縛していなかった。
}
