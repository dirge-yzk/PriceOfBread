// GameViewModel.Rows.cs ＝ 各ページの行オブジェクト（PopRow/WarRow/HeaderStat…）と地図のDTO。GameViewModel の外側の型。
// ★2026-08-10のUI分割：GameViewModel.cs の末尾にあった型を**そのまま**移した（宣言は1文字も変えていない）。
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PriceOfBread.App.ViewModels;

// 戦争タブの1行（自国が交戦中の1戦争＝戦勝点・犠牲者・戦費・厭戦）。キー＝敵Power.Id（変名しても行を差し替えない）。
/// <summary>他国の戦争への調停（列強の介入・プレイヤー側 2026-09-06）。キー＝"A|B"（Power.Id）。</summary>
public sealed partial class MediationRow : ObservableObject
{
    public string Key { get; init; } = "";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _info = "";
    [ObservableProperty] private bool _canMediate;
    [ObservableProperty] private string _reason = "";
    public void CopyFrom(MediationRow s) { Title = s.Title; Info = s.Info; CanMediate = s.CanMediate; Reason = s.Reason; }
}

public sealed partial class WarRow : ObservableObject
{
    public string Key { get; init; } = "";
    [ObservableProperty] private string _enemy = "";
    [ObservableProperty] private string _score = "";
    [ObservableProperty] private IBrush _scoreBrush = Brushes.Gray;
    [ObservableProperty] private string _info = "";
    [ObservableProperty] private string _forces = "";
    [ObservableProperty] private string _losses = "";
    [ObservableProperty] private string _cost = "";
    [ObservableProperty] private string _exhaustion = "";
    [ObservableProperty] private IReadOnlyList<StatTerm> _terms = System.Array.Empty<StatTerm>();   // 自国の厭戦の内訳（説明API#63）
    public bool HasTerms => Terms.Count > 0;
    partial void OnTermsChanged(IReadOnlyList<StatTerm> value) => OnPropertyChanged(nameof(HasTerms));
    public void CopyFrom(WarRow s)
    {
        Enemy = s.Enemy; Score = s.Score; ScoreBrush = s.ScoreBrush; Info = s.Info; Forces = s.Forces;
        Losses = s.Losses; Cost = s.Cost; Exhaustion = s.Exhaustion; Terms = s.Terms;
    }
}

// 終わった戦争の1行（講和後 PastWarKeepYears 年だけ戦争タブに残す）。
public sealed class PastWarRow
{
    public string Key { get; init; } = "";
    public double EndDay { get; init; }
    public string Title { get; init; } = "";    // 「イギリス　勝利」
    public string Detail { get; init; } = "";   // 期間・犠牲者・戦費
}


/// <summary>XAMLの小さな変換（2026-08-04）：与党を太字に・議席率をバー幅に。値だけを写す＝規律準拠。</summary>
public static class Conv
{
    public static readonly Avalonia.Data.Converters.IValueConverter BoolToWeight =
        new Avalonia.Data.Converters.FuncValueConverter<bool, FontWeight>(b => b ? FontWeight.Bold : FontWeight.Normal);
    // ★半円グラフ（2026-08-22 円→2026-09-07 半円）：VM が組む SVG 記法の Path 文字列を Geometry へ。空文字は null＝描かない（シェア0の扇形）。
    public static readonly Avalonia.Data.Converters.IValueConverter PathData =
        new Avalonia.Data.Converters.FuncValueConverter<string?, Avalonia.Media.Geometry?>(s => string.IsNullOrEmpty(s) ? null : Avalonia.Media.Geometry.Parse(s));
}

/// <summary>議席の1行（2026-08-04）：族名・議席率・得票率・与党か。SeatFrac はバーの幅（0..1）。</summary>
public sealed class SeatRow
{
    public string Name { get; init; } = "";
    public string Seats { get; init; } = "";
    public double SeatFrac { get; init; }
    public string Votes { get; init; } = "";
    public bool IsGov { get; init; }
    public IBrush Brush { get; init; } = Brushes.Gray;
}

