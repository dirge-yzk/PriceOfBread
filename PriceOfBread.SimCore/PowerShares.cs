namespace PriceOfBread.SimCore;

/// <summary>★権力の取り分＝**「各階級が、どの機関をどれだけ支えているか」の表1枚**（作者設計 最終形 2026-08-18）。
/// docs/202608/20260818_権力の取り分を同一スケールへ_部分完了.md（設計の経緯・史学検証1/2の反証一覧・対応表・次段の危険箇所）。
///
/// <code>
/// 表 ＝ 階級（旧支配層/近代エリート/大衆/小営業 ＋ ★王そのもの）× 機関（執行権/議会/将校/兵卒/革命勢力）
///
///   取り分   ＝ 表の【列】の合計        （軍部 ＝ 将校 ＋ 兵卒）
///   貴族率   ＝ 表の【行】の比           （執行権＝集権度の逆／議会＝議会の性格／将校＝将校団の性格）
///   全セルの合計 ＝ 1                    ★同一スケールの芯
/// </code>
///
/// ★**計算は1つだけ**（<see cref="Table"/>）。取り分も貴族率も近代エリート率も大衆率も、その表からの導出＝コードの重複が無い。
///
/// ★★★**「王そのもの」の行を必ず残す**＝正統性・財政・常備軍を養う能力（<see cref="StateCapacity"/>）。
///   社会的支持が全部剥がれても執行権はゼロにならない。逆に **1787-88のフランスは「王そのもの」だけが残り、
///   貴族の重みが議会側へ移った状態**として表現される＝「貴族の陣営の内生化」が**構成そのもの**になる。
///
/// ⚠**2階級（貴族／近代エリート）で閉じてはならない**（史学検証1の反証2件）：
///   ① **1870年以降の議会は近代エリートの器ではない**（スウェーデン農民身分・デンマーク/ノルウェー農民党・独中央党/社民党・
///      英労働党/アイルランド党・仏急進社会党・ベルギーカトリック党＝この帯では多数派）→ 議会に**大衆率と小営業**が要る。
///   ② **ブルジョワが王権側に付く例が反復**（第二帝政のクレディ・モビリエとオスマン改造・1879独「鉄とライ麦」・
///      明治の政商・露十月党。人民投票を考えれば大衆も）→ **執行権も近代エリート/大衆を抱えられる**こと。
///   ★だから階級の固定対応（王権＝貴族／議会＝近代エリート）は書かない——**陣営は内生**（<see cref="PopExecutiveLean"/>）。
///
/// ★★★**軍は執行権の対抗者ではなく執行権の道具であり、道具が主人を食う**（史学検証2）＝
///   「1−執行権−議会」だけだと執行権が強い国で軍が消える（符号が逆・露の近衛1801／独の軍事内閣1916-18／1930年代日本）。
///   → 軍部シェアは**常在項（常備軍の対人口比）＋漏れ**（<see cref="ArmyBaseline"/>）。
/// ★★★**軍の政治力の大半はクーデタでなく拒否権として行使された**（<see cref="VetoPower"/>）＝日本1912の二個師団増設問題。
/// ★★★**将校と兵卒は別の入力から読む**（史学検証3＝イベリア＋ラテンアメリカ 約92件の実証）——
///   **兵卒・下士官の反乱はほぼ例外なく物質的苦情を伴う**（ラ・グランハ1836の給与3か月未払／サンヒル1866の昇進の天井と
///   年金差別／ブラジル1910シバータの要求書＝給与引上げ・給食改善・勤務時間制限／チリ1931コキンボの30%減俸／
///   キューバ1933の未払給与／エルサルバドル1931の全軍給与停止）。**対して将校の反乱は賃金では動かない**——
///   地位・自律・名誉（伯1883-87の年金拠出義務化＋新聞発言禁止＋文民大臣への従属／西1923のピカソ調書／
///   ボリビア1934の司令部更迭への先手）。⚠同じ「軍事費」から両方を引くとこの分岐が消える。
/// ★★★**軍の最頻動作は「奪う」ではなく「支えるのをやめる」**（伯1831ペドロ1世退位＝守備隊長が通告し**忠誠を撤回して
///   文民連合に引き渡した**／1889年の共和政宣言も当初目的は内閣打倒／1930年10月の軍事評議会は数日で崩れヴァルガスへ引き渡した）。
///   → 軍部シェアの上昇は、まず**執行権の取り分が崩れる**形で現れる（軍事独裁は最終段＝<see cref="SeeksMilitaryRule"/> の相対条件）。
/// ★★★**純軍政の割合は普遍でなく文民制度の厚みで決まる**（<see cref="CivilianInstitutions"/>）——
///   ラテンアメリカ全体で文民同盟型52%・純軍政26%だが、スペイン14%・ブラジル8%・メキシコ10%・ポルトガル21%に対し
///   **アンデス三国は約70%・チリ44%**。⚠**軍部の定数項を全国共通の定数として置いてはいけない**。
/// ⚠**陸軍と海軍の帰属を分ける**（チリ1891＝陸軍は昇進と資源配分を大統領に全面依存・海軍は恩顧網から構造的に独立／
///   西1936＝海軍は将校がほぼ一様に共和国に敵対的だったのに水兵が反乱を起こして艦隊を奪還）のは
///   **現物の状態では算出不能**（軍種別の人事・恩顧の状態が無い）＝**将校と兵卒の分裂**で代理する（docs §3）。
/// ★★★**軍部は将校と兵卒に分ける**＝1スカラーだと両者が打ち消し合って**両方消える**（1917露・1836ラ・グランハ・
///   1857セポイ・1918キール・1933キューバ軍曹）。⚠**軍事的な有効性は貴族率で下げない**（プロイセン＝ユンカー将校団＋
///   当時最も近代的な軍）。下げるのは社会的近代化（平民将校の登用・資格任用）のみ＝本型は観測なので有効性に触れない。
///
/// ⚠**金は必要だが十分ではない。正統性を失った政権は軍を買えない。**（<see cref="ArmyBinding"/>）
///   ⚠しかも**金は実質で測る**——1917ロシアは金庫に金があってもパンが無く軍が離反した（<see cref="SoldierWelfare"/>）。
///   ⚠加算に崩してはならない（「払えば必ず従う」になり 1830七月・1848・1917・1918キール・1936西が全部消える）。
///   ⚠プロイセン1862は根拠にしない（議会は徴税を止められず、給与を払っていたのは王権のまま＝反例として弱い）。
///
/// ★表と取り分（<see cref="Table"/>／<see cref="Shares"/>）は**観測のみ**（Runner 計器・挙動へ未配線）。⚠部品はシムが読む＝<see cref="PrivilegeIntact"/>／
///   <see cref="StatePatronage"/>（Ideologies）・<see cref="CivilianInstitutions"/>／<see cref="ParliamentControl"/>（PowerAffinity→Discovery）。新しい状態は作らない＝純関数＝SaveGame 不変。
/// ⚠決定論＝RNG なし。走査順は <see cref="World.AllPops"/>（Regions→Pops・Seceded スキップ）に固定。
/// ⚠NaN ガードは**否定形**（`!(x &gt;= a &amp;&amp; x &lt;= b)`）で書く＝ Math.Clamp は NaN を除去しない。
/// ⚠比のガードは絶対値で書かない＝必ず正規化し、分母が痩せたときの倒し先を明示する。</summary>
public static class PowerShares
{
    // =====================================================================================
    //  表の軸
    // =====================================================================================

    /// <summary>表の【行】＝階級。0〜3は現物の利益ブロックそのまま（新しい概念ゼロ）。
    /// ⚠4「王そのもの」は階級ではない＝正統性・財政・常備軍を養う能力（<see cref="StateCapacity"/>）の行。
    /// ⚠3「小営業」は現物の第4束（<c>InterestGroups.PettyPops</c>）＝<c>PettyInterestGate=0</c>（既定）では
    ///   常に0＝現物の3束分類と同じ（休眠の作法を守る＝呼ばないことが恒等）。</summary>
    public enum Cls { OldRuling = 0, ModernElite = 1, Mass = 2, Petty = 3, Sovereign = 4 }
    public const int ClassCount = 5;
    public static readonly string[] ClassNames = ["旧支配層", "近代エリート", "大衆", "小営業", "王そのもの"];

    /// <summary>表の【列】＝機関。軍部は将校（階級構成をもつ）と兵卒（大衆）に分かれる。</summary>
    public enum Organ { Executive = 0, Parliament = 1, Officers = 2, Ranks = 3, Revolution = 4 }
    public const int OrganCount = 5;
    public static readonly string[] OrganNames = ["執行権", "議会", "将校", "兵卒", "革命勢力"];

