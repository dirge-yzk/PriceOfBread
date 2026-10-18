// GameViewModel.Trade.cs ＝ 貿易タブ＝輸出入一覧・輸送サマリ・条約港。
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

// ===== 貿易タブ（Yキー・2026-07-24新設＝外国経済の可視化doc §3-6/§3-7）=====
// 第1部＝輸出一覧・輸入一覧（額の降順・世界シェア%/輸入依存度%つき）＋ヘッダ行（総額・収支・関税収入）。
// 第2部＝輸送サマリ（ヘッダから移設した鉄道/船舶＋海運の占有分解）。
// 全てデータ行のみ（UI恒久規律）。額は世界価格建て（決済実態と一致）・表示は月割（÷MonthsPerClearing＝関税予想 Fiscal.cs と同じ単位ヘルパー）。
// 貿易の1行（作者指定2026-08-11）＝材名／額(世界価格比)／依存度（輸出は世界シェア）／相手。
// ★2026-09-23：**量（Qty）の列を畳んだ**＝2段組の1段298.5pxでは財名に30.5pxしか残らず、実写（清1900）で
//   輸入18行中13行・輸出40行中25行が「コー…」「チョ…」で読めなかった。量の情報は隣の列に相対形で残る
//   （輸入＝依存度＝国内消費に占める輸入量／輸出＝世界シェア＝世界の輸出量に占める自国の量）。算術は TradeTab.axaml。
public sealed record TradeRow(string Name, string Value, string Extra, string Partner = "");
public sealed record TransportRow(string Label, string Value);
// ⚠撤去2026-08-23（読み手ゼロ）: LogisticsRow／LogisticsRows ＝州別物流表は axaml から外され、VM 側は宣言だけが残っていた（生成コードも無し）。

public partial class GameViewModel
{
    [ObservableProperty] private List<TradeRow> _exportRows = new();
    [ObservableProperty] private List<TradeRow> _importRows = new();
    [ObservableProperty] private string _tradeExportTotal = "—";
    [ObservableProperty] private string _tradeImportTotal = "—";
    [ObservableProperty] private string _tradeBalance = "—";
    public string TreatyPortLine { get; private set; } = "";   // ★条約港（2026-08-04）。空＝非表示
    public bool HasTreatyPorts => TreatyPortLine.Length > 0;
    [ObservableProperty] private List<TransportRow> _transportRows = new();
    public bool HasExportRows => ExportRows.Count > 0;
    public bool HasImportRows => ImportRows.Count > 0;

