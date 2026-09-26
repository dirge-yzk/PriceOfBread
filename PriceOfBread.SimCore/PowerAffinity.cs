namespace PriceOfBread.SimCore;

/// <summary>★正当性の内訳＝**距離1本で割る**（作者設計 2026-08-19・docs/202608/20260819_正当性の内訳を距離で割る_部分完了.md）。
///
/// <code>
/// 正当性     ＝ 王権正当性 ＋ 議会正当性      ← ★非闘争的な支持（誰に統治されたいか）
/// 1 − 正当性 ＝ 軍部正当性 ＋ 革命勢力        ← ⚠闘争的な圧力（誰が体制を壊すか）
/// </code>
///
/// ★★★**分岐を1つも書かない。**あるのは【座標表】と【距離関数】と【闘争度の乗算】だけ＝
/// 思想⑧「例外の例外は無視する」が構造的に強制される（`if (国)` `if (政体)` を書く場所が無い）。
///
/// <code>
/// 各POPについて
///   重み w ＝ Size × 発言権            ⚠意識は掛けない（後述）
///   闘争度 m ＝ Militancy
///
///   aC ＝ SupportAt(popの思想分布, Absolutist座標,            slope)   ★王権への親和
///   aP ＝ SupportAt(popの思想分布, Republic座標,              slope)   ★議会への親和
///   aA ＝ SupportAt(popの思想分布, MilitaryDictatorship座標,  slope)   ★軍部への親和
///   aR ＝ (AffMax + AffMin) − max(aC, aP, aA)                          ⚠革命＝どの機関からも遠いこと
///
///   (1−m)×w → 王権/議会へ softmax(aC, aP) で按分
///     m  ×w → 軍部/革命へ softmax(aA, aR) で按分
/// </code>
///
/// ⚠**機関の座標は既存の政体座標をそのまま流用する**（<see cref="Ideologies.BasePolityCoord"/>）＝新しい定数ゼロ。
/// ⚠**革命だけが座標を持たない**＝「最も近い機関からの遠さ」を同じレンジへ折り返したもの。
///   これは旧 `PowerShares.RevolutionLeak`（＝闘争度 ×(1−参政権の開放度)・表モデルと一緒に 2026-09-26 に削除＝pol-09）の一般化である。
///   `Openness(Suffrage)` は**議会への親和の粗い代理**でしかなく「参政権が無い＝全部革命」になるが、
///   距離なら**王権が受け皿になっている場合**も拾える（★ロシアの農民は参政権が無くても皇帝を信じていた＝革命勢力ではない）。
///
/// ★**意識（Consciousness）を重みに掛けない理由**＝二重計上を避けるため。
///   意識は既に <c>Ideology.cs</c> で思想の形成に効いており（★啓蒙→自由主義 r[2] += 2.5×意識×prog）、
///   思想座標の【参加】軸として親和に入っている。作者設計の「王権←政治意識なし／議会←政治意識あり」は
///   **参加軸から自動的に出る**＝意識を直接使う必要が無い。
///   ⚠<see cref="Ideologies.National"/> は意識を掛けるが、あちらは「政体引力を動かすのは意識ある層」という別の問い。
///   こちらは**意識の無い層こそが王権正当性の母数**なので、掛けると母数が消える。
///
/// ★**ラチェットが破れる理由**＝意識は参加軸の【高い】思想（自由・共和・社会）を押すが、
///   国民意識は参加軸の【低い】ナショナリズムを押す（★<c>r[4] += NationalismFromConsciousness × NC</c>・実装済み）。
///   さらに窮乏が強権系を押す（★<c>r[5] += depSoc + famSoc</c>）。**2つの意識が逆向きに働く**ので
///   「時代が下るほど議会が単調に強くなる」は起きない（⚠1933年のドイツは教育水準が世界最高だった）。
///
/// ⚠**新しい状態（World のフィールド）を1本も作らない＝純関数＝SaveGame も不変。**
/// ⚠**どの挙動にも配線されていない**＝この型を呼ばない限り1ビットも変わらない（観測層）。
/// ⚠決定論＝RNG なし。走査順は <see cref="World.AllPops"/>（Regions→Pops・Seceded スキップ）に固定。
/// ⚠NaN ガードは**否定形**（`!(x &gt;= a &amp;&amp; x &lt;= b)`）で書く＝ Math.Clamp は NaN を除去しない。</summary>
public static class PowerAffinity
{
    // =====================================================================================
    //  機関
    // =====================================================================================

    /// <summary>正当性の受け皿4つ。⚠<see cref="Revolution"/> だけが座標を持たない。</summary>
    public enum Holder { Crown = 0, Parliament = 1, Army = 2, Revolution = 3 }
    public const int HolderCount = 4;
    public static readonly string[] HolderNames = ["王権", "議会", "軍部", "革命勢力"];

    /// <summary>★機関の座標＝既存の政体座標の流用（新しい定数ゼロ）。
    /// ⚠王権は「参加を求めない執行権」なので <c>Absolutist</c>。立憲君主制の王も**機関としては同じ執行権**で、
    ///   違うのは座標ではなく【取り分】である（座標を政体で変えると `if (政体)` が生える）。</summary>
    public static double[] CoordOf(Holder h) => h switch
    {
        Holder.Crown => Ideologies.BasePolityCoord(Polity.Absolutist),
        Holder.Parliament => Ideologies.BasePolityCoord(Polity.Republic),
        Holder.Army => Ideologies.BasePolityCoord(Polity.MilitaryDictatorship),
        _ => Ideologies.BasePolityCoord(Polity.Absolutist),   // ⚠革命は座標を持たない（この分岐は呼ばれない）
    };

