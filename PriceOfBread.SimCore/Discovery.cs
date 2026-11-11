namespace PriceOfBread.SimCore;

/// <summary>発見の分野（作者裁定 2026-09-28 ⑤＝制度・思想・製法・発明。発明は新兵器と新製品の2つの規則）。
/// 制度（特許・株式会社）と思想（社会主義・共産主義・帝国主義・ファシズム）は 2026-10-01 (152) に同じ表へ（設計 §1A-6＝財を持たない行）。</summary>
public enum DiscoveryField { Institution, Idea, Process, Weapon, Product }

/// <summary>新兵器の軍事の圧力の分野（作者裁定 2026-09-28 ⑤＝陸・海・空で分ける）。</summary>
public enum WarDomain { None, Land, Sea, Air }

/// <summary>発見の表の1行（<see cref="Catalog.Discoveries"/>）＝名前・財・レシピ（""＝既定のレシピ）・分野・年代（10年刻み）・新兵器の分野・代替する物。
/// <paramref name="Replaces"/>＝その行が生み出す物が代替する物（作者 2026-10-02＝必要性はその値段の高さ・★(165) 新しいレシピで作る費用と比べる・<see cref="Discovery"/> の ReplacedStrength）＝<see cref="Catalog.ReplacesOwn"/>（製法＝その行の財そのもの）・
/// 新製品は代替する既存の財の id・<see cref="Catalog.LandFreight"/>（機関車＝陸の輸送＝財ではない）。新兵器・制度・思想の行は読まない。
/// <paramref name="After"/>＝★(336) 系統×段階の前提の行の名前（""＝前提なし＝系統の根〔段1〕）。前提の行がまだの国ではこの行を判定しない（点数0・飛ばして先の段を解禁しない）。
/// 系統＝根の行（前提のない行）から枝分かれする木・段＝根からの深さ（<see cref="Catalog.DiscoveryTier"/>）＝系統と段は前提の1本から導く（別に持たない）。（旧の旗 DiscoveryTierGate は 2026-10-18 に焼き込み。）
/// ★(461) <paramref name="EngineerParent"/>＝時計の形（<see cref="Coefficients.DiscoveryClockWorldPerMonth"/> が正）で、さらに <see cref="Coefficients.DiscoveryClockMotorParent"/> が正のときだけ（既定 0）、親の産業の技師を数える財（−1＝今の規則〔製法は行の財・新製品は Replaces の財〕）。カタログのデータ（国別でない）・必要性の <c>Replaces</c> は触らない。</summary>
public readonly record struct DiscoveryRow(string Name, int Good, string Recipe, DiscoveryField Field, int Decade, WarDomain Domain = WarDomain.None, int Replaces = Catalog.ReplacesOwn, string After = "", int EngineerParent = -1);

/// <summary>発見＝解禁の【唯一の形】（★命名の作者裁定2026-08-31＝「研究」はプレイヤーが方向を選ぶ動詞を連想させる・この機構は選べず社会から創発して見つかる＝「発見」）。
/// ★段2A（2026-10-01・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A-2）：**発見は2値**・国ごと・行（レシピ）ごとに毎四半期
/// <c>点数 ＝ 技術力（0〜70）＋ 必要性（0〜70）＋ 伝わり（0〜70）</c> を求め、**一度でも100に届いたら解禁**（以後失われない）。
/// 旧＝技術水準の時計の傾斜（onset〜onset+1 で 0→1 の連続の門・財の門×レシピの門の積）＝全ての国が技術水準だけで同じ順に同じ速さで開いた。
/// ・どの項の最大も門の100未満＝**1つだけでは届かない**／どの2つの最大の和も100以上＝**2つ揃えば届く**（作者の意図＝設計 §1A-2 の性質）。
/// ・**必要性が0の国は自力では発見しない**（技術力70＋伝わり0＝70）。**世界で最初の発見は必ず技術力＋必要性**（誰も持っていなければ伝わりは0）。
/// ・★物を生む行（製法・新製品）の必要性（2026-10-02 (159)）＝作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する。
///   例えば蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＝生み出す物（製法＝同じ財・新製品＝代替する既存の財）の平滑化した値段の高さ（<see cref="ReplacedStrength"/>）。
///   ★(165) 比べる相手（作者 2026-10-02「2でよい」）＝その発見が出す物の値段＝同じ国・同じ時期の平滑化した投入の値段から新しいレシピで作る費用（(159) は参照価格と比べた）。
///   旧＝製法は今の方法の投入の費用の割高さ・労働を代替する発明は人件費の世界比（作者 2026-09-30「労働を代替する発明は、安い人件費の国では必要とされない、という認識でよい」の AI の読み）・新製品は満足した消費者の厚み。
/// ・状態＝国×行の「解禁済み」の2値（<see cref="World.Discovered"/>）だけ＝設計思想⑩の「本当に必要な場合」（作者裁定 2026-09-28 で許可）。項はどれも今の盤面から導く。
/// ⚠時期感は**較正の目標**であってコードに書かない（暦年ゲートの裏口を作らない）＝行の年代はデータ・難易度は1本の Y で技術水準へ写す（<see cref="Difficulty"/>）。
/// ★制度と思想（2026-10-01 (152)・設計 §1A-6）も同じ表の行（財を持たない）＝<see cref="SpreadRows"/>（旧＝制度は中核エリートと工業化度の傾斜 Unlock・思想は世界の発展度の傾斜）。
/// ★場所に結びついた発見（大鉱床・植林＝<see cref="Catalog.Sites"/>・2026-10-01 (148)）も同じ関数で判定する（<see cref="SiteTerms"/>＝2項・状態は生産者の <see cref="Producer.SiteFound"/>）。</summary>
public static class Discovery
{
    /// <summary>各項の最大（技術力・必要性・伝わり）と門（作者裁定 2026-09-28 §1A-2「比率は仮」）。⚠較正で動かすときも「項の最大＜門≦項の最大×2」を保つ（番人）。</summary>
    public const double TermMax = 70.0, GateScore = 100.0;

    /// <summary>行の難易度（技術水準の単位）＝ Y ×（行の年代 − 1780）÷10（作者裁定 Q11＝10年刻みの年代を1本の Y で写す・全体の早い遅いは Y で直す）。
    /// Y の物差し（Q11）＝「各行の年代に、最も必要に迫られた平均的な先進国が届く値」＝英仏普の技術水準の平均（r144 で10年あたり約0.25）が
    /// 必要性 約45 と合わせて100に届く＝ 0.25÷(55÷70)≈0.32〜0.35。1780年代の行（1783年の開幕で既知）は0＝技術力の項は最大（技術の押しの分だけ）。</summary>
    public static double Difficulty(int row, Coefficients c) => DifficultyOf(Catalog.Discoveries[row].Decade, c);
    /// <summary>年代の難易度（発見の表と産地の発見の表 <see cref="Catalog.Sites"/> が共有する1本の Y）。</summary>
    public static double DifficultyOf(int decade, Coefficients c)
        => c.DiscoveryTechPerDecade * (decade - Catalog.DiscoveryBaseDecade) / 10.0;

    /// <summary>★(415) 生産者が雇った高度労働力の月あたりの量＝実産出（前の月・充足を経た値）×高度労働力の投入の産出1単位あたりの量。工場でない生産者（投入に高度労働力が無い）は0。
    /// 否定形＝NaN・負の産出は0。<see cref="Score"/> が門の開いた生産者についてだけ財ごとに足す（新しい状態を持たない・設計思想⑩）。</summary>
    internal static double EngineerUse(Producer p)
    {
        double o = p.Output;
        if (!(o > 0)) return 0;
        foreach (var inp in p.Inputs)
            if (inp.GoodId == Catalog.AdvLaborId) return inp.QtyPerOutput > 0 ? o * inp.QtyPerOutput : 0;
        return 0;
    }

    /// <summary>★(415) 行の親の産業の技師の量（月あたり・事前登録 [3-1]）＝親の財（製法の行は行の財・新製品は <see cref="DiscoveryRow.Replaces"/> の財・財でない <c>Replaces</c> の行は行の財）と、
    /// 行の代表のレシピの投入（労働力・高度労働力・農業労働力を除く）の財について、財ごとの雇用 <paramref name="eng"/>（<see cref="EngineerUse"/> の国の和）を足す。同じ財は1度だけ。
    /// ★(461) <paramref name="clockParent"/>＝別の旗 <see cref="Coefficients.DiscoveryClockMotorParent"/> が正（時計の形の上）のとき、行が持つ親の産業の欄 <see cref="DiscoveryRow.EngineerParent"/>（電動機＝蒸気機関の財）を使う。既定（偽）は今の規則。</summary>
    internal static double ParentEngineers(int row, Producer rp, double[] eng, bool clockParent = false)
    {
        var d = Catalog.Discoveries[row];
        int g0 = clockParent && d.EngineerParent >= 0 ? d.EngineerParent : d.Replaces >= 0 ? d.Replaces : d.Good;   // ★(461) 時計の形（W0 が正）のときだけ、行が親の産業を別に定める（電動機＝蒸気機関の財）
        double a = g0 >= 0 && g0 < eng.Length ? eng[g0] : 0;
        foreach (var inp in rp.Inputs)
        {
            int g = inp.GoodId;
            if (g < 0 || g >= eng.Length || g == g0 || Catalog.IsLaborGood(g)) continue;
            a += eng[g];
        }
        return a;
    }

    /// <summary>★(415) 開幕の互換の解禁（<see cref="Simulation"/> の構築が開幕の既知の発見の後に1度呼ぶ）＝旗 <see cref="Coefficients.DiscoveryEngineerHalfPerMonth"/> が0以下のあいだ、
    /// 行が無かった旧と同じにするため合成染料の行を解禁済みにする。旗が正なら何もしない（見つかるまで作れない）。</summary>
    internal static void SeedCompatRows(World w)
    {
        if (SyntheticDyeRow >= 0 && !(w.Coef.DiscoveryEngineerHalfPerMonth > 0)) w.Discovered[SyntheticDyeRow] = true;
    }

    /// <summary>★(415) 行ごとの親の産業の技師の量（月あたり・求めない行〔制度と思想・この国に代表の生産者が居ない・稼働中の州が無い〕は NaN）。読むだけ＝判定が読む量と同じ <see cref="Score"/> の中の値（番人と計器が読む）。</summary>
    internal static double[] ParentEngineersByRow(World w, WorldView? view, Coefficients c)
        => ParentEngineersByRow(w, view, c, out _, out _);

    /// <summary>★(450) 同じ 1 回の <see cref="Score"/>（includeKnown）が返す点数の項と、代替する物の値段・新しいレシピの費用も返す（計器の T 行の必要性・代替する物の値段・新しいレシピの費用の 3 列〔解禁済みの行も〕）。読むだけ＝判定の値は同じ。</summary>
    internal static double[] ParentEngineersByRow(World w, WorldView? view, Coefficients c, out Terms[] terms, out (double Replaced, double Cost)[] ratio)
    {
        var a = new double[Catalog.Discoveries.Length];
        ratio = new (double Replaced, double Cost)[Catalog.Discoveries.Length];
        terms = Score(w, view, c, includeKnown: true, null, ratio, a, out _, out _);
        return a;
    }

    /// <summary>★(415) 技師の比∈[0,1]＝<c>A÷(A＋A0×d)</c>（A0＝<see cref="Coefficients.DiscoveryEngineerHalfPerMonth"/>・d＝(行の年代−1780)÷10）。d が0以下の行は1（今の <c>diff</c>＝0 の比1と同じ）。
    /// 技師が居ない（A が0・NaN）産業は0。A に対して単調に増え1を超えない・同じ A なら d が大きいほど小さい（天井も床も作らない＝飽和の形）。否定形で NaN は0へ。</summary>
    internal static double EngineerFactor(int row, double a, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        if (!(a > 0)) return 0.0;
        double f = a / (a + c.DiscoveryEngineerHalfPerMonth * d);
        return PoliticalQuantities.Clamp01(f);
    }

