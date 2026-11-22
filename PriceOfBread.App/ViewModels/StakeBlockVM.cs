using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

/// <summary>選んだ法の欄の階級 1 行（絵・名前・賛成寄りは「賛成 n%」・反対寄りは「反対 n%」）。</summary>
public sealed record StakeRowVM(string Name, string Text, IImage? Icon);

/// <summary>一歩（実際の法の段の名「A→B」）への階級の賛否を、賛成寄りと反対寄りの左右に分けた 1 組。
/// 「賛成 n%」＝その階級の中で賛成する割合（<see cref="LawStakes.Sides"/>）。</summary>
public sealed record StakeBlockVM(string Caption, IReadOnlyList<StakeRowVM> Pro, IReadOnlyList<StakeRowVM> Con)
{
    public bool HasPro => Pro.Count > 0;
    public bool HasCon => Con.Count > 0;

    public static StakeBlockVM From(StakeSides s) => new(s.Caption, s.Pro.Select(c => Row(c, true)).ToList(), s.Con.Select(c => Row(c, false)).ToList());

    // 賛成寄りの側は「賛成 n%」・反対寄りの側は「反対 n%」（n＝その階級の反対の割合＝100−賛成・作者の指示 2026-11-23）
    private static StakeRowVM Row(ClassStance c, bool pro)
    {
        int forPct = (int)System.Math.Round(c.ForShare * 100.0, System.MidpointRounding.AwayFromZero);
        return new(c.Name, (pro ? "賛成 " + forPct : "反対 " + (100 - forPct)).ToString(CultureInfo.InvariantCulture) + "%", ClassIcons.Of(c.ClassId));
    }
}
