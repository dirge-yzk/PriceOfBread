using PriceOfBread.App;

namespace PriceOfBread.App.Controls;

/// <summary>ピン留めの受け口（設計 §4-3）。表の行・線グラフ・ヘッダーの指標の右クリックが呼ぶ。盤面の画面（GameView）が <see cref="PinHost.Current"/> に自分の VM を差す
/// （<c>TableView.DefaultConfirm</c> と同じ作法）。部品は VM を知らない＝部品の単体の確かめは口が null の形で動く。</summary>
public interface IPinHost
{
    bool IsPinned(string id);
    void Toggle(PinRef pin);
    /// <summary>表が値を映した（最後に映した形をピン留めが引き直す元にする。見えていないページの表は更新されないので、その間は最後の形のまま）。</summary>
    void SnapshotSeen(TableSnapshot snapshot);
    /// <summary>線グラフが自分を名乗った（鍵→線グラフ。ピン留めが値と書式を引き直す）。</summary>
    void ChartSeen(string key, LineChart chart);
}

public static class PinHost
{
    public static IPinHost? Current { get; set; }
}
