namespace PriceOfBread.SimCore;

/// <summary>
/// pop 意思決定（§9-D・段1）。農村↔都市の階級間で、1人あたり所得の差を低温 softmax で
/// 緩和的に再配分する（プレイヤーは勾配を変えるだけ・結果は pop が選ぶ §2.2.1）。
/// 規模と貯蓄は移動者に比例して一緒に動かす（人口・貨幣を保存）。
/// 1階級以下なら何もしない（段0 はここを通っても無効）。
/// </summary>
public static class PopDecision
{
    private const double Eps = 1e-9;
    private const double MaxUrbanShareCap = 0.85;   // 都市化天井の絶対上限（§2.4・工業化に追従する天井のハードストップ）

    /// <param name="serfFree">農奴解放の梯子（None:0/Limited:0.5/Full:1＝Colonial.cs入植と同じ）。rank3 SerfMobilityGate>0 のとき農村→都市流を縛る。既定1.0＝自由。</param>
    public static void Run(Region region, Coefficients coef, double serfFree = 1.0)   // 02断線走査2026-07-28：rng死引数を削除（旧MigrationNoiseはC裁定2026-07-09で撤去済み＝決定論に乱数の誤読を残さない）
    {
        if (region.Pops.Count < 2) return;

        Pop? rural = null, urban = null;
        foreach (var p in region.Pops)
        {
            if (p.Closed) continue;        // 閉鎖階級（世襲の特権身分）は移民しない（史実の身分制）
            if (p.IsUrban) urban ??= p;
            else rural ??= p;
        }
        if (rural is null || urban is null) return;

        double incRural = rural.Size > Eps ? rural.Earned / rural.Size : 0.0;
        double incUrban = urban.Size > Eps ? urban.Earned / urban.Size : 0.0;
        double total = rural.Size + urban.Size;
        if (total <= Eps) return;

        // 低温 softmax（2階級＝シグモイド）：都市階級の目標シェア
        double z = Math.Clamp((incUrban - incRural) / coef.MigrationTemperature, -40, 40);
        double targetUrban = 1.0 / (1.0 + Math.Exp(-z));
        double curUrban = urban.Size / total;

        // 都市化の上限（§2.4）：工業化度が都市化の天井を決める＝都市化は工業化に従う。
        // 工業化度が無ければ前工業の天井に留まり、投資（誘導）で天井が上がって初めて都市化が進む。
        if (region.CapitalEnabled)
        {
            // 監査2026-07-05（D3）：都市化天井の工業化追従ラグ＝住宅・インフラの建設時間。UrbanCapFollowSpeed 既定0.02（点火2026-08-16・時定数≈4年）で天井が工業化に数年遅れて追いつく。1.0 なら即時追従＝旧挙動。
            double indForCap = region.TotalIndustrialization;   // 都市化天井は立っている建屋・雇用基盤の総量＝新旧合計（座礁しても街は残る。ヴィンテージ休眠時は恒等・2026-07-10）
            if (coef.UrbanCapFollowSpeed < 1.0)
            {
                if (region.UrbanIndSmoothed < 0) region.UrbanIndSmoothed = region.TotalIndustrialization;   // 初回＝現在値から開始（ジャンプなし）
                region.UrbanIndSmoothed += coef.UrbanCapFollowSpeed * (region.TotalIndustrialization - region.UrbanIndSmoothed);
                indForCap = region.UrbanIndSmoothed;
            }
            double maxUrbanShare = Math.Clamp(coef.BaseUrbanCap + indForCap * coef.UrbanPerInd, 0, MaxUrbanShareCap);
            if (targetUrban > maxUrbanShare) targetUrban = maxUrbanShare;
        }

        double flow = coef.Mobility * (targetUrban - curUrban) * total;   // 決定論的緩和（§0-B）。旧MigrationNoise（実験用ノブ）はC裁定2026-07-09で撤去

        // rank3（SerfMobilityGate=1.0点火2026-08-16・社会移動監査2026-08-15）：農奴は移動証明・人格的隷属で都市へ出られない。
        // 農村→都市方向のみ (1−g)+g×serfFree を乗算（都市→農村は自由＝非対称）。露普の都市化が農奴解放後に始まる順序が法から出る。0＝厳密恒等。
        if (flow > 0 && coef.SerfMobilityGate > 0)
            flow *= (1.0 - coef.SerfMobilityGate) + coef.SerfMobilityGate * serfFree;

        if (flow > 0) Move(rural, urban, flow, coef);     // 農村 → 都市
        else if (flow < 0) Move(urban, rural, -flow, coef); // 都市 → 農村
    }

    /// <summary>src から dst へ amount 人を移す。貯蓄は1人あたりで按分して一緒に移す（保存）。男女比は移動者で加重混合。</summary>
    private static void Move(Pop src, Pop dst, double amount, Coefficients coef)
    {
        if (amount <= 0 || src.Size <= Eps) return;
        amount = Math.Min(amount, src.Size);
        double perCapitaSavings = src.Savings / src.Size;
        double moveSavings = perCapitaSavings * amount;
        double moveDebt = src.Debt / src.Size * amount;   // 個人債務も按分して運ぶ（2026-08-02・既定0で恒等）
        double moveBond = src.BondHoldings / src.Size * amount;   // ★国債の保有台帳（2026-09-21）も貯蓄と同じ按分（州内の移動＝発行国は同じ）

        double newSize = dst.Size + amount;
        if (newSize > Eps)
        {
            dst.FemaleShare = (dst.FemaleShare * dst.Size + src.FemaleShare * amount) / newSize;
            dst.ChildShare = (dst.ChildShare * dst.Size + src.ChildShare * amount) / newSize;
            dst.OldShare = (dst.OldShare * dst.Size + src.OldShare * amount) / newSize;   // 倍率排除⑧：国内移動も老年比率を運搬（OldShare=0の世界では恒等＝較正中立）
            dst.CoreEthnicShare = (dst.CoreEthnicShare * dst.Size + src.CoreEthnicShare * amount) / newSize;   // 倍率排除⑧：国内移動は中核民族割合を運搬
            // 人1：識字・闘争度の運搬（4移動サイトの属性運搬を統一）。移住者は自分の人的資本(識字)と不満(闘争度)を携えて着地popへ加重混合。
            // 欠けると農村→都市の移住者が着地と同時に無償で都市の識字を獲得（人的資本の生成）＝per-region分布が歪む。国家集計では加重平均で中立。
            // MigrationCarriesHumanCapital 既定1.0（#17で点火済み・live）＝運搬する。0 で旧挙動（着地時無償識字）。
            if (coef.MigrationCarriesHumanCapital > 0)
            {
                dst.Literacy = (dst.Literacy * dst.Size + src.Literacy * amount) / newSize;
                dst.Militancy = Military.BlendCarry(dst.Militancy, dst.Size, src.Militancy, amount, newSize, clamp: true);   // ★運搬1本化（段1・恒等）
            }
        }
        src.Size -= amount;
        src.Savings -= moveSavings;
        src.Debt -= moveDebt; src.BondHoldings -= moveBond;   // ⚠既存バグ修正2026-08-08：moveDebt が計算されるだけで**適用行が無かった**（按分6箇所ルール違反＝移動者の債務が送り出し側に残る）
        dst.Size += amount;
        dst.Savings += moveSavings;
        dst.Debt += moveDebt; dst.BondHoldings += moveBond;
    }
}
