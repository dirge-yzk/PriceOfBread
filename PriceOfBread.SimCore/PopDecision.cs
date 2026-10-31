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

    /// <summary>都市化の天井（都市の人口の割合の上限）＝<c>BaseUrbanCap + 工業化度 × UrbanPerInd</c> を 0〜<see cref="MaxUrbanShareCap"/> に挟んだ値。人口の移動（<see cref="Run"/>）と
    /// 職の移し（<c>Meritocracy.UrbanHeadroom</c>）が同じ 1 本を読む（(532) 以前は両方に同じ式の写しがあった）。<paramref name="indForCap"/>＝天井が追う工業化度（平滑化の有無は呼び手が決める）。</summary>
    internal static double UrbanShareCap(Coefficients c, double indForCap)
        => Math.Clamp(c.BaseUrbanCap + indForCap * c.UrbanPerInd, 0, MaxUrbanShareCap);

    /// <summary>農村↔都市の対の選び（(668) 案 U）＝閉鎖階級と犯罪者を飛ばし、最初の農村 POP と最初の都市 POP を取る。人口の移動（<see cref="Run"/>）・職の移しの天井（<c>Meritocracy.UrbanHeadroom</c>）・計器 <c>UrxPdDrive</c> が同じ 1 本を読む
    /// （旧＝3 か所の手書きで、犯罪者を飛ばすのは <see cref="Run"/> と計器だけだった。犯罪者の POP は Pops の末尾に足されるので値は変わらない＝恒等）。対が成り立たなければ該当の側が null。</summary>
    internal static (Pop? Rural, Pop? Urban) MigrationPair(Region region)
    {
        Pop? rural = null, urban = null;
        foreach (var p in region.Pops)
        {
            if (p.Closed || p.IsCriminal) continue;        // 閉鎖階級（世襲の特権身分）は移民しない（史実の身分制）。★(366) 犯罪者も農村↔都市の再配分に入れない（IsUrban=false の犯罪者が〈農村〉の代表に化けるのを防ぐ）
            if (p.IsUrban) urban ??= p;
            else rural ??= p;
        }
        return (rural, urban);
    }

    /// <summary>★(706) 線 urb3（設計 20261031_都市化の目標の式_設計.md 案B）：農村と都市の対の〈移る値打ち〉V（1 人・月・その州の名目）と、その対称の相対差 g。呼び手＝<see cref="Run"/>（旗 <c>UrbanFlowReal</c> が正のとき）と計器 <c>UrxPdDrive</c>（常に・読むだけ）の 2 本＝式は 1 本。
    /// <para>農村の V＝現金の所得÷人数（負なら 0）＋自給の値÷人数（<see cref="Pop.SubsistenceValuePerMonth"/>）。都市の V＝現金の所得÷人数（負なら 0）−住居の費用÷人数。V は 0 で切る（合流 (702) の訂正＝家賃が現金を超えると分母が 0 に近づき g が発散する縁を守る・設計思想⑧の例外〈発散する量の守り〉）。</para>
    /// <para>住居の費用＝<c>HousingNeedPerUrban</c>×その州の住居の今の市場価格（<c>EconomyStep.Run</c> の都市住宅と同じ門＝州の都市の割合が <c>HousingUrbanThreshold</c> を超えたときだけ立つ）。買い手の楔は含まない（自給の値と同じ基礎の価格）。
    /// 食料の値段は別の項にしない（州の中では農村も都市も同じ値段で買う＝食料の値段の差は自給の値だけを通って現れる）。</para>
    /// <para>g＝2(V都市−V農村)÷(V都市＋V農村)（|g|≦2・両方 0 なら 0）。否定形で書く＝NaN は 0 の側。</para></summary>
    internal static MigrationValueRow MigrationValues(Region region, Coefficients coef, Pop rural, Pop urban)
    {
        double earnedR = rural.Size > Eps ? rural.EarnedPerMonth.Value / rural.Size : 0.0;
        double earnedU = urban.Size > Eps ? urban.EarnedPerMonth.Value / urban.Size : 0.0;
        double sub = rural.Size > Eps ? rural.SubsistenceValuePerMonth.Value / rural.Size : 0.0;
        double housing = 0.0;
        if (coef.HousingNeedPerUrban > 0 && Catalog.HousingId < region.Markets.Length)
        {
            double totalPop = region.SumPopulation();
            double urbanShare = totalPop > Eps ? region.SumUrbanPopulation() / totalPop : 0.0;
            if (urbanShare > coef.HousingUrbanThreshold) housing = coef.HousingNeedPerUrban * region.Markets[Catalog.HousingId].Price;
        }
        double vr = (earnedR > 0 ? earnedR : 0.0) + (sub > 0 ? sub : 0.0);
        double vuRaw = (earnedU > 0 ? earnedU : 0.0) - housing;
        double vu = vuRaw > 0 ? vuRaw : 0.0;
        double sum = vr + vu;
        double g = sum > Eps ? 2.0 * (vu - vr) / sum : 0.0;
        if (!(g == g)) g = 0.0;
        return new MigrationValueRow(vr, vu, sub > 0 ? sub : 0.0, housing, g);
    }

    /// <summary>対の〈移る値打ち〉の行（<see cref="MigrationValues"/>）。<c>SubsistenceValue</c>＝農村の 1 人あたりの自給の値・<c>HousingCost</c>＝都市の 1 人あたりの住居の費用（V に入れた量そのもの＝0 で切る前の家賃）。</summary>
    internal readonly record struct MigrationValueRow(double VRural, double VUrban, double SubsistenceValue, double HousingCost, double Gsym);

    /// <param name="serfFree">農奴解放の梯子（None:0/Limited:0.5/Full:1＝Colonial.cs入植と同じ）。rank3 SerfMobilityGate>0 のとき農村→都市流を縛る。既定1.0＝自由。</param>
    public static void Run(Region region, Coefficients coef, double serfFree = 1.0)   // 02断線走査2026-07-28：rng死引数を削除（旧MigrationNoiseはC裁定2026-07-09で撤去済み＝決定論に乱数の誤読を残さない）
    {
        if (region.Pops.Count < 2) return;

        var (rural, urban) = MigrationPair(region);
        if (rural is null || urban is null) return;

        double total = rural.Size + urban.Size;
        if (total <= Eps) return;

        // ★(706) 線 urb3：旗 UrbanFlowReal（既定 0）。正のときは目標の割合も天井も温度も持たず、〈移る値打ち〉の対称の相対差に比例させる（案B・設計 20261031_都市化の目標の式_設計.md）。
        //   流れ＝Mobility×g×送り手の規模（g＞0 は農村の規模・g＜0 は都市の規模＝1 か月に動けるのは送り手の最大 |g|×Mobility＝|g|≦2 で 4%）。農奴の門と飢えの門は下で両方の枝に掛かる。
        //   Mobility の意味は〈目標との差の月の割合〉から〈相対差 1 あたりの送り手の月の割合〉へ変わる（値は同じ）。旗 0 はこの分岐に入らず、下の else の旧の式が 1 ビットも変わらない。
        double flow;
        if (coef.UrbanFlowReal > 0)
        {
            double g = MigrationValues(region, coef, rural, urban).Gsym;
            flow = g > 0 ? coef.Mobility * g * rural.Size : coef.Mobility * g * urban.Size;
        }
        else
        {
            double incRural = rural.Size > Eps ? (rural.EarnedPerMonth / rural.Size).Value : 0.0;
            double incUrban = urban.Size > Eps ? (urban.EarnedPerMonth / urban.Size).Value : 0.0;

            // 低温 softmax（2階級＝シグモイド）：都市階級の目標シェア
            double z = Math.Clamp((incUrban - incRural) / coef.MigrationTemperature, -40, 40);
            double targetUrban = 1.0 / (1.0 + Math.Exp(-z));
            double curUrban = urban.Size / total;

            // 都市化の上限（§2.4）：工業化度が都市化の天井を決める＝都市化は工業化に従う。
            // 工業化度が無ければ前工業の天井に留まり、投資（誘導）で天井が上がって初めて都市化が進む。
            if (region.CapitalEnabled && !(coef.UrbanCapOff > 0))   // ★(605) 旗 UrbanCapOff：工業化度の天井も平滑化の更新も使わない
            {
                // 監査2026-07-05（D3）：都市化天井の工業化追従ラグ＝住宅・インフラの建設時間。UrbanCapFollowSpeed 既定0.02（点火2026-08-16・時定数≈4年）で天井が工業化に数年遅れて追いつく。1.0 なら即時追従＝旧挙動。
                double indForCap = region.TotalIndustrialization;   // 都市化天井は立っている建屋・雇用基盤の総量＝新旧合計（座礁しても街は残る。ヴィンテージ休眠時は恒等・2026-07-10）
                if (coef.UrbanCapFollowSpeed < 1.0)
                {
                    if (region.UrbanIndSmoothed < 0) region.UrbanIndSmoothed = region.TotalIndustrialization;   // 初回＝現在値から開始（ジャンプなし）
                    region.UrbanIndSmoothed += coef.UrbanCapFollowSpeed * (region.TotalIndustrialization - region.UrbanIndSmoothed);
                    indForCap = region.UrbanIndSmoothed;
                }
                double maxUrbanShare = UrbanShareCap(coef, indForCap);
                if (targetUrban > maxUrbanShare) targetUrban = maxUrbanShare;
            }

            flow = coef.Mobility * (targetUrban - curUrban) * total;   // 決定論的緩和（§0-B）。旧MigrationNoise（実験用ノブ）はC裁定2026-07-09で撤去
        }

        // rank3（SerfMobilityGate=1.0点火2026-08-16・社会移動監査2026-08-15）：農奴は移動証明・人格的隷属で都市へ出られない。
        // 農村→都市方向のみ (1−g)+g×serfFree を乗算（都市→農村は自由＝非対称）。露普の都市化が農奴解放後に始まる順序が法から出る。0＝厳密恒等。
        if (flow > 0 && coef.SerfMobilityGate > 0)
            flow *= (1.0 - coef.SerfMobilityGate) + coef.SerfMobilityGate * serfFree;

        // ★(143) 飢饉の線の下にあって農村より食べられない都市の POP へは移らない（線2 後半・飢えで州が痩せる型の残り）。
        //   旧＝目標の都市の割合は貨幣の所得の差のシグモイド（差0で 0.5）を都市化の上限で切るだけ＝植民地の州は都市の POP が飢えていても上限（0.20）へ引かれ続けた
        //   （(143) の計器・種1 1783-1833＝植民地の工場労働者は州の 12%・失業 60%・主食の充足 0.22〔0.4 未満が 75%〕・所得 0.116／人月〔本国の労働者 1.67〕・農民は自給 70% で充足 0.79）
        //   ＝農民が飢えた都市へ移り飢えの死で消える流しの管＝植民地の州が百年かけて痩せる型の半分（残りは浅い慢性の飢え＝DemographicStep の飢えの深さの率）。
        //   農村がもっと飢えている時（農村の飢饉で町へ逃げる）と、都市が飢饉の線の上にいる時は、これまでどおり移る。新しい数0（門は FamineMigrationFillGate）。
        if (flow > 0 && urban.FoodFill < coef.FamineMigrationFillGate && urban.FoodFill < rural.FoodFill) flow = 0;
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
