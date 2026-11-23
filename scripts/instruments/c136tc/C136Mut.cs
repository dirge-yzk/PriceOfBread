// (1310) c136tc 突然変異の実の走り（使い捨ての木で回す・コミットは台本だけ）：実の試験の本体 PolicyPoliticsTests.TaxChainCheck に、試験の中の w.Coef の突然変異を渡して赤か緑かを書き出す。
using PriceOfBread.SimCore;
using Xunit;
namespace PriceOfBread.Tests;

static class C136M
{
    static readonly string Out = Environment.GetEnvironmentVariable("C136_OUT") ?? "D:/PoB_work/c136tc_work/out";
    static readonly object Lock = new();
    public static void Go(string tag, double? iew, Action<Coefficients> mut)
    {
        string res;
        try { PolicyPoliticsTests.TaxChainCheck(iew, mut); res = "GREEN"; }
        catch (Xunit.Sdk.XunitException e) { res = "RED\t" + e.Message.Replace("\r", "").Replace("\n", " ").Substring(0, Math.Min(160, e.Message.Length)); }
        lock (Lock) { Directory.CreateDirectory(Out); File.AppendAllText($"{Out}/mutreal.tsv", $"{tag}\t{(iew is double d ? d.ToString() : "0.3")}\t{res}\n"); }
    }
}
public class C136M_M0a { [Fact] public void Go() => C136M.Go("M0", null, c => { }); }
public class C136M_M0b { [Fact] public void Go() => C136M.Go("M0", 0.0, c => { }); }
public class C136M_M1a { [Fact] public void Go() => C136M.Go("M1_WantWeight0", null, c => c.WantWeight = 0); }
public class C136M_M1b { [Fact] public void Go() => C136M.Go("M1_WantWeight0", 0.0, c => c.WantWeight = 0); }
public class C136M_M2a { [Fact] public void Go() => C136M.Go("M2_FamineGain0", null, c => c.FamineDriveGain = 0); }
public class C136M_M2b { [Fact] public void Go() => C136M.Go("M2_FamineGain0", 0.0, c => c.FamineDriveGain = 0); }
public class C136M_M3a { [Fact] public void Go() => C136M.Go("M3_LivingRefSpeed1", null, c => c.LivingRefSpeed = 1.0); }
public class C136M_M3b { [Fact] public void Go() => C136M.Go("M3_LivingRefSpeed1", 0.0, c => c.LivingRefSpeed = 1.0); }
public class C136M_M4a { [Fact] public void Go() => C136M.Go("M4_WantWeight_neg", null, c => c.WantWeight = -0.8); }
public class C136M_M4b { [Fact] public void Go() => C136M.Go("M4_WantWeight_neg", 0.0, c => c.WantWeight = -0.8); }