    /// <summary>取り分をまとめて見るときの4主体（軍部＝将校＋兵卒）。</summary>
    public enum Holder { Executive = 0, Parliament = 1, Army = 2, Revolution = 3 }
    public const int HolderCount = 4;
    public static readonly string[] HolderNames = ["執行権", "議会", "軍部", "革命勢力"];

    /// <summary>★「組む相手として成立する取り分」の下限（史学検証2の修正C）。
    /// ⚠旧＝「軍部が単独50%以上なら軍事独裁」という**絶対値の閾値**。実測では純粋軍政は約4件に1件で、
    ///   単独支配を選んだ国は軍が強かったのではなく**組む相手がいなかった**（ペルー1930・ポルトガル1926・チリ1927・ギリシャ1925）。
    ///   逆に軍が最も圧倒的だった局面（ギリシャ1909・ポルトガル1910・オスマン1908）では**文民に権力を渡している**。
    ///   → 条件を**相対値**へ＝規則が1つ減る。</summary>
    public const double PartnerViableShare = 0.5;

    // ── 分母が消えたときの倒し先（★「比のガードは絶対値で書かない」の実装）──
    private const int ExecFallbackRow = (int)Cls.Sovereign;   // 執行権の支え手が誰もいない＝「王そのもの」だけが残る（1787-88仏の極限）
    private const int ParlFallbackRow = (int)Cls.Mass;        // 議会の支え手が誰もいない＝形式上の議席は大衆の器とみなす
    private const int OfficerFallbackRow = (int)Cls.Mass;     // 将校団の階級構成が定まらない＝兵は民から出る
    private const int LeakFallbackHolder = (int)Holder.Army;  // 漏れの内訳が定まらない＝銃を持つ側

    // =====================================================================================
    //  1. POP の政治的重みと陣営（★階級の固定対応を書かない層）
    // =====================================================================================

    /// <summary>★POP の政治的重み＝**資産 × 発言権**（正規化前）。
    /// ・資産＝流動資産（Savings）＋ 閉鎖階級の**土地の資本化**（LandRent × <c>LandedPowerCapitalization</c>）。
    ///   ⚠<see cref="InterestGroups"/> の <c>BlocsSplit</c> 内のローカル関数 <c>LandedWealth</c> と**同じ式**。
    ///   （ローカル関数は呼べず、当該ファイルは本段の変更対象外なので**意図的な重複**＝点火段で一本化する）
    /// ・発言権＝<see cref="InterestGroups.FranchiseVoice"/>（実効参政権の単一ソース）。
    ///   ★これで「参政権を通じて支えている」が自動で選ばれる＝**1783英は資産保有者が地主なので議会が自動的に貴族的になる**
    ///   （特例を書かずに史学反証①＝穀物法1815が解ける）。1890日本の地租15円有権者も同じ式で出る。</summary>
    public static double PopWeight(Pop p, World w)
    {
        double wealth = System.Math.Max(0, p.Savings);
        if (p.Closed && w.Coef.LandedPowerCapitalization > 0)
            wealth += w.Coef.LandedPowerCapitalization * System.Math.Max(0, p.LandRent);
        if (!(wealth >= 0)) wealth = 0;
        double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        if (!(voice >= 0)) voice = 0;
        double v = wealth * voice;
        return !(v >= 0) ? 0 : v;
    }

    /// <summary>★執行権が**貴族の特権**（免税・農奴制・身分）を守っている度合い∈[0,1]。
    /// <code>
    /// 農奴制 ＝ SerfEmancipation（なし1 / 限定0.5 / 全面0）      ★1861露・1871-76明治で落ちる
    /// 免税   ＝ 1 − clamp(土地税 ＋ 累進課税, 0, 1)               ★1787仏カロンヌの土地税提案で落ちる
    /// 身分   ＝ Bureaucracy（猟官1 / 資格任用0.5 / 専門0）        ★身分任用の解体で落ちる
    /// </code>
    /// ★守っていれば貴族は執行権側（ユンカー）、手を付ければ議会/反乱側（1787-88仏の名士会・高等法院／ヨーゼフ2世／
    ///   シュタイン改革／1861露農奴解放／廃藩置県・秩禄処分）＝**符号が両方向に出る**。</summary>
    public static double PrivilegeIntact(World w)
    {
        double serf = w.SerfEmancipation switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double taxExempt = 1.0 - System.Math.Clamp(w.LandTax + w.ProgressiveTax, 0, 1);
        if (!(taxExempt >= 0 && taxExempt <= 1)) taxExempt = 1.0;   // 否定形（NaNは「手を付けていない」側へ）
        double estate = w.Bureaucracy switch { BureaucracyLaw.Spoils => 1.0, BureaucracyLaw.MeritExam => 0.5, _ => 0.0 };
        double v = (serf + taxExempt + estate) / 3.0;
        return !(v >= 0 && v <= 1) ? 0.5 : v;
    }

    /// <summary>★執行権が**資本家に供給**している度合い∈[0,1]（関税・国家発注・秩序）。
    /// ★第二帝政（クレディ・モビリエとオスマン改造）・1879独「鉄とライ麦」・明治の政商・露十月党が**1本の機構**から出る。
    /// ⚠**インフラは0**として扱う＝現物に「国家が供給するインフラ」の単一スカラーが無い（巨大建造物は離散イベント）。</summary>
    public static double StatePatronage(World w)
    {
        double tariff = System.Math.Clamp(w.Tariff, 0, 1);
        if (!(tariff >= 0 && tariff <= 1)) tariff = 0;
        double orders = System.Math.Clamp(w.MilitarySpending, 0, 1);
        if (!(orders >= 0 && orders <= 1)) orders = 0;
        double order = System.Math.Clamp(w.PublicOrder, 0, 1);
        if (!(order >= 0 && order <= 1)) order = 0;
        return (tariff + orders + order) / 3.0;
    }

    /// <summary>★POP が執行権側へ傾いている度合い∈[0,1]（＝**陣営は内生**・階級の固定対応を書かない）。
    /// <code>
    /// 思想 lean ＝ 保守族（伝統/反動/ナショ/帝国/ファシズム）の合計シェア  ★PoliticalQuantities.FamilyOf の唯一の写像
    /// 供給      閉鎖階級 → PrivilegeIntact ／ 近代エリート → StatePatronage ／ それ以外 → 供給チャネルなし
    /// align     ＝ 供給チャネルがあれば (lean ＋ 供給)/2、無ければ lean
    /// </code>
    /// ★同じ貴族POPが、執行権のふるまい次第で両側に立つ。★参政権拡大が**単調でない**のもここから出る：
    ///   新規有権者が保守的な農村大衆なら、その資産×発言権は**執行権の側へ**入る（1867ビスマルク・ナポレオン3世・1907墺）。</summary>
    public static double PopExecutiveLean(Pop p, World w)
    {
        double lean = 0;
        var s = p.IdeologyShares;
        for (int i = 0; i < Ideologies.Count && i < s.Length; i++)
            if (PoliticalQuantities.FamilyOf(i) == 0) lean += s[i];
        lean = System.Math.Clamp(lean, 0, 1);
        if (!(lean >= 0 && lean <= 1)) lean = 0.5;   // 否定形＝NaNは中立へ
        if (p.Closed) return (lean + PrivilegeIntact(w)) / 2.0;
        if (p.IsModernElite) return (lean + StatePatronage(w)) / 2.0;
        return lean;
    }

    // =====================================================================================
    //  2. 階級別の走査（表の素）
    // =====================================================================================

    /// <summary>階級別の政治的重みと陣営（正規化済み・合計1／総重み0なら全て0）。
    /// <c>Weight[cls]</c>＝その階級の重みシェア、<c>Exec[cls]</c>＝そのうち執行権側へ傾いた分、
    /// <c>Lean[cls]</c>＝その階級の平均の傾き（算出不能は NaN）、<c>SoldierLean</c>＝兵卒（兵士POP・頭数加重）の傾き。</summary>
    public static (double[] Weight, double[] Exec, double[] Lean, double SoldierLean, bool Any) ClassScan(World w)
    {
        var cw = new double[ClassCount];
        var ce = new double[ClassCount];
        double solW = 0, solLean = 0;
        // ⚠小営業（第4束）は PettyInterestGate>0 のときだけ分類する＝呼ばないことが休眠の恒等（現物の作法）。
        //   HashSet は Contains のみに使い**列挙しない**（列挙順＝参照ハッシュ順は非決定＝決定論を破る）。
        var petty = w.Coef.PettyInterestGate > 0 ? InterestGroups.PettyPops(w) : null;
        foreach (var p in w.AllPops())
        {
            double a = PopExecutiveLean(p, w);
            if (!(a >= 0 && a <= 1)) a = 0.5;
            if (p.IsSoldier) { double sz = System.Math.Max(0, p.Size); solW += sz; solLean += sz * a; }
            double weight = PopWeight(p, w);
            if (!(weight > 0)) continue;
            int cls = p.Closed ? (int)Cls.OldRuling
                    : petty is not null && petty.Contains(p) ? (int)Cls.Petty
                    : p.IsModernElite ? (int)Cls.ModernElite
                    : (int)Cls.Mass;
            cw[cls] += weight;
            ce[cls] += weight * a;
        }
        double total = 0;
        for (int i = 0; i < ClassCount; i++) total += cw[i];
        var lean = new double[ClassCount];
        for (int i = 0; i < ClassCount; i++) lean[i] = cw[i] > 1e-12 ? ce[i] / cw[i] : double.NaN;
        if (!(total > 1e-12))
            return (new double[ClassCount], new double[ClassCount], lean, solW > 1e-12 ? solLean / solW : double.NaN, false);
        for (int i = 0; i < ClassCount; i++) { cw[i] /= total; ce[i] /= total; }
        return (cw, ce, lean, solW > 1e-12 ? solLean / solW : double.NaN, true);
    }

