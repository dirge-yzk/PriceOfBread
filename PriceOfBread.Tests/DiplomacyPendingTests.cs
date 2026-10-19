using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (440) 線 dp：外交の待ち（参戦要請・最後通牒）の寿命と、侵略の近接の緊張の標的の座標（事前登録 measurements/dp_20261019_prereg.txt の G1〜G5・追記 4 で形を直した）。
/// 期待は本体の別の述語から作る（戦争の一覧・緊張の値）＝待ちを下ろす同じ式を写さない。
/// 何もしない機構は必ず緑になるので、各テストは〈下りる〉と〈下りない〉の両方（対照）を同じ盤面で見る。
/// </summary>
public class DiplomacyPendingTests
{
    private static MultiNationSim Board()
        => new([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France(), NationConfig.Russia()], seed: 1789);

    /// <summary>四半期の境でない月だけを歩く（AI の外交・調停は四半期の境の月にしか動かない）。</summary>
    private static readonly int[] NonQuarterMonths = [1, 2, 4, 5, 7, 8, 10, 11];

    [Fact]
    public void 参戦要請の待ちは終えた戦争を指すものだけが消える_続いている戦争の待ちは残る()
    {
        var sim = Board(); var dip = sim.Diplomacy; var c = sim.Coef;
        var me = sim.Nations[0].World.SelfPower!; var foe = sim.Nations[1].World.SelfPower!;
        var third = sim.Nations[2].World.SelfPower!; var russia = sim.Nations[3].World.SelfPower!;
        me.IsPlayer = true;
        var ending = new War { A = foe, B = third, ForcedPeace = true };      // 次の月の歩みで講和へ落ちる
        var lasting = new War { A = russia, B = me };                         // 続く戦争（対照）
        dip.Wars.Add(ending); dip.Wars.Add(lasting);
        me.PendingCallToArms = (foe, third);        // 終える戦争を指す（呼んだ国, 敵）
        russia.PendingCallToArms = (third, foe);    // 同じ戦争を逆向きに指す（順不同で一致する）
        foe.PendingCallToArms = (russia, me);       // 続く戦争を指す

        dip.Step(c, sim.Rng, 1);

        // ⚠失敗の表示で国の対象グラフを書き出させない（待ちの組をそのまま Assert に渡すと表示の再帰でテストホストが落ちる）＝真偽で渡す
        Assert.False(dip.Wars.Contains(ending), "前提が崩れた：戦争が終わっていない（終えなければ検査になっていない）");
        Assert.True(dip.Wars.Contains(lasting), "前提が崩れた：続くはずの戦争が消えた");
        Assert.True(me.PendingCallToArms is null, "終えた戦争を指す参戦要請の待ちが残っている");
        Assert.True(russia.PendingCallToArms is null, "終えた戦争を逆向きに指す参戦要請の待ちが残っている");
        Assert.True(foe.PendingCallToArms is { } kept && kept.Caller == russia && kept.Enemy == me, "続いている戦争の待ちが消えた");
        // 続いている戦争の待ちは月を進めても残る
        foreach (int m in NonQuarterMonths) dip.Step(c, sim.Rng, m);
        Assert.True(dip.Wars.Contains(lasting), "前提が崩れた：続くはずの戦争が月を進めて終わった");
        Assert.True(foe.PendingCallToArms is not null, "続いている戦争の待ちが月を進めて消えた");
    }

    /// <summary>プロイセン（プレイヤー）の緊張を保つ（歩みの中の減衰を打ち消す）。保った組の門が開く（水準以上）ことを確かめる。</summary>
    private static void Hold(Diplomacy dip, Power me, Power foe, Coefficients c)
    {
        double t = dip.Tension(me, foe);
        if (t < 190) dip.AddTension(me, foe, 190 - t, TensionReason.Threat);
        Assert.True(dip.Tension(me, foe) >= c.WarDeclareTension, "前提が崩れた：保った緊張が水準を下回っている");
    }

    [Fact]
    public void 最後通牒の待ちは根拠の門が閉じても消えない_答えずに待ちが消える道は無い()
    {
        var sim = Board(); var dip = sim.Diplomacy; var c = sim.Coef;
        var me = sim.Nations[1].World.SelfPower!;        // プロイセン（プレイヤー）
        var cold = sim.Nations[2].World.SelfPower!;      // フランス＝緊張が水準未満（根拠が消えている）
        me.IsPlayer = true;
        Assert.True(dip.Tension(me, cold) < c.WarDeclareTension, "前提が崩れた：開幕の緊張が水準を下回っていない");
        me.PendingWar = cold;
        // 四半期の末（3・6・9・12 月）を含めて 1 年歩んでも、置き換える相手が居なければ待ちはそのまま（AI の組は時計が満ちた月に即開戦する＝プレイヤーだけが免れる道を作らない）
        for (int m = 1; m <= 12; m++) dip.Step(c, sim.Rng, m);
        Assert.True(me.PendingWar == cold, "根拠の門が閉じただけで最後通牒の待ちが消えた（答えずに待ちが消える道がある）");
    }

