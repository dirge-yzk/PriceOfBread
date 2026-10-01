namespace PriceOfBread.SimCore;

/// <summary>発見の分野（作者裁定 2026-09-28 ⑤＝制度・思想・製法・発明。発明は新兵器と新製品の2つの規則）。
/// 制度（特許・株式会社）と思想（社会主義・共産主義・帝国主義・ファシズム）は 2026-10-01 (152) に同じ表へ（設計 §1A-6＝財を持たない行）。</summary>
public enum DiscoveryField { Institution, Idea, Process, Weapon, Product }

/// <summary>新兵器の軍事の圧力の分野（作者裁定 2026-09-28 ⑤＝陸・海・空で分ける）。</summary>
public enum WarDomain { None, Land, Sea, Air }

/// <summary>発見の表の1行（<see cref="Catalog.Discoveries"/>）＝名前・財・レシピ（""＝既定のレシピ）・分野・年代（10年刻み）・新兵器の分野・代替する物。
/// <paramref name="Replaces"/>＝その行が生み出す物が代替する物（作者 2026-10-02＝必要性はその値段の高さ・<see cref="Discovery.ReplacedStrength"/>）＝<see cref="Catalog.ReplacesOwn"/>（製法＝その行の財そのもの）・
/// 新製品は代替する既存の財の id・<see cref="Catalog.LandFreight"/>（機関車＝陸の輸送＝財ではない）。新兵器・制度・思想の行は読まない。</summary>
public readonly record struct DiscoveryRow(string Name, int Good, string Recipe, DiscoveryField Field, int Decade, WarDomain Domain = WarDomain.None, int Replaces = Catalog.ReplacesOwn);

