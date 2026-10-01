namespace PriceOfBread.SimCore;

/// <summary>
/// 理想の軍隊の形（2026-08-01・作者指示「根本問題は**需要と供給の関係が存在しない**こと。理想の軍隊の形を定義する関数を用意し、
/// なるべく理想を目指す構造がモノ・ヒト・カネの需要の過不足を生むようにする」）。設計書＝docs/202607/20260801_理想の軍隊の形_部分完了.md。
///
/// ★理想の軍隊 ＝「**自国に向けられている他国の武力**」＋「**自力で守らねばならない資産**（国内の秩序・海上の交易と植民地）」。
///
/// ★★★中核の装置＝**注意配分**。過去5回の試みが壊れたのは「自分に届く他国軍の**総和**」を必要量にしたためで、
/// 現物の距離減衰（1万kmで0.5）と首都53件中46件が同一陸塊という実データでは行和が20を超え、**内点の均衡が存在しない**
/// （＝均衡が徴募プールのクランプになる＝「リミットが要る時点で設計が誤り」の再演）。
/// 本実装は和を捨てず、**相手の軍は同時に全方位へは向けられない**という物理で列方向に正規化する：
///     a(j→i) = h(i,j)·r(j→i) / ( u(j) + Σ_k h(k,j)·r(j→k) )
/// 列和が真に1未満（u(j)＝内向きに使われる軍）なので、**世界の必要量の総和は世界の戦力の総和より必ず小さい**＝
/// 軍拡は構造的に縮小写像になる。⚠**減衰係数を1本も置かない。** 建艦競争（仮想敵国の艦隊が海軍の必要を押し上げる）を
/// 入れても発散しないのは、この正規化があるからである。⚠**注意配分より先に対抗項を入れてはならない。**
///
/// ★live（ArmyShapeEnabled=1.0）。読み手＝Simulation が Need* の陸海比を Region.ShapeNavalShare へ写し、EconomyStep が
/// NavalProcurementFromShape で軍需の陸海配分に使う（0へ戻すと非恒等）。分布が外れたら係数を足さず**式を疑う**（armyshape プローブ）。
/// </summary>
public static class ArmyShape
{
    /// <summary>理想の形を全国ぶん計算して Power の Need* へ書く。四半期に1回で十分（形は年単位でしか動かない）。</summary>
    public static void Compute(Diplomacy dip, Coefficients c)
    {
        if (!(c.ArmyShapeEnabled > 0)) return;   // 0（対照用）で1命令も実行しない。否定形＝NaN係数も不走行側へ
        var ps = new List<Power>();
        foreach (var p in dip.Powers) if (p.Nation is not null) ps.Add(p);   // 抽象標的は脅威側からも注意配分の宛先からも外す（Projection の HomeDefenseBonus 分岐の誤用を避ける）
        int n = ps.Count;
        if (n == 0) return;

        // 1. 敵意×到達（j→i＝j の武力が i に届く強さ）。陸は国境の長さ＋陸続きの投射、海を越える陸軍は投射（距離減衰×制海権）、艦隊は海盆プレゼンスの重なり。
        var hrLand = new double[n * n];
        var hrCross = new double[n * n];   // ★(151) 海を越えて届く陸軍（j の首都から i の首都が別の陸塊）＝i は海で止める＝海の必要量へ
        var hrSea = new double[n * n];
        var border = Borders(ps, c);
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                if (i == j) continue;
                double h = Hostility(dip, ps[i], ps[j], c);
                // ★陸の到達＝届く強さ（投射＝距離減衰×制海権）＋国境の長さ（2026-10-01 (151)・作者裁定 2026-09-29「国境を接している国は脅威だが、そうでない国はあまり脅威ではない
                //   （国境線が長いほど軍事費が必要になる）」・AI の提案〈陸軍は安全保障＝国境の長さ×地形の通りにくさ・植民地の国境も数える〉に作者「その通り、推奨でよい」）。
                //   国境＝陸路グラフで隣り合う州の組の数×地形の通りにくさ（<see cref="Borders"/>・植民地格の州も州として数える）。接しない国は投射だけ（国境の辺1本ぶんが上限）＝あまり脅威でない。
                // ★海を越えて来る陸軍は海で止める（(151)・AI の提案〈海軍は別のベース＝艦隊の比と海路の接触〉の AI の読み）＝首都どうしが別の陸塊（IsSeaCrossing）の投射は海の必要量へ
                //   （投射は海越えに制海権 SeaControl を掛ける＝i の艦隊が大きいほど届かない＝海の任務そのもの）。国境（植民地の陸の国境を含む）は陸のまま。
                double proj = dip.Projection(ps[j], ps[i], c);
                bool cross = dip.IsSeaCrossing(ps[j], ps[i], c);
                hrLand[j * n + i] = h * (border[j * n + i] + (cross ? 0 : proj));
                hrCross[j * n + i] = cross ? h * proj : 0;
                double sea = 0;
                for (int b = 0; b < Basins.Count; b++) sea += ps[i].BasinPresence[b] * ps[j].BasinPresence[b];
                // ⚠BasinPresence は L1 正規化済み＝この内積を「海軍需要の水準」の根拠にしてはならない（分散する国ほど内積が小さく符号が逆転する）。
                //   ここでは「j の艦隊のうち i に向く分」を決める**相対重み**としてのみ使い、水準は下の通商保護項（絶対量）が担う。
                hrSea[j * n + i] = h * sea;
            }

