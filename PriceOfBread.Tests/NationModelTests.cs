using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 国家モデルの拡充（仏・日・英・米・露・普・オスマン）。同じ94財カタログを階級・識字・法・政体で差別化。
/// 専制君主制（ツァーリ／啓蒙専制）が初期政体として使われ、教育の差が技術（革新）の差を生む。
/// </summary>
public class NationModelTests
{
    private static NationConfig[] Seven() =>
    [
        NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America(),
        NationConfig.Russia(), NationConfig.Prussia(), NationConfig.Ottoman(),
    ];

    [Fact]
    public void 七国家は並列に動き各国が保存する()
    {
        var sim = new MultiNationSim(Seven(), seed: 1789);
        double[] base0 = [.. sim.Nations.Select(n => n.World.BaseMoney)];
        var ex = Record.Exception(() => sim.Run(300));
        Assert.Null(ex);
        // P6点火（2026-07-17・清の初期不満0.25）で300Q内に分離独立が起きうる＝Nations が7超に成長し、当事国は建国時に金と人口が新国家へ移転して
        // 基準線が引き直される（エンジンの Invariants は RebaselineInvariants で追随済み＝Run が例外なしで走った時点で保存は検証されている）。
        // 本テストの t=0 基準の素朴な式は「分離を経験していない元の7カ国」にのみ適用する。
        for (int i = 0; i < base0.Length; i++)
        {
            var w = sim.Nations[i].World;
            if (w.HadSecession) continue;
            Assert.True(Math.Abs(w.BaseMoney - base0[i] - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued) < 1e-6 * base0[i], $"{w.Region.Name} 保存破れ");
        }
    }

    [Fact]
    public void 専制君主制が初期政体として使われる()   // ロシア＝ツァーリ専制。プロイセン/オスマン＝絶対君主制（D5：プロイセンは啓蒙絶対主義＝Absolutist。Despoticは過小評価だった）
    {
        Assert.Equal(Polity.Despotic, new Simulation(NationConfig.Russia().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Absolutist, new Simulation(NationConfig.Prussia().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Absolutist, new Simulation(NationConfig.Ottoman().ToScenario(1), new NullAgent()).World.Polity);
    }

    [Fact]
    public void 教育で技術が分かれる_公教育は革新_宗教教育は停滞()   // プロイセン(公)≫ロシア(宗教＋農奴)
    {
        // 判定は**前改革期＝300四半期（1789〜1864）**で行う（監査2026-07-12 接2の有効化に伴う変更・ユーザー裁定「物理が正しくテストが間違い」）：
        // 間接税（関税・専売）が予算基数に入ったことで、高関税の専制ロシアも1870年代以降に行政→経済→公教育採択の遅い近代化が走る＝アレクサンドル2世改革期の史実そのもの。
        // 「教育の差が技術の差を生む」という設計の柱は、宗教教育のままの前改革期で検証する（400Qまで見ると露の遅い離陸が差を縮めるのは正しい挙動）。
        // 第4段4-4（2026-07-27）：陸続き判定の是正（大陸一致）で普露が正しく陸接し、2カ国世界では勢力均衡が無く露の侵攻が普を潰す
        //（普0.33 vs 露0.41＝教育でなく戦争の帰結を測ってしまう）。主題＝教育→技術の隔離のため外交凍結（ColonialPopTests と同作法）。
        var sim = new MultiNationSim([NationConfig.Prussia(), NationConfig.Russia()], seed: 1789) { DiplomacyFrozen = true };
        sim.Run(300);
        double prussia = sim.Nations[0].World.TechLevel;
        double russia = sim.Nations[1].World.TechLevel;
        Assert.True(prussia > russia * 2, $"公教育のプロイセン({prussia:F2})が宗教教育のロシア({russia:F2})を大きく上回るはず（前改革期1789-1864）");
    }
}
