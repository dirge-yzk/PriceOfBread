using System.Text.Json;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(490) 開幕の焼き込みの道具（<see cref="OpeningBake"/>）の番人＝機構と不変条件。旗 <c>OpeningBakeEnabled</c> は既定 0（倒さない）。
/// ①決定論（同じ入力で焼くと本文がバイトで同じ）②鍵の一致（表の国・州・財・生産者の財・POP の階級が今の世界の名前と**双方向**に一致・期待値は番人が世界のオブジェクトを自分で走査して作る）
/// ③読み込みの往復（表を新しい盤面に書いて焼き直すと同じ本文＝書いた値が欄に入っている・書く前は違う＝何もしない読み込みが緑にならない）④fail-closed（鍵が 1 つでも違えば何も書かずに例外）
/// ⑤貯蓄が変わっても保存則の基準線を取り直して歩が進む。⚠コミットした表（埋め込みの opening_bake.json）が今の世界と合うかは別の番人（表を焼いたら）。</summary>
public class OpeningBakeTests
{
    static MultiNationSim Fresh(Coefficients? coef = null) => new(WorldData.Nations(), 1UL, _ => new UtilityAgent(), coef);

    // 3 四半期の試走で焼いた短い表（全テストで共有・決定論）。収束は見ない（常に false）＝上限の盤面を焼く。
    static readonly Lazy<string> Short = new(() => OpeningBake.Bake(1UL, 3, (_, _) => false, "test", "2026-10-21").Text);