    // =====================================================================================
    //  3. 「王そのもの」と、執行権 vs 議会 の按分
    // =====================================================================================

    /// <summary>★**「王そのもの」**∈[0,1]＝正統性・財政・常備軍と官僚を養う能力
    /// <code>FiscalFundingRatio × (組織された武力 ＋ 行政効率)/2</code>
    /// ★これがあるので**社会的支持が全部剥がれても執行権はゼロにならない**。
    ///   「国庫が潤沢なら執行権は貴族を要らない」（ヨーゼフ2世・明治）と「財政破綻で貴族に譲歩する」（1787仏の名士会招集・
    ///   1789三部会）が**両方**創発する。⚠貴族の富を執行権の基盤に置かない＝防衛的近代化（王権が貴族と対立）を表現できるようにする。</summary>
    public static double StateCapacity(World w)
    {
        double fund = System.Math.Clamp(w.FiscalFundingRatio, 0, 1);
        if (!(fund >= 0 && fund <= 1)) fund = 0;
        double admin = System.Math.Clamp(w.AdminEfficiency, 0, 1);
        if (!(admin >= 0 && admin <= 1)) admin = 0;
        double v = fund * ((ArmyForce(w) + admin) / 2.0);
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>議会統制力∈[0,1]（−1センチネル/NaN は0側・1超は1へ）＝**形式上の**統制。
    /// ⚠実効は <see cref="ParliamentGrip"/>（歳入を止められるか × 年次審議の外に置かれた歳出）を掛けたもの。</summary>
    public static double ParliamentControl(World w)
    {
        double pc = w.ParliamentControl;
        if (!(pc >= 0 && pc <= 1)) return !(pc > 0) ? 0 : 1;
        return pc;
    }

    /// <summary>★★★**議会が実際に差し止められる歳入の割合**∈[0,1]（史学検証4＝1850年プロイセン憲法の原文で決着）。
    /// <code>
    /// 年次議決に載る歳入 ＝ 所得税 ＋ 累進課税                 ★毎年の査定機構が要る19世紀の発明
    /// 常設・条約の歳入   ＝ 人頭税 ＋ 土地税 ＋ 専売 ＋ 関税  ★第109条「既存の税および賦課は徴収され続ける」（専売＝広さ×標準率＝実効平均率。広さは家計の購入額加重なのでこの積は真の実効平均率）
    /// 起債               ＝ 国債残高/名目GDP                    ★第103条＝議会が実際に噛めた唯一の場所
    /// 掌握 ＝ 1 −(1 − 税の掌握)(1 − 起債への依存)               ★noisy-or＝どちらか一方でも噛めれば掌握できる
    /// </code>
    /// ★★プロイセン1862＝階級税（1820年法）・分類所得税（1851年）・地租・製粉屠畜税・関税同盟の条約関税がすべて
    ///   第109条で存続し、**議会が授権するのは歳出であって歳入ではない**（第99条）。1866年の免責法も
    ///   「法的に確定した予算なしに行われた**行政**」の赦免を求めた＝**歳入は一度も問題にならなかった**。
    ///   だから政府は借入ではなく経常収入で回した＝**起債の項が低いほど議会は噛めない**。
    /// ⚠分母が0（税も債務も無い）＝掌握できる歳入が無い → 0（倒し先を明示）。</summary>
    public static double ParliamentGrip(World w)
    {
        double annual = System.Math.Max(0, w.TaxRate) + System.Math.Max(0, w.ProgressiveTax);
        // ★専売の項（2026-09-20）＝**実効平均率**＝専売の広さ×標準率（standing の単位＝率を保つ）。
        //   ⚠ここは**政策軸を読んでいない**（PolicyAxis.StateMonopoly は同日に撤去した）＝読むのは実収入の率であり、
        //     第109条が言う「既存の税および賦課」＝常設歳入の分母の一部。プロイセンの製粉屠畜税がまさにこれ（上の引用）。
        //   ⚠0 を入れて済ませない＝分母が痩せて taxGrip = annual/(annual+standing) が跳ねる（専売を取っている国の議会掌握が一斉に上がる）
        //     ＝誰も頼んでいない挙動変更で、しかも史実と逆向き。
        double standing = System.Math.Max(0, w.PollTax) + System.Math.Max(0, w.LandTax)
                        + System.Math.Max(0, w.StateMonopolyBreadth) * System.Math.Max(0, w.Coef.StateMonopolyMargin) + System.Math.Max(0, w.Tariff);
        double tot = annual + standing;
        double taxGrip = !(tot > 1e-12) ? 0 : annual / tot;
        if (!(taxGrip >= 0 && taxGrip <= 1)) taxGrip = 0;
        double debtGrip = System.Math.Clamp(System.Math.Max(0, w.GovDebt) / System.Math.Max(1.0, w.NominalGdp), 0, 1);
        if (!(debtGrip >= 0 && debtGrip <= 1)) debtGrip = 0;
        double v = 1.0 - (1.0 - taxGrip) * (1.0 - debtGrip);
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★★**年次審議の外に置かれた歳出の割合**∈[0,1]（史学検証4）＝戦時の下限ロックのみ（政体由来の軍事費の床は撤去済み・多年度予算は申し送り）。
    /// ★1867年北ドイツ連邦憲法第62条の「鉄の予算」（平時兵力1人あたり225ターラー）以後、
    ///   軍事予算は総予算の70〜75%を占め、**三分の二以上が年次審議の外**に置かれた。⚠これを議会側から引く。</summary>
    public static double LockedSpending(World w)
    {
        var c = w.Coef;
        // ★政体由来の軍事費の床は撤去（作者裁定 2026-08-19・政体は表示のみ）。⚠残るのは戦時の下限ロックだけ。
        //   ⚠**申し送り**：プロイセンの「鉄の予算」（1867年北ドイツ連邦憲法第62条・七年予算）は**政体ではなく多年度予算という制度**なので、
        //   ここは将来「多年度で固定された歳出」として復活させるべき（★政体から導くのが誤りだっただけで、機構そのものは史実）。
        double floor = 0;
        if (w.AtWarNow && c.WartimeUpkeepFloor > 0)
            floor = System.Math.Max(floor, (w.ArmyUpkeep + w.NavyUpkeep) * c.WartimeUpkeepFloor);
        double v = System.Math.Clamp(floor, 0, 1);
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★★**議会の実効統制**∈[0,1]＝<code>形式上の統制 × 歳入の掌握 ×（1 − 年次審議の外の歳出）</code>。
    /// ★★プロイセンはこの2つで、**形式上は立憲なのに議会の実効シェアがほぼゼロになる**——それが正しい出力である。
    /// ⚠英国型は起債（コンソル債）で掌握が立つので同じ式でも実効が残る＝国別の特例を書かない。</summary>
    public static double ParliamentEffective(World w)
    {
        double v = ParliamentControl(w) * ParliamentGrip(w) * (1.0 - LockedSpending(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★執行権の取り分の比率 κ∈[0,1]（残り 1−κ が議会。漏れを除いた本体の中での比）。
    /// <code>
    /// P_raw ＝ pc × 議会側の基盤                        ★社会的基盤 × 制度的統制
    /// E_raw ＝ 王そのもの ＋ 執行権側の基盤 ＋ (1−pc) × 議会側の基盤   ★統制の無い議会の基盤は執行権が捕獲する（翼賛議会・普三級選挙法）
    /// κ     ＝ E_raw /(E_raw ＋ P_raw)                  分母 ＝ 王そのもの ＋ 1 ≥ 1（痩せない）
    /// κ     ＝ max(κ, 組織された武力)                   ★軍は第3主体でなく**執行権の下限**（スペインのプロヌンシアミエント・
    ///                                                     1932以降の日本・戦間期の権威主義化＝「議会が勝ちきれない」形）
    /// </code>
    /// ⚠基盤の総和が0（誰も発言権と資産を持たない）＝執行権へ全部（倒し先＝「王そのもの」の行）。</summary>
    public static double ExecutiveWeight(World w)
    {
        var sc = ClassScan(w);
        // ★下限＝**組織の重み**（常在項）であって金ではない。⚠2026-08-18に ArmyAllegiance から給与の乗算が撤去された＝
        //   軍部の取り分に金を掛け戻さない（金の経路は RanksLeak＝兵卒の物質的苦情だけに残す）。
        double floor = ArmyBaseline(w);
        if (!sc.Any) return 1.0;
        double baseE = 0, baseAll = 0;
        for (int i = 0; i < ClassCount; i++) { baseE += sc.Exec[i]; baseAll += sc.Weight[i]; }
        double baseP = baseAll - baseE;
        double pc = ParliamentEffective(w);
        double pRaw = pc * baseP;
        double eRaw = StateCapacity(w) + baseE + (1.0 - pc) * baseP;
        double den = eRaw + pRaw;
        if (!(den > 1e-12)) return 1.0;
        double k = eRaw / den;
        if (!(k >= 0 && k <= 1)) k = 1.0;
        return System.Math.Clamp(System.Math.Max(k, floor), 0, 1);
    }

    // =====================================================================================
    //  4. 軍部（常在項 ＋ 漏れ／将校 ＋ 兵卒）
    // =====================================================================================

    /// <summary>★組織された武力∈[0,1]＝**実際に兵へ届いている金**
    /// <code>軍事費（ArmyUpkeep＋NavyUpkeep）× 給与充足（FiscalFundingRatio × MilitaryReadiness）</code>
    /// ★給与充足は現物の <c>CrownParliament</c> の <c>pay</c> と同一式＝**`ArmyAllegiance` の「金」の項をこちらへ移した**。
    /// ⚠兵士人口シェアを重ねて掛けない（<see cref="World.MilitaryReadiness"/> が兵の実在を既に運んでいる＝二重計上）。</summary>
    public static double ArmyForce(World w)
    {
        double pay = System.Math.Clamp(w.FiscalFundingRatio * w.MilitaryReadiness, 0, 1);
        if (!(pay >= 0 && pay <= 1)) pay = 0;
        double spend = System.Math.Clamp(w.MilitarySpending, 0, 1);
        if (!(spend >= 0 && spend <= 1)) spend = 0;
        return spend * pay;
    }

    /// <summary>★★**文民制度の厚み**∈[0,1]＝議会・官僚・政党の3本の平均。
    /// <code>議会＝議会統制力 ／ 官僚＝Bureaucracy（猟官0/資格任用0.5/専門1） ／ 政党＝実効参政権の開放度</code>
    /// ★★★史学検証3の修正④：**軍部の定数項を全国共通の定数として置いてはいけない**——
    ///   純軍政の割合は普遍でなく**文民制度の厚みで決まる**（ラテンアメリカ全体で文民同盟型52%・純軍政26%だが、
    ///   スペイン14%・ブラジル8%・メキシコ10%・ポルトガル21%に対し**アンデス三国は約70%・チリ44%**）。
    ///   ギリシャの純軍政4件が小アジアの破局で文民政党の威信が焼けた直後に集中したのも同じ機構。
    /// ⚠国別の定数を1つも置かない＝地域差は**既存の制度の値の差**として出る（思想②＝データが運ぶ）。</summary>
    public static double CivilianInstitutions(World w)
    {
        double parliament = ParliamentEffective(w);   // ★形式上でなく**実効**（歳入を止められるか）で測る＝史学検証4
        double bureau = System.Math.Clamp(PoliticalQuantities.BureaucracyDepth(w.Bureaucracy), 0, 1);   // ★既存の唯一実装を読む
        double parties = System.Math.Clamp(Ideologies.Openness(w.Suffrage), 0, 1);
        if (!(parties >= 0 && parties <= 1)) parties = 0;
        // ★4本目＝正統性（史学検証5・東アジア）。**不処罰は累積する**（日本1928張作霖爆殺の行政処分のみ→三月事件の処罰なし→
        //   十月事件の20日謹慎→満州事変と朝鮮軍の越境が無処罰で当人は後に首相→五・一五の助命嘆願11万通）。
        //   ⚠これを新しいストックで書かない——**介入が成功して罰されなければ正統性が落ち、次の介入が容易になる**が
        //   ここに入っていれば自動で閉じる。断ち切ったのが「体制が覆せない唯一のアクター」だったことも正統性の側の量。
        double legitimacy = System.Math.Clamp(w.Legitimacy, 0, 1);
        if (!(legitimacy >= 0 && legitimacy <= 1)) legitimacy = 0;
        double v = (parliament + bureau + parties + legitimacy) / 4.0;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★軍部の**常在項**∈[0,1]＝<code>既存の政治力目盛り × 常備軍の対人口比 ×（1 − 文民制度の厚み）</code>。
    /// ★史学検証2の修正A：**軍は執行権の道具であり、道具が主人を食う**——漏れとは独立に常在項が要る
    ///   （露の近衛1801・独の軍事内閣1916-18・1930年代日本＝執行権が最強のときに軍の取り分も最大）。
    /// ★史学検証3の修正④：（1−議会統制力）だけでなく**文民制度の厚み**（議会・官僚・政党）の逆数にする＝
    ///   アンデス圏とイベリア・ブラジルの差が**国別の定数を置かずに**出る。硬性条件（英・米・蘭・白・丁・諾・瑞）も
    ///   同じ因子が構造的に満たす（3本とも厚い国では常在項が消える）。
    /// ⚠**宮廷クーデタは常在項にも漏れにも計上しない**（1801露・1825露・1903セルビア・1876オスマン＝
    ///   君主が代わるだけで体制型は不変）。過剰修正すると英国型の国にまで軍部が湧く。</summary>
    public static double ArmyBaseline(World w)
    {
        double pop = System.Math.Max(1.0, w.TotalPopulation);
        double soldiers = System.Math.Max(0, w.SoldierSize());
        double v = w.Coef.ArmyPoliticalWeight * (soldiers / pop) * (1.0 - CivilianInstitutions(w));
        if (!(v >= 0)) return 0;
        return System.Math.Min(1.0, v);
    }

    /// <summary>★将校団の**自律**∈[0,1]＝<code>1 −(1 − 軍の政治的優位)× 文民制度の厚み</code>。
    /// ⚠<see cref="OfficerGrievance"/> の素。軍の政治的優位が高いほど、文民制度が厚くても自律が残る。
    /// ★2026-08-18：入力を <see cref="PoliticalQuantities.MilitaryPoliticalRatio"/> へ差し替え（旧 MilitarismDegree＝軍事費×徴兵法の深さは
    ///   史学検証とエンジン実測で棄却＝再点火禁止。英1913は徴兵深度0なのに国防費が中央歳出の43%／実測で61カ国が0.350へ張り付き識別力ゼロ）。</summary>
    public static double OfficerAutonomy(World w)
    {
        double mil = System.Math.Clamp(PoliticalQuantities.MilitaryPoliticalRatio(w), 0, 1);
        if (!(mil >= 0 && mil <= 1)) mil = 0;
        double v = 1.0 - (1.0 - mil) * CivilianInstitutions(w);
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    /// <summary>★★★**将校の不満**∈[0,1]。⚠**兵卒とは別の入力から読む**（史学検証3の修正①）。
    /// <code>
    /// ① 自律の喪失 ＝ 1 − 将校団の自律      ★文民大臣への従属・訴追（伯1883-87の年金拠出義務化と新聞発言禁止／西1923ピカソ調書）
    /// ② 身分の剥奪 ＝ 将校団の貴族率 ×（1 − 特権保全）  ★将校貴族の地位そのものが削られる
    /// 将校の不満 ＝ (① ＋ ②)/2
    /// </code>
    /// ★★★**将校の反乱は賃金では動かない。地位・自律・名誉で動く**（伯1883-87／西1923／ボリビア1934＝
    ///   大統領が司令部更迭に前線へ来たのを先手で逮捕）。⚠同じ「軍事費」から将校と兵卒の両方を引くとこの分岐が消える。</summary>
    public static double OfficerGrievance(World w)
    {
        double lostAutonomy = 1.0 - OfficerAutonomy(w);
        double nob = ArmyNobility(w);
        double stripped = !(nob >= 0 && nob <= 1) ? 0 : nob * (1.0 - PrivilegeIntact(w));
        double v = (lostAutonomy + stripped) / 2.0;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★兵卒の**実質**の待遇∈[0,1]＝兵士POPの**補給の充足**（食料の台帳＋満足度の台帳）の人口加重平均。
    /// ★★「金」は実質で測る——**1917ロシアは金庫に金があってもパンが無く軍が離反した**。
    /// ★階層3分割（作者裁定2026-09-20・2026-09-22 既定化）の後は満足度の台帳に食料が載らない＝満足度を読むとパンが消える。
    ///   ⇒ 食料の台帳（FoodMet/FoodNeed）と満足度の台帳（NeedMet/NeedTotal＝日用品＋買えた嗜好品）を足した**補給の台帳**を読む
    ///   ＝分割前の満足度と同じ量（主食の重み1.0は旧台帳の重みそのもの＝新しい数も新しい状態もゼロ）。
    ///   読み手は2つで1本＝兵卒の漏れ（<see cref="RanksLeak"/>）と即応度（<see cref="Military"/>.Step）。
    /// ⚠兵士POPが居ない世界は 1.0（中立＝この因子で束縛を減らさない）。台帳が空の pop（最初の月次決算の前）も 1.0＝旧の満足度の初期値と同じ中立。</summary>
    public static double SoldierWelfare(World w)
    {
        double size = 0, sat = 0;
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            double sz = System.Math.Max(0, p.Size);
            double s = p.SupplyFill;   // 食料＋満足度の台帳（Pop.SupplyFill＝唯一の定義）
            size += sz; sat += sz * s;
        }
        if (!(size > 1e-12)) return 1.0;
        double v = sat / size;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    /// <summary>★将校団の階級構成（合計1）＝**将校は身分から出る**が、資格任用が開くほど平民将校が入る。
    /// <code>
    /// 開放 merit ＝ Bureaucracy（猟官0 / 資格任用0.5 / 専門1）
    /// 旧支配層 ＝ 重み ×(1 ＋(1 − merit))   ★閉鎖度のぶん過剰代表（売官制・身分制の将校団）
    /// 近代エリート ＝ 重み
    /// 大衆・小営業 ＝ 重み × merit           ★平民将校は資格任用の開放に比例
    /// </code>
    /// ⚠**徴兵法は将校団の開放度に入れない**（徴兵は兵卒の話）＝プロイセン（皆兵＋ユンカー将校団）が矛盾なく出る。
    /// ⚠**軍事的な有効性はここから一切下げない**（プロイセンの参謀本部は実力主義で当時最も近代的だった）。
    /// ⚠倒し先＝構成が定まらないときは大衆（兵は民から出る）。</summary>
    public static double[] OfficerComposition(World w)
    {
        var sc = ClassScan(w);
        var oc = new double[ClassCount];
        double merit = System.Math.Clamp(PoliticalQuantities.BureaucracyDepth(w.Bureaucracy), 0, 1);   // ★既存の唯一実装を読む
        oc[(int)Cls.OldRuling] = sc.Weight[(int)Cls.OldRuling] * (1.0 + (1.0 - merit));
        oc[(int)Cls.ModernElite] = sc.Weight[(int)Cls.ModernElite];
        oc[(int)Cls.Mass] = sc.Weight[(int)Cls.Mass] * merit;
        oc[(int)Cls.Petty] = sc.Weight[(int)Cls.Petty] * merit;
        double sum = 0;
        for (int i = 0; i < ClassCount; i++) { if (!(oc[i] >= 0)) oc[i] = 0; sum += oc[i]; }
        if (!(sum > 1e-12)) { for (int i = 0; i < ClassCount; i++) oc[i] = 0; oc[OfficerFallbackRow] = 1.0; return oc; }
        double done = 0;
        for (int i = 0; i < ClassCount - 1; i++) { oc[i] /= sum; done += oc[i]; }
        oc[ClassCount - 1] = 1.0 - done;   // 端数は最後の行へ＝合計が厳密に1
        if (!(oc[ClassCount - 1] >= 0)) oc[ClassCount - 1] = 0;
        return oc;
    }

    /// <summary>★軍部の**貴族率**＝将校団に占める旧支配層の割合（<see cref="OfficerComposition"/> の読み出し）。
    /// ★これが**親軍法を撤去した穴を埋める**（作者設計）——貴族将校の軍は執行権寄り・平民将校の軍は議会寄り
    ///   （<see cref="ArmyExecutiveTilt"/>）。⚠軍事的な有効性は下げない。</summary>
    public static double ArmyNobility(World w)
    {
        var oc = OfficerComposition(w);
        double v = oc[(int)Cls.OldRuling];
        return !(v >= 0 && v <= 1) ? double.NaN : v;
    }

    /// <summary>★観測の軍の帰属∈[−1,1]（+1＝執行権 / −1＝議会 / 0＝自律）＝
    /// <code>(ArmyAllegiance ＋ 任免の所在(OfficerAppointmentCrownward) ＋ (2×将校団の貴族率 − 1)) / 3</code>（貴族率が算出不能なら前2項の平均）
    /// ★現物の <see cref="World.ArmyAllegiance"/> は書き換えない。**親軍法の撤去で痩せた第2項を将校団の貴族率で埋める**
    ///   （作者設計）＝貴族将校の軍は執行権寄り・平民将校の軍は議会寄り。⚠掛け算の構造は <see cref="ArmyBinding"/> で保つ。</summary>
    public static double ArmyExecutiveTilt(World w)
    {
        double a = System.Math.Clamp(w.ArmyAllegiance, -1, 1);
        if (!(a >= -1 && a <= 1)) a = 0;
        double appoint = System.Math.Clamp(PoliticalQuantities.OfficerAppointmentCrownward(w), -1, 1);   // ★人事系譜＝誰が任命し昇進させたか
        if (!(appoint >= -1 && appoint <= 1)) appoint = 0;
        double nob = ArmyNobility(w);
        if (!(nob >= 0 && nob <= 1)) return System.Math.Clamp((a + appoint) / 2.0, -1, 1);
        double v = System.Math.Clamp((a + appoint + (2.0 * nob - 1.0)) / 3.0, -1, 1);
        return !(v >= -1 && v <= 1) ? a : v;
    }

    /// <summary>★軍（将校団）が体制へ束縛されている度合い∈[0,1]＝<code>|観測の帰属| × 命令を出す主権者の正統性</code>。
    /// ★★★⚠**「金＝帰属」は取り下げた**（史学検証4・欧州側15件の反証）——同一の給与で**3週間後に符号が反転**した
    ///   （ライヒスヴェーアは1920年3月にカップへの発砲を拒み、4月にルールの労働者1000人以上を殺した）。
    ///   **給与変数は符号を変えられない。** 正しい因果の矢は**正統性 → 官僚の服従 → 財政へのアクセス**であって金→忠誠ではない。
    /// ★したがって帰属の重みは金でなく、**人事系譜（誰が任命し昇進させたか＝将校団の貴族率）**と
    ///   **命令を出す主権者の正統性**から引く（組織の自律への脅威は <see cref="OfficerGrievance"/> が担う）。
    /// ⚠金の経路は <see cref="RanksLeak"/>（兵卒の物質的苦情）だけに残す。</summary>
    public static double ArmyBinding(World w)
    {
        double tilt = System.Math.Abs(ArmyExecutiveTilt(w));
        double leg = System.Math.Clamp(w.Legitimacy, 0, 1);
        if (!(leg >= 0 && leg <= 1)) leg = 0;
        double v = tilt * leg;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★★**命令が出されたか**∈[0,1]（史学検証4の追加機構(1)）＝<code>1 − 執行権の取り分 × 正統性</code>
    /// ＝**執行権が軍に命令を出せない/出さない度合い**。
    /// ★★イタリア1922はこれなしにはモデル化できない——プリエーゼ将軍はオルテ・チヴィタヴェッキア・アヴェッツァーノ・
    ///   セーニ・ヴィテルボで鉄道を切り、全接近路とテヴェレ川の橋を固め、当日朝に王冠への絶対的忠誠を再確認していた。
    ///   相手は「26,000名、貧弱な武装、食料なし、豪雨に濡れて」。参謀総長ディアズは王に
    ///   「陛下、軍は義務を果たしますが、試さない方がよろしいでしょう」と述べた。
    ///   **どの部隊も拒否していない——命令が一度も出されなかったからだ。**
    /// ⚠観測のみ＝軍部シェアが高くても、この値が高ければ何も起きない。</summary>
    public static double CommandWithheld(World w)
    {
        double leg = System.Math.Clamp(w.Legitimacy, 0, 1);
        if (!(leg >= 0 && leg <= 1)) leg = 0;
        double exec = Shares(w)[(int)Holder.Executive];
        double v = 1.0 - exec * leg;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    /// <summary>★★★**軍の内部規律が文民立法に依存する度合い**∈[0,1]（史学検証4の追加機構(3)）。
    /// <code>議会の実効統制 ×（1 − 軍国度）</code>
    /// ★★英国の反乱法（1689年勅裁4月3日）は**1年限りの時限法**で、毎年更新されなければ**軍の内部規律法そのものが失効する**
    ///   ——王は反乱・脱走を軍法会議にかける法的権能を失う。つまり**英国でクーデタを起こせば、起こした側の部下に対する
    ///   権威が同時に溶ける**（年次陸軍法として1881年陸軍法まで継承）。
    /// ★★これで小国群（英・米・蘭・白・丁・諾・瑞）の軍部シェア≈0 が**例外でなく一般則**として出る＝国別の定数を置かない。
    ///   共通する構造（交代のある立憲政治／小規模な民兵・徴集軍／**軍が政治的法人でないこと**／軍とは別の警察・憲兵／
    ///   実際にコストを課せる組織労働）のうち、前3つがこの1式に畳まれている（組織労働は <see cref="StrikePower"/>）。</summary>
    public static double DisciplineDependsOnParliament(World w)
    {
        double mil = System.Math.Clamp(PoliticalQuantities.MilitaryPoliticalRatio(w), 0, 1);
        if (!(mil >= 0 && mil <= 1)) mil = 0;
        double v = ParliamentEffective(w) * (1.0 - mil);
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★★**組織労働の拒否権**∈[0,1]（史学検証4の追加機構(2)）＝<code>大衆の闘争度 × 労働組合の合法性</code>。
    /// ★★**ストライキは軍の士気への修正項ではなく、独自の能力を持つ別のアクター**である——
    ///   ポーランド1926（鉄道員のストがポズナンからの増援を遅らせた）／カップ一揆（1200万人のゼネスト）／
    ///   デンマーク1920の復活祭危機（**終わらせたのは兵士ではなく労働総同盟のゼネスト最後通牒**・復活祭の朝4時10分）／
    ///   スペイン1932サンフルハダ（セビーリャのゼネストが折った）。
    /// ⚠観測のみ。⚠労働組合が非合法なら0＝ストライキという手段が制度的に存在しない。</summary>
    public static double StrikePower(World w)
    {
        double union = w.LaborUnion switch { LaborUnionLaw.Tolerated => 1.0, LaborUnionLaw.Controlled => 0.5, _ => 0.0 };
        double mil = System.Math.Clamp(Politics.AvgMassMilitancy(w), 0, 1);
        if (!(mil >= 0 && mil <= 1)) mil = 0;
        double v = union * mil;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★**兵卒の漏れ**∈[0,1]＝<code>兵の政治的重み ×（1 − 兵卒の実質の待遇）×（1 − 規律の文民依存）×（1 − 組織労働の拒否権）</code>。
    /// ⚠**物質的苦情で動く**（史学検証3）＝ラ・グランハ1836（給与3か月未払）／サンヒル1866（昇進の天井と年金差別）／
    ///   ブラジル1910シバータ（給与引上げ・給食改善・勤務時間制限の要求書）／チリ1931コキンボ（30%減俸）／
    ///   キューバ1933（未払給与と昇進）／エルサルバドル1931（全軍給与の支払停止）。
    /// ⚠**ここだけが「金」の経路である**（帰属＝<see cref="ArmyBinding"/> からは金を外した＝下記）。</summary>
    public static double RanksLeak(World w)
    {
        double pop = System.Math.Max(1.0, w.TotalPopulation);
        double weight = System.Math.Clamp(w.Coef.ArmyPoliticalWeight * (System.Math.Max(0, w.SoldierSize()) / pop), 0, 1);
        if (!(weight >= 0 && weight <= 1)) weight = 0;
        double v = weight * (1.0 - SoldierWelfare(w)) * (1.0 - DisciplineDependsOnParliament(w)) * (1.0 - StrikePower(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★**将校の漏れ**∈[0,1]＝
    /// <code>常在項 × 将校の不満 ×（1 − 帰属の束縛）×（1 − 規律の文民依存）×（1 − 組織労働の拒否権）</code>。
    /// ⚠**制度側の量で動く**（賃金では動かない）。★「解隊命令が最大の忠誠ショック」（1920年2月29日のエアハルト旅団解散命令）
    ///   は <see cref="OfficerGrievance"/> の「自律の喪失」に入る。</summary>
    public static double OfficerLeak(World w)
    {
        double v = ArmyBaseline(w) * OfficerGrievance(w) * (1.0 - ArmyBinding(w))
                 * (1.0 - DisciplineDependsOnParliament(w)) * (1.0 - StrikePower(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★軍部の総主張∈[0,1]＝<code>1 −(1 − 常在項)(1 − 兵卒の漏れ)(1 − 将校の漏れ)</code>（noisy-or＝自己有界）。
    /// ★史学検証2の修正A「軍部シェア＝**定数項＋漏れの取り分**」＋史学検証3の修正①「将校と兵卒は別の入力」の実装。</summary>
    public static double ArmyClaim(World w)
    {
        double v = 1.0 - (1.0 - ArmyBaseline(w)) * (1.0 - RanksLeak(w)) * (1.0 - OfficerLeak(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★革命勢力∈[0,1]＝**制度が吸収しそこねた組織的不満**＝大衆の闘争度 ×（1 − 参政権の開放度）。
    /// ── ⚠<see cref="Politics.RevolutionPressure"/> を使った旧実装は実データで棄却された（2026-08-18・1800/1815断面）──
    /// あれは <c>clamp(動員÷実効保持力, 0, 1)</c> ＝**政体遷移の敷居判定に使う述語**であり、動員が保持力を超えた瞬間に1へ飽和する。
    /// 取り分（シェア）として読むと張り付き、noisy-or 経由で漏れ→1・本体→0 になる。実測では
    /// **1815年のイギリスが革命95%・アメリカが97%**でパイを丸ごと飲み、議会の列が全世界で0%になっていた
    /// （しかもこの飽和が「議会の掌握が痩せている」という別の欠陥を覆い隠していた＝欠陥Aの発見が遅れた）。
    /// ── 置き換えの根拠 ────────────────────────────────────────────────
    /// 作者の設計「権力は執行権と議会の奪い合いで、**漏れ**を軍部と革命勢力が担う」に照らすと、革命勢力の取り分は
    /// 「保持力に勝てるか」ではなく「**制度に入りそこねた不満がどれだけあるか**」でなければならない。
    /// 参政権を持つ大衆の闘争度は議会へ流れ、持たない大衆の闘争度が革命へ流れる＝英1832/1867が運動を吸収した側と、
    /// チャーティズムが参政権の外にあったから街頭に出た側が、同じ機構の両面になる。
    /// ⚠新しい状態も定数も作らない（<see cref="Politics.AvgMassMilitancy"/> と <see cref="Ideologies.Openness"/> の積）。
    /// ⚠飽和しない：参政権が開くほど0へ向かうので、英米蘭のような国が革命でパイを飲むことが構造的に起きない。</summary>
    public static double RevolutionLeak(World w)
    {
        double unabsorbed = 1.0 - Ideologies.Openness(w.Suffrage);          // 参政権の外に置かれた度合い（議会へ流れない分）
        double v = Politics.AvgMassMilitancy(w) * unabsorbed;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★漏れの合計∈[0,1]＝<code>1 −(1 − 軍部の総主張)(1 − 革命勢力)</code>（noisy-or）。
    /// ★現物の作法（<c>World.OversightV2</c> と同型・汚職8f の裁定）を流用＝**自己有界**。
    /// ⚠単純な和にすると執行権/議会が負になる＝取り分でなくなる。</summary>
    public static double Leak(World w)
    {
        double v = 1.0 - (1.0 - ArmyClaim(w)) * (1.0 - RevolutionLeak(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★★軍の**拒否権**の水準∈[0,1]（クーデタの手前の段階）。
    /// <code>制度の梃子 × min(1, 軍部の総主張 ／ PartnerViableShare)　　制度の梃子 ＝ 1 −(1 − 軍国度)× 議会統制力</code>
    /// ★史学検証2の修正B：**史実では軍の政治力の大半はクーデタでなく拒否権として行使された**——
    ///   日本1912の二個師団増設問題（陸相の単独辞職＋後任を出さず内閣崩壊＝**予算権を持つ側が予算以外の手段で負けた**）・
    ///   プロイセンの軍事内閣・オスマンの帷幄上奏・スペインのフンタ・英カラーの反抗1914・伊1922の不作為。
    /// ★**発火の梃子を軍の政治的優位（軍の圧力×(1−文民制度の弁)・2026-08-18に差し替え）が上げる形にした**＝
    ///   軍部大臣現役武官制のような制度を、**統帥権という独立の状態変数を足さずに**表現する。⚠観測のみ。</summary>
    public static double VetoPower(World w)
    {
        double mil = System.Math.Clamp(PoliticalQuantities.MilitaryPoliticalRatio(w), 0, 1);
        if (!(mil >= 0 && mil <= 1)) mil = 0;
        double lever = 1.0 - (1.0 - mil) * ParliamentEffective(w);
        double organized = System.Math.Min(1.0, ArmyClaim(w) / PartnerViableShare);
        double v = lever * organized;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    // =====================================================================================
    //  5. ★表（計算はここ1本。取り分も貴族率もここからの導出）
    // =====================================================================================

    /// <summary>★★★**階級 × 機関の重み表**（全セルの合計が厳密に1）。<c>Table(w)[cls][organ]</c>。
    /// <code>
    /// 執行権 ＝ 王そのもの ＋ Σ 支えている各階級                （倒し先＝王そのもの）
    /// 議会   ＝ Σ 参政権を通じて支えている各階級 × 議会統制力    （倒し先＝大衆）
    /// 将校   ＝ 軍部 ×(1 − 徴兵法の深さ) を将校団の階級構成へ    ★志願兵制ほど将校団に重みが乗る
    /// 兵卒   ＝ 軍部 × 徴兵法の深さ                             ★国民皆兵ほど兵卒に重みが乗る（1917露・1918キール）
    /// 革命   ＝ 革命勢力の取り分（担い手＝大衆）
    /// </code>
    /// ⚠兵卒と革命勢力の行を**大衆に固定**しているのは、現物の担い手（兵士POP／改革派＋急進派の動員）が
    ///   いずれも大衆層だからである。⚠将校POPは**作らない**（POP追加は Pops.Count が変わり全世界の軌道が動く）。</summary>
    public static double[][] Table(World w)
    {
        var t = new double[ClassCount][];
        for (int i = 0; i < ClassCount; i++) t[i] = new double[OrganCount];

        var sc = ClassScan(w);
        double pc = ParliamentEffective(w);
        double m = ArmyClaim(w), r = RevolutionLeak(w);
        double leak = 1.0 - (1.0 - m) * (1.0 - r);
        if (!(leak >= 0 && leak <= 1)) leak = 0;
        double den = m + r;
        double army = !(den > 1e-12) ? (LeakFallbackHolder == (int)Holder.Army ? leak : 0) : leak * (m / den);
        double body = 1.0 - leak;
        double k = ExecutiveWeight(w);
        double exec = body * k;
        double parl = body - exec;
        double rev = 1.0 - exec - parl - army;
        if (!(rev >= 0)) rev = 0;

        // --- 執行権の列：王そのもの ＋ 各階級（傾き分 ＋ 統制の無い議会の基盤の捕獲分）---
        var execRaw = new double[ClassCount];
        execRaw[(int)Cls.Sovereign] = StateCapacity(w);
        for (int i = 0; i < ClassCount; i++)
        {
            if (i == (int)Cls.Sovereign) continue;
            execRaw[i] = sc.Exec[i] + (1.0 - pc) * (sc.Weight[i] - sc.Exec[i]);
            if (!(execRaw[i] >= 0)) execRaw[i] = 0;
        }
        Spread(t, (int)Organ.Executive, exec, execRaw, ExecFallbackRow);

        // --- 議会の列：参政権を通じて支えている各階級 × 議会統制力 ---
        var parlRaw = new double[ClassCount];
        for (int i = 0; i < ClassCount; i++)
        {
            if (i == (int)Cls.Sovereign) continue;
            parlRaw[i] = pc * (sc.Weight[i] - sc.Exec[i]);
            if (!(parlRaw[i] >= 0)) parlRaw[i] = 0;
        }
        Spread(t, (int)Organ.Parliament, parl, parlRaw, ParlFallbackRow);

        // --- 軍部の列：将校（階級構成をもつ・制度で動く）と兵卒（大衆・物質で動く）---
        //   ⚠2つは**別の入力**から読む（史学検証3の修正①）＝将校＝常在項＋制度的不満／兵卒＝物質的不満。
        //   同じ「軍事費」から両方を引くと、階級による引き金の分岐（将校＝地位・自律・名誉／兵卒＝給与・食糧）が消える。
        double officersRaw = ArmyBaseline(w) + OfficerLeak(w);
        double ranksRaw = RanksLeak(w);
        double branchSum = officersRaw + ranksRaw;
        double ranks = !(branchSum > 1e-12) ? 0 : army * (ranksRaw / branchSum);
        double officers = army - ranks;
        if (!(officers >= 0)) { officers = army; ranks = 0; }
        Spread(t, (int)Organ.Officers, officers, OfficerComposition(w), OfficerFallbackRow);
        t[(int)Cls.Mass][(int)Organ.Ranks] = ranks;

        // --- 革命勢力の列：担い手は大衆 ---
        t[(int)Cls.Mass][(int)Organ.Revolution] = rev;
        return t;
    }

    // 列の合計が amount になるように raw の比で配る。⚠raw が全て0なら fallback 行へ全部（倒し先の明示）。
    // 端数は最後の非ゼロ行へ寄せる（合計が厳密に amount＝複合代入の罠を避ける2文）。
    private static void Spread(double[][] t, int organ, double amount, double[] raw, int fallbackRow)
    {
        if (!(amount > 0)) return;
        double sum = 0;
        for (int i = 0; i < ClassCount; i++) { if (!(raw[i] >= 0)) raw[i] = 0; sum += raw[i]; }
        if (!(sum > 1e-12)) { t[fallbackRow][organ] = amount; return; }
        double done = 0; int last = fallbackRow;
        for (int i = 0; i < ClassCount; i++)
        {
            if (!(raw[i] > 0)) continue;
            double v = amount * (raw[i] / sum);
            t[i][organ] = v; done += v; last = i;
        }
        double rest = amount - done;
        if (rest > 0 || rest < 0) t[last][organ] += rest;   // 端数の吸収（負なら微小な丸め戻し）
        if (!(t[last][organ] >= 0)) t[last][organ] = 0;
    }

    /// <summary>★4主体の取り分＝**表の列の合計**（軍部＝将校＋兵卒）。合計は厳密に1・各要素[0,1]。</summary>
    public static double[] Shares(World w)
    {
        var t = Table(w);
        var s = new double[HolderCount];
        for (int i = 0; i < ClassCount; i++)
        {
            s[(int)Holder.Executive] += t[i][(int)Organ.Executive];
            s[(int)Holder.Parliament] += t[i][(int)Organ.Parliament];
            s[(int)Holder.Army] += t[i][(int)Organ.Officers] + t[i][(int)Organ.Ranks];
            s[(int)Holder.Revolution] += t[i][(int)Organ.Revolution];
        }
        for (int h = 0; h < HolderCount; h++) if (!(s[h] >= 0 && s[h] <= 1)) s[h] = s[h] > 1 ? 1 : 0;   // 否定形＝NaNは0へ（Math.Clamp は NaN を通す）
        double tot = s[0] + s[1] + s[2] + s[3];
        if (!(tot > 1e-12)) { var z = new double[HolderCount]; z[(int)Holder.Executive] = 1.0; return z; }
        double done = 0;
        for (int h = 0; h < HolderCount - 1; h++) { s[h] /= tot; done += s[h]; }
        s[HolderCount - 1] = 1.0 - done;
        if (!(s[HolderCount - 1] >= 0)) s[HolderCount - 1] = 0;
        return s;
    }

    /// <summary>★機関の**階級構成**（表の【行】の比）＝その機関を支えているのは誰か。列の合計が0なら NaN（＝算出不能・勝手に埋めない）。</summary>
    public static double ClassRatio(World w, Organ organ, Cls cls)
    {
        var t = Table(w);
        double sum = 0;
        for (int i = 0; i < ClassCount; i++) sum += t[i][(int)organ];
        if (!(sum > 1e-12)) return double.NaN;
        double v = t[(int)cls][(int)organ] / sum;
        return !(v >= 0 && v <= 1) ? double.NaN : v;
    }

    /// <summary>★**執行権の貴族率**＝⚠**1 − これ ＝ 集権度**。
    /// 高い＝王は**貴族連合の議長**（ポーランドのマグナート・幕藩体制・神聖ローマ）／
    /// 低い＝**官僚と常備軍で直接統治**（ルイ14世・プロイセンの官僚国家・明治）。
    /// ★分母に「王そのもの」の行が入っているのが要＝官僚と常備軍が育つほど貴族が要らなくなる＝
    ///   **封建→絶対王政が1本の動きで表現できる**（作者が「中央集権度は問わない」としたものが無料で手に入る）。</summary>
    public static double ExecutiveNobility(World w) => ClassRatio(w, Organ.Executive, Cls.OldRuling);

    /// <summary>★**議会の貴族率**。高い＝英1783（後援者統制309〜359議席・上院は文字通り貴族院・**穀物法1815**）・
    /// ハンガリー県議会・ポーランドのセイム・各国の貴族院。1832/1867/1884の改革で下がる。</summary>
    public static double ParliamentNobility(World w) => ClassRatio(w, Organ.Parliament, Cls.OldRuling);

    /// <summary>★**将校団と兵卒の乖離**∈[0,1]＝<code>将校団の平均の傾き と 兵卒の傾き の差</code>。
    /// ★軍部を1つのスカラーで持つと将校団と兵卒が打ち消し合って**両方消える**——その指摘への答え。
    ///   1917ロシア（貴族将校は皇帝寄り・農民兵卒は離反）・1836ラ・グランハ・1857セポイ・1918キール・1933キューバ軍曹。
    /// ⚠将校POPは作らないので、将校団の傾きは**階級構成 × 各階級の平均の傾き**で合成する。
    /// ⚠どちらかが算出不能なら NaN。</summary>
    public static double OfficerRankSplit(World w)
    {
        var sc = ClassScan(w);
        if (!(sc.SoldierLean >= 0 && sc.SoldierLean <= 1)) return double.NaN;
        var oc = OfficerComposition(w);
        double lean = 0, wsum = 0;
        for (int i = 0; i < ClassCount; i++)
        {
            if (!(oc[i] > 0)) continue;
            if (!(sc.Lean[i] >= 0 && sc.Lean[i] <= 1)) continue;
            lean += oc[i] * sc.Lean[i]; wsum += oc[i];
        }
        if (!(wsum > 1e-12)) return double.NaN;
        double v = System.Math.Abs(lean / wsum - sc.SoldierLean);
        return !(v >= 0 && v <= 1) ? double.NaN : v;
    }

    // =====================================================================================
    //  6. 軍の身の振り方（★観測のみ＝どの挙動にも繋がない）
    // =====================================================================================

    /// <summary>★軍事独裁を狙うか（史学検証2の修正C＝**絶対値の閾値を捨てて相対条件へ**）＝
    /// <code>軍部 &gt; 執行権 かつ 軍部 &gt; 議会 かつ 軍部 &gt; 革命勢力 かつ max(執行権,議会,革命) &lt; PartnerViableShare</code>
    /// ★＝**組む相手がいないときだけ単独支配を選ぶ**（ペルー1930・ポルトガル1926・チリ1927・ギリシャ1925）。
    ///   軍が最も圧倒的だった局面でも相手がいれば文民に渡す（ギリシャ1909・ポルトガル1910・オスマン1908）。</summary>
    public static bool SeeksMilitaryRule(World w) => SeeksMilitaryRule(Shares(w));

    private static bool SeeksMilitaryRule(double[] s)
    {
        double army = s[(int)Holder.Army];
        double other = System.Math.Max(s[(int)Holder.Executive], System.Math.Max(s[(int)Holder.Parliament], s[(int)Holder.Revolution]));
        return army > other && other < PartnerViableShare;
    }

    /// <summary>★軍が手を組む相手（単独支配を選ばないときは 議会/執行権/革命勢力 のいずれかに付く）。
    /// <code>引力(主体) ＝ その主体の取り分 ×（1 ＋ 思想の近さ）      ★金 × 思想（掛け算＝加算に崩さない）</code>
    ///   執行権 ＝ max(0, +観測の帰属)／議会 ＝ max(0, −観測の帰属)／革命 ＝ 1 − 正統性。
    /// ⚠同点は 執行権 → 議会 → 革命勢力 の順（決定論＝比較の順序を固定する）。</summary>
    public static Holder Ally(World w)
    {
        var s = Shares(w);
        if (SeeksMilitaryRule(s)) return Holder.Army;
        double a = ArmyExecutiveTilt(w);
        double leg = System.Math.Clamp(w.Legitimacy, 0, 1);
        if (!(leg >= 0 && leg <= 1)) leg = 0;
        double pullExec = s[(int)Holder.Executive] * (1.0 + System.Math.Max(0, a));
        double pullParl = s[(int)Holder.Parliament] * (1.0 + System.Math.Max(0, -a));
        double pullRev = s[(int)Holder.Revolution] * (1.0 + (1.0 - leg));
        var best = Holder.Executive; double bestV = pullExec;
        if (pullParl > bestV) { best = Holder.Parliament; bestV = pullParl; }
        if (pullRev > bestV) best = Holder.Revolution;
        return best;
    }

    /// <summary>★★★軍の政治関与の**4つの水準**（Finer）＝**影響 → 恐喝 → 更迭 → 簒奪**。
    /// ★★中心命題は「なぜ軍が政治に関与するのかではなく、**なぜ関与しないことがあるのか**を問うべきだ」＝
    ///   **服従の側が説明を要する**。抑えているのは力の不足ではなく、**誰も従わない後継の定式しか持てないこと**
    ///   （＝<see cref="CivilianInstitutions"/> と <see cref="DisciplineDependsOnParliament"/>）。
    /// ★Nordlinger の3類型が出力段に対応する：<b>MODERATOR</b>＝特定政策への拒否権を保持し統治しない（影響/恐喝）／
    ///   <b>GUARDIAN</b>＝自ら権力を取るが一時的（ブラジル1930の軍事評議会が10日で文民に引き渡した／ギリシャ1909＝
    ///   退位も王政廃止も求めず統治も望まなかった＝更迭）／<b>RULER</b>＝体制を支配し社会と経済の大部分を統制しようとする
    ///   （簒奪・**稀であるはず**）。⚠観測のみ＝挙動には繋がない。⚠新しい閾値を1つも置かない。</summary>
    public enum MilitaryLevel { Influence = 0, Blackmail = 1, Displacement = 2, Supplantment = 3 }
    public static readonly string[] MilitaryLevelNames = ["影響", "恐喝", "更迭", "簒奪"];

    /// <summary>★4つの水準の判定（既存の <see cref="PartnerViableShare"/> と取り分の比較だけで決める）。
    /// <code>
    /// 簒奪 ＝ 単独支配の相対条件（SeeksMilitaryRule）
    /// 更迭 ＝ 軍部の取り分 &gt; 執行権の取り分     ★内閣を倒せるが後継は文民（GUARDIAN・伯1889/1930）
    /// 恐喝 ＝ 拒否権 ≥ PartnerViableShare        ★日本1912の二個師団増設問題（MODERATOR）
    /// 影響 ＝ それ以外
    /// </code></summary>
    public static MilitaryLevel Level(World w)
    {
        var s = Shares(w);
        // ★★史学検証5：**議会があるところでは、軍の最強の武器はクーデタではなく憲法論だった**
        //   （日本の統帥権干犯・中国の憲法重大信条十九条・コーラートの七項目）。しかも日本ではその武器を軍に渡したのは
        //   **文民野党**だった（犬養と鳩山が1930年4月に統帥権を浜口攻撃に使い、25か月後に海軍将校が同じ論理で犬養を殺した）。
        //   ⇒ **議会が実効を持つ国では、出力は簒奪/更迭でなく「拒否」で止まる**（新しい閾値を置かず PartnerViableShare を再利用）。
        bool constitutional = ParliamentEffective(w) >= PartnerViableShare;
        if (!constitutional && SeeksMilitaryRule(s)) return MilitaryLevel.Supplantment;
        if (!constitutional && s[(int)Holder.Army] > s[(int)Holder.Executive]) return MilitaryLevel.Displacement;
        if (VetoPower(w) >= PartnerViableShare || (constitutional && s[(int)Holder.Army] > s[(int)Holder.Executive]))
            return MilitaryLevel.Blackmail;
        return MilitaryLevel.Influence;
    }

    /// <summary>★★**既成事実の追認**∈[0,1]（史学検証5）＝<code>軍部の総主張 × 命令の留保</code>。
    /// ★★若槻は1931年9月23日に**自分が禁じた侵略に事後的に予算をつけた**——理由は**部隊が既に川を渡っていたから**。
    ///   財政のレバーは、**事実を作れる軍に対しては弱い**。「軍は命令されたか」ゲート（<see cref="CommandWithheld"/>）の裏返しで、
    ///   **軍が命令なしに動いた後は執行権が追認するしかない**。⚠観測のみ＝挙動には繋がない。</summary>
    public static double FaitAccompli(World w)
    {
        double v = ArmyClaim(w) * CommandWithheld(w);
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    /// <summary>★軍政の第2の引き金＝**防衛反応型**（1936スペイン・1973チリ・1961トルコ）＝
    /// 「執行権・議会いずれも信用できない」ではなく「**一方が強すぎて軍自身が脅かされた**」型
    /// ＝軍の取り分が急激に削られた度合い∈[0,1]（前期比の相対削減率）。
    /// ⚠時間差の観測なので**2時点の取り分を引数で受け取る**（World に履歴を作らない）。
    /// ⚠前期が0＝削られようがない → 0へ倒す（分母のガードは絶対値でなく「正であること」で書く）。</summary>
    public static double CutSeverity(double previousArmyShare, double currentArmyShare)
    {
        if (!(previousArmyShare > 1e-9)) return 0;
        double v = (previousArmyShare - currentArmyShare) / previousArmyShare;
        if (!(v >= 0)) return 0;
        return System.Math.Min(1.0, v);
    }
}
