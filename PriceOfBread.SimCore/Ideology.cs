namespace PriceOfBread.SimCore;

/// <summary>
/// イデオロギーの創発（§政体・画一遷移の力ベクトル）。各POPのイデオロギー分布を
/// 階級の基本傾向（経済導出）× 条件（意識・闘争度）× 時代（年）× 戦争（厭戦）から毎四半期に更新する。
/// 段2稼働＝集計（<see cref="National"/>/<see cref="Regional"/>）→政体親和（<see cref="SupportAt"/>）を Transition・CrownParliament・PowerAffinity が読む。
/// 重み（BaseWeights・更新式）はコード定数。インデックスは <see cref="Ideology"/> enum と一致（0..10・9=ファシズム・10=王党派）。
/// </summary>
public static class Ideologies
{
    public const int Count = 11;   // 9ファシズム（1934拡張E7）・10王党派（2026-08-19）を追加。各ゲート0でそのシェアは常に0＝短い配列と数値等価（総和/ArgMax/加重平均に無影響）
    // 0伝統 1反動 2自由 3共和 4ナショ 5社会 6共産 7無政府 8帝国 9ファシズム 10王党派
    // マジックナンバー一掃（2026-07-20・値据え置き・命名のみ）：平均超の相対的富の上限（0..3＝失う物）。正規化と再スケールで同値を共有
    private const double WealthExcessCap = 3.0;

