namespace PriceOfBread.SimCore;

/// <summary>
/// 創発的国内勢力 F2＝Faction クラスタの**読み取り専用スナップショット**（docs/2026-07-09_設計メモ.md §3 Phase2）＝クラスタリングと要求導出の共有核。
/// - World を一切変更しない純読み取り（pure static・決定論）。<see cref="ComputeClusters"/> は Factions.Step（F3a永続化）と UI「国内勢力」パネルが、
///   <see cref="ComputeDemands"/> は Parliament（公約）・Politics（F3b譲歩/補償）・UI が読む＝同じ束ね方・同じ要求導出を共有する。
/// - クラスタリングは決定論的で単純に：闘争度>閾値の pop を「イデオロギー主軸（argmax）×階級ブロック（InterestGroups の3区分）×地域」で束ね規模順。
/// - 要求ベクトル＝そのクラスタ（＝ブロック）の PolicyPreferences と現行法/政策の乖離 top-k 軸
///   （法をゲートする同じ Pref 表から要求が導出される＝要求のハードコード不要という対称性・設計メモ§3）。
/// </summary>
public static class FactionSnapshot
{
    /// <summary>クラスタに入る闘争度の閾値（設計メモ§3 Phase2 の例示値0.5）。</summary>
    public const double MilitancyThreshold = 0.5;
    /// <summary>表示する最小規模＝全国人口比（ノイズ級の微小クラスタを省く）。</summary>
    public const double MinSizeFraction = 0.01;
    /// <summary>要求軸の top-k。</summary>
    public const int TopK = 3;
    /// <summary>要求として拾う最小スコア（選好強度×残り可動域）。これ未満＝実質的な要求なし。</summary>
    public const double MinDemandScore = 0.05;

    /// <summary>要求1軸：どの政策軸を・どちらの向きへ・どれだけ強く（スコア＝ブロック選好の強さ×現行法からの残り可動域）。</summary>
    public readonly record struct Demand(PolicyAxis Axis, bool Increase, double Score);

    /// <summary>観測された国内勢力1件（読み取り専用の派生オブジェクト＝人を新たにシミュしない）。</summary>
    public sealed class Faction
    {
        public string Name = "";           // 機械生成名（地域×イデオロギー×階級）
        public double Size;                // 参加 pop の Size 合計
        public double AvgMilitancy;        // 人口加重平均闘争度
        public int Bloc;                   // 0=旧支配層 1=近代エリート 2=大衆 3=小営業（プチブル利益束2026-08-15・PettyInterestGate>0のみ）
        public List<Demand> Demands = []; // 要求 top-k（スコア降順）
    }