        // 2. 列方向の正規化＝注意配分。分母に u(j)＝内向きに使われる軍を足すので**列和は真に1未満**。
        for (int i = 0; i < n; i++) { ps[i].NeedLand = 0; ps[i].NeedSea = 0; }
        double maxColSum = 0;
        var unrest = new double[n];   // Unrest は全POP走査＝1国1回（下の外生アンカーでも読む）
        for (int j = 0; j < n; j++)
        {
            double u = unrest[j] = Unrest(ps[j]);
            double sumL = u, sumS = u;
            for (int k = 0; k < n; k++) { sumL += hrLand[j * n + k] + hrCross[j * n + k]; sumS += hrSea[j * n + k]; }   // j の陸軍は陸の国境と海越えの両方へ配る（同じ1つの分母）
            double colL = 0;
            for (int i = 0; i < n; i++)
            {
                if (sumL > 1e-12)
                {
                    double a = hrLand[j * n + i] / sumL, ax = hrCross[j * n + i] / sumL;
                    ps[i].NeedLand += a * ps[j].LandPower;
                    ps[i].NeedSea += ax * ps[j].LandPower;   // 海を越えて来る陸軍＝海で止める
                    colL += a + ax;
                }
                if (sumS > 1e-12) ps[i].NeedSea += hrSea[j * n + i] / sumS * ps[j].NavalPower;
            }
            if (colL > maxColSum) maxColSum = colL;   // 診断：**列和が1未満であること**が縮小写像の実証（段1で必ず確認する）
        }

