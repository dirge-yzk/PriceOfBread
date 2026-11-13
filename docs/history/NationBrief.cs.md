# PriceOfBread.SimCore/NationBrief.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L6-13 W 直後 `public static class NationBrief`
````
/// <summary>国家選択画面が読む「開始条件の要約」＝おすすめ国と難易度（docs/202608/おすすめ国家と難易度_2026-08-09_完了.md）。
///
/// ★難易度を world.json に固定値で書かない（同doc §3-3）。理由＝序列を決めている2係数
/// （行政の必要量 <see cref="Coefficients.AdminNeedPerPop"/>〔2026-09-30 (135) まで AdminCostPerPop〕・<see cref="Coefficients.SecessionHoldPerPop"/>）は
/// 「人口だけの式」としてA級に分類され、いずれ機構へ置き換える予定＝固定値で書くとラベルだけが取り残される。
/// ここで**実行時に導出**しておけば、係数を直した日にラベルが自動で追随する。
///
/// ★UIロジックでなくSimCore側に置く理由＝係数と world.json を読む導出だから（Appから係数の意味を再解釈させない）。</summary>
````

## L16-19 W 直後 `public static readonly string[] Recommended =`
````
/// <summary>おすすめ国＝「有名だから」でなく**このエンジンが何を回すか**で選んだ7カ国（同doc §1・§6。作者指示2026-08-11で6→7＝簡単2／普通4／挑戦的1）。
/// 各国がそれぞれ違う系統を教える（英＝議会／米＝ゴールドラッシュと識字／普＝2段の変体／
/// 日＝幕府という固有政体／露＝勅令の効く専制／清＝人口が罰になる構造／蘭＝小国・金融・海運＝人口比の富が断トツ）。
/// ★おすすめは「入口」であって制限ではない＝シム側は全国プレイ可能（NationSelectViewModel）。</summary>
````

## L23-28 W 直後 `public static string Description(string name) => name switch`
````
/// <summary>カードに出す説明＝**1783年までの歴史的背景＋最後の1文でプレイフィール**（作者裁定2026-08-11）。
/// ★裁定②＝開始年までを書き、**その後に起きたことは書かない**（思想①台本禁止と噛み合う＝筋書きを与えない）。
/// 裁定③＝歴史に詳しくない人が読んで分かる短さ。**この画面はUI規律⑦（説明文を書かない）の例外**
/// ＝ゲーム開始前の画面であり、選択のための情報提供が目的（作者裁定2026-08-09 ①）。
/// ⚠アメリカの文章に「自由な国」と書いてはならない（奴隷制が未実装＝同doc §5-1）。
/// ★ホバーは置かない（作者指示2026-08-11）＝画面に出ている文章が全部。読むために操作を要求しない。</summary>
````

## L66-66 W 直後 `public static double Population(NationConfig c)`
````
/// <summary>開始時の総人口（config の階級の和）。行政費と分離耐性の分母＝難易度の主因。</summary>
````

## L70-71 W 直後 `public static double TreasuryMonths(NationConfig c, Coefficients coef)`
````
/// <summary>国庫が行政の固定費だけで何ヶ月持つか（同doc §3 ①）。分母＝全ての頭数の行政の必要量を参照価格で買う月の額（官僚の給与×人数＝EconomyStep の行政の購買の充足1）。
/// 2026-09-30 (135)：旧の分母＝行政費の人頭割り AdminCostPerPop×人口（削除＝行政の購買と同じ役務の二重の支払い）。</summary>
````

## L74-74 W 直後 `if (c.TreasuryMonths > 0) return c.TreasuryMonths;`
````
// ★(388) world.json の絶対額 treasury は 0 の置き場（開幕の国庫は助走の終わりに歳入×月数で置く＝NationConfig.TreasuryMonths）。規則のある国は月数をそのまま読む（歳入≒行政の必要量の月の額）。
````

## L80-80 W 直後 `public static double SecessionHold(NationConfig c, Coefficients coef)`
````
/// <summary>分離独立に抗う力（同doc §3 ②）。Politics の保持力の分母と同式＝人口が大きいほど下がる。</summary>
````

