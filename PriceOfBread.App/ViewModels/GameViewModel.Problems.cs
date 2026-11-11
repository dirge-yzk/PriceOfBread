// GameViewModel.Problems.cs ＝ 問題の一覧（(501) UI の作り直し 段4・設計 §5）。左の列の上。
// ・行は本体の目録（<see cref="ProblemScan"/>）が渡す。ここは種類ごとの見出しと上位の件数、前の四半期からの向き（↑↓）、押したときの飛び先を持つだけ。
// ・閾値で選ばない：0 でない物を大きさの順に並べる（どこからが問題かは画面に出さない）。上位の件数だけ見せ、見出しを押すと全部を見る。
// ・更新は月ごと（ヘッダーと同じ）。向きは前の四半期の頭の大きさとの比較＝基準は四半期の番号で持つ（基準は画面の側の記憶で、盤面には書かない）。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

/// <summary>問題の一覧の 1 行。種類の見出し（<see cref="IsHeader"/>）か、問題 1 件。押すと見出しは全部の開閉・問題はそのページのその行へ飛ぶ。</summary>
public sealed partial class ProblemRow : ObservableObject
{
    public ProblemRow(string key, Action<string> activate)
    {
        Key = key;
        ActivateCommand = new RelayCommand(() => activate(Key));
    }

    public string Key { get; }
    public IRelayCommand ActivateCommand { get; }
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsItem))] private bool _isHeader;
    [ObservableProperty] private string _kind = "";
    [ObservableProperty] private string _target = "";
    [ObservableProperty] private string _sizeText = "";
    [ObservableProperty] private string _trend = "";
    public bool IsItem => !IsHeader;
    public void CopyFrom(ProblemRow s) { IsHeader = s.IsHeader; Kind = s.Kind; Target = s.Target; SizeText = s.SizeText; Trend = s.Trend; }
}

/// <summary>飛び先の要求（ページを開いたあと、表の行・欄へ飛ぶ）。</summary>
public sealed record FocusRequest(Page Page, string TableKey, string RowKey, string Section);

public partial class GameViewModel
{
    /// <summary>種類ごとに見せる上位の件数（見出しを押すと全部）。</summary>
    public const int ProblemTopN = 4;

    /// <summary>前の四半期との比較の不感帯（これ以内の増減は向きを出さない＝月ごとの揺れで矢印が点滅しない）。画面に出さない。</summary>
    private const double ProblemTrendBand = 0.05;

    public ObservableCollection<ProblemRow> ProblemRows { get; } = new();
    public bool HasProblems => ProblemRows.Count > 0;
    public bool NoProblems => ProblemRows.Count == 0;

    private IReadOnlyList<ProblemItem> _problemItems = Array.Empty<ProblemItem>();
    private readonly Dictionary<string, ProblemItem> _problemByRowKey = new();
    private readonly HashSet<ProblemKind> _problemExpanded = new();
    private Dictionary<string, double> _problemBase = new();      // いまの四半期の頭での大きさ（鍵→大きさ）
    private Dictionary<string, double> _problemCompare = new();   // 比べる相手＝前の四半期の頭での大きさ。四半期の境を 2 回通るまで空（空の間は向きを出さない）
    private int _problemBaseQuarter = -1;                         // 基準を取った四半期の番号（年×4＋四半期）
    /// <summary>飛び先の要求。GameView が受けて、ページの表の行へ飛ぶ。</summary>
    public event Action<FocusRequest>? FocusRequested;