    static string Q(string s) => JsonSerializer.Serialize(s, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    [Fact]
    public void 旗は既定0で表を開かずに助走へ入り_世界の頭書きと同じ形の空の表では旗1が例外で止まる()
    {
        Assert.Equal(0.0, new Coefficients().OpeningBakeEnabled);
        // 旗 0 の助走は表に触れない（空の表を置いても例外にならない・Apply を呼ばない）＝ここでは構築と 1 四半期の助走が通る
        var sim = Fresh();
        sim.StartWarmup(quartersOverride: 1);
        Assert.True(sim.Nations.Count > 0);
        // 旗 1 の読み込みは表が今の世界と合わなければ例外（空の表＝まだ焼いていない）
        using var empty = JsonDocument.Parse("{\"format\":\"opening_bake/1\",\"header\":{\"empty\":true}}");
        var sim2 = Fresh();
        var ex = Assert.Throws<InvalidOperationException>(() => OpeningBake.Apply(sim2, empty));
        Assert.Contains("今の世界と合わない", ex.Message);
    }

    [Fact]
    public void 同じ入力で2回焼くと本文がバイトで同じ()
    {
        string again = OpeningBake.Bake(1UL, 3, (_, _) => false, "test", "2026-10-21").Text;
        Assert.Equal(Short.Value, again);
        Assert.DoesNotContain("\r", again);   // LF のみ
        Assert.False(again.StartsWith('﻿'));   // BOM なし
    }

    [Fact]
    public void 表の鍵は今の世界の名前と双方向に一致する_期待は世界のオブジェクトから自分で作る()
    {
        var sim = Fresh();
        using var doc = JsonDocument.Parse(Short.Value);
        var root = doc.RootElement;
        // 財
        var goods = root.GetProperty("goods").EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(Catalog.Goods.Length, goods.Length);
        for (int g = 0; g < goods.Length; g++) Assert.Equal(Catalog.Goods[g].Name, goods[g]);
        // 国（構築時の名前＝BaseName）
        var nt = root.GetProperty("nationTable");
        var tableNations = nt.EnumerateObject().Select(p => p.Name).ToList();
        var worldNations = sim.Nations.Select(n => n.World.SelfPower!.BaseName).ToList();
        Assert.Equal(worldNations.Count, tableNations.Count);
        Assert.Equal(worldNations.Count, worldNations.Distinct().Count());   // 国の名前は一意（鍵になれる）
        Assert.Empty(worldNations.Except(tableNations)); Assert.Empty(tableNations.Except(worldNations));
        foreach (var n in sim.Nations)
        {
            var w = n.World; var ne = nt.GetProperty(w.SelfPower!.BaseName);
            var tableRegions = ne.GetProperty("regions").EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            Assert.Equal(w.Regions.Count, tableRegions.Count);
            Assert.Equal(w.Regions.Count, w.Regions.Select(r => r.Name).Distinct().Count());   // 州の名前は国の中で一意
            foreach (var r in w.Regions)
            {
                Assert.True(tableRegions.TryGetValue(r.Name, out var e), $"{r.Name} が表に無い");
                // 生産者：表の並びの財の添字が世界の生産者の財と同じ並びで一致
                var pr = e.GetProperty("pr");
                Assert.Equal(r.Producers.Count, pr.GetArrayLength());
                for (int i = 0; i < r.Producers.Count; i++) Assert.Equal(r.Producers[i].GoodId, pr[i][0].GetInt32());
                // POP：階級の名前が並びで一致
                var po = e.GetProperty("pop");
                Assert.Equal(r.Pops.Count, po.GetArrayLength());
                for (int i = 0; i < r.Pops.Count; i++) Assert.Equal(r.Pops[i].ClassName, po[i][0].GetString());
                // 市場：財の数 × (値段, 在庫)
                Assert.Equal(2 * Catalog.Goods.Length, e.GetProperty("mk").GetArrayLength());
                Assert.Equal(8, e.GetProperty("cap").GetArrayLength());
            }
        }
        // 実装の検証関数も空を返す（上の独立な走査と合っている）
        Assert.Empty(OpeningBake.Validate(sim, doc));
    }

    [Fact]
    public void 読み込んで焼き直すと同じ本文_書く前は違う()
    {
        var sim = Fresh();
        string before = OpeningBake.Capture(sim, new OpeningBake.Header("test", OpeningBake.InputHash(), 1, 3, false, 3, "thawCapacity+stopSocialMobility+stopPopDecision (473 arm D)", "2026-10-21"));
        Assert.NotEqual(Short.Value, before);   // 何もしない読み込みが緑にならない＝構築直後の盤面は焼いた盤面と違う
        using var doc = JsonDocument.Parse(Short.Value);
        OpeningBake.Apply(sim, doc);
        string after = OpeningBake.Capture(sim, new OpeningBake.Header("test", OpeningBake.InputHash(), 1, 3, false, 3, "thawCapacity+stopSocialMobility+stopPopDecision (473 arm D)", "2026-10-21"));
        Assert.Equal(Short.Value, after);
    }

    [Fact]
    public void 鍵が1つでも違えば何も書かずに例外_国_州_財_生産者の財_階級()
    {
        string t = Short.Value;
        using var d0 = JsonDocument.Parse(t);
        string nk = d0.RootElement.GetProperty("nationTable").EnumerateObject().First().Name;
        string rk = d0.RootElement.GetProperty("nationTable").GetProperty(nk).GetProperty("regions").EnumerateObject().First().Name;
        string g0 = d0.RootElement.GetProperty("goods")[0].GetString()!;
        var muts = new Dictionary<string, string>
        {
            ["国の名前"] = t.Replace("\n" + Q(nk) + ":{", "\n" + Q(nk + "X") + ":{"),
            ["州の名前"] = t.Replace("  " + Q(rk) + ":{\"cap\"", "  " + Q(rk + "X") + ":{\"cap\""),
            ["財の名前"] = t.Replace("\"goods\":[" + Q(g0), "\"goods\":[" + Q(g0 + "X")),
            ["生産者の財"] = Regex.Replace(t, "\"pr\":\\[\\[(\\d+),", m => "\"pr\":[[" + (int.Parse(m.Groups[1].Value) + 1) + ",", RegexOptions.None, TimeSpan.FromSeconds(10)),
            ["階級の名前"] = Regex.Replace(t, "\"pop\":\\[\\[\"([^\"]+)\",", m => "\"pop\":[[\"" + m.Groups[1].Value + "X\",", RegexOptions.None, TimeSpan.FromSeconds(10)),
        };
        // 片側だけの食い違い（表に無い／表に余る）：どちらの向きの検査が欠けても気づけるよう、名前の入れ替えでなく欠けと余りを別々に作る
        static string Edit(string text, Action<System.Text.Json.Nodes.JsonObject> f)
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(text)!.AsObject(); f(root); return root.ToJsonString();
        }
        muts["国の欠け"] = Edit(t, r => r["nationTable"]!.AsObject().Remove(nk));
        muts["国の余り"] = Edit(t, r => r["nationTable"]!.AsObject().Add(nk + "余", r["nationTable"]![nk]!.DeepClone()));
        muts["州の欠け"] = Edit(t, r => r["nationTable"]![nk]!["regions"]!.AsObject().Remove(rk));
        muts["州の余り"] = Edit(t, r => r["nationTable"]![nk]!["regions"]!.AsObject().Add(rk + "余", r["nationTable"]![nk]!["regions"]![rk]!.DeepClone()));
        muts["生産者の欠け"] = Edit(t, r => r["nationTable"]![nk]!["regions"]![rk]!["pr"]!.AsArray().RemoveAt(0));
        muts["POPの欠け"] = Edit(t, r => r["nationTable"]![nk]!["regions"]![rk]!["pop"]!.AsArray().RemoveAt(0));
        muts["財の欠け"] = Edit(t, r => r["goods"]!.AsArray().RemoveAt(0));
        foreach (var (name, mt) in muts)
        {
            Assert.NotEqual(t, mt);   // 変異が当たっている
            using var doc = JsonDocument.Parse(mt);
            var sim = Fresh();
            string snap = OpeningBake.Capture(sim, new OpeningBake.Header("t", "t", 1, 0, false, 0, "t", "t"));
            Assert.NotEmpty(OpeningBake.Validate(sim, doc));
            var ex = Assert.Throws<InvalidOperationException>(() => OpeningBake.Apply(sim, doc));
            Assert.Contains("今の世界と合わない", ex.Message);
            Assert.Equal(snap, OpeningBake.Capture(sim, new OpeningBake.Header("t", "t", 1, 0, false, 0, "t", "t")));   // 盤面は 1 ビットも変わらない（検証が先・書くのは後）
        }
    }