    public static void UpdateShares(World w)
    {
        // 国民平均の1人あたり富（相対的富＝イデオロギーの導出に使う）。富で「失う物」が決まる＝保守/急進の分岐。
        double totSav = 0, totPop = 0, urbanPop = 0;
        foreach (var p in w.AllPops()) { totSav += System.Math.Max(0, p.Savings.Value); totPop += p.Size; if (p.IsUrban) urbanPop += p.Size; }
        double meanWealth = totPop > 1e-9 ? totSav / totPop : 1;

        var era = EraGates(w, totPop, urbanPop);
        double dev = era.Dev, eraSoc = era.Socialism, eraComm = era.Communism, eraImp = era.Imperialism, eraFasc = era.Fascism;
        // 厭戦（war exhaustion）はイデオロギーへ直接加点しない＝Politics で闘争度入力へ移管（倍率排除/長い裾）。闘争→既存の mil 項（閉鎖階級→反動・大衆→共和/社会/共産）が拾う。

        // 機能7-1/7-2（第7段第2波・2026-07-28）：敗戦ショック（短期＝DefeatShock減衰0.85/四半期）＋失地ストック（長期＝半減期20年）→
        // r[4]ナショナリズムの噴出（刺され伝説1918独・アルザス1871→1914）。ループ前に1回評価＝O(pops)回避。
        // 二重計上裁定（機能7-6＝案A）：RevanchismTensionは「特定の相手国への対外敵意（緊張・ワンショット）」・本項は「国内イデオロギー分布（持続）」＝出力先が異なる。
        // 機能8-②/⑤（2026-07-28）：戦勝→主戦論（日本型・敗戦のr[4]+r[1]に対する対称）＋外圧→攘夷/富国強兵（清の反帝国主義）もr[4]へ。
        double nationalismSurge = w.Coef.DefeatNationalism * w.DefeatShock + w.Coef.LostLandNationalism * w.LostRegionsCumulative
            + w.Coef.VictoryNationalism * w.VictoryShock + w.Coef.ExternalPressureNationalism * w.ExternalPressure;
        double victoryImp = w.Coef.VictoryImperialism * w.VictoryShock;   // 機能8-②：戦勝→帝国主義（植民地熱）
        // 機能8-③（2026-07-28）：恐慌→両極化（左＝社会/共産・右＝反動。ワイマール型）。恐慌は平均60.8回/試合＝係数は小さく（1項ずつ--zero帰属）。
        double dd = w.GdpDrawdown;
        double depSoc = w.Coef.DepressionSocialism * dd, depComm = w.Coef.DepressionCommunism * dd, depReact = w.Coef.DepressionReaction * dd;
        // 機能8-④（2026-07-28）：飢えの記憶→急進側（共和1789/社会）。「パンの値段」の題名に最も近い機構。
        double famRep = w.Coef.FamineRepublicanism * w.FamineMemory, famSoc = w.Coef.FamineSocialism * w.FamineMemory;

        // 王党派の供給源（★ループ前に1回＝O(pops)回避。nationalismSurge と同じ作法）。RoyalistGate=0 なら読まない。
        double royalPrivilege = w.Coef.RoyalistGate > 0 ? PowerShares.PrivilegeIntact(w) : 0;
        double royalPatronage = w.Coef.RoyalistGate > 0 ? PowerShares.StatePatronage(w) : 0;
        // ★反動の担い手＝**地位を失いつつある層**（2026-08-20・休眠）。ループ前に1回（O(pops)回避・上と同じ作法）。
        //   ⚠特権の毀損＝農奴解放・免税の廃止・身分制の解体が進むほど大きい＝**旧秩序が現に壊されている度合い**。
        //   ★史実の順序どおり＝1861年の農奴解放が貴族の反動を生み、1876年の秩禄処分が士族反乱を生んだ。
        double privLoss = w.Coef.ReactionFromLoss > 0 ? 1.0 - PowerShares.PrivilegeIntact(w) : 0;
        if (!(privLoss >= 0 && privLoss <= 1)) privLoss = 0;
        // ★国民意識→ナショナリズム（休眠）の国民意識もループ前に1回（add-05・2026-09-26）：World.NationalConsciousness は全POPを走査する＝
        //   ループの中で読むと点火した瞬間に POP 数の2乗になっていた。読む値（各POPの Size と国民意識）はこのループで書かれない＝厳密恒等。
        double ncForNationalism = w.Coef.NationalismFromConsciousness > 0 ? System.Math.Max(0, w.NationalConsciousness) : 0;
        foreach (var p in w.AllPops())
        {
            var r = p.IdeologyShares;
            double rich = BaseWeights(p, r, meanWealth, w.Coef);          // 平均超の富（0..3＝失う物）
            double cons = p.Consciousness, mil = p.Militancy;
            // 啓蒙・過激化は**加算**（基盤0からでも生まれる＝農民でも意識が上がれば自由主義が芽生える）。
            // 倍率排除/長い裾：反動の Closed ハードコードを廃し**相対的富（失う物）から連続化**。富裕ほど旧秩序を脅かす急進左派を恐れ反動化
            // ＝1848のブルジョワ反動。進歩傾向（意識→自由/共和、闘争→左派）は失う物の少ない層ほど強い＝(1−執着)。
            // 富裕な閉鎖貴族は conserv≈1 で旧「反動のみ」を再現／無産大衆は conserv≈0 で旧「進歩のみ」。
            double conserv = PoliticalQuantities.Clamp01(rich / 2.0);                        // 反動への執着（富裕ほど強い・wealthRel≥3 で飽和）
            double prog = 1.0 - conserv;
            // ★反動＝旧式は「脅かされた失う物のある層」＝闘争度×富。⚠ReactionFromLoss>0 で**地位の喪失**へ差し替える（休眠・既定0で厳密恒等）。
            //   ⚠旧式は闘争度と富の両立を要求するが現物では両立しない＝実測で反動が全期間ほぼ0（→ファシズムの燃料も枯れる）。
            if (w.Coef.ReactionFromLoss > 0)
            {
                // ★★担い手は**大衆を含む**（2026-08-21 第2走の是正）。第1走は閉鎖階級と兵士に絞って外した：
                //   ⚠実測＝閉鎖階級は人口の2〜6%で、基底が伝統4+2×富/反動2と厚いので0.5を足してもPOP内シェアが動かず、
                //     国全体の反動は**1%が構造上の上限**だった（測定値0.01と一致）。
                //   ⚠しかも自分の史実と矛盾していた＝**ヴァンデの反革命は農民**・国家人民党の基盤は農業経営者と俸給生活者＝**反動は大衆現象**。
                // ★大衆の門＝(1−意識)。同じ大衆の不満が**政治意識で左右に割れる**：意識の高い不満層は左へ（既存経路）、
                //   **意識の低い不満層は、旧秩序が現に壊されているとき反動へ**＝素朴王政観の層は普段は天命を支え、
                //   その世界が壊されると反革命になる（ヴァンデ）。3層分割（文盲→天命）と整合。
                double gate = (p.Closed || p.IsSoldier) ? 1.0 : PoliticalQuantities.Clamp01(1.0 - cons);
                r[1] += 2.0 * mil * w.Coef.ReactionFromLoss * privLoss * gate;
            }
            else
                r[1] += 2.0 * mil * conserv;                             // 反動（脅かされた失う物のある層）
            r[2] += 2.5 * cons * prog;                                   // 啓蒙→自由主義
            r[3] += (1.5 * cons + 1.0 * mil) * prog;                     // 共和主義（啓蒙＋過激）
            r[5] += (1.0 * cons + 1.5 * mil) * prog;                     // 社会主義
            r[6] += 2.0 * mil * prog;                                    // 共産主義（過激）
            r[7] += 1.0 * mil * prog;                                    // 無政府主義（過激）
            // 倍率排除⑥/長い裾：伝統の意識減衰 r[0]*=1−0.7cons を撤去。意識・闘争が r[2..7] を加点し Normalize で総和1へ割られるため伝統シェアは自動低下＝明示倍率は二重計上。正規化に委ねる。
            // 戦争崩壊（1917／敗戦の反動）の厭戦は闘争度経由（Politics）で上の mil 項が拾う＝Ideology からの直接加点は撤去（倍率排除/長い裾）。
            r[4] += nationalismSurge;   // 機能7-1/7-2/8-②/8-⑤：敗戦＋失地＋戦勝＋外圧→ナショナリズム（思想の門・ファシズム式の前＝r[9]の燃料として自動で効く）
            // 独統一トリガー解錠B（wf_d460b52d調査2・2026-08-08）：国民意識の高い国は平時もナショナリズムが底上げされる
            // （統一運動＝1848の前提。旧＝ナショはイベント駆動のみで、125年×53カ国で統一閾値0.25に誰も届かなかった）。
            // ⚠既定0＝加算ごとスキップ＝厳密恒等。国名なし＝国民意識という既存の実数から創発。
            if (w.Coef.NationalismFromConsciousness > 0)
                r[4] += w.Coef.NationalismFromConsciousness * ncForNationalism;
            // ★マニフェストの達成（作者指示2026-08-24）＝加担した思想へ支持が乗る（減衰ストック・Parliament.StepManifesto が積む）。
            //   出来事→思想の既存パターン（上の nationalismSurge・depSoc と同じ位置・同じ物差し）＝新しい経路を作らない。
            if (w.ManifestoIdeology >= 0 && w.ManifestoIdeology < r.Length && w.ManifestoCredit > 0)
                r[w.ManifestoIdeology] += w.ManifestoCredit;
            r[8] += victoryImp;         // 機能8-②：戦勝→帝国主義（帝国主義の門 eraImp＝発見の2値が見つけるまでを抑える）
            r[1] += depReact;           // 機能8-③：恐慌→反動（右極化）
            r[3] += famRep;             // 機能8-④：飢えの記憶→共和（1789）
            r[5] += depSoc + famSoc;    // 機能8-③/④：恐慌＋飢え→社会主義
            r[6] += depComm;            // 機能8-③：恐慌→共産（共産主義の門 eraComm＝発見の2値が見つけるまでを抑える）
            // 思想の門（後発イデオロギーはその国が見つけるまで0＝発見の表の2値・2026-10-01 (152)）＝加算後に掛ける。※ナショ(4)は門無し＝ボナパルティズムとして初期から存在。
            r[5] *= eraSoc; r[6] *= eraComm; r[8] *= eraImp;
            // ファシズム（E7・E9点火）：ファシズムを見つけた国(eraFasc)の発展した経済(dev)で危機(mil)が高いほど、反動(r[1])と国民主義(r[4])が権威主義へ結晶＝それらから recruit（反共・国家動員）。FascismGate=0で不発＝r[9]=0＝配列長9と数値等価。
            if (w.Coef.FascismGate > 0) r[9] += w.Coef.FascismGate * eraFasc * dev * mil * (r[1] + r[4]);
            // ★★★10 王党派（2026-08-19・作者裁定）：**政治意識を持つ王権支持者**＝「意識が上がる＝議会支持」という一本道を断つ。
            //   供給＝**王権から実際に利益を得ている層** × 政治意識。誰が利益を得ているかは既存の唯一実装を読む
            //   （PowerShares.PrivilegeIntact＝農奴制/免税/身分、PowerShares.StatePatronage＝関税/国家発注/秩序）。
            //   ⚠閉鎖階級→特権／近代エリート→恩顧／それ以外→供給チャネルなし＝旧 PowerShares.PopExecutiveLean（表モデルと一緒に 2026-09-26 に削除＝pol-09）と同じ写像。
            //   ⚠⚠次段の課題：この2関数は観測層(PowerShares)にあり、エンジンが観測層を呼ぶのは依存の向きとして逆。
            //     ただし「実装を2つ作らない」規律を優先した（旧doc §6-2 危険7）。移すならエンジン側の適切な場所へ。
            //   RoyalistGate=0 で1行も走らず r[10]=0 ＝ 配列長10と数値等価（9ファシズムと同じ作法）。
            if (w.Coef.RoyalistGate > 0)
            {
                double royalSupply = p.Closed ? royalPrivilege : (p.IsModernElite ? royalPatronage : 0);
                if (royalSupply > 0) r[10] += w.Coef.RoyalistGate * cons * royalSupply;
            }
            Normalize(r);
        }
    }

