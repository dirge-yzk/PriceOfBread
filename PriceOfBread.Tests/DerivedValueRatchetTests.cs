using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1364) 導いた値のラチェット（作者 2026-11-25「許可する入力数・出力数を規則として規定するのはどうだろうか。具体的な数は任せる」）。
/// 7 つの導いた値（闘争度・正統性・厭戦・政治意識・満足度・不満度・天命）の**書き手・入力・出力の数**が、今の数の表を超えないこと。
/// 数え方は <see cref="ValueScan"/> の 1 本（エンジンの本体＝SimCore・Instruments と表示と試験は数えない）。
///
/// ── 目標と今の表 ──
/// 目標（AI の推奨・作者「具体的な数は任せる」）＝書き手 1・入力 3・出力 3。今の数はそれを大きく超える＝**今の数の表だけが猶予**（例外の枠は作らない）。
/// 減らす変更はそのまま通る（上限であって固定値ではない）。減らしたら表を下げる（＝宣言）。増やす変更は赤＝増やすなら因果の最長の道へ寄せる（設計思想⑪）。
/// ⚠入力は機械で数えた下限に偏る＝局所変数は 1 項に畳む（式が局所変数に入る量は、局所変数の数がそのまま入力の数）。文の指紋と手数えの表は置いていない。
/// ⚠不満度は欄でなく関数 FactionSnapshot.DiscontentOf＝書き手の欄が無い（0）・関数の呼び出しを読みとして数える。
/// ⚠追加の量は後の波の番人の線が足す（最初の対象は上の 7 つ）。
/// 表の数は合流の木で数え直す（ut が .Value を、lb が書き手の周りの行を同じ波で動かす）。</summary>
public class DerivedValueRatchetTests
{
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    /// <summary>(1373) 表の検査の本体（人工の行の自己試験 M12 も同じ入口を呼ぶ）。書き手はドットつき＋ドットなし。超えた項目の名を返す（空なら緑）。</summary>
    internal static string[] Violations(System.Collections.Generic.IReadOnlyList<string> texts, string spec, bool isFunction, int writers, int inputs, int outputs)
    {
        string name = ValueScan.Parse(spec).Member;
        int w = isFunction ? 0 : texts.Sum(t => ValueScan.CountAllWriters(t, spec));
        int i = isFunction ? 0 : ValueScan.CountInputs(texts, spec);
        int o = isFunction ? texts.Sum(t => ValueScan.CountCalls(t, name)) : texts.Sum(t => ValueScan.CountReads(t, spec));
        string hint = $"（{spec}：書き手 {w}/{writers}・入力 {i}/{inputs}・出力 {o}/{outputs}・目標 1/3/3）＝増やすなら導く元を 1 本へ寄せる（設計思想⑪）。減らしたら表を下げる。";
        var v = new System.Collections.Generic.List<string>();
        if (w > writers) v.Add("書き手が表を超えた" + hint);
        if (i > inputs) v.Add("入力が表を超えた" + hint);
        if (o > outputs) v.Add("出力が表を超えた" + hint);
        return v.ToArray();
    }

