using System.Collections.Generic;

namespace PriceOfBread.SimCore;

/// <summary>
/// 国際金融（段4-貨幣 第3b段・民間外資＝A案）。**材の世界清算の「型」を資本に流用**：資本は収益率の高い国へ流れ（比較優位）、
/// 流入は工業化を支え（投資家の貯蓄を増やす＝既存の投資機構が回す）、毎期 配当を本国へ送り、信用崩壊で**逃避**する。
/// どこかの危機は世界の資本を萎縮させ**伝染**する（ベアリングス型 1873/90/1907）。
/// ストック：<see cref="World.ForeignCapitalIn"/>（この国にある外資＝負債・配当を払う）／<see cref="World.CapitalAbroad"/>（他国に持つ資本＝資産・配当を受ける）。
/// 保存則：流入＝金流入・配当/逃避＝金流出を必ず**対**で動かす（host が出した額＝owner が受けた額・各々 CumulativeTradeFlow に計上）＝世界で金が閉じる。
/// 多国（MultiNationSim）でのみ走る＝単一国較正セル・回帰テストに無影響。
/// </summary>
public static class CapitalMarket
{
    private static readonly RatioToGdpYear NoGdpDistressDebtRatio = new(9);       // NominalGdpPerMonth≈0 かつ債務ありのときの代替（極大）債務比率（年の名目GDP の9倍＝DebtDistressRatio を越える番兵）
    private const double BondConfMin = 0.2;             // 国債スプレッドが対外信認を下げる際の下限クランプ
    private const double ExchangeRateCreditFloor = 0.3; // 為替減価が対外信認を削る際の下限クランプ

    /// <summary>外資の余地 headroom（遅れている国ほど資本を呼ぶ吸引の素）。旧＝<c>CapitalIndTarget − 工業化度の平均</c>。★(605) 旗 <c>CapitalHeadroomFromReturn</c> が正なら
    /// <c>CapitalIndTarget × World.ModernizeReturnShare()</c>（国庫の配り先の設備の収益の持ち分＝AI の近代化投資の信号と同じ 1 本。★この 1 本は設備の収益の飽和と州の上限を通じて州の資本の在庫を間接に読む＝工業化度の直読みを外すだけで、在庫への結びは残る。CapitalIndTarget は旗 1 では 0〜1 の割合に掛ける目盛り）。旗 0 の式は抜き出す前と同じ。番人が直に呼ぶ。</summary>
    internal static double Headroom(Coefficients c, World w)
        => c.CapitalHeadroomFromReturn > 0 ? c.CapitalIndTarget * w.ModernizeReturnShare()
                                           : System.Math.Max(0, c.CapitalIndTarget - w.MeanIndustrialization);