/// <summary>支持基盤の1行（2026-08-04）：階級・全有権者に占める重み・その票の族の割れ方。</summary>
public sealed class SupportRow
{
    public string Class { get; init; } = "";
    public string Share { get; init; } = "";
    // ★カラム式（作者指示2026-08-22）＝旧「保守29%/自由48%/社会23%」の連結文字列をやめ、列に分ける（見出し行と揃う）。
    public string Cons { get; init; } = "";
    public string Lib { get; init; } = "";
    public string Soc { get; init; } = "";
}

public sealed class BlocRow
{
    public string Name { get; init; } = "";
    public string Percent { get; init; } = "";
    public string Tip { get; init; } = "";   // 力の源泉の分解（2026-08-04・ホバー）
}

// 政体別実績ストックの1行（信認⇔失望∈[-1,+1]）。正は緑・負は赤のバーで符号を色分け（データ表示）。
public sealed class RegimeRow
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";       // 符号つきパーセント（＋42% / −18%）
    public double BarWidth { get; init; }           // |値|×幅
    public IBrush Brush { get; init; } = Brushes.Gray;
}

// 権力の奪い合い（2026-08-17）の1行＝議会統制力を今期動かした1経路。名前・符号つきの寄与・入力のホバー。
// 並べ替えのキーは Magnitude（|寄与|の大きい順）＝押している側と押し返している側の主役から読める。
// 可変プロパティ＝毎期の更新は既存インスタンスへ値だけ流す（SyncRows）。キー＝Name（経路名＝安定）。
public sealed partial class PowerShiftRow : ObservableObject
{
    public string Name { get; init; } = "";
    [ObservableProperty] private string _value = "";    // 符号つきパーセント（＋0.80% / −0.35%）
    [ObservableProperty] private string _tip = "";      // その経路の入力（データのみ）
    [ObservableProperty] private IBrush _brush = Brushes.Gray;
    public double Magnitude;                            // 並べ替えのキー＝|寄与|。表示しない
    public void CopyFrom(PowerShiftRow s) { Value = s.Value; Tip = s.Tip; Brush = s.Brush; Magnitude = s.Magnitude; }
}

// 国内勢力（F2/F3a）の1行：機械生成名・規模・結束・要求top-3の**表示のみ**。
// 蜂起への対処に常設ボタンは持たない＝国内危機イベントのポップアップが唯一の入口（AIも同じ択を即決＝対称・2026-07-21再確認）。
// 可変プロパティ＝毎tickの更新は既存インスタンスへ値だけ流す（SyncRows・#87の据え置き同期）。キー＝Key（機械生成名＝安定）。
public sealed partial class FactionRow : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _size = "";
    // 結束∈[0,1]（FactionState.Cohesion）＝運動体としての持続の証。闘争度は戦力比の因子そのものなので列にしない（2026-09-08）。
    [ObservableProperty] private string _cohesion = "";
    // 蜂起への近接度＝エンジンの点火条件と同じ物差し・100%超＝蜂起圏（旧＝戦力比 FactionStrength÷(EffectiveCoerce+郷勇)／
    // 段4の点火後＝連言2門の律速側 min(意志÷門1, 絶望÷門2)。見出しも切り替わる＝GameViewModel.FactionProximityHeader）。
    // スナップショット行（永続FactionState無し＝強度が定義されない）は "—"＋ホバー無し（HasStrengthTip=false）。
    [ObservableProperty] private string _strength = "";
    [ObservableProperty] private string _strengthTip = "";      // 内訳ホバー（勢力の強さ／治安維持力／義勇兵＝ChallengeTipと同作法）
    [ObservableProperty] private bool _hasStrengthTip;          // 空ホバーの吹き出しを出さない2枚組切替（HeaderStatのTermsと同方式）
    [ObservableProperty] private IBrush _strengthBrush = UiRes.Brush("GrayBrush", Brushes.Gray);
    [ObservableProperty] private string _demands = "";
    public string Key { get; init; } = "";                       // 据え置き同期のキー（安定＝結束%を含めない）
    public void CopyFrom(FactionRow s) { Name = s.Name; Size = s.Size; Cohesion = s.Cohesion; Strength = s.Strength; StrengthTip = s.StrengthTip; HasStrengthTip = s.HasStrengthTip; StrengthBrush = s.StrengthBrush; Demands = s.Demands; }
}


