# InstrumentsSb.cs の注の履歴

由来：1cd66aae5 の InstrumentsSb.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSb.cs#番号〕だけが残る。

## §1　形=full　元の行=5-21　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (520) 交渉の取り分と地代の表 sbu_s・sbr_s・sbrreg_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSm.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（蓄えは ConditionalWeakTable＝セーブに載らない）。INS=1 のときだけ動く。
// 事前登録 measurements/sb520_20261024_prereg.txt [5][6]。行を出すのは旗が正の国だけ（旗 0 では新しいファイルが 1 つも増えない＝腕 A の恒等の証明）。
// sbu_s（旗 UnionBargaining・国×四半期）：
//   law＝四半期の末の労働組合の法（0 禁止・1 統制・2 容認）・suffrage＝参政権の段。regionMonths＝法が禁止でない月の（州×月）の数・tolMonths／ctrlMonths＝そのうち容認／統制の数（法は四半期の途中で変わりうる＝U1・U5 はこの列で判定）・kappaSum＝その κ の和。
//   massN・urbanMassSum＝κ の分母（州の大衆の束の POP の人数）の和とそのうち都市 POP（農村の大衆と兵士が分母に入る薄まりの大きさ＝追記 3 ⑷）・facScan＝κ の運動体の走査の件数の和（計算量の census）。
//   eligN＝適格な工場×月の数・posN＝そのうち上乗せ u＞0・guardN＝守り（u は rent を超えない）が効いた数。vaSum＝適格な工場の付加価値の和・excessSum＝Σ max(0, a−ā)×頭数（上乗せの土台）。
//   uSum＝上乗せの和（Transfer が返した額）。ownerLoss＝rent(前)−rent(後) の和・wageGain＝wages(後)−wages(前) の和（U2：独立に積んだ 2 本＝uSum と一致するはず）。
//   betaVa＝β×付加価値の和（÷vaSum で付加価値加重の β）・betaNoStrikeVa＝罷業の力を除いた β×付加価値の和（U5：容認の国で betaVa≧これ）。sSum・uRateSum・vwSum・voSum・strikeSum＝適格な工場×月の S・失業率・V_w（票の声）・V_o（票の声）・m（罷業の力＝容認のときの闘争度）の和（S＝1−(1−V_w÷(V_w+V_o))(1−m)）。
//   premN・premPos・sVoteSum（追記 4）＝適格な工場×月のうち β＞0 かつ 超過＞0 の数と、そのうち持ち主の減り・労働者の増えが実際に出た数／票の道だけの S（V_w÷(V_w+V_o)）の和。
//   exportFactory・exportAll＝PaySales（輸出代金・輸送財）が分けた額のうち 工場（BuysLabor の非 RGO）の分と全体（上乗せの入口が無い輸出代金の大きさ＝追記 3 ⑺）。
// sbr_s（産地の地代・国×四半期・(529) で旗 LandRentResidual を撤回し INS のとき常に出す）：rgoN・rgoVa・rgoRent・rgoWages＝産地×月（付加価値が非負）の数・付加価値・地代（国営の分け前の前）・耕す人への付加価値の残り。
//   selfN・selfVa・selfResid＝所有者の POP＝耕す人の POP の産地（自作農）の数・付加価値・|rent＋wages−付加価値| の和（R1：0）。capN・capErr＝法の上限が 1 未満の産地×月の数と |取り分−上限| の和（R3：0）。
//   legBreach・customBindN（(529)）＝Σ max(0, 地代÷付加価値−法の上限)（0 のはず＝法の上限の独立の照合）・慣習の取り分が法の上限より低くて効いた産地×月の数。capErr の期待値は RentShareOf を呼ばず min(生産者の RentShare, 法の上限) をその場で書く（自己参照を避ける）。
//   closedEarned・ruralMassEarned・ruralMassSize＝四半期の末の月の Earned（旧支配層の POP の和・農村の大衆の POP の和・その人数）＝地代の割合が動いたときの所得の記録。landRentLast＝その月の Pop.LandRentPerMonth の和（土地税の課税の元）。
// sbrreg_s（挙げた国×州×四半期）：crowd＝農村人口÷耕地（ArableLand〈センチネル 1e8 以上は nan〉）・rev・inCost・va・rent・wages＝産地の売上・投入費・付加価値・地代・残りの四半期の和（rent÷rev と crowd の順位相関を記録で読む）。
// ⚠限界（計器が自分で申告する）：①頭数は能力の式の頭数（Producer.HeadsLastMonth・投入の足りない工場も同じ）②失業率は前の月の値③輸出代金は ④ を通らない（上乗せの入口が無い）④κ は四半期に 1 度更新される運動体の値（Size と Cohesion）を月に読む。
`````

## §2　形=full　元の行=74-74　次のメンバー=legal
`````text
        // ★(529) 期待する取り分は RentShareOf を呼ばずにその場で書き直す（エンジン自身の関数と比べる自己参照を避ける・反証役 pf2 高#1）。
`````
