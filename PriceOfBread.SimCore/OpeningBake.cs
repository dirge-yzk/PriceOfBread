using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PriceOfBread.SimCore;

/// <summary>★(490) 開幕の焼き込みの道具（読む線 (464)(473) の形・設計は docs/202609/99_実装ログ_20260903.md の (490)）。
/// 焼く＝凍結を解いた試走（<see cref="RunOptions"/>＝能力の凍結を外す・社会移動を止める・州の中の階級遷移を止める＝(473) の腕 D）で落ち着いた盤面から、
/// 州×レシピの能力（<see cref="Producer.Scale"/>・<see cref="Producer.BuiltScale"/>・<see cref="Producer.SmoothedMargin"/>・<see cref="Producer.Bankrupt"/>）・州の資本（工業化度ほか）・
/// 州×財の在庫と値段・州の物価水準・国の発見の値段の基準・州×階級の貯蓄を、別の表（<c>opening_bake.json</c>・鍵は名前）へ書く。
/// 読み込み＝旗 <see cref="Coefficients.OpeningBakeEnabled"/> が正のとき（既定 0＝表を開きもしない＝厳密恒等）、<see cref="MultiNationSim.StartWarmup"/> の頭で 1 回（<see cref="Apply"/>）。
/// 表の鍵が今の世界の名前と**全て**一致しなければ何も書かずに例外（fail-closed）＝世界のデータ（州・レシピ・財・階級）を変えたら焼き直す。
/// 新しい状態は作らない（思想⑩）＝読み込みは既存の欄へ書くだけ。焼いた値は模型の出力で、world.json（手で直す史実のデータ）とは別の置き場。
/// ⚠焼かないもの：人口（総数・年齢の層）・階級の構成・政体・法・国庫（<c>RebaseOpeningFiscal</c> の規則のまま）・国債・技術・発見・暦・外交・市場の流れの欄（LastSold 等）・労働の留保賃金・生産者の産出と労働の重み。
/// 読み込んだ直後は貯蓄が変わる＝基礎貨幣（貯蓄＋国庫−貸出の導出量）が変わるので保存則の基準線を取り直す（助走の各四半期も取り直す）。</summary>
public static class OpeningBake
{
    /// <summary>焼く試走の腕（焼く側だけが <see cref="World.BakeRun"/> に立てる・遊ぶ側と測る側は null＝何も変わらない）。</summary>
    public sealed class RunOptions
    {
        /// <summary>助走の中で能力の凍結（<c>EconomyStep</c> の建った結果・減価・ヴィンテージ・国家建設・送電網）を外す。</summary>
        public bool ThawCapacity = true;
        /// <summary>助走の中で社会移動（<c>Meritocracy.Step</c>）を止める。</summary>
        public bool StopSocialMobility = true;
        /// <summary>助走の中で州の中の階級遷移（<c>PopDecision.Run</c>）を止める（階級の構成を world.json のまま保つ＝(473) の腕 D）。</summary>
        public bool StopPopDecision = true;
    }

    public const string Format = "opening_bake/1";
    const string ResourceName = "opening_bake.json";
    /// <summary>州の資本の欄（<see cref="Region"/>）。並びは表の cap の並び。</summary>
    internal static readonly string[] CapNames = ["Industrialization", "OldVintageCapital", "VintageTechLevel", "StrandedVintage", "DevelopmentPeak", "GridCapacity", "PriceLevel", "LocalPriceMultiplier"];
    /// <summary>生産者の欄（<see cref="Producer"/>）の並び（表の pr の各行は [財の添字, これら]）。</summary>
    internal static readonly string[] ProducerNames = ["Scale", "BuiltScale", "SmoothedMargin", "Bankrupt"];

    /// <summary>表の頭書き（焼いた木の SOURCE-HASH は表を足す前の木＝表自体は SOURCE-HASH に入る）。</summary>
    public sealed record Header(string SourceHash, string InputHash, int Seed, int QuartersRun, bool Converged, int CapQuarters, string Arm, string Generated);

