namespace PriceOfBread.SimCore;

/// <summary>巨大建造物（2026-07-28・docs/202607/01_巨大建造物_部分完了.md 段3/段4）＝**単体で動作する疎結合モジュール**。
/// 設計方針（ユーザー指示2026-07-28）＝「**数値は相互に密結合で干渉しあうべきだが、モジュールは単体で動作する疎結合であるべき**」。
/// 本モジュールの依存は Diplomacy（個体の台帳）と各国 Simulation（host 州の解決）と Coefficients だけで、
/// 運河（StepCanals）・港湾・保険のどれにも依存しない＝運河を移行しなくても単体で成立する。
/// 一方で**数値は密結合**にしてある：建設は工場と同じ鋼/機械/セメント/製材と労働の市場を奪い合い、同じ国庫を食い、
/// 完成効果（耕地・送電容量・工業化度）は既存の経済がそのまま読む＝新しい状態を1つも作らない。
/// 運河テンプレから借りるのは「個体・完成判定・保有者の毎四半期再解決」だけで、**支払いは実物**（EconomyStep が資材＋労働を市場から買う）。
/// 2相方式＝ここで国庫→州の予約（MonumentBuildQueue）を積み、翌月以降 EconomyStep が買い、四半期末にここで束を回収して進捗へ移す。
/// 着工条件＝(1)技術ゲート (2)**host 州を自国が領有** (3)国庫。★(2)を既存の運河に適用してはならない（スエズは「所有に関わらず各国が投資」＝仏資本の史実で、
/// ガードを入れるとオスマンだけが拠出することになり永久に開通しない）＝だから建造物は運河と別の機構として立てている。
/// Coefficients.MegaprojectEnabled=0 で個体が生成されず本 Step も即 return＝厳密恒等（較正中立も同じガード）。</summary>
public static class Megaprojects
{
    /// <summary>州に積まれた建設の束を、前四半期の拠出比で各建造物へ配る（2026-08-02）。
    /// ★束は個体でなく州に溜まる（Region.MonumentBuiltUnits）ので、host 州を共有する組（ロンドン＝水晶宮/地下鉄、
    /// ベルン＝ゴッタルド/アルプス水力、カイロ＝アスワン/ケープ＝カイロ鉄道）では「誰の束か」が会計上決まらない。
    /// 拠出額（PendingClaim）の比で配る＝払った者が受け取る。拠出0の個体には1単位も渡らない＝未着工がタダで完成しない。
    /// 誰も拠出していないのに束がある場合は捨てる（予約は請求権でカネは未出＝保存は不変。EconomyStep の Queue ゼロ化と同じ作法）。</summary>
    private static void CollectBuiltUnits(Diplomacy dip, IReadOnlyList<Simulation> nations)
    {
        foreach (var sim in nations)
            foreach (var r in sim.World.Regions)
            {
                if (r.MonumentBuiltUnits <= 0) continue;
                double total = 0;
                foreach (var mp in dip.Megaprojects)
                    if (!mp.IsComplete && mp.Host == r.Name && mp.PendingClaim > 0) total += mp.PendingClaim;
                if (total > 1e-12)
                    foreach (var mp in dip.Megaprojects)
                        if (!mp.IsComplete && mp.Host == r.Name && mp.PendingClaim > 0)
                            mp.BuildProgress += r.MonumentBuiltUnits * (mp.PendingClaim / total);
                r.MonumentBuiltUnits = 0;
            }
        foreach (var mp in dip.Megaprojects) mp.PendingClaim = 0;   // 拠出の記録は1四半期限り（積み残さない）
    }

    /// <summary>残りの建設に要る金額の見積り（2026-08-02・作者指示「工場建設のようにコストを表示してクリック」）。
    /// 建設の束1単位は工場・国家建設と**同じ束**（鋼1.0／機械0.15／セメント0.4／製材0.5）を食い、
    /// さらに材料費×ConstructionLaborWageFrac の工賃が労働市場に立つ。
    /// ★価格は **host 州のローカル市場**を読む＝EconomyStep が実際に払う場所と同じ（首都価格で代用すると内陸/植民地 host でずれる）。
    /// 見積りであって固定価格ではない（工場の FactorySpec.Cost と違い、資材相場で実支出は上下する）。</summary>
    public static double EstimateCost(Region host, Megaproject mp, Coefficients coef)
    {
        double costPerUnit = EconomyStep.BundleCost(host, coef);   // 建設の資材の束の1本（建てる州の価格）
        double perUnit = costPerUnit * (1.0 + coef.ConstructionLaborWageFrac);
        return Math.Max(0, mp.BuildUnits - mp.BuildProgress) * perUnit;
    }

