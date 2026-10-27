using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 蜂起R群 2026-08-16（docs/202608/20260816_蜂起不発と極端プレイ診断_部分完了.md）のON経路検証。
/// R1 coerce飽和を壁→漸近逓減（Government.CoercionTargetFromForce・旧の旗 CoercionSoftCap は2026-10-18に焼き込み）／
/// R2 FactionRevoltLocalCoerce＝F3b右辺の州pop按分ブレンド（Politics.LocalizedHold）／
/// R3 は★段2（2026-09-09）で機構ごと消滅＝熱は州平均闘争度1量（同じ主張を新しい形で測る2本に書き直し済み）／
/// R4 ModernizationBudgetSoftCap＝近代化投資のminクランプ→漸近逓減（World.ModernizationBudget）。
/// ⚠**R1・R2・R4は2026-08-16の点火梯子で点火済み**（R1=1.0＋水準 CoercionMax 4000→2000／R2=0.5／R4=1.0。R3は段2で削除）
/// ＝既定はもう休眠ではない。各テストは**旧式（OFF）側を明示的に0へピン**して対照を取る
/// （pob-ignition-comment-sweep の教訓＝既定に寄りかかった対照は点火で空洞化する）。
/// 作法＝壁でなく逓減。
/// </summary>
public class RevoltIgnitionTests
{
    [Fact]
    public void 既定係数は点火済みの宣言値である()   // 番人＝いま live な値の宣言（うっかり動いたらここが赤になる）
    {
        // ⚠POB_COEF で明示的に上書きした測定走行では見ない（点火A/Bを同一バイナリで取るため）。
        var c = new Coefficients();
        string ov = Coefficients.OverrideSummary();
        if (!ov.Contains("CoercionMax", StringComparison.Ordinal)) Assert.Equal(2000.0, c.CoercionMax);   // R1と同時に水準も是正（旧4000）
        if (!ov.Contains("FactionRevoltLocalCoerce", StringComparison.Ordinal)) Assert.Equal(0.5, c.FactionRevoltLocalCoerce);
        // ★段2（設計メモ §7-3・作者裁定2026-09-09）で確定した4値＝史実から逆算した数（勘で置いていない）。うっかり動いたらここが赤になる。
        if (!ov.Contains("MilitancySpeed", StringComparison.Ordinal)) Assert.Equal(0.30, c.MilitancySpeed);
        if (!ov.Contains("RegionalRevoltThreshold", StringComparison.Ordinal)) Assert.Equal(0.50, c.RegionalRevoltThreshold);
        if (!ov.Contains("WarWearinessMilitancy", StringComparison.Ordinal)) Assert.Equal(0.35, c.WarWearinessMilitancy);
        if (!ov.Contains("RegionalRevoltStrength", StringComparison.Ordinal)) Assert.Equal(0.5, c.RegionalRevoltStrength);
        // ★構造条件（設計メモ §7-3）：σ が門を下回っていないと、平穏な州が1四半期の欠乏で一揆を起こす＝作者裁定「一瞬の飢餓では起きない」が破れる。
        Assert.True(c.MilitancySpeed < c.RegionalRevoltThreshold, "構造条件 σ < 門 が破れた＝1四半期の欠乏で一揆が発火する");
        Assert.True(1.0 - Math.Pow(1.0 - c.MilitancySpeed, 2) > c.RegionalRevoltThreshold, "2四半期でも門に届かない＝「半年の飢餓で大量の反乱軍」が表せない");
        // ★満期の厭戦（0.9）が単独では門に届かない＝ナポレオン戦争級の長期戦でも戦争だけでは全土蜂起しない（飢餓が足されて初めて越える）。
        Assert.True(0.9 * c.WarWearinessMilitancy < c.RegionalRevoltThreshold, "厭戦だけで門を越える＝1.5年超の全戦争が全土蜂起になる");
        if (!ov.Contains("ModernizationBudgetSoftCap", StringComparison.Ordinal)) Assert.Equal(1.0, c.ModernizationBudgetSoftCap);
    }

    [Fact]
    public void R1_ソフト化で高軍事費でも点火可能な帯が開く()   // 診断A1：旧CoercionMax=4000は等号点≈1250の3.2倍＝軍事費≳0.28で革命が代数的に不能だった
    {
        var c = new Coefficients();
        // 等号点（診断A1の導出）：challenge理論最大 ≈ Pop×1.25 → 点火 ⇔ BaseCoercion×Leg×funding < PoliticalReferencePop×1.25 ≈ 1250
        double ignitionBound = c.PoliticalReferencePop * 1.25;
        double soft = Government.CoercionTargetFromForce(1600.0, c);   // 旧なら1600＝点火不能の帯
        Assert.True(soft < ignitionBound,
            $"ソフト化しても点火帯が開かない（force1600 → {soft:F1} ≥ 等号点 {ignitionBound:F0}）");
        Assert.True(System.Math.Min(1600.0, c.CoercionMax) >= ignitionBound, "前提が崩れた：旧式でも同じ軍で点火可能になっている");
        // 壁でなく逓減：どれだけ大軍でも CoercionMax 未満（漸近）・単調増加は保つ（軍を増やせば保持力は増える＝限界効用だけ逓減）
        Assert.True(Government.CoercionTargetFromForce(1e12, c) < c.CoercionMax, "漸近が壁を超えた");
        Assert.True(Government.CoercionTargetFromForce(8000.0, c) < Government.CoercionTargetFromForce(16000.0, c), "単調性が壊れた");
    }

