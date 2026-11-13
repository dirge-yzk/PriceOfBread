# PriceOfBread.SimCore/Factions.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-14 W 直後 `public sealed class FactionState`
````
/// <summary>
/// 創発的国内勢力 F3a＝クラスタの**永続化と変異**（2026-07-12・裁定＝docs/2026-07-09_設計メモ.md §3 Phase3 の前半）。
/// F2（FactionSnapshot＝表示専用の瞬間値）に「持続する運動体」の実体を与える：
/// - **結束（Cohesion）**＝同じクラスタ（州×ブロック×イデオロギー主軸）が四半期をまたいで観測され続けるほど育つストック
///   （指導部は新状態を持たず、成長率がブロックの意識に比例＝識字・意識から創発）。消えたクラスタは減衰し、やがて忘れられる。
/// - **変異（Mutated）**＝危機（州の飢え・敗戦）の中の結束したクラスタが稀に千年王国的な旗を掲げる（太平天国・タボル派）。
///   独立RNG系列（World.FactionRng）＝主系列・豊凶・疫病と完全分離＝較正/決定論に非干渉。名前と結束ボーナスのみ＝
///   **要求は既存19法軸のまま**（ユーザー裁定2026-07-11＝宗教法ラダーは作らない）。
/// - F3a は書く側（結束/変異/合流の状態更新）。読み手＝F3b（Politics.CheckFactionRevolt／FactionStrength＝常に有効・旧の旗 FactionRevoltScale は (739) で焼き込み）と表示。
/// 較正/最小世界＝ParliamentEnabled=false または FactionRng=null で完全不発（EthnicEnabled と同型の遮蔽）。
/// SaveGame はリフレクション直列化で List/Region参照/RNG状態とも自動追従。
/// </summary>
````

## L17 T `public Region Region = null!;`
````
// クラスタの本拠州（参照持ち＝SecedingRegion と同じ作法・割譲等で消えれば不在減衰で自然消滅）
````

## L18 T `public int Bloc;`
````
// 0=旧支配層 1=近代エリート 2=大衆
````

## L19 T `public int Ideo;`
````
// イデオロギー主軸（Ideology の序数）
````

## L20 T `public double Size;`
````
// 直近観測の規模（人口千人）
````

## L21 T `public double AvgMilitancy;`
````
// 直近観測の平均闘争度
````

## L22 T `public double Cohesion;`
````
// 結束∈[0,1]＝運動体としての持続の証（発火ゲート・F3b）
````

## L23 T `public bool Mutated;`
````
// 変異済み（千年王国の旗）＝結束成長2倍
````

## L24 T `public string MutantName = "";`
````
// 変異名（「広州の太平会」等）。未変異は空＝表示は機械生成名
````

## L25 T `public double Rejections;`
````
// ★段3（内戦一元化doc §4・2026-08-30）：拒否の累積（弾圧・対決・別軸譲歩=本丸拒否が+1・受諾で0へ）。読み手は FactionRejectionMutation>0 の変異ロールのみ＝0では積むだけ＝恒等
````

## L26 T `public double EquipFill = 1.0;`
````
// ★武器の市場購入（2026-09-11）：装備の充足＝国軍の MilitaryEquipFill と**同じ関数**（EconomyStep.EquipFill）の値。書き手は EconomyStep の武器購入の決済だけ＝休眠（RebelArmsMarketEnabled=0）では 1.0 のまま（FactionStrength も読まない）
````

## L27 T `public Dictionary<int, double>? ArmsStock;`
````
// 運動の武器庫（財ID→量）＝国軍の Region.ArmsStock と同じ物差し・同じ摩耗（ArmsFillContribution）。休眠では null（1件も作られない）。新しい運動は0から（国軍の「未初期化は目標」は当てない）
````

## L28-30 W 直後 `public int[]? CrisisDemands;`
````
// ★要求は法の経路（2026-09-11）：通知（PendingFactionCrisis）時点の要求の記録＝「要求した法が通常の経路で動いたか」を次の評価で比べる物差し。
//   CrisisDemands＝要求の軸（序数+1・増加＝正／減少＝負・先頭＝本丸＝ComputeDemands のスコア順）／CrisisDemandPos＝同時点の FactionSnapshot.AxisPosition。
//   通知が立っていない運動は null（SaveGame は null/配列とも自動追従）。運動は解散しない（09-09）＝通知が下りるときに null へ戻す。
````

## L37-37 W 直後 `private static readonly string[] MutantStems = ["太平", "千年", "救世", "光明", "天命", "新生", "浄化", `
````
// 変異名の語幹と接尾（千年王国運動の型・国を問わず読める語彙）。RNG上位bitでindex＝決定論。
````

## L41 T `private const double MutationCohesionGate = 0.3;`
````
// 変異ロールに要る結束の下限（旗は結束した運動体のみ・頻度絞り2026-07-12）
````

## L42 T `private const double ForgetCohesionFloor = 0.05;`
````
// 不在の運動の結束がこれ未満で忘却
````

## L44-44 W 直後 `public static void Step(World w, Coefficients c)`
````
/// <summary>四半期の更新（Politics.Step から・PoliticsEnabled世界のみ）。観測クラスタと永続状態を突き合わせ、結束を育て、危機で変異をロールする。</summary>
````

