using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1343) c141wd 旗 WarmupNoDecisions：助走の間（World.WarmupFrozen）は全ての国で四半期の決定の段
/// （AI の近代化の率 UtilityAgent.AutoModernizationFrac・プレイヤーの海軍の維持の配分 SetNavalUpkeepShare・INationAgent.DecideQuarter）を飛ばす。
/// 旗 0＝今の形（AI の国だけが助走の間も決め、NullAgent のプレイヤーは決めない＝非対称）。
/// 盤面は試験の中で置く（2 国・助走 2 四半期）。決定の欄＝NationPolicy の値型の公開の欄の全て＋World.ModernizationFrac＋World.Mobilized。
/// 呼ばれた回数は包んだ代理人が数える（エンジンの関数を呼ばない独立の数え手）。</summary>
public class WarmupDecisionSymmetryTests
{
    private const int WarmQ = 2;

    /// <summary>DecideQuarter の呼ばれた回数を数え、中身は包んだ代理人へ渡す。</summary>
    private sealed class CountingAgent(INationAgent inner) : INationAgent
    {
        public int Calls;
        public void DecideQuarter(World world) { Calls++; inner.DecideQuarter(world); }
    }

    private static readonly FieldInfo[] PolicyFields = typeof(NationPolicy)
        .GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Where(f => f.FieldType.IsValueType).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();

    /// <summary>決定の欄の写し（値型の欄は箱に入れた値・double はビットで比べる）。</summary>
    private static object?[] Snap(World w)
    {
        var v = new object?[PolicyFields.Length + 2];
        for (int i = 0; i < PolicyFields.Length; i++) v[i] = PolicyFields[i].GetValue(w.Policy);
        v[^2] = BitConverter.DoubleToInt64Bits(w.ModernizationFrac);
        v[^1] = w.Mobilized;
        return v;
    }

    private static string Name(int i) => i < PolicyFields.Length ? PolicyFields[i].Name : i == PolicyFields.Length ? "ModernizationFrac" : "Mobilized";

    private static int[] Diff(object?[] a, object?[] b)
        => Enumerable.Range(0, a.Length).Where(i => !Equals(a[i], b[i])).ToArray();

    /// <summary>国 0＝イギリス（数える代理人の中に UtilityAgent）・国 1＝フランス。player=true なら国 1 は NullAgent＋IsPlayer、false なら UtilityAgent。</summary>
    private static (MultiNationSim Sim, CountingAgent Ai, CountingAgent Other) Board(double flag, bool player)
    {
        CountingAgent? ai = null, other = null;
        var gb = WorldData.ByName("イギリス"); var fr = WorldData.ByName("フランス");
        var sim = new MultiNationSim([gb, fr], seed: 1789,
            cfg => ReferenceEquals(cfg, gb) ? (ai = new CountingAgent(new UtilityAgent()))
                                            : (other = new CountingAgent(player ? new NullAgent() : new UtilityAgent())));
        if (player) sim.Diplomacy.Powers[1].IsPlayer = true;
        foreach (var n in sim.Nations) { n.TelemetryEnabled = false; n.World.Coef.WarmupNoDecisions = flag; }   // 係数は国ごとの World.Coef が読まれる
        sim.Diplomacy.Log.Enabled = false;
        return (sim, ai!, other!);
    }

