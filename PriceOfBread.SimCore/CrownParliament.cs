namespace PriceOfBread.SimCore;

/// <summary>
/// 王権議会＝P0内部状態化（2026-07-10）→P1動学フック（召集・後退）・P2ラベル導出・P3b連続動学・実績ストックまで点火済み。
/// 出典＝docs/2026-07-09_王権議会_史実精査.md（§3新規1-6・§4結論の3.5軸・§4-b協力度2次元・§5の4アクター判断）と docs/2026-07-09_設計メモ.md §2。
/// 毎四半期（Politics.Step 末尾）に ParliamentControl／協力度3チャネル／ArmyAllegiance／RegimeTrackRecord／NationalConsciousness を更新する。
/// ParliamentControl と RegimeTrackRecord は遷移（Politics）・ラベル（<see cref="LabelFromAxes"/>）・軍政判定（PowerAffinity.ArmyHoldsCommand）の一次量。
/// 決定論＝RNGなし（既存状態の純関数のみ）。SaveGame はリフレクション直列化で新フィールドに自動追従。
/// </summary>
public static class CrownParliament
{
    private const int RegimeFamilyCount = 4;                    // 政体実績の4系統（0君主/1議会制/2強権/3急進）
    private const double TensionScale = 200.0;                  // 緊張の尺度（AddTension のクランプ上限）＝外圧を0..1へ正規化して内圧（闘争度）と比べるため
    // ⚠internal＝**読み取り専用の公開**（値も式も不変・InternalsVisibleTo("PriceOfBread.App")）。政治タブ「権力の奪い合い」が
    //   王権の巻き返しの発火条件を**同じ定数**で判定するため（UIが 0.7/0.3 を書き写すと閾値が2箇所に割れる）。
    internal const double CrownRollbackLegitimacyMin = 0.7;      // ストルイピン後退の発火＝高正統性の下限
    internal const double CrownRollbackArmyAllegianceMin = 0.3;  // ストルイピン後退の発火＝王権寄り軍の帰属下限

    /// <summary>毎四半期の導出・更新（Politics.Step 末尾から呼ぶ＝PoliticsEnabled の世界のみ。較正セル/最小テストは Politics が早期 return で不動）。</summary>
    public static void Step(World w, Coefficients c)
    {
        // --- 1. 議会統制力（§3新規1）：franchise（Suffrage）と独立の第2議会軸＝大臣責任・予算権の強度。
        //     観測値（政体からの単純導出）は初期化と ApplyRegime の遷移ジャンプにのみ使う＝EMAなし。初回（-1=未捕捉）は跳躍を作らず観測値で直接初期化。
        double target = ParliamentControlTarget(w.Polity);
        // P2恒久確定（02断線走査2026-07-28）：旧EMA枝（ParliamentControlSpeed・PolityLabelFromAxes<=0のみ到達）を係数ごと削除＝
        // PCはApplyRegimeの遷移ジャンプとP1フック（召集・下賜・後退）だけが動かす一次量であることを構造で保証。
        if (w.ParliamentControl < 0)
            w.ParliamentControl = target;   // 未捕捉のみ初期化（構築時ApplyRegime未経由の小世界向け）
        // P1-H 破産召集（1789三部会）：財政緊縮が深い（AusterityIntensity>0.5）ほど王権は議会に予算協力を乞う＝統制力が上がる。既定0.02で点火（0で休眠）。
        if (c.SummonParliamentRate > 0 && w.AusterityIntensity > 0.5)
            w.ParliamentControl = PoliticalQuantities.Clamp01(w.ParliamentControl + c.SummonParliamentRate * (w.AusterityIntensity - 0.5) * 2.0);
        // P1-E ストルイピン後退：敵対的議会×高正統性×王権寄りの軍＝王権が統制力を段階的に巻き戻す（1907六三クーデタ）。既定0.02で点火（0で休眠）。
        // ★2026-08-12：`w.Crown != CrownForm.None` を追加（旧の旗 CrownlessCollapseKeepsRepublic〔2026-10-18 焼き込み〕）
        //   ＝**王冠の無い共和国で「王権が統制力を巻き戻す」処理が走っていた**。
        //   すぐ下の CrownPressureRate（②王権圧力）と RepublicanDeposition（③廃位）は同じガードを持っており、ここだけ欠けていた。
        //   実測の被害：アメリカ（Republic・Crown=None）の統制力が一方通行で削られて立憲床を割り、LabelFromAxes の
        //   「無君主×議会を潰した＝通常到達不能」と自称する枝から BourgeoisDictatorship へ落ちる。反動政体への遷移は
        //   自由化ラチェットを外すので、参政権 AdultMale→Census・報道 FreePress→Censorship が各1段剥奪される（1923年の実測値と一致）。
        if (c.CrownRollbackRate > 0 && w.Crown != CrownForm.None && w.CoopParliamentCrown < -0.5 && w.Legitimacy > CrownRollbackLegitimacyMin && w.ArmyAllegiance > CrownRollbackArmyAllegianceMin)
            w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.CrownRollbackRate);

        // --- 2. 協力度3チャネル（§5判断③＝6ペア行列でなく3チャネル）---
        // ②議会↔王権：③階級親和（王権の基盤=旧支配層〔判断②〕と議席の構成の距離＝Σ議席×(1−族番号)＝保守の議席−社会の議席∈[−1,1]・王冠なしは0）
        //   − ①政策距離（Policy目標 vs 施行済み現行法/スライダーの正規化乖離＝立法の停滞度。憲法紛争1862型の凍結が高乖離に写る）
        //   − ②紛争記憶（既存 MandateSpent ストック＝反対を押し切った履歴がそのまま敵対の蓄積器＝新規状態を作らず参照・§4-b）。
        //   議会のない政体はチャネル無定義＝0（中立）。「均衡×敵対」の角＝二重権力（露1917二月）を将来ここで観測する。
        bool hasLegislature = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic;
        if (hasLegislature && w.ParliamentEnabled)
        {
            // ★回5-1 項目16／Q16（2026-09-26）：旧＝`1.0 − GoverningParty`（第一党の札で保守+1・自由0・社会−1 に段差で跳ぶ）→ 議席の加重平均へ。
            //   議席の和は1に正規化済み（ComputeVotes）＝保守+1・自由0・社会−1 の重みを議席で平均した値。第一党が替わっても議席が同じなら協力度は動かない。
            //   王冠の無い国（共和国）では王権の基盤との親和がそもそも定義されない＝0（登録簿の Crown で見る＝上のストルイピン後退と同じ守り・旗は読まない）。
            double classAffinity = w.Crown != CrownForm.None ? w.PartySeats[0] - w.PartySeats[2] : 0.0;
            double policyDist = PolicyDistance(w);                               // [0,1]
            // ★レンジの実測（裁定S1・2026-09-21）＝**ここに書いてあった旧の予測「協力度が構造的に優しくなる」は逆だった**（書き直し）。
            //   旧の指数減衰の S は実測で 中央0.0041／p90 0.0487／最大0.1417＝そもそも Min(1.0,·) に一度も触っていない。
            //   θ を退役して在庫だけの門にすると S は 中央0.1196／p90 0.1748／最大0.2399（b 中央0.1804）＝**中央で29倍に厚くなる**（隔離計器・seed1・604四半期）
            //   （線形回復のもとでは常に b 近傍まで使い切るから）。Min(1.0,·) が効かないのは新旧どちらも同じで、変わるのは値の桁。
            //   ⇒ −CoopConflictMemoryWeight×memory は −0.004 → −0.120（最悪 −0.24）＝CrownRollbackRate の発火線
            //     CoopParliamentCrown < −0.5（ストルイピン後退・1907六三クーデタ）へ紛争記憶だけで線の24%を供給する＝**届きやすくなる**。
            //   ⚠例外＝Politics.ConcedeToFaction（内戦の講和）は門を通さず積むので S>b は作れる（そのとき残り a=0＝政治資本が尽きた状態）。
            double memory = System.Math.Min(1.0, w.MandateSpent);                // 紛争記憶（目減り S＝b/40×実行度で線形に薄れる。Politics が毎期回復済み）
            w.CoopParliamentCrown = System.Math.Clamp(
                c.CoopClassAffinityWeight * classAffinity - c.CoopPolicyGapWeight * policyDist - c.CoopConflictMemoryWeight * memory, -1, 1);
        }
        else w.CoopParliamentCrown = 0;

