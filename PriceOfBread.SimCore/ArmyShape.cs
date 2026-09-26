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

        // 1. 敵意×到達（j→i＝j の武力が i に届く強さ）。陸は投射（距離減衰×制海権）、海は海盆プレゼンスの重なり。
        var hrLand = new double[n * n];
        var hrSea = new double[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                if (i == j) continue;
                double t = System.Math.Max(0, dip.Tension(ps[i], ps[j]));
                // 有理飽和＝h ∈ [φ, 1) が構造的に有界。⚠上限下限のクランプを書かない（折れ点を作らない）。
                double h = c.AttentionHostilityFloor + (1 - c.AttentionHostilityFloor) * t / (t + c.AttentionTensionHalf);
                if (dip.HasAlliance(ps[i], ps[j]) || dip.IsDependentOf(ps[j], ps[i]) || dip.IsDependentOf(ps[i], ps[j]))   // ⚠Suzerain は O(|Powers|) の線形走査＝n²ループ内で使わない（レビュー指摘）
                    h *= 1.0 - c.AllyAttentionDiscount;   // 同盟・従属は注意が割り引かれる＝日英同盟型の軍拡の緩和
                hrLand[j * n + i] = h * dip.Projection(ps[j], ps[i], c);
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
            for (int k = 0; k < n; k++) { sumL += hrLand[j * n + k]; sumS += hrSea[j * n + k]; }
            double colL = 0;
            for (int i = 0; i < n; i++)
            {
                if (sumL > 1e-12) { double a = hrLand[j * n + i] / sumL; ps[i].NeedLand += a * ps[j].LandPower; colL += a; }
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
            p.NeedInternal = garrisonMen / System.Math.Max(1e-9, c.SoldiersPerMilitary);
            double seaValue = 0;
            // ⚠**IsPort で絞る**（2026-08-01 の自己バグ修正）：LastPortThroughput は名前に反して**内陸州にも立つ**（世界市場との取引額そのもの）。
            // 絞らずに足すと、段1プローブで**スイス248・内モンゴル239**が「守るべき海上価値」の上位に来た（英は65）＝機構ごと誤り。
            // 既存の SyncBasinPresence が `r.State == Active && r.IsPort` で絞っているのが正しい作法。
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.IsPort) seaValue += r.LastPortThroughput;
            foreach (var col in p.ColonyList) seaValue += col.Value;
            p.NeedSea += c.SeaEscortPerTradeValue * seaValue;   // ★通商保護＝**絶対量**（正規化しない）。英の海軍が世界最大になるのは守るべき海上価値が世界最大だから
            var (qTech, qCohesion, qMilitarism) = Quality(w, c);
            double q = qTech * qCohesion * qMilitarism;
            p.NeedMen = c.SoldiersPerMilitary * ((p.NeedLand + p.NeedInternal) + c.NavalCrewPerGauge * p.NeedSea) / System.Math.Max(0.05, q);
            p.AttentionColSumMax = maxColSum;
        }
    }

    /// <summary>不穏度＝内向きに使われる軍の割合（大衆の闘争度×正統性の欠如）。★これが注意配分の分母に入り、列和を1未満にする。</summary>
    private static double Unrest(Power p)
        => p.Nation is { } w ? System.Math.Clamp(Politics.AvgMassMilitancy(w) * (1.0 - w.Legitimacy), 0, 1) : 0;

    /// <summary>軍の質（技術×結束×軍国）。⚠**装備充足と即応度を入れない**——入れると
    /// 「装備が買えない→必要兵数が増える→予算が人へ→さらに買えない」の正帰還が閉じる（旧 SoldierSustainDriven 凍結と同型の罠）。
    /// 理想は**原因だけ**で作り、症状（充足率）は結果側にだけ出す。
    /// ★2026-09-26（棚卸し mil-13）：Diplomacy の戦力合成も**この1本**を読む（旧＝同じ式の写しが2か所・制度疲労の門が InstitutionalDecay と DecayForReaders で食い違っていた）。
    ///   3因子を別々に返す＝各呼び手の掛ける順（浮動小数の結合順）をそのまま保つ。</summary>
    internal static (double Tech, double Cohesion, double Militarism) Quality(World w, Coefficients c)
    {
        double techQuality = c.TechMilLowFloor + (1 - c.TechMilLowFloor) * System.Math.Clamp(w.TechLevel / c.TechMilFullLevel, 0, 1);
        if (c.DecayMilitaryWeight > 0 && w.DecayForReaders > 0)   // ★特権段6：読み手は DecayForReaders（ゲート0で従来値）
            techQuality *= 1.0 - System.Math.Clamp(c.DecayMilitaryWeight * w.DecayForReaders
                * System.Math.Clamp((w.WorldTechLevel - w.TechLevel) / System.Math.Max(1e-9, w.WorldTechLevel), 0, 1), 0, 0.9);
        double cohesion = w.EthnicEnabled ? 1 + c.UnityMilitaryQuality * (w.EthnicUnity - 0.5) : 1.0;
        // ①軍の練度（2026-08-18・親軍法の撤去）＝識字×組織（専門官僚制・鉄道）×勤務年数。旧＝親軍法ONで一律1.3の段差。
        // 露は皆兵＋巨額でも低質（識字と下士官層の不在）・英は志願長期勤務で最高＝法でなく実物量が質を作る。
        double militarism = 1.0 + (c.MilitarismStrengthBonus - 1.0) * PoliticalQuantities.ArmyProfessionalism(w);
        return (techQuality, cohesion, militarism);
    }
}