    [Fact]
    public void R2_大国の州クラスタが按分後に点火しうる()   // 診断A2：左辺=州クラスタ規模 vs 右辺=全国coerce＝人口の大きい国ほど点火不能の逆理
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var c = w.Coef;
        var faction = new FactionState { Region = w.Regions[1], Bloc = 2, Ideo = 5 };   // 非首都州の大衆クラスタ（LocalizedHoldはRegionのみ読む）
        double share = w.Regions[1].SumPopulation() / w.TotalPopulation;
        Assert.True(share > 0 && share < 0.9, $"前提が崩れた：州人口シェアが比として立たない（{share:F3}）");
        const double coerce = 1000.0;
        c.FactionRevoltLocalCoerce = 0;   // ★OFF側を明示ピン（既定は点火0.5）
        Assert.Equal(coerce, Politics.LocalizedHold(w, c, faction.Region, coerce));   // 旧＝全国coerceのまま（ビット恒等）
        c.FactionRevoltLocalCoerce = 1.0;   // 完全按分
        double local = Politics.LocalizedHold(w, c, faction.Region, coerce);
        Assert.Equal(coerce * share, local, 9);
        // 点火帯が開く：全国では鎮圧される規模（strength≤coerce）の運動が、州按分後の保持力は上回れる
        double strength = (local + coerce) / 2.0;
        Assert.True(strength > local && strength <= coerce, $"按分で点火帯が開かない（local {local:F1} / 全国 {coerce:F1}）");
        c.FactionRevoltLocalCoerce = 0.5;   // ブレンド中間＝旧と完全按分の間（点火A/Bで中間値を掃引できる連続化）
        double blended = Politics.LocalizedHold(w, c, faction.Region, coerce);
        Assert.True(local < blended && blended < coerce, $"ブレンドが単調でない（f=1: {local:F1} / f=0.5: {blended:F1} / f=0: {coerce:F1}）");
    }

    // ★段2（設計メモ §7-3・作者裁定2026-09-09）：R3（熱の欠乏因子に州平均満足を併置するスイッチ）は**機構ごと消えた**。
    //   熱＝州平均闘争度×欠乏因子 という2量の積は、欠乏を「闘争度の中（飢餓の駆動項）」と「熱の外側」の両方で数える二重計上だった。
    //   ⇒ 熱は1量になり、R3が解いていた「FoodFill=1.0 で熱が恒久0」という遮断スイッチも同じ変更で構造的に消えている。
    //   下の2本は、旧R3の2本を**同じ主張の新しい形**（積をやめた・飢えは闘争度を通る）へ書き直したもの＝弱めていない。
    [Fact]
    public void 段2_州の熱は州平均闘争度そのものである()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var r = sim.World.Region; var c = sim.World.Coef;
        foreach (var p in r.Pops) { p.Militancy = 0.6; p.Satisfaction = 0.3; }
        foreach (var fill in new[] { 1.0, 0.7, 0.0 })   // 主食充足をどう振っても熱は動かない＝熱の**外側**に欠乏因子はもう無い
        {
            r.FoodFill = fill;
            Assert.Equal(0.6, Politics.RegionRevoltHeat(r, c), 9);
        }
        Assert.Equal(Politics.AvgRegionMilitancy(r), Politics.RegionRevoltHeat(r, c));   // 定義そのもの（1量）
        // 旧R3の主張「食えていても満たされない州には熱が立つ」は保たれている＝闘争度が立てば熱が立つ（主食は関係ない）。
        r.FoodFill = 1.0;
        Assert.True(Politics.RegionRevoltHeat(r, c) > 0, "主食が満杯だと熱が恒久0＝遮断スイッチが戻っている");
    }

    [Fact]
    public void 段2_飢えは闘争度のストックを通ってから熱になる()
    {
        // ★作者裁定の受け入れ基準そのもの（設計メモ §6-4-1）＝「一瞬の飢餓では起きないが半年（2四半期）続けば越える」。
        //   これは**機構の主張**である（σと利得の算術＋クランプ）＝創発の主張ではない。
        // ★回B（2026-09-22）で駆動は**平常 FoodRef からの落ち込み**を測る（Politics.FamineDrive）＝盤面は「平常 1.0 から急に落ちた」に置く。
        var (w, c, noble) = FamineBoard(foodMet: 0.10, foodRef: 1.0);
        Assert.Equal(GainDefault, c.FamineDriveGain);   // ★既定＝導出値（回B 2026-09-22・Coefficients.FamineDriveGain のコメント）＝この番人自身が既定値の宣言
        Assert.Equal(OnsetDefault, c.FamineDropOnset);  // 起点の既定＝観測5カ国の落ち込み D の p90 の中央値（この番人自身が既定値の宣言）
        // 盤面＝「飢え以外は平穏」（参照点＝現在の生活水準で失望0・中核民族100%で民族項0・闘争度0）。
        // 食料は平常 1.0 から充足 0.10 へ落ちた＝落ち込み 0.90（2四半期目も慣れで 0.873）≫ 起点+1/利得＝駆動1.0＝targetは他項に依らずクランプで 1.0。
        Assert.True(c.FamineDropOnset + 1.0 / c.FamineDriveGain < 0.85, "前提：落ち込み0.85で飽和しない係数（盤面の置き方を見直す）");
        Politics.Step(w, c);
        Assert.Equal(c.MilitancySpeed, noble.Militancy, 9);                       // 1四半期＝σ
        Assert.True(noble.Militancy < c.RegionalRevoltThreshold, $"1四半期の欠乏で門を越えた（{noble.Militancy:F3}）＝作者裁定が破れる");
        Assert.Equal(1.0 + c.LivingRefSpeed * (0.10 - 1.0), noble.FoodRef, 12);   // 平常は駆動の【後】に慣れの速度で寄る（同一ティック内で閉じない）
        Politics.Step(w, c);
        Assert.Equal(1.0 - Math.Pow(1.0 - c.MilitancySpeed, 2), noble.Militancy, 9);   // 2四半期＝1−(1−σ)²
        Assert.True(noble.Militancy > c.RegionalRevoltThreshold, $"2四半期の欠乏でも門に届かない（{noble.Militancy:F3}）");
        Assert.Equal(1.0, noble.MilFamine);   // 飽和帯＝駆動1.0（診断の内訳も同じ値）
        // ★起点の番人：落ち込みが起点より小さい pop には飢餓の駆動が**厳密に0**（平時の揺れの9割）。
        //   落ち込み = 起点の半分 → 駆動 0。斜路の中（起点+0.5/利得＝門0.50に達する落ち込み）→ 駆動 0.5。
        double small = 0.5 * c.FamineDropOnset, ramp = c.FamineDropOnset + 0.5 / c.FamineDriveGain;
        foreach (var p in w.AllPops()) { p.Militancy = 0; p.FoodRef = 0.9; p.FoodMet = 0.9 - small; }
        Politics.Step(w, c);
        Assert.Equal(0.0, noble.MilFamine);
        // 闘争度の移動は**飢餓以外の3項だけ**（内訳の代入4本は target と同じ値・(551) で生活水準の項 MilLiving を削除）＝σ×(政治+厭戦+民族)。飢餓が1ビットでも入れば等式が破れる。
        Assert.Equal(c.MilitancySpeed * (noble.MilVoice + noble.MilWar + noble.MilNation), noble.Militancy, 9);
        foreach (var p in w.AllPops()) { p.Militancy = 0; p.FoodRef = 0.9; p.FoodMet = 0.9 - ramp; }
        Politics.Step(w, c);
        Assert.Equal(0.5, noble.MilFamine, 9);   // 斜路＝線形（クランプ前）＝門0.50に達する落ち込みで駆動ちょうど0.5
    }

    // ★回B（2026-09-22）の既定値の宣言（導出は Coefficients.FamineDriveGain のコメント）＝段2の番人が読む。
    private const double GainDefault = 30.0;
    private const double OnsetDefault = 0.07;

    /// <summary>番人の盤面＝英1789を20四半期回し、全popを「飢え以外は平穏」に置いて食料の充足と平常だけを与える。物差しは閉鎖pop（貴族）
    /// ＝F1の一揆の緩和も戦死も「非閉鎖pop」だけを書き換えるので、同一ティックの下流の書き込みから隔離できる（計器の隔離）。</summary>
    private static (World w, Coefficients c, Pop noble) FamineBoard(double foodMet, double foodRef)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var c = w.Coef;
        foreach (var p in w.AllPops())
        {
            p.Militancy = 0; p.LivingRef = p.Satisfaction; p.CoreEthnicShare = new Share(1.0);
            p.FoodNeed = 1.0; p.FoodMet = foodMet; p.FoodRef = foodRef;
        }
        Pop? noble = null;
        foreach (var p in w.AllPops()) if (p.Closed) { noble = p; break; }
        Assert.NotNull(noble);
        return (w, c, noble!);
    }

    [Fact]
    public void 階層3分割_飢えは闘争度へ飢餓の項1本だけで入る()
    {
        // ★番人(a)の政治側＋(e)（作者裁定2026-09-20「食料不足は闘争度、日用品は不満度、嗜好品は満足度」・2026-09-22 既定化）。
        //   主張は「既定の世界で、食料の不足は闘争度を上げる／その入口は飢餓の項だけ（生活の失望＝満足度を読む側は食料で1ビットも動かない）」。
        //   満足度の台帳に食料が載らないこと（経済側）は EconomyTests の番人が見る＝2本で「同じ欠乏が2つの量へ入らない」が閉じる。
        //   ★回B：不足＝平常 1.0 からの急な落ち込み（慢性の低さは駆動しない＝回B番人(b)）。
        static (double voice, double famine, double militancy) Probe(double foodMet)
        {
            var (w, c, noble) = FamineBoard(foodMet, foodRef: 1.0);
            Politics.Step(w, c);
            return (noble.MilVoice, noble.MilFamine, noble.Militancy);
        }
        Assert.True(new Coefficients().FamineDriveGain > 0, "飢餓の駆動が休眠に戻っている（裁定＝分割と対で既定の点火）");
        var fed = Probe(1.0);         // 平常どおり＝落ち込み0＝駆動0
        var starving = Probe(0.10);   // 平常 1.0 から 0.10 へ＝飽和帯＝駆動1
        Assert.Equal(0.0, fed.famine);
        Assert.Equal(1.0, starving.famine);
        // (a) 食料の不足は闘争度を上げる。
        Assert.True(starving.militancy > fed.militancy + 1e-9,
                    $"食料の不足が闘争度を上げない（飽食 {fed.militancy:F4} → 飢餓 {starving.militancy:F4}）");
        // (e) 入口は飢餓の項だけ：飢餓以外の項（ここでは政治の隔たり＝法の項）は食料の有無で1ビットも動かない（生活の項は闘争度の目標に無い＝(551) で Pop.MilLiving の欄ごと削除）。
        Assert.Equal(fed.voice, starving.voice);
    }

    [Fact]
    public void 回B_a_平常から急に落ちると飢餓の駆動が立つ()
    {
        // ★モラル・エコノミー（回B 2026-09-22・作者承認）：食料暴動は慣れた水準からの**急な**落ち込みで立つ。
        //   平常 1.0 の pop が充足 0.75 へ落ちる＝旧式の絶対水準（0.73 未満で駆動）では**駆動0**だった水準でも、落差 0.25 が起点を超えれば駆動が立つ。
        var (w, c, noble) = FamineBoard(foodMet: 0.75, foodRef: 1.0);
        double expect = Math.Min(1.0, c.FamineDriveGain * (0.25 - c.FamineDropOnset));
        Assert.True(expect > 0, "前提：落差0.25が起点を超える係数");
        Assert.Equal(expect, Politics.FamineDrive(noble, c), 12);   // 純関数＝ループが呼ぶ1本（平常−充足−起点）
        Politics.Step(w, c);
        Assert.Equal(expect, noble.MilFamine, 12);                  // ループの寄与も同じ値
        // 同じ盤面で「落ちていない」腕（平常＝今の充足 0.75）＝駆動0。闘争度は飢餓の項の分だけ上に居る。
        var (w0, c0, noble0) = FamineBoard(foodMet: 0.75, foodRef: 0.75);
        Politics.Step(w0, c0);
        Assert.Equal(0.0, noble0.MilFamine);
        Assert.True(noble.Militancy > noble0.Militancy + 1e-9, $"急な落ち込みが闘争度を上げない（{noble0.Militancy:F4} → {noble.Militancy:F4}）");
    }

    [Fact]
    public void 回B_b_慢性的に低くても平常どおりなら駆動は立たない()
    {
        // ★回Bの根（2026-09-22）：コンゴの人口の39.8%が充足0.51〜0.52に恒常的に居て、旧式（絶対水準）は慢性の栄養不足を飢饉と読んで飽和させた
        //   （同じ9カ国が両シードで1783〜84年に内戦）。慢性的に低いが平常どおりの pop には駆動が**厳密に0**。
        //   充足 0.45＝旧式の飽和帯（0.73−1/10=0.63 未満＝旧なら駆動1.0）。
        var (w, c, noble) = FamineBoard(foodMet: 0.45, foodRef: 0.45);
        for (int q = 0; q < 4; q++)
        {
            foreach (var p in w.AllPops()) { p.FoodNeed = 1.0; p.FoodMet = 0.45; }   // 1年間ずっと同じ低さ（慢性）
            Politics.Step(w, c);
            Assert.Equal(0.0, noble.MilFamine);
            Assert.Equal(0.45, noble.FoodRef, 12);   // 平常は慢性の低さのまま（慣れの更新は今の充足へ寄せる＝動かない）
        }
        // ★開始時（1783年の塊）の形：平常が未捕捉（-1）の pop は初回に今の充足で捕まる＝慢性の低さが「平常」として入り、駆動0。
        var (w2, c2, noble2) = FamineBoard(foodMet: 0.45, foodRef: -1.0);
        Assert.Equal(0.0, Politics.FamineDrive(noble2, c2));   // 純関数は未捕捉＝今の充足として読む（書かない）
        Assert.Equal(-1.0, noble2.FoodRef);
        Politics.Step(w2, c2);
        Assert.Equal(0.0, noble2.MilFamine);
        Assert.Equal(0.45, noble2.FoodRef, 12);
    }

    [Fact]
    public void R4_近代化投資のfrac06と10に区別が生まれる()   // 診断B3：minクランプがfrac≳0.5の凸コストを食い、0.6と1.0の区別が消滅していた
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        Assert.True(w.NominalGdpPerMonth.Value > 5_000, "前提が崩れた：GDPが小さすぎて要求額が国庫半分に届かない");
        w.SetTreasuryRaw(1_000.0);   // 国庫半分=500 ≪ 要求額＝minクランプが確実に食う帯
        double Budget(double frac) { w.ModernizationFrac = frac; return w.ModernizationBudget(); }
        w.Coef.ModernizationBudgetSoftCap = 0;   // ★OFF側を明示ピン（既定は点火1.0）
        double off06 = Budget(0.6), off10 = Budget(1.0);
        Assert.Equal(off06, off10);   // 旧＝区別の消滅（「100%は不可能」が実挙動に不存在）の再現
        Assert.True(off10 > 0);
        w.Coef.ModernizationBudgetSoftCap = 1.0;
        double on03 = Budget(0.3), on06 = Budget(0.6), on10 = Budget(1.0);
        Assert.Equal(off10, on10);    // frac=1.0＝漸近の極限＝旧の壁と同額（上限保存・convex=+∞でもNaNにならない）
        Assert.True(on06 < on10, $"frac0.6と1.0の区別が生まれない（0.6: {on06:F1} / 1.0: {on10:F1}）");
        Assert.True(on03 < on06, $"fracの単調性が壊れた（0.3: {on03:F1} / 0.6: {on06:F1}）");
        Assert.True(on06 > 0);
    }

    // ============================================================================================
    // ★段0＝幽霊の封じ（F3b蜂起doc §7-1・2026-09-08・係数なし）。
    //   反乱国家が建つと本拠州は親の World.Regions を離れるが、FactionState は親の w.Factions に残って
    //   Region 参照を握ったままになる（建国路は w.Factions を掃除しない）。州の State は Active のままなので
    //   State ゲートを素通りし、選ばれると民族分岐が**親が所有していない州**を SecedingRegion に代入 →
    //   StartCivilWar → CivilWarYears が減らないまま政治が永久凍結する＝破綻。
    //   選ばれた運動には**通知の札**（World.PendingFactionCrisis＝要求は法の経路・2026-09-11点火）が必ず立つので、
    //   「札がその運動か」＝「選ばれたか」の分岐非依存の物差しになる（決着は翌評価＝Rejections は通知の四半期には動かない）。
    // ============================================================================================
    private static FactionState 巨大な運動を差し込む(World w, Region home)
    {
        var f = new FactionState
        {
            Region = home, Bloc = 2, Ideo = (int)Ideology.Communism,
            Size = 1e12, AvgMilitancy = 1.0, Cohesion = 1.0,   // 結束1.0＝1四半期の不在減衰(0.10)後も闘争閾値0.8を上回る
            Mutated = true, MutantName = "幽霊の旗",   // 旗＝忘却と変異ロールの側の量（★段2で強度からは組織核ごと消えた）
            Rejections = 3.0,   // 通知の四半期には動かない（動けば「決着の枝を踏んだ」＝物差しの汚れ）
        };
        w.Factions.Add(f);
        return f;
    }

    [Fact]
    public void 段0対照_自国の州を本拠に持つ巨大な運動は選ばれる()   // ⚠先に対照を取る：これが通らないと下の除外テストは空洞（機構に届いていない）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        Assert.True(w.Regions.Count > 1, "前提が崩れた：非首都州がない");
        Assert.True(w.FactionRng is not null && w.Coef.FactionRevoltScale > 0, "前提が崩れた：F3bが遮蔽されている");
        var own = 巨大な運動を差し込む(w, w.Regions[1]);
        Assert.True(Politics.FactionStrength(w, w.Coef, own) > Politics.LocalizedHold(w, w.Coef, own.Region, PoliticalQuantities.EffectiveCoerce(w, w.Coef)) + Politics.LoyalistMilitia(w, w.Coef, own),
            "前提が崩れた：差し込んだ運動が保持力を超えていない");
        Politics.Step(w, w.Coef);
        Assert.Same(own, w.PendingFactionCrisis);   // 対照＝自国の州の運動は選ばれる（通知の札が立つ）
    }

    [Fact]
    public void 段0_他国の州を本拠に持つ勢力は選ばれない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var other = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        sim.Run(20); other.Run(20);
        var w = sim.World;
        Assert.True(other.World.Regions.Count > 1, "前提が崩れた：他国に非首都州がない");
        var foreign = other.World.Regions[1];   // ★反乱国家へ移った後の本拠州と同じ状態＝親の Regions にない
        Assert.DoesNotContain(foreign, w.Regions);
        var ghost = 巨大な運動を差し込む(w, foreign);
        Politics.Step(w, w.Coef);
        Assert.NotSame(ghost, w.PendingFactionCrisis);   // 選定ループが幽霊を素通りした（通知の札が立たない）
        Assert.Equal(3.0, ghost.Rejections);              // どの決着枝にも入っていない
        Assert.NotSame(ghost, w.RevoltingFaction);
        Assert.NotSame(foreign, w.SecedingRegion);   // ★破綻の道：親が所有しない州が分離戦の対象になっていない
        if (w.PendingRebelRegions is { } pend) Assert.DoesNotContain(foreign, pend);
    }

    [Fact]
    public void 段0_合流ループも他国の州の運動を吸わない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var other = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        sim.Run(20); other.Run(20);
        var w = sim.World; var c = w.Coef;
        Assert.True(c.FactionMergeRangeKm > 0, "前提が崩れた：合流が休眠＝この番人は空洞（doc の『0で休眠』は陳腐化）");
        var mine = new FactionState { Region = w.Regions[1], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 100, AvgMilitancy = 0.5, Cohesion = 0.4 };
        var ghost = new FactionState { Region = other.World.Regions[1], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 100_000, AvgMilitancy = 0.5, Cohesion = 0.9 };
        w.Factions.Add(mine); w.Factions.Add(ghost);
        string ghostRegionName = ghost.Region.Name;
        Factions.Step(w, c);
        // 合流は成立時に必ずログを1行書く＝**合流したか**の分岐非依存の物差し（結束の値は不在減衰でも動くので物差しにしない）。
        Assert.DoesNotContain(w.PendingLog, l => l.Contains("合流") && l.Contains(ghostRegionName));
        Assert.True(mine.Cohesion > 0, "自国の運動が幽霊に吸収された（吸収側は small.Cohesion=0 を書く）");
    }

    // ============================================================================================
    // ★段2＝鎮圧の後始末（F3b蜂起doc §3-4・作者裁定②＝A・2026-09-08）。
    //   内戦一元化の主経路（War で戦う）の再併合サイト（MultiNationSim.ReabsorbNations）には後始末が1行も無かった＝鎮圧されても運動は結束満額のまま残り、
    //   同じ旗が翌期また立つ。★2026-09-28（処理の一元化 段2 束2）：休眠の係数 RevoltAftermathOnWarPath を削除し、旧経路の鎮圧の行（Politics.SettleRevolt）を
    //   常に通す＝運動の燃え尽き・断裂の窓・**反乱していた州（返還州）の住民だけ**の闘争度の緩和（作者裁定 d）。正統性の減算は旧経路の鎮圧の行に無い＝持ち込まない。
    //   ⚠実経路（F3b点火→建国→戦争→講和）は稀＝**再併合の予約を直接置き、四半期の他の流れを挟まずに再併合の1段だけを踏む**（FiscalTests の再併合の貸借と同じ流儀）
    //     ＝機構の検査であって創発の主張ではない。
    // ============================================================================================
    [Fact]
    public void 段2_戦争経路の再併合で運動が燃え尽き返還州だけが静まる()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()],
                                     seed: 1789, coef: new Coefficients());
        sim.Run(8);
        var pw = sim.Nations[0].World; var rw = sim.Nations[1].World;
        Assert.True(pw.SelfPower is not null && rw.SelfPower is not null && pw.Regions.Count > 0 && rw.Regions.Count > 0);
        var c = sim.Coef;
        var spent = new FactionState   // 内乱の主体＝本拠州が反乱国の州（親が武力で決着させた運動・結束は闘争閾値0.8を大きく超えたまま）
        {
            Region = rw.Regions[0], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0,
        };
        pw.Factions.Add(spent);
        pw.RevoltingFaction = spent;
        pw.Legitimacy = 0.5; pw.RuptureWindowQuarters = 0;
        var returning = new System.Collections.Generic.List<Region>(rw.Regions);   // 返還される州（＝闘争度の緩和の対象）
        var home = new System.Collections.Generic.List<Region>(pw.Regions);        // 親の元からの州（＝緩和しない・作者裁定 d）
        foreach (var r in returning) foreach (var p in r.Pops) p.Militancy = 0.5;
        foreach (var r in home) foreach (var p in r.Pops) p.Militancy = 0.4;
        sim.Diplomacy.PendingReintegrations.Add((pw.SelfPower!, rw.SelfPower!, false, pw.Polity));   // 親の勝利＝鎮定して再併合
        sim.ReabsorbNations();
        Assert.Equal(0.0, spent.Cohesion);             // 燃え尽き＝翌期そのまま再蜂起できない
        Assert.Null(pw.RevoltingFaction);
        foreach (var r in returning) foreach (var p in r.Pops) Assert.Equal(0.5 * c.ReformMilitancyRelief, p.Militancy, 12);   // 返還州だけが静まる
        foreach (var r in home) foreach (var p in r.Pops) Assert.Equal(0.4, p.Militancy, 12);                                  // 親の元からの州は触らない
        Assert.Equal((int)c.RuptureWindowQuartersOnCrush, pw.RuptureWindowQuarters);   // 反動の窓（1849年）
        Assert.Equal(0.5, pw.Legitimacy);             // 正統性は削らない（旧経路の鎮圧の行に無い）
    }

    // ============================================================================================
    // ★段3＝亡霊の残り(i)（F3b蜂起doc・2026-09-08・係数なし）：好機の「弱み」から**内戦を除く**。
    //   Government.AtWar は CivilWarParent 付きの戦争も数えるので、内戦一元化の主経路では反乱が1つ立った瞬間に
    //   親が恒久に弱みになり、好機のブレーキ（FactionOpportunism）も圧倒判定も両方外れた＝1つ目の蜂起が2つ目を呼ぶ。
    //   ⚠2本の腕の違いは**普通の戦争を1本足したかどうかだけ**＝点火したなら弱みの項が読んだのは外征だと確定する
    //     （足りない強度で落ちたのではないことは、外征ありの腕が点火することが証明する）。
    //   ★2026-09-29 段6 の点火（共有クールダウンの撤去）で、内戦の最中は状態の排他（World.InCivilWar）が選定そのものを止める
    //     ＝「1つ目の蜂起が2つ目を呼ばない」は排他が構造で満たす（外征の弱みがあっても内戦の最中は新しい旗は立たない）。
    //     対照は内戦の無い親＝外征の弱みで選ばれ、弱みが無ければ待つ（好機のブレーキ）。
    // ============================================================================================
    private static bool 段3_親で選定を1回回す(bool civilWar, bool addExternalWar)
    {
        // 圧倒の迂回路を閉じる＝**弱みの項だけ**が点火を決める帯にする。★2026-09-28 回8-1：自発的な開戦も止める（外征は対照の腕が手で足す1本だけ）
        //   ＝AI の挑発がならしで毎月動くようになり、8四半期の助走で仏英が開戦しうる（内戦だけの腕に外征が紛れると弱みの切り分けが崩れる）。
        var coef = new Coefficients { FactionOverwhelmFactor = 1e9, WarDeclareTension = 1e9 };
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()],
                                     seed: 1789, coef: coef);
        sim.Run(8);
        var pw = sim.Nations[0].World;
        Assert.True(coef.FactionOpportunism > 0, "前提が崩れた：好機のブレーキが休眠＝この番人は空洞");
        Assert.True(pw.FactionRng is not null && coef.FactionRevoltScale > 0, "前提が崩れた：F3bが遮蔽されている");
        if (civilWar) sim.Diplomacy.DeclareCivilWar(pw.SelfPower!, sim.Nations[1].World.SelfPower!, pw.Polity, coef);   // 反乱国が地図の上に立った状態
        Assert.False(sim.Diplomacy.Wars.Exists(w => (w.A == pw.SelfPower || w.B == pw.SelfPower) && w.CivilWarParent is null), "前提が崩れた：助走の間に外征が立った＝内戦だけの腕にならない");
        if (addExternalWar) sim.Diplomacy.Wars.Add(new War { A = pw.SelfPower!, B = sim.Nations[2].World.SelfPower! });   // 対照＝外征（内戦タグなし）
        Assert.Equal(civilWar || addExternalWar, Government.AtWar(pw));   // 前提＝内戦も交戦として数えられる（旧経路の再現）
        Assert.Equal(civilWar, pw.InCivilWar);
        pw.Legitimacy = 1.0;   // 弱みの正統性項を閉じる（閾値0.5＝1四半期の変動では割らない）
        Assert.False(pw.GdpDrawdown.Value > coef.FactionStrikeDrawdown.Value, $"前提が崩れた：恐慌項が開いている（GdpDrawdown {pw.GdpDrawdown:F3}）＝弱みの切り分けができない");
        var f = 巨大な運動を差し込む(pw, pw.Regions[0]);   // 段0と同じ注入＝保持力を桁で上回る運動（力の門は確実に越える）
        Politics.Step(pw, coef);
        return ReferenceEquals(pw.PendingFactionCrisis, f);   // 札が立った＝選定に入った／立たない＝好機を待った
    }

    [Fact]
    public void 段3_内戦の最中の親には次の旗が立たず_外征の弱みでも開かない()
    {
        Assert.True(段3_親で選定を1回回す(civilWar: false, addExternalWar: true), "対照が成立しない：外征の弱みで選ばれていない＝強度が足りない帯に落ちている");
        Assert.False(段3_親で選定を1回回す(civilWar: false, addExternalWar: false));   // 弱みが無い＝圧倒に届かない運動は待つ（好機のブレーキ）
        Assert.False(段3_親で選定を1回回す(civilWar: true, addExternalWar: true));    // 外征の弱みがあっても、内戦の最中は新しい旗は立たない（状態の排他）
        Assert.False(段3_親で選定を1回回す(civilWar: true, addExternalWar: false));   // 1つ目の蜂起が2つ目を呼ばない
    }

    // ============================================================================================
    // ★段4＝点火の付け替え（F3b蜂起doc §3-2・作者裁定・2026-09-08）。
    //   旧＝点火も決着も同じ不等式（FactionStrength > 保持力+郷勇）＝蜂起は**勝てるときにしか始まれない**（成功率100%）。
    //   さらに左辺は f.Size に比例する州クラスタ規模・右辺の郷勇は全国スケール＝点火が人口に反比例した。
    //   新＝連言2門：門1 組織された意志（FactionStrength÷f.Size＝示強量）／門2 州の絶望（RegionRevoltHeat＋怨嗟）。
    //   ⚠ON経路の係数は **w.Coef に世界側で書く**（MultiNationSim(coef:) は sim 側にしか効かない既知の罠。ここは単国 Simulation なので
    //     w.Coef が Politics.Step へ渡す当のインスタンス）。
    //   ⚠物差しは段0・段3と同じ通知の札（World.PendingFactionCrisis）＝「選ばれたか」の分岐非依存の量（立たなければ ClearFactionCrisis で null）。
    // ============================================================================================
    private static (World W, FactionState F) 段4_絶望した州に小さな運動を置く(double regionMilitancy, double fight, double rejections)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        // 初期の国債（world.json の govDebt＝1783年の英 年GDP の約1.1・2026-09-29）を構築の直後に消す＝旧の盤面（英の初期国債0）と同じ状態から回す。
        //   ⚠初期の国債があると「緊縮の下限／上限」の門が人頭税と社会保障の段を塞ぎ、この盤面が測る法の経路（受諾・施行・通らない段）の前提が崩れる（門そのものの番人は別）。
        //   2026-09-29 に緊縮の物差しを年の名目GDP へ直した後も戻さない＝英の1783年の国債（年GDP の約1.1）は満額（AusterityStartDebt＋AusterityRange＝1.0）を越える
        //   ＝史実のピットの緊縮（1783-92）そのもの（物差しの誤りではない）。金は動かない＝請求権と保有の記録だけを消す。
        sim.World.GovDebt = new MoneyStock(0); sim.World.ForeignDebt = new MoneyStock(0);
        foreach (var r in sim.World.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);
        sim.Run(20);
        var w = sim.World;
        Assert.True(w.Regions.Count > 1 && w.FactionRng is not null && w.Coef.FactionRevoltScale > 0, "前提が崩れた：F3bが遮蔽されている");
        Assert.True(w.Suffrage < Suffrage.AdultMale, $"前提が崩れた：大衆の発言権が開いている（{w.Suffrage}）＝(1−発言権)=0 で強度が消える");
        var home = w.Regions[1];
        // ★段2：州の絶望＝RegionRevoltHeat＝**州平均闘争度そのもの**（欠乏因子との積はやめた）。
        // ⚠σが 0.03→0.30 になったので、注入した闘争度が Politics.Step の**同一ティック内**で大きく動く（旧はσが小さく事実上不動だった）。
        //   この番人は**点火の門**を測るものであってσを測るものではないので、σを0にピンして盤面を凍結する＝較正値から独立させる。
        w.Coef.MilitancySpeed = 0;
        foreach (var p in home.Pops) { p.Militancy = regionMilitancy; p.Satisfaction = 0; p.Consciousness = 0; }
        var f = new FactionState
        {
            Region = home, Bloc = 2, Ideo = (int)Ideology.Communism,
            Size = 1.0, AvgMilitancy = fight, Cohesion = 1.0,
            Mutated = true, MutantName = "旗を掲げた運動",
            Rejections = rejections,
        };
        w.Factions.Add(f);
        return (w, f);
    }

    [Fact]
    public void 段4_力の門に届かない運動でも両門を越えれば点火する()
    {
        // OFF腕＝現行（既定0）。ON腕＝同じ世界・同じ注入で係数1本だけ違う。
        var (offW, offF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 1.0, rejections: 3.0);
        offW.Coef.RevoltIgnitionIntensity = 0;   // ★OFF側を明示ピン（既定に寄りかかった対照は点火で空洞化する）
        double coerce = PoliticalQuantities.EffectiveCoerce(offW, offW.Coef);
        double strength = Politics.FactionStrength(offW, offW.Coef, offF);
        double hold = Politics.LocalizedHold(offW, offW.Coef, offF.Region, coerce);
        double militia = Politics.LoyalistMilitia(offW, offW.Coef, offF);
        // ★この不等式こそが欠陥の実体＝旧経路では「勝てない運動は始まりもしない」
        Assert.True(strength <= hold + militia, $"前提が崩れた：小さな運動が力の門を越えている（強度 {strength:F3} vs 保持力+郷勇 {hold + militia:F3}）");
        Assert.True(Politics.RegionRevoltHeat(offF.Region, offW.Coef) > offW.Coef.RegionalRevoltThreshold, "前提が崩れた：州の絶望が門2に届いていない");
        Politics.Step(offW, offW.Coef);
        Assert.Null(offW.PendingFactionCrisis);   // 旧＝力の門で落ちる＝選定に入らない

        var (onW, onF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 1.0, rejections: 3.0);
        onW.Coef.RevoltIgnitionIntensity = 0.5;   // 門1＝示強量（この注入の意志は結束の1四半期減衰後も0.9）
        double intensity = Politics.FactionStrength(onW, onW.Coef, onF) / onF.Size;
        Assert.True(intensity > onW.Coef.RevoltIgnitionIntensity, $"前提が崩れた：注入した運動の意志が門1に届いていない（{intensity:F3}）");
        Politics.Step(onW, onW.Coef);
        Assert.Same(onF, onW.PendingFactionCrisis);   // 両門を越えた運動が点火した（通知の札）
    }

    [Fact]
    public void 段4_戦う気の乏しい運動は同じ絶望でも点火しない()
    {
        // ★段2で書き直し：旧は「旗（変異）＝組織核 org」の有無で2腕を作っていたが、**org は作者裁定で削除**された
        //   （組織の物差しは結束1本）。門1が実在することの対照は、いま強度に残っている因子＝**運動の闘争度**で取る。
        //   ⚠結束は落とせない（結束<FactionCohesionFightThreshold=0.8 は候補の走査そのものから外れる＝門1でなく足切りを測る空洞の番人になる）。
        var (yesW, yesF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 1.0, rejections: 3.0);
        yesW.Coef.RevoltIgnitionIntensity = 0.5;
        Politics.Step(yesW, yesW.Coef);
        Assert.Same(yesF, yesW.PendingFactionCrisis);   // 対照＝戦う気のある運動は点火する

        var (noW, noF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 0.4, rejections: 3.0);
        noW.Coef.RevoltIgnitionIntensity = 0.5;
        Assert.True(noF.Cohesion >= noW.Coef.FactionCohesionFightThreshold, "前提が崩れた：足切りで落ちる＝門1を測れていない");
        Assert.True(Politics.FactionStrength(noW, noW.Coef, noF) / noF.Size <= noW.Coef.RevoltIgnitionIntensity,
                    "前提が崩れた：闘争度を落とした腕でも門1を越えている");
        Assert.True(Politics.RegionRevoltHeat(noF.Region, noW.Coef) > noW.Coef.RegionalRevoltThreshold, "前提が崩れた：この腕で州の絶望が落ちている＝2腕の違いが闘争度だけでない");
        Politics.Step(noW, noW.Coef);
        Assert.Null(noW.PendingFactionCrisis);   // 絶望だけでは組織運動の内乱にならない（F1の一揆の領分）
    }

    [Fact]
    public void 段4_受諾で怨嗟が消えた運動は絶望が門を割る()
    {
        // 州の絶望を門2の**すぐ下**（闘争度0.45＝熱0.45 < 0.50）に置き、怨嗟だけが門を越えさせる帯で測る。
        // 拒否は蜂起・本丸拒否（本丸以外の法だけが動いた）で積み、**本丸の法が通れば0**＝チャーティズムが法で溶けた物理。
        var (angryW, angryF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.45, fight: 1.0, rejections: 3.0);
        angryW.Coef.RevoltIgnitionIntensity = 0.5;
        angryW.Coef.RevoltGrievanceWeight = 0.2;
        double heat = Politics.RegionRevoltHeat(angryF.Region, angryW.Coef);
        Assert.True(heat < angryW.Coef.RegionalRevoltThreshold, $"前提が崩れた：怨嗟なしでも門2を越える帯にいる（熱 {heat:F3}）");
        Assert.True(heat + angryW.Coef.RevoltGrievanceWeight * Math.Min(1.0, angryW.Coef.FactionRejectionMutation * angryF.Rejections) > angryW.Coef.RegionalRevoltThreshold,
                    "前提が崩れた：怨嗟を足しても門2に届かない＝この帯では対照が取れない");
        Politics.Step(angryW, angryW.Coef);
        Assert.Same(angryF, angryW.PendingFactionCrisis);   // 怨嗟の積み上がった運動が点火した

        var (calmW, calmF) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.45, fight: 1.0, rejections: 0.0);   // 要求が通った運動
        calmW.Coef.RevoltIgnitionIntensity = 0.5;
        calmW.Coef.RevoltGrievanceWeight = 0.2;
        Politics.Step(calmW, calmW.Coef);
        Assert.Null(calmW.PendingFactionCrisis);   // 選ばれていない
        Assert.Equal(0.0, calmF.Rejections);
    }

    // ============================================================================================
    // ★裁定1＝発言権の床（F3b蜂起doc §8(A)・作者裁定②・2026-09-08）。
    //   (1−発言権) は旧支配層(bloc0)で恒久0、大衆(bloc2)も男子普選の到達後は恒久0＝**強度が恒久ゼロ**＝
    //   点火式をどう直しても武装できない（実測：1888年の英仏の大衆運動は闘争度0.99・結束1.00でも intensity 0.0000）。
    //   ⚠段4本体とは別の係数＝単独で測れる（参政権の意味は決着・UI・AIの択も読む）。
    // ============================================================================================
    [Fact]
    public void 裁定1_発言権の床が旧支配層と普選後の大衆に強度を残す()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var c = w.Coef;
        Assert.Equal(0.0, c.RevoltVoiceFloor);   // 既定＝休眠（この番人自身が既定値の宣言）
        FactionState Make(int bloc) => new()
        {
            Region = w.Regions[1], Bloc = bloc, Ideo = (int)Ideology.Communism,
            Size = 1000, AvgMilitancy = 1.0, Cohesion = 1.0,
            Mutated = true, MutantName = "旗を掲げた運動",
        };
        var mass = Make(2); var oldRuling = Make(0);
        const double floor = 0.2;
        double full = 1000 * c.FactionRevoltScale;   // 声なき大衆の強度＝Size×闘争1×結束1×(1−0)×scale（★段2で org・年季は削除）

        // (i) 声を持たない側＝床より上＝**1ビットも動かない**（床は上限を作らない）
        Assert.True(w.Suffrage < Suffrage.AdultMale, "前提が崩れた：この時点で男子普選に達している");
        Assert.Equal(full, Politics.FactionStrength(w, c, mass));
        c.RevoltVoiceFloor = floor;
        Assert.Equal(full, Politics.FactionStrength(w, c, mass));

        // (ii) 旧支配層＝(1−発言権)≡0＝恒久ゼロ（ヴァンデ1793・カルリスタ1833・西南戦争1877・クリステロ1926が構造的に不可能だった）
        c.RevoltVoiceFloor = 0;
        Assert.Equal(0.0, Politics.FactionStrength(w, c, oldRuling));
        c.RevoltVoiceFloor = floor;
        Assert.Equal(full * floor, Politics.FactionStrength(w, c, oldRuling), 9);

        // (iii) 男子普選の到達後の大衆も同じ穴（スパルタクス団1919・ルール1920・アストゥリアス1934）
        w.Suffrage = Suffrage.AdultMale;
        c.RevoltVoiceFloor = 0;
        Assert.Equal(0.0, Politics.FactionStrength(w, c, mass));
        c.RevoltVoiceFloor = floor;
        Assert.Equal(full * floor, Politics.FactionStrength(w, c, mass), 9);
    }

    // ============================================================================================
    // ★段5＝民兵と寝返り（F3b蜂起doc・作者裁定3・2026-09-08）。
    //   ①民兵は**POPの状態**（Pop.MilitiaShare＝そのPOPの何割が武器を取ったか）＝IsSoldier（常備軍）とは別概念。
    //   ②蜂起した運動の**自分のブロック**から不満度加重で立つ（⚠階級の禁止規則をどこにも書かない）。
    //   ③兵士POPの寝返りは**割合**（闘争度が決める＝二値にしない）。④戦力への寄与＝兵数×a。
    //   ⑤剥がれるのは「戦争の終わり」という**状態**（タイマーではない）。⑥死は既存経路の**配分の重み**として入る。
    //   ⚠ON経路の係数は w.Coef（単国 Simulation ではこれが Politics.Step へ渡る当のインスタンス）／
    //     建国路（MultiNationSim.SpawnNation）は sim 側の _coef を読むので **コンストラクタの coef:** で渡す。
    // ============================================================================================
    private static (World W, FactionState F) 段5_蜂起させる(double militiaRaise, int bloc = 2, double voiceFloor = 0, double theta = 0.5, Action<Region>? tune = null)
    {
        var (w, f) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 1.0, rejections: 3.0);
        f.Bloc = bloc;
        w.Coef.RevoltIgnitionIntensity = theta;
        w.Coef.RevoltVoiceFloor = voiceFloor;
        w.Coef.FactionCohesionDecay = 0;        // 不在減衰を凍結（通知→評価の2四半期で結束が門を割らない）
        w.Coef.RegionalRevoltThreshold = 0;     // F1 遮蔽（同じ州の熱で F1 が先に燃えると共有クールダウンが F3b の評価を止める）
        w.Coef.RevoltMilitiaRaise = militiaRaise;
        // ★2026-09-28（処理の一元化 段2 束3）：単国の世界の蜂起は地図の上に立てられない＝即時の決着（旧＝旧経路の内戦 ProcessCivilWar）。
        //   小さな運動は負ける＝本拠州に鎮圧の後始末（闘争度×ReformMilitancyRelief）が掛かる＝この番人が測る「民兵を立てた後の闘争度」が書き換わる。
        //   民兵の札だけを測るので後始末の緩和を1（据え置き）にピンする（後始末そのものは WarThemeTests の番人が見る）。
        w.Coef.ReformMilitancyRelief = 1.0;
        tune?.Invoke(f.Region);
        // ★要求は法の経路（2026-09-11点火）：1四半期目＝通知（誰も法の目標を置かない＝NullAgent）→翌評価で要求が通っていない＝蜂起
        Politics.Step(w, w.Coef);
        Assert.Same(f, w.PendingFactionCrisis);
        Politics.Step(w, w.Coef);
        Assert.True(w.FactionRevolts > 0, $"前提が崩れた：蜂起が点火していない（bloc {bloc}／床 {voiceFloor}／θ {theta}）");
        return (w, f);
    }

    [Fact]
    public void 段5_係数0では民兵の札が1枚も立たない()   // (i) 恒等
    {
        // ⚠既定が0であること自体は DormantIdentityTests の DormantZeroFields が唯一の宣言（ここで二重に書かない）。
        //   ここが見るのは「0のとき機構が本当に何もしないか」＝蜂起は起きたのに札が1枚も立たないこと。
        var (w, _) = 段5_蜂起させる(militiaRaise: 0);
        foreach (var p in w.AllPops()) Assert.Equal(0.0, p.MilitiaShare.Value);   // 蜂起は起きたのに札は1枚も立たない＝読み手（労働・戦力・死の配分）は全て素通り
    }

    [Fact]
    public void 段5_不満度の高いブロックほど民兵率が高い()   // (ii)
    {
        Pop? angry = null, calm = null, soldier = null;
        var (w, f) = 段5_蜂起させる(militiaRaise: 0.5, tune: home =>
        {
            foreach (var p in home.Pops)
            {
                if (p.IsSoldier) { soldier = p; p.Militancy = 1.0; continue; }   // 常備軍＝民兵ではない（闘争度は最大にして対照を作る）
                if (p.ClassId == PopClass.Peasant) { angry = p; p.Militancy = 0.8; }        // 農民＝大衆ブロック
                else if (p.ClassId == PopClass.Worker) { calm = p; p.Militancy = 0.2; }    // 都市労働者＝同じブロック・**不満だけが違う**
            }
        });
        Assert.True(angry is not null && calm is not null && soldier is not null, "前提が崩れた：農民/都市労働者/兵士のPOPが揃っていない");
        // 式そのもの＝min(1, 係数×そのPOPの闘争度)。⚠闘争度は Politics.Step の中で target へ寄るので**Step後の値**で照合する
        //   （民兵を立てた後に闘争度を書き換える経路はこの枝に無い）。
        Assert.Equal(Math.Min(1.0, 0.5 * angry!.Militancy), angry.MilitiaShare.Value, 9);
        Assert.Equal(Math.Min(1.0, 0.5 * calm!.Militancy), calm.MilitiaShare.Value, 9);
        Assert.True(angry.MilitiaShare > calm.MilitiaShare, $"不満の順序が民兵率の順序になっていない（怒 {angry.MilitiaShare:F3} ≤ 静 {calm.MilitiaShare:F3}）");
        Assert.Equal(0.0, soldier!.MilitiaShare.Value);   // 兵士POPは民兵にならない（寝返りは建国路の別機構）
        foreach (var p in f.Region.Pops) if (p.IsModernElite && !p.IsSoldier) Assert.Equal(0.0, p.MilitiaShare.Value);   // 別ブロック（近代エリート）は立たない＝旗が違う
    }

    [Fact]
    public void 段5_反動の蜂起では有産の側に民兵が立つ()   // (ii) 階級の禁止規則が無いことの対照＝旧支配層の運動（ヴァンデ1793・カルリスタ1833・西南戦争1877）
    {
        Pop? noble = null;
        // 旧支配層は (1−発言権)≡0＝裁定1の床が無いと武装できない＝床を敷いた世界で測る（θ は床0.5×結束0.9＝0.45 の下）。
        var (w, f) = 段5_蜂起させる(militiaRaise: 0.5, bloc: 0, voiceFloor: 0.5, theta: 0.4, tune: home =>
        {
            foreach (var p in home.Pops) if (p.Closed && !p.IsSoldier) { noble = p; break; }
        });
        Assert.True(noble is not null, "前提が崩れた：本拠州に閉鎖階級（貴族・聖職）のPOPがいない");
        Assert.True(noble!.MilitiaShare.Value > 0, "旧支配層の蜂起で有産の民兵が1人も立たない＝階級の禁止規則が入り込んでいる");
        // 大衆が立たないのは**階級で禁じたから**ではなく、この運動のブロックでないから（旗が違う）
        foreach (var p in f.Region.Pops) if (!p.Closed && !p.IsModernElite && !p.IsSoldier) Assert.Equal(0.0, p.MilitiaShare.Value);
    }

    [Fact]
    public void 段5_戦争の終わりで民兵率が0へ戻る()   // (iii) タイマーではなく状態で剥がす
    {
        var (w, f) = 段5_蜂起させる(militiaRaise: 0.5);
        double raised = 0; foreach (var p in f.Region.Pops) raised = Math.Max(raised, p.MilitiaShare.Value);
        Assert.True(raised > 0, "前提が崩れた：民兵が1人も立っていない");
        // 内戦の状態＝この国を親とする内戦タグの戦争（World.InCivilWar の1本）。★2026-09-28：旧＝旧経路の内戦（CivilWarYears）が立った
        //   ＝その経路は削除・単国の世界の蜂起は即時の決着＝戦争の形の内戦の状態を盤面に置いて「状態で剥がす」を測る。
        var dip = new Diplomacy();
        var me = new Power { Name = "政府", Nation = w };
        w.Diplomacy = dip; w.SelfPower = me;
        dip.Wars.Add(new War { A = me, B = new Power { Name = "反乱" }, CivilWarParent = me });
        Assert.True(w.InCivilWar, "前提が崩れた：内戦の状態になっていない");

        Politics.Step(w, w.Coef);   // 内戦中の四半期＝剥がれない（時間で消していないことの対照）
        double still = 0; foreach (var p in f.Region.Pops) still = Math.Max(still, p.MilitiaShare.Value);
        Assert.True(still > 0, "内戦中に民兵の札が剥がれた＝戦争という状態でなく時間で剥がしている");

        // 戦争の終わり（再併合・独立の確定・白紙分断のどれで終わっても同じ状態＝内戦でなくなる）
        dip.Wars.Clear(); w.RevoltingFaction = null; w.PendingRebelRegions = null;
        Politics.Step(w, w.Coef);
        foreach (var p in w.AllPops()) Assert.Equal(0.0, p.MilitiaShare.Value);   // 武器を置いて畑と工場へ戻る＝労働供給がその瞬間に回復する
    }

    [Fact]
    public void 段5_民兵は札であって人も金も動かさない()   // (iv) 保存
    {
        var (offW, _) = 段5_蜂起させる(militiaRaise: 0);
        var (onW, _) = 段5_蜂起させる(militiaRaise: 0.5);
        double raised = 0; foreach (var p in onW.AllPops()) raised += p.MilitiaShare.Value;
        Assert.True(raised > 0, "前提が崩れた：ON腕で民兵が立っていない");
        // 2腕の違いは札だけ＝同じ四半期の人口も貨幣も1ビットも違わない（民兵は人を作らない・移さない）
        Assert.Equal(offW.TotalPopulation, onW.TotalPopulation, 9);
        Assert.Equal(offW.BaseMoney.Value, onW.BaseMoney.Value, 9);
    }

    [Fact]
    public void 段5_死は増えず配分だけが民兵へ寄る()   // 裁定3＝死は既存の経路を通す（新しい死の経路を作らない）
    {
        // 同じ州・同じブロックの2つのPOPを「武器を取った側」と「家にいた側」に分け、戦場の民間人死（Diplomacy の戦争の民間人死）の配分だけを見る。
        // ★2026-09-28（処理の一元化 段2 束3）：旧＝旧経路の内戦の民間人死（ProcessCivilWar・CivilWarCivilianToll）で測った＝その経路は削除
        //   ＝内戦は全て戦争の形＝蜂起した国の民間人も対外戦争と同じ1本（WarCivilianToll×劣勢×接触）で死ぬ＝その1本の配分の重みを測る。
        //   ⚠総数では比べない：a は Power.Military（民兵の戦力）にも入る＝押しと戦死が動く。見るのは配分の重みそのもの＝同じ月の2つのPOPの死亡率の比
        //   （重み 1+a×民兵率＝武装1.0 と 非武装0.0 で比は 1+a）。総死者を配り直すだけ＝重みは Σ で正規化（Diplomacy の戦場の民間人死）。
        static (double ArmedRate, double HomeRate, double CivDead) 戦場の1か月(double a)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            sim.Coef.WarDeclareTension = 1e9;   // 自発的な宣戦を止める（戦線は手で1本だけ立てる）
            sim.Run(20);
            sim.Coef.SeaInvasionControlMin = 0; // 上陸ゲートを恒等値へ（この盤面が測るのは死の配分で、上陸の可否ではない＝WarThemeTests.OneFront と同じ）
            sim.Coef.MilitiaCombatValue = a;
            // ★(169) 測りの桁の守り（番人の線は変えない）：この盤面の戦場の接触は小さく（月の死亡率 約3e-8）、POP の規模の引き算の丸め（1e-16÷率）が比を 6e-9 ずらした
            //   （(169) で英の開幕の蒸気機関〔データ〕が盤面を変えた）。率を100倍にして比を丸めの外で読む＝重みの比（1+a）は率の大きさに依らない。
            sim.Coef.WarCivilianToll *= 100;
            var fr = sim.Nations[0].World; var br = sim.Nations[1].World;
            Pop? armed = null, athome = null;
            foreach (var p in fr.Regions[1].Pops)
            {
                if (p.IsSoldier) continue;
                if (p.ClassId == PopClass.Peasant) { armed = p; p.MilitiaShare = new Share(1.0); }
                else if (p.ClassId == PopClass.Worker) { athome = p; p.MilitiaShare = new Share(0.0); }
            }
            Assert.True(armed is not null && athome is not null && armed.Size > 1e-6 && athome.Size > 1e-6, "前提が崩れた：2つのPOPが揃っていない");
            var war = new War { A = fr.SelfPower!, B = br.SelfPower!, Score = -0.5 };   // A＝劣勢＝戦場になる側
            sim.Diplomacy.Wars.Add(war);
            double a0 = armed!.Size, h0 = athome!.Size;
            sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 60);
            return ((a0 - armed.Size) / a0, (h0 - athome.Size) / h0, war.CivDeadA);
        }
        var off = 戦場の1か月(0);
        var on = 戦場の1か月(0.5);
        Assert.True(off.CivDead > 0 && on.CivDead > 0, "前提が崩れた：戦場の民間人死が走らない帯にいる");
        Assert.Equal(1.0, off.ArmedRate / off.HomeRate, 9);   // a=0＝重みは全員1＝民兵率に関わらず同じ死亡率
        Assert.Equal(1.5, on.ArmedRate / on.HomeRate, 9);     // ★a=0.5＝武器を取った層は 1+0.5×1.0 倍の率で死ぬ（配り直しただけ＝Σ重みで割る）
    }

    // 建国路（MultiNationSim.SpawnNation）を実経路で踏む（民兵の札と戦力。守備兵は闘争度0.5＝半分が寝返る＝両腕とも同じ）。⚠人口・貨幣の保存は Simulation の月次 CheckInvariants が
    //   この StepQuarter の中で見張っている（建国は S/P 確定と PopulationDelta の間に1命令も入れてはいけない帯＝doc §7-3）。
    private static (double RebelSoldiers, double ParentSoldiers, double RebelMilitary, double HomeLabor) 段5_反乱国家を建てる(
        Coefficients coef, double militiaShare = 0, int quarters = 1, bool frozen = true)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = frozen };
        var pw = sim.Nations[0].World;
        Assert.True(pw.Regions.Count > 1, "前提が崩れた：親に非首都州がない");
        var home = pw.Regions[1];
        var garrison = home.Pops.Find(p => p.IsSoldier);
        Assert.True(garrison is not null && garrison.Size > 1e-9, "前提が崩れた：反乱州に守備兵がいない");
        garrison!.Militancy = 0.5;   // ⚠この後の四半期で target へ少し動く（両腕とも同じ軌道＝比較は成立する）
        if (militiaShare > 0) foreach (var p in home.Pops) if (!p.IsSoldier) p.MilitiaShare = new Share(militiaShare);
        pw.PendingRebelRegions = [home];
        pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        Assert.Contains(home, rw.Regions);
        return (rw.SoldierSizeK(), pw.SoldierSizeK(), rw.SelfPower?.Military ?? 0, home.Markets[Catalog.LaborId].LastProduced);
    }

    [Fact]
    public void 段5_民兵になった分だけ労働が畑と工場から抜ける()   // 裁定3＝生産が痩せる（既存の労働配分に乗せる＝新しい経路を作らない）
    {
        // 2腕の違いは**民兵の札だけ**（係数は両腕とも既定＝a=0 で戦力にも死の配分にも触れない＝労働だけが動く）。
        var none = 段5_反乱国家を建てる(new Coefficients(), militiaShare: 0, quarters: 2, frozen: true);
        var armed = 段5_反乱国家を建てる(new Coefficients(), militiaShare: 0.9, quarters: 2, frozen: true);   // ★(493) 0.5→0.9：軌道が動き、0.5 では州の労働の需要が供給の残りより小さく抜けが産出に現れなかった（0.5＝6248≥なし 6210・0.9＝1847）。札が供給へ届く機構は 0.9 で確かめる
        Assert.True(none.HomeLabor > 0, "前提が崩れた：反乱州で労働力が産出されていない");
        Assert.True(armed.HomeLabor < none.HomeLabor,
            $"民兵が労働から抜けていない（民兵0.9 {armed.HomeLabor:F2} ≥ 民兵0 {none.HomeLabor:F2}）＝札が労働供給へ届いていない");
    }

    // ★地方の建国は厭戦0から・開戦で両陣営を自国の POP で測る（2026-09-29 線2＝(100) の申し送り2・4）。
    //   ①厭戦の継承は首都発の建国だけ（同じ社会の首都の住民）＝地方の蜂起と分離独立の建国は0から（旧＝全ての建国が継ぎ、分離独立の戦争の月数の中央が1）。
    //   ②崩壊の線（1−RevolutionSurrenderWeight×革命圧力）は両陣営で同じ式＝読む動員を開戦の瞬間に自国の POP で測り直す（Diplomacy.DeclareCivilWar→Politics.MeasureMobilization）。
    //     旧＝反乱国は測定0（革命圧力0）・政府は出て行った首都の住民を含む前の測定のまま最初の四半期を戦った。新しい国の保持力の測定は親の値から（保持力0で満杯にしない）。
    [Fact]
    public void 地方の建国は親の厭戦を継がず_開戦で両陣営の動員は自国のPOPで測る()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
        sim.StepQuarter();
        var pw = sim.Nations[0].World;
        const double exhaustion = 0.37;
        pw.SelfPower!.WarExhaustion = exhaustion;
        var far = pw.Regions.FindAll(r => r.Id != 0);
        Assert.True(far.Count >= 2, "前提が崩れた：首都のほかに活動州が2つない");

        pw.PendingRebelRegions = [far[0]]; pw.PendingRebelPolity = Polity.Republic;   // 地方の蜂起（首都でない州の政体の建国）
        sim.SpawnSecededNations();
        Assert.Contains(far[0], sim.Nations[^1].World.Regions);
        Assert.Equal(0.0, sim.Nations[^1].World.SelfPower!.WarExhaustion);
        pw.PendingSecessionWars = [far[1]];                                            // 分離独立（州ごとの建国）
        sim.SpawnSecededNations();
        Assert.Contains(far[1], sim.Nations[^1].World.Regions);
        Assert.Equal(0.0, sim.Nations[^1].World.SelfPower!.WarExhaustion);
        Assert.Equal(exhaustion, pw.SelfPower!.WarExhaustion);                          // 親は変わらない

        // 首都の蜂起：厭戦は継ぐ（同じ社会の首都の住民）・動員は両陣営とも自国の POP で測った値（測り直しても1ビットも動かない）
        var capital = pw.Regions[0];
        double coerce0 = pw.Coercion;
        pw.PendingRebelRegions = [capital]; pw.PendingRebelPolity = Polity.Republic;
        sim.SpawnSecededNations();
        var rw = sim.Nations[^1].World;
        Assert.Contains(capital, rw.Regions);
        Assert.Equal(exhaustion, rw.SelfPower!.WarExhaustion);
        Assert.True(rw.ReformistPower + rw.RadicalPower > 0, "反乱国の動員が測られていない（旧＝最初の四半期は0＝革命圧力0）");
        Assert.Equal(coerce0, rw.Coercion);                                             // 保持力の測定は親の値から
        Assert.True(Politics.RevolutionPressure(rw) > 0, "反乱国の革命圧力が0＝崩壊の線が下がらない");
        foreach (var w in new[] { pw, rw })
        {
            double r0 = w.ReformistPower, d0 = w.RadicalPower;
            Politics.MeasureMobilization(w, w.Coef);
            Assert.Equal(r0, w.ReformistPower);   // 蓄えた動員＝自国の今の POP で測った値（政府は出て行った首都の住民を含まない）
            Assert.Equal(d0, w.RadicalPower);
        }
    }

    // ★段5＝守備兵の寝返りと武器庫（2026-09-29 処理の一元化 段3＝点火・旧係数 SoldierDefectionByMilitancy を1で焼き込んで削除）
    //   ＋忠誠の兵と武器庫は政府について行く・建国した国は親の厭戦を継ぐ（2026-09-29 首都の分離独立の続き）。
    //   首都の分離独立（首都州が反乱国へ・政府は残る州へ遷都）で、首都の守備兵（兵士POP）は**闘争度の割合**だけ寝返り、
    //   残りは**政府の遷都先**（選び方の1本 Territory.NextCapital＝段2 の遷都と同じ州）の兵士POPへ移る。首都の武器庫（Region.ArmsStock＝全国軍の耐久装備）も
    //   **同じ割合**で割れ、寝返らなかった分は遷都先の武器庫へ（旧＝親の最初の残る州＝遷都先と違う州になり、調達が首都の1州だけなので移した武器庫は読まれなかった）。
    //   遷都先の武器庫は開いている（寝返りが全員でも＝未初期化の財を目標と読まない）。反乱国の厭戦＝親の厭戦（旧＝0から）。
    //   建国路の1段（MultiNationSim.SpawnSecededNations）だけを踏む＝四半期の他の流れを挟まない＝闘争度が動かない＝割合を厳密に測れる。
    //   保存＝全ての国の人口の和と、全ての州の武器庫の和が1段の前後で変わらない（財も人も湧かず消えず、移るだけ）。
    [Theory]
    [InlineData(0.0)]   // 誰も寝返らない＝兵も武器庫も全部が政府に残る
    [InlineData(0.5)]   // 半分ずつ
    [InlineData(1.0)]   // 全員が寝返る＝兵も武器庫も全部が反乱国へ
    public void 段5_首都の守備兵と武器庫は闘争度の割合で寝返り残りは政府の遷都先へ_反乱国は親の厭戦を継ぐ(double militancy)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
        sim.StepQuarter();   // 首都の武器庫が立つ（軍需の調達は首都の1州）
        var pw = sim.Nations[0].World;
        var capital = pw.Regions[0];
        var garrison = capital.Pops.Find(p => p.IsSoldier);
        Assert.True(garrison is not null && garrison.Size > 1e-9, "前提が崩れた：首都に守備兵がいない");
        Assert.True(capital.ArmsStock is { Count: > 0 }, "前提が崩れた：首都に武器庫がない");
        var seat = Territory.NextCapital(pw, [capital]);
        Assert.True(seat is not null, "前提が崩れた：首都を出た後に政府の移れる州がない");
        var first = pw.Regions.Find(r => !ReferenceEquals(r, capital));
        Assert.False(ReferenceEquals(first, seat), "前提が崩れた：遷都先が親の最初の残る州と同じ＝旧の行き先と区別できない");
        var seatSoldier = seat!.Pops.Find(p => p.IsSoldier);
        Assert.True(seatSoldier is not null, "前提が崩れた：遷都先に兵士POPの器がない");
        garrison!.Militancy = militancy;
        const double exhaustion = 0.37;
        pw.SelfPower!.WarExhaustion = exhaustion;
        double g0 = garrison.Size, l0 = seatSoldier!.Size;
        var arms0 = new Dictionary<int, double>(capital.ArmsStock!);
        var seatArms0 = seat.ArmsStock is { } sa ? new Dictionary<int, double>(sa) : new Dictionary<int, double>();
        double armsTotal0 = 0, pop0 = 0;
        foreach (var n in sim.Nations) { pop0 += n.World.TotalPopulation; foreach (var r in n.World.Regions) if (r.ArmsStock is { } a) foreach (var kv in a) armsTotal0 += kv.Value; }
        Assert.True(armsTotal0 > 0, "前提が崩れた：武器庫が空");

        pw.PendingRebelRegions = [capital];
        pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.SpawnSecededNations();
        Assert.True(sim.Nations.Count == n0 + 1, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        Assert.Contains(capital, rw.Regions);
        Assert.DoesNotContain(capital, pw.Regions);
        Assert.Same(seat, pw.Regions[0]);   // 政府は選び方の1本が先に決めた州へ遷都した＝忠誠の兵と武器庫の行き先と同じ州

        // 厭戦：建国した国は親の厭戦を継ぐ（親は変わらない）
        Assert.Equal(exhaustion, rw.SelfPower!.WarExhaustion);
        Assert.Equal(exhaustion, pw.SelfPower!.WarExhaustion);
        // 兵：寝返った割合＝闘争度・残りは遷都先の兵士POPへ
        Assert.Equal(g0 * militancy, garrison.Size, 9);
        Assert.Equal(l0 + g0 * (1.0 - militancy), seatSoldier.Size, 9);
        // 武器庫：同じ割合で割れ、寝返らなかった分は遷都先の武器庫へ・遷都先の武器庫は開いている（全員が寝返っても）
        Assert.True(seat.ArmsStock is not null, "遷都先の武器庫が開いていない＝未初期化の財が目標と読まれる");
        foreach (var (g, a0) in arms0)
        {
            Assert.Equal(a0 * militancy, capital.ArmsStock!.GetValueOrDefault(g), 9);
            Assert.Equal(seatArms0.GetValueOrDefault(g) + a0 * (1.0 - militancy), seat.ArmsStock!.GetValueOrDefault(g), 9);
        }
        // 保存：人も財も湧かず消えない
        double armsTotal1 = 0, pop1 = 0;
        foreach (var n in sim.Nations) { pop1 += n.World.TotalPopulation; foreach (var r in n.World.Regions) if (r.ArmsStock is { } a) foreach (var kv in a) armsTotal1 += kv.Value; }
        Assert.True(Math.Abs(armsTotal1 - armsTotal0) <= 1e-12 * armsTotal0, $"武器庫の和が動いた（{armsTotal0:R} → {armsTotal1:R}）");
        Assert.True(Math.Abs(pop1 - pop0) <= 1e-12 * pop0, $"人口の和が動いた（{pop0:R} → {pop1:R}）");
    }

    // ★武器庫は政府について行き、新しい首都に財は湧かない（2026-09-29 首都の分離独立の続き）＝「未初期化は目標」は一度も開いていない1783年の開幕の武器庫だけ
    //   （EconomyStep.ArmsHeld）。遷都（World.SetCapital）と建国（MultiNationSim.SpawnNation）の新しい首都は開いた空の武器庫で来る。
    //   ①遷都（割譲で首都州を失う＝旧首都の武器庫は州と一緒に去った）：遷都先の武器庫は開いた空。1四半期の調達の後の小銃（耐久品・月の摩耗 2%）の在庫は
    //     兵1人あたりで、首都を失わなかった双子（同じ種・開幕の武器庫を目標から持つ首都）の2割に届かない（1四半期の納入＝目標の約6%）。
    //     旧＝最初の月に目標の満額が湧き、双子と同じ水準に並んだ。開幕の武器庫が目標から始まらなければ双子も納入だけ＝比が2割を超える（開幕の規則の番人も兼ねる）。
    //   ②国家形成型の遷都（旧首都がまだ国に居る）：旧首都の武器庫は新首都へ移る（財ごとに和が不変・旧首都は空）。
    //   ③建国（分離独立の州）：新しい国の首都の武器庫は開いた空。
    [Fact]
    public void 遷都と建国の新しい首都では未初期化の武器庫が湧かず旧首都の武器庫は政府について行く()
    {
        static (MultiNationSim sim, World br) Make()
        {
            var s = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789) { DiplomacyFrozen = true };
            s.StepQuarter();   // 開幕の武器庫が立つ（首都の1州）
            return (s, s.Nations[0].World);
        }
        // ①
        var (simA, brA) = Make();
        var (simB, brB) = Make();
        var c = simA.Coef;
        int rifle = c.MilitaryGoods[0];
        Assert.True(c.MilitaryWearRatesPerMonth[0] < 1.0, "前提が崩れた：小銃が耐久品でない");
        Territory.TransferRegion(brB, simB.Nations[1].World, brB.Regions[0], Territory.Rehoming.Ceded);
        var seatB = brB.Regions[0];
        Assert.True(seatB.ArmsStock is { Count: 0 }, "遷都先の武器庫が開いた空で来ていない");
        simA.StepQuarter();
        simB.StepQuarter();
        Assert.Same(seatB, brB.Regions[0]);
        double perA = brA.Regions[0].ArmsStock!.GetValueOrDefault(rifle) / brA.SoldierSizeK();
        double perB = seatB.ArmsStock!.GetValueOrDefault(rifle) / brB.SoldierSizeK();
        Assert.True(perA > 0, $"前提が崩れた：双子の首都に小銃がない（{perA:G6}）");
        Assert.True(perB < 0.2 * perA, $"遷都先に武器庫が湧いたか、開幕の武器庫が目標から始まっていない（兵1人あたりの小銃 遷都先 {perB:G6}・双子の首都 {perA:G6}＝比 {perB / perA:F3} ≧ 0.2）");

        // ②
        var (simC, brC) = Make();
        var capC = brC.Regions[0];
        var toC = brC.Regions.Find(r => r.Id != 0)!;
        var armsCap0 = new Dictionary<int, double>(capC.ArmsStock!);
        var armsTo0 = toC.ArmsStock is { } t0 ? new Dictionary<int, double>(t0) : new Dictionary<int, double>();
        Assert.True(armsCap0.Count > 0, "前提が崩れた：首都に武器庫がない");
        brC.SetCapital(toC);
        Assert.Same(toC, brC.Regions[0]);
        foreach (var (g, a0) in armsCap0) Assert.Equal(armsTo0.GetValueOrDefault(g) + a0, toC.ArmsStock!.GetValueOrDefault(g), 12);
        Assert.True(capC.ArmsStock is { Count: 0 }, "旧首都に武器庫が残った＝政府について行かない");

        // ③
        var (simD, brD) = Make();
        var sec = brD.Regions.Find(r => r.Id != 0 && r.ArmsStock is null);
        Assert.True(sec is not null, "前提が崩れた：武器庫の開いていない非首都州がない");
        brD.PendingSecessionWars = [sec!];
        int nD = simD.Nations.Count;
        simD.SpawnSecededNations();
        Assert.True(simD.Nations.Count == nD + 1, "前提が崩れた：分離独立の国が建っていない");
        Assert.Same(sec, simD.Nations[^1].World.Regions[0]);
        Assert.True(sec!.ArmsStock is { Count: 0 }, "建国の首都の武器庫が開いた空で来ていない");
    }

    [Fact]
    public void 段5_民兵は兵数かける係数だけ戦力になる()   // (iv) 係数a
    {
        var off = 段5_反乱国家を建てる(new Coefficients(), militiaShare: 0.3, quarters: 2, frozen: false);
        var on = 段5_反乱国家を建てる(new Coefficients { MilitiaCombatValue = 0.5 }, militiaShare: 0.3, quarters: 2, frozen: false);
        Assert.True(off.RebelMilitary > 0, "前提が崩れた：反乱国の戦力が算出されていない（外交の同期が回っていない）");
        Assert.True(on.RebelMilitary > off.RebelMilitary,
            $"民兵が戦力に入っていない（a=0.5 {on.RebelMilitary:F2} ≤ a=0 {off.RebelMilitary:F2}）＝札が戦争システムへ届いていない");
    }

    // ============================================================================================
    // ★段6＝クールダウンの畳み込み（F3b蜂起doc §6・2026-09-08）＝**2026-09-29 点火**（作者裁定「蜂起の共有クールダウンは撤去＝根拠のない待ち」）。
    //   旧＝RevoltCooldownQuarters（4四半期の固定タイマー）で「同一国で年に何度も燃えない」を作っていた＝物理でない待ち行列
    //   （旗 RevoltCooldownRetire・係数 RegionalRevoltCooldown ごと削除）。F1 が燃えるたびに F3b の選定も4四半期止まっていた。
    //   残るのは (a) 決着で結束0→ロジスティック再成長（4〜16年） (b)「その運動はもう野に出ている」という状態の排他。
    //   物差しは段0と同じ通知の札（World.PendingFactionCrisis）と、F1 の一揆の記録（鎮圧でも譲歩でも1行＝分岐非依存）。
    // ============================================================================================
    private static (World w, FactionState f) 段6_危機の寸前を作る()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        var f = 巨大な運動を差し込む(w, w.Regions[1]);
        w.PendingLog.Clear();   // 助走の記録を外す＝以後の一揆の行はこの検査の四半期のものだけ
        return (w, f);
    }

    /// <summary>1州だけを飢えさせて熱を門の上に置く（他の州は平穏）＝F1 が必ずその州で燃える盤面。</summary>
    private static Region 段6_飢えた州を作る(World w, int idx)
    {
        var hungry = w.Regions[idx];
        foreach (var r in w.Regions) { r.FoodFill = 1.0; foreach (var p in r.Pops) { p.Militancy = 0; p.Satisfaction = 1.0; } }
        hungry.FoodFill = 0.0;
        foreach (var p in hungry.Pops) { p.Militancy = 1.0; p.Satisfaction = 0.0; }
        Assert.True(Politics.RegionRevoltHeat(hungry, w.Coef) > w.Coef.RegionalRevoltThreshold, "前提が崩れた：飢えた州の熱が閾値を超えていない");
        return hungry;
    }

    [Fact]
    public void 段6_熱が門の上にある州は翌四半期も一揆を起こす_待ちで数えない()
    {
        var (w, _) = 段6_危機の寸前を作る();
        w.Factions.Clear();              // F3bを外し、F1だけを見る
        w.Coef.MilitancySpeed = 0;       // 盤面を凍結＝熱は2四半期とも門の上（測るのは待ちの有無だけ）
        var hungry = 段6_飢えた州を作る(w, 1);
        Politics.Step(w, w.Coef);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆") && l.Contains(hungry.Name));   // 対照＝1四半期目に燃えた
        Assert.Null(w.PendingRebelRegions);   // 前提＝首都の蜂起で F1 の判定が飛ばされていない
        w.PendingLog.Clear();
        Politics.Step(w, w.Coef);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆") && l.Contains(hungry.Name));   // 旧＝共有のタイマーが4四半期閉じていた
    }

    [Fact]
    public void 段6_一揆が燃えた翌四半期も組織運動の選定は止まらない()
    {
        // 旧＝F1 が燃えると共有のタイマーが4四半期閉じ、F3b の選定ループに入れなかった（結束のある運動の86四半期のうち60四半期）。
        var (w, f) = 段6_危機の寸前を作る();
        w.Coef.MilitancySpeed = 0;
        foreach (var r in w.Regions) if (r != f.Region) foreach (var p in r.Pops) p.Militancy = 0;
        foreach (var p in f.Region.Pops) p.Militancy = 1.0;   // 運動の本拠が門の上＝同じ四半期に F1 も燃える
        Politics.Step(w, w.Coef);
        Assert.Same(f, w.PendingFactionCrisis);                    // 1四半期目＝通知（誰も法の目標を置かない＝NullAgent）
        Assert.Contains(w.PendingLog, l => l.Contains("一揆"));   // 同じ四半期に F1 が燃えた（旧＝ここで共有のタイマーが閉じた）
        Assert.Equal(0, w.FactionRevolts);
        Politics.Step(w, w.Coef);
        Assert.Equal(1, w.FactionRevolts);   // 評価の四半期＝要求した法が1段も動かなかった＝蜂起（旧＝タイマーで選定に入れなかった）
    }

    [Fact]
    public void 段6_既に蜂起が野に出ていれば次の旗は立たない()
    {
        var (w, _) = 段6_危機の寸前を作る();
        w.PendingRebelRegions = [w.Regions[1]];   // 既に別の運動が州に拠って立った（建国＝開戦の直前）
        Politics.Step(w, w.Coef);
        Assert.Null(w.PendingFactionCrisis);   // 時間ではなく**状態**で排他されている
    }

    [Fact]
    public void 段6_蜂起を予約した州はF1の走査から外れる()
    {
        // 対照：飢えた州を1つ作る＝F1（州一揆）の走査がその州を選ぶ。
        var (w, _) = 段6_危機の寸前を作る();
        w.Factions.Clear();   // F3bを外し、F1だけを見る（同じ四半期に両方走るため）
        var hungry = 段6_飢えた州を作る(w, 1);
        Assert.Same(hungry, Politics.SelectRevoltRegion(w, w.Coef).Region);

        // 本体：同じ州が蜂起を予約している（＝同じ四半期に組織運動がその州に拠って立った）なら、F1 の走査はその州を選ばない。
        //   ⚠Step で見ない理由＝四半期の頭に予約がある盤面は「首都の蜂起の四半期」として F1 の判定そのものが飛ぶ（Politics.Step の capitalRose）
        //   ＝排他が効くのは F3b が同じ四半期の中で予約を立てた後の F1 だけ＝走査の関数（エンジンと計器が共有する1本）を直接見る。
        w.PendingRebelRegions = [hungry];
        Assert.NotSame(hungry, Politics.SelectRevoltRegion(w, w.Coef).Region);

        // 対照の続き：予約を下ろせば同じ州が門を越えて燃える（熱が門の上にある盤面であることの確認）。
        w.PendingRebelRegions = null;
        Politics.Step(w, w.Coef);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆") && l.Contains(hungry.Name));
    }

    // ============================================================================================
    // ★内戦タグの講和の3枝（膠着＝分断固定／分離戦で反乱勝ち＝独立確定／それ以外＝再併合予約）は**割譲の手前で return** する＝州は1つも動かない。
    //   （反乱の膨張の器 RevoltExpansionScore は 2026-10-18 に削除した＝名前の復活は DormantIdentityTests.RetiredFields が禁じる。）
    //   ⚠講和は StepWars の中でしか呼ばれず、そこへ届くには押し合いと運が戦勝点を動かす＝**戦勝点を手で置いて
    //     ResolvePeace を直接踏む**（internal＝ON経路テスト用・TreatyPortLapse と同じ流儀）。機構の検査であって創発の主張ではない。
    // ============================================================================================
    private static (MultiNationSim Sim, World PW, World RW, War War) 段7_内戦を1本立てる(Coefficients coef)
    {
        // DiplomacyFrozen＝Diplomacy.Step を1度も呼ばない＝押し合いも運も走らず戦勝点は手で置いた値のまま（決定論）。
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: coef) { DiplomacyFrozen = true };
        var pw = sim.Nations[0].World;
        Assert.True(pw.Regions.Count > 3, "前提が崩れた：親に非首都州が3つ以上ない＝2州ぶんの膨張の余地がない");
        pw.PendingRebelRegions = [pw.Regions[1]];
        pw.PendingRebelPolity = Polity.Republic;
        int n0 = sim.Nations.Count;
        sim.StepQuarter();   // 建国路（SpawnNation）＋内戦タグ付き開戦
        Assert.True(sim.Nations.Count > n0, "前提が崩れた：反乱国家が建っていない");
        var rw = sim.Nations[^1].World;
        var war = sim.Diplomacy.Wars.Find(x => x.CivilWarParent == pw.SelfPower);
        Assert.True(war is not null && war.A == pw.SelfPower,
                    "前提が崩れた：内戦タグの戦争が親を A 側にして立っていない＝戦勝点の符号の向きが変わる");
        return (sim, pw, rw, war!);
    }

    // (i) 0で講和の3枝が現行のまま＝割譲を1つも起こさない（新経路の命令が1つも走らない）。
    //     ⚠**長距離のビット恒等はこの番人では示せない**（8四半期の DormantIdentityTests と同じ限界）＝savetest 相対差0が担当。
    [Fact]
    public void 段7対照_既定では内戦の講和の3枝が州を1つも動かさない()
    {
        // 3枝＝膠着(分断固定)／分離戦で反乱勝ち(独立確定)／それ以外(再併合予約)。最後は反乱勝ち・親勝ちの両向きを踏む。
        foreach (var (score, secession, rump, reint) in
                 new[] { (-0.20, true, true, false), (-0.60, true, false, false), (-0.60, false, false, true), (0.60, false, false, true) })   // ★2026-09-29 線2：膠着の枝は分離独立の白紙（政体の内戦の膠着は勝者の枝へ）
        {
            var (sim, pw, rw, war) = 段7_内戦を1本立てる(new Coefficients());
            int pn = pw.Regions.Count, rn = rw.Regions.Count;
            double pPop = pw.TotalPopulation, rPop = rw.TotalPopulation, pDelta = pw.PopulationDelta, pFlow = pw.CumulativeTradeFlow.Value;
            war.Score = score; war.CivilWarSecession = secession;
            sim.Diplomacy.ResolvePeace(sim.Coef, war);
            Assert.Equal(pn, pw.Regions.Count);           // 割譲の手前で return＝州は1つも動かない
            Assert.Equal(rn, rw.Regions.Count);
            Assert.Equal(pPop, pw.TotalPopulation);       // 人も金も台帳も1ビットも動かない
            Assert.Equal(rPop, rw.TotalPopulation);
            Assert.Equal(pDelta, pw.PopulationDelta);
            Assert.Equal(pFlow, pw.CumulativeTradeFlow.Value);
            // 枝そのものは現行どおり成立している（＝「何も起きない」ではなく「割譲だけが無い」ことの確認）
            Assert.Equal(rump, rw.RumpParentName.Length > 0);
            Assert.Equal(reint, sim.Diplomacy.PendingReintegrations.Count > 0);
        }
    }

    // ============================================================================================
    // ★武器の市場購入（設計メモ §7 作者裁定2026-09-09 の未実装3件(a)・実装2026-09-11・休眠 RebelArmsMarketEnabled=0）。
    //   反乱勢力が参加POPの貯蓄で国軍と同じ財を同じ市場で買う／法「民間の武器購入」（禁止＝Contraband.LeakFraction だけ漏れる）／FactionStrength の第5因子＝装備。
    //   ⚠既定が0であること自体は DormantIdentityTests の DormantZeroFields が唯一の宣言（ここで二重に書かない）。
    //   ⚠単国世界は MilitaryScale=0（軍需調達は多国世界の首都だけ＝EquipDiscoveryTests の注記）＝軍需財の需要は反乱勢力だけ＝差分が全部この機構のもの。
    // ============================================================================================
    /// <summary>単国の英（開幕から20四半期）の首都州に運動を1本立て、最大の大衆POPを唯一の参加者（闘争度0.9・貯蓄大＝予算は縛らない）にする。
    /// 束と主軸は FactionSnapshot の同じ1本で導く（テストが参加の述語を再実装しない）。他POPの闘争度は0＝参加者は1人＝払い手の帳簿が読める。</summary>
    private static (Simulation Sim, FactionState F, Pop Payer) 武装する運動(double enabled, Action<Coefficients>? tune = null)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.World.Coef.TechThresholdOff = 0;   // ★(613) 20 四半期の前に置く（後で置いても盤面は新しい既定で 20 四半期を経ている）
        sim.Run(20);
        var w = sim.World; var c = w.Coef;
        // ★(613) この番人の前提（20 四半期の英の盤面・運動の鍵が通しで同じ器）を保つ＝旗 TechThresholdOff〔既定 1・制度と思想の行の技術の比〕の旧の枝。新しい既定では 1 年買い続けた武器庫が減る盤面になる（機構は動いている・前提の器が違うだけ＝(542) の作法）
        c.RebelArmsMarketEnabled = enabled;
        tune?.Invoke(c);
        var r = w.Region;
        Pop? payer = null;
        foreach (var p in r.Pops)
        {
            p.Militancy = 0;
            if (!p.Closed && !p.IsModernElite && !p.IsSoldier && (payer is null || p.Size > payer.Size)) payer = p;
        }
        Assert.True(payer is not null && payer.Size > 1e-9, "前提が崩れた：首都州に大衆POPが無い");
        payer!.Militancy = 0.9;
        // 参加者の財布を厚くする＝**州の他POPの貯蓄の半分を移す**（金を湧かせない＝貨幣保存の番人が黙ったまま通しで走れる）
        double pool = 0;
        foreach (var p in r.Pops) if (!ReferenceEquals(p, payer) && p.Savings.Value > 0) { pool += p.Savings.Value * 0.5; p.SetSavings(p.Savings.Value * (0.5)); }
        payer.SetSavings(payer.Savings.Value + (pool));
        Assert.True(payer.Savings.Value > 1e3, $"前提が崩れた：参加者の財布が薄い（{payer.Savings:F1}）");
        var (bloc, ideo) = FactionSnapshot.BlocIdeo(payer);
        var f = new FactionState { Region = r, Bloc = bloc, Ideo = ideo, Size = payer.Size, AvgMilitancy = 0.9, Cohesion = 1.0 };
        w.Factions.Add(f);
        return (sim, f, payer);
    }

    [Fact]
    public void 武器購入_係数0では1回も買わず法も第5因子も動かない()   // (a) 恒等
    {
        var (sim, f, _) = 武装する運動(enabled: 0);
        var w = sim.World; var c = w.Coef;
        EconomyStep.Run(w.Region, c, 1.0, null, w);
        Assert.Equal(0.0, w.RebelArmsSpent);   // 市場取引が1回も起きない
        Assert.Null(f.ArmsStock);              // 武器庫は1件も作られない
        Assert.Equal(1.0, f.EquipFill);        // 書き手が走らない
        // 第5因子は掛からない＝EquipFill をどう振っても4因子の積そのもの（ビット恒等）。大衆は男子普選前＝発言権0＝(1−0)=1.0
        f.EquipFill = 0.25;
        Assert.True(w.Suffrage < Suffrage.AdultMale, "前提が崩れた：この時点で男子普選に達している");
        Assert.Equal(f.Size * f.AvgMilitancy * f.Cohesion * 1.0 * c.FactionRevoltScale, Politics.FactionStrength(w, c, f));
        // 法は選好に現れない（＝綱領・要求・戦線のどこにも載らない）し、目標を置いても施行経路が動かない
        var (trad, modern, mass, net) = PolicyPreferences.Stance(w, PolicyAxis.CivilianArms, increasing: true);
        Assert.True(trad == 0 && modern == 0 && mass == 0 && net == 0, $"休眠中に民間の武器購入の法が選好に現れた（{trad}/{modern}/{mass}）");
        w.Policy.CivilianArmsBan = true;
        Government.Step(w, c);
        Assert.False(w.CivilianArmsBan);
    }

    [Fact]
    public void 武器購入_点火すると資金が参加者の貯蓄から減り売り手に届く()   // (b) 金の保存
    {
        const int rifle = Catalog.RifleId;
        var (sim, f, payer) = 武装する運動(enabled: 1);
        var w = sim.World; var c = w.Coef; var r = w.Region;
        double s0 = r.SumSavings().Value;
        EconomyStep.Run(r, c, 1.0, null, w);
        double s1 = r.SumSavings().Value;
        Assert.True(w.RebelArmsSpent > 0, "武器が1単位も買えていない");
        // 州の帳簿：Σ貯蓄の増減 ＝ 国家の支払い（国庫→売り手＝外からの流入）− 専売（月末に国庫へ）。
        // 反乱勢力の支払いは参加者→売り手＝州の**内側**で閉じる＝この恒等式に新しい項を足さない（足したら金が湧いたか消えたかのどちらか）。
        double external = r.StateSpent.Sum() + r.StateSpent[(int)TreasuryCat.StateMonopoly];   // 旧 StateSpent−LastStateMonopolyRevenue と同じ量（専売益は区分 StateMonopoly に負で積まれる・2026-09-28）
        Assert.True(Math.Abs((s1 - s0) - external) <= 1e-6 * Math.Max(1.0, Math.Abs(s0)), $"貨幣が湧いたか消えた：Δ貯蓄 {s1 - s0:F6} ≠ 外部 {external:F6}");
        Assert.True(payer.PaidPerMonth.Value >= w.RebelArmsSpent * (1 - 1e-9), $"参加者の支払い {payer.PaidPerMonth:F3} に武器代 {w.RebelArmsSpent:F3} が載っていない");
        Assert.True(r.Markets[rifle].LastSold > 0, "小銃が売れていない（売り手に届いていない）");
        Assert.NotNull(f.ArmsStock);
        Assert.True(f.ArmsStock![rifle] > 0, "武器庫に小銃が積まれていない");
        Assert.True(f.EquipFill > 0 && f.EquipFill < 1, $"装備充足 {f.EquipFill:F3} が (0,1) に無い＝新しい運動は丸腰から数年かけて武装するはず");
        // 第5因子が掛かる（国軍の即応度×装備充足と同じ物差し）
        Assert.True(w.Suffrage < Suffrage.AdultMale);
        Assert.Equal(f.Size * f.AvgMilitancy * f.Cohesion * 1.0 * c.FactionRevoltScale * f.EquipFill, Politics.FactionStrength(w, c, f), 12);

        // 通しで1年走らせても貨幣保存の番人（Simulation の月次 CheckInvariants）が黙っている＋買い続けた武器庫が育つ（別の世界＝直接 Run の帳簿と混ぜない）
        var (sim2, f2, payer2) = 武装する運動(enabled: 1);
        // ★「買い続けた」の前提を明示で保つ（2026-09-22）：参加の2条件＝闘争度＞MilitancyThreshold と運動の鍵（束・主軸）を毎四半期そのままにする。
        //   旧は盤面任せだった＝闘争度は σ=0.3/四半期で減衰して2四半期目に参加の門0.5を割り（旧でも新でも2四半期目以降は1回も買っていない）、
        //   階層3分割の後の世界ではさらに参加者のイデオロギーの argmax が1四半期目に隣の主軸へ移って f2 の鍵から外れた
        //   （買い物は同じ州に自然に立った同じ束の運動へ付き、その武器庫は1年で 40→193 と育っていた＝機構は動いていて、外れていたのは器の同一性だけ）。
        var shares0 = (double[])payer2.IdeologyShares.Clone();
        void HoldMembership() { payer2.Militancy = 0.9; System.Array.Copy(shares0, payer2.IdeologyShares, shares0.Length); }
        HoldMembership();
        sim2.Run(1);
        Assert.NotNull(f2.ArmsStock);
        double stock1 = f2.ArmsStock![rifle];
        Assert.True(stock1 > 0);
        for (int q = 0; q < 4; q++) { HoldMembership(); sim2.Run(1); }
        Assert.True(f2.ArmsStock[rifle] > stock1, $"1年買い続けても武器庫が増えない（{stock1:F3} → {f2.ArmsStock[rifle]:F3}）");
        Assert.True(sim2.World.RebelArmsSpent > 0);
    }

    [Fact]
    public void 武器購入_禁止法は行政充足の分だけ止め_取り締まれなければ漏れる()   // (c) 密輸＝Contraband.LeakFraction そのもの
    {
        double Spent(bool ban, double adminFill, double smugglingMax)
        {
            var (sim, _, _) = 武装する運動(enabled: 1, tune: c => c.SmugglingMax = new(smugglingMax));
            var w = sim.World; var r = w.Region;
            w.CivilianArmsBan = ban;
            r.AdminFill = adminFill;   // 漏れは前月の行政充足を読む（Run の末尾で上書きされる前）。⚠専売の徴収漏れも同じ値を読むので比べる腕どうしは揃える
            EconomyStep.Run(r, w.Coef, 1.0, null, w);
            return w.RebelArmsSpent;
        }
        double allowed = Spent(ban: false, adminFill: 0.0, smugglingMax: 0.5);
        Assert.True(allowed > 0, "前提が崩れた：許可でも買えていない");
        Assert.Equal(0.0, Spent(ban: true, adminFill: 1.0, smugglingMax: 0.5));       // 禁止＋行政が完全＝1単位も買えない
        Assert.Equal(allowed, Spent(ban: true, adminFill: 0.0, smugglingMax: 1.0));   // 禁止＋行政が無力（漏れ上限1.0）＝許可と同量（×1.0＝ビット恒等）
        double leaked = Spent(ban: true, adminFill: 0.0, smugglingMax: 0.5);           // 既定の漏れ上限0.5＝取り締まれない州でも半分しか通らない（禁令＝完全遮断ではない）
        Assert.True(leaked > 0 && leaked < allowed, $"禁止＋行政無力の漏れ {leaked:F3} が (0, 許可 {allowed:F3}) に無い");
    }

    [Fact]
    public void 武器購入_未発見の武器は買えない()   // (d) 必要量は国軍と同じ DiscoveredRates を通る
    {
        // 汽船85＝発見の表の1820年代（既定のレシピの行・EquipDiscoveryTests と同じ前提）。艦隊財ではない（FleetGoodIds＝戦闘艦）
        // ＝陸海の規則で消えるのではなく**発見**で消えることを見る。
        // ★双子（2026-09-27 回2-1＝旗 EquipNeedsDiscovery を点火して削除したので OFF の腕は無い）：B だけ汽船の原単位を**素の表で0**にする（「要らない」を表で言う）。
        //   運動の購入が発見の表を通っていれば、A の汽船は既に0＝A と B の決済は同じ（汽船の需要も武器庫も充足もビット一致）。素の表を読んでいれば A だけ汽船を買う。
        int ship = Catalog.SteamshipId; const int rifle = Catalog.RifleId;
        var (simA, fA, _) = 武装する運動(enabled: 1);
        var (simB, fB, _) = 武装する運動(enabled: 1, tune: c =>
        {
            var z = (double[])c.MilitaryRates.Clone();
            z[Array.IndexOf(c.MilitaryGoods, ship)] = 0.0;
            c.MilitaryRates = z;
        });
        foreach (var s in new[] { simA, simB })
        {
            Assert.False(Discovery.Known(s.World.Discovered, Catalog.DiscoveryRowOf(ship, "")), "前提が崩れた：汽船が発見済み");
            Assert.True(Array.IndexOf(s.World.Coef.FleetGoodIds, ship) < 0, "前提が崩れた：汽船が艦隊財に数えられている");
            EconomyStep.Run(s.World.Region, s.World.Coef, 1.0, null, s.World);
        }
        Assert.Equal(0.0, Discovery.GoodGate(simA.World.Region, ship));   // 1783年＝汽船は未発見
        Assert.NotNull(fA.ArmsStock);
        Assert.Equal(0.0, fA.ArmsStock![ship]);      // 未発見の汽船は1単位も買わない（必要量0＝発注0）
        Assert.True(fA.ArmsStock[rifle] > 0);       // 発見済みの小銃は買う
        Assert.Equal(simB.World.Region.Markets[ship].LastDemand, simA.World.Region.Markets[ship].LastDemand);
        Assert.Equal(fB.EquipFill, fA.EquipFill);
    }


    // ============================================================================================
    // ★項目2(b)＋項目3（2026-09-11・設計メモ §7 作者裁定2026-09-09 の未実装分・計器 fp300／smoke60 の知見・同日点火＝係数は削除）。
    //   2(b)＝F1鎮圧枝は闘争度を緩和しない（弾圧は鎮めない・譲歩が鎮める＝施行の時点）。
    //   3＝F1の州選択の鍵は 熱×群衆（旧の「熱が最大の州」は英・露で群衆9.5人／85人の植民地前哨を毎年選んでいた）。
    //   ここは機構の検査＝盤面を凍結（σ=0）して1四半期だけ踏む。選ばれた州はログの州名で読む（緩和の有無に依らない物差し）。
    // ============================================================================================
    private static World 項目3_盤面(double microHeat, double bigHeat, double strengthScale = -1)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        // 初期の国債を構築の直後に消す＝旧の盤面と同じ状態から回す（理由は 段4_絶望した州に小さな運動を置く と同じ＝緊縮の門が法の経路の前提を塞ぐ）
        sim.World.GovDebt = new MoneyStock(0); sim.World.ForeignDebt = new MoneyStock(0);
        foreach (var r in sim.World.Regions) foreach (var p in r.Pops) p.BondHoldings = new MoneyStock(0);
        sim.Run(20);
        var w = sim.World; var c = w.Coef;
        Assert.True(w.Regions.Count > 2, "前提が崩れた：非首都州が2つ以上ない");
        if (strengthScale > 0) c.RegionalRevoltStrength = strengthScale;
        c.MilitancySpeed = 0;   // 盤面を凍結＝闘争度は F1 の枝でしか動かない（測るのは選択と緩和だけ）
        w.Factions.Clear(); w.PendingRebelRegions = null;
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Militancy = 0;
        var micro = w.Regions[1]; var big = w.Regions[2];
        // 微小州＝群衆を合計10人に潰す（フォートシンプソン／チャーチルの写し）。大州＝群衆はそのまま（数千）。熱は閉鎖階級・兵士も同じ値に置く＝州平均がその値そのもの
        double crowdMicro = Politics.RegionCrowd(micro);
        Assert.True(crowdMicro > 1e-9, "前提が崩れた：微小州にする州に大衆がいない");
        foreach (var p in micro.Pops) if (!p.Closed && !p.IsSoldier) p.Size *= 10.0 / crowdMicro;
        foreach (var p in micro.Pops) p.Militancy = microHeat;
        foreach (var p in big.Pops) p.Militancy = bigHeat;
        Assert.True(Politics.RegionCrowd(big) > 100 * Politics.RegionCrowd(micro), "前提が崩れた：大州の群衆が微小州の100倍に届かない");
        return w;
    }

    /// <summary>★幽霊の国の根（2026-09-30 (118)）：離脱の力は州の大衆の人の数で測る＝圧力×N/(N+N0)（N0＝植民地の州昇格の人口 ColonyPromotionMinPop）。
    /// 同じ盤面を3つ＝X（大衆そのまま）・Y（大衆の99%を首都へ移す）・Z（1人だけ残す）。国の総人口・闘争度・関税の利害・民族・意識・引き留めは3つとも同じ
    /// ＝支配度の1歩の差は圧力の差だけ。1人あたりの圧力は同じなので、圧力の比は人の数の飽和の比そのもの。⚠突然変異「人の数を掛けない」「基準人口を取り違える」で赤。</summary>
    [Fact]
    public void 幽霊の国の根_離脱の力は州の大衆の人の数で測る()
    {
        (World W, Region R) Board(double keep)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World; var c = w.Coef;
            var r = w.Regions.Find(x => x.Id != 0 && Politics.RegionCrowd(x) > 100)!;
            Assert.NotNull(r);
            var cap = w.Regions.Find(x => x.Id == 0)!;
            w.Tariff = 1.0;             // 国の関税姿勢＝保護の端（州の関税の利害は開幕で0＝乖離1）
            w.Legitimacy = 0.02;        // 引き留めを小さく＝大衆のいる州は離れる向き
            foreach (var q in w.Regions) foreach (var p in q.Pops) p.Militancy = 0;
            foreach (var p in r.Pops) { p.Militancy = 1.0; p.CoreEthnicShare = new Share(0.0); p.NationalConsciousness = 0.0; }   // 州の中で一様＝人を動かしても州の平均は同じ
            r.Dominance = 0.5;
            foreach (var p in r.Pops)
            {
                if (p.Closed || p.IsSoldier || !(p.Size > 0)) continue;
                var dp = cap.Pops.Find(x => x.ClassId == p.ClassId && !x.IsSoldier);
                Assert.NotNull(dp);
                double move = p.Size * (1.0 - keep);
                p.Size -= move; dp!.Size += move;   // 国の中で動かす＝国の総人口（引き留めの分母）は変わらない
            }
            return (w, r);
        }
        var (wX, rX) = Board(1.0);
        var (wY, rY) = Board(0.01);
        var (wZ, rZ) = Board(0.0);   // 大衆を全て首都へ移し、1人だけ残す
        var keepOne = rZ.Pops.Find(p => !p.Closed && !p.IsSoldier)!; keepOne.Size = 0.001;   // 1人＝0.001単位
        Assert.Equal(wX.TotalPopulation, wY.TotalPopulation, 6);
        var c = wX.Coef;
        foreach (var (w, _) in new[] { (wX, rX), (wY, rY), (wZ, rZ) }) Politics.CheckSecession(w, w.Coef, 0.0);
        double s = c.DominanceSpeed, h = wX.SecessionHold;   // 標的の州が最悪の州（他の州は闘争度0）＝その州に届く引き留め
        double pX = h - (rX.Dominance - 0.5) / s, pY = h - (rY.Dominance - 0.5) / s;
        Assert.True(pX > h, $"前提が崩れた：大衆のいる州が離れる向きでない（圧力 {pX:F4}・引き留め {h:F4}）");
        double nX = Politics.RegionCrowd(rX), nY = Politics.RegionCrowd(rY), n0 = c.ColonyPromotionMinPop;
        double fX = nX / (nX + n0), fY = nY / (nY + n0);
        Assert.True(System.Math.Abs(pY / pX - fY / fX) <= 1e-9, $"圧力の比が人の数の飽和の比でない（{pY / pX:F9} 対 {fY / fX:F9}）");
        Assert.True(rZ.Dominance > 0.5, $"1人の州の支配度が回復していない（{rZ.Dominance:F6}）＝数人の州が離れる向きのまま");
    }

    [Fact]
    public void 項目3_人口加重では微小州は選ばれず大州が燃える()
    {
        var w = 項目3_盤面(microHeat: 0.99, bigHeat: 0.6);
        var micro = w.Regions[1]; var big = w.Regions[2];
        Assert.True(Politics.RegionRevoltHeat(micro, w.Coef) > Politics.RegionRevoltHeat(big, w.Coef), "前提が崩れた：微小州の熱が最大でない（旧の鍵なら微小州が選ばれる帯）");
        var (sel, heat, crowd) = Politics.SelectRevoltRegion(w, w.Coef);
        Assert.Same(big, sel);
        Assert.Equal(0.6, heat, 12);   // 門に掛かるのは選ばれた州の熱（最大の熱0.99ではない）
        Assert.Equal(Politics.RegionCrowd(big), crowd);
        var polity0 = w.Polity;
        Politics.Step(w, w.Coef);
        Assert.Equal(polity0, w.Polity);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆"));   // F1が発火した（大州の熱0.6は門0.5超）＝鎮圧でも譲歩でも一揆の記録が1行
        Assert.Contains(w.PendingLog, l => l.Contains("一揆") && l.Contains(big.Name));
        Assert.DoesNotContain(w.PendingLog, l => l.Contains("一揆") && l.Contains(micro.Name));

        // 既知の帰結（係数コメントに記録）＝門は選ばれた州の熱に掛かる：大州がぬるければ（0.4）微小州が門を越えていても燃えない
        //（微小州は人の多い州に負ける。リヨン1831がパリの平穏で隠れる形＝点火の計測で見る）。
        var w2 = 項目3_盤面(microHeat: 0.99, bigHeat: 0.4);
        var (sel2, heat2, _) = Politics.SelectRevoltRegion(w2, w2.Coef);
        Assert.Same(w2.Regions[2], sel2);
        Assert.True(heat2 < w2.Coef.RegionalRevoltThreshold, "前提が崩れた：ぬるい大州が門を越えている");
        Politics.Step(w2, w2.Coef);
        Assert.DoesNotContain(w2.PendingLog, l => l.Contains("一揆"));
    }

    [Fact]
    public void 項目2b_鎮圧も譲歩の約束も闘争度を鎮めない()   // 作者裁定2026-09-09「弾圧は鎮めない・譲歩が鎮める」＝譲歩が鎮めるのは施行の時点（F1法の経路 (b) が検査）
    {
        // 鎮圧枝（強度≤保持）：闘争度はそのまま（残るのは死者と正統性の傷）
        var w = 項目3_盤面(microHeat: 0.0, bigHeat: 0.9);
        var big = w.Regions[2]; var polity0 = w.Polity; double leg0 = w.Legitimacy, pop0 = Politics.RegionCrowd(big);
        var mass = big.Pops.FindAll(p => !p.Closed && !p.IsSoldier);
        Assert.True(mass.Count > 0, "前提が崩れた：大州に大衆がいない");
        Politics.Step(w, w.Coef);
        Assert.Equal(polity0, w.Polity);   // 前提＝政体遷移（別サイトの緩和）が混ざっていない
        Assert.Contains(w.PendingLog, l => l.Contains(big.Name) && l.Contains("一揆を鎮圧"));   // 鎮圧枝に入った（対照が空洞でない）
        foreach (var p in big.Pops) Assert.Equal(0.9, p.Militancy, 12);   // 大衆も閉鎖階級も兵士も1ビットも鎮まらない
        Assert.True(w.Legitimacy < leg0, "鎮圧の正統性の傷が消えている（緩和が無くても傷は残る）");
        Assert.True(Politics.RegionCrowd(big) < pop0, "鎮圧の死者が消えている（緩和が無くても死者は出る）");
        // 譲歩枝（強度≫保持＝勢い係数を巨大に）：目標を置いただけでは鎮まらない（施行の時点で鎮める＝F1法の経路 (b)）
        var wc = 項目3_盤面(microHeat: 0.0, bigHeat: 0.9, strengthScale: 1e9);
        var bigC = wc.Regions[2]; var polityC = wc.Polity;
        Politics.Step(wc, wc.Coef);
        Assert.Equal(polityC, wc.Polity);
        Assert.Contains(wc.PendingLog, l => l.Contains(bigC.Name) && l.Contains("一揆に譲歩"));
        Assert.NotEqual(0, wc.PendingRevoltDemand);   // 譲歩枝に入った（記録が立つ）
        foreach (var p in bigC.Pops) Assert.Equal(0.9, p.Militancy, 12);
    }
    // ============================================================================================
    // ★要求は法の経路（作者裁定2026-09-11・同日点火「蜂起前の要求が簡単に通り過ぎる／受け入れる場合も本当の法改正と同じ経路／
    //   モーダルは通知用・蜂起専用の改正ルートは作らない／即時に上げ下げできるものは要求に加えない／蜂起は要求が通らないこと＝政体の else if は不要」）。
    //   実測（2026-09-11）：ComputeDemands は TopK=3 で常に要求3件＝旧AI決着の別軸枝 `Count >= 2` が常時真＝蜂起枝は到達不能（既定149/149・点火腕707/707が別軸）。
    //   旧＝受諾/別軸は ApplyDemand（現行値と目標を同時に直書き＝MandateGate・前提DAG・不可逆を迂回）＝蜂起専用の改正ルート＝物理削除済み。
    //   新＝通知（PendingFactionCrisis＝AI/プレイヤー共通の札）→ 法の目標（AI＝SelfPlay.Apply／プレイヤー＝法律タブ／初心者＝Parliament.TargetCrisisDemands＝同じ p.X）
    //   → Government.Step の通常経路 → 要求した法が動かなければ蜂起（政体が同じでも）。
    //   ⚠F1（州一揆）は RegionalRevoltThreshold=0 で遮蔽する＝同じ州の熱で F1 が先に燃えると共有クールダウンが物差しを汚す。
    //     段4の門2は `despair > 0` になる＝この番人は門2を測るものではない（門2の番人は段4の各テスト）。
    // ============================================================================================
    // ★2026-09-20 段の点火（裁定C1）：税5軸は段の法になった＝即時値の軸は連続のままの3軸だけ（旧9軸）。
    //   ⚠専売は同日の作者裁定（率の廃止）で**材ごとの ON/OFF の離散の決断**になった＝即時値の軸から外れた。
    private static readonly PolicyAxis[] InstantAxes =
    [
        PolicyAxis.Coercion, PolicyAxis.NavyUpkeep, PolicyAxis.Tariff,
    ];

    // 全軸の現在位置（法とスライダーの実値）＝「法を直接書いたか」の物差し（AxisPosition＝要求の可動域と同じ1本）
    private static double[] LawVector(World w)
    {
        var v = new double[FactionSnapshot.Axes.Length];
        for (int i = 0; i < v.Length; i++) v[i] = FactionSnapshot.AxisPosition(w, FactionSnapshot.Axes[i]);
        return v;
    }
    // 段になった税の実効の率（段の位置でなく Move が動かす値）。段を持たない軸は null。
    private static double? StagedTaxRate(World w, PolicyAxis a) => a switch
    {
        PolicyAxis.IncomeTax => w.TaxRate.Value, PolicyAxis.ProgressiveTax => w.ProgressiveTax, PolicyAxis.PollTax => w.PollTax,
        PolicyAxis.LandTax => w.LandTax, PolicyAxis.EducationSpending => w.EducationSpending, _ => null,
    };
    // 全法目標（NationPolicy の全フィールド）＝「どの目標が置かれたか」の物差し（反射＝軸ごとの読み手を新設しない）
    private static object?[] PolicyVector(World w) => Array.ConvertAll(typeof(NationPolicy).GetFields(), fi => fi.GetValue(w.Policy));

    private static (World W, FactionState F) 法の経路_危機が立つ世界()
    {
        var (w, f) = 段4_絶望した州に小さな運動を置く(regionMilitancy: 0.6, fight: 1.0, rejections: 3.0);
        w.Coef.RevoltIgnitionIntensity = 0.5;   // 点火の門（09-08）＝示強量 1.0×結束1.0 が越える
        w.Coef.RevoltVoiceFloor = 1.0;          // 発言権を門から外す（要求で参政権が動いても示強量が消えない＝門の評価を法の動きから独立させる）
        w.Coef.FactionCohesionDecay = 0;        // 不在減衰を凍結（複数四半期の評価で結束が門を割らない）
        w.Coef.RegionalRevoltThreshold = 0;     // F1 遮蔽（上の注記）
        return (w, f);
    }

    [Fact]
    public void 法の経路_受諾は法を直接変えず目標を置き施行は通常経路()   // (b)
    {
        var (w, f) = 法の経路_危機が立つ世界();
        var before = LawVector(w); double spent = w.MandateSpent;
        Politics.Step(w, w.Coef);                     // 1四半期目＝通知だけ（AI/プレイヤー共通の札）
        Assert.Same(f, w.PendingFactionCrisis);
        Assert.Equal(before, LawVector(w));           // 法は1段も直接動かない（ApplyDemand は呼ばれない）
        Assert.Equal(spent, w.MandateSpent);          // 蜂起専用の請求も無い（政治資本は Government.Step の Spend だけが取る）
        Assert.Equal(3.0, f.Rejections);              // 怨嗟は目標を置いた時点では動かない
        Assert.Equal(0, w.FactionRevolts);
        Assert.NotNull(f.CrisisDemands);
        Assert.True(f.CrisisDemands!.Length > 0, "前提が崩れた：要求が無い運動では受諾の経路を測れない");
        // 受諾＝法の目標を置く（AI＝Apply／プレイヤー＝法律タブ／初心者＝同じ関数）。政治ロックを外す＝門が通る世界で「目標→施行」を1四半期で通す
        var topAxis = (PolicyAxis)(Math.Abs(f.CrisisDemands[0]) - 1); bool topInc = f.CrisisDemands[0] > 0;
        double p0 = f.CrisisDemandPos![0];
        w.PoliticalLockEnabled = false;
        var (topOk, _, topWhy) = Government.GateInfo(w, topAxis, topInc, Government.NextStage(w, topAxis, topInc));   // 法律タブが押せる段か（同じ関数）
        Assert.True(topOk, $"前提が崩れた：本丸 {topAxis} は政治ロックを外しても置けない（{topWhy}）＝この世界では受諾の経路を測れない");
        Parliament.TargetCrisisDemands(w);
        Assert.Equal(before, LawVector(w));           // 目標を置いただけ＝現行法は不変
        double? r0 = StagedTaxRate(w, topAxis);
        Government.Step(w, w.Coef);                   // 通常の施行経路（1期1段・前提DAG・不可逆）
        double p1 = FactionSnapshot.AxisPosition(w, topAxis);
        // ★主張の書き直し（2026-09-20 段の点火）：段になった税が本丸のとき、通常経路（Move の指数接近）は1四半期では段の境界を跨がない
        //   （Government.EnactingStage の注記）＝「動いた」は段の位置でなく**実効の率が要求の向きへ動き、段が施行中である**こと
        //   （Politics.CrisisDemandsMoved が受諾と読む同じ述語）。enum 法の本丸は従来どおり段の位置で見る。
        if (r0 is double rate0)
        {
            double rate1 = StagedTaxRate(w, topAxis)!.Value;
            Assert.True(topInc ? rate1 > rate0 : rate1 < rate0, $"本丸 {topAxis} の率が通常経路で動いていない（{rate0:F3}→{rate1:F3}）");
            Assert.True(Government.EnactingStage(w, topAxis, topInc), $"本丸 {topAxis} の段が施行中でない");
        }
        else Assert.True(topInc ? p1 > p0 : p1 < p0, $"本丸 {topAxis} が通常経路で動いていない（{p0:F3}→{p1:F3}）");
        Politics.Step(w, w.Coef);                     // 評価＝本丸が動いた＝受諾の実体（怨嗟の解消は施行の時点）
        Assert.Equal(0.0, f.Rejections);
        Assert.Equal(0, w.FactionRevolts);
        Assert.Same(f, w.PendingFactionCrisis);       // 運動は解散しない（09-09）＝札は立ったまま・次の要求を取り直している
        Assert.NotNull(f.CrisisDemands);
    }

    [Fact]
    public void 法の経路_要求が通らなければ政体が同じでも蜂起する()   // (c) 点火の else if（NearestPolity）は無い
    {
        // 体制内の要求（主軸の最寄り政体＝現政体・旧は受諾の枝＝蜂起しない側）＝通知→誰も目標を置かない（NullAgent・法律タブも触らない）→要求が通らない→蜂起
        var (w, f) = 法の経路_危機が立つ世界();
        int sameIdeo = -1;
        for (int i = 0; i < Ideologies.Count; i++) if (Politics.NearestPolity(i) == w.Polity) { sameIdeo = i; break; }
        Assert.True(sameIdeo >= 0, "前提が崩れた：現政体を最寄りとする主軸が無い");
        f.Ideo = sameIdeo;
        Assert.Equal(w.Polity, Politics.NearestPolity(f.Ideo));
        Politics.Step(w, w.Coef);
        Assert.Same(f, w.PendingFactionCrisis);
        Assert.Equal(0, w.FactionRevolts);            // 通知の四半期＝通常の法改正の遅延（タイマーではない＝四半期順序そのもの）
        Politics.Step(w, w.Coef);
        Assert.Equal(1, w.FactionRevolts);            // 要求した法が1段も動かなかった＝蜂起（政体が同じでも）
        Assert.Null(w.PendingFactionCrisis);
        Assert.Equal(4.0, f.Rejections);              // TriggerFactionRevolt の +1 だけ（新しい記帳は足していない）
        // ★2026-09-28（処理の一元化 段2 束3）：単国の世界の蜂起は地図の上に立てられない＝即時の決着（Politics.SettleRevolt）＝運動は燃え尽きる
        //   （旧＝旧経路の内戦の主体 RevoltingFaction に残った）。政体の分類（要求戦＝政体が同じ）は決着の側に残る。
        Assert.Null(w.RevoltingFaction);
        Assert.Equal(0.0, f.Cohesion);
    }

    [Fact]
    public void 法の経路_要求に即時値の軸は載らない()   // (d)
    {
        // 分類の宣言＝連続のままの Government.Move で書く4軸（段になった税5軸は法＝2026-09-20。専売は商品ごとの贅沢税＝法でない）。法（StepEnum/StepBool・段）はこの4軸以外の全軸
        foreach (var a in FactionSnapshot.Axes) Assert.Equal(Array.IndexOf(InstantAxes, a) >= 0, FactionSnapshot.IsInstantAxis(a));
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        var off = new System.Collections.Generic.List<FactionSnapshot.Demand>[PolicyPreferences.BlocCount];
        for (int b = 0; b < off.Length; b++) off[b] = FactionSnapshot.ComputeDemands(w, b);   // 綱領・UI の読み手（既定 lawsOnly=false）＝全軸
        bool anyInstantOff = false;
        for (int b = 0; b < off.Length; b++)
        {
            var on = FactionSnapshot.ComputeDemands(w, b, lawsOnly: true);   // 危機側の読み手の引数（RecordCrisisDemands／TargetCrisisDemands／factionprobe が渡す）
            Assert.Equal(off[b], FactionSnapshot.ComputeDemands(w, b));       // ★範囲是正（2026-09-11）：綱領・UI の読み手（既定 lawsOnly=false）は全軸のまま＝減税を掲げる政党は残る
            foreach (var d in on) Assert.False(FactionSnapshot.IsInstantAxis(d.Axis), $"bloc {b} の要求に即時値の軸 {d.Axis} が載っている");
            foreach (var d in off[b]) anyInstantOff |= FactionSnapshot.IsInstantAxis(d.Axis);
            // 法の要求の順位は変わらない（除いた分だけ下位の法が繰り上がる）＝旧の要求のうち法だけを抜いた並びは新の先頭部分と一致
            var offLaws = off[b].FindAll(d => !FactionSnapshot.IsInstantAxis(d.Axis));
            Assert.True(on.Count >= offLaws.Count);
            for (int i = 0; i < offLaws.Count; i++) Assert.Equal(offLaws[i], on[i]);
        }
        Assert.True(anyInstantOff, "対照が成立しない：旧の要求にも即時値の軸が1つも無い（フィルタが何も除いていない世界）");
    }

    [Fact]
    public void 法の経路_通らない段は目標に置かれない()   // 置く/置かないは法律タブと同じ GateInfo（前提DAG＝官僚制度←独占禁止）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World; var p = w.Policy;
        Assert.True(w.Bureaucracy < BureaucracyLaw.MeritExam && w.Antitrust == AntitrustLaw.None, "前提が崩れた：独占禁止の前提（資格任用）が既に満ちている");
        var (ok, _, reason) = Government.GateInfo(w, PolicyAxis.Antitrust, true, (int)AntitrustLaw.CartelBan);
        Assert.False(ok); Assert.Contains("官僚制度", reason);   // 法律タブならこの段は押せない（LawRowVM.CanReachAndAfford＝同じ関数）
        p.Antitrust = w.Antitrust;
        Parliament.TargetDemand(w, PolicyAxis.Antitrust, true);
        Assert.Equal(w.Antitrust, p.Antitrust);        // 置かれない＝拒否（幽霊戦線も開かない）
        // （旧の対照＝門を見ない呼び手〔議会の動的与党の要求〕は 2026-09-26 に撤去＝回5-1 項目17・引数 gated ごと消えた）
        // 前提が満ちて門が開けば同じ呼び出しで置かれる
        w.Bureaucracy = BureaucracyLaw.MeritExam; w.PoliticalLockEnabled = false;
        p.Antitrust = w.Antitrust;
        Parliament.TargetDemand(w, PolicyAxis.Antitrust, true);
        Assert.Equal(AntitrustLaw.CartelBan, p.Antitrust);
        Assert.Equal(AntitrustLaw.None, w.Antitrust);   // 目標だけ＝現行法は Government.Step が動かす
    }

    [Fact]
    public void 法の経路_AIとプレイヤーは同じ危機に同じ目標を置く()   // (e) 対称＝AIの判断は IsPlayer を読まない・法は直接書かない
    {
        static World Build()
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Japan(), NationConfig.America()], seed: 1789, _ => new NullAgent());
            sim.Run(8);
            return sim.Nations[0].World;
        }
        var wa = Build(); var wb = Build();
        Assert.True(wa.SelfPower is not null && wa.Regions.Count > 0);
        wa.SelfPower!.IsPlayer = true;   // 片方だけプレイヤーの印（UI と同じ）
        foreach (var w in new[] { wa, wb })
        {
            var f = new FactionState { Region = w.Regions[0], Bloc = 2, Ideo = (int)Ideology.Communism, Size = 1.0, AvgMilitancy = 1.0, Cohesion = 1.0, Rejections = 3.0 };
            w.Factions.Add(f); w.PendingFactionCrisis = f;   // 同じ危機の札（通知）
            // ★(82) で置いた潤沢の減税の明示ピン（OpulenceTaxRelief=0）は (85) で外した＝反射が門（GateAllLawTargets）の前へ移り、
            //   縮めた税の目標も危機の要求と同じ門を通る＝ピンなしでこの番人が緑であること自体が対称の証拠（(82) は門の後の倍率が段3→2 を置いて赤だった）。
        }
        Assert.Equal(PolicyVector(wb), PolicyVector(wa));   // 前提：出発点の法目標が同じ
        var lawsA = LawVector(wa);
        new UtilityAgent().DecideQuarter(wa);
        new UtilityAgent().DecideQuarter(wb);
        Assert.Equal(PolicyVector(wb), PolicyVector(wa));   // 全法目標が一致＝要求への応答も含めて IsPlayer で分岐しない
        Assert.Equal(lawsA, LawVector(wa));                  // 目標を置いただけ＝現行法は不変（施行は Government.Step）
        // AIが置いた目標＝初心者経路/プレイヤーの法律タブと同じ関数（TargetCrisisDemands＝gated）を同じ世界へ重ねても1つも動かない＝同じ段を指している
        var afterAi = PolicyVector(wb);
        Parliament.TargetCrisisDemands(wb);
        Assert.Equal(afterAi, PolicyVector(wb));
    }

    // ============================================================================================
    // ★F1 譲歩は法の経路（作者裁定2026-09-11「F1 の譲歩も同じ原則」＝同じ目標機構・同日点火）：
    //   旧 ConcedeToRevolt は固定梯子（穀物法↓→労働時間↓→社会保障↑→農奴解放↑→参政権↑）を現行法と目標の直書き（w.X=…; p.X=…）で1段のむ
    //   ＝政治資本・反対・法の天井を迂回する蜂起専用の改正ルート（梯子が尽きるので譲歩回数が天井＝実測どの腕も20回）＝物理削除済み。
    //   新＝次の段を Parliament.TargetDemand（gated＝F3b の TargetCrisisDemands が使う同じ1本）で法の目標に置き World.PendingRevoltDemand に記録
    //   → Government.Step の通常経路（滞る・通らない）→ 動けば Politics.Step 先頭が記録を下ろす。AI は Apply の末尾（TargetCrisisDemands）で置き直す＝IsPlayer で分岐しない。
    //   固定代金は請求しない（施行側の MandateGate だけが取る＝第二の課金を作らない）。
    //   ⚠盤面＝項目3_盤面（大州の熱0.9・勢い係数1e9＝譲歩枝・σ=0 で凍結・Factions.Clear() で F3b は遮蔽済み）。
    // ============================================================================================
    private static readonly (PolicyAxis Axis, bool Inc)[] F1Ladder =
    [
        (PolicyAxis.CornLaws, false), (PolicyAxis.ShorterHours, false), (PolicyAxis.SocialSecurity, true), (PolicyAxis.SerfEmancipation, true), (PolicyAxis.Suffrage, true),
    ];
    // 旧梯子の「次の段」（現行法で判定する else-if と同じ順・同じ述語）＝番人側の物差し。尽きていれば null
    private static (PolicyAxis Axis, bool Inc)? F1NextRung(World w) =>
          w.CornLaws > ReformLevel.None ? F1Ladder[0]
        : w.WorkHours > WorkHours.Eight ? F1Ladder[1]
        : w.SocialSecurityLaw < SocialSecurityLaw.Pension ? F1Ladder[2]
        : w.SerfEmancipation < ReformLevel.Full ? F1Ladder[3]
        : w.Suffrage < Suffrage.AdultMale ? F1Ladder[4]
        : null;
    private static int F1LawLevel(World w, PolicyAxis a) => a switch   // 現行法の段
    {
        PolicyAxis.CornLaws => (int)w.CornLaws, PolicyAxis.ShorterHours => (int)w.WorkHours, PolicyAxis.SocialSecurity => (int)w.SocialSecurityLaw,
        PolicyAxis.SerfEmancipation => (int)w.SerfEmancipation, PolicyAxis.Suffrage => (int)w.Suffrage, _ => throw new ArgumentOutOfRangeException(nameof(a)),
    };
    private static int F1TargetLevel(World w, PolicyAxis a) => a switch   // 法律タブが書く p.X の段（社会保障は法の段 v/3＝GameViewModel.Law.cs と同じ単位）
    {
        PolicyAxis.CornLaws => (int)w.Policy.CornLaws, PolicyAxis.ShorterHours => (int)w.Policy.WorkHours,
        PolicyAxis.SocialSecurity => (int)Math.Round(w.Policy.SocialSecurityTarget * Government.SocialSecurityMaxLevel),
        PolicyAxis.SerfEmancipation => (int)w.Policy.SerfEmancipation, PolicyAxis.Suffrage => (int)w.Policy.Suffrage, _ => throw new ArgumentOutOfRangeException(nameof(a)),
    };
    private static World F1_譲歩の盤面() => 項目3_盤面(microHeat: 0.0, bigHeat: 0.9, strengthScale: 1e9);   // 大州が門を越え・勢い≫保持＝譲歩枝

    [Fact]
    public void F1法の経路_譲歩は法を直接変えず目標を置き施行は通常経路()   // (b)
    {
        var w = F1_譲歩の盤面();
        var rung = F1NextRung(w); Assert.True(rung is not null, "前提が崩れた：梯子が尽きている");
        var (axis, inc) = rung!.Value;
        w.PoliticalLockEnabled = false;   // 門が通る世界（置く/置かないは GateInfo＝法律タブと同じ）＝「目標→施行」を1四半期で通す
        var (ok, _, why) = Government.GateInfo(w, axis, inc, Government.NextStage(w, axis, inc));
        Assert.True(ok, $"前提が崩れた：段 {axis} は政治ロックを外しても置けない（{why}）");
        int law0 = F1LawLevel(w, axis); double spent = w.MandateSpent; double pos0 = FactionSnapshot.AxisPosition(w, axis); var polity0 = w.Polity;
        var before = LawVector(w);
        w.Coef.MilitancySpeed = 0;   // ストックの追従を止める＝緩和のインパルスだけを見る
        var mil0 = Array.ConvertAll(w.Regions[2].Pops.ToArray(), q => q.Militancy);
        Politics.Step(w, w.Coef);
        Assert.Equal(polity0, w.Polity);
        Assert.Contains(w.PendingLog, l => l.Contains(w.Regions[2].Name) && l.Contains("一揆に譲歩"));
        Assert.Equal(before, LawVector(w));                                    // 法は1段も直接動かない（w.X の直書きが無い）
        Assert.True(w.MandateSpent <= spent, "蜂起専用の固定代金が残っている");   // 政治資本は Government.Step の MandateGate（Spend）だけが取る
        Assert.Equal(inc ? (int)axis + 1 : -((int)axis + 1), w.PendingRevoltDemand);   // 記録＝梯子の次の段（旧と同じ順）
        Assert.Equal(pos0, w.PendingRevoltDemandPos);
        Assert.Equal(w.Regions[2].Id, w.PendingRevoltRegion);                  // 記録＝一揆の州
        Assert.Equal(mil0, Array.ConvertAll(w.Regions[2].Pops.ToArray(), q => q.Militancy));   // 目標を置いただけでは鎮まらない（緩和は施行の時点）
        Assert.Equal(law0 + (inc ? 1 : -1), F1TargetLevel(w, axis));          // 目標＝現行法から1段（法律タブが書くのと同じ p.X）
        var placed = PolicyVector(w);
        Parliament.TargetDemand(w, axis, inc);                                 // 同じ関数（初心者経路/AI）を重ねても動かない＝同じ段を指している
        Assert.Equal(placed, PolicyVector(w));
        Government.Step(w, w.Coef);                                            // 通常の施行経路（1期1段・前提DAG・不可逆・MandateGate）
        Assert.Equal(law0 + (inc ? 1 : -1), F1LawLevel(w, axis));
        Politics.Step(w, w.Coef);                                              // 翌評価＝要求の向きへ動いた＝記録が下りる（クールダウン中でも先頭で見る）
        Assert.Equal(0, w.PendingRevoltDemand);
        for (int i = 0; i < mil0.Length; i++)                                  // 法が動いた＝一揆の州の大衆だけ既存の緩和率（譲歩が鎮めるのは施行の時点）
        {
            var q = w.Regions[2].Pops[i];
            Assert.Equal((!q.Closed && !q.IsSoldier) ? mil0[i] * w.Coef.ReformMilitancyRelief : mil0[i], q.Militancy);
        }
    }

    [Fact]
    public void F1法の経路_通らない段は施行されず要求が残る()   // (c) 天井（前提DAG＝法律タブで押せない段）＝置かれない・施行されない・記録は残る・AI の Apply も置かない
    {
        var w = F1_譲歩の盤面(); var p = w.Policy;
        // 梯子を社会保障の段（労災→失業）まで登らせる：失業の段は官僚制度（資格任用以上）が前提（Government.LawPrereq）＝政治ロックを外しても通らない
        w.CornLaws = ReformLevel.None; p.CornLaws = w.CornLaws;
        w.WorkHours = WorkHours.Eight; p.WorkHours = w.WorkHours;
        w.SocialSecurityLaw = SocialSecurityLaw.WorkersComp; w.SocialSecurity = 1 / 3.0; p.SocialSecurityTarget = 1 / 3.0;
        Assert.True(w.Bureaucracy < BureaucracyLaw.MeritExam, "前提が崩れた：官僚制度が既に資格任用以上");
        Assert.Equal(F1Ladder[2], F1NextRung(w));
        w.PoliticalLockEnabled = false;
        var (ok, _, why) = Government.GateInfo(w, PolicyAxis.SocialSecurity, true, (int)SocialSecurityLaw.Unemployment);
        Assert.False(ok); Assert.Contains("官僚制度", why);   // 法律タブならこの段は押せない（LawRowVM.CanReachAndAfford＝同じ関数）
        int code = (int)PolicyAxis.SocialSecurity + 1;
        var pol0 = PolicyVector(w); var polity0 = w.Polity;
        Politics.Step(w, w.Coef);
        Assert.Equal(polity0, w.Polity);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆に譲歩"));
        Assert.Equal(code, w.PendingRevoltDemand);                 // 要求は立つ
        Assert.Equal(pol0, PolicyVector(w));                        // 置かれない＝拒否（幽霊戦線も開かない）
        Assert.Equal(SocialSecurityLaw.WorkersComp, w.SocialSecurityLaw);
        for (int q = 0; q < 4; q++)
        {
            var polQ = PolicyVector(w);
            Parliament.TargetCrisisDemands(w);                      // AI の Apply の末尾＝同じ関数（初心者経路も同じ）
            Assert.Equal(polQ, PolicyVector(w));                    // 通らない段は毎期置かれない
            Government.Step(w, w.Coef);
            Assert.Equal(SocialSecurityLaw.WorkersComp, w.SocialSecurityLaw);   // 施行されない
            Politics.Step(w, w.Coef);
            Assert.Equal(code, w.PendingRevoltDemand);              // 要求は残る（新しいタイマーは無い＝諦めの恒久化もしない）
        }
        // 前提が満ちれば同じ呼び出しで置かれ、通常経路で施行され、記録が下りる
        w.Bureaucracy = BureaucracyLaw.MeritExam; p.Bureaucracy = w.Bureaucracy;
        Parliament.TargetCrisisDemands(w);
        Assert.Equal((int)SocialSecurityLaw.Unemployment, F1TargetLevel(w, PolicyAxis.SocialSecurity));
        Government.Step(w, w.Coef);
        Assert.Equal(SocialSecurityLaw.Unemployment, w.SocialSecurityLaw);
        Politics.Step(w, w.Coef);
        Assert.Equal(0, w.PendingRevoltDemand);
    }

    [Fact]
    public void F1法の経路_梯子が尽きたら何も置かない()   // (d) のむものが無い＝return（緩和も請求も無い）＝記録も目標も書かない
    {
        var w = F1_譲歩の盤面(); var p = w.Policy;
        w.CornLaws = ReformLevel.None; p.CornLaws = w.CornLaws;
        w.WorkHours = WorkHours.Eight; p.WorkHours = w.WorkHours;
        w.SocialSecurityLaw = SocialSecurityLaw.Pension; w.SocialSecurity = 1.0; p.SocialSecurityTarget = 1.0;
        w.SerfEmancipation = ReformLevel.Full; p.SerfEmancipation = w.SerfEmancipation;
        w.Suffrage = Suffrage.AdultMale; p.Suffrage = w.Suffrage;
        Assert.Null(F1NextRung(w));
        var before = LawVector(w); var pol0 = PolicyVector(w); double spent = w.MandateSpent; var polity0 = w.Polity;
        Politics.Step(w, w.Coef);
        Assert.Equal(polity0, w.Polity);
        Assert.Contains(w.PendingLog, l => l.Contains("一揆に譲歩"));   // 譲歩枝には入る
        Assert.Equal(before, LawVector(w)); Assert.Equal(pol0, PolicyVector(w));
        Assert.True(w.MandateSpent <= spent, "尽きた梯子が請求している");
        Assert.Equal(0, w.PendingRevoltDemand);
    }

    [Fact]
    public void F1法の経路_AIとプレイヤーは同じ一揆に同じ目標を置く()   // (e) 対称＝ConcedeToRevolt も Apply の置き直しも IsPlayer を読まない・法は直接書かない
    {
        static World Build()
        {
            var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Japan(), NationConfig.America()], seed: 1789, _ => new NullAgent());
            sim.Run(8);
            return sim.Nations[0].World;
        }
        var wa = Build(); var wb = Build();
        Assert.True(wa.SelfPower is not null && wa.Regions.Count > 1);
        wa.SelfPower!.IsPlayer = true;   // 片方だけプレイヤーの印（UI と同じ）
        foreach (var w in new[] { wa, wb })
        {
            var c = w.Coef;
            c.MilitancySpeed = 0; c.RegionalRevoltStrength = 1e9;   // 勢い≫保持＝譲歩枝
            w.Factions.Clear(); w.PendingRebelRegions = null;
            foreach (var r in w.Regions) foreach (var p in r.Pops) p.Militancy = 0;
            foreach (var p in w.Regions[1].Pops) p.Militancy = 0.9;   // 同じ州の同じ一揆
        }
        Assert.Equal(PolicyVector(wb), PolicyVector(wa));   // 前提：出発点の法目標が同じ
        var laws = LawVector(wa);
        Politics.Step(wa, wa.Coef); Politics.Step(wb, wb.Coef);
        Assert.NotEqual(0, wa.PendingRevoltDemand);          // 譲歩枝に入った（対照が空洞でない）
        Assert.Equal(wb.PendingRevoltDemand, wa.PendingRevoltDemand);
        Assert.Equal(PolicyVector(wb), PolicyVector(wa));   // 譲歩で置いた目標が IsPlayer の有無で一致
        Assert.Equal(laws, LawVector(wa));                   // 現行法は不変（施行は Government.Step）
        new UtilityAgent().DecideQuarter(wa);
        new UtilityAgent().DecideQuarter(wb);
        Assert.Equal(PolicyVector(wb), PolicyVector(wa));   // AI の置き直し（Apply 末尾の TargetCrisisDemands）も IsPlayer で分岐しない
        Assert.Equal(laws, LawVector(wa));
        // AI が置いた目標＝初心者経路/プレイヤーの法律タブと同じ関数を同じ世界へ重ねても1つも動かない＝同じ段を指している
        var afterAi = PolicyVector(wb);
        Parliament.TargetCrisisDemands(wb);
        Assert.Equal(afterAi, PolicyVector(wb));
    }

    // ============================================================================================
    // ★裁定C1（2026-09-12「税率は反乱の要求に乗せるが、法の変更そのものは闘争度に関与しない」）：
    //   段になった税6軸は**もう即座に上げ下げできない**＝危機の要求から除かない（2026-09-20 点火）。
    //   ⚠治安維持力（軍事費）・海軍維持費・関税は除外のまま（前者は AxisPosition が別量、後2つは連続のスライダー）。
    //   ⚠旧の対照「ゲート0では9軸とも載らない」は旗とともに退役（段の無い世界が消えた）。
    // ============================================================================================

    [Fact]
    public void 法の経路_段になった税は要求に載る()   // (d)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var w = sim.World;
        // 段2まで課している世界＝減税の可動域がある（段0の国では税の要求はそもそも立たない＝この番人が測れない）
        w.PollTax = 2.0 / 3.0; w.TaxRate = new Share(2.0 / 3.0);
        w.Policy.PollTaxTarget = 2.0 / 3.0; w.Policy.IncomeTaxTarget = 2.0 / 3.0;
        // ★主張の書き直し（2026-09-20）：旧はここで専売の段を置き、その位置が要求に載ることも測っていた。
        //   作者裁定で専売は政策軸ごと撤去された（政治問題ではなく生産マネジメントの範囲）＝要求に載る余地が無い。
        //   この番人が測るのは「段になった**税**が要求に載る」ことだけになった（専売は別の番人＝専売は政治の軸ではない）。

        bool anyTax = false;
        for (int b = 0; b < PolicyPreferences.BlocCount; b++)
        {
            var off = FactionSnapshot.ComputeDemands(w, b);                        // 綱領・UI の読み手＝全軸（裁定C1でも不変）
            var on = FactionSnapshot.ComputeDemands(w, b, lawsOnly: true);         // 危機側の読み手
            foreach (var d in on)
                Assert.False(FactionSnapshot.IsInstantAxis(d.Axis),
                             $"連続のままの軸 {d.Axis} が危機の要求に載っている（1期0.018の動きで必ず受諾になる）");
            // 除外は連続のままの4軸だけ＝全軸の並びからそれを抜いた列が、危機側の先頭部分と一致する（順位は動かない）
            var expect = off.FindAll(d => !FactionSnapshot.IsInstantAxis(d.Axis));
            Assert.True(on.Count >= expect.Count);
            for (int i = 0; i < expect.Count; i++) Assert.Equal(expect[i], on[i]);
            foreach (var d in on)
                anyTax |= d.Axis is PolicyAxis.IncomeTax or PolicyAxis.ProgressiveTax or PolicyAxis.PollTax
                                 or PolicyAxis.LandTax or PolicyAxis.EducationSpending;   // ⚠専売は軸ごと撤去（2026-09-20）＝そもそも d.Axis になり得ない
        }
        Assert.True(anyTax, "段にしても税の軸が1つも危機の要求に載らない＝裁定C1が効いていない");
    }

    [Fact]
    public void 法の経路_税の要求は段を目標に置けば通り置かなければ蜂起する()   // (e)
    {
        // 本丸を人頭税の減税に固定する（ComputeDemands の順位に依存させない＝ここで測るのは「動いたか」の判定だけ）。
        (int Revolts, double Rejections) Run(bool concede)
        {
            var (w, f) = 法の経路_危機が立つ世界();
            w.PollTax = Government.TaxValue(PolicyAxis.PollTax, 2); w.Policy.PollTaxTarget = w.PollTax;   // 実効も目標も段2＝政府は何も約束していない（★錨2026-09-18＝段の値は軸ごとの錨から引く）
            Assert.Equal(2, Government.TaxStage(PolicyAxis.PollTax, w.PollTax));
            Politics.Step(w, w.Coef);                                     // 通知の四半期（要求を掲げる）
            Assert.Same(f, w.PendingFactionCrisis);
            f.CrisisDemands = [-((int)PolicyAxis.PollTax + 1)];           // 本丸＝人頭税を下げろ（減税＝inc=false）
            f.CrisisDemandPos = [FactionSnapshot.AxisPosition(w, PolicyAxis.PollTax)];
            f.Rejections = 3.0;
            if (concede) w.Policy.PollTaxTarget = Government.TaxValue(PolicyAxis.PollTax, 1);   // 法の目標を1段下げる＝法律タブ／AIの Apply が置くのと同じ p.X
            Assert.Equal(concede, Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false));
            Assert.Equal(2, Government.TaxStage(PolicyAxis.PollTax, w.PollTax));              // ⚠実効段は**まだ1段も動いていない**（Move は指数接近＝境界まで4〜6四半期）
            Politics.Step(w, w.Coef);                                     // 評価の四半期
            return (w.FactionRevolts, f.Rejections);
        }
        Assert.Equal(1, Run(concede: false).Revolts);   // 誰も段を置かない＝要求は拒まれた＝蜂起
        var ok = Run(concede: true);
        Assert.Equal(0, ok.Revolts);                    // 段を目標に置いた＝施行中＝蜂起しない（実効段が動くのを待つ間に蜂起で潰されない）
        Assert.Equal(0.0, ok.Rejections);               // 受諾の記帳（怨嗟0）
    }

    [Fact]
    public void 法の経路_施行中の述語は目標の段が向こう側にありその一歩が今通ることを見る()   // (e') 差分でもタイマーでもない
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.PollTax = 2.0 / 3.0; w.Policy.PollTaxTarget = 1.0 / 3.0;
        w.Coef.MandateOverStockLegitPower = 0;   // ★(405)(419) 壁の一歩を閉じた世界〔旧の旗 0＝壁の一歩は永遠に通らない〕を見る番人＝払える量の指数を 0 に明示（既定は 2）
        // ⚠旧の対照「ゲート0＝常に false」は旗とともに退役（2026-09-20 段の点火）。
        // ★空手形の封じ（2026-09-12 反証）：門が閉じている向きは目標を置いても施行中ではない＝Move が1ミリも動かさないので
        //   毎月の取り立ては1ミリも変わらない（1789年の仏＝国庫が痩せた国で人頭税を下げる純支持は負）。
        Assert.False(Government.GateInfo(w, PolicyAxis.PollTax, increasing: false,
                                         Government.NextStage(w, PolicyAxis.PollTax, increasing: false)).Ok);
        Assert.False(Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false));
        w.PoliticalLockEnabled = false;   // 政治的可否と直交な機構の単体検証（Move/MandateGate/GateInfo が共有する隔離弁）
        Assert.True(Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false));
        Assert.False(Government.EnactingStage(w, PolicyAxis.PollTax, increasing: true));    // 向きが違えば施行中ではない
        w.Policy.PollTaxTarget = 2.0 / 3.0;                                                  // 約束を撤回＝目標＝現在段
        Assert.False(Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false));
        // 実効値が目標へ永久に漸近しても（Move の指数接近）、段が同じなら「施行中」にはならない＝差分の誤読を封じている
        w.PollTax = 2.0 / 3.0 - 1e-6;
        Assert.False(Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false));
        // 段を持たない軸（enum法・連続スライダー）は述語を持たない＝判定は段の差分のまま
        foreach (var a in new[] { PolicyAxis.Suffrage, PolicyAxis.Coercion, PolicyAxis.Tariff, PolicyAxis.NavyUpkeep, PolicyAxis.SocialSecurity })
        {
            Assert.False(Government.EnactingStage(w, a, increasing: true));
            Assert.False(Government.EnactingStage(w, a, increasing: false));
        }
    }

    /// <summary>★(410)（(407) の申し送り ②）上の番人は払える量の指数 <c>MandateOverStockLegitPower</c> を 0 に固定している（壁の一歩を閉じた世界＝(419) で焼き込んだ旧の旗 0）。
    /// 既定（壁の一歩が値段で通る・(405) で採用）の世界の対：反対を押し切る代価が b を超える一歩（壁の一歩）でも、**その一歩が今通るときだけ**目標は施行中になる＝空手形は既定の経路でも閉じている。
    /// 同じ盤面（1789 年の仏・人頭税を下げる目標・b を 0.3 に固定して代価 ＞ b の壁にする）で、政治資本の在庫 S と正統性だけを動かす：
    /// ①満タン（S＝0）で正統性を 0.05 刻みで掃引し、各点で〈壁の式（代価 ≦ 正統性の二乗＝(409) で採った払える量 avail^2・この四半期の請求済みは 0）〉と門の可否と施行中の述語が一致（通る点と通らない点の両側があること）／
    /// ②述語は今その一歩が通るときだけ真（空手形の封じ）／③S が少しでも使われていれば正統性が十分でも通らない（壁は満タンのときだけ）。
    /// 代価は正統性に依って動く（純支持が正統性を読む）ので、各点の代価は門の照会から取る。</summary>
    [Fact]
    public void 法の経路_既定の経路でも施行中の述語は壁の一歩が今通るときだけ真()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.PollTax = 2.0 / 3.0; w.Policy.PollTaxTarget = 1.0 / 3.0;
        Assert.Equal(2.0, w.Coef.MandateOverStockLegitPower);              // ★(412) 既定は採用値（(409) 払える量＝avail^2・床の係数は削除した）。期待は下で指数 2 のリテラルから独立に組む
        w.PoliticalCapitalMax = 0.3;                                        // b を固定（Step の前に World が持つ値）。代価は 0.3 を超える＝壁の一歩（下で確かめる）
        w.MandateSpentNew = 0;
        var next = Government.NextStage(w, PolicyAxis.PollTax, increasing: false);

        // ⚠代価は正統性に依って動く（純支持が正統性を読む）＝正統性を動かすたびに、その盤面の代価を門の照会から取る。期待は〈壁の式〉（満タンで 代価 ≦ 正統性²）を盤面ごとに独立に当てる。
        (bool gate, bool enacting, double cost) At(double spent, double legitimacy)
        {
            w.MandateSpent = spent; w.Legitimacy = legitimacy;
            var g = Government.GateInfo(w, PolicyAxis.PollTax, increasing: false, next);
            return (g.Ok, Government.EnactingStage(w, PolicyAxis.PollTax, increasing: false), g.Cost);
        }
        int open = 0, closed = 0; double openLegit = double.NaN;
        for (double legit = 0.05; legit <= 1.0001; legit += 0.05)
        {
            var s = At(0, legit);                                           // ①満タン（S＝0）
            Assert.True(s.cost > w.PoliticalCapitalMax, $"盤面の前提の破れ：この一歩は壁（代価 {s.cost} ＞ b {w.PoliticalCapitalMax}）でない（正統性 {legit}）");
            Assert.Equal(0.0, w.MandateSpentNew);                            // この四半期の請求済み 0＝avail は正統性そのもの
            bool expect = s.cost <= legit * legit;                           // 壁の一歩は満タンで代価が正統性の二乗以下のときだけ（★(412) (409) の形へ書き換えた）
            Assert.True(s.gate == expect, $"満タンの壁の一歩の可否が壁の式と違う（正統性 {legit}・代価 {s.cost}・払える量 {legit * legit}・gate {s.gate}）");
            Assert.True(s.enacting == s.gate, $"施行中の述語が門と違う（正統性 {legit}・gate {s.gate}・施行中 {s.enacting}）");   // ②空手形の封じ＝述語は今その一歩が通るときだけ真
            if (s.gate) { open++; if (double.IsNaN(openLegit)) openLegit = legit; } else closed++;
        }
        Assert.True(open > 0 && closed > 0, $"正統性の掃引が片側だけ（通る {open}・通らない {closed}）＝壁の式を両側から見ていない");
        var used = At(0.001, 1.0);                                           // ③在庫が少しでも使われている（S＞0）＝満タンでない＝壁は通らない（正統性が十分でも）
        Assert.True(At(0, 1.0).gate, "前提の破れ：正統性 1.0・満タンで壁の一歩が通らない");
        Assert.False(used.gate); Assert.False(used.enacting);
    }
}