    [Fact]
    public void 根拠の閉じた待ちは_門が開いて時計が満ちた別の組で置き換わる()
    {
        var sim = Board(); var dip = sim.Diplomacy; var c = sim.Coef;
        var me = sim.Nations[1].World.SelfPower!;        // プロイセン（プレイヤー）
        var cold = sim.Nations[2].World.SelfPower!;      // フランス＝根拠が消えた待ちの相手
        var hot = sim.Nations[3].World.SelfPower!;       // ロシア＝緊張が水準以上（門が開く）
        me.IsPlayer = true;
        me.PendingWar = cold;
        for (int i = 0; i < c.WarTriggerMonths - 1; i++)
        {
            Hold(dip, me, hot, c); dip.Step(c, sim.Rng, NonQuarterMonths[i]);
            Assert.True(me.PendingWar == cold, "時計が満ちる前に待ちが置き換わった");   // 時計が満ちるまでは何も動かない
        }
        Hold(dip, me, hot, c); dip.Step(c, sim.Rng, NonQuarterMonths[c.WarTriggerMonths - 1]);
        Assert.True(me.PendingWar == hot, "門が開いて時計が満ちたのに、根拠の消えた待ちが枡を塞いだまま（塞がれの解消）");
        Assert.False(dip.IsAtWar(me, hot));   // 最後通牒＝プレイヤーの組は宣戦でなく待ちになる
        // 置き換わった待ちは、門が開いているあいだ歩んでも残る
        Hold(dip, me, hot, c); dip.Step(c, sim.Rng, 1);
        Assert.True(me.PendingWar == hot, "門が開いている最後通牒の待ちが歩みで動いた");
    }

    [Fact]
    public void 根拠のある待ちは別の有効な組に取られない_空の枡には積まれる()
    {
        // 対照：枡が空なら、門が開いて時計が満ちたイギリスの組が積まれる（イギリスの組の門が開く盤面であることの証拠）
        var simA = Board(); var dipA = simA.Diplomacy; var cA = simA.Coef;
        var meA = simA.Nations[1].World.SelfPower!; var brA = simA.Nations[0].World.SelfPower!;
        meA.IsPlayer = true;
        for (int i = 0; i < cA.WarTriggerMonths; i++) { Hold(dipA, meA, brA, cA); dipA.Step(cA, simA.Rng, NonQuarterMonths[i]); }
        Assert.True(meA.PendingWar == brA, "前提が崩れた：空の枡にイギリスの最後通牒が積まれない（この盤面ではイギリスの組の門が開かない）");

        // 本体：枡にはイギリス（門が開いている）。ロシアの門も開いて時計が満ちるが、有効な待ちは取られない（組の走査はイギリスの組が先・ロシアの組が後＝後の組が上書きする実装なら赤になる）
        var sim = Board(); var dip = sim.Diplomacy; var c = sim.Coef;
        var me = sim.Nations[1].World.SelfPower!; var br = sim.Nations[0].World.SelfPower!; var ru = sim.Nations[3].World.SelfPower!;
        me.IsPlayer = true;
        me.PendingWar = br;
        for (int i = 0; i < c.WarTriggerMonths + 2; i++)
        {
            Hold(dip, me, ru, c); Hold(dip, me, br, c);
            dip.Step(c, sim.Rng, i < NonQuarterMonths.Length ? NonQuarterMonths[i] : 1);
        }
        Assert.True(me.PendingWar == br, "根拠のある（門が開いている）最後通牒の待ちが別の組に取られた");
    }

    [Fact]
    public void AIの組には最後通牒の待ちは積まれず_プレイヤーの待ちは別の組の走査で動かない()
    {
        var sim = Board(); var dip = sim.Diplomacy; var c = sim.Coef;
        var me = sim.Nations[1].World.SelfPower!; var hot = sim.Nations[3].World.SelfPower!;
        var a = sim.Nations[0].World.SelfPower!; var b = sim.Nations[2].World.SelfPower!;
        me.IsPlayer = true;
        me.PendingWar = hot;
        dip.AddTension(me, hot, 190, TensionReason.Threat);
        double cold = dip.Tension(a, b);
        Assert.True(cold < c.WarDeclareTension, "前提が崩れた：AI どうしの組の緊張が水準を下回っていない");
        dip.Step(c, sim.Rng, 1);   // AI どうしの組（英×仏）の門は閉じている＝プレイヤーの待ち（プロイセン×ロシア）には触れない
        Assert.True(me.PendingWar == hot, "AI どうしの組の走査でプレイヤーの有効な待ちが動いた");
        Assert.True(a.PendingWar is null && b.PendingWar is null && hot.PendingWar is null, "AI の国に最後通牒の待ちが積まれた");
    }