/// <summary>発見＝解禁の【唯一の形】（★命名の作者裁定2026-08-31＝「研究」はプレイヤーが方向を選ぶ動詞を連想させる・この機構は選べず社会から創発して見つかる＝「発見」）。
/// ★段2A（2026-10-01・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A-2）：**発見は2値**・国ごと・行（レシピ）ごとに毎四半期
/// <c>点数 ＝ 技術力（0〜70）＋ 必要性（0〜70）＋ 伝わり（0〜70）</c> を求め、**一度でも100に届いたら解禁**（以後失われない）。
/// 旧＝技術水準の時計の傾斜（onset〜onset+1 で 0→1 の連続の門・財の門×レシピの門の積）＝全ての国が技術水準だけで同じ順に同じ速さで開いた。
/// ・どの項の最大も門の100未満＝**1つだけでは届かない**／どの2つの最大の和も100以上＝**2つ揃えば届く**（作者の意図＝設計 §1A-2 の性質）。
/// ・**必要性が0の国は自力では発見しない**（技術力70＋伝わり0＝70）。**世界で最初の発見は必ず技術力＋必要性**（誰も持っていなければ伝わりは0）。
/// ・★物を生む行（製法・新製品）の必要性（2026-10-02 (159)）＝作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する。
///   例えば蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」＝生み出す物（製法＝同じ財・新製品＝代替する既存の財）の平滑化した値段の高さ（<see cref="ReplacedStrength"/>）。
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
    ///   ⚠危機だけ（0〜1の国の指数）では負けた戦争1つで強度が 0.85 へ跳び、技術水準 3 の国が1870〜80年代に見つけた（(152) の試走 v1）。</summary>
    private static double SpreadNeed(int row, World w, double maxInd, double workerMil, double colonialShare, double popMil, ref double crisis)
    {
        double s;
        if (row == PatentRow) s = w.EliteCore;
        else if (row == JointStockRow) s = maxInd;
        else if (row == SocialismRow || row == CommunismRow) s = workerMil;
        else if (row == ImperialismRow) s = colonialShare;
        else if (row == FascismRow) { if (double.IsNaN(crisis)) crisis = Ideologies.FascismCrisis(w); s = crisis * popMil; }
        else s = 0;
        return !(s > 0) ? 0 : s;   // 否定形＝NaN・負は0
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

    /// <summary>国の外から見た値（<see cref="MultiNationSim"/> が四半期末に求めて渡す・単独の世界は null）＝行ごとの接触（★(159)：人件費の世界の平均〔(147)〕は削除＝必要性は生み出す物の値段だけを読む）。
    /// ★<paramref name="Loss"/>＝この四半期の陸・海・空の損耗の強さ（添字は <see cref="WarDomain"/>・月の平均・0〜1＝<see cref="World.DiscoveryLossQ"/> を <see cref="Step"/> が直した値・2026-10-01 (153)）
    /// ＝国の中の値だが、記帳は判定で捨てる＝画面と計器が判定と同じ3項を読むためにここへ載せる（<see cref="World.LastDiscoveryView"/>）。</summary>
    public sealed record WorldView(double[]? Contact, double[]? Loss = null);

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
    /// 制度と思想の行の伝わりは接触ではなく到達（<see cref="World.SpreadReach"/>＝単独の世界は0のまま）。
    /// 解禁済みの行と、この国に代表の生産者が居ない行（その国では使えないレシピ）は 0。値段は平滑化した値段（<see cref="World.DiscoveryPrice"/>・作者裁定 Q12）。</summary>
    /// <param name="realPrice">読む実質の値段の差し替え（計器だけ＝discprobe が「値段の跳ねで決まった解禁」を数える・既定 null＝エンジンと同じ値段）。</param>
    public static Terms[] Evaluate(World w, WorldView? view, Coefficients c, bool includeKnown = false, double[]? realPrice = null) => Score(w, view, c, includeKnown, realPrice);

    /// <summary>瞬間の実質の値段（財ごと・参照価格の単位・死んだ市場は参照価格）＋末尾の1欄＝陸の輸送の値段（<see cref="LandFreightPrice"/>）＝平滑化した値段の入力（計器と画面が読む）。</summary>
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
    private static Terms[] Score(World w, WorldView? view, Coefficients c, bool includeKnown, double[]? realPrice = null)
    {
        double[]? contact = view?.Contact;
        double[]? loss = view?.Loss;
        var rows = Catalog.Discoveries;
        int nr = rows.Length, ng = Catalog.Goods.Length;
        var res = new Terms[nr];
        // --- 国の集計（財ごと・稼働中の州の和）：国内の産出・輸入・需要・需要で重みを付けた値段・作れるか（門の開いた生産者が居る）---
        var refP = Catalog.RefPrices;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];   // imp＝州の輸入−輸出の和＝国の純輸入（同じ国の州どうしの積み出しは打ち消す）
        var canMake = new bool[ng];
        var rep = new Producer?[nr];
        int nAct = Aggregate(w, ng, prod, imp, dem, pxd);
        double maxInd = 0;   // 州の最大工業化度（株式会社の必要性＝立っている工業資本の大きさ・旧の閾値と同じ量）
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            if (r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
            foreach (var p in r.Producers)
            {
                int row = p.DiscoveryRow;
                if (row >= 0 && rep[row] is null) rep[row] = p;
                if (p.GoodId >= 0 && p.GoodId < ng && !canMake[p.GoodId] && Gate(r, p) > 0) canMake[p.GoodId] = true;
            }
        }
        if (nAct == 0) return res;
        // 値段＝平滑化した実質の値段（作者裁定 Q12・World.DiscoveryPrice）。瞬間の値（InstantReal）は需要で重みを付けた州の値段÷物価水準・死んだ市場は参照価格・末尾の1欄は陸の輸送の通した割合。
        double[] real;
        if (realPrice is { } rpx && rpx.Length >= ng) real = rpx;
        else real = w.DiscoveryPrice is { } dpx && dpx.Length >= ng ? dpx : InstantReal(w, ng, prod, imp, dem, pxd);   // 平滑の前（ロード直後・開幕の画面）は瞬間の値
        // 軍事の圧力（新兵器）＝その分野の脅威（相手の強さ）が自国の強さとの和に占める割合∈[0,1)＝理想の軍隊の形（ArmyShape）の陸と海の必要量 ÷（必要量＋自国の陸軍力・海軍力）。空は陸の膠着（設計 §1A-4）。
        //   ⚠比（相手÷自国）のままでは弱い国が数十倍になり必要性が70へ張り付く（1種の試走でダホメが1831年に機関銃・コンゴが1837年に弩級戦艦）＝割合にすれば最大は 70÷(1＋半分点)。
        //   ★＋交戦中のその分野の損耗（2026-10-01 (153)・設計 §1A-4 (4a) の第2項・作者裁定 2026-09-28 ⑤「陸・海・空で分ける」）＝この四半期の損耗の強さ（<see cref="WorldView.Loss"/>＝
        //   その月に失った割合÷総力戦の月の上限の割合の平均＝0〜1＝脅威の割合と同じ「最大を1とした割合」）。陸＝戦死（敵の空から受けた分を除く）・海＝艦隊摩耗・空＝制空劣勢で増えた戦死。
        //   空の脅威は陸の割合のまま（理想の軍隊の形に空の必要量が無い＝AI の読み・(147) の⑤）＝空の損耗だけが空の分野の量。
        double landPress = 0, seaPress = 0;
        if (w.SelfPower is { } sp)
        {
            landPress = Share(sp.NeedLand, sp.LandPower);
            seaPress = Share(sp.NeedSea, sp.NavalPower);
        }
        // 制度と思想の必要性の量（<see cref="SpreadNeed"/>）＝工場労働者の人数×闘争度・人々の闘争度・植民地の州に住む人。兵士は数えない。
        //   ★(159)：新製品の必要性（旧＝満足した消費者の厚み Σ人数×満足度÷人口＝作者裁定 Q13）は代替する物の値段へ（作者 2026-10-02・<see cref="ReplacedStrength"/>）。
        double popSum = 0, workerMilSum = 0, colonialPop = 0, milSum = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
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
        var reach = w.SpreadReach;
        for (int row = 0; row < nr; row++)
        {
            if (!includeKnown && Known(w.Discovered, row)) continue;   // includeKnown＝計器だけ（解禁済みの行の項も見る）
            var d = rows[row];
            bool spread = IsSpread(d.Field);   // 制度と思想＝財と投入を持たない（技術の押しは1・伝わりは到達）
            var rp = rep[row];
            if (!spread && rp is null) continue;   // この国に代表の生産者が居ない行（その国では使えないレシピ）
            // 技術力＝70×min(1, 技術水準÷難易度)×技術の押し（設計 §1A-3＝投入のうち自国で作っている割合・参照価格で重み付け・制度と思想は1）
            double diff = Difficulty(row, c);
            double ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = !(ratio > 0) ? 0 : ratio > 1 ? 1 : ratio;
            if (spread)
            {
                double sNeed = SpreadNeed(row, w, maxInd, workerMil, colonialShare, popMil, ref crisis);
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
            double tech = TermMax * ratio * push;
            // 必要性＝70×強度÷(強度＋分野の半分点)（設計 §1A-4）
            double s = 0, half = 1;
            switch (d.Field)
            {
                case DiscoveryField.Process:
                case DiscoveryField.Product:
                    // ★物を生む行（2026-10-02 (159)）＝生み出す物（製法＝同じ財・新製品＝代替する既存の財・機関車＝陸の輸送）の平滑化した値段の高さ（<see cref="ReplacedStrength"/>）。
                    //   作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、その発見が生み出すものが代替するものが高いか、で判定する」。
                    //   ⚠旧（(147)〜(154)）＝製法は今の方法の投入の費用÷新しいレシピの投入の費用が参照の比を超えた分・労働を代替する発明（蒸気機関・電動機）は人件費の世界比
                    //     ×参照の資本と燃料÷自国・新製品は満足した消費者の厚み＝背景（人件費・豊かさ）と新しいレシピの費用を読んでいた。
                    s = ReplacedStrength(row, real, dem, prod, imp);
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

    /// <summary>瞬間の実質の値段（財ごと・参照価格の単位）＝需要で重みを付けた州の値段÷物価水準＋末尾の1欄＝陸の輸送の値段（<see cref="LandFreightPrice"/>・参照＝1）。
    /// ⚠死んだ市場（国内に需要が無いか、国内に供給〔産出＋純輸入の正の分〕が無い財）の値段は凍った値（需要の無い財は床・供給の無い財は天井へ張り付く）
    ///   ＝割高さの物差しにならない（1種の試走で、蒸気機関の財が売れない国ほど「蒸気機関が安い」＝人件費が割高に見えた）＝参照価格（その財が普通に出回ったときの値段）で読む。
    ///   否定形＝NaN も参照価格へ倒す（平滑の状態へ NaN を入れない）。</summary>
    private static double[] InstantReal(World w, int ng, double[] prod, double[] imp, double[] dem, double[] pxd)
    {
        double pl = w.NominalOf(1.0);
        var refP = Catalog.RefPrices;
        var x = new double[ng + 1];
        for (int g = 0; g < ng; g++)
        {
            bool alive = pl > 0 && Alive(dem[g], prod[g], imp[g]);
            double v = alive ? pxd[g] / dem[g] / pl : refP[g];
            x[g] = v >= 0 ? v : refP[g];
        }
        x[ng] = LandFreightPrice(w);
        return x;
    }

    /// <summary>生きた市場（国内に需要と供給〔産出＋純輸入の正の分〕の両方がある財）＝値段が物差しになる（瞬間の値段・割高さ・世界の値段の重みが同じ1本を読む）。</summary>
    private static bool Alive(double dem, double prod, double imp) => dem > 1e-9 && prod + (imp > 0 ? imp : 0) > 1e-9;

    /// <summary>平滑化した値段を1歩（四半期に1回・判定の段の頭だけ＝<see cref="MultiNationSim"/> は全ての国を先に進めてから判定する・単独の世界は <see cref="Simulation"/> の四半期末）
    /// ＝ s ← s ＋ 率×（瞬間 − s）。最初（null・建国した国）は瞬間の値で始める＝立ち上がりの跳躍なし。書き手はこの1か所だけ（画面と計器は読むだけ）。</summary>
    internal static void AdvancePrice(World w, Coefficients c)
    {
        int ng = Catalog.Goods.Length;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        Aggregate(w, ng, prod, imp, dem, pxd);
        var x = InstantReal(w, ng, prod, imp, dem, pxd);
        var s = w.DiscoveryPrice;
        if (s is null || s.Length != x.Length) { w.DiscoveryPrice = x; return; }
        double a = PriceSmoothingQ(c);
        for (int g = 0; g < x.Length; g++) s[g] += a * (x[g] - s[g]);
    }

    /// <summary>項の飽和＝70×強度÷(強度＋半分点)（発見の表と産地の発見が共有）。否定形＝NaN・負は0。</summary>
    private static double Saturate(double s, double half) => s > 0 && half > 0 ? TermMax * s / (s + half) : 0;

    /// <summary>★物を生む行（製法・新製品）の必要性の強度（2026-10-02 (159)・作者 2026-10-02「発見の必要性は今後、その発見が必要とされた背景や因果ではなく、
    /// その発見が生み出すものが代替するものが高いか、で判定する。例えば蒸気機関は「人件費が高いか」ではなく「(間接的に人が生み出している)動力が高いか」を見る」）
    /// ＝その行が生み出す物が代替する物（<see cref="DiscoveryRow.Replaces"/>＝製法はその行の財そのもの・新製品は代替する既存の財）の平滑化した実質の値段÷参照価格の対数（参照より高い分だけ・参照以下は0）。
    /// 参照価格＝その財が普通に出回ったときの値段（費用の積み上げ・役務は錨）＝「高いか」を普通の値段と比べて読む（推奨で進めた）。
    /// ・陸の輸送（機関車）＝陸の輸送の値段（<see cref="LandFreightPrice"/>＝輸送力の不足を市場の値段の均衡式で値段へ・参照＝1）の対数＝財と同じ規則
    ///   （作者 2026-10-02「鉄道研究の必要性は、軍事力ではなく、輸送力の不足から導かれるべきだ」）。
    /// ・背景・因果（人件費の世界比・豊かさ・脅威・軍）と、新しいレシピの投入の費用・他国の値段は読まない（作者の例「「肥料が高いか」だけを見るべきだろう」・使うかは市場が決める＝安いレシピを選ぶ既存の仕組み）。
    /// ・⚠物差しの選び方（(159) の10種の2腕・実装ログ (159) §3）：世界の値段（各国の平滑化した値段を需要で重み付けた平均）と比べる腕 F は、最も安く作る国（英）ほど新製品を必要とせず遅れ、
    ///   米 8/10 の常勝＝判定の線（F1）の外。参照価格と比べる腕 R（この形）は F1 の内側（exact 51.8→52.8・near 85.4→87.9・清5・米3・英1・仏1）。
    ///   ⚠労働力（動力）の参照価格は費用の積み上げでない一律の錨（0.5）＝開幕の動力の値段は主要国の全てで錨の 0.47〜0.91 倍（種1・ネーデルラント 1.15 だけが上）＝開幕では誰も蒸気機関を必要とせず、
    ///   工業化で動力が錨を超えた国が見つける（英1801・米1794〔10種の中央〕）。
    /// ・対数＝値段の比は比で読む（(147) と同じ）。自国の市場が死んでいる（国内に需要か供給が無い＝<see cref="Alive"/>）財は0＝使っていない財の代わりは要らない・凍った値段は物差しにならない
    ///   （⚠需要があって供給が無い〔封鎖で全く手に入らない〕国も0＝値段が立たない＝記録の限界）。全ての値段が参照価格の世界では全ての行で0（番人）。</summary>
    public static double ReplacedStrength(int row, double[] real, double[] dem, double[] prod, double[] imp)
    {
        var d = Catalog.Discoveries[row];
        int ng = Catalog.Goods.Length;
        // 陸の輸送は末尾の1欄（参照＝1・内陸の州が無い・潜在量が無い国は値段1＝不足なし＝生きた市場の判定は要らない）。財は自国の市場が生きているときだけ。
        int g = d.Replaces == Catalog.LandFreight ? ng : d.Replaces == Catalog.ReplacesOwn ? d.Good : d.Replaces;
        if (g < 0 || g > ng || g >= real.Length) return 0;
        if (g < ng && (g >= dem.Length || !Alive(dem[g], prod[g], imp[g]))) return 0;
        double v = real[g] / (g < ng ? Catalog.RefPrices[g] : 1.0);
        return v > 1.0 ? System.Math.Log(v) : 0;   // 否定形＝NaN は0
    }

    /// <summary>★陸の輸送の値段（2026-10-02 (159)・機関車の必要性の入力＝作者 2026-10-02「鉄道研究の必要性は、軍事力ではなく、輸送力の不足から導かれるべきだ」）＝参照＝1（輸送が交易を絞っていない）。
    /// 陸の運賃は市場の値段ではない（地形と鉄道の2値の係数）＝輸送力の不足（<see cref="LandFreightServed"/>＝潜在の交易のうち容量が通した割合）を、市場の値段の均衡と同じ規則
    /// （P*＝参照×(需要÷供給)^(1/2ε)・<see cref="Coefficients.PriceElasticity"/>）で値段へ直す＝(1÷通した割合)^(1/2ε)・天井は財の値段の上限と同じ値（<see cref="Coefficients.PriceCeilFactor"/>＝
    /// 通した割合0 の発散を止める＝思想⑧の例外「発散する量へのロック＝価格の上限」）。新しい係数は無い。平滑化した値段の末尾の1欄（判定と同じ遅れ）。
    /// ⚠(160)（(159) の反証役の指摘・低）＝形は硬い上限（小さい方を取る）で、財の値段の SoftBand（対数空間で境界へ漸近する）とは違う。PriceElasticity 0.5 では値段＝1÷通した割合
    ///   ＝鉄道の前の英・仏・清（通した割合 0.7〜0.9%＝値段 111〜143）では天井が効いている＝安全網ではなく実際に動く点（必要性は天井でも漸近形でも約61〜64で飽和＝実害は小さい）。
    ///   漸近形へ替えるのは挙動の変更＝輸送の回へ申し送り（00一覧 §0-A の 5 の残り）。</summary>
    internal static double LandFreightPrice(World w)
    {
        var c = w.Coef;
        double cap = c.PriceCeilFactor > 1.0 ? c.PriceCeilFactor : 1.0;
        double e = c.PriceElasticity > 0 ? 1.0 / (2.0 * c.PriceElasticity) : 1.0;
        double served = LandFreightServed(w);
        if (!(served > 0)) return cap;   // 否定形＝NaN と0（全く通らない）は天井
        double p = System.Math.Pow(1.0 / served, e);
        return p < cap ? p : cap;
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
            t = !(t > 0) ? 0 : t > 1 ? 1 : t;   // 否定形＝NaN は「通らない」の側（律速率の定義域 [0,1] の守り）
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
            for (int i = 0; i < lq.Length; i++) avg[i] = lq[i] / Calendar.MonthsPerQuarter;
            view = (view ?? new WorldView(null)) with { Loss = avg };
            w.DiscoveryLossQ = null;
        }
        var terms = Score(w, view, c, includeKnown: false);   // 平滑化した値段（判定の段の頭で AdvancePrice が1歩進めた値・作者裁定 Q12）を読む
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
    public static Terms SiteTerms(World w, Region r, Producer p, Coefficients c, double[]? realPrice = null)
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
        double s = thick * RealValue(w, site.Good, realPrice);
        return new Terms(TermMax * ratio, Saturate(s, c.DiscoveryNeedHalfSite), 0);
    }

    /// <summary>財の実質の値段÷参照価格＝発見の表と同じ平滑化した値段（<see cref="World.DiscoveryPrice"/>・作者裁定 Q12・判定の中では <see cref="Step"/> が同じ四半期に1歩進めた後）。
    /// 死んだ市場は参照価格＝1（<see cref="InstantReal"/>）。★(150)：生きた市場の判定を発見の表と1本にした（旧＝国内に残る供給〔産出＋輸入−輸出〕で読み、全て輸出する産地の国は1と読んだ）。
    /// 判定の前（ロード直後・開幕の画面）は瞬間の値。</summary>
    private static double RealValue(World w, int g, double[]? realPrice = null)
    {
        int ng = Catalog.Goods.Length;
        double rp = g >= 0 && g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0;
        if (!(rp > 0) || g >= ng) return 1.0;
        var s = realPrice is { } rpx && rpx.Length >= ng ? rpx : w.DiscoveryPrice;
        if (s is null || s.Length < ng) s = InstantRealPrices(w);
        double v = s[g] / rp;
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
