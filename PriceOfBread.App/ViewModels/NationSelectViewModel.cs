using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class NationSelectViewModel : ViewModelBase
{
    // おすすめ＝難易度の3列（簡単2／普通4／挑戦的1）。国・難易度・強さは SimCore.NationBrief が world.json と係数から導出
    // ＝ここに固定表を持たない（係数を直した日にラベルが取り残されない・docs/202608/おすすめ国家と難易度_2026-08-09_完了.md §3-3）。
    public ObservableCollection<NationChoice> Recommended { get; } = new();
    // その他の国＝おすすめ以外の全国。★常時表示（作者指示2026-08-11）＝開閉でおすすめ国の位置が動かない。
    public ObservableCollection<NationChoice> Others { get; } = new();

    private readonly Action<int, bool> _onSelect;

    /// <summary>観戦（選んだ国の判断も全て AI が行う）。カードを押した時点の値が開始に渡る。</summary>
    [ObservableProperty] private bool _spectate;

    public NationSelectViewModel(Action<int, bool> onSelect, bool spectate = false)
    {
        _onSelect = onSelect;
        _spectate = spectate;
        Fill();
    }

    private void Fill()
    {
        var all = WorldData.Nations();
        var coef = new Coefficients();
        var (rank, score, soldiers) = NationBrief.Opening(all, coef);   // 世界を1つ組んでエンジンに数えさせる(★(449) 助走を通すので実測 約6.5秒・開始時のランクと国力と兵の数は不変)＝難易度の安全の項も同じ世界の兵を読む
        var diff = NationBrief.Difficulties(all, coef, soldiers);
        var reco = new List<(NationChoice Card, double Ease)>();
        var others = new List<(NationChoice Card, double Score)>();
        for (int i = 0; i < all.Length; i++)
        {
            string name = all[i].Name, strength = rank.GetValueOrDefault(name, "");
            if (diff.TryGetValue(name, out var d))
                reco.Add((new NationChoice(i, name, idx => _onSelect(idx, Spectate), strength, d, NationBrief.Description(name)),
                          NationBrief.Ease(all[i], all, coef, soldiers)));
            else others.Add((new NationChoice(i, name, idx => _onSelect(idx, Spectate), strength), score.GetValueOrDefault(name)));
        }
        // その他の国＝国力の降順（作者指示2026-08-11）。同点は名前順＝決定論。
        others.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : string.CompareOrdinal(a.Card.Name, b.Card.Name));
        foreach (var (card, _) in others) Others.Add(card);
        // 易しい順に並べる＝難易度で列を分けなくても勾配が並びで伝わる(同点は名前順＝決定論)
        reco.Sort((a, b) => b.Ease != a.Ease ? b.Ease.CompareTo(a.Ease) : string.CompareOrdinal(a.Card.Name, b.Card.Name));
        foreach (var (card, _) in reco) Recommended.Add(card);
    }
}

public partial class NationChoice : ObservableObject
{
    private readonly Action<int> _select;
    public int Index { get; }
    public string Name { get; }
    /// <summary>おすすめカードの見出しの左側＝国名（強さ）。★難易度は <see cref="Difficulty"/> へ分離（作者指示2026-08-15）
    /// ＝見出しの右端に難易度色で出す（1本の文字列だと右寄せも色分けもできない）。</summary>
    public string Title { get; }
    /// <summary>難易度＝「簡単」「普通」「挑戦的」の3値（NationBrief.Difficulties）。おすすめ以外は空。</summary>
    public string Difficulty { get; }
    /// <summary>難易度の文字色（作者指示2026-08-15＝挑戦的は赤系・普通は中間色・簡単は緑系）。
    /// 3色ともアプリの既存パレット（警告赤 #A0432D・ニュース金 #8A6A20・ニュース緑 #2E7D32）＝パーチメント地でも沈まない濃さ。</summary>
    public IBrush DifficultyBrush { get; }
    private static readonly IBrush DiffEasy = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush DiffNormal = new SolidColorBrush(Color.Parse("#8A6A20"));
    private static readonly IBrush DiffHard = new SolidColorBrush(Color.Parse("#A0432D"));
    /// <summary>その他の国のボタン右端に置く強さ＝「（中堅）」。★名前と分けて持つ理由＝
    /// 1本の文字列にすると「サルデーニャ＝ピエモンテ（中堅）」が幅に入らず**強さの側から切れる**
    /// （右端の情報が消えるのに、切れていることが見えない）。分ければ長い名前だけが省略される。</summary>
    public string StrengthLabel { get; }
    /// <summary>カードに出す説明＝歴史的背景＋末尾にプレイフィール（おすすめ以外は空）。
    /// ★ホバーは置かない（作者指示2026-08-11）＝画面に出ている文章が全部。読むために操作を要求しない。</summary>
    public string Description { get; }

    public NationChoice(int index, string name, Action<int> select, string strength = "", string difficulty = "",
                        string description = "")
    {
        Index = index; Name = name; _select = select;
        Description = description;
        Title = difficulty.Length > 0 ? $"{name}({strength})" : name;
        Difficulty = difficulty;
        DifficultyBrush = difficulty switch { "簡単" => DiffEasy, "挑戦的" => DiffHard, _ => DiffNormal };
        StrengthLabel = strength.Length > 0 ? $"({strength})" : "";
    }

    [RelayCommand] private void Choose() => _select(Index);
}
