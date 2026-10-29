namespace PriceOfBread.SimCore.Display;

/// <summary>法の置き場（画面のどの表に出すか）。<see cref="NoRow"/> は画面の行を持たない法（報告にだけ載る）。</summary>
public enum LawSlot { Law, Econ, Nationalization, Welfare, NoRow }

/// <summary>法 1 本の定義（画面の法の表と Runner の play の報告が同じ 1 本を読む＝設計 §6-2・§7-4）。
/// 文字と読み口だけを持つ（盤面に書かない・状態を持たない）。書き込みは AI・議会と同じ <see cref="NationPolicy"/> の目標の欄だけを通る。
/// <para><see cref="Key"/>＝報告の書き込みの口（decision.txt の <c>キー=値</c>）が受ける NationPolicy の欄名（主）。<see cref="Fields"/>＝この行が読み書きする欄の全て（阿片は 2 欄）。</para>
/// <para><see cref="Stages"/>＝段の押し釦の名前。段の番号は <see cref="LevelBase"/> から（軍事費のように最下段が 1 の法がある）。<see cref="Current"/>・<see cref="Target"/>＝施行中の段・置いた目標の段（<see cref="Stages"/> の添字でなくエンジン側の段番号）。</para>
/// <para><see cref="Words"/>＝通知文に使う言い方（enum の序数で引く。null なら <see cref="Stages"/> と同じ）。<see cref="EnumType"/>＝その enum の型（通知文が型から言い方を引く）。</para></summary>
public sealed record LawDef(
    string Key, string Name, string Group, LawSlot Slot, PolicyAxis Axis, string[] Stages, string[] Fields,
    Func<World, int> Current, Func<World, int> Target, Action<NationPolicy, int> SetTarget,
    Func<World, bool>? Visible = null, Type? EnumType = null, string[]? Words = null, int LevelBase = 0)
{
    /// <summary>画面に出すか（休眠中の新法は出さない＝死んだ行を出さない）。</summary>
    public bool IsVisible(World w) => Visible is null || Visible(w);

    /// <summary>段の名前（エンジン側の段番号から）。範囲外は空。</summary>
    public string StageName(int level)
    {
        int i = level - LevelBase;
        return i >= 0 && i < Stages.Length ? Stages[i] : "";
    }
}

/// <summary>法の表（<see cref="LawDef"/> の一覧）。表の並びがそのまま画面の並び（群ごとに連続・群の最初の行が見出しを持つ）。
/// 法を足すときは本体の側のこの表に 1 行足す＝App と Runner は手を入れない。</summary>
public static class LawCatalog
{
    private static readonly string[] OnOff = ["なし", "あり"];

    private static LawDef Ladder(string key, string name, string group, PolicyAxis axis, string[] stages,
        Func<World, int> cur, Func<World, int> tgt, Action<NationPolicy, int> set, Func<World, bool>? vis = null,
        Type? enumType = null, string[]? words = null, LawSlot slot = LawSlot.Law, string[]? fields = null, int levelBase = 0)
        => new(key, name, group, slot, axis, stages, fields ?? [key], cur, tgt, set, vis, enumType, words, levelBase);

    private static int B(bool v) => v ? 1 : 0;

    /// <summary>阿片の梯子の段（許可 → 輸入禁止 → 輸出禁止 → 全面禁止）。エンジンの状態は 2 欄＝保有（<see cref="ReformLevel"/>）と輸出（bool）のまま、梯子は規制が 1 段ずつ増える順。
    /// 組み合わせが梯子に乗らない時は強い方へ丸める。</summary>
    private static int OpiumStage(ReformLevel possession, bool exportBan) => possession == ReformLevel.Full ? 3 : exportBan ? 2 : possession != ReformLevel.None ? 1 : 0;

