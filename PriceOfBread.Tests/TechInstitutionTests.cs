using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 研究システム＝技術の連続創発（カード廃止・§2.8）＋制度の施行（カードに代わる離散選択）。
/// 技術は社会（エリート・自由・教育）から連続で伸び、多国では世界世界の技術水準へ収束する（後発の優位）。
/// 宗教教育の社会はエリートが乏しく追いつけない（永遠の二番手）。制度は社会の閾値で解禁・施行される。
/// </summary>
// ⚠2026-08-26：構築時の植民地州昇格（ColoniesAreRegions=1 が既定）を落として検証する。
//   このクラスの検証対象は**制度の閾値**（自由＋公教育＋エリート→特許）であって植民地の政治ではない。
//   昇格を入れると英の人口が倍増し（植民地11州）、軌道が変わって政変→ブルジョワ独裁→国営報道→Freedom 0.5 となり、
//   **閾値の機構としては正しく**特許が入らない（instprobe 実測2026-08-26・SuffrageScopeGate=1 でも不変＝ブロック集計の歪みではなく
//   植民地経済で強くなった近代ブロックの創発）。制度の式を検証するには政治環境を固定するのが正しい。
public class TechInstitutionTests
{
    private static NationConfig[] Four() =>
        [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];

    /// <summary>主張＝「自由な報道と公教育を持つ国は、世界の技術水準へ届く（停滞帯の床 t&gt;1.10 を越え、宗教教育に固定した国の1.3倍を超える）」。
    /// これは**機構**の主張（<see cref="TechEducation.GrowTech"/> の革新項は 自由≥閾値 × 公教育 × 報道の自由 で開く）なので、
    /// 報道法・自由・公教育を**試験の中で固定**した世界で検証する。
    /// <para>★書き直し（2026-09-21 ㉖・政治資本を唯一の門として既定化した回）。旧と新を並べる（床 1.10・相対 ×1.3・300四半期・同じ4カ国と seed は不変）：</para>
    /// <para>旧＝「統治AI（UtilityAgent）が治める英・米（公教育）は t&gt;1.10 かつ 宗教固定の対照×1.3 を越える」。英の報道法と政体は AI の選択に任されていた
    ///   ＝**AI が自国の報道を検閲へ戻すかどうか**という創発に依存する（規律＝xUnit は機構と不変条件だけ・創発の主張は帯が見る）。
    ///   実測（2026-09-21 ㉖の診断）：AI が治めた英は余白の門 θ の有無に関係なく最後はブルジョワ独裁・国営報道・自由0.5 に至り、違いは検閲を買う時期だけ
    ///   （θ あり q125 → 在庫の門 q50）。技術はそこで凍る（1.201 → 1.091）。⚠統治しない対照（全国 NullAgent）の英も q25→q50 にブルジョワ独裁・国営報道へ落ち、
    ///   その 1.110 は終盤の後追い（米の先端への採用）の値＝自由報道の世界の値ではない。</para>
    /// <para>新＝「報道法＝自由報道・教育法＝公教育・参政権＝初期値（自由＝参政権＋報道法の導出なので自由も固定される）を毎四半期書き戻した英・米は
    ///   t&gt;1.10 かつ 宗教固定の対照×1.3 を越える」。統治する主体は置かない（全国 <see cref="NullAgent"/>＝政体・報道法を動かす AI がいない）。
    ///   英・米の議会は止める（宗教固定の日本と同じ分離手法。旧は議会の綱領も報道法・教育法の目標を書き換えた＝2026-09-26 に撤去＝回5-1・今は議席の純支持を切る分離）。
    ///   ⚠弱めていない：床も相対の倍率も期間も旧のまま。旧が「AI が自由を保てば届く」の合成だったのを、「自由と公教育があれば届く」の機構そのものへ絞った
    ///   ＝入力（自由・公教育）を保証したうえで出力（技術）だけを見る。AI が早く検閲を買う挙動は帯の観察事項（99_実装ログ 2026-09-21 ㉖）。</para></summary>
    [Fact]
    public void 公教育の自由国は世界の技術水準へ到達する()   // 英・米＝初期から公教育（早い近代化）
    {
        // アンカー再設計（#131・2026-07-25）：絶対閾値1点（1.5→1.45と2度器差調整）はカオス軌道のノイズに脆く、水力レシピ追加（1.507→1.493）・
        // 綿花気候ゲート（1.493→1.369）と**機構的に正しい変更のたびに割れた**。仕様の実体は「公教育＋自由の国は、宗教教育に固定された国を
        // 大きく引き離して伸びる（停滞しない）」＝**宗教固定の対照との相対**で検証し、絶対値は停滞の下限だけを置く。
        var cfg = Four();
        var sim = new MultiNationSim(cfg, seed: 1789, c => c == cfg[2] || c == cfg[3] ? new FreePressPublicEducationPin() : new NullAgent(),
            coef: new Coefficients { ColoniesAreRegions = 0 });   // ⚠環境固定（クラス冒頭の注記）
        sim.Nations[2].World.ParliamentEnabled = false;   // 議会を止める（宗教固定の日本と同じ分離手法。綱領は 2026-09-26 に撤去）
        sim.Nations[3].World.ParliamentEnabled = false;
        // 前提の番人＝固定が効いている（効いていなければ主張の入力が崩れている）。
        // ★回1-1（2026-09-26）で**全ての四半期の終わり**を見る形へ（旧＝300四半期の終わりの1点だけ）。
        //   固定は四半期の頭に書き戻し、技術の伸び（QuarterEnd の GrowTech）は政治の段と講和より先に走る（下の FreePressPublicEducationPin の注記）
        //   ＝その四半期に政体が替わった国は、法が伸びの後に動いた＝その四半期だけ数えない（次の四半期の頭に書き戻しが入る）。
        //   ⚠旧の1点は「最後の四半期に政変が無い軌道」を暗に仮定していた＝回1-1 の軌道（1858年＝最後の四半期に英が講和の後に絶対王政へ）で
        //     伸びの入力は固定されていたのに赤になった。新は残りの全ての四半期（旧の1点を含む）で固定を確かめる。
        var polity = new[] { sim.Nations[2].World.Polity, sim.Nations[3].World.Polity };
        int pinChecked = 0; var pinBroken = new System.Collections.Generic.List<string>();
        for (int q = 0; q < 300; q++)
        {
            sim.StepQuarter();
            for (int k = 0; k < 2; k++)
            {
                var w = sim.Nations[2 + k].World;
                bool regimeChanged = w.Polity != polity[k]; polity[k] = w.Polity;
                if (regimeChanged) continue;
                pinChecked++;
                if (w.Censorship != CensorshipLaw.FreePress || w.EducationLaw != EducationLaw.Public
                    || !(System.Math.Abs(Politics.FreedomFromLaws(w.Suffrage, w.Censorship) - w.Freedom) <= 1e-12))
                    pinBroken.Add($"Q{q + 1} {w.SelfPower?.Name} 報道={w.Censorship} 教育={w.EducationLaw} 自由={w.Freedom:F3}");
            }
        }
        Assert.True(pinBroken.Count == 0, $"固定が崩れた四半期 {pinBroken.Count}件（政体の替わった四半期を除く）：" + string.Join("／", pinBroken.Take(5)));
        Assert.True(pinChecked >= 2 * 300 * 9 / 10, $"政体の替わった四半期が多すぎて固定を確かめられない（{pinChecked}/{2 * 300}）");
        var pinned = RunWithJapanPinnedReligious(300);   // 対照＝宗教教育に固定した日本（同seed・同期間の別世界）
        double japanReligious = pinned.Nations[1].World.TechLevel;
        double britain = sim.Nations[2].World.TechLevel, america = sim.Nations[3].World.TechLevel;
        // 期待値再導出（#17・2026-08-08作者許可）：絶対床1.25は軌道ピン化していた（R3安定形＝潤沢判定の是正で
        // 米1.224と髪一重で割れる一方、相対条件×1.3は通過＝仕様の実体は相対側）。#131の設計どおり
        // 「相対＝仕様・絶対＝破綻検出の床」へ純化＝床は停滞帯(≈0.9-1.0)の上・1.10に置く。
        Assert.True(britain > 1.10, $"イギリス（自由報道＋公教育に固定）が停滞水準にある t={britain:F3}");
        Assert.True(america > 1.10, $"アメリカ（自由報道＋公教育に固定）が停滞水準にある t={america:F3}");
        Assert.True(britain > japanReligious * 1.3, $"イギリス（自由報道＋公教育に固定）が宗教固定の対照を引き離せていない 英={britain:F3} 対照={japanReligious:F3}");
        Assert.True(america > japanReligious * 1.3, $"アメリカ（自由報道＋公教育に固定）が宗教固定の対照を引き離せていない 米={america:F3} 対照={japanReligious:F3}");
    }