// 戦争タブ「理想の軍隊」の1行（2026-08-16 作者指示）：材ごとの必要量・実際・充足率。兵士POPも同じ形の1行。
// Header 非空＝段の小見出し（人員/基本装備/上位装備/最上位装備/空軍装備/運用燃料）＝外交タブの NationCard と同じ
// 「見出しを同じコレクションに混ぜる」作法（別リストにすると並びがずれうる）。
// 可変プロパティ＝毎月の更新は既存インスタンスへ値だけ流す（SyncRows）。キー＝Key（材名＋段＝安定）。
public sealed partial class ArmyNeedRow : ObservableObject
{
    public string Key { get; init; } = "";
    public string Header { get; init; } = "";
    public bool IsHeader => Header.Length > 0;
    public bool IsRow => Header.Length == 0;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _need = "";     // 必要量（消耗の段は「/月」付き・耐久の段は目標在庫）
    [ObservableProperty] private string _have = "";     // 実際（買えた量 or 保有量 or 現在の兵士数）
    [ObservableProperty] private string _fill = "";     // 充足率（％）
    public double FillV;                                // 充足率の実数（節見出しの平均・表示はしない）
    [ObservableProperty] private IBrush _fillBrush = Brushes.Gray;
    [ObservableProperty] private string _tip = "";      // 内訳ホバー（データのみ＝単価・必要額・予算の充足・市場の配給…）
    public double Cost;                                 // 並べ替えのキー＝必要額（段の中で大きい順）。表示しない
    public void CopyFrom(ArmyNeedRow s) { Name = s.Name; Need = s.Need; Have = s.Have; Fill = s.Fill; FillV = s.FillV; FillBrush = s.FillBrush; Tip = s.Tip; Cost = s.Cost; }
}

// 出来事の1行：アイコン＋本文＋深刻度の色。Mine＝自国が当事者（年表の絞り「自国」の判定）・Great＝列強が関わる（絞り「列強」の判定）。
public sealed class AlertRow
{
    public string Icon { get; }
    public string Text { get; }
    public IBrush Color { get; }
    public bool Mine { get; }
    public bool Great { get; }
    public AlertRow(string icon, string text, string colorHex, bool mine = false, bool great = false)
    {
        // alerts.csv の color 列が不正（綴り誤り・桁数不足・空）でも落ちない契約を守る＝TryParse で失敗時は中立グレーへ（Make の string.Format 保護と対称）。
        Icon = icon; Text = text;
        Color = new SolidColorBrush(Avalonia.Media.Color.TryParse(colorHex, out var _col) ? _col : Avalonia.Media.Color.Parse("#808080")); Mine = mine; Great = great;
    }
    public AlertRow(string icon, string text, IBrush color, bool mine = false, bool great = false) { Icon = icon; Text = text; Color = color; Mine = mine; Great = great; }
}

