using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

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
    /// <summary>ホバー：必要な政治資本／不可の理由。**空なら null**＝Avalonia の ToolTip.Tip は null なら吹き出しを出さないが空文字では空の吹き出しを出す。
    /// 軸の無い行（財政のレバーの5段）は代価も賛否も無く null（<see cref="StageGate.Hint"/> が最初から null を返す＝ここで畳まない）。</summary>
    public string? Hint => Row.StageHint(Index);
    internal void Notify() { OnPropertyChanged(nameof(IsCurrent)); OnPropertyChanged(nameof(IsTarget)); OnPropertyChanged(nameof(Enabled)); OnPropertyChanged(nameof(Hint)); }
}

/// <summary>法律1本の行（1934拡張E8）。段階を横並びボタンで出し、改正可能なら発光＋政治資本コスト、不可なら理由を Detail に出す。
/// 施行は Government.StepEnum/StepBool の政治ゲート（1期1段ずつ）。GateInfo で「次の一歩」の可否・コスト・理由を副作用なしに引く。</summary>
public sealed partial class LawRowVM : ObservableObject
{
    private readonly World _w;
    private readonly PolicyAxis? _axis;   // null＝**政治軸を持たない行**（財政タブのレバーの5段＝行政費/近代化投資/関税/政策金利・2026-09-23）。議会を通らないので門も代価も賛否も無い
    private readonly Func<int> _current;   // 施行中の段階（World の現在値）
    private readonly Func<int> _target;    // プレイヤーが置いた目標（Policy）
    private readonly Action<int> _setTarget;
    private readonly Func<bool> _visible;
    private readonly int _levelBase;   // 最下段のエンジン段番号（既定0）。軍事費だけ1＝「なし」の段が存在しない（Government.LawPrereq が段0を拒む）

    private readonly string[] _labels;

    public string Title { get; }
    public IReadOnlyList<LawStageVM> Stages { get; }

    /// <param name="levelBase">最下段のエンジン段番号。既定0＝ラベルの添字がそのまま段番号（従来の全行）。
    /// 軍事費だけ1＝「少額」がエンジンの段1（R2「軍事費に『なし』は無い」＝Government.LawPrereq が段0を拒む）。</param>
    public LawRowVM(World w, PolicyAxis? axis, string title, string[] labels,
        Func<int> current, Func<int> target, Action<int> setTarget, Func<bool>? visible = null, int levelBase = 0)
    {
        _w = w; _axis = axis; Title = title; _labels = labels;
        _current = current; _target = target; _setTarget = setTarget;
        _visible = visible ?? (() => true);
        _levelBase = levelBase;
        var st = new LawStageVM[labels.Length];
        for (int i = 0; i < labels.Length; i++) st[i] = new LawStageVM(this, levelBase + i, labels[i]);
        Stages = st;
        Refresh();
    }

    /// <summary>本体の法の表（<see cref="LawDef"/>）の 1 行から作る。読み口と書き込み口は表のもの＝App に第二の式を置かない。</summary>
    public LawRowVM(World w, LawDef def)
        : this(w, def.Axis, def.Name, def.Stages, () => def.Current(w), () => def.Target(w), v => def.SetTarget(w.Policy, v), () => def.IsVisible(w), def.LevelBase) { }

    /// <summary>本体の財政の項の表（<see cref="FiscalStage"/>）の 1 行から作る（税・教育費・軍事費の段）。</summary>
    public LawRowVM(World w, FiscalStage st)
        : this(w, st.Axis, st.Name, st.Stages, () => st.Current(w), () => st.Target(w), v => st.SetTarget(w.Policy, v), null, st.LevelBase) { }

    public int CurrentLevel => _current();
    public int TargetLevel => _target();
    public bool Visible => _visible();

    /// <summary>制定できるか。前提法DAG＋純支持ゲート＋政治資本の在庫（★2026-09-21 裁定S1：純支持が負の段は残り a が代価に届くときだけ押せる＝GateInfo が施行と同じ1本を読む）。式は <see cref="StageGate"/> の 1 本。</summary>
    public bool CanReachAndAfford(int level) => StageGate.CanReachAndAfford(_w, _axis, level, CurrentLevel);

    /// <summary>段ボタンのホバー文。1行目=「法律名:段階名」、2行目=「現在の段階」／「制定:可能（政治資本消費:n）」／「制定:不可（…）」、以降=勢力の賛否。軸の無い行は null。本文は <see cref="StageGate.Hint"/> の 1 本。</summary>
    public string? StageHint(int level) => StageGate.Hint(_w, _axis, Title, _labels, _levelBase, level, CurrentLevel);

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
        else if (_axis is null)
        {
            // ★軸の無い行（レバーの段・2026-09-23）＝議会を通らないので代価も否決も無い。目標段の金枠だけが「置いた値と施行値の差」を語る
            //   （行政費・関税・政策金利はエンジンが PolicyBaseSpeed で目標へ寄る＝数四半期は現行段と目標段が別になる）。
            Amendable = false; Detail = "";
        }
        else
        {
            (Amendable, Detail) = LawView.Detail(_w, _axis, cur, tgt);   // 式と注は本体の目録（LawView.Detail）
        }
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(TargetLevel));
        OnPropertyChanged(nameof(Amendable));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(Visible));
        foreach (var s in Stages) s.Notify();
    }
}
