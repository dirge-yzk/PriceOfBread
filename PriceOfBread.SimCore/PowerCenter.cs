namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1090) 王権と議会の段 P1：政府の中心（執政・議会・軍・革命勢力）の取り分の純関数 ----------------------------------------------------------------
// 設計＝docs/202611/20261112/ の王権の設計（下書き・方針は作者「それでよい」2026-11-12・式と段は AI の推奨・確証なし）§3・§4-2。事前登録 measurements/m1090_20261113_prereg_cp.txt。
// 入力は World でなく数の束（段 P2 がエンジンから同じ関数を呼ぶ・試験の期待値を紙の式から作る）。エンジンの状態を読まず・書かず・乱数を引かない。
// 今は計器 InstrumentsCenter だけが呼ぶ（エンジンの挙動 0）。倒し先（否定形）：質量の和が正でなければ ℓ＝1／G＋qM_外 が正でなければ執政＝1／歳入が正でなければ w＝0／NaN は 0 に倒す。
internal static class PowerCenter
{
    /// <summary>兵の分かれ（ℓ＋d_A＋d_V＝1）。思想ごとの質量 mass[k] を、3 つの位置（体制 X・軍政 X_A・革命 X_V）のうち親和が一番高い所へ配る（同じなら体制→軍政→革命の順）。
    /// affX／affA／affV[k]＝思想 k から各位置への親和（Ideologies.AffinityOf と同じ物差し）。質量の和が正でなければ ℓ＝1。NaN・負の質量は 0 とみなす（否定形）。</summary>
    internal static (double Loyal, double Army, double Revolt) SoldierSplit(ReadOnlySpan<double> mass, ReadOnlySpan<double> affX, ReadOnlySpan<double> affA, ReadOnlySpan<double> affV)
    {
        double l = 0, a = 0, v = 0, total = 0;
        for (int k = 0; k < mass.Length && k < affX.Length && k < affA.Length && k < affV.Length; k++)
        {
            double m = mass[k];
            if (!(m > 0)) continue;
            total += m;
            if (affA[k] > affX[k] && affA[k] >= affV[k]) a += m;
            else if (affV[k] > affX[k] && affV[k] > affA[k]) v += m;
            else l += m;
        }
        if (!(total > 0)) return (1.0, 0.0, 0.0);
        return (l / total, a / total, v / total);
    }

    /// <summary>国内に居る全ての兵 G＝L−W（前線に縛られた W だけが首都に居ない）。負・NaN は 0。</summary>
    internal static double GOf(double l, double w) { double g = l - w; return g > 0 ? g : 0.0; }

    /// <summary>比べるための旧の定義 H＝min(L−W, 人口×q)（支配の力の H と同じ形）。</summary>
    internal static double HOf(double l, double w, double popTimesQ) { double g = GOf(l, w); double c = popTimesQ > 0 ? popTimesQ : 0.0; return g < c ? g : c; }

    /// <summary>財布 w（議会が止められる歳入の割合）。勅令＝i（利払い÷歳入）・部分承認＝確率の和 1−(1−t)(1−i)・議会承認＝1。歳入が正でなければ 0（否定形）。
    /// interestShare＝利払い÷歳入・taxYearShare＝年ごとに査定する税÷歳入（呼び手が [0,1] に収めて渡す。範囲外・NaN は 0 に倒す）。</summary>
    internal static double Wallet(BudgetLaw k, double interestShare, double taxYearShare, bool revenuePositive)
    {
        if (!revenuePositive) return 0.0;
        double i = interestShare > 0 ? (interestShare < 1 ? interestShare : 1.0) : 0.0;
        double t = taxYearShare > 0 ? (taxYearShare < 1 ? taxYearShare : 1.0) : 0.0;
        return k switch { BudgetLaw.Decree => i, BudgetLaw.Partial => 1.0 - (1.0 - t) * (1.0 - i), _ => 1.0 };
    }

    /// <summary>今の政体から K を導く写像（世界のデータに欄が無い間の開幕と途中の導出＝限界①）。</summary>
    internal static BudgetLaw KFromPolity(Polity p) => p switch { Polity.ConstitutionalMonarchy => BudgetLaw.Partial, Polity.Republic => BudgetLaw.Parliamentary, _ => BudgetLaw.Decree };

    /// <summary>政体の名（K と王冠と奪取から）：奪取あり＝奪取の名／勅令＝王冠の形の名（王冠なし＝ブルジョア独裁）／部分・議会＝王冠ありは立憲・なしは共和。</summary>
    internal static Polity NameOf(BudgetLaw k, CrownForm crown, SeizureForm seizure)
    {
        switch (seizure)
        {
            case SeizureForm.Bourgeois: return Polity.BourgeoisDictatorship;
            case SeizureForm.Proletarian: return Polity.ProletarianDictatorship;
            case SeizureForm.Military: return Polity.MilitaryDictatorship;
            case SeizureForm.Anarchy: return Polity.Anarchy;
        }
        if (k == BudgetLaw.Decree)
            return crown switch { CrownForm.Absolutist => Polity.Absolutist, CrownForm.Despotic => Polity.Despotic, CrownForm.Shogunate => Polity.Shogunate, _ => Polity.BourgeoisDictatorship };
        return crown != CrownForm.None ? Polity.ConstitutionalMonarchy : Polity.Republic;
    }

