namespace PriceOfBread.SimCore;

/// <summary>★権力の取り分の**部品**（2026-08-18 の作者設計「権力の取り分を同一スケールへ」＝docs/202608/20260818_権力の取り分を同一スケールへ_部分完了.md から、
/// シムが読む部品だけが残った静的な純関数の置き場）。
/// ★2026-09-26（棚卸し pol-09・回0-3）：**表モデル**（階級×機関の表 Table・取り分 Shares・軍部の常在項と漏れ・拒否権・軍政の判定 SeeksMilitaryRule／Level／Ally・
///   将校団の構成・既成事実の追認ほか約30関数・約600行）を削除した＝SimCore と App に読み手が無く（Runner の計器 powershares とテストだけ）、
///   翌日（2026-08-19〜20）の <see cref="PowerAffinity"/> が同じ問い（正統性の按分・軍の独占）を別の式で持っていた＝軍政の判定がエンジン・画面・診断の3本で食い違う元だった。
///   経緯と史学検証（1〜5）は上の doc と git の履歴（`git show b7fd1c8:PriceOfBread.SimCore/PowerShares.cs`）にある。
/// 残る部品と読み手：<see cref="PrivilegeIntact"/>／<see cref="StatePatronage"/>（Ideologies の王党派・反動）・<see cref="ParliamentControl"/>（PowerAffinity）・
///   <see cref="ParliamentEffective"/>（PoliticalQuantities・画面）とその2因子 <see cref="ParliamentGrip"/>／<see cref="LockedSpending"/>・
///   <see cref="CivilianInstitutions"/>（PowerAffinity）・<see cref="SoldierWelfare"/>（Military の即応度）・<see cref="PartnerViableShare"/>（PowerAffinity）。
/// 新しい状態は作らない＝純関数＝SaveGame 不変。
/// ⚠NaN ガードは**否定形**（`!(x &gt;= a &amp;&amp; x &lt;= b)`）で書く＝ Math.Clamp は NaN を除去しない。
/// ⚠比のガードは絶対値で書かない＝必ず正規化し、分母が痩せたときの倒し先を明示する。</summary>
public static class PowerShares
{
    /// <summary>★「組む相手として成立する取り分」の下限（史学検証2の修正C）。
    /// ⚠旧＝「軍部が単独50%以上なら軍事独裁」という**絶対値の閾値**。実測では純粋軍政は約4件に1件で、
    ///   単独支配を選んだ国は軍が強かったのではなく**組む相手がいなかった**（ペルー1930・ポルトガル1926・チリ1927・ギリシャ1925）。
    ///   逆に軍が最も圧倒的だった局面（ギリシャ1909・ポルトガル1910・オスマン1908）では**文民に権力を渡している**。
    ///   → 条件を**相対値**へ＝規則が1つ減る。</summary>
    public const double PartnerViableShare = 0.5;

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

    /// <summary>★兵卒の**実質**の待遇∈[0,1]＝兵士POPの**補給の充足**（食料の台帳＋満足度の台帳）の人口加重平均。
    /// ★★「金」は実質で測る——**1917ロシアは金庫に金があってもパンが無く軍が離反した**。
    /// ★階層3分割（作者裁定2026-09-20・2026-09-22 既定化）の後は満足度の台帳に食料が載らない＝満足度を読むとパンが消える。
    ///   ⇒ 食料の台帳（FoodMet/FoodNeed）と満足度の台帳（NeedMet/NeedTotal＝日用品＋買えた嗜好品）を足した**補給の台帳**を読む
    ///   ＝分割前の満足度と同じ量（主食の重み1.0は旧台帳の重みそのもの＝新しい数も新しい状態もゼロ）。
    ///   読み手は即応度（<see cref="Military"/>.Step）＝飢えた軍は戦えない（兵卒の漏れ RanksLeak は表モデルと一緒に 2026-09-26 に削除＝pol-09）。
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
}
