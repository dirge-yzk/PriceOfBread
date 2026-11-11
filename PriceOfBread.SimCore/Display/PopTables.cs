namespace PriceOfBread.SimCore.Display;

/// <summary>階級の表の 1 行（全国の同じ階級の POP を集約した物）。割合は 0〜1・人数は千人。吹き出しの文は本体の目録の側で組む（書式は <see cref="DisplayFormat"/>）。</summary>
public sealed record ClassLine(int ClassId, string Name, int Layer, double Size, double Share, double Unemployed, double UnempRate,
    double Satisfaction, double Militancy, double Literacy, double LeanNone, double LeanConservative, double LeanLiberal, double LeanSocialist,
    string Tip, string? SatisfactionTip, string? MilitancyTip, string Privileges, string? PrivilegeTip, string? LayerTip, double Discontent = 0);

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
            return c.PrivilegeFromCrownShare.Value > 0 ? null : "政体：" + PlayText.PolityRegimeJa(w.Polity);
        string? title = kind switch
        {
            Privileges.Tithe => "国教",
            Privileges.VestedOffice => "官僚制度",
            Privileges.Seigneurial => "農奴解放",
            Privileges.Monopoly => "独占禁止",
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
            tip += "\n残高：" + (!(deg >= 0) ? "—" : DisplayFormat.Share(deg));   // 否定形＝NaN は「—」
            string ben = PrivilegeBeneficiaries(w, k);
            if (ben.Length > 0) tip += "\n受益：" + ben;
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
            foreach (var p in rg.Pops)
            {
                if (!agg.TryGetValue(p.ClassId, out var l)) { agg[p.ClassId] = l = new(); order.Add(p.ClassId); }
                l.Add((rg, p));
            }
        }
        double popTotal = 0;
        foreach (var rp in w.AllPops()) popTotal += rp.Size;
        bool parliamentActive = HasParliament(w);
        var lit = PlayText.ClassLayerMeans(w, p => p.Literacy); var sat = PlayText.ClassLayerMeans(w, p => p.Satisfaction); var mil = PlayText.ClassLayerMeans(w, p => p.Militancy);
        string? LayerTip(int layer)
        {
            string name = PlayText.ClassLayerNames[layer], s = "";
            foreach (var (label, rows) in new (string Label, List<(string Layer, double Mean)> Rows)[] { ("識字率", lit), ("満足度", sat), ("闘争度", mil) })
                foreach (var (ly, mean) in rows)
                    if (ly == name) s += (s.Length == 0 ? "" : "\n") + $"{label}　{DisplayFormat.Share(mean)}";
            return s.Length == 0 ? null : s;
        }

        // 不満度（階級ごと）＝ヘッダーの不満度と同じ式（Politics.Discontent）を階級の POP の人口加重で平均する
        double[] disCoord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, w.Coef);
        double[] disGap = w.Coef.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        var lines = new List<ClassLine>();
        foreach (int cid in order)
        {
            var members = agg[cid];
            var first = members[0].P;
            if (!(members.Sum(m => m.P.Size) > 1e-9)) continue;   // 人数 0 の階級は行にしない（満足度・不満度などが 0% に見える）
            double size = 0, sav = 0, earned = 0, satW = 0, milW = 0, disW = 0, litW = 0, femW = 0, childW = 0, oldW = 0, unempW = 0;
            double leanC = 0, leanL = 0, leanS = 0;
            double mVoice = 0, mWar = 0, mNation = 0, mFamine = 0;
            double debt = 0;
            double fMet = 0, fNeed = 0, nMet = 0, nTot = 0;
            foreach (var (rg, p) in members)
            {
                size += p.Size; sav += p.Savings.Value; earned += p.EarnedPerMonth.Value; debt += p.Debt.Value;
                fMet += p.FoodMet; fNeed += p.FoodNeed; nMet += p.NeedMet; nTot += p.NeedTotal;
                satW += p.Satisfaction * p.Size; milW += p.Militancy * p.Size;
                disW += Politics.Discontent(w, w.Coef, p, disCoord, disGap.Length > 0 ? disGap[PolicyPreferences.BlocOf(p)] : 0) * p.Size; litW += p.Literacy * p.Size;
                double a20 = p.AgeShare(20, double.PositiveInfinity);   // 女性比＝20歳以上のうちの女性・子供比＝15歳未満・老年比＝60歳以上
                femW += (a20 > 1e-300 ? p.AgeShare(20, double.PositiveInfinity, 1) / a20 : 0) * p.Size; childW += p.AgeShare(0, 15) * p.Size; oldW += p.AgeShare(60, double.PositiveInfinity) * p.Size;
                if (!p.IsSoldier) unempW += p.UnemploymentRate.Value * p.Size;   // 兵士の〈失業〉＝給与の未払いの割合＝失業者に数えない
                mVoice += p.MilVoice * p.Size; mWar += p.MilWar * p.Size;
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
            string satTip = $"主食の充足：{DisplayFormat.Share(foodFill)}\n主食以外の充足：{DisplayFormat.Share(othFill)}";
            string party = first.Closed ? "保守党" : first.IsModernElite ? "自由党" : "社会党";   // 支持政党＝利益ブロック
            string tip = first.ClassName
                       + $"\n所得：{DisplayFormat.Money(earned)}　貯蓄：£{perCap:F1}　借金：£{(size > 1e-9 ? debt / size : 0):F1}";
            foreach (var (rg, p) in members.Where(m => m.P.Size > 0.5).OrderByDescending(m => m.P.Size))
            {
                double un = p.IsSoldier ? 0 : p.Size * p.UnemploymentRate.Value;
                tip += $"\n{rg.Name}:{DisplayFormat.PopK(p.Size)}" + (un > 0.5 ? $"(失業{DisplayFormat.PopK(un)})" : "");
            }
            tip += $"\n女性：{DisplayFormat.Share(fem)}　子供：{DisplayFormat.Share(child)}　老年：{DisplayFormat.Share(old)}"
                 + $"\n失業率：{DisplayFormat.Share(unemp)}" + (parliamentActive ? $"　支持政党：{party}" : "");
            // 闘争度の要因内訳（全国 size 加重の各寄与・多い順・0.5% 未満は省略）
            var milb = new List<(string Label, double V)>
            {
                (PlayText.MilitancyPartNames[0], mVoice * inv), (PlayText.MilitancyPartNames[1], mWar * inv),
                (PlayText.MilitancyPartNames[2], mNation * inv), (PlayText.MilitancyPartNames[3], mFamine * inv),
            };
            string? milTip = null;
            foreach (var (label, v) in milb.Where(x => x.V > 0.005).OrderByDescending(x => x.V))
            {
                milTip = (milTip is null ? "" : milTip + "\n") + $"・{label}:+{DisplayFormat.Share(v)}";
            }
            double leanNone = Math.Max(0, size - (leanC + leanL + leanS));   // 差で取る＝4 列の合計は常に規模と一致する
            var (held, privTip) = PrivilegeCells(w, cid);
            int layer = PlayText.ClassLayerOf(first);
            lines.Add(new ClassLine(cid, first.ClassName, layer, size, popTotal > 1e-9 ? size / popTotal : 0, size * unemp, unemp,
                satAvg, milAvg, litAvg, leanNone * inv, leanC * inv, leanL * inv, leanS * inv, tip, satTip, milTip, held, privTip, LayerTip(layer), disW * inv));
        }
        return lines.Select((l, i) => (l, i)).OrderBy(t => t.l.Layer).ThenBy(t => t.i).Select(t => t.l).ToList();   // 層ごとにまとめる（層の中は最初に現れた順）
    }

    /// <summary>階級の表の既定の形（増減の矢印を添えない）。目録の一覧と問題の一覧・ピン留めが読む。画面は <see cref="Classes"/> に矢印の関数を渡して使う（行の鍵と列は同じ）。</summary>
    public static Table<ClassLine> ClassesPlain { get; } = Classes();

    /// <summary>階級の表（2026-11-11 作者指示）：規模（人数(構成比)）・失業者（人数(失業率)＋増減の矢印）・満足度・不満度・闘争度（増減の矢印）・識字率・特権。
    /// 支持政党の列は政治の側にある＝ここには置かない。<paramref name="arrow"/>＝列の鍵（unemployed・satisfaction・discontent・militancy）ごとの増減の矢印（無ければ添えない）。</summary>
    public static Table<ClassLine> Classes(Func<ClassLine, string, string>? arrow = null)
        => new("pop.classes", "階級", Page.Population, (_, w) => ClassLines(w), l => "class." + l.ClassId, l => l.Name,
        [
            Col.Text<ClassLine>("name", "階級", l => l.Name, l => l.Tip),
            Col.Text<ClassLine>("size", "規模", l => $"{DisplayFormat.PopK(l.Size)}({DisplayFormat.Share(l.Share)})"),
            Col.Text<ClassLine>("unemployed", "失業者", l => { string a = arrow?.Invoke(l, "unemployed") ?? ""; return l.UnempRate > 0.005 ? $"{DisplayFormat.PopK(l.Unemployed)}({DisplayFormat.Share(l.UnempRate)}){a}" : "—"; }),   // 丸めて 0% の階級は「—」。矢印の差分は表示の有無に依らず全階級で採る
            Col.Text<ClassLine>("satisfaction", "満足度", l => $"{DisplayFormat.Share(l.Satisfaction)}{arrow?.Invoke(l, "satisfaction")}", l => l.SatisfactionTip),
            Col.Text<ClassLine>("discontent", "不満度", l => $"{DisplayFormat.Share(l.Discontent)}{arrow?.Invoke(l, "discontent")}"),
            Col.Text<ClassLine>("militancy", "闘争度", l => $"{DisplayFormat.Share(l.Militancy)}{arrow?.Invoke(l, "militancy")}", l => l.MilitancyTip),
            Col.Number<ClassLine>("literacy", "識字率", Unit.Share, l => l.Literacy),
            Col.Text<ClassLine>("privileges", "特権", l => l.Privileges, l => l.PrivilegeTip),
        ], l => PlayText.ClassLayerNames[l.Layer], l => l.LayerTip);

    // ===== 州の表 =====

    /// <summary>支配度の 4 区分（国家・在地・外国・無統治）。区分は州ごとに毎四半期 Politics.CheckSecession が目標へ寄せる live な量＝そのまま出す（0% は 0% と出す）。
    /// 4 要素でない州だけ、国家＝支配度・残りは無統治。</summary>
    public static IReadOnlyList<(string Name, double Share)> RuleParts(Region rg)
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
        Col.Number<RegionLine>("port", "港の拡張", Unit.Count, l => l.PortCapacity, hide: l => !l.IsPort),
    ]);

    // ===== 宗教と文化の構成 =====

    private static IReadOnlyList<CompositionLine> Composition(World w, bool religion)
    {
        var src = new Dictionary<string, double>();
        double total = 0;
        foreach (var r in w.Regions)
        {
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

    /// <summary>年齢の層ごとの人数（男女別・千人）。**表示のときだけ 10 年ごとに足し合わせる**（作者の指示 2026-11-11「データ的にはスケールを分けているが、他と同じく
    /// 10 年単位で表示〔足し合わせ〕でよい」）＝POP の層は幅がそろっていない（11層は 0 歳＝1 年・1〜20 歳＝5 年・21 歳以上＝10 年）ので、そのまま並べると棒の高さの尺度がそろわず形が崩れて見える。
    /// 足し合わせは前から、群の幅が 10 年に届くまで（11層なら 0〜10 歳の 3 層・11〜20 歳の 2 層・以降は 1 層ずつ）。名札は実際の年齢（0〜10・11〜20…・71以上）。
    /// 10 歳刻みの旧の 8 層は足し合わせない（今のまま）。シミュの層・人数の和は変えない＝表示の写しだけ。</summary>
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
        var labels = new List<string>(); var m = new List<double>(); var f = new List<double>();
        int start = 0;
        while (start < lay.Bands)
        {
            int end = start;   // 群の最後の層
            while (end < lay.Bands - 1 && lay.Upper[end] - lay.Lower[start] < 10.0 - 1e-9) end++;
            double sm = 0, sf = 0;
            for (int b = start; b <= end; b++) { sm += male[b]; sf += female[b]; }
            int lo = (int)Math.Round(lay.Lower[start]);
            if (end == lay.Bands - 1) labels.Add($"{lo}以上");
            else labels.Add($"{lo}〜{(int)Math.Round(lay.Upper[end]) - 1}");
            m.Add(sm); f.Add(sf);
            start = end + 1;
        }
        return new AgePyramid(labels.ToArray(), m.ToArray(), f.ToArray());
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
