// GameViewModel.Header.cs ＝ 上部バーの指標（HeaderStat）＝年次時系列のサンプリング・折れ線・説明API内訳。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // === 年次時系列（ヘッダーのホバーグラフ）===========================================

    private double TotalProductionValue()
    {
        double s = 0;
        foreach (var rg in _player.Regions)
            if (rg.State == RegionState.Active) s += RegionProduction(rg);
        return s;
    }

    // 王権と議会（P0〜P2の観測軸・政治タブ「王権と議会」セクション 2026-07-11）
    private static string Pct(double v) => v.ToString("+0%;-0%;0%");   // −1〜+1を符号つきパーセントへ（王権と議会の協力度）
    // ★政体（2026-08-04）：「統治形態」が **王冠の軸だけ** を出していて、立憲君主制の英が「絶対王政」と表示され、
    //   すぐ下の「議会健全性 65%」と真っ向から矛盾していた。政体は王権×議会の2軸から導かれる合成値＝それを出す。
    //   王冠の形（CrownText）は別行に残す＝2軸のどちらが動いたのかが読める。
    // ★導出政体（2026-08-21・作者裁定）＝ヘッダーの政体も権力シェアの読み出し（忠実度・外交パネルと同じ物差し）。
    //   ⚠w.Polity（旧ラベル）は遷移機構の内部量として残るが、**表示はすべて導出**＝画面に2種類の政体が並ぶ事故を防ぐ。
    public string PolityText => PolityRegimeJa(PowerAffinity.PolityFromShares(_player));

    public string CrownText
    {
        get
        {
            string crown = _player.Crown switch { CrownForm.Absolutist => "絶対王政", CrownForm.Despotic => "専制", CrownForm.Shogunate => "幕府", _ => "君主なし" };
            string seize = _player.Seizure switch { SeizureForm.Bourgeois => "　ブルジョワの簒奪", SeizureForm.Proletarian => "　プロレタリアの簒奪", SeizureForm.Military => "　軍部の簒奪", SeizureForm.Anarchy => "　無政府", _ => "" };
            return crown + seize;
        }
    }
    // ★2026-08-18（作者「立憲君主か絶対王政かが判断できなくなってくる」）：議会の無い政体でも**数値を出す**。
    //   旧は立憲/共和以外で「議会なし」と伏せていたが、統制力は**床を越えてラベルを変える当の量**（CrownParliament.LabelFromAxes）
    //   ＝絶対王政で積み上がっている最中にこそ見たい数字を隠していた（1789三部会が画面に出ない）。
    //   ⚠「議会なし」を残すのは CoopParliamentText（協力度＝議会という相手が居ないと定義できない量）だけ＝そちらは伏せたまま。
    public string ParliamentControlText => _player.ParliamentControl < 0 ? "計測前"
        : $"{Math.Clamp(_player.ParliamentControl, 0, 1):P0}";
    public string CoopParliamentText => _player.ParliamentEnabled && _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic ? Pct(_player.CoopParliamentCrown) : "議会なし";
    // 「世論の王権支持率」の行は撤去（2026-08-09作者フィードバック）＝当時はヘッダーの王権支持度とそのホバーで同じ量が読めた。
    // ⚠2026-08-23にその王権支持度もヘッダーから撤去＝**天命・世論の王権支持率はUIのどこからも読めない**（エンジンは稼働中）。
    //   戻すならここ（政治タブ「王権と議会」節）が元の置き場所。写像（体制への協力度 −1〜+1 → 0〜100%）は Politics 側と同式。
    // 軍の帰属＝−1議会〜+1王権を5段階のラベルで（数字は出さない）。
    public string ArmyAllegianceText
    {
        get
        {
            double a = _player.ArmyAllegiance;
            return a > 0.6 ? "王権" : a > 0.15 ? "王権寄り" : a >= -0.15 ? "中立" : a >= -0.6 ? "議会寄り" : "議会";
        }
    }
    // 政治資本（MandateSpent）の表示は2026-08-15にヘッダーからも撤去（エンジンは稼働）。

    // 政体フレーバー レバー4（2026-07-24）：現政体で効いている量のデータ行（政治タブ「政体」節・説明文なし）。
    // ⚠★「与党」を名乗るのは**議会が実在して議席がある**ときだけ（2026-08-02・作者指摘「議会がなしの状態で与党がある」）。
    //   政体が共和制/立憲でも、参政権が「なし」なら有権者ゼロ＝議席ゼロで与党は存在しない。
    //   判定は ParliamentText と同一条件＝2箇所の表示が食い違わない。
    private bool HasSeatedParliament => _player.ParliamentEnabled
        && (_player.PartySeats[0] + _player.PartySeats[1] + _player.PartySeats[2]) > 0.005;
    // ★2026-09-19：経済軸（関税・福祉・累進）の綱領を撤去＝与党はこの3軸の目標を書かない（議席×党選好が純支持＝通るかに入るだけ）。
    //   旧「与党の綱領」「保守綱領の強制」は消えた機構を名乗っていた＝目標は政府（プレイヤー／AIの教義）が置き、議会は通すかを決める。
    public string PolityEconControlText => _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic && HasSeatedParliament
        ? "議会の承認"
        : "君主の直接統制";
    public string PolityBondDiscountText => _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
        ? $"-{_player.Coef.BondRateRepresentationDiscount:P0}" : "0%";
    // 「天命の有効度」の行は撤去（2026-08-09作者フィードバック）＝当時はヘッダーの王権支持度のホバーが同じ値を出していた。
    // ⚠2026-08-23の王権支持度の撤去で読み手ゼロ＝第5段5-3/5-4の母数（PoliticalQuantities.MandateBase）はエンジン専用に戻った。

    // 総動員の兵士割合（旧名「総動員の深さ上限」→「総動員時の兵士割合」・2026-08-16に分母つきの名前へ）。
    // ★2026-08-16 作者指摘「新たに集められる割合なのか、元から兵士だったPOPの割合なのか分からない」＝**どちらでもない**。
    //   実体＝Military.Step:191 の目標兵数 `conscriptFrac × lawFrac × 兵役の母数`＝総動員ONの戦時に、
    //   徴兵対象人口（農民＋都市労働者＋**現役の兵士**＝Military.cs:166 の manpower）のうち兵士になる割合（新規分でなく到達点）。
    //   よってラベルは分母を名乗る＝「総動員の兵士割合（徴兵対象人口比）」。
    // 内訳：levée en masse の深さ MobilizationConscriptFrac ×
    //   国民意識ゲート（低国民意識ほど動員が届かない・下限＝Military.MobilizationGateFloor／熟練度が緩める）× 徴兵法の倍率（皆兵＝UniversalConscriptionFactor）。
    // ⚠徴兵法の倍率（皆兵で2.5倍）は Military.cs:190-191 が最後に必ず掛けるのに表示だけが落としていた＝
    //   皆兵の国で実際の 2/5 の数字を出していた（同ファイルの原則「表示だけが別式になるのを避ける」の破れ）。同時に是正。
    public string PolityMobilizationText
    {
        get
        {
            var c = _player.Coef;
            double ncGate = c.NationBuildMobilizationWeight * (1.0 - Math.Max(0, _player.NationalConsciousness));
            ncGate *= Math.Max(0, 1.0 - c.SkillMobilizationRelief * Math.Clamp(_player.ArmySkill, 0, 1));
            double gate = c.NationBuildMobilizationWeight > 0 ? Math.Max(Military.MobilizationGateFloor, 1.0 - ncGate) : 1.0;
            double lawFrac = _player.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionFactor : 1.0;
            double manpower = 0, soldiers = 0;
            foreach (var r in _player.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double pool = 0, sld = 0;
                foreach (var p in r.Pops)
                {
                    if (p.IsSoldier) sld += p.Size;
                    else if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB) pool += p.Size;
                }
                soldiers += sld;
                manpower += (pool + sld) * Politics.RuleDelivery(r, c, c.RuleSharesTaxBase);   // 兵役の母数＝Military.Step と同一（州ごとの支配度 段3・RuleSharesTaxBase=0 で ×1）
            }
            if (!(soldiers > 0)) return "—";
            // Military.Step の目標兵数と同一式（母数＝農民＋都市労働者＋現役の兵士）。★M1（2026-09-19）：強制徴募は徴兵法≥選抜の国だけ（Military.cs:187 と同じ述語）
            //   ＝志願兵制の国では総動員しても徴募の目標が立たない＝下の下限1.0倍（表示だけが別式になるのを避ける）。
            double target = _player.ConscriptionLaw >= ConscriptionLaw.Selective ? c.MobilizationConscriptFrac * gate * lawFrac * manpower : 0;
            return $"{Math.Max(1.0, target / soldiers):F1}倍";   // 徴募は max(現状, 総動員目標)＝総動員で軍が減ることはない＝下限1.0倍
        }
    }
    public string PolityRollbackText => _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic ? "不可" : "可能";
    // 政体の支持と挑戦（革命の予兆・2026-07-11）：国民イデオロギー集計→各政体の支持（純関数の再計算＝表示のみ）と
    // 動員（改革派＋急進派＝Politicsが今期保存）対 実効保持力（治安維持力×人口スケール）。
    public string PolityAttractorText
    {
        get
        {
            if (!_player.PoliticsEnabled) return "—";
            var agg = Ideologies.National(_player);
            Polity best = _player.Polity; double bestS = double.NegativeInfinity;
            foreach (var pol in Ideologies.Polities)
            {
                double sup = Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(pol), _player.Coef.IdeologyAffinitySlope);
                if (sup > bestS) { bestS = sup; best = pol; }
            }
            // ★2026-08-09作者フィードバック：「現政体が最有力」という自明な言い回しをやめ、**素直に政体名を出す**。
            //   一致/不一致はホバー（PolityAttractorTip の「◀ 現政体」）で読める＝行は望まれている政体の名前だけを持つ。
            return PolityRegimeJa(best);
        }
    }
    // 政体支持ベクトル（革命の方向・2026-07-15）：各政体の支持度を降順で。現政体に印。PolityAttractorText のホバーに出す（捨てていた計算値を露出＝ほぼ追加計算なし）。
    public string PolityAttractorTip
    {
        get
        {
            if (!_player.PoliticsEnabled) return "";
            var agg = Ideologies.National(_player);
            var list = new List<(Polity Pol, double Sup)>();
            foreach (var pol in Ideologies.Polities)
                list.Add((pol, Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(pol), _player.Coef.IdeologyAffinitySlope)));
            var current = PowerAffinity.PolityFromShares(_player);
            return string.Join("\n", list.OrderByDescending(x => x.Sup)
                .Select(x => $"{PolityRegimeJa(x.Pol)}　{x.Sup:P0}{(x.Pol == current ? "　◀ 現政体" : "")}"));
        }
    }
    // 国民意識はヘッダーの年次グラフへ一本化＝政治タブの重複行と専用プロパティは撤去（2026-07-16）。
    // ⚠2026-09-23：その説明API内訳はヘッダーから外れ、政治タブへ移す（受け取りは ExplainTerms("NationalConsciousness") を呼ぶだけ）。
    // 反体制派の戦力比の内訳（実値＝エンジンの勢力値）：改革派＋急進派 対 治安維持力。ChallengeText のホバー。
    // 語は国内勢力の StrengthTip（勢力の強さ／治安維持力／義勇兵）に揃える＝同じ量を2つの名前で呼ばない。
    public string ChallengeTip
    {
        get
        {
            double reform = _player.ReformistPower, radical = _player.RadicalPower;
            double coerce = PoliticalQuantities.EffectiveCoerce(_player, _player.Coef);   // 第5段5-1：coerce 4実装の一元化（エンジンの判定と同一式）
            double sum = reform + radical + coerce;
            return sum <= 1e-9 ? "" : $"改革派 {reform:F1}\n急進派 {radical:F1}\n治安維持力 {coerce:F1}";
        }
    }
    // 兌換カバー率の内訳（比率＝基礎貨幣÷広義貨幣）。金融テキストのホバー。
    public string CoverTip => _player.BroadMoney <= 1e-9 ? "" : $"基礎貨幣 {Money(_player.BaseMoney)}\n広義貨幣 {Money(_player.BroadMoney)}\n維持線 {_player.Coef.GoldCoverMin:P0}";
    // 戦力余剰・好戦世論の内訳（World.WarFever の6項を再構成＋膨張意欲の今四半期増分）。ヘッダー左のホバー。
    public string ExpansionTip
    {
        get
        {
            var c = _player.Coef;
            var blocs = InterestGroups.Blocs(_player);
            double trad = blocs.Traditional;
            double soldiers = 0, total = 0;
            foreach (var p in _player.AllPops()) { total += p.Size; if (p.IsSoldier) soldiers += p.Size; }
            double soldierShare = total > 1e-9 ? soldiers / total : 0;
            double threat = _player.SelfPower is { } sp ? _sim.Diplomacy.MaxTension(sp) / 100.0 : 0;
            double mil = PoliticalQuantities.WarFeverPublicSphere(_player);   // ★2026-08-18＝好戦世論の公共圏（報道×識字×都市化×屈辱）。同：World.WarFever
            double outlet = Math.Clamp(trad + c.WarFeverMilitancyGate * mil, 0, 1);
            double diversion = c.WarFeverMilitancy * Politics.AvgMassMilitancy(_player) * outlet;
            double fever = _player.WarFever();
            double cap = Math.Max(1e-9, c.ExpansionWillCap);
            double gain = fever > c.WarFeverThreshold ? c.ExpansionWillGrowth * (fever - c.WarFeverThreshold) : 0;
            double bourg = c.ExpansionWillBourgeois > 0 ? c.ExpansionWillBourgeois * blocs.Modern : 0;
            string tip = $"好戦世論 {Math.Min(1.0, fever):P0}";
            if (mil > 1e-9) tip += $"\n+{c.WarFeverMilitarism * mil:P0}　好戦世論の公共圏";
            tip += $"\n+{c.WarFeverMilSpend * _player.MilitarySpending:P0}　軍事費"
                 + $"\n+{c.WarFeverNoble * trad:P0}　将校貴族"
                 + $"\n+{c.WarFeverSoldier * soldierShare:P0}　兵士規模"
                 + $"\n+{c.WarFeverThreat * threat:P0}　外的脅威"
                 + $"\n+{diversion:P0}　大衆の闘争の外征転化"
                 + $"\n\n戦力余剰の今四半期の増加"   // ★2026-09-23作者指示：語を「戦力余剰」へ
                 + $"\n+{gain / cap:P0}　戦争熱の超過分";
            if (bourg > 1e-9) tip += $"\n+{bourg / cap:P0}　商業ロビー";
            return tip;
        }
    }

    // 自国の国力スコア4分解（勝利条件の可視化・2026-07-15）：軍事/生産の豊かさ/金融/技術＋首位との差。国名/地位のホバーに出す（順位だけでは何を伸ばせば良いか読めない）。
    public string PlayerScoreTip
    {
        get
        {
            double mil = NationalScore.Military(_player), wea = NationalScore.Wealth(_player), fin = NationalScore.Finance(_player), tec = NationalScore.Tech(_player);
            double mine = mil + wea + fin + tec, top = 0; string topName = "";
            foreach (var q in _sim.Diplomacy.Powers)
                if (q.Nation is { } qw) { double s = NationalScore.Of(qw); if (s > top) { top = s; topName = qw.SelfPower?.Name ?? ""; } }
            string gap = top > mine + 1e-9 ? $"首位 {topName} との差 {top - mine:F1}" : "首位";
            return $"軍事 {mil:F1}\n生産の豊かさ {wea:F1}\n金融 {fin:F1}\n技術 {tec:F1}\n合計 {mine:F1}　{gap}";
        }
    }
    // 反体制派の戦力比＝動員（改革派＋急進派）÷鎮圧（治安維持力×人口）。100%超＝鎮圧を上回る＝政変圏
    //（Politics.Transition の `challenge <= coerce なら政体は動かない` と同じ不等式＝表示と点火が同じ物差し）。
    // ★2026-08-16 作者指摘「マイナスの軍事力は意味が通らない」の是正：旧＝(challenge−coerce)/(challenge+coerce) の
    //   符号つき正規化（−100%〜+100%）をやめ、すぐ下の国内勢力の「強さ比」（FactionRow の Strength 計算・RefreshFactions）と
    //   **同じ比・同じ分母ガード Max(1,·)・同じ100%の意味**へ一本化する＝1つの画面に2つの正規化を並べない。
    public string ChallengeText
    {
        get
        {
            double challenge = _player.ReformistPower + _player.RadicalPower;
            if (!(challenge > 0)) return "0%";   // ★否定形＝NaN も弾く（有限値では challenge <= 0 と厳密同値）
            double coerce = PoliticalQuantities.EffectiveCoerce(_player, _player.Coef);   // 第5段5-1：coerce 4実装の一元化（エンジンの判定と同一式）
            return (challenge / Math.Max(1.0, coerce)).ToString("P0");
        }
    }
    // 分離リスクの表示は撤去（★2026-08-16 作者裁定＝国内勢力の一覧が同じことを行ごとに出す）。プロパティ・通知とも削除済み。
    // 反体制派の戦力比の色＝反乱勢力行（FactionRow.StrengthBrush）と同一規則：100%超＝政変圏で警告色。
    public Avalonia.Media.IBrush ChallengeBrush
    {
        get
        {
            double challenge = _player.ReformistPower + _player.RadicalPower;
            double coerce = PoliticalQuantities.EffectiveCoerce(_player, _player.Coef);
            return challenge > Math.Max(1.0, coerce) ? FactionStrengthDanger : FactionStrengthCalm;
        }
    }
    // 移民の収支はヘッダーの人口 Stat へ統合（POPタブから移動・専用プロパティ撤去2026-07-12）。

    // 実効汚職率＝歳入からの漏れ率。★汚職V2点火（2026-08-15）：正準定義 World.EffectiveCorruption を読む
    // （旧＝(1−行政)(1−清廉度)のVM内複製＝点火後は誤表示だった。監察上限（span）は旧機構ごと不読＝表示も撤去）。
    private double EffectiveCorruption() =>
        _player.Coef.CorruptionMax * _player.EffectiveCorruption;

    private void SampleYearStats()
    {
        double popTotal = 0, litSum = 0;
        foreach (var p in _player.AllPops()) { popTotal += p.Size; litSum += p.Size * p.Literacy; }
        _histPop.Add(popTotal);
        _histGdp.Add(_player.Gdp);
        _histProd.Add(TotalProductionValue());
        _histTech.Add(_player.TechLevel);
        _histLit.Add(popTotal > 1e-9 ? litSum / popTotal : 0);
        _histSat.Add(NationalScore.MeanSatisfaction(_player));
        _histMil.Add(Politics.AvgMassMilitancy(_player));
        // ★2026-09-23（作者指示「識字率の右に満足度・不満度・闘争度」）：不満度＝エンジンの集計 Politics.AvgMassDiscontent
        //   （大衆POPの人口加重平均。闘争度 AvgMassMilitancy と**同じ母集団の述語**を共有＝UIに第二の式を置かない）。
        //   ⚠満足度だけ分母が「全POP」で、不満度・闘争度は「大衆のみ」＝母集団を揃えるのは測定回へ送る（2026-09-23の決め）。
        _histDis.Add(Politics.AvgMassDiscontent(_player));
        _histCorr.Add(EffectiveCorruption());
        _histInd.Add(_player.MeanIndustrialization);
        _histCredit.Add(_player.CreditRating);
        _histFin.Add(NationalScore.Finance(_player));
        _histFood.Add(_player.FoodFill);
        _histLeg.Add(_player.Legitimacy);
        _histCons.Add(Math.Max(0, _player.NationalConsciousness));   // 初期−1（未捕捉）はグラフを歪めないよう0でクランプ（他系列と同年数に揃える）
        _histMandate.Add(_player.MandateSpent);   // 上限廃止（2026-07-26）＝生値を採取
        _histTreasury.Add(_player.Treasury);
        // ジニ係数（Politics が全POPの1人あたり貯蓄から毎期算出した確定値）。
        // ★開始盤面の経済助走は政治を止めて回す（WarmupEconomy）＝1783年の起点では**まだ一度も測っていない**（0のまま）。
        //   未計測の0を起点に残すと折れ線が毎回そこまで落ちる。測れた年から始める（起点の年は系列長から逆算＝RangeText）。
        if (_histGini.Count > 0 || _player.GiniIndex > 0) _histGini.Add(_player.GiniIndex);
    }

    // 折れ線の点列（幅240×高さ64・min-max正規化）。1点＝水平線（開始直後からホバーが出る・2026-07-16）。
    // min/max は呼び出し側（SeriesView）が LINQ で求めて渡す＝点数が変わるまで再計算しない（式は従来と同一）。
    private static global::Avalonia.Points GraphPoints(List<double> series, double min, double max, double w = 240, double h = 64)
    {
        var pts = new global::Avalonia.Points();
        if (series.Count < 1) return pts;
        if (series.Count == 1) { pts.Add(new global::Avalonia.Point(0, h / 2)); pts.Add(new global::Avalonia.Point(w, h / 2)); return pts; }
        double span = max - min; if (span < 1e-12) span = 1;
        for (int i = 0; i < series.Count; i++)
            pts.Add(new global::Avalonia.Point(i * w / (series.Count - 1), h - 3 - (series[i] - min) / span * (h - 6)));
        return pts;
    }

    /// <summary>年次系列の点列・最小・最大（点数が変わった時だけ作り直す・2026-08-10）。
    /// 系列は Add 追記（年1回）とロード時の一括流し込みだけで縮まない＝点数が同じなら中身も同じ。
    /// 点列の**実体を使い回す**ので、HeaderStat.Points への代入も参照が同じ＝通知が飛ばず再描画も起きない。</summary>
    private (global::Avalonia.Points pts, double min, double max) SeriesView(List<double> series)
    {
        if (_seriesCache.TryGetValue(series, out var c) && c.N == series.Count) return (c.Pts, c.Min, c.Max);
        double min = series.Count > 0 ? series.Min() : 0, max = series.Count > 0 ? series.Max() : 0;
        c = (series.Count, GraphPoints(series, min, max), min, max);
        _seriesCache[series] = c;
        return (c.Pts, c.Min, c.Max);
    }

    // ★2026-09-23（作者指示「詳細表示は全てグラフのみ」）：グラフを持たない Stat の作り手（旧 StatT＝行政力）は撤去。
    //   グラフの無い項目はホバーそのものを持たない（HeaderStat.Plain）＝素の文字だけで出す。

    // 説明API（#63）：ある数値の内訳を StatTerm 列へ整形。正の寄与を上・負を下・|Δ|降順、|Δ|<5e-4 は非表示（Runnerのexactコマンドと同流儀）。緑＝加算/赤＝減算。
    private static readonly IBrush TermPlus = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush TermMinus = new SolidColorBrush(Color.Parse("#B23A2E"));
    private List<StatTerm> ExplainTerms(string quantity, Func<double, string> fmt)
    {
        var rows = new List<StatTerm>();
        if (_player.Explain is not { } led) return rows;
        var terms = led.Get(quantity);
        foreach (var t in terms.Where(t => Math.Abs(t.Delta) >= 5e-4).OrderByDescending(t => t.Delta))
            rows.Add(new StatTerm { Text = $"{(t.Delta >= 0 ? "＋" : "−")}{fmt(Math.Abs(t.Delta))}　{t.Reason}", Brush = t.Delta >= 0 ? TermPlus : TermMinus });
        return rows;
    }

    /// <summary>年次グラフの Stat。<paramref name="boxWidth"/>＝桁が動いても右の項目を押さないための箱の最小幅（画素）。
    /// <paramref name="hoverTitle"/>＝ホバー1行目（既定は title と同じ。GDP だけは「一人当たりGDPと世界順位」をここに載せる
    /// ＝作者指示2026-09-23「GDPの量なのでGDPのグラフのホバーに1行だけ残す」）。突き合わせキー（Key）は常に title＝ホバー題が
    /// 毎月変わっても SyncRows が行を作り直さない。</summary>
    private HeaderStat StatG(string text, string title, List<double> series, Func<double, string> fmt, double boxWidth = 0, string? hoverTitle = null)
    {
        bool has = series.Count >= 1;   // 1点から表示（開始直後からホバーが出る・2026-07-16）
        var (pts, min, max) = SeriesView(series);
        // 起点の年＝人口系列（毎年必ず1点採る基準系列）との長さの差から逆算（2026-08-10）。
        // 途中から採り始めた系列（不満度＝末尾に足した新系列／旧セーブで未復元だった系列）でも年ラベルがずれない。
        int startYear = _histStartYear + Math.Max(0, _histPop.Count - series.Count);
        return new HeaderStat
        {
            Text = text,
            HasGraph = has,
            GraphTitle = hoverTitle ?? title,
            Key = title,
            BoxWidth = boxWidth,
            Points = has ? pts : new(),
            RangeText = has ? $"{startYear}〜{startYear + series.Count - 1}年　最小:{fmt(min)}　最大:{fmt(max)}" : "",
        };
    }

    /// <summary>グラフを持たない Stat（ホバーなし・素の文字）。★2026-09-23「詳細表示は全てグラフのみ」で
    /// 行政力の内訳ホバーが財政タブへ移った＝この項目はヘッダーでは数字だけを名乗る。</summary>
    private static HeaderStat StatPlain(string text, string key, double boxWidth) => new() { Text = text, Key = key, BoxWidth = boxWidth };

    // 説明API内訳の共通整形（緑=加算/赤=減算・|Δ|<5e-4は呼び出し側で除外済み前提）。
    private static readonly IBrush TermNeutral = new SolidColorBrush(Color.Parse("#5A4E3C"));
    private static StatTerm Signed(double delta, string reason, Func<double, string> fmt)
        => new StatTerm { Text = $"{(delta >= 0 ? "＋" : "−")}{fmt(Math.Abs(delta))}　{reason}", Brush = delta >= 0 ? TermPlus : TermMinus };
    private static StatTerm Neutral(string text) => new StatTerm { Text = text, Brush = TermNeutral };

    /// <summary>行政力ホバー（作者指示2026-08-15）：説明API内訳の先頭に行政費（財政タブのレバー＝目標/実効）を添える
    /// ＝「行政費→行政財の購入→充足」が内訳の官僚制項に流れ込む関係を、レバーの現在値ごと1画面で読めるように。</summary>
    private List<StatTerm> AdminTerms()
    {
        var terms = ExplainTerms("AdminEfficiency", v => v.ToString("P0"));
        terms.Insert(0, Neutral($"行政費　目標 {_player.Policy.AdminSpendingTarget:P0}　実効 {_player.AdminSpending:P0}"));
        return terms;
    }

    // === VM側の内訳ビルダー（SimCore非改変・確定値を横で読み直して構築＝ビット恒等自明）===
    // ⚠★2026-09-23（作者指示「詳細表示は全てグラフのみ」）：この節の6本はヘッダーのホバーから外れ、**移送先のタブが呼ぶ**。
    //   移送先＝CreditTerms/AdminTerms→財政タブ・ExplainTerms("Legitimacy")/("NationalConsciousness")→政治タブ・
    //   PopulationTerms/LiteracyClassTerms/SatisfactionClassTerms/MilitancyClassTerms→POPタブ。
    //   式は**ここにあるものをそのまま呼ぶ**こと（タブ側に第二の式を書き写さない＝同じ量を2つの名前で呼ばない）。
    //   受け取り側が結線するまでの間この6本は呼ばれない＝結線が済むまでは死蔵（実装ログの申し送りに記載）。
    // 汚職＝要因の実値を中立表示（行政効率と清廉度が高いほど上限から削られる）。符号分解は誤読を招いたため撤去（2026-07-16）。
    /// <summary>対外信用の内訳（2026-08-13・作者指摘「信用が非常に大切というのがUIからわからない」）。
    /// 信用は4項の**積**なので、どれが足を引っ張っているかは積の各項を並べれば一目で分かる。
    /// 説明文は書かない＝35%という数字が原因を名指しする（既存の文言規律）。</summary>
    private List<StatTerm> CreditTerms() =>
    [
        Neutral($"国債残高　{_player.CreditDebtTerm:P0}"),
        Neutral($"兌換　{_player.CreditGoldTerm:P0}"),
        Neutral($"為替　{_player.CreditFxTerm:P0}"),
        Neutral($"国債の利回り　{_player.CreditBondTerm:P0}"),
    ];

    /// <summary>工業化の内訳＝州別の上位（2026-08-13）。スコア最大の重み(×9)なのに自国の全体値がUIのどこにも無かった。</summary>
    private List<StatTerm> IndustrializationTerms()
    {
        var rows = new List<StatTerm>();
        var list = new List<(string Name, double Ind)>();
        foreach (var r in _player.Regions) if (r.State == RegionState.Active) list.Add((r.Name, r.Industrialization));
        list.Sort((a, b) => b.Ind.CompareTo(a.Ind));
        for (int i = 0; i < list.Count && i < 8; i++) rows.Add(Neutral($"{list[i].Name}　{list[i].Ind:F2}"));
        return rows;
    }

    // 技術力のホバーは撤去（2026-07-16ユーザー指示＝かっこ・内訳とも不要）。
    // 識字率＝階級ごとの識字率そのもの（上流≈100%・下流が低い等）。人口比の寄与でなく各層の実際の値を出す（2026-07-16修正）。
    private List<StatTerm> LiteracyClassTerms()
    {
        var rows = new List<StatTerm>();
        double[] litMass = new double[3], sz = new double[3];
        foreach (var p in _player.AllPops())
        {
            int layer = (p.Closed || p.ClassName == CapitalistClass) ? 0 : (p.IsModernElite || p.ClassName is BureaucratClass or ArtisanClass or MerchantClass) ? 1 : 2;
            litMass[layer] += p.Size * p.Literacy; sz[layer] += p.Size;
        }
        string[] name = { "上流", "中流", "下流" };
        for (int i = 0; i < 3; i++) if (sz[i] > 1e-9) rows.Add(Neutral($"{name[i]}　{litMass[i] / sz[i]:P0}"));
        return rows;
    }

    // 闘争度＝階級ごとの平均闘争度そのもの（Pop.Militancy＝闘争度のストック）。
    // ⚠不満度は別の導出量（Politics.Discontent）＝2026-09-23にヘッダーの独立した項目になった（闘争度の内訳ではない・同じ数を2つの名前で呼ばない）。
    private List<StatTerm> MilitancyClassTerms()
    {
        var rows = new List<StatTerm>();
        double[] milMass = new double[3], sz = new double[3];
        foreach (var p in _player.AllPops())
        {
            int layer = (p.Closed || p.ClassName == CapitalistClass) ? 0 : (p.IsModernElite || p.ClassName is BureaucratClass or ArtisanClass or MerchantClass) ? 1 : 2;
            milMass[layer] += p.Size * p.Militancy; sz[layer] += p.Size;
        }
        string[] name = { "上流", "中流", "下流" };
        for (int i = 0; i < 3; i++) if (sz[i] > 1e-9) rows.Add(Neutral($"{name[i]}　{milMass[i] / sz[i]:P0}"));
        return rows;
    }
    // 満足度＝階級ごとの平均満足度そのもの（どの階級が不満か）。人口比の寄与でなく各層の実際の値を出す（2026-07-16修正）。
    private List<StatTerm> SatisfactionClassTerms()
    {
        var rows = new List<StatTerm>();
        double[] satMass = new double[3], sz = new double[3];
        foreach (var p in _player.AllPops())
        {
            int layer = (p.Closed || p.ClassName == CapitalistClass) ? 0 : (p.IsModernElite || p.ClassName is BureaucratClass or ArtisanClass or MerchantClass) ? 1 : 2;
            satMass[layer] += p.Size * p.Satisfaction; sz[layer] += p.Size;
        }
        string[] name = { "上流", "中流", "下流" };
        for (int i = 0; i < 3; i++) if (sz[i] > 1e-9) rows.Add(Neutral($"{name[i]}　{satMass[i] / sz[i]:P0}"));
        return rows;
    }
    // 人口＝保存則（TotalPopulation=初期+PopulationDelta）で3項＋今四半期の移民の流入/流出（要素ごと）。
    private List<StatTerm> PopulationTerms()
    {
        var rows = new List<StatTerm>();
        double war = _player.WarDeadCumulative;
        double baseP = _player.TotalPopulation - _player.PopulationDelta;
        double other = _player.PopulationDelta + war;   // 戦没を戻した自然増減・移民・領土の累積
        Func<double, string> pf = v => PopFmt(v);
        if (Math.Abs(baseP) >= 5e-4) rows.Add(Signed(baseP, "初期人口", pf));
        if (Math.Abs(other) >= 5e-4) rows.Add(Signed(other, "自然増減と移民と領土(累積)", pf));
        if (Math.Abs(war) >= 5e-4) rows.Add(Signed(-war, "戦没と粛清(累積)", pf));
        // 今四半期の移民（要素ごと）：流入＝国際（世界プール）＋越境（他国から）／流出＝越境（他国へ）。
        double extIn = _player.LastImmigrationExternal, crossIn = Math.Max(0, _player.LastImmigration - extIn), crossOut = _player.LastEmigration;
        if (extIn >= 5e-4 || crossIn >= 5e-4)
        {
            rows.Add(Neutral("流入(今四半期)"));
            if (extIn >= 5e-4) rows.Add(Signed(extIn, "国際移民(世界から)", pf));
            if (crossIn >= 5e-4) rows.Add(Signed(crossIn, "越境移民(他国から)", pf));
        }
        if (crossOut >= 5e-4)
        {
            rows.Add(Neutral("流出(今四半期)"));
            rows.Add(Signed(-crossOut, "越境移民(他国へ)", pf));
        }
        // 年齢構成（高齢化採用2026-07-18＝OldLaborFactor<1で老年が労働から退く）：人口加重平均の実値
        double childSum = 0, oldSum = 0, szSum = 0;
        foreach (var p in _player.AllPops()) { childSum += p.ChildShare * p.Size; oldSum += p.OldShare * p.Size; szSum += p.Size; }
        if (szSum > 1e-9) rows.Add(Neutral($"子供比 {childSum / szSum:P0}　老年比 {oldSum / szSum:P0}"));
        return rows;
    }
    // ★2026-09-23（作者指示「素材などの表示は削除」）：GDPホバーのティア内訳（主食/日用品/奢侈品/素材/生産財/役務）は撤去。
    //   読み手が0になった GdpTierTerms／TierValueRows／TierJa も同時に削除した（死蔵を残さない）。
}