    /// <summary>
    /// 思想の門（0か1＝発見の表の思想の行の2値・2026-10-01 (152)）と発展度スカラー。<see cref="UpdateShares"/> が毎四半期に読む式そのもの＝
    /// 発見パネル（思想の解禁）も同じ関数を読む（第二の式を作らない）。
    /// </summary>
    public readonly record struct EraGateSet(double Dev, double Socialism, double Communism, double Imperialism, double Fascism);

    /// <summary>解禁度の取り出し（読み取り専用）。都市労働者シェアのために POP を1巡する。</summary>
    public static EraGateSet EraGates(World w)
    {
        double totPop = 0, urbanPop = 0;
        foreach (var p in w.AllPops()) { totPop += p.Size; if (p.IsUrban) urbanPop += p.Size; }
        return EraGates(w, totPop, urbanPop);
    }

    // 本体（呼び手が既に人口を数えている場合はこちら＝UpdateShares は1巡で済ませる）。
    private static EraGateSet EraGates(World w, double totPop, double urbanPop)
    {
        // ★思想の門＝**発見の表の思想の行の2値**（2026-10-01 (152)・設計 §1A-6・作者裁定 2026-09-28 ⑤＝4分野とも同じ骨組み・§1A-2＝発見は2値）。
        //   旧＝世界の発展度 dev の傾斜（社会主義 dev 0.20〜0.55・共産主義 0.35〜0.70・帝国主義 0.50〜0.85・ファシズム 0.75〜1.10×危機）と到達（旧 IdeologyReach）の大きい方
        //   ＝dev は世界最先端の工業化度で決まる＝全ての国がほぼ同時に開いた（計器 (152) の種1〜5＝社会主義の門 0.5 が主要国の全てで1788〜93年・共産主義 1792〜1806年）。
        //   新＝その国がその思想を見つけた（点数＝技術力＋必要性＋伝わりが一度でも100）なら1・まだなら0。必要性＝社会主義と共産主義は都市労働者の割合×闘争度・
        //   帝国主義は植民地に住む人の割合・ファシズムは危機（<see cref="FascismCrisis"/>＝旧の門に掛けていた条件）＝Discovery の SpreadNeed。
        //   伝わり＝思想と制度の到達（World.SpreadReach＝旧の到達と同じ StepDiffusion の積み上げ）。⚠ファシズムの支持は発見の後も UpdateShares の式（発展度×闘争度×（反動＋国民主義））が担う＝危機は門から必要性へ移った。
        // ※ナショナリズムは門を持たない＝**ボナパルティズムとしても扱う**：仏革命〜ナポレオン(1789〜)の革命的ナショナリズム
        //   ＝軍事的強権・国民動員は初期から存在する（MilitaryDictatorship への親和が高い＝ボナパルト型）。
        // 発展度スカラー dev∈[0,1]＝世界最先端の工業化度（IdeologyGateIndScale で0..1へ）と都市の割合の加重（IdeologyGateUrbanWeight）＝ファシズムの支持の式と発見パネルのホバーが読む。
        //   ★「世界最先端の工業化度」は w.IndustrialFrontier（MultiNationSim が各国最高の MeanIndustrialization を毎四半期注入）。単一国と多国の初四半期は自国の値へフォールバック。
        double frontierInd = System.Math.Max(w.IndustrialFrontier, w.MeanIndustrialization);
        double urbanWorkerShare = totPop > 1e-9 ? urbanPop / totPop : 0;
        double dev = PoliticalQuantities.Clamp01((1.0 - w.Coef.IdeologyGateUrbanWeight) * PoliticalQuantities.Clamp01(frontierInd / w.Coef.IdeologyGateIndScale)
                           + w.Coef.IdeologyGateUrbanWeight * urbanWorkerShare);
        var k = w.Discovered;
        return new EraGateSet(dev, Gate(k, Ideology.Socialism), Gate(k, Ideology.Communism), Gate(k, Ideology.Imperialism), Gate(k, Ideology.Fascism));
        static double Gate(bool[] k, Ideology i) => Discovery.Known(k, Discovery.IdeaRow(i)) ? 1.0 : 0.0;
    }

