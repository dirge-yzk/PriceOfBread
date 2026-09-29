using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
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
        // ★添字つきの書き込み（`.StateSpent[(int)TreasuryCat.Army] += …`）も数える（2026-09-28 段2-1＝StateSpent を区分の配列にした＝数え落とさない）。
        var rx = new Regex($@"\.{member}(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))");
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
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束2＝戦後処理の1本）：闘争度 38→**35**・正統性 15→**14**（実数は着手前 36・15）。
    //   内戦の決着を Politics.SettleRevolt の1本へ畳んだ＝旧経路の鎮圧の緩和（全POP）と戦争の形の休眠の後始末（返還州の緩和・正統性の減算）が1行の緩和になり、
    //   休眠の後始末の正統性の減算は旧経路の鎮圧の行に無い＝持ち込まなかった（係数 RevoltAftermathOnWarPath ごと削除）。
    // ★2026-09-28 基準値の更新（＝宣言・処理の一元化 段2 束3＝旧経路の内戦〔第二の戦争エンジン ProcessCivilWar〕と単一州の分離の削除）：
    //   闘争度 35→**32**（旧経路の分離戦の決着の緩和2本〔独立・鎮圧〕と国家分裂の両陣営の緩和1本）・正統性 14→**13**（内戦の消耗 CivilWarLegitimacyDrain）。
    //   首都の蜂起は戦争の形（決着の緩和は SettleRevolt の1本・戦死と民間人の死は対外戦争と同じ1本）。
    [InlineData("Militancy", 32)]      // 力学1・運搬8（式は1本化済み）・事件スパイク約17・緩和約4・初期化ほか（内戦の決着の緩和は SettleRevolt の1本）
    [InlineData("Satisfaction", 4)]    // ⚠プロパティ代入のみ（初期化子は構築＝対象外）
    [InlineData("Legitimacy", 13)]
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
    // ★2026-09-28 段1-3（国庫の口を1本へ）：Treasury 2→0＝外交層の財布 Power.Treasury への代入（同期窓の冒頭・助走明けの同期）を撤去。
    //   国を持つ Power の国庫は World.Treasury（private set）＝口は World.Pay だけ。Power.Treasury は抽象標的の財布（構築時の初期化子だけが書く）。
    // ★2026-09-28 段1-2（按分の本体を1本へ）：Savings 91→83＝World.Distribute（旧 Fiscal.Distribute）へ畳んだ複製＝Colonial.DistributeToPops 2・
    //   World.DistributeToVoters 2・GiveGold 1・信用の利息の配当 1・海上保険の受け手側 2。
    // ★2026-09-28 段1-5（重複の削除）：Savings 83→79＝Military.MoveCohort の3枝（徴兵・復員・通常）のお金の運搬を末尾の1か所へ。
    // ★2026-09-28 E6 の2歩目（基礎貨幣が負の国の返せない与信の償却）：数は変えない。CumulativeFiatIssued 3 のまま＝償却の非常発行は
    //   Simulation.BackstopNegativeSavings の末尾の1行（銀行の無い世界の③と同じ行）。Loans 6 のまま＝償却の1行（+1）と、
    //   Credit.Step の与信の増減（投資家の枝と全POPへ注入する枝の同じ2行）を1行へ（−1）。
    // ★2026-09-29 段2-2・2-3（保存の穴と他人から取る・端数と式の結合順＝処理の一元化 段4a）：Savings 79→**47**・CumulativeTradeFlow 33→**31**（＝宣言）。
    //   移し方の1本 World.Transfer（旧 MultiNationSim.TransferToPops を World へ移して一般化・+1）へ畳んだ＝CapitalMarket.TakeGold／GiveGold の本体・関税と補助
    //   （旧 InternationalMarket.CollectTariff／PaySubsidy を削除）・港湾料（旧 CollectPortFee を削除）・運河の出資・招聘の支度金・対外借入の貸し手の引き落とし。
    //   按分の本体 World.Distribute（符号つき・微小な額も配る）へ畳んだ＝与信の注入と回収（旧 Credit.InjectMoney を削除）・貿易の仲介料と残余・国債の持ち手・
    //   窃盗・年金・恐怖政治・十分の一税・港の建設労務・生産者のいない在庫の代金（輸送の買い付け）。再点火の代金は月内の仮勘定（Paid）へ（建設の材料と工賃の Paid の2行を1行へ＝Paid は 9 のまま）。
    //   銀行の無い世界の底打ち（最富裕→全員から取る・作者裁定 Q11 で撤去）。CumulativeFiatIssued は3のまま（非常発行の行＝負の国の与信の償却だけになった）。
    // ★2026-09-29 段3-1（貿易＝処理の一元化 段4b）：Savings 47→**43**・CumulativeTradeFlow 31→**30**（＝宣言）。Earned は11 のまま。
    //   貿易の純流入を国の全POP の貯蓄比に配る Apply（TradeFlow 1）・輸出代金の持ち主への直書き（旧 PayExportToProducers・Savings 1）・輸送財の買い手の引き落とし2行（Transport）を削除＝
    //   輸入代金・関税・運賃・売り手の受取・仲介料・運賃の受け手は全て移し方の1本（World.Transfer）へ。按分の本体 World.Distribute の書き込みを1行へ（端数は最後の受け手＝同じ式・Savings −1）＋
    //   所得（Earned）も同じ行で書く引数（月次の精算の後の受け取り＝Earned +1）・十分の一税の所得の別の行を畳んだ（Earned −1）。
    // ★2026-09-29 高速化の段 S3b（輸入の為替手形）：Savings 43→**42**・HouseholdLoans 4→**3**（＝宣言）。家計の与信の貸しと返しを Bank.Lend の1本へ
    //   （消費者信用の貸し・返済規律の返し・手形の振り出しと決済＝旧は消費者信用と返済規律がそれぞれ Savings と HouseholdLoans を直に書いた）＋
    //   POP が国をまたぐときの与信の移し替えを Bank.Carry の1本へ（割譲・建国・再併合・越境移民・招聘）。⚠Bank の中の書き込みは `.` の無い形＝数の規則の外（1本の移し方そのもの）。
    // ★2026-09-30 段3-2（財を買わない国家支出）：Savings 42→**38**・CumulativeTradeFlow 30→**27**（＝宣言）。植民地投資・捕囚市場・港の建設の代金と拿捕を
    //   ④の持分（InternationalMarket.PaySales＝移し方の1本）へ＝投資家の貯蓄の直の引き落とし（植民地投資の2行・港の1行）・捕囚市場の投資家への直の払い・
    //   捕囚市場と拿捕の台帳の手書き（3行）が消えた。Power.Treasury（抽象標的の財布）は欄ごと削除（代入0のまま）。
    [InlineData("Savings", 38)]               // POP の貯蓄（段3の終わりに Pop の中の3つへ閉じる）
    // ★2026-09-28 段2-1（国家の支払い率）：StateSpent 25→24＝区分つきの配列へ（添字つきの書き込みを数える）・月次のゼロ化は Array.Clear（代入でない）。
    //   FiscalFundingRatio（国家の支払い率 r）を足した＝書き手は月初の1か所（Simulation）＋ランナーの盤面の復元（GovernanceSnapshot）の2つ＝増やさない。
    [InlineData("StateSpent", 24)]            // 州の未精算の国家支出（区分つき＝TreasuryCat の配列）
    [InlineData("FiscalFundingRatio", 2)]     // 国家の支払い率 r（月初の1か所＋盤面の復元）
    [InlineData("CumulativeTradeFlow", 27)]   // 世界の台帳・国境をまたいだ出入り（移し方の中の1か所へ畳む）
    [InlineData("CumulativeFiatIssued", 3)]   // 不換の発行と回収（2）＋非常発行（1＝負の国の与信の償却だけ・銀行なしの底打ちは 2026-09-29 段2-2 で撤去）
    [InlineData("CumulativeGoldMinted", 2)]   // 鋳造と準備の売却
    [InlineData("Loans", 6)]                  // 銀行の与信（企業・損失の負債化・負の国の償却）
    [InlineData("HouseholdLoans", 3)]         // 銀行の与信（家計）＝Bank.Carry の受け手・再併合の合算・盤面の復元（貸しと返しは Bank.Lend の1本）
    [InlineData("Paid", 9)]                  // 月内の仮勘定（払い）
    [InlineData("Earned", 11)]                // 月内の仮勘定（受け取り）
    [InlineData("Treasury", 0)]               // 外交層の財布への代入（World.Treasury は private set＝構造で閉じている・Power.Treasury は 2026-09-30 に欄ごと削除）
    public void お金への書き手は増やさない(string member, int baseline)
    {
        int n = CountWriters(member);
        Assert.True(n <= baseline,
            $"{member} への書き手が {n} 箇所（基準 {baseline}）＝**増えている**。" +
            "支払いは1本の移し方を通すこと（計画 §10-12）。" +
            "どうしても必要なら基準値を上げて理由をこのファイルに書く（＝宣言）。");
    }

    // ★2026-09-30（作者裁定「計算量の予算」＝推奨で採択）：**計算量の番人**＝1種（seed 1）・40四半期（助走を含む＝バッチと同じ道
    //   SelfPlayMatch.Run(lean)）の処理ごとの「見た件数」（CostLedger）が宣言した表を超えないこと。書き手の数の番人と同じく、
    //   **件数が増えるコミットは表を上げて理由をここに書く（＝宣言）**＝予算の超過はどこで払ったかを実装ログとコミットに書く。
    //   ・許しの幅 +2%＝軌道の小さな揺れ（係数・式の変更で軌道が動くと件数も少し動く）で毎回表を書き換えずに済む幅。
    //   ・表より 10% を超えて下がったら落ちる＝表を下げる（＝改善の宣言）。緩んだ表を残すと、後で同じだけ増えても番人が鳴らない。
    //   ⚠40四半期では時代とともに育つ費用（従属10倍・成約2.5倍・工事中2倍）が見えない＝全長の件数はバッチの測り（measure.sh の <出力>.cost）で見る。
    //   ⚠件数は診断（セーブに載らない・軌道を変えない）＝下の「セーブに載らない」の番人と対。
    private const double CostSlackUp = 0.02, CostSlackDown = 0.10;
    private static readonly Lazy<long[]> Cost40 = new(() => SelfPlayMatch.Run(1, 40, null, lean: true).Cost ?? new long[CostLedger.Count]);

    [Theory]
    // 表＝2026-09-30 の実測（宣言・計器なしの全長の報告と同じ道）。
    // ★2026-09-30 表の更新（＝改善の宣言・計算量の予算 第1回＝恒等の手）：
    //   価格の財の歩 22,505,040→15,536,007（死んだ財を一覧で回さない＝Region.LiveGoods）・建設 19,761,052→28,231（工事中の一覧＝Region.BuildSites）・
    //   消費 97,521,840→29,300,830（必要の無い財の POP のループを飛ばす）・請求 9,037,980→9,688（国営持分の請求の一覧＝Region.StateSites）・
    //   覇権の判定 7,460,493→1,706,531（陣営戦力の閾値は同盟の候補が居るときだけ・従属の離反は安い条件から）。他の10本は1件も変わらない（軌道が同じ）。
    // ★2026-09-30 表の更新（＝改善の宣言・計算量の予算 第2回＝近似の手）：軌道が変わる（近似）＝15本とも今の件数へ置き直した。
    //   清算の着値の充填 44,882,844→35,263,928（−21.4%＝月の清算で不足だけで起きた行のうち、前の走査で成約せず自州価格が上がっていない行を眠らせる）・
    //   価格の財の歩 15,536,007→14,629,209（−5.8%＝供給も在庫も無く張り付いた財は不動点に着いた旬から月の残りを眠る）・日次の写像の回 40,223,878→39,615,736（−1.5%）。
    //   ほかは軌道の違いの揺れ（建設 +0.6%・清算の買い手行 −0.6%・覇権の判定 −2.9%・請求 −0.1%・消費 −0.01%・残り7本は同じ）。
    // ★2026-09-30 表の更新（(122) 合流の前段＝線2〔(118) 幽霊の国の根・(119) 助走は法を動かさない〕を載せた木＝軌道が動いた分の宣言・新しい処理は無い）：
    //   建設 28,392→13,571・請求 9,682→4,710（下がった＝表を下げる）・覇権の判定 1,657,020→2,300,741（+39%＝開幕の法と同盟の軌道が変わり、
    //   同盟の候補が居る月が増えた＝処理の形は同じ）。推定の単位では 40四半期で +6.7ms（単価 0.01035µs）＝全長の予算の内／超過は合わせた main の10種の .cost で見る（実装ログ (122)）。
    [InlineData(CostLedger.PriceGoods, 14629209L)]
    [InlineData(CostLedger.PriceDailyRuns, 39615736L)]
    [InlineData(CostLedger.ConstructionSeen, 13571L)]
    [InlineData(CostLedger.CreditSpans, 33138L)]
    [InlineData(CostLedger.ConsumePopGoods, 29298230L)]
    [InlineData(CostLedger.ProducerMonths, 9037980L)]
    [InlineData(CostLedger.ClaimProducers, 4710L)]
    [InlineData(CostLedger.MonthlyPops, 706680L)]
    [InlineData(CostLedger.ClearingRows, 5577638L)]
    [InlineData(CostLedger.ClearingFills, 35263928L)]
    [InlineData(CostLedger.DiploPowerMonths, 9906L)]
    [InlineData(CostLedger.DiploHegemonScans, 2300741L)]
    [InlineData(CostLedger.QuarterStartPops, 235560L)]
    [InlineData(CostLedger.QuarterEndPops, 235560L)]
    [InlineData(CostLedger.SharedNationQuarters, 3682L)]
    public void 計算量の見た件数は宣言した表を超えない(int item, long declared)
    {
        long n = Cost40.Value[item];
        string name = CostLedger.Names[item];
        Assert.True(n <= declared * (1 + CostSlackUp),
            $"計算量「{name}」が {n} 件（宣言 {declared}・許し +{CostSlackUp:P0}）＝**増えている**。" +
            "増やすなら表を上げ、どこで払ったか（他の処理を削った分）をこのファイル・実装ログ・コミットに書く（＝宣言）。" +
            "新しい処理なら docs の「処理の刻みの表」に行を足し、刻みの根拠を書く。");
        Assert.True(n >= declared * (1 - CostSlackDown),
            $"計算量「{name}」が {n} 件（宣言 {declared}）＝{CostSlackDown:P0} を超えて**下がった**＝表を {n} へ下げて理由を書く（＝改善の宣言）。");
    }

    [Fact]
    public void 計算量の台帳はセーブに載らない()
    {
        // 台帳は作業領域（[NonSerialized]）＝SaveGame が辿らない。ロードした盤面の台帳は空（null）から数え直す＝件数がセーブの中身と往復の一致に触れない。
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        Assert.Contains(sim.Nations, n => n.World.CostOrNull is { } c && c.Any(v => v > 0));   // 前提＝数えている
        Assert.True(sim.CostTotals().Sum() > 0);
        using var ms = new MemoryStream();
        SaveGame.Save(sim, ms);
        ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        Assert.All(sim2.Nations, n => Assert.Null(n.World.CostOrNull));
        Assert.Null(sim2.Diplomacy.CostOrNull);
        Assert.Null(sim2.Market.CostOrNull);
        Assert.Equal(0L, sim2.CostTotals().Sum());
    }
}
