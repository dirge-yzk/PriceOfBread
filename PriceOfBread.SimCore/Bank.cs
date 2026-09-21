namespace PriceOfBread.SimCore;

/// <summary>
/// 銀行（§2.5.1）。基礎貨幣（金）の準備の上に信用創造で広義マネー（預金）を生む。
/// 通貨は貸出で増え（信用創造）、返済・収縮で減る＝内生的貨幣供給。金は採掘・貿易なしの最小核では一定。
/// </summary>
public sealed class Bank
{
    public double Gold;              // 基礎貨幣（準備）。保存される土台。**導出量＝独立に書くな**（Credit.Step が毎日 BaseMoney×(1−退蔵率) で無条件上書き＝単一ソースはBaseMoney。CurrencyHoldingRatio=0.1 で点火済みだが Gold は依然 BaseMoney 導出＝独立ストック化は未実施）
    public double Loans;             // 与信残高 ＝ 信用創造された広義マネー（= 預金 − 金）
    /// <summary>家計向け与信の残高（POPの個人債務・設計 docs/20260802・2026-08-02）。★<see cref="Loans"/> とは**別ストック**。
    /// ⚠★分ける理由が本設計の急所：<c>Credit.Step</c> は**目標追従の制御器**で、家計が返済して <see cref="Loans"/> が
    ///   減っても目標（BaseMoney起点）は不変なので、**半減期1.16年で機械的に貸し直す**。しかも再創造された貨幣は
    ///   返済した家計ではなく**首都州の投資家POP**へ渡る。同じストックに載せるとデレバレッジが取り消され、
    ///   実体は「家計→資本家への遅延移転」に化け、**負債デフレが原理的に起きない**。
    /// ★制御器は従来どおり <see cref="Loans"/> だけを見る（無改造）。<c>World.OutstandingLoans</c> の定義だけを拡張して
    ///   BaseMoney の恒等式へ算入する＝貸出も返済も ΔBaseMoney=0 が閉じる。既定0で厳密ビット恒等。</summary>
    public double HouseholdLoans;
    public double Confidence = 1.0;  // 貸す意欲（好況で↑・ショックで↓）。ブーム/バストの駆動
    public double Heat;              // 金融過熱度＝与信使用率（Loans÷上限・0..1）。高いほどバスト前夜＝恐慌の予兆ゲージ（ガラス箱）
}
