// GameViewModel.News.cs ＝ 出来事（通知）＝フィルタ・四半期のイベント検出・距離ラグ付きの配達。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 旧SatBars（満足度チャート）・旧Notifications（外交生ログ）＝XAML未バインドの死構築を削除（監査2026-07-12 死16。満足度はヘッダーの年次グラフ・外交はニュース配達が現役）
    public ObservableCollection<AlertRow> Alerts { get; } = new();   // 出来事（表示中＝フィルタ適用後）
    // 出来事のフィルタ（2026-07-19・2026-10-23 に 3 つへ）：全国（すべて）・列強（列強が関わる出来事）・自国（自国が当事者）。全件は _allAlerts に保持し、表示は Alerts へ流す。
    private readonly List<AlertRow> _allAlerts = new();
    private const int AlertsShown = 25, AlertsKept = 150;
    /// <summary>全件（絞りの前）。撮影が絞りごとの件数を数えるために読む。</summary>
    internal IReadOnlyList<AlertRow> AlertsForCapture => _allAlerts;
    /// <summary>年表の絞り。"world"＝全国・"great"＝列強・"mine"＝自国。</summary>
    [ObservableProperty] private string _newsFilter = "world";
    public bool NewsWorld => NewsFilter == "world";
    public bool NewsGreat => NewsFilter == "great";
    public bool NewsMine => NewsFilter == "mine";
    partial void OnNewsFilterChanged(string value) { OnPropertyChanged(nameof(NewsWorld)); OnPropertyChanged(nameof(NewsGreat)); OnPropertyChanged(nameof(NewsMine)); RebuildAlerts(); }
    [RelayCommand] private void SetNewsFilter(string? p) => NewsFilter = p is "great" or "mine" ? p : "world";
    /// <summary>出来事 1 行が今の絞りに合うか。</summary>
    private bool NewsPasses(bool mine, bool great) => NewsFilter switch { "mine" => mine, "great" => great, _ => true };
    /// <summary>列強が関わる出来事か。列強の判定は外交の既存の定義（<see cref="Diplomacy.IsGreatPower"/>＝国力の順位）をそのまま使う。
    /// 本文に名前が出る国（旧名も）に列強がいれば真・自国の出来事は自国が列強のときも真。</summary>
    private bool InvolvesGreatPower(string body, bool mine)
    {
        var dip = _sim.Diplomacy;
        if (mine && dip.IsGreatPower(dip.Powers[_playerIndex])) return true;
        foreach (var p in dip.Powers)
            if (dip.IsGreatPower(p) && (MentionsName(body, p.Name) || (p.BaseName != p.Name && MentionsName(body, p.BaseName)))) return true;
        return false;
    }

    /// <summary>本文に国名が出るか。国名の前後が漢字のとき（国名の端も漢字）は別の語の一部とみなす＝「清」が「粛清」「清算」に当たらない。</summary>
    public static bool MentionsName(string body, string name)
    {
        if (name.Length == 0) return false;
        static bool Han(char c) => c is >= '一' and <= '鿿';
        for (int at = body.IndexOf(name, StringComparison.Ordinal); at >= 0; at = body.IndexOf(name, at + 1, StringComparison.Ordinal))
        {
            int end = at + name.Length;
            bool headCut = at > 0 && Han(body[at - 1]) && Han(name[0]);
            bool tailCut = end < body.Length && Han(body[end]) && Han(name[^1]);
            if (!headCut && !tailCut) return true;
        }
        return false;
    }
    private void RebuildAlerts()
    {
        Alerts.Clear();
        foreach (var a in _allAlerts)
        {
            if (!NewsPasses(a.Mine, a.Great)) continue;
            Alerts.Add(a);
            if (Alerts.Count >= AlertsShown) break;
        }
    }

    // イベント検出（四半期ごと）：状態遷移の「瞬間」を「yyyy/MM/dd 国名で、〇〇が発生」で Alerts に積む。
    // 全国の政変・内戦（追加した政体機構＝革命/独裁化の通知）＋自国の恐慌・飢饉・緊縮・切下げ。解決法は出さない（何が起きたかだけ）。
    /// <summary>転機通知の基準値を現在の状態で取り直す（唯一定義）。月次の <see cref="DetectEvents"/> の末尾と、開始/ロード時の初期化が同じ1本を呼ぶ
    /// ＝基準の項目が片方だけ抜ける（旧＝初期化に治安と条約港が無く、治安&lt;50%で開始/ロードすると1回目に誤発火）ことがない（2026-09-11 #20）。</summary>
    private void SnapshotTurningPoints()
    {
        _pfSuf = _player.Suffrage; _pfEdu = _player.EducationLaw; _pfCen = _player.Censorship; _pfUni = _player.LaborUnion; _pfWh = _player.WorkHours;
        _pfTreatyPortInForce = _player.UnequalTreatyInForce;   // 条約港の失効通知（案A）＝真から偽への監視
        _pfChild = _player.ChildLaborLegal >= 0.5; _pfWomenRights = _player.WomenRights;
        _pfCorn = _player.CornLaws; _pfLand = _player.LandReform; _pfSerf = _player.SerfEmancipation; _pfPen = _player.Pension; _pfSS = _player.SocialSecurityLaw;
        _pfColSuf = _player.ColonialSuffrage; _pfMigSuf = _player.MigrantNativeSuffrage;   // 参政権の範囲2法（2026-08-16）
        _pfGold = _player.OnGoldStandard;
        _pfDefault = _player.HadSovereignDefault; _pfPatent = _player.PatentLaw; _pfJoint = _player.JointStock; _pfEpidemic = _player.EpidemicActiveYears > 0;
        _pfOrderLow = _player.PublicOrder < 0.5;   // 治安崩壊のエッジ（接15）
        _pfParty = _player.GoverningParty; _pfRank = _sim.Diplomacy.RankOf(_sim.Diplomacy.Powers[_playerIndex]);
    }

    private void DetectEvents()
    {
        SampleWorldPrices();   // 財パネルの世界価格スパークライン（40四半期・2026-07-24）：四半期ごとにサンプル
        var powers = _sim.Diplomacy.Powers;
        // 分離独立の建国（新国家の誕生・R8民族分離）：前四半期になかった実国のIdが現れたらワールドニュース。count変動に強いId基準（初回は積まず基準化のみ）。
        var curIds = new System.Collections.Generic.HashSet<long>();
        foreach (var p in powers) if (p.Nation is not null) curIds.Add(p.Id);
        if (_prevPowerIds != null)
            foreach (var p in powers)
                if (p.Nation is { } nw && !_prevPowerIds.Contains(p.Id))
                {
                    QueueNewsText("🏛", $"{p.Name}が独立して建国された", NewsGold, nw.Latitude, nw.Longitude, false); Annal("🏛", $"{p.Name}が独立して建国");
                    // ★内戦の陣営選択（P8・2026-09-06）：自国を親とする内戦タグの戦争の相手＝自国から立った反乱国家。既にモーダルが開いていれば今回は出さない（排他連鎖）。
                    if (_player.SelfPower is { } meP && !AnyPopupOpen)
                        foreach (var w2 in _sim.Diplomacy.Wars)
                            if (w2.CivilWarParent == meP && (w2.A == p || w2.B == p))
                            {
                                int idx = _sim.Nations.FindIndex(n => ReferenceEquals(n.World, nw));
                                if (idx >= 0)
                                {
                                    _sideChoiceIndex = idx;
                                    SideChoiceBody = $"{p.Name}が{meP.Name}に対して立った（{(w2.CivilWarSecession ? "分離独立" : "内戦")}）\n政府側に残るか、{p.Name}で続けるか";
                                    SideChoiceOpen = true;
                                }
                                break;
                            }
                }
        _prevPowerIds = curIds;

        // 初回のみ開始イベント（基準化だけ）。★前回値は国の Id をキーに持つ＝国数が変動（建国/消滅）した四半期も比べる（建国は上で既に告知済み・新しい国は前回値が無い＝比べない）。
        if (_prevPolities == null)
        {
            _prevPolities = new System.Collections.Generic.Dictionary<long, Polity>();
            _prevTerror = new System.Collections.Generic.Dictionary<long, double>();
            foreach (var p in powers)
                if (p.Nation is { } tw) { _prevPolities[p.Id] = tw.Polity; _prevTerror[p.Id] = tw.TerrorPurgeCumulative; }
            _prevAusterity = _player.AusterityIntensity > AusterityActive;
            _prevDeval = _player.CumulativeDevaluation;
            _prevDepression = SelfPlayMatch.InDepression(false, _player.GdpPcDrawdown.Value);   // ★E2：1人あたり（割譲で総額が落ちただけの国を恐慌と呼ばない）
            _prevHeatHot = _player.FinancialHeat > FinancialHeatHot;
            _prevFamine = _player.HarvestFactor < FamineHarvest;
            QueueNews("ev_start", PlayerNationName, 0, 0, true);
            return;
        }

        // ★内戦一元化（2026-08-31）：内戦の実体は War.CivilWarParent 付きの戦争（2026-09-28 から首都の蜂起も＝旧経路の CivilWarYears は削除）＝
        //   タグ付き戦争の出現＝開戦ニュース・消滅＝決着の年表。ペアIdキー＝国数の変動（建国/消滅）に強い（Id基準の作法）。
        if (_player.Diplomacy is { } dipCw)
        {
            _prevCivilWarKeys ??= new System.Collections.Generic.HashSet<long>();
            var curKeys = new System.Collections.Generic.HashSet<long>();
            foreach (var war in dipCw.Wars)
                if (war.CivilWarParent is { } cwp)
                {
                    var reb = war.A == cwp ? war.B : war.A;
                    long k = ((long)cwp.Id << 32) | (uint)reb.Id;
                    curKeys.Add(k);
                    if (!_prevCivilWarKeys.Contains(k))
                    {
                        bool mineCw = cwp.Nation == _player;
                        QueueNews("ev_civilwar", cwp.Name, cwp.Nation?.Latitude ?? 0, cwp.Nation?.Longitude ?? 0, mineCw);
                        Annal("⚔", $"{cwp.Name}で内戦：{reb.Name}が地図の上に立った");
                    }
                }
            foreach (var k in _prevCivilWarKeys)
                if (!curKeys.Contains(k))
                {
                    string pname = powers.FirstOrDefault(p => p.Id == (int)(k >> 32))?.Name ?? "";
                    Annal("🕊", pname.Length > 0 ? $"{pname}の内戦が決着した" : "内戦が決着した");   // 詳細（再併合/革命/分断）は外交ログが持つ＝二重に書かない
                }
            _prevCivilWarKeys = curKeys;
        }
        // 全国の政変・内戦・恐怖政治（列強すべて＝ワールドニュース）。遠い国ほど遅れて届く（QueueNews が距離でラグを付与）。
        for (int i = 0; i < powers.Count; i++)
        {
            var w = powers[i].Nation;
            if (w == null) continue;
            bool mine = w == _player;
            long id = powers[i].Id;
            if (_prevPolities.TryGetValue(id, out var prev) && w.Polity != prev)
            { QueueNews("ev_revolution", powers[i].Name, w.Latitude, w.Longitude, mine, PolityRegimeJa(w.Polity)); Annal("🏛", $"{powers[i].Name}:{PolityRegimeJa(prev)} → {PolityRegimeJa(w.Polity)}"); }
            // 恐怖政治（タスクB）：粛清累計が増えた＝急進革命で旧支配層が粛清・亡命した。Id基準で前回値と比較。
            double prevPurge = _prevTerror != null && _prevTerror.TryGetValue(id, out var pv) ? pv : w.TerrorPurgeCumulative;
            if (w.TerrorPurgeCumulative > prevPurge + 1e-6)
                QueueNewsText("⚔", $"{powers[i].Name}で恐怖政治：旧支配層が粛清された（{Fmt.PopFmt(w.TerrorPurgeCumulative - prevPurge)}）", NewsRed, w.Latitude, w.Longitude, mine);
            if (_prevTerror != null) _prevTerror[id] = w.TerrorPurgeCumulative;
            // 外国経済イベント（外国経済の可視化doc §3-2・2026-07-24）：恐慌入り/回復・金本位の離脱/復帰・デフォルト・大凶作を既存の距離遅延網へ。
            // 自国分は既存の自国エッジ（恐慌/金本位/デフォルト＝即時）が流すため他国のみ＝二重にしない。Id基準＝建国/消滅の国数変動に強い。
            if (!mine)
            {
                _prevEconState ??= new System.Collections.Generic.Dictionary<long, (bool Dep, bool Gold, bool Def, bool Fam)>();
                // ★ヒステリシス（2026-08-23）：入り>0.20／出<0.08＝エンジンの計器（depEnter/depExit）と同じ2値。以前は同一閾値で入りと出を判定＝境界で点滅していた。
                bool depN = SelfPlayMatch.InDepression(_prevEconState.TryGetValue(id, out var pePrev) && pePrev.Dep, w.GdpPcDrawdown.Value);
                bool famN = w.HarvestFactor < FamineHarvest;
                if (_prevEconState.TryGetValue(id, out var pe))
                {
                    // ★語と閾値の1対1（2026-08-23）：>0.25＝恐慌／それ以下＝景気後退＝自国側の通知（ev_depression/ev_recession）と同じ分け方
                    string word = SelfPlayMatch.DepressionWord(w.GdpPcDrawdown.Value);   // ★E2：以下この節は1人あたりで統一
                    if (depN && !pe.Dep) { QueueNewsText("📉", $"{powers[i].Name}が{word}に入った（1人あたりの下落{Fmt.Share(w.GdpPcDrawdown.Value)}）", NewsRed, w.Latitude, w.Longitude, false); Annal("📉", $"{powers[i].Name}が{word}（下落{Fmt.Share(w.GdpPcDrawdown.Value)}）"); }
                    else if (!depN && pe.Dep) QueueNewsText("📈", $"{powers[i].Name}の景気が回復した", NewsGreen, w.Latitude, w.Longitude, false);
                    if (w.OnGoldStandard && !pe.Gold) QueueNewsText("🪙", $"{powers[i].Name}が金本位制に加入した", NewsGold, w.Latitude, w.Longitude, false);
                    else if (!w.OnGoldStandard && pe.Gold) QueueNewsText("🪙", $"{powers[i].Name}が兌換を停止した", NewsGold, w.Latitude, w.Longitude, false);
                    if (w.HadSovereignDefault && !pe.Def) { QueueNewsText("💥", $"{powers[i].Name}が債務不履行に陥った", NewsRed, w.Latitude, w.Longitude, false); Annal("💥", $"{powers[i].Name}が債務不履行"); }
                    if (famN && !pe.Fam) QueueNewsText("🌾", $"{powers[i].Name}が大凶作（収穫{Fmt.Share(w.HarvestFactor)}）", NewsRed, w.Latitude, w.Longitude, false);
                }
                _prevEconState[id] = (depN, w.OnGoldStandard, w.HadSovereignDefault, famN);
            }
            _prevPolities[id] = w.Polity;
        }

        // 自国の経済イベント（突入の瞬間＝エッジのみ）。自国＝距離0＝即時。
        string me = PlayerNationName;
        double dd = _player.GdpPcDrawdown.Value, heat = _player.FinancialHeat, harvest = _player.HarvestFactor;   // ★E2：1人あたりで統一（外国側・初期基準値と同じ物差し）
        bool depression = SelfPlayMatch.InDepression(_prevDepression, dd);   // ★ヒステリシス（2026-08-23）＝外国側と同じ2値
        if (depression && !_prevDepression) { QueueNews(SelfPlayMatch.IsGreatDepression(dd) ? "ev_depression" : "ev_recession", me, 0, 0, true, dd); Annal("📉", $"{me}が{SelfPlayMatch.DepressionWord(dd)}（下落{Fmt.Share(dd)}）"); }   // ★年代記の語も通知の分岐と一致させる
        bool famine = harvest < FamineHarvest;
        if (famine && !_prevFamine) QueueNews("ev_famine", me, 0, 0, true, harvest);
        bool heatHot = heat > FinancialHeatHot;
        if (heatHot && !_prevHeatHot) QueueNews("ev_credit_heat", me, 0, 0, true, heat);
        bool austerity = _player.AusterityIntensity > AusterityActive;
        if (austerity && !_prevAusterity) QueueNews("ev_austerity", me, 0, 0, true);
        else if (!austerity && _prevAusterity) QueueNews("ev_austerity_end", me, 0, 0, true);
        if (_player.CumulativeDevaluation > _prevDeval + 1) QueueNews("ev_devaluation", me, 0, 0, true);

        _prevDepression = depression; _prevFamine = famine; _prevHeatHot = heatHot;
        _prevAusterity = austerity; _prevDeval = _player.CumulativeDevaluation;

        // === 自国の制度・政治の転機（プレイ判断に効く通知・変化した瞬間だけ）===
        void Notice(string icon, string text, IBrush b) => QueueNewsText(icon, text, b, 0, 0, true);
        if (_player.Suffrage != _pfSuf) Notice("🗳", $"参政権が「{JaEnumConverter.Ja(_player.Suffrage)}」になった", NewsBlue);
        if (_player.EducationLaw != _pfEdu) Notice("🗳", $"教育が「{JaEnumConverter.Ja(_player.EducationLaw)}」になった", NewsBlue);
        if (_player.Censorship != _pfCen) Notice("🗳", $"報道が「{JaEnumConverter.Ja(_player.Censorship)}」になった", NewsBlue);
        if (_player.LaborUnion != _pfUni) Notice("🗳", $"労働組合が「{JaEnumConverter.Ja(_player.LaborUnion)}」になった", NewsBlue);
        // 条約港の自然失効（案A・2026-08-16）：力の均衡が不平等条約を風化させた＝関税自主権の回復はプレイヤーに直接効く転機
        if (_pfTreatyPortInForce && !_player.UnequalTreatyInForce) Notice("🕊", "条約港が失効し関税自主権を回復しつつある", NewsGreen);
        if (_player.WorkHours != _pfWh) Notice("🗳", $"労働時間が「{JaEnumConverter.Ja(_player.WorkHours)}」になった", NewsBlue);
        bool childNow = _player.ChildLaborLegal >= 0.5;
        if (childNow != _pfChild) Notice("🗳", childNow ? "児童労働が合法化された" : "児童労働が禁止された", NewsBlue);
        if (_player.WomenRights != _pfWomenRights) Notice("🗳", $"女性の権利が「{JaEnumConverter.Ja(_player.WomenRights)}」になった", NewsBlue);   // ★統合法（2026-08-10）：段の変わり目を残す（教育権の段は旧UIに通知が無かった＝統合で自然に埋まる）
        if (_player.CornLaws != _pfCorn) Notice("🗳", $"穀物法が「{JaEnumConverter.Ja(_player.CornLaws)}」になった", NewsBlue);
        if (_player.LandReform != _pfLand) Notice("🗳", $"土地改革が「{JaEnumConverter.Ja(_player.LandReform)}」になった", NewsBlue);
        if (_player.SerfEmancipation != _pfSerf) Notice("🗳", $"農奴解放が「{JaEnumConverter.Ja(_player.SerfEmancipation)}」になった", NewsBlue);
        if (_player.Pension != _pfPen) Notice("🗳", $"年金が「{JaEnumConverter.Ja(_player.Pension)}」になった", NewsBlue);
        if (_player.SocialSecurityLaw != _pfSS) Notice("🗳", $"社会保障が「{JaEnumConverter.Ja(_player.SocialSecurityLaw)}」になった", NewsBlue);   // 弧3-3（2026-07-24）：段階の変わり目をニュースに残す
        // 参政権の範囲2法（2026-08-16）：施行通知。点火2026-09-06
        if (_player.ColonialSuffrage != _pfColSuf) Notice("🗳", $"植民地参政権が「{JaEnumConverter.Ja(_player.ColonialSuffrage)}」になった", NewsBlue);
        if (_player.MigrantNativeSuffrage != _pfMigSuf) Notice("🗳", _player.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? "移民原住民参政権が施行された" : "移民原住民参政権が廃止された", NewsBlue);
        if (_player.GoverningParty != _pfParty) Notice("🗳", $"選挙で{GoverningName()}が与党になった", NewsBlue);
        if (_player.OnGoldStandard != _pfGold) Notice("🪙", _player.OnGoldStandard ? "金本位制に加入した" : "兌換を停止し、金本位制から離脱した", NewsGold);
        if (_player.HadSovereignDefault && !_pfDefault) { Notice("💥", "債務不履行（デフォルト）に陥った", NewsRed); Annal("💥", $"{me}が債務不履行"); }
        if (_player.PatentLaw && !_pfPatent) Notice("⚙", "特許制度が施行された", NewsGreen);
        if (_player.JointStock && !_pfJoint) Notice("⚙", "株式会社制度が施行された", NewsGreen);
        bool epiNow = _player.EpidemicActiveYears > 0;
        if (epiNow != _pfEpidemic) Notice("🦠", epiNow ? "疫病が流行し始めた" : "疫病が終息した", epiNow ? NewsRed : NewsGreen);
        // 接15（監査2026-07-11・2026-07-25）：治安崩壊のエッジ通知（従来は観測量が毎月更新されるのにプレイヤーに不可視だった）
        bool orderLow = _player.PublicOrder < 0.5;
        // ⚠★指標名は**UIと通知で完全に一致させる**（2026-08-02・作者指示「治安と秩序など」）。
        //   同じ量を2つの名前で呼ぶと、プレイヤーは別々の量だと思う。ヘッダーの指標名（治安）が正。
        if (orderLow != _pfOrderLow) Notice("🚨", orderLow ? $"治安が崩れている(治安{Fmt.Share(_player.PublicOrder)})" : "治安が回復した", orderLow ? NewsRed : NewsGreen);
        var rankNow = _sim.Diplomacy.RankOf(_sim.Diplomacy.Powers[_playerIndex]);
        if (rankNow != _pfRank) Notice("⭐", $"国家ランクが{RankJa(rankNow)}になった", NewsGold);
        SnapshotTurningPoints();
        // 終局年代記（#129）：全国の開戦/終戦のエッジ（Id対キー＝国数変動に強い）。ニュース網とは独立の事実記録
        var warNow = new System.Collections.Generic.Dictionary<(long, long), string>();
        foreach (var war in _sim.Diplomacy.Wars)
        {
            var k = war.A.Id < war.B.Id ? (war.A.Id, war.B.Id) : (war.B.Id, war.A.Id);
            warNow[k] = $"{war.A.Name}と{war.B.Name}";
            if (_prevWars != null && !_prevWars.ContainsKey(k)) Annal("⚔", $"{warNow[k]}が開戦");
        }
        if (_prevWars != null)
            foreach (var kv in _prevWars)
                if (!warNow.ContainsKey(kv.Key)) Annal("🕊", $"{kv.Value}が講和");
        _prevWars = warNow;
        DetectDiplomacyPopups();   // #106：緊張ポップアップ／開戦の最後通牒（四半期）
    }

    private string PlayerNationName => _player.SelfPower?.Name ?? _player.Region.Name;
    private string EventDate() => DateAt(_simDays);
    private static string DateAt(double day) { int t = Math.Max(0, (int)day); return $"{Calendar.StartYear + t / Calendar.DaysPerYear:D4}/{t % Calendar.DaysPerYear / Calendar.DaysPerMonth + 1:D2}/{t % Calendar.DaysPerMonth + 1:D2}"; }   // 任意の経過日→表示暦（開戦日の導出）
    // 講和結果のログ文字列照合（PeaceResultFromLog）は廃止＝包含国名（ローマ⊂神聖ローマ等）で誤爆するため、
    // エンジンの終戦記録 Diplomacy.EndedWars（終戦月の最終値つき）を一次ソースにした（レビュー2026-07-19）。

    // 報せを配達キューへ。自国(isPlayer)＝即時。他国＝首都間の大圏距離×年代のラグだけ遅れて届く（電信普及でラグ縮小、1870年で実質0）。
    // 文言（助言なし）は alerts.csv。日付は「届いた日」＝遅延分は（nヶ月前）を付す。
    private void QueueNews(string id, string nation, double srcLat, double srcLon, bool isPlayer, params object[] args)
    {
        var e = AlertCatalog.Make(id, args);
        DispatchNews(e.Icon, $"{nation}で、{e.Text}", e.Color, srcLat, srcLon, isPlayer);
    }

    // 任意文面の報せを距離ラグ付きで配達キューへ（外交ログ用。QueueNews と同じ伝播則＝電信普及で短縮・1870で実質0）。
    private void QueueNewsText(string icon, string body, IBrush color, double srcLat, double srcLon, bool isPlayer)
        => DispatchNews(icon, body, color, srcLat, srcLon, isPlayer);

    private void DispatchNews(string icon, string body, IBrush color, double srcLat, double srcLon, bool isPlayer)
    {
        double delay = 0;
        if (!isPlayer)
        {
            double km = Geo.DistanceKm(_player.Latitude, _player.Longitude, srcLat, srcLon);
            // ★2026-09-03（キューAUD-05・監査「暦年・台本」）：**暦年で書かない**（規律①）。
            //   報せの速さは電信・鉄道・汽船＝前線技術のランプそのものなので、距離定数の時代関数と**同じ物差し**を読む。
            //   ⚠新しい技術の量も新しい定数も作らない（EraFactor の内部ランプ＝TransportRevolutionOnset/Span を1回だけ再現）。
            //   1783（技術が閾値未満）=1.0 → 前線技術が育ち切ると 0＝旧「1783→1870で線形に0」と同じ形で、暦でなく世界の技術が動かす。
            var coef = _player.Coef;
            double ramp = System.Math.Clamp((_player.WorldTechLevel - coef.TransportRevolutionOnset) / System.Math.Max(1e-9, coef.TransportRevolutionSpan), 0, 1);
            if (!(ramp >= 0 && ramp <= 1)) ramp = 0;   // 否定形＝NaNは「技術以前」へ倒す
            double slowness = 1.0 - ramp;
            delay = km * NewsDelayPerKm * slowness;
        }
        if (delay < 1) DeliverNews(icon, body, color, _simDays, isPlayer);                                   // ほぼ即時
        else _pending.Add(new PendingNews(_simDays + delay, _simDays, icon, body, color, isPlayer));         // 距離分だけ遅れて到着
    }

    // 到着日を過ぎた報せを配達（毎tick・日次判定）。
    // ★同じ日にまとめて届く分の並び（2026-08-10・作者「月が逆走している気がする」）：出来事パネル全体は新しい順なのに、
    //   **1回の配達の中だけが古い順**に並んでいた。原因＝待ち行列を末尾から（新しいものから）走査して配達し、
    //   DeliverNews が毎回 Insert(0) するので、**最後に配られた最古の報せが一番上に来る**という反転。
    //   除去のための逆走査は正しいので、取り出した分を古い順へ戻してから配達する＝先頭に積み上がる順が新しい順になる。
    private void FlushPending()
    {
        List<PendingNews>? due = null;
        for (int k = _pending.Count - 1; k >= 0; k--)
            if (_pending[k].DeliveryDay <= _simDays)
            {
                (due ??= new()).Add(_pending[k]);
                _pending.RemoveAt(k);
            }
        if (due is null) return;
        due.Reverse();   // 逆走査で新しい順に溜まっている＝古い順へ戻す（_pending は発生順＝EventDay の昇順で積まれる）
        foreach (var n in due) DeliverNews(n.Icon, n.Body, n.Color, n.EventDay, n.Mine);
    }

    // 配達：日付は「届いた日」。遅れて届いた報せは（nヶ月前）を付す。全件リストへ積み、フィルタ（世界⇄自国のみ）に合う分だけ表示へ。
    private void DeliverNews(string icon, string body, IBrush color, double eventDay, bool mine = true)
    {
        int lateMonths = (int)System.Math.Round((_simDays - eventDay) / (double)Calendar.DaysPerMonth);
        string suffix = lateMonths >= 1 ? $"({lateMonths}ヶ月前)" : "";
        var row = new AlertRow(icon, $"{EventDate()} {body}{suffix}", color, mine, InvolvesGreatPower(body, mine));
        _allAlerts.Insert(0, row);
        while (_allAlerts.Count > AlertsKept) _allAlerts.RemoveAt(_allAlerts.Count - 1);
        if (NewsPasses(mine, row.Great))
        {
            Alerts.Insert(0, row);
            while (Alerts.Count > AlertsShown) Alerts.RemoveAt(Alerts.Count - 1);
        }
    }
}
