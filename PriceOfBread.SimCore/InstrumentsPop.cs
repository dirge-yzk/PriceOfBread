namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (361) 人口の作り替え 段A 計器＋段B 影の器（読むだけ・設計 docs/202610/20261015/20261015_人口の作り替え_出生と死亡_設計.md §2-4・§3-2） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず1本のファイルで持つ（衝突面を消す）。力学の値を1つも書かない・乱数を引かない・メモ化された量を新しい位置で読まない。
// 新しいシミュの状態を足さない（Pop／World／Region に欄なし）＝蓄えは全部 WState の PopAcc。新しい数（φ・m_max・θ）は Coefficients でなくここの const（〈AI の推定・読み手なし〉＝段C で表へ昇格させる）。

public static partial class Instruments
{
    // ---- 影の定数（全て【推】＝設計 doc §2-4(e)・出典の頁は見ていない概数。計器の列の定義であって力学の係数ではない）----
    /// <summary>θ＝子の籠÷大人の籠（【推】0.5 前後）。</summary>
    internal const double PopTheta = 0.5;
    /// <summary>m_max＝皆婚の上限＝15〜49 歳の女のうち結婚している割合（【推】0.85 前後）。</summary>
    internal const double PopMMax = 0.85;
    /// <summary>自然出生の夫婦の合計出生率（【推】7〜8＝Henry 1961／Coale-Trussell の中央 7.5）。</summary>
    internal const double PopTfrNatural = 7.5;
    /// <summary>出産年齢の幅（15〜49 歳＝35 年）。合計出生率の代理（出生÷15〜49 歳の女の人年×35）と φ＝TfrNatural÷35（女1人1年あたり）が使う。</summary>
    internal const double PopFertileYears = 35.0;
    /// <summary>子の費用の年数（設計 doc §2-4(c)）：籠 15 年・教育 10 年（5〜14 歳）・稼ぎ 5 年（10〜14 歳）。</summary>
    internal const double PopChildBasketYears = 15.0, PopChildEduYears = 10.0, PopChildWorkYears = 5.0;

    /// <summary>国×窓の蓄え（DemographicStep の POP のループの hook が積む・行を書いたら0へ戻す）。層は 10 歳刻み 8 本（男女計）。</summary>
    internal sealed class PopAcc
    {
        public int Months, RecoveringMonths, NoMotherMonths;
        public double PopMonths, Births, DeathsNormal, DeathsFamine, FertileWomenMonths;
        public readonly double[] BandDeaths = new double[Pop.AgeBands], BandExposure = new double[Pop.AgeBands];
        public double Diag0Births, Diag0Deaths, Diag0War;   // 前の行を書いた時の World.DiagBirths／DiagDeaths／WarDeadCumulative（差分で取る）
        public void Clear(World w)
        {
            Months = RecoveringMonths = NoMotherMonths = 0; PopMonths = Births = DeathsNormal = DeathsFamine = FertileWomenMonths = 0;
            Array.Clear(BandDeaths); Array.Clear(BandExposure);
            Diag0Births = w.DiagBirths; Diag0Deaths = w.DiagDeaths; Diag0War = w.WarDeadCumulative;
        }
    }
    static PopAcc PAcc(World w) { var st = S(w); return st.PopA ??= new PopAcc(); }
    /// <summary>番人（テスト）が蓄えを読むための口。</summary>
    internal static PopAcc PopAccOf(World w) => PAcc(w);

    // ---------------------------------------------------------------- 本体の位置から呼ばれる hook（Instruments.On の中だけ） ----------------------------------------------------------------

    /// <summary>国×月に1回（<c>DemographicStep</c> の出生と死の総数が決まった所）：月の数・回復期（net ≧ 出生＝平常の死 0）の月・産む人が居ない月。</summary>
    internal static void DemoMonth(World w, bool recovering, bool noMothers)
    {
        var a = PAcc(w); a.Months++; if (recovering) a.RecoveringMonths++; if (noMothers) a.NoMotherMonths++;
    }