    // ── 旧 WriterRatchetTests「心理量への書き手は増やさない」の経緯（(1373) でこの表へ寄せた＝数え方を ValueScan の 1 本に）──
    // 基準値＝2026-08-29 実測（docs §14-4 のベースラインと一致）。**増えたら落ちる**。
    // 減らしたらこの数字を下げること（＝改善の宣言）。増やす変更は原則禁止＝
    // 新しい事件スパイクを足すのではなく、力学（生活苦・政治距離）か記憶（§14-7）へ流すこと。
    // ★2026-08-29 段1実施＝運搬8箇所の**式**を Military.BlendCarry 1本へ集約（厳密恒等リファクタ・clampの有無はsiteごとに保存）。
    //   ⚠計数は「代入サイト」なので39のまま＝この数が減るのは段2/6/7（スパイクの削除・記憶への置換）から。
    // ★2026-09-08 基準値の更新（＝宣言・F3b蜂起doc §3-4 段2）：闘争度 39→40・正統性 15→16。
    //   ⚠**新しい事件スパイクではない**。内戦一元化（CivilWarAsWar=1.0）で内戦の主経路が War へ移ったのに、
    //   その再併合サイト（MultiNationSim.ApplyReintegrations）にだけ後始末が無く、旧経路（Politics.cs の
    //   ProcessCivilWar 決着）が既に書いている**同じ2つの規則**（返還州の闘争度×ReformMilitancyRelief／
    //   正統性−RevoltSuppressLegitCost）が live 経路で抜け落ちていた＝規則を増やしたのではなく、
    //   同じ規則の**二つ目の経路**に同じ1行を置いた（RevoltAftermathOnWarPath=0 で休眠）。
    //   ★この+2は旧経路（首都発の単国シム経路）を畳む段6で消える＝そのとき 39/15 へ戻すこと。
    // ★2026-09-11 基準値の更新（＝宣言・要求は法の経路の点火）：闘争度 40→**38**。書き手が2つ消えた＝
    //   F1 州一揆の枝の緩和 ×ReformMilitancyRelief（作者裁定「弾圧は鎮めない・譲歩が鎮める」＝鎮圧は掛けない・譲歩の緩和は施行の時点の Step 先頭の1行へ）と、
    //   別軸譲歩（本丸を拒んで副次要求を直書きする旧枝）の緩和＝旧枝ごと物理削除。正統性 16→**15**＝同日の実測（この点火は正統性の書き手を触っていない＝基準が実数より緩かった）。
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束2＝戦後処理の1本）：闘争度 38→**35**・正統性 15→**14**（実数は着手前 36・15）。
    //   内戦の決着を Politics.SettleRevolt の1本へ畳んだ＝旧経路の鎮圧の緩和（全POP）と戦争の形の休眠の後始末（返還州の緩和・正統性の減算）が1行の緩和になり、
    //   休眠の後始末の正統性の減算は旧経路の鎮圧の行に無い＝持ち込まなかった（係数 RevoltAftermathOnWarPath ごと削除）。
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束3＝旧経路の内戦〔第二の戦争エンジン ProcessCivilWar〕と単一州の分離の削除）：
    //   闘争度 35→**32**（旧経路の分離戦の決着の緩和2本〔独立・鎮圧〕と国家分裂の両陣営の緩和1本）・正統性 14→**13**（内戦の消耗 CivilWarLegitimacyDrain）。
    //   首都の蜂起は戦争の形（決着の緩和は SettleRevolt の1本・戦死と民間人の死は対外戦争と同じ1本）。
    // ★2026-09-30 基準値の更新（＝宣言・処理の一元化 段4e I＝人の移し方の1本 Pop.Blend）：闘争度 32→**25**＝運搬の書き手8か所（階級の移動3枝・州の中・社会移動・
    //   州の間・越境・招聘）が Blend の1行へ（−7）。政治意識 1→**2**＝運搬の1行（作者裁定 2026-09-28 §14-2 の6「国内の移動でも国民意識・政治意識・思想の分布を運ぶ＝1本の Blend で全部」）。
    //   ⚠事件スパイクではない＝人と一緒に動く量の保存（旧＝移った人の政治意識が着地の POP の値に化けた＝移動のたびに意識が湧くか消えた）。力学の書き手は Politics の1本のまま。
    // 名前・関数か・書き手・入力・出力＝2026-11-25 実測（基 11bfbfcd6・measurements/m1364_20261125_prereg_wr.txt）。
    // (1373) 書き手はドットなしの書き手を含む（正統性 11→12＝World.cs の自分の欄への代入）・闘争度は Pop と Colony を別の行へ（受け手の名で分ける）。
    [Theory]
    [InlineData("Militancy@pop", false, 14, 7, 37)]        // 闘争度（Pop の側・力学 1・運搬 1・事件のスパイク・緩和）
    [InlineData("Militancy@colony", false, 2, 0, 3)]     // 闘争度（Colony の側）
    [InlineData("Legitimacy", false, 12, 10, 47)]       // 正統性（ドットなし 1 を含む）
    [InlineData("WarExhaustion", false, 3, 7, 31)]     // 厭戦（段 1・2 で作り直し予定＝rd の材料）(1385) c147wb 出力 34→31＝厭戦の 3 つの読み〔保持力の戦時の乗数・闘争度の項・天命の項〕の焼き込み
    [InlineData("Consciousness", false, 2, 1, 14)]     // 政治意識（力学 1＋運搬 1）
    [InlineData("Satisfaction", false, 1, 2, 12)]      // 満足度（EconomyStep の 1 行）
    [InlineData("DynasticMandate", false, 2, 1, 4)]    // 天命（World.DynasticMandate・Politics の 2 行）
    [InlineData("DiscontentOf", true, 0, 0, 2)]        // 不満度（関数 FactionSnapshot.DiscontentOf・書き手の欄なし・呼び出し 2）
    public void 導いた値の書き手と入力と出力は表を超えない(string spec, bool isFunction, int writers, int inputs, int outputs)
    {
        var texts = ValueScan.EngineFiles(SimCoreDir()).Select(File.ReadAllText).ToList();
        var v = Violations(texts, spec, isFunction, writers, inputs, outputs);
        Assert.True(v.Length == 0, string.Join(" / ", v));
    }

