using System.Reflection;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段0：表示の目録（SimCore/Display）の番人。機構と不変条件だけを見る（画面の見た目は --uishot が見る）。
/// 目録は文字と読み口だけ＝盤面を書かない。ここで固定するのは、表の抜け・重なり・名前の規則・書き込みの往復。</summary>
public class DisplayCatalogTests
{
    private static World Board() => new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789).Nations[0].World;

    // ===== キー（作者の指示 2026-10-23「QWERASDFZXCV の優先度で、順に埋める」・2026-11-04「生産は Tab・市場を Q・地図のキーは無し」）=====

    [Fact]
    public void キーは生産がTab_残りのページが作者の優先度の列を前から順_地図にキーは無く_空きが残る()
    {
        Assert.Equal("QWERASDFZXCV", PageKeys.Priority);
        var pages = Enum.GetValues<Page>();
        Assert.Equal(7, pages.Length);
        Assert.Equal('\0', PageKeys.Key(Page.Production));
        var lettered = pages.Where(p => p != Page.Production).ToArray();
        Assert.Equal("QWERAS", new string(lettered.Select(PageKeys.Key).ToArray()));
        Assert.Equal("DFZXCV", PageKeys.FreeKeys);
        // 重なりが無く、ページと空きで列が過不足なく埋まる
        string all = new string(lettered.Select(PageKeys.Key).ToArray()) + PageKeys.FreeKeys;
        Assert.Equal(PageKeys.Priority, all);
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void ページの名前と押し釦の文字は規則に合い_文字のキーから引ける()
    {
        foreach (var p in Enum.GetValues<Page>())
        {
            Assert.True(DisplayNames.IsValid(PageKeys.Name(p)), $"ページの名前が規則に合わない: {p}");
            if (p == Page.Production) continue;   // 生産は Tab＝文字で引かない
            Assert.Equal($"{PageKeys.Key(p)}:{PageKeys.Name(p)}", PageKeys.ButtonText(p));
            Assert.Equal(p, PageKeys.PageOfKey(PageKeys.Key(p)));
            Assert.Equal(p, PageKeys.PageOfKey(char.ToLowerInvariant(PageKeys.Key(p))));
        }
        Assert.Null(PageKeys.PageOfKey('F'));   // 地図のキーは無い
        Assert.Null(PageKeys.PageOfKey('\0'));
        Assert.Null(PageKeys.PageOfKey('Z'));
        Assert.Equal("生産", PageKeys.Name(Page.Production));
        Assert.Equal("Tab:生産", PageKeys.ButtonText(Page.Production));
        Assert.Equal("Q:市場・清算・貿易", PageKeys.ButtonText(Page.Market));
        Assert.True(DisplayNames.IsValid(PageKeys.MapName));
    }

    [Fact]
    public void 名前の規則_漢字1字とかっこの補足と空は落とす()
    {
        Assert.False(DisplayNames.IsValid("税"));
        Assert.False(DisplayNames.IsValid("税収（月）"));
        Assert.False(DisplayNames.IsValid("price(x)"));
        Assert.False(DisplayNames.IsValid(" "));
        Assert.True(DisplayNames.IsValid("税収"));
        Assert.True(DisplayNames.IsValid("A"));   // 漢字でない 1 字は許す（英字・かな）
    }

    // ===== 法の表 =====

    [Fact]
    public void 法の表は_NationPolicyの法の欄を全て持ち_欄は重ならず_全ての欄が実在する()
    {
        var policyFields = typeof(NationPolicy).GetFields(BindingFlags.Public | BindingFlags.Instance);
        var lawFields = policyFields.Where(f => f.FieldType.IsEnum || f.FieldType == typeof(bool)).Select(f => f.Name).ToList();
        var listed = LawCatalog.All.SelectMany(l => l.Fields).ToList();
        Assert.Equal(listed.Count, listed.Distinct().Count());
        var missing = lawFields.Where(n => !listed.Contains(n)).ToList();
        Assert.True(missing.Count == 0, "法の表に無い NationPolicy の法の欄：" + string.Join(",", missing));
        var allNames = policyFields.Select(f => f.Name).Concat(typeof(NationPolicy).GetProperties().Select(p => p.Name)).ToHashSet();
        Assert.All(listed, n => Assert.Contains(n, allNames));
        Assert.Equal(LawCatalog.All.Count, LawCatalog.All.Select(l => l.Key).Distinct().Count());
        Assert.All(LawCatalog.All, l => Assert.Contains(l.Key, l.Fields));
    }

    [Fact]
    public void 法の表の名前と段の名前は規則に合い_段は2つ以上あり_群は連続し_見出しは群ごとに1つ()
    {
        Assert.All(LawCatalog.All, l =>
        {
            Assert.True(DisplayNames.IsValid(l.Name), $"法の名前が規則に合わない: {l.Name}");
            Assert.True(l.Stages.Length >= 2, $"段が 2 つ未満: {l.Name}");
            Assert.All(l.Stages, s => Assert.True(DisplayNames.IsValid(s), $"段の名前が規則に合わない: {l.Name}/{s}"));
            if (l.Group.Length > 0) Assert.True(DisplayNames.IsValid(l.Group));
        });
        // 群の行は表の中で連続している（間に別の群が挟まらない）
        var groups = LawCatalog.In(LawSlot.Law).Select(l => l.Group).ToList();
        var seen = new HashSet<string>();
        string prev = "<none>";
        foreach (var g in groups)
        {
            if (g != prev && !seen.Add(g)) Assert.Fail($"群が分断されている: {g}");
            prev = g;
        }
        var heads = LawCatalog.In(LawSlot.Law).Where(LawCatalog.IsGroupHead).ToList();
        Assert.Equal(new[] { "政治参加", "教育と労働", "身分と土地", "国家と軍", "統治機構", "国境管理" }, heads.Select(l => l.Group).ToArray());
    }

    [Fact]
    public void 法の表_目標を各段へ置くと読み戻しが同じ段になる_現在の段は段の範囲に収まる()
    {
        var w = Board();
        foreach (var l in LawCatalog.All)
        {
            int cur = l.Current(w);
            Assert.True(l.StageName(cur).Length > 0, $"現在の段が範囲外: {l.Name} {cur}");
            Assert.True(l.StageName(l.Target(w)).Length > 0, $"目標の段が範囲外: {l.Name}");
            var saved = w.Policy.Clone();
            for (int i = 0; i < l.Stages.Length; i++)
            {
                int level = l.LevelBase + i;
                l.SetTarget(w.Policy, level);
                Assert.True(level == l.Target(w), $"{l.Name} の段 {level} を置いて読み戻すと {l.Target(w)}");
                Assert.Equal(l.Stages[i], l.StageName(l.Target(w)));
            }
            w.Policy.CopyFrom(saved);
        }
    }

    [Fact]
    public void 法の表_阿片の梯子は2欄を書き_社会保障の梯子は目標の3分割へ写す()
    {
        var w = Board();
        var opium = LawCatalog.ByField(nameof(NationPolicy.OpiumExportBan))!;
        Assert.Same(opium, LawCatalog.ByField(nameof(NationPolicy.OpiumPossession)));
        opium.SetTarget(w.Policy, 2);   // 輸出禁止の段
        Assert.Equal(ReformLevel.Limited, w.Policy.OpiumPossession);
        Assert.True(w.Policy.OpiumExportBan);
        opium.SetTarget(w.Policy, 3);
        Assert.Equal(ReformLevel.Full, w.Policy.OpiumPossession);
        opium.SetTarget(w.Policy, 0);
        Assert.Equal(ReformLevel.None, w.Policy.OpiumPossession);
        Assert.False(w.Policy.OpiumExportBan);
        var ss = LawCatalog.ByField(nameof(NationPolicy.SocialSecurityLaw))!;
        ss.SetTarget(w.Policy, 3);
        Assert.Equal(1.0, w.Policy.SocialSecurityTarget, 9);
        ss.SetTarget(w.Policy, 1);
        Assert.Equal(1.0 / 3.0, w.Policy.SocialSecurityTarget, 9);
    }

    [Fact]
    public void 通知文の言い方_段のボタンと同じ語彙で_旧の変換表と同じ()
    {
        // 旧 JaEnumConverter の 1 対 1 の写し（画面の通知文を 1 文字も変えない）
        Assert.Equal("教育なし", LawCatalog.Word(EducationLaw.None));
        Assert.Equal("宗教学校", LawCatalog.Word(EducationLaw.Religious));
        Assert.Equal("公教育", LawCatalog.Word(EducationLaw.Public));
        Assert.Equal("義務教育", LawCatalog.Word(EducationLaw.Compulsory));
        Assert.Equal("国営報道のみ", LawCatalog.Word(CensorshipLaw.StatePressOnly));
        Assert.Equal("検閲", LawCatalog.Word(CensorshipLaw.Censorship));
        Assert.Equal("自由報道", LawCatalog.Word(CensorshipLaw.FreePress));
        Assert.Equal("非合法", LawCatalog.Word(LaborUnionLaw.Illegal));
        Assert.Equal("官許", LawCatalog.Word(LaborUnionLaw.Controlled));
        Assert.Equal("容認", LawCatalog.Word(LaborUnionLaw.Tolerated));
        Assert.Equal("8時間", LawCatalog.Word(WorkHours.Eight));
        Assert.Equal("10時間", LawCatalog.Word(WorkHours.Ten));
        Assert.Equal("12時間", LawCatalog.Word(WorkHours.Twelve));
        Assert.Equal("無制限", LawCatalog.Word(WorkHours.Unlimited));
        Assert.Equal("参政権なし", LawCatalog.Word(Suffrage.None));
        Assert.Equal("制限選挙", LawCatalog.Word(Suffrage.Census));
        Assert.Equal("男性普通選挙", LawCatalog.Word(Suffrage.AdultMale));
        Assert.Equal("男女普通選挙", LawCatalog.Word(Suffrage.Universal));
        Assert.Equal("なし", LawCatalog.Word(ReformLevel.None));
        Assert.Equal("限定的", LawCatalog.Word(ReformLevel.Limited));
        Assert.Equal("全面", LawCatalog.Word(ReformLevel.Full));
        Assert.Equal("救貧法", LawCatalog.Word(SocialSecurityLaw.PoorLaw));
        Assert.Equal("労災", LawCatalog.Word(SocialSecurityLaw.WorkersComp));
        Assert.Equal("失業保険", LawCatalog.Word(SocialSecurityLaw.Unemployment));
        Assert.Equal("年金", LawCatalog.Word(SocialSecurityLaw.Pension));
        Assert.Equal("なし", LawCatalog.Word(WomenRightsLaw.None));
        Assert.Equal("財産権", LawCatalog.Word(WomenRightsLaw.Property));
        Assert.Equal("教育権", LawCatalog.Word(WomenRightsLaw.Education));
        Assert.Equal("選挙権", LawCatalog.Word(WomenRightsLaw.Suffrage));
        Assert.Equal("なし", LawCatalog.Word(ColonialSuffrageLaw.None));
        Assert.Equal("自国民のみ", LawCatalog.Word(ColonialSuffrageLaw.OwnNationals));
        Assert.Equal("全面", LawCatalog.Word(ColonialSuffrageLaw.Full));
        Assert.Null(LawCatalog.Word(Polity.Republic));   // 表に無い型は null（呼び手が既定の文字へ落とす）
    }

    [Fact]
    public void 報告の1行は画面と同じ日本語の名前と段の名前で出る()
    {
        var w = Board();
        w.Policy.LaborUnion = LaborUnionLaw.Tolerated;
        var def = LawCatalog.ByField(nameof(NationPolicy.LaborUnion))!;
        var (name, cur, tgt) = LawCatalog.StatusRow(def, w);
        Assert.Equal("労働組合", name);
        Assert.Equal(def.StageName((int)w.LaborUnion), cur);
        Assert.Equal("容認", tgt);
    }

    // ===== 財政の項の表 =====

    [Fact]
    public void 財政の項の表は台帳の区分を全て1度ずつ持ち_名前は規則に合う()
    {
        var cats = Enum.GetValues<TreasuryCat>().Where(c => c != TreasuryCat.Count).ToList();
        Assert.Equal(cats, FiscalCatalog.Items.Select(i => i.Cat).ToList());   // 宣言の順・過不足なし
        Assert.All(FiscalCatalog.Items, i => Assert.True(DisplayNames.IsValid(i.Name), $"台帳の名前が規則に合わない: {i.Cat}"));
        Assert.Equal(FiscalCatalog.Items.Count, FiscalCatalog.Items.Select(i => i.Name).Distinct().Count());
        // 収入側が先・支出側が後（Runner の報告は並びで読む）
        var incomeLast = FiscalCatalog.Items.ToList().FindLastIndex(i => i.Side == FiscalSide.Income);
        var outlayFirst = FiscalCatalog.Items.ToList().FindIndex(i => i.Side == FiscalSide.Outlay);
        Assert.True(incomeLast < outlayFirst);
        Assert.Equal(new[] { TreasuryCat.Tax, TreasuryCat.Tariff, TreasuryCat.StateMonopoly, TreasuryCat.Borrow, TreasuryCat.Devalue, TreasuryCat.StateFactory, TreasuryCat.TributeIn, TreasuryCat.OtherIn },
            FiscalCatalog.Cats(FiscalSide.Income).ToArray());
        Assert.Equal("その他の支出", FiscalCatalog.LedgerName(TreasuryCat.OtherOut));
        Assert.Equal("税収", FiscalCatalog.LedgerName(TreasuryCat.Tax));
    }

    [Fact]
    public void 財政の段の行_軸は重ならず_目標を各段へ置くと読み戻しが同じ段になる()
    {
        var w = Board();
        Assert.Equal(FiscalCatalog.Stages.Count, FiscalCatalog.Stages.Select(s => s.Axis).Distinct().Count());
        Assert.Equal(new[] { "所得税", "累進課税", "人頭税", "土地税", "教育費", "軍事費" }, FiscalCatalog.Stages.Select(s => s.Name).ToArray());
        foreach (var s in FiscalCatalog.Stages)
        {
            Assert.True(DisplayNames.IsValid(s.Name));
            var saved = w.Policy.Clone();
            for (int i = 0; i < s.Stages.Length; i++)
            {
                int level = s.LevelBase + i;
                s.SetTarget(w.Policy, level);
                // 軍事費の最上段（戦時）は交戦中かつ総動員の間だけ開く＝平時は 1 つ下で読み戻す（エンジンの梯子の上限）
                int expect = s.Axis == PolicyAxis.Coercion && i == s.Stages.Length - 1 && !(w.AtWarNow && w.Mobilized) ? level - 1 : level;
                Assert.True(expect == s.Target(w), $"{s.Name} の段 {level} を置いて読み戻すと {s.Target(w)}");
            }
            w.Policy.CopyFrom(saved);
        }
    }

    // ===== 表の一覧 =====

    [Fact]
    public void 表の一覧_表の鍵は重ならず_列の鍵と名前は規則に合い_数の列は単位を持つ()
    {
        Assert.Equal(17, TableCatalog.All.Count);   // 表を足したら一覧と番人の期待を同じ回で直す（統合：段3 の 3 枝の静的な表を全部載せて 15＋階級の既定の形で 16＋発見の政府と思想〔2026-11-11 2 段組み〕で 17。行を呼ぶ側が渡す表〔支出・収入・国庫の台帳〕は載せない）
        Assert.Equal(TableCatalog.All.Count, TableCatalog.All.Select(t => t.Key).Distinct().Count());
        foreach (var t in TableCatalog.All)
        {
            Assert.True(DisplayNames.IsValid(t.Name), $"表の名前が規則に合わない: {t.Key}");
            Assert.NotEmpty(t.ColumnInfos);
            Assert.Equal(t.ColumnInfos.Count, t.ColumnInfos.Select(c => c.Key).Distinct().Count());
            foreach (var c in t.ColumnInfos)
            {
                Assert.True(DisplayNames.IsValid(c.Name), $"{t.Key} の列の名前が規則に合わない: {c.Name}");
                if (c.Kind == ColumnKind.Number) Assert.NotNull(c.Unit);   // どの数の列も単位が決まる
                else Assert.Null(c.Unit);
            }
            Assert.Same(t, TableCatalog.ByKey(t.Key));
        }
        Assert.Null(TableCatalog.ByKey("存在しない"));
    }

    // ===== 列の作り口 =====

    [Fact]
    public void 型つきの数の列は単位を型から取る_型の無い物は名乗る()
    {
        Assert.Equal(Unit.MoneyStock, Col.Of<int, MoneyStock>("a", "国庫", _ => new MoneyStock(1)).Unit);
        Assert.Equal(Unit.MoneyPerMonth, Col.Of<int, MoneyPerMonth>("a", "収入", _ => new MoneyPerMonth(1)).Unit);
        Assert.Equal(Unit.MoneyPerYear, Col.Of<int, MoneyPerYear>("a", "年収", _ => new MoneyPerYear(1)).Unit);
        Assert.Equal(Unit.Share, Col.Of<int, Share>("a", "割合", _ => new Share(1)).Unit);
        Assert.Equal(Unit.RatePerYear, Col.Of<int, RatePerYear>("a", "年率", _ => new RatePerYear(1)).Unit);
        Assert.Equal(Unit.RatePerMonth, Col.Of<int, RatePerMonth>("a", "月率", _ => new RatePerMonth(1)).Unit);
        Assert.Equal(Unit.RatePerQuarter, Col.Of<int, RatePerQuarter>("a", "四半期率", _ => new RatePerQuarter(1)).Unit);
        Assert.Equal(Unit.RealGdpPerMonth, Col.Of<int, RealGdpPerMonth>("a", "産出", _ => new RealGdpPerMonth(1)).Unit);
        Assert.Equal(Unit.RatioToGdpYear, Col.Of<int, RatioToGdpYear>("a", "債務比", _ => new RatioToGdpYear(1)).Unit);
        // 専用の値型は 9 つ全てが単位に写る（Units.cs に型を足したら Unit にも足す）
        var unitTypes = typeof(MoneyStock).Assembly.GetTypes().Where(t => UnitValues.IsUnit(t)).ToList();
        Assert.Equal(9, unitTypes.Count);
        Assert.All(unitTypes, t => Assert.NotNull(Col.UnitOf(t)));
        Assert.Null(Col.UnitOf(typeof(double)));
        Assert.Equal(Unit.People, Col.Number<int>("a", "人口", Unit.People, _ => 1).Unit);
        var c = Col.Of<int, Share>("a", "割合", r => new Share(r / 10.0));
        Assert.Equal(0.3, c.Read(3), 9);
    }
}