    /// <summary>兵士以外の POP ごとに1回（<c>StepAges</c> の直前＝年齢の構成が月初のまま）：月初の規模 <paramref name="size0"/>・出生・実際に減った平常の死 <paramref name="deaths"/>。
    /// 層ごとの平常の死は <c>StepAges</c> と同じ関数（<see cref="Pop.TakeWeighted"/>）を同じ入力で呼び直して取る（複製でなく同じ本体）。人年は層ごとの月初の人数（人×月）。</summary>
    internal static void DemoPop(World w, Pop p, double size0, double births, double deaths, double[] mortW)
    {
        var a = PAcc(w);
        Span<double> n0 = stackalloc double[2 * Pop.AgeBands];
        for (int k = 0; k < n0.Length; k++) n0[k] = p.Ages[k] * size0;
        a.PopMonths += size0; a.Births += births; a.DeathsNormal += deaths; a.FertileWomenMonths += size0 * p.AgeShare(15, 50, 1);
        for (int b = 0; b < Pop.AgeBands; b++) a.BandExposure[b] += n0[2 * b] + n0[2 * b + 1];
        if (deaths > 0)
        {
            Span<double> tmp = stackalloc double[2 * Pop.AgeBands];
            Span<double> took = stackalloc double[2 * Pop.AgeBands];
            n0.CopyTo(tmp);
            Pop.TakeWeighted(tmp, took, mortW, deaths);
            for (int b = 0; b < Pop.AgeBands; b++) a.BandDeaths[b] += took[2 * b] + took[2 * b + 1];
        }
    }

    /// <summary>飢えの超過死亡の1回（<c>KillAges</c> の直前＝年齢の構成は <c>StepAges</c> の後）：<paramref name="d"/> 人を災害死の年齢の重み <paramref name="disasterW"/> で層から取る（<c>KillAges</c> と同じ関数・同じ入力）。</summary>
    internal static void DemoFamine(World w, Pop p, double d, ReadOnlySpan<double> disasterW)
    {
        var a = PAcc(w);
        Span<double> n = stackalloc double[2 * Pop.AgeBands];
        Span<double> took = stackalloc double[2 * Pop.AgeBands];
        for (int k = 0; k < n.Length; k++) n[k] = p.Ages[k] * p.Size;
        Pop.TakeWeighted(n, took, disasterW, d);
        a.DeathsFamine += d;
        for (int b = 0; b < Pop.AgeBands; b++) a.BandDeaths[b] += took[2 * b] + took[2 * b + 1];
    }

    // ---------------------------------------------------------------- 影の式（段B・純関数＝番人が手の算術で突き合わせる） ----------------------------------------------------------------

    /// <summary>子の価値の影の入力（階級ごと）。金額は全て〈規模1単位あたり・月あたり〉（規模の単位は千人＝比だけが意味を持つ）。</summary>
    internal readonly record struct PopShadowIn(
        double YAdult,        // 大人（15〜64 歳）1人あたりの月の稼ぎ（前の月の確定した所得÷大人の数・税の前）
        double Basket,        // 籠（主食＋日用品の供給後の費用）の1人あたり月額
        double ChildWork,     // ChildWorkFactor（児童の半人前係数）
        double ChildLaborRate,// 10〜14 歳の児童労働の割合（人数で重みづけ）
        double EduPerChild,   // 学齢の子1人あたり月の教育の必要量（EduPerChild）
        double EduPrice,      // 教育の値段
        double NEdu,          // 教育の必需の割合の代理（就学の充足率・学齢の人数で重みづけ）
        double PublicShare,   // 義務教育の国庫の肩代わりの割合（財源率）
        double S10,           // 10 歳まで生き残る確率
        double L60,           // 60 歳以後の余命（年）
        double Ns,            // 生き残る子の見込み（0〜19 歳÷20〜49 歳の女）
        double Pi,            // 年金の覆う割合
        double Knowledge);    // 制限の知識の届き（識字で代用＝発見の行はまだ無い）

