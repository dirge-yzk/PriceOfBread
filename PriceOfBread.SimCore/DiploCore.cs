namespace PriceOfBread.SimCore;

/// <summary>★外交コアの確定形＝過不足ベクトルと関係値（docs/202608/20260814_緊張度をコアにする_部分完了.md・作者裁定 2026-08-14）。
///
/// 各国は s ∈ [−1,+1]^8（8カテゴリの過不足）と P ∈ [0,1]（軍事力・相対）を持ち、
///   競合度(a,b) ＝ s_a · s_b            ⚠内積**そのまま**＝同符号（共に過多/共に不足）が正＝【競合】
///   補完度(a,b) ＝ −競合度(a,b)         ★異符号（過多×不足）が正＝【補完】
///   コア項(a,b) ＝ 補完度 × min(P_a,P_b) × 交易到達
/// が関係値の芯になる。⚠符号の罠は <c>DiploCoreTests</c> の2本が固定している（設計整合レビュー#1＝式より先にテスト）。
///
/// ★現段階＝**観測のみ**（§12 手順1）：この型の値はどの挙動にも配線されていない。
///   Runner の diploprobe が全ペア表を出し、史実ペアとの照合（露土・英露が内積から出ないことの確認＝13l）を経てから点火する。
///
/// ⚠カテゴリ表は**手書きの仮表**（13a 裁定＝「現行案で観測を開始し、あまりに乖離するなら調整」）。
/// ⚠正規化（充足率→[−1,+1]）は 13b 未決＝**仮置き**：s_k = clamp((生産V−需要V)/max(生産V,需要V,ε), −1, +1)・RefPrice建て
///   （市場価格建てにすると価格の循環が入る＝S4欠陥の教訓と同じ理由で参照価格を使う）。
/// ⚠金・銀は外す（Bank.Gold が別管理＝通貨制度）。⑧金融は財でない＝資本家貯蓄の対GDP比（正規化は probe 側で世界中央値比）。</summary>
public static class DiploCore
{
    public enum Cat { Staple = 0, Fuel = 1, Fiber = 2, Metal = 3, Chemical = 4, ColonialGoods = 5, Industry = 6, Finance = 7 }
    public const int CatCount = 8;
    public static readonly string[] CatNames = ["主食", "燃料", "繊維原料", "金属", "化学肥料", "植民地物産", "工業製品", "金融"];

    // ── 手書き仮表（13a）。①主食は GoodTier.Staple ＋穀物上流（麦・小麦粉＝露の穀物輸出を表現）・⑥は IsColonial（実行時）──
    private static readonly int[] StapleExtra = [Catalog.WheatId, Catalog.FlourId];                      // 麦・小麦粉（Staple tier の上流＝過不足は連鎖の頭で余る）
    private static readonly int[] FuelGoods = [Catalog.CoalId, Catalog.CrudeOilId, Catalog.TimberId, Catalog.LampOilId, Catalog.GasId];        // 石炭・原油・木・コークス・灯油・ガス
    private static readonly int[] FiberGoods = [Catalog.CottonId, Catalog.HempId, Catalog.SilkId, Catalog.WoolId];               // 綿花・麻・生糸・羊毛
    private static readonly int[] MetalGoods = [Catalog.IronOreId, Catalog.CopperOreId, Catalog.LeadOreId, Catalog.IronId, Catalog.SteelId, Catalog.CopperId, Catalog.LeadId, Catalog.ChromeId, Catalog.TinOreId];   // 鉄鉱石・銅鉱・鉛鉱・鉄・鋼・銅・鉛・クロム・錫鉱
    private static readonly int[] ChemicalGoods = [Catalog.SulfurId, Catalog.ChemicalsId, Catalog.GunpowderId, Catalog.FertilizerId, Catalog.SaltpeterId, Catalog.DynamiteId];   // 硫黄・化学薬品・火薬・肥料・硝石・ダイナマイト（グアノ121はIsColonial=true→⑥へ先に分類されるため掲載しない＝到達不能だった死エントリを削除）
    private static readonly int[] IndustryGoods = [Catalog.CottonClothId, Catalog.WoolenClothId, Catalog.SilkClothId, Catalog.MachineryId, Catalog.SteamEngineId, Catalog.RailId, Catalog.ClothingId, Catalog.ArtilleryId, Catalog.RifleId, Catalog.AmmunitionId, Catalog.SailingShipId, Catalog.SteamshipId, Catalog.WarshipId, Catalog.LocomotiveId, Catalog.MachineGunId, Catalog.IroncladId, Catalog.TankId, Catalog.DreadnoughtId, Catalog.CombustionEngineId, Catalog.BicycleId, Catalog.MilitaryAircraftId, Catalog.AutomobileId, Catalog.ChemicalWeaponId];
    // 織物3種・衣料＋機械系＋軍需（⚠電力87/送電網99/上下水道111＝NonTradeable の役務インフラは対外過不足に載らないので除外）

