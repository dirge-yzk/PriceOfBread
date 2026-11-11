// GameViewModel.Refresh.cs ＝ 毎月の全体更新 Refresh()＝全タブの表示値をまとめて作り直す唯一の入口。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 国選択モーダル（SelectNation/CloseDetailModal/RefreshSelectedInfo/MapPolJa）＝モーダル廃止後の残骸を削除（監査2026-07-12 死35：地図クリックはEllipse判定で捨てられ到達不能・XAML参照ゼロ）。州クリックに機能を持たせる案は別件。

    private void Refresh(bool includeMap = true)
    {
        UpdateDate();
        RefreshDaily();   // 国庫の1行は日次と同じ1本（2026-09-11 #19d）＝物価/為替/詳細パネルの価格も同時に最新化
        TreasuryHover = StatG("", "国庫", _histTreasury, Fmt.Money);   // 国庫の年次推移（ヘッダー国庫のホバー）
        // 技術水準の年次推移（_histTech）はセーブの系列として残す（財政と貨幣のページの技術水準の欄は 2026-11-11 作者指示で削除）。
        // ⚠ここで同じものを二度作らない（同じ量を2つの名前で呼ばない）。採取は SampleYearStats の _histTech.Add の1本だけ。
        RefreshTrade();   // 貿易タブ（2026-07-24）：開いている時のみ集計（閉＝即return）
        // 金融（物価・為替・金利・損失の負債化・家計債務・兌換カバー率）は財政と貨幣のページの金融の表（本体の目録 FiscalTables.Finance）が読む＝ここでは作らない。

        if (includeMap) RefreshMap();                  // 地図中心UI：全国の州○を所有国色で再配置（重い＝四半期のみ。月次はスキップ）

        // マップ：国カード（国力順・自国/緊張/戦争・政体色）
        // ★並びはランクが第一キー（作者指摘2026-08-11「ランクと順位が一致しない」）。
        //   原因＝一覧は**今の国力**で並べ、ランクは年1回の再分類（RankUpdateIntervalQuarters=4）の結果なので、
        //   その間に国力が入れ替わると「中堅なのに列強より上」が起こる。ランクで束ねてから国力で並べれば定義上ずれない。
        var scoreByPower = NationalScore.ScoresByPower(_sim.Diplomacy);   // ★(449) 国力は全国分を 1 回で（一覧の並び・カード・順位が同じ値を読む・国ごとに世界の総量を求め直さない）
        var linesByPower = NationalScore.BreakdownsByPower(_sim.Diplomacy);   // 同じ国の内訳（ホバーの詳細が読む）
        var powers = _sim.Diplomacy.Powers
            .OrderByDescending(p => (int)_sim.Diplomacy.RankOf(p))
            .ThenByDescending(p => p.Nation != null ? scoreByPower[p.Id] : -1).ToList();
        var me = _sim.Diplomacy.Powers[_playerIndex];
        var myRank = _sim.Diplomacy.RankOf(me);
        // 自国が交戦中の戦争と、その相手国の集合（カード表示・後段の戦争タブ集計の両方が読む＝二重走査をやめる）
        var myWars = new List<War>();
        var warEnemyIds = new HashSet<long>();
        foreach (var war0 in _sim.Diplomacy.Wars) if (war0.A == me || war0.B == me) { myWars.Add(war0); warEnemyIds.Add((war0.A == me ? war0.B : war0.A).Id); }
        // P4 統一戦争：狙える変体と必要州の保有国リスト（このrefresh内で1回だけ計算）
        var unifRule = _player.UnificationRule;
        var unifTargets = unifRule != null ? Formations.UnificationTargets(_sim.Diplomacy, me, unifRule) : null;
        UnifLine = unifRule != null ? $"統一の機運　目標 {unifRule.NewName}" : "";   // 外交タブ上部の状態表示（機運なし＝非表示）
        if (Layout.IsShown(Slot.DiplomacyWar)) BuildNationTable(powers, scoreByPower, linesByPower, me, warEnemyIds, unifTargets);   // 国の一覧の表（外交と戦争のページが見えている間だけ）
        double willCap = _player.Coef.ExpansionWillCap;
        ExpansionText = $"戦力余剰：{Fmt.Share(_player.ExpansionWill / willCap)}";   // ★2026-09-23作者指示「余剰戦力」→「戦力余剰」。好戦世論はホバー内訳の先頭へ統合（ヘッダーの類似数値の削減・2026-07-16）
        // ★政治資本（MandateSpent）のヘッダー表示は撤去（2026-08-15作者確認＝選挙梯子E5で廃止予定・表示部分を前倒し。内部は「紛争記憶」への改名待ち＝E5本体）
        OnPropertyChanged(nameof(ExpansionTip));   // 戦力余剰・好戦世論のホバー内訳を再計算
        // ★防衛同盟の枠数はヘッダーから撤去（2026-08-23作者指示）＝同盟の有無は外交タブの国カードのバッジ「同盟」で読む。

        // 概況（ヘッダーの満足度〜正統性・2026-11-03 に1段へ）。外した項目の控えは下の stats の注記。
        double corrRate = EffectiveCorruption();
        // 技術力＝世界前線比（自国技術÷各国最高＝0〜100%）。開始盤面は技術の助走（WarmupTech・2026-07-13）で前線t≈0.23まで育つ＝
        // 全国ゼロの「団子」を脱し、識字×世俗教育の差が前線比で見える（前線<0.05のみ「揺籃期」＝助走を通らない単一国/古いセーブの安全弁）。
        // 旧表示「技術力:{Fmt.Share(TechLevel)}」は0〜3の絶対値を%扱い（後期300%超）＋「遅れ」を別スケールの前線比で出す不整合＝初期に0%/98%と矛盾表示になっていた（2026-07-12修正）。
        // ★技術力の対最先端比はヘッダーから撤去（2026-08-15＝財政タブの技術効率へ移設）
        // ★軍事力（順位＋即応度）はヘッダーから撤去（2026-08-24作者指示）＝戦争タブの「理想の軍隊」の節見出しへ移設（ArmyIdealHeader）。
        // 地位：列強は順位付き「列強3位」・それ以外は「準列強」「中堅」「弱小」。★2026-09-23に1行目（国名の隣）から概況行へ移したので、
        // 下の stats が読む前にここで作る（旧はSyncRowsの後で作っており、そのまま stats に入れると1か月前の値が出る）。
        // 順位＝国力スコア（NationalScore）で自国より上の列強の数＋1（旧＝Powersリストの生成順インデックスは無関係で、
        // 仏が32番目に生成されると「列強32位」と誤表示していた不具合の修正・2026-07-12）。
        int myPos = 1;
        if (myRank == PowerRank.Great)
        {
            double myScore = scoreByPower[me.Id];
            foreach (var q in powers)
            {
                if (q == me || q.Nation is not { } qw || _sim.Diplomacy.RankOf(q) != PowerRank.Great) continue;
                double qs = scoreByPower[q.Id];
                if (qs > myScore || (qs == myScore && q.Id < me.Id)) myPos++;   // 同点は Id で全順序（ComputeRanks と同規約）
            }
        }
        PlayerName = _player.SelfPower?.Name ?? _player.Region.Name;
        PlayerRankText = myRank == PowerRank.Great ? $"列強{myPos}位" : myRank == PowerRank.Secondary ? "準列強" : myRank == PowerRank.Middle ? "中堅" : "弱小";
        OnPropertyChanged(nameof(PlayerScoreTip));   // 国名ホバーのスコア4分解を再計算
        // ★2026-11-03（作者）：ヘッダーを1段に＝国名(地位)・国庫(月の収支)・満足度・不満度・闘争度・正統性だけ。満足度〜正統性は深刻度の色。
        //   外した項目（GDP・GDP順位・人口・識字率・工業化・外資信用・行政力・国民意識・統一の機運・国債・戦力余剰）は再配置の待ち＝
        //   docs/202610/20261023/20261023_UIの作り直し_設計.md の「ヘッダーから外した項目（2026-11-03）」。系列（_hist*）の採取とセーブは据え置き。
        //   色の帯：闘争度・不満度＝暴動の閾値 RiotMilitancyThreshold 0.5 で赤・その半分で琥珀／満足度＝逆向き（50% 未満で琥珀・25% 未満で赤）／
        //   正統性＝同盟が付く下限 AllyJoinLegitimacyMin 0.4 未満で赤・0.6 未満で琥珀。
        double sat = NationalScore.MeanSatisfaction(_player), dis = Politics.AvgMassDiscontent(_player), mil = Politics.AvgMassMilitancy(_player);
        var stats = new List<HeaderStat>
        {
            Sev(StatG($"満足度：{Fmt.Share(sat)}", "満足度", _histSat, v => Fmt.Share(v), 76), SeverityBrushes.LowIsBad(sat, 0.5, 0.25)),
            Sev(StatG($"不満度：{Fmt.Share(dis)}", "不満度", _histDis, v => Fmt.Share(v), 76), SeverityBrushes.HighIsBad(dis, 0.25, 0.5)),
            Sev(StatG($"闘争度：{Fmt.Share(mil)}", "闘争度", _histMil, v => Fmt.Share(v), 76), SeverityBrushes.HighIsBad(mil, 0.25, 0.5)),
            Sev(StatG($"正統性：{Fmt.Share(_player.Legitimacy)}", "正統性", _histLeg, v => v.ToString("F3"), 76), SeverityBrushes.LowIsBad(_player.Legitimacy, 0.6, 0.4)),
        };
        SyncRows(HeaderStats, stats, h => h.Key, (t, s) => t.CopyFrom(s));   // 据え置き同期＝ヘッダー2行目が点滅しない（キーは GraphTitle でなく Key＝空にならない）

        // 戦争タブ（レビュー2026-07-19で改構）：犠牲者＝エンジンの戦争別台帳 War.DeadA/DeadB（兵士＋将校）と CivDeadA/CivDeadB（民間人）＝内戦・粛清の混入なし、
        // 戦費＝エンジンが実際に払った軍の月次の支出（_monthWarCost＝台帳の陸軍・海軍・戦費・GameViewModel.cs の OnTick が取る）を戦線数で均等按分した累計、
        // 開戦日＝war.Months から導出、勝敗＝講和ログを一次判定。
        var wrows = new List<WarRow>();
        var nowEnemies = new HashSet<long>();
        bool newEnemy = false;
        bool seeded = _warSeeded; _warSeeded = true;
        int monthStamp = (int)(_simDays / (double)Calendar.DaysPerMonth + 1e-9);
        // ★2026-09-23 戦費按分の分母を「その月に抱えていた戦線数」で固定する。旧＝継続中は myWars.Count、終戦した戦争は
        //   myWars.Count+1 を各自が使っていたので、**同じ月にN件が終戦すると分母が 0+1=1 になり、N件それぞれが
        //   その月の国全体の戦費を丸ごと受け取っていた**（思想⑨の二重計上）。終戦月も軍の支出は立つ＝継続分＋終戦分＝その月の全戦線。
        foreach (var w0 in myWars) nowEnemies.Add((w0.A == me ? w0.B : w0.A).Id);
        int endedThisRefresh = 0;
        foreach (var kv in _warTracks) if (!nowEnemies.Contains(kv.Key)) endedThisRefresh++;
        int frontsThisMonth = myWars.Count + endedThisRefresh;
        bool accrue = myWars.Count > 0 && monthStamp != _warCostStamp;   // 月が変わった最初のRefreshだけ按分（ボタン起因の再Refreshで二重加算しない）
        if (accrue) _warCostStamp = monthStamp;
        double costShareMonth = frontsThisMonth > 0 ? _monthWarCost / frontsThisMonth : 0;
        foreach (var war in myWars)
        {
            bool meIsA = war.A == me;
            var enemy = meIsA ? war.B : war.A;
            double sc = meIsA ? war.Score : -war.Score;
            if (!_warTracks.TryGetValue(enemy.Id, out var tr))
            {
                _warTracks[enemy.Id] = tr = new WarTrack { Won0 = me.WarsWon, Lost0 = me.WarsLost };
                if (seeded) newEnemy = true;   // 初回Refresh（新規開始/ロード直後）は既存の戦争で自動オープンしない
            }
            if (accrue) tr.CostShare += costShareMonth;
            tr.Civil = war.CivilWarParent is not null; tr.Secession = war.CivilWarSecession; tr.MeParent = war.CivilWarParent == me;
            tr.Demand = tr.Civil && war.CivilWarParent!.Nation?.Polity == war.CivilWarPolity;   // 同じ政体の要求戦
            tr.EnemyName = SecessionNames.WarName(enemy, _sim.Diplomacy.Wars);   // 政体をめぐる内戦の政府側は戦争の間だけ頭書き＋国名（反乱側は名そのものが頭書きつき）
            tr.LastScore = sc;
            tr.LastMonths = war.Months;
            tr.LastDeadSelf = meIsA ? war.DeadA : war.DeadB;
            tr.LastDeadEnemy = meIsA ? war.DeadB : war.DeadA;
            tr.LastCivDeadSelf = meIsA ? war.CivDeadA : war.CivDeadB;
            tr.LastCivDeadEnemy = meIsA ? war.CivDeadB : war.CivDeadA;
            wrows.Add(new WarRow
            {
                Key = enemy.Id.ToString(),
                Enemy = tr.EnemyName,
                Score = $"戦勝点 {Fmt.SignedPct(sc)}",   // ★2026-09-23：第3節（ゼロ節）を足す＝2節のままだと0へ丸まる負が `-+0%`（符号2つ）で出る（.NET 10 実測）
                ScoreBrush = sc > 0.05 ? TermPlus : sc < -0.05 ? TermMinus : TermNeutral,
                Info = $"{DateAt(_simDays - war.Months * (double)Calendar.DaysPerMonth)} 開戦　{war.Months}か月経過",
                Forces = $"自軍 {Troops(me)}　即応度 {Fmt.Share(_player.MilitaryReadiness)}　敵軍 {Troops(enemy)}",   // ★実兵数（2026-08-08）
                // ★2026-09-23：兵士の戦死と民間人の死を**別の行**で出す（民間は兵の17〜25倍＝1本に足すと兵の戦死が桁で消える）
                Losses = $"戦死 自国 {Fmt.PopFmt(tr.LastDeadSelf)}" + (enemy.Nation is not null ? $"　相手 {Fmt.PopFmt(tr.LastDeadEnemy)}" : "")
                       + $"\n民間人の死 自国 {Fmt.PopFmt(tr.LastCivDeadSelf)}" + (enemy.Nation is not null ? $"　相手 {Fmt.PopFmt(tr.LastCivDeadEnemy)}" : ""),
                Cost = $"戦費 {Fmt.Money(tr.CostShare)}",
                Exhaustion = $"厭戦 自国 {Fmt.Share(me.WarExhaustion)}　相手 {Fmt.Share(enemy.WarExhaustion)}",
                Terms = ExplainTerms("Exhaustion", v => Fmt.Share(v)),   // 自国の厭戦の内訳（説明API#63）
            });
        }
        SyncRows(WarRows, wrows, r => r.Key, (t, s) => t.CopyFrom(s));
        // 終わった戦争：追跡中だが Wars に無い＝講和済み。エンジンの終戦記録（EndedWars＝終戦月の最終値）を一次ソースに、
        // 無い場合のみ勝敗回数の増分でフォールバック（レビュー2026-07-19：ログ文字列照合は包含国名で誤爆するため廃止）。
        var curRegionNames = new HashSet<string>(_player.Regions.Select(r => r.Name));
        var curDependentIds = new HashSet<long>(_sim.Diplomacy.DependentsOf(me).Select(d => (long)d.Id));
        long curSuzerainId = _sim.Diplomacy.Suzerain(me)?.Id ?? -1;
        foreach (var (id, tr) in _warTracks.Where(kv => !nowEnemies.Contains(kv.Key)).ToList())
        {
            var ep = _sim.Diplomacy.Powers.FirstOrDefault(p => p.Id == id);
            EndedWarRecord? rec = null;
            for (int ri = _sim.Diplomacy.EndedWars.Count - 1; ri >= 0; ri--)
            {
                var r = _sim.Diplomacy.EndedWars[ri];
                if ((r.AId == me.Id && r.BId == id) || (r.BId == me.Id && r.AId == id)) { rec = r; break; }   // 同ペアの最新＝いま終わった戦争（終戦は単一経路）
            }
            double scEnd = rec is null ? tr.LastScore : rec.AId == me.Id ? rec.Score : -rec.Score;
            double deadSelf = rec is null ? tr.LastDeadSelf : rec.AId == me.Id ? rec.DeadA : rec.DeadB;
            double deadEnemy = rec is null ? tr.LastDeadEnemy : rec.AId == me.Id ? rec.DeadB : rec.DeadA;
            double civSelf = rec is null ? tr.LastCivDeadSelf : rec.AId == me.Id ? rec.CivDeadA : rec.CivDeadB;
            double civEnemy = rec is null ? tr.LastCivDeadEnemy : rec.AId == me.Id ? rec.CivDeadB : rec.CivDeadA;
            int months = rec?.Months ?? (tr.LastMonths + 1);
            // ★(456) 結果は終戦記録の判定（EndedWarRecord.VerdictOf＝講和 ResolvePeace と同じ 1 本）。記録が無いときだけ勝敗回数の増分で推す。語は PlayText.WarResultWord の 1 本（play の報告と同じ）
            bool? won = rec is not null ? rec.WonBy(me.Id)
                : me.WarsWon > tr.Won0 && me.WarsLost == tr.Lost0 ? true
                : me.WarsLost > tr.Lost0 && me.WarsWon == tr.Won0 ? false : null;
            string result = PlayText.WarResultWord(won);
            tr.CostShare += costShareMonth;   // 終戦月の戦費（軍の支出は終戦月も立つ＝取りこぼし補填）。分母は継続分＋終戦分＝その月の全戦線
            PastWarRows.Insert(0, new PastWarRow
            {
                Key = $"{id}@{(int)_simDays}",
                EndDay = _simDays,
                Title = $"{ep?.Name ?? (tr.EnemyName.Length > 0 ? tr.EnemyName : "—")}　{result}",
                Detail = $"{DateAt(_simDays - months * (double)Calendar.DaysPerMonth)}〜{EventDate()}　{months}か月"
                       + $"\n戦死 自国 {Fmt.PopFmt(deadSelf)}" + (ep?.Nation is not null ? $"　相手 {Fmt.PopFmt(deadEnemy)}" : "")
                       + $"\n民間人の死 自国 {Fmt.PopFmt(civSelf)}" + (ep?.Nation is not null ? $"　相手 {Fmt.PopFmt(civEnemy)}" : "")
                       + $"\n戦費 {Fmt.Money(tr.CostShare)}",
            });
            // #106 終戦ポップアップ：双方の戦死・戦費・得失を表示（同四半期に複数終戦なら最初の1件）
            if (!WarEndPopupOpen)
            {
                // 得失の州と属国化＝前回の Refresh の自国の州の名・属国の集合との差（SimCore に欄を足さない・割譲の記録は年表の文字列にしか無い）
                var gainedRegions = _prevOwnRegionNames is null ? new List<string>() : curRegionNames.Where(n => !_prevOwnRegionNames.Contains(n)).ToList();
                var lostRegions = _prevOwnRegionNames is null ? new List<string>() : _prevOwnRegionNames.Where(n => !curRegionNames.Contains(n)).ToList();
                bool madeVassal = _prevDependentIds is not null && curDependentIds.Contains(id) && !_prevDependentIds.Contains(id);
                bool becameVassal = _prevSuzerainId != id && curSuzerainId == id;
                WarEndPopupBody = WarEndPopupText(ep?.Name ?? (tr.EnemyName.Length > 0 ? tr.EnemyName : "—"), result, Fmt.SignedPct(scEnd), months,   // ★2026-09-23：第3節（ゼロ節）＝0へ丸まる負の `-+0%` を止める
                    Fmt.PopFmt(deadSelf), ep?.Nation is not null ? Fmt.PopFmt(deadEnemy) : null,
                    Fmt.PopFmt(civSelf), ep?.Nation is not null ? Fmt.PopFmt(civEnemy) : null,
                    Fmt.Money(tr.CostShare),
                    tr.Civil ? CivilWarOutcome(tr, won) : null, won, gainedRegions, lostRegions, madeVassal, becameVassal);
                WarEndPopupOpen = true;
            }
            _warTracks.Remove(id);
        }
        _prevOwnRegionNames = curRegionNames; _prevDependentIds = curDependentIds; _prevSuzerainId = curSuzerainId;
        for (int pi2 = PastWarRows.Count - 1; pi2 >= 0; pi2--)
            if (_simDays - PastWarRows[pi2].EndDay > Calendar.DaysPerYear * PastWarKeepYears) PastWarRows.RemoveAt(pi2);
        PlayerAtWar = WarRows.Count > 0;
        if (WarRows.Count == 0 && _player.Mobilized) { _player.Mobilized = false; OnPropertyChanged(nameof(PlayerMobilized)); }   // #106：講和で自動的に動員解除（総動員は戦時のみ）
        NoActiveWars = WarRows.Count == 0;
        HasPastWars = PastWarRows.Count > 0;
        // ★他国の戦争への調停（2026-09-06）：自国が当事国でない戦争を列挙し、AIと同じ述語で可否と理由（データのみ）を出す。
        {
            var mrows = new List<MediationRow>();
            var cM = _player.Coef;
            foreach (var war in _sim.Diplomacy.Wars)
            {
                if (war.A == me || war.B == me) continue;
                if (war.A.Nation is null && war.B.Nation is null) continue;
                bool ok = _sim.Diplomacy.CanOfferPeace(me, war, cM, out var why);
                double sc = war.Score;
                // 政体をめぐる内戦の政府側は戦争の間だけ頭書き＋国名（戦争タブの自国の戦争と同じ表＝SimCore の1か所）
                string aName = SecessionNames.WarName(war.A, _sim.Diplomacy.Wars), bName = SecessionNames.WarName(war.B, _sim.Diplomacy.Wars);
                string lead = System.Math.Abs(sc) < 0.05 ? "拮抗" : (sc > 0 ? $"{aName}が優勢 {Fmt.Share(sc)}" : $"{bName}が優勢 {Fmt.Share(-sc)}");
                mrows.Add(new MediationRow
                {
                    Key = $"{war.A.Id}|{war.B.Id}",
                    Title = $"{aName} ⚔ {bName}",
                    Info = $"{war.Months}か月経過　{lead}" + (ok ? $"　戦力余剰 {WillPct(_sim.Diplomacy.InterventionWillCost(me, war, cM))}" : ""),   // 押せない理由は釦の右（Reason）へ（B48）   // ★2026-09-23：語を「戦力余剰」へ統一
                    CanMediate = ok, Reason = Fmt.Zen(why),
                });
            }
            SyncRows(MediationRows, mrows, r => r.Key, (t, s2) => t.CopyFrom(s2));
            NoMediation = MediationRows.Count == 0;
        }
        WarTabVisible = true;   // 戦争タブは常時表示（国内勢力を統合＝戦争がなくても勢力の閲覧/鎮圧に使う・2026-07-20）
        if (newEnemy && !Layout.IsShown(Slot.DiplomacyWar)) Layout.Open(Slot.DiplomacyWar);   // 開戦＝外交と戦争のページを選んでいる枠へ自動で開く（生産を見ている枠は邪魔しない）

        // 外交ログ（開戦・講和・同盟・植民地化・独立…）を事件ログへ（新規分のみ）。
        // 日付は yyyy/mm/dd（事件の発生日）。発生地＝本文に最初に現れる国の首都＝遠い事件ほど遅れて届く（QueueNews と同じ距離ラグ）。
        var dlog = _sim.Diplomacy.Log;
        foreach (string tidied in Fmt.TidyDiploLog(dlog, _diploLogSeen))
        {
            string body = tidied;
            bool warLine = body.Contains("開戦") || body.Contains("宣戦");
            string icon = warLine ? "⚔" : body.Contains("同盟") ? "🤝" : body.Contains("講和") ? "🕊" : body.Contains("侵略") || body.Contains("属国") ? "🏴" : body.Contains("成立を宣言") || body.Contains("から独立") ? "👑" : body.Contains("座礁") ? "🏭" : body.Contains("職人が没落") ? "🔨" : "🌐";   // 座礁/職人の没落（通知・2026-09-05）
            // 発生地＝本文で最初に名前が出る国。**自国が当事者（本文に自国名）なら距離ラグなしで即時**＝宣戦布告される・属国化される等の重大事を見逃さない。
            bool mentionsMe = body.Contains(Me.Name, StringComparison.Ordinal);
            Power? src = null; int best = int.MaxValue;
            foreach (var p in _sim.Diplomacy.Powers)
            {
                int ix = body.IndexOf(p.Name, StringComparison.Ordinal);
                if (ix < 0 && p.BaseName != p.Name) ix = body.IndexOf(p.BaseName, StringComparison.Ordinal);   // 変名直前に書かれたログ行は旧名のまま＝旧名でも解決
                if (ix >= 0 && ix < best) { best = ix; src = p; }
            }
            double sLat = _player.Latitude, sLon = _player.Longitude; bool isMine = mentionsMe;   // 自国が当事者＝本文に自国名がある時だけ（国名の出ない行〔勢力の合流など〕を自国の出来事にしない・V14）
            if (src is not null && src != Me && !mentionsMe)
            {
                isMine = false;
                if (src.Nation is { } sw) { sLat = sw.Latitude; sLon = sw.Longitude; }
                else if (src.ColonyList.Count > 0) { sLat = src.ColonyList[0].Latitude; sLon = src.ColonyList[0].Longitude; }
            }
            var lineBrush = warLine && mentionsMe ? NewsRed : NewsBlue;   // 自国が絡む開戦＝赤で強調
            QueueNewsText(icon, body, lineBrush, sLat, sLon, isMine);   // 日付は配達時に付与（届いた日＋遅延か月）
        }
        _diploLogSeen = dlog.Count;

        // 教育費（義務教育の国庫負担）・専売益・外資フローの実績＝州の累積カウンタの四半期差分→月平均（市場依存で毎tick予測が難しい項目）。
        // 監査2026-07-11（他7）：Refreshは月次でも呼ばれる＝毎回/3で割ると教育費・専売が実際の約1/3表示・外資は3か月のうち2か月£0点滅だった→
        // 四半期境界を跨いだ時だけ差分を取り月平均を更新（境界間は前回値を保持）。
        int qStamp = _player.Year * Calendar.QuartersPerYear + _player.Quarter;
        if (qStamp != _prevQuarterStamp)
        {
            double eduCum = 0, stateMonopCum = 0;
            foreach (var rg0 in _player.Regions) { eduCum += rg0.CumEducationCost; stateMonopCum += rg0.CumStateMonopolyRevenue; }
            double fNet = (_player.ForeignCapitalIn - _player.CapitalAbroad).Value;   // 外資の純フロー：正＝流入（収入側）・負＝流出（支出側）。片方は£0
            if (_prevQuarterStamp >= 0)
            {
                _eduMonthly = Calendar.PerQuarterToPerMonth(System.Math.Max(0, eduCum - _prevEduCum));
                _stateMonopMonthly = Calendar.PerQuarterToPerMonth(System.Math.Max(0, stateMonopCum - _prevStateMonopCum));
                double fFlow = Calendar.PerQuarterToPerMonth(fNet - _prevForeignNet);
                _fInMonthly = System.Math.Max(0, fFlow);
                _fOutMonthly = System.Math.Max(0, -fFlow);
            }
            _prevEduCum = eduCum; _prevStateMonopCum = stateMonopCum; _prevForeignNet = fNet;
            _prevQuarterStamp = qStamp;
        }

        UpdateForecast();   // 予算予想（支出/収入・実効%・予想額）＝毎tickも更新されるがRefresh時にも即時反映

        RefreshPopulation();   // 人口のページ（階級・州・宗教・文化・年齢の層・出生と死）。階級の集約と増減の矢印の差分は見えているかに依らず毎月採る（GameViewModel.Pop.cs）
        BuildParliament();    // 議会の実体（議席・得票・支持基盤・2026-08-04）

        // 利益団体（旧支配層/近代エリート/大衆の政治勢力＝富×参政権。POP由来＝政治の主役・革命の駆動）
        var (trad, modern, mass) = InterestGroups.Blocs(_player);
        double bsum = trad + modern + mass; if (bsum < 1e-9) bsum = 1;
        Blocs.Clear();
        // ★力の源泉ホバー（2026-08-04）：旧支配層の力＝流動資産＋土地（地代の資本化）。何が力を支えているかの分解（データのみ）
        double nobSav = 0, nobLand = 0;
        foreach (var pp in _player.AllPops())
            if (pp.Closed) { nobSav += System.Math.Max(0, pp.Savings.Value); nobLand += _player.Coef.LandedPowerCapitalization * System.Math.Max(0, pp.LandRentPerMonth.Value); }
        double nobTot = nobSav + nobLand;
        // ★括弧書きの内訳（貴族と聖職／ブルジョワと専門職／労働者と農民）は削除（2026-08-23作者指示）
        Blocs.Add(new BlocRow { Name = "旧支配層", Percent = Fmt.Share((trad / bsum)),
            Tip = nobTot > 1e-9 ? $"力の源泉　流動資産 {Fmt.Share(nobSav / nobTot)}/土地（地代の資本化） {Fmt.Share(nobLand / nobTot)}" : "" });
        Blocs.Add(new BlocRow { Name = "近代エリート", Percent = Fmt.Share((modern / bsum)) });
        Blocs.Add(new BlocRow { Name = "大衆", Percent = Fmt.Share((mass / bsum)) });

        // 政体別実績ストック（信認⇔失望∈[-1,+1]・4系統）＝固着⇔カオスのドライバ。正=緑/負=赤のバーで符号を色分け。
        RegimeRecords.Clear();
        // ★系統2の表示名を「強権」→「独裁制」へ（2026-08-23作者指摘）。実体は CrownParliament.RegimeFamily の系統2＝軍事独裁とブルジョワ独裁。
        //   撤去済みのヘッダー指標「強権」（MandateSpent）と同語だったため、そちらの残骸に見えていた。
        string[] regimeNames = { "君主制", "議会制", "独裁制", "急進" };
        for (int ri = 0; ri < 4 && ri < _player.RegimeTrackRecord.Length; ri++)
        {
            double v = _player.RegimeTrackRecord[ri];
            RegimeRecords.Add(new RegimeRow
            {
                Name = regimeNames[ri],
                Value = $"{(v >= 0 ? "＋" : "−")}{Fmt.Share(Math.Abs(v))}",
                BarWidth = Math.Min(1.0, Math.Abs(v)) * 130,
                Brush = v >= 0 ? TermPlus : TermMinus,
            });
        }

        // 予算レバーの現在値は財政と貨幣のページの表が UpdateForecast で読む（通知は要らない）。
        RefreshLaws();   // 法律タブの段階ボタン行（E8）：現行段階・改正可否・政治資本コストを毎期更新
        UpdateLawAlert();     // 法律タブの通知＝改革の一歩が「通らない→通る」へ変わったら点灯
        OnPropertyChanged(nameof(PolityText));
        OnPropertyChanged(nameof(CrownText)); OnPropertyChanged(nameof(ParliamentControlText));
        OnPropertyChanged(nameof(CoopParliamentText));
        OnPropertyChanged(nameof(ArmyAllegianceText));   // 王権と議会の観測軸（政治タブ）。政治資本(MandateSpent)・世論の王権支持率はヘッダーへ移動
        OnPropertyChanged(nameof(PolityAttractorText)); OnPropertyChanged(nameof(PolityAttractorTip)); OnPropertyChanged(nameof(ChallengeText)); OnPropertyChanged(nameof(ChallengeTip)); OnPropertyChanged(nameof(ChallengeBrush));   // 反体制派の強さ比は戦争タブ（色は国内勢力行と同一規則）。分離リスクは撤去2026-08-16＝国内勢力の一覧が同じことを行ごとに出す
        OnPropertyChanged(nameof(PolityEconControlText)); OnPropertyChanged(nameof(PolityBondDiscountText));
        OnPropertyChanged(nameof(PolityMobilizationText)); OnPropertyChanged(nameof(PolityRollbackText));
        OnPropertyChanged(nameof(WarScaleText)); OnPropertyChanged(nameof(WarHomeLosingText));   // ★(195) 総動員の判断の材料（AI と同じ Diplomacy.WarScale）   // 政体パネル（2026-07-24）。総動員時の兵士割合
        OnPropertyChanged(nameof(ParliamentText));
        OnPropertyChanged(nameof(MobilizeButtonText));
        // 法律の現行値/支持団体の8本への通知は撤去（2026-08-10）＝2026-08-02の「議会立法」ブロック撤去で購読者が0件になっていた
        // （現行段は法律一覧の濃塗り・賛否は段ボタンのホバーが持つ）。行そのものの追従は上の RefreshLaws が担う。
        // 制度（法律タブ）：解禁は四半期判定＝状態行を毎期読み直す

        if (ProductionShown) RefreshProduction();   // 生産タブが開いていれば四半期ごとに統計を追従
        if (DiscoveryShown) RefreshDiscovery();   // 発見パネル（F）も同じ追従
        if (DiplomacyShown) RefreshFactions();   // 外交と戦争のページが見えていれば国内勢力を四半期ごとに追従（2026-07-20移設・国内勢力は戦争タブが正位置）
        // 巨大建造物は生産タブの左下（2026-08-10移設）＝上の RefreshProduction が BuildMegaRows を含む＝ここで別に組まない
        RefreshProblems();   // 左の列の問題の一覧（月ごと・向きの基準は四半期の番号で取り直す）
    }
}