    /// <summary>⚠革命の「親和」＝**最も近い機関からの遠さ**を、機関の親和と同じレンジ [AffMin, AffMax] へ折り返したもの。
    /// <code>
    /// best = AffMax（どれかに非常に近い） → AffMin（革命は最低）
    /// best = AffMin（どれからも遠い）     → AffMax（革命が最大）
    /// </code>
    /// ★折り返しの中心 <c>(AffMax + AffMin)</c> は既存定数から導かれる＝新しい定数を置かない。</summary>
    public static double RevolutionAffinity(double best) => (Ideologies.AffMax + Ideologies.AffMin) - best;

    /// <summary>★POPの重み＝<c>Size × 発言権</c>。⚠意識は掛けない（型のdocを参照）。
    /// 発言権は <see cref="Ideologies.National"/> と同じ式（floor＋参政権）＝**声の測り方を2つ作らない**。</summary>
    public static double PopWeight(Pop p, World w)
    {
        if (!(p.Size > 0)) return 0;
        double voice = w.Coef.IdeologyVoiceFloor
                     + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        if (!(voice >= 0)) voice = 0;
        double v = p.Size * voice;
        return !(v >= 0) ? 0 : v;
    }

    // =====================================================================================
    //  本体
    // =====================================================================================

    /// <summary>★4つの取り分（合計1）。⚠母数がゼロなら王権へ倒す（＝統治者不在の世界を作らない）。</summary>
    public static double[] Shares(World w) => Detail(w).Shares;