    /// <summary>着工できるか（2026-08-02）。★プレイヤーのボタンとAIの判断が**この同じ関数**を通る＝AI=プレイヤー対称。
    /// 条件＝機構有効・未完成・未着工・建設地の州を自国が領有・技術水準が足りている。</summary>
    public static bool CanStart(World w, Megaproject mp) => StartRefusal(w, mp) is null;

    /// <summary>(435) 着工できない理由（できるなら null）。<see cref="CanStart"/> はこれが null かどうかそのもの＝可否の条件と評価の順は旧の条件式と同じ（Runner の play の <c>!mega</c> の拒否の理由が本体の述語から出る・第二の基準を作らない）。
    /// 文言は定数（AI が毎四半期に呼ぶ経路で文字列を組まない）。</summary>
    public static string? StartRefusal(World w, Megaproject mp)
    {
        if (!(w.Coef.MegaprojectEnabled > 0)) return "巨大建造物の機構が無効";
        if (mp.IsComplete) return "完成している";
        if (mp.Started) return "着工済み";
        if (!(mp.Owner is { } o && w.SelfPower is { } me && ReferenceEquals(o, me))) return "建設地の州を自国が領有していない";
        if (!(w.TechLevel >= mp.TechGate)) return "技術が足りない";
        return null;
    }

    /// <summary>着工する（動詞）。条件を満たさなければ何もせず false＝押せてしまう経路が出来ても状態は壊れない。</summary>
    public static bool TryStart(World w, Megaproject mp)
    {
        if (!CanStart(w, mp)) return false;
        mp.Started = true;
        w.Diplomacy?.Log.Add($"{w.Year} {mp.Name}が着工（{w.SelfPower?.Name ?? "中立"}）");
        return true;
    }

