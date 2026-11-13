# InstrumentsSecession.cs の注の履歴

由来：1cd66aae5 の InstrumentsSecession.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSecession.cs#番号〕だけが残る。

## §1　形=full　元の行=3-23　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (395) 州の分離の内訳の表 secession_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsBur.cs／InstrumentsCapacity.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝(391) の読む線〈英の人口の倍率 0.72→0.42 の帰属＝中核ベンガル・内陸ベンガル・マドラスが 10/10 種で 1788〜1808 年に分離独立した。落とした先が正統性の床の往復か・緩和か・保持力の式かは、
//   州ごとの闘争度・分離の圧力・保持力の内訳の列が無いと割れない〉への答え。CheckSecession の起点（毎四半期の離脱圧力と保持力と支配度の積分）を、州ごとに 1 行で読む。
// 取り方＝本体の CheckSecession が支配度を積分した直後に 1 行（<c>if (Instruments.On) Instruments.SecObserve(…)</c>）で呼ぶ。本体のローカル変数（姿勢・保持力・共有分母・不穏の件数・州ごとの（引き留め, 圧力））は本体が使った値そのまま、
//   圧力の因数（闘争度・文化の距離・民族の少数派・国民意識・参政権の算入・大衆の飽和）は <see cref="SecRebuild"/> で同じ式・同じ順で組み直して、組み直した圧力と本体の圧力の差を列で申告する（`pressureRebuildGapIndex`＝0 でなければ写しが本体とずれた）。
// 行＝国×四半期×州（首都と活きていない州を除く＝CheckSecession が走る州）。挙げた国は毎四半期・他の全ての国は年末。1 試合 約 数十MB。
// 列（単位は接尾辞：Index＝無次元の量・Share＝0〜1・K＝千人・Count＝件数・Flag＝0/1）：
//   国の列（その国の全ての州の行に同じ値）：legitimacyIndex＝正統性／totalPopK＝国の総人口／coerceIndex＝呼び手が渡した軍・警察の保持力（人あたり強度）／holdBaseIndex＝投射と共有の前の中央の保持力
//     （正統性÷(1＋総人口×SecessionHoldPerPop)＋SecessionHoldCoercion×SecessionThreshold×coerce÷(coerce＋基準人口)）の組み直し／holdBaseRebuildGapIndex＝組み直しと本体の hold×共有分母の差（相対・0 に近いはず）／
//     stanceIndex＝国の関税の姿勢（−1 自由貿易..+1 保護）／sharedDivIndex＝共有分母（1＋k×(不穏の件数−1)）／restiveCount＝閾値を越えた運動の数（反乱国の戦線を足した後）。
//   州の列：tier／popK＝州の全 POP／crowdK＝州の大衆（閉鎖階級と兵士を除く）／crowdFactorShare＝大衆の飽和 N÷(N＋N0)／militancyMeanIndex＝州の平均闘争度（人口加重・閉鎖階級と兵士を除く）／
//     cultureDistanceIndex＝文化と宗教の距離（民族の寄せを含む最終値）／coreShareMeanShare＝州の平均中核度／consciousnessMeanIndex＝州の国民意識の平均／suffrageInclusionShare＝参政権の算入率／
//     tariffInterestIndex＝州の関税利害（−1..+1）／tariffGapIndex＝|姿勢−州の関税利害|／tariffTermIndex＝関税の項（乖離×闘争度×圧力の尺度×(1＋文化の重み×距離)）／ethnicTermIndex＝民族の加算の項／
//     consciousnessFactorShare・suffrageFactorShare＝国民意識の割引・参政権の割引（掛ける因数）／pressureIndex＝本体の離脱圧力／pressureRebuiltIndex＝組み直した圧力／pressureRebuildGapIndex＝組み直しと本体の差（相対）／
//     effHoldBeforeShareIndex＝その州に届く保持力（投射済み・共有分母の前）／holdShareIndex＝共有分母で割った保持力（支配度の積分に使った引き留め）／marginIndex＝holdShareIndex−pressureIndex（支配度の増分は marginIndex×DominanceSpeed）／
//     dominanceShare＝積分の後の支配度／goFlag＝支配度が 0 になった州（分離独立の戦争へ入る）／capitalDistanceKm＝首都（国の座標）からの大圏距離（座標未登録は nan）。
// ⚠限界（計器が自分で申告する）：
//   ①圧力の因数は組み直した値＝本体が因数を取り直す式を変えたら組み直しがずれる（pressureRebuildGapIndex で見える・番人が 0 を見る）。②共有分母を掛ける前の保持力は pressureIndex の隣の列＝holdShareIndex の分母は国で 1 つ。
//   ③行の値は CheckSecession を呼んだ時点（四半期の政治の段）の値＝行を書く四半期の末より前の瞬間（その後の移民・人口の段で州の POP が動いた後の値ではない）。④1 四半期に CheckSecession が複数回走れば最後の 1 回の行だけ。
//   ⑤支配度の積分の前の値は列に無い（前の四半期の行の dominanceShare を読み手が使う・新しい州は前の行が無い）。⑥capitalDistanceKm は国の座標と州の座標の大圏距離＝投射の入力であって、投射そのもの（距離減衰）の率ではない。
`````

## §2　形=full　元の行=34-41　次のメンバー=SecC01
`````text
    // ---- (850) c69po 州ごとの治安 P1-1 の末尾 10 列（読むだけ・CheckSecession の瞬間＝Rule のループの直前・計器が自分で州の POP と国の Coercion を読む＝エンジンの関数を呼ばない） ----
    //   orderRawNowIndex＝R＝今の Coercion の切る前の被覆＝Coercion×(総人口÷max(1, PoliticalReferencePop))÷max(1e-9, 総人口×CrimeOrderPerPop)（Crime.Step と同じ式・同じ結合順）／
    //   orderOldIndex＝Clamp01(w.PublicOrder)（旗 0 の秩序＝Crime.Step が前の四半期の Coercion で書いた値）／militancyNationMeanIndex＝m̄＝国の全ての州（首都を含む）の大衆（閉鎖階級と兵士を除く）の闘争度の人口加重の平均（州ごとの和を足す順）／
    //   militancyRegionOwnIndex＝m_r（同じ述語で計器が数えた値・militancyMeanIndex と一致するはず）／orderRegionNowIndex＝order_r＝m_r が正でなければ 1・そうでなければ Clamp01(R×m̄÷m_r)／
    //   reachOwnShare＝首都（国の座標）からの到達 1÷(1＋DistanceWarDecay×km÷10000)（座標未登録・距離が正でない・非有限は 1）／ungovTargetOldShare＝(1−orderOld)×(1−到達)（旗 0 の目標）／
    //   ungovTargetNewShare＝(1−order_r)×(1−到達)（旗 1 の目標）／ruleUngovBeforeShare・ruleForeignBeforeShare＝Rule のループの前の r.Rule[3]・r.Rule[2]（長さ 4 でなければ nan）。
    //   ⚠限界：⑦目標の 2 列は計器が数えた〈意図した式〉＝エンジンが実際に寄せた目標ではない（実際の側は rule_s の四半期の末の ruleUngovShare と ruleUngovBeforeShare から (after−before×(1−速度))÷速度 で読み手が読む・外国の区分が正の行は正規化で外れる）。
    //   ⑧首都の行は無い（CheckSecession の州の行と同じ）＝m̄ にだけ入る。⑨数は G10 の 10 桁で書く＝読み手の (after−before×0.975)÷0.025 は約 4e-9 まで丸めが乗る。
`````

## §3　形=full　元の行=114-114　次のメンバー=effNow
`````text
        // (850) 国の 3 つの量（計器が自分で数える）：R・旗 0 の秩序・m̄
`````