    // ───────────────────────────── 入力のハッシュ ─────────────────────────────

    /// <summary>世界のデータの入力のハッシュ＝SimCore の埋め込みの資源（world.json・goods.json・places.json ほか。opening_bake.json 自身を除く）を名前順に並べた sha256 の先頭 16 桁。
    /// 違うだけでは赤にしない（<see cref="Apply"/> は鍵の一致だけを見る）＝名乗りに書いて、焼いた木と今の木の入力が同じかを人が読む。</summary>
    public static string InputHash()
    {
        var asm = typeof(OpeningBake).Assembly;
        using var sha = SHA256.Create();
        foreach (var n in asm.GetManifestResourceNames().Where(x => !x.EndsWith(ResourceName, StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal))
        {
            using var s = asm.GetManifestResourceStream(n)!;
            var h = SHA256.HashData(s);
            sha.TransformBlock(Encoding.UTF8.GetBytes(n), 0, Encoding.UTF8.GetByteCount(n), null, 0);
            sha.TransformBlock(h, 0, h.Length, null, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant()[..16];
    }

    // ───────────────────────────── 鍵の名乗り（国・州・財・階級） ─────────────────────────────

    /// <summary>国の鍵＝構築時の名前（<see cref="Power.BaseName"/>・改名で変わらない）。</summary>
    internal static string NationKey(World w) => w.SelfPower?.BaseName ?? throw new InvalidOperationException("開幕の焼き込み：国に Power が無い（鍵が立たない）");

    /// <summary>発見の値段の並び（財の名前＋末尾の 1 欄＝陸の輸送）。</summary>
    internal static string[] DiscoveryPriceNames() => [.. Catalog.Goods.Select(g => g.Name), "(陸の輸送)"];

    static string Num(double v)
    {
        if (!double.IsFinite(v)) throw new InvalidOperationException($"開幕の焼き込み：非有限の値 {v}（表に書かない）");
        return v.ToString("R", CultureInfo.InvariantCulture);
    }

    static string Q(string s) => JsonSerializer.Serialize(s, JsonOpt);
    static readonly JsonSerializerOptions JsonOpt = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // ───────────────────────────── 試走（焼く側の入口） ─────────────────────────────

    public sealed record BakeResult(string Text, int Quarters, bool Converged);
    sealed class StopBake : Exception { }

    /// <summary>凍結を解いた試走を回して表の本文を作る（Runner の bakeopen とテストの入口）。構築は selfplay と同じ（全国 <see cref="UtilityAgent"/>・既定の係数）。
    /// 試走は <see cref="MultiNationSim.WarmupEconomy"/> を直に呼ぶ（技術の助走・開幕の軍の確定・ランクの取り直しは回さない＝取り出す盤面は〈凍結を解いた助走の直後〉だけ）。
    /// <paramref name="converged"/>＝各四半期の終わり（国庫の戻しの後）に呼ばれ、true を返した四半期でその盤面を焼いて止める。上限 <paramref name="maxQuarters"/> まで true が出なければ上限の盤面を焼く（Converged＝false）。
    /// 乱数は引かない（シミュの乱数をそのまま）＝同じ入力で 2 回焼くと本文がバイトで同じ。</summary>
    public static BakeResult Bake(ulong seed, int maxQuarters, Func<MultiNationSim, int, bool> converged, string sourceHash, string generated)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
        foreach (var n in sim.Nations) { n.TelemetryEnabled = false; n.World.BakeRun = new RunOptions(); }
        sim.Diplomacy.Log.Enabled = false;
        int q = 0; bool conv = false;
        try { sim.WarmupEconomy(maxQuarters, () => { q++; if (converged(sim, q)) { conv = true; throw new StopBake(); } }); }
        catch (StopBake) { }
        var h = new Header(sourceHash, InputHash(), (int)seed, q, conv, maxQuarters, "thawCapacity+stopSocialMobility+stopPopDecision (473 arm D)", generated);
        string text = Capture(sim, h);
        foreach (var n in sim.Nations) n.World.BakeRun = null;
        return new BakeResult(text, q, conv);
    }

    // ───────────────────────────── 焼く（盤面 → 表の本文） ─────────────────────────────

    /// <summary>盤面の今の状態を表の本文（JSON）にする。決定論：国・州は世界の並び（構築の順）・財は <see cref="Catalog.Goods"/> の順・数は "R"（往復で同じ）・LF・BOM なし。
    /// 鍵（国の名前・州の名前）が重なれば例外（fail-closed）。</summary>
    public static string Capture(MultiNationSim sim, Header h)
    {
        int ng = Catalog.Goods.Length;
        var sb = new StringBuilder(1 << 22);
        sb.Append("{\n\"format\":").Append(Q(Format)).Append(",\n\"header\":{\n");
        sb.Append(" \"sourceHash\":").Append(Q(h.SourceHash)).Append(",\n");
        sb.Append(" \"inputHash\":").Append(Q(h.InputHash)).Append(",\n");
        sb.Append(" \"seed\":").Append(h.Seed.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        sb.Append(" \"quartersRun\":").Append(h.QuartersRun.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        sb.Append(" \"converged\":").Append(h.Converged ? "true" : "false").Append(",\n");
        sb.Append(" \"capQuarters\":").Append(h.CapQuarters.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        sb.Append(" \"arm\":").Append(Q(h.Arm)).Append(",\n");
        sb.Append(" \"generated\":").Append(Q(h.Generated)).Append(",\n");
        sb.Append(" \"nations\":").Append(sim.Nations.Count.ToString(CultureInfo.InvariantCulture)).Append("\n},\n");
        sb.Append("\"capNames\":[").Append(string.Join(',', CapNames.Select(Q))).Append("],\n");
        sb.Append("\"producerNames\":[").Append(string.Join(',', ProducerNames.Select(Q))).Append("],\n");
        sb.Append("\"goods\":[").Append(string.Join(',', Catalog.Goods.Select(g => Q(g.Name)))).Append("],\n");
        sb.Append("\"nationTable\":{\n");
        var seenN = new HashSet<string>(StringComparer.Ordinal);
        bool firstN = true;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nk = NationKey(w);
            if (!seenN.Add(nk)) throw new InvalidOperationException($"開幕の焼き込み：国の名前が重なる（{nk}）");
            if (!firstN) sb.Append(",\n"); firstN = false;
            sb.Append(Q(nk)).Append(":{\n");
            sb.Append(" \"level\":[").Append(Num(w.PriceLevel)).Append(',').Append(Num(w.CostPriceLevel)).Append("],\n");
            sb.Append(" \"disc\":");
            if (w.DiscoveryPrice is { } dp)
            {
                if (dp.Length != ng + 1) throw new InvalidOperationException($"開幕の焼き込み：発見の値段の長さ {dp.Length} が財の数+1 {ng + 1} と違う（{nk}）");
                sb.Append('[').Append(string.Join(',', dp.Select(Num))).Append(']');
            }
            else sb.Append("null");
            sb.Append(",\n \"regions\":{\n");
            var seenR = new HashSet<string>(StringComparer.Ordinal);
            bool firstR = true;
            foreach (var r in w.Regions)
            {
                if (!seenR.Add(r.Name)) throw new InvalidOperationException($"開幕の焼き込み：{nk} の州の名前が重なる（{r.Name}）");
                if (r.Markets.Length != ng) throw new InvalidOperationException($"開幕の焼き込み：{nk}/{r.Name} の市場の数 {r.Markets.Length} が財の数 {ng} と違う");
                if (!firstR) sb.Append(",\n"); firstR = false;
                sb.Append("  ").Append(Q(r.Name)).Append(":{\"cap\":[").Append(string.Join(',', CapValues(r).Select(Num))).Append("],\"mk\":[");
                for (int g = 0; g < ng; g++) { if (g > 0) sb.Append(','); sb.Append(Num(r.Markets[g].Price)).Append(',').Append(Num(r.Markets[g].Stock)); }
                sb.Append("],\"pr\":[");
                for (int i = 0; i < r.Producers.Count; i++)
                {
                    var p = r.Producers[i];
                    if (i > 0) sb.Append(',');
                    sb.Append('[').Append(p.GoodId.ToString(CultureInfo.InvariantCulture)).Append(',').Append(Num(p.Scale)).Append(',').Append(Num(p.BuiltScale)).Append(',').Append(Num(p.SmoothedMargin)).Append(',').Append(p.Bankrupt ? '1' : '0').Append(']');
                }
                sb.Append("],\"pop\":[");
                for (int i = 0; i < r.Pops.Count; i++)
                {
                    var p = r.Pops[i];
                    if (i > 0) sb.Append(',');
                    sb.Append('[').Append(Q(p.ClassName)).Append(',').Append(Num(p.Savings.Value)).Append(']');
                }
                sb.Append("]}");
            }
            sb.Append("\n }\n}");
        }
        sb.Append("\n}\n}\n");
        return sb.ToString();
    }

    static double[] CapValues(Region r) => [r.Industrialization, r.OldVintageCapital, r.VintageTechLevel, r.StrandedVintage, r.DevelopmentPeak, r.GridCapacity, r.PriceLevel, r.LocalPriceMultiplier];

    // ───────────────────────────── 読む（表 → 盤面） ─────────────────────────────

    static readonly Lazy<JsonDocument> Embedded = new(() =>
    {
        var asm = typeof(OpeningBake).Assembly;
        string? name = Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith(ResourceName, StringComparison.Ordinal));
        if (name is null) throw new InvalidOperationException("opening_bake.json（埋め込みリソース）が見つかりません。SimCore.csproj の <EmbeddedResource> を確認。");
        using var s = asm.GetManifestResourceStream(name)!;
        return JsonDocument.Parse(s);
    });

    /// <summary>埋め込みの表（番人と焼き直しの比べが読む）。</summary>
    internal static JsonDocument EmbeddedDoc => Embedded.Value;

    /// <summary>埋め込みの表を盤面に書く（旗が正のとき <see cref="MultiNationSim.StartWarmup"/> が 1 回呼ぶ）。鍵が全て一致しなければ何も書かずに例外。</summary>
    internal static void Apply(MultiNationSim sim) => Apply(sim, Embedded.Value);

    /// <summary>表（<paramref name="doc"/>）を盤面に書く。①検証（鍵の一致・長さ・非有限）②書く、の 2 段＝①で例外なら盤面は 1 ビットも変わらない。</summary>
    internal static void Apply(MultiNationSim sim, JsonDocument doc)
    {
        var errs = Validate(sim, doc);
        if (errs.Count > 0)
            throw new InvalidOperationException($"開幕の焼き込みの表が今の世界と合わない（{errs.Count} 件・先頭 {string.Join(" ／ ", errs.Take(5))}）。世界のデータ（州・レシピ・財・階級）を変えたら Runner の bakeopen で焼き直す。");
        var root = doc.RootElement;
        int ng = Catalog.Goods.Length;
        var nt = root.GetProperty("nationTable");
        foreach (var n in sim.Nations)
        {
            var w = n.World; var ne = nt.GetProperty(NationKey(w));
            var lv = ne.GetProperty("level"); w.PriceLevel = lv[0].GetDouble(); w.CostPriceLevel = lv[1].GetDouble();
            var disc = ne.GetProperty("disc");
            if (disc.ValueKind == JsonValueKind.Array) { var d = new double[ng + 1]; int i = 0; foreach (var e in disc.EnumerateArray()) d[i++] = e.GetDouble(); w.DiscoveryPrice = d; }
            var re = ne.GetProperty("regions");
            foreach (var r in w.Regions)
            {
                var e = re.GetProperty(r.Name);
                var cap = e.GetProperty("cap");
                r.Industrialization = cap[0].GetDouble(); r.OldVintageCapital = cap[1].GetDouble(); r.VintageTechLevel = cap[2].GetDouble(); r.StrandedVintage = cap[3].GetDouble();
                r.DevelopmentPeak = cap[4].GetDouble(); r.GridCapacity = cap[5].GetDouble(); r.PriceLevel = cap[6].GetDouble(); r.LocalPriceMultiplier = cap[7].GetDouble();
                var mk = e.GetProperty("mk");
                for (int g = 0; g < ng; g++) { r.Markets[g].Price = mk[2 * g].GetDouble(); r.Markets[g].Stock = mk[2 * g + 1].GetDouble(); }
                var pr = e.GetProperty("pr");
                for (int i = 0; i < r.Producers.Count; i++)
                {
                    var row = pr[i]; var p = r.Producers[i];
                    p.Scale = row[1].GetDouble(); p.BuiltScale = row[2].GetDouble(); p.SmoothedMargin = row[3].GetDouble(); p.Bankrupt = row[4].GetDouble() != 0;
                }
                var po = e.GetProperty("pop");
                for (int i = 0; i < r.Pops.Count; i++) r.Pops[i].ApplyBakedSavings(po[i][1].GetDouble());
            }
        }
        sim.RebaselineInvariants();   // 貯蓄が変わった＝基礎貨幣（貯蓄＋国庫−貸出）が変わる＝保存則の基準線を取り直す
    }

    static bool NamesMatch(JsonElement root, string prop, string[] names)
    {
        if (!root.TryGetProperty(prop, out var a) || a.ValueKind != JsonValueKind.Array || a.GetArrayLength() != names.Length) return false;
        for (int i = 0; i < names.Length; i++) if (a[i].GetString() != names[i]) return false;
        return true;
    }

    /// <summary>表と盤面の鍵の突き合わせ（双方向）。空＝一致。盤面は書かない。</summary>
    internal static List<string> Validate(MultiNationSim sim, JsonDocument doc)
    {
        var errs = new List<string>();
        var root = doc.RootElement;
        if (!root.TryGetProperty("format", out var f) || f.GetString() != Format) { errs.Add($"format が {Format} でない"); return errs; }
        // ★(491) 欄の並びも鍵（反証役 hz4〔v〕低#3＝Apply は添字で直書き＝同じ長さのまま欄を入れ替えると値が黙って別の欄に入る）
        if (!NamesMatch(root, "capNames", CapNames)) errs.Add($"資本の欄の並びが世界と違う（世界 {string.Join(',', CapNames)}）");
        if (!NamesMatch(root, "producerNames", ProducerNames)) errs.Add($"生産者の欄の並びが世界と違う（世界 {string.Join(',', ProducerNames)}）");
        int ng = Catalog.Goods.Length;
        if (!root.TryGetProperty("goods", out var goods)) { errs.Add("空の表（まだ焼いていない）"); return errs; }
        if (goods.GetArrayLength() != ng) errs.Add($"財の数 表 {goods.GetArrayLength()} ≠ 世界 {ng}");
        else for (int g = 0; g < ng; g++) if (goods[g].GetString() != Catalog.Goods[g].Name) { errs.Add($"財 {g} の名前 表 {goods[g].GetString()} ≠ 世界 {Catalog.Goods[g].Name}"); break; }
        if (!root.TryGetProperty("nationTable", out var nt) || nt.ValueKind != JsonValueKind.Object) { errs.Add("nationTable が無い（空の表）"); return errs; }
        var tableN = new List<string>(); foreach (var p in nt.EnumerateObject()) tableN.Add(p.Name);
        var worldN = sim.Nations.Select(n => NationKey(n.World)).ToList();
        if (tableN.Count != tableN.Distinct(StringComparer.Ordinal).Count()) errs.Add("表の国の名前が重なる");
        foreach (var x in worldN.Except(tableN, StringComparer.Ordinal)) errs.Add($"世界の国 {x} が表に無い");
        foreach (var x in tableN.Except(worldN, StringComparer.Ordinal)) errs.Add($"表の国 {x} が世界に無い");
        if (errs.Count > 0) return errs;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nk = NationKey(w); var ne = nt.GetProperty(nk);
            var lv = ne.GetProperty("level"); if (lv.GetArrayLength() != 2 || !AllFinite(lv)) errs.Add($"{nk} の物価水準が不正");
            var disc = ne.GetProperty("disc");
            if (disc.ValueKind == JsonValueKind.Array) { if (disc.GetArrayLength() != ng + 1 || !AllFinite(disc)) errs.Add($"{nk} の発見の値段の長さ・値が不正"); }
            else if (disc.ValueKind != JsonValueKind.Null) errs.Add($"{nk} の発見の値段が配列でも null でもない");
            var re = ne.GetProperty("regions");
            var tableR = new List<string>(); foreach (var p in re.EnumerateObject()) tableR.Add(p.Name);
            var worldR = w.Regions.Select(r => r.Name).ToList();
            if (worldR.Count != worldR.Distinct(StringComparer.Ordinal).Count()) errs.Add($"{nk} の世界の州の名前が重なる");
            foreach (var x in worldR.Except(tableR, StringComparer.Ordinal)) errs.Add($"{nk} の世界の州 {x} が表に無い");
            foreach (var x in tableR.Except(worldR, StringComparer.Ordinal)) errs.Add($"{nk} の表の州 {x} が世界に無い");
            if (errs.Count > 20) return errs;
            foreach (var r in w.Regions)
            {
                if (!re.TryGetProperty(r.Name, out var e)) continue;
                string rk = nk + "/" + r.Name;
                var cap = e.GetProperty("cap"); if (cap.GetArrayLength() != CapNames.Length || !AllFinite(cap)) errs.Add($"{rk} の資本の欄が不正");
                var mk = e.GetProperty("mk"); if (mk.GetArrayLength() != 2 * ng || r.Markets.Length != ng || !AllFinite(mk)) errs.Add($"{rk} の市場の長さ・値が不正");
                var pr = e.GetProperty("pr");
                if (pr.GetArrayLength() != r.Producers.Count) errs.Add($"{rk} の生産者の数 表 {pr.GetArrayLength()} ≠ 世界 {r.Producers.Count}");
                else for (int i = 0; i < r.Producers.Count; i++)
                    {
                        var row = pr[i];
                        if (row.GetArrayLength() != 1 + ProducerNames.Length || row[0].GetInt32() != r.Producers[i].GoodId) { errs.Add($"{rk} の生産者 {i} の財が表と違う（表 {row[0].GetInt32()} ≠ 世界 {r.Producers[i].GoodId}）"); break; }
                        for (int k = 1; k < row.GetArrayLength(); k++) if (!double.IsFinite(row[k].GetDouble())) { errs.Add($"{rk} の生産者 {i} に非有限"); break; }
                    }
                var po = e.GetProperty("pop");
                if (po.GetArrayLength() != r.Pops.Count) errs.Add($"{rk} の POP の数 表 {po.GetArrayLength()} ≠ 世界 {r.Pops.Count}");
                else for (int i = 0; i < r.Pops.Count; i++)
                    {
                        if (po[i].GetArrayLength() != 2 || po[i][0].GetString() != r.Pops[i].ClassName) { errs.Add($"{rk} の POP {i} の階級が表と違う（表 {po[i][0].GetString()} ≠ 世界 {r.Pops[i].ClassName}）"); break; }
                        if (!double.IsFinite(po[i][1].GetDouble())) { errs.Add($"{rk} の POP {i} の貯蓄が非有限"); break; }
                    }
                if (errs.Count > 20) return errs;
            }
        }
        return errs;
    }

    static bool AllFinite(JsonElement arr) { foreach (var e in arr.EnumerateArray()) if (!double.IsFinite(e.GetDouble())) return false; return true; }
}
