namespace PriceOfBread.SimCore;

/// <summary>
/// 創発的国内勢力 F3a＝クラスタの**永続化と変異**（2026-07-12・裁定＝docs/2026-07-09_設計メモ.md §3 Phase3 の前半）。
/// F2（FactionSnapshot＝表示専用の瞬間値）に「持続する運動体」の実体を与える：
/// - **結束（Cohesion）**＝同じクラスタ（州×ブロック×イデオロギー主軸）が四半期をまたいで観測され続けるほど育つストック
///   （指導部は新状態を持たず、成長率がブロックの意識に比例＝識字・意識から創発）。消えたクラスタは減衰し、やがて忘れられる。
/// - **変異（Mutated）**＝危機（州の飢え・敗戦）の中の結束したクラスタが稀に千年王国的な旗を掲げる（太平天国・タボル派）。
///   独立RNG系列（World.FactionRng）＝主系列・豊凶・疫病と完全分離＝較正/決定論に非干渉。名前と結束ボーナスのみ＝
///   **要求は既存19法軸のまま**（ユーザー裁定2026-07-11＝宗教法ラダーは作らない）。
/// - F3a は書く側（結束/変異/合流の状態更新）。読み手＝F3b（Politics.CheckFactionRevolt／FactionStrength＝FactionRevoltScale=1.0 稼働中・0で観察のみ）と表示。
/// 較正/最小世界＝ParliamentEnabled=false または FactionRng=null で完全不発（EthnicEnabled と同型の遮蔽）。
/// SaveGame はリフレクション直列化で List/Region参照/RNG状態とも自動追従。
/// </summary>
public sealed class FactionState
{
    public Region Region = null!;      // クラスタの本拠州（参照持ち＝SecedingRegion と同じ作法・割譲等で消えれば不在減衰で自然消滅）
    public int Bloc;                   // 0=旧支配層 1=近代エリート 2=大衆
    public int Ideo;                   // イデオロギー主軸（Ideology の序数）
    public double Size;                // 直近観測の規模（人口千人）
    public double AvgMilitancy;        // 直近観測の平均闘争度
    public double Cohesion;            // 結束∈[0,1]＝運動体としての持続の証（発火ゲート・F3b）
    public int AbsentQuarters;         // 連続不在の四半期数（8Q＋低結束で除去）
    public bool Mutated;               // 変異済み（千年王国の旗）＝結束成長2倍
    public string MutantName = "";     // 変異名（「広州の太平会」等）。未変異は空＝表示は機械生成名
    public double Rejections;          // ★段3（内戦一元化doc §4・2026-08-30）：拒否の累積（弾圧・対決・別軸譲歩=本丸拒否が+1・受諾で0へ）。読み手は FactionRejectionMutation>0 の変異ロールのみ＝0では積むだけ＝恒等
    public double EquipFill = 1.0;     // ★武器の市場購入（2026-09-11）：装備の充足＝国軍の MilitaryEquipFill と**同じ関数**（EconomyStep.EquipFill）の値。書き手は EconomyStep の武器購入の決済だけ＝休眠（RebelArmsMarketEnabled=0）では 1.0 のまま（FactionStrength も読まない）
    public Dictionary<int, double>? ArmsStock;   // 運動の武器庫（財ID→量）＝国軍の Region.ArmsStock と同じ物差し・同じ摩耗（ArmsFillContribution）。休眠では null（1件も作られない）。新しい運動は0から（国軍の「未初期化は目標」は当てない）
    // ★要求は法の経路（2026-09-11）：通知（PendingFactionCrisis）時点の要求の記録＝「要求した法が通常の経路で動いたか」を次の評価で比べる物差し。
    //   CrisisDemands＝要求の軸（序数+1・増加＝正／減少＝負・先頭＝本丸＝ComputeDemands のスコア順）／CrisisDemandPos＝同時点の FactionSnapshot.AxisPosition。
    //   通知が立っていない運動は null（SaveGame は null/配列とも自動追従）。運動は解散しない（09-09）＝通知が下りるときに null へ戻す。
    public int[]? CrisisDemands;
    public double[]? CrisisDemandPos;
}

public static class Factions
{
    // 変異名の語幹と接尾（千年王国運動の型・国を問わず読める語彙）。RNG上位bitでindex＝決定論。
    private static readonly string[] MutantStems = ["太平", "千年", "救世", "光明", "天命", "新生", "浄化", "黎明", "再臨", "弥勒"];
    private static readonly string[] MutantSuffixes = ["会", "団", "軍", "同盟"];