// ヘッダー2行目の国家指標1項目。HasGraph の項目はホバーで年次推移の折れ線を出す（データのみ・説明文なし）。
// 可変プロパティ＝毎月の更新は既存インスタンスへ値だけ流す（リスト再生成＝ヘッダー点滅の原因を避ける・2026-07-19）。キー＝GraphTitle。
public sealed partial class HeaderStat : ObservableObject
{
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _hasGraph;
    /// <summary>字の色（深刻度・2026-11-03 作者「深刻度に合わせて色分け」）。既定＝ふつうの墨。</summary>
    [ObservableProperty] private IBrush _brush = SeverityBrushes.Normal;
    public string GraphTitle { get; init; } = "";
    /// <summary>据え置き同期（SyncRows）の突き合わせキー。★グラフも内訳も持たない項目は GraphTitle が空で、
    /// **空キーが2つ並んだ瞬間に SyncRows がキー重複と見て全行の作り直しへ落ちる**（ヘッダー14項目の破棄・再生成が
    /// 毎 Refresh 走っていた・2026-08-10）。表示には使わない＝一意であればよい。</summary>
    public string Key { get; init; } = "";
    /// <summary>この項目の箱の最小幅（画素・2026-09-23 作者指示「文字数固定」）＝桁が増減しても右の項目が動かない。
    /// ★Width でなく **MinWidth**：見積りを超える値が来たときに無言で切れるのでなく伸びる
    /// （固定 Width は収まらない行を黙ってクリップする＝2026-09-23の罠8）。各項目の最長文字列と算術は Refresh 側に書いてある。
    /// 行オブジェクトは据え置き同期（SyncRows）で使い回すが、この値は Key ごとの定数なので init のままでよい。</summary>
    public double BoxWidth { get; init; }
    [ObservableProperty] private global::Avalonia.Points _points = new();
    /// <summary>Polyline へ渡す点列（2026-08-02）。★null を絶対に返さない＝Points が null だと Avalonia が毎フレーム例外を投げる。</summary>
    public global::Avalonia.Points SafePoints => Points ?? new();
    partial void OnPointsChanged(global::Avalonia.Points value) => OnPropertyChanged(nameof(SafePoints));
    [ObservableProperty] private string _rangeText = "";
    // ★2026-09-23（作者指示「詳細表示は全てグラフのみ」）：説明API内訳（Terms）はヘッダーから撤去した。
    //   内訳の行き先＝外資信用/行政力→財政タブ・正統性/国民意識→政治タブ・人口/識字率/満足度/闘争度→POPタブ。
    public bool HasHover => HasGraph;   // ホバー＝折れ線だけ
    public bool Plain => !HasGraph;     // 素のテキスト（ホバーなし）
    partial void OnHasGraphChanged(bool value) { OnPropertyChanged(nameof(HasHover)); OnPropertyChanged(nameof(Plain)); }
    public void CopyFrom(HeaderStat s) { Text = s.Text; HasGraph = s.HasGraph; Brush = s.Brush; Points = s.Points; RangeText = s.RangeText; }
}

/// <summary>ヘッダーの深刻度の色（2026-11-03）。通常＝墨・注意＝琥珀・危険＝赤。帯の境目は各項目の側（Refresh.cs）。</summary>
public static class SeverityBrushes
{
    public static readonly IBrush Normal = UiRes.Brush("InkBrush", Brushes.Black);
    public static readonly IBrush Caution = new SolidColorBrush(Color.Parse("#B7791F"));
    public static readonly IBrush Danger = new SolidColorBrush(Color.Parse("#B23A2E"));
    /// <summary>悪いほど大きい量。<paramref name="caution"/> 以上で琥珀・<paramref name="danger"/> 以上で赤（NaN は通常）。</summary>
    public static IBrush HighIsBad(double v, double caution, double danger) => v >= danger ? Danger : v >= caution ? Caution : Normal;
    /// <summary>良いほど大きい量。<paramref name="caution"/> 未満で琥珀・<paramref name="danger"/> 未満で赤（NaN は通常）。</summary>
    public static IBrush LowIsBad(double v, double caution, double danger) => v < danger ? Danger : v < caution ? Caution : Normal;
}

// 説明API（#63）の内訳1行＝理由＋符号つき寄与量。正の寄与は緑・負は赤で色分け（データ表示＝UI規律に適合）。
public sealed class StatTerm
{
    public string Text { get; init; } = "";
    public IBrush Brush { get; init; } = Brushes.Gray;
}