## L84-85 W 直後 `public static Dictionary<string, double> OpeningSoldiers(NationConfig[] all, Coefficients `
````
// 【削除2026-09-26（棚卸し x-04）】IsIsolated（本国州が他国と陸続きでないか）＝呼び手がテストだけだった。
//   陸続きの判定の実体は下の NeighborMilitary（同じ陸塊の他国の軍＝島国と大洋の向こうは0）が難易度に使っている。
````

## L87-88 W 直後 `public static Dictionary<string, double> OpeningSoldiers(NationConfig[] all, Coefficients `
````
/// <summary>★(235) 開始時の兵の数（千人）＝**構築した世界**の兵士POPの和（World.SoldierSizeK）。(231) で military 欄を外したあと、安全の項は人口（農民＋都市労働者）に比例する写しを読んでいた
/// ＝構築の播種の式と第二の式を抱えていた。世界を1つ組んで実数を読む（ランクと国力と同じ1本の構築＝<see cref="Opening"/>）。呼び手が同じ世界を何度も組まないよう、引数で受け取れる。</summary>
````

## L89 T `public static Dictionary<string, double> OpeningSoldiers(NationConfig[] all, Coefficients `
````
// ★(449) 兵の数だけ要る呼び手は助走を通さない（兵の数は構築直後の値＝助走の前・旗に依らず同じ）
````

## L91-93 W 直後 `private static double NeighborMilitary(NationConfig c, NationConfig[] all, IReadOnlyDictio`
````
/// <summary>同じ陸塊に本国州を持つ他国の軍のうち、上位3つの和（＝取り囲まれの度合い）。
/// 最大1つでは足りない＝プロイセンは墺2.6・露2.0・仏1.8に**同時に**面しており、最大値だけ見ると
/// 「墺だけの隣国」に見えて実態より安全に出る。海を隔てた国は数えない（英・米・日はここで0になる）。</summary>
````

## L98 T `double a = 0, b = 0, d = 0;`
````
// 上位3つ（並べ替えを作らずに3枠へ落とす＝決定論・割り当てゼロ）
````

## L113-113 W 直後 `public static (double Ease, double Resource, double Hold, double Safety, double Load)`
````
/// <summary>難易度の素点の内訳（0..1／素点は加重和）。UIと診断（selectprobe）が同じ1本を読む。</summary>
````

## L117 T `soldiers ??= OpeningSoldiers(all, coef);`
````
// 呼び手が渡さなければ世界を1つ組んで読む（国ごとに繰り返し呼ぶ側は渡すこと）
````

## L118-118 W 直後 `double resource = PoliticalQuantities.Clamp01(Math.Log10(Math.Max(1.0, TreasuryMonths(c, c`
````
// ①資源の余裕＝国庫が行政費だけで持つ月数。桁で効く（仏4・米5 と 普274＝2026-09-29 に開幕の国庫を史実の桁へ直した後・旧＝清4 と 蘭6667）ので対数で圧縮（1ヶ月=0／10000ヶ月=1）。
````

## L120-120 W 直後 `double hold = SecessionHold(c, coef);`
````
// ②人口による分離圧に抗う力（Politics の保持力と同式）。
````

## L122-122 W 直後 `double safety = 1.0 / (1.0 + NeighborMilitary(c, all, soldiers) / Math.Max(1e-9, soldiers.`
````
// ③安全＝陸続きの隣国の軍（上位3和）に対する自国の軍。海に隔てられていれば隣国0＝1.0（英・米・日）。
````

## L124-127 W 直後 `double load = PoliticalQuantities.Clamp01((c.Provinces.Length + c.InitialColonies.Length +`
````
// ④操作量＝**プレイヤーが毎期見る対象の数**（本国州＋植民地＋工業の厚み）。
//   ★作者指摘2026-08-11「イギリスは操作量が多く簡単でない」＝資源が豊かでも、州5＋植民地11＋世界一の工業は
//   最も忙しい盤面になる。難易度は「生き残れるか」だけでなく「回せるか」でもある。
//   工業を3倍で数えるのは、1点の工業化度が概ね数州ぶんの生産・補助金・建設の判断を増やすため。
````