        // ①軍：イデオロギー親和（兵士popの政治的傾き）＋将校の任免権の所在。実体＝ArmyAllegiance（下の帰属導出）。
        //   ★2026-08-18：給与の項は撤去（下の帰属導出のコメント参照）＝給与は向きでなく規律・能力を動かす。
        //   CoopArmyCrown（読み手ゼロ・ArmyAllegiance と入力同一の重複表現）は第3段3-2で削除（2026-07-27）＝pay/lean/milAff は ArmyAllegiance 用に存続。
        // ★pay（給与充足）はここから撤去（2026-08-18）＝帰属を動かさない。給与は規律・能力（MilitaryReadiness）の側だけに効く。
        double lean = SoldierCrownLean(w);                                       // [-1,1]＝兵士の（伝統+反動+ナショ+帝国）−（自由+共和+社会+共産+無政府）
        // ④将校の任免権の所在（2026-08-18・親軍法の撤去）∈[−1,1]。旧＝親軍法ONで一律に王権寄り＝**符号が逆だった**
        // （常備軍こそ王の治安維持力の道具で民兵・皆兵は共和派の対案／軍が王政を倒す例が反復＝伯1889・葡1910・西1931）。
        // 独将校団が王党派だったのは軍旗宣誓が「憲法」でなく「皇帝個人」に向いていたから＝任免権の所在がそのまま忠誠の向きになる。
        // ⚠掛け算の構造（金 pay × 思想/正統性 lean+milAff）は崩さない＝加算にすると「払えば必ず従う」になり1917露・1936西の型が消える。
        double milAff = c.ArmyMilitarismAffinity * PoliticalQuantities.OfficerAppointmentCrownward(w);

        // ③世論↔体制：世論は派生面（§5判断①＝pop層の満足/闘争の集計＝既存データの表面・新規状態ほぼ不要）。
        //   満足した社会=+・高闘争の社会=−。MeanSatisfactionPol/AvgMassMilitancy＝四半期頭の凍結値（第5段5-1・Agg）。
        //   満足項の中立点は分位の写像で導出（Coefficients.CoopPublicSatPivot・2026-09-22＝階層3分割で満足度の水準が下がった／2026-09-25＝満足度を大衆のみへ揃えた）。中立点0.5で旧 2×満足−1 と同じ式。
        //   ★2026-09-25：満足項（MeanSatisfactionPol）と闘争項（AvgMassMilitancy）は同じ母集団（Politics.IsMassPop）を見る。
        w.CoopPublicRegime = System.Math.Clamp((w.MeanSatisfactionPol - c.CoopPublicSatPivot) / (1.0 - c.CoopPublicSatPivot) - Politics.AvgMassMilitancy(w), -1, 1);

