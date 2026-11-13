namespace PriceOfBread.SimCore;

/// <summary>
/// (1117) 支配の力の段 C′（設計 design_rule_power_2026-11-12 §3・§8）の共有層。四半期の最終日・全ての国の Close の前の盤面から 1 回だけ作る。
/// </summary>
public sealed partial class MultiNationSim
{
    /// <summary>四半期の最終日・全ての国の StepDayClose の前に 1 回（StepDay から）。</summary>
    void RulePowerQuarterEnd()
    {
        if (Instruments.On) Instruments.RpDesign(this);   // 計器の側の〈D〉（エンジンの関数を呼ばない）
    }
}