    public static void Step(Diplomacy dip, IReadOnlyList<Simulation> nations, Coefficients coef)
    {
        if (!(coef.MegaprojectEnabled > 0) || dip.Megaprojects.Count == 0) return;   // 0（較正中立ガード）＝機構ごと非実行。否定形＝NaN係数も不走行側へ
        CollectBuiltUnits(dip, nations);   // 2相方式の後半＝州に積まれた束を、前四半期の拠出比で各個体へ配る（2026-08-02）
        // host 州→(保有 Simulation, Region) を1回だけ索引化（州名は一意＝先着順は旧の全国×全州の線形探索と同値）。
        var hostIndex = new Dictionary<string, (Simulation Sim, Region Region)>();
        foreach (var sim in nations)
            foreach (var r in sim.World.Regions)
                hostIndex.TryAdd(r.Name, (sim, r));
        foreach (var mp in dip.Megaprojects)
        {
            // host 州とその保有国を解決（毎四半期＝征服で保有者が動く）。植民地は見ない＝州が無ければ着工不能。
            Simulation? hostSim = null; Region? hostRegion = null;
            if (hostIndex.TryGetValue(mp.Host, out var host)) { hostSim = host.Sim; hostRegion = host.Region; }
            mp.Owner = hostSim?.World.SelfPower;
            if (hostSim is null || hostRegion is null) continue;   // 建設地が存在しない（未領有・分離・未格上げの植民地）＝着工不能
            var hw = hostSim.World;

            if (mp.IsComplete)
            {
                ApplyEffect(mp, hostRegion, coef);   // 毎期整合＝割譲・征服で州ごと移っても効果は州に付いて回る（Institutions と同型）
                PayForeignYield(dip, nations, mp, hw, coef);   // 段5：完成後も果実の一部が恒久的に外へ流れる（外資を入れた代償）
                continue;
            }
            if (mp.BuildUnits > 1e-9 && mp.BuildProgress >= mp.BuildUnits)
            {
                mp.IsComplete = true;
                ApplyEffect(mp, hostRegion, coef);
                // 段6 威信（2026-07-28）：完成の威信を**正統性へ一回加算**。Formations の「国家統一の威信」と同型で、
                // Politics.cs の正統性の再計算（前期値−侵食+再生→マンデート減算→クランプ）の**外**で clamp 込みに足す＝既存の加算順を乱さない。
                // ★Explain への記帳は必須＝ExplainTests が「Legitimacy の内訳合計 == w.Legitimacy」を全断面で検査する硬性ゲート。
                if (coef.MegaprojectPrestige > 0)
                {
                    double legBefore = hw.Legitimacy;
                    hw.Legitimacy = PoliticalQuantities.Clamp01(hw.Legitimacy + coef.MegaprojectPrestige);
                    hw.Explain?.Add("Legitimacy", $"{mp.Name}の完成(国家事業の威信)", hw.Legitimacy - legBefore);
                }
                dip.Log.Add($"{hw.Year} {mp.Name}が完成（{mp.Owner?.Name ?? "中立"}）");
                continue;
            }
            if (hw.TechLevel < mp.TechGate) continue;   // 技術ゲート＝時代を出す（暦年で発火させない＝台本禁止）
            if (!mp.Started) continue;                 // 着工の動詞（2026-08-02）＝未着工は国庫を引かない
            // 国庫から予約を積む。★有界化は「1国あたりの**総額**」で効かせる＝同じ国が複数を同時に建てると
            // 1件ずつ Frac×Treasury を取り、国庫が一気に吸われる（征服で建設地が1国に集まると現実に起きる）。
            // 1件あたり Frac／全体で Cap を超えない、の小さい方。件数はリスト順に数えるので決定論。
            int inProgressHere = 0;
            foreach (var m2 in dip.Megaprojects)
                if (m2.Started && !m2.IsComplete && m2.Owner is { } o2 && ReferenceEquals(o2, hw.SelfPower) && hw.TechLevel >= m2.TechGate) inProgressHere++;
            double share = inProgressHere > 0
                ? Math.Min(coef.MegaprojectStateFrac.Value, coef.MegaprojectStateCap / inProgressHere)
                : coef.MegaprojectStateFrac.Value;
            // 段7 反景気循環（2026-07-28）：恐慌**かつ起債可能**なら拠出を増やす＝遊休労働を吸う。
            // 好況では建造物が民間から労働を奪い（段1の律速経由でむしろ建設を止める側に働く）、不況では遊休を吸う＝
            // 同じ機構が景気局面で逆の意味を持つ。史実のニューディールは赤字財政で成立したので、条件は「財政に余裕」ではなく「まだ借りられる」。
            double cyc = 1.0; if (Instruments.On && coef.MegaprojectCounterCyclical > 0 && hw.GdpDrawdown.Value > coef.MegaprojectCrisisDrawdown.Value) Instruments.MegaCrisis(hw, Fiscal.BorrowGateOpen(hw, coef));   // 計器（読むだけ・行数を変えない＝経路名の鍵 route_names.py が Megaprojects.cs の行番号で引く）：恐慌の拠出の判定に来た回数と、うち「まだ借りられる」が真だった回数
            if (coef.MegaprojectCounterCyclical > 0
                && hw.GdpDrawdown.Value > coef.MegaprojectCrisisDrawdown.Value
                && Fiscal.BorrowGateOpen(hw, coef))   // 「まだ借りられる」＝Fiscal.Borrow の門と同じ1本（利払い÷歳入が限界の下・冷却中でない・助走でない。LastInterestPerMonth・LastRevenue は直近の月次決算の量＝翌月初の借入の門が読む量）。旧は助走の条件の無い手書きの2条件＝同じ式（(281) 恒等）：呼び手の MultiNationSim は `!_calendarFrozen` の中でだけ呼び、WarmupFrozen は _calendarFrozen と同じ区間（WarmupEconomy）でだけ立つ＝助走の条件はここへ来る時に常に偽
                cyc += coef.MegaprojectCounterCyclical;
            double claim = share * cyc * hw.Treasury.Value;
            if (claim <= 1e-9 || hw.Treasury.Value <= 0) continue;
            // 段5 外資（2026-07-28）：**単一パートナー**を決定論のargmaxで1度だけ選び latch する。毎期再選定すると
            // 貯蓄の揺らぎで受取国が点滅し、配当先が振動して604四半期で軌道が発散する。
            if (coef.MegaprojectForeignShare.Value > 0 && mp.ForeignPartnerId < 0 && mp.Owner is { } own)
                ChooseForeignPartner(dip, nations, mp, own, coef);
            // 外資が入っていれば、その持分ぶん**追加で**拠出される＝建設が速くなる（自前は遅いが果実は全部自国＝トレードオフ）。
            // ★段3-3（C12・2026-09-30）：出し手は外資国の**投資家**（CapitalMarket.Investors＝相手を選ぶ物差しと同じ集合・貯蓄の比・貯蓄まで）→保有国の国庫
            //   （国が建てる事業へ外国の資本家が出資する＝スエズ会社の株主・鉄道の外債）。旧＝外資国の国庫が払った（相手は投資家の貯蓄で選ぶのに国庫が払い国庫が受ける）。
            //   額は外資国の通貨・保有国は為替で換算して受け取る（移し方の1本）。元本 ForeignPrincipal は**金建て**（利回りの請求と相手選びの控除を1つの物差しで）。
            //   外資国の国家の支払い率 r は掛けない（民間の出資＝国家の支払いではない）。
            if (mp.ForeignPartnerId >= 0 && mp.ForeignShare > 0 && mp.ForeignShare < 0.95)
            {
                double want = claim * mp.ForeignShare / (1.0 - mp.ForeignShare);   // 保有国の通貨
                foreach (var sim2 in nations)
                {
                    var fw = sim2.World;
                    if (fw.SelfPower is not { } fp || fp.Id != mp.ForeignPartnerId) continue;
                    // 1件が1四半期に引ける額を投資家の貯蓄の一定比で頭打ち＝1国が全世界の事業を同時に賄えない（資本の希少性）。
                    var investors = CapitalMarket.Investors(fw);
                    double cap = 0; foreach (var ip in investors) cap += Math.Max(0, ip.Savings.Value);
                    double ask = Math.Min(want * hw.FxRate / fw.FxRate, coef.MegaprojectForeignDrawFrac.Value * cap);   // 外資国の通貨
                    if (!(ask > 1e-9)) break;
                    double give = World.Transfer((fw, investors), (hw, null, null), ask, TreasuryCat.OtherIn);   // 外資国＝対外流出・保有国＝対外流入（保存・為替で換算）
                    if (!(give > 0)) break;
                    mp.ForeignPrincipal += give * fw.FxRate;   // 金建て
                    claim += give * fw.FxRate / hw.FxRate;       // 保有国が受け取った額（移し方の1本と同じ式）
                    break;
                }
            }
            hostRegion.MonumentBuildQueue += claim;   // 実支払いは EconomyStep の StateSpent（買えた分だけ）＝国庫→生産者で保存
            mp.PendingClaim = claim;                  // 誰がいくら払ったか（翌四半期の束の按分に使う・2026-08-02）
        }
    }

