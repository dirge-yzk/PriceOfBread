# InstrumentsRule.cs の注の履歴

由来：1cd66aae5 の InstrumentsRule.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsRule.cs#番号〕だけが残る。

## §1　形=full　元の行=3-24　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (399) 支配の4区分の表 rule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCoup.cs／InstrumentsSecession.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/rs_20261018_prereg.txt の記録の線 R1〜R8（英と清の税÷GDP・分割ポーランドと英の国家区分の順・条約港の代わりの外国区分の州と年・在地の区分が農奴解放の年で消えないこと・
//   法の浸透〔enforce〕の分布・仲介者の取り分・植民地の州の区分・無統治の分布）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期×活きている州（人口が 0 の国は出さない）。挙げた国は毎四半期・他の全ての国は年末。(399) で区分を点火したので常に動く値を出す（点火前の腕 A〔旗 0〕の表は measurements/rs399_20261018_*）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・K＝千人・Km・Count＝件数・Flag＝0／1・PerMonth＝月あたりの流れ・Code＝法の段の整数）：
//   州の列：tier／popK／ruleStateShare・ruleLocalShare・ruleForeignShare・ruleUngovShare（Region.Rule の 4 区分）／ruleInitLocalShare（開幕のデータの在地・RuleInit[1]）／foreignRulerCount（外国持分の主体の数）／
//     dominanceShare（支配度）／capitalDistanceKm（国の座標からの大圏距離・座標未登録は nan）／reachShare（Politics.RegionReach＝② の到達）。
//   国の列（その国の全ての州の行に同じ値）：serfLawCode・bureaucracyLawCode・landReformLawCode（法の段）／intermediaryDegreeIndex（① 仲介者の権利の残高）／publicOrderIndex（治安）／
//     stateReachIndex（法の浸透 enforce の値＝Politics.StateReach）／adminEfficiencyIndex（行政効率＝(399) で読み手が計器だけになった）／
//     treatyPortInForceFlag（条約港が効いているか）・portForeignShare（③ の関税の目減り）／revenuePerMonth・gdpPerMonth（税÷GDP を読み手が組む）／
//     ruleCutPerMonth（直近月の仲介者の取り分＝納税者→州の貴族・World.LastRuleCut・R6）／stateReachHomeIndex（植民地格の州を除いた人口加重の RuleReach・R5）。
//     ⚠(412) (408) 以後は stateReachIndex（Politics.StateReach）が州ごとに ×AdminFill を掛け、この列は掛けない＝2 列は別の量（1783 の仏で 0.70 と 0.08）。2 列を比べる読み手は (408) 以後の世界では使わない（列の意味を連続に保つため式は変えない）。★(427) 旗 AdminServesStateRule は (419) で焼き込み〔常に 1〕＝〈旗 1 の世界〉は今の世界の全て。
// (842) 段 P1-0＝州ごとの治安の形 P1（下書き docs/202611/20261105/20261105_自給の世帯の家計の段と州ごとの治安_下書き.md §2・AI の案・確証なし）の恒等の計器。末尾の 9 列（nat の後）。挙動は変えない。計器が自分で州の POP と国の Coercion を読む（エンジンの関数の局所変数を受け取らない）。
//   orderRawIndex＝切る前の R＝effCoerce÷max(1e-9, 人口×CrimeOrderPerPop)（Crime.cs と同じ式・四半期の末の w.Coercion で数える。人口は約分される）。
//   militancyRegionIndex＝m_r（州の平民〔IsCommoner〕の人口加重の闘争度・首都を含む・平民が 0 の州は nan）／militancyNationIndex＝m̄（国の全ての州〔首都を含む〕の平民の人口加重の平均・全ての州の行に同じ値）。
//   orderRegionIndex＝order_r＝Clamp01(R×m̄÷m_r)（m_r が正でない州は 1）／ungovTargetOldShare＝(1−Clamp01(w.PublicOrder))×(1−reachShare)＝今のエンジンの無統治の目標／ungovTargetNewShare＝(1−order_r)×(1−reachShare)。
//   遅れた R（下書きの反証役の訂正 8）：w.PublicOrder は Crime.Step が前の四半期の Coercion で書いた値＝1 四半期遅れ。orderRawLagIndex＝前の四半期の末に数えた R（計器の持ち物・最初の四半期は nan）・orderRegionLagIndex・ungovTargetNewLagShare は R の代わりにそれで数えた値。
//     publicOrderIndex が 1 未満の行では orderRawLagIndex と publicOrderIndex が一致する（試験 T7 が固定）。Rule の目標を行ごとに数える州は Rule の長さが 4 の州だけ（それ以外の州の目標の列は nan）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Instruments.Quarter の呼び時点）／外国持分の主体名は列にしない（個数だけ）／orderRawIndex は四半期の末の Coercion＝エンジンの Rule のループが走った時点の値と同じとは限らない（Rule のループの時点の R を貯めない＝思想⑩）／
//   遅れた R は計器が毎四半期の末に 1 つ持つ値＝挙げた国でない国も毎四半期更新する（年末の行しか出ない国でも遅れは前の四半期の値）。
//   (846) m_r・m̄ とその派生（militancyRegionIndex・militancyNationIndex・orderRegionIndex・ungovTargetNewShare）は四半期の末の値で、エンジンの Rule のループが読む CheckSecession の瞬間の値と 92% の行で違う（(842)・measurements/m842_20261105/rule_vs_secession.txt）＝エンジンの〈働いた〉の数に使わない。
`````

## §2　形=doc3　元の行=30-30　次のメンバー=RuleRawOrder
`````text
    /// <summary>(842) 切る前の R（計器が自分で書く式）：EffectiveCoerce と同じ結合順 C×(P/ref) を先に、人口は約分されるが約分しない形のまま。人口が正でなければ nan。</summary>
`````

## §3　形=doc3　元の行=39-39　次のメンバー=RuleCrowdMilitancy
`````text
    /// <summary>(842) 州の平民の闘争度の和と規模の和（IsCommoner の述語を計器が自分で数える）。</summary>
`````

## §4　形=doc3　元の行=53-53　次のメンバー=RuleP1HeaderText
`````text
    /// <summary>(842) 段 P1-0 の末尾の 9 列の見出し（先頭にタブ）。</summary>
`````

## §5　形=doc3　元の行=56-56　次のメンバー=HomeStateReach
`````text
    /// <summary>本国の州だけの法の到達（植民地格の州を除く人口加重の <see cref="Politics.RuleReach"/> の平均・読むだけ・★(412) 官僚の役務の充足 AdminFill は掛けない＝(408) 以後の stateReachIndex とは別の量）。本体の enforce は読まない（<c>StateReach</c> は植民地格の州も含む）。</summary>
`````

## §6　形=full　元の行=73-73　次のメンバー=lagSt
`````text
        // (842) 遅れた R の持ち物：毎四半期（挙げた国でなくても・行を出さない四半期も）更新する。行を出す前に前の値を取り、今の値を次の四半期のために置く。
`````

## §7　形=full　元の行=85-85　次のメンバー=crowdMil
`````text
        // (842) 段 P1-0：m̄＝国の全ての州（首都を含む）の平民の人口加重の平均（州の列と別に 1 回数える）
`````