    /// <summary>財id→カテゴリ（-1＝どのカテゴリにも属さない）。⑥は IsColonial を実行時に読む。</summary>
    public static int CategoryOf(int gid)
    {
        var g = Catalog.Goods[gid];
        if (g.Tier == GoodTier.Staple) return (int)Cat.Staple;
        if (g.IsColonial) return (int)Cat.ColonialGoods;
        if (System.Array.IndexOf(StapleExtra, gid) >= 0) return (int)Cat.Staple;
        if (System.Array.IndexOf(FuelGoods, gid) >= 0) return (int)Cat.Fuel;
        if (System.Array.IndexOf(FiberGoods, gid) >= 0) return (int)Cat.Fiber;
        if (System.Array.IndexOf(MetalGoods, gid) >= 0) return (int)Cat.Metal;
        if (System.Array.IndexOf(ChemicalGoods, gid) >= 0) return (int)Cat.Chemical;
        if (System.Array.IndexOf(IndustryGoods, gid) >= 0) return (int)Cat.Industry;
        return -1;
    }

    /// <summary>国の過不足ベクトル s ∈ [−1,+1]^8（観測用・純関数＝状態を書かない）。
    /// ★観測v2（2026-08-14・v1の器の欠陥3つを是正）：
    ///   (1) 過不足＝**純輸出**（LastExport−LastImport・RefPrice建て）÷取扱高。v1の(生産−需要)は市場清算後で±0.1に平坦だった
    ///       ——docの意図は貿易ポジション（英1900の⑦++＝工業品の輸出国）である。
    ///   (2) ⑥植民地物産＝**植民地の産出を宗主の供給へ算入**。v1は本国州だけを見て全国−1.00の定数次元と化し、
    ///       全ペアへ一様な「共に不足＝競合」を撒くノイズだった（このカテゴリの意味は「誰が供給を支配しているか」）。
    ///   (3) ⑧金融＝**対外資本ポジション（CapitalAbroad−ForeignCapitalIn）÷名目GDP**。v1の貯蓄/GDP比は英が負に出た（逆）。
    /// 正規化の確定は13b＝観測が史実の形を出してから。</summary>
    public static double[] SupplyVector(World w)
    {
        // ★観測v3（2026-08-14）：v2の欠陥2つを是正。
        //   (a) 財の流れ＝Region.LastExport/Import は四半期境界でリセットされ、StepQuarter 直後の読みで全0になっていた
        //       → **World.ExportsByGood/ImportsByGood**（BilateralMarket が決済ごとに書く国別×財別の値・次の決済まで持続）を読む。
        //   (b) 正規化＝ターンオーバー比は一方向貿易で±1に飽和し「植民地を持たない国同士が全員競合」の珍事を生んだ
        //       → **GDP比**へ：s_k = clamp(年換算純輸出_k / (κ×名目GDP), −1, +1)・κ=0.03（あるカテゴリの純輸出が年GDPの3%＝全強度）。
        //       ⚠κは観測器の目盛りであって力学の係数ではない（13b の確定はこの観測の収束後）。
        var netX = new double[CatCount];
        var eg = w.ExportsByGood; var ig = w.ImportsByGood;
        if (eg is not null && ig is not null)
            for (int gi = 0; gi < eg.Length && gi < Catalog.Goods.Length; gi++)
            {
                int cat = CategoryOf(gi);
                if (cat < 0 || cat == (int)Cat.Finance) continue;
                netX[cat] += eg[gi] - (gi < ig.Length ? ig[gi] : 0);
            }
        // ⑥植民地物産＝植民地の産出を宗主の供給側に算入（Colony.Resources×人口ウェイト＝供給支配の近似・RefPrice建て・月次相当）
        if (w.SelfPower is { } sp)
        {
            foreach (var col in sp.ColonyList)
                foreach (int gid in col.Resources)
                {
                    if (gid < 0 || gid >= Catalog.Goods.Length) continue;
                    if (CategoryOf(gid) != (int)Cat.ColonialGoods) continue;
                    netX[(int)Cat.ColonialGoods] += Catalog.RefPrices[gid] * System.Math.Max(0, col.Population) * ColonySupplyScale;
                }
            // ★是正（型統合後の旧読み手・監査2026-09-02 #1）：ColoniesAreRegions=1 で州へ昇格した植民地は ColonyList から消える
            //（Diplomacy.cs:47-50 の両建て規約と同じ理由）。昇格州は元 Colony の Resources を保持する（Simulation.PromoteColonyToRegion）ので同じ物差しで足す。
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || r.Tier != RegionTier.Colonial) continue;
                foreach (int gid in r.Resources)
                {
                    if (gid < 0 || gid >= Catalog.Goods.Length) continue;
                    if (CategoryOf(gid) != (int)Cat.ColonialGoods) continue;
                    netX[(int)Cat.ColonialGoods] += Catalog.RefPrices[gid] * System.Math.Max(0, r.SumPopulation()) * ColonySupplyScale;
                }
            }
        }
        var s = new double[CatCount];
        double scale = System.Math.Max(1.0, (0.03 * w.NominalGdpPerMonth).Value);   // κ=0.03（年GDP比）
        for (int k = 0; k < CatCount; k++)
        {
            if (k == (int)Cat.Finance) continue;
            double v = netX[k] * w.Coef.ClearingsPerYear / scale;   // 清算単位→年換算（月次清算に追随・2026-08-22）
            v = System.Math.Clamp(v, -1, 1);
            if (!(v >= -1 && v <= 1)) v = 0;   // NaNは中立へ（否定形）
            s[k] = v;
        }
        // ⑧金融＝対外資本ポジション（債権+／債務−）・GDP比。目盛りは FinanceScale（観測で較正＝probe が生値も出す）
        double net = (w.CapitalAbroad - w.ForeignCapitalIn) / System.Math.Max(1.0, w.NominalGdpPerMonth.Value);
        double f = System.Math.Clamp(net / FinanceScale, -1, 1);
        if (!(f >= -1 && f <= 1)) f = 0;
        s[(int)Cat.Finance] = f;
        return s;
    }

    public const double ColonySupplyScale = 20.0;   // 植民地1人口ウェイトの供給価値の目盛り（観測用・較正対象）
    public const double FinanceScale = 0.5;         // 対外資本ポジション=GDPの50%で全強度（観測用・probe が生値併記）

    /// <summary>競合度＝s_a·s_b。⚠内積そのまま＝同符号（共に過多・共に不足）が正＝競合（doc §0）。</summary>
    public static double Rivalry(double[] a, double[] b)
    {
        double d = 0;
        for (int k = 0; k < CatCount; k++) d += a[k] * b[k];
        return d;
    }

    /// <summary>補完度＝−競合度。異符号（過多×不足）が正＝補完＝交易の利得がある関係。</summary>
    public static double Complement(double[] a, double[] b) => -Rivalry(a, b);

    /// <summary>コア項＝補完度 × min(P_a,P_b) × 交易到達。符号を持つ（正＝協力・負＝敵対）。
    /// min(P)＝関係を動かすのは弱い方の力が上限（13k＝小国同盟が弱くなる懸念は未決・観測で見る）。</summary>
    public static double CoreTerm(double[] a, double[] b, double pA, double pB, double reach)
        => Complement(a, b) * System.Math.Min(PoliticalQuantities.Clamp01(pA), PoliticalQuantities.Clamp01(pB)) * PoliticalQuantities.Clamp01(reach);

    /// <summary>交易到達の仮関数（13i 未決＝仮置き）：首都間距離の半減カーネル。
    /// ★本実装では BilateralMarket の実経路コストを使う（doc §0）——観測の初回はこの近似で回し、経路接続は点火段で。</summary>
    public static double ReachInterim(double km) => !(km >= 0) ? 0 : 1.0 / (1.0 + km / 4000.0);
}