    public static void Step(IReadOnlyList<Simulation> nations, Coefficients c)
    {
        int n = nations.Count;
        if (n < 2) return;
        var w = new World[n];
        // 保存量プローブ（leakhunt・既定OFF＝実行ゼロ）：国際金融のどの段（逃避配当/還流/新規フロー）で D が動いたかを特定する。
        double[]? probeD = null;
        void ProbeSnap()
        {
            if (!Simulation.ConservationProbe) return;
            probeD ??= new double[n];
            for (int i = 0; i < n; i++) probeD[i] = (w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued).Value;
        }
        void ProbeDiff(string label)
        {
            if (!Simulation.ConservationProbe || probeD is null) return;
            for (int i = 0; i < n; i++)
            {
                double now = (w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued).Value;
                if (System.Math.Abs(now - probeD[i]) > 1e-4)
                    System.Console.Error.WriteLine($"[PROBE金融] {label} {w[i].Region.Name} {w[i].Year}年 ΔD={now - probeD[i]:F6}");
                probeD[i] = now;
            }
        }
        var ret = new double[n];    // 投資収益率（＝資本価格）：工業化の伸びしろ×信用。高い国が外資を呼ぶ
        var cred = new double[n];   // 信用：低債務×金本位×通貨安定。低いと外資が逃げる
        var ngdp = new double[n];   // 名目GDP（下の1周目で国ごとに1回だけ求める）
        // ★段3-3（2026-09-30・A7 と同じ規則）：国境をまたぐ資本移動・配当・逃避・国債の元利は**金の値打ち**で動く＝払い手は自国の通貨の額を為替で金へ、
        //   受け手は金を自国の通貨へ（貿易の清算と同じ為替 World.FxRate）。量の比べ（外資の需要と供給・受け皿の按分・債権の比）も金建て。
        //   旧＝名目 1:1（貿易だけが換算＝経路ごとに物差しが違った・(108) の申し送り2）。差は為替の換算の台帳（Diplomacy.CumulativeFxResidual）へ＝fx に積んで最後に1回。
        //   ストックの単位＝各自の通貨（ForeignCapitalIn＝受けた国の通貨・CapitalAbroad と ForeignBondClaims＝持ち手の国の通貨）。
        var e = new double[n];
        double fx = 0;   // 受け手が受け取った額−払い手が払った額（各自の通貨）
        for (int i = 0; i < n; i++)
        {
            w[i] = nations[i].World;
            e[i] = w[i].FxRate;
            ngdp[i] = w[i].NominalGdpPerMonth.Value;   // ★A2-2（2026-09-26）：名目GDP（州×生産者の全件走査）は国ごとにこの1回だけ。この関数が書くのは貯蓄・対外資産・外資・信用の記帳だけ＝産出・州・物価水準は動かない＝下の読み手はビット単位で同じ値を読む
            // [単位・B13] 債務/GDP＝**年次名目**（GovDebt÷(月次NominalGdpPerMonth×12)）＝Fiscal の国債の金利の dGdp と同じ物差し（2026-09-29・棚卸し fis-02）。
            //   旧＝月次名目（GovDebt÷月次NominalGdpPerMonth）＝閾値 DebtDistressRatio 2.0 は年に直すと約0.17＝国債÷年GDP が史実の水準（英1815 約2.5・仏1789 0.6〜0.8）へ
            //   立った国は対外信用の債務の項が0＝外資が逃げた。NoGdpDistressDebtRatio・信用の閾値はこの単位。
            RatioToGdpYear debtR = ngdp[i] > 1e-9 ? Calendar.ToGdpYear(w[i].GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(ngdp[i]))) : (w[i].GovDebt.Value > 0 ? NoGdpDistressDebtRatio : default);
            // ★AUD-09（Fiscal.cs の dGdp と同じ規律・同じ書き方）：分母の絶対値ガードだけでは微小非ゼロで比が発散し、
            //   NaN は `Math.Clamp` を素通りして CreditDebtTerm を毒する。⚠否定形で番兵へ倒す。
            //   ★有限値では厳密同値：読み手は下2行の `Clamp(1−debtR/DebtDistressRatio, 0, 1)` だけで、
            //     DebtDistressRatio=3.0 に対し番兵9は既に 0 へクランプされる帯＝**挙動は変わらない**。
            if (!(debtR <= NoGdpDistressDebtRatio)) debtR = NoGdpDistressDebtRatio;
            // 国債二次流通との連動（改修2026-06-28・金融改革①）：国債の利回りスプレッド(暴落)が対外信用を下げる＝国債暴落→資本逃避→為替減価→（既存経路で）さらに国債金利↑のバーリング危機ループ。
            double bondSpread = System.Math.Max(0, (w[i].GovBondRatePerYear - c.BondBaseRatePerYear).Value);
            double bondConf = System.Math.Clamp(1.0 - c.BondYieldFlightWeight * bondSpread, BondConfMin, 1.0);
            double cr = PoliticalQuantities.Clamp01(1.0 - debtR / c.DebtDistressRatio)
                      * (c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility)   // 兌換可能性で対外信用を連続補間（倍率排除②・Conv=1で無ペナルティ・Conv=0で0.5）
                      * System.Math.Clamp(w[i].ExchangeRate, ExchangeRateCreditFloor, 1.0)   // 減価＝対外信用の低下
                      * bondConf;   // 国債暴落＝信認喪失で外資が逃げる
            cred[i] = cr;
            // ★純記帳（2026-08-13）：信用と4つの内訳を World へ写す。読み手はUIだけ＝力学に一切影響しない。
            w[i].CreditRating = cr;
            w[i].CreditDebtTerm = PoliticalQuantities.Clamp01(1.0 - debtR / c.DebtDistressRatio);
            w[i].CreditGoldTerm = c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility;
            w[i].CreditFxTerm = System.Math.Clamp(w[i].ExchangeRate, ExchangeRateCreditFloor, 1.0);
            w[i].CreditBondTerm = bondConf;
            double headroom = Headroom(c, w[i]);   // 改修2026-06-28：首都州のみ→全州平均（外資は国全体の工業化ギャップで流入先を選ぶ）
            // ★全球収束の是正・資本側（2026-08-02）：headroom は「遅れている国ほど資本を呼ぶ」無条件のポンプで、
            //   CapitalIndTarget=4.0 が全国共通の吸引点になっている。実測1913でダホメ4.02・ラオス3.82・エチオピア3.91 が
            //   英3.56・仏3.66・日3.50 と同じ帯に密集し、国力スコアの8割を占める水準量（工業化×9＋技術×12）が
            //   横並びになる＝史実の列強が上位7に入れない主因。
            //   技術側は同じ病に対して既に吸収能力ゲートを持つ（TechEducation の CatchupAbsorptionBlend＝コメントに
            //   「全球収束の是正」と明記）のに、資本側にだけ無かった。**同じ写像**を掛けて非対称を解消する。
            //   ＝資本は来るが、読める技師と情報流通が無ければ工業化には変換されない（史実の後発国の吸収能力）。
            //   CapitalAbsorptionBlend=0 で恒等（既定1.0＝live）。
            //   ★床つき（2026-08-02の再修正）：初版は blend=1 で headroom×absorption の**乗算ゲート**にしたため、
            //     識字ゼロ×国家統制の国は外資が恒久的にゼロになり、工業化できないまま国家の義務だけが残って
            //     慢性赤字の負債化（損失financing 38.1%＝硬性ゲート赤）を生んだ。史実でも外資は非識字国へ流れている
            //     （エジプト・オスマンの鉄道）＝流れないのではなく、実らずに債務不履行で終わる。
            //     すぐ下の信用項が「乗算ゲートでなく床つきプレミアム」を選んでいるのと**同じ作法**に揃える。
            //     床は信用と同じ CapitalCreditFloor を共有＝新しい係数を作らない。
            if (c.CapitalAbsorptionBlend > 0)
            {
                double abs = c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * TechEducation.AbsorptionOf(w[i], c);
                headroom *= 1.0 + (abs - 1.0) * c.CapitalAbsorptionBlend;
            }
            // ①修正：信用は「ゲート（乗算）」でなく「リスクプレミアム（床つき）」＝信用が低くても高潜在なら資本を呼ぶ（史実の新大陸＝不換でも英資本を吸う）。
            // 信用低の代償は引き付けの消滅でなく、高い逃避率・配当（下記）＝資本は来るが脆い（新興国のブーム＝サドンストップ）。
            // 政策金利（バンクレート）が外資を呼ぶ（carry＝高金利通貨へ資本が流れる・正貨還流）。信用(cr)でゲート＝破綻国は高金利でも呼べない。
            // uniform 分は下段の (ret−worldRate) で相殺＝**相対金利差**が効く（バンクレート引上げで他国より資本を吸う）。
            ret[i] = headroom * (c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * cr) + c.PolicyRateCapitalDraw * w[i].InterestRate * cr;
            // 説明API（金利doc B2・2026-07-30）：投資収益率＝外資を呼ぶ力の内訳。金利の第3チャネル（carry）が
            // 工業化の伸びしろに対してどれだけの比重かを測れる状態にする。Explain=null（既定）で短絡＝ビット恒等。
            if (w[i].Explain is { } exCap)
            {
                exCap.Reset("CapitalReturn");
                exCap.Add("CapitalReturn", "工業化の伸びしろ（信用でゲート）", headroom * (c.CapitalCreditFloor + (1.0 - c.CapitalCreditFloor) * cr));
                exCap.Add("CapitalReturn", "政策金利（高金利通貨への資本移動）", c.PolicyRateCapitalDraw * w[i].InterestRate * cr);
            }
        }
        double worldRate = 0, worldCred = 0; for (int i = 0; i < n; i++) { worldRate += ret[i]; worldCred += cred[i]; } worldRate /= n; worldCred /= n;
        // 伝染（倍率排除）：危機国の閾値カウント(cr<0.4)でなく**世界平均信用の低下**で連続的に。信用が下がるほど資本意欲が滑らかに縮む＝0.4クリフのないサドンストップ。
        double riskAppetite = PoliticalQuantities.Clamp01(1.0 - c.CapitalContagion * (1.0 - worldCred));

