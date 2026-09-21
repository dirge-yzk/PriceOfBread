using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

/// <summary>法律の1段階（横並びボタンの1つ・1934拡張E8）。現行/目標のハイライトは親 LawRowVM の状態から導出。</summary>
public sealed partial class LawStageVM : ObservableObject
{
    public LawRowVM Row { get; }
    /// <summary>この段の**エンジン側の段番号**（法のenum序数／財政の段 0..3／軍事費の段 1..4）。ラベル配列の添字ではなく
    /// 親の levelBase ぶん進む＝Government.LawPrereq/GateInfo にそのまま渡せる値（軍事費に「なし」が無いのを写す）。</summary>
    public int Index { get; }
    public string Label { get; }
    public LawStageVM(LawRowVM row, int index, string label) { Row = row; Index = index; Label = label; }
    public bool IsCurrent => Row.CurrentLevel == Index;   // 現在施行中の段階（濃い塗り）
    public bool IsTarget => Row.TargetLevel == Index && Row.TargetLevel != Row.CurrentLevel;   // プレイヤーが置いた目標（枠強調）
    public bool Enabled => Row.CanReachAndAfford(Index);  // 前提法が未達 or 政治資本が足りない段階はグレーアウト（押せない）
    public string Hint => Row.StageHint(Index);           // ホバー：必要な政治資本／不可の理由
    internal void Notify() { OnPropertyChanged(nameof(IsCurrent)); OnPropertyChanged(nameof(IsTarget)); OnPropertyChanged(nameof(Enabled)); OnPropertyChanged(nameof(Hint)); }
}

/// <summary>法律1本の行（1934拡張E8）。段階を横並びボタンで出し、改正可能なら発光＋政治資本コスト、不可なら理由を Detail に出す。
/// 施行は Government.StepEnum/StepBool の政治ゲート（1期1段ずつ）。GateInfo で「次の一歩」の可否・コスト・理由を副作用なしに引く。</summary>
public sealed partial class LawRowVM : ObservableObject
{
    private readonly World _w;
    private readonly PolicyAxis? _axis;   // BuildLaws の全呼び出しが値を渡す＝常に非null
    private readonly Func<int> _current;   // 施行中の段階（World の現在値）
    private readonly Func<int> _target;    // プレイヤーが置いた目標（Policy）
    private readonly Action<int> _setTarget;
    private readonly Func<bool> _visible;
    private readonly int _levelBase;   // 最下段のエンジン段番号（既定0）。軍事費だけ1＝「なし」の段が存在しない（Government.LawPrereq が段0を拒む）

    public string Title { get; }
    /// <summary>この行の直前に出す群見出し（2026-08-13・法の棚卸しdoc L1「A案」）。空なら見出しを出さない。
    /// 21本の法をフラットに並べていたので、探すのに全部読む必要があった。並び順と挙動は1行も変えず、
    /// **見出しを挟むだけ**＝SimCore に一切触れない（帯リスク0）。</summary>
    public string GroupHeader { get; set; } = "";
    public bool HasGroupHeader => GroupHeader.Length > 0;
    public IReadOnlyList<LawStageVM> Stages { get; }

    /// <param name="levelBase">最下段のエンジン段番号。既定0＝ラベルの添字がそのまま段番号（従来の全行）。
    /// 軍事費だけ1＝「少額」がエンジンの段1（R2「軍事費に『なし』は無い」＝Government.LawPrereq が段0を拒む）。</param>
    public LawRowVM(World w, PolicyAxis? axis, string title, string[] labels,
        Func<int> current, Func<int> target, Action<int> setTarget, Func<bool>? visible = null, int levelBase = 0)
    {
        _w = w; _axis = axis; Title = title;
        _current = current; _target = target; _setTarget = setTarget;
        _visible = visible ?? (() => true);
        _levelBase = levelBase;
        var st = new LawStageVM[labels.Length];
        for (int i = 0; i < labels.Length; i++) st[i] = new LawStageVM(this, levelBase + i, labels[i]);
        Stages = st;
        Refresh();
    }

    public int CurrentLevel => _current();
    public int TargetLevel => _target();
    public bool Visible => _visible();