    // ===== 侵略の近接の緊張の標的の座標（G3・G4）=====

    private static MultiNationSim TwoNations()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        return sim;
    }

    private static Power InvasionTarget(MultiNationSim sim)
        => sim.Diplomacy.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && sim.Diplomacy.RankOf(p) == PowerRank.Minor && sim.Diplomacy.Suzerain(p) is null)!;

    /// <summary>侵略を 1 回行い、攻者から見て（屈服したか・近接の理由の緊張の和〔攻者×仏〕・Invade の戻り値）を返す。屈服は攻者を圧倒的に強く、拒否は標的の屈服線の下に置いて作る。
    /// 標的は抽象標的（植民地を持つ弱小）＝屈服すると植民地が攻者の州へ移り <c>ColonyList</c> が空になる。</summary>
    internal static (bool Submitted, double Proximity, bool Ret, MultiNationSim Sim, Power Target) InvadeOnce(bool submit, double flag, double range = 1_000_000)
    {
        var sim = TwoNations();
        var w = sim.Nations[0].World; var me = w.SelfPower!; var fr = sim.Nations[1].World.SelfPower!; var c = w.Coef;
        var target = InvasionTarget(sim);
        Assert.NotNull(target);
        c.ProximityTensionPointsBeforeSubmit = flag;
        c.ProximityTensionRange = range;   // フランスが標的の勢力圏に入る距離にする（増分が正になる盤面）
        w.ExpansionWill = 1e12; w.FiscalFundingRatio = new Share(1.0);
        target.Military = 100;
        me.Military = submit ? 1e9 : c.InvadeMilitaryFactor * target.Military * 1.001;   // 拒否＝挑める最小の戦力（屈服線 3×標的 に届かない）
        int[] goods = [Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId];
        foreach (int g in goods) w.Region.Markets[g].Stock = 1e6;
        Assert.True(w.CanInvade(target, out string why), $"前提が崩れた：侵略できない（{why}）");
        double force = sim.Diplomacy.ColonizeForce(me, target, c), line = sim.Diplomacy.SubmitLine(target, c);
        Assert.Equal(submit, force >= line);   // 前提＝屈服の枝／拒否の枝が狙いどおり選ばれる盤面
        Assert.True(target.ColonyList.Count > 0 && target.Nation is null, "前提が崩れた：標的が植民地を持つ抽象標的でない");
        bool ret = w.Invade(target);
        double prox = 0;
        foreach (var (reason, v) in sim.Diplomacy.TensionBreakdown(me, fr)) if (reason == TensionReason.Proximity) prox += v;
        return (!sim.Diplomacy.IsAtWar(me, target), prox, ret, sim, target);
    }

    [Fact]
    public void 旗0は旧のとおり抽象標的の屈服で積まず_旗1は屈服の前の座標で積む_拒否の枝は旗に依らない()
    {
        // 旧（旗 0）：抽象標的は屈服で植民地が攻者の州へ移り座標が空になる＝屈服の枝は 0・拒否の枝（植民地が標的に残る）だけ正。旗 1：座標を屈服の前に取る＝屈服の枝も積む。拒否の枝は座標が動かない＝旗に依らず同じ。
        var (subm0, p0, r0, _, _) = InvadeOnce(submit: true, flag: 0);
        var (subm0r, pr0, rr0, simR0, tgtR0) = InvadeOnce(submit: false, flag: 0);
        var (subm1, p1, r1, _, _) = InvadeOnce(submit: true, flag: 1);
        var (subm1r, pr1, rr1, simR1, tgtR1) = InvadeOnce(submit: false, flag: 1);
        Assert.True(subm0 && subm1); Assert.False(subm0r); Assert.False(subm1r);   // 屈服は戦争にならず、拒否は戦争になる
        Assert.True(simR0.Diplomacy.IsAtWar(simR0.Nations[0].World.SelfPower!, tgtR0));
        Assert.True(simR1.Diplomacy.IsAtWar(simR1.Nations[0].World.SelfPower!, tgtR1));
        Assert.Equal(0.0, p0);                     // 旗 0 の屈服＝旧の欠陥（植民地を接収した後に座標を取る）の対照。旗を焼き込むときはこの行を書き直す
        Assert.True(pr0 > 0, "旗 0 の拒否の枝は旧のとおり積む（対照）");
        Assert.True(p1 > 0, "旗 1 の屈服の枝は積む（機構を踏んだ証拠）");
        Assert.Equal(pr0, pr1, 9);                 // 拒否の枝は旗に依らず同じ量（座標が屈服の前後で動かない）
        Assert.Equal(pr0, p1, 9);                  // 同じ標的の座標・同じ列強＝枝が違っても積む量は同じ（座標を屈服の前に取った分）
        Assert.True(r0 && rr0 && r1 && rr1, "Invade の戻り値は旗にも枝にも依らず true（実行したか）");
    }

    [Fact]
    public void Subjugateの戻り値は屈服したかで_拒否は戦争になる()
    {
        foreach (bool submit in new[] { true, false })
        {
            var sim = TwoNations();
            var w = sim.Nations[0].World; var me = w.SelfPower!; var c = w.Coef;
            var target = InvasionTarget(sim);
            target.Military = 100;
            me.Military = submit ? 1e9 : c.InvadeMilitaryFactor * target.Military * 1.001;
            Assert.Equal(submit, sim.Diplomacy.ColonizeForce(me, target, c) >= sim.Diplomacy.SubmitLine(target, c));   // 前提
            bool ret = sim.Diplomacy.Subjugate(me, target, c);
            Assert.Equal(submit, ret);
            Assert.Equal(!submit, sim.Diplomacy.IsAtWar(me, target));   // 別の述語（交戦の有無）から作った期待
        }
    }

    [Fact]
    public void 旗1の屈服の枝の積む量は標的の勢力圏までの距離で減衰する()
    {
        // 範囲を地球上の距離（最大 約 2 万 km）の桁にする。増分 = 最大 × (1 − 距離/範囲) の線形：2 つの範囲の増分から距離を逆算して 2 つが一貫することを見る（距離の幾何は写さない）
        double max = new Coefficients().ProximityTensionMax;
        double a = InvadeOnce(submit: true, flag: 1, range: 25_000).Proximity;
        double b = InvadeOnce(submit: true, flag: 1, range: 50_000).Proximity;
        Assert.True(a > 0 && b > a, $"距離減衰の向きが崩れた（範囲 2.5 万 km={a}・5 万 km={b}）");
        double kmA = 25_000 * (1 - a / max), kmB = 50_000 * (1 - b / max);
        Assert.InRange(kmA, 0, 20_100);   // 地球上の最大距離（約 2 万 km）以内
        Assert.Equal(kmA, kmB, 3);        // 同じ盤面＝同じ距離（線形の式が崩れれば 2 つの逆算がずれる）
    }
}

