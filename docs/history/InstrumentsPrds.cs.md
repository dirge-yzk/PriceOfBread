# InstrumentsPrds.cs の注の履歴

由来：1cd66aae5 の InstrumentsPrds.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPrds.cs#番号〕だけが残る。

## §1　形=full　元の行=3-29　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (700) 影の列 prds_s（社会主義の重みの一部を抑圧×闘争度で共産主義へ移す式の影・読むだけ・恒等） ----------------------------------------------------------------
// 設計書 docs/202610/20261030/20261030_社会主義と共産主義の重み_設計.md の式 2-1：
//     t = eraComm · repress · mil · r[5]      （r[5] は思想の門〔eraSoc〕の後・ファシズムの加点と Normalize の前。mil = max(不満度, 闘争度)）
//     r[5] -= t; r[6] += t                    （旗の下でだけ。ここでは世界へ書かない）
// 影の列は、**今の式のままの世界の上で**、その移し替えをしたら各 POP の主軸がどうなるかを計算するだけ。Ideologies.UpdateShares の POP のループの中で Normalize の直前に r を読み、
// 複製に移し替えを当てて Normalize（本体と同じ関数）と ArgMax（FactionSnapshot.BlocIdeo と同じ関数）を通す。r を書かない・乱数を引かない・新しいシミュの状態を作らない。INS=1（Instruments.On）のときだけ動く。
// 世界の思想の重みには使わない＝腕 B の帰結（闘争度の応答・理想点の変化）は入らない。
// 行＝国×四半期（UpdateShares が走った国）。人数は Pop.Size（千人）。Size ≦ 1e-9 の POP は数えない（prdf_s の popShare と同じ）。
//   polityCode ＝ (int)w.Polity／eraSoc・eraComm ＝ 思想の門（発見の表の 2 値）／unionLaw ＝ (int)LaborUnionLaw（0 非合法・1 統制・2 容認）／repress ＝ Politics.UnionRepression（1 − 組合の合法度）。
//   popK ＝ 数えた POP の人数の和。
//   tPosShare ＝ t が正の POP の人数割合／tPosUrbanShare ＝ うち都市の POP の人数割合（popK を分母にする）。
//   shadowComAxisK ＝ 移し替えの後に主軸が共産主義になる POP の人数（千人）／shadowComAxisShare ＝ その割合／shadowComAxisUrbanK ＝ うち都市の POP の人数。
//   realComAxisShare ＝ 今の式で主軸が共産主義の POP の人数割合（基線では 0）。
//   tShareLeft ＝ 移し替えの量 t（正規化の分母 Σr で割った割合）の人数加重の平均 ÷ 左の束（社会主義＋共産主義）の今のシェアの人数加重の平均＝左の束のうち移る割合。
//   milMean ＝ mil の人数加重の平均（NaN の POP は除く）／milMeanTPos ＝ t が正の POP だけの mil の平均。
// ★(708) prd2：運動体のセル（地域×束×主軸）の足切り 1% を写した影の N の列を末尾に足した（既存の 18 列は位置も値もそのまま）。
//   セルの鍵・参加の重み・足切りは Factions.Step が読む ComputeClusters と同じ物を通す（FactionSnapshot.BlocIdeo と同じ束・FactionSnapshot.Participation が参加の重み・足切りは MinSizeFraction×w.TotalPopulation）＝第二の定義を作らない。
//   nBodyComShadow ＝ 影の主軸が共産主義のセルのうち規模（参加で重みづけた人数）が全国人口の 1% 以上の数＝設計書 §3-2 の影の N／nCellComShadow ＝ 足切りの前の数／maxCellFracComShadow ＝ 最大のセルの規模÷全国人口。
//   nBodyComReal・nCellComReal・maxCellFracComReal ＝ 同じ量を今の主軸（移し替えなし）で数えた物＝prdf_s の nBodyCom・nCellCom・maxCellFracCom と同じ定義（Factions.Step の終わりの断面との照合用）。
//   topRegionComShadow・topBlocComShadow ＝ 影の共産主義のセルのうち最大の物の（地域の番号・束）＝セルの同一性（結束は同じ州×束×主軸のセルに育つ＝N≧1 の連続がどのセルかを追える・無ければ -1）。
//   nBodySocShadow ＝ 影の主軸が社会主義のセルの数（足切り後・移し替えで痩せた後）／nBodySocReal ＝ 今の主軸の同じ量（prdf_s の nBodySoc と照合できる）。
//   足切り 1% 以外の閾値は maxCellFracComShadow から読める（N≧1 ⇔ 最大のセルが閾値以上）。
// ★(724) prd4（出所 (715) prd3）：末尾に 6 列（既存の 28 列は位置も値もそのまま）。popVoiceLoK・popVoiceMidK・popVoiceHiK ＝ 実効発言権（Politics.EffectiveVoice）の 3 分位ごとの人数（千人）／comShareVoiceLo・Mid・Hi ＝ その分位の共産主義の重み IdeologyShares[6]（正規化後）の人数加重の平均。
//   3 分位の切り方は PrdsVoiceTerciles（発言権が厳密に等しい POP を 1 組にして累積の人数割合の中点で切る）。★(724) prd4：この 6 列は枝 prd3 の計器だけを main へ持ってきた恒等の線（旗 RadicalSplitByRepression と共有の関数 RadicalSplit は持ってこない＝影の式は (708) のまま）。
// ⚠限界（計器が自己申告する）：国の量しか出ない（POP 別の行は無い）／mil が NaN の POP は t が正にならない（旗 0 の本体と同じく NaN は r を 0 へ倒す）／unionLaw と repress は四半期の値（UpdateShares の時点の w.LaborUnion）／
//   セルの規模の参加の重みは、その POP の思想の更新の後（Normalize の後の IdeologyShares）の FactionSnapshot.Participation で測る＝Factions.Step が同じ四半期のこの後に読む断面と同じ式・同じ思想の重み（影の t が読む mil は更新の前の値）。UpdateShares の時点で測る（Factions.Step は同じ四半期のこの後に同じ式で測り直す＝militancy・生活の参照点がその間に動けば値がわずかに違う。照合の列 …Real がその差を出す）／
//   帰結（闘争度の応答・理想点の変化・束の結束・蜂起の門・政体の遷移）は入らない＝影の N は〈束が立つ入口〉の数であって政体が変わる数ではない。
`````

## §2　形=trail　元の行=44-44　次のメンバー=Coord
`````text
// (708) 参加の述語の文脈（UpdateShares が求めた物・読むだけ）
`````

## §3　形=trail　元の行=45-45　次のメンバー=Cells
`````text
// (708) 社会主義か共産主義が主軸（今か影）で、参加の重みが正の POP
`````

## §4　形=trail　元の行=46-46　次のメンバー=Voice
`````text
// (724) 実効発言権・人数・共産主義の重み（正規化後 IdeologyShares[6]）＝3 分位の列の入力
`````

## §5　形=doc3　元の行=49-49　次のメンバー=PrdsCell
`````text
    /// <summary>(708) セルの集計の入力 1 件＝POP の参照・束・参加の重み・今の主軸・影の主軸。</summary>
