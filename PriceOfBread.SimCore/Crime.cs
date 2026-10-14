namespace PriceOfBread.SimCore;

/// <summary>
/// 犯罪と抑止（治安）。秩序＝実効治安維持力（Coercion＝基礎×正統性×財政充足）の人口カバレッジ＝警察の手が回っているか。
/// 秩序が崩れると、貧しく不満な大衆が富を「闇で再分配」する＝窃盗（被害者の貯蓄→困窮大衆へ。純移転＝保存則安全）。
/// これにより治安維持力は革命鎮圧（Politics）だけでなく**日常の役割**を持つ：弱い国家・破産国家・恐慌下で犯罪が湧き、
/// 人口が育つほど治安には治安維持力の増強が要る（警察も人口比で要る）。1789の既定構成では秩序≈1＝犯罪ほぼ0（既定中立）。
/// </summary>
public static class Crime
{
    public static void Step(World w, Coefficients c)
    {
        if (!w.PoliticsEnabled) { w.PublicOrder = 1.0; w.CrimeRate = 0; return; }
        double pop = w.TotalPopulation;
        if (pop < 1e-9) return;

        // 秩序＝実効治安維持力の人口カバレッジ。財政破綻（funding↓）や正統性喪失で警察が崩れると治安も崩れる。
        // 治安維持力は人口あたりの強度（政体と同じ正規化）：実効力＝Coercion×人口/基準人口（第5段5-1＝PoliticalQuantitiesへ一元化・従来と同時点）。
        double effCoerce = PoliticalQuantities.EffectiveCoerce(w, c);
        double order = PoliticalQuantities.Clamp01(effCoerce / Math.Max(1e-9, pop * c.CrimeOrderPerPop));
        w.PublicOrder = order;
        double worstRate = 0;

        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double regionPop = r.SumPopulation();
            if (regionPop < 1e-9) continue;

            // 犯罪者の母体＝困窮（低満足）×不満（闘争度）から創発。階級フィルタは撤去：満たされた富裕層は (1−満足)≈0 で自然に weight≈0。
            // 没落した貴族・無給の兵士も困窮すれば母体に入る（désespoir は身分を選ばない）。秩序(1−order)で全体が律速。
            double criminal = 0;
            foreach (var p in r.Pops)
                criminal += p.Size * p.Militancy * (1.0 - p.Satisfaction);
            // 都市化（B-4）：過密スラム・匿名性・相対的剥奪が犯罪を増幅（19世紀ロンドン/パリ）。農村中心の地域は影響小。
            double urbanShare = r.SumUrbanPopulation() / regionPop;
            double crimeRate = c.CrimeBase * (1.0 - order) * PoliticalQuantities.Clamp01(c.CrimeMilitancyWeight * criminal / regionPop) * (1.0 + c.CrimeUrbanWeight * urbanShare);
            r.CrimeRate = crimeRate;   // 接16（監査2026-07-11・2026-07-25休眠配線）：州別値を保持（旧はローカルで捨てていた）＝移民pushの読み口（CrimePushWeight=0では誰も読まない）
            worstRate = Math.Max(worstRate, crimeRate);
            if (crimeRate <= 1e-9 || criminal <= 1e-9) continue;   // 受け手（困窮大衆）が実質不在なら盗まない＝盗んだpoolが再分配されず消える保存則違反の防止（leakhunt 2026-07-09）

            // 窃盗：全階級が貯蓄比で被害に遭い、取られた分は困窮大衆（犯罪者の母体）へ流れる（純移転＝保存）。
            // 【重要】受取重みは満足度ペナルティ適用『前』の Satisfaction で計算する（分母 criminal と同一基準）。
            // 満足度ペナルティを先に適用すると Σweight≠criminal となり done>pool＝残差が負値で末尾popから恣意的控除（貯蓄が負へ）になるため、
            // 貨幣移転を満足度ペナルティより先に済ませ、両者の基準を厳密に揃える。
            // ★2026-09-30 段4g：移し方の1本（州の全員＝払い手の重みは自分の被害＝貯蓄×犯罪率→困窮大衆＝重み 困窮×不満・端数は最後の受け手）。
            //   旧＝被害の手書きの引き落としと按分の本体の直の呼び出し（片側ずつ）。受け手の重みの和＝criminal（>1e-9 を上で確かめた）＝配り切る。
            //   受取も母体と同じ困窮×不満（階級フィルタ撤去）＝criminal と同一のペナルティ前満足度。
            double pool = 0;
            var stolen = new List<double>(r.Pops.Count);
            foreach (var p in r.Pops)
            {
                double steal = Math.Max(0, p.Savings.Value) * crimeRate;
                stolen.Add(steal);
                pool += steal;
            }
            if (pool > 1e-12)
            {
                var poor = new List<Pop>(); var need = new List<double>();
                foreach (var p in r.Pops)
                {
                    double weight = p.Size * p.Militancy * (1.0 - p.Satisfaction);
                    if (weight > 0) { poor.Add(p); need.Add(weight); }
                }
                World.Transfer((w, r.Pops), (w, poor, need), pool, fromWeights: stolen);
                if (Instruments.On) Instruments.Theft(r, pool, poor);   // ★(323) 計器（読むだけ・州ごとの窃盗の額と受け手の所得＝region_s の列）。Transfer の行（flow_s の鍵 Crime.cs:65）より後ろに置く
            }

            // 犯罪→満足度の還流（crime-is-dead-end + theft-no-property-disincentive 統合）：治安崩壊は非物質厚生（安心）を毀損する。
            // 富裕層(wealthRel>1)ほど『失う物を守る』財産不安が強い＝満足度を強く削られ、満足度経路から反動(Ideology/Politics)へ波及。
            // 較正中立：合成単一地域は order≈1 で crimeRate≈0、かつ既定 CrimeSatPenalty=CrimeWealthFearWeight=0（crimeRate乗算と二重ゲート）。満足度は非保存量で貨幣に触れない。
            // 窃盗再分配の『後』に置く：満足度を先に削ると受取重みの基準が分母 criminal とずれて保存則を破るため（上記コメント参照）。
            if (c.CrimeSatPenalty > 0 || c.CrimeWealthFearWeight > 0)
            {
                double sumW = 0, sumSz = 0;
                foreach (var pp in r.Pops) { sumW += Math.Max(0, pp.Savings.Value); sumSz += pp.Size; }
                double meanWpc = sumSz > 1e-9 ? sumW / sumSz : 0;
                foreach (var pp in r.Pops)
                {
                    double wpc = pp.Size > 1e-9 ? Math.Max(0, pp.Savings.Value) / pp.Size : 0;
                    double wealthRel = meanWpc > 1e-9 ? wpc / meanWpc : 1;
                    double penalty = crimeRate * (c.CrimeSatPenalty + c.CrimeWealthFearWeight * Math.Max(0, wealthRel - 1));
                    pp.Satisfaction = PoliticalQuantities.Clamp01(pp.Satisfaction - penalty);
                }
            }
        }
        w.CrimeRate = worstRate;
    }
}
