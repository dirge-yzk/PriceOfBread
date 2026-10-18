// GameViewModel.Pop.cs ＝ POPタブ＝階級別の行と文化/宗教の構成・児童労働。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // POPタブ：上流（貴族・聖職・資本家）／中流（専門職・官僚・職人・商人）／下流（労働者・農民・兵士・使用人）の3層。
    public ObservableCollection<PopRow> PopsUpper { get; } = new();
    public ObservableCollection<PopRow> PopsMiddle { get; } = new();
    public ObservableCollection<PopRow> PopsLower { get; } = new();
    // POPタブ：文化・宗教の構成（州ラベルを州人口で加重したシェア降順）。World.ReligiousUnity/EthnicUnity は「最大シェア」の
    // スカラーなので、その内訳をここで開く（#62・表示のみ＝SimCoreは読むだけで較正/自己対戦に無影響）。
    public ObservableCollection<CompositionRow> ReligionRows { get; } = new();
    public ObservableCollection<CompositionRow> CultureRows { get; } = new();
    public string ChildLaborLine { get; private set; } = "";   // 02断線走査2026-07-28：児童労働率（創発量）のPOPタブ表示

    // ★2026-09-24：ヘッダーから畳まれた内訳の**受け取り**（PopTab.axaml:21-28／:69-74 が置き場を確定済み・前の回はここが未結線で内訳が落ちていた）。
    //   値は GameViewModel.Header.cs の PopulationTerms／LiteracyClassTerms／SatisfactionClassTerms／MilitancyClassTerms を
    //   **呼ぶだけ**＝このファイルに第二の式を置かない（層の分け方も各層の値もあちらの1本が持つ）。
    //   ⚠ PopRow（GameViewModel.Rows.cs）と行の組み立て（GameViewModel.Refresh.cs）はこの枠の持ち物ではないので、
    //     行ごとのプロパティは足さず **VM のプロパティ1本**にした＝規模のセルは $parent 経由でこれを見る（WarTab.axaml:195 と同じ作法）。
    //   null＝吹き出しを出さない（PrivilegeChip の「なし」と同じ作法。空文字だと空の吹き出しが出る）。
    [ObservableProperty] private string? _populationTip;    // 規模のセル：人口の保存則3項＋今四半期の移民（流入/流出）＋年齢構成
    [ObservableProperty] private string? _popsUpperTip;     // 上流の見出し：識字率/満足度/闘争度のその層の実値
    [ObservableProperty] private string? _popsMiddleTip;    // 中流の見出し：同上
    [ObservableProperty] private string? _popsLowerTip;     // 下流の見出し：同上

    /// <summary>POPタブのホバー4本を組む（毎Refresh 1回）。⚠呼び手は <see cref="BuildParliament"/> の先頭
    /// ＝Refresh.cs がこの枠の持ち物でないため、毎Refresh 1回だけ通る点でこの枠の中にある唯一の場所がそこだった。
    /// Refresh.cs を触れる枠は BuildComposition／BuildParliament と同じ並びへ移してよい（挙動は同じ）。</summary>
    internal void BuildPopTips()
    {
        string body = "";
        foreach (var t in PopulationTerms()) body += (body.Length == 0 ? "" : "\n") + t.Text;
        // 題の1行＝ヘッダーの人口 Stat と同じ名前・同じ書式。階級ごとの規模セルに載る内訳なので、全国の量であることを題が名乗る。
        PopulationTip = body.Length == 0 ? null : $"人口　{PopFmt(_player.TotalPopulation)}\n" + body;
        var lit = LiteracyClassTerms(); var sat = SatisfactionClassTerms(); var mil = MilitancyClassTerms();
        PopsUpperTip = PopLayerTip("上流", lit, sat, mil);
        PopsMiddleTip = PopLayerTip("中流", lit, sat, mil);
        PopsLowerTip = PopLayerTip("下流", lit, sat, mil);
    }

    /// <summary>層の見出し1つぶんのホバー＝3本のリストからその層の行だけを拾い、POP表の列と同じ名前を冠する。
    /// 層の名前と値は Header.cs の3関数が組んだ文字列をそのまま使う（値を組み直さない＝桁も丸めも1箇所）。
    /// その層の pop が居ない（3関数が行を出さない）なら null＝吹き出しを出さない。</summary>
    private static string? PopLayerTip(string layer, List<StatTerm> lit, List<StatTerm> sat, List<StatTerm> mil)
    {
        string s = "";
        foreach (var (name, rows) in new (string Name, List<StatTerm> Rows)[] { ("識字率", lit), ("満足度", sat), ("闘争度", mil) })
            foreach (var t in rows)
                if (t.Text.StartsWith(layer, StringComparison.Ordinal))
                    s += (s.Length == 0 ? "" : "\n") + name + t.Text.Substring(layer.Length);   // 「上流　45%」→「識字率　45%」
        return s.Length == 0 ? null : s;
    }

    // ★増減の向き（作者指示2026-08-16(2)(3)）＝四半期スタンプ方式（GameViewModel.Fiscal.cs の _prevQuarterStamp が手本）。
    //   Refreshは月次でも呼ばれる＝毎回差分を取ると1ヶ月ぶんの微動しか出ない→四半期境界でのみ差分を確定し、境界間は前回確定値を表示。
    //   旧「増減」列は撤去し、規模セル/失業者セルの末尾の矢印（↑→↓）へ畳んだ。閾値は旧増減列と同一（PopTrendFlatK）。
    private readonly Dictionary<int, double> _prevPopSize = new();     // キー=ClassId・前四半期境界の全国規模（千人）
    private readonly Dictionary<int, double> _popSizeDelta = new();    // キー=ClassId・確定済みの四半期増減（千人）
    private readonly Dictionary<int, double> _prevPopUnemp = new();    // キー=ClassId・前四半期境界の失業者数（千人）
    private readonly Dictionary<int, double> _popUnempDelta = new();   // キー=ClassId・確定済みの四半期増減（千人）
    private int _prevPopQuarterStamp = -1;   // 年×4＋四半期。-1=未捕捉＝初回は基準化のみ（矢印なし）

    /// <summary>横ばいと見なす幅（千人）＝PopFmtK が丸めて0Kになる幅。旧「増減」列の「±0」判定をそのまま流用。</summary>
    private const double PopTrendFlatK = 0.5;

    /// <summary>四半期境界でのみ差分を確定し、確定済みの差分を返す（未確定＝null）。規模と失業者で共有する1本の物差し。</summary>
    private static double? StampPopDelta(Dictionary<int, double> prev, Dictionary<int, double> delta, int key, double now, bool boundary)
    {
        if (boundary)
        {
            if (prev.TryGetValue(key, out var p)) delta[key] = now - p;
            prev[key] = now;
        }
        return delta.TryGetValue(key, out var d) ? d : null;
    }

    /// <summary>増減の向き＝矢印と色。未確定は空文字（矢印そのものを出さない）・横ばいは「→」。増=緑/減=赤（既存パレット）。</summary>
    private static (string Arrow, IBrush Brush) PopTrend(double? delta)
        => delta is not double d ? ("", TermNeutral)
         : Math.Abs(d) < PopTrendFlatK ? ("→", TermNeutral)
         : d > 0 ? ("↑", TermPlus) : ("↓", TermMinus);

    /// <summary>POPの人数を「保守/自由/社会」の3族へ配る（単位＝千人・作者指示2026-08-16(1)）。
    /// 政治意識のある人数 Size×Consciousness を IdeologyShares の族比で割る＝残り Size×(1−Consciousness) が「なし」。
    /// 族への写像（PoliticalQuantities.FamilyOf）と、族が全く無いときの退避（＝保守）は Parliament.ComputeVotes と同一式。</summary>
    private static (double Cons, double Lib, double Soc) PopIdeologyHeads(Pop p)
    {
        double pc = 0, pl = 0, ps = 0;
        for (int i = 0; i < Ideologies.Count; i++)
        {
            int fam = PoliticalQuantities.FamilyOf(i);
            if (fam == 0) pc += p.IdeologyShares[i]; else if (fam == 1) pl += p.IdeologyShares[i]; else ps += p.IdeologyShares[i];
        }
        double sm = pc + pl + ps; if (sm < 1e-9) { pc = 1; sm = 1; }
        double aware = p.Size * Math.Clamp(p.Consciousness, 0, 1);
        return (aware * pc / sm, aware * pl / sm, aware * ps / sm);
    }

    /// <summary>その階級が**持ちうる**特権を1行ぶん組む（作者指示2026-09-07(7)(8)）＝先頭が「なし」・以下に特権名。
    /// 保有（残高>0）だけが墨になる＝法律タブの段と同じ2状態の見せ方。機構が休眠の特権（同業組合＝GuildLawGate=0）は並べない。
    /// 残高は <see cref="SimCore.Privileges.Degree"/>（法・政体・工業化から導く観測量）＝UIは新しい状態も判断も持たない。
    /// 1783年の初期状態は各国の初期法（world.json の polity／initialSerfEmancipation／bureaucracy）がそのまま決める＝国名分岐はゼロ。
    /// ★2026-09-23（作者指示「カーソルを合わせた時に内容が出るように」）：**保有・非保有を問わず全ての語にホバーを出す**。
    ///   旧は非保有が Tip=null＝カーソルを合わせても何も出ず、淡い語が何を指すのか画面から辿れなかった（保有でも「残高:n%」だけ）。
    ///   中身＝特権の名前／根拠（法の題名:現在の段。法でない身分制免税は政体）／残高／受益する階級＝すべて「名前:値」の作法。
    ///   ⚠取り分（PrivilegeTakeYear）・強度（PrivilegeHeldYears）・剥離の怒りは PrivilegeLedgerEnabled=0 で StepMonth が
    ///     1度も呼ばれない＝常に0（HeldYears は初期値 −1 のまま）＝**出さない**。今の世界で動くのは残高だけ。
    ///   ⚠「なし」の語は Tip なしのまま（null＝吹き出しを出さない既存作法。説明文を書かない）。</summary>
    private List<PrivilegeChip> PopPrivilegeChips(int classId)
    {
        var chips = new List<PrivilegeChip>();
        bool any = false;
        for (int k = 0; k < SimCore.Privileges.Count; k++)
        {
            if (!SimCore.Privileges.HeldBy(k, classId) || !SimCore.Privileges.Enabled(_player.Coef, k)) continue;
            double deg = SimCore.Privileges.Degree(_player, _player.Coef, k);
            bool held = deg > 0;   // NaN は held=false へ落ちる
            any |= held;
            string tip = SimCore.Privileges.Names[k];
            string? src = PopPrivilegeSourceLine(k);
            if (src is not null) tip += "\n" + src;
            tip += "\n残高:" + (!(deg >= 0) ? "—" : deg.ToString("P0"));   // 否定形＝NaN は「—」（Math.Clamp は NaN を除去しない・失業者セルと同じ記号）
            string ben = PopPrivilegeBeneficiaries(k);
            if (ben.Length > 0) tip += "\n受益:" + ben;
            chips.Add(new PrivilegeChip { Name = SimCore.Privileges.Names[k], Held = held, Tip = tip });
        }
        chips.Insert(0, new PrivilegeChip { Name = "なし", Held = !any });
        return chips;
    }

    /// <summary>特権 k の残高を決めている根拠の1行「題名:現在の段」（2026-09-23）＝<see cref="SimCore.Privileges.Degree"/> の分岐を**そのまま映す**
    /// （UIに第二の判定式を置かない）。段の語は法律タブの行（<see cref="Laws"/>）から読む＝法律タブと同じ文字列になる（第二の名前表を作らない）。
    /// これで非保有の語も「どの法がそれを消したか」が読める（例：領主権／農奴解放:全面／残高:0%）。
    /// null＝根拠が法でも政体でもない腕＝行そのものを出さない（嘘の根拠を書かない）。</summary>
    private string? PopPrivilegeSourceLine(int kind)
    {
        var c = _player.Coef;
        // 身分制免税だけは法律タブに行が無い＝政体が決める（PrivilegeFromCrownShare>0 の腕では王権の取り分＝法でも政体でもないので出さない）。
        if (kind == SimCore.Privileges.TaxExemption)
            return c.PrivilegeFromCrownShare.Value > 0 ? null : "政体:" + PolityRegimeJa(_player.Polity);
        string? title = kind switch
        {
            SimCore.Privileges.Tithe => "国教",
            SimCore.Privileges.VestedOffice => "官僚制度",
            SimCore.Privileges.Seigneurial => "農奴解放",
            SimCore.Privileges.Monopoly => c.AntitrustGate > 0 ? "独占禁止" : null,     // ゲート0＝MonopolyRentFactor 常に1.0＝法は効かない
            SimCore.Privileges.Guild => "同業組合",
            _ => null,
        };
        if (title is null) return null;
        foreach (var r in Laws)
            if (r.Title == title)
                foreach (var s in r.Stages)
                    if (s.IsCurrent) return title + ":" + s.Label;
        return title;   // 段が見つからない（法律タブ未構築）＝法の題名だけ
    }

    /// <summary>特権 k を実際に受ける階級の名前（2026-09-23）。身分制免税だけ2つ＝貴族と聖職者（<see cref="SimCore.Privileges.HeldBy"/> 1本で判定
    /// ＝取り分の分子と受益の分母を同じ人口で揃える）。名前は pop の ClassName＝POP表の階級名と**同じ文字列**（第二の階級名表を作らない）。</summary>
    private string PopPrivilegeBeneficiaries(int kind)
    {
        string s = "";
        for (int cls = 0; cls < PopClass.Count; cls++)
        {
            if (!SimCore.Privileges.HeldBy(kind, cls)) continue;
            foreach (var p in _player.AllPops())
                if (p.ClassId == cls && p.ClassName.Length > 0) { s += (s.Length == 0 ? "" : "・") + p.ClassName; break; }
        }
        return s;
    }
}