    /// <summary>ファシズムの危機＝敗戦か恐慌（どちらか）×左派の脅威∈[0,1]＝ファシズムの発見の必要性（Discovery の SpreadNeed・2026-10-01 (152)）。
    /// 旧＝ファシズムの門に掛けていた条件（2026-09-04 J-005②の実測で判明＝発展度だけでは大戦前にファシズムが解禁されていた〔◎7割帯 17→14〕・2026-09-05 点火・旗 FascismCrisisWeight は 2026-09-26 に焼き込み）。
    /// 史実＝伊1922・独1933は最も発展した社会ではない（英仏米の方が上）＝要ったのは**敗戦（または報われない勝利）× 大衆的な左翼の脅威 × 自由主義秩序の信用失墜（恐慌）**。
    /// ⚠前期の左派シェアを読む（National は pop に保存済みの値を集計する）＝同じティックの循環を作らない。</summary>
    public static double FascismCrisis(World w)
    {
        double defeat = PoliticalQuantities.Clamp01(w.DefeatShock);
        double slump = PoliticalQuantities.Clamp01(w.GdpDrawdown / System.Math.Max(1e-9, w.Coef.FascismCrisisSlumpRef));
        var natIdeo = National(w);
        double left = PoliticalQuantities.Clamp01((natIdeo[(int)Ideology.Socialism] + natIdeo[(int)Ideology.Communism]) / System.Math.Max(1e-9, w.Coef.FascismCrisisLeftRef));
        return PoliticalQuantities.Clamp01(System.Math.Max(defeat, slump) * left);   // ⚠敗戦か恐慌の**どちらか**（両方は要らない）× 左派の脅威
    }

