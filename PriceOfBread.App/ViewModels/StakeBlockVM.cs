using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

/// <summary>選んだ法の欄の階級 1 行（絵・名前・「賛成 n%」）。</summary>
public sealed record StakeRowVM(string Name, string Text, IImage? Icon);

/// <summary>一歩（実際の法の段の名「A→B」）への階級の賛否を、賛成寄りと反対寄りの左右に分けた 1 組。
/// 「賛成 n%」＝その階級の中で賛成する割合（<see cref="LawStakes.Sides"/>）。</summary>
public sealed record StakeBlockVM(string Caption, IReadOnlyList<StakeRowVM> Pro, IReadOnlyList<StakeRowVM> Con)
{
    public bool HasPro => Pro.Count > 0;
    public bool HasCon => Con.Count > 0;

    public static StakeBlockVM From(StakeSides s) => new(s.Caption, s.Pro.Select(Row).ToList(), s.Con.Select(Row).ToList());

    private static StakeRowVM Row(ClassStance c)
        => new(c.Name, "賛成 " + ((int)System.Math.Round(c.ForShare * 100.0, System.MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "%", ClassIcons.Of(c.ClassId));
}