    private const double MutationCohesionGate = 0.3;   // 変異ロールに要る結束の下限（旗は結束した運動体のみ・頻度絞り2026-07-12）
    private const double ForgetCohesionFloor = 0.05;   // これ未満の結束＋長期不在で運動を忘却

    /// <summary>四半期の更新（Politics.Step から・PoliticsEnabled世界のみ）。観測クラスタと永続状態を突き合わせ、結束を育て、危機で変異をロールする。</summary>
    public static void Step(World w, Coefficients c)
    {
        if (!w.ParliamentEnabled || w.FactionRng is null) return;   // 較正/最小世界＝完全不発（EthnicEnabled と同型）
        double totalPop = w.TotalPopulation;
        if (totalPop <= 1e-9) return;

        var observed = FactionSnapshot.ComputeClusters(w, countCost: true);   // 共有核＝F2表示と同じ束ね方（決定論）
        // ブロックの意識は bloc（0..2）のみに依存し本Step中は不変＝クラスタごとの再計算を避け遅延キャッシュ（値・走査順は従来と同一）。
        var consciousnessByBloc = new double?[PolicyPreferences.BlocCount];
        // 1) 観測→状態の照合（キー＝州参照×ブロック×主軸）。観測順→状態順の走査＝決定論。
        foreach (var (reg, bloc, ideo, size, milSum) in observed)
        {
            if (size < FactionSnapshot.MinSizeFraction * totalPop) continue;   // 微小クラスタは運動体にならない（F2と同じ足切り）
            FactionState? st = null;
            foreach (var f in w.Factions) if (ReferenceEquals(f.Region, reg) && f.Bloc == bloc && f.Ideo == ideo) { st = f; break; }
            if (st is null) { st = new FactionState { Region = reg, Bloc = bloc, Ideo = ideo }; w.Factions.Add(st); }
            st.Size = size; st.AvgMilitancy = milSum / size;
            st.AbsentQuarters = 0;
            // 結束の成長＝意識ある層ほど組織が速い（指導部の創発）。変異済みは2倍（旗の下の凝集）。
            double consc = consciousnessByBloc[bloc] ??= BlocConsciousness(w, bloc);
            double growth = c.FactionCohesionGrowth * (0.5 + consc) * (st.Mutated ? 2.0 : 1.0);
            st.Cohesion = PoliticalQuantities.Clamp01(st.Cohesion + growth * (1.0 - st.Cohesion));
            // 2) 変異のロール（1クラスタ1回まで・危機×結束のみ・独立RNG系列）
            if (!st.Mutated && st.Cohesion >= MutationCohesionGate)   // 変異は結束した運動体のみ（頻度絞り2026-07-12＝旗は安売りしない）
            {
                double crisis = System.Math.Max(1.0 - reg.FoodFill, w.DefeatShock);
                // ★段3（内戦一元化doc §4・2026-08-30）：拒否の累積も変異の引き金＝「ピータールー→チャーティズム」「血の日曜日→1917」。
                //   危機と max 合成（doc の処方どおり）。FactionRejectionMutation=0＝crisis のまま＝厳密恒等。
                if (c.FactionRejectionMutation > 0)
                    crisis = System.Math.Max(crisis, System.Math.Min(1.0, c.FactionRejectionMutation * st.Rejections));
                if (crisis > c.FactionMutationCrisisFloor)
                {
                    ulong u = w.FactionRng.NextULong();
                    double roll = (u & 0xFFFF_FFFF) / (double)uint.MaxValue;   // 下位32bit＝受理判定
                    if (roll < c.FactionMutationRate * crisis)
                    {
                        st.Mutated = true;
                        int stem = (int)((u >> 32) % (ulong)MutantStems.Length);
                        int suf = (int)((u >> 48) % (ulong)MutantSuffixes.Length);
                        st.MutantName = $"{reg.Name}の{MutantStems[stem]}{MutantSuffixes[suf]}";
                        st.Cohesion = System.Math.Max(st.Cohesion, c.MutantCohesionFloor);   // 旗＝結束の床
                        w.FactionMutations++;
                        w.LogAdd($"{w.Year} {w.SelfPower?.Name}で{st.MutantName}が旗を掲げた");
                    }
                }
            }
        }
        // 3) 不在クラスタの減衰と忘却（走査は逆順＝除去安全）。観測済みは AbsentQuarters=0 に更新済み。
        for (int i = w.Factions.Count - 1; i >= 0; i--)
        {
            var f = w.Factions[i];
            bool seen = false;
            foreach (var (reg, bloc, ideo, size, _) in observed)
                if (ReferenceEquals(f.Region, reg) && f.Bloc == bloc && f.Ideo == ideo && size >= FactionSnapshot.MinSizeFraction * totalPop) { seen = true; break; }
            if (seen) continue;
            f.AbsentQuarters++;
            f.Cohesion *= 1.0 - c.FactionCohesionDecay;
            if (f.Cohesion < ForgetCohesionFloor && f.AbsentQuarters >= c.FactionForgetQuarters) w.Factions.RemoveAt(i);   // 忘れられた運動
        }
        // 4) ★段6（内戦一元化doc §3・spec §3-2＝唯一の新規実装・2026-08-30）：派閥の合流＝太平天国の北上（通過地の不満民を吸収）。
        //    条件3つ＝【敵】同ブロック×同イデオロギー（同じ的）・【手段】変異状態の一致（旗と旗・議会派と議会派）・【距離】FactionMergeRangeKm以内。
        //    大きい方が小さい方を吸収＝結束を継ぎ足し（新しい頭数は参加述語が毎期導出＝規模は運ばない）。1四半期1件×勝者（連鎖合体の暴走なし）。
        //    FactionMergeRangeKm=0＝この節に1命令も入らない＝厳密恒等。走査順＝州名の全順序＝決定論。
        if (c.FactionMergeRangeKm > 0 && w.Factions.Count >= 2)
        {
            var merged = new System.Collections.Generic.HashSet<FactionState>();
            // ★段0＝幽霊の封じ（F3b蜂起doc §7-1・2026-09-08・係数なし）：反乱国家が建った後の本拠州は親の Regions を離れるのに
            //   FactionState は親のリストに残る。除外しないと**他国の州の運動**が親の運動へ結束を継ぎ足す（合流の相手が国外）。
            //   選定ループ（Politics.CheckFactionRevolt）と同じ述語で、同じ理由で外す。
            var ordered = new System.Collections.Generic.List<FactionState>(w.Factions.Count);
            foreach (var f0 in w.Factions) if (w.Regions.Contains(f0.Region)) ordered.Add(f0);
            ordered.Sort((a, b) => a.Region.Name != b.Region.Name ? string.CompareOrdinal(a.Region.Name, b.Region.Name) : a.Bloc != b.Bloc ? a.Bloc.CompareTo(b.Bloc) : a.Ideo.CompareTo(b.Ideo));
            for (int i2 = 0; i2 < ordered.Count; i2++)
                for (int j2 = i2 + 1; j2 < ordered.Count; j2++)
                {
                    var a = ordered[i2]; var b = ordered[j2];
                    if (merged.Contains(a) || merged.Contains(b)) continue;
                    if (a.Bloc != b.Bloc || a.Ideo != b.Ideo || a.Mutated != b.Mutated || ReferenceEquals(a.Region, b.Region)) continue;
                    var ca = Climate.CityCoord(a.Region.Name); var cb = Climate.CityCoord(b.Region.Name);
                    if ((ca.Lat == 0 && ca.Lon == 0) || (cb.Lat == 0 && cb.Lon == 0)) continue;
                    if (Geo.DistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon) > c.FactionMergeRangeKm) continue;
                    var big = a.Size >= b.Size ? a : b; var small = big == a ? b : a;
                    big.Cohesion = PoliticalQuantities.Clamp01(big.Cohesion + c.FactionMergeCohesionGain.Value * small.Cohesion);
                    big.Rejections = System.Math.Max(big.Rejections, small.Rejections);   // 怨嗟は深い方が残る
                    small.Cohesion = 0; small.AbsentQuarters = (int)c.FactionForgetQuarters;   // 吸収＝次の掃除で自然消滅（即Removeせず走査を壊さない）
                    merged.Add(big); merged.Add(small);
                    w.LogAdd($"{w.Year} {DisplayName(small)}が{DisplayName(big)}に合流した");
                }
        }
    }

    /// <summary>表示名：変異済みは旗の名・未変異は機械生成名（F2と同じ語彙）。</summary>
    public static string DisplayName(FactionState f) =>
        f.Mutated ? f.MutantName : $"{f.Region.Name}の{FactionSnapshot.IdeologyJa((Ideology)f.Ideo)}{FactionSnapshot.BlocJa(f.Bloc)}";

    // ブロックの人口加重平均の意識（結束の成長率＝識字・意識のある層ほど組織化が速い）。
    private static double BlocConsciousness(World w, int bloc)
    {
        double sum = 0, size = 0;
        foreach (var p in w.AllPops())
        {
            int b = PolicyPreferences.BlocOf(p);
            if (b != bloc || p.IsSoldier) continue;
            sum += p.Consciousness * p.Size; size += p.Size;
        }
        return size > 1e-9 ? sum / size : 0;
    }
}