    /// <summary>クラスタリングの共有核（F2表示とF3永続化が同じ束ね方を使う＝決定論・World非変更）。
    /// 参加した pop を（州×ブロック×イデオロギー主軸）で束ね、規模降順（同点=挿入順）で返す。規模フィルタは呼び手が行う。
    /// ★段3（二量分離・2026-09-09）＝**参加の述語は不満度**（<see cref="Coefficients.ParticipationDiscontent"/>）。
    /// 既定0では旧＝「闘争度 &gt; <see cref="MilitancyThreshold"/>」の二値（ビット恒等）。</summary>
    internal static List<(Region Region, int Bloc, int Ideo, double Size, double MilSum)> ComputeClusters(World w)
    {
        var clusters = new Dictionary<(int Region, int Bloc, int Ideo), (Region Region, double Size, double MilSum)>();
        var order = new List<(int Region, int Bloc, int Ideo)>();   // 挿入順（規模同点の決定論的タイブレーク）
        var (petty, coord, lawGap) = MembershipContext(w);
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                double part = Participation(w, p, coord, lawGap);
                if (!(part > 0)) continue;   // 否定形＝NaN も「参加しない」側へ（旧の二値では NaN は 1.0 になるのでここには落ちない）
                var (bloc, ideo) = BlocIdeo(p, petty);
                var key = (r.Id, bloc, ideo);
                // ⚠規模も闘争度の総和も**参加で重みづける**（×1.0 は IEEE754 で厳密恒等＝旧経路はビット同一）。
                //   規模＝運動の頭数／MilSum÷規模＝参加加重の平均闘争度＝反乱の戦力が読む「戦う気」（戦力の判定は闘争度のまま＝§6-4-3）。
                if (clusters.TryGetValue(key, out var cur))
                    clusters[key] = (cur.Region, cur.Size + p.Size * part, cur.MilSum + p.Militancy * p.Size * part);
                else { clusters[key] = (r, p.Size * part, p.Militancy * p.Size * part); order.Add(key); }
            }
        }
        var result = new List<(Region, int, int, double, double)>(order.Count);
        foreach (var key in SortBySizeDesc(order, clusters))
        {
            var (reg, size, milSum) = clusters[key];
            result.Add((reg, key.Bloc, key.Ideo, size, milSum));
        }
        return result;
    }

    /// <summary>参加の述語の文脈＝**国の量**を pop ループの外で1回だけ取る（<see cref="Participation"/>／<see cref="BlocIdeo"/> と対）。
    /// 第4束「小営業」（プチブル利益束2026-08-15）＝gate=0でnull＝従来の3束分類とビット恒等。
    /// ★段3：不満度は pop の純関数だが、政体座標と法の隔たりは国の量（Politics.Discontent の注意書きと同じ作法・キャッシュは作らない＝World に置いた導出量は記憶になる）。
    /// ⚠既定（ParticipationDiscontent=0）では両方とも1度も計算しない＝旧経路のコストのまま。
    /// ⚠**同一ティック内の時点差は仕様**：闘争度ループは前期の参照点（LivingRef）で失望を測り、ここは更新後の参照点で測る
    ///   （Politics.Step の順＝闘争度→Factions.Step）。不満度は「記憶を持たず**今**の盤面を読む」量なので、これは分子と分母の非対称ではなく定義どおり
    ///   （差は LivingRefSpeed=0.03＝ギャップの3%）。
    /// 読み手＝<see cref="ComputeClusters"/> と武器の市場購入（EconomyStep・RebelArmsMarketEnabled&gt;0 のみ）＝運動の頭数と資金が**同じ述語**で数えられる。</summary>
    internal static (HashSet<Pop>? Petty, double[] Coord, double[] LawGap) MembershipContext(World w)
    {
        var c = w.Coef;
        var petty = c.PettyInterestGate > 0 ? InterestGroups.PettyPops(w) : null;
        bool byDiscontent = c.ParticipationDiscontent > 0;
        double[] coord = byDiscontent ? PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c) : [];
        double[] lawGap = byDiscontent && c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        return (petty, coord, lawGap);
    }

    /// <summary>pop 1つの参加∈[0,1]＝運動の頭数を数える**唯一の述語**。旧＝二値（NaN の闘争度は旧と同じく「参加」側へ倒す＝`p.Militancy &lt;= θ` が偽になるのと同値）。
    /// 新（ParticipationDiscontent&gt;0）＝max(不満度, 闘争度)＝不満度が動力・闘争度が上書き（作者裁定2026-09-09 その2①）。</summary>
    internal static double Participation(World w, Pop p, double[] coord, double[] lawGap)
    {
        var c = w.Coef;
        return c.ParticipationDiscontent > 0
            ? Math.Max(Politics.Discontent(w, c, p, coord, lawGap.Length > 0 ? lawGap[PolicyPreferences.BlocOf(p)] : 0), p.Militancy)
            : p.Militancy <= MilitancyThreshold ? 0.0 : 1.0;
    }

    /// <summary>pop 1つの（束・主軸）＝クラスタの鍵の**唯一の定義**（束＝InterestGroups の分類＋第4束・主軸＝イデオロギーの argmax）。</summary>
    internal static (int Bloc, int Ideo) BlocIdeo(Pop p, HashSet<Pop>? petty)
        => (p.Closed ? 0 : petty != null && petty.Contains(p) ? PolicyPreferences.PettyBloc : p.IsModernElite ? 1 : 2, ArgMax(p.IdeologyShares));

    /// <summary>スナップショットの計算（World 非変更・決定論）。クラスタなし＝空リスト（UI側は「目立った国内勢力なし」表示）。</summary>
    public static List<Faction> Compute(World w)
    {
        double totalPop = w.TotalPopulation;
        if (totalPop <= 1e-9) return [];
        var clusters = ComputeClusters(w);
        if (clusters.Count == 0) return [];

        // 要求ベクトル（ブロック単位＝クラスタの物質的基盤が要求を決める）：全軸のブロック選好×現行法からの残り可動域→top-k。
        // 束は最大4つ（第4束「小営業」＝PettyInterestGate>0のみ）＝束ごとに1回だけ計算して共有（Stance は毎回 AllPops を走るため）。
        var demandsByBloc = new List<Demand>?[PolicyPreferences.PettyBloc + 1];
        var result = new List<Faction>();
        foreach (var (reg, bloc, ideoIdx, size, milSum) in clusters)
        {
            if (size < MinSizeFraction * totalPop) continue;
            demandsByBloc[bloc] ??= ComputeDemands(w, bloc);
            var ideo = (Ideology)ideoIdx;
            result.Add(new Faction
            {
                Name = $"{reg.Name}の{IdeologyJa(ideo)}{BlocJa(bloc)}",
                Size = size,
                AvgMilitancy = milSum / size,
                Bloc = bloc,
                Demands = demandsByBloc[bloc]!,
            });
        }
        return result;
    }

    private static IEnumerable<(int Region, int Bloc, int Ideo)> SortBySizeDesc(
        List<(int Region, int Bloc, int Ideo)> order,
        Dictionary<(int Region, int Bloc, int Ideo), (Region Region, double Size, double MilSum)> clusters)
    {
        var arr = order.ToArray();
        // 安定ソート（挿入順を保つ）＝規模同点でも決定論。Array.Sort は不安定なので手動の安定キー（index）を併用。
        var idx = new int[arr.Length];
        for (int i = 0; i < idx.Length; i++) idx[i] = i;
        System.Array.Sort(idx, (a, b) =>
        {
            int cmp = clusters[arr[b]].Size.CompareTo(clusters[arr[a]].Size);   // 規模降順
            return cmp != 0 ? cmp : a.CompareTo(b);                              // 同点＝挿入順
        });
        foreach (int i in idx) yield return arr[i];
    }

    // 全 PolicyAxis の並び（議会・勢力経路から四半期に複数回呼ばれる ComputeDemands のため1回だけ確保・順序は Enum.GetValues と同一）。
    // ⚠<see cref="PolicyPreferences.LawGap"/>（不満度の法の隔たり・段1 2026-09-09）も**この1本**を走る＝軸の集合が二重にならない。
    internal static readonly PolicyAxis[] Axes = System.Enum.GetValues<PolicyAxis>();

    /// <summary>即時に上げ下げできる軸＝連続のままの Government.Move（1期1段の法の梯子ではない）＝**危機の要求**（ComputeDemands の lawsOnly＝危機側の読み手だけが渡す・
    /// 総選挙の議題と UI は全軸）から除く軸＝Coercion（武力＝軍事費と治安維持力・AxisPosition が別量 BaseCoercion を読み、CoercionFollowsForce 下では目標が軍規模で毎期上書きされる）／
    /// NavyUpkeep（海軍維持費＝Coercion 軸の合計ゲート越しの連続の比）／Tariff（R3＝スライダーのまま）。1期0.018の動きで「譲歩された」と読めてしまう（必ず受諾の壊れ方）。
    /// SocialSecurity は法（Government.Step が SocialSecurityLaw を StepEnum）＝残す。
    /// ★裁定C1（2026-09-12「税率は反乱の要求に乗せる」・2026-09-20 点火）：税5軸は段の法になった＝ここから外した（旧9軸→4軸）。
    /// ★2026-09-20：専売は軸ごと撤去（作者裁定「政治問題ではなく生産マネジメントの範囲」）＝この判定の対象でもなくなった。
    /// ⚠新軸を足したら Government.Move で書くならここにも載せる（要求に載って施行されない no-op 要求＝2026-08-01 の前例を作らない）。</summary>
    internal static bool IsInstantAxis(PolicyAxis a) => a is PolicyAxis.Coercion or PolicyAxis.NavyUpkeep or PolicyAxis.Tariff;

    // ブロックの要求 top-k：各政策軸の「進める向き」への選好（法をゲートする PolicyPreferences.Stance と同じ Pref 表＝対称性）を評価し、
    // 望む向きの残り可動域（現行法/スライダーが端に達していれば要求は消える）を掛けたスコアの上位を返す。
    // lawsOnly（範囲是正2026-09-11）＝即時値の軸を除く（危機側の読み手＝Politics.RecordCrisisDemands／Parliament.TargetCrisisDemands／factionprobe の行き先が true で渡す）。
    //   既定 false＝総選挙の議題（Parliament.BuildElectionEvent）と UI（Compute・App）は全軸＝減税を掲げる政党は残る（与党の要求押しは 2026-09-26 に撤去＝回5-1）（作者裁定(3)は「蜂起の要求」についての裁定）。
    public static List<Demand> ComputeDemands(World w, int bloc, bool lawsOnly = false)
    {
        var list = new List<Demand>();
        foreach (var axis in Axes)
        {
            // 『全て』専用軸（§11-6d）は要求軸にしない＝国有化要求は Nationalization 梯子（最終段＝『全て』・StepNationalization）が一元管理。
            // 選好は Nationalization と同一なので載せると**同文の二重要求**になる（ゲート緑≠機構が働いた・2026-08-01の施行なきno-op要求と同族の罠）。
            if (axis == PolicyAxis.NationalizationAll) continue;
            // ★要求は法の経路（作者裁定2026-09-11「蜂起の要求に、即座に上げ下げできるもの(減税など)は加えない」）：
            //   即時値の軸（IsInstantAxis）は**危機の要求**に載せない（lawsOnly＝危機側の読み手だけ）。総選挙の議題・UI（lawsOnly=false）は全軸のまま（範囲是正2026-09-11）。
            // ★裁定C1（2026-09-12「税率は反乱の要求に乗せる」）：段になった税5軸は**もう即座に上げ下げできない**（4段の法）ので除外しない
            //   （2026-09-20 の段の点火で条件式を IsInstantAxis の4軸＝治安維持力・海軍維持費・関税・専売へ畳んだ。専売は同日の裁定で段の法から外れて即時値の側に残った）。
            if (lawsOnly && IsInstantAxis(axis)) continue;
            var (trad, modern, mass, _) = PolicyPreferences.Stance(w, axis, increasing: true);
            double prefInc = bloc == 0 ? trad : bloc == 1 ? modern : bloc == 2 ? mass
                           : PolicyPreferences.PettyStance(w, axis, increasing: true);   // 第4束「小営業」（PettyInterestGate>0のクラスタのみ到達）。減少向きの選好＝−prefInc（Pref×sign の定義から厳密）
            bool increase = prefInc >= 0;
            double strength = System.Math.Abs(prefInc);
            double score = strength * Headroom(w, axis, increase);
            // 第6段6-5②（2026-07-28）：狙う軸を選べる＝体制の守り（その軸の反対勢力の力）が薄い軸を優先（中核ルールdoc §3-3②・
            // 新データ不要＝OpposingPowerの再利用）。運動は正面の壁を避けて弱い扉を押す。FactionTargetsWeakAxis=0で旧＝選好×可動域のみ。
            if (w.Coef.FactionTargetsWeakAxis > 0)
                score /= 1.0 + w.Coef.FactionTargetsWeakAxis * PolicyPreferences.OpposingPower(w, axis, increase);
            if (score > MinDemandScore) list.Add(new Demand(axis, increase, score));
        }
        list.Sort((a, b) => { int cmp = b.Score.CompareTo(a.Score); return cmp != 0 ? cmp : a.Axis.CompareTo(b.Axis); });   // スコア降順・同点＝軸順＝決定論
        if (list.Count > TopK) list.RemoveRange(TopK, list.Count - TopK);
        return list;
    }

    // その向きへの残り可動域∈[0,1]（現行値の正規化位置から）。端に達した軸は要求から消える＝「現行法との乖離」の実装。
    // ⚠読み手は2つ＝<see cref="ComputeDemands"/>（要求の順位）と <see cref="PolicyPreferences.LawGap"/>（不満度の法の隔たり）。
    //   「余地」の定義は**この1本**（AxisPosition と対）＝要求が消える点と不満が消える点が同じ座標で一致する。
    internal static double Headroom(World w, PolicyAxis a, bool increase)
    {
        double v = AxisPosition(w, a);
        return increase ? 1 - v : v;
    }

    /// <summary>その軸の現在位置∈[0,1]（可動域の端＝0/1に正規化）。<see cref="Headroom"/> と
    /// マニフェストの達成判定（Parliament）が読む**唯一の物差し**＝要求の可動域と公約の進捗が同じ座標で測られる。</summary>
    internal static double AxisPosition(World w, PolicyAxis a)
    {
        // ★税・支出の段（作者裁定2026-09-12 R1）：段になった軸は**段の序数/3**で測る（`EducationLaw => (int)w.EducationLaw / 3.0` と同形）。
        //   ⚠Government.Move は目標へ指数接近するだけで到達しないので、実効値のままだと最上段を与えきっても v<1 が残り
        //     「法を与えきっても要求が消えない／不満（LawGap）が満額のまま」になる＝2026-08-01 の no-op 要求と同型の力学バグ。
        // ★錨2026-09-18：**Q4（率）ではなく段序数/段数**を読む。錨が1.0だった間だけ両者は数値的に同一で、軸ごとの錨（所得税0.35等）を
        //   入れた瞬間に Q4 は率を返す＝最上段を与えきっても v=0.35 で Headroom が0.65 残り、要求も不満（LawGap）も永久に消えない
        //   ＝上のコメントが警告している 2026-08-01 の no-op 要求と同型の力学バグそのもの。ここは「正規化位置」を測る場所なので段で測る。
        double Stage(double v0) => Government.TaxStage(a, v0) / (double)Government.TaxLawStages;
        double v = a switch   // 現行値の正規化位置 [0,1]
        {
            PolicyAxis.IncomeTax => Stage(w.TaxRate),
            PolicyAxis.SocialSecurity => w.SocialSecurity,
            PolicyAxis.Coercion => System.Math.Clamp(w.BaseCoercion / System.Math.Max(1e-9, w.Coef.CoercionMax), 0, 1),
            PolicyAxis.Tariff => System.Math.Clamp(w.Tariff, 0, 1),
            PolicyAxis.EducationSpending => Stage(w.EducationSpending),
            PolicyAxis.PollTax => Stage(w.PollTax),
            PolicyAxis.LandTax => Stage(w.LandTax),
            PolicyAxis.ProgressiveTax => Stage(w.ProgressiveTax),
            PolicyAxis.EducationLaw => (int)w.EducationLaw / 3.0,
            PolicyAxis.Suffrage => (int)w.Suffrage / 3.0,
            PolicyAxis.Censorship => (int)w.Censorship / 2.0,
            PolicyAxis.LaborUnion => (int)w.LaborUnion / 2.0,
            PolicyAxis.ShorterHours => (int)w.WorkHours / 3.0,   // 軸の「進める向き」＝enum増＝長時間化（PolicyPreferences と同規約）
            PolicyAxis.ChildLabor => w.ChildLaborAllowed ? 1.0 : 0.0,   // 児童労働法（02断線走査2026-07-28）：軸の増＝合法化
            PolicyAxis.WomenRights => (int)w.WomenRights / 3.0,   // ★統合法（2026-08-10）：なし0／財産権1/3／教育権2/3／選挙権1.0＝段を与えきれば要求が消える
            PolicyAxis.CornLaws => (int)w.CornLaws / 2.0,
            PolicyAxis.LandReform => (int)w.LandReform / 2.0,
            PolicyAxis.SerfEmancipation => (int)w.SerfEmancipation / 2.0,
            // 02断線走査（2026-07-28）：E5/E6の8軸＝旧・既定0.5の固定headroomで「法を完全に与えても要求が消えない／
            // 動かせない要求がtop-3を占拠する」の両向きが壊れていた（繋ぐべき筆頭群）。
            PolicyAxis.ConscriptionLaw => (int)w.ConscriptionLaw / 2.0,   // ★3値化2026-08-01
            PolicyAxis.SecretBallot => (int)w.SecretBallot / 1.0,
            PolicyAxis.Bureaucracy => (int)w.Bureaucracy / 2.0,
            PolicyAxis.StateReligion => System.Math.Min((int)w.StateReligion, 2) / 2.0,   // 法の到達上限=信仰の自由（StateAtheismは政体の属性でクランプして1.0扱い）。/3.0だと施行上限2/3で要求が消えないno-op化バグ（2026-09 wave2b）
            PolicyAxis.Antitrust => (int)w.Antitrust / 2.0,
            PolicyAxis.Guild => (int)w.Guild / 2.0,   // ★特権 段4
            PolicyAxis.WartimeRationing => w.WartimeRationing ? 1.0 : 0.0,
            PolicyAxis.MilitaryExport => w.MilitaryExportBan ? 1.0 : 0.0,   // 軍需輸出の法（禁制品doc C5・2026-07-30）：軸の増＝禁止
            PolicyAxis.CivilianArms => w.CivilianArmsBan ? 1.0 : 0.0,       // ★民間の武器購入の法（2026-09-11）：軸の増＝禁止（休眠中は Pref=0＝要求に載らないが、器2026-08-01の「余地0.5固定」の罠を先に封じる）
            PolicyAxis.OpiumPossession => (int)w.OpiumPossession / 2.0,     // 阿片の保有法（C3・2026-07-30）：軸の増＝許可→輸入禁止→禁止
            PolicyAxis.OpiumExport => w.OpiumExportBan ? 1.0 : 0.0,         // 阿片の輸出法（同）
            PolicyAxis.NavyUpkeep => w.NavyUpkeep,   // ★海軍維持費（2026-08-10 作者裁定＝旧「海軍偏重」の振り替え先）：0..1 の維持費そのものが現行値＝軸の増＝艦隊に払う金を増やす。
                                                     // 満額（1.0）まで払えば「海軍の増強」の要求が消える。落ちていると _ => 0.5 に吸われ**余地が0.5固定＝与えても要求が消えない**
            // ★器2026-08-01：新軸は必ず Headroom にも入れる（落ちると _ => 0.5 に吸われ「法を完全に与えても要求が消えない」力学バグ＝旧 NavalEmphasis の前例）。
            // 器の段では対応する状態がまだ無いので 0.5（中庸）を明示的に返す。⚠選好が常に0なので要求リストには載らない＝厳密恒等。
            PolicyAxis.ImmigrationRestriction => (int)w.ImmigrationRestriction / 2.0,   // ★点火2026-08-01：開放0／選別0.5／閉鎖1.0＝法を与えきれば要求が消える
            PolicyAxis.SpecialistExitBan => (int)w.SpecialistExitLaw / 2.0,   // ★段⑤：自由0／制限0.5／禁止1.0
            PolicyAxis.MonetaryStandard => (int)w.MonetaryLaw / 2.0,   // ★点火2026-08-01／★3値化2026-08-13（銀の段live=2026-08-21）：不換0／銀0.5／金1.0（gate=0に戻せば両端しか取らず旧と一致）
            PolicyAxis.CapitalMobility => w.CapitalControlLaw ? 1.0 : 0.0,   // ★資本移動の法（2026-08-13）：自由0／規制1.0
            // 参政権の範囲2法（2026-08-16）：段を与えきれば要求が消える（落とすと _ => 0.5 に吸われる器2026-08-01の力学バグの罠）。休眠中は選好0＝要求に載らない＝厳密恒等。
            PolicyAxis.ColonialSuffrage => (int)w.ColonialSuffrage / 2.0,           // なし0／自国民のみ0.5／全面1.0
            PolicyAxis.MigrantNativeSuffrage => (int)w.MigrantNativeSuffrage / 1.0, // なし0／あり1.0
            PolicyAxis.Nationalization => NationalizationLitRate(w),
            _ => 0.5,
        };
        return System.Math.Clamp(v, 0, 1);   // ⚠端の外（税率>1等）を先に畳む＝Headroom 側の 1−v も [0,1] に収まる（旧＝反転後に Clamp・同値）
    }

    // 02断線走査（2026-07-28）：国有化6行（5部門＋『全て』＝§11-6d統合）の点灯率（Headroom用＝全点灯で要求が消える）。
    private static double NationalizationLitRate(World w)
    {
        int lit = (w.NationalizeRailways ? 1 : 0) + (w.NationalizeMines ? 1 : 0) + (w.NationalizeMilitary ? 1 : 0)
                + (w.NationalizeOil ? 1 : 0) + (w.NationalizeElectricity ? 1 : 0) + (w.NationalizeAll ? 1 : 0);
        return lit / 6.0;
    }

    private static int ArgMax(double[] shares)
    {
        int best = 0; double bv = shares[0];
        for (int i = 1; i < shares.Length; i++) if (shares[i] > bv) { bv = shares[i]; best = i; }   // 同点＝先頭優先＝決定論
        return best;
    }

    /// <summary>勢力名のイデオロギー形容（機械生成名の素）。</summary>
    public static string IdeologyJa(Ideology i) => i switch
    {
        Ideology.Traditionalism => "伝統派",
        Ideology.Reactionism => "反動派",
        Ideology.Liberalism => "自由主義",
        Ideology.Republicanism => "共和主義",
        Ideology.Nationalism => "国民主義",
        Ideology.Socialism => "社会主義",
        Ideology.Communism => "共産主義",
        Ideology.Anarchism => "無政府主義",
        Ideology.Imperialism => "帝国主義",
        Ideology.Fascism => "ファシズム",
        Ideology.Royalism => "王党派",
        _ => "",
    };

    /// <summary>勢力名の階級語（機械生成名の素）。</summary>
    public static string BlocJa(int bloc) => bloc switch
    {
        0 => "の名望家層",      // 旧支配層（貴族・聖職）＝郷紳・名望家の運動（フロンド/湘軍型）
        1 => "の市民派",        // 近代エリート（ブルジョワ・専門職）
        PolicyPreferences.PettyBloc => "の小営業層",   // 第4束（プチブル利益束2026-08-15・PettyInterestGate>0のみ）＝店主・親方の運動（1848急進派/反百貨店運動型）
        _ => "の大衆運動",      // 大衆（労働者・農民・兵士）。例＝「パリの共和主義の大衆運動」
    };

    /// <summary>要求1軸の日本語表示（「軸名の方向」）。UI「国内勢力」パネル用。</summary>
    public static string DemandJa(PolicyAxis a, bool increase) => a switch
    {
        PolicyAxis.IncomeTax => increase ? "所得税の増税" : "所得税の減税",
        PolicyAxis.SocialSecurity => increase ? "社会保障の拡充" : "社会保障の削減",
        PolicyAxis.Coercion => increase ? "治安維持力の強化" : "治安維持力の縮小",
        PolicyAxis.Tariff => increase ? "保護関税" : "自由貿易",
        PolicyAxis.EducationSpending => increase ? "教育費の拡充" : "教育費の削減",
        PolicyAxis.PollTax => increase ? "人頭税の増税" : "人頭税の廃止",
        PolicyAxis.LandTax => increase ? "土地税の増税" : "土地税の減税",
        PolicyAxis.ProgressiveTax => increase ? "累進課税の強化" : "累進課税の緩和",
        PolicyAxis.EducationLaw => increase ? "公教育の拡大" : "教会教育への回帰",
        PolicyAxis.Suffrage => increase ? "参政権の拡大" : "参政権の制限",
        PolicyAxis.Censorship => increase ? "報道の自由化" : "検閲の強化",
        PolicyAxis.LaborUnion => increase ? "労働組合の容認" : "組合の禁圧",
        PolicyAxis.ShorterHours => increase ? "労働時間規制の撤廃" : "労働時間の短縮",   // 軸の増＝長時間化（PolicyPreferences 規約）
        PolicyAxis.ChildLabor => increase ? "児童労働の解禁" : "児童労働の禁止",   // 軸の増＝合法化（02断線走査2026-07-28）
        PolicyAxis.WomenRights => increase ? "女性の権利の拡大" : "家父長制の維持",   // ★統合法（2026-08-10）：段は「財産権→教育権→選挙権」＝向きで言う
        PolicyAxis.CornLaws => increase ? "穀物法の強化" : "穀物法の撤廃",
        PolicyAxis.LandReform => increase ? "土地改革" : "土地改革の巻き戻し",
        PolicyAxis.SerfEmancipation => increase ? "農奴解放" : "農奴制の維持",
        // 02断線走査（2026-07-28）：E5/E6の8軸＝旧・英語enum名がそのままUIへ出ていた。
        PolicyAxis.ConscriptionLaw => increase ? "徴兵の拡大" : "徴兵の縮小",   // ★3値化2026-08-01：段が3つになったので向きで言う
        PolicyAxis.SecretBallot => increase ? "秘密投票" : "公開投票への回帰",
        PolicyAxis.Bureaucracy => increase ? "官僚制度の専門化" : "猟官制への回帰",
        PolicyAxis.StateReligion => increase ? "信教の寛容化" : "国教の強化",
        PolicyAxis.Antitrust => increase ? "独占の規制" : "独占の放任",
        PolicyAxis.Guild => increase ? "同業組合の廃止" : "同業組合の保護",   // ★特権 段4
        PolicyAxis.WartimeRationing => increase ? "戦時配給" : "配給の廃止",
        PolicyAxis.MilitaryExport => increase ? "軍需輸出の禁止" : "軍需輸出の解禁",   // 軸の増＝禁止（禁制品doc C5・2026-07-30）
        PolicyAxis.CivilianArms => increase ? "民間の武器購入の禁止" : "民間の武器購入の許可",   // ★軸の増＝禁止（2026-09-11）
        PolicyAxis.OpiumPossession => increase ? "阿片の禁令" : "阿片の解禁",           // 軸の増＝許可→輸入禁止→禁止（C3・2026-07-30）
        PolicyAxis.OpiumExport => increase ? "阿片輸出の禁止" : "阿片輸出の解禁",
        PolicyAxis.NavyUpkeep => increase ? "海軍の増強" : "海軍の縮小",   // ★海軍維持費（2026-08-10 作者裁定）＝軸の増＝艦隊に払う金を増やす（減＝軍縮。旧「陸軍の重視」は注力度の言い方＝廃止）
        PolicyAxis.ImmigrationRestriction => increase ? "移民の制限" : "移民の受け入れ",   // ★器2026-08-01（日本語UIに英字が出るのを防ぐ）
        PolicyAxis.SpecialistExitBan => increase ? "技術者の出国禁止" : "技術者の出国自由",   // ★段⑤（2026-08-17）
        PolicyAxis.MonetaryStandard => increase ? "兌換の維持" : "兌換の停止",             // 同上
        PolicyAxis.CapitalMobility => increase ? "資本移動の規制" : "資本移動の自由",      // ★資本移動の法（2026-08-13）
        PolicyAxis.Nationalization => increase ? "国有化" : "民営化",
        PolicyAxis.NationalizationAll => increase ? "全産業の国有化" : "民営化",   // 保険＝要求軸にはしない（ComputeDemands が除外）が、英字enum名がUIへ漏れる既知の罠を封じる
        PolicyAxis.ColonialSuffrage => increase ? "植民地参政権の拡大" : "植民地参政権の制限",             // 参政権の範囲2法（2026-08-16。英字enum名がUIへ漏れる罠を封じる）
        PolicyAxis.MigrantNativeSuffrage => increase ? "移民原住民参政権の付与" : "移民原住民参政権の剥奪",
        _ => a.ToString(),
    };
}