    /// <summary>★内訳つきの算出。診断が読む。</summary>
    public static Result Detail(World w)
    {
        double slope = w.Coef.IdeologyAffinitySlope;
        var s = new double[HolderCount];
        double[] cc = CoordOf(Holder.Crown), pc = CoordOf(Holder.Parliament), ac = CoordOf(Holder.Army);

        // ★★★①文民制度による軍の抑制（2026-08-19 第2走で追加）。旧 doc §2-4(3) の史学検証：
        //   英の反乱法（1689年勅裁）は**1年限りの時限法**で、更新されなければ軍の内部規律法そのものが失効する
        //   ＝**英国でクーデタを起こせば、起こした側の部下に対する権威が同時に溶ける**。
        // ⚠これが無いと硬性条件（英米蘭白丁諾瑞西の軍部シェア≈0）が全部赤になる（★第2走で実測＝英23.5%）。
        // ★軍部の重みを抑えるだけで、削った分は革命へ回る＝**合計1は保たれる**（⚠史実：英で不満が高まっても
        //   軍は動かず、チャーティストや労働党へ流れた）。civ=0 で従来と厳密一致。
        double civ = PowerShares.CivilianInstitutions(w);
        if (!(civ >= 0 && civ <= 1)) civ = 0;
        double armyDamp = 1.0 - civ;

        // ★★★③軍の**組織性**（2026-08-19 第6走）＝体制の外へ出た層のうち、**軍部を担えるのは軍という組織が現に在る分だけ**。
        // ⚠旧＝軍部と革命を思想の親和だけで按分していたため、**革命が受け皿を独占**し、軍事独裁が全16断面・全65カ国で0件だった。
        //   （★軍部の連合前シェアは常在項を足しても数%＝王権か議会が必ず上にいる＝単独支配の候補にすらならない。）
        // ★史実＝軍事独裁は「軍が最大だったから」ではなく、**王権と議会が同時に崩れ、軍だけが組織を保っていたから**起きた
        //   （西1923プリモ・デ・リベラ／葡1926／波1926＝いずれも文民の議会政治が麻痺した直後）。
        // ★★非対称の正体＝**革命は誰でも参加できるが、軍は入隊した者しか担えない**。⇒ 常備軍の対人口比を軍部側に掛ける。
        //   ⚠正規化は既存の `MaxSoldierFraction`（平時に兵にできる上限）を流用＝**新しい定数ゼロ**。
        //   ★平時は軍が小さく、王権と議会が痩せたときだけ相対的に浮上する＝**順序が史実になる**。
        double soldierOrg = w.SoldierSize() / System.Math.Max(1.0, w.TotalPopulation) / System.Math.Max(1e-9, w.Coef.MaxSoldierFraction);
        if (!(soldierOrg >= 0)) soldierOrg = 0; else if (soldierOrg > 1) soldierOrg = 1;

        // ★★②形だけの議会は「制度に届いた」ことにしない（同上）。
        // ⚠露1917＝ドゥーマは存在したが機能していなかった。参政権だけを見ると reach が高く出て革命が来ない。
        // ★`ParliamentGrip`（歳入の掌握）ではなく `ParliamentControl`（制度としての統制）を使う
        //   ＝旧 doc §7 の欠陥A（Grip が全世界で痩せている＝英1783で0.08）を踏まない。
        double pctl = PowerShares.ParliamentControl(w);
        if (!(pctl >= 0 && pctl <= 1)) pctl = 0;

        double totalW = 0, loyalW = 0, restiveW = 0;
        double sumM = 0, sumInstitutional = 0;   // ★制度への近さの加重平均（＝どれだけ体制に居場所があるか）

        foreach (var p in w.AllPops())
        {
            double wt = PopWeight(p, w);
            if (!(wt > 0)) continue;
            double m = p.Militancy;
            if (!(m >= 0 && m <= 1)) m = 0;

            double aC = Ideologies.SupportAt(p.IdeologyShares, cc, slope);
            double aP = Ideologies.SupportAt(p.IdeologyShares, pc, slope);
            double aA = Ideologies.SupportAt(p.IdeologyShares, ac, slope);
            if (!(aC >= Ideologies.AffMin && aC <= Ideologies.AffMax)) aC = Ideologies.AffMin;
            if (!(aP >= Ideologies.AffMin && aP <= Ideologies.AffMax)) aP = Ideologies.AffMin;
            if (!(aA >= Ideologies.AffMin && aA <= Ideologies.AffMax)) aA = Ideologies.AffMin;

            double best = System.Math.Max(aC, System.Math.Max(aP, aA));
            double aR = RevolutionAffinity(best);

            // ★★★制度に届いているか（noisy-or）＝**参政権** または **王権への信任**。
            // ⚠これが無いと闘争度がそのまま「体制の外」になり、平時の不満（RiotMilitancyThreshold=0.5 が暴動の閾値
            //   というスケール）で全世界の正当性が消える（★2026-08-19 の第1走で実測＝英1815 の正当性 4%）。
            // ★旧 PowerShares.RevolutionLeak の `×(1 − Openness(Suffrage))` の POP 単位への一般化。
            // ⚠非対称は意図的＝**議会に届くには参政権が要るが、王権には信仰でよい**
            //   （★ロシアの農民は参政権が無くても皇帝を信じていた＝革命勢力ではない／
            //     ⚠チャーティストは思想的に議会寄りでも参政権が無いから街頭に出た）。
            // ★②形だけの議会は「届いた」ことにしない。⚠ただし【掛け算は強すぎた】（2026-08-19 実測）：
            //   議会の無い政体では ParliamentControl≈0 → 議会シェア0 → 議会が立たない、という**自己強化ループ**になり、
            //   日本が全13断面で議会0%（★1890年の帝国議会開設後も）になった。★noisy-or へ＝**どちらか一方があれば届く**。
            double vote = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (!(vote >= 0 && vote <= 1)) vote = 0;
            double franchise = 1.0 - (1.0 - vote) * (1.0 - pctl);
            if (!(franchise >= 0 && franchise <= 1)) franchise = 0;
            double crownTrust = (aC - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin);
            if (!(crownTrust >= 0 && crownTrust <= 1)) crownTrust = 0;
            double reach = 1.0 - (1.0 - franchise) * (1.0 - crownTrust);
            double outside = m * (1.0 - reach);          // ⚠体制の外へ出た分
            if (!(outside >= 0 && outside <= 1)) outside = 0;

            // ★★★体制の中 → 王権/議会は【投票の民意 vs 非投票の民意】で割る（作者設計 2026-08-19）。
            // ⚠思想座標では割らない。理由＝実測で王権シェアが全13断面・全64カ国で14〜18%の頭打ちだった
            //   （★王権座標 Absolutist に近い思想が10種のうち2つしか無く、構造的に議会が勝つ）。
            // ★この切り分けの唯一の危険＝「参政権が無い＝王党派」に化けること（⚠チャーティストが王党派になる）は、
            //   **闘争的な層を先に体制の外へ出している**ので起きない（★不満のある非投票層は革命/軍部へ抜ける）。
            // ⚠副作用（承知のうえ）：参政権を持つ王党派は【議会へ】行く＝王権シェアには効かず、
            //   代わりに**議会の性格**（保守的か自由的か）を決める量になる。★英保守党も独国家人民党も議会にいた＝史実的に正しい。
            {
                double loyal = wt * (1.0 - outside);
                // ★★★軍部の常在項（2026-08-19 第5走）。⚠旧＝軍部は**闘争プールからしか取れず**、そこを革命と分け合うので
                //   実測で全16断面・全65カ国の連合前シェアが1〜4%＝**構造的に最大になれず軍事独裁が0件**だった。
                // ★史学検証（イベリア＋ラテンアメリカ92件・欧露15事例）の確定＝**軍部シェア＝常在項＋漏れ**。
                //   「軍は執行権の対抗者ではなく**道具**であり、道具が主人を食う」＝軍は体制の外にいるのではなく**内側にいる**
                //   （露の近衛が皇帝を殺す1801／独の軍事内閣1916-18／1930年代日本＝執行権が最強のときこそ軍の取り分が最大化した）。
                // ★常備軍そのものが軍部の基盤＝兵士POPの非闘争分は王権/議会でなく軍部へ。⚠新しい係数も座標もゼロ。
                //   既存の armyDamp（1−文民制度）で抑える＝**英米蘭白丁諾瑞西の硬性条件はここが守る**（英の反乱法＝規律が文民立法に依存）。
                double sold = p.IsSoldier ? armyDamp : 0.0;
                s[(int)Holder.Army] += loyal * sold;
                double civilian = loyal * (1.0 - sold);
                s[(int)Holder.Crown] += civilian * (1.0 - franchise);    // ⚠非投票の民意
                s[(int)Holder.Parliament] += civilian * franchise;       // ★投票の民意
                loyalW += loyal;
            }

            // ⚠体制の外 → 軍部/革命（誰が体制を壊すか）
            double eA = System.Math.Exp(aA) * armyDamp * soldierOrg, eR = System.Math.Exp(aR);   // ★①文民制度が軍を抑える／③軍は組織のある分しか担えない
            double d2 = eA + eR;
            if (d2 > 1e-12)
            {
                double restive = wt * outside;
                s[(int)Holder.Army] += restive * eA / d2;
                s[(int)Holder.Revolution] += restive * eR / d2;
                restiveW += restive;
            }

            totalW += wt;
            sumM += wt * outside;
            sumInstitutional += wt * reach;   // ★制度に届いている度合いの加重平均（診断）
        }

        double tot = s[0] + s[1] + s[2] + s[3];
        if (!(tot > 1e-12))
        {
            var z = new double[HolderCount];
            z[(int)Holder.Crown] = 1.0;   // ⚠母数ゼロ＝統治者不在を作らない（倒し先を明示）
            return new Result(z, 0, 0, 0, false);
        }
        double done = 0;
        for (int h = 0; h < HolderCount - 1; h++) { s[h] /= tot; done += s[h]; }
        s[HolderCount - 1] = 1.0 - done;
        if (!(s[HolderCount - 1] >= 0)) s[HolderCount - 1] = 0;

        double meanM = totalW > 1e-12 ? sumM / totalW : 0;
        double meanInst = totalW > 1e-12 ? sumInstitutional / totalW : 0;
        return new Result(s, meanM, meanInst, totalW, true);
    }