    // 階級の基本傾向を**経済属性から導出**（手書き階級スイッチを廃止）。[Trad,React,Lib,Repub,Nat,Soc,Comm,Anarch,Imp]（9ファシズム・10王党派は UpdateShares の下段で加点）
    // 物質的立場＝地代依存度（農地の旧秩序）／相対的富（失う物＝保守 vs 窮乏＝急進）／特権(Closed)／教育エリート／都市／兵士。
    // ＝同じ階級でも富/所得源が変わればイデオロギーが動く（没落貴族の急進化等）＝創発。意識・闘争・時代の変調は下段（従来）で掛かる。
    // 返り値＝平均超の富 rich（呼び手の conserv 用＝同じ式を2度書かない）。
    private static double BaseWeights(Pop p, double[] r, double meanWealth, Coefficients c)
    {
        System.Array.Clear(r, 0, r.Length);
        double rent = System.Math.Max(0, p.LandRentPerMonth.Value), earned = System.Math.Max(0, p.EarnedPerMonth.Value);
        double rentier = (rent + earned) > 1e-9 ? rent / (rent + earned) : 0;          // 地代への依存度（地主＝旧秩序）
        double wpc = p.Size > 1e-9 ? System.Math.Max(0, p.Savings.Value) / p.Size : 0;
        double wealthRel = meanWealth > 1e-9 ? wpc / meanWealth : 1;                    // 相対的富（1=平均）
        double rich = PoliticalQuantities.Clamp01((wealthRel - 1) / WealthExcessCap) * WealthExcessCap;                             // 平均超の富（0..3＝失う物）
        double poor = PoliticalQuantities.Clamp01(1 - wealthRel);                                           // 平均未満の窮乏（0..1）

        if (p.Closed)
        {
            // （旧＝特権身分は所得構成を見ず一律に伝統・反動＝旗 NobleWeightsFromIncome=0 の枝は 2026-09-26 に削除）
            // ★所得構成から（NobleWeightsFromIncome・2026-09-06）：地代依存の貴族＝伝統、商業化した貴族（富裕・非地代）＝自由/帝国、没落貴族（窮乏）＝国民主義・急進。
            //   特権は加算（NoblePrivilegeWeight）＝旧秩序の受益者であることは所得源に依らず残る。
            r[0] = 2 * rentier + 0.5 + c.NoblePrivilegeWeight;
            r[1] = c.NoblePrivilegeWeight * 0.5 + rentier;
            r[2] = 1.0 + 3 * rich * (1 - rentier);
            r[3] = 0.5;
            r[4] = 0.5 + poor;                                         // 没落貴族＝国民主義（ポーランド小貴族・薩摩の下級武士）
            r[5] = 0.5 * poor * (1 - rentier);
            r[8] = rich * (1 - rentier) + 0.5;
        }
        else if (p.IsSoldier)    // 兵士＝武力と国家＝ナショ・帝国
        {
            r[4] = 4; r[8] = 2; r[0] = 1; r[1] = 1;
        }
        else
        {
            r[0] = 2 * rentier + 0.5;                                  // 伝統＝地代（農地の旧秩序）依存
            r[2] = (p.IsModernElite ? 3.0 : 1.0) + 3 * rich * (1 - rentier);   // 自由＝資本（富裕非地代）・教育
            r[3] = p.IsModernElite ? 2.0 : 1.0;                        // 共和＝市民・教育
            r[5] = 2 * poor * (1 - rentier) + (p.IsUrban ? 1.0 : 0);   // 社会＝賃金窮乏・都市
            r[6] = 1.5 * poor * (p.IsUrban ? 1.0 : 0);                 // 共産＝都市の窮乏
            r[7] = poor;                                               // 無政府＝窮乏（無産）
            r[8] = (p.IsModernElite ? 1.0 : 0) + rich * (1 - rentier); // 帝国＝資本
            r[4] = 0.5;
        }
        return rich;
    }

