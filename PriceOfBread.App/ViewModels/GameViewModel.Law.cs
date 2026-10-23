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
    /// <summary>財政タブに置く法（作者指示2026-08-11＝金本位に関する法は経済の画面で扱いたい）。
    /// ★法としての扱いは同じ（議会の政治ゲートを通る＝LawRowVM そのもの）＝置き場所だけが違う。
    /// 法律タブとは**排他**＝同じ法を2箇所に出さない（金本位の二重実装を潰した趣旨をここでも守る）。</summary>
    public ObservableCollection<LawRowVM> EconLaws { get; } = new();
    public ObservableCollection<LawRowVM> NationalizationLaws { get; } = new();   // 国有化6法（5部門＋『全て』＝§11-6d統合2026-08-15）は法律タブの右列（作者指示2026-08-15で生産タブから移動）
    public bool NationalizationVisible => _player.Coef.NationalizationGate > 0;   // パネルごとゲート（未点火ならパネル自体を出さない）

    // === 財政の段（作者裁定2026-09-12 R1/R2・2026-09-20 点火）：税・支出・軍事費を法と同じ段ボタンにする（税・支出のスライダーは廃止＝段ボタンへ一本化）。
    //     行は財政タブの各項目の直下に**1本ずつ**置く（金額の予測/実効列と交互に並ぶので ItemsControl では組めない）。
    public LawRowVM? IncomeTaxLaw { get; private set; }
    public LawRowVM? ProgressiveTaxLaw { get; private set; }
    public LawRowVM? PollTaxLaw { get; private set; }
    public LawRowVM? LandTaxLaw { get; private set; }
    public LawRowVM? EduSpendLaw { get; private set; }
    public LawRowVM? MilitaryLaw { get; private set; }   // 軍事費の**合計**（陸海の比は2026-09-23にレバーごと廃止＝陸と海の必要量が置く〔(151)・旧＝地理〕＝UtilityAgent.SetNavalUpkeepShare）
    /// <summary>社会保障の法（作者裁定2026-09-23＝法律タブから財政タブへ移設。金本位・資本移動と同じ扱い）。
    /// 法としての扱いは不変（議会の政治ゲートを通る＝LawRowVM そのもの）＝置き場所だけが違う。法律タブからは外した（同じ法を2箇所に出さない）。</summary>
    public LawRowVM? WelfareLaw { get; private set; }
    // === レバーの5段（作者裁定2026-09-23「行政費・近代化投資・関税・金利のバーも5段のボタンにする」）＝**法ではない**4本。
    //     PolicyAxis を持たない＝議会を通さない（行政費＝行政府の内部事項／関税はR3・近代化投資はR4・政策金利＝段の法の対象外、という既存の裁定は据え置き）。
    public LawRowVM? AdminSpendLever { get; private set; }
    public LawRowVM? ModernizeLever { get; private set; }
    public LawRowVM? TariffLever { get; private set; }
    public LawRowVM? RateLever { get; private set; }
    private const int LeverStages = 4;   // レバーの段の**区間数**＝ボタンは5個（Government.TaxLawStages と同じ流儀＝段数を数字で散らさない）
    private LawRowVM[] _fiscalRows = [];

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
        var w = _player; var c = _player.Coef;
        // ★法の表は本体（SimCore/Display の LawCatalog）が 1 本で持つ：名前・群・段の名前・現在と目標の読み口・出す条件・書き込み先。
        //   ここは表を回して行を作るだけ＝法を足すときに App を直さない。置き場（法律の表・財政の通貨と資本移動・国有化・社会保障）と
        //   群の見出し（群の最初の行）も表が決める。休眠中の新法（出す条件が偽）は行を作ったうえで Visible=false で出さない。
        foreach (var def in LawCatalog.All)
        {
            var row = new LawRowVM(w, def) { GroupHeader = def.Slot == LawSlot.Law && LawCatalog.IsGroupHead(def) ? def.Group : "" };
            switch (def.Slot)
            {
                case LawSlot.Law: Laws.Add(row); break;
                case LawSlot.Econ: EconLaws.Add(row); break;   // 財政タブへ置く法（金本位・資本移動）
                case LawSlot.Nationalization: NationalizationLaws.Add(row); break;
                case LawSlot.Welfare: WelfareLaw = row; break;   // 社会保障は財政タブの金額行の直下
            }
        }

        // ★財政の段（作者裁定2026-09-12 R1/R2）：税・教育費・軍事費を法と同じ段ボタンにする。表は本体の FiscalCatalog.Stages。
        //   ⚠関税（R3）・行政費・近代化投資・政策金利は**この段の法の対象外**＝PolicyAxis を持たず議会を通らない（下の Lever 4 本）。
        //   段の値・可否・現在段は**全てエンジンの 1 本**（Government.TaxStage/MilitaryStage/MilitaryTotalTarget/LawPrereq/GateInfo）から引く
        //   ＝UIに第二の判定式を置かない。AIも同じ Policy のフィールドを同じ段で置く（SelfPlay.GateAllLawTargets）＝対称。
        LawRowVM Fiscal(string key) => new(w, FiscalCatalog.Stages.First(s => s.Key == key));
        IncomeTaxLaw = Fiscal(nameof(NationPolicy.IncomeTaxTarget));
        ProgressiveTaxLaw = Fiscal(nameof(NationPolicy.ProgressiveTaxTarget));
        PollTaxLaw = Fiscal(nameof(NationPolicy.PollTaxTarget));
        LandTaxLaw = Fiscal(nameof(NationPolicy.LandTaxTarget));
        EduSpendLaw = Fiscal(nameof(NationPolicy.EducationSpendingTarget));
        MilitaryLaw = Fiscal(nameof(NationPolicy.MilitarySpendingTarget));
        // ★レバーの5段（作者裁定2026-09-23）。4本とも既にスライダーの TickFrequency=0.05 の**21点の離散値**だったので、
        //   5段化は**粗視化だけ**＝エンジンは1ビットも変わらない（書き込み口も既存のプロパティ setter をそのまま通る＝クランプを二重に書かない）。
        //   段の値＝そのレバー自身の**下限と上限を4等分**して Government.Q5（エンジンの0.05格子）へ乗せた5点。
        //   ⚠新しい上限・下限は1つも作らない（既存の TariffFloor／TariffCeil／InterestCeil／0..1 を読むだけ）＝関税は -25%/0%/25%/50%/最大。
        //   ⚠格子へ乗せる理由＝施行側（Government）が Q5 を掛けるので、乗せないと「関税 13%　実効 15%」が永久に一致しない。
        //   ラベルは値そのもの（画面はデータだけ）。最上段だけ「最大」＝そのレバーの上限（関税75%・政策金利50%・他は100%）。
        double LeverValue(double lo, double hi, int k) => Government.Q5(lo + (hi - lo) * k / (double)LeverStages);
        int LeverStage(double lo, double hi, double v)
            => !(hi - lo > 1e-9) ? 0 : Math.Clamp((int)Math.Round((v - lo) / (hi - lo) * LeverStages, MidpointRounding.AwayFromZero), 0, LeverStages);   // 否定形＝NaN/退化した幅は最下段へ
        LawRowVM Lever(string title, double lo, double hi, Func<double> cur, Func<double> tgt, Action<double> set)
        {
            var labels = new string[LeverStages + 1];
            for (int i = 0; i < labels.Length; i++) labels[i] = i == LeverStages ? "最大" : LeverValue(lo, hi, i).ToString("P0");
            return new LawRowVM(w, null, title, labels, () => LeverStage(lo, hi, cur()), () => LeverStage(lo, hi, tgt()), v => set(LeverValue(lo, hi, v)));
        }
        // 現在段＝**施行値**／目標段＝**目標値**（税の段と同じ写像）。書き込みは財政タブの既存プロパティ＝上限クランプと通知が1箇所に残る。
        AdminSpendLever = Lever("行政費", 0, 1, () => w.AdminSpending, () => AdminSpending, v => AdminSpending = v);
        ModernizeLever = Lever("近代化投資", 0, 1, () => w.ModernizationFrac, () => ModernizationInvest, v => ModernizationInvest = v);   // 目標と施行が同じ1本（World.ModernizationFrac）＝即時
        TariffLever = Lever("関税", c.TariffFloor, TariffCeil, () => w.Tariff, () => Tariff, v => Tariff = v);
        RateLever = Lever("政策金利", 0, InterestCeil, () => w.InterestRate, () => InterestLever, v => InterestLever = v);
        _fiscalRows = [IncomeTaxLaw, ProgressiveTaxLaw, PollTaxLaw, LandTaxLaw, EduSpendLaw, MilitaryLaw,
                       WelfareLaw, AdminSpendLever, ModernizeLever, TariffLever, RateLever];
    }
    private void RefreshLaws()
    {
        foreach (var l in Laws) l.Refresh();
        foreach (var l in EconLaws) l.Refresh();
        foreach (var l in NationalizationLaws) l.Refresh();
        foreach (var l in _fiscalRows) l.Refresh();   // 財政の段（R1/R2）
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
