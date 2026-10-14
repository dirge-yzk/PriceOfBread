using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★専用の値型（Units.cs・README §1-9・(246)）の番人。
/// 型の誤りは「コンパイルが通らない」ことで防ぐので、コンパイルされない式そのものは試験にできない＝代わりに、
/// ① 型の演算子の一覧が許す形（同型の加減・符号反転・スカラー倍と除算・同型の比・同型の比較）だけであること
///   （暗黙・明示の double への変換や、型をまたぐ演算子が足されたら赤）、
/// ② 型をまたぐ換算（年⇔月）が Calendar の中にしか無いこと、
/// ③ 量の宣言が表のとおり型になっていること（double に戻したら赤）、
/// ④ 書式と等値が double と同じであること（$"{x:F0}" が型名を出す事故・NaN の等値）、
/// ⑤ セーブの往復と係数の名前引きが型を通ることを見る。
/// ⚠突然変異：`public static implicit operator double(RatePerYear r) => r.Value;` を Units.cs に足して①が赤。Calendar の外に
///   `RatePerMonth` を返す換算を 1 本足して②が赤。World の `GovBondRatePerYear` を double に戻して③が赤。</summary>
public class UnitTypeTests
{
    /// <summary>専用の値型の一覧（新しい型を足したらここへ。Units.cs の型と一致することを下で確かめる）。</summary>
    private static readonly Type[] UnitTypes = [typeof(RatePerYear), typeof(RatePerMonth), typeof(RatePerQuarter), typeof(MoneyPerMonth), typeof(MoneyPerYear), typeof(MoneyStock), typeof(RealGdpPerMonth), typeof(Share), typeof(RatioToGdpYear)];

    [Fact]
    public void 型の一覧は宣言された専用の値型の全部である()
    {
        var declared = typeof(IUnitValue).Assembly.GetTypes().Where(t => t.IsValueType && typeof(IUnitValue).IsAssignableFrom(t)).OrderBy(t => t.Name).ToArray();
        Assert.Equal(UnitTypes.OrderBy(t => t.Name).Select(t => t.Name), declared.Select(t => t.Name));
    }