    /// <summary>問題の一覧を引き直す（毎月・何度呼んでもよい）。向きの基準は四半期の番号で持つ＝番号が変わった時だけ取り直す（同じ四半期の中の組み直しで矢印が変わらない）。</summary>
    private void RefreshProblems()
    {
        var flows = new FiscalFlows(0, 0, 0, 0, _ledgerDiff);   // 収支の赤だけを読む（月の台帳の差分）
        _problemItems = ProblemScan.Scan(_sim, _player, flows);
        int quarter = _player.Year * Calendar.QuartersPerYear + _player.Quarter;
        if (quarter != _problemBaseQuarter)
        {
            _problemCompare = _problemBase;   // 比べる相手＝前の四半期の頭の値
            _problemBase = _problemItems.ToDictionary(p => p.Key, p => p.Size);
            _problemBaseQuarter = quarter;
        }
        var rows = new List<ProblemRow>();
        _problemByRowKey.Clear();
        foreach (var group in _problemItems.GroupBy(p => p.Kind))
        {
            var kind = group.Key;
            var list = group.ToList();
            bool open = _problemExpanded.Contains(kind) || list.Count <= ProblemTopN;
            var h = new ProblemRow("h|" + kind, ActivateProblem)
            {
                IsHeader = true, Kind = ProblemScan.KindName(kind),
                SizeText = $"{list.Count}件",   // 件数だけ（旧「4/81」＝見せている数/全数は意味が読めない・2026-11-11 作者指摘）
            };
            rows.Add(h);
            foreach (var p in open ? list : list.Take(ProblemTopN))
            {
                rows.Add(new ProblemRow(p.Key, ActivateProblem) { Target = p.Target, SizeText = SizeTextOf(p), Trend = TrendOf(p) });
                _problemByRowKey[p.Key] = p;
            }
        }
        SyncRows(ProblemRows, rows, r => r.Key, (t, s) => t.CopyFrom(s));
        OnPropertyChanged(nameof(HasProblems)); OnPropertyChanged(nameof(NoProblems));
    }

    /// <summary>問題の大きさの文字。休止と倒産は棟数（「13棟」）・動かせる法は数が全行 1 で意味が無いので出さない。</summary>
    private static string SizeTextOf(ProblemItem p) => p.Kind switch
    {
        ProblemKind.Bankruptcy => Fmt.Of(p.Unit, p.Size) + "棟",
        ProblemKind.MovableLaw => "",
        _ => Fmt.Of(p.Unit, p.Size),
    };

    /// <summary>前の四半期からの向き。基準に無い問題（新しく出た）は上向き・消えた問題は一覧に無いので出ない。</summary>
    private string TrendOf(ProblemItem p)
    {
        if (_problemCompare.Count == 0) return "";   // 開始の直後（比べる四半期がまだ無い）は向きを出さない
        if (!_problemCompare.TryGetValue(p.Key, out double before)) return "↑";
        if (p.Size > before * (1 + ProblemTrendBand)) return "↑";
        if (p.Size < before * (1 - ProblemTrendBand)) return "↓";
        return "";
    }

    /// <summary>行を押した。見出し＝その種類の全部／上位だけの切り替え・問題＝そのページのその行へ飛ぶ。</summary>
    private void ActivateProblem(string key)
    {
        if (key.StartsWith("h|", StringComparison.Ordinal))
        {
            if (Enum.TryParse<ProblemKind>(key[2..], out var kind) && !_problemExpanded.Remove(kind)) _problemExpanded.Add(kind);
            RefreshProblems();
            return;
        }
        if (_problemByRowKey.TryGetValue(key, out var p)) OpenAndFocus(p.Page, p.TableKey, p.RowKey, p.Section);
    }

    /// <summary>ページを選んでいる分割に開き、その行・欄へ飛ぶ（問題の一覧とピン留めが使う 1 本）。ページが載って値が入ったあとに要求を出す
    /// （見えた瞬間の更新は <see cref="GameViewModel.RefreshOnShown"/> が同期で済ませる。部品の配置が済むのを待つため 1 回だけ後ろへ回す）。</summary>
    public void OpenAndFocus(Page page, string tableKey, string rowKey, string section)
    {
        Layout.Open((Slot)(int)page);
        var req = new FocusRequest(page, tableKey, rowKey, section);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => FocusRequested?.Invoke(req), Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>生産の財の表の行への飛び先：まだ選んでいない財だけ選ぶ（選び直すと選びが外れる＝同じ財を選ぶ動作は切替なので）。</summary>
    public void SelectProdGoodIfNew(int gid)
    {
        if (_prodSelGood != gid) SelectProdGood(gid);
    }

    /// <summary>撮影専用：上位の件数に収まらない種類を全部見せる／戻す。</summary>
    internal void ExpandAllProblemsForCapture(bool expand)
    {
        _problemExpanded.Clear();
        if (expand) foreach (var k in Enum.GetValues<ProblemKind>()) _problemExpanded.Add(k);
        RefreshProblems();
    }

    /// <summary>撮影専用：いまの問題（検査用）。</summary>
    internal IReadOnlyList<ProblemItem> ProblemItemsForCapture => _problemItems;
}