        // (1) 資本逃避：信用の低い国の外資が本国送還（host から流出）。(2) 配当：host が ForeignCapitalIn に divRate を払う。
        //     払えた総額を owner 国へ CapitalAbroad 比で戻す（逃避は元本＝資産も逃避の額だけ減る／配当は資産不変）。
        // 帰属修正（監査経9・#17で有効化）：流入は toInvestors:true で投資家貯蓄へ入るのに、配当/逃避の流出は全pop按分＝
        //   「外資で資本家が潤い、配当は国民の人頭税で払う」非対称の是正＝流出も投資家原資。
        //   ★2026-09-26：2値の旗 CapitalOutflowFromInvestors（既定1.0）を焼き込み（x-01）。
        //   ★★2026-09-27：不足分の全popフォールバックを撤去＝流出は**投資家まで**（流入は投資家へだけ入る＝分子と分母の対称）。
        //     旧＝投資家が尽きると農民・労働者の貯蓄まで国外へ出し、受け皿の基礎貨幣が干上がって物価が下限に張り付き主食の採算が割れた（世界規模の補給崩壊の根）。
        //     B4裁定（2026-07-31）の「中間値＝両者の按分」は、全pop按分の経路が無くなったので対象が無い。
        // ★★2026-09-27（C）：払えない逃避は**債権ごと消える**＝不履行で債権者が損をかぶる。host の ForeignCapitalIn は逃避の額だけ減り、
        //   下の還流で owner の CapitalAbroad も同じ額だけ減る（請求権の両側を同時に消す・貨幣は払えた額しか動かない）。
        //   旧＝負債は払えた額しか減らず、払えなかった元本が債権のまま残って毎四半期もう一度の逃避と配当を請求された（戻らない輪）。
        //   払えない配当も同じ規則＝未払いを積まない（host に延滞の負債も、owner に未収の債権も立たない）。
        double flightTotal = 0;   // 逃避で消えた請求権の総額（払えた額でなく逃避の額・金建て）
        var taken = new double[n];   // host 別に実際に抜いた額（逃避＋配当）＝owner へ戻る金。保有者が居ない場合は抜いた host へ返金＝金を捨てない（保存）
        ProbeSnap();
        for (int i = 0; i < n; i++)
        {
            // 資金逃避（07_再設計 §5(b)＝B3自己成就期待の第2形・2026-07-31）：**外資の引き上げだけでなく、自国の資本家も逃げる**。
            // 引き金＝通貨への不信（累積切り下げ／月次名目GDP＝通貨改革と兌換の再加入の門 broken と同じ物差し・⚠国債の金利の信認の喪失は 2026-09-29 から直近の切り下げの速さ＝別の量）。逃げた金は対外資産（CapitalAbroad）へ移り、
            // 国内の投資原資が痩せる＝「信認を失った国は自国の資本にも見捨てられる」。★自己成就＝逃避→通貨/信認↓→さらに逃避。
            // 保存＝国内popから引いた額を受け皿の国の投資家へ渡す（下の★2026-09-22）。CapitalDomesticFlightRatePerQuarter=0 で厳密恒等。
            if (c.CapitalDomesticFlightRatePerQuarter.Value > 0 && ngdp[i] > 1e-9)
            {
                double distrust = PoliticalQuantities.Clamp01(w[i].CumulativeDevaluation / ngdp[i] - c.CapitalFlightDistrustThreshold);
                // ★逃げた**金**の両建て（2026-09-22）：旧＝TakeGold で流出を記帳するだけで、どの国にも金が入らず世界から消えていた
                //   （国ごとの保存は閉じる＝計器に掛からない片側記帳。対外元本の償還と同じ形）。旧注記の「保存則は移転なので破れず」は誤りだった。
                //   受け皿＝自国以外の信用×名目GDPの按分（国名なし＝「資本はロンドンへ逃げる」が信用の分布から創発）＝逃げた金はその国の投資家の預金になる
                //   （GiveGold＝外資の流入と同じ入口）。受け皿が無い（他国の信用×GDPが0）なら逃げない。
                // ★★請求権の両建て（2026-09-22・作者裁定「全て推奨」で旗 DomesticFlightHasHost を削除）：受け皿は届いた額と同じだけ外資の負債
                //   （ForeignCapitalIn）を記帳する＝逃げた側の CapitalAbroad に相手が立つ（新規の資本移動 (3) の流入と同じ2行）。
                //   旧＝CapitalAbroad だけが増え、相手のいない請求権が還流（CapitalAbroad 比の按分）で他国の実物FDIの配当プールを吸っていた
                //   （2026-08-14 の外交観測：清の幻の対外資産79×GDP）。今は受け皿がその負債に配当を払い、逃避の対象にもなる。
                double credSum = 0;   // 受け皿の重み＝信用×名目GDP（金建て）
                if (distrust > 0)
                    for (int h = 0; h < n; h++) if (h != i) credSum += cred[h] * System.Math.Max(0, ngdp[h]) * e[h];
                if (credSum > 1e-9)
                {
                    double want = c.CapitalDomesticFlightRatePerQuarter.Value * distrust * InvestorSavings(w[i]);
                    double got = TakeGold(w[i], want);
                    w[i].CapitalAbroad += new MoneyStock(got);   // 逃げた金は対外資産＝還流ループ（配当・逃避の按分）に乗る
                    fx -= got;
                    for (int h = 0; h < n; h++)
                    {
                        if (h == i) continue;
                        double k = got * e[i] * (cred[h] * System.Math.Max(0, ngdp[h]) * e[h] / credSum) / e[h];   // 金建ての取り分を受け皿の通貨へ
                        GiveGold(w[h], k);
                        w[h].ForeignCapitalIn += new MoneyStock(k);
                        fx += k;
                    }
                }
            }
            double flight = System.Math.Min(w[i].ForeignCapitalIn.Value, (w[i].ForeignCapitalIn * c.CapitalFlightRatePerQuarter.Value * (1.0 - cred[i])).Value);
            if (flight > 1e-9) { taken[i] += TakeGold(w[i], flight); w[i].ForeignCapitalIn -= new MoneyStock(flight); flightTotal += flight * e[i]; }   // 払えない分も債権ごと消える（C）＝下の還流で owner も同額減る＝Σ流入＝Σ対外保有の台帳整合（消えた請求権は金建てで数える）
            double div = (w[i].ForeignCapitalIn * c.CapitalDividendRatePerQuarter.Value).Value;
            if (div > 1e-9) taken[i] += TakeGold(w[i], div);   // 払えない配当は積まない（未払いの負債も未収の債権も立たない）
        }
        ProbeDiff("逃避配当");
        // 按分は金建て（対外資産×為替の比）＝逃避の請求権と戻る金を金建てで配り、各国の通貨へ戻す。
        double abroadSum = 0, backTotal = 0, takenSum = 0; for (int i = 0; i < n; i++) { abroadSum += (w[i].CapitalAbroad * e[i]).Value; backTotal += taken[i] * e[i]; takenSum += taken[i]; }
        if (abroadSum > 1e-9 && backTotal + flightTotal > 1e-9)
        {
            fx -= takenSum;
            for (int i = 0; i < n; i++)
            {
                double share = (w[i].CapitalAbroad * e[i]).Value / abroadSum;
                w[i].CapitalAbroad = new MoneyStock(System.Math.Max(0, w[i].CapitalAbroad.Value - flightTotal * share / e[i]));   // 消えた元本ぶん資産が減る（払えなかった分も＝債権者が損をかぶる）
                double k = backTotal * share / e[i];
                GiveGold(w[i], k);
                fx += k;
            }
        }
        else if (takenSum > 1e-9)   // ★A-2#4修正：対外保有が無い（台帳のズレ）期に抜いた配当/逃避がどの pop にも戻らず蒸発するのを防ぐ＝抜いた host へ返金（金を捨てない）
            for (int i = 0; i < n; i++)
                if (taken[i] > 0) GiveGold(w[i], taken[i]);   // 同じ国へ戻す＝換算なし
        ProbeDiff("還流");