        // --- P3b 連続動学（2026-07-12・遷移グラフの置換素子・5係数とも点火済み）：政体の漸進変化を「軸の連続移動＋床交差の再ラベル」で
        //     出す4素子。イデオロギー支持は Transition と同じ物差し（Ideologies.National/SupportAt/BasePolityCoord）＝新表なし。
        //     出口は下の P2 床交差再ラベル（同四半期に反映）。革命・奪取（独裁/無政府/急進暴力）は従来どおり Transition→ExecuteRegimeChange（P3a）が担う。
        // P3b 振動減衰（2026-07-13）：**奪取（独裁・無政府）中は連続動学を凍結**＝ユント/独裁は議会プロセスを停会する（史実：軍政・ボナパルティズムは立憲手続きを止める）。
        //   奪取中も議会/王権圧力がPCを動かすと、奪取が明けた瞬間に不整合なPC値で床を越え**立憲↔軍政が振動**する（P3第1測定の回帰主因＝弱国の振動増）。
        //   奪取中のPCドリフトは可視ラベルに出ない（LabelFromAxes は Seizure 時 PC 無視）が、ApplyRegime が出口でPCを再設定するので凍結は無害＝純粋に spurious drift のみ除く。
        //   廃位と王権の形の変異は元々 Seizure==None を自己ゲート済み＝この外周ゲートで挙動不変（旧の旗 RepublicanDeposition・CrownFormMutation は 2026-10-18 に焼き込み＝ブロックは常に走る）。
        if (w.Seizure == SeizureForm.None)
        {
            var agg = Ideologies.National(w);
            double S(Polity p) => Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(p), c.IdeologyAffinitySlope);
            // 現政体の支持は Transition と同じ**参政権ドリフト**込みで測る（倍率排除⑧段階2＝実際の参政権が現政体の参加軸を動かす）。
            // 素の BasePolityCoord で測ると、専制のAIが発行する紙の参政権で rep−cur が過大になり
            // オマーン1809立憲→軍政→立憲の振動が出る（2026-07-12実測＝ドリフトが旧グラフの吸収器だった）。
            double[] curCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);   // 第5段5-1（2026-07-27）：旧3実装の一元化（従来と同時点＝挙動恒等）
            double cur = Ideologies.SupportAt(agg, curCoord, c.IdeologyAffinitySlope);
            double rep = System.Math.Max(S(Polity.ConstitutionalMonarchy), S(Polity.Republic));
            // ①議会圧力：代議制支持の正差だけ統制力が連続に育つ（改革法案の積み重ね・慣行の確立＝漸進の立憲化）。
            //   保持力ゲート＝動員（改革+急進）が実効治安維持力を超えている間だけ流れる（旧グラフの challenge>coerce と同じ敷居＝
            //   幕府の弾圧は議会運動も潰す。ゲートなしでは較正の幕府帯が全て明治化＝2026-07-12実測）。
            //   慣性バー＝支持差が PolityInertia×正統性を超えることも要る（旧グラフの第2ゲートと同じ物差し。
            //   これなしでは微小な正差の積分でオマーン1809立憲などが出る＝2026-07-12実測）。
            double inertia = c.PolityInertia * w.Legitimacy;
            // P10②権力の呼吸は第3段3-6（2026-07-27）で削除＝RedefinedBreathAmplitude=0（0.3点火は清30%下振れ+仏共和未達で却下済み2026-07-18）の恒等ブロック（×1.0の死荷重）。
            if (c.ParliamentPressureRate > 0 && rep - cur > inertia
                && w.ReformistPower + w.RadicalPower > PoliticalQuantities.EffectiveCoerce(w, c))   // 第5段5-1：coerce 4実装の一元化（従来と同時点＝挙動恒等）
                w.ParliamentControl = PoliticalQuantities.Clamp01(w.ParliamentControl + c.ParliamentPressureRate * (rep - cur));
            // ②王権圧力：王権の形の支持が代議制を上回る分×正統性で巻き戻す（一般化した反動の常時項。P1-Eクーデタ型とは別）。
            //   慣性バーなし＝**旧グラフの平和的復帰の置換素子**（王権は代議制優位でない限り常に取り返す。旧世界では緊縮の
            //   P1-H召集でPCが床を越えてもグラフが翌期Despoticへ戻していた＝これなしではオマーン1809立憲が固着＝2026-07-12実測）。
            if (c.CrownPressureRate > 0 && w.Crown != CrownForm.None)
            {
                double crown = System.Math.Max(S(Polity.Absolutist), System.Math.Max(S(Polity.Despotic), S(Polity.Shogunate)));
                if (crown > rep)
                    w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.CrownPressureRate * (crown - rep) * w.Legitimacy);
            }
            // ②'（第6段6-2・2026-07-28）4主体の欠けていた腕＝軍：王権寄りの軍（lean>0）が文民統制の確立を組織として押し戻す
            //   （プロイセン統帥権・憲法紛争1862＝議会が予算を握っても軍は王に従った）。力＝軍の政治力（6-4と同源＝兵士シェア×(1−PC)）×王権傾き。
            //   これで4主体が揃う＝議会↑(①)・王権↓(②)・軍↓(②'・忠誠条件付き)・大衆(①の保持力ゲート＝動員が押す)。ArmyPcPressure=0で恒等。
            if (c.ArmyPcPressure > 0 && lean > 0)
                w.ParliamentControl = System.Math.Max(0, w.ParliamentControl - c.ArmyPcPressure * lean * PolicyPreferences.ArmyPower(w));
            // ③廃位イベント：議会が優位（統制力≥立憲床）の下で共和の支持が立憲を保持力差で上回る＝王冠の廃止（1792/1910ポルトガル）。
            //   ラベルは下の床交差で共和へ。革命でなく議決＝Dispossessed/Repudiateは通さない。
            if (w.Crown != CrownForm.None && w.Seizure == SeizureForm.None
                && w.ParliamentControl >= c.ConstitutionalControlFloor
                && S(Polity.Republic) - S(Polity.ConstitutionalMonarchy) > c.PolityInertia * w.Legitimacy)
            {
                w.Crown = CrownForm.None;
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の議会が王冠を廃した");
            }
            // ④王権の形の変異＝**実体駆動へ全面再設計**（07_再設計 §1 ＋ A1裁定・2026-07-31）。
            //   ⚠旧＝支持座標駆動（S(FormPolity(f)) の比較）は 1789年に亜細亜の専制が一斉に絶対王政化した＝
            //     形の登録簿（Abs/Des/Sho）は軸で区別されない（P2知見）ため、支持で形を選ぶこと自体が誤りだった。
            //   新＝**形ごとに固有の実体トリガー**を持たせる。国名の特例は書かない（「幕府という形が日本にしかない」ことをデータが運ぶ＝思想②）。
            if (w.Seizure == SeizureForm.None)
            {
                // ★(383) 作者 2026-10-16「行政効率→廃止」：(2) 専制→絶対王政（官僚絶対主義・プロイセン型）の変異は、行政効率が高い状態の継続 HighAdminQuarters（DespoticToAbsolutistAdmin／Quarters）を門にしていた。
                //   その量（4 因子つきの行政効率の閾値）を廃止したので、カウンタごと変異 (2) を物理削除した（専制→絶対王政は政体側の遷移と議会が担う）。
                if (w.Crown == CrownForm.Shogunate)
                {
                    // (1) ★幕府の終わり（A1裁定＝**両経路を許す**）。外圧（黒船＝対外緊張）または内圧（大衆の闘争度）で追い詰められた幕府は倒れる。
                    //     どちらの経路でも「幕府」という形は終わる（将軍→天皇＝親政）が、**下からの圧力が優勢なら議会つき**で終わる：
                    //     議会統制力を立憲の床まで引き上げ、ラベルは下の床交差（P2）に委ねる＝新しいラベル遷移路を書かない。
                    double tension = w.Diplomacy is { } sdip && w.SelfPower is { } ssp ? sdip.MaxTension(ssp) : 0;
                    double mil = Politics.AvgMassMilitancy(w);
                    bool external = tension > c.ShogunateEndTension;
                    bool fromBelow = mil > c.ShogunateEndMilitancy;
                    if (external || fromBelow)
                    {
                        w.Crown = CrownForm.Absolutist;
                        // 内圧が外圧の相対強度（緊張を0..1へ正規化＝開戦閾値200が満点）を上回るなら「下からの倒幕」＝議会つき
                        if (fromBelow && mil >= tension / TensionScale)
                        {
                            w.ParliamentControl = System.Math.Max(w.ParliamentControl, c.ConstitutionalControlFloor);
                            w.LogAdd($"{w.Year} {w.SelfPower?.Name}の幕府が倒れ議会が開かれた");
                        }
                        else w.LogAdd($"{w.Year} {w.SelfPower?.Name}の幕府が倒れ親政に復した");
                    }
                }
                // (3) 絶対王政は**変異しない**＝君主制の外への遷移は既存の政体遷移（Polity側）が担う。後退（絶対→専制）も入れない
                //     ＝行政力の崩壊は政体側の遷移が罰する（二重化しない。制度疲労のスカラーは2026-10-16に削除）。
            }
        }

        // --- P2：床交差の再ラベル（ヒステリシス）＝統制力が立憲床を上抜け/下抜けした時だけラベルが変わる。
        //     召集の累積で1789仏「三部会→立憲」・ストルイピン後退で「停止→専制復帰」が創発。変更時は建国制度（軸→法）を通す
        //     （Dispossessed/RepudiateDebtは通さない＝漸進であり革命でない・設計正典）。
        {
            var derived = LabelFromAxes(w, c);
            if (derived != w.Polity)
            {
                w.RegimeTenureQuarters = 0;   // 実績：床交差の再ラベルも政体交代＝在任年数リセット
                w.Polity = derived;
                Politics.RaiseFoundingInstitutions(w, allowRevoke: true, c);
                w.LogAdd($"{w.Year} {w.SelfPower?.Name}の統治が変わった（{derived}）");
            }
        }

        // --- 3. 軍帰属（§3新規4）：王権(+1)/議会(−1)/自律(0)の連続量。方向はイデオロギー傾き＋将校の任免権の所在。---
        // ★★2026-08-18（欧州・ロシア15事例の検証）：**給与の乗算を撤去**した。旧＝pay×(lean+milAff) で「払えない軍は方向を失う」。
        //   反証が決定的：①ライヒスヴェーアは1920年3月にカップ一揆への発砲を拒み、**同じ給与のまま**4月にルールの労働者を1000人以上殺した
        //     ＝給与変数は符号を変えられない。②カップ派はベルリンを5日間**無給のまま**保持して離反しなかった（次官レヴァルトが一揆側への
        //     給与支払を拒否／国庫を握る合法政府の側に撃つ気のある兵がいなかった）。③ヒトラー一揆を撃ったのは1ドル＝6285億マルクの日。
        //     ④勝った反乱者が自由に要求を書いたとき（ソヴィエト命令第1号7項目＋キール14項目＝計21項目）**金の項目は一つも無い**。
        //     ⑤カラーの将校57名はキャリア保全の申し出を蹴って恩給を捨てた。⑥1830年7月の王室親衛隊は2日間パン無しで離反しなかった。
        //   ⇒ **給与は帰属（向き）でなく規律・能力（大きさ）を動かす**＝出力は脱走・不服従・動員の遅れ・練度低下。
        //     その経路は既に存在する：兵士popの満足↓ → <see cref="World.MilitaryReadiness"/>（士気×装備充足）↓ → 戦力ゲージが痩せる。
        //     1848年3月以前のハプスブルク大蔵省がラデツキーを飢えさせた例がまさにこれ＝**軍は弱くなったが裏切らなかった**。
        //   ⚠旧指示「掛け算の構造（金×思想）を崩すな」は満たしている：金を積んでも帰属は買えない（1917露・1936西の型は lean が担う）。
        w.ArmyAllegiance = System.Math.Clamp(lean + milAff, -1, 1);

        // --- 4. 政体形態の実績ストック（信認⇔失望・経路依存・docs/2026-07-13_形態別失望ストック.md）---
        //     現体制系統に CoopPublicRegime（満足−闘争）を**符号のまま**蓄積（長く満足=信認+／長く不満=失望−）＋半減期50年で全系統減衰。
        //     正の信認は在任年数でゲート（長期成功のみ＝英CMの定着）・負の失望は即時（失敗の刻印は速い）。
        //     読み手＝Transition（RegimeTrackRecordWeight=0.3・現政体族の乗数）・PowerAffinity.ArmyHoldsCommand（[1]の符号）・CivicOpenness（[1]の正部）。
        w.RegimeTenureQuarters++;
        for (int i = 0; i < RegimeFamilyCount; i++) w.RegimeTrackRecord[i] *= 1.0 - c.RegimeRecordDecay;
        // ★政権への失望（2026-09-29）は**1世代**の政治の記憶＝失地・講和の遺恨と同じ半減期20年（LostLandHalflifeQuarters）で薄れる（2026-09-29 線2・推奨で進めた）。
        //   ⚠実績ストックの半減期50年（2世代）ではない＝史実の「失望→秩序の受け入れ」は革命の10年（1789〜99・1848〜51・1868〜74）で積もり次の世代で薄れた
        //   （仏は1789〜1815 の約7回の後も1830・1848 に革命＝50年だと 1830 の期待 H≈0.2・20年だと約0.35）。新しい係数は作らない（世代の長さの1本を読む）。
        // ★2026-09-30 段4e 後半 4e-3：1世代の政治の記憶＝国の世代の置き換わり率（τ＝0＝作者の「1世代で薄れる」そのもの・旧は固定の半減期20年 LostLandHalflifeQuarters＝1783年は近い恒等）。
        w.RecentRegimeChanges *= 1.0 - w.ReplacementRate(Calendar.MonthsPerQuarter);
        double record = w.CoopPublicRegime;   // [-1,1]
        if (record > 0)   // 正の信認は時間が要る（始まったばかりの形態は実績を持てない）
            record *= PoliticalQuantities.Clamp01(w.RegimeTenureQuarters / (Calendar.QuartersPerYear * System.Math.Max(1.0, c.RegimeRecordSuccessTenureGate)));
        int fam = RegimeFamily(w.Polity);
        w.RegimeTrackRecord[fam] = System.Math.Clamp(w.RegimeTrackRecord[fam] + c.RegimeRecordAccumRate * record, -1, 1);

        // --- 5. 国民意識（市民的結束＝民主主義の前提・docs/2026-07-13_国民意識.md）---
        //     公教育×識字＋徴兵で育つ（ロジスティック）・多民族は育ちにくい（天井でなく抵抗＝floorでcivic克服＝米/スイス）・国難は既存結束で符号が決まる両刃。
        //     **読みチャネルが有効な時だけ蓄積を走らせる**＝両weight0では一切走らず厳密恒等（累積の副作用も含めて完全休眠）。
        //     ⚠C2-C5撤去（2026-08-16）：旧・汚職チャネル（civicGraft→Fiscal leak）はV2恒久ONで物理撤去済み＝CorruptionWeight の現読者は本ゲートと World.civicCarriesEthnic（ゲートは実挙動に効く＝残置）。
        if (c.NationalConsciousnessCorruptionWeight > 0 || c.NationBuildMobilizationWeight > 0)
        {
            if (w.NationalConsciousness < 0)   // 未捕捉のみ初期化（跳躍なし）＝データ初期値（既定0.075・米0.32/仏0.1）。第3段3-6：v1（一律0.3）の到達不能枝を焼き込み削除
                w.NationalConsciousness = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
            double eduLvl = w.EducationLaw switch { EducationLaw.Compulsory => 1.0, EducationLaw.Public => 0.6, EducationLaw.Religious => 0.2, _ => 0.0 };
            double popSz = 0, soldierSz = 0, radioSum = 0;
            foreach (var p in w.AllPops()) { popSz += p.Size; if (p.IsSoldier) soldierSz += p.Size; radioSum += p.RadioFill * p.Size; }
            double avgLit = PoliticalQuantities.AvgLiteracy(w);   // 唯一定義（同じ集合・同じ順の加重平均＝ビット恒等・2026-09-11 M8）
            double conscript = popSz > 1e-9 ? soldierSz / popSz : 0;
            {
                // P9 v2 購入モデル（2026-07-17）→ E3財化（2026-07-19）：NC ← NC×(1−減衰) + 蓄積率×(新聞項＋ラジオ項＋徴兵項)×drag×危機×headroom。
                // 第3段3-6（2026-07-27）：NationBuildPurchaseMode=1（2026-07-18点火）を焼き込み＝v1（自動成長）の到達不能本体を削除・v2を直走。
                // 新聞項＝NationBuildingSpending（Fiscal＝識字×NewsFill×熱意の実測）×学校の器。旧・avgLit因子は新聞需要の識字比例に内包済み＝二重を排し、
                // (1−汚職)因子は国庫を通らない家計消費に理論的根拠がなく削除（汚職の代償は汚職サブシステム側＝V2の leak/正統性侵食が担う。旧・読み手 civicGraft はC2-C5撤去2026-08-16で消滅）。
                // ラジオ項＝識字不要・**検閲非依存**（国営宣伝は国民精神を育てるが政治意識は育てない＝Politicsのpress側と分業）。
                // 徴兵項＝軍は国民の学校（非財政・識字不要＝貧国の唯一の経路）。天井＝教育法×識字（法の後退で成長停止＋減衰＝反動の巻き戻し）。
                double spendTerm = w.NationBuildingSpending * eduLvl;
                double radioTerm = c.RadioNationWeight * (popSz > 1e-9 ? radioSum / popSz : 0);
                double conscriptTerm = c.NationBuildConscript * conscript;
                bool nbAtWar = false;
                if (w.Diplomacy is { } dipW && w.SelfPower is { } spW)
                    foreach (var war in dipW.Wars) if (war.A == spW || war.B == spW) { nbAtWar = true; break; }
                double crisisMult = 1.0 + (nbAtWar ? c.NationBuildCrisisDefense : 0)
                                        + (w.InCivilWar ? c.NationBuildCrisisCivilWar : 0)   // ★B3：述語1本
                                        + (w.BornOfSecession && nbAtWar ? c.NationBuildCrisisIndependence : 0);
                double lawCeil = w.EducationLaw switch { EducationLaw.Compulsory => 0.9, EducationLaw.Public => 0.7, EducationLaw.Religious => 0.4, _ => 0.2 };
                double ceilingNb = lawCeil * System.Math.Min(1.0, avgLit / 0.6);
                // ★P12残渣①（原典2026-07-17・旧の旗 CivicOpennessEnabled は 2026-10-18 に焼き込み）：民族抵抗を速度dragから「天井=1−民族距離×(1−開放度)」へ移す。
                // 速度dragは無い（当てがい値 EthnicUnity+NationBuildCivicFloor の廃止）・スカラー経路は国平均の民族距離(1−EthnicUnity)で同型の天井。
                // POP経路は AccumulatePopConsciousness 内で per-pop の民族距離(1−CoreEthnicShare)を天井に適用（core速度係数は1.0へ）
                // ＝読み手（Military の徴兵ゲート NationBuildMobilizationWeight／World.civicCarriesEthnic）が消費する値への民族抵抗は「天井経由の一本」だけになる
                // （原典の二重計上ガード＝分離は忠誠度のみ・NCは動員/天命/汚職のみ＝NCの新しい読み手は作らない）。
                double openness = CivicOpenness(w);
                double ceilScalar = ceilingNb * PoliticalQuantities.ProbOr(w.EthnicUnity, openness);
                double prevNc2 = w.NationalConsciousness;
                double headroom = System.Math.Max(0, ceilScalar - prevNc2);
                double ncDrive = c.NationBuildAccumRate * (spendTerm + radioTerm + conscriptTerm) * crisisMult;   // ★段4：積の順序は旧式のまま（×headroom を外に出しただけ＝ビット恒等）
                double gain = ncDrive * headroom;
                double lose = prevNc2 * c.NationBuildDecayRate;
                // ★★段4（2026-09-09・作者裁定5「国民意識は廃止しない。盤面で読めるようにするだけ」／設計メモ§7-7バグb）：
                //   蓄積スタック（自前の減衰率・天井・蓄積率を持つ記憶）をやめ、**同じ漸化式の不動点**を毎期そのまま置く。
                //     NC ← NC(1−decay) + drive×(天井−NC)  の不動点は  NC* = 天井 × drive/(decay+drive)
                //   ＝新しい量も新しい係数の意味も導入せず（drive・天井・decay は上でいま計算した現物）、記憶だけが消える。
                //   ⇒ 盤面（教育法・識字・新聞/ラジオ支出・徴兵率・民族構成・開放度・戦争）が変われば**その場で**変わる。
                //   ⇒ 国内経路の運搬漏れ（階級昇格・徴兵/復員・職業移動・州内移住が NC を混ぜていない＝§7-7バグb）は
                //     「運ぶべき状態」が無くなるので**定義ごと消える**（保存則の対象でなくなる）。
                //   0＝旧＝ストック＝厳密恒等。点火値 1.0。
                bool ncDerived = c.NationConsciousnessDerived > 0;
                // ★2026-09-03（J-004＝作者裁定「修正対象」）：⚠**台帳が別の量の話をしていた**のが残差の正体だった。
                //   `World.NationalConsciousness` の getter は `NationConsciousnessPerPop>0`（現行1.0）のとき
                //   **POPのsize加重平均を返し、setter が書く _ncScalar を一切読まない**。
                //   ⇒ 上のスカラー漸化式（前期＋新聞＋ラジオ＋徴兵−減衰）は**表示されていない量**の内訳であり、
                //   実値と合わないのは当然だった（記帳漏れの書き手ではなく、**主語の取り違え**）。
                //   ★是正＝POP側の物語で書く（下の PerPop ブロックで Reset して4行を積む）。
                //   ⚠スカラー側の漸化式は _ncScalar の更新として残す（PerPop=0 の世界＝較正/単体テストではこちらが実値）。
                double rawNc2 = ncDerived
                    ? (!(c.NationBuildDecayRate + ncDrive > 0) ? 0.0 : ceilScalar * ncDrive / (c.NationBuildDecayRate + ncDrive))   // NaNは否定形で0側へ倒す
                    : prevNc2 - lose + gain;
                if (w.Explain != null && !(c.NationConsciousnessPerPop > 0))
                {
                    w.Explain.Reset("NationalConsciousness");
                    if (ncDerived)
                    {   // 導出量＝記憶を持たないので「前期」の行は無い。天井と、駆動が天井のどこまで届いたかの2行。
                        w.Explain.Add("NationalConsciousness", "国民化の天井（教育法×識字×民族の距離）", ceilScalar);
                        w.Explain.Add("NationalConsciousness", "天井に届かない分（新聞・ラジオ・徴兵の弱さ）", rawNc2 - ceilScalar);
                    }
                    else
                    {
                        w.Explain.Add("NationalConsciousness", "前期からの国民意識（履歴の累積）", prevNc2);
                        w.Explain.Add("NationalConsciousness", "新聞を読む国民", c.NationBuildAccumRate * spendTerm * crisisMult * headroom);
                        w.Explain.Add("NationalConsciousness", "ラジオ放送", c.NationBuildAccumRate * radioTerm * crisisMult * headroom);
                        w.Explain.Add("NationalConsciousness", "徴兵による育成", c.NationBuildAccumRate * conscriptTerm * crisisMult * headroom);
                        w.Explain.Add("NationalConsciousness", "減衰", -lose);
                    }
                }
                // ★2026-09-03（J-004＝作者裁定「修正対象」）：**クランプ分を記帳する**。
                //   ⚠内訳は 前期＋3つの増分−減衰 の5行なのに、実値は [0,1] でクランプされた値＝
                //   天井/床に当たった四半期だけ内訳合計と実値がずれる（実測＝仏で 4.9e-5）。
                //   ★これは既知の教訓（「Explain会計＝クランプ分も記帳する」）の**同じ型の再発**である。
                //   ⚠Explain は既定 null＝バッチ/較正では1命令も走らない＝厳密恒等。
                w.NationalConsciousness = PoliticalQuantities.Clamp01(rawNc2);   // ⚠PerPop>0 では _ncScalar を書くだけ（getter は読まない）
                // ★国民意識のPOP化（点火2026-08-01）：国のスカラーは上のまま残し、**POP側にも同じ形で積む**。
                // 違いは3つだけ——⑴民族の抵抗を国平均(EthnicUnity)でなく**そのPOPの中核民族シェア**で見る、
                // ⑵**首都からの距離**で配信が減衰する（学校も新聞も鉄道の先には届きにくい）、⑶天井はPOPごとに残る。
                // 国の値は最後に「スカラー」と「POP加重平均」の混合になる（レバー0＝旧値そのまま＝厳密恒等）。
                if (c.NationConsciousnessPerPop > 0)
                {
                    double drive = c.NationBuildAccumRate * (spendTerm + radioTerm + conscriptTerm) * crisisMult;
                    double prevReported = w.NationalConsciousness;   // ★J-004：**実際に報告される量**（POP加重平均）の前期値
                    double popAvg = AccumulatePopConsciousness(w, c, drive, ceilingNb, c.NationBuildDecayRate, openness, out double wGain, out double wLose);
                    double lever = PoliticalQuantities.Clamp01(c.NationConsciousnessPerPop);
                    w.NationalConsciousness = PoliticalQuantities.Clamp01((1 - lever) * w.NationalConsciousness + lever * popAvg);
                    // ★J-004：台帳をPOP側の物語で書き直す＝**報告される量そのもの**の内訳になる。
                    //   ⚠4行目「人口構成の変化」は残差ではなく**実体のある項**＝移民・出生・階級移動で加重（POPの大きさ）が動くと、
                    //   一人ひとりの意識が変わらなくても国の加重平均は動く。単純な漸化式が合わない理由がこれである。
                    if (w.Explain != null)
                    {
                        double reported = w.NationalConsciousness;
                        w.Explain.Reset("NationalConsciousness");
                        w.Explain.Add("NationalConsciousness", "前期の国民意識（POP加重平均）", prevReported);
                        w.Explain.Add("NationalConsciousness", "育成（新聞・ラジオ・徴兵）", wGain);
                        w.Explain.Add("NationalConsciousness", "減衰", -wLose);
                        w.Explain.Add("NationalConsciousness", "人口構成の変化（移民・出生・階級移動）", reported - prevReported - wGain + wLose);
                    }
                    // ★2026-09-03（キューA-04）：**記帳漏れだった書き手**。台帳の恒等式番人を全量へ広げたら仏で 4.9e-5 のズレとして出た。
                    //   ⚠Explain は既定 null＝バッチ/較正では1命令も走らない（恒等）。
                    // ⚠旧「POP加重平均への混合」の行は撤去＝上の4行が**報告される量そのもの**を説明しており、
                    //   スカラーとの差を足すと二重になる（lever=1 では混合という現象自体が無い）。
                }
            }
        }
    }

    /// <summary>★国民意識をPOPへ積み、size加重平均を返す（点火2026-08-01・docs/202607/20260731_国民意識のPOP化_部分完了.md【一部】.md）。
    /// 作者指示「国民意識を国家ラベルでなくPOPにつけたい。**植民地あるいは本国と離れた州、非コアのPOPは上がりづらい**」。
    /// ⚠国名も「植民地かどうかのフラグ」も見ない——**距離と民族構成という現物**だけで植民地の性質が出る
    /// （遠くて中核民族が少ない州＝定義上の植民地）。だから州の身分ラベル(RegionTier)が空でも正しく効く。
    /// ⚠−1＝未初期化のPOPは国の現在値で種を播く（新規POP＝移民や階級移動も同じ＝跳躍しない）。
    /// ★既知の限界の是正（P12残渣②・2026-08-09）：移民が「到着国の平均的な国民意識」を持って現れる件は
    ///   人の移し方の1本 Pop.Blend が出身popのNCをSize加重平均で運搬する（旧の旗 MigrantCarriesConsciousness は 2026-09-30 段4e で焼き込み・国内の移動も同じ1本）（★2026-08-09に点火＝既定1.0）。
    /// ★P12残渣①（旧の旗 CivicOpennessEnabled=1・原典2026-07-17・2026-10-18 焼き込み）：民族抵抗をcore速度係数から原典式の**天井**へ移す＝
    ///   popの国民化天井 = ceiling×(1−民族距離×(1−開放度))・民族距離=1−CoreEthnicShare・開放度=<see cref="CivicOpenness"/>。
    ///   ★2026-08-09に点火。internal＝テストの式検証用（InternalsVisibleTo）。</summary>
    /// <summary>★2026-09-03（J-004）：説明台帳のために**加重平均で見た増分と減衰**も返す（報告専用・力学は読まない）。
    /// ⚠既存の呼び手のために出力は out 引数で足す＝戻り値と本体の計算は1文字も変えない。</summary>
    internal static double AccumulatePopConsciousness(World w, Coefficients c, double drive, double ceiling, double decay, double openness)
        => AccumulatePopConsciousness(w, c, drive, ceiling, decay, openness, out _, out _);

    internal static double AccumulatePopConsciousness(World w, Coefficients c, double drive, double ceiling, double decay, double openness,
                                                     out double wGain, out double wLose)
    {
        bool derived = c.NationConsciousnessDerived > 0;   // ★段4：0＝ストック（旧・厳密恒等）／>0＝毎期の導出量（不動点）
        wGain = 0; wLose = 0;   // ★J-004：早期returnでも定義済みにする（報告専用）
        if (w.Regions.Count == 0) return 0;
        // ★種の値はループの**前に1回だけ**決める（是正2026-08-01・敵対レビューで確定）。
        // ⚠旧はループ内で w.NationalConsciousness を読んでいたが、その getter 自身が「まだ全員 −1 のPOP平均」を
        //   返すため Math.Max(0, −1)=0 になり、**world.json の国別初期値（米0.32・仏0.1・既定0.075）が1ビットも
        //   世界に入らず全国が0から始まっていた**。データから直接取れば自己参照が切れる。
        double seed = w.NationalConsciousness;
        if (!(seed >= 0)) seed = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
        var cap = Climate.CityCoord(w.Regions[0].Name);   // Regions[0]＝首都州（Diplomacy の投射計算と同じ規約）
        double sum = 0, size = 0;
        double gainAcc = 0, loseAcc = 0;   // ★J-004：加重平均で見た増分と減衰（報告専用・力学は読まない）
        foreach (var r in w.Regions)
        {
            var cc = Climate.CityCoord(r.Name);
            double km = Geo.DistanceKm(cap.Lat, cap.Lon, cc.Lat, cc.Lon);
            // 到達＝1/(1+km/減衰長)。⚠**否定形**で書く（NaN座標が来ても reach=1 に落ちず 0 側へ倒れる）
            double reach = !(c.NationConsciousnessReachDecay > 0) ? 1.0
                         : !(km >= 0) ? 0.0
                         : 1.0 / (1.0 + km / c.NationConsciousnessReachDecay);
            foreach (var p in r.Pops)
            {
                // ⚠是正2026-08-01：Closed（貴族・聖職者）を**外さない**。国民意識は労働市場の話ではなく、
                //   むしろ最初に国民意識を持ったのは士官団と聖職者である（プロイセン将校団・仏の聖職者身分の議論）。
                //   外すと彼らが −1 のまま残り、国の加重平均を永久に汚す（旧実装の実害）。
                if (!(p.Size > 1e-9)) continue;
                if (p.NationalConsciousness < 0) p.NationalConsciousness = seed;
                double coreShare = PoliticalQuantities.Clamp01(p.CoreEthnicShare.Value);
                // P12残渣①：民族抵抗は速度でなく天井（天井=ceiling×(1−民族距離×(1−開放度))）
                double ceil = ceiling * PoliticalQuantities.ProbOr(coreShare, openness);
                double head = System.Math.Max(0, ceil - p.NationalConsciousness);
                double before = p.NationalConsciousness;
                if (derived)
                {
                    // ★段4（作者裁定5）：同じ漸化式の**不動点**＝ NC* = 天井 × d/(decay+d)、d = drive×到達。
                    //   記憶を持たない＝同じ盤面なら何度でも同じ値（前期の値を1度も読まない）。⚠NaN/0除算は否定形で0側へ。
                    double d = drive * reach;
                    p.NationalConsciousness = !(decay + d > 0) ? 0.0 : PoliticalQuantities.Clamp01(ceil * d / (decay + d));
                    // 報告専用（台帳の恒等は保つ）：増分/減衰でなく**実際に動いた向き**を積む。
                    double moved = p.NationalConsciousness - before;
                    if (moved > 0) gainAcc += moved * p.Size; else loseAcc += -moved * p.Size;
                }
                else
                {
                    p.NationalConsciousness = PoliticalQuantities.Clamp01(p.NationalConsciousness * (1 - decay) + drive * reach * head);
                    gainAcc += drive * reach * head * p.Size;   // ★J-004：報告専用（クランプ前の増分）
                    loseAcc += before * decay * p.Size;                // ★J-004：報告専用
                }
                sum += p.NationalConsciousness * p.Size; size += p.Size;
            }
        }
        wGain = size > 1e-9 ? gainAcc / size : 0;   // ★J-004：加重平均へ直した増分／減衰
        wLose = size > 1e-9 ? loseAcc / size : 0;
        return size > 1e-9 ? sum / size : 0;
    }

    /// <summary>★P12残渣①（原典2026-07-17 P12節）：市民的開放度∈[0,1]＝「国民の定義」の類型を国別ハードコードせずに導出。
    /// 開放度 = (共和・革命の経験 + 参政権の広さ + 自由度) / 3（等分＝新係数を増やさない・f内部の重みはコード定数）。
    /// ・共和・革命の経験＝現政体の共和/立憲系判定と、議会制系の実績ストック正部（<see cref="World.RegimeTrackRecord"/>[1]＝
    ///   「共和/立憲であった履歴」・半減期50年＝既存の保存状態・新規フィールドなし）の大きい方。
    /// ・参政権の広さ＝<see cref="Ideologies.Openness"/>（参加軸の単一ソース：なし0/制限0.55/男子普通0.80/男女普通0.95）。
    /// ・自由度＝<see cref="World.Freedom"/>（参政権＋報道法から創発済み・LawTension が読むのと同じ0..1）。
    /// 検算（原典の創発リスト）：革命→共和の仏は開放度が跳ね「誰でもフランス人」（jus soli創発）／君主制下で統一した独は
    /// 低開放度でポーランド人が天井に止まり恒久亀裂／米は共和×自由報道で最初から高い（るつぼが特例なしで創発）／
    /// 清・露・オスマンの専制は低い（同化の強行か永遠の火種か）。決定論＝既存状態の純関数・RNGなし。</summary>
    internal static double CivicOpenness(World w)
    {
        double rep = w.Polity switch
        {
            Polity.Republic => 1.0,                    // 共和＝市民的国民の定義そのもの（仏1792・米）
            Polity.ProletarianDictatorship => 0.8,     // 革命の経験＝出自でなく信条の国民（ただし参加は制限される）
            Polity.ConstitutionalMonarchy => 0.6,      // 立憲系＝部分的な市民的定義（英）
            Polity.BourgeoisDictatorship => 0.3,       // 共和の自己クーデタ＝共和経験の残滓（ナポレオン3世型）
            _ => 0.0,                                  // 君主・専制・幕府・軍政・無政府＝血統と身分の国民
        };
        rep = System.Math.Max(rep, PoliticalQuantities.Clamp01(w.RegimeTrackRecord[1]));   // 「あった履歴」＝議会制系の信認ストック正部（専制へ後退しても約2世代は残る）
        double suffrage = Ideologies.Openness(w.Suffrage);
        double freedom = PoliticalQuantities.Clamp01(w.Freedom);
        return PoliticalQuantities.Clamp01((rep + suffrage + freedom) / 3.0);
    }

    /// <summary>政体→実績の4系統（0君主/1議会制/2強権/3急進・docs/2026-07-13_形態別失望ストック.md §3.1）。
    /// 立憲君主制は「議会制の経験」として議会制系（1）に算入＝CMを選ぶ=代議制へ寄る（v1は単一写像・CMの君主/議会両属はP2のPC加重で将来精緻化）。</summary>
    internal static int RegimeFamily(Polity p) => p switch
    {
        Polity.Absolutist or Polity.Despotic or Polity.Shogunate => 0,           // 君主系
        Polity.ConstitutionalMonarchy or Polity.Republic => 1,                   // 議会制系（代議制）
        Polity.MilitaryDictatorship or Polity.BourgeoisDictatorship => 2,        // 強権系
        _ => 3,                                                                   // 急進系（プロレタリア独裁・無政府）
    };

    /// <summary>P2：政体遷移・初期化の**単一の書き込み口**＝登録簿（Crown/Seizure）と政体キャッシュ（w.Polity）を更新。
    /// flag=1 なら PC を新政体の観測目標へジャンプ（革命＝不連続・史実精査§1仏）。flag=0 では登録簿と Polity のみ（dead write＝挙動恒等）。
    /// 規則：共和→廃位（Crown=None）／プロ独裁→廃位＋Seizure=Proletarian（1917）／立憲→Crown維持・NoneならAbsolutist（蘭1815の君主招請）／
    /// 絶対・専制・幕府→その形＋Seizure解除（復古）／ブル独裁・軍政→**Crown維持**＋Seizure（ボナパルティスト皇帝・名目スルタン）／無政府→Crown維持＋Seizure=Anarchy。</summary>
    public static void ApplyRegime(World w, Coefficients c, Polity target)
    {
        switch (target)
        {
            case Polity.Republic: w.Crown = CrownForm.None; w.Seizure = SeizureForm.None; break;
            case Polity.ProletarianDictatorship: w.Crown = CrownForm.None; w.Seizure = SeizureForm.Proletarian; break;
            case Polity.ConstitutionalMonarchy: if (w.Crown == CrownForm.None) w.Crown = CrownForm.Absolutist; w.Seizure = SeizureForm.None; break;
            case Polity.Absolutist: w.Crown = CrownForm.Absolutist; w.Seizure = SeizureForm.None; break;
            case Polity.Despotic: w.Crown = CrownForm.Despotic; w.Seizure = SeizureForm.None; break;
            case Polity.Shogunate: w.Crown = CrownForm.Shogunate; w.Seizure = SeizureForm.None; break;
            case Polity.BourgeoisDictatorship: w.Seizure = SeizureForm.Bourgeois; break;
            case Polity.MilitaryDictatorship: w.Seizure = SeizureForm.Military; break;
            case Polity.Anarchy: w.Seizure = SeizureForm.Anarchy; break;
        }
        if (w.Polity != target)
        {
            w.RegimeTenureQuarters = 0;   // 実績：政体交代で在任年数リセット（新体制は実績ゼロから積み直す）
            w.RecentRegimeChanges += 1;   // ★政権への失望（2026-09-29）：事件の政変を1回と数える（即時の政変・内戦の決着・矯正講和）。⚠床交差の再ラベル（Step の P2＝漸進）は数えない
        }
        w.Polity = target;
        w.ParliamentControl = ParliamentControlTarget(target);
    }

    /// <summary>P2：軸（登録簿＋議会統制力）→政体ラベルの導出（純関数）。奪取が最優先→統制力の床（現ラベルが立憲/共和なら
    /// 下抜け床＝ヒステリシス）→王権の形。Crown=None×低統制＝ブル独裁（共和大統領の自己クーデタ＝ナポレオン3世型・Rollback経路で開通）。</summary>
    public static Polity LabelFromAxes(World w, Coefficients c)
    {
        switch (w.Seizure)
        {
            case SeizureForm.Anarchy: return Polity.Anarchy;
            case SeizureForm.Military: return Polity.MilitaryDictatorship;
            case SeizureForm.Proletarian: return Polity.ProletarianDictatorship;
            case SeizureForm.Bourgeois: return Polity.BourgeoisDictatorship;
        }
        double floor = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
            ? c.ConstitutionalControlExit : c.ConstitutionalControlFloor;   // ヒステリシス：立憲系は下抜け床で判定
        if (w.ParliamentControl >= floor)
            return w.Crown == CrownForm.None ? Polity.Republic : Polity.ConstitutionalMonarchy;
        // ★2026-08-12：下の `_ =>` は自身のコメントで「通常到達不能」と宣言しているのに、Crown=None の共和国で
        //   統制力が床を割れば必ずここへ来る＝アメリカが自己クーデタ扱いでブルジョワ独裁になっていた（実測）。
        //   奪った主体が居ないのに独裁の名を付けるのが誤り＝**奪取（Seizure）は上の switch が既に全て捌いている**ので、
        //   王冠も奪取も無いなら「議会の弱い共和国」であって共和国のままである。（旧の旗 CrownlessCollapseKeepsRepublic は 2026-10-18 に焼き込み。）
        if (w.Crown == CrownForm.None) return Polity.Republic;
        return w.Crown switch
        {
            CrownForm.Absolutist => Polity.Absolutist,
            CrownForm.Despotic => Polity.Despotic,
            CrownForm.Shogunate => Polity.Shogunate,
            _ => Polity.BourgeoisDictatorship,   // 無君主×議会を潰した＝自己クーデタ（上のガードが 0 のときだけ到達）
        };
    }

    // 政体→議会統制力の観測目標。P2点火後は未捕捉時の初期化と ApplyRegime の遷移ジャンプ先としてのみ使う（以後の PC は P1/P3b の動学が動かす一次量）。
    // 共和=1.0（行政・予算は議会が握る）・立憲=0.65（大臣責任は確立・王権留保が残る英型）・
    // 独裁系＝翼賛議会（存在するが統制力なし＝§4-bの「議会を利用する王権」）・絶対/専制/幕府/無政府=0。
    private static double ParliamentControlTarget(Polity p) => p switch
    {
        Polity.Republic => 1.0,
        Polity.ConstitutionalMonarchy => 0.65,
        Polity.BourgeoisDictatorship => 0.30,
        Polity.ProletarianDictatorship => 0.20,
        Polity.MilitaryDictatorship => 0.10,
        _ => 0.0,   // Absolutist / Despotic / Shogunate / Anarchy＝議会の行政・予算統制なし
    };

    // 兵士popの王権寄りイデオロギー傾き∈[-1,1]（人口加重）：（伝統0+反動1+ナショ4+帝国8+ファシズム9+王党派10）−（自由2+共和3+社会5+共産6+無政府7）。
    // 兵士不在（最小テスト世界等）は0＝中立。IdeologyShares は合計1に正規化済みなので差も[-1,1]。
    // ⚠internal＝**読み取り専用の公開**（純関数・World非変更・InternalsVisibleTo("PriceOfBread.App")）。政治タブ「権力の奪い合い」が
    //   軍の圧力（②'）と軍の忠誠の内訳を出すのに、この傾きをUI側で組み直さず**この関数を呼ぶ**ため。
    internal static double SoldierCrownLean(World w)
    {
        double sum = 0, size = 0;
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            var s = p.IdeologyShares;
            // 第5段5-1（2026-07-27）：思想→3族の写像を PoliticalQuantities.FamilyOf に一元化（王権寄り＝保守族0・反王権＝自由1+社会2＝従来と同値。ファシズム9・王党派10=保守側）。
            double crown = 0, anti = 0;
            for (int i = 0; i < Ideologies.Count; i++)
                if (PoliticalQuantities.FamilyOf(i) == 0) crown += s[i]; else anti += s[i];
            sum += (crown - anti) * p.Size;
            size += p.Size;
        }
        return size > 1e-9 ? sum / size : 0.0;
    }

    // 政策距離∈[0,1]＝Policy の目標と施行済み現行値の正規化乖離の平均。
    // 目標は教義（SelfPlay）・プレイヤー・要求への応答が置く＝議会は目標を書かない（2026-09-19 に経済軸の綱領・2026-09-26 に法軸の綱領を撤去＝回5-1 項目17。
    // 法軸の旧綱領の段は AI の意図〔UtilityAgent.Apply〕へ移した）。党の選好は議席×党選好として純支持に入る。
    // 「国家が目指す法」と「実際に施行されている法」のギャップ＝立法の停滞度（政治ゲートに阻まれた目標＝議会の不満）。
    // enum＝|目標−現行|/レンジ・bool＝不一致1・スライダー＝|目標−現行|（いずれも[0,1]）。既存データのみ＝純関数。
    private static double PolicyDistance(World w)
    {
        var p = w.Policy;
        double sum = 0; int n = 0;
        void Enum2<T>(T cur, T tgt) where T : struct, System.Enum
        {
            int range = System.Enum.GetValues<T>().Length - 1;
            if (range <= 0) return;
            sum += System.Math.Abs(System.Convert.ToInt32(tgt) - System.Convert.ToInt32(cur)) / (double)range; n++;
        }
        void Slider(double cur, double tgt) { sum += PoliticalQuantities.Clamp01(System.Math.Abs(tgt - cur)); n++; }

        Enum2(w.Suffrage, p.Suffrage);
        Enum2(w.EducationLaw, p.EducationLaw);
        Enum2(w.Censorship, p.Censorship);
        Enum2(w.LaborUnion, p.LaborUnion);
        Enum2(w.WorkHours, p.WorkHours);
        Enum2(w.CornLaws, p.CornLaws);
        Enum2(w.LandReform, p.LandReform);
        Enum2(w.SerfEmancipation, p.SerfEmancipation);
        // 02断線走査（2026-07-28）：Enum2(w.Pension, p.Pension)を削除＝p.Pensionは施行経路を持たず、自由党綱領のLimitedが恒久0.5/16の距離オフセットとなり「自由党内閣は永久に王権と非協力」という偽力学を作っていた（分母nは16→15）。
        // ★親軍法の撤去（2026-08-18）：Bool2(w.Militarism, p.Militarism) を削除＝政策距離の項数 n は14→13（現在 n=13＝enum 9本＋スライダー4本）。
        //   ⚠代わりに徴兵法を足すことはしない（綱領は徴兵法の目標を書かない＝02断線走査で p.Pension を外したのと同じ理由。
        //     書き手のない目標を距離に混ぜると恒久オフセットになり「与党は永久に王権と非協力」という偽力学を作る）。
        // ★女性の権利（統合法・2026-08-10）：旧＝財産権bool＋参政権boolの**2項**（教育権は項に無かった＝非対称）→ 4段の1項へ。
        //   刻みは 0/1 の2値から 0/⅓/⅔/1 になり、項数 n は 2→1 に減る（＝王権×議会の協力度が動く点として記録）。
        Enum2(w.WomenRights, p.WomenRights);
        Slider(w.Tariff, p.TariffTarget);                    // 関税（保護/自由貿易）
        Slider(w.SocialSecurity, p.SocialSecurityTarget);    // 福祉
        Slider(w.ProgressiveTax, p.ProgressiveTaxTarget);    // 累進
        Slider(w.TaxRate.Value, p.IncomeTaxTarget);                // 所得税（予算権の物差し＝§2-C 予算闘争の核）
        return n > 0 ? sum / n : 0.0;
    }
}