    /// <summary>★★★権力の取り分から**政体ラベルを読み出す**（作者裁定 2026-08-19「もはや政体は意味を持たない。
    /// 絶対王政と立憲君主と共和制は連続的。政体ごとの上限は全廃し、逆に権力円グラフから政体を算出して**外交パネルの表示のみ**に使う」）。
    ///
    /// ⚠**これは表示のための読み出しであって、挙動を決める量ではない。**★誰かがこれで分岐を書き始めたら設計が元に戻る。
    ///
    /// <code>
    /// 支配者（連合後に単独で PartnerViableShare 以上＝組む相手がいなかった勢力）
    ///   軍部       → 軍事独裁
    ///   革命勢力   → プロレタリア独裁（★担い手が大衆＝革命の座標がそこにしか無い）
    ///   王権       → 王冠あり＝絶対君主制 ／ 王冠なし＝ブルジョア独裁（★君主なき執行権の独裁）
    ///   議会       → 王冠あり＝立憲君主制 ／ 王冠なし＝共和制
    ///   居ない（連合政治）→ 王権と議会の**比**で連続的に読む（★これが作者の言う「連続」の実体）
    /// </code>
    ///
    /// ★**新しい閾値を1つも置かない**＝`PartnerViableShare`（既存）とその半分（無政府）だけで切る。
    /// ⚠幕府・専制は**導出では出ない**（★どちらも「王権の単独支配」であり、絶対君主制と権力配分では区別できない
    ///   ＝作者の「連続的」の帰結として正しい。⚠初期条件のラベルとしては world.json に残る）。
    /// ⚠無政府は**支配者が居らず王権も議会も痩せている**とき＝両方が PartnerViableShare の半分に届かない場合。</summary>
    public static Polity PolityFromShares(World w)
    {
        var co = Coalition(w);
        var eff = co.Effective;
        bool crowned = w.Crown != CrownForm.None;
        if (co.SoleRuler is { } r)
            return r switch
            {
                Holder.Army => Polity.MilitaryDictatorship,
                Holder.Revolution => Polity.ProletarianDictatorship,
                Holder.Crown => crowned ? Polity.Absolutist : Polity.BourgeoisDictatorship,
                _ => crowned ? Polity.ConstitutionalMonarchy : Polity.Republic,
            };
        // ★連合政治＝誰も単独では届かない。⇒ 王権と議会の比で読む（連続の実体）。
        double cw = eff[(int)Holder.Crown], pw = eff[(int)Holder.Parliament];
        if (!(cw + pw > PartnerViableShare * 0.5)) return Polity.Anarchy;   // ⚠正統な担い手がどちらも痩せた＝秩序崩壊
        return pw > cw ? (crowned ? Polity.ConstitutionalMonarchy : Polity.Republic)
                       : (crowned ? Polity.Absolutist : Polity.BourgeoisDictatorship);
    }

    // =====================================================================================
    //  ★★★予算権と統帥権（作者設計 2026-08-20）
    // -------------------------------------------------------------------------------------
    //  王権正統性 Lc ＋ 議会正統性 Lp ＝ 1。**予算権も統帥権もこの同じ Lc/Lp から直接計算し、独立した権力ストックを持たない。**
    //  ⚠独立ストックを持つと正統性と乖離してドリフトする＝その事故が構造的に起きない。
    //  ★追加したのは**法2本だけ**（<see cref="BudgetLaw"/> / <see cref="CommandLaw"/>）で、新しい状態変数はゼロ。
    //  ★軍と革命には**正統性のシェアを与えない**——軍にあるのは正統性ではなく**力**である
    //  （Finer「抑えているのは力の不足ではなく、**誰も従わない後継の定式しか持てないこと**」＝軍は正統性の請求者ではない）。
    //  ⚠革命勢力も同じ＝正統性への**挑戦者**であって請求者ではない（既存の闘争度と蜂起の機構が担う）。
    // =====================================================================================