    /// <summary>統治の固定（試験用・2026-09-21 ㉖）：毎四半期の頭で報道法＝自由報道・教育法＝公教育・参政権＝初期値を**状態と目標の両方**へ書き戻し、
    /// 自由をその場で導き直す（<see cref="Politics.FreedomFromLaws"/>＝エンジンと同じ1本）。他の政策には触れない＝<see cref="NullAgent"/> と同じ。
    /// ⚠四半期末の技術の伸び（<c>QuarterEnd</c> の GrowTech）は政治の段（Politics.Step）より先に走る＝政変が四半期末に法を動かしても、
    /// 次の伸びの前にこの書き戻しが入る。</summary>
    private sealed class FreePressPublicEducationPin : INationAgent
    {
        private Suffrage? _suffrage;
        public void DecideQuarter(World w)
        {
            _suffrage ??= w.Suffrage;
            w.Suffrage = w.Policy.Suffrage = _suffrage.Value;
            w.Censorship = w.Policy.Censorship = CensorshipLaw.FreePress;
            w.EducationLaw = w.Policy.EducationLaw = EducationLaw.Public;
            w.Freedom = Politics.FreedomFromLaws(w.Suffrage, w.Censorship);
        }
    }

    // 2026-07-10更新：収束是正（CatchupAbsorptionBlend）後の軌道では日本が途中で明治化＝革命→議会与党の綱領が
    // 公教育へ転換する（新基準＝忠実度7割では**望ましい**挙動・batch30で日本exact73%）。機構の意図＝
    // 「宗教教育に**留まる**国は追いつけない」を検証するため、日本の議会を無効化（梯子テストと同じ分離手法）＝
    // NullAgent世界では教育目標が初期値（宗教）のまま動かず、綱領上書きによる転換だけを封じる（政変・経済は生きたまま）。
    // ★2026-09-26（回5-1 項目17）：綱領は撤去＝議会はもう目標を書かない。分離は議席の純支持を切る分として残す。
    private static MultiNationSim RunWithJapanPinnedReligious(int quarters)
    {
        var sim = new MultiNationSim(Four(), seed: 1789, coef: new Coefficients { ColoniesAreRegions = 0 });
        sim.Nations[1].World.ParliamentEnabled = false;
        // ★(159) 盤面の前提（主張は同じ）：物を生む行の必要性が動力（労働力の財）の平滑化した値段÷参照価格になり、この4カ国の閉じた世界では動力が参照を超えず
        //   誰も蒸気機関を必要としない（1843年まで）＝工場が石炭を焚かず英米の技術水準が 1.04・1.20 で凍る（r154 の盤面は4カ国とも開幕から蒸気機関を知り英 2.18）。
        //   この番人の主題は制度（特許・株式会社）と宗教教育＝蒸気機関は開幕から4カ国に与える（r154 の盤面と同じ前提）。
        foreach (var n in sim.Nations) n.World.Discovered[Discovery.SteamRow] = true;
        sim.Run(quarters);
        return sim;
    }

