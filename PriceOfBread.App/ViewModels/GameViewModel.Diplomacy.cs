// GameViewModel.Diplomacy.cs ＝ 外交タブ＝国一覧（2段組）・カメラ移動・侵略/統一戦争/通商条約の確認モーダル。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 外交タブの国一覧＝**2段組**（2026-08-10作者指示）。順位の並びを列で読む（左列＝上位半分・右列＝残り）ので、
    // 段ごとに別のコレクションへ分ける（1本を折り返す実装では順位が行方向にジグザグする）。
    public ObservableCollection<NationCard> NationsLeft { get; } = new();
    public ObservableCollection<NationCard> NationsRight { get; } = new();

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
    // 禁輸ボタン（DiploEmbargo）は廃止（禁制品doc C1・2026-07-30）。禁輸は挑発に同梱され（AIの侮辱と同一）、戦時は強制・終戦で自動解除。
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
}