    /// <summary>その段階を制定できるか（前提法DAG）。現行以下（降格）は常に可。前提未達の上位段はグレーアウト。</summary>
    public bool CanReach(int level)
    {
        if (level <= CurrentLevel) return true;
        return Government.LawPrereq(_w, _axis.Value, level, _w.Coef).Ok;
    }

    // その段へ動く「次の一歩」(cur→cur±1)の情報（GateInfoは副作用なし）。降格方向にも純支持コストが要りうる。
    private (bool ok, double cost, string reason) NextStep(int level)
    {
        if (level == CurrentLevel) return (true, 0, "");
        bool inc = level > CurrentLevel;
        int next = CurrentLevel + (inc ? 1 : -1);
        return Government.GateInfo(_w, _axis.Value, inc, next);
    }

    /// <summary>制定できるか。前提法DAG＋純支持ゲート。政治資本の上限は廃止（2026-07-26）＝コストは常に払える（青天井に積み・正統性で贖う）＝AIと同一条件。</summary>
    public bool CanReachAndAfford(int level)
    {
        if (level == CurrentLevel) return true;   // 現行段は常に選べる（no-op）
        if (level > CurrentLevel && !CanReach(level)) return false;   // 前提法DAG
        var (ok, _, _) = NextStep(level);
        return ok;                                // 純支持ゲート（議会の反対等）のみ
    }

    /// <summary>段ボタンのホバー文。1行目=「法律名:段階名」（全分岐共通・作者指示2026-08-15）、
    /// 2行目=「現在の段階」／「制定:可能（政治資本消費:n）」／「制定:不可（…）」、以降=勢力の賛否。データのみ＝規律準拠。</summary>
    public string StageHint(int level)
    {
        string head = $"{Title}:{Stages[level - _levelBase].Label}";   // どの段のホバーかをホバー自身が名乗る（段番号→ラベル添字）
        if (level == CurrentLevel) return head + "\n現在の段階";
        if (level > CurrentLevel && !CanReach(level))
            return head + "\n制定:不可\n" + FormatPrereq(Government.LawPrereq(_w, _axis!.Value, level, _w.Coef).Reason);
        var (ok, cost, reason) = NextStep(level);
        // ★下げる側の理由も出す（2026-09-12 反証）：裁定Bで**床**（緊縮の下限・戦争中は軍を解散できない・軍事費に「なし」は無い）が
        //   Government.LawPrereq へ移り、床は**下げる向きにしか現れない**。上の分岐は level>CurrentLevel だけを見るので、
        //   下げるボタンのホバーが「政治資本消費:0　不足」＝**政治資本の問題だと嘘をつく**（政治資本をいくら積んでも開かない門なのに）。
        //   GateInfo は前提で止めたとき理由をそのまま返す＝その1本を出すだけ（UIに第二の判定式は置かない）。
        //   ★N2（2026-09-21）：在庫の門の否決（ReasonNoCapitalStock）も同じ扱い＝ここで早期returnせず、下の「必要量＋不足」と勢力の賛否を出す。
        //   文字列は Government の const が単一ソース（生成側と比較側で散らせない＝2026-08-22 の作者指示が壊れる事故の型）。
        if (!ok && reason.Length > 0 && reason != Government.ReasonNoMandate && reason != Government.ReasonNoCapitalStock)
            return head + "\n制定:不可\n" + FormatPrereq(reason);
        // ★2026-08-02（作者指摘「政治資本の消費なしと政治資本0の差がわからない」）：**常に同じ書式**にまとめる。
        //   0でも「政治資本消費:0」と出せば、0という値なのか対象外なのかで迷わない。
        //   ⚠否決のときも**必要量を出す**（作者指摘「政治資本がいくつ必要か知りたい」）＝同じ数字に「・不足」を付けるだけ。
        //   前提法が足りない等、政治資本では解けない理由は上の2分岐で「制定:不可」＋前提行として処理済み。
        //   併せて**誰がどの程度望んでいるか**を添える（賛成が強い順・NetSupport と同じ項の分解＝合計は一致）。
        var body = new System.Text.StringBuilder(head);
        body.Append('\n').Append(ok ? $"制定:可能（政治資本消費:{cost * 100:F0}）" : $"制定:不可（政治資本消費:{cost * 100:F0}　不足）");
        var parts = PolicyPreferences.SupportBreakdown(_w, _axis.Value, level > CurrentLevel);
        foreach (var (who, wgt) in parts)
        {
            if (System.Math.Abs(wgt) < 0.005) continue;   // 誤差同然の項は出さない
            // ⚠符号だけだと何の増減か読めない（作者指摘「近代エリート +24 ←何がプラスされるかわからない」）＝
            //   賛成/反対を語で示し、数字はその勢力の**押しの強さ**。書式は他行と同じ「◯◯:△△」コロン形式（作者指示2026-08-15）。
            body.Append('\n').Append(who).Append(wgt > 0 ? ":賛成 " : ":反対 ").Append(System.Math.Abs(wgt * 100).ToString("F0"));
        }
        return body.ToString();
    }