    /// <summary>T1：旗 1 では助走の間に決定の段が 1 度も走らない＝AI の国（UtilityAgent）もプレイヤー（NullAgent）も決定の欄が助走の前の値のまま。
    /// 対照：旗 0 では AI の国の DecideQuarter が助走の四半期ごとに呼ばれ、決定の欄の少なくとも 1 つが動く（盤面が決定の段を通ることの確かめ）。</summary>
    [Fact]
    public void T1_助走の間は全ての国で決定の欄が開幕の値のまま()
    {
        var (sim, ai, other) = Board(1.0, player: true);
        var a0 = Snap(sim.Nations[0].World); var p0 = Snap(sim.Nations[1].World);
        sim.WarmupEconomy(WarmQ);   // 助走の本体だけ（StartWarmup の後段の Diplomacy.OpenGame は陸海の維持費の比を全ての国に同じ関数で置く＝助走の決定ではない）
        Assert.Equal(0, ai.Calls);
        Assert.Equal(0, other.Calls);
        var da = Diff(a0, Snap(sim.Nations[0].World));
        var dp = Diff(p0, Snap(sim.Nations[1].World));
        Assert.True(da.Length == 0, $"旗 1 の助走で AI の国の決定の欄が動いた：{string.Join("・", da.Select(Name))}");
        Assert.True(dp.Length == 0, $"旗 1 の助走でプレイヤーの国の決定の欄が動いた：{string.Join("・", dp.Select(Name))}");

        var (s0, ai0, _) = Board(0.0, player: true);
        var b0 = Snap(s0.Nations[0].World);
        s0.WarmupEconomy(WarmQ);
        Assert.Equal(WarmQ, ai0.Calls);   // 対照：旗 0 は AI の国だけが助走の四半期ごとに決める
        var d0 = Diff(b0, Snap(s0.Nations[0].World));
        Assert.True(d0.Length >= 1, "対照：旗 0 の助走で AI の国の決定の欄が 1 つも動かない（盤面が決定の段を通っていない）");
        Assert.Contains(PolicyFields.Length, d0);   // 対照：旗 0 は AI の近代化の率を助走の間に書き換える（開幕の値 OpeningModernizationFrac から動く）
    }

    /// <summary>T2：助走の後の 1 四半期目は、旗に依らず決定の段が走る（数える代理人が 1 回・AI の近代化の率が開幕の値から動く）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T2_助走の後の最初の四半期は旗に依らず決める(double flag)
    {
        var (sim, ai, other) = Board(flag, player: false);
        sim.StartWarmup(null, WarmQ);
        int a = ai.Calls, o = other.Calls;
        var w0 = sim.Nations[0].World;
        long m0 = BitConverter.DoubleToInt64Bits(w0.ModernizationFrac);
        sim.StepQuarter();
        Assert.Equal(1, ai.Calls - a);
        Assert.Equal(1, other.Calls - o);
        Assert.NotEqual(m0, BitConverter.DoubleToInt64Bits(w0.ModernizationFrac));
    }

    /// <summary>T3：同じ国（フランス）を AI（UtilityAgent）で置いた盤面と、プレイヤー（NullAgent＋IsPlayer）で置いた盤面を、同じ種で助走させる。
    /// 旗 1 では開幕のフランスの決定の欄と名目 GDP がビットで同じ（同じ規則で開幕の値が置かれる）。旗 0 では違う（対照）。</summary>
    [Fact]
    public void T3_助走の後のAIとプレイヤーの開幕の値が同じ規則で置かれる()
    {
        (object?[] Ai, object?[] Pl, long GdpAi, long GdpPl) Run(double flag)
        {
            var (sa, _, _) = Board(flag, player: false);
            var (sp, _, _) = Board(flag, player: true);
            sa.StartWarmup(null, WarmQ); sp.StartWarmup(null, WarmQ);
            var wa = sa.Nations[1].World; var wp = sp.Nations[1].World;
            return (Snap(wa), Snap(wp), BitConverter.DoubleToInt64Bits(wa.NominalGdpPerMonth.Value), BitConverter.DoubleToInt64Bits(wp.NominalGdpPerMonth.Value));
        }
        var on = Run(1.0);
        var d1 = Diff(on.Ai, on.Pl);
        Assert.True(d1.Length == 0, $"旗 1：AI で置いたフランスとプレイヤーで置いたフランスの開幕の決定の欄が違う：{string.Join("・", d1.Select(Name))}");
        Assert.Equal(on.GdpAi, on.GdpPl);
        var off = Run(0.0);
        Assert.True(Diff(off.Ai, off.Pl).Length >= 1, "対照：旗 0 で AI とプレイヤーの開幕の決定の欄が同じ（盤面が非対称を出していない）");
    }
}
