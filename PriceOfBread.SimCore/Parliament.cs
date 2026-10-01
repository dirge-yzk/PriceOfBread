namespace PriceOfBread.SimCore;

/// <summary>
/// 政党1つ（F4政党統一・2026-07-12）。常設3党（保守/自由/社会＝族の受け皿・index0-2固定）＋クラスタ由来の動的政党。
/// **互換射影**＝World.PartySeats[3]/GoverningParty は常に「族」の集計値のまま（既存読者は全て無改修・Runnerの3長配列も安全）。
/// 動的政党は族内で得票を分け合う（Σ族=不変）＝DynamicParties（既定1.0）を0にすると常設3党のみ。
/// FactionState参照は持たない（セーブの参照グラフを単純に保つ＝FactionStateと同じ「Region参照＋値」の形）。
/// </summary>
public sealed class PoliticalParty
{
    public string Name = "";
    public int Family;                 // 0=保守 1=自由 2=社会（互換射影の族）
    // 【墓標2026-09-26・回5-1 項目17】Bloc（給源ブロック＝動的与党の選挙時の要求の導出元）＝唯一の読み手（要求の押し込み）の撤去で書くだけの欄になった＝削除（思想⑩）
    public int Ideo = -1;              // イデオロギー（動的政党＝国民政党のキー。常設=-1）
    public double VoteShare;           // 全国得票率
    public bool Permanent;             // 常設3党
    public int AbsentQuarters;         // 適格クラスタの連続不在（10年で解党）
}

/// <summary>
/// 議会・選挙・政党（段5）。政体（権威/自由の層）の**上に乗る層**：自由な政体では数年ごとの選挙で与党が決まり、
/// 議席が**党選好×議席**として法の通りやすさ（PolicyPreferences.ParliamentNet→NetSupport）に入る＝**選挙で政権交代→通る向きが変わる**。
/// 議会は目標（w.Policy）を1つも書かない（2026-09-26・回5-1 項目17＝綱領〔法軸〕と動的与党の選挙時の要求を撤去。経済軸の綱領は 2026-09-19 に撤去済み）。
/// 党が効くのは法の通りやすさ（議席×党選好＝ParliamentNet）だけ。法軸の旧綱領の段は AI の意図（UtilityAgent.Apply の「議会の構成」の段落）へ移した＝プレイヤーの目標は上書きされない。
/// 政党＝11イデオロギー（<see cref="Ideologies"/>）の3族クラスタ（PoliticalQuantities.FamilyOf）：保守(伝統/反動/ナショ/帝国/ファシズム/王党派)・自由(自由/共和)・社会(社会/共産/無政府)。
/// 得票＝Σ 有権者(規模×参政権ゲート) × そのpopのイデオロギー比。＝参政権の拡大が大衆政党(社会/自由)を押し上げる中心ドラマ。
/// 経済運営（行政費・財政ストレス対応・工業化）は doctrine（<see cref="UtilityAgent"/>）のまま＝党は「色」を、AIは「経営」を担う。
/// <b>較正保護</b>：ParliamentEnabled が false（合成較正・最小テスト）では即 return＝政体遷移モデルに無干渉。
/// </summary>
public static class Parliament
{
    public enum Party { Conservative, Liberal, Socialist }

    private const int PartyAbsenceDissolveQuarters = 40;   // 適格クラスタの連続不在で解党（10年）

    public static void Step(World w, Coefficients c)
    {
        if (!w.ParliamentEnabled || !w.PoliticsEnabled || w.InCivilWar) return;   // ★B3：述語1本   // 内戦中は平時の選挙を止める（戦乱中の政権交代は非現実的。A-4#12）

        if (w.Parties.Count == 0)   // 遅延初期化（較正/最小世界＝ParliamentEnabled=falseなら空のまま）
        {
            w.Parties.Add(new PoliticalParty { Name = "保守党", Family = 0, Permanent = true });
            w.Parties.Add(new PoliticalParty { Name = "自由党", Family = 1, Permanent = true });
            w.Parties.Add(new PoliticalParty { Name = "社会党", Family = 2, Permanent = true });
        }
        if (c.DynamicParties > 0) SyncDynamicParties(w, c);   // F4：クラスタ→政党の出入り（既定1.0・0で常設3党のみ）
        StepManifesto(w, c);   // ★公約の達成判定と支持の減衰（作者指示2026-08-24）

        // 政7：得票率（世論）の毎期計算。SeatsFrozenBetweenElections（既定1.0）：得票率を VoteShares に置き、PartySeats（政治力学＝ConstMonarchyErosion・
        // 一党支配判定・法案の議席加重が読む）は選挙時のスナップショットで固定（レームダック・任期中の民意乖離）。0で PartySeats を毎期上書き＝表示と議席が同一。
        // ⚠是正（第2段2-5・2026-07-27）：旧注記「VoteShares＝表示専用」は嘘＝**選挙の新与党判定（Winner）が現在の民意として VoteShares を読む**（凍結時に確定議席でなく民意で交代を決める＝実質の力学読み手）。
        ComputeVotes(w, c);
        // ★開院時の議席（2026-08-04・作者指摘「議会の存在感が薄い」）＝**開幕の議会が空席だった**バグの是正。
        //   旧＝選挙政体の PartySeats は QuartersSinceElection が ElectionIntervalQuarters(=16) に達する**最初の総選挙まで全て0**。
        //   その間 ParliamentNet も OpposingPower の議会項も 0＝**1783〜1787の4年間、英も米も議会が法に一切効かない**。
        //   1783年の英には1780年総選挙で選ばれた議会が現に座っている＝「開幕は空席」は史実でも設計でもない単なる初期化漏れ。
        //   直し方＝**新しい判断を持ち込まず**、非選挙政体が毎期使っているのと同じ SnapshotSeats を開院時に1回だけ呼ぶ
        //   ＝「初回は直近の総選挙の結果が既に出ている」。以後は従来どおり16四半期ごと。
        //   ⚠ SeatsFrozenBetweenElections=0（凍結なし）では SnapshotSeats 自体が不要（VoteShares と PartySeats が毎期恒等）。
        //   ⚠ ParliamentSeatOnOpen=0 で旧＝空席のまま（休眠・厳密恒等）。
        //   ⚠**初回Stepでは席を取らない**：Simulation.Step では Parliament.Step が Politics.Step（＝Ideology.UpdateShares）より
        //     先に走るので、1四半期目の IdeologyShares は全て0＝ComputeVotes の縮退フォールバック（族が全く無い時は保守1）で
        //     得票が保守100%になる。そこで席を固定すると**開幕の議会が丸ごと保守党**という嘘の議会が4年間座り続ける（実測で確認）。
        //     QuartersSinceElection>=1＝少なくとも1回 Politics.Step を通った後＝思想分布が出そろっている。
        //   ⚠議席が空のときだけ＝一度きり（選挙後の再入なし・専制から立憲へ移った国は既に席があるので発火しない）。
        bool seatsEmpty = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2] < 1e-9;
        if (seatsEmpty && w.QuartersSinceElection >= 1
            && c.ParliamentSeatOnOpen > 0 && c.SeatsFrozenBetweenElections > 0
            && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic)
        {
            SnapshotSeats(w);
            w.GoverningParty = (int)Winner(w);   // 開院時の与党＝第一党（選挙と同じ判定）
        }
        w.QuartersSinceElection++;

