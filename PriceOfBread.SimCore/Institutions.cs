namespace PriceOfBread.SimCore;

/// <summary>
/// 制度（研究C＝カードの代替・§2.8）。技術そのものは社会から連続創発する（<see cref="TechEducation.GrowTech"/>）。
/// 社会が閾値（自由・エリート・工業化）を超えると制度が解禁され、対応分野を底上げする。
/// 施行は全国自動（AI/プレイヤー対称・選択UIは無い）。DiscoveryInstitutions>0（既定1.0）では特許/株式会社の解禁度は
/// Discovery.Unlock の傾斜で毎四半期再計算（bool は表示用 ramp≥0.5・可逆）。鉄道と旧経路（ゲート0）は一度きり・不可逆。
/// </summary>
public static class Institutions
{
    /// <summary>四半期：解禁条件を満たした制度を施行（全国自動）。効果＝フラグ＋州ブースト（所有国のフラグから毎期整合）。</summary>
    public static void Step(World w, Coefficients c)
    {
        // 解禁判定は全活性州の最大工業化度で（地方に工業州を持つ国＝英ランカシャー/独ルールも解禁される。旧：首都州 w.Region のみで永久に解禁されなかった）。
        // 効果（投資加速・生産性底上げ）は全活性州へ適用＝多州国でも工業州が恩恵を受ける（単一州国では1州のみ＝旧挙動と一致＝較正中立）。
        double maxInd = 0;
        foreach (var rr in w.Regions) if (rr.State == RegionState.Active && rr.TotalIndustrialization > maxInd) maxInd = rr.TotalIndustrialization;   // 解禁閾値は立っている資本の総量＝新旧合計（ヴィンテージ2026-07-10）

        // ★研究・段2（DiscoveryInstitutions・2026-08-31）：bool 閾値（研究C＝離散選択の名残）を Discovery.Unlock の傾斜へ。
        //   特許＝時計TechLevel×適性EliteCore（効果先＝エリートの革新）／株式会社＝時計TechLevel×適性maxInd（効果先＝工業投資）。
        //   bool は**表示のみ**（ramp≥0.5）＝読者（GrowTechの特許倍率・下の州ブースト）は傾斜を読む。0＝旧bool閾値＝厳密恒等。
        if (c.DiscoveryInstitutions > 0)
        {
            w.PatentLaw = Discovery.Unlock(w.EliteCore, 1.0, c.PatentDiscoveryOnset, c.PatentDiscoverySpan) >= 0.5;
            w.JointStock = Discovery.Unlock(maxInd, 1.0, c.JointStockDiscoveryOnset, c.JointStockDiscoverySpan) >= 0.5;
        }
        else
        {
        // 特許制度：自由＋公教育でエリートが育った社会＝革新の制度（国レベルの条件＝そのまま。宗教学校の社会は解禁されない＝採用止まり）
        if (!w.PatentLaw
            && w.Freedom >= c.FrontierFreedomThreshold
            && w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory
            && w.EliteCore >= c.PatentEliteThreshold)   // ★2026-08-26：閾値は**率**なので本国で測る（植民地の非識字人口で本国の制度を止めない）
        {
            w.PatentLaw = true;   // 効果＝GrowTech が革新を加速（World.PatentLaw を読む）
        }

        // 株式会社：工業化が一定進み資本市場が立つ＝投資の制度
        if (!w.JointStock && maxInd >= c.JointStockIndThreshold) w.JointStock = true;
        }

        // 鉄道：成熟した工業＝インフラの制度
        if (!w.Railways && maxInd >= c.RailwayIndThreshold) w.Railways = true;

        // 州ブーストの適用＝**所有国のフラグから毎期整合**（監査2026-07-11 二2）。
        // 旧＝解禁時に一回だけ州変数へ焼き込み：州割譲がブースト値ごとRegionを物理移動するため、未解禁国へ渡った州が
        // 後の解禁で再乗算（×1.6²）・解禁済み国へ来た州や解禁後にActive化した州は恒久未適用、という二重適用/取りこぼしが起きていた
        //（実走でマンチェスターが英→露→オスマンと移転した記録あり）。適用済みフラグを州に持ち、所有国の制度と常に一致させる＝
        // 割譲された州は新所有国の制度に自動で従う。非割譲の通常経路では解禁と同じ四半期に一度だけ適用＝旧挙動と厳密一致（較正中立）。
        // ★研究・段2：株式会社の投資ブーストを連続化（適用済み倍率を州に持ち、望む倍率との比で毎期整合＝
        //   割譲州も新所有国の傾斜へ自動追随＝旧boolのApplied機構と同じ整合思想の連続版）。ゲート0では旧bool経路のみ＝恒等。
        double jsWant = c.DiscoveryInstitutions > 0
            ? 1.0 + (c.JointStockInvestBonus - 1.0) * Discovery.Unlock(maxInd, 1.0, c.JointStockDiscoveryOnset, c.JointStockDiscoverySpan)
            : 0;
        foreach (var rr in w.Regions)
        {
            if (rr.State != RegionState.Active) continue;
            if (c.DiscoveryInstitutions > 0)
            {
                if (rr.JointStockApplied) { rr.IndustrializationBoost /= c.JointStockInvestBonus; rr.JointStockApplied = false; }   // 旧bool適用からの移行（ゲート点火をまたぐセーブ）
                if (System.Math.Abs(jsWant - rr.JointStockMulApplied) > 1e-12)
                { rr.IndustrializationBoost *= jsWant / rr.JointStockMulApplied; rr.JointStockMulApplied = jsWant; }
            }
            else if (w.JointStock && !rr.JointStockApplied) { rr.IndustrializationBoost *= c.JointStockInvestBonus; rr.JointStockApplied = true; }    // 工業化投資が加速（EconomyStep が読む）
            else if (!w.JointStock && rr.JointStockApplied) { rr.IndustrializationBoost /= c.JointStockInvestBonus; rr.JointStockApplied = false; }
            // ⚠撤去2026-08-28（作者裁定④）：鉄道の州生産性ブースト（ProductivityBonus+0.15）＝「輸送が増える」の代理だった
            //   （巨大建造物53件と同じ構図＝docs/202608/20260826_輸送インフラの巨大建造物を廃止する_完了.md §7-1）。
            //   効果の本体＝輸送容量（Transport.cs の鉄道購入・地形係数の railGate ブレンド）が既に担う＝因果の一段飛ばしを閉じる。
            //   Railways フラグ自体は残す（解禁の事実・UI表示・連続化ラウンドの対象）。
        }
    }
}
