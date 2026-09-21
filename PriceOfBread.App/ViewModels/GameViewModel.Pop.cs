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
    /// 1783年の初期状態は各国の初期法（world.json の polity／initialSerfEmancipation／bureaucracy）がそのまま決める＝国名分岐はゼロ。</summary>
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
            chips.Add(new PrivilegeChip { Name = SimCore.Privileges.Names[k], Held = held, Tip = held ? $"残高:{deg:P0}" : null });
        }
        chips.Insert(0, new PrivilegeChip { Name = "なし", Held = !any });
        return chips;
    }
}