    internal readonly record struct PopShadowOut(
        double E, double ChildBasketCost, double EduCostFull, double ChildCost, double OldAge, double DirectUtility1,
        double V, double PsiBreakeven, double G, double CKnown, double A, double MNew, double TfrShadow);

    /// <summary>(c) 子の価値 v（ψ＝0）と (b) の m・c の影。設計 doc §2-4：
    /// E＝稼ぎ×ChildWorkFactor×児童労働の割合×(5年×12)／C＝15年×12×θ×籠＋10年×12×EduPerChild×教育の値段×n×(1−公費)／O＝L60×12×籠×(1−π)÷Ns／U1＝稼ぎ×15年×12（ψ＝1 の分）／
    /// v＝(S10×E＋O＋ψU1−C)÷C（ここは ψ＝0）・ψ*＝v＝0 になる ψ＝(C−S10×E−O)÷U1／g＝1÷(1＋e^−v)／c＝1−K×(1−g)／A＝稼ぎ÷(籠×(1＋θ×Ns))・m＝m_max×A÷(1＋A)（新居型）／合計出生率の影＝7.5×m×c。
    /// 費用が正でない・入力が NaN の行は全て NaN（否定形）。</summary>
    internal static PopShadowOut PopShadow(in PopShadowIn x)
    {
        double e = x.YAdult * x.ChildWork * x.ChildLaborRate * (PopChildWorkYears * 12);
        double cb = PopChildBasketYears * 12 * PopTheta * x.Basket;
        double ce = PopChildEduYears * 12 * x.EduPerChild * x.EduPrice;
        double c = cb + ce * x.NEdu * (1.0 - x.PublicShare);
        double o = x.L60 * 12 * x.Basket * (1.0 - x.Pi) / x.Ns;
        double u1 = x.YAdult * PopChildBasketYears * 12;
        double v = (x.S10 * e + o - c) / c;
        double psi = (c - x.S10 * e - o) / u1;
        double g = 1.0 / (1.0 + Math.Exp(-v));
        double ck = 1.0 - x.Knowledge * (1.0 - g);
        double a = x.YAdult / (x.Basket * (1.0 + PopTheta * x.Ns));
        double m = PopMMax * a / (1.0 + a);
        double tfr = PopTfrNatural * m * ck;
        if (!(c > 0)) { v = psi = g = ck = tfr = double.NaN; }
        return new PopShadowOut(e, cb, ce, c, o, u1, v, psi, g, ck, a, m, tfr);
    }

    /// <summary>層 b（10 歳刻み）の月初の人数に対する年あたりの死の率（平常＋飢え）。人年が 0 は NaN。</summary>
    static double PopMu(PopAcc a, int b) => a.BandExposure[b] > 0 ? a.BandDeaths[b] / a.BandExposure[b] * 12.0 : double.NaN;

    /// <summary>60 歳以後の余命（年）＝層 6（60〜69）と層 7（70 以上）の死の率から閉じた形：(1−e^−10μ6)÷μ6＋e^−10μ6÷μ7。率が正でなければ NaN（平常の死が 0 の回復期の四半期は NaN になる＝計器の限界）。</summary>
    internal static double PopL60(double mu6, double mu7)
    {
        if (!(mu6 > 0) || !(mu7 > 0)) return double.NaN;
        double s = Math.Exp(-10.0 * mu6);
        return (1.0 - s) / mu6 + s / mu7;
    }

    // ---------------------------------------------------------------- 四半期の末（Quarter から・CeFold の前） ----------------------------------------------------------------

    sealed class PopCls
    {
        public double Size, Adult, Pool, FertWomen, Kids, Women2049, School, ClrW, ClrDen, EduFillW, LitW, PriceW, PubW, W0014, W60;
    }

