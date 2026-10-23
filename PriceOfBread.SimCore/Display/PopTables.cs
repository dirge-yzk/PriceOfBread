namespace PriceOfBread.SimCore.Display;

/// <summary>階級の表の 1 行（全国の同じ階級の POP を集約した物）。割合は 0〜1・人数は千人。吹き出しの文は本体の目録の側で組む（書式は <see cref="DisplayFormat"/>）。</summary>
public sealed record ClassLine(int ClassId, string Name, int Layer, double Size, double Share, double Unemployed, double UnempRate,
    double Satisfaction, double Militancy, double Literacy, double LeanNone, double LeanConservative, double LeanLiberal, double LeanSocialist,
    string Tip, string? SatisfactionTip, string? MilitancyTip, string Privileges, string? PrivilegeTip, string? LayerTip);

/// <summary>州の表の 1 行。割合は 0〜1・人数は千人。<see cref="Rule"/>＝支配度の 4 区分（国家・在地・外国・無統治）。</summary>
public sealed record RegionLine(string Name, double Population, double Unemployed, double FoodFill, double HousingFill, bool HousingShown, IReadOnlyList<(string Name, double Share)> Rule, bool IsPort, double PortCapacity);

/// <summary>文化・宗教の構成の 1 行（州のラベルを州人口で加重した合計）。</summary>
public sealed record CompositionLine(string Name, double Population, double Share);

/// <summary>年齢の層の人数（千人・下の層から上の層の順）。<see cref="Labels"/>＝層の名前（0／1〜5／…／71以上）。</summary>
public sealed record AgePyramid(IReadOnlyList<string> Labels, IReadOnlyList<double> Male, IReadOnlyList<double> Female)
{
    public static AgePyramid Empty { get; } = new([], [], []);
}

/// <summary>出生と死の標本（累積の出生と死亡・人口・出産可能な女性）。年に 1 回の差分から粗出生率・粗死亡率・合計出生率の代理を出す。</summary>
public sealed record DemographyStock(double BirthsCum, double DeathsCum, double Population, double FertileWomen);

/// <summary>人口ページの表（設計 §3-3）。階級（特権つき）・州・宗教・文化・年齢の層。
/// 値は旧 App の POP タブ（<c>Refresh</c> の階級の行・<c>BuildComposition</c>・地図の州ホバー・特権の語）を本体の目録へ移した物＝式は同じ。
/// 盤面は読むだけ・状態を持たない。</summary>
public static class PopTables
{
    private const double FillWarnThreshold = 0.95;   // 充足率がこれ未満で不足（過密/主食/住居）
    private static readonly string[] PartyNames = ["保守党", "自由党", "社会党"];