/// <summary>(440) 侵略の計器 <c>invade_s</c>：屈服か・積んだ量・旧の規則なら積んだはずの量（G5）。静的な切り替えを使うので他の試験と並べない。</summary>
[Collection("Instruments")]
public class InvadeInstrumentTests
{
    [Fact]
    public void 侵略の表は屈服か拒否かと積んだ量と旧の規則での量を行に残す()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_invade_" + Guid.NewGuid().ToString("N"));
        try
        {
            Instruments.Configure(dir, null);
            var rows = new List<string[]>();
            string[] header = [];
            foreach (bool submit in new[] { true, false })
            {
                var sim = TwoNationsForInstrument();
                var w = sim.Nations[0].World; var me = w.SelfPower!; var c = w.Coef;
                Instruments.Phase(sim, 1789UL, "open");
                var target = sim.Diplomacy.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && sim.Diplomacy.RankOf(p) == PowerRank.Minor && sim.Diplomacy.Suzerain(p) is null)!;
                c.ProximityTensionPointsBeforeSubmit = 1; c.ProximityTensionRange = 1_000_000;
                w.ExpansionWill = 1e12; w.FiscalFundingRatio = new Share(1.0);
                target.Military = 100; me.Military = submit ? 1e9 : c.InvadeMilitaryFactor * target.Military * 1.001;
                foreach (int g in new[] { Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId }) w.Region.Markets[g].Stock = 1e6;
                Assert.True(w.CanInvade(target, out string why), why);
                w.Invade(target);
                Instruments.Close(sim);
                var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "invade_s1789.tsv"));
                header = lines[0].Split('\t');
                rows.Add(lines[1].Split('\t'));
                Assert.Equal(2, lines.Length);   // 侵略 1 回＝データ行 1
                Assert.Equal(9, header.Length);
            }
            int iNat = Array.IndexOf(header, "targetIsNation"), iSub = Array.IndexOf(header, "submitted"), iAdd = Array.IndexOf(header, "proximityAdded"), iWould = Array.IndexOf(header, "proximityWouldHave");
            int iAddT = Array.IndexOf(header, "addedToTarget"), iWouldT = Array.IndexOf(header, "wouldHaveToTarget");
            Assert.True(iNat >= 0 && iSub >= 0 && iAdd >= 0 && iWould >= 0 && iAddT >= 0 && iWouldT >= 0, "表の列の名前が足りない");
            Assert.Equal("1", rows[0][iSub]); Assert.Equal("0", rows[1][iSub]);
            Assert.Equal("0", rows[0][iNat]); Assert.Equal("0", rows[1][iNat]);   // どちらも抽象標的
            double D(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(D(rows[0][iAdd]) > 0, "旗 1 の屈服の行の積んだ量が正でない");
            Assert.Equal(0.0, D(rows[0][iWould]));   // 旧の規則（屈服の後の座標）では抽象標的の屈服は 0＝旗 1 の新しい反応
            Assert.True(D(rows[1][iAdd]) > 0, "拒否の行の積んだ量が正でない（拒否の枝は旗 1 でも積む）");
            Assert.Equal(D(rows[1][iAdd]), D(rows[1][iWould]), 9);   // 拒否の枝は座標が動かない＝積んだ量＝旧の量
            Assert.Equal(D(rows[0][iAdd]), D(rows[1][iAdd]), 9);     // 同じ座標・同じ列強＝枝が違っても同じ量
            Assert.Equal(0.0, D(rows[0][iAddT])); Assert.Equal(0.0, D(rows[1][iAddT]));   // 抽象標的は列強でない＝標的自身の組には積まれない
            Assert.Equal(0.0, D(rows[0][iWouldT])); Assert.Equal(0.0, D(rows[1][iWouldT]));
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 標的が実在の国なら標的自身の組にも満額が積まれ_計器の列に分けて残る()
    {
        // 標的が実在の弱小国（スウェーデン）＝その首都が攻者の勢力圏の走査でも標的の座標でもある（距離 0）＝標的自身の組へ満額（ProximityTensionMax）が積まれる。
        // 期待は係数の定義値（満額 × (1 − 0/範囲)＝満額）から作る＝走査の幾何を写さない。第三国（フランス）の分は標的自身の分を引いた残り。
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_invade_nat_" + Guid.NewGuid().ToString("N"));
        try
        {
            Instruments.Configure(dir, null);
            foreach (bool submit in new[] { true, false })
            {
                var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("スウェーデン")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
                sim.StepQuarter();
                var w = sim.Nations[0].World; var me = w.SelfPower!; var c = w.Coef;
                Instruments.Phase(sim, 1789UL, "open");
                var target = sim.Diplomacy.Powers.Find(p => p.Name == "スウェーデン")!;
                Assert.True(target.Nation is not null && sim.Diplomacy.RankOf(target) == PowerRank.Minor, "前提が崩れた：標的が実在の弱小国でない");
                c.ProximityTensionRange = 25_000;   // 地球上のどの組も範囲に入る
                w.ExpansionWill = 1e12; w.FiscalFundingRatio = new Share(1.0);
                target.Military = 100; me.Military = submit ? 1e9 : c.InvadeMilitaryFactor * target.Military * 1.001;
                foreach (int g in new[] { Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId }) w.Region.Markets[g].Stock = 1e6;
                Assert.True(w.CanInvade(target, out string why), why);
                Assert.Equal(submit, sim.Diplomacy.ColonizeForce(me, target, c) >= sim.Diplomacy.SubmitLine(target, c));   // 前提＝枝が狙いどおり
                w.Invade(target);
                Instruments.Close(sim);
                var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "invade_s1789.tsv"));
                var header = lines[0].Split('	'); var row = lines[1].Split('	');
                double D(string name) => double.Parse(row[Array.IndexOf(header, name)], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal("1", row[Array.IndexOf(header, "targetIsNation")]);
                Assert.Equal(c.ProximityTensionMax, D("addedToTarget"), 9);        // 距離 0＝満額
                Assert.True(D("proximityAdded") >= D("addedToTarget"), "合計が標的自身の分を下回る");
                Assert.Equal(D("proximityAdded"), D("proximityWouldHave"), 9);     // 座標が動かない枝＝旗 0 でも積んだ量＝旧の量
                Assert.Equal(D("addedToTarget"), D("wouldHaveToTarget"), 9);
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static MultiNationSim TwoNationsForInstrument()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        return sim;
    }
}
