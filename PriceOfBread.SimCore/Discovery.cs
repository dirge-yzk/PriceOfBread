namespace PriceOfBread.SimCore;

/// <summary>発見の分野（作者裁定 2026-09-28 ⑤＝制度・思想・製法・発明。発明は新兵器と新製品の2つの規則）。
/// ⚠この回（段2A の骨組み・2026-10-01）は製法と発明（新兵器・新製品）の行だけ＝制度（特許・株式会社）と思想は別の回（設計 §1A-6）。</summary>
public enum DiscoveryField { Institution, Idea, Process, Weapon, Product }

/// <summary>新兵器の軍事の圧力の分野（作者裁定 2026-09-28 ⑤＝陸・海・空で分ける）。</summary>
public enum WarDomain { None, Land, Sea, Air }

/// <summary>発見の表の1行（<see cref="Catalog.Discoveries"/>）＝名前・財・レシピ（""＝既定のレシピ）・分野・年代（10年刻み）・新兵器の分野。</summary>
public readonly record struct DiscoveryRow(string Name, int Good, string Recipe, DiscoveryField Field, int Decade, WarDomain Domain = WarDomain.None);

/// <summary>発見＝解禁の【唯一の形】（★命名の作者裁定2026-08-31＝「研究」はプレイヤーが方向を選ぶ動詞を連想させる・この機構は選べず社会から創発して見つかる＝「発見」）。
/// ★段2A（2026-10-01・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A-2）：**発見は2値**・国ごと・行（レシピ）ごとに毎四半期
/// <c>点数 ＝ 技術力（0〜70）＋ 必要性（0〜70）＋ 伝わり（0〜70）</c> を求め、**一度でも100に届いたら解禁**（以後失われない）。
/// 旧＝技術水準の時計の傾斜（onset〜onset+1 で 0→1 の連続の門・財の門×レシピの門の積）＝全ての国が技術水準だけで同じ順に同じ速さで開いた。
/// ・どの項の最大も門の100未満＝**1つだけでは届かない**／どの2つの最大の和も100以上＝**2つ揃えば届く**（作者の意図＝設計 §1A-2 の性質）。
/// ・**必要性が0の国は自力では発見しない**（技術力70＋伝わり0＝70）＝作者 2026-09-30「中国では人と生糸が余っているので、紡績機は導入されない、という形にしたい。なので、それでよい」
///   「労働を代替する発明は、安い人件費の国では必要とされない、という認識でよい」。**世界で最初の発見は必ず技術力＋必要性**（誰も持っていなければ伝わりは0）。
/// ・状態＝国×行の「解禁済み」の2値（<see cref="World.Discovered"/>）だけ＝設計思想⑩の「本当に必要な場合」（作者裁定 2026-09-28 で許可）。項はどれも今の盤面から導く。
/// ⚠時期感は**較正の目標**であってコードに書かない（暦年ゲートの裏口を作らない）＝行の年代はデータ・難易度は1本の Y で技術水準へ写す（<see cref="Difficulty"/>）。
/// ⚠制度（特許・株式会社）の傾斜 <see cref="Unlock"/> はこの回の外（設計 §1A-6＝別の回）。
/// ★場所に結びついた発見（大鉱床・植林＝<see cref="Catalog.Sites"/>・2026-10-01 (148)）も同じ関数で判定する（<see cref="SiteTerms"/>＝2項・状態は生産者の <see cref="Producer.SiteFound"/>）。</summary>
public static class Discovery
{
    /// <summary>制度の解禁度（ramp・0..1）＝特許・株式会社だけが読む（設計 §1A-6 の別の回で2値の点数へ移す）。⚠span≤0 は設定ミス＝常に0。</summary>
    public static double Unlock(double clock, double aptitude, double onset, double span)
        => span > 0 ? PoliticalQuantities.Clamp01((clock * aptitude - onset) / span) : 0.0;

    /// <summary>各項の最大（技術力・必要性・伝わり）と門（作者裁定 2026-09-28 §1A-2「比率は仮」）。⚠較正で動かすときも「項の最大＜門≦項の最大×2」を保つ（番人）。</summary>
    public const double TermMax = 70.0, GateScore = 100.0;

