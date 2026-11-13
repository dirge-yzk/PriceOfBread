namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1037) c98lw 労働力の重みの表 lwk_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない（蓄えは ConditionalWeakTable＝計器の物）。
// 目的＝事前登録 measurements/m1037_20261111_prereg_lw.txt の門 G0〜G5 を読む＝同じ盤面で旧の規則（女 1・子 ChildWorkFactor）と新の規則（形 A）を両方数え、エンジンが使った work と並べる。
// 行＝年末の四半期（q%4==3）・全ての国の全ての州の、労働の財（労働力・農業労働力・高度労働力）を売る POP ごとに 1 行（その四半期の最後の月の断面）。
// 計器が自分で数える物：州の動力の割合 m（労働力を投入する生産者を自分で選び、産出×労働の実効の投入量×動力の割合÷州の労働力の前の月の総需要）・POP の役割（売る労働の財）・
//   旧と新の work とその項・最初に見た月の旧と新の work（捕捉の影）。
// 列（単位は接尾辞：Share＝0〜1・Index＝人数あたりの無次元の量）：
//   role（1＝労働力・2＝農業労働力・4＝高度労働力の和）・mShare（計器の州の m）・m0Share（捕捉の月の m）・partShare／part0Share（女の参加率・今と捕捉の月）・clrShare／clr0Share（児童労働の割合）・
//   wFemIndex／wChildIndex（計器が新の規則で置いた女と子の重み・高度労働力だけを売る POP は旧の 1 と ChildWorkFactor）・menIndex（男の項）・femOldIndex／femNewIndex・childOldIndex／childNewIndex・
//   workOldIndex／workNewIndex（計器の旧と新の work）・workEngineIndex（エンジンが使った work・民兵の前）・initialWorkEngineIndex（Pop.InitialWork）・
//   initialWorkOldShadowIndex／initialWorkNewShadowIndex（計器が最初に見た月の旧と新の work）・militiaShare。
// ⚠限界（計器が自分で申告する）：①女の参加率 part はエンジンから受け取る（LaborSupply の式は試す式ではない）②Habit.LaborFactor は計器も同じ関数を呼ぶ（入力）③断面は四半期の最後の月
//   ④捕捉の影は計器が最初に見た月＝エンジンの捕捉と同じ月（hook がその直後に呼ばれる）・計器を途中で入れた世界では食い違う⑤m の分子の Output は前の月の値（エンジンと同じ時点で数える＝hook は供給のループの前）。
public static partial class Instruments
{
    internal const string LwkHeaderText = "quarter\tyear\tnation\tregion\tpopIndex\tclassName\trole\tmShare\tm0Share\tpartShare\tpart0Share\tclrShare\tclr0Share"
        + "\twFemIndex\twChildIndex\tmenIndex\tfemOldIndex\tfemNewIndex\tchildOldIndex\tchildNewIndex\tworkOldIndex\tworkNewIndex\tworkEngineIndex"
        + "\tinitialWorkEngineIndex\tinitialWorkOldShadowIndex\tinitialWorkNewShadowIndex\tmilitiaShare";

    private sealed class LwkReg { public double M; public byte[] Role = []; public Coefficients? Coef; }
    private sealed class LwkRec
    {
        public bool HasInit, Fresh;
        public double InitOld, InitNew, M0, Part0, Clr0;
        public int PopIndex, Role;
        public double M, Part, Clr, WF, WC, Men, FemOld, FemNew, ChildOld, ChildNew, WorkOld, WorkNew, WorkEng, InitEng, Militia;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, LwkReg> LwkRegs = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Pop, LwkRec> LwkRecs = new();

    /// <summary>hook A（EconomyStep の供給のループの前・毎月）：計器が自分で州の m と POP の役割を数える。</summary>
    internal static void LwkRegion(Region r, Coefficients c)
    {
        var reg = LwkRegs.GetOrCreateValue(r);
        reg.Coef = c;
        int n = r.Pops.Count;
        if (reg.Role.Length < n) reg.Role = new byte[n];
        for (int i = 0; i < reg.Role.Length; i++) reg.Role[i] = 0;
        double num = 0.0;
        foreach (var s in r.Producers)
        {
            int lp = s.LaborPopIndex;
            if (lp >= 0 && lp < n)
            {
                if (s.GoodId == Catalog.LaborId) reg.Role[lp] |= 1;
                if (s.GoodId == Catalog.FarmLaborId) reg.Role[lp] |= 2;
                if (s.GoodId == Catalog.AdvLaborId) reg.Role[lp] |= 4;
            }
            if (!(s.Output > 0)) continue;
            double power = s.ElecFill + (1.0 - s.ElecFill) * s.SteamFill;
            foreach (var inp in s.Inputs)
            {
                if (inp.GoodId != Catalog.LaborId) continue;
                double qty = inp.QtyPerOutput;
                if (s.BuysLabor) qty = qty * (1.0 - c.ElecLaborSave * s.ElecFill) * (1.0 - c.LaborSaveRate * r.Mechanization);
                num += s.Output * qty * power;
            }
        }
        double den = Catalog.LaborId < r.Markets.Length ? r.Markets[Catalog.LaborId].LastDemand : 0.0;
        double m = den > 0 ? num / den : 0.0;
        reg.M = m >= 0 ? m : 0.0;
    }

