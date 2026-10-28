namespace PriceOfBread.SimCore;

/// <summary>
/// 国家（プレイヤー or AI）の意思決定インターフェース。四半期ごとに介入レバー（§3.2）を
/// 操作する。プレイヤーと AI は同じ問題を解くため同一インターフェースを共有する（§8）。
/// </summary>
public interface INationAgent
{
    void DecideQuarter(World world);
}

/// <summary>何もしない放置エージェント（§8 開発ループの第一段＝null agent）。</summary>
public sealed class NullAgent : INationAgent
{
    public void DecideQuarter(World world) { /* 介入しない */ }
}

/// <summary>
/// 近代化派AI（決定論のテスト/デモ用 archetype）：公教育・高教育費・適度な所得税で財源を作り、治安維持力を緩めて改革を容認、
/// 近代化に応じて参政権を**普通選挙まで積極的に押す**（利益集団ゲートが制限選挙で頭打ちにするのを検証する用途＝PolicyPoliticsTests）。
/// 実 selfplay の AI は <see cref="UtilityAgent"/>（効用最大化・教義は支配ブロックから創発）に統合済み。反動派 archetype は UtilityAgent の
/// 軍事/均衡スタンスに吸収されたため削除（倍率排除⑧：散在するハードコード archetype を効用フレームへ統合）。
/// </summary>
public sealed class ModernizerAgent : INationAgent
{
    private const double IncomeTaxTarget = 0.25;          // 富裕層に課税して財源（議会が強いと上げにくい）
    private const double CoercionTarget = 200;            // 治安維持力を緩め改革を容認（旧支配層は縮小に抵抗）
    private const double SuffrageTechThreshold = 0.25;    // 制限選挙へ踏み出す技術水準

    public void DecideQuarter(World w)
    {
        var p = w.Policy;
        p.EducationLaw = EducationLaw.Public;       // 世俗の公教育＝エリート創出
        p.EducationSpendingTarget = Government.EducationSpendingTop;   // ★錨2026-09-18＝教育費の教義最大（Modernize と同値）
        p.IncomeTaxTarget = IncomeTaxTarget;
        p.CoercionTarget = CoercionTarget;
        if ((w.Coef.TechThresholdOff > 0 || w.TechLevel > SuffrageTechThreshold) && p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
        if (w.Polity != Polity.Absolutist && p.Suffrage < Suffrage.AdultMale) p.Suffrage = Suffrage.AdultMale;
    }
}
