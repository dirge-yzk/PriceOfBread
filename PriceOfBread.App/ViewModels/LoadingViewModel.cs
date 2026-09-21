using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PriceOfBread.App.ViewModels;

/// <summary>読み込み中の画面。国を選んでから盤面が出るまでの間（開始前の助走＝価格が落ち着くまで世界を回す）に挟む。
/// ★2026-08-15：助走が裏スレッドへ移りUIスレッドが空いたので、末尾の点をタイマーで1つずつ増やす＝進行中である印
/// （旧＝UIスレッド上の同期構築で描画が凍り、固定文字列しか出せなかった・作者指示2026-08-11）。</summary>
public sealed partial class LoadingViewModel : ViewModelBase
{
    private readonly string _baseMessage;
    private readonly DispatcherTimer _timer;
    private int _frame;

    /// <summary>表示文＝基底文＋点0〜3個の循環（300ms刻み）。</summary>
    [ObservableProperty] private string _message;

    /// <summary>最長フレーム（点3つ）。View が枠寸法の固定に使う＝点の増減で文字が左右へ揺れない。</summary>
    public string MessageWidest { get; }

    public LoadingViewModel(string baseMessage)
    {
        _baseMessage = baseMessage;
        _message = baseMessage;
        MessageWidest = baseMessage + "...";
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _timer.Tick += (_, _) => { _frame = (_frame + 1) % 4; Message = _baseMessage + new string('.', _frame); };
        _timer.Start();
    }

    /// <summary>点のタイマーを止める。★この画面を差し替える側が必ず呼ぶ（LoadingView の視覚木離脱でも保険で呼ばれる）
    /// ＝DispatcherTimer は止めない限り Tick がこのVMを掴み続ける（リーク）。何度呼んでも安全。</summary>
    public void Stop() => _timer.Stop();
}
