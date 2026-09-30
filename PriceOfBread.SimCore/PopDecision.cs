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

        // ★(143) 飢饉の線の下にあって農村より食べられない都市の POP へは移らない（線2 後半・飢えで州が痩せる型の残り）。
        //   旧＝目標の都市の割合は貨幣の所得の差のシグモイド（差0で 0.5）を都市化の上限で切るだけ＝植民地の州は都市の POP が飢えていても上限（0.20）へ引かれ続けた
        //   （(143) の計器・種1 1783-1833＝植民地の工場労働者は州の 12%・失業 60%・主食の充足 0.22〔0.4 未満が 75%〕・所得 0.116／人月〔本国の労働者 1.67〕・農民は自給 70% で充足 0.79）
        //   ＝農民が飢えた都市へ移り飢えの死で消える流しの管＝植民地の州が百年かけて痩せる型の半分（残りは浅い慢性の飢え＝DemographicStep の飢えの深さの率）。
        //   農村がもっと飢えている時（農村の飢饉で町へ逃げる）と、都市が飢饉の線の上にいる時は、これまでどおり移る。新しい数0（門は飢饉死の門 FamineDeathFillFloor）。
        if (flow > 0 && urban.FoodFill < coef.FamineDeathFillFloor && urban.FoodFill < rural.FoodFill) flow = 0;
        if (flow > 0) Move(rural, urban, flow);     // 農村 → 都市
        else if (flow < 0) Move(urban, rural, -flow); // 都市 → 農村
    }

    /// <summary>src から dst へ amount 人を移す（農村⇄都市）＝人の移し方の1本（<see cref="Pop.Blend"/>・2026-09-30 処理の一元化 段4e I）。
    /// 移住者は自分の人的資本（識字）・不満（闘争度）・政治意識・国民意識・思想・年齢と男女の構成を携えて着地の POP へ混ざり、貯蓄・債務・国債の保有は1人あたりで一緒に移る（保存）。
    /// ⚠旧＝ここの写しは女性比を子供込みの規模で重み付けた（成人のうちの女性比＝成人数で重み付ける＝Military の G11#2 の直しが届いていなかった）。</summary>
    private static void Move(Pop src, Pop dst, double amount)
    {
        if (amount <= 0 || src.Size <= Eps) return;
        Pop.Blend(src, dst, amount);
    }
}