    /// <summary>hook B（EconomyStep の労働の塊・InitialWork の捕捉の直後・民兵の前）：エンジンが使った <paramref name="workEngine"/> と参加率 <paramref name="part"/> を受け取り、旧と新の式は計器が書く。</summary>
    internal static void LwkWork(Region r, int lp, double part, double workEngine)
    {
        if (!LwkRegs.TryGetValue(r, out var reg) || reg.Coef is not { } c || lp < 0 || lp >= r.Pops.Count || lp >= reg.Role.Length) return;
        var p = r.Pops[lp];
        var rec = LwkRecs.GetOrCreateValue(p);
        int role = reg.Role[lp];
        double h = Habit.LaborFactor(c, r);
        double old = c.OldLaborFactor;
        double men = (p.AgeShare(15, 65, 0) + old * p.AgeShare(65, double.PositiveInfinity, 0)) * h;
        double femBase = (p.AgeShare(15, 65, 1) * part + old * p.AgeShare(65, double.PositiveInfinity, 1) * part) * h;
        double kids = p.AgeShare(10, 15), clr = p.ChildLaborRate.Value;
        double wf, wc;
        if ((role & 1) != 0) { wf = c.FemaleHandLaborWeight + (1.0 - c.FemaleHandLaborWeight) * reg.M; wc = c.ChildHandLaborWeight + (1.0 - c.ChildHandLaborWeight) * reg.M; }
        else if ((role & 2) != 0) { wf = c.FemaleHandLaborWeight; wc = c.ChildHandLaborWeight; }
        else { wf = 1.0; wc = c.ChildWorkFactor; }
        double femOld = femBase, femNew = femBase * wf, chOld = kids * c.ChildWorkFactor * clr, chNew = kids * wc * clr;
        double wOld = men + femOld + chOld, wNew = men + femNew + chNew;
        if (!rec.HasInit) { rec.HasInit = true; rec.InitOld = wOld; rec.InitNew = wNew; rec.M0 = reg.M; rec.Part0 = part; rec.Clr0 = clr; }
        rec.PopIndex = lp; rec.Role = role; rec.M = reg.M; rec.Part = part; rec.Clr = clr; rec.WF = wf; rec.WC = wc;
        rec.Men = men; rec.FemOld = femOld; rec.FemNew = femNew; rec.ChildOld = chOld; rec.ChildNew = chNew; rec.WorkOld = wOld; rec.WorkNew = wNew;
        rec.WorkEng = workEngine; rec.InitEng = p.InitialWork; rec.Militia = p.MilitiaShare.Value; rec.Fresh = true;
    }

    /// <summary>計器の子の重み（pop_s の影が読む）：この POP を労働の hook で見ていれば計器の w_子、見ていなければ ChildWorkFactor。</summary>
    internal static double LwkChildWeight(Pop p, Coefficients c) => LwkRecs.TryGetValue(p, out var rec) && rec.HasInit ? rec.WC : c.ChildWorkFactor;

    static void LwkFold(Sink sk, ulong seed, World w, int q, bool yearEnd)
    {
        if (!yearEnd) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
            {
                if (!LwkRecs.TryGetValue(p, out var x) || !x.Fresh) continue;
                x.Fresh = false;
                sk.Lwk ??= Open("lwk", seed);
                if (!sk.LwkHeader) { sk.Lwk.WriteLine(LwkHeaderText); sk.LwkHeader = true; }
                sk.Lwk.WriteLine(string.Join('\t', I(q), I(w.Year), nation, r.Name, I(x.PopIndex), p.ClassName, I(x.Role), F(x.M), F(x.M0), F(x.Part), F(x.Part0), F(x.Clr), F(x.Clr0),
                    F(x.WF), F(x.WC), F(x.Men), F(x.FemOld), F(x.FemNew), F(x.ChildOld), F(x.ChildNew), F(x.WorkOld), F(x.WorkNew), F(x.WorkEng),
                    F(x.InitEng), F(x.InitOld), F(x.InitNew), F(x.Militia)));
            }
    }
}
