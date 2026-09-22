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
    private const int NoGdpDistressDebtRatio = 9;       // NominalGdp≈0 かつ債務ありのときの代替（極大）債務比率
    private const double BondConfMin = 0.2;             // 国債スプレッドが対外信認を下げる際の下限クランプ
    private const double ExchangeRateCreditFloor = 0.3; // 為替減価が対外信認を削る際の下限クランプ

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
            for (int i = 0; i < n; i++) probeD[i] = w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued;
        }
        void ProbeDiff(string label)
        {
            if (!Simulation.ConservationProbe || probeD is null) return;
            for (int i = 0; i < n; i++)
            {
                double now = w[i].BaseMoney - w[i].CumulativeTradeFlow - w[i].CumulativeGoldMinted - w[i].CumulativeFiatIssued;
                if (System.Math.Abs(now - probeD[i]) > 1e-4)
                    System.Console.Error.WriteLine($"[PROBE金融] {label} {w[i].Region.Name} {w[i].Year}年 ΔD={now - probeD[i]:F6}");
                probeD[i] = now;
            }
        }
        var ret = new double[n];    // 投資収益率（＝資本価格）：工業化の伸びしろ×信用。高い国が外資を呼ぶ
        var cred = new double[n];   // 信用：低債務×金本位×通貨安定。低いと外資が逃げる
        for (int i = 0; i < n; i++)
        {
            w[i] = nations[i].World;
            // [単位・B13] 債務/GDP＝**月次名目**（GovDebt÷月次NominalGdp）＝Fiscal の年次名目 dGdp の約12倍の値。NoGdpDistressDebtRatio・信用の閾値はこの単位
            double debtR = w[i].NominalGdp > 1e-9 ? w[i].GovDebt / w[i].NominalGdp : (w[i].GovDebt > 0 ? NoGdpDistressDebtRatio : 0);
            // ★AUD-09（Fiscal.cs の dGdp と同じ規律・同じ書き方）：分母の絶対値ガードだけでは微小非ゼロで比が発散し、
            //   NaN は `Math.Clamp` を素通りして CreditDebtTerm を毒する。⚠否定形で番兵へ倒す。
            //   ★有限値では厳密同値：読み手は下2行の `Clamp(1−debtR/DebtDistressRatio, 0, 1)` だけで、
            //     DebtDistressRatio=2.0 に対し番兵9は既に 0 へクランプされる帯＝**挙動は変わらない**。
            if (!(debtR <= NoGdpDistressDebtRatio)) debtR = NoGdpDistressDebtRatio;
            // 国債二次流通との連動（改修2026-06-28・金融改革①）：国債の利回りスプレッド(暴落)が対外信用を下げる＝国債暴落→資本逃避→為替減価→（既存経路で）さらに国債金利↑のバーリング危機ループ。
            double bondSpread = System.Math.Max(0, w[i].GovBondRate - c.BondBaseRate);
            double bondConf = System.Math.Clamp(1.0 - c.BondYieldFlightWeight * bondSpread, BondConfMin, 1.0);
            double cr = System.Math.Clamp(1.0 - debtR / c.DebtDistressRatio, 0, 1)
                      * (c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility)   // 兌換可能性で対外信用を連続補間（倍率排除②・Conv=1で無ペナルティ・Conv=0で0.5）
                      * System.Math.Clamp(w[i].ExchangeRate, ExchangeRateCreditFloor, 1.0)   // 減価＝対外信用の低下
                      * bondConf;   // 国債暴落＝信認喪失で外資が逃げる
            cred[i] = cr;
            // ★純記帳（2026-08-13）：信用と4つの内訳を World へ写す。読み手はUIだけ＝力学に一切影響しない。
            w[i].CreditRating = cr;
            w[i].CreditDebtTerm = System.Math.Clamp(1.0 - debtR / c.DebtDistressRatio, 0, 1);
            w[i].CreditGoldTerm = c.OffGoldCapitalPenalty + (1.0 - c.OffGoldCapitalPenalty) * w[i].Convertibility;
            w[i].CreditFxTerm = System.Math.Clamp(w[i].ExchangeRate, ExchangeRateCreditFloor, 1.0);
            w[i].CreditBondTerm = bondConf;
            double headroom = System.Math.Max(0, c.CapitalIndTarget - w[i].MeanIndustrialization);   // 改修2026-06-28：首都州のみ→全州平均（外資は国全体の工業化ギャップで流入先を選ぶ）
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
        double riskAppetite = System.Math.Clamp(1.0 - c.CapitalContagion * (1.0 - worldCred), 0, 1);

        // (1) 資本逃避：信用の低い国の外資が本国送還（host から流出）。(2) 配当：host が ForeignCapitalIn に divRate を払う。
        //     両者の総額を owner 国へ CapitalAbroad 比で戻す（逃避は元本＝資産も減る／配当は資産不変）。
        // 帰属修正（監査経9・#17で有効化）：流入は toInvestors:true で投資家貯蓄へ入るのに、配当/逃避の流出は全pop按分＝
        //   「外資で資本家が潤い、配当は国民の人頭税で払う」非対称。CapitalOutflowFromInvestors>0 で流出も投資家原資（不足分のみ全体フォールバック）・0で全pop按分。
        //   ⚠2値スイッチ（既定1.0）。B4裁定（2026-07-31）の「中間値＝両者の按分」は未実装＝0<値<1 は 1 と同じ挙動。
        bool outFromInvestors = c.CapitalOutflowFromInvestors > 0;
        double flightTotal = 0, divTotal = 0;
        var taken = new double[n];   // host 別に実際に抜いた額（逃避＋配当）。保有者が居ない場合の返金に使う＝金を捨てない（保存）
        ProbeSnap();
        for (int i = 0; i < n; i++)
        {
            // 資金逃避（07_再設計 §5(b)＝B3自己成就期待の第2形・2026-07-31）：**外資の引き上げだけでなく、自国の資本家も逃げる**。
            // 引き金＝国債への不信（累積切り下げ／名目GDP＝Fiscal の distrust と同じ物差し）。逃げた金は対外資産（CapitalAbroad）へ移り、
            // 国内の投資原資が痩せる＝「信認を失った国は自国の資本にも見捨てられる」。★自己成就＝逃避→通貨/信認↓→さらに逃避。
            // 保存＝国内popから引いた額を受け皿の国の投資家へ渡す（下の★2026-09-22）。CapitalDomesticFlightRate=0 で厳密恒等。
            if (c.CapitalDomesticFlightRate > 0 && w[i].NominalGdp > 1e-9)
            {
                double distrust = System.Math.Clamp(w[i].CumulativeDevaluation / w[i].NominalGdp - c.CapitalFlightDistrustThreshold, 0, 1);
                // ★★逃避先の実在化（2026-08-14・外交観測が照らした会計非対称）：旧＝CapitalAbroad だけ増やし
                //   **相手国の ForeignCapitalIn をどこにも増やさない**＝世界のΣ対外純資産が正へ膨張し続け、
                //   慢性不信の国（清）が幻の対外資産79×GDPを積んで**他国の実物FDIの配当プールを吸い上げていた**
                //   （還流は CapitalAbroad 比で分配されるため）。
                //   1で逃避金の受け皿＝自国以外の最も信用の高い国の ForeignCapitalIn へ（credで加重・国名なし＝
                //   「資本はロンドンへ逃げる」が信用の分布から創発）。0＝負債は立たない（旧挙動）。
                // ★逃げた**金**の両建て（2026-09-22）：旧＝TakeGold で流出を記帳するだけで、どの国にも金が入らず世界から消えていた
                //   （国ごとの保存は閉じる＝計器に掛からない片側記帳。対外元本の償還と同じ形）。旧注記の「保存則は移転なので破れず」は誤りだった。
                //   受け皿は上と同じ「自国以外の信用×名目GDP」の按分＝逃げた金はその国の投資家の預金になる（GiveGold＝外資の流入と同じ入口）。
                //   受け皿が無い（他国の信用×GDPが0）なら逃げない。DomesticFlightHasHost は負債（ForeignCapitalIn）の記帳だけを受け持つ。
                double credSum = 0;
                if (distrust > 0)
                    for (int h = 0; h < n; h++) if (h != i) credSum += cred[h] * System.Math.Max(0, w[h].NominalGdp);
                if (credSum > 1e-9)
                {
                    double want = c.CapitalDomesticFlightRate * distrust * InvestorSavings(w[i]);
                    double got = TakeGold(w[i], want, fromInvestors: true);
                    w[i].CapitalAbroad += got;   // 逃げた金は対外資産＝還流ループ（配当・逃避の按分）に乗る
                    for (int h = 0; h < n; h++)
                    {
                        if (h == i) continue;
                        double k = got * (cred[h] * System.Math.Max(0, w[h].NominalGdp) / credSum);
                        GiveGold(w[h], k, toInvestors: true);
                        if (c.DomesticFlightHasHost > 0) w[h].ForeignCapitalIn += k;
                    }
                }
            }
            double flight = System.Math.Min(w[i].ForeignCapitalIn, w[i].ForeignCapitalIn * c.CapitalFlightRate * (1.0 - cred[i]));
            if (flight > 1e-9) { double got = TakeGold(w[i], flight, fromInvestors: outFromInvestors, fallbackToAll: outFromInvestors); w[i].ForeignCapitalIn -= got; flightTotal += got; taken[i] += got; }   // ③修正：実際に引けた額でストックを減らす（Σ流入＝Σ対外保有の台帳整合）
            double div = w[i].ForeignCapitalIn * c.CapitalDividendRate;
            if (div > 1e-9) { double gotDiv = TakeGold(w[i], div, fromInvestors: outFromInvestors, fallbackToAll: outFromInvestors); divTotal += gotDiv; taken[i] += gotDiv; }
        }
        ProbeDiff("逃避配当");
        double abroadSum = 0; for (int i = 0; i < n; i++) abroadSum += w[i].CapitalAbroad;
        double backTotal = flightTotal + divTotal;
        if (abroadSum > 1e-9 && backTotal > 1e-9)
            for (int i = 0; i < n; i++)
            {
                double share = w[i].CapitalAbroad / abroadSum;
                w[i].CapitalAbroad = System.Math.Max(0, w[i].CapitalAbroad - flightTotal * share);   // 引き上げた元本ぶん資産が減る
                GiveGold(w[i], backTotal * share, toInvestors: true);
            }
        else if (backTotal > 1e-9)   // ★A-2#4修正：対外保有が無い（台帳のズレ）期に抜いた配当/逃避がどの pop にも戻らず蒸発するのを防ぐ＝抜いた host へ返金（金を捨てない）
            for (int i = 0; i < n; i++)
                if (taken[i] > 1e-9) GiveGold(w[i], taken[i], toInvestors: true);
        ProbeDiff("還流");

        // (3) 新規フロー：収益率＞worldRate の国が呼び込み（流入）、＜の国が送り出す（流出）。材の清算（min(需要,供給)）と同型。
        var demand = new double[n]; var supply = new double[n];
        double totalDemand = 0, totalSupply = 0;
        for (int i = 0; i < n; i++)
        {
            if (ret[i] > worldRate)
            {
                demand[i] = (ret[i] - worldRate) * System.Math.Max(0, w[i].NominalGdp) * c.CapitalDemandScale;
                totalDemand += demand[i];
            }
            else
            {
                double exportable = InvestorSavings(w[i]) * c.CapitalExportFrac;
                supply[i] = (worldRate - ret[i]) * exportable;
                totalSupply += supply[i];
            }
        }
        double traded = System.Math.Min(totalDemand, totalSupply) * riskAppetite;
        if (traded > 1e-9)
        {
            double dFill = traded / totalDemand, sFill = traded / totalSupply;
            for (int i = 0; i < n; i++)
            {
                if (demand[i] > 0) { double k = demand[i] * dFill; w[i].ForeignCapitalIn += k; GiveGold(w[i], k, toInvestors: true); }   // 流入→投資家貯蓄（工業化を支える）
                else if (supply[i] > 0) { double k = System.Math.Min(supply[i] * sFill, InvestorSavings(w[i])); w[i].CapitalAbroad += k; TakeGold(w[i], k, fromInvestors: true); }   // 送り出し→対外資産
            }
        }
        ProbeDiff("新規フロー");

        // === 02断線走査（2026-07-28）繋ぐべき筆頭＝対外国債の相手方（レンティア帝国の成立）===
        // 旧＝外債の利子は債務国のCumulativeTradeFlowから一方向に消えるだけ（片側記帳）＝「債務国は痛むが債権国は儲からない」
        // ＝19世紀金融の主役（シティのアルゼンチン債・パリのロシア債）が不在だった。ForeignBondCounterparty=0で配当なし（旧）。
        // 今四半期の対外利払い（Fiscal が月次加算）はゲートの外で毎期回収＝0 のとき積み上がり続け再点火時に一括配当される事故を防ぐ。
        double intPool = 0; for (int i = 0; i < n; i++) { intPool += w[i].ForeignIntPaidThisQuarter; w[i].ForeignIntPaidThisQuarter = 0; }
        if (c.ForeignBondCounterparty > 0)
        {
            // (1) 債権ストックの繰上げ：各債務国のΔForeignDebt（新規>0/償還<0）を債権国（自国以外）の投資家貯蓄比で按分してForeignBondClaimsへ。
            //     償還は各債権国の債権シェア比で減額（二国間台帳は持たない＝按分近似・決定論）。
            double worldClaims = 0; for (int i = 0; i < n; i++) worldClaims += w[i].ForeignBondClaims;
            for (int i = 0; i < n; i++)
            {
                double delta = w[i].ForeignDebt - w[i].PrevForeignDebt;
                w[i].PrevForeignDebt = w[i].ForeignDebt;
                if (System.Math.Abs(delta) < 1e-9) continue;
                if (delta > 0)
                {
                    double lenders = 0; for (int j = 0; j < n; j++) if (j != i) lenders += InvestorSavings(w[j]);
                    if (lenders > 1e-9)
                        for (int j = 0; j < n; j++) if (j != i) w[j].ForeignBondClaims += delta * (InvestorSavings(w[j]) / lenders);
                }
                else if (worldClaims > 1e-9)
                {
                    for (int j = 0; j < n; j++)
                        w[j].ForeignBondClaims = System.Math.Max(0, w[j].ForeignBondClaims + delta * (w[j].ForeignBondClaims / worldClaims));
                }
            }
            // (2) 元利の配当：債務国が今四半期に払った対外の利子と**元本**（Fiscalで既に対外流出として記帳済み）を、
            //     債権シェア比で債権国の投資家へGiveGold＝世界で金が閉じる（債権ゼロの初期は投資家貯蓄比で退避＝当期から閉鎖）。
            //     ★元本（2026-09-22）：旧＝(1) で債権を減らすだけで元本の金はどこにも届かなかった。(1) の減額は各国の債権に比例する＝
            //       減額後の債権の比は減らした債権の比と同じ＝元本は「減らした債権の比」で返る（同じ四半期の新規引受の按分近似は利子と共通）。
            if (intPool > 1e-9)
            {
                double claims = 0; for (int i = 0; i < n; i++) claims += w[i].ForeignBondClaims;
                double basis = claims;
                if (basis <= 1e-9) { for (int i = 0; i < n; i++) basis += InvestorSavings(w[i]); }
                if (basis > 1e-9)
                    for (int i = 0; i < n; i++)
                    {
                        double share = claims > 1e-9 ? w[i].ForeignBondClaims / claims : InvestorSavings(w[i]) / basis;
                        if (share > 1e-12) GiveGold(w[i], intPool * share, toInvestors: true);
                    }
            }
            ProbeDiff("外債利払い");
        }
    }


    /// <summary>★投資家の述語（貴族doc・2026-08-14）。旧＝IsModernElite のみ＝Closed（貴族・聖職）は投資から構造的に排除され、
    /// earned が増えず rentier=rent/(rent+earned)=1 に固定＝**思想機構は正しいのに入口が塞がっていて「貴族が永久に保守」**だった。
    /// NobleInvests>0 で Closed も投資家に含める（「近代化した国の貴族は投資する」が国名なしで出る＝プロイセンのユンカー工業投資）。
    /// 既定0＝旧述語と厳密恒等。⚠聖職者も Closed に含まれる（分離は6a裁定待ち＝含めたまま観測）。</summary>
    internal static bool IsInvestor(Pop p, Coefficients c)
        => !p.IsSoldier && (p.IsModernElite || (c.NobleInvests > 0 && p.Closed));

    // 投資家（資本家＝近代エリート）の貯蓄合計（送り出しの原資・流入の受け皿）。
    private static double InvestorSavings(World w)
    {
        double s = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (IsInvestor(p, w.Coef)) s += System.Math.Max(0, p.Savings);   // ★NobleInvests（既定0＝旧述語と恒等）
        return s;
    }

    // 金を国外へ出す（pop から引く＋CumulativeTradeFlow に流出計上）。実際に引けた額を返す（残高不足は部分＝部分的な不履行）。
    // fromInvestors＝投資家(近代エリート)の貯蓄を原資にする。fallbackToAll＝投資家で賄えない不足分のみ全pop按分で追加徴収（配当/逃避の帰属修正・監査経9）。
    private static double TakeGold(World w, double amount, bool fromInvestors = false, bool fallbackToAll = false)
    {
        double done = TakeGoldFrom(w, amount, fromInvestors);
        // 投資家原資で不足した分だけ非投資家(残りの全pop)へフォールバック＝原資は投資家優先だが不履行を全体で埋める（不足分のみ全体按分）。
        if (fallbackToAll && fromInvestors && amount - done > 1e-9)
            done += TakeGoldFrom(w, amount - done, fromInvestors: false, excludeInvestors: true);
        return done;
    }

    // 指定 pop 集合から貯蓄比例で amount を引く（TakeGold の実体）。excludeInvestors＝投資家を除いた残りの pop から引く（フォールバック用）。
    private static double TakeGoldFrom(World w, double amount, bool fromInvestors, bool excludeInvestors = false)
    {
        var pops = new List<Pop>();
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            bool isInv = IsInvestor(p, w.Coef);   // ★NobleInvests（既定0＝旧述語と恒等）
            if (fromInvestors && !isInv) continue;
            if (excludeInvestors && isInv) continue;
            pops.Add(p);
        }
        double total = 0; foreach (var p in pops) total += System.Math.Max(0, p.Savings);
        double take = System.Math.Min(amount, total);   // 残高超は引けない（払えない＝部分的不履行）
        if (take <= 1e-12 || total <= 1e-9) return 0;
        double done = 0;
        for (int i = 0; i < pops.Count; i++)
        {
            double s = take * (System.Math.Max(0, pops[i].Savings) / total);
            pops[i].Savings -= s; done += s;
        }
        w.CumulativeTradeFlow -= done;   // 対外流出（保存：金が国外へ）
        return done;
    }

    // 金を国外から受ける（pop へ足す＋CumulativeTradeFlow に流入計上）。toInvestors＝投資家へ（流入は工業化を支える）。
    private static void GiveGold(World w, double amount, bool toInvestors = false)
    {
        if (amount <= 1e-12) return;
        var pops = new List<Pop>();
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (!toInvestors || IsInvestor(p, w.Coef)) pops.Add(p);
        if (pops.Count == 0) foreach (var r in w.Regions) foreach (var p in r.Pops) pops.Add(p);   // 投資家不在なら全popへ
        if (pops.Count == 0) { w.Pay(TreasuryCat.OtherIn, amount); w.CumulativeTradeFlow += amount; return; }   // pop不在の World（併合国の残骸等）＝国庫残置（wave2b：pops[^1]のIndexOutOfRange回避。TakeGoldFromは同条件で0返しなのに片側だけ守られていなかった）
        double total = 0; foreach (var p in pops) total += System.Math.Max(0, p.Savings);
        double done = 0;
        if (total > 1e-9)
            for (int i = 0; i < pops.Count - 1; i++) { double s = amount * (System.Math.Max(0, pops[i].Savings) / total); pops[i].Savings += s; done += s; }
        pops[^1].Savings += amount - done;   // 端数は最後のpopへ（残高ゼロでも受け取れる）
        w.CumulativeTradeFlow += amount;   // 対外流入（保存：金が国内へ）
    }
}