        // (3) 新規フロー：収益率＞worldRate の国が呼び込み（流入）、＜の国が送り出す（流出）。材の清算（min(需要,供給)）と同型。
        var demand = new double[n]; var supply = new double[n];
        double totalDemand = 0, totalSupply = 0;
        for (int i = 0; i < n; i++)
        {
            if (ret[i] > worldRate)
            {
                demand[i] = (ret[i] - worldRate) * System.Math.Max(0, ngdp[i]) * e[i] * c.CapitalDemandScale;   // 金建て
                totalDemand += demand[i];
            }
            else
            {
                double exportable = InvestorSavings(w[i]) * c.CapitalExportFrac.Value;
                supply[i] = (worldRate - ret[i]) * exportable * e[i];   // 金建て
                totalSupply += supply[i];
            }
        }
        double traded = System.Math.Min(totalDemand, totalSupply) * riskAppetite;
        if (traded > 1e-9)
        {
            // ★支払いの一元化 段2-2（A6・2026-09-29）：受け手は**実際に動いた額**だけ受け取る＝先に送り手から引き（送り手の投資家の貯蓄まで）、
            //   引けた額の合計を需要の比で受け手へ配る（端数は最後の受け手）。旧＝受け手は traded を満額受け取り、送り手だけ貯蓄で切った
            //   （切られた分だけ世界に湧いた・TakeGold が引けなかった分も対外資産 CapitalAbroad に積んだ）。
            double sFill = traded / totalSupply, sent = 0;   // sent＝送り出せた金（金建て）
            for (int i = 0; i < n; i++)
                if (!(demand[i] > 0) && supply[i] > 0)
                {
                    double k = TakeGold(w[i], supply[i] * sFill / e[i]);   // 送り出し→対外資産（引けた額だけ・自国の通貨）
                    w[i].CapitalAbroad += new MoneyStock(k); sent += k * e[i]; fx -= k;
                }
            int lastD = -1; for (int i = 0; i < n; i++) if (demand[i] > 0) lastD = i;
            double given = 0;
            for (int i = 0; i < n; i++)
                if (demand[i] > 0)
                {
                    double kg = i == lastD ? sent - given : sent * (demand[i] / totalDemand);
                    given += kg;
                    double k = kg / e[i];
                    w[i].ForeignCapitalIn += new MoneyStock(k); GiveGold(w[i], k); fx += k;   // 流入→投資家貯蓄（工業化を支える・受け手の通貨）
                }
        }
        ProbeDiff("新規フロー");