    const string PopHeader = "scope\tquarter\tyear\tnationUid\tnation\tclassId\tclassName\tmonthsCount\t"
        + "popK\tpopMeanK\tshare0to14\tfertileWomenShare\tshare60plus\t"
        + "birthsK\tdeathsNormalK\tdeathsFamineK\tdeathsOtherK\twarDeadK\tcbrPerMille\tcdrPerMille\tcdrNormalPerMille\tdeathsBand0K\tdeathsBand1K\tdeathsBand2K\tdeathsBand3K\tdeathsBand4K\tdeathsBand5K\tdeathsBand6K\tdeathsBand7K\t"
        + "mu0PerYear\tmu1PerYear\tmu2PerYear\tmu3PerYear\tmu4PerYear\tmu5PerYear\tmu6PerYear\tmu7PerYear\ts10Share\tl60Years\ttfrProxy\trecoveringMonths\tnoMotherMonths\t"
        + "adultShare\tyAdultPerMonth\tbasketPerMonth\tchildLaborRateShare\tchildEarningsE\tchildBasketCost\teduCostFull\tnEduFillShare\tnEduLitShare\tpublicEduShare\tchildCostC\toldAgeO\tdirectUtilityU1\t"
        + "pensionPiShare\tsurvivingChildrenNs\tvNoUtilityIndex\tpsiBreakevenIndex\tgOfVShare\tknowledgeShare\tcKnownShare\tmarriedMNewShare\ttfrShadow\tpoolK\tshadowBirthsPerYearK";
    const int PopFlowCols = 29;   // 見出しの birthsK〜noMotherMonths の列の数（C 行は空）