    private static void Normalize(double[] r)
    {
        double sum = 0;
        for (int i = 0; i < r.Length; i++) { if (!(r[i] >= 0)) r[i] = 0; sum += r[i]; }   // 否定形＝NaN も 0 へ倒す（有限値では r[i] < 0 と同値）
        if (sum < 1e-9) { r[0] = 1; return; }   // 退避：伝統100%（早期で全ゲート0等）
        for (int i = 0; i < r.Length; i++) r[i] /= sum;
    }


    // ===== 画一遷移（段2）：国民イデオロギー集計 → 政体親和の内積 → 引力点 =====

    // アナルシーは引力点から廃止（一過性の秩序崩壊は政体選択肢にしない）。財政破綻は経済悪化(Credit)で表現。
    public static readonly Polity[] Polities =
        [Polity.Absolutist, Polity.Despotic, Polity.Shogunate, Polity.ConstitutionalMonarchy,
         Polity.Republic, Polity.BourgeoisDictatorship, Polity.ProletarianDictatorship,
         Polity.MilitaryDictatorship];

    // 政体×イデオロギー親和（倍率排除⑧＝制度座標化）：手書き9×9を廃し、政体・思想を**4軸の制度座標**の点として置き、
    // 親和＝座標の近さ（距離）から創発させる。軸（すべて0..1）：
    //   軸0 参加（一人/エリート 0 ↔ みんな 1）＝参政権の広さ
    //   軸1 強権（合意・伝統 0 ↔ 武力・弾圧 1）
    //   軸2 経済（労働/再分配 0 ↔ 資本 0.5 ↔ 地主/伝統 1）
    //   軸3 国家軍（控えめ 0 ↔ 国家/軍/民族が前へ 1）
    // 段階1＝固定座標（法律連動のズレは段階2で追加）。年代ゲート（社会/共産/帝国）は UpdateShares で従来どおり維持。
    // 親和 = clamp(AffMax − AffSlope×距離, AffMin, AffMax)＝近い思想ほど高く、遠いほど低い（旧 −1..+2 のレンジを再現）。
    public const double AffMax = 2.0, AffMin = -1.0;   // ★2026-08-19：PowerAffinity が親和のレンジを読むため公開（アクセス修飾子のみ＝挙動不変）