    /// <summary>★★王権正統性 Lc と議会正統性 Lp（**合計1**）。軍と革命を除いた、正統な統治の担い手どうしの取り分。
    /// ⚠双方ゼロ（正統な担い手が居ない）のときは王権へ寄せる＝**倒し先を明示する**（統治者不在を作らない）。</summary>
    public static (double Crown, double Parliament) Legitimacy2(World w)
    {
        var s = Shares(w);
        double c = s[(int)Holder.Crown], p = s[(int)Holder.Parliament], sum = c + p;
        if (!(sum > 1e-12)) return (1.0, 0.0);
        return (c / sum, p / sum);
    }

    /// <summary>★予算法の重み（王権, 議会）。⚠固定値＝法が離散なので重みも離散でよい（新しい係数を作らない）。</summary>
    public static (double Crown, double Parliament) BudgetWeights(BudgetLaw law) => law switch
    {
        BudgetLaw.Decree => (1.0, 0.0),          // 勅令＝露(1906年以前)・清
        BudgetLaw.Partial => (0.5, 0.5),         // 部分承認＝普(第99/109条)・日(第67/71条)
        _ => (0.0, 1.0),                          // 議会承認＝英・仏第三共和政
    };

    /// <summary>★統帥法の重み（王権, 議会）。★中段の**分割**＝軍政は内閣・**軍令は君主に直属**（明治憲法第11条・普の軍事内閣）。</summary>
    public static (double Crown, double Parliament) CommandWeights(CommandLaw law) => law switch
    {
        CommandLaw.Crown => (1.0, 0.0),
        CommandLaw.Split => (0.5, 0.5),
        _ => (0.0, 1.0),
    };

    /// <summary>★★★軍が統帥権を独占するか。**分岐はこれ1つだけ**（作者設計）。
    /// <code>|ArmyAllegiance| &lt; 1 − 文民制度</code>
    /// ★<b>帰属が中立に近い＝どちらも軍を満たせていない</b>＝軍が自分の主になる（+1=王権 / −1=議会 / 0=中立・既存量）。
    /// ⚠⚠**閾値を定数0.5にしてはいけない**（2026-08-20の是正）——職業軍人が政治的に中立な国はまさに帰属が0付近で、
    ///   定数だと**イギリスとアメリカが軍事独裁になる**（★硬性条件が全部赤）。Huntington の客観的統制＝
    ///   **専門職化した軍は中立であり、かつクーデタを起こさない**。
    /// ★**中立と無拘束は別物**：西1923の軍は中立**かつ無拘束**だったが、英の軍は中立でも**反乱法で縛られていた**
    ///   （1689年・**1年限りの時限法**＝更新されなければ軍の内部規律法そのものが失効する＝
    ///     **クーデタを起こせば起こした側の部下への権威が同時に溶ける**）。
    /// ⇒ 閾値を**文民制度の厚み**にする＝分岐は1つのまま・新しい定数もゼロ（`CivilianInstitutions` は既存で硬性条件を守っている量）。
    ///   ★英は文民制度が厚い→閾値がほぼ0→**帰属が完全に中立でも発火しない**／西は薄い→閾値が1に近い→少し中立に傾けば発火する。</summary>
    public static bool ArmyHoldsCommand(World w)
    {
        // ★測ってから決めた形（2026-08-20・実分布 poweraff の★計器）。⚠推測で置いた2案は実測で棄却された：
        //   ⚠案1「|ArmyAllegiance| < 0.5」＝**英米が軍事独裁になる**（職業軍人が中立な国はまさに帰属0付近。Huntington の客観的統制）。
        //   ⚠案2「|ArmyAllegiance| < 1 − 文民制度」＝**ほぼ全世界で発火**（★109件）。私が1783年の断面だけを見て
        //     「帰属は全部0付近」と判断したのが誤りで、実分布は**中央0.18〜0.78**と時代で大きく動いていた（1783年だけが初期化前の0）。
        //   ⚠案3「文民の統帥（Lc×重み+Lp×重み）が薄いとき」も誤り＝勅令/王権の段では重みが(1,0)なので**値は Lc そのもの**になり、
        //     **専制国家ほど1に近づく**（1783年の中央0.91）＝意味が反転していた。
        // ★★★正しい形＝**正統な担い手そのものが薄いとき**（作者設計「どちらも軍を満たせていない」）。
        //   ⚠`Legitimacy2` は合計1に正規化しているので「**両方とも弱い**」が原理的に出せない＝**正規化前の生シェア**を見る。
        //   ★王権＋議会の生シェア（4者の中での取り分）が文民制度を下回ったとき＝**文民の側が、軍を抑えるだけの制度も正統性も持たない**。
        //   ★比べる2つが同じ[0,1]で意味も揃う（旧案はレンジの不一致だった）。
        //   ★史実：西1923は議会政治が麻痺して正統性が割れ、かつ文民制度が薄かった／英は正統性も文民制度も厚い＝発火しない。
        // ⚠**向きの是正（第2走）**：閾値を文民制度そのものにすると**文民制度が厚い国ほど発火しやすい**という反転になり、
        //   実測で**イギリス1934年が発火**した（★国民政府下の安定した議会政治＝明確な誤り）。
        //   ⇒ 文民制度は**閾値ではなく抑制側**に掛ける＝`1 − 文民制度`。★英は文民制度0.72→閾値0.28で発火せず、
        //     文民制度の薄い国（0.25）は閾値0.75となり、正統性が割れていれば発火する＝**向きが揃う**。
        double civ = PowerShares.CivilianInstitutions(w);
        if (!(civ >= 0 && civ <= 1)) civ = 0;
        var s = Shares(w);
        double civilian = s[(int)Holder.Crown] + s[(int)Holder.Parliament];
        if (!(civilian >= 0)) return false;   // NaN は否定形で落とす（発火させない）
        // ★★★時間条件（2026-08-21・第3走）：**軍事独裁は危機の帰結であって常態ではない**。
        //   第2走の実測＝向きを直しても27件で、**ロシアが1800〜1934年まで通しで軍事独裁**になった（史実の露は専制であって軍政ではない）。
        //   史実の軍事独裁（西1923・葡1926・波1926・希1922）は**議会政治が機能していた国が麻痺した後**にだけ起きた
        //   ＝もともと議会が無い専制は軍事独裁に**なる余地が無い**。
        // ★既存ストック `RegimeTrackRecord[1]`（議会制の信認・[-1,1]・現政体の族にのみ蓄積・減衰つき）がこの時間条件そのもの：
        //   ⚠**符号は相談相手の提案（>0）から反転させた**。3群を分けるのは符号である：
        //     ・>0 ＝ 議会制が信認されている（英）→ 軍は出ない（そもそも正統性が厚く第1条件で落ちる）
        //     ・=0 ＝ **議会制の歴史そのものが無い**（露＝Despotic は族2にしか書かないので[1]は厳密に0のまま）→ 出ない（余地が無い）
        //     ・<0 ＝ **議会制が試みられ、失望された**（西のトゥルノ体制の腐敗・波1919-26の混乱・希1922の破局）→ ここだけが軍政の入口
        //   提案の >0 だと「信認されている英」を通し「失望された西」を弾く＝**狙いと逆**になる。
        //   Finer の機会項（政治化された層における正統性の減価）＝まさに「失望」の側。
        if (!(w.RegimeTrackRecord[1] < 0)) return false;   // 議会制が試みられ失望された国だけ（NaNも否定形で落ちる）
        return civilian < 1.0 - civ;
    }

