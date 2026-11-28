using System.Collections;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1256) 計器のフックを表ごとに閉じる：選んだ表だけを出す走りの表のバイトが、全表の走りの同じ表とバイトで一致する（共有する状態を持つ組は 1 つの bool に束ねてある）。
/// 〈選ばない表の計器の状態が空のまま〉も数える（入口の閉じを外すと、選ばない走りの累積が全表の走りと同じ件数まで増えて赤になる）。
/// 期待値（全表の走り）は閉じを通らない同じ盤面の別の走りから作る＝同語反復にしない。
/// </summary>
[Collection("Instruments")]
public class InstrumentHookClosureTests
{
    private const int Quarters = 10;

    private static MultiNationSim Make()
    {
        var uk = NationConfig.Britain().ShallowClone(); uk.OpeningStateMonopolyTier = 0;
        NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()];
        return new MultiNationSim(cfgs, 1789UL, _ => new UtilityAgent());
    }

    /// <summary>計器の static 欄のうち、辞書・ConditionalWeakTable・リストの合計の件数（選ばない表の累積が溜まっていないかの数え）。</summary>
    internal static Dictionary<string, int> FieldCounts()
    {
        var d = new Dictionary<string, int>();
        foreach (var f in typeof(Instruments).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (f.IsLiteral) continue;
            var v = f.GetValue(null);
            if (v is null || v is string) continue;
            int n = 0;
            var t = v.GetType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Runtime.CompilerServices.ConditionalWeakTable<,>)) continue;   // (1322) ConditionalWeakTable は件数の増えでなく〈基準に無かった鍵の数〉で数える（CwtKeys）
            if (v is ICollection c) n = c.Count;
            if (n > 0) d[f.Name] = n;
        }
        return d;
    }

    /// <summary>(1322) ConditionalWeakTable の欄ごとの今ある鍵の集合（参照で比べる）。前の走りの州・国が走りの途中で回収されて件数の増えが 0 に見える（試験の順に依る誤りの赤）を避けるため、走りの前の鍵の集合を控え、走りの後に〈控えに無かった鍵の数〉を数える。</summary>
    private static Dictionary<string, HashSet<object>> CwtKeys()
    {
        var d = new Dictionary<string, HashSet<object>>();
        foreach (var f in typeof(Instruments).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (f.IsLiteral) continue;
            var v = f.GetValue(null);
            if (v is null) continue;
            var t = v.GetType();
            if (!(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Runtime.CompilerServices.ConditionalWeakTable<,>))) continue;
            var set = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var kv in (IEnumerable)v) { var k = kv.GetType().GetProperty("Key")!.GetValue(kv); if (k is not null) set.Add(k); }
            d[f.Name] = set;
        }
        return d;
    }

    internal sealed record RunOut(string Dir, int State, Dictionary<string, int> Fields);

    internal static RunOut Run(string? tablesCsv)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ih_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables(tablesCsv);
        Instruments.Configure(dir, "*");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var baseFields = FieldCounts(); var baseKeys = CwtKeys();
        var sim = Make();
        try
        {
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            for (int q = 0; q < Quarters; q++)
            {
                if (q == 2)
                {
                    var d = sim.Diplomacy;
                    var fr = d.Powers.Find(p => p.BaseName == "フランス"); var pr = d.Powers.Find(p => p.BaseName == "プロイセン");
                    Assert.True(fr is not null && pr is not null, "前提が崩れた：仏かプロイセンの力が無い");
                    d.DeclareWar(fr!, pr!, sim.Coef);
                }
                sim.StepQuarter();
                Instruments.Quarter(sim, 1789UL, q);
            }
            var fields = FieldCounts().Where(kv => kv.Value > baseFields.GetValueOrDefault(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value - baseFields.GetValueOrDefault(kv.Key));
            foreach (var kv in CwtKeys())
            {
                int added = kv.Value.Count(k => !baseKeys.GetValueOrDefault(kv.Key, new HashSet<object>()).Contains(k));
                if (added > 0) fields[kv.Key] = added;
            }
            int state = fields.Values.Sum();
            Instruments.Close(sim);
            return new RunOut(dir, state, fields);
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); }
    }

    private static readonly Lazy<RunOut> Full = new(() => Run(null));

    private static byte[] Bytes(string p)
    {
        using var fs = new System.IO.FileStream(p, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        using var ms = new System.IO.MemoryStream(); fs.CopyTo(ms); return ms.ToArray();
    }

    private static string FirstDiff(string a, string b)
    {
        var la = System.Text.Encoding.UTF8.GetString(Bytes(a)).Split((char)10); var lb = System.Text.Encoding.UTF8.GetString(Bytes(b)).Split((char)10);
        for (int i = 0; i < Math.Min(la.Length, lb.Length); i++)
            if (la[i] != lb[i])
            {
                var h = la[0].Split((char)9); var x = la[i].Split((char)9); var y = lb[i].Split((char)9);
                var cols = new List<string>();
                for (int c = 0; c < Math.Min(x.Length, y.Length) && cols.Count < 6; c++) if (x[c] != y[c]) cols.Add($"{(c < h.Length ? h[c] : c.ToString())}:{x[c]}→{y[c]}");
                return $"最初の違い={i}行目 列[{string.Join(" ", cols)}]";
            }
        return $"行数 全表={la.Length} 選び={lb.Length}";
    }

    private static string Path(RunOut r, string table) => System.IO.Path.Combine(r.Dir, table + "1789.tsv");

    [Fact]
    public void FullRunWritesManyTables()
    {
        int n = System.IO.Directory.GetFiles(Full.Value.Dir, "*_s1789.tsv").Length;
        Assert.True(n >= 90, $"対照：全表の走りが出した表が {n} 本（90 未満）＝比べる母集合が痩せている");
        Assert.True(Full.Value.State > 0, "対照：全表の走りの計器の状態が空");
    }

    // 組＝共有する状態を持つ file の束（束ごとに 1 つの bool）。表の名は書く表（Open の鍵）。
    private static readonly HashSet<string> NoOutputInThisRun = ["BuyShadow/Nationgood", "BuyShadow/Recipeout", "Lwk/Rdh", "BuyShadow/Tradeskip", "BuyShadow/Sellr", "Cede/Impose"];

    [Theory]
        [InlineData("Basisx/Basisx", "basisx_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Basisx/Corncf", "corncf_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Basisx/Cornint", "cornint_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Basisx/Landsale", "landsale_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Basisx/Landshare", "landshare_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Basisx/Lordstat", "lordstat_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/BuyShadow", "s4cost_s,s7staple_s,t1buy_s,tb_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/MineCap", "minecap_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/Nationgood", "nationgood_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/Prodcap", "prodcap_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/Recipeout", "recipeout_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/Sellr", "sellr_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("BuyShadow/Tradeskip", "tradeskip_s,tskpair_s,tskshadow_s,tsreg_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Cede", "cederule_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Center", "center_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Cpeace", "cpeace_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Cpmed", "cpmed_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Wexit", "wexit_s")]   // (1399) 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Dep", "dep_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Impose", "imp_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/PopPref", "poppref_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Rule", "rule_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Cede/Unif", "formation2_s,tensionwhy_s,unifwar_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Lwk/Lwk", "lwk_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Lwk/Rdh", "rdh_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("PopClose/PopClose", "popclose_s")]   // (1261) 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Mintcap/Mintcap", "mintcap_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Mintcap/Mintreg", "mintreg_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Portch/Portch", "portch_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Portch/Portlm", "portlm_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Pxstep/Pxstep", "pxstep_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Pxstep/Scope", "csc_s,cscx_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Rk/Rk", "reach_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Rk/Rponset", "rponset_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Rk/RulePower", "reach_s,rponset_s,rulepeace_s,rulepower_s,rulepoweralloc_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Rk/Score", "score_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Silverst/Silverst", "silverst_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("Silverst/SilverWeight", "silverwt_s")]   // 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("War/Wars", "wars_s")]   // (1268) 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("War/Invade", "invade_s")]   // (1268) 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("War/Warpair", "warpair_s")]   // (1268) 束の中の 1 file だけを選ぶ（共有する状態を持つ組）
        [InlineData("SMALL", "nation_s,stock_s,wars_s,casualty_s,region_s,migration_s,pxmonth_s,scalars_s,aib_s,canal_s,class_s,disc_s,engineer_s,epi_s,goods_s,ideology_s,idn_s,invade_s,labor_s,monai_s,mongood_s,monlaw_s,pclamp_s,pxgoods_s,lawgate_s,reform_s,sbu_s,sf_s,fiscal_s")]   // ident.cfg の SMALL（計器つき 10 種の標準の読み手が読む 29 表）
        [InlineData("Admin", "admrule_s")]
        [InlineData("AiBuild", "aib_s")]
        [InlineData("Anchor", "anchor_s")]
        [InlineData("Arms", "armsfill_s")]
        [InlineData("Basisx", "basisx_s,cearn_s,cgrp_s,corncf_s,cornint_s,cpop_s,landbase_s,landbasereg_s,landfix_s,landsale_s,landshare_s,lordstat_s")]
        [InlineData("BuyShadow", "minecap_s,nationgood_s,prodcap_s,recipeout_s,s4cost_s,s7staple_s,sellr_s,t1buy_s,tb_s,tradeskip_s,trbin_s,trnation_s,trrail_s,trroute_s,trstat_s,tskpair_s,tskshadow_s,tsreg_s")]
        [InlineData("Cede", "cederule_s,center_s,cpeace_s,cpmed_s,crownpc_s,dep_s,formation2_s,ideology_s,imp_s,monai_s,mongood_s,monlaw_s,parlseat_s,poppref_s,rule_s,tensionwhy_s,unifwar_s,warband_s,warentry_s,warpair_s,wexit_s")]
        [InlineData("Cess", "cess_s,cessp_s")]
        [InlineData("Chem", "chem_s,chemgood_s")]
        [InlineData("Civwar", "cwmult_s")]
        [InlineData("Clock", "clock_s")]
        [InlineData("Cpwd", "cpwd_s,cpwdft_s,cpworld_s")]
        [InlineData("Dipcool", "dipcool_s")]
        [InlineData("Discontent", "dcont_s")]
        [InlineData("Divhost", "divhost_s")]
        [InlineData("Ed", "ed_s")]
        [InlineData("Equipment", "eq_s,eqr_s")]
        [InlineData("Fb", "fb_s")]
        [InlineData("Fd", "fd_s")]
        [InlineData("House", "house_s")]
        [InlineData("Idn", "idn_s")]
        [InlineData("Ivf", "ivf_s")]
        [InlineData("LawGate", "lawgate_s")]
        [InlineData("Lw", "labor_s")]
        [InlineData("Lwk", "age_s,lwk_s,pop_s,rdh_s,urx_s,urxpd_s,urxrt_s")]
        [InlineData("Mintcap", "mintcap_s,mintreg_s")]
        [InlineData("Pay", "pay_s")]
        [InlineData("PopClose", "popclose_s")]
        [InlineData("Paysale", "paysale2_s")]
        [InlineData("Portch", "portch_s,portlm_s")]
        [InlineData("Prdf", "prdf_s")]
        [InlineData("Facfloor2", "facfloor2_s")]   // (1435)
        [InlineData("Prds", "prds_s")]
        [InlineData("Pxstep", "csc_s,cscx_s,pxstep_s")]
        [InlineData("RailSpend", "railspend_s")]
        [InlineData("Rk", "nation_s,reach_s,rponset_s,rulepeace_s,rulepower_s,rulepoweralloc_s,score_s")]
        [InlineData("SatNeeds", "satneeds_s")]
        [InlineData("Sb", "sbr_s,sbrreg_s,sbu_s")]
        [InlineData("Scalars", "scalars_s")]
        [InlineData("Sf", "sf_s")]
        [InlineData("SilverParity", "silverpar_s")]
        [InlineData("Silverst", "silverst_s,silverwt_s")]
        [InlineData("SoldierVac", "soldiervac_s")]
        [InlineData("Subst", "subst_s")]
        [InlineData("TreatyPort", "tport_s")]
        [InlineData("U4", "consf_s")]
        [InlineData("Wp", "wp_s")]
        [InlineData("War", "invade_s,warband_s,warentry_s,warpair_s,warpolity_s,wars_s")]   // (1268) 束
        [InlineData("Habitlg", "habitlg_s")]   // (1268) 束
        [InlineData("Capskip", "capskip_s")]   // (1337) 束（(1329) c139sm で足した Tg.Capskip の群）
    public void SelectedGroupIsByteIdenticalToFullRun(string group, string csv)
    {
        var full = Full.Value;
        var sel = Run(csv);
        int compared = 0;
        foreach (var t in csv.Split(','))
        {
            string a = Path(full, t), b = Path(sel, t);
            Assert.True(System.IO.File.Exists(a) == System.IO.File.Exists(b), $"{group}/{t}：出力の有無が全表の走りと違う");
            if (!System.IO.File.Exists(a)) continue;
            if (t is "clock_s" or "burscan_s") { compared++; continue; }   // 走査の時計（Stopwatch の刻み）を書く表（clock_s・burscan_s）＝バイトは走るたびに揺れる（出力の有無だけ比べる）
            Assert.True(Bytes(a).AsSpan().SequenceEqual(Bytes(b)), $"{group}/{t}：選んだ表のバイトが全表の走りと一致しない {FirstDiff(a, b)}");
            compared++;
        }
        // 出力が両方とも無いまま緑になる群＝この走り（4 国・種 1789）では表が出ない群（(1262) c129sb で数えた 6 群）。ここに無い群で比べた表が 0 本なら赤＝新しい空振りを見逃さない。
        if (!NoOutputInThisRun.Contains(group)) Assert.True(compared > 0, $"{group}：対照＝比べた表が 0 本（出力が両方とも無いまま緑になる）");
        else Assert.True(compared == 0, $"{group}：表が出るようになった＝NoOutputInThisRun の名簿から外す");
        Assert.True(sel.State <= full.State, $"{group}：選ばない表の積算が全表の走りより多い");
        try { System.IO.Directory.Delete(sel.Dir, true); } catch { }
    }

    // 閉じた file の累積の置き場（辞書・ConditionalWeakTable）。nation_s だけを選ぶ走りで空のまま、全表の走りでは溜まる。
    private static readonly string[] ClosedAccs = ["AibAccs", "EqAccs", "LwSteps", "SbAccs", "NgAccs", "PxAccs", "CscAccs", "PopCloseAccs", "SoldierVacAccs", "IvfAccs", "MintregAccs", "MintcapAccs", "SatNeedsAccs", "UnifStates", "RpStates", "SubstSinks", "DivhostAccs", "WarSts", "EdDiags", "CpIterFlags", "CpStates", "HabitlgAccs", "NgEvs", "PxSinks", "CmStates", "BxNats", "BxRegs", "CkSinks", "CkWorlds", "CrownPrevs", "CrownSinks", "CrownSrcs", "FdDiags", "HabitlgSinks"];   // (1322) 後ろの 5 つは (1268) で入口を閉じたのに置き場の名簿が無かった物（CpIter・CpMonth・HabitLgStep・NgRegionMoved・PxstepTable の閉じを外すと赤になる）。(1329) CpStates は CpMonth に加えて CpBefore・CmBefore の入口も閉じた（Cpmed・Dep・Impose・War の読み口は各自の Tg の門の内）＝擬似の欄 CpStates.Month をやめて本物の置き場で数える

    /// <summary>(1322) 四半期の末に表を書いて置き場を空にする物（全表の走りの末でも 0＝〈全表で溜まる〉の対照は取れない）。選ばない走りで溜まっていないことだけを見る。外すと入口の閉じ（HabitLgStep）を外したとき赤になる。</summary>
    private static readonly string[] ClearedAtQuarterEnd = ["HabitlgAccs"];

    [Fact]
    public void UnselectedTablesAccumulateNothing()
    {
        var full = Full.Value;
        var sel = Run("nation_s");
        var missing = ClosedAccs.Where(f => !ClearedAtQuarterEnd.Contains(f) && full.Fields.GetValueOrDefault(f) == 0).ToList();
        Assert.True(missing.Count == 0, "対照：全表の走りで溜まらない置き場がある " + string.Join(",", missing));
        var leaked = ClosedAccs.Where(f => sel.Fields.GetValueOrDefault(f) > 0).ToList();
        Assert.True(leaked.Count == 0, "選ばない表の積算が溜まっている（入口の閉じが効いていない）：" + string.Join(",", leaked));
        try { System.IO.Directory.Delete(sel.Dir, true); } catch { }
    }

    /// <summary>(1322) 置き場を数える：nation_s だけを選ぶ走りでも溜まる置き場は、名簿（OpenStores＝入口を閉じていない置き場）か ClosedAccs に入っていなければ赤。
    /// 新しい置き場が閉じられないまま足されたら赤＝閉じる（ClosedAccs へ）か、閉じない理由を書いて名簿へ足す（走る試験の順で現れる置き場もあるため、名簿の物が現れないことは赤にしない）。</summary>
    private static readonly string[] OpenStores = ["ConAccs", "Dips", "Flows", "InheritBorns", "InheritOwnStarts", "InheritRegs", "InheritSinks", "Inps", "LandbaseSinks", "Lawgate2Table", "LbAdjs", "LbFrozen", "LbNationHist", "LbNats", "LbRegionHist", "MerchBufs", "MerchSinks", "MerchSteps", "MonSinks", "MonWs", "PopOwner", "PortAccs", "RailAccs", "Sinks", "StartGates", "States", "TrAccs", "UrxPpStates", "UrxRtStates", "UrxStates", "WarPushes", "AreaCache", "_monLawNames"];   // (1329) 今ある 33 の置き場（(1322) の 44 から CpStates・CmStates・BxNats・BxRegs・CkSinks・CkWorlds・CrownPrevs・CrownSinks・CrownSrcs・FdDiags・HabitlgSinks の 11 本を閉じた）＝入口を閉じていない置き場の名簿。新しい置き場が足されると赤・閉じたら名簿から外す。
    // 閉じていない理由（1 行）：Sinks・States・PopOwner・Flows・StartGates・Dips・ConAccs・PortAccs・RailAccs・TrAccs＝nation_s ほか全ての表が共有する土台の置き場（閉じる＝計器の土台の作り直し）／Inherit*・Landbase 系（LandbaseSinks・Lb*）・Merch*・Mon*・Urx*・Inps・Lawgate2Table・WarPushes＝表ごとの入口の門（Tg）がまだ無い（00一覧 0 の① に残す）／AreaCache・_monLawNames＝この走りでは現れない（走る試験の順で現れる）

    [Fact]
    public void OpenStoresAreExactlyTheNamedOnes()
    {
        var sel = Run("nation_s");
        var unnamed = sel.Fields.Keys.Where(f => !ClosedAccs.Contains(f) && !OpenStores.Contains(f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
        Assert.True(unnamed.Count == 0, "nation_s だけの走りで溜まる置き場が名簿に無い（閉じるか名簿へ足す）：" + string.Join(",", unnamed));
        try { System.IO.Directory.Delete(sel.Dir, true); } catch { }
    }

    /// <summary>(1322) 入口を直に呼ぶ番人：HabitLgStep（habitlg_s）と NgRegionMoved（nationgood_s）は、走りでは置き場が四半期の末に空になる・州の移りが無い、のため件数では閉じの外れを拾えない
    /// （突然変異で外しても全表の走りの比べが緑だった）。表を選ばないで呼んだ州の鍵が置き場に残らないことを見る（対照：選ぶと鍵が入る）。</summary>
    [Fact]
    public void DirectEntriesLeaveNoKeyWhenTheirTableIsNotSelected()
    {
        var sim = Make(); var w = sim.Nations[0].World; var r = w.Regions[0];
        Assert.NotNull(w.Diplomacy);
        bool Has(string field, object key)
        {
            var v = typeof(Instruments).GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(null)!;
            foreach (var kv in (IEnumerable)v) if (ReferenceEquals(kv.GetType().GetProperty("Key")!.GetValue(kv), key)) return true;
            return false;
        }
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ihd_" + Guid.NewGuid().ToString("N"));
        try
        {
            Instruments.SelectTables("nation_s"); Instruments.Configure(dir, "*");
            Instruments.HabitLgStep(r, w.Coef, 1.0); Instruments.NgRegionMoved(w, r);
            Assert.False(Has("HabitlgAccs", r), "habitlg_s を選ばないのに HabitLgStep が置き場へ積んだ（入口の閉じが外れている）");
            Assert.False(Has("NgEvs", w.Diplomacy!), "nationgood_s を選ばないのに NgRegionMoved が置き場へ積んだ（入口の閉じが外れている）");
            Instruments.Configure(null, null);
            Instruments.SelectTables("habitlg_s,nationgood_s"); Instruments.Configure(dir, "*");
            Instruments.HabitLgStep(r, w.Coef, 1.0); Instruments.NgRegionMoved(w, r);
            Assert.True(Has("HabitlgAccs", r), "対照：habitlg_s を選ぶと HabitLgStep が積まない（試験の前提が崩れた）");
            Assert.True(Has("NgEvs", w.Diplomacy!), "対照：nationgood_s を選ぶと NgRegionMoved が積まない（試験の前提が崩れた）");
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(1337) c140nt 入口を直に呼ぶ番人：Tg.Capskip の 3 つの門（CapSkip・CapSkipDedup・CapskipTable）は、4 国・種 1789 の走りでは容量で飛ばした売り手が出ず、件数では外れを拾えない
    /// （(1329) の突然変異 M5 は CapSkip と CapSkipDedup の門を一緒に外しただけで、Dedup だけ・CapskipTable だけは未確認だった）。表を選ばないで呼んだ国・シムの鍵が置き場に残らないことを見る（対照：選ぶと鍵が入る）。</summary>
    [Fact]
    public void CapskipDirectEntriesLeaveNoKeyWhenTheirTableIsNotSelected()
    {
        var sim = Make(); var w = sim.Nations[0].World; var r = w.Regions[0];
        var table = typeof(Instruments).GetMethod("CapskipTable", BindingFlags.Static | BindingFlags.NonPublic)!;
        bool Has(string field, object key)
        {
            var v = typeof(Instruments).GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(null)!;
            foreach (var kv in (IEnumerable)v) if (ReferenceEquals(kv.GetType().GetProperty("Key")!.GetValue(kv), key)) return true;
            return false;
        }
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ihc_" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1) CapSkipDedup だけ：CkWorlds に国の鍵が入るか
            Instruments.SelectTables("nation_s"); Instruments.Configure(dir, "*");
            Instruments.CapSkipDedup(w, 0, 1.0, 1.0);
            Assert.False(Has("CkWorlds", w), "capskip_s を選ばないのに CapSkipDedup が置き場へ積んだ（Dedup の入口の閉じが外れている）");
            Instruments.Configure(null, null);
            Instruments.SelectTables("capskip_s"); Instruments.Configure(dir, "*");
            Instruments.CapSkipDedup(w, 0, 1.0, 1.0);
            Assert.True(Has("CkWorlds", w), "対照：capskip_s を選ぶと CapSkipDedup が積まない（試験の前提が崩れた）");
            Instruments.Configure(null, null);
            // 2) CapSkip だけ（別の国で）：Dedup の門を通らない入口
            var w2 = sim.Nations[1].World; var r2 = w2.Regions[0];
            Instruments.SelectTables("nation_s"); Instruments.Configure(dir, "*");
            Instruments.CapSkip(w2, 0, r2.Markets[0], r2.Markets[0], r2, 1.0, 1.0, true, 0.0);
            Assert.False(Has("CkWorlds", w2), "capskip_s を選ばないのに CapSkip が置き場へ積んだ（CapSkip の入口の閉じが外れている）");
            Instruments.Configure(null, null);
            // 3) CapskipTable だけ：CkSinks にシムの鍵が入るか
            Instruments.SelectTables("nation_s"); Instruments.Configure(dir, "*");
            table.Invoke(null, [sim, 1789UL, 0]);
            Assert.False(Has("CkSinks", sim), "capskip_s を選ばないのに CapskipTable が置き場へ積んだ（Table の入口の閉じが外れている）");
            Instruments.Configure(null, null);
            Instruments.SelectTables("capskip_s"); Instruments.Configure(dir, "*");
            table.Invoke(null, [sim, 1789UL, 0]);
            Assert.True(Has("CkSinks", sim), "対照：capskip_s を選ぶと CapskipTable が積まない（試験の前提が崩れた）");
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(1322) Nationgood 群（nationgood_s）は 4 国・種 1789 の走りでは年の間隔（NgEveryYears＝10）のため出力が無い＝NoOutputInThisRun で比べられなかった。間隔を 1 年にして、出力が出る走りで全表の走りと選んだ表のバイトを比べる。</summary>
    [Fact]
    public void NationgoodGroupWithOutputIsByteIdenticalToFullRun()
    {
        int saved = Instruments.NgEveryYears;
        Instruments.NgEveryYears = 1;
        try
        {
            var full = Run(null); var sel = Run("nationgood_s");
            string a = Path(full, "nationgood_s"), b = Path(sel, "nationgood_s");
            Assert.True(System.IO.File.Exists(a) && System.IO.File.Exists(b), "対照：nationgood_s が出ない走り（間隔を 1 年にしても出力が無い）");
            Assert.True(new System.IO.FileInfo(a).Length > 200, "対照：nationgood_s が見出しだけ");
            Assert.True(Bytes(a).AsSpan().SequenceEqual(Bytes(b)), "nationgood_s：選んだ表のバイトが全表の走りと一致しない " + FirstDiff(a, b));
            Assert.True(sel.State <= full.State, "選ばない表の積算が全表の走りより多い");
            try { System.IO.Directory.Delete(full.Dir, true); System.IO.Directory.Delete(sel.Dir, true); } catch { }
        }
        finally { Instruments.NgEveryYears = saved; }
    }

    /// <summary>全ての表を 1 本ずつ単独で選ぶ（環境変数 POB_IH_EXHAUSTIVE=1 のときだけ・約 5 分）。束に入れ損ねた共有する状態をここで拾う。</summary>
    /// <summary>環境変数 POB_IH_EXHAUSTIVE=1 が無いときは Skip（(1262) c129sb：以前は黙って return して緑に数えていた＝走っていないのに通ったと読めた）。</summary>
    private sealed class FactIfExhaustiveAttribute : FactAttribute
    {
        public FactIfExhaustiveAttribute()
        {
            if (System.Environment.GetEnvironmentVariable("POB_IH_EXHAUSTIVE") != "1") Skip = "POB_IH_EXHAUSTIVE=1 のときだけ走る（約 5 分）";
        }
    }

    [FactIfExhaustive]
    public void EveryTableAloneIsByteIdenticalToFullRun()
    {
        var full = Full.Value;
        var names = System.IO.Directory.GetFiles(full.Dir, "*_s1789.tsv").Select(f => System.IO.Path.GetFileName(f)[..^"1789.tsv".Length]).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var bad = new List<string>();
        foreach (var t in names)
        {
            if (t is "clock_s" or "burscan_s") continue;
            var sel = Run(t);
            string a = Path(full, t), b = Path(sel, t);
            if (!System.IO.File.Exists(b) || !Bytes(a).AsSpan().SequenceEqual(Bytes(b))) bad.Add(t + " " + (System.IO.File.Exists(b) ? FirstDiff(a, b) : "出力なし"));
            try { System.IO.Directory.Delete(sel.Dir, true); } catch { }
        }
        Assert.True(bad.Count == 0, "単独で選ぶと全表の走りと違う表：" + string.Join(" | ", bad));
    }
}
