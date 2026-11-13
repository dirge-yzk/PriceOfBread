using System.Reflection;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>表示の層（PriceOfBread.Display）が SimCore から別アセンブリになったので、UnitTypeTests の型の反射の走査
/// （SimCore の型だけを数える）から外れた Display の型を、同じ見方で数える 2 本。専用の値型（Units.cs）の換算と年GDP比の口が表示の層に無いこと。</summary>
public class DisplayUnitTypeTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void 表示の層に型をまたぐ換算の口が無い()
    {
        var types = typeof(DisplayFormat).Assembly.GetTypes();
        Assert.NotEmpty(types);   // 対照：走査の母集合が空でない
        var offenders = new List<string>();
        foreach (var type in types)
            foreach (var m in type.GetMethods(All))
            {
                if (!UnitValues.IsUnit(m.ReturnType)) continue;
                bool crosses = m.GetParameters().Any(p => UnitValues.IsUnit(p.ParameterType) && p.ParameterType != m.ReturnType);
                if (crosses) offenders.Add($"{type.Name}.{m.Name}");
            }
        Assert.True(offenders.Count == 0, "型をまたぐ換算が表示の層にある: " + string.Join(", ", offenders));
    }

    [Fact]
    public void 表示の層に年GDP比を作る口が無い()
    {
        var makers = new List<string>();
        foreach (var type in typeof(DisplayFormat).Assembly.GetTypes())
            foreach (var m in type.GetMethods(All))
            {
                if (m.ReturnType != typeof(RatioToGdpYear)) continue;
                if (m.IsSpecialName && m.Name.StartsWith("get_", StringComparison.Ordinal)) continue;
                makers.Add($"{type.Name}.{m.Name}");
            }
        Assert.True(makers.Count == 0, "年GDP比を作る口が表示の層にある: " + string.Join(", ", makers));
    }
}