    /// <summary>外資パートナーの決定論的な選定（1度だけ・latch）。規則＝「資本余剰が最も大きい列強」。
    /// ★浮動小数の argmax は同値近傍で分岐が反転するので、比較は必ず (投資家貯蓄プール 降順) → (海路距離 昇順) → (Power.Id 昇順) の**3段全順序**。
    /// ★史実の当事国を再現することは目的にしない（スエズで仏でなく英が選ばれうる）＝決定論・単一・可読の3条件を優先する。</summary>
    private static void ChooseForeignPartner(Diplomacy dip, IReadOnlyList<Simulation> nations, Megaproject mp, Power owner, Coefficients coef)
    {
        Power? best = null; double bestPool = 0, bestKm = 0;
        foreach (var sim in nations)
        {
            var w = sim.World;
            if (w.SelfPower is not { } p || ReferenceEquals(p, owner)) continue;
            if (coef.MegaprojectForeignMinRank > 0 && !dip.IsGreatPower(p)) continue;   // 資本輸出国＝列強
            if (dip.IsAtWar(p, owner)) continue;                                        // 交戦国の事業には投じない（資本の論理）
            double pool = CapitalMarket.InvestorSavings(w) * w.FxRate;   // ★段3-3（C12・C13・2026-09-30）：投資家の述語の1本・金建て（旧＝各州の資本家1人の貯蓄を名目で）
            // ★資本の希少性（2026-07-28）：**すでに他の建造物へ拠出した元本を余剰から差し引く**。完成済みも含めて差し引く＝
            // 元本は外国資産に固定され利回りを生み続けており、もう自由に使えないため（会計として正しい）。
            // ⚠当初は !IsComplete（進行中のみ）で控除したが**効かなかった**：建造物どうしは時期的にほとんど重ならず
            // （ガンジス1868-69・アスワン1885-86・八幡1891-92…）、選定時点で進行中の他事業が無く控除が常に0だったため。
            // ★なお**一極集中それ自体は史実的に正しい**（戦前の対外投資は英一国が約4割）。ここで直すべきは「1国が支配すること」ではなく、
            // その1国が誰になるかで、それは列強の顔ぶれ一致13%という別問題（#24）の現れ。本控除は回転を狙うものではない。
            foreach (var m2 in dip.Megaprojects)
                if (m2.ForeignPartnerId == p.Id) pool -= m2.ForeignPrincipal;
            if (pool <= 1e-6) continue;
            double km = Geo.SeaDistanceKm(w.Latitude, w.Longitude, owner.Nation?.Latitude ?? 0, owner.Nation?.Longitude ?? 0,
                                          dip.ChokeOpenFor(Diplomacy.SuezId, p), dip.ChokeOpenFor(Diplomacy.PanamaId, p));
            bool better = best is null
                || pool > bestPool
                || (pool == bestPool && (km < bestKm || (km == bestKm && p.Id < best.Id)));
            if (better) { best = p; bestPool = pool; bestKm = km; }
        }
        if (best is null) return;
        mp.ForeignPartnerId = best.Id;
        mp.ForeignShare = coef.MegaprojectForeignShare.Value;
        dip.Log.Add($"{owner.Nation?.Year ?? 0} {mp.Name}に{best.Name}の資本が入った（持分{mp.ForeignShare:P0}）");
    }

