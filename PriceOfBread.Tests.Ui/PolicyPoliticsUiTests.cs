using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>PolicyPoliticsTests のうち App の画面部品（LawRowVM）を読む 1 本を、画面の試験のプロジェクトへ移した物（エンジン側の補助は PolicyPoliticsTests の internal を読む）。</summary>
public class PolicyPoliticsUiTests
{
    /// <summary>(h') 法律タブのホバーは門の理由をそのまま言う（2026-09-21 ㉖）。憲法級の否決（賛成不足＝硬い純支持&gt;0）に
    /// 「政治資本消費:n　不足」を出さない＝政治資本をいくら貯めても開かない門を政治資本の不足と言わない。
    /// 在庫の門の否決は従来どおり必要量＋不足。表示は <c>LawRowVM.StageHint</c>＝法律タブが実際に出す1本。</summary>
    [Fact]
    public void S1_法律タブのホバーは憲法級の否決を政治資本の不足と言わない()
    {
        var sim = PolicyPoliticsTests.CapitalBoard(); var w = sim.World;
        w.PoliticalCapitalMax = 1e9; w.MandateSpent = 0;   // 在庫は潤沢＝止めているのが在庫でないことを盤面で保証する
        string[] labels = ["0", "1", "2", "3", "4", "5", "6", "7"];
        string Line2(PolicyAxis a, int level) =>
            new PriceOfBread.App.ViewModels.LawRowVM(w, a, "法", labels, () => PolicyPoliticsTests.LawLevel(w, a), () => PolicyPoliticsTests.LawLevel(w, a), _ => { })
                .StageHint(level)!.Split('\n')[1];
        // ① 憲法級（参政権）の否決
        (bool Inc, int Level)? opp = null;
        foreach (bool inc in new[] { true, false })
        {
            int level = Government.NextStage(w, PolicyAxis.Suffrage, inc);
            if (Government.GateInfo(w, PolicyAxis.Suffrage, inc, level).Reason == Government.ReasonNoMandate) { opp = (inc, level); break; }
        }
        Assert.True(opp != null, "テスト前提が崩れた：憲法級の否決（賛成不足）を作れていない");
        string constitutional = Line2(PolicyAxis.Suffrage, opp!.Value.Level);
        Assert.Equal($"制定:不可（{Government.ReasonNoMandate}）", constitutional);
        Assert.DoesNotContain("政治資本", constitutional);
        // ② 在庫の門の否決（非憲法級・残りが代価の半分）＝必要量と「不足」
        var (a2, _, l2, cost) = PolicyPoliticsTests.FindOpposedStep(w);
        w.PoliticalCapitalMax = cost * 0.5; w.MandateSpent = 0;
        string stock = Line2(a2, l2);
        Assert.StartsWith("制定:不可（政治資本消費:", stock);
        Assert.EndsWith("　不足）", stock);
    }
}