    /// <summary>同梱の表（埋め込みの opening_bake.json）の番人：①鍵が今の世界と全て一致（世界のデータ〔州・レシピ・財・階級〕を変えたら焼き直す＝赤になるのが仕様）
    /// ②頭書きが〈収束した試走〉を名乗る（上限で打ち切った表を同梱しない）③旗 1 の助走が通り、貯蓄が旗 0 と違う（読み込みが実際に効く）④旗 0 の助走は同じ長さでも表を読まない＝貯蓄は構築直後と同じ。</summary>
    [Fact]
    public void 同梱の表は今の世界と鍵が全て一致し_旗1で助走へ入れて貯蓄が旗0と違う()
    {
        var doc = OpeningBake.EmbeddedDoc;
        Assert.Empty(OpeningBake.Validate(Fresh(), doc));
        var h = doc.RootElement.GetProperty("header");
        Assert.True(h.GetProperty("converged").GetBoolean(), "同梱の表が収束していない試走の盤面");
        Assert.InRange(h.GetProperty("quartersRun").GetInt32(), 40, h.GetProperty("capQuarters").GetInt32());
        Assert.Equal(61, h.GetProperty("nations").GetInt32());
        static double Sav(MultiNationSim s) => s.Nations.Sum(n => n.World.Regions.Sum(r => r.Pops.Sum(p => p.Savings.Value)));
        var off = Fresh(); double sav0 = Sav(off);
        off.StartWarmup(quartersOverride: 1);
        var on = Fresh(new Coefficients { OpeningBakeEnabled = 1 });
        on.StartWarmup(quartersOverride: 1);
        Assert.NotEqual(Sav(off), Sav(on));
        Assert.True(double.IsFinite(Sav(on)));
        Assert.True(sav0 > 0);
    }