    [Fact]
    public void 演算子の一覧が許す形だけである()
    {
        foreach (var t in UnitTypes)
        {
            string n = t.Name;
            var actual = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("op_", StringComparison.Ordinal))
                .Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})->{m.ReturnType.Name}")
                .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var expect = new[]
            {
                $"op_Addition({n},{n})->{n}", $"op_Subtraction({n},{n})->{n}", $"op_UnaryNegation({n})->{n}",
                $"op_Multiply({n},Double)->{n}", $"op_Multiply(Double,{n})->{n}", $"op_Division({n},Double)->{n}", $"op_Division({n},{n})->Double",
                $"op_LessThan({n},{n})->Boolean", $"op_GreaterThan({n},{n})->Boolean", $"op_LessThanOrEqual({n},{n})->Boolean", $"op_GreaterThanOrEqual({n},{n})->Boolean",
                $"op_Equality({n},{n})->Boolean", $"op_Inequality({n},{n})->Boolean",
            }.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            // ★(256) 割合（Share）だけは積の演算子を持つ：割合×割合＝割合・割合×お金＝お金（同じ型のまま）。お金の型の側の一覧は変えない（演算子は Share の中に置く）
            if (t == typeof(Share))
                expect = expect.Concat(new[]
                {
                    "op_Multiply(Share,Share)->Share",
                    "op_Multiply(Share,MoneyStock)->MoneyStock", "op_Multiply(MoneyStock,Share)->MoneyStock",
                    "op_Multiply(Share,MoneyPerMonth)->MoneyPerMonth", "op_Multiply(MoneyPerMonth,Share)->MoneyPerMonth",
                    "op_Multiply(Share,MoneyPerYear)->MoneyPerYear", "op_Multiply(MoneyPerYear,Share)->MoneyPerYear",
                }).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Assert.Equal(expect, actual);   // op_Implicit／op_Explicit（double への変換）と型をまたぐ演算子はここで赤になる
            // 構造：中身は double 1つだけ・値型・書式と取り出しの口
            Assert.True(t.IsValueType);
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.Single(fields);
            Assert.Equal(typeof(double), fields[0].FieldType);
            Assert.True(typeof(IUnitValue).IsAssignableFrom(t));
            Assert.True(typeof(IFormattable).IsAssignableFrom(t));
            Assert.True(typeof(IComparable<>).MakeGenericType(t).IsAssignableFrom(t));   // OrderBy・Max・Min は実行時に要る（無いと落ちる）
            Assert.Empty(t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name is "op_Implicit" or "op_Explicit"));
        }
    }

    [Fact]
    public void 型をまたぐ換算はCalendarの中にしか無い()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var type in typeof(IUnitValue).Assembly.GetTypes())
            foreach (var m in type.GetMethods(All))
            {
                if (type == typeof(Share) && m.Name == "op_Multiply") continue;   // ★(256) Share×お金の積は「演算子の一覧が許す形だけである」と「異なる型の間には…」が許す組だけに固定している。★(257) 反証役 C2 低#2：除外は Share の op_Multiply だけ＝専用の値型でない型が型をまたぐ演算子を宣言したら、ここで赤
                if (!UnitTypes.Contains(m.ReturnType)) continue;
                bool crosses = m.GetParameters().Any(p => UnitTypes.Contains(p.ParameterType) && p.ParameterType != m.ReturnType);
                if (crosses && type != typeof(Calendar)) offenders.Add($"{type.Name}.{m.Name}");
            }
        Assert.True(offenders.Count == 0, "型をまたぐ換算が Calendar の外にある: " + string.Join(", ", offenders));
        // 対照（何もしない番人は必ず緑）：Calendar には年⇔月の型つきの換算がある
        Assert.NotNull(typeof(Calendar).GetMethod("PerYearToPerMonth", [typeof(RatePerYear)]));
        Assert.NotNull(typeof(Calendar).GetMethod("PerMonthToPerYear", [typeof(RatePerMonth)]));
        // ★(317) 四半期率の換算も Calendar の型つきの多重定義（★(318) 使い手のある 1 本だけ残した）
        Assert.NotNull(typeof(Calendar).GetMethod("PerQuarterToPerMonth", [typeof(RatePerQuarter)]));
    }

    /// <summary>量の宣言と型の表（double に戻したら赤）。World のプロパティ・Coefficients のフィールド。</summary>
    private static readonly (Type Owner, string Member, Type Expect)[] Declared =
    [
        (typeof(World), "GovBondRatePerYear", typeof(RatePerYear)),
        (typeof(World), "GovAvgCouponPerYear", typeof(RatePerYear)),
        (typeof(World), "ExpectedInflationPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondBaseRatePerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondRateMaxPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "LoanSpreadPerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "OpulenceSpendRatePerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "BondRolloverFracPerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "BondRepayFracPerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "PrizeMoneyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "SubsidyDependenceRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "SoldierMigrationRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "MintFloorBuyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "MintReserveSellRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "ForceUnlockTreasuryCostPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "GdpPerMonth", typeof(RealGdpPerMonth)),   // ★(250) 実質（参照価格×量）。名目は NominalGdpPerMonth（貨幣）＝別の型
        (typeof(World), "NominalGdpPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastRevenuePerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastInterestPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "FiscalCapacityPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastAdminClaimPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastSoldierPayPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastMilitaryClaimPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastWelfareCostPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastPensionCostPerMonth", typeof(MoneyPerMonth)),
        (typeof(World), "LastColonialUpkeepPerMonth", typeof(MoneyPerMonth)),
        (typeof(Pop), "EarnedPerMonth", typeof(MoneyPerMonth)),
        (typeof(Pop), "LastEarnedPerMonth", typeof(MoneyPerMonth)),
        (typeof(Pop), "LandRentPerMonth", typeof(MoneyPerMonth)),
        (typeof(Pop), "PaidPerMonth", typeof(MoneyPerMonth)),
        (typeof(Pop), "IndustrialRentPerMonth", typeof(MoneyPerMonth)),
        (typeof(Power), "GdpPerMonth", typeof(RealGdpPerMonth)),
        // ★(250) 在庫のお金（国庫・国債の残高・貯蓄）。流れ（MoneyPerMonth）とは足せない
        (typeof(World), "Treasury", typeof(MoneyStock)),
        (typeof(World), "GovDebt", typeof(MoneyStock)),
        (typeof(Pop), "Savings", typeof(MoneyStock)),
        // ★(252) 在庫のお金の残り。不変条件「Σ保有＝GovDebt−ForeignDebt」（BondLedger）の両辺が同じ型になる／BaseMoney＝TotalMoney＋国庫−与信 も型のまま
        (typeof(World), "ForeignDebt", typeof(MoneyStock)),
        (typeof(World), "PrevForeignDebt", typeof(MoneyStock)),
        (typeof(Pop), "BondHoldings", typeof(MoneyStock)),
        (typeof(Pop), "Debt", typeof(MoneyStock)),
        (typeof(Bank), "Loans", typeof(MoneyStock)),
        (typeof(Bank), "HouseholdLoans", typeof(MoneyStock)),
        (typeof(World), "OutstandingLoans", typeof(MoneyStock)),
        (typeof(World), "TotalMoney", typeof(MoneyStock)),
        (typeof(World), "BroadMoney", typeof(MoneyStock)),
        (typeof(World), "BaseMoney", typeof(MoneyStock)),
        // ★(256) 在庫のお金の残り：銀行の準備（BaseMoney から毎期導く）・保存則の累計の台帳3本・対外の資本と債権（四半期の中で貯めて0に戻す PendingForeignBorrow／ForeignIntPaidThisQuarter は在庫でない＝double のまま）
        (typeof(Bank), "Gold", typeof(MoneyStock)),
        (typeof(World), "CumulativeTradeFlow", typeof(MoneyStock)),
        (typeof(World), "CumulativeGoldMinted", typeof(MoneyStock)),
        (typeof(World), "CumulativeFiatIssued", typeof(MoneyStock)),
        (typeof(World), "ForeignCapitalIn", typeof(MoneyStock)),
        (typeof(World), "CapitalAbroad", typeof(MoneyStock)),
        (typeof(World), "ForeignBondClaims", typeof(MoneyStock)),
        // ★(256) 割合（0〜1）＝財政の割合：税率・国家の支払い率（r）・歳入シェア・閾値。年GDP 比（在庫÷年の流れ・1 を超える）は別の型
        (typeof(World), "TaxRate", typeof(Share)),
        (typeof(World), "FiscalFundingRatio", typeof(Share)),
        (typeof(Coefficients), "TitheRate", typeof(Share)),
        (typeof(Coefficients), "AdminBudgetFrac", typeof(Share)),
        (typeof(Coefficients), "WelfareRevenueShare", typeof(Share)),
        (typeof(Coefficients), "MaxInterestShareOfRevenue", typeof(Share)),
        (typeof(Coefficients), "BorrowFractionOfGdp", typeof(Share)),
        (typeof(Coefficients), "CurrencyReformBurnShare", typeof(Share)),
        (typeof(Coefficients), "CurrencyReformForgiveShare", typeof(Share)),
        (typeof(Coefficients), "GoldStandardMinFunding", typeof(Share)),
        (typeof(Coefficients), "LolrMinFunding", typeof(Share)),
        (typeof(Coefficients), "AllyJoinFundingMin", typeof(Share)),
        (typeof(Coefficients), "DebtDistressRatio", typeof(RatioToGdpYear)),
        (typeof(Coefficients), "AusterityStartDebt", typeof(RatioToGdpYear)),
        (typeof(Coefficients), "AusterityRange", typeof(RatioToGdpYear)),
        // ★(297) 割合の型の残り その1＝Model.cs（Pop・Producer・Market・Region）の割合と World の汽船の割合・貸出金利（年率）
        (typeof(Pop), "MilitiaShare", typeof(Share)),
        (typeof(Pop), "UnemploymentRate", typeof(Share)),
        (typeof(Pop), "InitialFertileShare", typeof(Share)),
        (typeof(Pop), "ChildLaborRate", typeof(Share)),
        (typeof(Pop), "CoreEthnicShare", typeof(Share)),
        (typeof(Producer), "RentShare", typeof(Share)),
        (typeof(Producer), "MothballFrac", typeof(Share)),
        (typeof(Producer), "SubsidyRate", typeof(Share)),
        (typeof(Market), "LastRation", typeof(Share)),
        (typeof(Region), "GarrisonShare", typeof(Share)),
        (typeof(World), "ShipSteamShare", typeof(Share)),
        (typeof(World), "LoanRatePerYear", typeof(RatePerYear)),
        // ★(303) 割合の型の残り その2＝軍事費の家族（陸海の維持費・その和と海の比）・海軍シェア・失業率の移動平均・地代の上限。World の ShipSteamShareEma は容量加重の割合なので ShipSteamShare へ改名（上の行）
        (typeof(World), "ArmyUpkeep", typeof(Share)),
        (typeof(World), "NavyUpkeep", typeof(Share)),
        (typeof(World), "MilitarySpending", typeof(Share)),
        (typeof(World), "NavalSpendShare", typeof(Share)),
        (typeof(Power), "NavalShare", typeof(Share)),
        (typeof(Pop), "UnemploymentEma", typeof(Share)),
        (typeof(Region), "FarmRentCap", typeof(Share)),
        // ★(307) 割合の型の残り その3＝軍の規模・動員・徴兵の係数（係数のテーマ1）。働き盛りの男の何割・軍事費の器の割合・艦隊の割合は Share、月ごとの動員・摩耗の速さは RatePerMonth（名前に PerMonth を足した）
        (typeof(Coefficients), "MaxSoldierFraction", typeof(Share)),
        (typeof(Coefficients), "ConscriptionFraction", typeof(Share)),
        (typeof(Coefficients), "ConscriptionFloorFraction", typeof(Share)),
        (typeof(Coefficients), "MobilizationConscriptFrac", typeof(Share)),
        (typeof(Coefficients), "MilitaryEnvelopeShare", typeof(Share)),
        (typeof(Coefficients), "AbstractNavalShare", typeof(Share)),
        (typeof(Coefficients), "OfficerCasualtyShare", typeof(Share)),
        (typeof(Coefficients), "ConscriptionRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "NavalAttritionRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "ArmsWearRatePerMonth", typeof(RatePerMonth)),
        // ★(313) 割合の型の残り その4＝外交・緊張・戦争の係数（係数のテーマ2）。国力の首位比の閾値・賠償と賞金の割合は Share、戦争の月の率は RatePerMonth。緊張の率（緊張点／月）・軍の強さの比・倍率・旗は移していない
        (typeof(Coefficients), "TreatyPortLapseRatio", typeof(Share)),
        (typeof(Coefficients), "GreatPowerScoreFraction", typeof(Share)),
        (typeof(Coefficients), "MinorPowerScoreFraction", typeof(Share)),
        (typeof(Coefficients), "ReparationGdpFraction", typeof(Share)),
        (typeof(Coefficients), "PrizeCargoShare", typeof(Share)),
        (typeof(Coefficients), "WarExhaustionRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "WarMilitancyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "WarCasualtyRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "WarCasualtyCapPerMonth", typeof(RatePerMonth)),
        // ★(317) 割合の型の残り その5＝人口・移民・社会移動の係数（係数のテーマ3）。3か月ごとの実行の刻みの率は新しい型 RatePerQuarter（名前に PerQuarter を足した）、月の率は RatePerMonth、年の率は RatePerYear、
        //   法の率・下限・昇格の比率は Share。EmigrationRate（機会差 1 あたりの率）・MinorityPromotionPenalty・JobPoolTemperature・JobSearchLiteracyWeight・JobSearchUsesEma は移していない
        (typeof(Coefficients), "CapitalRiseRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "UpperFallRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "MiddleFallRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "BankruptcyFallRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "ClergyJoinRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "ClergyExitRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "ArtisanDeclineRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "JobSearchRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "PettyEntryRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "PromotionSpeedPerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "EmigrationMaxPerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "ImmigrationReceiveCapPerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "ColonySettlerRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "PopGrowthRatePerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "CrudeBirthRatePerYear", typeof(RatePerYear)),
        (typeof(Coefficients), "ClergyFloorShare", typeof(Share)),
        (typeof(Coefficients), "ReligiousProfessionShare", typeof(Share)),
        (typeof(Coefficients), "ColonyPromotionCoreShare", typeof(Share)),
        (typeof(Coefficients), "ImmigrationSelectiveFrac", typeof(Share)),
        (typeof(Coefficients), "ImmigrationClosedFrac", typeof(Share)),
        // ★(321) 割合の型の残り その6＝金融・財政の係数 19 本（RatePerQuarter 3・RatePerMonth 1・Share 15）。四半期末 1 回の国際金融（CapitalMarket.Step）の配当・逃避は RatePerQuarter、月 1 回の切り下げの上限は RatePerMonth、その他の割合は Share。移さなかった物の理由は実装ログ (321)。
        (typeof(Coefficients), "CapitalDividendRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "CapitalFlightRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "CapitalDomesticFlightRatePerQuarter", typeof(RatePerQuarter)),
        (typeof(Coefficients), "DevaluationCapFracPerMonth", typeof(RatePerMonth)),
        (typeof(Coefficients), "FiatIssueFrac", typeof(Share)),
        (typeof(Coefficients), "GoldMintFraction", typeof(Share)),
        (typeof(Coefficients), "BondRateRepresentationDiscount", typeof(Share)),
        (typeof(Coefficients), "PreTightenHeatFrac", typeof(Share)),
        (typeof(Coefficients), "CurrencyHoldingRatio", typeof(Share)),
        (typeof(Coefficients), "PublicEduFundingShare", typeof(Share)),
        (typeof(Coefficients), "BankRunMax", typeof(Share)),
        (typeof(Coefficients), "LolrStrength", typeof(Share)),
        (typeof(Coefficients), "CurrencyReformBurnCap", typeof(Share)),
        (typeof(Coefficients), "InterestCapitalization", typeof(Share)),
        (typeof(Coefficients), "BondAppetite", typeof(Share)),
        (typeof(Coefficients), "ForeignBondCapacity", typeof(Share)),
        (typeof(Coefficients), "CorruptionMax", typeof(Share)),
        (typeof(Coefficients), "CapitalExportFrac", typeof(Share)),
        (typeof(Coefficients), "InsuranceCoverageMax", typeof(Share)),
        // ★(325) 割合の型の残り その7＝財政の係数 3 本（Share）。資本規制下の開放度・身分制免税の割引率・免税を王権の取り分に結ぶ率。`GoldCoverMin` は `SelfPlay.cs` が比べる相手で、並行の流れF の触れる所なので移していない（実装ログ (325)）。
        (typeof(Coefficients), "CapitalControlOpenness", typeof(Share)),
        (typeof(Coefficients), "PrivilegeTaxDiscount", typeof(Share)),
        (typeof(Coefficients), "PrivilegeFromCrownShare", typeof(Share)),
    ];

    [Fact]
    public void 量の宣言が表のとおり型になっている()
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var (owner, member, expect) in Declared)
        {
            Type? actual = owner.GetField(member, Any)?.FieldType ?? owner.GetProperty(member, Any)?.PropertyType;
            Assert.True(actual is not null, $"{owner.Name}.{member} が無い（改名したら表も直す）");
            Assert.True(actual == expect, $"{owner.Name}.{member} は {expect.Name} のはず（{actual!.Name} になっている）");
        }
    }

    [Fact]
    public void 書式と等値はdoubleと同じである()
    {
        double[] xs = [0.0, 0.04, -0.0125, 1234.5678, 1e-9, double.NaN, double.PositiveInfinity];
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var x in xs)
        {
            foreach (var t in UnitTypes)
            {
                dynamic y = Activator.CreateInstance(t, x)!;
                Assert.Equal(x.ToString(), (string)y.ToString());
                Assert.Equal(x.ToString("F3", inv), (string)y.ToString("F3", inv));
                Assert.Equal($"{x,9:F2}|{x:R}|{x}", $"{(object)y,9:F2}|{(object)y:R}|{(object)y}");   // 補間文字列の書式・桁寄せ・R も double と同じ
                Assert.Equal(x == x, (bool)(y == y));                                  // NaN は自分と等しくない（record の既定の double.Equals とは違う）
                Assert.Equal(x != x, (bool)(y != y));
            }
        }
        // 並べ替え：double と同じ順序（NaN は最小・-0.0 と 0.0 は等しい＝double.CompareTo と同じ）
        double[] ys = [0.3, -1.0, double.NaN, 0.0, 2.5, -0.0];
        foreach (var t in UnitTypes)
        {
            var boxed = ys.Select(v => Activator.CreateInstance(t, v)!).ToArray();
            var cmp = (System.Collections.IComparer)typeof(Comparer<>).MakeGenericType(t).GetProperty("Default")!.GetValue(null)!;   // OrderBy・Max が使う既定の比較子
            Array.Sort(boxed, cmp);
            var sorted = boxed.Select(o => ((IUnitValue)o).Value).ToArray();
            var expect = ys.OrderBy(v => v).ToArray();
            Assert.Equal(expect.Select(BitConverter.DoubleToInt64Bits), sorted.Select(BitConverter.DoubleToInt64Bits));
        }
        Assert.True(new RatePerYear(0.0) == new RatePerYear(-0.0));
        Assert.False(new RatePerYear(double.NaN) == new RatePerYear(double.NaN));
        Assert.True(new RatePerYear(double.NaN) != new RatePerYear(double.NaN));
    }

    [Fact]
    public void 演算はdoubleの1演算そのものである()
    {
        double[] xs = [0.04, -0.0125, 0.3, 1.0 / 3.0, 0.0075];
        foreach (var a in xs) foreach (var b in xs)
        {
            var ra = new RatePerYear(a); var rb = new RatePerYear(b);
            Assert.Equal(BitConverter.DoubleToInt64Bits(a + b), BitConverter.DoubleToInt64Bits((ra + rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a - b), BitConverter.DoubleToInt64Bits((ra - rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((ra * b).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((a * rb).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits((ra / b).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits(ra / rb));
            Assert.Equal(BitConverter.DoubleToInt64Bits(-a), BitConverter.DoubleToInt64Bits((-ra).Value));
            Assert.Equal(a < b, ra < rb); Assert.Equal(a <= b, ra <= rb); Assert.Equal(a > b, ra > rb); Assert.Equal(a >= b, ra >= rb);
        }
        foreach (var x in xs)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(Calendar.PerYearToPerMonth(x)), BitConverter.DoubleToInt64Bits(Calendar.PerYearToPerMonth(new RatePerYear(x)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(Calendar.PerMonthToPerYear(x)), BitConverter.DoubleToInt64Bits(Calendar.PerMonthToPerYear(new RatePerMonth(x)).Value));
            // ★(317) 四半期率の換算も double の 1 回の除算そのもの（x/3）
            Assert.Equal(BitConverter.DoubleToInt64Bits(x / 3), BitConverter.DoubleToInt64Bits(Calendar.PerQuarterToPerMonth(new RatePerQuarter(x)).Value));
        }
        // ★(256) 割合の積と年GDP 比の作り口も double の1演算そのもの
        foreach (var a in xs) foreach (var b in xs)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((new Share(a) * new Share(b)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((new Share(a) * new MoneyStock(b)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(b * a), BitConverter.DoubleToInt64Bits((new MoneyPerMonth(b) * new Share(a)).Value));   // 旧の `double * MoneyPerMonth`（k * a.Value）と同じビット
            Assert.Equal(BitConverter.DoubleToInt64Bits(a * b), BitConverter.DoubleToInt64Bits((new Share(a) * new MoneyPerYear(b)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits(new Share(a) / new Share(b)));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a / b), BitConverter.DoubleToInt64Bits(Calendar.ToGdpYear(new MoneyStock(a), new MoneyPerYear(b)).Value));
            Assert.Equal(BitConverter.DoubleToInt64Bits(a - b), BitConverter.DoubleToInt64Bits((new RatioToGdpYear(a) - new RatioToGdpYear(b)).Value));
        }
    }

    /// <summary>★(256) 年GDP 比（在庫÷年の流れ）を計算から作る口は `Calendar.ToGdpYear(MoneyStock, MoneyPerYear)` の1本だけ。型を返す関数を全て数える（演算子と record の自動生成は除く）＋
    /// SimCore・Runner・App の `new RatioToGdpYear(` は0件（係数の宣言の `new(…)` と反射の橋〔UnitValues〕とセーブの読みは除く）。⚠限界＝目的の型から推す `new(…)`（係数の宣言・定数）は文字列で数えられない。
    /// ⚠突然変異「Calendar の外に MoneyStock を受けて RatioToGdpYear を返す関数を足す」「SimCore に `new RatioToGdpYear(` を書く」で赤。</summary>
    [Fact]
    public void 年GDP比を作る口はCalendarToGdpYearの1本だけである()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var makers = new List<string>();
        foreach (var type in typeof(IUnitValue).Assembly.GetTypes())
            foreach (var m in type.GetMethods(All))
            {
                if (m.ReturnType != typeof(RatioToGdpYear)) continue;
                if (type == typeof(RatioToGdpYear) && (m.Name.StartsWith("op_", StringComparison.Ordinal) || m.Name is "get_Value")) continue;   // 自分の型の演算子（同じ型どうしの加減・倍・符号反転）
                if (m.IsSpecialName && m.Name.StartsWith("get_", StringComparison.Ordinal)) continue;   // 欄の読み出し（係数・定数）
                makers.Add($"{type.Name}.{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
        Assert.Equal(["Calendar.ToGdpYear(MoneyStock,MoneyPerYear)"], makers.ToArray());
        // `new RatioToGdpYear(` の直書き（コンストラクタ）は SimCore（Units.cs を除く）・Runner・App に無い
        string root = GoodIdGuardTests.RepoRoot();
        var hits = new List<string>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.Runner", "PriceOfBread.App" })
            foreach (var f in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(root, dir), "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string p = f.Replace('\\', '/');
                if (p.Contains("/obj/") || p.Contains("/bin/") || p.EndsWith("/Units.cs")) continue;
                int n = System.Text.RegularExpressions.Regex.Matches(GoodIdGuardTests.StripCode(System.IO.File.ReadAllText(f)), @"\bnew\s+RatioToGdpYear\s*\(").Count;
                if (n > 0) hits.Add($"{System.IO.Path.GetFileName(f)} {n}");
            }
        Assert.True(hits.Count == 0, "`new RatioToGdpYear(` の直書き＝年GDP 比を ToGdpYear の外で作っている: " + string.Join("、", hits));
    }

    [Fact]
    public void セーブの往復で専用の値型が残る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var w1 = sim1.Nations[0].World;
        w1.GovBondRatePerYear = new RatePerYear(0.0625); w1.GovAvgCouponPerYear = new RatePerYear(1.0 / 3.0); w1.ExpectedInflationPerYear = new RatePerYear(-0.0125);
        w1.Coef.LoanSpreadPerYear = new RatePerYear(0.05); w1.Coef.BondRepayFracPerMonth = new RatePerMonth(0.0175);
        w1.SetTreasuryRaw(1234.5678); w1.GovDebt = new MoneyStock(2.0 / 3.0);   // ★(250) 在庫のお金（国庫・国債の残高・貯蓄）
        w1.Regions[0].Pops[0].SetSavings(w1.Regions[0].Pops[0].Savings.Value * (1.0 / 3.0));   // (273) 旧 ScaleSavings（倍率）は置き直しの RebaseSavingsTo〔値〕に替わった＝盤面の道具で同じ値にする
        // ★(252) 在庫のお金の残り（対外債・保有・個人債務・銀行の与信）＝割り切れない値で往復を確かめる
        w1.ForeignDebt = new MoneyStock(1.0 / 7.0); w1.PrevForeignDebt = new MoneyStock(2.0 / 7.0);
        w1.Regions[0].Pops[0].BondHoldings = new MoneyStock(3.0 / 7.0); w1.Regions[0].Pops[0].Debt = new MoneyStock(4.0 / 7.0);
        w1.Bank!.Loans = new MoneyStock(5.0 / 7.0); w1.Bank.HouseholdLoans = new MoneyStock(6.0 / 7.0);
        // ★(256) 在庫のお金の残り（銀行の準備・保存則の台帳3本・対外の資本と債権）と割合・年GDP 比＝割り切れない値（11分の幾つ）で往復を確かめる
        w1.Bank.Gold = new MoneyStock(1.0 / 11.0);
        w1.CumulativeTradeFlow = new MoneyStock(2.0 / 11.0); w1.CumulativeGoldMinted = new MoneyStock(3.0 / 11.0); w1.CumulativeFiatIssued = new MoneyStock(4.0 / 11.0);
        w1.ForeignCapitalIn = new MoneyStock(5.0 / 11.0); w1.CapitalAbroad = new MoneyStock(6.0 / 11.0); w1.ForeignBondClaims = new MoneyStock(7.0 / 11.0);
        w1.TaxRate = new Share(8.0 / 11.0); w1.FiscalFundingRatio = new Share(9.0 / 11.0); w1.Coef.TitheRate = new Share(10.0 / 11.0);
        w1.Coef.AusterityStartDebt = new RatioToGdpYear(12.0 / 11.0);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var w2 = SaveGame.Load(ms).Nations[0].World;
        Assert.Equal(w1.GovBondRatePerYear, w2.GovBondRatePerYear);
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.GovAvgCouponPerYear.Value), BitConverter.DoubleToInt64Bits(w2.GovAvgCouponPerYear.Value));
        Assert.Equal(w1.ExpectedInflationPerYear, w2.ExpectedInflationPerYear);
        Assert.Equal(w1.Coef.LoanSpreadPerYear, w2.Coef.LoanSpreadPerYear);
        Assert.Equal(w1.Coef.BondRepayFracPerMonth, w2.Coef.BondRepayFracPerMonth);
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Treasury.Value), BitConverter.DoubleToInt64Bits(w2.Treasury.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.GovDebt.Value), BitConverter.DoubleToInt64Bits(w2.GovDebt.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Regions[0].Pops[0].Savings.Value), BitConverter.DoubleToInt64Bits(w2.Regions[0].Pops[0].Savings.Value));
        Assert.NotEqual(0.0, w1.Regions[0].Pops[0].Savings.Value);   // 対照：0を往復して緑になる空の番人にしない
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.ForeignDebt.Value), BitConverter.DoubleToInt64Bits(w2.ForeignDebt.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.PrevForeignDebt.Value), BitConverter.DoubleToInt64Bits(w2.PrevForeignDebt.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Regions[0].Pops[0].BondHoldings.Value), BitConverter.DoubleToInt64Bits(w2.Regions[0].Pops[0].BondHoldings.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Regions[0].Pops[0].Debt.Value), BitConverter.DoubleToInt64Bits(w2.Regions[0].Pops[0].Debt.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Bank.Loans.Value), BitConverter.DoubleToInt64Bits(w2.Bank!.Loans.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Bank.HouseholdLoans.Value), BitConverter.DoubleToInt64Bits(w2.Bank.HouseholdLoans.Value));
        // ★(256)
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Bank.Gold.Value), BitConverter.DoubleToInt64Bits(w2.Bank.Gold.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.CumulativeTradeFlow.Value), BitConverter.DoubleToInt64Bits(w2.CumulativeTradeFlow.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.CumulativeGoldMinted.Value), BitConverter.DoubleToInt64Bits(w2.CumulativeGoldMinted.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.CumulativeFiatIssued.Value), BitConverter.DoubleToInt64Bits(w2.CumulativeFiatIssued.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.ForeignCapitalIn.Value), BitConverter.DoubleToInt64Bits(w2.ForeignCapitalIn.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.CapitalAbroad.Value), BitConverter.DoubleToInt64Bits(w2.CapitalAbroad.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.ForeignBondClaims.Value), BitConverter.DoubleToInt64Bits(w2.ForeignBondClaims.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.TaxRate.Value), BitConverter.DoubleToInt64Bits(w2.TaxRate.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.FiscalFundingRatio.Value), BitConverter.DoubleToInt64Bits(w2.FiscalFundingRatio.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Coef.TitheRate.Value), BitConverter.DoubleToInt64Bits(w2.Coef.TitheRate.Value));
        Assert.Equal(BitConverter.DoubleToInt64Bits(w1.Coef.AusterityStartDebt.Value), BitConverter.DoubleToInt64Bits(w2.Coef.AusterityStartDebt.Value));
    }

    /// <summary>★(297) 割合の型の残り その1＝Pop・Producer・Market・Region・World の割合の欄が、セーブの往復で割り切れない値（13分の幾つ）のまま残る。
    /// 欄ごとに別の値を置く＝取り違えて読み書きしても赤（期待値は欄の番号 k/13 を直に書く＝セーブの出力から作らない）。</summary>
    [Fact]
    public void セーブの往復で割合の型の欄が残る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var w1 = sim1.Nations[0].World;
        var pop = w1.Regions[0].Pops[0]; var prod = w1.Regions[0].Producers[0]; var mkt = w1.Regions[0].Markets[0];
        pop.MilitiaShare = new Share(1.0 / 13.0); pop.UnemploymentRate = new Share(2.0 / 13.0); pop.InitialFertileShare = new Share(3.0 / 13.0);
        pop.ChildLaborRate = new Share(4.0 / 13.0); pop.CoreEthnicShare = new Share(5.0 / 13.0);
        prod.RentShare = new Share(6.0 / 13.0); prod.MothballFrac = new Share(7.0 / 13.0); prod.SubsidyRate = new Share(8.0 / 13.0);
        mkt.LastRation = new Share(9.0 / 13.0); w1.Regions[0].GarrisonShare = new Share(10.0 / 13.0); w1.ShipSteamShare = new Share(11.0 / 13.0);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var w2 = SaveGame.Load(ms).Nations[0].World;
        var pop2 = w2.Regions[0].Pops[0]; var prod2 = w2.Regions[0].Producers[0]; var mkt2 = w2.Regions[0].Markets[0];
        static long Bits(Share s) => BitConverter.DoubleToInt64Bits(s.Value);
        Assert.Equal(BitConverter.DoubleToInt64Bits(1.0 / 13.0), Bits(pop2.MilitiaShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(2.0 / 13.0), Bits(pop2.UnemploymentRate));
        Assert.Equal(BitConverter.DoubleToInt64Bits(3.0 / 13.0), Bits(pop2.InitialFertileShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(4.0 / 13.0), Bits(pop2.ChildLaborRate));
        Assert.Equal(BitConverter.DoubleToInt64Bits(5.0 / 13.0), Bits(pop2.CoreEthnicShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(6.0 / 13.0), Bits(prod2.RentShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(7.0 / 13.0), Bits(prod2.MothballFrac));
        Assert.Equal(BitConverter.DoubleToInt64Bits(8.0 / 13.0), Bits(prod2.SubsidyRate));
        Assert.Equal(BitConverter.DoubleToInt64Bits(9.0 / 13.0), Bits(mkt2.LastRation));
        Assert.Equal(BitConverter.DoubleToInt64Bits(10.0 / 13.0), Bits(w2.Regions[0].GarrisonShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(11.0 / 13.0), Bits(w2.ShipSteamShare));
    }

    /// <summary>★(303) 割合の型の残り その2＝陸海の維持費（World）・海軍シェア（Power）・失業率の移動平均（Pop）・地代の上限（Region）の欄が、セーブの往復で割り切れない値（13分の幾つ）のまま残る。
    /// 欄ごとに別の値を置く（期待値は欄の番号 k/13 を直に書く＝セーブの出力から作らない）。和の MilitarySpending と海の比 NavalSpendShare は維持費2本から導く派生＝往復後の導出も確かめる。</summary>
    [Fact]
    public void セーブの往復で軍事費の家族と残りの割合の欄が残る()
    {
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var w1 = sim1.Nations[0].World;
        w1.ArmyUpkeep = new Share(2.0 / 13.0); w1.NavyUpkeep = new Share(3.0 / 13.0);
        w1.SelfPower!.NavalShare = new Share(4.0 / 13.0);
        w1.Regions[0].Pops[0].UnemploymentEma = new Share(5.0 / 13.0);
        w1.Regions[0].FarmRentCap = new Share(6.0 / 13.0);
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var w2 = SaveGame.Load(ms).Nations[0].World;
        static long Bits(Share s) => BitConverter.DoubleToInt64Bits(s.Value);
        Assert.Equal(BitConverter.DoubleToInt64Bits(2.0 / 13.0), Bits(w2.ArmyUpkeep));
        Assert.Equal(BitConverter.DoubleToInt64Bits(3.0 / 13.0), Bits(w2.NavyUpkeep));
        Assert.Equal(BitConverter.DoubleToInt64Bits(4.0 / 13.0), Bits(w2.SelfPower!.NavalShare));
        Assert.Equal(BitConverter.DoubleToInt64Bits(5.0 / 13.0), Bits(w2.Regions[0].Pops[0].UnemploymentEma));
        Assert.Equal(BitConverter.DoubleToInt64Bits(6.0 / 13.0), Bits(w2.Regions[0].FarmRentCap));
        Assert.Equal(5.0 / 13.0, w2.MilitarySpending.Value, 12);   // 和（0..1 にクランプ）＝派生
        Assert.Equal(3.0 / 5.0, w2.NavalSpendShare.Value, 12);     // 海軍÷(陸軍＋海軍)
    }

    /// <summary>★(307) 割合の型の残り その3＝軍の規模・動員・徴兵の係数 10 本が、セーブの往復と係数の名前引き（反射の橋）で割り切れない値（13分の幾つ）のまま残る。
    /// 欄ごとに別の値（k/13）を直に書く（セーブの出力・Coefficients の既定から作らない）。旧名（改名前）の欄が残っていないことも見る。</summary>
    [Fact]
    public void セーブの往復と名前引きで軍の係数の欄が残る()
    {
        (string Name, Type Type)[] fields =
        [
            ("MaxSoldierFraction", typeof(Share)), ("ConscriptionFraction", typeof(Share)), ("ConscriptionFloorFraction", typeof(Share)),
            ("MobilizationConscriptFrac", typeof(Share)), ("MilitaryEnvelopeShare", typeof(Share)), ("AbstractNavalShare", typeof(Share)),
            ("OfficerCasualtyShare", typeof(Share)), ("ConscriptionRatePerMonth", typeof(RatePerMonth)),
            ("NavalAttritionRatePerMonth", typeof(RatePerMonth)), ("ArmsWearRatePerMonth", typeof(RatePerMonth)),
        ];
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var c1 = sim1.Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            f.SetValue(c1, UnitValues.FromDouble(f.FieldType, (k + 2) / 13.0));   // POB_COEF／Runner の反射と同じ入口
        }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var c2 = SaveGame.Load(ms).Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            Assert.Equal(fields[k].Type, f.FieldType);
            Assert.True(UnitValues.TryToDouble(f.GetValue(c2), out double got));
            Assert.Equal(BitConverter.DoubleToInt64Bits((k + 2) / 13.0), BitConverter.DoubleToInt64Bits(got));
        }
        foreach (var old in new[] { "ConscriptionRate", "NavalAttritionRate", "ArmsWearRate" })
            Assert.Null(typeof(Coefficients).GetField(old));   // 改名前の欄が残っていない（POB_COEF の旧名は〈存在しない係数名〉で止まる）
    }

    /// <summary>★(313) 割合の型の残り その4＝外交・緊張・戦争の係数 9 本が、セーブの往復と係数の名前引き（反射の橋）で割り切れない値（13分の幾つ）のままビットが残ること・
    /// 欄の型が表のとおりであること・改名前の欄が残っていないこと。期待値は k から直に書く（セーブの出力・Coefficients の既定から作らない）。</summary>
    [Fact]
    public void セーブの往復と名前引きで外交の係数の欄が残る()
    {
        (string Name, Type Type)[] fields =
        [
            ("TreatyPortLapseRatio", typeof(Share)), ("GreatPowerScoreFraction", typeof(Share)), ("MinorPowerScoreFraction", typeof(Share)),
            ("ReparationGdpFraction", typeof(Share)), ("PrizeCargoShare", typeof(Share)), ("WarExhaustionRatePerMonth", typeof(RatePerMonth)),
            ("WarMilitancyRatePerMonth", typeof(RatePerMonth)), ("WarCasualtyRatePerMonth", typeof(RatePerMonth)), ("WarCasualtyCapPerMonth", typeof(RatePerMonth)),
        ];
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var c1 = sim1.Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            f.SetValue(c1, UnitValues.FromDouble(f.FieldType, (k + 2) / 13.0));   // POB_COEF／Runner の反射と同じ入口
        }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var c2 = SaveGame.Load(ms).Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            Assert.Equal(fields[k].Type, f.FieldType);
            Assert.True(UnitValues.TryToDouble(f.GetValue(c2), out double got));
            Assert.Equal(BitConverter.DoubleToInt64Bits((k + 2) / 13.0), BitConverter.DoubleToInt64Bits(got));
        }
        foreach (var old in new[] { "WarExhaustionRate", "WarMilitancyRate", "WarCasualtyRate", "WarCasualtyMonthlyCap" })
            Assert.Null(typeof(Coefficients).GetField(old));   // 改名前の欄が残っていない（POB_COEF の旧名は〈存在しない係数名〉で止まる）
    }

    /// <summary>★(317) 割合の型の残り その5＝人口・移民・社会移動の係数 20 本（新しい型 RatePerQuarter 12・RatePerMonth 2・RatePerYear 1・Share 5）が、セーブの往復と係数の名前引き（反射の橋）で
    /// 割り切れない値（13分の幾つ）のままビットが残ること・欄の型が表のとおりであること・改名前の欄が残っていないこと。期待値は k から直に書く（セーブの出力・Coefficients の既定から作らない）。</summary>
    [Fact]
    public void セーブの往復と名前引きで人口移民社会移動の係数の欄が残る()
    {
        (string Name, Type Type)[] fields =
        [
            ("CapitalRiseRatePerQuarter", typeof(RatePerQuarter)), ("UpperFallRatePerQuarter", typeof(RatePerQuarter)), ("MiddleFallRatePerQuarter", typeof(RatePerQuarter)),
            ("BankruptcyFallRatePerQuarter", typeof(RatePerQuarter)), ("ClergyJoinRatePerQuarter", typeof(RatePerQuarter)), ("ClergyExitRatePerQuarter", typeof(RatePerQuarter)),
            ("ArtisanDeclineRatePerQuarter", typeof(RatePerQuarter)), ("JobSearchRatePerQuarter", typeof(RatePerQuarter)), ("PettyEntryRatePerQuarter", typeof(RatePerQuarter)),
            ("PromotionSpeedPerQuarter", typeof(RatePerQuarter)), ("EmigrationMaxPerQuarter", typeof(RatePerQuarter)), ("ImmigrationReceiveCapPerQuarter", typeof(RatePerQuarter)),
            ("ColonySettlerRatePerMonth", typeof(RatePerMonth)), ("PopGrowthRatePerMonth", typeof(RatePerMonth)), ("CrudeBirthRatePerYear", typeof(RatePerYear)),
            ("ClergyFloorShare", typeof(Share)), ("ReligiousProfessionShare", typeof(Share)), ("ColonyPromotionCoreShare", typeof(Share)),
            ("ImmigrationSelectiveFrac", typeof(Share)), ("ImmigrationClosedFrac", typeof(Share)),
        ];
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var c1 = sim1.Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            f.SetValue(c1, UnitValues.FromDouble(f.FieldType, (k + 2) / 13.0));   // POB_COEF／Runner の反射と同じ入口
        }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var c2 = SaveGame.Load(ms).Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            Assert.Equal(fields[k].Type, f.FieldType);
            Assert.True(UnitValues.TryToDouble(f.GetValue(c2), out double got));
            Assert.Equal(BitConverter.DoubleToInt64Bits((k + 2) / 13.0), BitConverter.DoubleToInt64Bits(got));
        }
        foreach (var old in new[] { "CapitalRiseRate", "UpperFallRate", "MiddleFallRate", "BankruptcyFallRate", "ClergyJoinRate", "ClergyExitRate", "ArtisanDeclineRate", "JobSearchRate",
                                    "PettyEntryRate", "PromotionSpeed", "EmigrationMaxFrac", "ImmigrationReceiveCapFrac", "ColonySettlerRate", "PopGrowthRate", "CrudeBirthRate" })
            Assert.Null(typeof(Coefficients).GetField(old));   // 改名前の欄が残っていない（POB_COEF の旧名は〈存在しない係数名〉で止まる）
    }

    /// <summary>★(321) 割合の型の残り その6＝金融・財政の係数 19 本（RatePerQuarter 3・RatePerMonth 1・Share 15）が、セーブの往復と係数の名前引き（反射の橋）で欄ごとに別の値のままビットが残ること・欄の型が表のとおりであること・改名前の欄が残っていないこと。
    /// 期待値は k から直に書く（セーブの出力・Coefficients の既定から作らない）。</summary>
    [Fact]
    public void セーブの往復と名前引きで金融財政の係数の欄が残る()
    {
        (string Name, Type Type)[] fields = [("CapitalDividendRatePerQuarter", typeof(RatePerQuarter)), ("CapitalFlightRatePerQuarter", typeof(RatePerQuarter)), ("CapitalDomesticFlightRatePerQuarter", typeof(RatePerQuarter)), ("DevaluationCapFracPerMonth", typeof(RatePerMonth)), ("FiatIssueFrac", typeof(Share)), ("GoldMintFraction", typeof(Share)), ("BondRateRepresentationDiscount", typeof(Share)), ("PreTightenHeatFrac", typeof(Share)), ("CurrencyHoldingRatio", typeof(Share)), ("PublicEduFundingShare", typeof(Share)), ("BankRunMax", typeof(Share)), ("LolrStrength", typeof(Share)), ("CurrencyReformBurnCap", typeof(Share)), ("InterestCapitalization", typeof(Share)), ("BondAppetite", typeof(Share)), ("ForeignBondCapacity", typeof(Share)), ("CorruptionMax", typeof(Share)), ("CapitalExportFrac", typeof(Share)), ("InsuranceCoverageMax", typeof(Share))];
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var c1 = sim1.Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            f.SetValue(c1, UnitValues.FromDouble(f.FieldType, (k + 3) / 17.0));   // POB_COEF／Runner の反射と同じ入口
        }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var c2 = SaveGame.Load(ms).Nations[0].World.Coef;
        for (int k = 0; k < fields.Length; k++)
        {
            var f = typeof(Coefficients).GetField(fields[k].Name)!;
            Assert.Equal(fields[k].Type, f.FieldType);
            Assert.True(UnitValues.TryToDouble(f.GetValue(c2), out double got));
            Assert.Equal(BitConverter.DoubleToInt64Bits((k + 3) / 17.0), BitConverter.DoubleToInt64Bits(got));
        }
        foreach (var old in new[] { "CapitalDividendRate", "CapitalFlightRate", "CapitalDomesticFlightRate", "DevaluationCapFrac" })
            Assert.Null(typeof(Coefficients).GetField(old));   // 改名前の欄が残っていない（POB_COEF の旧名は〈存在しない係数名〉で止まる）
    }

    /// <summary>★(325) 割合の型の残り その7＝財政の係数 3 本（Share）が、セーブの往復と係数の名前引き（反射の橋）で欄ごとに別の値のままビットが残ること・欄の型が表のとおりであること。
    /// 期待値は k から直に書く（セーブの出力・Coefficients の既定から作らない）。</summary>
    [Fact]
    public void セーブの往復と名前引きで財政の係数の欄が残る()
    {
        string[] names = ["CapitalControlOpenness", "PrivilegeTaxDiscount", "PrivilegeFromCrownShare"];
        var sim1 = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], 1789, _ => new NullAgent());
        var c1 = sim1.Nations[0].World.Coef;
        for (int k = 0; k < names.Length; k++)
        {
            var f = typeof(Coefficients).GetField(names[k])!;
            f.SetValue(c1, UnitValues.FromDouble(f.FieldType, (k + 5) / 19.0));   // POB_COEF／Runner の反射と同じ入口
        }
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim1, ms);
        ms.Position = 0;
        var c2 = SaveGame.Load(ms).Nations[0].World.Coef;
        for (int k = 0; k < names.Length; k++)
        {
            var f = typeof(Coefficients).GetField(names[k])!;
            Assert.Equal(typeof(Share), f.FieldType);
            Assert.True(UnitValues.TryToDouble(f.GetValue(c2), out double got));
            Assert.Equal(BitConverter.DoubleToInt64Bits((k + 5) / 19.0), BitConverter.DoubleToInt64Bits(got));
        }
    }

    [Fact]
    public void 反射の橋は専用の値型を数として読み書きする()
    {
        Assert.True(UnitValues.IsUnit(typeof(RatePerYear)));
        Assert.False(UnitValues.IsUnit(typeof(double)));
        Assert.False(UnitValues.IsUnit(typeof(DateTime)));
        Assert.Equal(new RatePerMonth(0.5), UnitValues.FromDouble(typeof(RatePerMonth), 0.5));
        Assert.Equal(0.5, UnitValues.FromDouble(typeof(double), 0.5));
        Assert.True(UnitValues.TryToDouble(new RatePerYear(0.25), out double d) && d == 0.25);
        Assert.True(UnitValues.TryToDouble(3, out d) && d == 3.0);
        Assert.False(UnitValues.TryToDouble("x", out _));
        // 係数の名前引き（POB_COEF が使う反射）で型つきの係数を double から書き込める
        var c = new Coefficients();
        var f = typeof(Coefficients).GetField("BondRateMaxPerYear")!;
        f.SetValue(c, UnitValues.FromDouble(f.FieldType, 0.2));
        Assert.Equal(new RatePerYear(0.2), c.BondRateMaxPerYear);
    }

    [Fact]
    public void 異なる型の間には演算子も変換も無い_在庫と流れ_実質と名目()
    {
        // 反射で全ての組を確かめる：型 A が型 B（A と違う専用の値型）を引数に取る演算子・変換を持たない（(250) ③在庫のお金）。
        // 在庫（MoneyStock）＋流れ（MoneyPerMonth）・実質（RealGdpPerMonth）＋名目（MoneyPerMonth）はコンパイルが通らない。
        foreach (var a in UnitTypes)
            foreach (var b in UnitTypes)
            {
                if (a == b) continue;
                var ops = a.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m.Name.StartsWith("op_", StringComparison.Ordinal) && m.GetParameters().Any(p => p.ParameterType == b)).ToArray();
                // ★(256) 許す組は「割合×お金」だけ（Share が持つ・乗算だけ・戻りはお金の型）。割合＋お金・割合と年GDP 比（RatioToGdpYear）・割合と月率／年率・お金どうしの型違いはここで赤。
                bool shareTimesMoney = a == typeof(Share) && (b == typeof(MoneyStock) || b == typeof(MoneyPerMonth) || b == typeof(MoneyPerYear));
                if (shareTimesMoney)
                {
                    Assert.Equal(2, ops.Length);   // Share*お金 と お金*Share
                    Assert.All(ops, m => { Assert.Equal("op_Multiply", m.Name); Assert.Equal(b, m.ReturnType); });
                }
                else Assert.True(ops.Length == 0, $"{a.Name} が {b.Name} を取る演算子を持つ: {string.Join(",", ops.Select(m => m.Name))}");
            }
        // Calendar で在庫の型を返すのは OneMonthOf の1本だけ（MoneyPerMonth を受けて MoneyStock を返す＝Pop の月末の精算が貯蓄へ足す口）。⚠これは Calendar の換算の口の検査で、
        // 在庫を作る口の全てではない：double を受ける継ぎ目（World.Pay／Transfer の額・MoneyStock の public のコンストラクタ・CrossFx の在庫の型の版）は型で守っていない＝件数は下の「在庫のお金をdoubleから作る所の数」のラチェットが止める。
        var toStock = typeof(Calendar).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.ReturnType == typeof(MoneyStock)).ToArray();
        Assert.Single(toStock);
        Assert.Equal("OneMonthOf", toStock[0].Name);
        Assert.Equal([typeof(MoneyPerMonth)], toStock[0].GetParameters().Select(p => p.ParameterType).ToArray());
        // 実質から名目へは World.NominalOf（double を受ける）だけ。名目GDP は貨幣の型・実質GDP は別の型。
        Assert.Equal(typeof(MoneyPerMonth), typeof(World).GetProperty("NominalGdpPerMonth")!.PropertyType);
        Assert.NotEqual(typeof(World).GetProperty("GdpPerMonth")!.PropertyType, typeof(World).GetProperty("NominalGdpPerMonth")!.PropertyType);
    }

    [Fact]
    public void 月の決算は1か月分をそのまま在庫へ足す()
    {
        double[] savings = [0.0, 1234.5678, -77.7, 1e9 + 0.1];
        double[] earned = [0.0, 3.3, 1e-9, 98765.4321];
        double[] paid = [0.0, 3.1, 2.5e-9, 98000.0001];
        foreach (var s in savings) foreach (var e in earned) foreach (var p in paid)
        {
            var pop = new Pop { Savings = new MoneyStock(s), EarnedPerMonth = new MoneyPerMonth(e), PaidPerMonth = new MoneyPerMonth(p) };
            pop.SettleMonth();
            Assert.Equal(BitConverter.DoubleToInt64Bits(s + (e - p)), BitConverter.DoubleToInt64Bits(pop.Savings.Value));   // 旧の式 _savings += (Earned − Paid) と同じビット
        }
        Assert.Equal(BitConverter.DoubleToInt64Bits(0.1 - 0.3), BitConverter.DoubleToInt64Bits(Calendar.OneMonthOf(new MoneyPerMonth(0.1) - new MoneyPerMonth(0.3)).Value));
    }

    [Fact]
    public void 型にした係数を1本0にできる_Runnerの年代記とアブレーションと同じ反射()
    {
        // Runner の chronicle --zero と politics-ablation は UnitValues.FromDouble で係数を0にする。型を持つ係数の全部で、型が残って値が0になることを確かめる。
        var typed = typeof(Coefficients).GetFields().Where(f => UnitValues.IsUnit(f.FieldType) && !f.IsStatic && !f.IsInitOnly).ToArray();
        Assert.NotEmpty(typed);
        foreach (var f in typed)
        {
            var c = new Coefficients();
            f.SetValue(c, UnitValues.FromDouble(f.FieldType, 0.0));
            var after = f.GetValue(c);
            Assert.True(after is not null && after.GetType() == f.FieldType, $"{f.Name} の型が0にしたあと {after?.GetType().Name} になった");
            Assert.True(UnitValues.TryToDouble(after, out double d) && d == 0.0, $"{f.Name} が0にならない");
        }
        // 対照（何もしない番人は必ず緑）：double の係数も同じ橋で0にできる
        var df = typeof(Coefficients).GetFields().First(f => f.FieldType == typeof(double) && !f.IsStatic && !f.IsInitOnly);
        var c2 = new Coefficients();
        df.SetValue(c2, UnitValues.FromDouble(df.FieldType, 0.0));
        Assert.Equal(0.0, (double)df.GetValue(c2)!);
        // Runner のソースで、係数への書き込みと0化は UnitValues を通す（SetValue に素の double を渡さない）
        var runner = System.IO.File.ReadAllLines(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.Runner", "Program.cs"));
        Assert.Contains(runner, l => l.Contains("fi.SetValue(co, UnitValues.FromDouble(fi.FieldType, 0.0))"));   // politics-ablation
        Assert.Contains(runner, l => l.Contains("done.Add(co); fi.SetValue(co, UnitValues.FromDouble(fi.FieldType, 0.0))"));   // chronicle --zero
    }

    /// <summary>★(257) 反証役 C2 低#1：年の流れのお金（MoneyPerYear）は public のコンストラクタで double から作れる＝月の double を `new MoneyPerYear(x)` で包むと、
    /// 年GDP 比の12倍の取り違えがそのままコンパイルを通る（`Calendar.ToGdpYear` の型が守るのは「月の型を渡せない」ことだけ）。
    /// 年の流れは `Calendar.PerMonthToPerYear(MoneyPerMonth)` の型つきの換算から作る＝SimCore・Runner・App の `new MoneyPerYear(` の件数を宣言の数に固定する（ラチェット）。
    /// 増やすコミットは、包む値が年の値であることを確かめて、数を上げて理由を書く。⚠限界＝目的の型から推す `new(…)` は文字列で数えられない。</summary>
    [Fact]
    public void 年の流れのお金をdoubleから作る所の数は宣言のとおり()
    {
        const int Declared = 1;   // 2026-10-11 (257)：1＝Credit 1（銀行の評価損の債務比の分母＝`Math.Max(1.0, Calendar.PerMonthToPerYear(月の名目GDP))`＝年の値を床で挟んでから包む）。(256) の Fiscal 1 は型つきの換算へ寄せた
        var per = new[] { "PriceOfBread.SimCore", "PriceOfBread.Runner", "PriceOfBread.App" }
            .SelectMany(p => System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), p), "*.cs", System.IO.SearchOption.AllDirectories))
            .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"))
            .Select(f => (File: System.IO.Path.GetFileName(f), N: System.Text.RegularExpressions.Regex.Matches(GoodIdGuardTests.StripCode(System.IO.File.ReadAllText(f)), @"\bnew\s+MoneyPerYear\s*\(").Count))
            .Where(x => x.N > 0).OrderBy(x => x.File, StringComparer.Ordinal).ToList();
        int total = per.Sum(x => x.N);
        Assert.True(total == Declared, $"SimCore・Runner・App の `new MoneyPerYear(` が {total} 件（宣言 {Declared}）：" + string.Join("、", per.Select(x => $"{x.File} {x.N}"))
            + "＝増えたなら月の値を年の型で包んでいないか確かめる（年の流れは Calendar.PerMonthToPerYear）・減ったなら宣言を下げる");
    }

    /// <summary>★(251) 反証役 vm#1：在庫の型（MoneyStock）は public のコンストラクタで double から作れる＝`new MoneyStock(flow.Value)` と書けば OneMonthOf を通らずに流れを在庫へ足せる
    /// （国家の月末の精算 World.SettleStateSpent・未払いの利子の元本への繰り入れが今この形）。型では止められない＝SimCore の `new MoneyStock(` の件数を宣言の数に固定する（ラチェット）。
    /// 増やすコミットは、その行が在庫の値を作る所（残高の設定・取引の額の受け入れ・通貨の換算）であって月の流れの決算ではないことを確かめて、数を上げて理由を書く。
    /// ⚠限界＝目的の型から推す `new(…)`（MoneyStock の欄への代入）は文字列で数えられない。</summary>
    [Fact]
    public void 在庫のお金をdoubleから作る所の数は宣言のとおり()
    {
        const int Declared = 64;   // (273)：63→64＝Model.cs の Pop.RebaseSavingsTo(double) が置き直しの貯蓄を MoneyStock にする1件（旧 ScaleSavings は倍率で new が無かった） → 2026-10-11 (251)：48＝Simulation 30・Fiscal 6・Model 6・World 3・MultiNationSim 3 → (252)：54＝Simulation 32・Fiscal 8・Model 3・World 6・MultiNationSim 2・Bank 1・Diplomacy 2 → (256)：63＝CapitalMarket 8・Credit 1・MultiNationSim 3・World 5 が変わった（下）
        //   増えた理由（(252) の+6＝どれも double の流れ・取引の額・換算の結果を在庫の型へ入れる継ぎ目で、月の流れの決算ではない）：
        //   Bank 1＝家計の貸しと返しの動いた額を個人債務へ（Bank.Lend）／Diplomacy 2＝払えなかった貢納と賠償を対外債へ記帳する額（Fiscal.BookForeignDebt の引数が在庫の型になり呼び手が包む）／
        //   Fiscal +2＝減債で減る対外債（Math.Max の結果）・起債で資本家が払った額の保有（(251) の GovDebt 側の1件は対外債の側と共有して増やしていない・BookForeignDebt の引数は型になり −1）／
        //   Simulation +2＝損失の負債化で個人債務へ足す額・負の国の与信の償却／World +3＝世界の外の側の台帳（与信・家計の与信の動いた額）の2行と CrossFx の在庫の型の版（国境をまたぐ在庫の換算の1本）／
        //   MultiNationSim −1＝越境移民の持参金を型に（+1）・反乱国の国債の保有を預かる行と按分の行（−2）が型のまま足し引きになった／Model −3＝人と動くお金の運搬の引数と州の再換算が型のまま（Pop.Carry の包み2・Rebase 1）
        //   (256) の+9（どれも double の取引の額・移し方の戻り値・換算の結果を在庫の型へ入れる継ぎ目で、月の流れの決算ではない）：
        //   CapitalMarket +8＝国際金融の記帳（逃げた金を対外資産へ 1・受け皿の外資の負債 1・外資の逃避で減る負債 1・逃避で消える対外資産の再計算〔Math.Max の結果〕1・新規フローの送り出しで増える対外資産 1・流入で増える外資の負債 1・債権の按分の減り〔Math.Max の結果〕1・債権の無い世界の増え 1。額はどれも TakeGold／GiveGold が返す double と為替の按分の結果）／
        //   Credit +1＝銀行の準備 Bank.Gold を基礎貨幣から毎日導く 1 行／MultiNationSim +1＝対外借入の貸し手の国が払った額を貸し手の債権へ 1（World.Transfer が返す動いた額）／
        //   World −1＝台帳の記帳（Book）で動いた額の包みを5つの台帳が共有する 1 本にした（企業の与信と家計の与信の 2 行の包みが 1 行に畳まれた）。
        var dir = System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var per = System.IO.Directory.EnumerateFiles(dir, "*.cs", System.IO.SearchOption.AllDirectories)
            .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"))
            .Select(f => (File: System.IO.Path.GetFileName(f), N: System.Text.RegularExpressions.Regex.Matches(GoodIdGuardTests.StripCode(System.IO.File.ReadAllText(f)), @"\bnew\s+MoneyStock\s*\(").Count))
            .Where(x => x.N > 0).OrderBy(x => x.File, StringComparer.Ordinal).ToList();
        int total = per.Sum(x => x.N);
        Assert.True(total == Declared, $"SimCore の `new MoneyStock(` が {total} 件（宣言 {Declared}）：" + string.Join("、", per.Select(x => $"{x.File} {x.N}"))
            + "＝増えたなら月の流れを在庫へ直に足していないか確かめる（Pop の月末の精算は Calendar.OneMonthOf）・減ったなら宣言を下げる");
    }
}