    [Fact]
    public void 宗教教育の国は公教育国に追いつけず停滞する()   // §2.8 永遠の二番手・エリート律速
    {
        var sim = RunWithJapanPinnedReligious(400);
        double japan = sim.Nations[1].World.TechLevel;    // 宗教教育（寺子屋）＝エリートが乏しい
        double britain = sim.Nations[2].World.TechLevel;  // 公教育
        Assert.True(japan < britain * 0.6,
            $"宗教教育の日本({japan:F2})が公教育のイギリス({britain:F2})に追いつけてしまう（エリート律速の破れ）");
    }

    /// <summary>★2026-10-01 (152)：制度は発見の表の行（特許・株式会社）＝点数＝技術力＋必要性＋伝わり（設計 §1A-6）。
    /// 旧の主張「宗教教育の日本に特許が入らない」は旧の閾値（中核エリート 0.4）だけで決まる世界の主張＝新しい形では取引のある国から伝わる（史実の日本の特許法 1885＝米欧から学んだ）。
    /// 残る主張＝宗教教育の国は**自力では**特許を見つけない（中核エリートが薄い＝必要性が技術力70と合わせても門に届かない）・公教育の英は見つける・株式会社は技術力で開く。</summary>
    [Fact]
    public void 制度は社会の発展で施行され宗教国は特許を自力で見つけない()
    {
        var sim = RunWithJapanPinnedReligious(400);
        var uk = sim.Nations[2].World; var jp = sim.Nations[1].World;
        Assert.True(uk.PatentLaw, "イギリス（自由＋公教育）は特許を施行するはず");
        var tj = Discovery.Evaluate(jp, null, jp.Coef, includeKnown: true)[Discovery.PatentRow];
        Assert.True(tj.Tech + tj.Need < Discovery.GateScore, $"宗教教育の日本が特許を自力で見つけられる（技術力 {tj.Tech:F1}＋必要性 {tj.Need:F1}・中核エリート {jp.EliteCore:F3}）");
        Assert.True(uk.JointStock, $"工業化したイギリスに株式会社が入らない（技術水準 {uk.TechLevel:F2}）");
        // (154)：旧の主張（工業化した日本に株式会社が入る）を戻した＝(152) は主語を英へ替えていた（理由の記録なし）・合わせた木の400四半期で日本も入る（技術水準 0.197）。
        Assert.True(jp.JointStock, $"工業化した日本は株式会社が入るはず（技術水準 {jp.TechLevel:F2}）");
    }

    [Fact]
    public void 単独国でも技術はカードなしで決定論的に伸びる()
    {
        var a = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        var b = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        a.Run(200);
        b.Run(200);
        Assert.True(a.World.TechLevel > 0, "技術が伸びていない");
        Assert.Equal(a.World.TechLevel, b.World.TechLevel);   // 乱数を使わない＝決定論
    }
}
