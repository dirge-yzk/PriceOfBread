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
        var w = _player; var p = _player.Policy; var c = _player.Coef;
        void Add(string title, PolicyAxis? axis, string[] labels, Func<int> cur, Func<int> tgt, Action<int> set, Func<bool>? vis = null)
            => Laws.Add(new LawRowVM(w, axis, title, labels, cur, tgt, set, vis));
        void AddEcon(string title, PolicyAxis? axis, string[] labels, Func<int> cur, Func<int> tgt, Action<int> set, Func<bool>? vis = null)
            => EconLaws.Add(new LawRowVM(w, axis, title, labels, cur, tgt, set, vis));   // 財政タブへ置く法（金本位）
        void AddNat(string title, PolicyAxis? axis, string[] labels, Func<int> cur, Func<int> tgt, Action<int> set, Func<bool>? vis = null)
            => NationalizationLaws.Add(new LawRowVM(w, axis, title, labels, cur, tgt, set, vis));
        // --- 常設の法（開始年から選べる）---
        Add("参政権", PolicyAxis.Suffrage, ["なし", "制限選挙", "男性普通", "男女普通"], () => (int)w.Suffrage, () => (int)p.Suffrage, v => p.Suffrage = (Suffrage)v);
        // 参政権の範囲2法（作者裁定2026-08-16・旧の旗 SuffrageScopeGate は 2026-10-18 に焼き込み＝常に表示）。
        Add("植民地参政権", PolicyAxis.ColonialSuffrage, ["なし", "自国民のみ", "全面"], () => (int)w.ColonialSuffrage, () => (int)p.ColonialSuffrage, v => p.ColonialSuffrage = (ColonialSuffrageLaw)v);
        Add("移民原住民参政権", PolicyAxis.MigrantNativeSuffrage, ["なし", "あり"], () => (int)w.MigrantNativeSuffrage, () => (int)p.MigrantNativeSuffrage, v => p.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v);
        // 教育：初等の最下段は宗教学校（「なし」＝EducationLaw.None は無学社会の内部状態としてエンジンに残すが、プレイヤーの選択肢からは除外。実在国は宗教学校始点＝+1オフセットで写す）。
        Add("教育", PolicyAxis.EducationLaw, ["宗教学校", "公教育", "義務教育"], () => Math.Max(0, (int)w.EducationLaw - 1), () => Math.Max(0, (int)p.EducationLaw - 1), v => p.EducationLaw = (EducationLaw)(v + 1));
        Add("報道", PolicyAxis.Censorship, ["国営のみ", "検閲", "自由報道"], () => (int)w.Censorship, () => (int)p.Censorship, v => p.Censorship = (CensorshipLaw)v);
        Add("労働組合", PolicyAxis.LaborUnion, ["非合法", "官許", "容認"], () => (int)w.LaborUnion, () => (int)p.LaborUnion, v => p.LaborUnion = (LaborUnionLaw)v);
        Add("労働時間", PolicyAxis.ShorterHours, ["8時間", "10時間", "12時間", "無制限"], () => (int)w.WorkHours, () => (int)p.WorkHours, v => p.WorkHours = (WorkHours)v);
        Add("児童労働", PolicyAxis.ChildLabor, ["禁止", "合法"], () => w.ChildLaborAllowed ? 1 : 0, () => p.ChildLaborAllowed ? 1 : 0, v => p.ChildLaborAllowed = v == 1);   // 02断線走査2026-07-28：軸化＝無料スイッチ→政治ゲート。現在値も法の現在値（浸透度でなく）
        // ★「軍部優位」の行は2026-08-18に撤去（作者裁定「厳密には法ではなく政治機構だ」）＝軍国度は軍事費（財政タブの陸軍/海軍維持費）と
        //   徴兵法（下の「国家と軍」群）から導出される量になった＝法律タブで直接押すレバーは存在しない。
        // ★女性の権利（統合法・2026-08-10 作者裁定）：旧＝「女性財産権」「女性参政権」「女性教育権」の3行（あり/なし）→ 1行4段の梯子。
        //   段の並びが史実の順序（財産権→教育権→選挙権）＝遠い段は前提が満たされるまで自動で灰色になり、理由がホバーに出る（参政権と同じ作法）。
        Add("女性の権利", PolicyAxis.WomenRights, ["なし", "財産権", "教育権", "選挙権"], () => (int)w.WomenRights, () => (int)p.WomenRights, v => p.WomenRights = (WomenRightsLaw)v);
        // ★★3法の開放（設計＝docs/202608/20260802_3法の開放_部分完了.md【完了】.md・2026-08-02）：穀物法・土地改革・農奴解放。
        // ⚠これまで**議会とAIだけが動かせてプレイヤーは触れなかった**（対称性の違反）。機構は他22軸と完全に同じで、
        //   代価（政治資本・不可逆・前提法）は既に実装済み＝Add を3行足すだけで完結する。
        // ★ラベルは「なし／限定的／全面」＝変更通知（JaEnumConverter）と同じ語彙にする（同じ法を2種類の言葉で語らない）。
        // ★土地改革に表示条件を付けない（検地台帳の錠は (383) で物理削除＝最初から開いている）。
        Add("穀物法", PolicyAxis.CornLaws, ["なし", "限定的", "全面"], () => (int)w.CornLaws, () => (int)p.CornLaws, v => p.CornLaws = (ReformLevel)v);
        Add("土地改革", PolicyAxis.LandReform, ["なし", "限定的", "全面"], () => (int)w.LandReform, () => (int)p.LandReform, v => p.LandReform = (ReformLevel)v);
        Add("農奴解放", PolicyAxis.SerfEmancipation, ["なし", "限定的", "全面"], () => (int)w.SerfEmancipation, () => (int)p.SerfEmancipation, v => p.SerfEmancipation = (ReformLevel)v);
        // ⚠年金は行にしない：Government の法適用（w.Pension の導出）で **社会保障の法から導出**される（w.Pension = SocialSecurityLaw==Pension ? Full : None）＝
        //   独立に設定できず、行にすると「押しても効かないボタン」になる。社会保障の法の行が既にそれを表している。
        // --- 1934拡張の法（gate点火で有効＝gate=0では非表示）---
        Add("徴兵法", PolicyAxis.ConscriptionLaw, ["志願兵", "選抜徴兵", "国民皆兵"], () => (int)w.ConscriptionLaw, () => (int)p.ConscriptionLaw, v => p.ConscriptionLaw = (ConscriptionLaw)v);
        // ★2026-08-01：SimCore に入れた2つの法がUIに1行も無く、**AIと議会だけが動かせてプレイヤーが動かせない**状態だった（対称性の違反）。
        Add("移民制限", PolicyAxis.ImmigrationRestriction, ["開放", "選別", "閉鎖"], () => (int)w.ImmigrationRestriction, () => (int)p.ImmigrationRestriction, v => p.ImmigrationRestriction = (ImmigrationLaw)v);
        // ★3値化（銀本位doc G4・2026-08-13／点火1.0=2026-08-21銀本位ラウンド＝現行は3択）：gate=0に戻したときは中段が存在しないので2択のまま出す＝押せない段を並べない。
        if (c.SilverStandardGate > 0)
            AddEcon("通貨制度", PolicyAxis.MonetaryStandard, ["不換紙幣", "銀本位", "金本位"], () => (int)w.MonetaryLaw, () => Math.Clamp(p.MonetaryTarget, 0, 2), v => p.MonetaryTarget = v);
        else
            AddEcon("通貨制度", PolicyAxis.MonetaryStandard, ["不換紙幣", "金本位"], () => w.GoldStandardLaw ? 1 : 0, () => p.MonetaryTarget >= 2 ? 1 : 0, v => p.MonetaryTarget = v == 1 ? 2 : 0);
        // ★資本移動（トリレンマdoc・2026-08-13）。通貨制度の真下＝この2行の組が「金利が自分のものか」を決める（並べれば説明文は要らない＝思想⑦）。
        AddEcon("資本移動", PolicyAxis.CapitalMobility, ["自由", "規制"], () => w.CapitalControlLaw ? 1 : 0, () => p.CapitalControlTarget ? 1 : 0, v => p.CapitalControlTarget = v == 1);
        // 社会保障：最下段は救貧法（「なし」廃止＝救貧法が福祉水準0。2026-07-21）。4段＝target×3で写す（エンジン Government の導出と一致）。
        // ★2026-09-23（作者裁定「社会保障の段を財政タブへ」）：行は **WelfareLaw** として財政タブの社会保障の金額行の直下に置く。
        //   法律タブの「教育と労働」群からは外した（下の groups からも外してある）＝同じ法を2箇所に出さない。
        WelfareLaw = new LawRowVM(w, PolicyAxis.SocialSecurity, "社会保障", ["救貧法", "労災", "失業保険", "年金"], () => (int)w.SocialSecurityLaw, () => Math.Clamp((int)Math.Round(p.SocialSecurityTarget * 3), 0, 3), v => p.SocialSecurityTarget = v / 3.0);
        Add("秘密投票", PolicyAxis.SecretBallot, ["公開", "秘密"], () => (int)w.SecretBallot, () => (int)p.SecretBallot, v => p.SecretBallot = (SecretBallotLaw)v);
        Add("官僚制度", PolicyAxis.Bureaucracy, ["猟官制", "資格任用", "専門官僚"], () => (int)w.Bureaucracy, () => (int)p.Bureaucracy, v => p.Bureaucracy = (BureaucracyLaw)v);
        Add("国教", PolicyAxis.StateReligion, ["国教のみ", "内心の自由", "信仰の自由", "国家無神論"], () => (int)w.StateReligion, () => (int)p.StateReligion, v => p.StateReligion = (StateReligionLaw)v);   // 国家無神論＝前提DAGでプロレタリア独裁のみ（例外①）
        Add("独占禁止", PolicyAxis.Antitrust, ["規制なし", "談合禁止", "独占の解体"], () => (int)w.Antitrust, () => (int)p.Antitrust, v => p.Antitrust = (AntitrustLaw)v, () => c.AntitrustGate > 0);
        Add("同業組合", PolicyAxis.Guild, ["保護", "制限", "廃止"], () => (int)w.Guild, () => (int)p.Guild, v => p.Guild = (GuildLaw)v, () => c.GuildLawGate > 0);   // ★特権 段4（2026-09-07）
        Add("戦時配給", PolicyAxis.WartimeRationing, ["なし", "あり"], () => w.WartimeRationing ? 1 : 0, () => p.WartimeRationing ? 1 : 0, v => p.WartimeRationing = v == 1);
        Add("軍需輸出", PolicyAxis.MilitaryExport, ["許可", "禁止"], () => w.MilitaryExportBan ? 1 : 0, () => p.MilitaryExportBan ? 1 : 0, v => p.MilitaryExportBan = v == 1);   // 禁制品doc C5（2026-07-30）：禁止＝緊張の高い相手へは売らない（相手は法が選ぶ）
        Add("民間の武器購入", PolicyAxis.CivilianArms, ["許可", "禁止"], () => w.CivilianArmsBan ? 1 : 0, () => p.CivilianArmsBan ? 1 : 0, v => p.CivilianArmsBan = v == 1, () => c.RebelArmsMarketEnabled > 0);   // ★反乱勢力の武器購入（2026-09-11）＝禁止しても Contraband.LeakFraction で漏れる。RebelArmsMarketEnabled=0 では行を出さない（効かないレバーを置かない＝同業組合と同じ作法）。AIは SelfPlay.DecideModernLaws が同じ法を同じ条件で動かす＝対称
        // 阿片の法（禁制品doc C3・2026-07-30／★2026-08-11に作者指示で**2行を1本の梯子へ統合**）。
        //   エンジンの状態は2つ（OpiumPossession＝保有/輸入・OpiumExportBan＝輸出）のままで、UIだけが1本の梯子として見せる
        //   ＝規制が1段ずつ増える順に並べる：許可 → 輸入禁止 → 輸出禁止（＋輸入も禁止のまま）→ 全面禁止（保有まで）。
        //   ⚠議会は2軸を別々に動かせるので、梯子に乗らない組み合わせ（輸出だけ禁止 等）が起こりうる。
        //     現在段は**強い方へ丸める**（下の cur）＝どの組み合わせでも必ず1つの段に落ちる。政治ゲートは保有軸で代表する。
        int OpiumStage() => w.OpiumPossession == ReformLevel.Full ? 3 : w.OpiumExportBan ? 2 : w.OpiumPossession != ReformLevel.None ? 1 : 0;
        int OpiumTarget() => p.OpiumPossession == ReformLevel.Full ? 3 : p.OpiumExportBan ? 2 : p.OpiumPossession != ReformLevel.None ? 1 : 0;
        Add("阿片", PolicyAxis.OpiumPossession, ["許可", "輸入禁止", "輸出禁止", "全面禁止"], OpiumStage, OpiumTarget,
            v => { p.OpiumPossession = v >= 3 ? ReformLevel.Full : v >= 1 ? ReformLevel.Limited : ReformLevel.None; p.OpiumExportBan = v >= 2; },
            () => c.OpiumAddictionGrowth > 0);
        // 国有化6行＝5部門＋『全て』（§11-6d統合・作者裁定2026-08-14。全行 NationalizationGate 点火時のみ表示）
        Func<bool> natVis = () => c.NationalizationGate > 0;
        AddNat("鉄道国有化", PolicyAxis.Nationalization, ["民営", "国有"], () => w.NationalizeRailways ? 1 : 0, () => p.NationalizeRailways ? 1 : 0, v => p.NationalizeRailways = v == 1, natVis);
        AddNat("鉱山国有化", PolicyAxis.Nationalization, ["民営", "国有"], () => w.NationalizeMines ? 1 : 0, () => p.NationalizeMines ? 1 : 0, v => p.NationalizeMines = v == 1, natVis);
        AddNat("軍需産業国有化", PolicyAxis.Nationalization, ["民営", "国有"], () => w.NationalizeMilitary ? 1 : 0, () => p.NationalizeMilitary ? 1 : 0, v => p.NationalizeMilitary = v == 1, natVis);
        AddNat("石油国有化", PolicyAxis.Nationalization, ["民営", "国有"], () => w.NationalizeOil ? 1 : 0, () => p.NationalizeOil ? 1 : 0, v => p.NationalizeOil = v == 1, natVis);
        AddNat("電力国有化", PolicyAxis.Nationalization, ["民営", "国有"], () => w.NationalizeElectricity ? 1 : 0, () => p.NationalizeElectricity ? 1 : 0, v => p.NationalizeElectricity = v == 1, natVis);
        // 『全て』＝残り全産業の一括国有化（旧・通信/製造業/農業/小売の統合）。専用軸 NationalizationAll ＝LawPrereq がソ連型以外で不可を返し、
        // 既存のグレーアウト機構（CanReach→LawPrereq）がそのまま「プロレタリア独裁のみ」をホバーに出す（国家無神論と同じ作法）。
        AddNat("全て国有化", PolicyAxis.NationalizationAll, ["民営", "国有"], () => w.NationalizeAll ? 1 : 0, () => p.NationalizeAll ? 1 : 0, v => p.NationalizeAll = v == 1, natVis);

        // ★法の群分け（2026-08-13・法の棚卸しdoc L1「A案」）：21本をフラットに並べていたので、
        //   探すのに全部読む必要があった。**並び順も挙動も1行も変えず、見出しを挟むだけ**＝SimCore 不接触・帯リスク0。
        //   群は「何を規律する法か」で切る（政治参加／教育と労働／身分と土地／国家と軍／統治機構／人の出入り）。
        //   ⚠見出しは群の最初に**実際に存在する行**へ付ける（表示条件で消える行に付けると見出しごと消える）。
        var groups = new (string Header, string[] Titles)[]
        {
            ("政治参加", ["参政権", "植民地参政権", "移民原住民参政権", "秘密投票", "報道", "国教"]),   // 参政権の範囲2法（2026-08-16）＝参政権の直後。見出しは先頭の「参政権」（常設行）に付く＝gate=0で2行が消えても見出しは残る
            ("教育と労働", ["教育", "労働組合", "労働時間", "児童労働"]),   // ★2026-09-23：社会保障は財政タブへ移設（WelfareLaw）＝この群から外す。見出しは先頭の「教育」に付くので見出しは残る
            ("身分と土地", ["女性の権利", "穀物法", "土地改革", "農奴解放"]),
            ("国家と軍", ["徴兵法", "戦時配給", "軍需輸出"]),
            ("統治機構", ["官僚制度", "独占禁止"]),
            ("国境管理", ["移民制限", "阿片"]),   // 改名2026-08-16（作者指示「人と物の出入りはダサい」）
        };
        var order = new List<LawRowVM>();
        foreach (var (header, titles) in groups)
        {
            bool first = true;
            foreach (var t in titles)
            {
                var row = Laws.FirstOrDefault(r => r.Title == t);
                if (row is null) continue;
                if (first) { row.GroupHeader = header; first = false; }
                order.Add(row);
            }
        }
        foreach (var r in Laws) if (!order.Contains(r)) order.Add(r);   // 群に載せ忘れた法があっても落とさない
        Laws.Clear();
        foreach (var r in order) Laws.Add(r);

        // ★財政の段（作者裁定2026-09-12 R1「全ての税に関するスライダーを廃止し『なし/低/中/高』の4段階のボタンにします。
        //   これなら法として扱え反乱に乗る他、権力を無視して課税できなくなる」／R2「軍事費は『低/中/高/戦時』の4段」）。
        //   ⚠関税（R3）・行政費・近代化投資（R4）・政策金利は**この段の法の対象外**＝PolicyAxis を持たず議会を通らない。
        //     ⚠2026-09-23に「スライダーのまま」ではなくなった（作者裁定＝4本とも下の Lever で5段ボタンになり、財政タブから
        //       スライダーは1本も無くなった）。段になったのは操作の粒度だけで、議会を通さないという上の裁定は据え置き。
        //   段の値・可否・現在段は**全てエンジンの1本**（Government.TaxStage/MilitaryStage/MilitaryTotalTarget/LawPrereq/GateInfo）から引く
        //   ＝UIに第二の判定式を置かない。AIも同じ Policy のフィールドを同じ段で置く（SelfPlay.GateAllLawTargets）＝対称。
        // 語彙は税と支出で分ける（漢字1字の略称は使わない＝UI規律）。軍事費は支出の語彙＋最上段が戦時経済。
        string[] taxLabels = ["なし", "低率", "中率", "高率"];
        string[] spendLabels = ["なし", "少額", "中額", "多額"];
        // 現在段＝**施行値**の段／目標段＝**目標値**の段（Government.CurrentStage と同じ写像）。押した段はそのまま Government.TaxValue の目標になる。
        // ★錨2026-09-18：段↔率の写像は軸ごとの錨（Government.TaxLawTop）を引く2本（TaxStage/TaxValue）だけ＝UIに第二の式（v/3）を置かない。
        //   ラベル「なし/低率/中率/高率」＝0／最上段の1/3／2/3／最上段（所得税なら 0／11.7／23.3／35%）＝全て19世紀の実在帯。
        LawRowVM Staged(string title, PolicyAxis axis, string[] labels, Func<double> cur, Func<double> tgt, Action<double> set)
            => new(w, axis, title, labels, () => Government.TaxStage(axis, cur()), () => Government.TaxStage(axis, tgt()), v => set(Government.TaxValue(axis, v)));
        IncomeTaxLaw = Staged("所得税", PolicyAxis.IncomeTax, taxLabels, () => w.TaxRate.Value, () => p.IncomeTaxTarget, v => p.IncomeTaxTarget = v);
        ProgressiveTaxLaw = Staged("累進課税", PolicyAxis.ProgressiveTax, taxLabels, () => w.ProgressiveTax, () => p.ProgressiveTaxTarget, v => p.ProgressiveTaxTarget = v);
        PollTaxLaw = Staged("人頭税", PolicyAxis.PollTax, taxLabels, () => w.PollTax, () => p.PollTaxTarget, v => p.PollTaxTarget = v);
        LandTaxLaw = Staged("土地税", PolicyAxis.LandTax, taxLabels, () => w.LandTax, () => p.LandTaxTarget, v => p.LandTaxTarget = v);
        EduSpendLaw = Staged("教育費", PolicyAxis.EducationSpending, spendLabels, () => w.EducationSpending, () => p.EducationSpendingTarget, v => p.EducationSpendingTarget = v);
        // 軍事費＝合計だけが段（R2・D4：陸海を別々に段化すると平時0.25×海軍比上限0.60=0.15＜第1境界0.175 で全AIの海軍が段0＝英国に軍が無い）。
        // levelBase:1＝「なし」の段を作らない（Government.LawPrereq が段0を「軍事費に『なし』は無い」で拒む＝押せないボタンを並べない）。
        MilitaryLaw = new LawRowVM(w, PolicyAxis.Coercion, "軍事費", ["少額", "中額", "多額", "戦時"],
            () => Government.MilitaryStage(w.MilitarySpending.Value),
            () => (int)Math.Round(Government.MilitaryTotalTarget(w, p) * Government.MilitaryLawStages),
            v => p.MilitarySpendingTarget = v / (double)Government.MilitaryLawStages,
            levelBase: 1);
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
