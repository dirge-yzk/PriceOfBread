// GameViewModel.Discovery.cs ＝ 発見パネル（F・2026-08-31作者指示「UIはFボタンに配置」／2026-09-03にF1→Fへ）。
// 発見＝解禁の唯一の形の現在地をデータで見せる：制度の普及度（Discovery.Unlock）＋発見の表の行（Discovery.Evaluate＝2値・段2A）＋世界のゲートの進行。
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

        // ★2026-09-07（作者要望11「追加分をリストに追加」）＝エンジンに在る解禁物を漏れなく1つの物差しで並べる（未解禁0%も解禁済み100%も）。
        // ★2026-10-01 段2A：発見の表（Catalog.Discoveries＝行はレシピ）を全て並べる。発見は2値＝解禁済みは100%・まだの行は点数（技術力＋必要性＋伝わり）の割合
        //   ＝閾値は出さない・パーセントは出す（作者裁定 2026-09-23）。値は Discovery.Evaluate＝エンジンが判定に読む同じ関数（接触は直近の判定の値）。
        //   ホバー＝3項の値（データのみ）。並べ替えの第2キーは年代（旧＝onset）。
        var tech = new List<(string Name, double Pct, double Onset, string Tip)>();
        var terms = Discovery.Evaluate(w, w.LastDiscoveryView, c);
        for (int row = 0; row < Catalog.Discoveries.Length; row++)
        {
            var d = Catalog.Discoveries[row];
            bool known = Discovery.Known(w.Discovered, row);
            var tm = terms[row];
            double s = known ? 1.0 : System.Math.Clamp(tm.Score / Discovery.GateScore, 0, 1);
            if (!(s >= 0)) s = 0;
            tech.Add((d.Name, s, d.Decade, known ? $"{d.Decade}年代" : $"技術力 {tm.Tech:F0} ／ 必要性 {tm.Need:F0} ／ 伝わり {tm.Transmit:F0}"));
        }
        // ③ 財に紐づかない世界のゲート。★2026-10-01 (150)：蒸気動力・近代港湾（「蒸気機関」の行）・鉄道輸送（「機関車」の行）・冷蔵（「冷蔵」の行）は発見の表の行が開く
        //   ＝上の発見の表の行そのもの（旧＝技術水準の時計と工業化度の傾斜の4行＝発見と関係なく開いた）＝別の行を出さない。
        // 電信・鉄道・汽船＝報せの速さ（News の距離ラグが読む世界技術のランプ＝TransportRevolutionOnset/Span・AUD-05）。新しい財を足さず同じ量を見せる。
        if (c.TransportRevolutionSpan > 0)
        {
            double tr = System.Math.Clamp((w.WorldTechLevel - c.TransportRevolutionOnset) / c.TransportRevolutionSpan, 0, 1);
            if (!(tr >= 0)) tr = 0;
            tech.Add(("電信・鉄道・汽船（報せの速さ）", tr, c.TransportRevolutionOnset,
                      $"世界技術 {w.WorldTechLevel:F2}"));   // ★閾値の帯（〜）を出していた唯一の行＝2026-09-23 に撤去
        }
        // ★産地の発見（2026-10-01 (148)・Catalog.Sites＝大鉱床と植林）＝自国の州にある行だけ。見つかった産地は100%・まだの産地は点数（技術力＋必要性）の割合
        //   ＝値は Discovery.SiteTerms＝エンジンが判定に読む同じ関数。ホバー＝2項と州の名前（データのみ）。並べ替えの第2キーは年代。
        //   ★(149) 反証の指摘：エンジン（Discovery.Step）は稼働中の州だけを判定する＝画面も稼働中の州だけ（稼働していない州の産地は決して開かない点数を出していた）。
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                if (r.State != RegionState.Active) break;
                if (p.SiteRow < 0) continue;
                var site = Catalog.Sites[p.SiteRow];
                var st = Discovery.SiteTerms(w, r, p, c);
                double sp = p.SiteFound ? 1.0 : System.Math.Clamp(st.Score / Discovery.GateScore, 0, 1);
                if (!(sp >= 0)) sp = 0;
                tech.Add((site.Name, sp, site.Decade, p.SiteFound ? $"{r.Name}・{site.Decade}年代" : $"{r.Name}・技術力 {st.Tech:F0} ／ 必要性 {st.Need:F0}"));
            }
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