    /// <summary>★★予算権のシェア（王権, 議会, 軍部）。合計1。
    /// ⚠**クーデタ後は予算権も軍部へ移る**（作者設計）＝軍が統帥権を独占しているなら予算権も独占する
    /// （★史実：プリモ・デ・リベラの軍事総督府は**勅令で統治した**）。</summary>
    public static (double Crown, double Parliament, double Army) BudgetShares(World w)
    {
        if (ArmyHoldsCommand(w)) return (0, 0, 1.0);
        var (lc, lp) = Legitimacy2(w);
        var (wc, wp) = BudgetWeights(w.BudgetLaw);
        double a = lc * wc, b = lp * wp, sum = a + b;
        if (!(sum > 1e-12)) return (1.0, 0, 0);   // 双方ゼロ＝倒し先を明示（統治者不在を作らない）
        return (a / sum, b / sum, 0);
    }

    /// <summary>★★統帥権のシェア（王権, 議会, 軍部）。合計1。★軍が握るときは軍部が独占。</summary>
    public static (double Crown, double Parliament, double Army) CommandShares(World w)
    {
        if (ArmyHoldsCommand(w)) return (0, 0, 1.0);
        var (lc, lp) = Legitimacy2(w);
        var (wc, wp) = CommandWeights(w.CommandLaw);
        double a = lc * wc, b = lp * wp, sum = a + b;
        if (!(sum > 1e-12)) return (1.0, 0, 0);
        return (a / sum, b / sum, 0);
    }

    /// <summary>★正当性＝王権＋議会（作者設計の定義そのもの）。</summary>
    public static double Legitimacy(World w) { var s = Shares(w); return s[(int)Holder.Crown] + s[(int)Holder.Parliament]; }

    // =====================================================================================
    //  連合（★作者設計 2026-08-19）
    // =====================================================================================

    /// <summary>★「組む相手として成立する取り分」の下限。⚠旧 `PowerShares.PartnerViableShare` と同値＝**物差しを2つ作らない**。</summary>
    public const double PartnerViableShare = PowerShares.PartnerViableShare;