`````

## §6　形=doc3　元の行=52-52　次のメンバー=PrdsCellCount
`````text
    /// <summary>(708) セルの集計の結果（足切りの前の数・足切り後の数・最大のセルの規模÷全国人口）。</summary>
`````

## §7　形=doc3　元の行=55-56　次のメンバー=PrdsCount
`````text
    /// <summary>(708) 純関数：（地域・束・主軸）ごとの規模を足し、主軸 <paramref name="axis"/> のセルを数える。足切りは <see cref="FactionSnapshot.MinSizeFraction"/>×<paramref name="totalPop"/>（Factions.Step と同じ不等式）。
    /// 入力は（地域の番号・束・主軸・規模＝人数×参加の重み）。</summary>
`````

## §8　形=doc3　元の行=73-74　次のメンバー=PrdsVoiceTerciles
`````text
    /// <summary>(724) 純関数：POP（実効発言権・人数・共産主義の重み）を発言権の昇順に並べ、**発言権が厳密に等しい POP を 1 組**にして、累積の人数割合の中点で 1/3・2/3 に切って低・中・高の 3 分位へ入れる（同点の組は割らない＝順序に依らない）。
    /// 戻り値＝分位ごとの（人数の和・共産主義の重みの人数加重の平均〔人数 0 の分位は 0〕）。全員が同じ発言権なら全て中。NaN の発言権は互いに等しい組として先頭に並ぶ。</summary>
`````

## §9　形=trail　元の行=125-125　次のメンバー=Add
`````text
// (724) 計器だけ
`````

## §10　形=trail　元の行=136-136　次のメンバー=shadowAxis
`````text
// (708) セルの入力（参加の重みは Factions.Step と同じ述語）
`````

## §11　形=full　元の行=154-154　次のメンバー=regionOf
`````text
        // (708) セルの規模：POP → 地域の番号（World の POP の走査と同じ集合・参照で引く）
`````

## §12　形=trail　元の行=178-178　次のメンバー=vt
`````text
// (724)
`````