    /// <summary>行の難易度（技術水準の単位）＝ Y ×（行の年代 − 1780）÷10（作者裁定 Q11＝10年刻みの年代を1本の Y で写す・全体の早い遅いは Y で直す）。
    /// Y の物差し（Q11）＝「各行の年代に、最も必要に迫られた平均的な先進国が届く値」＝英仏普の技術水準の平均（r144 で10年あたり約0.25）が
    /// 必要性 約45 と合わせて100に届く＝ 0.25÷(55÷70)≈0.32〜0.35。1780年代の行（1783年の開幕で既知）は0＝技術力の項は最大（技術の押しの分だけ）。</summary>
    public static double Difficulty(int row, Coefficients c) => DifficultyOf(Catalog.Discoveries[row].Decade, c);
    /// <summary>年代の難易度（発見の表と産地の発見の表 <see cref="Catalog.Sites"/> が共有する1本の Y）。</summary>
    public static double DifficultyOf(int decade, Coefficients c)
        => c.DiscoveryTechPerDecade * (decade - Catalog.DiscoveryBaseDecade) / 10.0;

    /// <summary>人件費＝働き手（農民と工場労働者）の1人の月の所得÷物価水準（労働を代替する発明の必要性の入力・作者 2026-09-30「労働を代替する発明は、安い人件費の国では必要とされない」）。
    /// ⚠労働の財の値段（1単位あたり）ではなく**1人あたり**＝機械は人を置き換える（労働の値段は工業化の生産性で割られて国の間でほとんど違わない＝1種の試走で英 1.30・清 1.16）。
    /// 世界の平均（働き手の人数で重み付け）と比べる＝<see cref="MultiNationSim"/> が求めて渡す（単独の世界は自国＝比1）。</summary>
    private static readonly int[] LaborClasses = [PopClass.Peasant, PopClass.Worker];
    public static (double Wage, double Heads) LaborWage(World w)
    {
        double e = 0, z = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (int cls in LaborClasses)
            {
                var p = r.PopOf(cls);
                if (p is null || !(p.Size > 0) || !(p.Earned >= 0)) continue;
                e += p.Earned; z += p.Size;
            }
        }
        double pl = w.NominalOf(1.0);
        double wage = z > 1e-12 && pl > 0 ? e / z / pl : 0;
        return (!(wage >= 0) ? 0 : wage, z);
    }

    public static bool Known(bool[]? known, int row) => row >= 0 && known is not null && row < known.Length && known[row];

    /// <summary>ある州のある生産者（レシピ）の門∈{0,1}＝能力・未発明の判定・操業規模0・着工の可否・生産タブの灰色化が全てこの1本を読む
    /// （画面とエンジンで「作れるか」の定義が食い違わない）。表に無いレシピ（地理差）は常に1・退場のレシピは後継を発見したら0。
    /// ★産地の発見（(148)・<see cref="Catalog.Sites"/>）の生産者は、見つかるまで0（<see cref="Producer.SiteFound"/>＝国の解禁の配列ではなく生産者の2値＝州と一緒に移る）。
    /// 旧＝鉱床の後期の産地だけ技術水準の傾斜（Catalog.DepositTechScale）。</summary>
    public static double Gate(Region r, Producer p) => Gate(r, p, r.Discovered);
    /// <summary><see cref="Gate(Region, Producer)"/> を与えた解禁の配列で読む（需要の門＝世界の解禁の和で読む・<see cref="GoodGates"/>）。</summary>
    public static double Gate(Region r, Producer p, bool[]? known)
    {
        if (p.SiteRow >= 0 && !p.SiteFound) return 0.0;
        int row = p.DiscoveryRow;
        double g = row >= 0 ? (Known(known, row) ? 1.0 : 0.0) : 1.0;
        int ret = p.RetiredByRow;
        return ret >= 0 && Known(known, ret) ? 0.0 : g;
    }

    /// <summary>財の解禁∈[0,1]＝その州の生産者（レシピ）の門の最大（設計 §1-2＝財の解禁はレシピの解禁の最大）・生産者が居ない財（賦存の無い RGO 等）は既定のレシピの行で読む
    /// （「産地が無い」を「作れない」と取り違えない）。需要（EconomyStep の消費）・理想の軍隊の装備（作者裁定 2026-09-08「機関銃陣地を発見するまで、理想の軍隊に機関銃を入れない」）・
    /// 汽船の船腹・缶詰の糧食が読む＝需要＝財の解禁（設計 §1-2）。否定形＝NaN は「まだ発見していない」側へ倒す。</summary>
    public static double GoodGate(Region r, int gid)
    {
        double best = -1;
        foreach (var pr in r.Producers)
            if (pr.GoodId == gid && !Unfound(pr)) { double g = Gate(r, pr); if (g > best) best = g; }
        if (best < 0) { int row = Catalog.DiscoveryRowOf(gid, ""); best = row < 0 ? 1.0 : Known(r.Discovered, row) ? 1.0 : 0.0; }
        return !(best > 0) ? 0.0 : System.Math.Min(1.0, best);
    }

    /// <summary><see cref="GoodGate"/> を全ての財について1回の走査で（州の生産者を1周）＝与えた解禁の配列で読む（国の解禁＝<see cref="Region.Discovered"/>・
    /// 需要は世界の解禁の和＝<see cref="World.WorldDiscovered"/>）。値は財ごとの <see cref="GoodGate"/> と同じ（国の解禁のとき）。</summary>
    public static void GoodGates(Region r, double[] into, int n, bool[]? known)
    {
        for (int g = 0; g < n; g++) into[g] = -1;
        foreach (var pr in r.Producers)
        {
            int g = pr.GoodId;
            if (g < 0 || g >= n || Unfound(pr)) continue;
            double x = Gate(r, pr, known);
            if (x > into[g]) into[g] = x;
        }
        for (int g = 0; g < n; g++)
        {
            double b = into[g];
            if (b < 0) { int row = Catalog.DiscoveryRowOf(g, ""); b = row < 0 ? 1.0 : Known(known, row) ? 1.0 : 0.0; }
            into[g] = !(b > 0) ? 0.0 : System.Math.Min(1.0, b);
        }
    }

    /// <summary>まだ見つかっていない産地の生産者（財の解禁の最大から外す＝産地が無いのと同じ扱い＝「その場所がまだ知られていない」を「財がまだ発見されていない」と取り違えない）。</summary>
    private static bool Unfound(Producer p) => p.SiteRow >= 0 && !p.SiteFound;

    /// <summary>世界の解禁の和（どこかの国が解禁した行＝輸入で手に入る）を <paramref name="into"/> へ書く（配列は差し替えない＝国と州が同じ配列を参照し続ける）。</summary>
    public static void UnionInto(IEnumerable<World> worlds, bool[] into)
    {
        System.Array.Clear(into);
        foreach (var w in worlds)
        {
            var k = w.Discovered;
            for (int r = 0; r < into.Length && r < k.Length; r++) if (k[r]) into[r] = true;
        }
    }

    /// <summary>国の外から見た値（<see cref="MultiNationSim"/> が四半期末に求めて渡す・単独の世界は null）＝行ごとの接触・人件費の世界の平均（働き手1人の月の所得÷物価水準）。</summary>
    public sealed record WorldView(double[]? Contact, double Wage);

    /// <summary>点数の3項（技術力・必要性・伝わり）。</summary>
    public readonly record struct Terms(double Tech, double Need, double Transmit)
    {
        public double Score => Tech + Need + Transmit;
    }

    /// <summary>伝わりの運び手の財（行ごと）＝その行の財（取引できる財のとき＝設計 §1A-7「財そのものの輸入で学ぶ」）と、そのレシピの投入のうち取引できる資本財（Tier＝Producer）
    /// ＝製法を体に持つ設備（英国製の機関車で大陸の鉄道が始まった・蒸気機関の動力は蒸気機関の財・発電は機械）。
    /// ⚠新しい財（発明）は発見するまで需要も理想の軍隊の装備も立たない（需要＝財の解禁・作者裁定 2026-09-08）＝財そのものの輸入は発見の後にしか起きない
    ///   ＝発見の前に伝わる道は投入の資本財（機械・機関・火砲）の輸入と技師の移動だけ（1種の試走で、資本財を運び手から外すと英が機関銃・装甲艦を1950年まで持たなかった）。
    /// 行の代表の生産者（どこかの国の州にある同じ財・同じレシピの最初の1件）の投入から作る＝全ての国で同じレシピの定義。</summary>
    public static int[][] CarriersByRow(IEnumerable<World> worlds)
    {
        int nr = Catalog.Discoveries.Length;
        var rep = new Producer?[nr];
        foreach (var w in worlds)
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int row = p.DiscoveryRow;
                    if (row >= 0 && rep[row] is null) rep[row] = p;
                }
        var res = new int[nr][];
        var tmp = new List<int>();
        for (int row = 0; row < nr; row++)
        {
            tmp.Clear();
            int g0 = Catalog.Discoveries[row].Good;
            if (Carries(g0)) tmp.Add(g0);
            if (rep[row] is { } p)
                foreach (var inp in p.Inputs)
                    if (Carries(inp.GoodId) && Catalog.Goods[inp.GoodId].Tier == GoodTier.Producer && !tmp.Contains(inp.GoodId)) tmp.Add(inp.GoodId);
            res[row] = [.. tmp];
        }
        return res;
        static bool Carries(int g) => g >= 0 && g < Catalog.Goods.Length && Catalog.Goods[g].Tier != GoodTier.Service && !Catalog.Goods[g].NonTradeable;
    }

    /// <summary>運び手の財 → その財を運び手に持つ行（清算の成約1件で引く逆引き）。</summary>
    public static int[][] RowsByCarrier(int[][] carriers)
    {
        int ng = Catalog.Goods.Length;
        var lists = new List<int>?[ng];
        for (int row = 0; row < carriers.Length; row++)
            foreach (int g in carriers[row]) (lists[g] ??= new List<int>()).Add(row);
        var res = new int[ng][];
        for (int g = 0; g < ng; g++) res[g] = lists[g] is { } l ? [.. l] : [];
        return res;
    }

    /// <summary>国の点数の3項を全ての行について求める（読むだけ＝盤面を変えない）。AI とプレイヤーは同じ関数（発見の判定・画面・計器が同じ値を読む）。
    /// <paramref name="contact"/>＝行ごとの接触（<see cref="MultiNationSim"/> が四半期の取引と技師の移動から求める・単独の世界は null＝伝わり0）。
    /// 解禁済みの行と、この国に代表の生産者が居ない行（その国では使えないレシピ）は 0。</summary>
    public static Terms[] Evaluate(World w, WorldView? view, Coefficients c, bool includeKnown = false)
    {
        double[]? contact = view?.Contact;
        double worldWage = view?.Wage ?? 0;
        var rows = Catalog.Discoveries;
        int nr = rows.Length, ng = Catalog.Goods.Length;
        var res = new Terms[nr];
        // --- 国の集計（財ごと・稼働中の州の和）：国内の産出・輸入・需要・需要で重みを付けた値段・作れるか（門の開いた生産者が居る）---
        var refP = Catalog.RefPrices;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng]; var pxs = new double[ng];   // imp＝州の輸入−輸出の和＝国の純輸入（同じ国の州どうしの積み出しは打ち消す）
        var canMake = new bool[ng];
        var stdRef = new double[ng];   // 財の既定のレシピ（RecipeName＝空）の投入を参照価格で積んだ費用＝割高さの参照の側（分子の「投入の費用」と同じ規則＝工場の自己費用 FactoryVa を含まない）
        var rep = new Producer?[nr];
        int nAct = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            nAct++;
            int mn = System.Math.Min(r.Markets.Length, ng);
            for (int g = 0; g < mn; g++)
            {
                var m = r.Markets[g];
                prod[g] += m.LastProduced; imp[g] += m.LastImport - m.LastExport; dem[g] += m.LastDemand;
                pxd[g] += m.Price * m.LastDemand; pxs[g] += m.Price;
            }
            foreach (var p in r.Producers)
            {
                int row = p.DiscoveryRow;
                if (row >= 0 && rep[row] is null) rep[row] = p;
                if (p.GoodId >= 0 && p.GoodId < ng && !canMake[p.GoodId] && Gate(r, p) > 0) canMake[p.GoodId] = true;
                if (p.RecipeName.Length == 0 && p.GoodId >= 0 && p.GoodId < ng && !(stdRef[p.GoodId] > 0))
                {
                    double sr = 0;
                    foreach (var inp in p.Inputs) if (inp.GoodId >= 0 && inp.GoodId < ng) sr += inp.QtyPerOutput * refP[inp.GoodId];
                    stdRef[p.GoodId] = sr;
                }
            }
        }
        if (nAct == 0) return res;
        // 値段＝需要で重みを付けた州の値段。⚠死んだ市場（国内に需要か供給が無い財）の値段は凍った値（需要の無い財は床・供給の無い財は天井へ張り付く）
        //   ＝割高さの物差しにならない（1種の試走で、蒸気機関の財が売れない国ほど「蒸気機関が安い」＝人件費が割高に見えた）
        //   ＝死んだ市場は参照価格×物価水準（その財が普通に出回ったときの値段）で読む。
        var price = new double[ng];
        for (int g = 0; g < ng; g++)
        {
            bool alive = dem[g] > 1e-9 && prod[g] + (imp[g] > 0 ? imp[g] : 0) > 1e-9;
            price[g] = alive ? pxd[g] / dem[g] : w.NominalOf(refP[g]);
        }
        // 今の方法の費用（財ごと）＝国の門の開いたレシピのうち、投入の値段（国の値段）の和が最も安いもの（製法の必要性の「置き換える方法」）。
        var oldCost = new double[ng];
        for (int g = 0; g < ng; g++) oldCost[g] = double.PositiveInfinity;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                int g0 = p.GoodId;
                if (g0 < 0 || g0 >= ng || !(Gate(r, p) > 0)) continue;
                double cst = 0;
                foreach (var inp in p.Inputs) if (inp.GoodId >= 0 && inp.GoodId < ng) cst += inp.QtyPerOutput * price[inp.GoodId];
                if (cst < oldCost[g0]) oldCost[g0] = cst;
            }
        }
        double gdpNom = w.NominalGdp;
        // 軍事の圧力（新兵器）＝その分野の脅威（相手の強さ）が自国の強さとの和に占める割合∈[0,1)＝理想の軍隊の形（ArmyShape）の陸と海の必要量 ÷（必要量＋自国の陸軍力・海軍力）。空は陸の膠着（設計 §1A-4）。
        //   ⚠比（相手÷自国）のままでは弱い国が数十倍になり必要性が70へ張り付く（1種の試走でダホメが1831年に機関銃・コンゴが1837年に弩級戦艦）＝割合にすれば最大は 70÷(1＋半分点)。
        double landPress = 0, seaPress = 0;
        if (w.SelfPower is { } sp)
        {
            landPress = Share(sp.NeedLand, sp.LandPower);
            seaPress = Share(sp.NeedSea, sp.NavalPower);
        }
        // 満足した消費者の厚み（新製品）＝Σ（人数×満足度）÷人口（作者裁定 Q13＝段や富裕層の述語で分けない連続量）。兵士は数えない。
        double satSum = 0, popSum = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops) { if (p.IsSoldier || !(p.Size > 0)) continue; satSum += p.Size * p.Satisfaction; popSum += p.Size; }
        }
        double satThick = popSum > 1e-12 ? satSum / popSum : 0;
        // 人件費の世界比（労働を代替する発明）＝自国の1人の所得÷世界の平均（単独の世界・世界の平均が無い＝1）。
        double wageRel = 1.0;
        { var (ownWage, _) = LaborWage(w); if (worldWage > 0 && ownWage > 0) wageRel = ownWage / worldWage; }
        double pl = w.NominalOf(1.0);
        for (int row = 0; row < nr; row++)
        {
            if ((!includeKnown && Known(w.Discovered, row)) || rep[row] is not { } rp) continue;   // includeKnown＝計器だけ（解禁済みの行の項も見る）
            var d = rows[row];
            // 技術力＝70×min(1, 技術水準÷難易度)×技術の押し（設計 §1A-3＝投入のうち自国で作っている割合・参照価格で重み付け）
            double diff = Difficulty(row, c);
            double ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = !(ratio > 0) ? 0 : ratio > 1 ? 1 : ratio;
            double wSum = 0, wDom = 0;
            foreach (var inp in rp.Inputs)
            {
                int g = inp.GoodId;
                if (g < 0 || g >= ng) continue;
                double wt = inp.QtyPerOutput * refP[g];
                if (!(wt > 0)) continue;
                double sup = prod[g] + (imp[g] > 0 ? imp[g] : 0);   // 純輸出の国＝自国の分は1
                double dom = sup > 1e-12 ? prod[g] / sup : canMake[g] ? 1.0 : 0.0;   // 市場の無い投入＝作れるなら自国の分（輸入が無い）
                wSum += wt; wDom += wt * dom;
            }
            double push = wSum > 0 ? wDom / wSum : 1.0;
            double tech = TermMax * ratio * push;
            // 必要性＝70×強度÷(強度＋分野の半分点)（設計 §1A-4）
            double s = 0, half = 1;
            switch (d.Field)
            {
                case DiscoveryField.Process:
                {
                    // 製法＝置き換える方法の割高さ×使用量÷GDP。割高さ＝（財の値段÷新しいレシピの投入の値段）が参照価格で同じ比を超えた分
                    // ＝労働を代替する発明（蒸気機関・電動機）では「賃金÷資本と燃料の値段」（作者 2026-09-30＝安い人件費の国では必要とされない・§0-A の 6 と同じ物差し）。
                    int g0 = d.Good;
                    // 置き換える方法が国に無い（その財をまだ作れず・輸入もしていない）＝割高さは測れない＝必要性0（新しい財の最初のレシピは新製品の行が先）。
                    if (!canMake[g0] && !(imp[g0] > 0)) { half = c.DiscoveryNeedHalfProcess; break; }
                    double cLoc = 0, cRef = 0;
                    foreach (var inp in rp.Inputs)
                    {
                        int g = inp.GoodId;
                        if (g < 0 || g >= ng) continue;
                        cLoc += inp.QtyPerOutput * price[g]; cRef += inp.QtyPerOutput * refP[g];
                    }
                    // 労働を代替する発明（その行の財＝労働力）：割高さ＝人件費の世界比 ×（参照価格の資本と燃料 ÷ 自国の資本と燃料の実質の値段）−1
                    //   ＝「賃金÷資本と燃料の値段」が世界の平均の賃金・参照価格の資本と燃料より高い分（作者 2026-09-30・§0-A の 6 と同じ物差し）。
                    // それ以外の製法：割高さ＝（今の方法の投入の費用÷新しいレシピの投入の費用）が参照（財の既定のレシピの参照価格の投入の費用÷新しいレシピの参照価格の投入の費用）を超えた分
                    //   （設計 §1A-4「置き換える投入の割高さ＝州の価格 − 世界の参照価格（RefPrice×物価水準）」・作者裁定 2026-09-28 ④「代替の製法は、置き換える原料に割高な代償を払っている国でこそ見つかる」）。
                    //   ⚠財の市場の値段ではなく今の方法の投入の費用で測る＝発見の直後の新しい財の値段（工場が少なく天井に張り付く）は置き換える原料の割高さではない
                    //     （1種の試走で、ゴムの値段で測ると加硫の発見の直後に合成ゴムが1841〜55年に出た）。⚠参照は既定のレシピの投入の費用（財の参照価格から工場の自己費用を除いた分と同じ）
                    //     ＝分子（投入の費用）と同じ規則で積む（旧＝財の参照価格〔自己費用 FactoryVa 0.06 を含む〕を置き、参照の世界でも割高さが −2.8〜−8.9% に偏った＝(147) の discprobe の 1b＝分子と分母の非対称）。既定のレシピが新しい方法の財
                    //     （転炉鋼・木材パルプ・単離）は、今の方法（坩堝鋼・ぼろ布・樹皮の粉末）が高いほど全ての国で必要になる＝時期は技術力が決める。
                    double prem;
                    if (g0 == Catalog.LaborId) prem = cLoc > 0 && cRef > 0 && pl > 0 ? wageRel * cRef / (cLoc / pl) - 1.0 : 0;
                    else prem = cLoc > 0 && cRef > 0 && stdRef[g0] > 0 && oldCost[g0] < double.PositiveInfinity ? (oldCost[g0] / cLoc) / (stdRef[g0] / cRef) - 1.0 : 0;
                    // ⚠使用量（設計 §1A-4「×使用量÷GDP」）は掛けない＝財の市場の大きさで強度が百倍違い（労働 約0.3・ゴム 約0.001）、1本の半分点では小さな市場の製法
                    //   （合成ゴム・窒素固定・人絹）が最初の発見に届かない（世界で最初の発見は必ず技術力＋必要性）。使っていない財の代わりは要らない＝需要が無ければ0。
                    // 強度＝割高さの対数（値段の比は比で読む＝2倍と3倍の差は10倍と11倍の差より大きい）。⚠割高さそのままでは、置き換える原料が10倍の国（植民地の無い国の生ゴム）で
                    //   必要性が60へ張り付き、技術力40（難易度の57%）で届いた（1種の試走で合成ゴムが1850年）。
                    s = dem[g0] > 1e-9 && prem > 0 ? System.Math.Log(1.0 + prem) : 0;
                    half = c.DiscoveryNeedHalfProcess;
                    break;
                }
                case DiscoveryField.Weapon:
                    s = d.Domain == WarDomain.Sea ? seaPress : landPress;
                    half = c.DiscoveryNeedHalfWeapon;
                    break;
                case DiscoveryField.Product:
                    s = satThick;
                    half = c.DiscoveryNeedHalfProduct;
                    break;
            }
            double need = Saturate(s, half);
            // 伝わり＝70×接触÷(接触＋半分点)（設計 §1A-7＝既に解禁した国との、その財の取引と技師の移動）
            double x = contact is not null && row < contact.Length ? contact[row] : 0;
            double trans = Saturate(x, c.DiscoveryContactHalf);
            res[row] = new Terms(tech, need, trans);
        }
        return res;

        static double Share(double need, double own) => need > 1e-12 ? need / (need + (own > 0 ? own : 0)) : 0;
    }

    /// <summary>項の飽和＝70×強度÷(強度＋半分点)（発見の表と産地の発見が共有）。否定形＝NaN・負は0。</summary>
    private static double Saturate(double s, double half) => s > 0 && half > 0 ? TermMax * s / (s + half) : 0;

    /// <summary>四半期の発見の判定＝点数が100に届いた行を解禁する（2値・失われない）。多国は <see cref="MultiNationSim"/> の四半期末の共有層（接触を求めた直後）・
    /// 単独の世界は <see cref="Simulation"/> の四半期末（接触なし）。開幕の既知の集合は助走の終わりに1度（<see cref="MultiNationSim.StartWarmup"/>）。</summary>
    internal static void Step(World w, WorldView? view, Coefficients c)
    {
        var known = w.Discovered;
        w.Cost[CostLedger.DiscoveryNations]++;   // 計算量の台帳（診断）：発見の判定は国×四半期
        var terms = Evaluate(w, view, c);
        for (int row = 0; row < terms.Length && row < known.Length; row++)
        {
            if (known[row] || !(terms[row].Score >= GateScore)) continue;
            known[row] = true;
            w.LogAdd($"{w.Year} {w.SelfPower?.Name ?? w.Region.Name}が{Catalog.Discoveries[row].Name}を発見");
        }
        // ★産地の発見（(148)・Catalog.Sites）＝この国の州にある、まだ見つかっていない産地（大鉱床・植林）を同じ門で判定する。
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (!Unfound(p) || !(SiteTerms(w, r, p, c).Score >= GateScore)) continue;
                OpenSite(w, r, p, c);
            }
        }
        w.LastDiscoveryView = view;
    }

    /// <summary>★産地の発見の2項（(148)・<see cref="Catalog.Sites"/>）＝読むだけ（判定・画面・計器が同じ関数を読む）。伝わりは持たない（他国の発見は自分の土地の鉱床を見つけない）。
    /// ・技術力＝70×min(1, 国の技術水準÷難易度)（難易度＝年代×Y＝発見の表と同じ1本）。
    /// ・必要性＝70×強度÷(強度＋半分点 <see cref="Coefficients.DiscoveryNeedHalfSite"/>)・強度＝人の厚み N÷(N＋N0)×その財の実質の値段÷参照価格
    ///   （設計 §9-1 の提案③＝適性＝入植者×金の値・2026-09-28 作者が採用した提案＝文は AI の整理）。N＝鉱床は入植者＝州の POP の大きさ×中核民族の割合（CoreEthnicShare・現地住民は0で作られ同化で育つ）・
    ///   植林は住民（樹を植えて樹皮を剥ぐ働き手）。兵士は数えない。N0＝<see cref="Coefficients.ColonyPromotionMinPop"/>（州を名乗れる人の集まり＝(118) と同じ物差し）。
    ///   実質の値段＝国の州の値段を需要で重み付けた値÷物価水準（死んだ市場＝参照価格＝1）＝デフレの世では金の実質の値打ちが上がり探鉱が進む（金本位の下で名目の金の値段は動かない）。</summary>
    public static Terms SiteTerms(World w, Region r, Producer p, Coefficients c)
    {
        var site = Catalog.Sites[p.SiteRow];
        double diff = DifficultyOf(site.Decade, c);
        double ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
        ratio = !(ratio > 0) ? 0 : ratio > 1 ? 1 : ratio;
        double people = 0;
        foreach (var q in r.Pops)
        {
            if (q.IsSoldier || !(q.Size > 0)) continue;
            people += site.Kind == Catalog.SiteKind.Deposit ? q.Size * PoliticalQuantities.Clamp01(q.CoreEthnicShare) : q.Size;
        }
        double n0 = c.ColonyPromotionMinPop;
        double thick = people > 0 && n0 > 0 ? people / (people + n0) : 0;
        double s = thick * RealValue(w, site.Good);
        return new Terms(TermMax * ratio, Saturate(s, c.DiscoveryNeedHalfSite), 0);
    }

    /// <summary>財の実質の値段÷参照価格（国の稼働中の州の値段を需要で重み付け・物価水準で割る）。死んだ市場（国内に需要が無いか、国内に残る供給〔産出＋輸入−輸出の和〕が無い）は1
    /// （参照価格＝普通に出回ったときの値段＝<see cref="Evaluate"/> の死んだ市場と同じ扱い・⚠ 生きた市場の判定は Evaluate〔産出＋純輸入の正の分〕と少し違う＝全て輸出する産地の国は1と読む）。</summary>
    private static double RealValue(World w, int g)
    {
        double pd = 0, dd = 0, sup = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || g >= r.Markets.Length) continue;
            var m = r.Markets[g];
            pd += m.Price * m.LastDemand; dd += m.LastDemand; sup += m.LastProduced + m.LastImport - m.LastExport;
        }
        double pl = w.NominalOf(1.0), rp = g >= 0 && g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
        if (!(dd > 1e-9) || !(sup > 1e-9) || !(pl > 0) || !(rp > 0)) return 1.0;
        double v = pd / dd / pl / rp;
        return !(v > 0) ? 0 : v;   // 否定形＝NaN は「値打ちが無い」側（見つけない）へ倒す
    }

    /// <summary>産地を開く（(148)）＝2値を立てる（失われない・州と一緒に移る）。鉱床は開口＝即産（操業規模≥1＝ラッシュの形・旧シルバーラッシュと同じ）と
    /// **富鉱**（浅い高品位の鉱脈＝<see cref="Coefficients.GoldRushEndowment"/> を開いた財の生産者だけに＝検討 Q13 (c)・旧＝州の全ての鉱物の MineralDepletion）と
    /// 州の鉱脈在庫の補充（<see cref="Coefficients.BonanzaDepositYears"/>×初期在庫÷DepositYearsAtBirth・衰えは採掘による品位だけ）。
    /// 植林は門が開くだけ（樹は植えてから育つ＝操業規模は市場が育てる）。人の殺到は既存の賃金の割増→機会（Territory・越境の移民）から出る＝足さない。</summary>
    internal static void OpenSite(World w, Region r, Producer p, Coefficients c)
    {
        var site = Catalog.Sites[p.SiteRow];
        p.SiteFound = true;
        string gname = Catalog.Goods[site.Good].Name, who = w.SelfPower?.Name ?? w.Region.Name;
        if (site.Kind == Catalog.SiteKind.Deposit)
        {
            Simulation.ApplyEndowment(p, c.GoldRushEndowment, c);
            if (p.Scale < 1.0) p.Scale = 1.0;
            if (r.MineralDepositInitial > 0)
                r.MineralDeposit += c.BonanzaDepositYears * r.MineralDepositInitial / System.Math.Max(1e-9, c.DepositYearsAtBirth);
            w.LogAdd($"{w.Year} {r.Name}で{gname}の大鉱脈が見つかった（{who}）");
        }
        else w.LogAdd($"{w.Year} {r.Name}で{gname}の植林が始まった（{who}）");
    }
}
