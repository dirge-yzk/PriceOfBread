// GameViewModel.Fiscal.cs ＝ 財政と貨幣のページ（(501) UI の作り直し 段3）。
// 支出・収入・国庫の台帳・金融・国債と外資信用は、本体の目録（SimCore/Display/FiscalTables）が渡す表を共通の表（Controls/TableView）が描く。
// 予測と実効の額・段の押し釦（税・教育費・軍事費・社会保障・行政費・近代化投資・関税・政策金利・通貨制度・資本移動・通貨改革）の読み口と書き口は目録の側（AI と同じ門を通る）。
// ここに残すのは、App だけが採れる標本（教育費・専売益・外資の流れの四半期差分／国庫の台帳の月の差分）と、ヘッダーの国債の文字と、年次系列（国庫・国債・技術水準）の値の写し。
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // ---- 標本（四半期の境で採る差分）＝教育費・専売益・外資の流れの実績は州の累積カウンタの四半期差分→月平均（市場依存で毎 tick の予測が難しい項）。Refresh.cs が採る ----
    private double _eduMonthly, _stateMonopMonthly;                 // 実績（直近四半期の月平均）＝Region の累積の差分から
    private double _prevEduCum, _prevStateMonopCum;
    private int _prevQuarterStamp = -1;   // 四半期境界の検出（年×4＋四半期）。-1＝未捕捉＝初回は基準化のみ
    private double _prevForeignNet;       // 外資の純フロー（流入なら収入側・流出なら支出側に計上＝片方は £0）
    private double _fInMonthly, _fOutMonthly;
    /// <summary>国庫の台帳の月の差分（<see cref="TreasuryCat"/> の添字）。台帳（World.TreasuryLedger）はエンジンの全ての書き手を通した＝合計はヘッダーの収支と厳密に一致する。</summary>
    private double[] _ledgerDiff = new double[(int)TreasuryCat.Count];

    // ---- 表（本体の目録が行と列を渡す）----
    [ObservableProperty] private TableSnapshot _spendTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _incomeTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _ledgerTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _financeTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _debtTable = TableSnapshot.Empty;
    // 欄の見出しに合計を載せる（表の列と合計の位置を揃える仕掛けを作らない）：支出・収入＝予測と実効の合計／国庫の台帳＝今月の収支と予測の収支
    [ObservableProperty] private string _spendTitle = "支出", _incomeTitle = "収入", _ledgerTitle = "国庫の台帳";
    // ---- 年次の線（名前つきの系列の倉から値を写す）----
    [ObservableProperty] private IReadOnlyList<double> _treasuryValues = [];
    [ObservableProperty] private IReadOnlyList<double> _govDebtValues = [];

    private const string GovDebtSeries = "fiscal.govDebt", FiscalYearSeries = "fiscal.sampleYear";

    /// <summary>目録の表を読んで画面の写しにする。段の押し釦は、押したら書き込みの後ですぐ財政の表を読み直す（停止中でも押した瞬間に予測が追従する）。</summary>
    private TableSnapshot SnapFiscal(ITable table)
    {
        var data = table.Read(_sim, _player);
        var rows = data.Rows.Select(r => r with
        {
            Cells = r.Cells.Select(c => c is StageCell st ? st with { Select = level => { st.Select(level); UpdateForecast(); } } : c).ToList(),
        }).ToList();
        return TableSnapshot.From(data with { Rows = rows });
    }

    /// <summary>年に 1 回、国債の残高を名前つきの系列へ採る（年は系列に持つ＝ロード後も重ならない）。</summary>
    private void SampleFiscalYear()
    {
        var years = _series.Of(FiscalYearSeries);
        int y = _player.Year;
        if (years.Count > 0 && years[^1] >= y) return;
        years.Add(y);
        _series.Of(GovDebtSeries).Add(_player.GovDebt.Value);
    }

    /// <summary>国庫の台帳の月の差分を採る（月の境で 1 回・ヘッダーの収支と同じ瞬間）。表の組み直しは Refresh と見えた瞬間が行う。</summary>
    private void SampleTreasuryLedger()
    {
        var now = _player.TreasuryLedger;
        for (int i = 0; i < _ledgerDiff.Length; i++) _ledgerDiff[i] = i < now.Length && i < _ledgerPrev.Length ? now[i] - _ledgerPrev[i] : 0;
        _ledgerPrev = (double[])now.Clone();
    }

    // ★阿片の中毒度（主フラグ ON のゲームのみ）＝生産のページが出す。中毒は財の性質であって禁令の特例ではない（禁令は法の表の阿片）。
    public bool OpiumVisible => _player.Coef.OpiumAddictionGrowth > 0;
    [ObservableProperty] private string _opiumLine = "";
    private double OpiumAvgAddiction()
    {
        double add = 0, sz = 0;
        foreach (var r in _player.Regions) { double rp = r.SumPopulation(); add += r.OpiumAddiction * rp; sz += rp; }
        return sz > 1e-9 ? add / sz : 0;
    }

    [ObservableProperty] private string _unifLine = "";   // P4 統一の機運（ヘッダーの概況・空＝非表示）

    /// <summary>★総動員のスイッチ（戦争ページの総動員トグル）。総動員中は「復員」に変わる。</summary>
    public string MobilizeButtonText => PlayerMobilized ? "復員" : "総動員";
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleMobilize()
    {
        PlayerMobilized = !PlayerMobilized;
        OnPropertyChanged(nameof(MobilizeButtonText));
    }

    /// <summary>財政の更新（月・四半期の Refresh と、財政のページが見えた瞬間と、段の押し釦を押した直後）。
    /// 見えている時だけ表と線を組む（見えていないページは更新を受けない）。ヘッダーの国債の文字は常に追従する。</summary>
    private void UpdateForecast()
    {
        if (OpiumVisible) OpiumLine = $"アヘン中毒度 {OpiumAvgAddiction():P1}";   // 中毒度は財の性質＝表示は残る（禁令は法の表へ移行・2026-07-30）
        UpdateDebtHeader();
        SampleFiscalYear();   // 年に 1 回の採取は見えているかに依らない（線は見えた時に写す）
        if (!FiscalShown) return;
        var flows = new FiscalFlows(_eduMonthly, _stateMonopMonthly, _fInMonthly, _fOutMonthly, _ledgerDiff);
        var budget = FiscalTables.Budget(_player, flows);
        SpendTable = SnapFiscal(FiscalTables.Spend(budget));
        IncomeTable = SnapFiscal(FiscalTables.Income(budget));
        LedgerTable = SnapFiscal(FiscalTables.Ledger(flows));
        FinanceTable = SnapFiscal(FiscalTables.Finance);
        DebtTable = SnapFiscal(FiscalTables.Debt);
        SpendTitle = $"支出　予測 {Fmt.Of(Unit.MoneyPerMonth, budget.SpendForecast)}　実効 {Fmt.Of(Unit.MoneyPerMonth, budget.SpendActual)}";
        IncomeTitle = $"収入　予測 {Fmt.Of(Unit.MoneyPerMonth, budget.IncomeForecast)}　実効 {Fmt.Of(Unit.MoneyPerMonth, budget.IncomeActual)}";
        double bal = FiscalTables.LedgerBalance(flows), fb = budget.BalanceForecast;
        LedgerTitle = $"国庫の台帳　収支 {Fmt.SignedMoneyPerMonth(bal)}　予測 {Fmt.SignedMoneyPerMonth(fb)}";
        // 年次の線：写しを作り直す（同じ List の参照では線が描き直されない）
        TreasuryValues = _histTreasury.ToArray();
        GovDebtValues = _series.Of(GovDebtSeries).ToArray();
    }

    // 国債（借入）＝ヘッダー表示（借金ボタン廃止・2026-07-21／借入はエンジンが r＜1 の月に自動・2026-09-28）。DebtHeaderText＝残高（＋デフォルト予兆）／Tip＝内訳／Brush＝支払不能で赤。
    private void UpdateDebtHeader()
    {
        var c = _player.Coef;
        // ★(405) 比の門を相対に：残りの未払いの大きさ left を u（払えない割合）で割った月数が、ゲームに残る月数の内に収まるときだけ出す（割り算をしないので u が極小でも溢れない・u か left が NaN なら出さない）
        double unpaidShare = 1.0 - _player.FiscalFundingRatio.Value;
        double arrearsLeft = Math.Max(0, c.DefaultArrearsMonths - _player.InsolvencyMonths);
        double gameMonthsLeft = Math.Max(0, Calendar.YearsToMonths(Calendar.EndYear - Calendar.StartYear) - _simDays / Calendar.DaysPerMonth);
        bool showDefaultEta = _player.InsolvencyMonths > 0 && unpaidShare > 0 && arrearsLeft <= unpaidShare * gameMonthsLeft;
        DebtHeaderText = $"国債：{Fmt.Money(_player.GovDebt.Value)}"
                       + (showDefaultEta
                          ? $"　デフォルトまで{Math.Ceiling(arrearsLeft / unpaidShare):F0}か月" : "");   // ★2026-09-26：カウンタは月次で進む（dorm-29）・★(205) 計数は払えなかった大きさの累計＝残りを今の r の払えない割合で割った月数
        // 財7単位フリップ（2026-07-26）：表示も対年GDP（史実の債務比率と直接比較できる単位）。分母＝年次名目（月次 NominalGdpPerMonth×12）＝エンジン Fiscal.cs の兌換停止/国債金利の閾値と同じ単位
        DebtHeaderTip = $"国債 {Fmt.Money(_player.GovDebt.Value)}　対年GDP {(_player.NominalGdpPerMonth.Value > 1e-9 ? Math.Min(9.99, Calendar.ToGdpYear(_player.GovDebt, Calendar.PerMonthToPerYear(_player.NominalGdpPerMonth)).Value) : 0):P0}　金利 {Fmt.Share(_player.GovBondRatePerYear)}　債券価格 {Fmt.Share(_player.BondPrice)}"
                       + (_player.ForeignDebt.Value > 1 ? $"\nうち対外 {Fmt.Money(_player.ForeignDebt.Value)}" : "")
                       + (_player.AusterityIntensity > 0 ? $"\n緊縮 {Fmt.Share(_player.AusterityIntensity)}" : "")
                       + (_player.CumulativeDevaluation > 1 ? $"\n切下げ累計 {Fmt.Money(_player.CumulativeDevaluation)}" : "")
                       + (_player.FiscalFundingRatio.Value < 1 ? $"\n支払い率 {Fmt.Share(_player.FiscalFundingRatio)}" : "");
        DebtHeaderBrush = _player.InsolvencyMonths > 0 ? NewsRed : TermNeutral;
    }
    [ObservableProperty] private string _debtHeaderText = "";
    [ObservableProperty] private string _debtHeaderTip = "";
    [ObservableProperty] private HeaderStat _treasuryHover = new();   // 国庫のホバーグラフ（年次推移・2026-07-16）
    // ★2026-08-02 描画クラッシュの修正：Avalonia の Polyline は Points が null だと CreateDefiningGeometry で ArgumentNullException を投げ、毎フレーム描画パスが中断する。
    //   バインド経路の途中が null だと Points に null が届く＝VM 側で必ず非 null を返すプロパティを用意してそこへバインドする。空の Points は例外にならない。
    public global::Avalonia.Points TreasurySparkPoints => TreasuryHover?.Points ?? new();
    partial void OnTreasuryHoverChanged(HeaderStat value) => OnPropertyChanged(nameof(TreasurySparkPoints));
    [ObservableProperty] private IBrush _debtHeaderBrush = TermNeutral;
}
