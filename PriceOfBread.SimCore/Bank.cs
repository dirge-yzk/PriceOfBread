namespace PriceOfBread.SimCore;

/// <summary>
/// 銀行（§2.5.1）。基礎貨幣（金）の準備の上に信用創造で広義マネー（預金）を生む。
/// 通貨は貸出で増え（信用創造）、返済・収縮で減る＝内生的貨幣供給。金は採掘・貿易なしの最小核では一定。
/// </summary>
public sealed class Bank
{
    public double Gold;              // 基礎貨幣（準備）。保存される土台。**導出量＝独立に書くな**（Credit.Step が毎日 BaseMoney×(1−退蔵率) で無条件上書き＝単一ソースはBaseMoney。CurrencyHoldingRatio=0.1 で点火済みだが Gold は依然 BaseMoney 導出＝独立ストック化は未実施）
    public MoneyStock Loans;             // 与信残高 ＝ 信用創造された広義マネー（= 預金 − 金）
    /// <summary>家計向け与信の残高（POPの個人債務・設計 docs/20260802・2026-08-02）。★<see cref="Loans"/> とは**別ストック**。
    /// ⚠★分ける理由が本設計の急所：<c>Credit.Step</c> は**目標追従の制御器**で、家計が返済して <see cref="Loans"/> が
    ///   減っても目標（BaseMoney起点）は不変なので、**半減期1.16年で機械的に貸し直す**。しかも再創造された貨幣は
    ///   返済した家計ではなく**首都州の投資家POP**へ渡る。同じストックに載せるとデレバレッジが取り消され、
    ///   実体は「家計→資本家への遅延移転」に化け、**負債デフレが原理的に起きない**。
    /// ★制御器は従来どおり <see cref="Loans"/> だけを見る（無改造）。<c>World.OutstandingLoans</c> の定義だけを拡張して
    ///   BaseMoney の恒等式へ算入する＝貸出も返済も ΔBaseMoney=0 が閉じる。既定0で厳密ビット恒等。</summary>
    public MoneyStock HouseholdLoans;
    public double Confidence = 1.0;  // 貸す意欲（好況で↑・ショックで↓）。ブーム/バストの駆動
    public double Heat;              // 金融過熱度＝与信使用率（Loans÷上限・0..1）。高いほどバスト前夜＝恐慌の予兆ゲージ（ガラス箱）

    /// <summary>★家計の与信の貸しと返しの1本（2026-09-29 S3b）＝POP の債務の記録（<see cref="Pop.Debt"/>）・預金（Savings）・貨幣（<see cref="HouseholdLoans"/>）を
    /// 同じ額で書く＝ΔBaseMoney＝0。呼び手＝輸入の為替手形（BilateralMarket＝清算の始めの決済と振り出し）・消費者信用と返済規律（休眠 PopDebtDriven）。
    /// <paramref name="amount"/> は正（貸し）か負（返し）。<paramref name="w"/>＝POP の国（その国の銀行が貸し手）。
    /// ★2026-09-30 段4g：預金と与信は移し方の1本（<see cref="World.Transfer"/>・世界の外の側＝家計の与信 <see cref="World.Outside.HouseholdCredit"/>）が書く
    /// ＝債務の記録は動いた額（返しは貯蓄まで＝呼び手はどれも貯蓄までに切ってから呼ぶ）。旧＝ここで貯蓄と与信を手で書いた。</summary>
    internal static void Lend(World w, Pop p, double amount)
    {
        double moved = amount > 0
            ? World.Transfer((null, null), (w, [p], [1.0]), amount, outside: World.Outside.HouseholdCredit)
            : -World.Transfer((w, [p]), (null, null, null), -amount, outside: World.Outside.HouseholdCredit);
        p.Debt += new MoneyStock(moved);
    }

    /// <summary>★債務は債務者について行く（2026-09-29 S3b）＝POP が国をまたいで移る（割譲・建国・再併合・越境移民・招聘）とき、
    /// 運ぶ債務 <paramref name="debt"/> の分の家計の与信を受け入れ国の銀行へ移す（null＝建国：新しい国の銀行は構築で自国の POP の債務から始まる）。
    /// ⚠呼び手は同じ額を持参金の台帳から引く（運ぶ基礎貨幣＝貯蓄−債務）＝国ごとの保存が閉じる。
    /// ★2026-09-30 段4e（人と動くお金の換算）：受け入れ国の銀行には受け入れ国の通貨へ換算した額 <paramref name="debtIn"/>（POP の債務の換算と同じ値・呼び手が
    /// <see cref="World.CrossFx"/> で換算）。省略＝同じ通貨（建国＝新しい国の為替は親の値から始まる）。</summary>
    internal void Carry(Bank? to, MoneyStock debt, MoneyStock? debtIn = null)
    {
        HouseholdLoans -= debt;
        if (to is not null) to.HouseholdLoans += debtIn ?? debt;
    }
}