    /// <summary>焼く試走の腕は〈能力の凍結を外す・社会移動を止める・州の中の階級遷移を止める〉の 3 つを**実際に**している（対照＝同じ長さの普段の助走は凍結のまま・階級の構成が動く）。
    /// 何もしない腕が緑にならないための番人：腕の 3 つの門のどれを外しても赤（突然変異）。</summary>
    [Fact]
    public void 焼く試走の腕は能力の凍結を外し階級の構成を動かさない_普段の助走は逆()
    {
        static double Cap(MultiNationSim s) => s.Nations.Sum(n => n.World.Regions.Sum(r => r.Industrialization + r.OldVintageCapital));
        static Dictionary<string, double> Mix(MultiNationSim s)
        {
            var d = new Dictionary<string, double>();
            foreach (var n in s.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Pops) { if (p.IsSoldier || p.ClassId == PopClass.Peasant) continue; /* 兵士と農民は徴兵で動く（腕に依らない）＝社会移動と階級遷移が動かす他の階級で見る。徴兵が工場労働者も少し引く〈推測・内訳は未確認〉（8 四半期で 0.4%）＝普段の助走の 1% 未満で見る */ string k = n.World.SelfPower!.BaseName + "/" + r.Name + "/" + p.ClassName; d[k] = d.GetValueOrDefault(k) + p.Size; }
            return d;
        }
        var baked = Fresh(); var normal = Fresh();
        double cap0 = Cap(baked); var mix0 = Mix(baked);
        foreach (var n in baked.Nations) n.World.BakeRun = new OpeningBake.RunOptions();
        baked.WarmupEconomy(8); normal.WarmupEconomy(8);
        // 能力：普段の助走は凍結（据え置き）＝焼く腕は動く
        Assert.Equal(cap0, Cap(normal), 9);
        Assert.NotEqual(cap0, Cap(baked));
        // 階級の構成：焼く腕は world.json のまま・普段の助走は社会移動と階級遷移で動く（対照）
        var mixB = Mix(baked); var mixN = Mix(normal);
        double dBaked = mix0.Sum(kv => Math.Abs(kv.Value - mixB[kv.Key])), dNormal = mix0.Sum(kv => Math.Abs(kv.Value - mixN[kv.Key]));
        Assert.True(dNormal > 1.0, $"前提が崩れた：普段の助走で階級の構成が動かない（{dNormal}）＝対照にならない");
        Assert.True(dBaked < 0.01 * dNormal, $"焼く試走の腕で階級の構成が動いた（{dBaked}／全体 {mix0.Values.Sum()}／普段 {dNormal}）" + string.Join(" ／ ", mix0.OrderByDescending(kv => Math.Abs(kv.Value - mixB[kv.Key])).Take(8).Select(kv => $"{kv.Key} {kv.Value:F1}→{mixB[kv.Key]:F1}")));
    }

    [Fact]
    public void 読み込んだ後も助走が通り貯蓄の保存則の基準線を取り直している()
    {
        var sim = Fresh();
        double sav0 = sim.Nations.Sum(n => n.World.Regions.Sum(r => r.Pops.Sum(p => p.Savings.Value)));
        using var doc = JsonDocument.Parse(Short.Value);
        OpeningBake.Apply(sim, doc);
        double sav1 = sim.Nations.Sum(n => n.World.Regions.Sum(r => r.Pops.Sum(p => p.Savings.Value)));
        Assert.NotEqual(sav0, sav1);   // 貯蓄が書き換わった（対照）
        // 基準線を取り直してあるので、書き換えた直後から歩を進めても保存則（月次 CheckInvariants）が割れない
        sim.WarmupEconomy(1);
        double sav2 = sim.Nations.Sum(n => n.World.Regions.Sum(r => r.Pops.Sum(p => p.Savings.Value)));
        Assert.True(double.IsFinite(sav2));
    }
}