## L133-135 W 直後 `public static double Ease(NationConfig c, NationConfig[] all, Coefficients coef, IReadOnly`
````
/// <summary>難易度の素点（高いほど易しい）。★エンジンが実際に課す量だけで作る（docs §3 ＋作者指摘2026-08-11の操作量）。
/// ⚠識字は入れていない＝序列が識字を入れても変わらないため（入れると Simulation の13階級の初期識字を
/// 組み直す第二の式を抱えることになる。序列が変わる日が来たら、世界を1つ組んで実測値を読む形で足すこと）。</summary>
````

## L138-139 W 直後 `public static Dictionary<string, string> Difficulties(NationConfig[] all, Coefficients coe`
````
/// <summary>おすすめ国の難易度ラベルを**素点の順位**で割り当てる（簡単2／普通4／挑戦的1・作者指示2026-08-11）。
/// ★絶対値の閾値でなく順位にする理由＝閾値は係数が動くと全員が同じ側へ寄って空の列ができる。</summary>
````

## L149 T `scored.Sort((a, b) => b.Ease != a.Ease ? b.Ease.CompareTo(a.Ease) : string.CompareOrdinal(`
````
// 同点は名前順＝決定論
````

## L155 T `private const int EasyCount = 2, NormalCount = 4;`
````
// 挑戦的は残り（＝おすすめ7カ国なら1）
````

## L157-160 W 直後 `public static Dictionary<string, string> Ranks(NationConfig[] all, Coefficients coef)`
````
/// <summary>開始時（1783）の国力ランク＝**エンジンの分類そのもの**（列強／準列強／中堅／弱小＝Diplomacy.RankOf）。
/// ★選択画面のために第二の強さ指標を作らない＝世界を1つ組んで <see cref="Diplomacy"/> に数えさせる。
/// ⚠ここは世界の構築を1回走らせる（selectprobe で実測＝約1秒）。開始時のランクは不変なので、
/// 呼び手は結果を保持して使い回すこと。</summary>
````

## L164-166 W 直後 `public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores)`
````
/// <summary>開始時のランクと**国力そのもの**（NationalScore）。選択画面の並べ替えが読む
/// ＝「その他の国」を国力順に並べるのに、ラベル（列強/準列強…）だけでは同ランク内の順が決まらない。
/// ★世界の構築は1回だけ＝ランクと国力を同じ1本の走行から取る（2度組むと遅いうえ食い違いうる）。</summary>
````

## L174-175 W 直後 `public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores, Dictio`
````
/// <summary>★(235) 構築した世界の読み＝開始時のランク・国力・兵の数（千人）。世界の構築は1回だけ（3つを別々に組むと遅いうえ食い違いうる）。選択画面は1度これを呼んで、
/// 難易度（<see cref="Difficulties"/>）と素点（<see cref="Ease"/>）へ兵の数を渡す。</summary>
````

## L186 T `foreach (var p in sim.Diplomacy.Powers) if (p.Nation is not null) soldiers[p.Name] = p.Nat`
````
// 兵の数は構築直後の値のまま（助走の前＝旧と同じ）
````

## L187-188 W 直後 `if (warm) sim.StartWarmup();`
````
// ★(449)(456) ランクと国力は助走の直後の盤面から読む（構築時は市場が回る前＝流れの 3 項が 0 で国力が 3 項に退化する。ゲームの開始のランク〔StartWarmup の末尾で取り直す〕と同じ盤面）。
//   助走は全長で約 6.5 秒〔開発機・61 カ国〕＝選択画面の構築が 0.5 秒から延びる。兵の数だけ要る呼び手は warm:false で通らない（OpeningSoldiers＝約 0.5 秒）。
````

## L190 T `var scoreByPower = NationalScore.ScoresByPower(sim.Diplomacy);`
````
// ★(449) 国力は全国分を 1 回で（国ごとに世界の総量を求め直さない）
````