    /// <summary>前提未達の理由文を「◯◯:△△」レイアウトへ整形（作者指示2026-08-15）。
    /// 「◯◯（△△）が前提」→「前提 ◯◯:△△」のパターン変換のみ。合わない文（検地台帳が前提・プロレタリア独裁のみ 等）は
    /// そのまま返す＝安全側。整形は表示側（本VM）の責務＝Government.cs には触れない。</summary>
    private static string FormatPrereq(string reason)
    {
        const string tail = "）が前提";
        int open = reason.IndexOf('（');
        if (open > 0 && reason.EndsWith(tail, StringComparison.Ordinal) && open + 1 < reason.Length - tail.Length)
        {
            string state = reason[(open + 1)..^tail.Length];
            if (!state.Contains('（') && !state.Contains('）'))   // 入れ子括弧は変換対象外（誤切り出しを避ける）
                return $"前提 {reason[..open]}:{state}";
        }
        return reason;
    }

    /// <summary>段階ボタンのクリック＝目標をその段へ置く（施行は次tick以降、政治ゲート越しに1段ずつ進む）。</summary>
    [RelayCommand]
    private void Select(int index) { _setTarget(index); Refresh(); }

    public bool Amendable { get; private set; }   // 改正可能（次の一歩が通る）＝行を発光させる
    public string Detail { get; private set; } = "";   // 現行／改正可能／政治資本 xx%／不可の理由

    /// <summary>現行・目標・GateInfo を再評価して表示を更新（毎tick＋クリック時に呼ぶ）。副作用なし。</summary>
    public void Refresh()
    {
        int cur = CurrentLevel, tgt = TargetLevel;
        if (cur == tgt)
        {
            Amendable = false; Detail = "";   // 変更なし＝現行段階のボタンが濃塗りで示す（重複表示しない）
        }
        else
        {
            bool inc = tgt > cur;
            int next = cur + (inc ? 1 : -1);
            var (ok, cost, reason) = Government.GateInfo(_w, _axis.Value, inc, next);
            Amendable = ok;
            // ★「改正可能」の語は出さない（作者指示2026-08-11）＝押せることは目標段の金枠とボタンの有効/無効が示しており、
            //   語を足しても情報が増えない。残すのは**数字（必要な政治資本）と、押せない理由**だけ。
            // ★作者指示2026-08-22「法律タブから『政治資本届かず否決』の文字を削除」＝この定型句は行に出さない。
            //   押せないことは目標段の枠とボタンの無効が示しており、必要な政治資本の数字はホバー（StageHint＝「政治資本消費:n・不足」）にある。
            //   ⚠他の理由（前提法・政体の制約など）はそのまま出す＝消すのはこの定型句だけ。
            //   ★N2（2026-09-21）：在庫の門の否決も同じ定型句扱い＝行には出さない（必要な政治資本はホバーの数字・残り a/b は政治タブの行が出す）。
            Detail = ok ? (cost > 0.001 ? $"政治資本 {cost * 100:F0}" : "")
                        : (reason == Government.ReasonNoMandate || reason == Government.ReasonNoCapitalStock ? "" : reason);
        }
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(TargetLevel));
        OnPropertyChanged(nameof(Amendable));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(Visible));
        foreach (var s in Stages) s.Notify();
    }
}