        // === 02断線走査（2026-07-28）繋ぐべき筆頭＝対外国債の相手方（レンティア帝国の成立）===
        // 旧＝外債の利子は債務国のCumulativeTradeFlowから一方向に消えるだけ（片側記帳）＝「債務国は痛むが債権国は儲からない」
        // ＝19世紀金融の主役（シティのアルゼンチン債・パリのロシア債）が不在だった（2026-09-28 に旗 ForeignBondCounterparty ごと焼き込み）。
        // ★段3-3（2026-09-30）：元利の管は各国の通貨で積まれる＝金建てへ換算して集め、金建ての債権の比で配り、受け手の通貨へ戻す。
        double intPool = 0, intPaid = 0;
        for (int i = 0; i < n; i++) { intPool += w[i].ForeignIntPaidThisQuarter * e[i]; intPaid += w[i].ForeignIntPaidThisQuarter; w[i].ForeignIntPaidThisQuarter = 0; }
        // (1) 債権ストックの繰上げ：各債務国のΔForeignDebt を債権国（自国以外）の債権へ（AdjustClaims＝増えも減りも同じ規則・二国間台帳は持たない＝按分近似・決定論）。
        //     ★段3-3（C13・2026-09-30）：新しい対外借入は**貸し手が払った額そのもの**を貸し手の国の債権にする（MultiNationSim の貸し手の引き落とし＝払った比＝債権の比）。
        //       借入の月に PrevForeignDebt も上げる（Fiscal.Borrow）＝ここで数え直さない。ここへ来るのは利子の繰り入れ（増え）と償還・否認・不履行（減り）だけ。
        //     旧＝新規の借入を翌四半期の投資家の貯蓄の比で配った（払った比と債権の比が一致しない）・減りは自国の債権も含む全ての債権の比（増えは自国を除く＝C14）。
        for (int i = 0; i < n; i++)
        {
            double delta = (w[i].ForeignDebt - w[i].PrevForeignDebt).Value;
            w[i].PrevForeignDebt = w[i].ForeignDebt;
            if (System.Math.Abs(delta) < 1e-9) continue;
            AdjustClaims(nations, i, delta);
        }
        // (2) 元利の配当：債務国が今四半期に払った対外の利子と**元本**（Fiscalで既に対外流出として記帳済み）を、
        //     債権シェア比で債権国の投資家へGiveGold＝世界で金が閉じる（債権ゼロの初期は投資家貯蓄比で退避＝当期から閉鎖）。
        //     ★元本（2026-09-22）：旧＝(1) で債権を減らすだけで元本の金はどこにも届かなかった。(1) の減額は各国の債権に比例する＝
        //       減額後の債権の比は減らした債権の比と同じ＝元本は「減らした債権の比」で返る（同じ四半期の新規引受の按分近似は利子と共通）。
        //     ★段3-3：比は金建て（債権×為替）・取り分の閾値（旧 1e-12）を外した＝配り切る（配り残しが換算の台帳に紛れない）。
        if (intPool > 1e-9)
        {
            double claims = 0; for (int i = 0; i < n; i++) claims += (w[i].ForeignBondClaims * e[i]).Value;
            double basis = claims;
            if (basis <= 1e-9) { for (int i = 0; i < n; i++) basis += InvestorSavings(w[i]) * e[i]; }
            if (basis > 1e-9)
            {
                fx -= intPaid;
                for (int i = 0; i < n; i++)
                {
                    double share = claims > 1e-9 ? (w[i].ForeignBondClaims * e[i]).Value / claims : InvestorSavings(w[i]) * e[i] / basis;
                    double k = intPool * share / e[i];
                    if (k > 0) { GiveGold(w[i], k); fx += k; }
                }
            }
        }
        if (w[0].Diplomacy is { } dip) dip.CumulativeFxResidual += fx;   // 為替の換算の台帳（移し方の1本と同じ台帳）
        ProbeDiff("外債利払い");
    }


    /// <summary>★投資家の述語＝近代エリート（兵士を除く）。Closed（貴族・聖職）は投資から構造的に排除される（rentier=rent/(rent+earned)=1 に固定）。
    /// ★2026-10-11 (255)：貴族も投資家に含める休眠の係数 <c>NobleInvests</c>（貴族doc・2026-08-14・既定0）を削除した＝述語は引数が pop だけ。
    ///   根拠＝2026-08-14 の差し戻し（rentier 診断が二重に不成立・実効果は資金量＝露47%/清90% の貯蓄が投資プールへ入る清抑制の較正地雷）・休眠の棚卸し（measurements/dormant_20261011.tsv）。</summary>
    internal static bool IsInvestor(Pop p)
        => !p.IsSoldier && p.IsModernElite;

    /// <summary>★投資家の集合（段3-3・C13・2026-09-30＝述語 <see cref="IsInvestor"/> の1本・国の全ての州）。外資の払い手と受け手・対外債の貸し手・
    /// 運河の出資者と配当の受け手・巨大建造物の外資の出し手と利回りの受け手が同じ集合を読む（旧＝近代エリート〔活きた州だけ〕・IsInvestor・州の資本家1人〔InvestorPopIndex〕の3通り）。</summary>
    internal static List<Pop> Investors(World w)
    {
        var pops = new List<Pop>();
        foreach (var (_, p) in w.AllPopsWithRegion()) if (IsInvestor(p)) pops.Add(p);
        return pops;
    }

    // 投資家（資本家＝近代エリート）の貯蓄合計（送り出しの原資・流入の受け皿）。
    internal static double InvestorSavings(World w)
    {
        double s = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) if (IsInvestor(p)) s += System.Math.Max(0, p.Savings.Value);
        return s;
    }

    // 金を国外へ出す（投資家(近代エリート)の貯蓄から貯蓄比例で引く＋CumulativeTradeFlow に流出計上）。実際に引けた額を返す（残高不足は部分＝部分的な不履行）。
    // ★2026-09-27：原資は投資家だけ＝流入（GiveGold）と対称。旧＝fallbackToAll で投資家の不足分を全popから追加徴収していた。
    // ★支払いの一元化 段2-3（E2・2026-09-29）：引く・台帳へ書くは移し方の1本（World.Transfer＝払い手は投資家・受け手は世界の外）。ここに残るのは払い手の集合を選ぶことだけ。
    private static double TakeGold(World w, double amount, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0) => World.Transfer((w, Investors(w)), (null, null, null), amount, routeFile: routeFile, routeLine: routeLine);   // (271) routeFile/routeLine＝計器（読むだけ）＝呼び手の位置を移し方の1本へ通す   // 残高超は引けない（払えない＝部分的不履行）・引けた額を返す

    /// <summary>投資家へ払う（受け取りの1本・重みは貨幣の保有）。★段2-3（2026-09-29）：配る・台帳へ書くは移し方の1本（World.Transfer）。
    /// ★段3-3（C14・2026-09-30）：受け手は**投資家だけ**＝払い手（<see cref="TakeGold"/>＝投資家だけ）と対称。投資家のいない国（併合国の残骸等）は国庫へ
    ///   （旧＝投資家→全POP→国庫＝外資の配当や流入が農民の貯蓄へ落ちた）。<paramref name="from"/> 省略＝世界の外（国際金融の清算）／
    ///   国庫（国, null）＝運河の配当・巨大建造物の外資の利回り（区分 <paramref name="cat"/>・国境をまたげば移し方の1本が為替で換算）。戻り値＝払い手の通貨で動いた額。</summary>
    internal static double GiveGold(World w, double amount, (World? W, List<Pop>? Pops) from = default, TreasuryCat cat = TreasuryCat.OtherIn, [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)   // (271) routeFile/routeLine＝計器（読むだけ）
    {
        var pops = Investors(w);
        var held = new List<double>(pops.Count); double heldSum = 0;
        foreach (var p in pops) { double h = System.Math.Max(0, p.Savings.Value); held.Add(h); heldSum += h; }   // 重み＝貨幣の保有（貯蓄比）
        double moved = World.Transfer(from, (w, pops, heldSum > 1e-9 ? held : null), amount, cat, routeFile: routeFile, routeLine: routeLine);
        if (moved < amount) moved += World.Transfer(from, (w, null, null), amount - moved, cat, toCat: TreasuryCat.OtherIn, routeFile: routeFile, routeLine: routeLine);   // 投資家不在＝国庫
        return moved;
    }

    /// <summary>★債権の増減の1本（段3-3・C14・2026-09-30）：債務国 <paramref name="i"/> の対外債の変化 <paramref name="delta"/>（i の通貨）を、
    /// **i 以外**の国の債権（ForeignBondClaims・各自の通貨）へ金建ての債権の比で配る＝増えも減りも同じ集合・同じ規則（減りは0で止める＝損は持ち手）。
    /// 債権の無い世界の増え（利子の繰り入れ）は i 以外の投資家の貯蓄の比（金建て）。読み手＝四半期の繰上げ（<see cref="Step"/>）と再併合の否認（MultiNationSim.ReabsorbNations）。
    /// 旧＝減りは自国の債権も含む全ての債権の比（増えは自国を除く）・再併合の否認は別の複製。</summary>
    internal static void AdjustClaims(IReadOnlyList<Simulation> nations, int i, double delta)
    {
        int n = nations.Count;
        double g = delta * nations[i].World.FxRate, basis = 0;
        for (int j = 0; j < n; j++) if (j != i) basis += (nations[j].World.ForeignBondClaims * nations[j].World.FxRate).Value;
        if (basis > 1e-9)
        {
            for (int j = 0; j < n; j++)
                if (j != i) { var wj = nations[j].World; wj.ForeignBondClaims = new MoneyStock(System.Math.Max(0, (wj.ForeignBondClaims * (1.0 + g / basis)).Value)); }
            return;
        }
        if (!(g > 0)) return;
        double inv = 0;
        for (int j = 0; j < n; j++) if (j != i) inv += InvestorSavings(nations[j].World) * nations[j].World.FxRate;
        if (inv > 1e-9)
            for (int j = 0; j < n; j++)
                if (j != i) { var wj = nations[j].World; wj.ForeignBondClaims += new MoneyStock(g * (InvestorSavings(wj) * wj.FxRate / inv) / wj.FxRate); }
    }
}
