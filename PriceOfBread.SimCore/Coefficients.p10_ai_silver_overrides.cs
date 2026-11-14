using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // ==== 鋼市場AI側 2026-08-16（docs/202608/20260816_鋼市場診断_部分完了.md 修正#1〜#5・実装節＝同docの末尾）====


    public double FascismGate = 0.5;                   // 主フラグ：0でファシズムのイデオロギーシェアが常に0＝恒等／>0で創発の強さ（eraFasc×mil×(反動+ナショ)に乗る＝(640) で dev を消した）
    public double NationBuildMobilizationWeight = 0.5;  // 主フラグ：戦時徴兵の深さ（conscriptFrac）を国民意識でゲート＝大規模動員（nation-in-arms）には高い国民意識が要る
    // --- 国家アイデンティティ（変名・formable・2026-07-12・点火済み2026-07-27）---

    public double PrivilegeLedgerEnabled = 0.0;   // 0＝決着は従来の二値（挑戦力 > 治安維持力+民兵 で反乱勝利）＝厳密恒等／1＝州が陣営を選び、両陣営が州を保持したまま拮抗すれば国が2つに割れる
    public double PrivilegeStripMilitancy = 0.0;   // 0＝加算なし（恒等）
    public double PrivilegeMonopolyInLedger = 0.0;   // 0＝記帳なし＝恒等
    public double LaborFloorOffGate = 2.0;   // 事前登録 measurements/mw469_20261020_prereg.txt
    public double LaborBandFloorOffGate = 1.0;
    public double EmptyPopDebtFirst = 0.0;   // 0＝何もしない（旗 EmptyPopEstate ＞0 のときだけ読む）
    public double TakeFloorPositive = 1.0;   // ⑬ 塵の債務（AI の案・確証なし・事前登録 measurements/m984_20261109_prereg_dd.txt）
    public double LandShareCrownSlot = 1.0;   // 土地の持ち分 B-2b 案 S1（AI の案・確証なし・事前登録 measurements/m978_20261109_prereg_cs.txt）：> 0 かつ Producer.LandShares の長さ 5 のとき
    public double LandShareTaxSale = 1.0;   // 土地の持ち分 B-1 段 1〜2（AI の案・確証なし・事前登録 measurements/m991_20261109_prereg_sl.txt）：> 0 のとき
    public double ColonyPettyEntryEmptyTarget = 1.0;   // （AI の案・確証なし・事前登録 measurements/m1014_20261110_prereg_pe.txt）：> 0 のとき Meritocracy の小営業の入口 ①* で
    public double UbiquitousMineralExempt = 1.0;   // 段0″b（AI の案・確証なし・事前登録 measurements/m1020_20261110_prereg_ub.txt）：> 0 のとき遍在の財
    public double ResourceRowsFix = 1.0;   // 段0″a 資源のデータ（AI の案・確証なし・事前登録 measurements/m1025_20261110_prereg_rd.txt）
    public double EmptyPopEstate = 1.0;   // 0＝何もしない
    public double MinimumWageAiFromFloor = 1.0;   // 0＝今の規則〈本国の都市の割合の線で上げる目標だけを書く〉（UtilityAgent.MinimumWageTargetFromBoard）
    public double GuildLawGate = 0.0;   // 主ゲート＝法が動く（Government/AI目標/要求経路/UI）
    public double GuildRentPremium = 0.0;   // 取り分＝職人の自営レシピの財への上乗せ（買い手価格×これ×法の度合×職人の供給シェア＝消費者→職人・台帳へ）
    public double GuildMobilityDrag = 0.0;   // 歪み＝職人→工場労働者の没落（Meritocracy）が法の度合×これ だけ遅れる（0..1）
    // ==== 内戦を戦争に一元化（docs/202608/20260827_内戦を戦争に一元化する_部分完了.md・2026-08-30実装）＝主フラグ点火済み（FactionJoinMilitancy は段2で削除＝下記）====
    public double FactionRejectionMutation = 0.5;   // 0＝拒否カウンタは積むだけ＝読み手なし＝厳密恒等
    public double FactionMergeRangeKm = 800.0;   // 0＝1命令も走らない＝厳密恒等
    public Share FactionMergeCohesionGain = new(0.5);   // 合流時に勝者へ継ぎ足す結束の割合（FactionMergeRangeKm>0のときのみ読む）
    // --- 王権議会P2＝ラベル導出化（2026-07-11・点火済み）---
    public double ConstitutionalControlFloor = 0.45;   // 立憲化の床：ParliamentControl≥これで立憲/共和ラベル（下賜0.3<床＝弱い議会ではラベル不変＝プロイセン/明治型の自然点）
    public double ConstitutionalControlExit = 0.35;    // 立憲の下抜け（ヒステリシス）：これ以下で専制系へ戻る（墺斯曼1878停止・ちらつき防止）

    // ==== トリレンマを閉じる（2026-08-13・docs/202608/20260812_トリレンマを閉じる_完了.md）====
    public Share CapitalControlOpenness = new(0.25);       // 規制下の資本開放度
    public double CapitalControlPrefScale = 1.0;       // 資本の反対の強さ＝対外保有(CapitalAbroad÷名目GDP)に掛ける倍率
    // ==== 銀本位と大デフレ（2026-08-13・docs/202608/20260812_銀本位と大デフレ.REVERTED_部分完了.md）====
    public double DepositYearsAtBirth = 100.0;    // 初期鉱脈の大きさ＝開始時の産出率で掘り続けて何年分か（3倍掘れば33年で膝に届く）
    public Share DepositGradeKnee = new(0.5);         // 残量がこの割合を切ると品位が線形に低下し始める（浅い高品位層が尽きる点）
    public double DepositGradeFloor = 0.15;       // 掘り尽くしたあとの品位（深部の残滓＝ゼロにしない。閉山は採算側が決める）
    public double BonanzaDepositYears = 60.0;     // 大鉱床の発見が州の鉱脈に足す量（初期率換算の年数・産地の発見 Discovery.OpenSite）
    public double OversightPressCensored = 0.4;   // 検閲付き報道の監視力（自由報道=1.0・国家報道のみ=0.0）
    public double OversightMeritExam = 0.6;       // 科挙型（資格任用）の監視力（専門官僚=1.0・猟官=0.0）
    public double OversightFactorScale = 1.0;   // 1.0＝旧とビット恒等（press自由1.0→noisy-or飽和→汚職0）
    public double RgoScaleSpeedFactor = 0.25;   // 鉱山・畑の規模調整速度の倍率（坑道・開墾＝年単位。時定数は均衡を動かさない＝掃討基準の免除カテゴリ）
    public RatePerMonth MintFloorBuyRatePerMonth = new(0.25);    // 床モードの月次購入率（在庫のこの割合まで/月）
    public RatePerMonth MintReserveSellRatePerMonth = new(0.08); // 法が本位金属を捨てたあと準備を市場へ売る月次率（独1873-79のターラー銀売却＝暴落を深めた投げ売り。売り切りに約1年）
    // ==== 銀本位の史実度（2026-08-21・作者裁定「原因究明し原則に従って修正」→同日点火）====
    public double SilverParityFromRatio = 1.0;   // 1.0 に点火（既定 0.0 → 1.0・波57 c7ag）
    public double SilverParityDemandWeighted = 0.0;   // 全ての州の票の和が正でないときは比が不正の扱い（旧の統計へ倒れる）
    public double MintBuysAtMarket = 1.0;   // 点火2026-09-06＝0.0→1.0・銀本位の再開＝準備ギャップの月割りを市場価格で買う（第2版）
    public double MintParitySells = 0.0;   // ＝閉じる条件が誤り（信用の薄い1783 年から常に真＝造幣が一度も買わない）
    public double MonetaryPeerPullScale = 5.0;   // 【点火5.0・2026-08-21 銀本位ラウンド（上の墓標参照）】
    // ==== 距離定数の時代関数化（掃討doc 第2波・2026-08-13）====
    public double EraEpidemicRange = 1.0;        // 疫病の伝播距離（1817年コレラは陸路で数年・1892年は数週間）
    public double EraTensionRange = 1.0;         // 近接の緊張が届く距離（電信1870s＝遠方の危機が即座に本国へ届く）
    public double EraMigrationScale = 1.0;       // 移民の距離減衰（大西洋横断が6週間→6日）
    public double EraColonyCoal = -0.5;           // 汽船の機関の時代係数（旧名のまま＝植民地維持の距離あたり石炭の時代の改善として置いた・⚠負を入れる＝複式→三段膨張機関で燃費が改善し続けた）

    public double EraFactor(double worldTech, double k) => 1.0;   // 前線技術のランプに沿って距離定数を伸縮させる

    // ==== 国民意識の消費側（国民意識doc N4/N7・2026-08-13）====
    public double SecessionConsciousnessWeight = 1.0;   // 分離圧力に (1 − これ×州の国民意識) を掛ける
    public double MintReserveTargetWeight = 1.0;       // 準備不足に応じて造幣量を絞る強さ
    public double TrilemmaRateResponse = 0.5;          // カバー率の不足に対する金利の応答

    // ==== 社会移動リアリティ監査 2026-08-15・作者裁定 2026-08-16（点火状況は各行参照）====
    public double ParliamentStreetSupersede = 0.5;   // 未代表（小営業・軍・国庫）は街頭のまま
    public double RumpReabsorbYears = 5.0;           // 親が交戦中/消滅なら持ち越し

    // ==== 運用コスト監査 2026-08-16（燃料系＝「金は払うが物資を消費しない運用」の物資化。#1海軍燃料のみ休眠・#2#4は点火済み・#3汽船燃料は2026-09-27 に点火）====
    public double NavalFuelPerGauge = 0.0;      // 石炭→重油の二択と支払い経路は植民地補給線（Colonial.BuyUpkeepGood/ChooseFuelGood）と同一＝台帳は TreasuryCat.Navy（現在書き手ゼロの枠）
    public double NavalFuelSortieFloor = 0.4;   // NavalFuelPerGauge>0のときのみ読む
    public double NavalFuelFillSmoothing = 0.25; // NavalFuelPerGauge>0のときのみ読み書き
    public double EraNavalFuel = 0.0;           // 既定0でEraFactor=1.0＝恒等
    public double RailCoalRate = 0.22;          // 導出＝0.25×(0.4×高度労働0.8+0.3×労働0.5)/石炭0.54
    public double ShipFuelRate = 0.0016;        // 点火2026-09-27（回4-3 の再挑戦）
    public double AirFuelRate = 0.045;          // 充足は MilitaryAirFill に乗算合成（新フィールドなし）
    public double MotorFuelRate = 0.05;         // 車両は耐久財・燃料は毎月→機体比の論法で0.02×2〜3

    // ==== 蜂起R群 2026-08-16（docs/202608/20260816_蜂起不発と極端プレイ診断_部分完了.md・実装時は全て休眠＝既定0で厳密ビット恒等＝0で新しい読み書きゼロ・分岐1回のみ）====
    public double FactionRevoltLocalCoerce = 0.5;   // F3b点火の単位整合＝左辺FactionStrength（州×ブロック×主軸クラスタ規模）vs 右辺全国coerce の按分ブレンド（0..1）
    public double ModernizationBudgetSoftCap = 1.0;   // // R4（診断B3・最初の1手）：近代化投資のminクランプ→漸近逓減（World.ModernizationBudget）

    // ==== 海ノード吸着の是正 2026-08-16（docs/202608/20260816_海ノード吸着の是正_完了.md・実装時は休眠＝既定0で厳密ビット恒等／★点火梯子で0.5へ点火済み・live）====
    public double ImportPayerLive = 0;   // 0＝今の形（商人の POP があれば人数を見ずにその 1 POP・1 ビットも変わらない）
    public double SeaStitchLeadBudgetMult = 0.5;   // 港から海までの実測の出港距離×これ、を超えた先の陸は横断とみなす（0＝無制限に免除・旧規則）


    // ==== 環境変数による係数の上書き（測定用・2026-08-12）====
    private static readonly (string Name, double Value)[] Overrides = ParseOverrides();   // なぜ要るか：A/B を同一バイナリ・同一ソースハッシュで取るため

    private static (string, double)[] ParseOverrides()
    {
        string? spec = System.Environment.GetEnvironmentVariable("POB_COEF");
        if (string.IsNullOrWhiteSpace(spec)) return [];
        var list = new List<(string, double)>();
        foreach (var part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0) throw new ArgumentException($"POB_COEF の書式が不正: 「{part}」（名前=値 で書く）");
            string name = part[..eq].Trim();
            var field = typeof(Coefficients).GetField(name);
            if (field is null)
                throw new ArgumentException($"POB_COEF に存在しない係数名: 「{name}」");
            if (field.IsLiteral || field.IsInitOnly ||
                !(field.FieldType == typeof(double) || field.FieldType == typeof(int) || field.FieldType == typeof(bool) || UnitValues.IsUnit(field.FieldType)))   // 専用の値型（RatePerYear など）の係数も名前で引く
                throw new ArgumentException($"POB_COEF が扱えない係数: 「{name}」（{field.FieldType.Name}）");
            if (!double.TryParse(part[(eq + 1)..].Trim(), System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out double v))
                throw new ArgumentException($"POB_COEF の値が数値でない: 「{part}」");
            if (!double.IsFinite(v))
                throw new ArgumentException($"POB_COEF の値が有限でない（NaN/無限大）: {part}");   // 型を問わず（double の係数に NaN が入ると下流のガードが静かに倒れる）
            if (field.FieldType == typeof(int) && !(v >= int.MinValue && v <= int.MaxValue))
                throw new ArgumentException($"POB_COEF の値が int の範囲外/非数: 「{part}」");
            list.Add((name, v));
        }
        return [.. list];
    }

    public Coefficients()
    {
        foreach (var (name, value) in Overrides)
        {
            var f = typeof(Coefficients).GetField(name)!;
            if (f.FieldType == typeof(double)) f.SetValue(this, value);
            else if (UnitValues.IsUnit(f.FieldType)) f.SetValue(this, UnitValues.FromDouble(f.FieldType, value));   // 専用の値型
            else if (f.FieldType == typeof(int)) f.SetValue(this, (int)value);
            else if (f.FieldType == typeof(bool)) f.SetValue(this, value > 0.5);
            else throw new ArgumentException($"POB_COEF が扱えない型の係数: 「{name}」（{f.FieldType.Name}）");
        }
    }

    public static string OverrideSummary()   // この走行で上書きされた係数（測定の見出しに出して「掛けたつもり」を防ぐ）
        => Overrides.Length == 0 ? "" : string.Join(" ", Overrides.Select(o => $"{o.Name}={o.Value:G}"));
}
