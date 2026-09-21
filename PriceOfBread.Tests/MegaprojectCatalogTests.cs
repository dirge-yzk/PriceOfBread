using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 巨大建造物カタログ（<c>megaprojects.json</c>）の番人（2026-08-10・作者裁定「カタログを全国へ拡張」の恒久化）。
///
/// ⚠**きっかけ**：カタログは 73件→162件 に増えたが、データ側の誤りは**どこにも赤を出さない**：
///   ・存在しない州名を host に書いても <see cref="Megaprojects.Step"/> は `hostRegion is null` で静かに `continue` する
///     ＝その建造物は永久に着工されず、**カタログに載っているのに世界に無い**（タイプミスが検知できない）。
///   ・id が重複すると <c>Region.MonumentApplied</c>（Id集合）が2件目を「適用済み」と誤認して効果が落ちる。
///   ・効果値は較正の外側から直接 <c>Industrialization</c> 等へ加算されるので、1件の極端な値が帯を壊す。
/// ★<see cref="GeographyCoverageTests"/> と同じ作法＝「データを足したら番人が落ちる」ようにしておく。
/// </summary>
public class MegaprojectCatalogTests
{
    /// <summary>host（建設地）→ 1783年断面の保有国。world.json の州リストが真実源。</summary>
    private static Dictionary<string, string> HostToNation()
    {
        var map = new Dictionary<string, string>();
        foreach (var cfg in WorldData.Nations())
            foreach (var p in cfg.Provinces)
                map[p.City] = cfg.Name;
        return map;
    }

    [Fact]
    public void 全建造物のhostが実在する州である()
    {
        var map = HostToNation();
        foreach (var mp in WorldData.Megaprojects())
            Assert.True(map.ContainsKey(mp.Host),
                $"巨大建造物「{mp.Name}」（{mp.Id}）の host「{mp.Host}」が world.json の州に無い＝" +
                "Megaprojects.Step が建設地を解決できず永久に着工されない（例外も警告も出ない）。" +
                "★植民地は host にしない（州へ格上げされるまで置けない）＝docs/202608/巨大建造物カタログ完了_2026-08-09_完了.md【完了】.md §4 の保留表を参照");
    }

    [Fact]
    public void 全国家が最低1件の巨大建造物を持つ()
    {
        var map = HostToNation();
        var cnt = new Dictionary<string, int>();
        foreach (var cfg in WorldData.Nations()) cnt[cfg.Name] = 0;
        foreach (var mp in WorldData.Megaprojects())
            if (map.TryGetValue(mp.Host, out var nation)) cnt[nation]++;
        foreach (var kv in cnt)
            Assert.True(kv.Value >= 1,
                $"国「{kv.Key}」に巨大建造物が1件も無い（2026-08-10 作者裁定＝全国が最低1件）。" +
                "world.json に国家を足したら megaprojects.json にもその国の州を host にした史実の事業を足すこと");
    }

    [Fact]
    public void 建造物のidが重複しない()
    {
        var seen = new HashSet<string>();
        foreach (var mp in WorldData.Megaprojects())
            Assert.True(seen.Add(mp.Id),
                $"巨大建造物の id「{mp.Id}」が重複している＝Region.MonumentApplied が2件目を適用済みと誤認して効果が落ちる");
    }

    [Fact]
    public void 効果の型と値が既存の帯域に収まる()
    {
        foreach (var mp in WorldData.Megaprojects())
        {
            Assert.True(mp.EffectMode is "add" or "scale", $"{mp.Id}: effectMode「{mp.EffectMode}」は add/scale のいずれかでなければならない");
            switch (mp.EffectTarget)
            {
                // 帯の上端＝既存の最大値（八幡製鉄所/クルップ 0.3・アスワン 0.15・ドニエプル 6.0）。
                // 「極端に強力なものを作らない」（作者指示2026-08-08・2026-08-10）の恒久化。
                case "Industrialization":
                    Assert.True(mp.EffectMode == "add" && mp.EffectValue > 0 && mp.EffectValue <= 0.3,
                        $"{mp.Id}: Industrialization は add で 0<値≤0.3（既存最大＝八幡製鉄所0.3）。実値 {mp.EffectMode}/{mp.EffectValue}");
                    break;
                case "ArableLand":
                    Assert.True(mp.EffectMode == "scale" && mp.EffectValue > 0 && mp.EffectValue <= 0.15,
                        $"{mp.Id}: ArableLand は scale で 0<値≤0.15（既存最大＝アスワン0.15）。実値 {mp.EffectMode}/{mp.EffectValue}");
                    break;
                case "GridCapacity":
                    Assert.True(mp.EffectMode == "add" && mp.EffectValue > 0 && mp.EffectValue <= 6.0,
                        $"{mp.Id}: GridCapacity は add で 0<値≤6.0（既存最大＝ドニエプル6.0）。実値 {mp.EffectMode}/{mp.EffectValue}");
                    break;
                case "Prestige":
                    // 威信型＝経済効果を持たず、完成時の MegaprojectPrestige（正統性の一回加算）だけを取る。
                    Assert.Equal(0.0, mp.EffectValue);
                    break;
                default:
                    Assert.Fail($"{mp.Id}: 未知の effectTarget「{mp.EffectTarget}」＝ApplyEffect が不発になる（効果ゼロの死にデータ）");
                    break;
            }
        }
    }

    [Fact]
    public void 規模と技術ゲートが既存の刻みに従う()
    {
        foreach (var mp in WorldData.Megaprojects())
        {
            // S=300 / M=600 / 水力=900 / L=1500 / XL=3000 の5段（IndPerConstruction=0.01・州上限5.0 なので 500 で1州ぶん）
            Assert.True(mp.BuildUnits is 300 or 600 or 900 or 1500 or 3000,
                $"{mp.Id}: buildUnits {mp.BuildUnits} が規模の刻み（300/600/900/1500/3000）に無い");
            // 内生 TechLevel のアンカー＝1806年 1.00 / 1858年 1.80 / 1913年 2.92。0.9未満＝1783年以前・2.95超＝到達不能
            Assert.InRange(mp.TechGate, 0.9, 2.95);
        }
    }
}