    /// <summary>1 つの K の下の評価の入力（World を持たない）。</summary>
    internal readonly record struct Inputs(
        double G, double Q, double MRad, double MRefOut, double Loyal, double Army, double Revolt, double Push,
        double InterestShare, double TaxYearShare, bool RevenuePositive);

    /// <summary>評価の結果。F＝力（陸軍力の単位）・S＝取り分。</summary>
    internal readonly record struct Result(
        double Wallet, double ValveIndex, double AutonomyIndex,
        double FExec, double FPart, double FArmy, double FRev, double Sum, double SumParts,
        double SExec, double SPart, double SArmy, double SRev, double SRegime, double Safety);

    private static double Clamp01(double x) => x > 0 ? (x < 1 ? x : 1.0) : 0.0;

    /// <summary>K を与えたときの 4 つの力と取り分。M_外＝M_急＋M_改外。a_V＝d_V・a_A＝d_A×m_A・体制＝ℓ＋d_A×(1−m_A)・m_A＝押し×(1−弁)・弁＝0.5×((1−任免が執政)＋(1−s_革命))。
    /// F_執政＝G×a_E×(1−w×c)・F_議会＝G×(a_E×w×c＋a_P)・F_軍＝G×a_A・F_革命＝G×d_V＋q×M_外（c＝議会のまとまり＝この段は 1）。ΣF＝G＋q×M_外。</summary>
    internal static Result Evaluate(in Inputs x, BudgetLaw k)
    {
        double g = x.G > 0 ? x.G : 0.0, q = x.Q > 0 ? x.Q : 0.0;
        double mr = x.MRad > 0 ? x.MRad : 0.0, mo = x.MRefOut > 0 ? x.MRefOut : 0.0;
        double mExt = mr + mo;
        double sum = g + q * mExt;
        double dl = x.Loyal, da = x.Army, dv = x.Revolt;
        // 革命の取り分は m_A に依らない（輪が無い）：弁は先にこれを読む
        double sRev0 = sum > 0 ? (g * dv + q * mExt) / sum : 0.0;
        double appointExec = k == BudgetLaw.Parliamentary ? 0.0 : 1.0;
        double valve = Clamp01(0.5 * ((1.0 - appointExec) + (1.0 - sRev0)));
        double mA = Clamp01(x.Push) * (1.0 - valve);
        double w = Wallet(k, x.InterestShare, x.TaxYearShare, x.RevenuePositive);
        double aA = da * mA, regime = dl + da * (1.0 - mA), aV = dv;
        double aE = k == BudgetLaw.Parliamentary ? 0.0 : regime, aP = k == BudgetLaw.Parliamentary ? regime : 0.0;
        const double c = 1.0;
        double fE = g * aE * (1.0 - w * c), fP = g * (aE * w * c + aP), fA = g * aA, fV = g * aV + q * mExt;
        double parts = fE + fP + fA + fV;
        double sE, sP, sA, sV;
        if (sum > 0) { sE = fE / sum; sP = fP / sum; sA = fA / sum; sV = fV / sum; }
        else { sE = 1.0; sP = 0.0; sA = 0.0; sV = 0.0; }
        double gov = sE > sP ? sE : sP, partner = sE > sP ? sP : sE;
        double others = System.Math.Max(sA, System.Math.Max(sV, partner));
        double safety = gov > 0 ? others / gov : double.NaN;
        return new Result(w, valve, mA, fE, fP, fA, fV, sum, parts, sE, sP, sA, sV, sE + sP, safety);
    }

    /// <summary>点火とクーデタの判定（体制＝執政＋議会と比べる）。F_革命＞F_体制 で点火：急進の側（G×d_V＋q×M_急）が改革の側（q×M_改外）より大きければ暴力・でなければ平和な転換。
    /// ν＝F_革命÷F_体制（体制の力が 0 以下なら nan）。ν≧landslide で即時。クーデタ＝G×d_A×m_A＞G×ℓ（比 nuCoup＝d_A×m_A÷ℓ）。</summary>
    internal readonly record struct Verdict(bool Violent, bool Peaceful, bool Immediate, bool Coup, double Nu, double NuCoup);

    internal static Verdict Decide(in Inputs x, in Result r, double landslide)
    {
        double g = x.G > 0 ? x.G : 0.0, q = x.Q > 0 ? x.Q : 0.0;
        double mr = x.MRad > 0 ? x.MRad : 0.0, mo = x.MRefOut > 0 ? x.MRefOut : 0.0;
        double fReg = r.FExec + r.FPart;
        bool ignite = r.FRev > fReg;
        double radicalSide = g * x.Revolt + q * mr, reformSide = q * mo;
        bool violent = ignite && radicalSide > reformSide;
        bool peaceful = ignite && !violent;
        double nu = fReg > 0 ? r.FRev / fReg : double.NaN;
        bool immediate = ignite && landslide > 0 && fReg > 0 && nu >= landslide;
        double aArmy = r.AutonomyIndex * x.Army;
        bool coup = g * aArmy > g * x.Loyal;
        double nuCoup = x.Loyal > 0 ? aArmy / x.Loyal : double.NaN;
        return new Verdict(violent, peaceful, immediate, coup, nu, nuCoup);
    }
}