        bool hasElections = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic;
        if (hasElections)
        {
            w.QuartersWithElections++;   // E0計器：選挙政体の在籍四半期（平均間隔の分子）。カウンタのみ＝挙動不変
            // 機能2-5（2026-07-28）：不信任＝与党の民意（VoteShares）が閾値を割ると任期途中でも繰り上げ選挙（第三共和政の短命内閣・
            // 支持基盤の崩れた内閣は保たない）。最短間隔EarlyElectionMinQuartersで選挙の乱発を防ぐ。EarlyElectionEnabled=0で旧＝固定周期のみ恒等。
            bool noConfidence = c.EarlyElectionEnabled > 0
                && w.QuartersSinceElection >= (int)c.EarlyElectionMinQuarters
                && w.GoverningParty is >= 0 and < 3 && w.VoteShares[w.GoverningParty] < c.NoConfidenceThreshold;
            // ★E3公示（選挙doc §3-0/3-2・ElectionEventEnabled=1で点火済み2026-08-17）：次の四半期が定期総選挙なら公示が立つ。
            //   繰り上げ（不信任）には公示なし＝準備の時間が無いのが史実の解散総選挙。
            // ★E4・AI等価物（作者裁定2026-08-17「AI・プレイヤー完全等価。プレイヤーにのみ使える手段があってはならない」）：
            //   旧＝`w.SelfPower?.IsPlayer == true` で**プレイヤー国にだけ公示が立ち、AIは常に即決＝素の得票**だった
            //   ＝選挙への介入がプレイヤー専用の手段になっていた（総動員・移民法・国教法・幽霊戦線と同じ形の非対称）。
            //   直し方＝**機構としての公示は全国に立てる**／モーダルを出すかどうかはUIの都合（App が PendingElection を見て
            //   プレイヤー国のときだけポップアップする）＝UIの都合を SimCore に持ち込まない。
            //   AI国は**この場で**同じ動詞（World.ResolveElection）に答える＝未回答の公示を次の開票（下の開票時クリア）まで持ち越さない。
            //   ⚠プレイヤー国の公示は本人が答えるまで残る（既存挙動）＝AIが人の選挙を代行しない（Politics の危機イベントと同じ規律）。
            if (c.ElectionEventEnabled > 0 && w.PendingElection is null
                && !noConfidence && w.QuartersSinceElection == c.ElectionIntervalQuarters - 1)
            {
                w.PendingElection = BuildElectionEvent(w, c);
                w.ElectionCalls++;   // 診断カウンタ（書くだけ・力学は読まない）
                if (w.SelfPower?.IsPlayer != true) UtilityAgent.DecideElection(w);
            }
            if (w.QuartersSinceElection >= c.ElectionIntervalQuarters || noConfidence)
            {
                if (noConfidence && w.QuartersSinceElection < c.ElectionIntervalQuarters)
                    w.LogAdd($"{w.Year} {w.SelfPower?.Name}で内閣不信任＝繰り上げ総選挙");
                w.GoverningParty = (int)Winner(w);   // 選挙＝最大党が組閣（過半数なくても第一党）。現職効果は在任期間で効くのでカウンタを0に戻す前に勝者を確定（IncumbencyBias=0 の既定では順序不問＝恒等）
                w.QuartersSinceElection = 0;
                w.ElectionsHeld++;   // E0計器：総選挙の実施（定期・繰り上げの別を問わない）
                if (c.SeatsFrozenBetweenElections > 0) SnapshotSeats(w);   // 政7：選挙で議席を確定（以後の世論変動は VoteShares のみ動く）
                // ★E3：加担の押し込みは開票1回で消費（未回答の公示も流す＝択の先送りは「何もしない」と同じ）
                if (c.ElectionEventEnabled > 0) { w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.PendingElection = null; }
            }
        }
        else
        {
            // 権威主義・独裁＝選挙なし。与党＝統治ブロックの党。
            w.GoverningParty = (int)RulingParty(w.Polity);
            w.QuartersSinceElection = 0;
            // ★E4（2026-08-17）：議会が消えた（政変で非選挙政体へ転げた）＝**公示も加担も失効**する。
            //   ⚠これが無いと、公示から開票までの間に政体が転げた国で押し込み（ElectionPushIdeology）が開票時のクリアに永久に到達せず、
            //     ComputeVotes は毎四半期走る（非選挙政体は毎期 SnapshotSeats）ので**一度きりの動詞が恒久の議席補正になる**。
            //   プレイヤー国・AI国の区別なし＝同じ機構（既定0では1つも状態を書かない＝恒等）。
            if (c.ElectionEventEnabled > 0) { w.ElectionPushIdeology = -1; w.ElectionPushStrength = 0; w.PendingElection = null; }
            if (c.SeatsFrozenBetweenElections > 0) SnapshotSeats(w);   // 政7：非選挙政体は毎期スナップショット＝VoteShares と PartySeats は恒等（凍結でも従来同値）
        }