    // 思想座標（0伝統 1反動 2自由 3共和 4ナショ 5社会 6共産 7無政府 8帝国 9ファシズム 10王党派）。
    private static readonly double[][] IdeologyCoord =
    [           // 参加  強権  経済  国家軍
        [ 0.00, 0.40, 1.00, 0.25 ],   // 0 伝統＝旧秩序・地主・低参加
        [ 0.00, 0.75, 1.00, 0.40 ],   // 1 反動＝強権で旧秩序を守る
        [ 0.60, 0.10, 0.50, 0.20 ],   // 2 自由＝制限選挙の立憲自由主義（古典的自由主義は財産制限選挙＝立憲君主制を好む。普通選挙は共和へ）・資本
        [ 1.00, 0.10, 0.45, 0.20 ],   // 3 共和＝最も広い参加・自由・市民
        [ 0.45, 0.50, 0.50, 1.00 ],   // 4 ナショ＝国家動員（ボナパルティズム）
        [ 0.75, 0.30, 0.00, 0.20 ],   // 5 社会＝広い参加・労働・再分配
        [ 0.30, 0.85, 0.00, 0.20 ],   // 6 共産＝前衛・強権・労働
        [ 0.70, 0.00, 0.20, 0.00 ],   // 7 無政府＝反国家・無強権・無産
        [ 0.30, 0.60, 0.55, 1.00 ],   // 8 帝国＝資本＋国家/軍の対外膨張
        [ 0.35, 0.90, 0.55, 1.00 ],   // 9 ファシズム（E7）＝大衆動員だが真の参政権なし・最高強権・資本コーポラティズム・国家/民族/軍が全面。軍事独裁座標[0.15,0.75,0.60,0.85]/反動/帝国に最近接＝軍事独裁へ写像が座標系から創発
        [ 0.55, 0.55, 0.90, 0.50 ],   // 10 王党派（2026-08-19・★座標は仮置き＝調整でいじる前提）：参加を求めるが秩序と財産を重んじる。
                                      //    ⚠国家軍を 0.50 に抑えるのが要点＝1.00 にするとナショ/帝国/ファシズムと同じく**軍部**へ流れて意味が無くなる（実測で確認済み）。
    ];

    // 政体座標（(int)Polity と一致：Absolutist=0..Anarchy=8）。固定（段階2で法律連動のズレを足す）。
    private static readonly double[][] PolityCoord =
    [           // 参加  強権  経済  国家軍
        [ 0.00, 0.50, 1.00, 0.30 ],   // 0 Absolutist＝王が決める・伝統・地主
        [ 0.05, 0.80, 0.50, 0.60 ],   // 1 Despotic＝近代化する強権（上からの自由化・国家主導）
        [ 0.00, 0.55, 1.00, 0.15 ],   // 2 Shogunate＝封建・鎖国（国家膨張は低い）
        [ 0.60, 0.20, 0.55, 0.30 ],   // 3 ConstitutionalMonarchy＝制限参加・自由・資本
        [ 0.95, 0.10, 0.40, 0.25 ],   // 4 Republic＝広い参加・自由・資本/労働
        [ 0.30, 0.60, 0.50, 0.45 ],   // 5 BourgeoisDictatorship＝資本＋武力で労働を抑える
        [ 0.30, 0.85, 0.00, 0.20 ],   // 6 ProletarianDictatorship＝労働＋前衛の強権
        [ 0.15, 0.75, 0.60, 0.85 ],   // 7 MilitaryDictatorship＝武力＋国家/軍が前面、かつ反動/帝国の権威主義クラスタに近い（保守支配の危機で軍政coupが立つ＝旧表で反動1.0/帝国1.0に正の親和）
        [ 0.70, 0.00, 0.20, 0.00 ],   // 8 Anarchy＝反国家（引力点からは除外＝Polities に非掲載）
    ];

    /// <summary>制度座標の近さ→親和（倍率排除⑧）。距離が近い思想ほど高い親和。</summary>
    public static double AffinityOf(double[] polity, double[] ideology, double slope)   // ★2026-08-19：PowerAffinity が機関同士の近さを読むため公開（アクセス修飾子のみ＝振舞い不変）
    {
        double d2 = 0;
        for (int i = 0; i < 4; i++) { double x = polity[i] - ideology[i]; d2 += x * x; }
        return System.Math.Clamp(AffMax - slope * System.Math.Sqrt(d2), AffMin, AffMax);
    }