    /// <summary>★(461) 世界の高度労働力の雇用の合計 W（月あたり・全ての財の和）＝全ての国の稼働中の州の、ゲートの開いた生産者の <see cref="EngineerUse"/> の和。時計 S の W＝全ての行・全ての国で同じ 1 つのスカラー。
    /// 導出量（新しい状態を作らない）。国の並び・州・生産者の順に足す＝決定的。<paramref name="visited"/>＝訪れた生産者の数（計算量の台帳の件数・診断）。
    /// 先に雇用を見て、技師を雇わない生産者（一次産品の大半）は門の照会を飛ばす（0 を足す・足さないは和のビットを変えない）。</summary>
    internal static double WorldAdvLabor(IEnumerable<World> worlds, out long visited)
    {
        double total = 0; long n = 0;
        foreach (var w in worlds)
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    n++;
                    double u = EngineerUse(p);
                    if (!(u > 0) || p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length || !(Gate(r, p) > 0)) continue;
                    total += u;
                }
            }
        visited = n;
        return total;
    }

    /// <summary>★(461) 財ごとの国の技師の雇用（月あたり・計器だけが読む国別の A_n の表の土台）＝<see cref="Score"/> の代表の生産者の走査が国ごとに足す <c>eng</c> と同じ規則（州の状態・財の範囲・ゲート）。</summary>
    internal static double[] EngineersByGood(World w)
    {
        var eng = new double[Catalog.Goods.Length];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= eng.Length || !(Gate(r, p) > 0)) continue;
                eng[p.GoodId] += EngineerUse(p);
            }
        }
        return eng;
    }

    /// <summary>★(461) 時計 S∈[0,1]＝<c>clamp((W÷(W0×d)−1)÷w＋1/2, 0, 1)</c>（細ランプ・W＝世界の高度労働力の雇用の合計〔全ての財の和・全ての行と国で共通〕・W0＝<see cref="Coefficients.DiscoveryClockWorldPerMonth"/>・
    /// w＝<see cref="Coefficients.DiscoveryClockRampWidth"/>・d＝(行の年代−1780)÷10）。W＝W0·d で 1/2・W0·d·(1−w/2) で 0・W0·d·(1+w/2) で 1。
    /// d が0以下の行は1・W が0以下か NaN は0（否定形）・W0 か w が正でなければ0（呼び手は <c>poolOn</c> で先に除く）。W に対して単調に増え1を超えない・同じ W なら d が大きいほど小さい。暦の年も国の名前も読まない。</summary>
    internal static double ClockStage(int row, double w, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        double w0 = c.DiscoveryClockWorldPerMonth, width = c.DiscoveryClockRampWidth;
        if (!(w > 0) || !(w0 > 0) || !(width > 0)) return 0.0;
        double s = (w / (w0 * d) - 1.0) / width + 0.5;
        return PoliticalQuantities.Clamp01(s);
    }

    /// <summary>★(461)(430) 自国の項 C∈[0,1]＝<c>A÷(A＋Am)</c>（A＝自国のその発見の親の産業の技師・Am＝<see cref="Coefficients.DiscoveryOwnHalfPerMonth"/>）。d が0以下の行は1・A が0以下か NaN は0。
    /// 有界の飽和＝大国の絶対量で先取りせず、産業の無い国は0・小さな工房の国は A÷Am で抑える。人口を読まない。</summary>
    internal static double OwnCapacity(int row, double a, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        if (!(a > 0)) return 0.0;
        double am = c.DiscoveryOwnHalfPerMonth > 0 ? c.DiscoveryOwnHalfPerMonth : 0;   // 否定形＝NaN・負は0（A が正なら1）
        double f = a / (a + am);
        return PoliticalQuantities.Clamp01(f);
    }

    public static bool Known(bool[]? known, int row) => row >= 0 && known is not null && row < known.Length && known[row];

    /// <summary>★世界のゲート（2026-10-01 (150)・設計 §0-A の 5 の残り）＝財に紐づかない産業の門（工場の蒸気動力・鉄道・近代港湾・冷蔵）も発見の表の行で開く
    /// （旧＝技術水準の時計〔SteamTechOnset 0.25・RailTechOnset 0.35・PortTechOnset 0.2〕と工業化度の傾斜＝発見と関係なく開いた）。新しい行は置かない＝同じ技術の行を読む（推奨で進めた）。
    /// ・蒸気動力（工場が石炭を焚く・近代港湾＝蒸気の浚渫と埠頭）＝「蒸気機関」の行（労働力の財の蒸気のレシピ）＝必要性は動力（労働力の財）の平滑化した値段の高さ
    ///   （作者 2026-10-02「蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」・旧＝人件費の世界比〔(147)〕）。
    /// ・鉄道輸送（内陸の輸送の容量・陸路の費用・機関車の運行の石炭）＝「機関車」の行＝必要性は陸の輸送の不足（作者 2026-10-02「鉄道研究の必要性は、軍事力ではなく、輸送力の不足から導かれるべきだ」）。
    /// ・冷蔵（生鮮の腐敗の低減）＝「冷蔵」の行（効きの大きさは立っている設備の規模＝旧の傾斜のまま）。
    /// ・艦隊の運用燃料（休眠）は商船と同じ汽船の財の解禁（<see cref="GoodGate"/>）。2値＝普及のなめらかさは市場（投資で容量や工場が建つ速さ・石炭の値段）だけが持つ（設計 §1A-2）。</summary>
    public static readonly int SteamRow = Catalog.DiscoveryRowOf(Catalog.LaborId, "蒸気機関");
    public static readonly int LocomotiveRow = Catalog.DiscoveryRowOf(Catalog.LocomotiveId, "");
    public static readonly int ReeferRow = Catalog.DiscoveryRowOf(Catalog.RefrigeratorId, "");
    /// <summary>★(415) 並存レシピ〈合成染料〉（アニリン1856）の行。旗 <see cref="Coefficients.DiscoveryEngineerHalfPerMonth"/> が0以下のあいだは全ての国が開幕で解禁済みにする（旧＝行が無く1783年から作れた＝今と1ビットも違わない互換）。</summary>
    public static readonly int SyntheticDyeRow = Catalog.DiscoveryRowOf(Catalog.DyeId, "合成染料");
    public static bool SteamPower(bool[]? known) => Known(known, SteamRow);
    public static bool RailTransport(bool[]? known) => Known(known, LocomotiveRow);
    public static bool Refrigeration(bool[]? known) => Known(known, ReeferRow);

    /// <summary>★制度と思想（2026-10-01 (152)・設計 §1A-6・作者裁定 2026-09-28 ⑤＝4分野とも同じ骨組み）＝財を持たない行（<see cref="Catalog.NoGood"/>）。
    /// 旧＝制度は中核エリート（特許）と最大工業化度（株式会社・鉄道の閾値 0.8）の傾斜・思想は世界の発展度（世界最先端の工業化度と都市の割合）の傾斜
    /// ＝全ての国がほぼ同時に開いた（計器 (152) の種1〜5＝社会主義 1784〜88・共産主義 1787〜91・株式会社 1786〜91・鉄道 1788〜1808 に主要国の全てで）。
    /// ・技術力＝発見の表と同じ（技術水準÷年代の難易度・技術の押しは1＝投入を持たない）。
    /// ・必要性＝その制度が解く問題・その思想が答える社会の緊張（設計 §1A-4 の表・§1A-6）＝<see cref="SpreadNeed"/>。半分点は分野ごとに1本（制度・思想）。
    /// ・伝わり＝思想と制度の到達（<see cref="World.SpreadReach"/>＝<see cref="MultiNationSim"/> の StepDiffusion＝国の組の取引の割合×両国の媒体〔新聞×識字＋ラジオ〕の積み上げ・設計 §1A-7）。
    /// 効き：特許＝革新の加速（<see cref="World.PatentLaw"/>）・株式会社＝工業化の投資（<see cref="World.JointStock"/>）・思想＝その思想の支持の門（<see cref="Ideologies.EraGates(World)"/>）。
    /// 鉄道（旧＝制度の行・最大工業化度 0.8 の閾値）は行を置かない＝「機関車」の行（<see cref="RailTransport"/>・(150) の鉄道輸送と同じ行）。</summary>
    public static readonly int PatentRow = Catalog.DiscoveryRowNamed("特許");
    public static readonly int JointStockRow = Catalog.DiscoveryRowNamed("株式会社");
    public static readonly int SocialismRow = Catalog.DiscoveryRowNamed("社会主義");
    public static readonly int CommunismRow = Catalog.DiscoveryRowNamed("共産主義");
    public static readonly int ImperialismRow = Catalog.DiscoveryRowNamed("帝国主義");
    public static readonly int FascismRow = Catalog.DiscoveryRowNamed("ファシズム");
    /// <summary>★(621) 制度と思想の行の技術の比＝本国の識字 ÷ 基準 <paramref name="full"/>（0〜1）。否定形：NaN・0 以下の識字は 0（解禁の門なので閉じる側へ倒す）。基準が正でなければ 1（呼び手は基準が正のときだけ呼ぶ＝旗 0 の経路）。</summary>
    internal static double SpreadLiteracyRatio(double lit, double full)
        => !(lit > 0) ? 0.0 : !(full > 0) ? 1.0 : lit >= full ? 1.0 : lit / full;
    /// <summary>思想の門の行（門を持たない思想＝伝統・自由・国民主義ほかは −1＝最初から在る）。</summary>
    public static int IdeaRow(Ideology k) => k switch
    {
        Ideology.Socialism => SocialismRow, Ideology.Communism => CommunismRow,
        Ideology.Imperialism => ImperialismRow, Ideology.Fascism => FascismRow, _ => -1,
    };
    /// <summary>制度と思想の行（表の順）＝思想と制度の到達の欄の並び（<see cref="World.SpreadReach"/>）。</summary>
    public static readonly int[] SpreadRows = Enumerable.Range(0, Catalog.Discoveries.Length).Where(r => IsSpread(Catalog.Discoveries[r].Field)).ToArray();
    private static readonly int[] SpreadSlots = Enumerable.Range(0, Catalog.Discoveries.Length).Select(r => System.Array.IndexOf(SpreadRows, r)).ToArray();
    /// <summary>行 → 到達の欄（制度と思想の行でなければ −1）。</summary>
    public static int SpreadSlot(int row) => row >= 0 && row < SpreadSlots.Length ? SpreadSlots[row] : -1;
    public static bool IsSpread(DiscoveryField f) => f is DiscoveryField.Institution or DiscoveryField.Idea;

    /// <summary>到達の放射の強さ（国 a の欄 slot）＝思想はその思想を持つ人の割合（国の思想の集計 <paramref name="nat"/>＝旧の StepDiffusion の種と同じ）・
    /// 制度は国の法＝解禁済みなら1（推奨で決めた＝その国に住む全員がその制度の下にいる）。</summary>
    internal static double SpreadSource(World a, int slot, double[] nat)
    {
        int row = SpreadRows[slot];
        if (Catalog.Discoveries[row].Field == DiscoveryField.Institution) return Known(a.Discovered, row) ? 1.0 : 0.0;
        int k = row == SocialismRow ? (int)Ideology.Socialism : row == CommunismRow ? (int)Ideology.Communism
              : row == ImperialismRow ? (int)Ideology.Imperialism : row == FascismRow ? (int)Ideology.Fascism : -1;
        return k >= 0 && k < nat.Length && nat[k] > 0 ? nat[k] : 0.0;
    }

    /// <summary>制度と思想の必要性の強度（行ごとに読む量＝設計 §1A-4 の表「(1) 制度＝制度が解く問題の大きさ」「(2) 思想＝思想が答える社会の緊張」・§1A-6）。
    /// ・特許＝中核エリートの厚み（<see cref="World.EliteCore"/>＝発明する人＝旧の特許の閾値と同じ量）。
    /// ・株式会社＝立っている工業資本の大きさ（州の最大工業化度＝旧の株式会社の閾値と同じ量・⚠工業化度の廃止〔作者裁定 2026-09-29 の8〕の回で盤面の量へ置き換える）。
    /// ・社会主義・共産主義＝都市労働者の割合×労働者の闘争度＝Σ（工場労働者の人数×闘争度）÷人口（兵士を除く）。
    /// ・帝国主義＝植民地（州の格が植民地）に住む人の割合（設計 §1A-6【推】の「植民地の数」を人で数えた・「列強の順位の争い」は入れていない）。
    /// ・ファシズム＝危機（敗戦か恐慌×左派の脅威＝<see cref="Ideologies.FascismCrisis"/>＝旧の門に掛けていた条件・設計 §1A-6「そのまま適性へ」）×人々の闘争度（Σ人数×闘争度÷人口）
    ///   ＝危機の中で闘う人の割合（思想の4行の強度を同じ単位＝人口の割合にそろえる＝半分点が分野で1本だから・推奨で決めた）。
    ///   ⚠危機だけ（0〜1の国の指数）では負けた戦争1つで強度が 0.85 へ跳び、技術水準 3 の国が1870〜80年代に見つけた（(152) の試走 v1）。
    ///   ★(640) 線 idn：ファシズム＝<see cref="FasStrength"/>（旗 SpreadNeedFascismDecline・<see cref="IdeaBoardOf"/> の盤面の量）。社会主義と共産主義の盤面の旗は (798) で消した（差し戻し・式は上の工場労働者の人数×闘争度）。</summary>
    private static double SpreadNeed(int row, World w, double maxInd, double workerMil, double colonialShare, double popMil, ref double crisis, in IdeaBoard b, Coefficients c)
    {
        double s;
        if (row == PatentRow) s = w.EliteCore;
        else if (row == JointStockRow) s = maxInd;
        else if (row == SocialismRow) s = workerMil;
        else if (row == CommunismRow) s = workerMil;
        else if (row == ImperialismRow) s = colonialShare;
        else if (row == FascismRow) { if (double.IsNaN(crisis)) crisis = Ideologies.FascismCrisis(w); s = c.SpreadNeedFascismDecline > 0 ? FasStrength(crisis, popMil, b) : crisis * popMil; }
        else s = 0;
        return PoliticalQuantities.Pos(s);   // 否定形＝NaN・負は0
    }

    /// <summary>★(640) 線 idn：思想の必要性の盤面の量（<see cref="IdeaBoardOf"/> が兵士でない POP を 1 巡して返す・読むだけ・状態を書かない）。
    /// ・<see cref="WageShare"/>＝賃金だけで暮らす人（工場労働者）の人口比＝Σ(Size×(1−自給率))÷Σ(兵士でない Size)。
    /// ・<see cref="WageUnmet"/>＝その人々のうち暮らしが必要に届かない人数÷人口＝Σ(Size×(1−自給率)×(1−<see cref="Pop.SupplyFill"/>))÷Σ(兵士でない Size)＝社会主義の強度。
    /// ・<see cref="OwnerTake"/>＝工場の稼ぎのうち持ち主の取り分＝ΣIndustrialRentPerMonth÷(ΣIndustrialRentPerMonth＋Σ工場労働者の EarnedPerMonth)（分母が正でなければ 0）。
    /// ・<see cref="Decline"/>＝自国の国力の割合の落ち込み∈[0,1]＝(在庫−今)÷在庫（<see cref="ScoreDecline"/>）。
    /// ・<see cref="WorkerMil"/>・<see cref="ColonialShare"/>・<see cref="PopMil"/>＝今の強度の 3 部品（<see cref="Score"/> の POP の 1 巡と同じ定義＝計器が旧と新を並べるために同じ 1 巡で返す）。
    /// ・<see cref="WorkerSupplyFill"/>＝工場労働者の充足の人数加重平均（計器だけが読む・工場労働者が居なければ NaN）。</summary>
    internal readonly record struct IdeaBoard(double WageShare, double WageUnmet, double OwnerTake, double Decline, double WorkerMil, double ColonialShare, double PopMil, double WorkerSupplyFill);

    /// <summary>共産主義の強度の盤面の量＝持ち主の取り分×賃金だけで暮らす人の人口比（読み手は計器 idn_s だけ・(798) で旗を消した）。</summary>
    internal static double ComStrength(in IdeaBoard b) => b.OwnerTake * b.WageShare;
    /// <summary>ファシズムの強度（旗 <see cref="Coefficients.SpreadNeedFascismDecline"/>）＝危機×人々の闘争度×国力の落ち込み（今の式に落ち込みを掛ける）。</summary>
    internal static double FasStrength(double crisis, double popMil, in IdeaBoard b) => crisis * popMil * b.Decline;

    /// <summary>国力の落ち込み∈[0,1]＝(在庫−今)÷在庫。在庫が正でない（NaN・未初期化・0 以下）か今が無い（NaN・負・単独の世界）は 0（否定形）。今が在庫以上も 0。</summary>
    internal static double ScoreDecline(double standing, double? now)
    {
        if (now is not { } n || !(standing > 0) || !(n >= 0)) return 0.0;
        double d = (standing - n) / standing;
        return PoliticalQuantities.Clamp01(d);
    }

    /// <summary>★(640) <see cref="IdeaBoard"/> を求める（兵士でない POP を 1 巡・<see cref="Score"/> が旗が正のときだけ呼ぶ・計器 <c>idn_s</c> は旗 0 でも同じ関数を呼ぶ）。
    /// 判定の段は四半期末の共有層＝第 3 月の <c>EconomyStep.Run</c> の後なので、<see cref="Pop.SupplyFill"/>・<see cref="Pop.EarnedPerMonth"/>・<see cref="Pop.IndustrialRentPerMonth"/> は第 3 月の累積器の値
    /// （月頭のゼロ化は Run の 1 か所だけ）。分母は <see cref="Score"/> の popSum と同じ（兵士を除く・Size＞0）。</summary>
    internal static IdeaBoard IdeaBoardOf(World w, double? scoreNow)
    {
        double popSum = 0, wage = 0, unmet = 0, fillW = 0, rent = 0, workerEarned = 0, workerMilSum = 0, colonialPop = 0, milSum = 0, workerPop = 0;
        foreach (var r in w.Regions)
        {
            bool colonial = r.Tier == RegionTier.Colonial;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                popSum += p.Size;
                if (p.Militancy > 0) { milSum += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) workerMilSum += p.Size * p.Militancy; }
                if (colonial) colonialPop += p.Size;
                double ir = p.IndustrialRentPerMonth.Value;
                if (ir > 0) rent += ir;
                if (p.ClassId != PopClass.Worker) continue;
                workerPop += p.Size;
                double sub = p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis; sub = PoliticalQuantities.Clamp01(sub);   // ★(854) InstrumentsCearn と同じ三項式：読み込みの直後や Run の前の月も同じ値を読む（欄 SubsistenceShare は [NonSerialized]・Run の頭が毎月書く）
                double dep = p.Size * (1.0 - sub);
                double fill = p.SupplyFill;
                wage += dep; unmet += dep * (1.0 - fill); fillW += p.Size * fill;
                double e = p.EarnedPerMonth.Value;
                if (e > 0) workerEarned += e;
            }
        }
        double wageShare = popSum > 1e-12 ? wage / popSum : 0, wageUnmet = popSum > 1e-12 ? unmet / popSum : 0;
        double pie = rent + workerEarned, take = pie > 1e-12 ? rent / pie : 0;
        return new IdeaBoard(wageShare, wageUnmet, take, ScoreDecline(w.ScoreStanding, scoreNow),
            popSum > 1e-12 ? workerMilSum / popSum : 0, popSum > 1e-12 ? colonialPop / popSum : 0, popSum > 1e-12 ? milSum / popSum : 0,
            workerPop > 1e-12 ? fillW / workerPop : double.NaN);
    }

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
        if (p.OpenedByRow >= 0 && !Known(known, p.OpenedByRow)) return 0.0;   // ★(537) 新世代の門（既定の鋼の小銃・鋼の砲＝転炉鋼の発見まで 0・印の無い生産者は −1 でこの行を通らない）
        int ret = p.RetiredByRow;   // ★(537) 退場の表に残るのは鋳鉄の機関だけ（武器の旧のレシピは並行して作られる＝退場しない）
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
        if (best < 0) { int row = Catalog.DefaultDiscoveryRowOf(gid); best = row < 0 ? 1.0 : Known(r.Discovered, row) ? 1.0 : 0.0; }
        return PoliticalQuantities.Clamp01(best);
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
            if (b < 0) { int row = Catalog.DefaultDiscoveryRowOf(g); b = row < 0 ? 1.0 : Known(known, row) ? 1.0 : 0.0; }
            into[g] = PoliticalQuantities.Clamp01(b);
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

    /// <summary>国の外から見た値（<see cref="MultiNationSim"/> が四半期末に求めて渡す・単独の世界は null）＝行ごとの接触（★(159)：人件費の世界の平均〔(147)〕は削除＝必要性は生み出す物の値段だけを読む）。
    /// ★<paramref name="Loss"/>＝この四半期の陸・海・空の損耗の強さ（添字は <see cref="WarDomain"/>・月の平均・0〜1＝<see cref="World.DiscoveryLossQ"/> を <see cref="Step"/> が直した値・2026-10-01 (153)）
    /// ＝国の中の値だが、記帳は判定で捨てる＝画面と計器が判定と同じ3項を読むためにここへ載せる（<see cref="World.LastDiscoveryView"/>）。
    /// ★<paramref name="Market"/>（(165)→(169)）＝世界の市場（<see cref="WorldMarket"/>＝その四半期に市場が生きている国の平滑化した値段と重み）＝自国に市場の値段が無い財（作らず輸入もしない）を
    /// 「輸入すれば払う値段」＝着値（売り手の値段＋運賃＋関税＝<see cref="WorldMarket.Landed"/>）で読む（<see cref="ReplacedStrength"/>）。画面と計器は平均（<see cref="WorldMarket.Average"/>）。
    /// 判定の段の頭で全ての国の平滑化した値段を進めた直後に求める導出量（状態ではない）。単独の世界は null（他国が無い）。
    /// ★(461) <paramref name="WorldAdvLabor"/>＝世界の高度労働力の雇用の合計（全ての国・全ての財の <see cref="EngineerUse"/> の和・月あたり・<see cref="Discovery.WorldAdvLabor"/>）＝技術力の時計 S の W（<see cref="ClockStage"/>）。
    ///   旗 <see cref="Coefficients.DiscoveryClockWorldPerMonth"/> が正か計器のときだけ <see cref="MultiNationSim"/> が求めて渡す導出量（状態ではない）。無ければ（単独の世界）W は自国の全ての財の和へ倒す。
    ///   ★(640) <paramref name="ScoreNow"/>＝この国の国力の今の値（<see cref="NationalScore.ShareScores"/>＝世界の総量に対する割合の国力）＝ファシズムの必要性の〈落ち込み〉の〈今〉（在庫は <see cref="World.ScoreStanding"/>）。
    ///   旗 <see cref="Coefficients.SpreadNeedFascismDecline"/> が正か計器のときだけ <see cref="MultiNationSim"/> が判定の段の直前に 1 回だけ求めて渡す導出量（状態ではない）。無ければ（単独の世界）落ち込みは 0。</summary>
    public sealed record WorldView(double[]? Contact, double[]? Loss = null, WorldMarket? Market = null, double? WorldAdvLabor = null, double? ScoreNow = null);

    /// <summary>★(169) 世界の市場（判定の段の頭で1回組む導出量・状態ではない＝<see cref="WorldPrices"/>）＝その四半期に国内の市場が生きている国（瞬間の値段の観測がある国）の平滑化した値段と重み（その国の産出＝売っている量）。
    /// ・<see cref="Average"/>＝平滑化した値段を重みで平均した世界の値段（財ごと・どの国にも市場が無い財は <see cref="NoPrice"/>）＝画面と計器が読む（(165) の世界の値段と同じ）。
    /// ・<see cref="Landed"/>＝買い手の国が払う着値＝売り手ごとに 値段×(1＋運賃＋関税) を同じ重みで平均（清算の着値の倍率と同じ組み方＝運賃＝TransportMargin×(1＋距離÷1万km)×運賃革命・
    ///   関税＝買い手の関税の楔）。距離は国の経済の中心どうしの大圏（清算の海路と陸路の距離ではない＝近似）・従量の運賃と危険料と売買方針は読まない（AI の読み・推奨で進めた）。
    ///   旧（(165)）＝運賃と関税を入れない世界の値段（遠い国の輸入の費用を低く見た＝(167) 反証役 vd1#8）。着値は呼ばれた財だけ（市場の死んだ財）求める＝国×財の全ての組は回さない。</summary>
    public sealed class WorldMarket
    {
        private readonly World?[] _w;
        private readonly double[]?[] _s;
        private readonly double[][] _x;
        private readonly double[][] _wt;
        /// <summary>運賃革命の倍率（清算と同じ1本＝<see cref="InternationalMarket.FreightRevolution"/>・1＝革命の前）。</summary>
        public double Freight { get; }
        /// <summary>世界の平滑化した値段（重みで平均・財ごと）。</summary>
        public double[] Average { get; }

        internal WorldMarket(World?[] worlds, double[]?[] smoothed, double[][] inst, double[][] weight, double freight)
        {
            _w = worlds; _s = smoothed; _x = inst; _wt = weight; Freight = freight;
            int ng = Catalog.Goods.Length;
            var num = new double[ng]; var den = new double[ng];
            for (int i = 0; i < _s.Length; i++)
            {
                var s = _s[i]; var x = _x[i]; var wt = _wt[i];
                if (s is null) continue;
                for (int g = 0; g < ng && g < s.Length && g < x.Length && g < wt.Length; g++)
                {
                    if (!(x[g] >= 0) || !(s[g] >= 0) || !(wt[g] > 0)) continue;
                    num[g] += wt[g] * s[g]; den[g] += wt[g];
                }
            }
            var avg = new double[ng];
            for (int g = 0; g < ng; g++) avg[g] = den[g] > 1e-12 ? num[g] / den[g] : NoPrice;
            Average = avg;
        }

        /// <summary>運賃と関税の無い世界の値段だけの市場（番人の盤面＝着値＝与えた値段）。</summary>
        internal static WorldMarket Flat(double[] px)
        {
            var x = new double[px.Length]; var wt = new double[px.Length];
            for (int g = 0; g < px.Length; g++) { x[g] = px[g] >= 0 ? px[g] : NoPrice; wt[g] = 1.0; }
            return new WorldMarket([null], [px], [x], [wt], 1.0);
        }

        /// <summary>買い手 <paramref name="buyer"/> が財 <paramref name="g"/> に払う着値（実質）＝Σ重み×値段×(1＋運賃＋関税)÷Σ重み。売り手が居なければ <see cref="NoPrice"/>。
        /// <paramref name="km"/>＝売り手ごとの距離の作業領域（呼び手が1回の判定の中で使い回す・NaN＝未計算）。運賃と関税の無い市場（<see cref="Flat"/>）の売り手は距離0・運賃0。</summary>
        internal double Landed(int g, World buyer, double duty, double[] km)
        {
            var c = buyer.Coef;
            double num = 0, den = 0;
            for (int i = 0; i < _s.Length; i++)
            {
                var s = _s[i];
                if (s is null || g >= s.Length || g >= _x[i].Length || g >= _wt[i].Length) continue;
                double wt = _wt[i][g];
                if (!(_x[i][g] >= 0) || !(s[g] >= 0) || !(wt > 0)) continue;
                double fac;
                if (_w[i] is { } seller)
                {
                    double d = i < km.Length ? km[i] : double.NaN;
                    if (double.IsNaN(d))
                    {
                        d = ReferenceEquals(seller, buyer) ? 0 : Geo.DistanceKm(buyer.Latitude, buyer.Longitude, seller.Latitude, seller.Longitude);
                        if (i < km.Length) km[i] = d;
                    }
                    fac = 1.0 + c.TransportMargin * (1.0 + d / Diplomacy.DistanceNormKm) * Freight + duty;
                }
                else fac = 1.0;
                num += wt * s[g] * fac; den += wt;
            }
            return den > 1e-12 ? num / den : NoPrice;
        }

        /// <summary>売り手の国の数（距離の作業領域の長さ）。</summary>
        internal int Sellers => _s.Length;
    }

    /// <summary>損耗の記帳の長さ（<see cref="WarDomain"/> の数＝無・陸・海・空）。</summary>
    public const int LossDomains = (int)WarDomain.Air + 1;

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
    public static int[][] CarriersByRow(IEnumerable<World> worlds, bool downstream = false)
    {
        int nr = Catalog.Discoveries.Length;
        var rep = new Producer?[nr];
        // ★(360) 伝わりの道（<paramref name="downstream"/>＝係数 DiscoveryCarrierDownstream が正）：①運び手の財を投入に持つ取引できる資本財（Producer 段の財）の逆引き ②その財を投入に持つ生産者のうち
        //   発見の行に縛られない（行を解禁しなくても使える）ものがあるか。偽のときは求めない（旧と同じ）。
        HashSet<int>?[]? downOf = downstream ? new HashSet<int>?[Catalog.Goods.Length] : null;
        bool[]? freeUse = downstream ? new bool[Catalog.Goods.Length] : null;
        foreach (var w in worlds)
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int row = p.DiscoveryRow;
                    if (row >= 0 && rep[row] is null) rep[row] = p;
                    if (downOf is not null)
                        foreach (var inp in p.Inputs)
                        {
                            if (inp.GoodId < 0 || inp.GoodId >= downOf.Length || inp.GoodId == p.GoodId) continue;
                            if (row < 0) freeUse![inp.GoodId] = true;   // 行に縛られない生産者（地理差の既定のレシピ）もこの財を使う
                            else if (Carries(p.GoodId) && Catalog.Goods[p.GoodId].Tier == GoodTier.Producer) (downOf[inp.GoodId] ??= new HashSet<int>()).Add(p.GoodId);
                        }
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
            if (downOf is not null)
            {
                // ★(360) 1代だけ：上で数えた運び手のうち、使い道が全て発見の行に縛られた財（＝その財を使う口は自分で行を解禁した国にしか開かない＝解禁の前にその財が取引される道が無い。蒸気機関の財は
                //   蒸気の動力・汽船・機関車の 3 つの口だけ）を投入に持つ資本財を、財の番号の昇順で足す（接触の分母の和の順序を決定的にする）。機械や鋼のように行に縛られない使い道を持つ財は、解禁の前から
                //   取引されるので足さない。足した財をさらに辿らない（機関車の先まで広げない）。
                int n0 = tmp.Count;
                var add = new SortedSet<int>();
                for (int i = 0; i < n0; i++)
                    if (!freeUse![tmp[i]] && downOf[tmp[i]] is { } ds) foreach (int g in ds) if (!tmp.Contains(g)) add.Add(g);
                foreach (int g in add) tmp.Add(g);
            }
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
    /// 制度と思想の行の伝わりは接触ではなく到達（<see cref="World.SpreadReach"/>＝単独の世界は0のまま）。
    /// 解禁済みの行と、この国に代表の生産者が居ない行（その国では使えないレシピ）は 0。値段は平滑化した値段（<see cref="World.DiscoveryPrice"/>・作者裁定 Q12）。</summary>
    /// <param name="realPrice">読む実質の値段の差し替え（計器だけ＝discprobe が「値段の跳ねで決まった解禁」を数える・既定 null＝エンジンと同じ値段）。<see cref="NoPrice"/>＝観測なし。</param>
    /// <param name="ratioOut">★(165) 計器だけ＝物を生む行の比の2つの値（代替する物の値段×換算の比・新しいレシピで作る費用〔機関車は鉄道の輸送の費用〕）を行ごとに書く（立たない値は <see cref="NoPrice"/>）。</param>
    public static Terms[] Evaluate(World w, WorldView? view, Coefficients c, bool includeKnown = false, double[]? realPrice = null, (double Replaced, double Cost)[]? ratioOut = null)
        => Score(w, view, c, includeKnown, realPrice, ratioOut);

    /// <summary>★(165) 値段の観測なし（その四半期に国内の市場が死んでいる財＝需要か供給が無い・平滑化した値段がまだ一度も観測していない財）。値段は非負＝負の1本で表す。</summary>
    public const double NoPrice = -1.0;

    /// <summary>瞬間の実質の値段（財ごと・参照価格の単位・死んだ市場は <see cref="NoPrice"/>）＋末尾の1欄＝陸の輸送の値段（<see cref="LandFreightPrice"/>）＝平滑化した値段の入力（計器と画面が読む）。</summary>
    public static double[] InstantRealPrices(World w)
    {
        int ng = Catalog.Goods.Length;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        Aggregate(w, ng, prod, imp, dem, pxd);
        return InstantReal(w, ng, prod, imp, dem, pxd);
    }

    /// <summary>★作者裁定 Q12（設計 §6「製法の割高さを瞬間の価格と平滑化した価格のどちらで読むか」→「平滑化した価格」・2026-10-01 (150)）＝発見が読む財の値段の平滑の速さ
    /// （四半期あたり）＝工場の採算の平滑（<see cref="Coefficients.FactoryMarginSmoothing"/>＝月 0.025≒3年＝「一過性の赤字では潰れない・持続的危機のみ倒産」）を四半期へ直した値
    /// （1−(1−月の率)³）＝発見も一過性の値段の跳ねでは起きず、持続する割高さだけが起こす（同じ物差し・新しい係数を置かない＝推奨で進めた）。</summary>
    public static double PriceSmoothingQ(Coefficients c)
    {
        double a = PoliticalQuantities.Clamp01(c.FactoryMarginSmoothing);
        return 1.0 - (1.0 - a) * (1.0 - a) * (1.0 - a);
    }

    /// <summary>点数の本体（読むだけ）。平滑化した値段（<see cref="World.DiscoveryPrice"/>）は判定の段の頭で <see cref="AdvancePrice"/> が四半期に1歩進める（画面・計器・判定はここで読むだけ）。</summary>
    private static Terms[] Score(World w, WorldView? view, Coefficients c, bool includeKnown, double[]? realPrice = null, (double Replaced, double Cost)[]? ratioOut = null)
        => Score(w, view, c, includeKnown, realPrice, ratioOut, null, out _, out _);

    /// <summary>★(171) 点数の本体＋その判定の値段の帳と国の需要を返す（産地の発見の判定 <see cref="SiteTerms"/> が同じ帳を読む＝発見の表と同じ値段の読み方・1回の判定で帳を2度作らない）。
    /// 稼働中の州が無い国は帳を作らない（null）。</summary>
    /// <param name="engineerOut">★(415) 計器だけ＝行ごとの親の産業の技師の量（月あたり・求めない行は NaN）。旗が0でも書く（census）。</param>
    private static Terms[] Score(World w, WorldView? view, Coefficients c, bool includeKnown, double[]? realPrice, (double Replaced, double Cost)[]? ratioOut,
                                 double[]? engineerOut, out PriceBook? bookOut, out double[]? demOut)
    {
        bookOut = null; demOut = null;
        double[]? contact = view?.Contact;
        double[]? loss = view?.Loss;
        var market = view?.Market;
        var rows = Catalog.Discoveries;
        int nr = rows.Length, ng = Catalog.Goods.Length;
        var res = new Terms[nr];
        // --- 国の集計（財ごと・稼働中の州の和）：国内の産出・輸入・需要・需要で重みを付けた値段・作れるか（門の開いた生産者が居る）---
        var refP = Catalog.RefPrices;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];   // imp＝州の輸入−輸出の和＝国の純輸入（同じ国の州どうしの積み出しは打ち消す）
        var canMake = new bool[ng];
        var rep = new Producer?[nr];
        bool engOn = c.DiscoveryEngineerHalfPerMonth > 0;   // ★(415) 技術力を技師の量から求める旗（半分点を兼ねる・0以下＝今の式）
        bool poolOn = engOn && c.DiscoveryClockWorldPerMonth > 0 && c.DiscoveryClockRampWidth > 0;   // ★(461) 時計を世界の高度労働力の雇用の合計の細ランプから・自国の項を有界の飽和にする旗（技師の形の上に重なる・d＞0 の行は押しを掛けない）
        double[]? eng = engOn || engineerOut is not null ? new double[ng] : null;   // 財ごとの高度労働力の雇用（月あたり）＝旗が正か計器のときだけ
        if (engineerOut is not null) System.Array.Fill(engineerOut, double.NaN);
        var recipes = new List<Producer>?[ng];   // ★(165) 財ごとの、この国で門の開いたレシピ（レシピの名前ごとに最初の生産者）＝市場の値段が無い財を作る費用で読む（PriceBook）
        int nAct = Aggregate(w, ng, prod, imp, dem, pxd);
        double maxInd = 0;   // 州の最大工業化度（株式会社の必要性＝立っている工業資本の大きさ・旧の閾値と同じ量）
        foreach (var r in w.Regions)
        {
            if (r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
            foreach (var p in r.Producers)
            {
                int row = p.DiscoveryRow;
                if (row >= 0 && rep[row] is null) rep[row] = p;
                if (p.GoodId < 0 || p.GoodId >= ng || !(Gate(r, p) > 0)) continue;
                canMake[p.GoodId] = true;
                if (eng is not null) eng[p.GoodId] += EngineerUse(p);   // ★(415) 門の開いた生産者が雇った高度労働力（月あたり）を財ごとに足す
                var lr = recipes[p.GoodId] ??= new List<Producer>(2);
                bool seen = false; foreach (var q in lr) if (q.RecipeName == p.RecipeName) { seen = true; break; }
                if (!seen) lr.Add(p);
            }
        }
        if (nAct == 0) return res;
        double wClock = double.NaN;   // ★(461) 時計の W＝世界の高度労働力の雇用の合計（全ての行・国で共通・旗が正のときだけ）。単独の世界は自国の全ての財の和（⚠ W0 は世界の和で較正した値＝単独の世界では d＞0 の行が開きにくい〈推測〉・★(466) 製品の試合〔App と Runner の selfplay・play〕は MultiNationSim を通る＝単独の世界は Runner の診断の動詞とテストだけ）
        if (poolOn)
        {
            if (view?.WorldAdvLabor is { } wv) wClock = wv;
            else { wClock = 0; foreach (double e in eng!) wClock += e; }
        }
        // 値段＝平滑化した実質の値段（作者裁定 Q12・World.DiscoveryPrice）。瞬間の値（InstantReal）は需要で重みを付けた州の値段÷物価水準・死んだ市場は観測なし・末尾の1欄は陸の輸送の値段。
        double[] real;
        if (realPrice is { } rpx && rpx.Length >= ng) real = rpx;
        else real = w.DiscoveryPrice is { } dpx && dpx.Length >= ng ? dpx : InstantReal(w, ng, prod, imp, dem, pxd);   // 平滑の前（ロード直後・開幕の画面）は瞬間の値
        // ★(165) 今の四半期に国内の市場が生きている財（自国の平滑化した値段を読む）・新しいレシピの自己費用の物価（参照価格の単位の自己費用→実質＝自己費用の物価水準÷物価水準）・鉄道の輸送の費用。
        var alive = new bool[ng];
        for (int g = 0; g < ng; g++) alive[g] = Alive(dem[g], prod[g], imp[g]);
        double plv = w.NominalOf(1.0);
        double selfScale = plv > 0 ? w.CostPriceLevel / plv : 1.0;
        if (!(selfScale > 0)) selfScale = 1.0;   // 否定形＝NaN は平価へ
        double railCost = double.NaN;   // 機関車の行を見るときだけ求める
        // ★(169) 買い手の関税の楔（清算の着値と同じ＝関税率の正の分・穀物法は主食の連鎖だけ＝発見の行の財には掛けない）＝市場の値段が無い財の着値に足す。
        double duty = market is null ? 0 : System.Math.Max(0, InternationalMarket.TariffRate(w, w.Coef));
        var book = new PriceBook(real, alive, market, recipes, selfScale, w, duty);
        bookOut = book; demOut = dem;
        // 軍事の圧力（新兵器）＝その分野の脅威（相手の強さ）が自国の強さとの和に占める割合∈[0,1)＝理想の軍隊の形（ArmyShape）の陸と海の必要量 ÷（必要量＋自国の陸軍力・海軍力）。空は陸の膠着（設計 §1A-4）。
        //   ⚠比（相手÷自国）のままでは弱い国が数十倍になり必要性が70へ張り付く（1種の試走でダホメが1831年に機関銃・コンゴが1837年に弩級戦艦）＝割合にすれば最大は 70÷(1＋半分点)。
        //   ★＋交戦中のその分野の損耗（2026-10-01 (153)・設計 §1A-4 (4a) の第2項・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）＝この四半期の損耗の強さ（<see cref="WorldView.Loss"/>＝
        //   その月に失った割合÷総力戦の月の上限の割合の平均＝0〜1＝脅威の割合と同じ「最大を1とした割合」）。陸＝戦死（敵の空から受けた分を除く）・海＝艦隊摩耗・空＝制空劣勢で増えた戦死。
        //   空の脅威は陸の割合のまま（理想の軍隊の形に空の必要量が無い＝AI の読み・(147) の⑤）＝空の損耗だけが空の分野の量。
        //   ★(194) 陸の自国の強さは陸軍の大きさ（<see cref="ArmyShape.LandPotential"/>＝兵の頭数×質−艦に乗る兵・即応度なし）＝陸の必要量（相手の陸軍を同じ1本で読む）と同じ物差し
        //   （旧＝自国の LandPower〔即応度つき〕＝相手の LandPower で読んだ陸の必要量と対だった・(194) で陸の必要量を即応度なしへ移したので自国の側も移す＝分子と分母を同じ規則で）。
        double landPress = 0, seaPress = 0;
        if (w.SelfPower is { } sp)
        {
            landPress = Share(sp.NeedLand, ArmyShape.LandPotential(sp));
            seaPress = Share(sp.NeedSea, sp.NavalPower);
        }
        // 制度と思想の必要性の量（<see cref="SpreadNeed"/>）＝工場労働者の人数×闘争度・人々の闘争度・植民地の州に住む人。兵士は数えない。
        //   ★(159)：新製品の必要性（旧＝満足した消費者の厚み Σ人数×満足度÷人口＝作者裁定 Q13）は代替する物の値段へ（作者 2026-10-02・<see cref="ReplacedStrength"/>）。
        double popSum = 0, workerMilSum = 0, colonialPop = 0, milSum = 0;
        foreach (var r in w.Regions)
        {
            bool colonial = r.Tier == RegionTier.Colonial;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                popSum += p.Size;
                if (p.Militancy > 0) { milSum += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) workerMilSum += p.Size * p.Militancy; }
                if (colonial) colonialPop += p.Size;
            }
        }
        double workerMil = popSum > 1e-12 ? workerMilSum / popSum : 0, colonialShare = popSum > 1e-12 ? colonialPop / popSum : 0, popMil = popSum > 1e-12 ? milSum / popSum : 0;
        double crisis = double.NaN;   // ファシズムの必要性＝危機（国の思想の集計の1巡が要る＝その行を見るときだけ求める）
        // ★(640) 線 idn：思想の必要性の盤面の量（旗 SpreadNeedFascismDecline が正のときだけ POP の 1 巡を足す・旗 0 は何も増えない）
        IdeaBoard board = c.SpreadNeedFascismDecline > 0 ? IdeaBoardOf(w, view?.ScoreNow) : default;
        double spreadLit = double.NaN;   // ★(621) 制度と思想の技術の比の識字（旗 SpreadLiteracyFull が正のとき・d＞0 の行を見るときだけ1度求める）
        var reach = w.SpreadReach;
        for (int row = 0; row < nr; row++)
        {
            if (!includeKnown && Known(w.Discovered, row)) continue;   // includeKnown＝計器だけ（解禁済みの行の項も見る）
            {   // ★(336) 系統×段階：前提の行がまだの国はこの行を判定しない（点数0・伝わりも読まない。旧の旗 DiscoveryTierGate は (344) で既定1→2026-10-18 に焼き込み）
                int after = Catalog.DiscoveryAfter[row];
                if (after >= 0 && !Known(w.Discovered, after)) continue;
            }
            var d = rows[row];
            bool spread = IsSpread(d.Field);   // 制度と思想＝財と投入を持たない（技術の押しは1・伝わりは到達）
            var rp = rep[row];
            if (!spread && rp is null) continue;   // この国に代表の生産者が居ない行（その国では使えないレシピ）
            // 技術力＝70×min(1, 技術水準÷難易度)×技術の押し（設計 §1A-3＝投入のうち自国で作っている割合・参照価格で重み付け・制度と思想は1）
            //   ★(415) 旗 DiscoveryEngineerHalfPerMonth が正のとき、製法と新製品と新兵器の行は技術水準を読まない（下の技師の比 EngineerFactor）。制度と思想の行は今のまま（親の産業が定義できない・事前登録 [3-3]）。★(621)(627) ただし制度と思想の行（特許以外）は下で TechRatioOf の値を本国の識字÷SpreadLiteracyFull（0〜1）で上書きする（既定 0.5）。
            double ratio = TechRatioOf(w, row, spread, engOn, c);
            if (spread && c.SpreadLiteracyFull.Value > 0 && d.Decade > Catalog.DiscoveryBaseDecade)   // ★(621) 線 inst：制度と思想の技術の比を本国の識字÷基準へ（特許〔d≦0〕は 1 のまま・NaN の係数は偽＝比 1・旗 TechThresholdOff は (824) で焼き込み）
            {
                if (double.IsNaN(spreadLit)) spreadLit = PoliticalQuantities.AvgLiteracy(w, coreOnly: true);
                ratio = SpreadLiteracyRatio(spreadLit, c.SpreadLiteracyFull.Value);
            }
            if (spread)
            {
                double sNeed = SpreadNeed(row, w, maxInd, workerMil, colonialShare, popMil, ref crisis, in board, c);
                double hNeed = NeedHalf(d.Field, c);
                int slot = SpreadSlot(row);
                double xr = reach is not null && slot >= 0 && slot < reach.Length ? reach[slot] : 0;
                res[row] = new Terms(TermMax * ratio, Saturate(sNeed, hNeed), Saturate(xr, c.DiscoveryContactHalf));
                continue;
            }
            double wSum = 0, wDom = 0;
            foreach (var inp in rp!.Inputs)
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
            double aRow = eng is not null ? ParentEngineers(row, rp!, eng, poolOn && c.DiscoveryClockMotorParent > 0) : double.NaN;   // ★(461) 電動機の親の産業は別の旗（既定 0＝今の規則）。★(415) 親の産業の技師の量（旗が正か計器のときだけ求める）
            if (engineerOut is not null && row < engineerOut.Length) engineerOut[row] = aRow;
            double tech = poolOn && d.Decade > Catalog.DiscoveryBaseDecade ? TermMax * ClockStage(row, wClock, c) * OwnCapacity(row, aRow, c)   // ★(461) 世界の時計 S（細ランプ・全ての行・国で共通の W）× 自国の最低限の産業 C（押しは A の中に入っている）。d≦0 の行は下の今の式
                                                                            : TermMax * (engOn ? EngineerFactor(row, aRow, c) : ratio) * push;
            // 必要性＝70×強度÷(強度＋分野の半分点)（設計 §1A-4）
            double s = 0, half = 1;
            switch (d.Field)
            {
                case DiscoveryField.Process:
                case DiscoveryField.Product:
                    // ★物を生む行（2026-10-02 (159)・(165) で比べる相手を直した）＝代替する物（製法＝同じ財・新製品＝代替する既存の財・機関車＝陸の輸送）の平滑化した値段
                    //   ÷その発見が出す物を新しいレシピで作る費用（同じ国・同じ時期の平滑化した投入の値段×量＋規定の自己費用）の対数（<see cref="ReplacedStrength"/>）。
                    //   作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する」・
                    //   比べる相手＝作者 2026-10-02「2でよい」（代替する物の値段÷その発見が出す物の値段・固定の参照価格とは比べない）。
                    //   ⚠旧（(147)〜(154)）＝製法は今の方法の投入の費用÷新しいレシピの投入の費用が参照の比を超えた分・労働を代替する発明（蒸気機関・電動機）は人件費の世界比
                    //     ×参照の資本と燃料÷自国・新製品は満足した消費者の厚み＝背景（人件費・豊かさ）を読んでいた。(159)＝代替する物の値段÷参照価格（参照価格と比べた）。
                    if (d.Replaces == Catalog.LandFreight && double.IsNaN(railCost)) railCost = RailCostIndex(w, book, rp!, market?.Freight ?? 1.0);
                    s = ReplacedStrength(row, rp!, book, dem, railCost, out double vRep, out double vCost);
                    if (ratioOut is not null && row < ratioOut.Length) ratioOut[row] = (vRep, vCost);
                    half = NeedHalf(d.Field, c);
                    break;
                case DiscoveryField.Weapon:
                {
                    int dom = (int)d.Domain;
                    double lossI = loss is not null && dom < loss.Length && loss[dom] > 0 ? loss[dom] : 0;   // 否定形＝NaN は0（損耗なし）の側へ
                    s = (d.Domain == WarDomain.Sea ? seaPress : landPress) + lossI;
                    half = c.DiscoveryNeedHalfWeapon;
                    break;
                }
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

    /// <summary>分野の必要性の半分点（分野ごとに1本＝設計 §1A-2）。計器（Runner の discprobe）も同じ1本を読む。</summary>
    public static double NeedHalf(DiscoveryField f, Coefficients c) => f switch
    {
        DiscoveryField.Process => c.DiscoveryNeedHalfProcess, DiscoveryField.Weapon => c.DiscoveryNeedHalfWeapon,
        DiscoveryField.Institution => c.DiscoveryNeedHalfInstitution, DiscoveryField.Idea => c.DiscoveryNeedHalfIdea,
        _ => c.DiscoveryNeedHalfProduct,
    };
    /// <summary>国の財ごとの集計（稼働中の州の和）＝産出・純輸入（州の輸入−輸出の和）・需要・需要で重みを付けた値段の和。稼働中の州の数を返す。</summary>
    private static int Aggregate(World w, int ng, double[] prod, double[] imp, double[] dem, double[] pxd)
    {
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
                pxd[g] += m.Price * m.LastDemand;
            }
        }
        return nAct;
    }

    /// <summary>瞬間の実質の値段（財ごと・金建ての値段÷物価水準）＝需要で重みを付けた州の値段÷物価水準＋末尾の1欄＝陸の輸送の値段（<see cref="LandFreightPrice"/>・普通の陸の運賃＝1）。
    /// ⚠死んだ市場（国内に需要が無いか、国内に供給〔産出＋純輸入の正の分〕が無い財）の値段は凍った値（需要の無い財は床・供給の無い財は天井へ張り付く）
    ///   ＝物差しにならない（1種の試走で、蒸気機関の財が売れない国ほど「蒸気機関が安い」＝人件費が割高に見えた）＝観測なし（<see cref="NoPrice"/>）。
    ///   ★(165)：旧（(159)）＝死んだ市場は参照価格（その財が普通に出回ったときの値段）で読み、平滑化した値段は死んでいる間に参照価格へ寄った
    ///   ＝作者 2026-10-02「ゲーム中に変化しない参照価格、という実装はあまり良くないように感じる」＝発見の必要性から参照価格を外した（平滑化した値段は死んでいる間は動かない＝<see cref="AdvancePrice"/>）。
    ///   否定形＝NaN も観測なしへ倒す（平滑の状態へ NaN を入れない）。</summary>
    private static double[] InstantReal(World w, int ng, double[] prod, double[] imp, double[] dem, double[] pxd)
    {
        double pl = w.NominalOf(1.0);
        var x = new double[ng + 1];
        for (int g = 0; g < ng; g++)
        {
            double v = pl > 0 && Alive(dem[g], prod[g], imp[g]) ? pxd[g] / dem[g] / pl : NoPrice;
            x[g] = v >= 0 ? v : NoPrice;
        }
        x[ng] = LandFreightPrice(w);
        return x;
    }

    /// <summary>生きた市場（国内に需要と供給〔産出＋純輸入の正の分〕の両方がある財）＝値段が物差しになる（瞬間の値段・割高さ・世界の値段の重みが同じ1本を読む）。</summary>
    private static bool Alive(double dem, double prod, double imp) => dem > 1e-9 && prod + (imp > 0 ? imp : 0) > 1e-9;

    /// <summary>平滑化した値段を1歩（四半期に1回・判定の段の頭だけ＝<see cref="MultiNationSim"/> は全ての国を先に進めてから判定する・単独の世界は <see cref="Simulation"/> の四半期末）
    /// ＝ s ← s ＋ 率×（瞬間 − s）。最初（null・建国した国）は瞬間の値で始める＝立ち上がりの跳躍なし。書き手はこの1か所だけ（画面と計器は読むだけ）。
    /// ★(165)：観測なし（死んだ市場＝<see cref="NoPrice"/>）の財は動かさない（最後に生きていた値を保つ・一度も観測していない財は <see cref="NoPrice"/> のまま＝最初の観測で瞬間の値から始める）
    /// ＝旧（(159)）は参照価格へ寄せた。戻り値＝この四半期の瞬間の値段と国の産出（<see cref="WorldPrices"/> の重み・多国の判定の段が使う）。
    /// ★(169) 重み＝国内の産出（その財を実際に売っている国＝輸出か国内の売りに回る自国の生産）＝開幕の一律の在庫を少しの需要で捌くだけの国（産出≈0）は重み≈0
    ///   （(166) の根＝外へ売らない薄い市場の値段が世界の値段になった＝(167) の申し送りの腕・推奨で進めた）。旧（(165)〜(167)）＝国の需要（買う国の値段で平均した）。</summary>
    /// <remarks>★(320) 値段の履歴を持たない国（<see cref="World.DiscoveryPrice"/> が null）の最初の値は、係数 <see cref="Coefficients.DiscoveryPriceInitFromCost"/> が正なら自然価格
    /// （<see cref="NaturalPrior"/>）から始めて最初の観測を他の四半期と同じ率で入れる（観測の無い財・自然価格の立たない財は旧のまま＝瞬間の値）。既定1（(388) 採用）。0は旧＝最初の観測の瞬間の値。</remarks>
    internal static (double[] Inst, double[] Sold) AdvancePrice(World w, Coefficients c)
    {
        w.Cost[CostLedger.DiscoveryPriceNations]++;   // 計算量の台帳（診断）：平滑化した値段の歩は国×四半期（(186)＝数えていなかった処理を数える）
        int ng = Catalog.Goods.Length;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        Aggregate(w, ng, prod, imp, dem, pxd);
        var x = InstantReal(w, ng, prod, imp, dem, pxd);
        var s = w.DiscoveryPrice;
        if (s is null || s.Length != x.Length)
        {
            var s0 = (double[])x.Clone();
            if (c.DiscoveryPriceInitFromCost > 0)
            {
                var nat = NaturalPrior(w, ng);
                double a0 = PriceSmoothingQ(c);
                for (int g = 0; g < x.Length; g++)
                    if (x[g] >= 0 && nat[g] >= 0) s0[g] = nat[g] + a0 * (x[g] - nat[g]);   // 自然価格から、最初の観測を他の四半期と同じ率で（否定形＝NaN の観測・自然価格は旧のまま）
            }
            w.DiscoveryPrice = s0;
            return (x, prod);
        }
        double a = PriceSmoothingQ(c);
        for (int g = 0; g < x.Length; g++)
        {
            if (!(x[g] >= 0)) continue;                                   // 観測なし＝動かさない（否定形＝NaN も）
            s[g] = s[g] >= 0 ? PoliticalQuantities.Toward(s[g], x[g], a) : x[g];           // 最初の観測は瞬間の値から
        }
        return (x, prod);
    }

    /// <summary>★(320) 自然価格（財ごと・係数 <see cref="Coefficients.DiscoveryPriceInitFromCost"/> が正のとき、履歴の無い国の平滑化した値段の最初の値）
    /// ＝その国でその財を作る門の開いたレシピ（<see cref="PriceBook"/> と同じ集合・レシピの名前ごとに最初の生産者）のうち最も安いものの費用（自己費用×自己費用の物価水準÷物価水準＋Σ投入の量×投入の自然価格）。
    /// 全ての財を「市場が死んでいる」として <see cref="PriceBook.Of"/> に読ませる＝市場の値段も着値も使わず、レシピと自己費用だけから出る（参照価格を読まない）。作れない財・投入の輪・作れない投入を要する財は
    /// <see cref="NoPrice"/>（先入観なし）。導出量（状態ではない）・履歴の無い国の最初の1回だけ求める。</summary>
    internal static double[] NaturalPrior(World w, int ng)
    {
        var recipes = new List<Producer>?[ng];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= ng || !(Gate(r, p) > 0)) continue;
                var lr = recipes[p.GoodId] ??= new List<Producer>(2);
                bool seen = false; foreach (var q in lr) if (q.RecipeName == p.RecipeName) { seen = true; break; }
                if (!seen) lr.Add(p);
            }
        }
        double plv = w.NominalOf(1.0);
        double selfScale = plv > 0 ? w.CostPriceLevel / plv : 1.0;
        if (!(selfScale > 0)) selfScale = 1.0;   // 否定形＝NaN は平価へ
        var none = new double[ng + 1]; System.Array.Fill(none, NoPrice);
        var book = new PriceBook(none, new bool[ng], null, recipes, selfScale, w, 0);
        var res = new double[ng + 1];
        for (int g = 0; g < ng; g++) { double v = book.Of(g); res[g] = v >= 0 ? v : NoPrice; }
        res[ng] = NoPrice;
        return res;
    }

    /// <summary>★(165) 世界の平滑化した実質の値段（財ごと）＝その四半期に国内の市場が生きている国（瞬間の値段の観測がある国）の平滑化した値段を、★(169) その国の産出（売っている量・旧＝需要）で重み付けた平均。
    /// どの国にも市場が無い財は <see cref="NoPrice"/>。判定の段の頭（全ての国の <see cref="AdvancePrice"/> の後）で求める導出量＝状態ではない（<see cref="WorldView.Market"/>）。
    /// ★(169) 世界の市場（<see cref="WorldMarket"/>）として返す＝平均（<see cref="WorldMarket.Average"/>）と買い手ごとの着値（<see cref="WorldMarket.Landed"/>・運賃と関税）。
    /// <paramref name="freight"/>＝運賃革命の倍率（清算と同じ1本＝<see cref="InternationalMarket.FreightRevolution"/>）。
    /// ⚠(166) 計器（種1）＝外へ売らない薄い市場（開幕の一律の在庫を少しの需要で捌く小国＝2四半期目のデンマークの蒸気機関の財〔産出 1.5e-8・需要 0.048・在庫 575・輸出0〕の 1.69＝参照の 0.44倍）が
    ///   世界の値段になり、清・露・仏・日の蒸気の動力の費用が自国で作る費用の 1/10 前後へ落ちて、主要国の全てが開幕の年に蒸気機関を見つけた。
    ///   重みを国の純輸出にする腕（D1＝外へ売っている国の値段だけ＝輸入すれば払う値段）は、清の蒸気機関を 1784→1793 年へ遅らせたが、合わせた木の10種で常勝の疑い（米 6/10）＝入れていない
    ///   （パッチ measurements/m165_20261002_D1.diff・実装ログ (166)）。★(169) 重み＝国内の産出（売っている国＝純輸出でなく輸出か国内の売り）＝腕P（実装ログ (169)）。</summary>
    internal static WorldMarket WorldPrices(IReadOnlyList<World> worlds, IReadOnlyList<(double[] Inst, double[] Sold)> obs, double freight = 1.0)
    {
        int n = System.Math.Min(worlds.Count, obs.Count);
        var ws = new World?[n]; var ss = new double[]?[n]; var xs = new double[n][]; var wts = new double[n][];
        for (int i = 0; i < n; i++) { ws[i] = worlds[i]; ss[i] = worlds[i].DiscoveryPrice; xs[i] = obs[i].Inst; wts[i] = obs[i].Sold; }
        return new WorldMarket(ws, ss, xs, wts, freight);
    }

    /// <summary>項の飽和＝70×強度÷(強度＋半分点)（発見の表と産地の発見が共有）。否定形＝NaN・負は0。</summary>
    private static double Saturate(double s, double half) => s > 0 && half > 0 ? TermMax * s / (s + half) : 0;

    /// <summary>★物を生む行（製法・新製品）の必要性の強度（2026-10-02 (159)・(165) で比べる相手を直した）。
    /// 作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する。
    /// 例えば蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＋比べる相手＝作者 2026-10-02「2でよい」
    /// （必要性の強度＝代替する物の値段÷その発見が出す物の値段〔同じ時期・同じ国の平滑化した投入の値段から新しいレシピで作る費用〕・固定の参照価格とは比べない）。
    /// ＝ ln（代替する物の平滑化した値段×換算の比 ÷ 新しいレシピで作る費用）・1以下（新しいレシピの方が高いか同じ）は0。
    /// ・代替する物（<see cref="DiscoveryRow.Replaces"/>）＝製法はその行の財そのもの（今の方法で作る値段＝その財の平滑化した値段）・新製品は代替する既存の財・
    ///   蒸気機関と電動機の行は労働力（動力）の財＝人と家畜の動力の値段（アレン）・機関車は陸の輸送の値段（<see cref="LandFreightPrice"/>＝普通の陸の運賃＝1）。
    /// ・新しいレシピで作る費用＝代表の生産者（その国にある同じ財・同じレシピの生産者）の自己費用（<see cref="Producer.MarginalCost"/>＝規定の付加価値・参照価格の単位）×自己費用の物価水準÷物価水準
    ///   ＋Σ投入の量（<see cref="Input.QtyPerOutput"/>）×その投入の平滑化した値段（労働は労働力の財の値段）＝工場の採算の限界費用（<see cref="EconomyStep.MineralSelfCost"/>＋<see cref="EconomyStep.InputCostPerUnit"/>）と同じ組み方
    ///   （州の物価の乗数と電化・機械化の労働の節約は読まない＝国の値段の1本）。機関車＝鉄道の輸送の費用（<see cref="RailCostIndex"/>＝運転の費用〔清算の陸路の費用の鉄道÷馬〕＋線路と機関車の資本の費用）。
    ///   ★(169) 蒸気機関と電動機の行（動力＝労働力の財のレシピ）も自己費用を数える（製法・動力・機関車の行が同じ規則〔新製品の14行は換算の比＝参照価格の比・産地の行は (171) の物差し＝(172) 反証役 vd3#8〕・自己費用を数える形は AI の推奨＝作者の承認は未確認〔作者への確認①・vd3#3〕・旧 (165)〜(167) は数えなかった＝(167) 反証役 vd1#2）。
    /// ・値段の読み方（その国に市場の値段が無い財の扱い＝理由で決めた・推奨で進めた・<see cref="PriceBook"/>）：
    ///   ①代替する物＝国内に需要が無い財は0（使っていない物の代わりは要らない）。
    ///   ②国内の市場が今の四半期に生きている財（代替する物・投入）＝自国の平滑化した値段。
    ///   ③死んでいる財＝手に入れる安い方＝その国の着値（★(169) 世界の売り手の値段＋運賃＋関税＝<see cref="WorldMarket.Landed"/>・取引できる財）とこの国で作る費用（門の開いたレシピ・投入へ同じ読み方を繰り返す）の小さい方
    ///     （⚠開幕は蒸気機関の財の市場が世界のどこにも無い＝作る費用で読む）。どちらも無い（取引できず作れない電力など）＝費用が立たない＝0（作れない投入を要するレシピは必要とされない）。
    /// ・換算の比（新製品だけ・<see cref="ReplacedUnits"/>）＝新しい財1単位が置き換える代替する物の量（固定のデータ）。製法・蒸気機関・電動機・機関車は1（同じ物）。
    ///   ⚠(167) 新製品の行は換算の比（参照価格の比）が必要性の0点を決める＝参照価格と比べるのと同じ働き（作者への確認②）。
    ///   ⚠(169) 物の単位の換算の腕は入れていない（10種で電力・送電網・冷蔵・ゴム・自動車がどの国でも見つからない＝<see cref="ReplacedUnits"/> の注記）。
    /// ・背景・因果（人件費の世界比・豊かさ・脅威・軍）と他国の値段（自国に市場がある財）は読まない。対数＝値段の比は比で読む（(147) と同じ）。否定形＝NaN は0。</summary>
    private static double ReplacedStrength(int row, Producer rep, PriceBook book, double[] dem, double railCost, out double v, out double cost)
    {
        var d = Catalog.Discoveries[row];
        int ng = Catalog.Goods.Length;
        v = NoPrice; cost = NoPrice;
        if (d.Replaces == Catalog.LandFreight)
        {
            v = book.LandFreight;   // 陸の輸送の値段（平滑化した値段の末尾の1欄）
            cost = railCost;
        }
        else
        {
            int g = d.Replaces == Catalog.ReplacesOwn ? d.Good : d.Replaces;
            if (g < 0 || g >= ng || g >= dem.Length || !(dem[g] > 1e-9)) return 0;   // 使っていない物の代わりは要らない
            double pg = book.Of(g);
            v = pg < 0 ? NoPrice : pg * ReplacedUnits[row];   // ★(613) 観測なし（NoPrice＝負）に換算の比を掛けない（旧は -1×比 が計器に出た）。下の !(v>0) の返りは同じ＝挙動は変えない
            // ★(169) 動力（労働力の財）のレシピ（蒸気機関・電動機）も他の行と同じ規則＝自己費用（規定の付加価値＝人の働き）を数える（定義の本文「新しいレシピで作る費用」・分子の動力の値段も同じ自己費用を払う生産者の値段）。
            //   旧（(165)〜(167)）＝動力の2行だけ自己費用を数えなかった（AI が出した例「石炭と機械の蒸気の動力の値段」の字義）＝分子と分母が別の規則（(167) 反証役 vd1#2）。
            cost = book.Cost(rep);
        }
        if (!(v > 0) || !(cost > 0)) return 0;   // 値段か費用が立たない（観測なし＝NoPrice は負）
        double ratio = v / cost;
        return ratio > 1.0 ? System.Math.Log(ratio) : 0;   // 否定形＝NaN は0
    }

    /// <summary>★(165) 値段の帳（1回の判定の中だけの作業領域・状態ではない）＝その国が財に払う平滑化した実質の値段（<see cref="ReplacedStrength"/> の値段の読み方）：
    /// ①国内の市場が今の四半期に生きている財＝自国の平滑化した値段。
    /// ②死んでいる財（作らず輸入もしない・需要が無い）＝手に入れる安い方＝世界の平滑化した値段（取引できる財＝輸入すれば払う値段）と、この国で門の開いたレシピで作る費用
    ///   （自己費用×自己費用の物価水準÷物価水準＋Σ投入×その投入の値段〔同じ読み方を投入へ繰り返す〕＝市場の値段が無い財は作る費用で値打ちを測る）の小さい方。
    ///   どちらも無い（取引できず作れない・世界のどこにも市場が無く作れない）＝<see cref="NoPrice"/>＝費用が立たない。投入の輪（自分の投入へ戻る）は <see cref="NoPrice"/>。</summary>
    private sealed class PriceBook(double[] real, bool[] alive, WorldMarket? market, List<Producer>?[] recipes, double selfScale, World buyer, double duty)
    {
        private readonly double[] _memo = new double[alive.Length];
        private double[]? _km;   // 売り手の国ごとの距離（着値の作業領域・NaN＝未計算）
        private readonly byte[] _state = new byte[alive.Length];   // 0＝未・1＝計算中（輪の守り）・2＝済
        public double LandFreight => alive.Length < real.Length ? real[alive.Length] : NoPrice;

        public double Of(int g)
        {
            if (g < 0 || g >= alive.Length) return NoPrice;
            if (alive[g] && g < real.Length && real[g] >= 0) return real[g];
            if (_state[g] == 2) return _memo[g];
            if (_state[g] == 1) return NoPrice;   // 投入の輪
            _state[g] = 1;
            double best = NoPrice;
            bool trade = Catalog.Goods[g].Tier != GoodTier.Service && !Catalog.Goods[g].NonTradeable;
            if (trade && market is not null)
            {
                if (_km is null) { _km = new double[market.Sellers]; System.Array.Fill(_km, double.NaN); }
                double lp = market.Landed(g, buyer, duty, _km);   // ★(169) 着値＝売り手の値段＋運賃＋関税（旧＝運賃と関税の無い世界の値段）
                if (lp >= 0) best = lp;
            }
            if (recipes[g] is { } lr)
                foreach (var p in lr)
                {
                    double c = Cost(p);
                    if (c >= 0 && (best < 0 || c < best)) best = c;
                }
            _memo[g] = best; _state[g] = 2;
            return best;
        }

        /// <summary>そのレシピで作る費用（実質）＝自己費用×自己費用の物価水準÷物価水準＋Σ投入の量×投入の値段。投入の値段が立たなければ <see cref="NoPrice"/>。
        /// ★(169) 製法・動力・機関車の行が同じ規則（旧＝動力のレシピだけ自己費用を外す引数があった・自己費用を数える形は AI の推奨＝作者の承認は未確認・(172) 反証役 vd3#3・vd3#8）。</summary>
        public double Cost(Producer p)
        {
            double cost = p.MarginalCost * selfScale;
            foreach (var inp in p.Inputs)
            {
                if (!(inp.QtyPerOutput > 0)) continue;
                double px = Of(inp.GoodId);
                if (!(px >= 0)) return NoPrice;   // 作れない投入
                cost += inp.QtyPerOutput * px;
            }
            return cost >= 0 ? cost : NoPrice;
        }
    }

    /// <summary>★換算の比（(165)・新製品の行だけ）＝新しい財1単位が置き換える代替する物の量＝普通に出回ったときに同じ値打ちの量（新しい財の参照価格÷代替する物の参照価格）。
    /// 2つの財には共通の物の単位が無い＝値打ちで単位をそろえる固定のデータ（「高いか」の物差しではない＝高いかは今の代替する物の値段と今の新しいレシピの費用の比で読む）。
    /// 製法・蒸気機関・電動機・機関車・新兵器・制度・思想は1。⚠固定の換算に参照価格を使うのは AI の読み（作者への確認）。
    /// ⚠(167) 反証役の指摘＝新製品の強度＝ln((代替する物の値段÷その参照価格)÷(新しいレシピの費用÷新しい財の参照価格))＝必要性の0点は参照価格の比が決める
    ///   ＝働きは参照価格と比べるのと同じ＝作者 2026-10-02「ゲーム中に変化しない参照価格、という実装はあまり良くないように感じる」から外れうる（作者への確認②・代わりの案＝物の単位の換算をデータの欄に持つ）。
    /// ⚠(169) 物の単位の換算をデータの欄に持つ腕（同じ用途の枠の量の比・同じ人の同じ必要量の比・史実の働きの比＝AI の推奨）は10種で判定の線の内側（exact 51.3・near 86.1）だが、
    ///   電力（石炭火力・水力発電）・送電網・冷蔵・加硫ゴムと合成ゴム・自動車・電化製品がどの種のどの国でも見つからない（r167 は 石炭火力 103・送電網 172・冷蔵 116 件の解禁）
    ///   ＝AI の対応づけ（石炭火力と送電網→ガス・電化製品→家事奉仕）では、物の単位で比べると電気の明かりは同じ明かりのガスより安くない（史実の初期の電灯も同じ）＝必要性が立たない
    ///   ＝1950 年まで電気の無い世界＝史実と食い違う＝入れていない（作者への確認・パッチ measurements/m168_20261002_units.diff・実装ログ (169)）。</summary>
    public static readonly double[] ReplacedUnits = Catalog.Discoveries.Select(d =>
        d.Field == DiscoveryField.Product && d.Replaces >= 0 && d.Good >= 0 && Catalog.RefPrices[d.Replaces] > 0
            ? Catalog.RefPrices[d.Good] / Catalog.RefPrices[d.Replaces] : 1.0).ToArray();

    /// <summary>★鉄道の輸送の費用（(165)・機関車の行の比べる相手）＝陸の運賃と同じ単位（普通の馬の陸の運賃＝1）＝新しいレシピ（鉄道）で運ぶ費用＝運転の費用＋線路と機関車の資本の費用：
    /// ・運転の費用＝清算の陸路の費用の鉄道÷馬（BilateralMarket の陸路の費用＝LandSeaCostRatio×地形の係数・鉄道の後は×<see cref="Coefficients.RailCostFraction"/>・地形は馬→鉄道の係数・水路の州は水運のまま）。
    /// ・★(169) 資本の費用（AI の推奨・作者の承認は未確認）＝容量1あたりの月の減耗×財の値段（<see cref="Transport"/> が積む形＝線路と機関車を同じ単位数ずつ買い1単位が <see cref="Coefficients.TransportCapPerUnit"/> の容量・
    ///   月に <see cref="Coefficients.TransportDecay"/> ずつ減る＝TransportDecay×(線路の値段＋機関車を作る費用)÷(2×TransportCapPerUnit)）を、同じ容量の荷を馬で運ぶ陸の運賃の金額
    ///   （清算の陸の脚の運賃の倍率＝TransportMargin×LandSeaCostRatio×地形×最寄りの港までの陸の距離 <see cref="Region.PortLandKm"/>÷1万km×運賃革命・容量1あたりの荷の値打ちは1【推】）で割った値。
    ///   旧（(165)〜(167)）＝運転の費用だけ＝輸送の不足が無くても（通した割合1）必要性 54（(166) §0・(167) の作者への確認③）。
    /// を国の内陸の州（港でない稼働中の州・陸の脚のある州）の潜在の交易で重み付けた平均（<see cref="LandFreightServed"/> と同じ重み）。
    /// ★(169) 内陸の州の潜在の交易が無い国（陸の輸送を使っていない）は <see cref="NoPrice"/>＝必要性0（代替する物の需要が無ければ0＝他の行と同じ規則・旧＝RailCostFraction＝島国も必要性 54）。
    /// 線路か機関車の値段が立たない（作れない投入）＝<see cref="NoPrice"/>＝必要性0（他の行と同じ規則）。</summary>
    private static double RailCostIndex(World w, PriceBook book, Producer loco, double freight)
    {
        var c = w.Coef;
        double f = PoliticalQuantities.Clamp01(c.RailCostFraction);
        double pRail = book.Of(c.RailGoodId), pLoco = book.Cost(loco);
        if (!(pRail >= 0) || !(pLoco >= 0) || !(c.TransportCapPerUnit > 0)) return NoPrice;
        double capital = c.TransportDecay * (pRail + pLoco) / (2.0 * c.TransportCapPerUnit);   // 容量1あたりの月の資本の費用（実質）
        double horseUnit = c.TransportMargin * c.LandSeaCostRatio * (freight > 0 ? freight : 1.0) / Diplomacy.DistanceNormKm;   // 陸の脚1kmの馬の運賃の倍率（地形1）
        double pot = 0, sum = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.IsPort) continue;
            double p = r.LastTradePotential;
            if (!(p > 0)) continue;
            int ti = (int)r.Terrain;
            bool water = (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) || (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0);
            double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
            double m0 = water ? (r.Terrain == Terrain.WaterMajor ? c.TerrainWaterMajorCost : c.TerrainWaterMinorCost) : horse;
            double horseMoney = horseUnit * m0 * r.PortLandKm;   // この州の荷を馬で港まで運ぶ運賃（荷の値打ち1あたり）
            if (!(horseMoney > 0)) continue;   // 陸の脚が無い州（座標なし・港の隣）＝鉄道が代わる馬の運賃が無い
            double k = (water || !(horse > 0) ? f : f * rail / horse) + capital / horseMoney;
            pot += p; sum += p * k;
        }
        return pot > 1e-12 ? sum / pot : NoPrice;
    }

    /// <summary>★陸の輸送の値段（2026-10-02 (159)・機関車の必要性の入力＝作者 2026-10-02「鉄道研究の必要性は、軍事力ではなく、輸送力の不足から導かれるべきだ」）＝1＝普通の陸の運賃（輸送が交易を絞っていない）。
    /// ★(165) 比べる相手＝鉄道の輸送の費用（<see cref="RailCostIndex"/>・同じ単位）。
    /// 陸の運賃は市場の値段ではない（地形と鉄道の2値の係数）＝輸送力の不足（<see cref="LandFreightServed"/>＝潜在の交易のうち容量が通した割合）を、市場の値段の均衡と同じ規則
    /// （P*＝参照×(需要÷供給)^(1/2ε)・<see cref="Coefficients.PriceElasticity"/>）で値段へ直す＝(1÷通した割合)^(1/2ε)・天井は財の値段の上限と同じ値（<see cref="Coefficients.PriceCeilFactor"/>＝
    /// 通した割合0 の発散を止める＝思想⑧の例外「発散する量へのロック＝価格の上限」）。新しい係数は無い。平滑化した値段の末尾の1欄（判定と同じ遅れ）。
    /// ★(166) 天井は財の値段と同じ漸近形（<see cref="MarketFlow.SoftBandLn"/>＝対数空間で境界へ漸近・内側〔天井の対数の0.7倍＝100^0.7≈25 まで〕はほぼ恒等・鋭さ <see cref="Coefficients.SoftBandSharpness"/>）
    ///   ＝(160)（(159) の反証役の指摘・低）の申し送り「硬い上限（小さい方を取る）を漸近形へ」。旧＝Math.Min の硬い上限（鉄道の前の英・仏・清は通した割合 0.3〜0.9%＝上限を掛ける前の値段 111〜333＝天井 100 に張り付いた）。
    ///   必要性への効き（源から）＝通した割合 2%（値段 50）で 漸近形 44.5・硬い上限 50＝機関車の必要性 65.0 対 65.1（半分点 0.4・鉄道の費用 0.25）＝全域で 0.28 点以下（値段 25 で 24.5・10 で 10.0）。
    ///   ★(166) 鉄道の前の陸の容量を人口に比例させた（<see cref="Transport.PreRailCapacity"/>）後は、大国の鉄道の前の通した割合は 0.36〜1.0（(166) の10種・1784 の種の中央＝仏 0.36・英 0.68・露 0.94・清 0.96）＝値段 1〜3＝天井に届かない。
    /// ⚠ 機関車の必要性は鉄道の前に飽和する（源から・(166)）＝通した割合 1（輸送の不足なし＝値段 1）でも ln(1÷鉄道の費用 0.25)＝1.39 → 必要性 54（地形の鉄道÷馬が1の州）
    ///   ＝比べる相手が運転の費用だけ（鉄道の線路と機関車の資本の値段を読まない＝作者への確認③）＝「輸送力の不足」は ln(値段) の分だけ上に乗る。</summary>
    internal static double LandFreightPrice(World w)
    {
        var c = w.Coef;
        double cap = c.PriceCeilFactor > 1.0 ? c.PriceCeilFactor : 1.0;
        double e = c.PriceElasticity > 0 ? 1.0 / (2.0 * c.PriceElasticity) : 1.0;
        double served = LandFreightServed(w);
        if (!(served > 0)) return cap;   // 否定形＝NaN と0（全く通らない）は天井（漸近の極限）
        double p = System.Math.Pow(1.0 / served, e);
        double lnCap = System.Math.Log(cap);
        return MarketFlow.SoftBandLn(p, 1.0, lnCap, lnCap, c.SoftBandSharpness);   // 値段≧1（通した割合≦1）＝天井の側だけを使う
    }

    /// <summary>陸の輸送の通した割合（2026-10-02 (159)）＝国の内陸の州（港でない稼働中の州）で、潜在の交易（<see cref="Region.LastTradePotential"/>＝容量で切る前）のうち
    /// 輸送の容量が通した割合（<see cref="Region.LastTradeThrottle"/>）を潜在量で重み付けた平均＝1 は輸送が交易を絞っていない。内陸の州が無い・潜在量が無い国は1。</summary>
    internal static double LandFreightServed(World w)
    {
        double pot = 0, served = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.IsPort) continue;
            double p = r.LastTradePotential;
            if (!(p > 0)) continue;
            double t = r.LastTradeThrottle;
            t = PoliticalQuantities.Clamp01(t);   // 否定形＝NaN は「通らない」の側（律速率の定義域 [0,1] の守り）
            pot += p; served += p * t;
        }
        return pot > 1e-12 ? served / pot : 1.0;
    }

    /// <summary>四半期の発見の判定＝点数が100に届いた行を解禁する（2値・失われない）。多国は <see cref="MultiNationSim"/> の四半期末の共有層（接触を求めた直後）・
    /// 単独の世界は <see cref="Simulation"/> の四半期末（接触なし）。開幕の既知の集合は助走の終わりに1度（<see cref="MultiNationSim.StartWarmup"/>）。</summary>
    internal static void Step(World w, WorldView? view, Coefficients c)
    {
        var known = w.Discovered;
        w.Cost[CostLedger.DiscoveryNations]++;   // 計算量の台帳（診断）：発見の判定は国×四半期
        // ★損耗の記帳（(153)・World.DiscoveryLossQ）を四半期の月の平均へ直して判定の値に載せ、記帳は捨てる（伝わりの取引の記帳と同じ＝四半期に積み、判定で捨てる）。
        if (w.DiscoveryLossQ is { } lq)
        {
            var avg = new double[lq.Length];
            for (int i = 0; i < lq.Length; i++) avg[i] = Calendar.PerQuarterToPerMonth(lq[i]);
            view = (view ?? new WorldView(null)) with { Loss = avg };
            w.DiscoveryLossQ = null;
        }
        // 平滑化した値段（判定の段の頭で AdvancePrice が1歩進めた値・作者裁定 Q12）を読む。★(171) 産地の発見も同じ値段の帳を読む（下の SiteTerms）。
        bool insOn = Instruments.On;   // (921) 関数の頭で 1 回だけ読む（(913) と同じ形・並列の試験で On が途中で切り替わっても確保と使用が食い違わない）
        var ro = insOn ? new (double Replaced, double Cost)[Catalog.Discoveries.Length] : null;   // (320) 計器だけ＝代替する物の値段と新しいレシピの費用を書き出す（Score は書くだけ・判定の値は同じ）
        var eo = insOn ? new double[Catalog.Discoveries.Length] : null;   // ★(415) 計器だけ＝行ごとの親の産業の技師の量（判定の値は同じ）
        var terms = Score(w, view, c, includeKnown: false, null, ro, eo, out var book, out var dem);
        if (insOn) Instruments.IdnObserve(w, view, c, terms);   // ★(640) 計器だけ＝思想の必要性の量（旗 0 でも候補の量を書く）
        if (eo is not null && Calendar.QuarterIndexOfTick(w.Tick) % 8 == 0)   // ★(415) 計器だけ＝全ての行（解禁済みも）の技師の量の軌道を8四半期ごとに（census・事前登録 [6]）。解禁済みの行は判定が飛ばすので別に求める
        {
            var all = ParentEngineersByRow(w, view, c, out var allTerms, out var allRatio);   // ★(450) 必要性・代替する物の値段・新しいレシピの費用も同じ 1 回から（解禁済みの行も）
            for (int row = 0; row < all.Length && row < known.Length; row++)
                if (!double.IsNaN(all[row])) Instruments.EngineerRow("T", w, row, all[row], known[row] ? double.NaN : terms[row].Tech, known[row], allTerms[row].Need, allRatio[row]);
        }
        if (ro is not null)   // (320) 計器だけ＝まだ解禁していない蒸気機関と機関車の軌道（四半期ごと）
        {
            if (!known[SteamRow]) Instruments.DiscoveryTrace(w, SteamRow, terms[SteamRow], ro[SteamRow]);
            if (!known[LocomotiveRow]) Instruments.DiscoveryTrace(w, LocomotiveRow, terms[LocomotiveRow], ro[LocomotiveRow]);
        }
        for (int row = 0; row < terms.Length && row < known.Length; row++)
        {
            if (known[row] || !(terms[row].Score >= GateScore)) continue;
            known[row] = true;
            if (ro is not null) Instruments.DiscoveryEvent(w, row, terms[row], ro[row]);   // (320) 計器だけ
            if (eo is not null && !double.IsNaN(eo[row])) Instruments.EngineerRow("E", w, row, eo[row], terms[row].Tech, false, terms[row].Need, ro![row]);   // (415) 計器だけ
            if (eo is not null && !double.IsNaN(eo[row])) Instruments.ClockOpened(w, row, eo[row], view?.WorldAdvLabor ?? double.NaN);   // (461) 計器だけ
            w.LogAdd($"{w.Year} {w.SelfPower?.Name ?? w.Region.Name}が{Catalog.Discoveries[row].Name}を発見");
        }
        // ★産地の発見（(148)・Catalog.Sites）＝この国の州にある、まだ見つかっていない産地（大鉱床・植林）を同じ門で判定する。
        if (book is not null && dem is not null)
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    if (!Unfound(p)) continue;
                    double[]? po = insOn ? new double[3] : null;   // (316) 計器だけ＝開いた時の人・値段・費用を読む（判定の値は同じ）
                    var tm = SiteTerms(w, r, p, c, book, dem, po);
                    if (!(tm.Score >= GateScore)) continue;
                    if (po is not null) Instruments.SiteOpened(w, r, p, tm, po);
                    OpenSite(w, r, p, c);
                }
            }
        w.LastDiscoveryView = view;
    }

    /// <summary>★産地の発見の2項（(148)・<see cref="Catalog.Sites"/>）＝読むだけ（判定・画面・計器が同じ関数を読む）。伝わりは持たない（他国の発見は自分の土地の鉱床を見つけない）。
    /// ★(171) 作者 2026-10-02 の発見の必要性の一般則（「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する」）と
    ///   比べる相手（「2でよい」＝代替する物の値段÷その発見が出す物の値段〔同じ時期・同じ国の平滑化した投入の値段から新しいレシピで作る費用〕）を産地の行にも当てた（推奨で進めた・作者の承認は未確認）。⚠ 費用は国の今のレシピ（自己費用は全ての州で同じ＝RgoVa・産地の富み〔品位〕を読まない）＝実際に読むのは国の今の採算＝富みを費用に読むまでは候補2の「その発見が出す物の費用」と同じではない（(172) 反証役 vd4#3＝MineralCostFollowsGrade の器）：
    /// ・必要性＝70×強度÷(強度＋産地の半分点 <see cref="Coefficients.DiscoveryNeedHalfSite"/>)・強度＝ln（その財の平滑化した値段÷その国で今のレシピで掘る費用）・1以下は0
    ///   ＝発見の表の製法の行と同じ形（産地が生み出す物＝その財そのもの＝代替する物も同じ財・半分点は分野ごとに1本）。値段と費用は発見の表と同じ値段の帳（<see cref="PriceBook"/>＝自国の平滑化した値段・
    ///   市場が死んでいる財は着値か作る費用の安い方・費用＝その産地の生産者の自己費用×自己費用の物価水準÷物価水準＋Σ投入×投入の値段）。国内に需要の無い財は0（使っていない物の代わりは要らない）。
    ///   ＝デフレの世では金の実質の値段が掘る費用（人の働き）に対して上がり探鉱が進む（金本位の下で名目の金の値段は動かない＝ロコフの金の発見の経済学）。参照価格は読まない。
    /// ・技術力（見つける力）＝70×min(1, 国の技術水準÷難易度)×人の厚み N÷(N＋N0)（難易度＝年代×Y＝発見の表と同じ1本・N0＝<see cref="Coefficients.DiscoverySiteFindHalfPop"/>）
    ///   ＝人の厚みは背景（探す人が居るか）＝必要性から外し、見つける力へ移した（発見の表の技術の押し＝自国で作る投入の割合と同じ置き場所）。
    ///   N＝鉱床は入植者＝州の POP の大きさ×中核民族の割合（CoreEthnicShare・現地住民は0で作られ同化で育つ）・植林は住民（樹を植えて樹皮を剥ぐ働き手）。兵士は数えない。
    /// ⚠ 旧（(148)〜(169)）＝技術力＝70×min(1, 技術水準÷難易度)・必要性の強度＝人の厚み×その財の実質の値段÷参照価格（設計 §9-1 の提案③＝適性＝入植者×金の値・2026-09-28 作者が採用した提案＝文は AI の整理）
    ///   ＝作者 2026-10-02 の一般則の「今後」で上書きされると読んだ（AI の読み・作者の承認は未確認）。</summary>
    /// <param name="realPrice">読む実質の値段の差し替え（計器だけ＝discprobe・既定 null＝エンジンと同じ平滑化した値段）。</param>
    /// <param name="view">世界の市場（着値）を読む判定の値（既定 null＝その国の直前の判定の値 <see cref="World.LastDiscoveryView"/>＝エンジンが直前の判定に渡した値）。</param>
    /// <param name="partsOut">計器だけ＝[0] 人の数 N・[1] その財の値段・[2] その国で今のレシピで掘る費用（立たない値は <see cref="NoPrice"/>）。</param>
    public static Terms SiteTerms(World w, Region r, Producer p, Coefficients c, double[]? realPrice = null, WorldView? view = null, double[]? partsOut = null)
    {
        Score(w, view ?? w.LastDiscoveryView, c, includeKnown: false, realPrice, null, null, out var book, out var dem);
        if (book is null || dem is null) return default;   // 稼働中の州が無い国（判定もしない）
        return SiteTerms(w, r, p, c, book, dem, partsOut);
    }

    /// <summary>★(316) 計器だけ：まだ見つかっていない産地の2項を国の中で1度の Score で全部読む（読むだけ）。産地が無い国は Score を走らせない。
    /// <paramref name="visit"/> の最後の引数＝[0] 人の数 N・[1] その財の値段・[2] 今のレシピで掘る費用（公開の SiteTerms の partsOut と同じ）。</summary>
    internal static void VisitSiteTerms(World w, Coefficients c, System.Action<Region, Producer, Terms, double[]> visit)
    {
        bool any = false;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers) if (Unfound(p)) { any = true; break; }
            if (any) break;
        }
        if (!any) return;
        Score(w, w.LastDiscoveryView, c, includeKnown: false, null, null, null, out var book, out var dem);
        if (book is null || dem is null) return;
        var parts = new double[3];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (!Unfound(p)) continue;
                var t = SiteTerms(w, r, p, c, book, dem, parts);
                visit(r, p, t, parts);
            }
        }
    }

    /// <summary>★(613) 点数の技術の比＝技術水準÷難易度（0〜1）。制度と思想の行（<paramref name="spread"/>）は 1（注の〈技術の押しは1〉・旗 TechThresholdOff は (824) で焼き込み）＝★(621)(627) 呼び手（Step）が特許以外の制度と思想の行でこの 1 を本国の識字÷SpreadLiteracyFull に上書きする。製法ほかの行は技師の旗 <paramref name="engOn"/> が立つと 1（技師の比 EngineerFactor が代わる）。
    /// 式は元の Step の中の 4 行をそのまま抜き出した（ビット一致）。</summary>
    internal static double TechRatioOf(World w, int row, bool spread, bool engOn, Coefficients c)
    {
        double ratio = 1.0;
        if (!spread && !engOn)
        {
            double diff = Difficulty(row, c);
            ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = PoliticalQuantities.Clamp01(ratio);
        }
        return ratio;
    }

    /// <summary>産地の2項の本体（<see cref="Step"/> は判定の値段の帳をそのまま渡す）。</summary>
    private static Terms SiteTerms(World w, Region r, Producer p, Coefficients c, PriceBook book, double[] dem, double[]? partsOut)
    {
        var site = Catalog.Sites[p.SiteRow];
        double ratio = 1.0;   // ★(415) 旗 DiscoveryEngineerHalfPerMonth が正なら比を外し人の厚み N÷(N＋N0) だけ（技術水準を読まない）
        if (!(c.DiscoveryEngineerHalfPerMonth > 0))
        {
            double diff = DifficultyOf(site.Decade, c);
            ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = PoliticalQuantities.Clamp01(ratio);
        }
        double people = 0;
        foreach (var q in r.Pops)
        {
            if (q.IsSoldier || !(q.Size > 0)) continue;
            people += site.Kind == Catalog.SiteKind.Deposit ? q.Size * PoliticalQuantities.Clamp01(q.CoreEthnicShare.Value) : q.Size;
        }
        double n0 = c.DiscoverySiteFindHalfPop > 0 ? c.DiscoverySiteFindHalfPop : 0;   // 否定形＝NaN・負は0（人が居れば厚み1）
        double thick = people > 0 ? people / (people + n0) : 0;
        int g = site.Good;
        double v = NoPrice, cost = NoPrice, s = 0;
        bool used = g >= 0 && g < dem.Length && dem[g] > 1e-9;   // 使っていない物の代わりは要らない（発見の表の行と同じ）
        if (used || partsOut is not null)   // 計器は需要の無い国でも値段と費用を読む（判定は読まない）
        {
            v = book.Of(g);
            cost = book.Cost(p);
        }
        if (used)
        {
            double x = v > 0 && cost > 0 ? v / cost : 0;
            s = x > 1.0 ? System.Math.Log(x) : 0;   // 否定形＝NaN は0
        }
        if (partsOut is { Length: >= 3 }) { partsOut[0] = people; partsOut[1] = v; partsOut[2] = cost; }
        return new Terms(TermMax * ratio * thick, Saturate(s, c.DiscoveryNeedHalfSite), 0);
    }

    // ★(171) 旧 RealValue（財の実質の値段÷参照価格・死んだ市場は1）と AliveNow（1財の市場が生きているか）は削除＝産地の行も発見の表と同じ値段の帳（PriceBook）を読む。

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
