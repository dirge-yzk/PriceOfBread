// GameViewModel.Discovery.cs ＝ 発見パネル（F・2026-08-31作者指示「UIはFボタンに配置」／2026-09-03にF1→Fへ）。
// 発見＝解禁の唯一の形（Discovery.Unlock）の現在地をデータで見せる：制度の普及度＋解禁物（既定製法・代替製法・世界のゲート）の進行。
// 2026-09-07（作者要望11）：エンジンに在る解禁物を全部並べる（未解禁0%も解禁済み100%も載せる）＋パネル寸法を右ドックと一致させた。
// ⚠UI規律＝説明文なし・データのみ・漢字1字略称なし（ツールチップもデータ）。
// 2026-09-08（作者裁定）：思想の解禁（社会主義/共産主義/帝国主義/ファシズム）を同じ器で並べる＝Ideologies.EraGates を読む。
// 2026-09-23（作者裁定「閾値は出さないが、パーセントは出す」＋「政府・思想・技術でカテゴリ分け」「2段組みに」「バーは廃止」）：
//   ①ホバーから発見の閾値を全て撤去（onset は並べ替えの第2キーとしてだけ残る）／②種別「制度」→「政府」／
//   ③DiscoveryLeft・DiscoveryRight の2列（派生プロパティ＝状態は増やさない）／④バーは XAML 側で撤去（Pct は ByGate が読むので残す）。
//   ⚠ホバーは残す：閾値を落とした後に残るのは「その行の時計の名前と現在値」＝技術／最大工業化度／世界技術 の3種で、
//     どれが解禁を動かすかはパーセント列だけでは読めない（例＝冷蔵は技術でなく工業化度・電信は自国でなく世界技術）。
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public sealed record DiscoveryRow(string Name, string Kind, double Pct, string PctText, string Tip);

public partial class GameViewModel
{
    [ObservableProperty] private bool _discoveryOpen;

    public List<DiscoveryRow> DiscoveryRows { get; private set; } = [];

    // 2段組（作者指示2026-09-23「2段組みに」）。新しい状態変数は作らず <see cref="DiscoveryRows"/> から毎回導く＝真実源は1本のまま。
    // 左＝上半分・右＝下半分（奇数なら左が1行多い）＝並べ替え ByGate の全順序を「左列を上から読み切って右列へ」で保つ。
    private int DiscoveryHalf => (DiscoveryRows.Count + 1) / 2;
    public List<DiscoveryRow> DiscoveryLeft => DiscoveryRows.GetRange(0, DiscoveryHalf);
    public List<DiscoveryRow> DiscoveryRight => DiscoveryRows.GetRange(DiscoveryHalf, DiscoveryRows.Count - DiscoveryHalf);

    [RelayCommand]
    private void ToggleDiscovery()
    {
        DiscoveryOpen = !DiscoveryOpen;
        // 発見パネルは2026-09-07に右ドックと同寸へ＝同じ矩形に2枚重ねない（生産タブ＝全画面とも同時に出さない）
        if (DiscoveryOpen) { ActiveTab = ""; ProdOpen = false; RefreshDiscovery(); }
    }

    [RelayCommand] private void CloseDiscovery() => DiscoveryOpen = false;