    static void PopFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;   // 書かない四半期は蓄えを持ち越す（挙げていない国は年末に4四半期ぶんの行）
        var a = PAcc(w); var c = w.Coef;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);

        // ---- 四半期の末の断面（兵士以外の POP・稼働していない州も含む＝DemographicStep が回す集合と同じ）
        var cls = new PopCls[NC]; var names = new string?[NC];
        double popK = 0, w0014 = 0, wFert = 0, w60 = 0, oldSz = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            if (p.IsSoldier) continue;
            int k = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            var cl = cls[k] ??= new PopCls(); names[k] ??= p.ClassName;
            double s = p.Size;
            popK += s; w0014 += s * p.AgeShare(0, 15); wFert += s * p.AgeShare(15, 50, 1); double os = s * p.AgeShare(60, double.PositiveInfinity); w60 += os; oldSz += os;
            cl.Size += s; cl.Adult += s * p.AgeShare(15, 65); cl.Pool += s * Simulation.FertilePool(p, c); cl.FertWomen += s * p.AgeShare(15, 50, 1);
            cl.Kids += s * p.AgeShare(0, 20); cl.Women2049 += s * p.AgeShare(20, 50, 1); cl.W0014 += s * p.AgeShare(0, 15); cl.W60 += os;
            double sc = s * p.AgeShare(5, 15); cl.School += sc; cl.EduFillW += sc * p.EducationFill;
            double price = Catalog.EducationId < rg.Markets.Length ? rg.Markets[Catalog.EducationId].Price : double.NaN;
            cl.PriceW += sc * price; cl.PubW += sc * rg.CompulsoryEduFunding;
            double cw = s * p.AgeShare(10, 15); cl.ClrDen += cw; cl.ClrW += cw * p.ChildLaborRate.Value;
            cl.LitW += s * p.Literacy;
        }
        if (!(popK > 0)) { a.Clear(w); return; }   // 死んだ国（人口 0）は行を出さない

        // ---- 国全体の死の率（層別）・10 歳までの生存・60 歳以後の余命
        var mu = new double[Pop.AgeBands]; for (int b = 0; b < mu.Length; b++) mu[b] = PopMu(a, b);
        double s10 = Math.Exp(-10.0 * mu[0]);   // 率が NaN（人年 0）なら NaN
        double l60 = PopL60(mu[6], mu[7]);
        // ---- 籠は ce_s の供給後の籠（階級×国・EntryCtx が助走後の月に毎月積む）を読む。CeFold が戻す前
        var ceC = st.CeC;
        double nBasket = 0, nSize = 0;
        if (ceC is not null) for (int k = 0; k < NC; k++) { nBasket += ceC[k * CeK + CeSWant] + ceC[k * CeK + CeSWant + 1]; nSize += ceC[k * CeK + CeSize]; }
        double basketNation = nSize > 0 ? nBasket / nSize : double.NaN;
        double penPer = oldSz > 0 ? w.LastPensionCostPerMonth.Value / oldSz : 0.0;   // 年金の請求の1人あたり月額（年金なし＝0）
        double pi = penPer / (penPer + basketNation);                              // 籠が NaN なら NaN。年金なし・籠が正なら 0
        double mo = a.Months;
        double perYearScale = mo > 0 ? 12.0 / mo : double.NaN;                        // 窓の合計 → 年あたり

        // ---- 階級ごとの影
        var outs = new PopShadowOut?[NC]; var ins = new PopShadowIn[NC];
        var aux = new double[NC * 5];   // 0 yAdult 1 basket 2 clr 3 nEduFill 4 nLit
        var pub = new double[NC]; var adultSh = new double[NC]; var nsArr = new double[NC]; var litArr = new double[NC];
        for (int k = 0; k < NC; k++)
        {
            var cl = cls[k]; if (cl is null || !(cl.Size > 0)) continue;
            double basket = double.NaN, earnPerCap = double.NaN;
            if (ceC is not null && ceC[k * CeK + CeSize] > 0)
            {
                double cs = ceC[k * CeK + CeSize];
                basket = (ceC[k * CeK + CeSWant] + ceC[k * CeK + CeSWant + 1]) / cs;
                earnPerCap = ceC[k * CeK + CeEarnLastPos] / cs;
            }
            adultSh[k] = cl.Adult / cl.Size;
            double yAdult = adultSh[k] > 0 ? earnPerCap / adultSh[k] : double.NaN;
            double clr = cl.ClrDen > 0 ? cl.ClrW / cl.ClrDen : 0.0;
            double nFill = cl.School > 0 ? cl.EduFillW / cl.School : double.NaN;
            double price = cl.School > 0 ? cl.PriceW / cl.School : double.NaN;
            pub[k] = cl.School > 0 ? cl.PubW / cl.School : double.NaN;
            nsArr[k] = cl.Women2049 > 0 ? cl.Kids / cl.Women2049 : double.NaN;
            litArr[k] = cl.LitW / cl.Size;
            ins[k] = new PopShadowIn(yAdult, basket, c.ChildWorkFactor, clr, c.EduPerChild, price, nFill, pub[k], s10, l60, nsArr[k], pi, litArr[k]);
            outs[k] = PopShadow(ins[k]);
            aux[k * 5] = yAdult; aux[k * 5 + 1] = basket; aux[k * 5 + 2] = clr; aux[k * 5 + 3] = nFill; aux[k * 5 + 4] = litArr[k];
        }

        // ---- 行を書く
        sk.PopS ??= Open("pop", seed);
        if (!sk.PopSHeader) { sk.PopS.WriteLine(PopHeader); sk.PopSHeader = true; }
        int year = w.Year;
        string head(string scope, int cid, string cname) => string.Join('\t', scope, I(q), I(year), I(st.Uid), nation, cid < 0 ? "" : I(cid), cname, I(a.Months));
        string blank(int n) => new string('\t', n);   // n 個の空セルの連結（先頭に区切りを置く）

        // N 行：流れ（窓の合計→年あたり）と、階級を人口で重みづけた影
        double dNormal = a.DeathsNormal + a.DeathsFamine, births = a.Births;
        double dBirthsDiag = w.DiagBirths - a.Diag0Births, dDeathsDiag = w.DiagDeaths - a.Diag0Deaths, dWar = w.WarDeadCumulative - a.Diag0War;
        double other = dDeathsDiag - dNormal;   // 疫病・遠征の病死（KillPops 非暴力）＝台帳の差−平常−飢え
        double pm = a.PopMonths > 0 ? a.PopMonths : double.NaN;
        double cbr = births / pm * 12 * 1000, cdr = (dNormal + other) / pm * 12 * 1000, cdrN = dNormal / pm * 12 * 1000;
        double tfrProxy = a.FertileWomenMonths > 0 ? births / a.FertileWomenMonths * 12 * PopFertileYears : double.NaN;
        // 影の集計：v の項は規模で、m・c は出産のプールで重みづけ（NaN の階級は除く＝除いた分の重みは別に持たない）
        double wV = 0, wPS = 0, E = 0, CB = 0, EF = 0, CC = 0, O = 0, U1 = 0, V = 0, PS = 0, G = 0, YA = 0, BK = 0, CLR = 0, NF = 0, NL = 0, PU = 0, NS = 0, KN = 0, ADS = 0;
        double wM = 0, CK = 0, MN = 0, TS = 0, poolAll = 0, shadowBirths = 0;
        for (int k = 0; k < NC; k++)
        {
            var o = outs[k]; var cl = cls[k]; if (o is null || cl is null) continue;
            var x = o.Value;
            if (!double.IsNaN(x.V))
            {
                double sz = cl.Size; wV += sz;
                E += sz * x.E; CB += sz * x.ChildBasketCost; EF += sz * x.EduCostFull; CC += sz * x.ChildCost; O += sz * x.OldAge; U1 += sz * x.DirectUtility1; V += sz * x.V; G += sz * x.G;
                YA += sz * ins[k].YAdult; BK += sz * ins[k].Basket; CLR += sz * ins[k].ChildLaborRate; NF += sz * ins[k].NEdu; NL += sz * litArr[k]; PU += sz * pub[k]; NS += sz * nsArr[k]; KN += sz * ins[k].Knowledge; ADS += sz * adultSh[k];
                if (!double.IsNaN(x.PsiBreakeven) && !double.IsInfinity(x.PsiBreakeven)) { PS += sz * x.PsiBreakeven; wPS += sz; }   // ψ* は U1 が正で有限の行だけ（別の重み）
            }
            if (!double.IsNaN(x.TfrShadow) && cl.Pool > 0)
            {
                wM += cl.Pool; CK += cl.Pool * x.CKnown; MN += cl.Pool * x.MNew; TS += cl.Pool * x.TfrShadow;
                shadowBirths += cl.Pool * (PopTfrNatural / PopFertileYears) * x.MNew * x.CKnown;
            }
            poolAll += cl.Pool;
        }
        string Wv(double sum) => F(wV > 0 ? sum / wV : double.NaN);
        string Wm(double sum) => F(wM > 0 ? sum / wM : double.NaN);
        var nrow = new System.Text.StringBuilder(head("N", -1, ""));
        nrow.Append('\t').Append(F(popK)).Append('\t').Append(F(a.PopMonths > 0 ? a.PopMonths / mo : double.NaN)).Append('\t').Append(F(w0014 / popK)).Append('\t').Append(F(wFert / popK)).Append('\t').Append(F(w60 / popK));
        nrow.Append('\t').Append(F(births)).Append('\t').Append(F(a.DeathsNormal)).Append('\t').Append(F(a.DeathsFamine)).Append('\t').Append(F(other)).Append('\t').Append(F(dWar))
            .Append('\t').Append(F(cbr)).Append('\t').Append(F(cdr)).Append('\t').Append(F(cdrN));
        for (int b = 0; b < Pop.AgeBands; b++) nrow.Append('\t').Append(F(a.BandDeaths[b]));   // 層別の死（平常＋飢え・千人）
        for (int b = 0; b < Pop.AgeBands; b++) nrow.Append('\t').Append(F(mu[b]));
        nrow.Append('\t').Append(F(s10)).Append('\t').Append(F(l60)).Append('\t').Append(F(tfrProxy)).Append('\t').Append(I(a.RecoveringMonths)).Append('\t').Append(I(a.NoMotherMonths));
        nrow.Append('\t').Append(Wv(ADS)).Append('\t').Append(Wv(YA)).Append('\t').Append(Wv(BK)).Append('\t').Append(Wv(CLR)).Append('\t').Append(Wv(E)).Append('\t').Append(Wv(CB)).Append('\t').Append(Wv(EF))
            .Append('\t').Append(Wv(NF)).Append('\t').Append(Wv(NL)).Append('\t').Append(Wv(PU)).Append('\t').Append(Wv(CC)).Append('\t').Append(Wv(O)).Append('\t').Append(Wv(U1));
        nrow.Append('\t').Append(F(pi)).Append('\t').Append(Wv(NS)).Append('\t').Append(Wv(V)).Append('\t').Append(F(wPS > 0 ? PS / wPS : double.NaN)).Append('\t').Append(Wv(G)).Append('\t').Append(Wv(KN))
            .Append('\t').Append(Wm(CK)).Append('\t').Append(Wm(MN)).Append('\t').Append(Wm(TS)).Append('\t').Append(F(poolAll)).Append('\t').Append(F(shadowBirths));
        sk.PopS.WriteLine(nrow.ToString());

        // C 行：階級ごとの影（挙げた国は全階級・他の国は年末の7階級）。流れの列は空
        for (int k = 0; k < NC; k++)
        {
            var o = outs[k]; var cl = cls[k]; if (o is null || cl is null || !(cl.Size > 0)) continue;
            if (!picked && Array.IndexOf(CeOtherClasses, k) < 0) continue;
            var x = o.Value;
            var sb = new System.Text.StringBuilder(head("C", k, names[k] ?? ""));
            sb.Append('\t').Append(F(cl.Size)).Append('\t').Append('\t').Append(F(cl.W0014 / cl.Size)).Append('\t').Append(F(cl.FertWomen / cl.Size)).Append('\t').Append(F(cl.W60 / cl.Size));
            sb.Append(blank(PopFlowCols));   // 流れの列（birthsK〜noMotherMonths）は空
            sb.Append('\t').Append(F(adultSh[k])).Append('\t').Append(F(ins[k].YAdult)).Append('\t').Append(F(ins[k].Basket)).Append('\t').Append(F(ins[k].ChildLaborRate)).Append('\t').Append(F(x.E)).Append('\t').Append(F(x.ChildBasketCost)).Append('\t').Append(F(x.EduCostFull))
                .Append('\t').Append(F(ins[k].NEdu)).Append('\t').Append(F(litArr[k])).Append('\t').Append(F(pub[k])).Append('\t').Append(F(x.ChildCost)).Append('\t').Append(F(x.OldAge)).Append('\t').Append(F(x.DirectUtility1));
            sb.Append('\t').Append(F(pi)).Append('\t').Append(F(nsArr[k])).Append('\t').Append(F(x.V)).Append('\t').Append(F(x.PsiBreakeven)).Append('\t').Append(F(x.G)).Append('\t').Append(F(ins[k].Knowledge))
                .Append('\t').Append(F(x.CKnown)).Append('\t').Append(F(x.MNew)).Append('\t').Append(F(x.TfrShadow)).Append('\t').Append(F(cl.Pool)).Append('\t').Append(F(cl.Pool * (PopTfrNatural / PopFertileYears) * x.MNew * x.CKnown));
            sk.PopS.WriteLine(sb.ToString());
        }
        a.Clear(w);
    }
}