    /// <summary>完成後の恒久的な対外流出＝外資が拠出した元本への利回り。保有国の国庫→外資国の**投資家**（★段3-3・C12・2026-09-30＝出資した投資家へ・旧＝外資国の国庫）。
    /// これが「速いが果実の一部が恒久的に外へ流れる」の実体で、財政難のときに払えず**外国の介入口**になる。
    /// 請求＝利回り×元本（金建て）を保有国の通貨へ（財政の請求の見込み Simulation と同じ式）×保有国の r。★払えない分は元本へ繰り入れる（作者裁定 Q6＝外資の利回りは ForeignPrincipal へ）。
    /// 外資国が消えていれば払わず繰り入れもしない（受け手がいない）。</summary>
    internal static void PayForeignYield(Diplomacy dip, IReadOnlyList<Simulation> nations, Megaproject mp, World hw, Coefficients coef)
    {
        if (mp.ForeignPartnerId < 0 || mp.ForeignPrincipal <= 1e-9 || coef.MegaprojectForeignYield <= 0) return;
        foreach (var sim in nations)
        {
            var fw = sim.World;
            if (fw.SelfPower is not { } fp || fp.Id != mp.ForeignPartnerId) continue;
            double due = coef.MegaprojectForeignYield * mp.ForeignPrincipal / hw.FxRate;   // 保有国の通貨
            double pay = due * hw.FiscalFundingRatio.Value;   // 保有国の r の分だけ（2026-09-28 段2-1＝旧は国庫の残りで切る門）
            if (pay > 1e-9) CapitalMarket.GiveGold(fw, pay, (hw, null), TreasuryCat.Megaproject);   // 保有国＝対外流出・外資国の投資家＝対外流入（為替で換算）
            else pay = 0;
            double unpaid = due - pay;
            if (unpaid > 1e-12) mp.ForeignPrincipal += unpaid * hw.FxRate;   // 未払いは元本へ（金建て）
            return;
        }
    }

    /// <summary>完成した建造物の恒久効果を host 州へ適用（同一Idは1度だけ）。効かせ先は**実在する州の状態**に限る。
    /// 加算型を選ぶ理由＝係数0で厳密恒等になるため（乗算型 *= は0で恒等にならず休眠規律に反する）。</summary>
    private static void ApplyEffect(Megaproject mp, Region r, Coefficients c)
    {
        if (r.MonumentApplied.Contains(mp.Id)) return;
        double add = mp.EffectMode == "scale" ? mp.EffectValue * TargetOf(mp.EffectTarget, r) : mp.EffectValue;
        if (!(add > 0)) { r.MonumentApplied.Add(mp.Id); return; }   // 否定形＝NaN の効果量を州へ足さない
        switch (mp.EffectTarget)
        {
            case "ArableLand": r.ArableLand += add; break;
            case "GridCapacity": r.GridCapacity += add; break;
            case "Industrialization":
                // ★州上限（IndustryInvestRegionCap）は投資の**入口**（World.ApplyModernizationInvestment 国庫投資・EconomyStep の StateBuildQueue ヘッドルーム。手動建設 BuildIndustry は2026-09-26に削除）
                // でしか効いておらず、状態への直接加算は素通りする（設計書§6 地雷#4）。上限は「プレイヤー/AI共通のルール」と宣言されているので建造物にも適用する。
                double indAdd = Math.Min(add, Math.Max(0, c.IndustryInvestRegionCap - r.TotalIndustrialization));
                if (c.RecipeEquipment > 0) FactoryEquipment.Distribute(r, c, indAdd, r.TotalIndustrialization, null);   // ★(495) 同じ点を適格な個体の設備にも同じ割合で足す（在庫に足す前の値で φ を出す）
                r.Industrialization += indAdd;
                break;
            default: break;   // 未知の効かせ先＝不発（データが無い/違う＝不発の原則）
        }
        r.MonumentApplied.Add(mp.Id);
    }

    private static double TargetOf(string target, Region r) => target switch
    {
        "ArableLand" => r.ArableLand,
        "GridCapacity" => r.GridCapacity,
        "Industrialization" => r.Industrialization,
        _ => 0,
    };
}