    /// <summary>表の全体（画面の並び）。</summary>
    public static IReadOnlyList<LawDef> All { get; } =
    [
        // --- 政治参加 ---
        Ladder(nameof(NationPolicy.Suffrage), "参政権", "政治参加", PolicyAxis.Suffrage, ["なし", "制限選挙", "男性普通", "男女普通"],
            w => (int)w.Suffrage, w => (int)w.Policy.Suffrage, (p, v) => p.Suffrage = (Suffrage)v, null, typeof(Suffrage), ["参政権なし", "制限選挙", "男性普通選挙", "男女普通選挙"]),
        Ladder(nameof(NationPolicy.ColonialSuffrage), "植民地参政権", "政治参加", PolicyAxis.ColonialSuffrage, ["なし", "自国民のみ", "全面"],
            w => (int)w.ColonialSuffrage, w => (int)w.Policy.ColonialSuffrage, (p, v) => p.ColonialSuffrage = (ColonialSuffrageLaw)v, null, typeof(ColonialSuffrageLaw)),
        Ladder(nameof(NationPolicy.MigrantNativeSuffrage), "移民原住民参政権", "政治参加", PolicyAxis.MigrantNativeSuffrage, OnOff,
            w => (int)w.MigrantNativeSuffrage, w => (int)w.Policy.MigrantNativeSuffrage, (p, v) => p.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)v),
        Ladder(nameof(NationPolicy.SecretBallot), "秘密投票", "政治参加", PolicyAxis.SecretBallot, ["公開", "秘密"],
            w => (int)w.SecretBallot, w => (int)w.Policy.SecretBallot, (p, v) => p.SecretBallot = (SecretBallotLaw)v),
        Ladder(nameof(NationPolicy.Censorship), "報道", "政治参加", PolicyAxis.Censorship, ["国営のみ", "検閲", "自由報道"],
            w => (int)w.Censorship, w => (int)w.Policy.Censorship, (p, v) => p.Censorship = (CensorshipLaw)v, null, typeof(CensorshipLaw), ["国営報道のみ", "検閲", "自由報道"]),
        Ladder(nameof(NationPolicy.StateReligion), "国教", "政治参加", PolicyAxis.StateReligion, ["国教のみ", "内心の自由", "信仰の自由", "国家無神論"],
            w => (int)w.StateReligion, w => (int)w.Policy.StateReligion, (p, v) => p.StateReligion = (StateReligionLaw)v),   // 国家無神論＝前提でプロレタリア独裁のみ
        // --- 教育と労働（初等の最下段は宗教学校。「なし」はエンジンの無学社会の内部状態＝選択肢から除く＝序数に 1 を足して写す）---
        Ladder(nameof(NationPolicy.EducationLaw), "教育", "教育と労働", PolicyAxis.EducationLaw, ["宗教学校", "公教育", "義務教育"],
            w => Math.Max(0, (int)w.EducationLaw - 1), w => Math.Max(0, (int)w.Policy.EducationLaw - 1), (p, v) => p.EducationLaw = (EducationLaw)(v + 1),
            null, typeof(EducationLaw), ["教育なし", "宗教学校", "公教育", "義務教育"]),
        Ladder(nameof(NationPolicy.LaborUnion), "労働組合", "教育と労働", PolicyAxis.LaborUnion, ["非合法", "官許", "容認"],
            w => (int)w.LaborUnion, w => (int)w.Policy.LaborUnion, (p, v) => p.LaborUnion = (LaborUnionLaw)v, null, typeof(LaborUnionLaw)),
        Ladder(nameof(NationPolicy.WorkHours), "労働時間", "教育と労働", PolicyAxis.ShorterHours, ["8時間", "10時間", "12時間", "無制限"],
            w => (int)w.WorkHours, w => (int)w.Policy.WorkHours, (p, v) => p.WorkHours = (WorkHours)v, null, typeof(WorkHours)),
        Ladder(nameof(NationPolicy.ChildLaborAllowed), "児童労働", "教育と労働", PolicyAxis.ChildLabor, ["禁止", "合法"],
            w => B(w.ChildLaborAllowed), w => B(w.Policy.ChildLaborAllowed), (p, v) => p.ChildLaborAllowed = v == 1),
        Ladder(nameof(NationPolicy.MinimumWage), "最低賃金", "教育と労働", PolicyAxis.MinimumWage, ["なし", "生存", "生活", "充足"],
            w => (int)w.MinimumWage, w => (int)w.Policy.MinimumWage, (p, v) => p.MinimumWage = (MinimumWageLaw)v, w => w.Coef.MinimumWageLawGate > 0),   // ゲート 0 ＝非表示
        // --- 身分と土地 ---
        Ladder(nameof(NationPolicy.WomenRights), "女性の権利", "身分と土地", PolicyAxis.WomenRights, ["なし", "財産権", "教育権", "選挙権"],
            w => (int)w.WomenRights, w => (int)w.Policy.WomenRights, (p, v) => p.WomenRights = (WomenRightsLaw)v, null, typeof(WomenRightsLaw)),
        Ladder(nameof(NationPolicy.CornLaws), "穀物法", "身分と土地", PolicyAxis.CornLaws, ["なし", "限定的", "全面"],
            w => (int)w.CornLaws, w => (int)w.Policy.CornLaws, (p, v) => p.CornLaws = (ReformLevel)v, null, typeof(ReformLevel)),
        Ladder(nameof(NationPolicy.LandReform), "土地改革", "身分と土地", PolicyAxis.LandReform, ["なし", "限定的", "全面"],
            w => (int)w.LandReform, w => (int)w.Policy.LandReform, (p, v) => p.LandReform = (ReformLevel)v),
        Ladder(nameof(NationPolicy.SerfEmancipation), "農奴解放", "身分と土地", PolicyAxis.SerfEmancipation, ["なし", "限定的", "全面"],
            w => (int)w.SerfEmancipation, w => (int)w.Policy.SerfEmancipation, (p, v) => p.SerfEmancipation = (ReformLevel)v),
        // --- 国家と軍 ---
        Ladder(nameof(NationPolicy.ConscriptionLaw), "徴兵法", "国家と軍", PolicyAxis.ConscriptionLaw, ["志願兵", "選抜徴兵", "国民皆兵"],
            w => (int)w.ConscriptionLaw, w => (int)w.Policy.ConscriptionLaw, (p, v) => p.ConscriptionLaw = (ConscriptionLaw)v),
        Ladder(nameof(NationPolicy.WartimeRationing), "戦時配給", "国家と軍", PolicyAxis.WartimeRationing, OnOff,
            w => B(w.WartimeRationing), w => B(w.Policy.WartimeRationing), (p, v) => p.WartimeRationing = v == 1),
        Ladder(nameof(NationPolicy.MilitaryExportBan), "軍需輸出", "国家と軍", PolicyAxis.MilitaryExport, ["許可", "禁止"],
            w => B(w.MilitaryExportBan), w => B(w.Policy.MilitaryExportBan), (p, v) => p.MilitaryExportBan = v == 1),   // 禁止＝緊張の高い相手へは売らない（相手は法が選ぶ）
        // --- 統治機構 ---
        Ladder(nameof(NationPolicy.Bureaucracy), "官僚制度", "統治機構", PolicyAxis.Bureaucracy, ["猟官制", "資格任用", "専門官僚"],
            w => (int)w.Bureaucracy, w => (int)w.Policy.Bureaucracy, (p, v) => p.Bureaucracy = (BureaucracyLaw)v),
        Ladder(nameof(NationPolicy.Antitrust), "独占禁止", "統治機構", PolicyAxis.Antitrust, ["規制なし", "談合禁止", "独占の解体"],
            w => (int)w.Antitrust, w => (int)w.Policy.Antitrust, (p, v) => p.Antitrust = (AntitrustLaw)v, w => w.Coef.AntitrustGate > 0),
        // --- 国境管理 ---
        Ladder(nameof(NationPolicy.ImmigrationRestriction), "移民制限", "国境管理", PolicyAxis.ImmigrationRestriction, ["開放", "選別", "閉鎖"],
            w => (int)w.ImmigrationRestriction, w => (int)w.Policy.ImmigrationRestriction, (p, v) => p.ImmigrationRestriction = (ImmigrationLaw)v),
        Ladder(nameof(NationPolicy.OpiumPossession), "阿片", "国境管理", PolicyAxis.OpiumPossession, ["許可", "輸入禁止", "輸出禁止", "全面禁止"],
            w => OpiumStage(w.OpiumPossession, w.OpiumExportBan), w => OpiumStage(w.Policy.OpiumPossession, w.Policy.OpiumExportBan),
            (p, v) => { p.OpiumPossession = v >= 3 ? ReformLevel.Full : v >= 1 ? ReformLevel.Limited : ReformLevel.None; p.OpiumExportBan = v >= 2; },
            w => w.Coef.OpiumAddictionGrowth > 0, fields: [nameof(NationPolicy.OpiumPossession), nameof(NationPolicy.OpiumExportBan)]),
        // --- 群に載らない法 ---
        Ladder(nameof(NationPolicy.Guild), "同業組合", "", PolicyAxis.Guild, ["保護", "制限", "廃止"],
            w => (int)w.Guild, w => (int)w.Policy.Guild, (p, v) => p.Guild = (GuildLaw)v, w => w.Coef.GuildLawGate > 0),
        Ladder(nameof(NationPolicy.Inheritance), "相続", "", PolicyAxis.Inheritance, ["なし", "低率", "中率", "高率", "全額"],
            w => (int)w.Inheritance, w => (int)w.Policy.Inheritance, (p, v) => p.Inheritance = (InheritanceLaw)v, w => w.Coef.InheritanceLawGate > 0),   // ★線 inh (650)：ゲート 0 ＝非表示
        Ladder(nameof(NationPolicy.CivilianArmsBan), "民間の武器購入", "", PolicyAxis.CivilianArms, ["許可", "禁止"],
            w => B(w.CivilianArmsBan), w => B(w.Policy.CivilianArmsBan), (p, v) => p.CivilianArmsBan = v == 1, w => w.Coef.RebelArmsMarketEnabled > 0),   // 禁止しても漏れる＝反乱勢力の武器購入が有効な時だけ出す
        Ladder(nameof(NationPolicy.SpecialistExitLaw), "技術者流出禁止", "", PolicyAxis.SpecialistExitBan, ["自由", "制限", "禁止"],
            w => (int)w.SpecialistExitLaw, w => (int)w.Policy.SpecialistExitLaw, (p, v) => p.SpecialistExitLaw = (SpecialistExitLaw)v, slot: LawSlot.NoRow),   // 画面の行は無い（議会と AI だけが動かす）
        // --- 財政と貨幣の画面に置く法 ---
        Ladder(nameof(NationPolicy.MonetaryTarget), "通貨制度", "", PolicyAxis.MonetaryStandard, ["不換紙幣", "銀本位", "金本位"],
            w => (int)w.MonetaryLaw, w => Math.Clamp(w.Policy.MonetaryTarget, 0, 2), (p, v) => p.MonetaryTarget = v, slot: LawSlot.Econ),
        Ladder(nameof(NationPolicy.CapitalControlTarget), "資本移動", "", PolicyAxis.CapitalMobility, ["自由", "規制"],
            w => B(w.CapitalControlLaw), w => B(w.Policy.CapitalControlTarget), (p, v) => p.CapitalControlTarget = v == 1, slot: LawSlot.Econ),
        // 社会保障：最下段は救貧法。4 段＝目標の 0..1 を 3 倍して段へ写す（エンジンの導出と一致）。
        Ladder(nameof(NationPolicy.SocialSecurityLaw), "社会保障", "", PolicyAxis.SocialSecurity, ["救貧法", "労災", "失業保険", "年金"],
            w => (int)w.SocialSecurityLaw, w => Math.Clamp((int)Math.Round(w.Policy.SocialSecurityTarget * 3), 0, 3), (p, v) => p.SocialSecurityTarget = v / 3.0,
            enumType: typeof(SocialSecurityLaw), slot: LawSlot.Welfare, fields: [nameof(NationPolicy.SocialSecurityLaw), nameof(NationPolicy.SocialSecurityTarget)]),
        // --- 国有化（5 部門＋全て）---
        Ladder(nameof(NationPolicy.NationalizeRailways), "鉄道国有化", "", PolicyAxis.Nationalization, ["民営", "国有"],
            w => B(w.NationalizeRailways), w => B(w.Policy.NationalizeRailways), (p, v) => p.NationalizeRailways = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),
        Ladder(nameof(NationPolicy.NationalizeMines), "鉱山国有化", "", PolicyAxis.Nationalization, ["民営", "国有"],
            w => B(w.NationalizeMines), w => B(w.Policy.NationalizeMines), (p, v) => p.NationalizeMines = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),
        Ladder(nameof(NationPolicy.NationalizeMilitary), "軍需産業国有化", "", PolicyAxis.Nationalization, ["民営", "国有"],
            w => B(w.NationalizeMilitary), w => B(w.Policy.NationalizeMilitary), (p, v) => p.NationalizeMilitary = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),
        Ladder(nameof(NationPolicy.NationalizeOil), "石油国有化", "", PolicyAxis.Nationalization, ["民営", "国有"],
            w => B(w.NationalizeOil), w => B(w.Policy.NationalizeOil), (p, v) => p.NationalizeOil = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),
        Ladder(nameof(NationPolicy.NationalizeElectricity), "電力国有化", "", PolicyAxis.Nationalization, ["民営", "国有"],
            w => B(w.NationalizeElectricity), w => B(w.Policy.NationalizeElectricity), (p, v) => p.NationalizeElectricity = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),
        Ladder(nameof(NationPolicy.NationalizeAll), "全て国有化", "", PolicyAxis.NationalizationAll, ["民営", "国有"],
            w => B(w.NationalizeAll), w => B(w.Policy.NationalizeAll), (p, v) => p.NationalizeAll = v == 1, w => w.Coef.NationalizationGate > 0, slot: LawSlot.Nationalization),   // 残り全産業の一括＝プロレタリア独裁のみ
    ];

    /// <summary>置き場ごとの行（表の並びのまま）。</summary>
    public static IEnumerable<LawDef> In(LawSlot slot) => All.Where(l => l.Slot == slot);

    /// <summary>決定行の鍵（NationPolicy の欄名）で引く。主の欄と全ての欄のどちらでも引ける。無ければ null。</summary>
    public static LawDef? ByField(string field) => All.FirstOrDefault(l => l.Key == field) ?? All.FirstOrDefault(l => l.Fields.Contains(field));

    /// <summary>通知文の言い方（enum の値から）。表に無い型なら null。</summary>
    public static string? Word(Enum value)
    {
        foreach (var l in All)
        {
            if (l.EnumType != value.GetType()) continue;
            int ordinal = Convert.ToInt32(value);
            var words = l.Words ?? l.Stages;
            int i = ordinal - (l.Words is null ? l.LevelBase : 0);
            return i >= 0 && i < words.Length ? words[i] : null;
        }
        return null;
    }

    /// <summary>群の最初の行（見出しを持つ行）か。群を持たない行は false。</summary>
    public static bool IsGroupHead(LawDef law) => law.Group.Length > 0 && All.First(l => l.Slot == LawSlot.Law && l.Group == law.Group) == law;

    /// <summary>報告の 1 行（名前・現在の段・目標の段）。段の名前は画面と同じ。</summary>
    public static (string Name, string Current, string Target) StatusRow(LawDef law, World w)
        => (law.Name, law.StageName(law.Current(w)), law.StageName(law.Target(w)));
}
