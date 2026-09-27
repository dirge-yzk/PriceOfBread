using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★ラチェット（思想⑫・docs/202608/20260828_悪い形の全数調査と休眠のトリアージ_部分完了.md §14-4・2026-08-29）：
/// 心理量（闘争度・生活水準・正統性・政治意識）への**書き手の数**を不変条件にする。
///
/// ── なぜ要るか ──────────────────────────────────────────────────────────
/// 闘争度には書き手が39箇所あり（力学1・運搬8・事件スパイク約17・緩和約10・初期化2）、
/// 「どこで動いたか」を誰も説明できない量になっていた。リファクタ（記憶＋noisy-or）を待たずに
/// **悪化だけは今日から止める**＝書き手を増やす変更はこのテストを割る＝増やすなら基準値の更新を**宣言**させる。
///
/// ── 計数規則 ────────────────────────────────────────────────────────────
/// SimCore の .cs 全走査で「`.量名` への代入・複合代入」を数える（`==` 比較は除外）。
/// ⚠この規則は設計docの実測（2026-08-28＝闘争度39・正統性15・政治意識1）を**正確に再現**する。
///   生活水準だけ doc は7＝オブジェクト初期化子（`new Pop { Satisfaction = … }`）込みの数え方＝
///   ここでは**プロパティ代入のみ**（初期化子は構築であって副作用ではない）＝4 を基準にする。
/// ⚠数を**減らす**変更（統合・削除）はそのまま通る（上限であって固定値ではない）＝ラチェットの向き。</summary>
public class WriterRatchetTests
{
    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    private static int CountWriters(string member)
    {
        // 代入・複合代入のみ（`==` は除外）。`=>`（ラムダ/式形メンバ）も除外。
        var rx = new Regex($@"\.{member}\s*(\+=|-=|\*=|/=|=(?![=>]))");
        int n = 0;
        foreach (var f in Directory.EnumerateFiles(SimCoreDir(), "*.cs"))
            foreach (var line in File.ReadLines(f))
            {
                var code = line;
                int c = code.IndexOf("//", StringComparison.Ordinal);
                if (c >= 0) code = code[..c];   // コメント内の言及は数えない
                if (rx.IsMatch(code)) n++;
            }
        return n;
    }

    [Theory]
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
    [InlineData("Militancy", 38)]      // 力学1・運搬8（式は1本化済み）・事件スパイク約17・緩和約8・初期化ほか＋段2の返還州の緩和1
    [InlineData("Satisfaction", 4)]    // ⚠プロパティ代入のみ（初期化子は構築＝対象外）
    [InlineData("Legitimacy", 15)]     // ＋段2の内戦鎮定の流血1
    [InlineData("Consciousness", 1)]   // ★これが手本（書き手1本＝力学だけ）
    public void 心理量への書き手は増やさない(string member, int baseline)
    {
        int n = CountWriters(member);
        Assert.True(n <= baseline,
            $"{member} への書き手が {n} 箇所（基準 {baseline}）＝**増えている**。" +
            "事件スパイクを足すのではなく、力学（生活苦・政治距離）か記憶（docs/202608 §14-7）へ流すこと。" +
            "どうしても必要なら基準値を上げて理由をこのファイルに書く（＝宣言）。");
    }

    [Theory]
    // ★2026-09-28 支払いの一元化 段1-0（作者裁定 F4・計画 docs/202609/20260926/20260926_外部レビューの検討.md §10-12-8-2）：
    //   お金の口座と世界の台帳への**書き手の数**を不変条件にする。計数規則は上と同じ（代入・複合代入・コメントを除く・初期化子は数えない）。
    //   基準値＝2026-09-28 の実測（d3dc133）。段1〜段3で支払いを1本の移し方へ寄せるたびに下げる（＝宣言）。増やす変更は、1本の移し方を通すこと。
    // ★2026-09-28 段1-1（死んだ支払いの枝の削除）：Savings 94→91・StateSpent 26→25・TradeFlow 34→33・Paid 10→9・Earned 14→11
    //   ＝建設工賃の旧経路2（資本家・国家）・植民地の採取決済（TradeFlow 1・植民地の蓄え 1・植民地交易益の傾斜配分 Earned 3）。
    [InlineData("Savings", 91)]               // POP の貯蓄（段3の終わりに Pop の中の3つへ閉じる）
    [InlineData("StateSpent", 25)]            // 州の未精算の国家支出（段2-1で区分つきへ）
    [InlineData("CumulativeTradeFlow", 33)]   // 世界の台帳・国境をまたいだ出入り（移し方の中の1か所へ畳む）
    [InlineData("CumulativeFiatIssued", 3)]   // 不換の発行と回収（2）＋銀行なしの底打ち（1＝段2-2で消える）
    [InlineData("CumulativeGoldMinted", 2)]   // 鋳造と準備の売却
    [InlineData("Loans", 6)]                  // 銀行の与信（企業・損失の負債化）
    [InlineData("HouseholdLoans", 4)]         // 銀行の与信（家計）
    [InlineData("Paid", 9)]                  // 月内の仮勘定（払い）
    [InlineData("Earned", 11)]                // 月内の仮勘定（受け取り）
    [InlineData("Treasury", 2)]               // 外交層の財布 Power.Treasury への代入（World.Treasury は private set＝構造で閉じている）
    public void お金への書き手は増やさない(string member, int baseline)
    {
        int n = CountWriters(member);
        Assert.True(n <= baseline,
            $"{member} への書き手が {n} 箇所（基準 {baseline}）＝**増えている**。" +
            "支払いは1本の移し方を通すこと（計画 §10-12）。" +
            "どうしても必要なら基準値を上げて理由をこのファイルに書く（＝宣言）。");
    }
}