    private void RefreshTrade()
    {
        if (!TabTrade) return;
        var w = _player;
        var wp = _sim.Market.Price;
        int gc = Catalog.Goods.Length;

        // ---- 第1部：輸出入一覧（§3-4テレメトリ＝国際決済の国別×財別額。決済前/単一国は空） ----
        var expTmp = new List<(double val, TradeRow row)>();
        var impTmp = new List<(double val, TradeRow row)>();
        double expTotal = 0, impTotal = 0;
        if (w.ExportsByGood is { } eg && w.ImportsByGood is { } ig)
        {
            // 世界シェアの分母＝全モデル国の同財輸出額の合計
            var worldExp = new double[gc];
            foreach (var n in _sim.Nations)
                if (n.World.ExportsByGood is { } neg)
                    for (int g = 0; g < gc && g < neg.Length; g++) worldExp[g] += neg[g];
            // 相手＝**実際に取引した相手の州**（二国間交易2026-08-11）。エンジンが材ごとに最大額の相手を記録する。
            //   ⚠旧（世界市場が池だった頃）は相手が存在せず「その財を最も多く出している国」で代用していた＝近似だった。
            //   二国間の台帳が無い＝まだ置換前の経路（BilateralTrade=0）で走っている場合だけ、その近似へ落ちる。
            string TopPartner(int g, bool wantExporter)
            {
                var mine = wantExporter ? w.ImportFromByGood : w.ExportToByGood;   // 輸入の相手＝売り手／輸出の相手＝買い手
                if (mine is not null && g < mine.Length && !string.IsNullOrEmpty(mine[g])) return mine[g];
                string best = ""; double bv = 0;
                foreach (var n in _sim.Nations)
                {
                    if (ReferenceEquals(n.World, w)) continue;
                    var arr = wantExporter ? n.World.ExportsByGood : n.World.ImportsByGood;
                    if (arr is null || g >= arr.Length || !(arr[g] > bv)) continue;
                    bv = arr[g]; best = n.World.SelfPower?.Name ?? n.World.Region.Name;
                }
                return best;
            }
            // ★額が £0.0k に丸まる行は出さない（作者指摘2026-08-11「取引額0の材が並ぶ」）。
            //   判定は**表示に使う関数そのもの**で行う＝「0と表示されるなら出さない」が定義になり、書式を変えてもずれない。
            bool Shown(double monthly) => Money(monthly) is not ("£0.0k" or "-£0.0k");
            double mpc = w.Coef.MonthsPerClearing;   // 清算単位→月割（四半期清算=3・月次清算=1）＝単位ヘルパーに追随（2026-08-23・旧は /3.0 の直書き）
            // 輸入依存度の分母（財ごとの前期総需要）を先に1パスで積む（財×州の再走査をやめる・全州の合計は順序不変）。
            var demandByGood = new double[gc];
            foreach (var r in w.Regions)
                if (r.State == RegionState.Active)
                    for (int g = 0; g < gc && g < r.Markets.Length; g++) demandByGood[g] += r.Markets[g].LastDemand;
            for (int g = 0; g < gc; g++)
            {
                double p = g < wp.Count ? Math.Max(wp[g], 1e-9) : 1.0;
                double dom = g < w.Region.Markets.Length ? w.Region.Markets[g].Price : p;   // 自国（首都州）の相場＝世界価格比の分子
                string ratio = p > 1e-9 ? $"(×{dom / p:F2})" : "";
                if (g < eg.Length && eg[g] > 1e-9)
                {
                    expTotal += eg[g];
                    double share = worldExp[g] > 1e-12 ? eg[g] / worldExp[g] : 0;
                    if (Shown(eg[g] / mpc))
                        expTmp.Add((eg[g], new TradeRow(Catalog.Goods[g].Name, $"{Money(eg[g] / mpc)}{ratio}", $"{share:P0}", TopPartner(g, false))));
                }
                if (g < ig.Length && ig[g] > 1e-9)
                {
                    impTotal += ig[g];
                    double qtyMonthly = ig[g] / p / mpc;   // 輸入依存度＝消費（前期総需要・月次）に占める輸入量（月割）
                    double demand = demandByGood[g];
                    double dep = demand > 1e-9 ? Math.Min(1.0, qtyMonthly / demand) : 0;
                    if (Shown(ig[g] / mpc))
                        impTmp.Add((ig[g], new TradeRow(Catalog.Goods[g].Name, $"{Money(ig[g] / mpc)}{ratio}", $"{dep:P0}", TopPartner(g, true))));
                }
            }
        }
        double mpcTot = w.Coef.MonthsPerClearing;
        ExportRows = expTmp.OrderByDescending(t => t.val).Select(t => t.row).ToList();
        ImportRows = impTmp.OrderByDescending(t => t.val).Select(t => t.row).ToList();
        TradeExportTotal = $"{Money(expTotal / mpcTot)}/月";
        TradeImportTotal = $"{Money(impTotal / mpcTot)}/月";
        double bal = (expTotal - impTotal) / mpcTot;
        TradeBalance = $"{(bal >= 0 ? "＋" : "−")}{Money(Math.Abs(bal))}/月";
        // ★2026-08-15作者フィードバック(10)：関税行（財政タブと重複）・開放度行は撤去。収支は総額の下の大型表示へ（財政タブと同レイアウト）
        // ★条約港（2026-08-04・作者仕様(4)の可視化）：開かされた港の数と関税自主権の実効低下。0港なら行ごと出さない。
        TreatyPortLine = Politics.ForeignPortShare(w) > 0   // setter は private プロパティへ直接・★(402) 関税が目減りしている間は出す（旧＝UnequalTreatyInForce の半分の線で 0〜0.25 の持分が見えなかった・反証役 rs〔bv〕低#1）
            ? $"条約港　外国持分 {Politics.ForeignPortShare(w):P0}　関税の実効 ×{InternationalMarket.TreatyPortTariffScale(w):F2}"
            : "";
        OnPropertyChanged(nameof(TreatyPortLine)); OnPropertyChanged(nameof(HasTreatyPorts));
        OnPropertyChanged(nameof(HasExportRows)); OnPropertyChanged(nameof(HasImportRows));

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
            new("基本輸送力", $"{Transport.PreRailCapacity(w, w.Coef):F0}"),   // ★(166) 鉄道以前の陸の容量＝人口×基礎の目標（清算と同じ1本）
            new("鉄道輸送量", $"{w.RailCapacity:F0}"),
            new("海軍輸送量", $"{w.ShipCapacity:F0}"),
        };
        if (burdenLoss > 1e-9) tRows.Add(new TransportRow("植民地補給により", $"-{burdenLoss:F0}"));
        if (w.LastBlockadeRisk > 1e-9) tRows.Add(new TransportRow("リスク料(通商破壊)", $"{w.LastBlockadeRisk:P0}"));
        TransportRows = tRows;
    }

    // ⚠撤去2026-08-23（読み手ゼロ）: 弧3-1/3-2 の計器板（ArcGrowthText/ArcCreditMultText/ArcSubsidyText/ArcCrowdingText/ArcWelfareText/ArcFiscalCapacityText と NotifyArc）＝どの axaml も束縛していなかった。
}