    /// <summary>(1373・1379) 機械で数えられない入力＝局所変数を通る書き手の右辺と、その局所変数の定義の文の指紋（局所変数の中身が変わっても項の数は動かない＝文面で守る）。
    /// 変えたら宣言して指紋を更新する（その時は局所変数の中身を見て入力が増えていないか確かめる）。</summary>
    [Theory]
    [InlineData("Militancy@pop", "5:caa4320678ac")]
    [InlineData("Militancy@colony", "0:e3b0c44298fc")]
    [InlineData("Legitimacy", "8:d94e04138a65")]
    [InlineData("WarExhaustion", "2:d3723a024ea5")]
    [InlineData("Consciousness", "1:5f3bdc7fec61")]
    [InlineData("Satisfaction", "0:e3b0c44298fc")]
    [InlineData("DynasticMandate", "1:7728e74e495a")]
    public void 局所変数を通る入力は文の指紋で守る(string spec, string fingerprint)
    {
        var texts = ValueScan.EngineFiles(SimCoreDir()).Select(File.ReadAllText).ToList();
        Assert.Equal(fingerprint, ValueScan.LocalFedFingerprint(texts, spec));
    }

    /// <summary>(1373) 自己試験 M12（人工の行・期待は手のリテラル）：入力を 1 つ足すと表の検査が赤・ドットなしの書き手を 1 つ足すと赤・Toward の項の数え方。
    /// 突然変異「CountInputs の Distinct を外す」「BareWriters を空にする」で赤。</summary>
    [Fact]
    public void M12_入力と書き手を足すと表の検査が赤になる()
    {
        string[] one = ["a.M = x.P + y.Q;"];
        Assert.Equal(2, ValueScan.CountInputs(one, "M"));
        Assert.Empty(Violations(one, "M", false, 1, 2, 0));
        string[] plus = ["a.M = x.P + y.Q + z.R;"];
        Assert.Equal(3, ValueScan.CountInputs(plus, "M"));
        Assert.Contains(Violations(plus, "M", false, 1, 2, 0), s => s.StartsWith("入力が表を超えた"));
        // ドットなしの書き手（自分の型の中）
        string[] bare = ["a.M = 1;", "void F() { M = Math.Max(0, M - k.R); }"];
        Assert.Equal(2, ValueScan.CountAllWriters(string.Join("\n", bare), "M"));
        Assert.Contains(Violations(bare, "M", false, 1, 9, 9), s => s.StartsWith("書き手が表を超えた"));
        // 宣言・初期化子・比較は書き手でない
        Assert.Equal(0, ValueScan.CountBareWriters("public double M = 1.0;\nvar q = new Pop { M = 0.1, N = 2 };\nvar r = new Pop\n{\n    M = 0.2,\n};\nif (M == 1) { }", "M"));
        Assert.Equal(2, ValueScan.CountBareWriters("if (z) M += 1;\nelse M = 2;\nfoo(new X()) ; var q = new Pop { N = 2 };", "M"));
        // (1379) 受け手の指定がある量のドットなしの書き手は、その型の宣言の本体の中だけ数える（Pop の中の `Militancy = …` は @pop・Colony の中は @colony・他の型は数えない）
        const string typed = "public sealed class Pop\n{\n    public double M;\n    void F() { M = 1; }\n}\npublic sealed class Colony\n{\n    public double M;\n    void G() { M *= 2; M += 1; }\n}\nclass Other { void H() { M = 3; } }";
        Assert.Equal(1, ValueScan.CountBareWriters(typed, "M@pop"));
        Assert.Equal(2, ValueScan.CountBareWriters(typed, "M@colony"));
        Assert.Equal(1, ValueScan.CountAllWriters(typed, "M@pop"));
        // (1379) 指紋は局所変数の定義の文も入れる：定義を変えると指紋が変わる（右辺は同じ）
        string[] f1 = ["void F() { double tgt = a.P * 2; x.M = Math.Max(0, tgt); }"];
        string[] f2 = ["void F() { double tgt = a.P * 3; x.M = Math.Max(0, tgt); }"];
        Assert.NotEqual(ValueScan.LocalFedFingerprint(f1, "M"), ValueScan.LocalFedFingerprint(f2, "M"));
        // Toward の項：x が自分なら数えない・target は項・k（c.）は数えない
        Assert.Equal(["local:target"], ValueScan.InputTerms("PoliticalQuantities.Toward(p.M, target, c.Speed)", "M").ToArray());
        Assert.Equal(["Level", "local:target"], ValueScan.InputTerms("PoliticalQuantities.Toward(p.Level, target, c.Speed)", "M").OrderBy(s => s, StringComparer.Ordinal).ToArray());
        // 受け手の絞り込み
        Assert.Equal(1, ValueScan.CountWriters("col.M = 1; pop.M = 2;", "M@colony"));
        Assert.Equal(1, ValueScan.CountWriters("col.M = 1; pop.M = 2;", "M@pop"));
        Assert.Equal(1, ValueScan.CountReads("col.M = 1; x = pop.M + 1;", "M@pop"));
    }

    /// <summary>対照：表の量が実際に走査できている（数えた先が空で緑にしない）。書き手のある量は 1 以上・入力と出力も 1 以上。</summary>
    [Fact]
    public void 走査が空でない()
    {
        var texts = ValueScan.EngineFiles(SimCoreDir()).Select(File.ReadAllText).ToList();
        Assert.True(texts.Count >= 50, $"エンジンの .cs が少ない（{texts.Count}）");
        foreach (var m in new[] { "Militancy", "Legitimacy", "WarExhaustion", "Consciousness", "Satisfaction", "DynasticMandate" })
        {
            Assert.True(texts.Sum(t => ValueScan.CountAllWriters(t, m)) >= 1, m + " の書き手が 0＝走査が空");
            Assert.True(texts.Sum(t => ValueScan.CountReads(t, m)) >= 1, m + " の出力が 0＝走査が空");
        }
        Assert.True(texts.Sum(t => ValueScan.CountCalls(t, "DiscontentOf")) >= 1, "DiscontentOf の呼び出しが 0＝走査が空");
    }
}
