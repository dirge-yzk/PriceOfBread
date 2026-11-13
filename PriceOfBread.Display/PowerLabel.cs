namespace PriceOfBread.SimCore.Display;

/// <summary>画面に出す国の名の取り方の 1 本。同じ国が欄ごとに別の名（データの名・国号つきの名・頭書きだけの名）で出ないよう、
/// 国の一覧と同じ表（<see cref="SecessionNames.DisplayName"/>・政体は権力の取り分から導いた政体）から作る。データの名（<see cref="Power.Name"/>）は変えない。読むだけ。</summary>
public static class PowerLabel
{
    public static string Of(Power p, IReadOnlyList<War> wars)
        => SecessionNames.DisplayName(p, p.Nation is { } n ? PowerAffinity.PolityFromShares(n) : null, wars);
}