    /// <summary>議会（議席）が法に効く政体か（議席が無い段階では支持政党を出さない＝法の効きと同じ判定）。</summary>
    private static bool HasParliament(World w)
        => w.ParliamentEnabled && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && (w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2]) > 0.005;

    /// <summary>POP の人数を保守・自由・社会の 3 族へ配る（単位＝千人）。政治意識のある人数 Size×Consciousness を族比で割る＝残りが「なし」。
    /// 族への写像と、族が全く無いときの退避（＝保守）は議会の得票の計算と同じ。</summary>
    private static (double Cons, double Lib, double Soc) IdeologyHeads(Pop p)
    {
        double pc = 0, pl = 0, ps = 0;
        for (int i = 0; i < Ideologies.Count; i++)
        {
            int fam = PoliticalQuantities.FamilyOf(i);
            if (fam == 0) pc += p.IdeologyShares[i]; else if (fam == 1) pl += p.IdeologyShares[i]; else ps += p.IdeologyShares[i];
        }
        double sm = pc + pl + ps; if (sm < 1e-9) { pc = 1; sm = 1; }
        double aware = p.Size * Math.Clamp(p.Consciousness, 0, 1);
        return (aware * pc / sm, aware * pl / sm, aware * ps / sm);
    }

    // ===== 特権の語（階級の表の特権の列）=====

    /// <summary>特権 k の残高を決めている根拠の 1 行「題名:現在の段」＝<see cref="Privileges.Degree"/> の分岐をそのまま映す。段の名前は法の表と同じ。
    /// null＝根拠が法でも政体でもない腕＝行そのものを出さない（嘘の根拠を書かない）。</summary>
    private static string? PrivilegeSourceLine(World w, int kind)
    {
        var c = w.Coef;
        if (kind == Privileges.TaxExemption)   // 身分制免税だけは法の表に行が無い＝政体が決める
            return c.PrivilegeFromCrownShare.Value > 0 ? null : "政体:" + PlayText.PolityRegimeJa(w.Polity);
        string? title = kind switch
        {
            Privileges.Tithe => "国教",
            Privileges.VestedOffice => "官僚制度",
            Privileges.Seigneurial => "農奴解放",
            Privileges.Monopoly => c.AntitrustGate > 0 ? "独占禁止" : null,   // ゲート 0＝独占レントの係数は常に 1＝法は効かない
            Privileges.Guild => "同業組合",
            _ => null,
        };
        if (title is null) return null;
        var def = LawCatalog.All.FirstOrDefault(l => l.Name == title);
        return def is null ? title : title + ":" + def.StageName(def.Current(w));
    }

    /// <summary>特権 k を実際に受ける階級の名前（POP の階級名と同じ文字列）。</summary>
    private static string PrivilegeBeneficiaries(World w, int kind)
    {
        string s = "";
        for (int cls = 0; cls < PopClass.Count; cls++)
        {
            if (!Privileges.HeldBy(kind, cls)) continue;
            foreach (var p in w.AllPops())
                if (p.ClassId == cls && p.ClassName.Length > 0) { s += (s.Length == 0 ? "" : "・") + p.ClassName; break; }
        }
        return s;
    }

    /// <summary>その階級が持ちうる特権（保有の語の並びと、全ての特権の吹き出しの文）。機構が休眠の特権（同業組合＝GuildLawGate=0）は並べない。</summary>
    private static (string Held, string? Tip) PrivilegeCells(World w, int classId)
    {
        var held = new List<string>(); var tips = new List<string>();
        for (int k = 0; k < Privileges.Count; k++)
        {
            if (!Privileges.HeldBy(k, classId) || !Privileges.Enabled(w.Coef, k)) continue;
            double deg = Privileges.Degree(w, w.Coef, k);
            if (deg > 0) held.Add(Privileges.Names[k]);   // NaN は保有でない側へ落ちる
            string tip = Privileges.Names[k];
            if (PrivilegeSourceLine(w, k) is { } src) tip += "\n" + src;
            tip += "\n残高:" + (!(deg >= 0) ? "—" : deg.ToString("P0"));   // 否定形＝NaN は「—」
            string ben = PrivilegeBeneficiaries(w, k);
            if (ben.Length > 0) tip += "\n受益:" + ben;
            tips.Add(tip);
        }
        return (held.Count == 0 ? "なし" : string.Join("　", held), tips.Count == 0 ? null : string.Join("\n\n", tips));
    }

    // ===== 階級の表 =====

    /// <summary>全国の同じ階級の POP を集約した行（階級は最初に現れた順・層ごとにまとめる）。</summary>
    public static IReadOnlyList<ClassLine> ClassLines(World w)
    {
        var order = new List<int>();
        var agg = new Dictionary<int, List<(Region Rg, Pop P)>>();
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            foreach (var p in rg.Pops)
            {
                if (!agg.TryGetValue(p.ClassId, out var l)) { agg[p.ClassId] = l = new(); order.Add(p.ClassId); }
                l.Add((rg, p));
            }
        }
        double popTotal = 0;
        foreach (var rp in w.AllPops()) popTotal += rp.Size;
        bool foodShort = w.Regions.Any(r => r.State == RegionState.Active && r.FoodFill < FillWarnThreshold);
        double taxBurden = w.TaxRate.Value + w.PollTax * 0.5;
        bool parliamentActive = HasParliament(w);
        var lit = PlayText.ClassLayerMeans(w, p => p.Literacy); var sat = PlayText.ClassLayerMeans(w, p => p.Satisfaction); var mil = PlayText.ClassLayerMeans(w, p => p.Militancy);
        string? LayerTip(int layer)
        {
            string name = PlayText.ClassLayerNames[layer], s = "";
            foreach (var (label, rows) in new (string Label, List<(string Layer, double Mean)> Rows)[] { ("識字率", lit), ("満足度", sat), ("闘争度", mil) })
                foreach (var (ly, mean) in rows)
                    if (ly == name) s += (s.Length == 0 ? "" : "\n") + $"{label}　{mean:P0}";
            return s.Length == 0 ? null : s;
        }

        var lines = new List<ClassLine>();
        foreach (int cid in order)
        {
            var members = agg[cid];
            var first = members[0].P;
            double size = 0, sav = 0, earned = 0, satW = 0, milW = 0, litW = 0, femW = 0, childW = 0, oldW = 0, unempW = 0;
            double leanC = 0, leanL = 0, leanS = 0;
            double mLiving = 0, mVoice = 0, mWar = 0, mNation = 0, mFamine = 0;
            double debt = 0;
            double fMet = 0, fNeed = 0, nMet = 0, nTot = 0;
            foreach (var (rg, p) in members)
            {
                size += p.Size; sav += p.Savings.Value; earned += p.EarnedPerMonth.Value; debt += p.Debt.Value;
                fMet += p.FoodMet; fNeed += p.FoodNeed; nMet += p.NeedMet; nTot += p.NeedTotal;
                satW += p.Satisfaction * p.Size; milW += p.Militancy * p.Size; litW += p.Literacy * p.Size;
                double a20 = p.AgeShare(20, double.PositiveInfinity);   // 女性比＝20歳以上のうちの女性・子供比＝15歳未満・老年比＝60歳以上
                femW += (a20 > 1e-300 ? p.AgeShare(20, double.PositiveInfinity, 1) / a20 : 0) * p.Size; childW += p.AgeShare(0, 15) * p.Size; oldW += p.AgeShare(60, double.PositiveInfinity) * p.Size;
                if (!p.IsSoldier) unempW += p.UnemploymentRate.Value * p.Size;   // 兵士の〈失業〉＝給与の未払いの割合＝失業者に数えない
                mLiving += p.MilLiving * p.Size; mVoice += p.MilVoice * p.Size; mWar += p.MilWar * p.Size;
                mNation += p.MilNation * p.Size; mFamine += p.MilFamine * p.Size;
                var (headC, headL, headS) = IdeologyHeads(p);
                leanC += headC; leanL += headL; leanS += headS;
            }
            double inv = size > 1e-9 ? 1.0 / size : 0;
            double satAvg = satW * inv, milAvg = milW * inv, litAvg = litW * inv, fem = femW * inv, child = childW * inv, old = oldW * inv, unemp = unempW * inv;
            double perCap = size > 1e-9 ? sav / size : 0;
            // 満足度の吹き出し＝主食と主食以外の充足（満足度の台帳には食料が載っていない＝台帳そのものが「主食以外」）
            double foodFill = fNeed > 1e-9 ? Math.Clamp(fMet / fNeed, 0, 1) : 1.0;
            double othFill = nTot > 1e-9 ? Math.Clamp(nMet / nTot, 0, 1) : 1.0;
            string satTip = $"主食の充足:{foodFill:P0}\n主食以外の充足:{othFill:P0}";
            string party = first.Closed ? "保守党" : first.IsModernElite ? "自由党" : "社会党";   // 支持政党＝利益ブロック
            string tip = first.ClassName
                       + $"\n所得:{DisplayFormat.Money(earned)}　貯蓄:£{perCap:F1}　借金:£{(size > 1e-9 ? debt / size : 0):F1}";
            foreach (var (rg, p) in members.Where(m => m.P.Size > 0.5).OrderByDescending(m => m.P.Size))
            {
                double un = p.IsSoldier ? 0 : p.Size * p.UnemploymentRate.Value;
                tip += $"\n{rg.Name}:{DisplayFormat.PopK(p.Size)}" + (un > 0.5 ? $"(失業{DisplayFormat.PopK(un)})" : "");
            }
            tip += $"\n女性:{fem:P0}　子供:{child:P0}　老年:{old:P0}"
                 + $"\n失業率:{unemp:P0}" + (parliamentActive ? $"　支持政党:{party}" : "");
            // 闘争度の要因内訳（全国 size 加重の各寄与・多い順・0.5% 未満は省略）。生活水準の不足には操作可能な主因をヒントに添える
            var milb = new List<(string Label, double V)>
            {
                (PlayText.MilitancyPartNames[0], mLiving * inv), (PlayText.MilitancyPartNames[1], mVoice * inv), (PlayText.MilitancyPartNames[2], mWar * inv),
                (PlayText.MilitancyPartNames[3], mNation * inv), (PlayText.MilitancyPartNames[4], mFamine * inv),
            };
            var causes = new List<string>();
            if (foodShort) causes.Add("主食不足");
            if (!first.Closed && !first.IsSoldier && taxBurden > 0.30) causes.Add("重税");
            if (!first.Closed && !first.IsModernElite && w.WorkHoursValue >= 12 && first.IsUrban) causes.Add("長時間労働");
            string? milTip = null;
            foreach (var (label, v) in milb.Where(x => x.V > 0.005).OrderByDescending(x => x.V))
            {
                string extra = label == "生活水準の不足" && causes.Count > 0 ? $"({string.Join("、", causes)})" : "";
                milTip = (milTip is null ? "" : milTip + "\n") + $"・{label}:+{v:P0}{extra}";
            }
            double leanNone = Math.Max(0, size - (leanC + leanL + leanS));   // 差で取る＝4 列の合計は常に規模と一致する
            var (held, privTip) = PrivilegeCells(w, cid);
            int layer = PlayText.ClassLayerOf(first);
            lines.Add(new ClassLine(cid, first.ClassName, layer, size, popTotal > 1e-9 ? size / popTotal : 0, size * unemp, unemp,
                satAvg, milAvg, litAvg, leanNone * inv, leanC * inv, leanL * inv, leanS * inv, tip, satTip, milTip, held, privTip, LayerTip(layer)));
        }
        return lines.Select((l, i) => (l, i)).OrderBy(t => t.l.Layer).ThenBy(t => t.i).Select(t => t.l).ToList();   // 層ごとにまとめる（層の中は最初に現れた順）
    }

    /// <summary>階級の表。規模と失業者の後ろに添える増減の矢印は、四半期の差分を採る側（App）が関数で渡す（差分は今の盤面から導けない）。</summary>
    public static Table<ClassLine> Classes(Func<ClassLine, string>? sizeNote = null, Func<ClassLine, string>? unemployedNote = null)
        => new("pop.classes", "階級", Page.Population, (_, w) => ClassLines(w), l => "class." + l.ClassId, l => l.Name,
        [
            Col.Text<ClassLine>("name", "階級", l => l.Name, l => l.Tip),
            Col.NumberNote<ClassLine>("size", "規模", Unit.People, l => l.Size, l => (l.Share > 0 ? $"({l.Share:P0})" : "") + (sizeNote?.Invoke(l) ?? "")),
            Col.NumberNote<ClassLine>("unemployed", "失業者", Unit.People, l => l.UnempRate > 0.005 ? l.Unemployed : double.NaN,   // 丸めて 0% の階級は「—」
                l => { string arrow = unemployedNote?.Invoke(l) ?? ""; return l.UnempRate > 0.005 ? $"({l.UnempRate:P0})" + arrow : ""; }),   // 矢印の差分は表示の有無に依らず全階級で採る
            Col.Number<ClassLine>("satisfaction", "満足度", Unit.Share, l => l.Satisfaction, tip: l => l.SatisfactionTip),
            Col.Number<ClassLine>("militancy", "闘争度", Unit.Share, l => l.Militancy, tip: l => l.MilitancyTip),
            Col.Number<ClassLine>("literacy", "識字率", Unit.Share, l => l.Literacy),
            Col.Number<ClassLine>("none", "なし", Unit.Share, l => l.LeanNone),
            Col.Number<ClassLine>("conservative", "保守", Unit.Share, l => l.LeanConservative),
            Col.Number<ClassLine>("liberal", "自由", Unit.Share, l => l.LeanLiberal),
            Col.Number<ClassLine>("socialist", "社会", Unit.Share, l => l.LeanSocialist),
            Col.Text<ClassLine>("privileges", "特権", l => l.Privileges, l => l.PrivilegeTip),
        ], l => PlayText.ClassLayerNames[l.Layer], l => l.LayerTip);

    // ===== 州の表 =====

    /// <summary>支配度の 4 区分（国家・在地・外国・無統治）。区分は州ごとに毎四半期 Politics.CheckSecession が目標へ寄せる live な量＝そのまま出す（0% は 0% と出す）。
    /// 4 要素でない州だけ、国家＝支配度・残りは無統治。</summary>
    private static IReadOnlyList<(string Name, double Share)> RuleParts(Region rg)
    {
        double dom, loc = 0, foreign = 0, ungov = 0;
        if (rg.Rule.Length == 4)
        {
            double s0 = rg.Rule[0], s1 = rg.Rule[1], s2 = rg.Rule[2], s3 = rg.Rule[3];
            if (!(s0 > 0)) s0 = 0;   // ⚠Math.Clamp は NaN を落とさない＝否定形で書く
            if (!(s1 > 0)) s1 = 0;
            if (!(s2 > 0)) s2 = 0;
            if (!(s3 > 0)) s3 = 0;
            double sAll = s0 + s1 + s2 + s3;
            if (sAll > 1e-9) { dom = s0 / sAll; loc = s1 / sAll; foreign = s2 / sAll; ungov = s3 / sAll; }
            else { dom = 0; ungov = 1.0; }
        }
        else
        {
            dom = rg.Dominance;
            if (!(dom > 0)) dom = 0; else if (!(dom < 1)) dom = 1;
            ungov = 1.0 - dom;
        }
        return [("国家", dom), ("在地", loc), ("外国", foreign), ("無統治", ungov)];
    }

    /// <summary>自国の稼働中の州（人口の降順）。</summary>
    public static IReadOnlyList<RegionLine> RegionLines(World w)
    {
        var lines = new List<RegionLine>();
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            double un = 0;
            foreach (var p in rg.Pops) if (!p.IsSoldier) un += p.Size * p.UnemploymentRate.Value;   // 兵士の〈失業〉＝給与の未払いの割合＝失業者に数えない
            lines.Add(new RegionLine(rg.Name, rg.SumPopulation(), un, rg.FoodFill, rg.HousingFill, w.Coef.HousingNeedPerUrban > 0, RuleParts(rg), rg.IsPort, rg.PortCapacity));
        }
        return lines.Select((l, i) => (l, i)).OrderByDescending(t => t.l.Population).ThenBy(t => t.i).Select(t => t.l).ToList();
    }

    /// <summary>州の表（人口・失業者・主食と住居の充足・支配度の 4 区分・港の容量）。</summary>
    public static Table<RegionLine> Regions { get; } = new("pop.regions", "州の表", Page.Population, (_, w) => RegionLines(w), l => l.Name, l => l.Name,
    [
        Col.Text<RegionLine>("name", "州名", l => l.Name),
        Col.Number<RegionLine>("population", "人口", Unit.People, l => l.Population),
        Col.Number<RegionLine>("unemployed", "失業者", Unit.People, l => l.Unemployed),
        Col.Number<RegionLine>("food", "主食の充足", Unit.Share, l => l.FoodFill),
        Col.Number<RegionLine>("housing", "住居の充足", Unit.Share, l => l.HousingFill, hide: l => !l.HousingShown),
        Col.Share<RegionLine>("rule", "支配度", l => new ShareCell(l.Rule)),
        Col.Number<RegionLine>("port", "港の容量", Unit.Count, l => l.PortCapacity, hide: l => !l.IsPort),
    ]);

    // ===== 宗教と文化の構成 =====

    private static IReadOnlyList<CompositionLine> Composition(World w, bool religion)
    {
        var src = new Dictionary<string, double>();
        double total = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double p = r.SumPopulation();
            if (p <= 0) continue;
            total += p;
            string label = religion ? r.Religion : r.Culture;
            if (!string.IsNullOrEmpty(label)) src[label] = src.GetValueOrDefault(label) + p;
        }
        var list = new List<CompositionLine>();
        if (total <= 1e-9) return list;
        foreach (var kv in src.OrderByDescending(kv => kv.Value)) list.Add(new CompositionLine(kv.Key, kv.Value, kv.Value / total));
        return list;
    }

    private static Table<CompositionLine> CompositionTable(string key, string name, bool religion)
        => new(key, name, Page.Population, (_, w) => Composition(w, religion), l => l.Name, l => l.Name,
        [
            Col.Text<CompositionLine>("name", "名前", l => l.Name),
            Col.Number<CompositionLine>("population", "人口", Unit.People, l => l.Population),
            Col.Number<CompositionLine>("share", "割合", Unit.Share, l => l.Share),
        ]);

    /// <summary>宗教の構成（州の宗教のラベルを州人口で加重・割合の降順）。</summary>
    public static Table<CompositionLine> Religion { get; } = CompositionTable("pop.religion", "宗教", true);

    /// <summary>文化の構成（州の文化のラベルを州人口で加重・割合の降順）。</summary>
    public static Table<CompositionLine> Culture { get; } = CompositionTable("pop.culture", "文化", false);

    // ===== 年齢の層・出生と死・児童の就業率 =====

    /// <summary>年齢の層ごとの人数（男女別・千人）。層の数と名前は POP の年齢の器から引く（層の数を決め打ちしない）。</summary>
    public static AgePyramid Pyramid(World w)
    {
        AgeLayout? lay = null; double[] male = [], female = [];
        foreach (var p in w.AllPops())
        {
            var l = AgeLayout.OfLength(p.Ages.Length);
            if (lay is null) { lay = l; male = new double[l.Bands]; female = new double[l.Bands]; }
            if (!ReferenceEquals(lay, l)) continue;
            for (int b = 0; b < l.Bands; b++) { male[b] += p.Size * p.Ages[2 * b]; female[b] += p.Size * p.Ages[2 * b + 1]; }
        }
        if (lay is null) return AgePyramid.Empty;
        var labels = new string[lay.Bands];
        for (int b = 0; b < lay.Bands; b++)
        {
            int lo = (int)Math.Round(lay.Lower[b]);
            if (b == lay.Bands - 1) labels[b] = $"{lo}以上";
            else { int hi = (int)Math.Round(lay.Upper[b]) - 1; labels[b] = hi <= lo ? $"{lo}" : $"{lo}〜{hi}"; }
        }
        return new AgePyramid(labels, male, female);
    }

    /// <summary>出生と死の標本。出生と死亡は計器の累積（World.DiagBirths/DiagDeaths）。出産可能な女性は 15〜49 歳。</summary>
    public static DemographyStock Demography(World w)
    {
        double pop = 0, fertile = 0;
        foreach (var p in w.AllPops()) { pop += p.Size; fertile += p.Size * p.AgeShare(15, 50, 1); }
        return new DemographyStock(w.DiagBirths, w.DiagDeaths, pop, fertile);
    }

    /// <summary>児童の就業率（働きうる子供 10〜14 歳のいる POP の加重平均）。働きうる子供がいなければ null。</summary>
    public static double? ChildLaborRate(World w)
    {
        double sum = 0, size = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops)
                if (!p.IsSoldier && p.AgeShare(10, 15) > 1e-9) { sum += p.ChildLaborRate.Value * p.Size; size += p.Size; }
        return size > 1e-9 ? sum / size : null;
    }
}