## L47 T `if (!w.ParliamentEnabled || w.FactionRng is null) return;`
````
// 較正/最小世界＝完全不発（EthnicEnabled と同型）
````

## L51 T `var observed = FactionSnapshot.ComputeClusters(w, countCost: true);`
````
// 共有核＝F2表示と同じ束ね方（決定論）
````

## L52-52 W 直後 `var consciousnessByBloc = new double?[PolicyPreferences.BlocCount];`
````
// ブロックの意識は bloc（0..2）のみに依存し本Step中は不変＝クラスタごとの再計算を避け遅延キャッシュ（値・走査順は従来と同一）。
````

## L54-54 W 直後 `foreach (var (reg, bloc, ideo, size, milSum) in observed)`
````
// 1) 観測→状態の照合（キー＝州参照×ブロック×主軸）。観測順→状態順の走査＝決定論。
````

## L57 T `if (size < FactionSnapshot.MinSizeFraction * totalPop) continue;`
````
// 微小クラスタは運動体にならない（F2と同じ足切り）
````

## L62-62 W 直後 `double consc = consciousnessByBloc[bloc] ??= BlocConsciousness(w, bloc);`
````
// 結束の成長＝意識ある層ほど組織が速い（指導部の創発）。変異済みは2倍（旗の下の凝集）。
````

## L66-66 W 直後 `if (!st.Mutated && st.Cohesion >= MutationCohesionGate)   // 変異は結束した運動体のみ（頻度絞り2026-07-12＝旗`
````
// 2) 変異のロール（1クラスタ1回まで・危機×結束のみ・独立RNG系列）
````

## L67 T `if (!st.Mutated && st.Cohesion >= MutationCohesionGate)`
````
// 変異は結束した運動体のみ（頻度絞り2026-07-12＝旗は安売りしない）
````

## L70-71 W 直後 `if (c.FactionRejectionMutation > 0)`
````
// ★段3（内戦一元化doc §4・2026-08-30）：拒否の累積も変異の引き金＝「ピータールー→チャーティズム」「血の日曜日→1917」。
//   危機と max 合成（doc の処方どおり）。FactionRejectionMutation=0＝crisis のまま＝厳密恒等。
````

## L77 T `double roll = (u & 0xFFFF_FFFF) / (double)uint.MaxValue;`
````
// 下位32bit＝受理判定
````

## L84 T `st.Cohesion = System.Math.Max(st.Cohesion, c.MutantCohesionFloor);`
````
// 旗＝結束の床
````

## L91-91 W 直後 `for (int i = w.Factions.Count - 1; i >= 0; i--)`
````
// 3) 不在クラスタの減衰と忘却（走査は逆順＝除去安全）。(739) 群11：忘却の時計（連続不在 8 四半期）は外した＝結束が床を割ったら忘れる。
````

## L100 T `if (f.Cohesion < ForgetCohesionFloor) w.Factions.RemoveAt(i);`
````
// 忘れられた運動
````

## L102-105 W 直後 `if (c.FactionMergeRangeKm > 0 && w.Factions.Count >= 2)`
````
// 4) ★段6（内戦一元化doc §3・spec §3-2＝唯一の新規実装・2026-08-30）：派閥の合流＝太平天国の北上（通過地の不満民を吸収）。
//    条件3つ＝【敵】同ブロック×同イデオロギー（同じ的）・【手段】変異状態の一致（旗と旗・議会派と議会派）・【距離】FactionMergeRangeKm以内。
//    大きい方が小さい方を吸収＝結束を継ぎ足し（新しい頭数は参加述語が毎期導出＝規模は運ばない）。1四半期1件×勝者（連鎖合体の暴走なし）。
//    FactionMergeRangeKm=0＝この節に1命令も入らない＝厳密恒等。走査順＝州名の全順序＝決定論。
````

## L109-111 W 直後 `var ordered = new System.Collections.Generic.List<FactionState>(w.Factions.Count);`
````
// ★段0＝幽霊の封じ（F3b蜂起doc §7-1・2026-09-08・係数なし）：反乱国家が建った後の本拠州は親の Regions を離れるのに
//   FactionState は親のリストに残る。除外しないと**他国の州の運動**が親の運動へ結束を継ぎ足す（合流の相手が国外）。
//   選定ループ（Politics.CheckFactionRevolt）と同じ述語で、同じ理由で外す。
````

## L126 T `big.Rejections = System.Math.Max(big.Rejections, small.Rejections);`
````
// 怨嗟は深い方が残る
````

## L127 T `small.Cohesion = 0;`
````
// 吸収＝次の掃除で自然消滅（即Removeせず走査を壊さない）
````

## L132 T `if (Instruments.On) Instruments.PrdfObserve(w, observed, totalPop);`
````
// (681) 計器だけ＝急進の運動体の表（読むだけ）
````

## L135-135 W 直後 `public static string DisplayName(FactionState f) =>`
````
/// <summary>表示名：変異済みは旗の名・未変異は機械生成名（F2と同じ語彙）。</summary>
````

## L139-139 W 直後 `private static double BlocConsciousness(World w, int bloc)`
````
// ブロックの人口加重平均の意識（結束の成長率＝識字・意識のある層ほど組織化が速い）。
````