        // 3. 外生アンカー（他国の力を参照しない項）＝不動点が0にならないための土台。
        //    ⚠これが無いと「全員が軍を持たない」が吸収状態になる。下限定数を置かずに正の不動点を作る唯一の手。
        for (int i = 0; i < n; i++)
        {
            var p = ps[i]; var w = p.Nation!;   // ps は Nation 非null に絞ってある
            double garrisonMen = c.InternalGarrisonPerPop * w.TotalPopulation * unrest[i];   // 内的秩序＝八旗/緑営・露の内地守備・オスマンの駐屯
            // ★単位をそろえる（2026-10-01 (151)・分子と分母を同じ規則で）：陸と海の必要量は他国の戦力（即応度・質・熟練を掛けた LandPower・NavalPower）＝戦力のゲージ。
            //   外生の2つ（内の守り・通商保護）は「要る人の頭数」＝自国の兵1人あたりの戦力（eff＝Military÷兵の頭数のゲージ）で同じ単位へ直す。
            //   旧＝兵の頭数のゲージ（÷SoldiersPerMilitary）のまま＝即応度が 0.01〜0.2 の開幕は内の守りが他の2つの5〜100倍に見え、
            //   陸海の分け方（<see cref="NavalSpendShareOf"/>）を内の守りが決めていた（通商保護も同じ＝開幕だけ海が9割に見えた）。
            double men = w.SoldierSize();
            double eff = men > 1e-9 ? p.Military * c.SoldiersPerMilitary / men : 0;   // 自国の兵の頭数のゲージ1あたりの戦力
            p.NeedInternal = garrisonMen / System.Math.Max(1e-9, c.SoldiersPerMilitary) * eff;
            // ★通商保護（守る海の交易・2026-10-01 (151) で点火・AI の提案〈海軍は別のベース＝艦隊の比と海路の接触〉に作者「その通り、推奨でよい」）＝海に出ている経済の割合だけ、軍の同じ割合を海に置く
            //   （海の必要量の外生アンカー＝他国の艦隊を参照しない＝艦隊の比だけでは全ての海軍が0へ縮む吸収状態を防ぐ）。割合＝港の取扱高÷名目GDP（<see cref="SeaTradeShare"/>）＝新しい数は置かない。
            //   ⚠旧の設計（2026-08-01）は絶対量（係数 SeaEscortPerTradeValue×取扱高）＝係数は休眠のまま削除。絶対量は海の交易の大きい国の海軍を経済の規模で膨らませ
            //   （(151) の探索の腕＝清の取扱高は英の約1.5倍〔1914〕＝清の艦隊が英の約15倍に戻った）、軍の総合ゲージ（兵の頭数×即応度×質）と単位も合わない（開幕は即応度0.01〜0.2）。
            p.NeedSea += p.Military * SeaTradeShare(w);
            var (qTech, qCohesion, qMilitarism) = Quality(w, c);
            double q = qTech * qCohesion * qMilitarism;
            p.NeedMen = c.SoldiersPerMilitary * ((p.NeedLand + p.NeedInternal) + c.NavalCrewPerGauge * p.NeedSea) / System.Math.Max(0.05, q);
            p.AttentionColSumMax = maxColSum;
        }
    }

    /// <summary>★敵意 h(i,j)∈[0,1)（脅威の作り直し・2026-10-01 (151)）＝緊張の有理飽和（平時でも床 φ＝「無政府状態では武力は武力」）。
    /// ★同盟国は計上しない＝0（作者裁定 2026-09-29 の5「同盟国は計上しない」・旧＝3割）。★宗主と属国の間は仮想敵ほどではないが脅威＝×(1−AllyAttentionDiscount)
    /// （同「属国は仮想敵国ほどではないが、脅威を与える」・割引の係数は旧の同盟・従属の共用の1本）。理想の軍隊の形（陸と海の注意配分）と覇権の脅威の緊張（<see cref="Diplomacy"/> の ThreatTension）が同じ1本を読む。
    /// ⚠Suzerain は O(|Powers|) の線形走査＝n²ループ内で使わない（レビュー指摘）＝向きつきの IsDependentOf を2回引く。</summary>
    internal static double Hostility(Diplomacy dip, Power a, Power b, Coefficients c)
    {
        if (dip.HasAlliance(a, b)) return 0;
        double t = System.Math.Max(0, dip.Tension(a, b));
        // 有理飽和＝h ∈ [φ, 1) が構造的に有界。⚠上限下限のクランプを書かない（折れ点を作らない）。
        double h = c.AttentionHostilityFloor + (1 - c.AttentionHostilityFloor) * t / (t + c.AttentionTensionHalf);
        if (dip.IsDependentOf(b, a) || dip.IsDependentOf(a, b)) h *= 1.0 - c.AllyAttentionDiscount;
        return h;
    }

    /// <summary>陸路グラフの峠ノードの隣り（静的・起動時に1回）＝峠を挟んで隣り合う州の組を数えるため（峠は州でないので国境の両側は峠の隣り同士）。</summary>
    private static readonly int[][] PassNeighbors = BuildPassNeighbors();
    private static int[][] BuildPassNeighbors()
    {
        var nodes = PathGraph.Nodes;
        var lists = new List<int>?[nodes.Length];
        foreach (var e in PathGraph.Edges)
        {
            if (nodes[e.A].Kind == "pass") (lists[e.A] ??= []).Add(e.B);
            if (nodes[e.B].Kind == "pass") (lists[e.B] ??= []).Add(e.A);
        }
        var res = new int[nodes.Length][];
        for (int i = 0; i < nodes.Length; i++) res[i] = lists[i] is { } l ? [.. l] : [];
        return res;
    }

    /// <summary>★国境の長さ（2026-10-01 (151)・作者裁定 2026-09-29「国境を接している国は脅威だが、そうでない国はあまり脅威ではない（国境線が長いほど軍事費が必要になる）」・
    /// 測り方＝陸路グラフの隣接する州の組の数（AI の整理）×地形の通りにくさ・植民地の国境も数える（AI の提案に作者「その通り、推奨でよい」））。
    /// n×n の対称の表＝国 a の稼働中の州と国 b の稼働中の州が陸路グラフ（<see cref="PathGraph"/>）の辺で隣り合う組の数。峠を挟む組は峠の割増（PassCostMult）で割る。
    /// 1組の重み＝地形の通りにくさの逆数＝2÷（両端の州の地形の陸運の費用の和）＝交易の陸運の地形の表（<see cref="Coefficients.TerrainHorseCost"/>）を読む＝新しい数を置かない
    /// （平地1.0・丘1.5・森3.0・山2.5・荒地8.0＝山と砂漠の国境は守りやすい）。植民地格の州も州＝植民地の国境も数える。座標の無い州は陸路グラフに居ない＝数えない。
    /// 四半期に1回（<see cref="Compute"/>）＝州×1回の最寄りノード（座標の記憶つき）＋辺の走査（約840本）＋峠の隣りの組。</summary>
    internal static double[] Borders(List<Power> ps, Coefficients c)
    {
        int n = ps.Count, nn = PathGraph.Nodes.Length;
        var owner = new int[nn]; var terr = new double[nn];
        System.Array.Fill(owner, -1);
        for (int i = 0; i < n; i++)
        {
            if (ps[i].Nation is not { } w) continue;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;   // 座標の無い州＝陸路グラフに居ない
                int nd = PathGraph.NearestNode(cc.Lat, cc.Lon);
                if (nd < 0) continue;
                owner[nd] = i;
                int ti = (int)r.Terrain;
                terr[nd] = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            }
        }
        var b = new double[n * n];
        foreach (var e in PathGraph.Edges) Add(e.A, e.B, 1.0);
        for (int p = 0; p < nn; p++)
        {
            var nb = PassNeighbors[p];
            for (int x = 0; x < nb.Length; x++)
                for (int y = x + 1; y < nb.Length; y++) Add(nb[x], nb[y], 1.0 / PathGraph.PassCostMult);
        }
        return b;

        void Add(int u, int v, double mult)
        {
            int ou = owner[u], ov = owner[v];
            if (ou < 0 || ov < 0 || ou == ov) return;
            double w = mult * 2.0 / (terr[u] + terr[v]);
            b[ou * n + ov] += w; b[ov * n + ou] += w;
        }
    }

    /// <summary>★陸と海の軍事費の分割（2026-10-01 (151)・作者 2026-09-29「その通り、推奨でよい。陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」・
    /// AI の提案＝陸軍は安全保障〈国境の長さ×地形の通りにくさ・植民地の国境も数える〉・海軍は別のベース〈艦隊の比と海路の接触〉で軍事費を陸と海に分割）。
    /// 海の割合＝海の必要量 ÷ max(軍の総合ゲージ, 陸＋内の守り＋海の必要量)。
    /// ・海の必要量（<see cref="Power.NeedSea"/>）＝艦隊の比（海盆で接する仮想敵の艦隊の注意配分）＋海路の接触（海を越えて来る陸軍の注意配分・守る海の交易＝海に出ている経済の割合×軍の総合ゲージ）。
    /// ・陸と内の守り（<see cref="Power.NeedLand"/>・<see cref="Power.NeedInternal"/>）＝安全保障（国境の長さ×地形×隣国の陸軍・同盟は0・属国は弱く）＋国内の秩序。
    /// ・軍が必要量の和より大きい国＝海は任務の分だけ・残りの兵は陸に立つ（船は人で造れない＝海の大きさは海の任務が決める）＝**清は外洋艦隊を必要としない**が理由で出る
    ///   （旧＝地理の比＝外洋に開けば+0.10・植民地の質量×0.05＝陸続きの植民地格の州も数え、上限0.60に張り付いた清は兵の6割を艦の乗員に数えた）。
    ///   軍が足りない国＝必要量の比で分ける（陸も海も同じ割合で削る）。
    /// ・必要量が一度も算出されていない（全て0）＝−1（未定義＝呼び手は今の比のまま・軍需の陸海の再配分もしない）。⚠NaN も −1（否定形）。
    /// 読み手＝AI とプレイヤーの陸海の維持費の分け方（<see cref="UtilityAgent.SetNavalUpkeepShare"/>＝同じ関数）・軍需の陸海の再配分（Simulation の Region.ShapeNavalShare）・開幕の艦隊の播種（Diplomacy の初回の同期）。</summary>
    internal static double NavalSpendShareOf(Power p)
    {
        double need = p.NeedSea + p.NeedLand + p.NeedInternal;
        if (!(need > 0)) return -1.0;
        double s = p.NeedSea / System.Math.Max(p.Military, need);
        return s >= 0 ? s : -1.0;
    }

    /// <summary>★海に出ている経済の割合（2026-10-01 (151)）＝港の取扱高（海路の成約の額・四半期の累計・金建て＝Region.LastPortThroughput）÷四半期の名目GDP（自国の通貨を金建てへ＝為替 FxRate）。
    /// 通商保護（海の必要量の外生アンカー＝<see cref="Compute"/>）と診断の armyneed が読む同じ1本。
    /// ⚠**IsPort で絞る**（2026-08-01 の自己バグ修正）：LastPortThroughput は名前に反して**内陸州にも立つ**（最寄り港・借りた港まで運んだ海路の荷。2026-09-27 回4-1 から陸路の直行は積まない）。
    /// 絞らずに足すと、段1プローブで**スイス248・内モンゴル239**が「守るべき海上価値」の上位に来た（英は65）＝機構ごと誤り。既存の SyncBasinPresence が `r.State == Active && r.IsPort` で絞っているのが正しい作法。
    /// ⚠同じ国の州どうしの沿岸の海路は両端の港に積まれる（取扱高の定義）＝国内の沿岸の海運の厚い国は少し大きく出る（記録）。植民地は州＝植民地の港の取扱高として入る（旧の ColonyList の価値は単位が違い・開幕の州昇格の後は空＝外した）。
    /// 割合が定まらない（名目GDP が0・NaN）は0（否定形）。</summary>
    internal static double SeaTradeShare(World w)
    {
        double v = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.IsPort) v += r.LastPortThroughput;
        double s = v / (w.FxRate * Calendar.MonthsPerQuarter * w.NominalGdp);
        return s >= 0 && s < double.PositiveInfinity ? s : 0;
    }

    /// <summary>不穏度＝内向きに使われる軍の割合（大衆の闘争度×正統性の欠如）。★これが注意配分の分母に入り、列和を1未満にする。</summary>
    private static double Unrest(Power p)
        => p.Nation is { } w ? PoliticalQuantities.Clamp01(Politics.AvgMassMilitancy(w) * (1.0 - w.Legitimacy)) : 0;

    /// <summary>軍の質（技術×結束×軍国）。⚠**装備充足と即応度を入れない**——入れると
    /// 「装備が買えない→必要兵数が増える→予算が人へ→さらに買えない」の正帰還が閉じる（旧 SoldierSustainDriven 凍結と同型の罠）。
    /// 理想は**原因だけ**で作り、症状（充足率）は結果側にだけ出す。
    /// ★2026-09-26（棚卸し mil-13）：Diplomacy の戦力合成も**この1本**を読む（旧＝同じ式の写しが2か所・制度疲労の門が InstitutionalDecay と DecayForReaders で食い違っていた）。
    ///   3因子を別々に返す＝各呼び手の掛ける順（浮動小数の結合順）をそのまま保つ。</summary>
    internal static (double Tech, double Cohesion, double Militarism) Quality(World w, Coefficients c)
    {
        double techQuality = c.TechMilLowFloor + (1 - c.TechMilLowFloor) * PoliticalQuantities.Clamp01(w.TechLevel / c.TechMilFullLevel);
        if (c.DecayMilitaryWeight > 0 && w.DecayForReaders > 0)   // ★特権段6：読み手は DecayForReaders（ゲート0で従来値）
            techQuality *= 1.0 - System.Math.Clamp(c.DecayMilitaryWeight * w.DecayForReaders
                * PoliticalQuantities.Clamp01((w.WorldTechLevel - w.TechLevel) / System.Math.Max(1e-9, w.WorldTechLevel)), 0, 0.9);
        double cohesion = w.EthnicEnabled ? 1 + c.UnityMilitaryQuality * (w.EthnicUnity - 0.5) : 1.0;
        // ①軍の練度（2026-08-18・親軍法の撤去）＝識字×組織（専門官僚制・鉄道）×勤務年数。旧＝親軍法ONで一律1.3の段差。
        // 露は皆兵＋巨額でも低質（識字と下士官層の不在）・英は志願長期勤務で最高＝法でなく実物量が質を作る。
        double militarism = 1.0 + (c.MilitarismStrengthBonus - 1.0) * PoliticalQuantities.ArmyProfessionalism(w);
        return (techQuality, cohesion, militarism);
    }
}
