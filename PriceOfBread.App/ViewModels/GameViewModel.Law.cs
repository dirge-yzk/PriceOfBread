// GameViewModel.Law.cs ＝ 政治と法のページ（法の表）＝法の表（共通の表）の構築・段の押し下げの再読込。財政の段（税・支出・レバー）の行（LawRowVM）もここで作る（財政と貨幣のページが読む）。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // === 法律タブ（1934拡張E8）：段階を横並びボタンで出すデータ駆動リスト。改正可能なら発光・政治資本コスト／不可は理由（LawRowVM）。
    //     休眠中の新法（gate=0）は Visible=false で出さない（UI最小主義＝死んだ行を出さない）。E9でgate点火＝表示。
    /// <summary>法の行（LawRowVM）。画面の法の表は共通の表（<see cref="LawTable"/>）が描く＝この一覧の読み手は人口のページの特権の根拠の行（Pop.cs）だけ。
    /// 人口のページが法の表（<see cref="LawCatalog"/>）を直接読むようになったら、この一覧は消せる。</summary>
    public ObservableCollection<LawRowVM> Laws { get; } = new();
    // ★財政と貨幣のページに置く法（金本位・資本移動・社会保障）と税・支出・軍事費の段・レバーの 5 段は、(501) 段3 から本体の目録（SimCore/Display の FiscalTables・StageGate）が行を渡す。国有化の法と制度（特許制度・株式会社・鉄道）は政治と法のページの表（PoliticsTables）が映す＝ここには持たない。
    //   制度は発見の 2 値を読むだけの状態の表示で、施行の選択化はしない（条件を満たせば AI・プレイヤーを問わず自動で効く・2026-10-01 (152)）。

    private void BuildLaws()
    {
        var w = _player;
        // ★法の表は本体（SimCore/Display の LawCatalog）が 1 本で持つ：名前・群・段の名前・現在と目標の読み口・出す条件・書き込み先。
        //   ここは表を回して行を作るだけ＝法を足すときに App を直さない。置き場（法律の表・財政の通貨と資本移動・国有化・社会保障）と
        //   群の見出し（群の最初の行）も表が決める。休眠中の新法（出す条件が偽）は行を作ったうえで Visible=false で出さない。
        foreach (var def in LawCatalog.All)
        {
            if (def.Slot != LawSlot.Law) continue;   // 財政と貨幣のページの法・国有化・画面の行を持たない法は、ここでは行を作らない（それぞれ目録の表が映す）
            Laws.Add(new LawRowVM(w, def));
        }
    }
    private void RefreshLaws()
    {
        foreach (var l in Laws) l.Refresh();
        if (Layout.IsShown(Slot.PoliticsLaw)) RefreshLawTables();   // 見えているページだけ更新する
    }

    // === 政治と法のページの表（共通の表）。行は本体の目録（PoliticsTables）が組み、鍵で値だけ流す（表を作り直さない）。
    [ObservableProperty] private TableSnapshot _lawTable = TableSnapshot.Empty;

    /// <summary>政治と法のページが枠に載った瞬間の更新（以後は見えている間の毎回の Refresh が追従する）。</summary>
    public void RefreshPoliticsLawPage() => RefreshLawTables();

    /// <summary>法の表を読み直す（制度の表は発見のページ＝RefreshDiscovery）。段の押し釦を押した直後と、ページが見えた瞬間と、見えている間の毎回の更新。</summary>
    private void RefreshLawTables()
    {
        LawTable = TableSnapshot.From(AfterSelect(PoliticsTables.Laws.Read(_sim, _player), RefreshLawTables));
    }

    /// <summary>表の段の押し釦を押した直後に <paramref name="after"/> を呼ぶ形に包む（目標を置いた後の表を読み直す）。置く先は本体の口のまま（AI・議会と同じ門）。</summary>
    internal static TableData AfterSelect(TableData d, Action after)
        => d with
        {
            Rows = d.Rows.Select(r => r with
            {
                Cells = r.Cells.Select(c => c is StageCell st ? st with { Select = v => { st.Select(v); after(); } } : c).ToList(),
            }).ToList(),
        };

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