// 終局（1949年12月のあと＝GameQuarters 経過）の最終ランキング1行。
public sealed class EndingRow
{
    public string Order { get; init; } = "";
    public string Name { get; init; } = "";
    public string Score { get; init; } = "";
    public string Polity { get; init; } = "";
    public string Population { get; init; } = "";
    public string IronShare { get; init; } = "";
    public IBrush RowBrush { get; init; } = Brushes.Transparent;
}

// 終局年代記の1行（#129・2026-07-25）：そのプレイの政変・戦争・恐慌・デフォルト・建国の事実記録（発生日・全国）。
public sealed record AnnalRow(string Date, string Icon, string Text);

// 州○ホバーの詳細（地図左上パネルに表示）。行ごとに色/太さ＝生産量上位のハイライト等。
public sealed class MapHoverLine
{
    public string Text { get; init; } = "";
    public IBrush Brush { get; init; } = Brushes.Black;
    public FontWeight Weight { get; init; } = FontWeight.Normal;
    public global::Avalonia.Media.TextWrapping Wrap { get; init; } = global::Avalonia.Media.TextWrapping.Wrap;   // 生産物ハイライト等は NoWrap（改行なし）
}
public sealed class MapHoverInfo
{
    public string Title { get; init; } = "";
    /// <summary>題の名前の部分と括弧の部分（「ロンドン」と「（イギリス王国・…の属国）」）。窓の題は 2 つの塊として並べ、折り返すときは括弧の前で切る（語の途中で切らない）。</summary>
    public string TitleHead => Title.IndexOf('（') is > 0 and var i ? Title[..i] : Title;
    public string TitleNote => Title.IndexOf('（') is > 0 and var i ? Title[i..] : "";
    public List<MapHoverLine> Lines { get; } = new();
    public string ProducedGoods { get; set; } = "";   // 生産材（世界シェア上位）。窓「州」の左
    public string ImportedGoods { get; set; } = "";   // 輸入材（所有国の輸入額の世界シェア上位）。窓「州」の右
    // ★支配度 §7（州パネル・2026-09-07）：4区分の半円（SVG 記法→Conv.PathData）＋外周の帯（10年前の国家区分）＋凡例の値。HasRule=false＝描かない（区分が動かない世界）。
    public bool HasRule { get; init; }
    public string RuleState { get; init; } = "";
    public string RuleLocal { get; init; } = "";
    public string RuleForeign { get; init; } = "";
    public string RuleUngoverned { get; init; } = "";
    public string RuleBand { get; init; } = "";
    public string RuleStateText { get; init; } = "";
    public string RuleLocalText { get; init; } = "";
    public string RuleForeignText { get; init; } = "";
    public string RuleForeignWho { get; init; } = "";   // 外国勢力の持ち主（国名と持分）
    public string RuleUngovernedText { get; init; } = "";
    public string RuleBandText { get; init; } = "";
}

// 地図ラベル（海盆名・海峡/運河マーカー・ズーム時の州名）。座標は投影済み（Canvas.Left/Top = X/Y）。ツールチップ＝事実データ。
// 03地理 手順1：陸グラフの辺（地図レイヤ）。Start/End は投影済みキャンバス座標。
public sealed class MapEdgeLine
{
    public Avalonia.Point Start { get; init; }
    public Avalonia.Point End { get; init; }
    public double Thickness { get; init; } = 0.6;
    public IBrush Brush { get; init; } = Brushes.Gray;
}

public sealed class MapLabel
{
    public double X { get; init; }
    public double Y { get; init; }
    public string Text { get; init; } = "";
    public double FontSize { get; init; } = 14;
    public IBrush Brush { get; init; } = Brushes.Gray;
    public string? Tooltip { get; init; }   // null＝ツールチップなし（州名ラベル等。空文字だと空の吹き出し＝残骸が出る）
    public double LetterSpacing { get; init; }   // B3（2026-09-05）：国名を領土の広がりに沿わせる文字間隔（px・既定0）
}