    /// <summary>発見パネルの再計算（開いている間は四半期リフレッシュから追従）。値は全てエンジンの同じ式を読む＝UIに第二の式を置かない。</summary>
    public void RefreshDiscovery()
    {
        var w = _player;
        var c = w.Coef;
        var rows = new List<DiscoveryRow>();

        // 制度（発見・段2＝Institutions と同じ式）
        double maxInd = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
        double pat = c.DiscoveryInstitutions > 0
            ? Discovery.Unlock(w.EliteCore, 1.0, c.PatentDiscoveryOnset, c.PatentDiscoverySpan)
            : (w.PatentLaw ? 1.0 : 0.0);
        double js = c.DiscoveryInstitutions > 0
            ? Discovery.Unlock(maxInd, 1.0, c.JointStockDiscoveryOnset, c.JointStockDiscoverySpan)
            : (w.JointStock ? 1.0 : 0.0);
        // ★2026-09-23（作者指示「政府・思想・技術でカテゴリ分け」）：種別の呼び名を「制度」→「政府」へ。並びは既に 政府→思想→技術 の連続ブロック＝見出し行は足さない。
        rows.Add(new DiscoveryRow("特許制度", "政府", pat, $"{pat:P0}", $"中核エリート {w.EliteCore:F2}"));
        rows.Add(new DiscoveryRow("株式会社", "政府", js, $"{js:P0}", $"最大工業化度 {maxInd:F2}"));
        rows.Add(new DiscoveryRow("鉄道", "政府", w.Railways ? 1.0 : 0.0, $"{(w.Railways ? 1.0 : 0.0):P0}", $"最大工業化度 {maxInd:F2}"));   // P0統一（「0 %」の空白混入を排除）

        // 思想の解禁（2026-09-08作者裁定）＝後発イデオロギーの解禁度。値は Ideologies.EraGates＝
        // Ideologies.UpdateShares が毎四半期に読む式そのもの（UIに第二の式を置かない）。
        // 並べ替えの閾値は発展度のオンセット（解禁度は発展度だけで決まる＝暦年の混合は 2026-09-26 に削除）。
        var era = Ideologies.EraGates(w);
        var reach = w.IdeologyReach;
        string Tip(Ideology k)
        {
            string t = $"年 {w.Year} ／ 発展度 {era.Dev:F2}";
            if (c.IdeologyReachGate > 0 && reach is not null && (int)k < reach.Length) t += $" ／ 到達 {reach[(int)k]:F2}";
            return t;
        }
        var ideo = new List<(string Name, double Pct, double Onset, string Tip)>
        {
            ("社会主義",   era.Socialism,   c.IdeologyGateSocOnset,  Tip(Ideology.Socialism)),
            ("共産主義",   era.Communism,   c.IdeologyGateCommOnset, Tip(Ideology.Communism)),
            ("帝国主義",   era.Imperialism, c.IdeologyGateImpOnset,  Tip(Ideology.Imperialism)),
            ("ファシズム", era.Fascism,     c.IdeologyGateFascOnset, Tip(Ideology.Fascism)),
        };
        ideo.Sort(ByGate);
        foreach (var t in ideo) rows.Add(new DiscoveryRow(t.Name, "思想", t.Pct, $"{t.Pct:P0}", t.Tip));

        // ★2026-09-07（作者要望11「追加分をリストに追加」）＝エンジンに在る解禁物を漏れなく1つの物差しで並べる。
        //   旧版は「財」（連鎖深ゲート）と「技術」（州に実在する代替製法）を別カテゴリで持ち、しかも進行中（0%<x<100%）だけを載せていたので、
        //   1783年はほぼ空・解禁した物は一覧から消え、既定製法の発明ゲート（RecipeName が空＝工場の既定レシピ）は1つも出なかった。
        //   新版＝(財, 製法名) をキーに畳み、未解禁0%も解禁済み100%も並べる。種別は「技術」1つ＝同じ財が2行に割れない。
        //   解禁度は ProductionDiag.TechGate（＝AdvancedTechScale×RecipeTechScale・EconomyStep が能力に掛ける同じ積）＝UIに第二の式を置かない。
        var tech = new List<(string Name, double Pct, double Onset, string Tip)>();
        var gates = new Dictionary<(int GoodId, string Recipe), double>();   // 値＝発見の閾値（同じキーは大きい方＝遅い方が実効）
        void Gate((int, string) key, double onset)
        {
            if (!gates.TryGetValue(key, out double prev) || onset > prev) gates[key] = onset;
        }
        // ① 既定製法（国に依らない知識＝賦存の無い財も消さない）。ゲートを1つも持たない財は発見の対象でないので載せない。
        foreach (var g in Catalog.Goods)
        {
            if (g.Tier == GoodTier.Service) continue;
            double onset = Catalog.DefaultRecipeOnset(g.Id, c);
            if (!(onset > 0) && !(Catalog.AdvancedOnset(g.Id) > 0)) continue;
            Gate((g.Id, ""), onset);
        }
        // ② 自国の州に実在する製法（代替製法＝水力発電/蒸気タービン/石油ケロシン/黄鉄鉱法…と、起動時に既定 Producer へ焼かれた鋼/汽船/機関車）。
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var pr in r.Producers)
                if (pr.TechOnset > 0) Gate((pr.GoodId, pr.RecipeName), pr.TechOnset);
        }
        foreach (var kv in gates)
        {
            (int gid, string recipe) = kv.Key;
            if (gid < 0 || gid >= Catalog.Goods.Length) continue;
            double s = ProductionDiag.TechGate(gid, kv.Value, w.TechLevel);
            double onset = System.Math.Max(kv.Value, Catalog.AdvancedOnset(gid));   // 実効の閾値＝2本のゲートの遅い方
            string name = recipe.Length > 0 ? $"{Catalog.Goods[gid].Name}（{recipe}）" : Catalog.Goods[gid].Name;
            tech.Add((name, s, onset, $"技術 {w.TechLevel:F2}"));   // ★2026-09-23（作者裁定）：閾値は画面に出さない・パーセントで出す＝onset は並べ替え ByGate の第2キーとしてだけ残す
        }
        // ③ 財に紐づかない世界のゲート（産業の形を変えるが Producer を持たないので①②では出ない）。式は全てエンジンと同一。
        if (c.RefrigerationEffect > 0 && c.RefrigerationIndSpan > 0)   // 冷蔵＝生鮮の腐敗低減（EconomyStep の在庫腐敗・工業化度が時計）
            tech.Add(("冷蔵", System.Math.Clamp((maxInd - c.RefrigerationIndOnset) / c.RefrigerationIndSpan, 0, 1), c.RefrigerationIndOnset,
                      $"最大工業化度 {maxInd:F2}"));
        if (c.SteamTechWidth > 0)   // 蒸気動力＝工場が石炭を焚けるか（EconomyStep ②の steamGate）
            tech.Add(("蒸気動力", System.Math.Clamp((w.TechLevel - c.SteamTechOnset) / c.SteamTechWidth, 0, 1), c.SteamTechOnset,
                      $"技術 {w.TechLevel:F2}"));
        if (c.RailTechWidth > 0)    // 鉄道輸送＝内陸輸送容量のランプ（Transport の railGate。制度の「鉄道」とは別量）
            tech.Add(("鉄道輸送", System.Math.Clamp((w.TechLevel - c.RailTechOnset) / c.RailTechWidth, 0, 1), c.RailTechOnset,
                      $"技術 {w.TechLevel:F2}"));
        // 近代港湾（MultiNationSim の港料）はエンジン側が閾値の可否＝ランプでない。同じ形（0か1）で見せる。
        tech.Add(("近代港湾", w.TechLevel >= c.PortTechOnset ? 1.0 : 0.0, c.PortTechOnset, $"技術 {w.TechLevel:F2}"));
        // 電信・鉄道・汽船＝報せの速さ（News の距離ラグが読む世界技術のランプ＝TransportRevolutionOnset/Span・AUD-05）。新しい財を足さず同じ量を見せる。
        if (c.TransportRevolutionSpan > 0)
        {
            double tr = System.Math.Clamp((w.WorldTechLevel - c.TransportRevolutionOnset) / c.TransportRevolutionSpan, 0, 1);
            if (!(tr >= 0)) tr = 0;
            tech.Add(("電信・鉄道・汽船（報せの速さ）", tr, c.TransportRevolutionOnset,
                      $"世界技術 {w.WorldTechLevel:F2}"));   // ★閾値の帯（〜）を出していた唯一の行＝2026-09-23 に撤去
        }
        // 鉱脈の発見（ゴールドラッシュの前提＝技術で解禁・2026-09-04に暦年を撤去）
        if (c.MineralDiscoveryTechRef > 0)
            tech.Add(("鉱脈の発見", System.Math.Clamp(w.TechLevel / c.MineralDiscoveryTechRef, 0, 1), c.MineralDiscoveryTechRef,
                      $"技術 {w.TechLevel:F2}"));
        // 並びは解禁度の降順→閾値の昇順→名前（全順序＝四半期ごとに行が入れ替わらない）。技術と思想で同じ比較子を使う。
        static int ByGate((string Name, double Pct, double Onset, string Tip) a, (string Name, double Pct, double Onset, string Tip) b)
        {
            int d = b.Pct.CompareTo(a.Pct);
            if (d != 0) return d;
            d = a.Onset.CompareTo(b.Onset);
            return d != 0 ? d : string.CompareOrdinal(a.Name, b.Name);
        }
        tech.Sort(ByGate);
        foreach (var t in tech) rows.Add(new DiscoveryRow(t.Name, "技術", t.Pct, $"{t.Pct:P0}", t.Tip));


        DiscoveryRows = rows;
        OnPropertyChanged(nameof(DiscoveryRows));
        OnPropertyChanged(nameof(DiscoveryLeft));
        OnPropertyChanged(nameof(DiscoveryRight));
    }
}
