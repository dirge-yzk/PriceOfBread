// GameViewModel.Law.cs ＝ 法律タブ＝法の一覧（LawRowVM）の構築・群分け・制度の状態表示。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // === 法律タブ（1934拡張E8）：段階を横並びボタンで出すデータ駆動リスト。改正可能なら発光・政治資本コスト／不可は理由（LawRowVM）。
    //     休眠中の新法（gate=0）は Visible=false で出さない（UI最小主義＝死んだ行を出さない）。E9でgate点火＝表示。
    public ObservableCollection<LawRowVM> Laws { get; } = new();
    // ★財政と貨幣のページに置く法（金本位・資本移動・社会保障）と税・支出・軍事費の段・レバーの 5 段は、(501) 段3 から本体の目録（SimCore/Display の FiscalTables・StageGate）が行を渡す＝ここには持たない。
    public ObservableCollection<LawRowVM> NationalizationLaws { get; } = new();   // 国有化6法（5部門＋『全て』＝§11-6d統合2026-08-15）は法律タブの右列（作者指示2026-08-15で生産タブから移動）
    public bool NationalizationVisible => _player.Coef.NationalizationGate > 0;   // パネルごとゲート（未点火ならパネル自体を出さない）

    // === 制度（研究C・2026-08-10 表示を新設）：特許制度・株式会社・鉄道。
    // これまでUIに状態がゼロだった（特許と株式会社は通知が1度飛ぶだけ・鉄道は通知すら無い）＝**効いているのに見えない**3機構。
    // ⚠施行の選択化はしない：発見の表の制度の行（特許・株式会社）と「機関車」の行（鉄道）を見つけた四半期にAI/プレイヤーを問わず自動で効く（2026-10-01 (152)・旧＝Institutions.Step）＝
    //   ボタンを置いても「押すだけ」になる（UI最小主義）。ここは状態の表示のみ。
    // ⚠エンジンでは**解禁と施行が同じ四半期**に起きる＝「解禁済みだが未施行」という中間状態は存在しない（2状態＝未解禁／施行済み）。
    // 解禁条件の数値表示は2026-08-11に撤去（L51「★解禁条件の数値は出さない」参照）。
    // ⚠撤去2026-08-23（読み手ゼロ）: StageLabelOf ＝制度が段ボタン→テキスト表示化（2026-08-15）で役割消滅。private＝バインドからも到達不能だった。
    /// <summary>制度（特許制度・株式会社・鉄道）＝**法律と同じ形で状態だけを出す**（作者指示2026-08-11）。
    /// 施行はプレイヤーの操作を受け付けない（条件を満たせばAI/プレイヤーを問わず自動で施行される機構）ので、
    /// 段ボタンは押せない形で置く＝法律一覧と見た目が揃い、押せる/押せないの違いがそのまま意味になる。
    /// ★解禁条件の数値は出さない（旧「開発度(最高の州) 8 / 21 で解禁」）＝プレイヤーが直接動かせる量ではない。</summary>
    public IReadOnlyList<InstitutionRow> Institutions =>
    [
        new("特許制度", _player.PatentLaw),
        new("株式会社", _player.JointStock),
        new("鉄道", _player.Railways),
    ];

    private void BuildLaws()
    {
        var w = _player;
        // ★法の表は本体（SimCore/Display の LawCatalog）が 1 本で持つ：名前・群・段の名前・現在と目標の読み口・出す条件・書き込み先。
        //   ここは表を回して行を作るだけ＝法を足すときに App を直さない。置き場（法律の表・財政の通貨と資本移動・国有化・社会保障）と
        //   群の見出し（群の最初の行）も表が決める。休眠中の新法（出す条件が偽）は行を作ったうえで Visible=false で出さない。
        foreach (var def in LawCatalog.All)
        {
            if (def.Slot is not (LawSlot.Law or LawSlot.Nationalization)) continue;   // 財政と貨幣のページの法と画面の行を持たない法は、ここでは行を作らない
            var row = new LawRowVM(w, def) { GroupHeader = def.Slot == LawSlot.Law && LawCatalog.IsGroupHead(def) ? def.Group : "" };
            switch (def.Slot)
            {
                case LawSlot.Law: Laws.Add(row); break;
                case LawSlot.Nationalization: NationalizationLaws.Add(row); break;
                // 財政と貨幣のページに置く法（通貨制度・資本移動・社会保障）は本体の目録が行を渡す（FiscalTables）＝ここでは行を作らない
            }
        }
    }
    private void RefreshLaws()
    {
        foreach (var l in Laws) l.Refresh();
        foreach (var l in NationalizationLaws) l.Refresh();
    }

    // ★死コード撤去（2026-08-10）：LawCornText/LawLandReformText/LawSerfText/LawPensionText と
    //   LawCornSupport/LawLandReformSupport/LawSerfSupport/LawPensionSupport（＋private SupportLine）を削除。
    //   2026-08-02に「議会立法」ブロックを撤去した時点でXAMLの参照が0件になり、以後は Refresh が毎期
    //   誰も購読していない8本へ通知を撒くだけだった。同じ情報は法律一覧の行（現行段の濃塗り）と
    //   段ボタンのホバー（StageHint＝賛否と押しの強さ）が持っている。

    // 借入：ボタンも画面の予約も無い（2026-09-28）＝エンジンが Fiscal の1行（国家の支払い率 r＜1 の月）で自動で借りる。表示は DebtHeaderText が毎tick追従。

    // === 外交アクション（同ランク間＝緊張上下/同盟・列強→弱小＝侵略・抽象標的＝統一戦争。実国家間の開戦＝緊張の創発のみ）===
    // 緊張の増減は即時。侵略（弱小へ）・統一戦争は確認モーダルを経る。
    private Power? FindPower(string? name) => name is null ? null : _sim.Diplomacy.Powers.Find(p => p.Name == name || p.BaseName == name);   // 両受け＝変名の四半期のCommandParameter（旧名）も引ける
    private Power Me => _sim.Diplomacy.Powers[_playerIndex];
}

/// <summary>制度の1行＝名前と施行の有無。段ボタンは法律と同じ見た目で、押せない（自動施行の機構だから）。</summary>
public sealed record InstitutionRow(string Title, bool Enacted)
{
    public bool NotEnacted => !Enacted;
}
