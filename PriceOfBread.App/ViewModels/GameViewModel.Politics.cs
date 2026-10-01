// GameViewModel.Politics.cs ＝ 政治タブ＝議会（議席/得票/次の選挙）・与党・政体別実績・国内勢力の集計。
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
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    public ObservableCollection<BlocRow> Blocs { get; } = new();
    // 政体別実績ストック（2026-07-13）：4系統（君主制/議会制/強権/急進）の信認⇔失望∈[-1,+1]。正＝長期成功で信認（entrench）／負＝危機で倒れ discredited。固着⇔カオスの直接ドライバ。政治タブに符号色つきバーで表示。
    public ObservableCollection<RegimeRow> RegimeRecords { get; } = new();
    // 国内勢力（F2）：FactionSnapshot.Compute の読み取り専用表示。タブを開いた時＋開いている間の月次（Refreshごと）に更新。
    public ObservableCollection<FactionRow> DomesticFactions { get; } = new();
    [ObservableProperty] private bool _noFactions = true;   // クラスタなし＝「目立った国内勢力なし」表示

    // 与党名（動的政党なら「ダブリンの共和主義党」等）。議席の内訳は下の議会パネル（棒）が持つ＝重複表示をしない。
    // ★2026-08-04：政体で絞る（HasParliament と同条件）。旧＝絶対王政でも議席が出て、すぐ上の「議会健全性：議会なし」と
    //   真っ向から矛盾していた（専制でも PartySeats は世論として毎期計算されるが、法にも正統性にも効かない）。
    /// <summary>与党の行＝党名＋公約（作者指示2026-08-24）。公約＝総選挙で加担した議題そのもの（World.Manifesto*）。
    /// 達成すると加担した思想の支持が伸びる（Parliament.StepManifesto → Ideology）＝表示は同じ状態を映すだけ。</summary>
    public string ParliamentText => !HasParliament ? ""
        : _player.ManifestoIdeology < 0
            ? $"与党:{GoverningName()}"
            : $"与党:{GoverningName()}　マニフェスト:{FactionSnapshot.DemandJa(_player.ManifestoAxis, _player.ManifestoIncrease)}　{(_player.ManifestoDone ? "達成" : "未達成")}";   // 節の見出しが既に「議会」＝重ねない
    private static string PartyName(int i) => i switch { 0 => "保守党", 1 => "自由党", _ => "社会党" };
    // ★議会の実体（2026-08-04・作者指摘「議会の存在感が薄い」）＝議席・得票・次の選挙・支持基盤。
    //   旧＝1行の文字列だけで、何が議席を決め議席が何を決めるかが画面に一切出ていなかった。
    //   ⚠政体で絞る＝議席が法に効くのは立憲君主制/共和制だけ（PolicyPreferences.HasLegislature と同条件）。
    //     専制でも PartySeats は毎期計算されている（世論の形）が、法にも正統性にも効かない＝出すと嘘になる。
    public bool HasParliament => _player.ParliamentEnabled
        && _player.Polity is Polity.ConstitutionalMonarchy or Polity.Republic
        && (_player.PartySeats[0] + _player.PartySeats[1] + _player.PartySeats[2]) > 0.005;
    // ★権力の奪い合いの節だけは**議会の成立を待たない**（作者裁定2026-08-17）。
    //   この節が見せるのは「議会の状態」ではなく**権力が動いている過程**＝議会が無い段階こそ見どころだから：
    //   実測（シード1789）＝フランスは1783年から絶対王政のまま「破産による召集」で議会統制力が毎期+1.87%積み上がり、
    //   約6年で立憲の床を越える＝**王が金に詰まって議会に権力を渡していく過程（1789三部会）**。HasParliament で隠すと
    //   これが床を越えるまで一切見えない。軍政（Seizure）で軍の忠誠が動く局面も同じ理由で見せる。
    //   ⚠上段の ParliamentControlText の「議会なし」は別の話（**議席が法に効くか**）＝そちらの判定は動かさない。
    public bool HasPowerStruggle => _player.ParliamentControl > 0 || System.Math.Abs(_player.ArmyAllegiance) > 1e-9;
    [ObservableProperty] private List<SeatRow> _seatRows = new();
    [ObservableProperty] private List<SupportRow> _supportRows = new();
    // 次の総選挙まで（データのみ）。非選挙政体では議会節ごと出さない。
    public string NextElectionText
    {
        get
        {
            if (_player.Polity is not (Polity.ConstitutionalMonarchy or Polity.Republic)) return "—";
            int left = System.Math.Max(0, _player.Coef.ElectionIntervalQuarters - _player.QuartersSinceElection);
            return $"次の総選挙 {left / (double)Calendar.QuartersPerYear:F1}年";   // 裸の年数では何の年数か読めない（指標名を落とさない）
        }
    }

    private void BuildParliament()
    {
        BuildPopTips();   // ★2026-09-24：POPタブのホバー4本（GameViewModel.Pop.cs）。⚠ここに置いた理由＝呼び手は毎Refresh 1回でよく、Refresh.cs はこの枠の持ち物でない（BuildComposition/BuildParliament の並びへ移してよい）
        // ⚠通知は**先に**出す＝false→true へ転じた四半期に画面が追従する（早期returnの後ろに置くと、
        //   議席が0の間は通知が一度も出ず、参政権を得ても議会の節が現れないまま固まる）。
        OnPropertyChanged(nameof(HasParliament));
        OnPropertyChanged(nameof(HasPowerStruggle));   // ★権力の節は議会より**先に**現れうる（絶対王政の三部会）＝別の可視条件を別に通知する
        OnPropertyChanged(nameof(NextElectionText));
        OnPropertyChanged(nameof(ParliamentText));
        BuildPowerStruggle();   // ★権力の奪い合い＝**早期returnより前**（議会が無い段階こそ見どころ＝可視条件は HasPowerStruggle）。四半期の基準化も非表示中に進む
        if (!HasParliament) { SeatRows = new(); SupportRows = new(); return; }
        string[] fam = ["保守", "自由", "社会"];
        var rows = new List<SeatRow>();
        for (int i = 0; i < 3; i++)
            rows.Add(new SeatRow
            {
                Name = fam[i],
                Seats = _player.PartySeats[i].ToString("P0"),
                SeatFrac = System.Math.Clamp(_player.PartySeats[i], 0, 1),
                Votes = _player.VoteShares[i].ToString("P0"),
                IsGov = _player.GoverningParty == i,
                Brush = i == 0 ? PNameText : i == 1 ? PUtilText : PPlus,
            });
        SeatRows = rows;
        // 支持基盤＝階級ごとの票の重みと族の割れ方（SimCore と同じ式）。上位8階級（票の多い順）。
        var sup = new List<SupportRow>();
        double tot = 0;
        var baseList = SimCore.Parliament.SupportBase(_player);
        foreach (var b in baseList) tot += b.Voters;
        foreach (var b in baseList)
        {
            if (sup.Count >= 8 || b.Voters <= 1e-9) break;
            double v = b.Voters;
            sup.Add(new SupportRow
            {
                Class = b.Class,
                Share = tot > 1e-9 ? (v / tot).ToString("P0") : "—",
                Cons = (b.Cons / v).ToString("P0"), Lib = (b.Lib / v).ToString("P0"), Soc = (b.Soc / v).ToString("P0"),   // ★カラム式（2026-08-22）
            });
        }
        SupportRows = sup;
    }

    // ============ 権力の奪い合い（2026-08-17 作者の問い「王権・議会・軍の権力奪い合いモデルは完成しているか」）============
    // エンジン（CrownParliament.Step）は**議会統制力**を毎四半期5つの経路で奪い合わせているのに、政治タブには値の2行しかなく
    // 「誰が誰から奪っているか」が読めなかった＝**今期その値を動かした経路を寄与の大きい順に並べる**節を1つ置く。
    //
    // ⚠⚠ 内訳の出所＝**UI側の純関数による再計算**（説明API ExplainLedger には載せていない＝記帳を足すには SimCore の
    //    CrownParliament.Step を編集する必要があり、今回は SimCore を「読み取り専用の最小公開」に限る指示があるため）。
    //    よって**エンジンと式が2つに割れる危険**を負う。割れ幅を構造的に最小化する書き方（変更時に守ること）：
    //      ①下位量は必ず**エンジンの関数をそのまま呼ぶ**（CrownParliament.SoldierCrownLean／PolicyPreferences.ArmyPower／
    //        PoliticalQuantities.EffectiveCoerce・DriftedPolityCoord／Ideologies.National・SupportAt・BasePolityCoord）。
    //      ②閾値は**エンジンの定数・係数をそのまま読む**（CrownParliament.CrownRollbackLegitimacyMin 等＝数値を書き写さない）。
    //      ③重複するのは「ゲートの並び」と「係数の掛け算」だけ。下の各ブロックは CrownParliament.Step の該当節と1対1で、
    //        行コメントに係数名/関数名を明記する（行番号は編集のたびにずれるので使わない）＝**式を動かす時は必ず両方を直す**（片方だけ直すとUIが嘘をつく）。
    // ⚠読む時点の差：エンジンは四半期の**開始状態**で各項を順に適用し、ここは**適用後の状態**で同じ式を評価する。
    //    符号と順序は一致するが、大きさは入力が1四半期ぶん動いた分だけずれる。だから「今期の変化」だけは式を使わず
    //    **前四半期の値との差＝観測値**を出す（真値の錨。下賜や政体遷移のジャンプもここには必ず現れる）。
    public ObservableCollection<PowerShiftRow> PowerShifts { get; } = new();

    // ★★★権力の取り分（2026-08-21・作者設計「権力の取り分」＝2026-09-07 半円）＝PowerAffinity の連合後シェア（王権/議会/軍部/革命勢力・合計1）。
    //   ⚠これは表示＝観測。挙動はどこもこの値で分岐しない（作者裁定2026-08-19「政体は表示のみ」の帰結）。
    //   帯グラフの幅は SeatRow と同じ Conv.FracToWidth（0..1→px）＝新しい部品を作らない。
    public double ShareCrown { get; private set; }
    public double ShareParliament { get; private set; }
    public double ShareArmy { get; private set; }
    public double ShareRevolution { get; private set; }
    // ⚠撤去2026-08-23（読み手ゼロ）: ShareLegendText ＝PoliticsTab は ShareCrown/ShareParliament/ShareArmy/ShareRevolution を個別に束縛（凡例文字列は誰も読まない）。
    public string ShareTip { get; private set; } = "";            // 連合前→連合後（誰が誰に付いたか）
    public string LegitimacySplitText { get; private set; } = ""; // 正統性の比（王権:議会）
    public string BudgetPowerText { get; private set; } = "";     // 予算権の配分
    public string BudgetLawName { get; private set; } = "";       // 予算法（勅令/部分承認/議会承認）
    public string CommandPowerText { get; private set; } = "";    // 統帥権の配分
    public string CommandLawName { get; private set; } = "";      // 統帥法（王権/分割/議会統制）
    public bool ArmyHoldsCommand { get; private set; }            // ★軍が指揮権を独占（統帥と予算が軍部へ）
    // ★半円グラフ（作者指示2026-09-07「円グラフより半円グラフの方が可視性が高い＝政治・支配度は半円で」・旧2026-08-22 円グラフ）：4枚の扇形の Path データ（SVG 記法→Conv.PathData）。
    public string PieCrown { get; private set; } = "";
    public string PieParliament { get; private set; } = "";
    public string PieArmy { get; private set; } = "";
    public string PieRevolution { get; private set; } = "";

    /// <summary>★半円の扇形の Path データ（2026-09-07 作者指示＝円→半円）：底辺の中点 cx,cy・半径 r・開始 frac0〜終了 frac1∈[0,1]。
    /// 左端（9時）を0・右端（3時）を1として頂点を通り時計回り＝合計1で上半分の半円が閉じる。弧は常に180°以下＝large-arc は0。
    /// ⚠ほぼ全部（>0.999）は半円で描く。ほぼ0（<0.001）は空＝描かない。政治タブと州パネル（支配度）の両方がこの1本を使う。</summary>
    internal static string HalfSlice(double cx, double cy, double r, double f0, double f1)
    {
        double span = f1 - f0;
        if (!(span > 0.001)) return "";
        if (span > 0.999) return System.FormattableString.Invariant($"M {cx - r},{cy} A {r},{r} 0 0 1 {cx + r},{cy} Z");
        double a0 = f0 * System.Math.PI, a1 = f1 * System.Math.PI;
        double x0 = cx - r * System.Math.Cos(a0), y0 = cy - r * System.Math.Sin(a0);
        double x1 = cx - r * System.Math.Cos(a1), y1 = cy - r * System.Math.Sin(a1);
        return System.FormattableString.Invariant($"M {cx},{cy} L {x0:F2},{y0:F2} A {r},{r} 0 0 1 {x1:F2},{y1:F2} Z");
    }
    /// <summary>★半円の外周の帯（線だけ・塗らない）：0〜frac の弧。州パネルの「国家区分の10年前」に使う。</summary>
    internal static string HalfArc(double cx, double cy, double r, double frac)
    {
        if (!(frac > 0.001)) return "";
        double a1 = System.Math.Min(1.0, frac) * System.Math.PI;
        double x1 = cx - r * System.Math.Cos(a1), y1 = cy - r * System.Math.Sin(a1);
        return System.FormattableString.Invariant($"M {cx - r},{cy} A {r},{r} 0 0 1 {x1:F2},{y1:F2}");
    }
    [ObservableProperty] private string _parliamentControlTip = "";   // 議会統制力の床（この線を越えるとラベルが立憲/共和へ動く）
    // ★政治資本の在庫（作者裁定N2・2026-09-21）：現在 a／最大 b。ホバーは b と実行度の材料のデータ行だけ（説明文なし）。
    [ObservableProperty] private string _politicalCapitalText = "";
    [ObservableProperty] private string _politicalCapitalTip = "";
    // ★2026-08-24作者指示：正統性・予算権・統帥権を**小さな半円グラフ**で（大きな半円＝権力の取り分と同じ色の文法・同じ HalfSlice）。
    //   3つとも同じ (王権, 議会, 軍部) の3値＝軍が指揮権を独占している間は軍部100%の1色になる。値は半円の下の1行と ToolTip。
    [ObservableProperty] private string _legitPieCrown = "";
    [ObservableProperty] private string _legitPieParliament = "";
    [ObservableProperty] private string _legitPieArmy = "";
    [ObservableProperty] private string _budgetPieCrown = "";
    [ObservableProperty] private string _budgetPieParliament = "";
    [ObservableProperty] private string _budgetPieArmy = "";
    [ObservableProperty] private string _commandPieCrown = "";
    [ObservableProperty] private string _commandPieParliament = "";
    [ObservableProperty] private string _commandPieArmy = "";
    [ObservableProperty] private string _legitTopText = "";     // 半円の下＝優勢な側とその割合（内訳はホバー）
    [ObservableProperty] private string _budgetTopText = "";
    [ObservableProperty] private string _commandTopText = "";
    [ObservableProperty] private string _legitTip = "";
    [ObservableProperty] private string _budgetTip = "";
    [ObservableProperty] private string _commandTip = "";
    // 小半円の寸法＝底辺の中点(30,30)・半径29（箱 幅60×高さ31）。大半円は底辺の中点(70,70)・半径68（箱 幅140×高さ72）＝器と絵を揃える（2026-09-07 半円化）
    private const double SmallPieC = 30, SmallPieR = 29;
    /// <summary>王権/議会/軍部の3値から小円の扇形3枚と「優勢な側 n%」の1行を作る（合計が0なら空＝円を描かない）。</summary>
    private static (string c, string p, string a, string top) SmallPie(double crown, double parl, double army)
    {
        double sum = crown + parl + army;
        if (!(sum > 1e-9)) return ("", "", "", "—");
        double fc = crown / sum, fp = parl / sum;
        string top = army >= crown && army >= parl ? $"軍部 {army / sum:P0}"
                   : crown >= parl ? $"王権 {fc:P0}" : $"議会 {fp:P0}";
        return (HalfSlice(SmallPieC, SmallPieC, SmallPieR, 0, fc),
                HalfSlice(SmallPieC, SmallPieC, SmallPieR, fc, fc + fp),
                HalfSlice(SmallPieC, SmallPieC, SmallPieR, fc + fp, 1.0), top);
    }
    [ObservableProperty] private string _pcDeltaText = "—";           // 今期の変化＝観測値（前四半期との差）
    [ObservableProperty] private string _pcDeltaTip = "";
    [ObservableProperty] private IBrush _pcDeltaBrush = TermNeutral;
    [ObservableProperty] private string _armyLoyaltyText = "";        // 軍の忠誠＝向き（王権寄り/議会寄り）と値
    [ObservableProperty] private string _armyLoyaltyTip = "";
    [ObservableProperty] private string _armyPowerText = "";          // 軍の政治力＝どれだけの重さで押すか
    [ObservableProperty] private string _armyPowerTip = "";

    private int _pcQuarterStamp = -1;   // 四半期境界の検出（年×4＋四半期。GameViewModel.Fiscal.cs の _prevQuarterStamp と同型）
    private double _pcPrev;             // 前四半期の議会統制力（「今期の変化」の基準）
    private double _pcFrom;             // 確定した「今期の変化」の始点（ホバー用＝_pcPrev は次の基準へ進んでしまうので別に持つ）
    private bool _pcDeltaKnown;         // 初回（開始直後・ロード直後）は基準化のみ＝変化を出さない
    private int _pcGrantPrev;           // 前四半期の下賜回数（World.ReformGrantCount＝上からの改革で床を与えた診断カウンタ）
    private int _pcGrantInQuarter;      // 今期に下賜が起きた回数（ジャンプの説明＝ホバーに出す）

    // 四半期あたりの寄与の書式＝符号つきの百分率（議会統制力そのものが P0 表示＝同じ物差しの上での増減）。
    private static string Pp(double v) => v.ToString("+0.00%;-0.00%;0.00%");
    private static string Signed2(double v) => v.ToString("+0.00;-0.00;0.00");

    private void BuildPowerStruggle()
    {
        var w = _player;
        var c = w.Coef;
        double pc = Math.Clamp(w.ParliamentControl, 0, 1);
        // --- 今期の変化＝観測値（式を使わない）。四半期境界でだけ基準を進める＝月次Refreshで0に潰れない。
        int q = w.Year * Calendar.QuartersPerYear + w.Quarter;
        if (q != _pcQuarterStamp)
        {
            if (_pcQuarterStamp >= 0)
            {
                double d = pc - _pcPrev;
                PcDeltaText = Pp(d);
                PcDeltaBrush = Math.Abs(d) < 5e-6 ? TermNeutral : d > 0 ? TermPlus : TermMinus;
                _pcFrom = _pcPrev; _pcDeltaKnown = true;
                _pcGrantInQuarter = Math.Max(0, w.ReformGrantCount - _pcGrantPrev);
            }
            _pcPrev = pc; _pcGrantPrev = w.ReformGrantCount; _pcQuarterStamp = q;
        }
        // --- ★政治資本の在庫 a/b（作者裁定N2・2026-09-21）：a＝max(0, b−目減り)／b＝支持と権力の大きさ ---
        //   物差しは法律タブの代価表示（cost×100）と同じ＝「政治資本 18 / 22」と「政治資本 2」が同じ単位で並ぶ。
        //   ★2026-09-21 ㉖で旗を焼き込み＝行は常設（旧＝旗0では行ごと出さない可視条件）。
        {
            double bMax = w.PoliticalCapitalMax, aNow = Math.Max(0, bMax - w.MandateSpent);
            PoliticalCapitalText = $"{aNow * 100:F0} / {bMax * 100:F0}";
            double aff = Ideologies.SupportAt(Ideologies.National(w), PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
            double sup = Math.Max(0, Math.Min(1, (aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin)));
            double pe = PowerShares.ParliamentEffective(w);
            double seats = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];
            double maxSeat = Math.Max(w.PartySeats[0], Math.Max(w.PartySeats[1], w.PartySeats[2]));
            double split = seats > 1e-9 ? Math.Max(0, Math.Min(1, 1.0 - maxSeat / seats)) : 0;
            // ⚠2026-09-21（検証で発見）：旧は係数 MandateRefillQuarters（＝40）をそのまま出していた＝**実行度を無視した嘘の数**。
            //   満タンまでの実時間は 40/実行度（実測の実行度 0.44 の国なら 91四半期）。この行が「40」と言い続けると
            //   プレイヤーは議会制の国でも10年で満タンになると読む＝計器が機構と食い違う（規律「計器が一番バグる」）。
            double rate = PoliticalQuantities.PoliticalCapitalRate(w);
            double toFull = !(rate > 0) ? 0 : c.MandateRefillQuarters / rate;
            PoliticalCapitalTip = $"正統性 {w.Legitimacy:F2}　現政体の支持 {sup:F2}"
                                + $"\n議会の実効統制 {pe:F2}　議席の分裂 {split:F2}　内戦 {(w.InCivilWar ? 1 : 0)}"
                                + $"\n実行度 {rate:F2}　満タンまでの四半期 {toFull:F0}";
        }

        // --- ★権力の取り分（作者設計の半円グラフ・可視条件なし＝常設。連合後の実効シェア） ---
        {
            var co = PowerAffinity.Coalition(w);
            var eff = co.Effective;
            ShareCrown = eff[(int)PowerAffinity.Holder.Crown];
            ShareParliament = eff[(int)PowerAffinity.Holder.Parliament];
            ShareArmy = eff[(int)PowerAffinity.Holder.Army];
            ShareRevolution = eff[(int)PowerAffinity.Holder.Revolution];
            string armyAlly = co.Ally[(int)PowerAffinity.Holder.Army] == PowerAffinity.Holder.Army
                ? "単独" : PowerAffinity.HolderNames[(int)co.Ally[(int)PowerAffinity.Holder.Army]];
            ShareTip = $"連合前　王権 {co.Raw[0]:P0}　議会 {co.Raw[1]:P0}　軍部 {co.Raw[2]:P0}　革命勢力 {co.Raw[3]:P0}"
                     + $"\n軍部の同盟先　{armyAlly}"
                     + (co.SoleRuler is { } sr ? $"\n単独支配　{PowerAffinity.HolderNames[(int)sr]}" : "\n連合政治（単独支配なし）");
            var (l2c, l2p) = PowerAffinity.Legitimacy2(w);
            LegitimacySplitText = $"王権 {l2c:P0}　議会 {l2p:P0}";
            var (bC, bP, bA) = PowerAffinity.BudgetShares(w);
            var (mC, mP, mA) = PowerAffinity.CommandShares(w);
            BudgetLawName = w.BudgetLaw switch { BudgetLaw.Decree => "勅令", BudgetLaw.Partial => "部分承認", _ => "議会承認" };
            CommandLawName = w.CommandLaw switch { CommandLaw.Crown => "王権", CommandLaw.Split => "分割", _ => "議会統制" };
            ArmyHoldsCommand = PowerAffinity.ArmyHoldsCommand(w);
            BudgetPowerText = ArmyHoldsCommand ? "軍部 100%" : $"王権 {bC:P0}　議会 {bP:P0}";
            CommandPowerText = ArmyHoldsCommand ? "軍部 100%" : $"王権 {mC:P0}　議会 {mP:P0}";
            // 小半円グラフ（作者指示2026-08-24・2026-09-07 半円化）＝上と同じ値をそのまま扇形にする（第二の式を置かない）。正統性は王権と議会の2者だけ。
            var lp = SmallPie(l2c, l2p, 0);
            var bp = SmallPie(bC, bP, bA);
            var mp = SmallPie(mC, mP, mA);
            LegitPieCrown = lp.c; LegitPieParliament = lp.p; LegitPieArmy = lp.a; LegitTopText = lp.top;
            BudgetPieCrown = bp.c; BudgetPieParliament = bp.p; BudgetPieArmy = bp.a; BudgetTopText = bp.top;
            CommandPieCrown = mp.c; CommandPieParliament = mp.p; CommandPieArmy = mp.a; CommandTopText = mp.top;
            // ★2026-09-23：正統性の**説明API（ExplainLedger）の内訳**をこの半円のホバーへ足す（行は増やさない＝既存のセルのホバー）。
            //   理由＝ヘッダーの「正統性」Stat（GameViewModel.Refresh.cs の StatGExplain）が畳まれると ExplainLedger の読み手が
            //   UI から消えて死蔵になる。書式はそのヘッダーと**同じ F3**＝同じ量が2箇所で違う桁に見えない。
            //   整形（＋/−・|Δ|<5e-4 の間引き・降順）は既存の ExplainTerms 1本を呼ぶだけ＝第二の式を置かない。
            string legitTip = LegitimacySplitText;
            foreach (var t in ExplainTerms("Legitimacy", v => v.ToString("F3"))) legitTip += "\n" + t.Text;   // 台帳が空（計測前・非プレイヤー）なら1行も足さない＝旧表示のまま
            // ★2026-09-24：国民意識の説明API内訳も**同じ作法で**この半円のホバーへ（GameViewModel.Header.cs:155 の申し送り
            //   ＝ヘッダーから畳まれた内訳の移送先。前の回はここが未結線で ExplainLedger の "NationalConsciousness" が死蔵だった）。
            //   置き場の理由＝政治タブで台帳を持つセルはこの1つだけ（新しい行も新しいプロパティも作らない・2026-07-16の
            //   「国民意識の重複行は撤去」を戻さない）。⚠題の1行は**台帳に項がある時だけ**＝空ならヘッダーの数字を写さない。
            //   ⚠書式 P1＝ここに並ぶのは四半期ごとの増分で、ヘッダーと同じ P0 では育成・減衰が全て 0% に潰れて計器が嘘をつく
            //   （題の1行だけはヘッダーと同じ P0＝水準の数は2つの桁で出さない）。
            var ncTerms = ExplainTerms("NationalConsciousness", v => v.ToString("P1"));
            if (ncTerms.Count > 0)
            {
                legitTip += $"\n国民意識　{_player.NationalConsciousnessDisplay:P0}";
                foreach (var t in ncTerms) legitTip += "\n" + t.Text;
            }
            LegitTip = legitTip;
            BudgetTip = $"{BudgetLawName}　{BudgetPowerText}";
            CommandTip = $"{CommandLawName}　{CommandPowerText}";
            double c1 = ShareCrown, c2 = c1 + ShareParliament, c3 = c2 + ShareArmy;
            PieCrown = HalfSlice(70, 70, 68, 0, c1);
            PieParliament = HalfSlice(70, 70, 68, c1, c2);
            PieArmy = HalfSlice(70, 70, 68, c2, c3);
            PieRevolution = HalfSlice(70, 70, 68, c3, 1.0);
            OnPropertyChanged(nameof(PieCrown)); OnPropertyChanged(nameof(PieParliament));
            OnPropertyChanged(nameof(PieArmy)); OnPropertyChanged(nameof(PieRevolution));
            OnPropertyChanged(nameof(ShareCrown)); OnPropertyChanged(nameof(ShareParliament));
            OnPropertyChanged(nameof(ShareArmy)); OnPropertyChanged(nameof(ShareRevolution));
            OnPropertyChanged(nameof(ShareTip));
            OnPropertyChanged(nameof(LegitimacySplitText)); OnPropertyChanged(nameof(BudgetPowerText));
            OnPropertyChanged(nameof(BudgetLawName)); OnPropertyChanged(nameof(CommandPowerText));
            OnPropertyChanged(nameof(CommandLawName)); OnPropertyChanged(nameof(ArmyHoldsCommand));
        }
        if (!HasPowerStruggle) { PowerShifts.Clear(); return; }   // ★可視条件は節と同一（HasParliament ではない＝議会の成立前から組む・作者裁定2026-08-18）

        // 床は**ラベルが動く境界**そのもの（CrownParliament.LabelFromAxes）＝いまどちら側に居て、あと何ポイントで名前が変わるかを出す。
        // 現ラベルが立憲/共和なら下抜け床、それ以外は立憲の床が「次に越える線」＝ヒステリシスの向きに合わせて1本だけ示す。
        bool inConstitutional = w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic;
        double edge = inConstitutional ? c.ConstitutionalControlExit : c.ConstitutionalControlFloor;
        ParliamentControlTip = $"立憲の床 {c.ConstitutionalControlFloor:P0}　下抜け {c.ConstitutionalControlExit:P0}"
                             + $"\n{(inConstitutional ? "下抜けまで" : "立憲まで")} {Math.Abs(pc - edge):P1}"
                             + (w.ReformGrantCount > 0 ? $"\n上からの改革の下賜 {w.ReformGrantCount}回" : "");
        PcDeltaTip = (_pcDeltaKnown ? $"前四半期 {_pcFrom:P1}　今期 {pc:P1}" : $"今期 {pc:P1}")
                   + (_pcGrantInQuarter > 0 ? $"\n上からの改革の下賜 {_pcGrantInQuarter}回" : "");

        // --- 軍＝どちらに（忠誠）、どれだけの重さで（政治力）押すか -----------------------------------------
        // 忠誠の実体＝CrownParliament `ArmyAllegiance = clamp(lean + milAff, -1, 1)`。2因子をホバーへ開く。
        // ★2026-08-18：給与の乗算は撤去（給与は帰属でなく規律・能力＝即応度を動かす）。即応度は軍事タブの側で見る。
        double lean = CrownParliament.SoldierCrownLean(w);                            // エンジンの関数をそのまま呼ぶ
        double milAff = c.ArmyMilitarismAffinity * PoliticalQuantities.OfficerAppointmentCrownward(w);   // ★2026-08-18＝将校の任免権の所在
        ArmyLoyaltyText = $"{ArmyAllegianceText}　{Signed2(w.ArmyAllegiance)}";
        ArmyLoyaltyTip = $"兵士の思想の傾き {Signed2(lean)}　将校の任免権 {Signed2(milAff)}";
        // 政治力＝PolicyPreferences.ArmyPower（係数×兵士の人口比×(1−議会統制力)）。×100＝法律タブの賛否ホバーと同じ目盛り。
        double armyPower = PolicyPreferences.ArmyPower(w);
        ArmyPowerText = (armyPower * 100).ToString("F1");
        ArmyPowerTip = $"兵士の人口比 {(w.TotalPopulation > 1 ? w.SoldierSize() / w.TotalPopulation : 0):P1}"
                     + $"　議会統制力 {pc:P0}　係数 {c.ArmyPoliticalWeight:F1}";

        // --- 議会統制力を動かした経路（CrownParliament.Step と1対1・寄与の大きい順） -------------------------
        var rows = new List<PowerShiftRow>();
        void Add(string name, double delta, string tip)
        {
            // 表示で 0.00% に丸まる項は出さない＝休眠の係数もゲートの閉じた経路もここで落ちる（「効いていない」を数字で語らない）
            if (!(Math.Abs(delta) > 5e-6)) return;
            rows.Add(new PowerShiftRow { Name = name, Value = Pp(delta), Magnitude = Math.Abs(delta), Tip = tip, Brush = delta >= 0 ? TermPlus : TermMinus });
        }

        // ① 破産召集（1789三部会）＝CrownParliament.Step の SummonParliamentRate 節
        if (c.SummonParliamentRate > 0 && w.AusterityIntensity > 0.5)
            Add("破産による議会の召集", c.SummonParliamentRate * (w.AusterityIntensity - 0.5) * 2.0,
                $"債務緊縮の強度 {w.AusterityIntensity:P0}　発火の下限 50%");
        // ② 王権の巻き返し（1907六三クーデタ）＝CrownParliament.Step の CrownRollbackRate 節（閾値の定数もエンジンから読む）
        if (c.CrownRollbackRate > 0
            && (c.CrownlessCollapseKeepsRepublic <= 0 || w.Crown != CrownForm.None)
            && w.CoopParliamentCrown < -0.5
            && w.Legitimacy > CrownParliament.CrownRollbackLegitimacyMin
            && w.ArmyAllegiance > CrownParliament.CrownRollbackArmyAllegianceMin)
            Add("王権の巻き返し", -c.CrownRollbackRate,
                $"王権と議会の協力度 {Signed2(w.CoopParliamentCrown)}　正統性 {w.Legitimacy:P0}　軍の忠誠 {Signed2(w.ArmyAllegiance)}");

        // ③〜⑤ 連続動学（P3b）＝CrownParliament.Step の該当節。外周ゲート（奪取中は凍結・5係数のいずれかが生きている）もエンジンと同じ形で持つ。
        if (w.Seizure == SeizureForm.None
            && (c.ParliamentPressureRate > 0 || c.CrownPressureRate > 0 || c.RepublicanDeposition > 0 || c.CrownFormMutation > 0 || c.ArmyPcPressure > 0))
        {
            var agg = Ideologies.National(w);
            double S(Polity p) => Ideologies.SupportAt(agg, Ideologies.BasePolityCoord(p), c.IdeologyAffinitySlope);
            double cur = Ideologies.SupportAt(agg, PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
            double rep = Math.Max(S(Polity.ConstitutionalMonarchy), S(Polity.Republic));
            double inertia = c.PolityInertia * w.Legitimacy;
            double coerce = PoliticalQuantities.EffectiveCoerce(w, c);
            double challenge = w.ReformistPower + w.RadicalPower;
            // ③ 議会の圧力（漸進の立憲化）＝ParliamentPressureRate
            if (c.ParliamentPressureRate > 0 && rep - cur > inertia && challenge > coerce)
                Add("議会の圧力", c.ParliamentPressureRate * (rep - cur),
                    $"代議制の支持 {rep:F2}　現政体の支持 {cur:F2}　慣性 {inertia:F2}\n動員 {challenge:F1}　実効治安維持力 {coerce:F1}");
            // ④ 王権の圧力（反動の常時項）＝CrownPressureRate
            if (c.CrownPressureRate > 0 && w.Crown != CrownForm.None)
            {
                double crown = Math.Max(S(Polity.Absolutist), Math.Max(S(Polity.Despotic), S(Polity.Shogunate)));
                if (crown > rep)
                    Add("王権の圧力", -c.CrownPressureRate * (crown - rep) * w.Legitimacy,
                        $"王権の形の支持 {crown:F2}　代議制の支持 {rep:F2}　正統性 {w.Legitimacy:P0}");
            }
            // ⑤ 軍の圧力（統帥権＝プロイセン憲法紛争1862）＝ArmyPcPressure
            if (c.ArmyPcPressure > 0 && lean > 0)
                Add("軍の圧力", -c.ArmyPcPressure * lean * armyPower,
                    $"兵士の思想の傾き {Signed2(lean)}　軍の政治力 {armyPower * 100:F1}");
        }

        rows.Sort((a, b) => b.Magnitude.CompareTo(a.Magnitude));   // 寄与の大きい順（符号でなく大きさ＝押している/押し返しているの主役から読める）
        SyncRows(PowerShifts, rows, r => r.Name, (t, s) => t.CopyFrom(s));
    }

    // F4：与党の実体名（クラスタ由来の動的政党が族内最大なら「ダブリンの共和主義党」等）。空＝未初期化は族名。
    private string GoverningName() => _player.GoverningPartyName.Length > 0 ? _player.GoverningPartyName : PartyName(_player.GoverningParty);


    // 反乱勢力の戦力比の色：通常＝反乱規模列と同じ茶／100%超＝蜂起圏＝警告赤（TensionHighと同色）。
    // internal＝ヘッダーの「反体制派の戦力比」（GameViewModel.Header.cs）も同じ2色を読む＝同一の物差しは同一の配色。
    internal static readonly IBrush FactionStrengthCalm = new SolidColorBrush(Color.Parse("#6B5230"));
    internal static readonly IBrush FactionStrengthDanger = new SolidColorBrush(Color.Parse("#A0432D"));
    // 反乱勢力の末列の見出し＝その世界で実際に点火を決めている量の名前（段4の点火後は戦力比でなく2門の近接度）。3文字＝列幅48pxのまま。
    public string FactionProximityHeader => _player.Coef.RevoltIgnitionIntensity > 0 ? "近接度" : "戦力比";

    // 国内勢力（F2・設計メモ2026-07-09 §3 Phase2）：運動に参加したpopを地域×階級×イデオロギー主軸で観察（読み取り専用・World非変更）。
    // ⚠参加の述語はエンジン側の1本（FactionSnapshot.ComputeClusters）＝既定は「闘争度>0.5」の二値、
    //   ParticipationDiscontent>0（二量分離 段3・2026-09-09）では max(不満度, 闘争度) の**重み**＝規模は「参加した頭数」になる。
    // 表示＝機械生成名・規模・平均闘争度・要求top-3（法をゲートする同じPref表から導出）。蜂起への対処は国内危機イベント（蜂起寸前にポップアップ）が唯一の入口＝ここは表示のみ。
    private void RefreshFactions()
    {
        var rows = new List<FactionRow>();
        // ★2026-09-23 作者指示「反乱勢力は規模欄に n人(n%) を表示」。
        //   **(n%) の分母＝自国人口**（World.TotalPopulation＝分離独立で抜けた州を除く全popの和）。この分母を選ぶ理由は2つ：
        //     ① エンジンの足切り（Factions.Step の MinSizeFraction）が **同じ w.TotalPopulation** を分母にしている＝画面の%を閾値とそのまま比べられる。
        //     ② POPタブの規模「3187K(49%)」の分母と同じ＝タブ間で「%」の意味が割れない。
        //   ⚠分子と分母の非対称（既知・エンジンも同じ）：分子 f.Size は **Active州のpop×参加の重み**の和、分母は Seceded を除く全州。
        //     よって%は「国の人口のうち、この運動に参加している頭数の割合」＝100%を超えない（参加の重みは∈[0,1]）。
        //   ⚠TotalPopulation は毎回全州全popを走査するプロパティ＝**行ごとに呼ぶとO(n²)**。ループの外で1回だけ取る。
        double natPop = _player.TotalPopulation;
        // F3a（2026-07-12）：永続クラスタ（World.Factions＝結束・変異の旗を持つ運動体）があればそれを表示。
        // 無ければ従来のスナップショット（F2互換＝ロード直後や闘争度の低い平時）。
        if (_player.Factions.Count > 0)
        {
            var demandsByBloc = new List<FactionSnapshot.Demand>?[PolicyPreferences.PettyBloc + 1];   // 第4束「小営業」(bloc3)対応（プチブル利益束2026-08-15・PettyInterestGate>0で現れる）
            // 強さ比（UI修正R7・2026-08-16 診断「警告と点火の乖離」）：闘争度はどの点火不等式にも単独で現れない＝
            // 蜂起の真の近接度＝F3b点火不等式そのもの（Politics.CheckFactionRevolt）を行ごとに出す
            // ＝旧は力の門（FactionStrength > 保持力+郷勇）、段4の点火後は連言2門（下の RevoltIgnitionIntensity 分岐）。
            // いずれも純関数の再計算＝World非変更。分母ガードは PoliticalQuantities.ChallengeRatio と同じ Max(1,·)。
            double coerce = PoliticalQuantities.EffectiveCoerce(_player, _player.Coef);   // エンジンの判定と同一式（第5段5-1の一元化・ChallengeTipと同じ）
            foreach (var f in _player.Factions.OrderByDescending(x => x.Size))
            {
                demandsByBloc[f.Bloc] ??= FactionSnapshot.ComputeDemands(_player, f.Bloc);
                var dl = demandsByBloc[f.Bloc]!;
                string demands = dl.Count > 0
                    ? "要求:" + string.Join("・", dl.Select(d => FactionSnapshot.DemandJa(d.Axis, d.Increase)))
                    : "要求:なし";
                string display = Factions.DisplayName(f);
                double strength = Politics.FactionStrength(_player, _player.Coef, f);
                double militia = Politics.LoyalistMilitia(_player, _player.Coef, f);   // internal（InternalsVisibleTo・読み取り専用）
                double ratio = strength / Math.Max(1.0, coerce + militia);
                string tip = $"勢力の強さ {strength:F1}\n治安維持力 {coerce:F1}\n義勇兵 {militia:F1}";
                // ★段4＝点火の付け替え（F3b蜂起doc §3-5・2026-09-08）。点火が戦力比から連言2門へ移ったら**この列も移す**＝
                //   さもないと赤い行から蜂起せず白い行から蜂起する（画面が嘘をつく）。近接度＝2門の**律速側**＝
                //   min(意志÷門1, 絶望÷門2)＝100%超⇔両門を越える⇔エンジンの点火条件そのもの（100%の意味は旧列と同じ）。
                //   戦力比は「勝てるか」＝決着の見込みとしてホバーに残る（LoyalistMilitia の読み手はここと Politics の決着）。
                if (_player.Coef.RevoltIgnitionIntensity > 0)
                {
                    double intensity = strength / Math.Max(1e-9, f.Size);
                    double despair = Politics.RegionRevoltHeat(f.Region, _player.Coef)
                                   + _player.Coef.RevoltGrievanceWeight * Math.Min(1.0, _player.Coef.FactionRejectionMutation * f.Rejections);
                    ratio = Math.Min(intensity / _player.Coef.RevoltIgnitionIntensity, despair / Math.Max(1e-9, _player.Coef.RegionalRevoltThreshold));
                    tip = $"組織された意志 {intensity:F3}（点火 {_player.Coef.RevoltIgnitionIntensity:F3}）\n"
                        + $"州の絶望 {despair:F3}（点火 {_player.Coef.RegionalRevoltThreshold:F3}）\n" + tip;
                }
                rows.Add(new FactionRow
                {
                    Key = display,
                    Name = display,
                    Size = PopFmt(f.Size) + (!(natPop > 1e-9) ? "" : $"({f.Size / natPop:P0})"),   // 分母＝自国人口（上の natPop のコメント）。比の判定は否定形＝NaN も空へ落ちる
                    Cohesion = f.Cohesion.ToString("P0"),
                    Strength = ratio.ToString("P0"),
                    StrengthTip = tip,
                    HasStrengthTip = true,
                    StrengthBrush = ratio >= 1.0 ? FactionStrengthDanger : FactionStrengthCalm,
                    Demands = demands,
                });
            }
        }
        else foreach (var f in FactionSnapshot.Compute(_player))
        {
            string demands = f.Demands.Count > 0
                ? "要求:" + string.Join("・", f.Demands.Select(d => FactionSnapshot.DemandJa(d.Axis, d.Increase)))
                : "要求:なし";
            rows.Add(new FactionRow
            {
                Key = f.Name,
                Name = f.Name,
                Size = PopFmt(f.Size) + (!(natPop > 1e-9) ? "" : $"({f.Size / natPop:P0})"),   // 永続クラスタ側と同じ分母・同じ書式（第二の式を置かない）
                Cohesion = "—",   // スナップショット＝永続FactionState無し＝結束が定義されない
                Strength = "—",   // スナップショット＝永続FactionState無し＝FactionStrengthが定義されない（結束・変異を持たない）
                StrengthTip = "",
                HasStrengthTip = false,
                StrengthBrush = FactionStrengthCalm,
                Demands = demands,
            });
        }
        SyncRows(DomesticFactions, rows, r => r.Key, (t, s) => t.CopyFrom(s));
        NoFactions = DomesticFactions.Count == 0;
        RefreshArmyNeeds();   // 理想の軍隊（戦争タブ）＝国内勢力と同じ更新経路（タブを開いた瞬間＋開いている間の毎期）
    }

    // ============ 理想の軍隊（2026-08-16 作者指示「理想の軍隊に必要な材（兵士POP含め）を戦争タブに羅列し、
    //   それぞれどの程度充足しているかを表示」）============
    // ★「必要量」と「実際」はエンジンの充足率の**分母と分子そのもの**を材ごとに開いたもの＝新しい物差しを作らない：
    //   ・消耗の段（基本装備・空軍装備）＝EconomyStep の want（MilitaryScale×レート×**陸海の再配分**＝afford前の必要量）と
    //     got（発注×配給率）。発注＝必要量×予算の充足×価格による買い控え。
    //     ★2026-09-03（キューA-14）：**是正済み**＝エンジン側 equipWant（EconomyStep）は陸海の再配分 navalMul/landMul を掛けた
    //       材別加重の分母になっており、ここの材別行と同じ定義になっている。旧コメント（一律レート＝定義が違う）は現物と食い違っていた。
    //   ・耐久の段（上位装備・最上位装備／ArmsStockEnabled>0）＝EconomyStep の need（平時規模×レート÷摩耗率＝目標在庫）と
    //     Region.ArmsStock（保有）。⚠この段だけ「実際」は在庫であって今月買えた量ではない（エンジンの Fill の定義と同じ）。
    //   ・兵士＝Military.Step の desired（平時＝扶養限界の目標の州按分〔(145)＝軍事費の段は平時の兵数を動かさない〕・戦時は徴兵の max）と現在の兵士POP。
    // ⚠**需要が立っていない材は行を作らない**（必要量0＝休眠の係数・港のない国の艦・世界にまだ存在しない財）
    //   ＝「充足0%」と書くと嘘になる（燃料4本は2026-08-16時点で既定0＝休眠＝1行も出ない）。
    public ObservableCollection<ArmyNeedRow> ArmyNeeds { get; } = new();
    [ObservableProperty] private bool _noArmyNeeds = true;
    /// <summary>この節の見出し＝「軍事力:〇位(充足n%)」（ヘッダーから移設・作者指示2026-08-24）。
    /// 順位＝自国より戦力ゲージ（質込み）が大きい国の数＋1。充足＝**この表に出ている行の平均**＝画面に無い第二の式を作らない
    /// （⚠エンジン集計の Region.MilitaryEquipFill は分母に陸海の再配分が入っておらず「軍艦1083%」を生んだ側＝読まない）。</summary>
    [ObservableProperty] private string _armyIdealHeader = "軍事力";
    [ObservableProperty] private string _armyIdealTip = "";

    private void RefreshArmyNeeds()
    {
        const double Eps = 1e-9;
        var c = _player.Coef;
        var r = _player.Region;          // 軍需調達が立つのは首都州（Simulation の capShare＝他州は MilitaryScale 0）
        int ng = r.Markets.Length;
        var rows = new List<ArmyNeedRow>();
        var unavailArr = ProductionDiag.UnavailableGoods(_sim, c);   // まだ世界に存在しない財は数字を出さない（作者裁定2026-08-10・生産タブと同一の物差し）
        bool Unavail(int g) => g < 0 || g >= unavailArr.Length || unavailArr[g];

        // ---- 人員 ----
        var sol = SoldierTargetNow();
        if (sol.Target > Eps || sol.Now > Eps)
        {
            rows.Add(new ArmyNeedRow { Key = "＿見出し＿人員", Header = "人員" });
            // 給与の充足＝請求の賃金のうち払えた割合＝国家の支払い率 r（(140) から兵の給与は請求×r で払う＝全ての州で同じ r）。
            //   ★(144)：旧の州の欄 SoldierPayAffordRaw は r の写し（導ける量を貯めていた＝思想⑩）＝欄ごと撤去して r を読む。
            double payFill = Math.Min(1.0, _player.FiscalFundingRatio);
            // ★(149) 反証の指摘：軍事費の段は (145) から平時の兵数を動かさない（装備の予算・即応度・治安維持力を動かす）＝兵の目標の行から装備の行へ移した。
            string solTip = $"徴募可能人口 {PopFmtK(sol.Manpower)}"
                          + (sol.Sustain > Eps ? $"\n扶養限界の目標 {PopFmtK(sol.Sustain)}" : "")
                          + $"\n軍事費 {_player.MilitarySpending:P0}　装備の充足 {_player.Region.MilitaryEquipFill:P0}　給与の充足 {payFill:P0}　即応度 {_player.MilitaryReadiness:P0}";   // ★2026-09-03（A-14）：装備の充足を復帰＝エンジンの分母（equipWant）が陸海再配分込みになり、直下の材別行と同じ定義になった（旧＝定義が2つあるので非表示にしていた）
            rows.Add(NeedRow("兵士", "兵士", sol.Target, sol.Now, sol.Target > Eps ? sol.Now / sol.Target : 1.0, solTip, PopFmtK, ""));
        }

        // ---- 装備（4段＝EconomyStep の軍需調達と同じ順序・同じ式）----
        bool milProc = r.MilitaryScale > Eps && EconomyStep.AllPresent(c.MilitaryGoods, ng);
        if (milProc)
        {
            // 装備の原単位＝発見の解禁度を通した表（EconomyStep.DiscoveredRates＝エンジンと同じ1本・2026-09-11 #17）
            double[] milRates = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates),
                     qRates = EconomyStep.DiscoveredRates(r, c, c.MilitaryQualityGoods, c.MilitaryQualityRates),
                     t3Rates = EconomyStep.DiscoveredRates(r, c, c.MilitaryTier3Goods, c.MilitaryTier3Rates),
                     airRates = EconomyStep.DiscoveredRates(r, c, c.MilitaryAirGoods, c.MilitaryAirRates);
            double Price(int g) => r.Markets[g].Price;
            double Ration(int g) => r.Markets[g].LastRation;
            // 価格による買い控え＝国家調達の価格の門の1本（EconomyStep.StatePriceAfford＝エンジンと同じ式・2026-09-30 段4e K）
            double PriceAfford(int g) => EconomyStep.StatePriceAfford(r, c, g);
            // 陸海の再配分（EconomyStep）：港のない国は艦を発注せず、その分が陸へ回る。理想の形の海比（ShapeNavalShare）にも寄る。
            double flatNaval = 0, flatAll = 0;
            for (int b = 0; b < c.MilitaryGoods.Length; b++)
            {
                double cst = milRates[b] * Price(c.MilitaryGoods[b]);
                flatAll += cst;
                if (Array.IndexOf(c.FleetGoodIds, c.MilitaryGoods[b]) >= 0) flatNaval += cst;
            }
            double flatShare = flatAll > Eps ? flatNaval / flatAll : 0;
            double navalMul = 1.0, landMul = 1.0;
            if (c.NavalProcurementNeedsPort > 0 && !r.NationHasPort)
            {
                navalMul = 0.0;
                landMul = flatShare < 1 - Eps ? 1.0 / (1.0 - flatShare) : 1.0;
            }
            else if (c.NavalProcurementFromShape > 0 && r.ShapeNavalShare >= 0 && flatShare > Eps && flatShare < 1 - Eps)
            {
                double want = Math.Clamp(r.ShapeNavalShare, 0, 1), k = c.NavalProcurementFromShape;
                navalMul = Math.Max(0, 1.0 + k * (want / flatShare - 1.0));
                landMul = Math.Max(0, 1.0 + k * ((1 - want) / (1 - flatShare) - 1.0));
            }
            double MilMul(int g) => Array.IndexOf(c.FleetGoodIds, g) >= 0 ? navalMul : landMul;
            // ★①基本段の耐久品（2026-08-26）＝財ごとの摩耗率。1.0＝消耗品＝従来のフロー行（弾薬・馬）。
            // ⚠**未初期化の在庫は目標**とみなす＝エンジンの EconomyStep.ArmsHeld と同一の定義（表示とエンジンで在庫の意味を割らない）。
            bool baseStock = c.BaseArmsStockEnabled > 0;
            double BaseWear(int b) => baseStock && b < c.MilitaryWearRates.Length ? c.MilitaryWearRates[b] : 1.0;
            bool BaseDurable(int b) => BaseWear(b) < 1.0;
            // ⚠目標には**陸海の再配分を必ず掛ける**＝エンジンの EconomyStep.BaseArmsTarget と同一（分子と分母を同じ規則に）。
            double BaseTarget(int b) => r.MilitaryScalePeace * milRates[b] * MilMul(c.MilitaryGoods[b]) / Math.Max(1e-6, BaseWear(b));
            double BaseHeld(int b) => r.ArmsStock is { } bd && bd.TryGetValue(c.MilitaryGoods[b], out double bv) ? bv : BaseTarget(b);
            // 予算の充足（EconomyStep）：残余カスケード（pro rata の器 MilitaryProcureProRata は 2026-09-26 に削除）。
            double estCost = 0;
            for (int b = 0; b < c.MilitaryGoods.Length; b++) estCost += r.MilitaryScale * milRates[b] * Price(c.MilitaryGoods[b]);
            double signalBudget = r.MilitaryBudgetCeiling;   // EconomyStep と同じ＝Budget ≤ Ceiling は Simulation の書き手の単調性で成立（旧 Math.Max(Ceiling, Budget) と同値・2026-09-11 M4）
            double afford = estCost > Eps ? Math.Min(1.0, r.MilitaryAffordRaw) : 0.0;
            // 耐久装備の目標在庫と発注（EconomyStep）
            double wear = Math.Max(1e-6, c.ArmsWearRate);
            double warWear = wear * (1.0 + c.ArmsWarWearScale * Math.Max(0, r.MilitaryWarMultLast - 1.0));
            bool stock = c.ArmsStockEnabled > 0;
            double StockTarget(double rate) => r.MilitaryScalePeace * rate / wear;
            double Held(int g) => r.ArmsStock?.GetValueOrDefault(g) ?? 0.0;
            double StockOrder(int g, double rate) => Math.Max(0, StockTarget(rate) - Held(g));

            // 段1＝基本装備（★2026-08-26 以降は耐久＝在庫／消耗＝フローの混在。BaseArmsStockEnabled=0 では全て従来のフロー行）
            var tier1 = new List<ArmyNeedRow>();
            for (int b = 0; b < c.MilitaryGoods.Length; b++)
            {
                int g = c.MilitaryGoods[b];
                if (BaseDurable(b))
                {
                    // 耐久品＝「必要量」は目標在庫・「実際」は保有（上位装備の行と同じ読み方）
                    double needS = BaseTarget(b);
                    if (!(needS > Eps) || Unavail(g)) continue;
                    double haveS = BaseHeld(b);
                    double orderS = r.MilitaryScale * milRates[b];   // ★発注量は従来のフローのまま（在庫化しても発注は変えない）
                    double bWear = Math.Max(1e-6, BaseWear(b));
                    double bWarWear = bWear * (1.0 + c.ArmsWarWearScale * Math.Max(0, r.MilitaryWarMultLast - 1.0));
                    string tipS = $"単価 {Money(Price(g))}　今月の発注 {MilQty(orderS * afford * MilMul(g) * PriceAfford(g))}"
                                + $"\n月次の摩耗 {Math.Min(0.9, bWarWear):P1}　予算の充足 {afford:P0}　市場の配給 {Ration(g):P0}";
                    tier1.Add(NeedRow("基本" + g, Catalog.Goods[g].Name, needS, haveS, Math.Min(1.0, haveS / needS), tipS, MilQty, "", needS * Price(g)));
                    continue;
                }
                // ★必要量に陸海の再配分を掛ける（作者報告2026-08-21「軍艦1083%」の是正）。実発注（EconomyStep）は
                //   `MilitaryScale×rate×afford×MilMul×PriceAfford` なので、分母を一律レートのままにすると比が MilMul 倍
                //   （実測 navalMul=10.83）に膨らんでいた。理想の海シェアが高い国は軍艦の必要量そのものが一律レートより多い。
                //   実際（need×fill）は再配分込みのまま＝エンジンとビット一致。navalMul=0 の内陸国は行ごと消える
                //   （解禁前の飛行機を「充足0%」と書かない Unavail と同じ規律）。
                double need = r.MilitaryScale * milRates[b] * MilMul(g);
                if (!(need > Eps) || Unavail(g)) continue;
                tier1.Add(FlowRow("基本", g, need, afford * PriceAfford(g) * Ration(g), afford, Ration(g), PriceAfford(g), MilMul(g), Price(g)));
            }
            // 輜重自動車の燃料（EconomyStep・★2026-08-29点火＝既定0.05。=0なら行なし）。原油の継続生産があるときだけ需要が立つ
            if (c.MotorFuelRate > 0 && Catalog.CrudeOilId < ng)
            {
                double need = r.MilitaryScale * c.MotorFuelRate;
                if (need > Eps && r.Markets[Catalog.CrudeOilId].LastProduced > Colonial.OilViableProductionFrac * need && !Unavail(Catalog.CrudeOilId))
                    tier1.Add(FlowRow("輜重燃料", Catalog.CrudeOilId, need, afford * PriceAfford(Catalog.CrudeOilId) * Ration(Catalog.CrudeOilId),
                                      afford, Ration(Catalog.CrudeOilId), PriceAfford(Catalog.CrudeOilId), 1.0, Price(Catalog.CrudeOilId), "原油(輜重)"));
            }
            AddGroup(rows, "基本装備", tier1);

            // 段2＝上位装備（耐久＝目標在庫と保有）／段3＝最上位装備／段4＝空軍装備（消耗）
            if (EconomyStep.AllPresent(c.MilitaryQualityGoods, ng))
            {
                double remaining = Math.Max(0, r.MilitaryBudget - estCost * afford);
                double qCost = 0;
                for (int b = 0; b < c.MilitaryQualityGoods.Length; b++)
                    qCost += (stock ? StockOrder(c.MilitaryQualityGoods[b], qRates[b]) : r.MilitaryScale * qRates[b]) * Price(c.MilitaryQualityGoods[b]);
                double qAfford = qCost > Eps ? Math.Min(1.0, remaining / qCost) : 0.0;
                AddGroup(rows, "上位装備", TierRows("上位", c.MilitaryQualityGoods, qRates, qAfford));

                if (EconomyStep.AllPresent(c.MilitaryTier3Goods, ng))
                {
                    double remaining3 = Math.Max(0, remaining - qCost * qAfford);
                    double t3Cost = 0;
                    for (int b = 0; b < c.MilitaryTier3Goods.Length; b++)
                        t3Cost += (stock ? StockOrder(c.MilitaryTier3Goods[b], t3Rates[b]) : r.MilitaryScale * t3Rates[b]) * Price(c.MilitaryTier3Goods[b]);
                    double t3Afford = t3Cost > Eps ? Math.Min(1.0, remaining3 / t3Cost) : 0.0;
                    AddGroup(rows, "最上位装備", TierRows("最上位", c.MilitaryTier3Goods, t3Rates, t3Afford));

                    if (EconomyStep.AllPresent(c.MilitaryAirGoods, ng))
                    {
                        double remainingA = Math.Max(0, remaining3 - t3Cost * t3Afford);
                        double aCost = 0, airFuelNeed = 0;
                        for (int b = 0; b < c.MilitaryAirGoods.Length; b++)
                            aCost += r.MilitaryScale * airRates[b] * Price(c.MilitaryAirGoods[b]);
                        if (c.AirFuelRate > 0 && Catalog.CrudeOilId < ng)   // 空軍の燃料（EconomyStep・★2026-08-29点火＝既定0.045。=0なら行なし）
                        {
                            double afNeed = r.MilitaryScale * c.AirFuelRate;
                            if (r.Markets[Catalog.CrudeOilId].LastProduced > Colonial.OilViableProductionFrac * afNeed)
                            { airFuelNeed = afNeed; aCost += airFuelNeed * Price(Catalog.CrudeOilId); }
                        }
                        double aAfford = aCost > Eps ? Math.Min(1.0, remainingA / aCost) : 0.0;
                        var air = new List<ArmyNeedRow>();
                        for (int b = 0; b < c.MilitaryAirGoods.Length; b++)
                        {
                            int g = c.MilitaryAirGoods[b];
                            double need = r.MilitaryScale * airRates[b];
                            if (!(need > Eps) || Unavail(g)) continue;   // 解禁前の飛行機を「充足0%」と書かない
                            air.Add(FlowRow("空軍", g, need, aAfford * PriceAfford(g) * Ration(g), aAfford, Ration(g), PriceAfford(g), 1.0, Price(g)));
                        }
                        if (airFuelNeed > Eps && !Unavail(Catalog.CrudeOilId))
                            air.Add(FlowRow("空軍燃料", Catalog.CrudeOilId, airFuelNeed, aAfford * PriceAfford(Catalog.CrudeOilId) * Ration(Catalog.CrudeOilId),
                                            aAfford, Ration(Catalog.CrudeOilId), PriceAfford(Catalog.CrudeOilId), 1.0, Price(Catalog.CrudeOilId), "原油(空軍)"));
                        AddGroup(rows, "空軍装備", air);
                    }
                }
            }

            // 艦隊の運用燃料（Military.NavalFuelStep・既定 NavalFuelPerGauge=0＝休眠＝行なし）。充足は炭庫のEMA
            if (c.NavalFuelPerGauge > 0)
            {
                double gauge = _player.SelfPower?.FleetGauge ?? 0;
                double steamGate = Discovery.GoodGate(_player.Region, c.ShipTier2GoodId);   // 汽船の門＝Military.NavalFuelStep と同じ1本（(150)）
                double coalWant = c.NavalFuelPerGauge * gauge * Math.Clamp(_player.NavyUpkeep, 0, 1) * steamGate * c.EraFactor(_player.WorldTechLevel, c.EraNavalFuel);
                if (coalWant > Eps)
                {
                    var (fg, want) = Colonial.ChooseFuelGood(r, c, coalWant);
                    if (fg < ng && want > Eps && !Unavail(fg))
                    {
                        double fill = Math.Clamp(_player.NavalFuelFillEma, 0, 1);
                        string nm = Catalog.Goods[fg].Name + "(艦隊)";
                        string tip = $"単価 {Money(Price(fg))}　必要額 {Money(want * Price(fg))}/月"
                                   + $"\n市場の配給 {Ration(fg):P0}　艦隊ゲージ {gauge:F1}　海軍維持費 {_player.NavyUpkeep:P0}";
                        AddGroup(rows, "運用燃料", new List<ArmyNeedRow> { NeedRow("艦隊燃料", nm, want, want * fill, fill, tip, MilQty, "/月") });
                    }
                }
            }

            // 段2/3の1行（耐久＝目標在庫と保有／休眠時は消耗と同じ式）。ローカル関数＝上の afford 群を閉包で共有する
            List<ArmyNeedRow> TierRows(string tag, int[] goods, double[] rates, double tierAfford)
            {
                var list = new List<ArmyNeedRow>();
                for (int b = 0; b < goods.Length; b++)
                {
                    int g = goods[b];
                    if (Unavail(g)) continue;
                    if (stock)
                    {
                        double need = StockTarget(rates[b]);
                        if (!(need > Eps)) continue;
                        double have = Held(g), order = StockOrder(g, rates[b]);
                        string tip = $"単価 {Money(Price(g))}　今月の発注 {MilQty(order * tierAfford * MilMul(g) * PriceAfford(g))}"
                                   + $"\n月次の摩耗 {Math.Min(0.9, warWear):P1}　予算の充足 {tierAfford:P0}　市場の配給 {Ration(g):P0}";
                        list.Add(NeedRow(tag + g, Catalog.Goods[g].Name, need, have, Math.Min(1.0, have / need), tip, MilQty, "", need * Price(g)));
                    }
                    else
                    {
                        double need = r.MilitaryScale * rates[b] * MilMul(g);   // 段1と同じ是正＝必要量が再配分込み（上の「軍艦1083%」の注記）
                        if (!(need > Eps)) continue;
                        list.Add(FlowRow(tag, g, need, tierAfford * PriceAfford(g) * Ration(g), tierAfford, Ration(g), PriceAfford(g), MilMul(g), Price(g)));
                    }
                }
                return list;
            }
            // 消耗の1行：必要量＝一律レート×陸海の再配分／実際＝必要量×(予算の充足×価格による買い控え×市場の配給)
            ArmyNeedRow FlowRow(string tag, int g, double need, double fill, double budget, double ration, double gate, double mul, double price, string? label = null)
            {
                string tip = $"単価 {Money(price)}　必要額 {Money(need * price)}/月"
                           + $"\n予算の充足 {budget:P0}　市場の配給 {ration:P0}"
                           + (gate < 1 - 1e-6 ? $"　価格による買い控え {1 - gate:P0}" : "")
                           + (Math.Abs(mul - 1.0) > 1e-6 ? $"\n陸海の再配分 ×{mul:F2}" : "");
                return NeedRow(tag + g, label ?? Catalog.Goods[g].Name, need, need * fill, fill, tip, MilQty, "/月", need * price);
            }
        }
        SyncRows(ArmyNeeds, rows, x => x.Key, (t, s) => t.CopyFrom(s));
        NoArmyNeeds = ArmyNeeds.Count == 0;

        // 節の見出し＝軍事力の順位と、この表の充足の平均（見出し行は数えない）。
        int milPos = 1;
        if (_sim.Diplomacy is { } dipA && _player.SelfPower is { } meA)
            foreach (var q in dipA.Powers) if (q != meA && q.Nation is not null && q.Military > meA.Military) milPos++;
        double fillSum = 0; int fillN = 0;
        foreach (var row in rows) if (row.IsRow) { fillSum += row.FillV; fillN++; }
        double fillAvg = fillN > 0 ? fillSum / fillN : 1.0;
        ArmyIdealHeader = fillN > 0 ? $"軍事力:{milPos}位(充足{fillAvg:P0})" : $"軍事力:{milPos}位";
        ArmyIdealTip = $"順位＝戦力ゲージ（質込み）が自国より大きい国の数＋1\n充足＝下の{fillN}行の平均　即応度 {_player.MilitaryReadiness:P0}";
    }

    /// <summary>軍需の数量表記。1万以上は生産タブと同じ k/M（Qty）、それ未満は素の数。
    /// ⚠k単位に一本化しない理由＝軍需の量は数十〜数百のオーダー（1850年の仏＝小銃357/月・馬36/月）で、
    /// k化すると必要量も実際も「0.0k」に潰れて**数字が情報を持たなくなる**（緊張が常に100%と出ていたのと同型の失敗）。
    /// 生産タブのk統一は「生産タブが出す量」への裁定＝金額・百分率と同じく、桁が届かない量はその外にある。</summary>
    private static string MilQty(double v)
    {
        double a = Math.Abs(v);
        return a >= 1e4 ? Qty(v) : a >= 100 ? v.ToString("N0") : v.ToString("F1");
    }

    /// <summary>段の小見出し＋その段の行（必要額の大きい順・1行も無ければ見出しごと出さない＝空の節を作らない）。</summary>
    private static void AddGroup(List<ArmyNeedRow> rows, string header, List<ArmyNeedRow> group)
    {
        if (group.Count == 0) return;
        group.Sort((a, b) => b.Cost.CompareTo(a.Cost));
        rows.Add(new ArmyNeedRow { Key = "＿見出し＿" + header, Header = header });
        rows.AddRange(group);
    }

    private static ArmyNeedRow NeedRow(string key, string name, double need, double have, double fill, string tip, Func<double, string> fmt, string unit, double cost = 0)
        => new()
        {
            Key = key,
            Name = name,
            Need = fmt(need) + unit,
            Have = fmt(have) + unit,
            Fill = fill.ToString("P0"),
            FillV = fill,   // 節見出しの平均に使う実数（丸めた文字から平均を作らない）
            FillBrush = fill < FillWarnThreshold ? FactionStrengthDanger : FactionStrengthCalm,   // 既存パレット（国内勢力の強さ比と同じ2色）・閾値も既存（充足率の不足警告）
            Tip = tip,
            Cost = cost,
        };

    /// <summary>目標兵数（全国）＝Military.Step の desired の州別合計と同一式。
    /// 平時＝扶養限界の目標の州按分（(145)＝旧の軍事費駆動との混合 ArmySustainWeight は削除）、戦時＝徴兵の max 経路。
    /// ⚠エンジンの徴兵率は Military.Step のローカル変数で外から読めない＝ここで同じ式を組む（PolityMobilizationText と同じ作法）。
    /// 式を動かすときは Military.cs と両方を直す。</summary>
    private (double Target, double Now, double Sustain, double Manpower) SoldierTargetNow()
    {
        var c = _player.Coef;
        var dip = _sim.Diplomacy;
        var me = Me;
        bool atWar = false;
        foreach (var war in dip.Wars) if (war.A == me || war.B == me) { atWar = true; break; }
        double conscriptFrac = c.ConscriptionFraction;
        if (atWar && c.ConscriptionScalesWithWar > 0)
        {
            int fronts = 0; double enemyLand = 0;
            foreach (var war in dip.Wars)
            {
                var foe = war.A == me ? war.B : war.B == me ? war.A : null;
                if (foe is null) continue;
                fronts++; enemyLand += foe.LandPower;
            }
            double frontDepth = Math.Min(1.0, Math.Max(0, fronts - 1) * c.ConscriptionFrontStep);
            double ratioDepth = me.LandPower > 1e-9 ? Math.Clamp(enemyLand / me.LandPower - 1.0, 0, 1) : 0;
            double depth = Math.Clamp(c.ConscriptionExhaustionWeight * me.WarExhaustion + frontDepth + c.ConscriptionEnemyRatioWeight * ratioDepth, 0, 1);
            conscriptFrac = c.ConscriptionFloorFraction + (c.ConscriptionFraction - c.ConscriptionFloorFraction) * depth;
        }
        if (atWar && _player.Mobilized) conscriptFrac = Math.Max(conscriptFrac, c.MobilizationConscriptFrac);
        if (c.NationBuildMobilizationWeight > 0)
        {
            double ncGate = c.NationBuildMobilizationWeight * (1.0 - Math.Max(0, _player.NationalConsciousness));
            ncGate *= Math.Max(0, 1.0 - c.SkillMobilizationRelief * Math.Clamp(_player.ArmySkill, 0, 1));
            conscriptFrac *= Math.Max(Military.MobilizationGateFloor, 1.0 - ncGate);
        }
        bool wartimeConscript = atWar && _player.ConscriptionLaw >= ConscriptionLaw.Selective;   // Military.cs:187 と同式（M1 2026-09-19＝総動員は志願兵制を迂回しない）
        double lawFrac = _player.ConscriptionLaw == ConscriptionLaw.Universal ? c.UniversalConscriptionFactor : 1.0;
        double sustainTargetNat = Military.EstablishmentOf(_player, c, out double natMan);   // (155) 平時の目標＝定員（制度が開く席の数）
        double total = 0, now = 0, manTotal = 0;
        foreach (var rg in _player.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            double manpower = Military.RegionManpower(rg, c);   // Military.Step の兵役の母数と同じ1本（働き盛りの男・兵士も同じ単位・支配度の届き方）＝兵士POPの無い州は0
            if (!(manpower > 0)) continue;   // 兵士POPの無い州は徴募の対象外（Military.Step の soldier==null と同じ）
            double sld = 0; foreach (var p in rg.Pops) if (p.IsSoldier) sld += p.Size;
            now += sld; manTotal += manpower;
            double desired = natMan > 1e-9 ? sustainTargetNat * (manpower / natMan) : 0;
            if (wartimeConscript) desired = Math.Max(desired, conscriptFrac * lawFrac * manpower);
            total += desired;
        }
        return (total, now, sustainTargetNat, manTotal);
    }
}