        w.GoverningPartyName = GoverningPartyOf(w)?.Name ?? "";   // F4：与党の実体名（族内最大得票・未初期化=空でUIが族名へフォールバック）
        // 【墓標2026-09-26・回5-1 項目17】ここにあった3つを撤去＝議会は目標を1つも書かない。
        //   ①綱領 ApplyPlatform（選挙政体で自由党・社会党が与党だと、毎四半期、教育法・参政権・労働時間・労組の目標をプレイヤー国の分まで上書き）
        //   ②動的与党の選挙時の要求（FactionSnapshot.ComputeDemands→TargetDemand を門なしで。関税・累進・社会保障を含む軸を上げ下げ1段ずつ）
        //   ③非選挙政体の return（係数 NonElectedSkipsPlatform＝①②の後ろに何も残らず意味を失った）。
        //   法が通るか・どれだけ速く動くかは従来どおり議席×党選好（PolicyPreferences.ParliamentNet→NetSupport・OpposingPower）。
        //   ①の段は AI の意図（UtilityAgent.Apply の「議会の構成」の段落・与党の札で読む）へ移した＝値も適用範囲も同じで、門（GateAllLawTargets）の前に置く。
    }

    /// <summary>機能2-4（2026-07-28）：議会の解散＝任期満了扱いにして次のStepで総選挙（プレイヤーの動詞。AI側の等価物＝不信任の繰り上げ選挙2-5）。</summary>
    public static void Dissolve(World w)
    {
        if (w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && w.ParliamentEnabled)
            w.QuartersSinceElection = w.Coef.ElectionIntervalQuarters;
    }

    // 政7：現在の得票率（VoteShares）を確定議席（PartySeats）へ書き写す。選挙時（選挙政体）・毎期（非選挙政体）に呼ぶ。
    private static void SnapshotSeats(World w)
    {
        w.PartySeats[0] = w.VoteShares[0];
        w.PartySeats[1] = w.VoteShares[1];
        w.PartySeats[2] = w.VoteShares[2];
    }

    // 議席シェア（得票率）。有権者（参政権ゲートを通る規模）× そのpopのイデオロギー比 を党へ集計。
    /// <summary>党別の支持基盤（2026-08-04・作者指摘「POPの誘導が困難で、議会に影響を与えるのが難しい」）。
    /// 階級ごとに「有権者としての重み」と「その票が3族へどう割れるか」を返す＝**誰を動かせば議席が動くか**の地図。
    /// ★ComputeVotes と**同じ式**（FranchiseVoice × IdeologyShares の族集計）＝表示と実体が一致する。副作用なし。</summary>
    public static List<(string Class, double Voters, double Cons, double Lib, double Soc)> SupportBase(World w)
    {
        var by = new Dictionary<string, (double v, double c, double l, double s)>();
        var eff = w.Suffrage;
        foreach (var p in w.AllPops())
        {
            double voters = p.Size * InterestGroups.FranchiseVoice(p, eff, w.Coef);
            if (voters <= 1e-9) continue;
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += p.IdeologyShares[i]; else if (fam == 1) pl += p.IdeologyShares[i]; else ps += p.IdeologyShares[i];
            }
            double sum = pc + pl + ps; if (sum < 1e-9) { pc = 1; sum = 1; }
            by.TryGetValue(p.ClassName, out var acc);
            by[p.ClassName] = (acc.v + voters, acc.c + voters * pc / sum, acc.l + voters * pl / sum, acc.s + voters * ps / sum);
        }
        var list = new List<(string, double, double, double, double)>();
        foreach (var kv in by) list.Add((kv.Key, kv.Value.v, kv.Value.c, kv.Value.l, kv.Value.s));
        // 票の多い順。同数は階級名で決める＝Dictionary の列挙順（挿入順に依存）が表示に漏れない。
        list.Sort((a, b) => b.Item2 != a.Item2 ? b.Item2.CompareTo(a.Item2) : string.CompareOrdinal(a.Item1, b.Item1));
        return list;
    }

    private static void ComputeVotes(World w, Coefficients c)
    {
        var eff = w.Suffrage;   // 実効参政権＝法律値（政体含意は floor-raise で法へ畳み込み済・倍率排除②）
        // ★E3（選挙doc §3-2・ElectionEventEnabled=0または未加担＝push=-1＝分岐に一度も入らない＝ビット恒等）：
        //   加担した思想のシェアだけ (1+効き×Scale) 倍で票を集計＝その pop の中で票が移る（voters総量は保存）。開票1回で消費（Step側でリセット）。
        //   ★E3''（2026-08-17）：ElectionPushStrength は**効き**＝投入額の逓減 A/(A+半減点)∈[0,1)（World.ElectionPushEffectOf）
        //   ＝ここは「いくら注いだか」を知らない（金と効きの翻訳は唯一の口 World.ResolveElection が済ませている）。
        int push = c.ElectionEventEnabled > 0 ? w.ElectionPushIdeology : -1;
        double pushMul = 1.0 + w.ElectionPushStrength * c.ElectionPushScale;
        double cons = 0, lib = 0, soc = 0;
        // F4：政党別の得票の素材（族の中の分割）＝動的政党がある時だけ、同じ走査で思想別の票も積む（走査を2度しない）。
        // 動的政党（国民政党＝イデオロギー単位）の票田＝その思想への全国票。
        bool dynamic = w.Parties.Count > 3;
        var ideoRaw = new double[Ideologies.Count];
        foreach (var p in w.AllPops())
        {
            double voters = p.Size * InterestGroups.FranchiseVoice(p, eff, w.Coef);
            if (voters <= 1e-9) continue;
            var s = p.IdeologyShares;
            // 第5段5-1（2026-07-27）：思想→3族の写像を PoliticalQuantities.FamilyOf に一元化（保守=伝統0+反動1+ナショ4+帝国8+ファシズム9+王党派10・自由=2+3・社会=5+6+7＝従来と同値）。
            double pc = 0, pl = 0, ps = 0;
            for (int i = 0; i < Ideologies.Count; i++)
            {
                double sv = i == push ? s[i] * pushMul : s[i];
                int fam = PoliticalQuantities.FamilyOf(i);
                if (fam == 0) pc += sv; else if (fam == 1) pl += sv; else ps += sv;
            }
            double sum = pc + pl + ps;
            if (sum < 1e-9) { pc = 1; sum = 1; }      // 退避＝保守（伝統100%等）
            cons += voters * pc / sum; lib += voters * pl / sum; soc += voters * ps / sum;
            if (dynamic)
            {
                double sumI = s[0] + s[1] + s[4] + s[8] + s[2] + s[3] + s[5] + s[6] + s[7] + s[9] + s[10];   // ファシズム(9)・王党派(10)も算入（E7/2026-08-29点火・両ゲート0で+0＝恒等。wave2b：Ideologies.Count=11への型拡張後の旧読み手＝王党派だけ分母から漏れて族計÷(1−s[10])に膨張していた）
                if (push >= 0) sumI += s[push] * (pushMul - 1.0);   // ★E3：族集計と同じ押し込みを党内訳にも（push=-1＝実行されない＝恒等）
                if (!(sumI < 1e-9))   // 思想なし＝族集計では保守族へ退避済み（常設保守が引き算で受ける）
                    for (int i = 0; i < Ideologies.Count; i++) ideoRaw[i] += voters * (i == push ? s[i] * pushMul : s[i]) / sumI;
            }
        }
        double t = cons + lib + soc; if (t < 1e-9) t = 1;
        double v0 = cons / t, v1 = lib / t, v2 = soc / t;   // 全国得票率（純比例）
        // 政9：得票→議席の制度歪み（第一党ボーナス＝小選挙区の死票の近似）。ElectoralDistortion（既定0.3）：0で純比例（恒等）。
        // >0で得票率を凸変換（v^(1+γ) を再正規化）＝第一党の議席プレミアム・小党の過小代表。プロイセン三級選挙制・単純小選挙区の歪みを一係数で。
        if (c.ElectoralDistortion > 0)
        {
            double e = 1.0 + c.ElectoralDistortion;
            double d0 = System.Math.Pow(v0, e), d1 = System.Math.Pow(v1, e), d2 = System.Math.Pow(v2, e);
            double dt = d0 + d1 + d2; if (dt < 1e-9) dt = 1;
            v0 = d0 / dt; v1 = d1 / dt; v2 = d2 / dt;
        }
        // 政7：得票率は VoteShares に置き、PartySeats（政治力学が読む確定議席）は SnapshotSeats（選挙時/非選挙政体の毎期）でのみ更新＝レームダック。
        // 第3段3-6（2026-07-27）：SeatsFrozenBetweenElections=1.0（政7点火）を焼き込み＝旧・毎期上書き枝（到達不能）を削除。
        w.VoteShares[0] = v0; w.VoteShares[1] = v1; w.VoteShares[2] = v2;

        // F4：政党別の得票（族の中の分割＝**族計は上のv0-2と厳密一致**＝互換射影の恒等）。常設党＝族計−Σ動的（族の残り全部）。
        // 政9の歪みは族単位で掛かった後を比例配分（族内の相対比は素の得票比）。
        if (dynamic)
        {
            double[] famRaw = [cons, lib, soc]; double[] famFinal = [v0, v1, v2];
            double[] dynSum = new double[3];
            foreach (var pt in w.Parties)
            {
                if (pt.Permanent) continue;
                pt.VoteShare = famRaw[pt.Family] > 1e-9 ? ideoRaw[pt.Ideo] / famRaw[pt.Family] * famFinal[pt.Family] : 0;
                dynSum[pt.Family] += pt.VoteShare;
            }
            foreach (var pt in w.Parties)
                if (pt.Permanent) pt.VoteShare = System.Math.Max(0, famFinal[pt.Family] - dynSum[pt.Family]);
        }
        else foreach (var pt in w.Parties) if (pt.Permanent) pt.VoteShare = pt.Family == 0 ? v0 : pt.Family == 1 ? v1 : v2;
    }

    // F4：クラスタ→政党の出入り（裁定2026-07-11「統制力>0かつ参政権がブロックに票を与えるクラスタが政党化・他は在野運動」）。
    // 政党は**国民政党＝イデオロギー単位**（SPDは一つの党＝州ごとの支部ではない・共産党は社会党から割れる）＝動的政党の上限＝思想数（Ideologies.Count）/国でチャーンなし
    // （州×ブロック×主軸の粒度は結党649-909回/125年の明滅チャーンになった実測2026-07-12）。族＝ComputeVotesと同じ3分類。
    // 政党は運動より粘る組織（機関紙・党舎・名簿）＝適格クラスタの10年不在で解党（票は族の常設党へ戻る）。
    private static void SyncDynamicParties(World w, Coefficients c)
    {
        for (int i = w.Parties.Count - 1; i >= 0; i--)
        {
            var pt = w.Parties[i];
            if (pt.Permanent) continue;
            if (AnyQualifying(w, c, pt.Ideo)) pt.AbsentQuarters = 0;
            else if (++pt.AbsentQuarters >= PartyAbsenceDissolveQuarters) w.Parties.RemoveAt(i);
        }
        if (w.ParliamentControl <= 0) return;   // 議会に力がない＝新規の政党化は起きない
        foreach (var f in w.Factions)
        {
            if (!Qualifies(w, c, f)) continue;
            bool exists = false;
            foreach (var pt in w.Parties)
                if (!pt.Permanent && pt.Ideo == f.Ideo) { exists = true; break; }
            if (exists) continue;
            var np = new PoliticalParty
            {
                Name = $"{FactionSnapshot.IdeologyJa((Ideology)f.Ideo)}党",
                Family = PoliticalQuantities.FamilyOf(f.Ideo), Ideo = f.Ideo,
            };
            w.Parties.Add(np);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{np.Name}が結党した");
        }
    }

    // 政党化の適格＝結束した運動体×活きた本拠州×そのブロックに票がある（F3b発言権と同じ法の階級ゲート）。
    private static bool Qualifies(World w, Coefficients c, FactionState f)
    {
        if (f.Cohesion < c.PartyFormCohesion || f.Region.State != RegionState.Active) return false;
        double voice = f.Bloc == 0 ? 1.0
                     : f.Bloc == 1 ? (w.Suffrage >= Suffrage.Census ? 1.0 : c.ModernEliteSubCensusVoice)
                     : (w.Suffrage >= Suffrage.AdultMale ? 1.0 : 0.0);   // 大衆と第4束「小営業」(bloc3)＝男子普選から（Politics.FactionStrength と同じ物差し。プチブル利益束2026-08-15）
        return voice > 0;
    }

    private static bool AnyQualifying(World w, Coefficients c, int ideo)
    {
        foreach (var f in w.Factions) if (f.Ideo == ideo && Qualifies(w, c, f)) return true;
        return false;
    }

    /// <summary>与党の実体＝与党「族」の中で最大得票の政党（常設含む・同点は先着＝常設優先＝決定論）。政党リスト未初期化ならnull。</summary>
    public static PoliticalParty? GoverningPartyOf(World w)
    {
        PoliticalParty? best = null;
        foreach (var pt in w.Parties)
            if (pt.Family == w.GoverningParty && (best is null || pt.VoteShare > best.VoteShare)) best = pt;
        return best;
    }

    // 要求を**政策目標**へ1段置く（現行法から要求方向へ1段＝Governmentの利益集団ゲートが可決を律速＝強制施行のApplyDemandとは別物）。
    // 呼び手＝危機の要求（TargetCrisisDemands）と州一揆への譲歩（Politics.ConcedeToRevolt）。
    // internal（2026-08-10）：番人テストが軸ごとの「目標を置く経路」を直接叩けるように（理由は Politics.ApplyDemand と同じ）。
    // ★要求は法の経路（2026-09-11）：置く前に**法律タブと同じ** Government.GateInfo を見て、通らない段は置かない（＝拒否）。
    //   LawRowVM.CanReachAndAfford（前提DAG＋純支持ゲート）と同じ関数＝AIとプレイヤーの「置ける段」が1本の物差しで決まる（幽霊戦線も作らない）。
    //   ★2026-09-26（回5-1 項目17）：門を見ない呼び手（議会の動的与党の要求）の撤去で引数 gated を削除＝常に門を見る。
    internal static void TargetDemand(World w, PolicyAxis a, bool inc)
    {
        var p = w.Policy;
        int n = inc ? 1 : -1;
        int StepTo(int cur, int max)
        {
            int next = System.Math.Clamp(cur + n, 0, max);
            return next != cur && !Government.GateInfo(w, a, inc, next).Ok ? cur : next;
        }
        bool FlipTo(bool cur) => cur != inc && !Government.GateInfo(w, a, inc, inc ? 1 : 0).Ok ? cur : inc;   // bool法＝同じ門
        // ★税・支出の段（作者裁定2026-09-12 R1）：段になった軸は**段で置く**（staged:true）。
        //   ⚠0.15刻み（FactionConcessionSlider）のままだと段の第1境界0.175を越えられず「要求は成立するが施行されない no-op 要求」が生える
        //     （社会保障が 2026-09-11 に同じ罠で Slide→段へ直された前例）。StepTo が法律タブと同じ門（GateInfo）を見る。
        //   段を持たない連続の軸（社会保障の水準・関税・海軍維持費・専売）は旧式（0.15刻み）。
        double Slide(double cur, bool staged = false)
            => staged
                ? Government.TaxValue(a, StepTo(Government.TaxStage(a, cur), Government.TaxLawStages))   // ★錨2026-09-18＝段↔率の写像は Government の2本だけ（軸ごとの錨）
                : PoliticalQuantities.Clamp01(cur + n * w.Coef.FactionConcessionSlider);
        switch (a)
        {
            case PolicyAxis.Suffrage: p.Suffrage = (Suffrage)StepTo((int)w.Suffrage, 3); break;
            case PolicyAxis.EducationLaw: p.EducationLaw = (EducationLaw)StepTo((int)w.EducationLaw, 3); break;
            case PolicyAxis.Censorship: p.Censorship = (CensorshipLaw)StepTo((int)w.Censorship, 2); break;
            case PolicyAxis.LaborUnion: p.LaborUnion = (LaborUnionLaw)StepTo((int)w.LaborUnion, 2); break;
            case PolicyAxis.ShorterHours: p.WorkHours = (WorkHours)StepTo((int)w.WorkHours, 3); break;
            case PolicyAxis.CornLaws: p.CornLaws = (ReformLevel)StepTo((int)w.CornLaws, 2); break;
            case PolicyAxis.LandReform: p.LandReform = (ReformLevel)StepTo((int)w.LandReform, 2); break;
            case PolicyAxis.SerfEmancipation: p.SerfEmancipation = (ReformLevel)StepTo((int)w.SerfEmancipation, 2); break;
            case PolicyAxis.WomenRights: p.WomenRights = (WomenRightsLaw)StepTo((int)w.WomenRights, 3); break;   // ★統合法（2026-08-10）：現行段から1段ずつ（旧・bool2本の直接反転を置換）
            case PolicyAxis.IncomeTax: p.IncomeTaxTarget = Slide(w.TaxRate, staged: true); break;   // ★R1：段
            // ★要求は法の経路：社会保障は法（Government.Step が SocialSecurityLaw を StepEnum）＝目標は法の段（v/3＝法律タブ GameViewModel.Law.cs と同じ単位）で置く。
            //   旧 Slide(+0.15) は Government の Round(Q5(target)×3) で段に届かない（0.15→0.45→0段）no-op 要求になりうる（ApplyDemand の直書きが隠していた）。
            case PolicyAxis.SocialSecurity: p.SocialSecurityTarget = StepTo((int)w.SocialSecurityLaw, Government.SocialSecurityMaxLevel) / (double)Government.SocialSecurityMaxLevel; break;
            case PolicyAxis.Tariff: p.TariffTarget = Slide(w.Tariff); break;
            case PolicyAxis.PollTax: p.PollTaxTarget = Slide(w.PollTax, staged: true); break;                        // ★R1：段
            case PolicyAxis.ProgressiveTax: p.ProgressiveTaxTarget = Slide(w.ProgressiveTax, staged: true); break;   // ★R1：段
            // ★段になった3軸（R1/R2・2026-09-12）＝下の「doctrine の領域だから党綱領は触れない」の線引きは**法になった軸について解除される**
            //   （法である以上、与党の綱領が目標を置ける＝法律タブと同じ p.X・可否は同じ GateInfo）。行政費は段にしないので従来どおり触れない。
            case PolicyAxis.EducationSpending: p.EducationSpendingTarget = Slide(w.EducationSpending, staged: true); break;
            case PolicyAxis.LandTax: p.LandTaxTarget = Slide(w.LandTax, staged: true); break;
            case PolicyAxis.Coercion:   // 武力スライダー＝軍事費の合計（1..4段）。書き込み口は陸海比を保つ既存の1本＝Policy.MilitarySpendingTarget
                p.MilitarySpendingTarget = System.Math.Max(1, StepTo(Government.MilitaryStage(w.MilitarySpending), Government.MilitaryLawStages))
                                         / (double)Government.MilitaryLawStages;   // Max(1,…)＝「なし」を作らない（StepTo のクランプ下限0の受け）
                break;
            // 02断線走査（2026-07-28）：E5/E6の8軸＝動的与党の要求綱領が目標を置けるように（施行はGovernmentの政治ゲート越し＝従来どおり）。
            case PolicyAxis.ConscriptionLaw: p.ConscriptionLaw = (ConscriptionLaw)StepTo((int)w.ConscriptionLaw, 2); break;   // ★3値化2026-08-01：上限1では国民皆兵に届かない
            case PolicyAxis.ImmigrationRestriction: p.ImmigrationRestriction = (ImmigrationLaw)StepTo((int)w.ImmigrationRestriction, 2); break;   // ★是正2026-08-01：目標を置く経路が1つも無かった
            case PolicyAxis.SpecialistExitBan: p.SpecialistExitLaw = (SpecialistExitLaw)StepTo((int)w.SpecialistExitLaw, 2); break;   // ★段⑤（2026-08-17）
            // ★是正2026-08-01：同上（兌換の維持＝増加向き）／★3値化2026-08-13（点火1.0=2026-08-21）：銀を経由する梯子では1段ずつ（gate=0に戻せば旧2値と同じ両端）
            case PolicyAxis.MonetaryStandard: p.MonetaryTarget = w.Coef.SilverStandardGate > 0 ? StepTo((int)w.MonetaryLaw, 2) : (FlipTo(w.MonetaryLaw == MonetaryStandardLaw.Gold) ? 2 : 0); break;
            case PolicyAxis.CapitalMobility: p.CapitalControlTarget = FlipTo(w.CapitalControlLaw); break;   // ★資本移動の法（トリレンマdoc・2026-08-13）＝増加向き＝規制
            // 参政権の範囲2法（2026-08-16）：ここは要求top-k経由のみ
            // ＝休眠中は Pref=0 で要求に載らず不到達・点火時に目標を置く経路が無いと no-op 要求になるので先に配線（2026-08-01の是正と同型）。
            case PolicyAxis.ColonialSuffrage: p.ColonialSuffrage = (ColonialSuffrageLaw)StepTo((int)w.ColonialSuffrage, 2); break;
            case PolicyAxis.MigrantNativeSuffrage: p.MigrantNativeSuffrage = (MigrantNativeSuffrageLaw)StepTo((int)w.MigrantNativeSuffrage, 1); break;
            case PolicyAxis.NavyUpkeep: p.NavyUpkeepTarget = Slide(w.NavyUpkeep); break;   // ★海軍維持費（2026-08-10 作者裁定）＝綱領は目標だけを置く（施行は Government の政治ゲート越し）。旧・海軍偏重の振り替え先
            case PolicyAxis.SecretBallot: p.SecretBallot = (SecretBallotLaw)StepTo((int)w.SecretBallot, 1); break;
            case PolicyAxis.Bureaucracy: p.Bureaucracy = (BureaucracyLaw)StepTo((int)w.Bureaucracy, 2); break;
            case PolicyAxis.ChildLabor: p.ChildLaborAllowed = FlipTo(w.ChildLaborAllowed); break;   // 児童労働法（02断線走査2026-07-28）
            case PolicyAxis.StateReligion: p.StateReligion = (StateReligionLaw)System.Math.Min(StepTo((int)w.StateReligion, 2), 2); break;   // 上限=信仰の自由（国家無神論は政体の属性）
            case PolicyAxis.Antitrust: p.Antitrust = (AntitrustLaw)StepTo((int)w.Antitrust, 2); break;
            case PolicyAxis.Guild: if (w.Coef.GuildLawGate > 0) p.Guild = (GuildLaw)StepTo((int)w.Guild, 2); break;   // ★特権 段4（ゲート0では綱領に載せない）
            case PolicyAxis.WartimeRationing: p.WartimeRationing = FlipTo(w.WartimeRationing); break;
            case PolicyAxis.MilitaryExport: p.MilitaryExportBan = FlipTo(w.MilitaryExportBan); break;   // 軍需輸出の法（禁制品doc C5・2026-07-30）
            case PolicyAxis.CivilianArms: if (w.Coef.RebelArmsMarketEnabled > 0) p.CivilianArmsBan = FlipTo(w.CivilianArmsBan); break;   // ★民間の武器購入の法（2026-09-11・ゲート0では綱領に載せない）
            case PolicyAxis.OpiumPossession: p.OpiumPossession = (ReformLevel)StepTo((int)w.OpiumPossession, 2); break;   // 阿片の保有法（C3・2026-07-30）
            case PolicyAxis.OpiumExport: p.OpiumExportBan = FlipTo(w.OpiumExportBan); break;   // 阿片の輸出法（C3・2026-07-30）
            case PolicyAxis.Nationalization:   // 未点灯の最初の1部門を目標に（enum順＝決定論。減少向きは点灯中の最後を解除）
                // 最終段『全て』（§11-6d）は前提DAG（プロレタリア独裁のみ＋官僚制度）を綱領でも踏む＝通らない目標を置いて幽霊戦線を開かない。
                if (!Government.GateInfo(w, a, inc, inc ? 1 : 0).Ok) break;   // ★要求は法の経路：部門を灯す/消す目標も同じ門
                if (inc) { if (!p.NationalizeRailways) p.NationalizeRailways = true; else if (!p.NationalizeMines) p.NationalizeMines = true; else if (!p.NationalizeMilitary) p.NationalizeMilitary = true; else if (!p.NationalizeOil) p.NationalizeOil = true; else if (!p.NationalizeElectricity) p.NationalizeElectricity = true; else if (!p.NationalizeAll && Government.LawPrereq(w, PolicyAxis.NationalizationAll, 1, w.Coef).Ok) p.NationalizeAll = true; }
                else { if (p.NationalizeAll) p.NationalizeAll = false; else if (p.NationalizeElectricity) p.NationalizeElectricity = false; else if (p.NationalizeOil) p.NationalizeOil = false; else if (p.NationalizeMilitary) p.NationalizeMilitary = false; else if (p.NationalizeMines) p.NationalizeMines = false; else p.NationalizeRailways = false; }
                break;
            // 行政費＝doctrine（AIの経営）の領域＝要求は置かない。
            // ⚠治安維持力（軍事費）・教育費・土地税は 2026-09-12 の段（R1/R2）で**法になったので上の3 case が置く**。
        }
    }

    /// <summary>★要求は法の経路（作者裁定2026-09-11）：F1 州一揆に譲歩した段（World.PendingRevoltDemand＝Politics.ConcedeToRevolt が記録）と、
    /// 危機が立った運動（PendingFactionCrisis）の要求を、通る段だけ法の目標に置く。
    /// 呼び手＝AI（SelfPlay.Apply の末尾＝下からの改革圧力と同じ位置）／初心者経路（Runner）／番人テスト。プレイヤーは法律タブで同じ p.X を書く
    /// （LawRowVM.CanReachAndAfford＝同じ GateInfo で可否）。IsPlayer は読まない。置かない判断＝GateInfo が否（政治資本が届かない・前提・不可逆・憲法級）
    /// ＝法律タブでその段が押せないのと同じ物差し。要求は ComputeDemands（lawsOnly＝法だけ）＝スライダー軸はここに来ない。
    /// F1 の梯子の段は全て法（穀物法／労働時間／社会保障／農奴解放／参政権）＝同じ1本で置く（先に置く＝同じ軸で食い違えば危機の要求が最後の言葉）。</summary>
    internal static void TargetCrisisDemands(World w)
    {
        if (w.PendingRevoltDemand != 0)
            TargetDemand(w, (PolicyAxis)(System.Math.Abs(w.PendingRevoltDemand) - 1), w.PendingRevoltDemand > 0);
        if (w.PendingFactionCrisis is not { } f) return;
        foreach (var d in FactionSnapshot.ComputeDemands(w, f.Bloc, lawsOnly: true)) TargetDemand(w, d.Axis, d.Increase);
    }


    private static Party Winner(World w)
    {
        // 現職効果（elections-deterministic-plurality）：在任与党は利益誘導・知名度で僅かに上積み。IncumbencyBias=0 で純多数決＝恒等＝較正中立。
        // 得票は VoteShares（今期の世論）で判定＝凍結時は確定 PartySeats（前回スナップショット）でなく現在の民意で新与党を決める。非凍結（既定）は VoteShares==PartySeats で恒等。
        double bias = w.Coef.IncumbencyBias * System.Math.Min(1.0, w.QuartersSinceElection / (double)System.Math.Max(1, w.Coef.ElectionIntervalQuarters));
        double v0 = w.VoteShares[0] + (w.GoverningParty == 0 ? bias : 0);
        double v1 = w.VoteShares[1] + (w.GoverningParty == 1 ? bias : 0);
        double v2 = w.VoteShares[2] + (w.GoverningParty == 2 ? bias : 0);
        int best = 0; double bv = v0;
        if (v1 > bv) { best = 1; bv = v1; }
        if (v2 > bv) { best = 2; }
        return (Party)best;
    }

    // 選挙のない政体の与党＝統治ブロックの党。
    private static Party RulingParty(Polity p) => p switch
    {
        Polity.BourgeoisDictatorship => Party.Liberal,
        Polity.ProletarianDictatorship => Party.Socialist,
        _ => Party.Conservative,   // 絶対/専制/幕府/軍政/無政府＝旧支配層・軍＝保守
    };

    /// <summary>★マニフェスト（作者指示2026-08-24）＝総選挙で加担した議題の達成判定。
    /// 公約した軸が約束の向きへ <see cref="Coefficients.ManifestoFulfillMove"/> 以上動いたら達成＝加担した思想へ支持が乗る
    /// （Ideology.UpdateShares が読む減衰ストック）。軸の位置は要求の可動域と同じ FactionSnapshot.AxisPosition＝物差しは1本。
    /// プレイヤーもAIも World.ResolveElection で公約を持つので、この判定も両者同一。</summary>
    private static void StepManifesto(World w, Coefficients c)
    {
        if (c.ManifestoCreditDecay > 0 && w.ManifestoCredit > 0) w.ManifestoCredit *= 1.0 - c.ManifestoCreditDecay;
        if (w.ManifestoIdeology < 0 || w.ManifestoDone) return;
        double moved = FactionSnapshot.AxisPosition(w, w.ManifestoAxis) - w.ManifestoBaseline;
        if (!w.ManifestoIncrease) moved = -moved;
        if (!(moved >= c.ManifestoFulfillMove)) return;   // 否定形＝NaN も未達成側へ落とす
        w.ManifestoDone = true;
        w.ManifestoCredit += c.ManifestoFulfillCredit;
        w.LogAdd($"{w.Year} {w.SelfPower?.Name}の与党が公約を果たした（{FactionSnapshot.DemandJa(w.ManifestoAxis, w.ManifestoIncrease)}）");
    }

    /// <summary>★E3（選挙doc §3-0-0）：公示イベントの生成＝**その時点で支持上位3つの思想**が議題を掲げる。
    /// 議題＝その思想の族の要求top1（FactionSnapshot.ComputeDemands＝strength×Headroom の既存順位づけ・手書きの思想→議題表なし）。
    /// 族単位の看板は思想単位より粗いが台本ゼロ（docの例＝伝統→穀物法維持も保守族の要求として出る）。精緻化はE4点火後の実測を見て。
    /// 時代ゲートは自動（§3-0-0b）＝ファシズム(9)はその国がファシズムを見つける（発見の表の思想の行・Ideologies.EraGates）前はシェア0＝上位3に入らない。</summary>
    internal static ElectionEvent BuildElectionEvent(World w, Coefficients c)
    {
        var eff = w.Suffrage;
        var ideoRaw = new double[Ideologies.Count];
        foreach (var p in w.AllPops())
        {
            double voters = p.Size * InterestGroups.FranchiseVoice(p, eff, w.Coef);
            if (voters <= 1e-9) continue;
            var s = p.IdeologyShares;
            double sum = 0; for (int i = 0; i < Ideologies.Count; i++) sum += s[i];
            if (sum < 1e-9) continue;
            for (int i = 0; i < Ideologies.Count; i++) ideoRaw[i] += voters * s[i] / sum;
        }
        // 支持上位3（LINQ不使用＝SimCoreの作法）
        Span<int> top = [-1, -1, -1];
        for (int i = 0; i < Ideologies.Count; i++)
        {
            if (ideoRaw[i] <= 1e-9) continue;
            for (int k = 0; k < 3; k++)
                if (top[k] < 0 || ideoRaw[i] > ideoRaw[top[k]])
                { for (int m = 2; m > k; m--) top[m] = top[m - 1]; top[k] = i; break; }
        }
        int nTop = top[2] >= 0 ? 3 : top[1] >= 0 ? 2 : top[0] >= 0 ? 1 : 0;
        var ideos = new int[nTop]; var axes = new PolicyAxis[nTop]; var inc = new bool[nTop]; var labels = new string[nTop];
        List<FactionSnapshot.Demand>?[] demandByFam = new List<FactionSnapshot.Demand>?[3];
        for (int k = 0; k < nTop; k++)
        {
            ideos[k] = top[k];
            int fam = PoliticalQuantities.FamilyOf(top[k]);
            var ds = demandByFam[fam] ??= FactionSnapshot.ComputeDemands(w, fam);
            if (ds.Count > 0)
            {
                axes[k] = ds[0].Axis; inc[k] = ds[0].Increase;
                labels[k] = $"{FactionSnapshot.IdeologyJa((Ideology)top[k])}：{FactionSnapshot.DemandJa(ds[0].Axis, ds[0].Increase)}";
            }
            else { axes[k] = default; inc[k] = true; labels[k] = FactionSnapshot.IdeologyJa((Ideology)top[k]); }
        }
        return new ElectionEvent { AgendaIdeologies = ideos, AgendaAxes = axes, AgendaIncrease = inc, AgendaLabels = labels };
    }
}

/// <summary>★E3（選挙doc §3-0〜3-2・ElectionEventEnabled=1.0 で全国に公示・0で生成されない）：総選挙の公示＝プレイヤーの択待ち（AI国は同じ四半期に UtilityAgent.DecideElection が答える）。
/// 択は「どの思想に加担するか」1つだけ（§3-2a＝議題を個別に倒す択は置かない＝法の束の原則）。
/// 議題は「今回は何が焦点か」を知るための情報であって、個別に操作する対象ではない。</summary>
public sealed class ElectionEvent
{
    public int[] AgendaIdeologies = [];    // 支持上位3思想（Ideologies 添字・支持降順）
    public PolicyAxis[] AgendaAxes = [];   // 各思想の看板議題の法軸（代価＝この軸への世論の純支持で決まる・World.ResolveElection）
    public bool[] AgendaIncrease = [];     // 看板の向き（強化か緩和か）
    public string[] AgendaLabels = [];     // 表示用「思想：議題」（UIのポップアップが読む）
}