    /// <summary>国民イデオロギーの集計＝Σ POPのイデオロギー分布 ×（規模×政治的重み）。意識ある層が政治を動かす。正規化。</summary>
    public static double[] National(World w) => Aggregate(w, w.AllPops());

    /// <summary>州スコープのイデオロギー集計＝<see cref="National"/> の反復元を r.Pops に替えた同型。
    /// 内戦による国家分裂（docs/202607/20260731_内戦による国家分裂_部分完了.md【一部】.md）の「州が陣営を選ぶ」物差し＝Politics の州陣営判定（内戦一元化）が読む。
    /// ★思想②を1ミリも侵さない——国名も文化名も出てこない。アメリカ南部が離れるのは**南部の pop の思想分布がそうだから**であって「南部」と書いたからではない。</summary>
    public static double[] Regional(World w, Region r) => Aggregate(w, r.Pops);

    private static double[] Aggregate(World w, IEnumerable<Pop> pops)
    {
        var agg = new double[Count];
        double total = 0;
        foreach (var p in pops)
        {
            // 倍率排除②：政体引力を動かすのは**意識ある層**だが、参政権を持つ層ほど寄与が大きい（票が声を増幅）。
            // 参政権ゲート外でも公共圏（サロン・新聞・示威＝1789/1848）で一定は寄与（floor）＝参政権ゼロでも自由化の機運は育つ。
            // 票はその上に乗る増幅＝参政権拡大が国民集計を左へ動かす。完全排除は「無参政権→自由化不能」の罠を生むため floor で回避。
            double voice = w.Coef.IdeologyVoiceFloor + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            double weight = p.Size * p.Consciousness * voice;
            if (!(weight > 0)) continue;   // 否定形＝NaN も除外（有限値では weight <= 0 と同値）
            var s = p.IdeologyShares;
            for (int k = 0; k < Count; k++) agg[k] += s[k] * weight;
            total += weight;
        }
        if (total > 1e-9) for (int k = 0; k < Count; k++) agg[k] /= total;
        return agg;
    }

    /// <summary>政体の静的基準座標（段階2のドリフト前）。返り値は内部配列の参照＝呼び出し側は破壊変更しない（ドリフトは複製して行う）。</summary>
    public static double[] BasePolityCoord(Polity pol) => PolityCoord[(int)pol];

    /// <summary>参政権→参加軸の開放度（0..1・倍率排除⑧段階2）。現政体の参加軸を実際の参政権へドリフトさせるのに使う。</summary>
    public static double Openness(Suffrage s) => s switch
    {
        Suffrage.Universal => 0.95, Suffrage.AdultMale => 0.80, Suffrage.Census => 0.55, _ => 0.00,
    };

    /// <summary>POPの**4軸理想点**＝その思想混合(IdeologyShares)を思想座標で加重平均（政治の空間モデル）。
    /// 各popの経済から創発した思想分布が、4軸空間の1点（その層が望む体制の位置）に集約される。現政体座標との距離が政治的不満を生む。</summary>
    public static double[] IdealPoint(double[] shares)
    {
        var pt = new double[4];
        double tot = 0;
        for (int k = 0; k < Count; k++)
        {
            double s = shares[k];
            if (s <= 0) continue;
            var co = IdeologyCoord[k];
            for (int i = 0; i < 4; i++) pt[i] += s * co[i];
            tot += s;
        }
        if (tot > 1e-9) for (int i = 0; i < 4; i++) pt[i] /= tot;
        return pt;
    }

    /// <summary>与えた政体座標での支持＝国民イデオロギー集計 · 親和（座標の近さから創発）。段階2は現政体に法律連動でドリフトした座標を渡す。</summary>
    public static double SupportAt(double[] agg, double[] polityCoord, double affinitySlope)
    {
        double s = 0;
        for (int k = 0; k < Count; k++) s += agg[k] * AffinityOf(polityCoord, IdeologyCoord[k], affinitySlope);
        return s;
    }

    /// <summary>ある政体への支持＝静的基準座標で評価（Runner診断・後方互換）。遷移本体は SupportAt にドリフト座標を渡す。</summary>
    public static double Support(double[] agg, Polity pol, double affinitySlope) => SupportAt(agg, PolityCoord[(int)pol], affinitySlope);
}