    /// <summary>★★★連合＝**乗っ取れるシェアを得たが単独では勝てない勢力は、次に有力な勢力と手を組む**（作者設計 2026-08-19）。
    ///
    /// <code>
    /// 各勢力 h は「自分より大きく、かつ PartnerViableShare に届く」相手の中から
    ///   引力 ＝ 相手のシェア × 親和(h の座標, 相手の座標)      ★座標だけで決まる
    /// が最大の相手へ**シェアごと合流**する（作者裁定＝案2）。
    /// ⚠自分より大きい相手にしか付かない＝**循環しない**（最大の勢力は誰にも付かない）。
    /// ⚠1段だけ＝連鎖させない（思想⑧：深さ2を作らない）。
    /// </code>
    ///
    /// ★これで作者の要求がそのまま出る：
    ///   **王権が高い限りは王に使える軍／王権に陰りが見えると議会の軍／どちらも弱くなると単独か革命勢力と組む。**
    /// ⚠革命は座標を持たないので親和が低い＝**王権も議会も痩せて初めて革命の引力が勝つ**。
    ///
    /// ⚠⚠**旧 `PowerShares.SeeksMilitaryRule`（表モデルと一緒に 2026-09-26 に削除＝pol-09）の絶対閾値への回帰ではない**——
    ///   旧 doc は「軍が単独50%以上」という**絶対値**を捨てて相対条件へ移したが、
    ///   ★本式は**連合を計算した【後】**に見るので「単独で50%残った＝**組む相手がいなかった**」を意味し、
    ///   旧 doc の相対条件と**同じ結果**になる（⚠しかも規則が1つ減る）。
    /// ⚠史実：1918年のエーベルト＝グレーナー協定（軍が皇帝を見限り共和国と組んだ）が機構として出る。</summary>
    public static CoalitionResult Coalition(World w)
    {
        var raw = Shares(w);
        MechanismProbe.Eval(MechanismProbe.Site.CoalitionComputed);   // ★D-34：連合の計算そのものが走ったか（下の革命の行と取り違えないための対照）
        if (raw[(int)Holder.Revolution] > 0) MechanismProbe.Eval(MechanismProbe.Site.CoalitionRevolutionAlly);   // ★試行＝革命がシェアを持っていた回数
        bool probeAnyAlly = false, probeRevAlly = false;   // ★D-34：成立は**呼び出し1回**単位で数える（試行と同じ粒度＝下の Hit を参照）
        double slope = w.Coef.IdeologyAffinitySlope;
        var eff = (double[])raw.Clone();
        var ally = new Holder[HolderCount];
        for (int h = 0; h < HolderCount; h++) ally[h] = (Holder)h;   // 既定＝単独（自分自身）

        for (int h = 0; h < HolderCount; h++)
        {
            double mine = raw[h];
            if (!(mine > 0)) continue;
            int best = -1; double bestPull = 0;
            for (int k = 0; k < HolderCount; k++)
            {
                if (k == h) continue;
                double theirs = raw[k];
                if (!(theirs > mine)) continue;                  // ⚠自分より大きい相手にしか付かない（循環の禁止）
                // ⚠★ここに「相手が PartnerViableShare に届くこと」を課してはならない（2026-08-19 第4走で実測の誤り）。
                //   作者設計の 50% は**単独支配の条件**であって**連合の条件ではない**。課すと王権が50%を超えないため
                //   「王に使える軍」が原理的に出ず、硬性条件も1つも改善しなかった（★英13.6%が第3走と1桁も動かない）。
                double pull = theirs * Closeness((Holder)h, (Holder)k, slope, w.Coef.CoalitionClosenessFloor);
                if (pull > bestPull) { bestPull = pull; best = k; }
            }
            // ★★★部分合流（2026-08-19 第5走）：**移すのは親和の分だけ**。⚠旧＝全額合流（mine を丸ごと渡す）だったが、
            //   実測で**連合政治0件・軍事独裁0件が構造的に到達不能**になっていた（★全16断面・全65カ国）——
            //   全額合流だと勝者がほぼ必ず PartnerViableShare を超えるので SoleRuler が常に立ち、「組む相手がいなかった」状態が作れない。
            //   ⚠英の軍部は連合前に1〜4%あるのに**連合後は全断面で0%**＝毎回まるごと吸収されていた。
            // ★親和を掛けるだけ＝新しい係数も分岐もゼロ（Closeness は既に[0,1]へ正規化済み）。
            //   近い相手には深く溶け込み（英の王権が議会へ）、必要に迫られて組んだだけの相手には**自分の基盤を残す**（軍は軍のまま）。
            // ★★これで「組んだ相手を後で潰す」が**規則を1つも足さずに**出る＝取り分を保った軍部は、
            //   支えていた側が痩せた次の四半期には自分が最大になり誰にも付かなくなる（⚠深さ2を作らない＝思想⑧を守る）。
            if (best >= 0)
            {
                // ★D-34（2026-09-03）：⚠試行は**この関数の呼び出し1回**で数えているので、成立も**呼び出し1回**で数える
                //   （片方を勢力ごと・片方を呼び出しごとに数えると、それ自体が「分子と分母の非対称」＝当プロジェクト最多のバグ型になる）。
                probeAnyAlly = true;
                if ((Holder)h == Holder.Revolution || (Holder)best == Holder.Revolution) probeRevAlly = true;
                double bond = Closeness((Holder)h, (Holder)best, slope, w.Coef.CoalitionClosenessFloor);
                if (!(bond >= 0 && bond <= 1)) bond = 0;
                double moved = mine * bond;
                ally[h] = (Holder)best; eff[best] += moved; eff[h] -= moved;
            }
        }

        if (probeAnyAlly) MechanismProbe.Hit(MechanismProbe.Site.CoalitionComputed);       // ★D-34：この呼び出しで連合が1つでも成立したか
        if (probeRevAlly) MechanismProbe.Hit(MechanismProbe.Site.CoalitionRevolutionAlly); // ★D-34：この呼び出しで革命が絡む連合が成立したか
        // ★連合後に単独で PartnerViableShare 以上を占める勢力＝**組む相手がいなかった勢力**
        int rulerIdx = -1;
        for (int h = 0; h < HolderCount; h++)
            if (eff[h] >= PartnerViableShare && ally[h] == (Holder)h && (rulerIdx < 0 || eff[h] > eff[rulerIdx])) rulerIdx = h;

        return new CoalitionResult(raw, eff, ally, rulerIdx >= 0 ? (Holder?)rulerIdx : null);
    }

    /// <summary>★機関どうしの近さ∈[0,1]（座標間の距離→親和を [0,1] へ正規化）。革命は座標を持たないので床を返す。
    /// ⚠**AUD-09（監査2026-09-02 medium・2026-09-03投入）**：設計（上の L398）は「革命は親和が**低い**＝王権も議会も
    /// 痩せて初めて革命の引力が勝つ」だが、実装は 0 を返し、選定が `pull &gt; bestPull`（初期値0）なので
    /// **革命は誰とも組めず誰にも組まれない**＝この設計は構造的に到達不能だった（「低い」と「ゼロ」は違う）。
    /// ⚠根は正規化 (aff−AffMin)/(AffMax−AffMin) が最遠の対を 0 へ潰すこと＝「遠いが非ゼロ」が表現できない。
    /// ★床を置くのは当プロジェクトの既存の書き方（<see cref="Coefficients.CapitalCreditFloor"/> の「ゲートでなくプレミアム」）と同形＝新しい写像を作らない。
    /// ⚠旧注記「AffMin（＝0）」は誤り＝<see cref="Ideologies.AffMin"/> は −1.0（0なのは正規化後の下限）。
    /// ⚠floor=0（既定）で厳密恒等＝休眠。</summary>
    public static double Closeness(Holder a, Holder b, double slope, double floor = 0)
    {
        if (a == Holder.Revolution || b == Holder.Revolution) return floor;   // ⚠革命は誰とも「近く」ない（引力はシェアと床だけで決まる）
        double aff = Ideologies.AffinityOf(CoordOf(a), CoordOf(b), slope);
        double v = (aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin);
        if (!(v >= 0 && v <= 1)) return floor;   // 否定形＝NaN も床へ（floor=0 で旧と同値）
        return floor > 0 ? floor + (1.0 - floor) * v : v;
    }

    /// <param name="Raw">連合前のシェア（合計1）</param>
    /// <param name="Effective">連合後の実効シェア（合計1）。⚠付いた側は0に近づき、付かれた側が太る</param>
    /// <param name="Ally">各勢力が付いた先（★自分自身＝単独）</param>
    /// <param name="SoleRuler">連合後に単独で PartnerViableShare 以上を占めた勢力（⚠居なければ null＝連合政治）</param>
    public readonly record struct CoalitionResult(double[] Raw, double[] Effective, Holder[] Ally, Holder? SoleRuler);

    /// <param name="Shares">王権/議会/軍部/革命（合計1）</param>
    /// <param name="MeanMilitancy">加重平均の【体制の外へ出た度合い】＝闘争度×(1−制度に届く)。⚠生の闘争度ではない</param>
    /// <param name="MeanInstitutional">加重平均の「制度に届いている度合い」∈[0,1]＝参政権 or 王権への信任（noisy-or）</param>
    /// <param name="TotalWeight">母数（Size×発言権 の総和）</param>
    /// <param name="Any">母数が非ゼロだったか</param>
    public readonly record struct Result(double[] Shares, double MeanMilitancy, double MeanInstitutional, double TotalWeight, bool Any);

    // =====================================================================================
    //  診断のための分解（★挙動には一切使わない）
    // =====================================================================================

    /// <summary>★思想ごとの行き先＝「この思想は、非闘争なら王権と議会のどちらへ／闘争なら軍部と革命のどちらへ行くか」。
    /// 座標表だけで決まるので**世界に依存しない**（slope のみ）＝写像そのものの検算に使う。</summary>
    public static double[] IdeologyDestination(int ideologyIndex, double slope)
    {
        var one = new double[Ideologies.Count];
        if (ideologyIndex >= 0 && ideologyIndex < Ideologies.Count) one[ideologyIndex] = 1.0;
        double aC = Ideologies.SupportAt(one, CoordOf(Holder.Crown), slope);
        double aP = Ideologies.SupportAt(one, CoordOf(Holder.Parliament), slope);
        double aA = Ideologies.SupportAt(one, CoordOf(Holder.Army), slope);
        double aR = RevolutionAffinity(System.Math.Max(aC, System.Math.Max(aP, aA)));
        double eC = System.Math.Exp(aC), eP = System.Math.Exp(aP), eA = System.Math.Exp(aA), eR = System.Math.Exp(aR);
        double d1 = eC + eP, d2 = eA + eR;
        return [
            d1 > 1e-12 ? eC / d1 : 0.5,   // 非闘争のとき王権へ行く比
            d1 > 1e-12 ? eP / d1 : 0.5,   // 非闘争のとき議会へ行く比
            d2 > 1e-12 ? eA / d2 : 0.5,   // 闘争のとき軍部へ行く比
            d2 > 1e-12 ? eR / d2 : 0.5,   // 闘争のとき革命へ行く比
        ];
    }
}
