// GameViewModel.Production.cs ＝ 生産ページ（Qキー）＝サプライチェーンのノード/辺・財の表/設備の表/労働の表・詳細・着工/補助金/専売のレバー。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

// ===== 生産ページ（Qキー・(501) 段3＝旧の生産タブ〔TABキー・全画面〕を欄に分けた物）=====
// 素材（上）→加工財（下）の層状DAG（縦並び・逆ピラミッド＝作者指示2026-08-15。旧は左→右）。ノード＝財カード（世界価格比・工場の棟数と全国目標・稼働率・供給・生産量・過不足）、辺＝レシピ（材料→製品）。
// 財を選ぶと右パネルに全国の需給・首都価格と世界価格・用途の内訳と、州別の生産・過不足・材料価格・国営工場カウンター、補助金/専売/売買方針を出す。
// レイアウトは静的（初回に一度だけ計算）：基本レシピの最長経路で層（代替レシピは辺だけ） →層内は tier→50音順にソート → 幅予算(PRowBudgetW)を超える層は複数行へ分割。
public partial class GameViewModel
{
    // ★★巨大建造物の可視化（2026-08-02・作者指摘「巨大建築物がUI側にない」）。
    // ⚠ App 側の参照が**0件**で、エンジンには megaprojects.json の20件・技術ゲート・進捗・効果が揃っているのに
    //   プレイヤーは存在すら知れなかった（設計書も「最近点火した機構ほど見えない」と同じ穴を指摘していた）。
    // ★着工は動詞（StartMega）＝Megaprojects.TryStart を通す（AIの SelfPlay と同一経路）。可否と理由はボタンのツールチップで示す。
    [ObservableProperty] private List<MegaRow> _megaRows = new();
    public bool HasMegaRows => MegaRows.Count > 0;

    /// <summary>サプライチェーン図の節点（財カード）と辺（レシピ）。★2026-08-10：**器を作り直さない**。
    /// 旧＝毎回 List を組んで丸ごと差し替え＝ItemsControl は Reset を受けて全コンテナを破棄・再生成する
    /// （節点テンプレートは1枚あたり約8視覚要素＝約1000要素＋辺の Path 165本。しかも財をクリックするたび全部）。
    /// 集合も座標もレイアウトが静的（EnsureProdLayout）＝**不変**なので、器は一度だけ作り以後は値だけ流す。</summary>
    public ObservableCollection<ProdNode> ProdNodes { get; } = new();
    public ObservableCollection<ProdEdge> ProdEdges { get; } = new();
    /// <summary>tier（加工の深さ＝層）の区切り線（作者指示2026-08-15）＝層（元のDAG深さ）が変わる境目に引く盤幅いっぱいの水平1px。
    /// 同一層を複数行に分割した行間には引かない（層の境目のみ）。座標は <see cref="EnsureProdLayout"/> が一度だけ確定＝静的。</summary>
    public ObservableCollection<ProdTierLine> ProdTierLines { get; } = new();
    private Dictionary<int, ProdNode>? _nodeByGood;   // 節点の器（財id→インスタンス）。非nullなら器は構築済み
    [ObservableProperty] private ProdDetail? _prodDetail;
    /// <summary>生産ページの 3 つの表（財の表・設備の表・労働の表）。行と列は本体の目録（SimCore/Display/ProductionTables）が名乗り、共通の表（Controls/TableView）が描く。
    /// 値は盤の節点（財カード）と同じ 1 周（RefreshProduction）の集計から作る＝UI に第二の式を置かない。</summary>
    [ObservableProperty] private TableSnapshot _prodGoodTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _prodEquipTable = TableSnapshot.Empty;
    [ObservableProperty] private TableSnapshot _prodLaborTable = TableSnapshot.Empty;
    /// <summary>財の表で選んでいる行の鍵（財の番号。選んでいなければ null）。</summary>
    public string? ProdSelectedKey => _prodSelGood >= 0 ? _prodSelGood.ToString() : null;
    /// <summary>設備の欄を出すか（旗 <see cref="Coefficients.RecipeEquipment"/> が 0 の世界では欄ごと出さない）。</summary>
    [ObservableProperty] private bool _prodEquipmentOn = true;
    /// <summary>価格グラフ（対世界価格の比）の点列。★ProdDetail が null（財を未選択）でも**必ず非null**を返す＝
    /// Polyline が null を受けて毎フレーム例外を投げる描画クラッシュを構造的に閉じる。</summary>
    /// ★枠は常時表示（作者原則2026-08-10）＝標本が足りない開幕に枠ごと消えて下が跳ねない。点が0本なら何も描かれないだけ。
    public global::Avalonia.Points ProdSparkPoints => ProdDetail?.WorldSparkPoints ?? new();
    /// <summary>価格グラフの補助線（項目2・2026-08-10）＝枠に収まる段階とその逆数の水平線。中心線（比1.0）は常に y=32 なので XAML の1本。</summary>
    public Geometry ProdSparkGrid => ProdDetail?.SparkGrid ?? SparkGridEmpty;
    partial void OnProdDetailChanged(ProdDetail? value) { OnPropertyChanged(nameof(ProdSparkPoints)); OnPropertyChanged(nameof(ProdSparkGrid)); }
    [ObservableProperty] private double _prodCanvasW = 2200;
    [ObservableProperty] private double _prodCanvasH = 1200;
    private int _prodSelGood = -1;

    private const double PNodeW = 204, PNodeH = 56, PMargin = 24;
    // ★縦並び（作者指示2026-08-15）：層＝行（上＝素材・下＝最深加工財の逆ピラミッド）。
    //   行内の横間隔は小さく（カードが並ぶ）・行間は大きく（辺のベジェの通り道）。
    private const double PRowGapX = 16;
    // 縦の間隔（作者指示2026-08-16「縦に長すぎる」）：同一層の分割行間=28（詰める）・層の境目=40（区切り線とtierラベルの居場所）。
    private const double PRowGapY = 22, PLayerGapY = 32;   // ★2026-09-07 作者指示「行間を少し狭める」＝28→22／40→32（カードの幅と文字は不変）
    /// <summary>1行の幅予算の**既定**（盤の視野の幅がまだ分からない間・番人の基準）。(501) 段3 からは分割の枠の幅に盤が追従する＝実際の予算は <see cref="SetProdViewport"/> が視野の幅から導く
    /// （下の算術は旧の全画面〔右パネル 530 を除いた 1920〕の場合の値＝既定の予算と同じ）。
    /// 以下は旧の記録（作者指示2026-08-15＝盤の横スクロールを封じる）。旧 PMaxPerRow=8（枚数上限）を実効幅からの算出に置換。
    /// 算術＝想定幅1920 −右パネル530（ProductionTab.axaml の DockPanel.Dock=Right・枠込み）−縦スクロールバーの取り代12
    /// −盤の左右余白 PMargin×2(48) ＝ 1330。1行の最大枚数＝**6枚**（6×204＋5×16＝1304≦1330。カテゴリ余白 PCatGap 26 が
    /// 1つ入っても1330≦1330＝ちょうど収まる。2つ以上入る断片は EnsureProdLayout の実幅判定が分割数を1つ増やす＝枚数が減る。
    /// 7枚は 7×204＋6×16＝1524＞1330＝入らない）。
    /// ★2026-09-07に 172→204（作者要望(1)(2)）＝縦線を工場カウンターに拡張し、かつ100%表示が切れないための実幅を確保した。
    ///   カードの本文の可用幅＝204 −枠×2 −DockPanel余白5×2(10) −(工場カウンター**56**+隙間6) ＝
    ///   **129px**（枠1.5）／127.2px（入出力の強調＝枠2.4）／**126px**（選択中＝枠3.0）。
    ///   ⚠2026-09-23に実測して書き直した：旧コメントは「工場カウンター46」「可用幅139px／選択中136px」と書いていたが、
    ///     ProductionTab.axaml の縦線の実値は **56**（2026-09-08に民営/国営の2行を入れた時に46→56へ広げ、コメントだけ据え置かれた）
    ///     ＝10px ぶん本文の見積もりが甘く、下の「収まる」の判定が全部ずれていた。
    ///   ⚠2026-09-23（2度目）：幅は Yu Gothic UI（GameView.axaml の FontFamily）の hmtx から**字ごとに**積み直した。
    ///     旧の「半角は一律6.1」は誤り＝この書体の数字は**プロポーショナル**で、11pxでの送り幅は
    ///     漢字11.000／太字の 0=6.107・1=4.426・4=6.338（数字の最大）・+=7.638・/=4.549・%=9.244・半角スペース=3.024／
    ///     並字の 0=5.930・1=5.930・%=9.002・:=2.385。
    ///     旧コメントの「127.0px」「111.0px」「64.7px」はこの一律6.1から出た概算で、いずれも実寸より大きい。
    ///   ⚠2026-09-23（3度目）：「全角＝1.000em」も誤り＝**1.000em なのは漢字だけで、仮名は比例幅**。
    ///     11px 太字の実測（hmtx）＝ジ9.152・ガ8.809・イ8.008・モ8.395・ア8.744・コ7.831・ク8.459・ス8.567・ー7.251・ョ6.950
    ///     ＝0.63〜0.83em。この画面で幅を決めている文字列（稼働率・供給・民営・国営・生産・不足）は全部漢字なので上の算術は動かないが、
    ///     仮名を含む文字列（材名「ジャガイモ」42.30px・「チョコレート」46.34px＝1.000em 見積り55.00/66.00 に対し 8〜20px 狭い）を 1.000em で見積もると**実寸より広く**出る
    ///     ＝下の「収まる」の判定は安全側に外れているだけ（狭く見積もった箇所は無い）。
    ///     2行目「稼働率:100%　供給:100%」＝**124.35px**（並字）＝枠1.5でも**選択中126pxでも収まる**（旧コメントの1px超過は誤り）。
    ///     3行目「生産:1.2k」44.10＋隙間6＋「不足1000%」53.99＝**104.09px**＝どの枠でも収まる
    ///       （旧「過不足:-1000%」は72.06px＝合計122.16pxで、選択中126pxには収まっていたが枠2.4/1.5では余白がほぼ無かった。
    ///        2026-09-23の語の切替「余剰n%／不足n%」が18.07px詰めた）。
    ///   カウンター56の中身（太字）＝「民営100」**38.64px**・「民営10000」50.86px＝5桁まで収まる（2026-09-23に分母を落とした）。
    ///     国営は分母を残すので「国営12/12」47.61px・「国営99/99」50.99px までで、**「国営100/100」は59.83px＝3.83px超過**＝
    ///     3桁どうしになると末尾が無言で切れる（**現存する見切れ**）。縦線を広げて直すことはできない＝
    ///     本文が124.35px要るので縦線の上限は 204−枠3.0×2−余白5×2−隙間6−124.35＝**57.65px**＝1.65pxしか余地が無い。
    ///   ⚠旧（172px・縦線5）の可用幅は146pxで「稼働率:100%　材料供給:100%」＝149px＝**3px超過**＝末尾の「%」が無言で切れていた（作者報告(2)）。
    /// ★寸法で「収まる」を保証する＝ProductionTab.axaml 側の HorizontalScrollBarVisibility=Disabled とセット
    /// （スクロールバー任せにしない）。ウィンドウが1920より狭い時は盤の右端がクリップされる（作者許容2026-08-15）。</summary>
    private const double PRowBudgetW = 1920 - 530 - 12 - PMargin * 2;
    /// <summary>カテゴリの切れ目の余白（作者フィードバック2026-08-10 項目3）＝列の中で素材／主食／日用品／奢侈品／生産財が
    /// 切り替わる所に空ける縦の隙間。★見出しの文字は置かない＝並びと余白だけで区切りを示す（凡例の色が語る）。
    /// 行内間隔 PRowGapX(16) の1.6倍強＝「詰まった並び」と「切れ目」が一目で違う。</summary>
    private const double PCatGap = 26;
    private const double MaterialFillThreshold = 0.95;   // 材料供給がこれ未満＝ボトルネック/産出律速の表示
    // 価格グラフ（240×64のスパークライン）：中央線＝対世界価格の比1.0。上下それぞれ SparkHalfH に「枠」までを割り当てる。
    private const double SparkMidY = 32.0, SparkHalfH = 30.0;
    /// <summary>価格グラフの縦の段階集合（作者仕様2026-08-10 項目2）＝＋25%/＋50%/＋100%/＋200%/＋400%。
    /// 比は0未満にならないので**対数**で置く＝×b と ×(1/b)（−20%/−33%/−50%/−67%/−80%）が中心線から等距離に来る。
    /// 枠（縦の範囲）はこの集合の中から**データが収まる最小のもの**を選ぶ＝連続的に伸縮させない（目盛りが意味を持つ）。</summary>
    private static readonly double[] SparkBrackets = [1.25, 1.5, 2.0, 3.0, 5.0];
    private static readonly Geometry SparkGridEmpty = new StreamGeometry();   // 補助線の空実体（財を未選択でも Path.Data に非nullを渡す）

    // 節点色（tier別のアクセント＝縁と左帯・tier別の淡い地色＝最初から明るく判別できる）
    private static readonly IBrush PAccStaple  = new SolidColorBrush(Color.Parse("#3E7A4E"));
    private static readonly IBrush PAccDaily   = new SolidColorBrush(Color.Parse("#B8860B"));
    private static readonly IBrush PAccLuxury  = new SolidColorBrush(Color.Parse("#8A4A8A"));
    private static readonly IBrush PAccInter   = new SolidColorBrush(Color.Parse("#7A5A2E"));
    private static readonly IBrush PAccProducer= new SolidColorBrush(Color.Parse("#1F5FA8"));
    private static readonly IBrush PBgStaple   = new SolidColorBrush(Color.Parse("#E2EFE0"));
    private static readonly IBrush PBgDaily    = new SolidColorBrush(Color.Parse("#F3EBD2"));
    private static readonly IBrush PBgLuxury   = new SolidColorBrush(Color.Parse("#EFE1EF"));
    private static readonly IBrush PBgInter    = new SolidColorBrush(Color.Parse("#EDE4D2"));
    private static readonly IBrush PBgProducer = new SolidColorBrush(Color.Parse("#DEE9F6"));
    private static readonly IBrush PNodeBgSel  = new SolidColorBrush(Color.Parse("#C6DCF4"));
    // 供給が十分な財の地色（作者要望2026-09-07 (2)）＝tier色より濃い緑。主食の淡緑 PBgStaple(#E2EFE0) と紛れない濃さにする。
    private static readonly IBrush PBorderSel  = new SolidColorBrush(Color.Parse("#16478F"));
    private static readonly IBrush PBorderIn   = new SolidColorBrush(Color.Parse("#1F5FA8"));
    private static readonly IBrush PBorderOut  = new SolidColorBrush(Color.Parse("#A0432D"));
    private static readonly IBrush PEdgeDim    = new SolidColorBrush(Color.Parse("#9A8A66"));
    private static readonly IBrush PEdgeIn     = new SolidColorBrush(Color.Parse("#1F5FA8"));
    private static readonly IBrush PEdgeOut    = new SolidColorBrush(Color.Parse("#A0432D"));
    private static readonly IBrush PPlus       = new SolidColorBrush(Color.Parse("#1E7A3E"));
    private static readonly IBrush PMinus      = new SolidColorBrush(Color.Parse("#B02418"));
    private static readonly IBrush PDimText    = new SolidColorBrush(Color.Parse("#8A7A5E"));
    private static readonly IBrush PNameText   = new SolidColorBrush(Color.Parse("#2A2118"));   // 財名の通常色（ボトルネック時は PMinus 赤）
    private static readonly IBrush PUtilText   = new SolidColorBrush(Color.Parse("#5A4E3C"));   // 稼働率行の通常色（原料欠乏時は PMinus 赤）

    // 稼働率の色＝赤（0%）→黄（50%）→緑（100%）の連続（作者の指示 2026-11-04）。10% 刻みに量子化して筆を使い回す（毎月 数百枚のカードで筆を作らない）。
    private static readonly IBrush[] UtilBrushes = BuildUtilBrushes();
    private static IBrush[] BuildUtilBrushes()
    {
        static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
        var red = Color.Parse("#B02418"); var yellow = Color.Parse("#C99A1E"); var green = Color.Parse("#1E7A3E");
        var r = new IBrush[11];
        for (int i = 0; i <= 10; i++)
        {
            double t = i / 10.0;
            var (c0, c1, u) = t < 0.5 ? (red, yellow, t / 0.5) : (yellow, green, (t - 0.5) / 0.5);
            r[i] = new SolidColorBrush(Color.FromRgb(Lerp(c0.R, c1.R, u), Lerp(c0.G, c1.G, u), Lerp(c0.B, c1.B, u)));
        }
        return r;
    }
    /// <summary>稼働率（0〜1）→ 赤→黄→緑の筆。範囲外・NaN は赤側へ落とす（呼び手が 0〜1 に収める）。</summary>
    private static IBrush UtilColor(double util) => UtilBrushes[(int)Math.Round((!(util > 0) ? 0 : util < 1 ? util : 1) * 10)];
    // 技術不足の灰色（作者仕様2026-08-09 項目13）。tier色を捨てて灰に置き換える＝「今は作れない」を地色で一目に。凡例のヘッダーに同じ色の■技術不足を出す。
    private static readonly IBrush PBgTechShort = new SolidColorBrush(Color.Parse("#DCD8D2"));
    private static readonly IBrush PAccTechShort= new SolidColorBrush(Color.Parse("#9A948C"));
    // 世界価格との比較（作者仕様2026-08-09 項目3）：安い＝青・高い＝赤。州別の材料価格（項目11）も同じ規則に揃える。
    private static readonly IBrush PPriceBelow = new SolidColorBrush(Color.Parse("#1F5FA8"));
    private static readonly IBrush PPriceAbove = new SolidColorBrush(Color.Parse("#B02418"));

    private static IBrush TierAccent(GoodTier t) => t switch
    {
        GoodTier.Staple => PAccStaple,
        GoodTier.Daily => PAccDaily,
        GoodTier.Luxury => PAccLuxury,
        GoodTier.Producer => PAccProducer,
        _ => PAccInter,
    };

    private static IBrush TierBg(GoodTier t) => t switch
    {
        GoodTier.Staple => PBgStaple,
        GoodTier.Daily => PBgDaily,
        GoodTier.Luxury => PBgLuxury,
        GoodTier.Producer => PBgProducer,
        _ => PBgInter,
    };

    private static string TierName(GoodTier t) => ProductionTables.TierName(t);   // 層の名前は本体の目録の 1 本（財の表の群の見出しと同じ語）

    /// <summary>凡例の並び順（素材→主食→日用品→奢侈品→生産財）＝グラフの列の中の第1キー（作者フィードバック2026-08-10 項目2）。
    /// ★ヘッダーの凡例と**同じ順**にする＝色の並びと財の並びが一致する。役務はグラフに無いので最後（保険）。</summary>
    private static int TierOrder(GoodTier t) => t switch
    {
        GoodTier.Intermediate => 0,
        GoodTier.Staple => 1,
        GoodTier.Daily => 2,
        GoodTier.Luxury => 3,
        GoodTier.Producer => 4,
        _ => 5,
    };

    /// <summary>詳細パネルの見出し用のカテゴリ名（作者要望2026-09-07 (4)「素材は鉱石の場合『素材(普遍資源)』などとする」）。
    /// 素材（Intermediate）だけ**どこで採れるか**で割る＝エンジンが既に持つ3つの区別をそのまま読む（UIに分類表を書かない）：
    ///   ・遍在鉱物（<see cref="Catalog.IsMineral"/> かつ希少でない＝石炭・鉄鉱石…）＝どこでも少しは採れる → 「素材(普遍資源)」
    ///   ・希少鉱物（<see cref="Catalog.IsScarceMineral(int, Coefficients)"/>＝硫黄・金・銀…）＝産地でしか採れない → 「素材(産地限定)」
    ///   ・気候ゲート財（<see cref="Catalog.IsColonial"/>＝茶葉・生ゴム…）＝賦存州でしか産まない → 「素材(気候限定)」
    /// ⚠<see cref="TierName"/> は**触らない**＝用途一覧の先頭行の名前（＝SyncRows のキー）に使われており、
    ///   変えると行が毎更新で作り直される。見出し専用のここだけを増やす。</summary>
    private string TierLabel(int gid)
    {
        var t = Catalog.Goods[gid].Tier;
        if (t != GoodTier.Intermediate) return TierName(t);
        if (Catalog.IsScarceMineral(gid, _player.Coef)) return "素材(産地限定)";
        if (Catalog.IsMineral(gid)) return "素材(普遍資源)";
        if (Catalog.IsColonial(gid)) return "素材(気候限定)";
        return "素材";
    }

    /// <summary>用途の先頭行のラベル（作者フィードバック2026-08-10 項目6）＝popが**そのまま消費する分**のカテゴリ名。
    /// 主食だけ「食料」と呼ぶ（用途としては「食べる」＝凡例の語 主食 は財の分類名で、用途の名前ではない）。</summary>
    private static string UseTierName(GoodTier t) => t == GoodTier.Staple ? "食料" : TierName(t);

    /// <summary>★2026-08-02：UIスレッドを塞いでいる場所を**実行時に計測**する（作者報告「生産タブがフリーズし
    /// あらゆる操作を受け付けない」）。静的読解では特定できず（4レンズの監査で6件すべて反証）、crash.log にも例外が無いため、
    /// 「どこで何ミリ秒使ったか」を残すのが最短。⚠閾値超えのときだけ書く＝平常時はログを汚さない。</summary>
    internal static void TimeIt(string what, Action body)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        body();
        sw.Stop();
        if (sw.ElapsedMilliseconds >= 120) BootLog.Write($"⏱ {what} {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>右パネルの閉じるボタン（作者指示2026-08-11）＝詳細だけを畳む。生産画面は開いたまま。</summary>
    [RelayCommand] private void CloseProdDetail() { if (_prodSelGood >= 0) SelectProdGood(_prodSelGood); }

    /// <summary>ノードのクリック＝選択（同じ財の再クリックで解除）。ホバーでは変えない＝クリック時だけ再構築（軽量化）。View の PointerPressed から呼ばれる。
    /// ★選択時の自動スクロール（2026-08-02の ScrollGraphToX）は**廃止**（作者仕様2026-08-09 項目12）＝
    ///   財を選ぶたびに視界が飛ぶと、隣のノードを続けて押せない。視点の移動はドラッグとスクロールバーだけが動かす。</summary>
    public void SelectProdGood(int gid)
    {
        if (gid >= 0 && _pLayout is { } m && !m.ContainsKey(gid)) return;   // グラフに無い財（役務）＝選択不能（レビュー2026-07-19の防御）
        _prodSelGood = _prodSelGood == gid ? -1 : gid;
        // ★2026-08-10：選択は**集計を1つも動かさない**（全国の州×生産者の走査・技術不足の収束計算は前回のまま）。
        //   変わるのは節点の枠/地色/沈み具合・辺の強調・右パネルの中身だけ＝そこだけ塗り直す。
        //   旧は選択のたびに RefreshProduction 全体（実測145〜284ms）を回していた。
        if (_nodeByGood is null || _sProd is null) { TimeIt($"財の選択 gid={gid}", RefreshProduction); return; }
        TimeIt($"財の選択 gid={gid}", () => { ApplyProdSelection(); RefreshProdDetail(); });
    }

    /// <summary>選択に依存する見た目だけを流す（節点＝地色・枠・沈み／辺＝色・太さ・不透明度・重なり）。
    /// 集計に依存する項（品名の色・稼働率・生産量・過不足・工場の棟数）は触らない＝RefreshProduction が唯一の書き手。
    /// 技術不足（blocked）と休止（inactive）は集計の産物なので、直近の値を配列で持ち越す。</summary>
    private void ApplyProdSelection()
    {
        OnPropertyChanged(nameof(ProdSelectedKey));   // 財の表の選んでいる行
        if (_nodeByGood is null || _pLayout is null) return;
        int sel = _prodSelGood;
        var selIn = sel >= 0 && _pLayout.TryGetValue(sel, out var sn) ? sn.In : (IReadOnlyList<int>)Array.Empty<int>();
        var selOut = sel >= 0 && _pLayout.TryGetValue(sel, out var sn2) ? sn2.Out : (IReadOnlyList<int>)Array.Empty<int>();
        foreach (var (gi, nd) in _nodeByGood)
        {
            bool blocked = _sBlocked is { } bl && gi < bl.Length && bl[gi];
            bool inactive = _sInactive is { } ia && gi < ia.Length && ia[gi];
            bool isSel = gi == sel, isIn = selIn.Contains(gi), isOut = selOut.Contains(gi);
            double op = inactive ? 0.5 : 1.0;
            if (sel >= 0 && !isSel && !isIn && !isOut) op = Math.Min(op, 0.6);   // 選択時のみ無関係を沈める（非選択の初期状態は全て明るい）
            nd.Background = isSel ? PNodeBgSel : blocked ? PBgTechShort : TierBg(Catalog.Goods[gi].Tier);
            nd.BorderBrush = isSel ? PBorderSel : isIn ? PBorderIn : isOut ? PBorderOut : blocked ? PAccTechShort : TierAccent(Catalog.Goods[gi].Tier);
            nd.BorderThickness = isSel ? 3.0 : (isIn || isOut) ? 2.4 : 1.5;
            nd.Opacity = op;
        }
        // 選択財の入辺=青・出辺=赤・他は薄く。強調した辺を上に描く手段は**並べ替えでなく Z**（並べ替えは器の作り直しと同じ代償を払う）。
        foreach (var e in ProdEdges)
        {
            bool eIn = e.To == sel;      // 選択財へ入る辺＝材料
            bool eOut = e.From == sel;   // 選択財から出る辺＝加工先
            e.Stroke = eIn ? PEdgeIn : eOut ? PEdgeOut : PEdgeDim;
            e.Thickness = eIn || eOut ? 2.4 : 1.3;
            e.Opacity = eIn || eOut ? 1.0 : sel >= 0 ? 0.12 : 0.42;
            e.Z = eIn || eOut ? 1 : 0;
        }
    }

    // ---- 静的レイアウト ----
    private sealed class PNode
    {
        public int Id;
        public int Layer = -1;                      // -1=未計算・-2=計算中（循環ガード）
        public double X, Y;
        public readonly List<int> In = new(), Out = new();
    }
    private Dictionary<int, PNode>? _pLayout;
    // 材名の並び替えは全て50音順（作者指示2026-07-21）＝図の層内ソートと一覧表の「材名」列で同じ比較子を使う。
    private static readonly StringComparer JaNameComparer = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("ja-JP"), ignoreCase: true);

    private void EnsureProdLayout()
    {
        if (_pLayout is not null) return;
        var map = new Dictionary<int, PNode>();
        foreach (var d in Catalog.Goods)
            if (d.Tier != GoodTier.Service) map[d.Id] = new PNode { Id = d.Id };
        var seen = new HashSet<(int, int)>();
        void AddEdge(int from, int to)
        {
            if (from == to || !map.TryGetValue(from, out var src) || !map.TryGetValue(to, out var dst)) return;
            if (!seen.Add((from, to))) return;
            src.Out.Add(to); dst.In.Add(from);
        }
        foreach (var d in Catalog.Goods)
            foreach (var inp in d.Inputs) AddEdge(inp.GoodId, d.Id);
        // 代替レシピ（石油火力＝原油→電力・魚の缶詰＝漁獲→缶詰・ぼろ布製紙＝麻/綿布→紙…）の辺も合流させる
        // ＝用途内訳・従業員・ハイライトは全生産者を集計するため、グラフの辺も実レシピと揃える。労働・役務は map に無いので自然に除外。
        // ★全国民の生産者から取る（自国の保有州に依存しない静的グラフにする）＝征服・州昇格で後から加わる代替レシピの辺も初回表示から出る。
        foreach (var n in _sim.Nations)
            foreach (var rg in n.World.Regions)
                foreach (var p in rg.Producers)
                    foreach (var inp in p.Inputs) AddEdge(inp.GoodId, p.GoodId);
        // 層＝**基本レシピ**（Catalog.Goods の材）だけで数えた加工の深さ。代替レシピの辺は描くが層には数えない
        //   ＝全レシピの最長経路だと、採取財の機械化レシピ（鉱山＝機械→原油）や合成染料・電解ソーダ灰を伝って衣料が tier9 になっていた（作者指摘 2026-11-04）。
        //   代替レシピの辺は上向き・同じ層の向きにもなる（描き方は ApplyProdPlacement）。
        int LayerOf(PNode n)
        {
            if (n.Layer >= 0) return n.Layer;
            if (n.Layer == -2) return 0;            // 循環ガード（レシピは非循環のはず＝保険）
            n.Layer = -2;
            int l = 0;
            foreach (var i in Catalog.Goods[n.Id].Inputs)
                if (i.GoodId != n.Id && map.TryGetValue(i.GoodId, out var src)) l = Math.Max(l, LayerOf(src) + 1);
            n.Layer = l;
            return l;
        }
        foreach (var n in map.Values) LayerOf(n);

        // 層ごとの列。各層を**カテゴリ→50音順**にソート（作者フィードバック2026-08-10 項目2）。
        // 第1キー＝凡例と同じカテゴリの並び（素材→主食→日用品→奢侈品→生産財）／第2キー＝50音
        // （2026-07-21ユーザー指示の「財を探しやすい50音順」はカテゴリの中で保つ）。
        var nameOf = new Dictionary<int, string>();
        foreach (var d in Catalog.Goods) if (d.Tier != GoodTier.Service) nameOf[d.Id] = d.Name;
        var layers = map.Values.GroupBy(n => n.Layer).OrderBy(g => g.Key)
                        .Select(g => g.OrderBy(n => TierOrder(Catalog.Goods[n.Id].Tier))
                                      .ThenBy(n => nameOf.TryGetValue(n.Id, out var nm) ? nm : "", JaNameComparer).ToList()).ToList();
        _pLayers = layers;
        _pLayout = map;
        PlaceProd(_prodRowBudget);
    }

    private List<List<PNode>>? _pLayers;
    private double _prodRowBudget = PRowBudgetW;   // 盤の視野の幅から導いた 1 行の幅予算（既定＝旧の全画面の想定幅）
    private string _prodPlaceSig = "";             // 直近に置いた並びの署名（同じ並びなら座標を触らない）

    /// <summary>盤の座標を、1 行の幅予算 <paramref name="budget"/> に合わせて置く（層ごとに、全ての断片が予算に収まる最小の分割数）。幅予算は盤の視野の幅から来る＝分割の枠の幅に盤が追従する（旧は全画面の幅 1330 で固定）。
    /// 並び（各行に入る財）と盤の幅が前回と同じなら何も触らず false を返す。変わったら節点・区切り線・盤の大きさを置き直して true（器があれば呼び手が座標と辺の形を流す）。</summary>
    private bool PlaceProd(double budget)
    {
        var layers = _pLayers!;
        // ★縦並び（作者指示2026-08-15）：層＝**行**として上から座標を振る（上＝素材・下＝最深加工財）。
        //   太い層（素材41本）は複数行へ分割。各行は盤の幅の中で**横中央揃え**＝深い層ほど財が少なく行が短い＝逆ピラミッド。
        // 行の幅＝節点とその間隔に、カテゴリの切れ目の余白（項目3）を足したもの。
        static bool CatBreak(PNode a, PNode b) => Catalog.Goods[a.Id].Tier != Catalog.Goods[b.Id].Tier;
        static double RowWidth(List<PNode> sc)
        {
            double wd = sc.Count * PNodeW + (sc.Count - 1) * PRowGapX;
            for (int i = 1; i < sc.Count; i++) if (CatBreak(sc[i - 1], sc[i])) wd += PCatGap;
            return wd;
        }
        // 分割数＝均等割りを保ったまま、全ての断片が幅予算 PRowBudgetW に収まる**最小値**（作者指示2026-08-15＝横スクロールなし）。
        // 枚数でなく**実幅**で判定する＝カテゴリ余白 PCatGap の入り方が断片ごとに違っても予算超過を漏らさない。
        // 断片1枚（PNodeW=204）は常に予算内＝parts=row.Count で必ず fits＝ループは有限。
        // LayerEnd＝層（元のDAG深さ）の**最終行**＝tier区切り線を引く位置。同一層の分割行間には引かない（作者指示2026-08-15）。
        var screenRows = new List<(List<PNode> Nodes, bool LayerEnd)>();
        foreach (var row in layers)
        {
            int parts = 1, per;
            for (; ; parts++)
            {
                per = (row.Count + parts - 1) / parts;
                bool fits = true;
                for (int p = 0; p < parts && fits; p++)
                {
                    var sc = row.Skip(p * per).Take(per).ToList();
                    if (sc.Count > 1 && RowWidth(sc) > budget) fits = false;
                }
                if (fits) break;
            }
            for (int p = 0; p < parts; p++)
            {
                var sc = row.Skip(p * per).Take(per).ToList();
                if (sc.Count > 0) screenRows.Add((sc, (p + 1) * per >= row.Count));
            }
        }
        double maxW = 0;
        foreach (var (sc, _) in screenRows) maxW = Math.Max(maxW, RowWidth(sc));
        // 並びと幅が前回と同じなら座標は同じ＝何も触らない（視野の幅が少し動くたびに器を流し直さない）
        string sig = string.Join(";", screenRows.Select(r => string.Join(",", r.Nodes.Select(n => n.Id)))) + "|" + maxW.ToString("R");
        if (sig == _prodPlaceSig) return false;
        _prodPlaceSig = sig;
        double y = PMargin;
        ProdTierLines.Clear();
        int tier = 1;   // tier番号＝加工の深さ（上=素材=tier1・下ほど数字が大きい=作るのが難しい。作者指示2026-08-16）
        ProdTierLines.Add(new ProdTierLine { X = 0, Y = -1, W = 0, Label = "tier1", LabelX = PMargin, LabelY = 6 });   // 最上層は区切り線なし＝ラベルのみ（上余白24の中）
        for (int r = 0; r < screenRows.Count; r++)
        {
            var (sc, layerEnd) = screenRows[r];
            double x = PMargin + (maxW - RowWidth(sc)) / 2;
            for (int i = 0; i < sc.Count; i++)
            {
                if (i > 0) x += PNodeW + PRowGapX + (CatBreak(sc[i - 1], sc[i]) ? PCatGap : 0);
                sc[i].X = x; sc[i].Y = y;
            }
            bool lastRow = r == screenRows.Count - 1;
            double gap = layerEnd && !lastRow ? PLayerGapY : PRowGapY;   // 層の境目だけ広く（線とラベルの居場所）
            y += PNodeH + gap;
            // tier区切り線（作者指示2026-08-15）＝層の最終行の下・層間 PLayerGapY の中央に盤幅いっぱいの水平1px＋次tierのラベル（線の直上・左端）。
            // 最下層の下には引かない（その先は巨大建造物の帯＝層の境目ではない）。座標はここで確定＝静的（以後は触らない）。
            if (layerEnd && !lastRow)
            {
                tier++;
                double lineY = y - PLayerGapY / 2;
                ProdTierLines.Add(new ProdTierLine { X = 0, Y = lineY, W = maxW + PMargin * 2, Label = $"tier{tier}", LabelX = PMargin, LabelY = lineY - 15 });
            }
        }
        ProdCanvasW = maxW + PMargin * 2;
        // (501) 段3：巨大建造物は盤の下端の帯でなく、生産ページの自分の欄へ移した＝盤は節点の領域だけ。
        ProdCanvasH = y - PRowGapY + PMargin;   // 最終行の後に足したのは常に PRowGapY（最下層＝layerEndでもlastRowで狭い側）
        return true;
    }

    /// <summary>節点と辺の**器**を一度だけ作る（2026-08-10）。レイアウトが静的なので、集合も座標も辺の形も不変＝
    /// 毎回の再構築で作り直していたのは中身（文字と色）だけだった。
    /// ★辺の形はここで <see cref="StreamGeometry"/> に確定させる。旧は "M…C…" の**文字列**を Path.Data へバインドし、
    ///   既定コンバータの Geometry.Parse が辺の本数ぶん（165本超）毎回走っていた。</summary>
    private void EnsureProdVisuals()
    {
        if (_nodeByGood is not null) return;
        var lay = _pLayout!;
        var byGood = new Dictionary<int, ProdNode>(lay.Count);
        foreach (var pn in lay.Values)
        {
            int gid = pn.Id;   // 器は静的（作り直さない）＝財idを閉じ込めても別の財を操作しない
            var nd = new ProdNode(() => StepFactoryTargetAll(gid, +1), () => StepFactoryTargetAll(gid, -1))
            { GoodId = pn.Id, W = PNodeW, H = PNodeH, Name = Catalog.Goods[pn.Id].Name };
            byGood[pn.Id] = nd;
            ProdNodes.Add(nd);
        }
        foreach (var pn in lay.Values)
            foreach (var t in pn.Out) ProdEdges.Add(new ProdEdge { From = pn.Id, To = t });
        _nodeByGood = byGood;
        ApplyProdPlacement();
    }

    /// <summary>置いた座標（<see cref="PlaceProd"/>）を器へ流す＝節点の位置と辺の形。器（節点と辺の実体）は作り直さない。
    /// ★辺の形は <see cref="StreamGeometry"/> に確定させる。旧は "M…C…" の**文字列**を Path.Data へバインドし、既定コンバータの Geometry.Parse が辺の本数ぶん（165本超）毎回走っていた。</summary>
    private void ApplyProdPlacement()
    {
        var lay = _pLayout!;
        foreach (var nd in ProdNodes) { var pn = lay[nd.GoodId]; nd.X = pn.X; nd.Y = pn.Y; }
        // 出入りポートを**横**に散らして重なりを軽減（縦並び2026-08-15＝出は下辺・入は上辺）。
        var outPort = new Dictionary<(int a, int b), double>();
        var inPort = new Dictionary<(int a, int b), double>();
        foreach (var pn in lay.Values)
        {
            var outs = pn.Out.OrderBy(t => lay[t].X).ToList();
            for (int i = 0; i < outs.Count; i++)
                outPort[(pn.Id, outs[i])] = pn.X + PNodeW * (i + 1) / (outs.Count + 1.0);
            var ins = pn.In.OrderBy(s => lay[s].X).ToList();
            for (int i = 0; i < ins.Count; i++)
                inPort[(ins[i], pn.Id)] = pn.X + PNodeW * (i + 1) / (ins.Count + 1.0);
        }
        foreach (var e in ProdEdges)
        {
            var pn = lay[e.From]; var tn = lay[e.To];
            double x1 = outPort[(e.From, e.To)], x2 = inPort[(e.From, e.To)];
            // 層は基本レシピで数える（EnsureProdLayout）＝代替レシピの辺は上の行・同じ行へも向く。
            //   下向き＝材の下辺→製品の上辺／上向き＝材の上辺→製品の下辺／同じ行＝両方の上辺を上へ弧で結ぶ。
            double y1, y2, c1, c2;
            if (tn.Y > pn.Y) { y1 = pn.Y + PNodeH; y2 = tn.Y; double dyc = Math.Max(28, (y2 - y1) * 0.45); c1 = y1 + dyc; c2 = y2 - dyc; }
            else if (tn.Y < pn.Y) { y1 = pn.Y; y2 = tn.Y + PNodeH; double dyc = Math.Max(28, (y1 - y2) * 0.45); c1 = y1 - dyc; c2 = y2 + dyc; }
            else { y1 = pn.Y; y2 = tn.Y; c1 = y1 - 40; c2 = y2 - 40; }
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new global::Avalonia.Point(x1, y1), false);
                ctx.CubicBezierTo(new global::Avalonia.Point(x1, c1), new global::Avalonia.Point(x2, c2), new global::Avalonia.Point(x2, y2));
                ctx.EndFigure(false);
            }
            e.Data = geo;
        }
    }

    /// <summary>盤の視野の幅が変わった（分割の枠の幅に盤が追従する）。1 行の幅予算＝視野の幅 − 左右の余白 − 縦の流れの取り代。
    /// 並びが変わる幅のときだけ節点を置き直す（<see cref="PlaceProd"/>）。盤の中身（財カードの文字と色）は影響を受けない。</summary>
    public void SetProdViewport(double viewportWidth)
    {
        if (!(viewportWidth > 0 && viewportWidth < 1e6)) return;   // 否定形＝NaN・無限は無視
        // 1 行に最低 3 枚（狭い視野では盤の横の流れで動かす＝1 枚ずつの縦の一列にして層の形を壊さない）
        double budget = Math.Max(3 * PNodeW + 2 * PRowGapX, viewportWidth - PMargin * 2 - 12);
        _prodRowBudget = budget;
        if (_pLayout is null) return;   // 初回の置きは EnsureProdLayout がこの予算で行う
        if (PlaceProd(budget) && _nodeByGood is not null) ApplyProdPlacement();
    }

    // ---- 統計と再構築 ----

    /// <summary>数量の**実用ゼロ**（幻の生産の是正2026-08-10）＝これ以下の量は「事実上0」として、そこから作る
    /// 比・順位・率を画面に出さない（「—」へ落とす）。
    /// ★出所はエンジンの <see cref="Coefficients.TargetStockFloor"/>（在庫目標の下限＝需要が0の財に置く在庫の最小単位
    /// ＝エンジンが持つ「量として意味を持たない」の物差し）＝UI に第二の閾値を作らない。
    /// 旧 1e-9 では、技術ゲートで目標0のまま幾何級数で減り続ける残り火（1e-8 級）が分母に立ち、
    /// **ノイズ同士の比**が「世界の12%を生産」「廃棄率 12,543,372%」として出ていた。</summary>
    private double QtyEps => _player.Coef.TargetStockFloor;

    /// <summary>過不足の**率**（作者仕様2026-08-10 項目3）。
    /// 定義＝<c>(国内生産 − 国内消費) ÷ 国内生産</c>。国内生産＝Market.LastProduced（active州の合算）、
    /// 国内消費＝Market.LastSold（同）。**輸入は分子にも分母にも入れない**（LastImport / LastExport を一切読まない）
    /// ＝「自分で作った量に対してどれだけ余っている／足りていないか」だけを測る。
    /// 生産が事実上0（<see cref="QtyEps"/> 以下）の財は NaN を返す（表示はカードが空・州別テーブルが「—」＝Grid の桁を保つため）。
    /// 100%を超える不足＝消費が生産の2倍以上（差は輸入と在庫の取り崩し）。</summary>
    private double SurplusRate(double produced, double sold) => ProductionTables.SurplusRate(produced, sold, QtyEps);   // 式は本体の目録の 1 本

    /// <summary>過不足率の表示（否定形の判定＝NaN も空へ落とす）。
    /// ★語は符号で切り替える（作者指示2026-09-23「『過不足:n%』は意味が分からないので『余剰n%』『不足n%』を切り替える」）。
    ///   これが2026-09-07 (5) の「語は『過不足』1本」を上書きする（同じ作者の新しい指示が勝つ）。旧はコロン付きの
    ///   「過不足:+12%／過不足:-8%」で、符号だけが意味を担っていた。
    /// ★語と色は**同じ否定形の式**から出す＝`!(rate &lt; 0)` が真なら「余剰」＋緑（<see cref="PPlus"/>）、偽なら「不足」＋赤。
    ///   これで語と色が食い違う断面が原理的に無い（呼び出し側の `SurplusBrush` の式＝カードと州別テーブルの両方＝と同一の否定形）。
    /// ★**0の扱い**：ちょうど0は「余剰0%」（`!(0 &lt; 0)` が真＝緑側）。0へ丸まる負（−0.4%など）は「不足0%」＋赤＝
    ///   語が符号を語り、数は丸めたままにする（「不足0%」は"ほんの僅かに足りない"の意。0.5%刻みの語を新設しない）。
    ///   符号は <see cref="Math.Abs"/> で落とす＝負の零（-0.0）が「余剰-0%」と出る穴を塞ぐ（"0%" 書式は -0.0 に「-」を付ける）。
    /// ★NaN（生産なし）は手前の三項で**空文字**へ落ちる（2026-09-23・作者指摘「過不足は意味が分からない」の積み残しを回収）。
    ///   旧は「過不足:—」＝語の切替から取り残された最後の1箇所で、作者が捨てた語がここだけ残っていた。
    ///   「余剰—」は無意味なので作らない。「—」1文字も作らない。**空にする**根拠は3つ：
    ///   (1) 同じ状態（生産が実用ゼロ）は呼び出し側の `none` 枝（RefreshProduction）が既に空文字を出している
    ///       ＝1つの状態に2つの綴りがあるのをやめて、既に在る方へ寄せる。
    ///   (2) 作者裁定2026-08-11（このファイルの PriceTotalText＝「出せないときは空。『—』は情報量がない」）と同じ形。
    ///   (3) カードの3行目は横並びの StackPanel＝列の桁合わせが要らない（州別テーブルの「—」は Grid の桁を保つので**残す**）。
    ///   結果、生産が無い財の3行目は「生産:0.0k」だけになる（率の欄そのものが消える）。</summary>
    private static string SurplusText(double rate) => !(rate > double.MinValue && rate < double.MaxValue) ? ""
        : (!(rate < 0) ? "余剰" : "不足") + Math.Abs(rate).ToString("0%");

    public void RefreshProduction()
    {
        EnsureProdLayout();
        EnsureProdVisuals();
        int n = Catalog.Goods.Length;
        // 集計は本体の目録の 1 本（SimCore/Display/ProductionTables）＝盤の節点（財カード）と財の表が同じ 1 周から値を取る。
        // 生産量の内訳（RGO と工場）・原料充足率・工場の棟数・技術不足の判定・過不足の率の式はそこにある（UI に第二の式を置かない）。
        var st = ProductionStats.Collect(_player);
        var prod = st.Prod; var sold = st.Sold; var exp = st.Exp; var imp = st.Imp; var subsRate = st.SubsRate;
        var prodRgo = st.ProdRgo; var prodFac = st.ProdFac; var hasProducer = st.HasProducer;
        // ★「まだ存在しない財」（作者裁定2026-08-10）＝世界のどこにも生産が無く、どの国も技術的に作れない財。
        //   判定はエンジンの純関数1本（ProductionDiag.UnavailableGoods）＝UI に第二の式を置かない。
        //   ⚠技術不足（灰色化）は**自国**の技術で読む＝「他国は作れるが自国は作れない」財は灰色だが、
        //     価格もシェアも実在する（数字を出す）。この unavail は**世界で誰も作れない**＝数字そのものが無い。
        var unavail = ProductionDiag.UnavailableGoods(_sim, _player.Coef);
        var all = ProductionTables.AllLines(_sim, _player, st, unavail);

        // ノード（ボタン＝品名＋世界価格との差。縦線の中は合計の棟数と実質稼働数。地色は tier で最初から明るく、選択で連鎖を強調）
        var blockedOf = new bool[n]; var inactiveOf = new bool[n];   // 選択だけの塗り直し（ApplyProdSelection）へ持ち越す集計の産物
        foreach (var ln in all)
        {
            int gi = ln.Good; var d = Catalog.Goods[gi];
            // 全国の棟数（国営/民営）と建設予定・予定を含めた総数＝カードの分子と分母・財の表の列が同じ1本（World.FactoryCounter）
            int stU = ln.State, pvU = ln.Private, pendU = ln.Pending, planU = ln.Planned;
            bool isFactory = ln.IsFactory, none = ln.None, blocked = ln.Blocked, inactive = ln.Inactive;
            double util = ln.Util;
            // ★2026-08-10（作者フィードバック項目3）：産出が無い月は**測っていない**＝NaN。表示は 0%（供給の実績がゼロ）、
            //   赤（starving）は立てない。
            double inpFill = ln.InpFill;
            double surplusRate = ln.SurplusRate;   // 過不足＝**生産に対する比**。NaN＝生産なし
            // ★2026-11-04（作者）：カードは材名＋価格％だけを大きく出す（稼働率・生産・過不足は財の表にある）。工場の縦線は〈合計の棟数〉と〈実質稼働数＝棟数×稼働率〉の2行。
            // 世界価格との比較（項目3）＝首都価格×為替／世界価格−1（本体の目録の PriceGap）。安い＝青・高い＝赤。
            // グラフ本体は四半期再構築なので、この％も四半期（右パネルの首都価格行は日次）。
            bool hasPd = !double.IsNaN(ln.PriceGap);   // まだ存在しない財は世界価格との比を出さない（作者裁定2026-08-10）
            double pdg = hasPd ? ln.PriceGap : 0;
            // 稼働率行の赤＝素材が買えず産出が絞られている工場（労働不足は含まない）。材名の赤（ボトルネック変色）は廃止（2026-08-16）
            bool starving = !none && isFactory && inpFill < MaterialFillThreshold;   // NaN（産出なし＝未測定）は false＝赤にしない
            var nd = _nodeByGood![gi];   // 器は据え置き＝値だけ流す（GoodId/X/Y/W/H/品名は静的）
            // ★産地なしの材は灰色（作者指示2026-08-11）＝この国では作れない/作っていないことが一目で分かる。
            //   技術不足（blocked）と同じ淡色を使う＝「触れない材」の色の文法を1つに保つ。
            bool noSource = none && !hasProducer[gi];
            nd.NameBrush = blocked || noSource ? PDimText : PNameText;
            // ★書式の第3節「0%」は必須（2026-09-23・.NET 10で実測）＝**この画面の率の書式は全部3節**
            //   （nd.PriceDiff／ProdTableRow の Price・Surplus・MatCost／ProdDetail.PriceDiff／BuildPriceRows の Pct の6箇所）。
            //   2節の "+0%;-0%" は、0へ丸まる負数（−0.4%など）に対して負号を付けたうえで**第1節**を使う＝
            //   「-+0%」という符号が2つ並んだ文字列を出していた（-0.002 → "-+0%"／-0.2 → "-20%" と挙動が割れる）。
            //   第3節を足すと 0 も −0.0 も0へ丸まる負も「0%」＝符号の無い零になる（丸めて消えた符号を書かない）。
            //   ⚠色は割れていなかった（`!(x < 0)` は -0.002 を正しく赤に落とす）＝色の式は触らない。
            nd.PriceDiff = hasPd ? pdg.ToString("+0%;-0%;0%") : "";
            nd.PriceDiffBrush = pdg <= 0 ? PPriceBelow : PPriceAbove;
            nd.Accent = blocked ? PAccTechShort : TierAccent(d.Tier);
            blockedOf[gi] = blocked; inactiveOf[gi] = inactive;   // 地色・枠・沈みは選択と併せて ApplyProdSelection が塗る（式は1箇所）
            // 縦線の中の工場カウンター（作者仕様2026-09-07 (1)）＝「今ある棟数 / 予定を含めた棟数」。
            //   分子＝民営と国営の**完成した**合計棟数／分母＝合計＋建設予定（建設中＋まだ着工していない未達の意志）。
            //   ▲を押すと予定が1増え、エンジンが最も効率の良い州へ国営工場を建てて分子が追いつく。
            //   ▼は**予定のうちまだ州へ配っていない分**しか削れない（建った棟は壊せない）＝削る余地が無い時は押せない状態で出す
            //   （作者指示2026-09-07「マイナスのボタンは最初から表示する」＝0の時も隠さない）。
            // ★2026-11-04（作者「国営と民営の数を分ける必要を感じない＝合計値」「実質稼働数（工場数×稼働率）で表示」「稼働率が低い場合は色（赤→黄→緑）」）：
            //   1行目＝国営＋民営の完成した合計棟数（まだ建っていない意志があれば「+差分」を足す＝▲を押した手応え）。
            //   2行目＝実質稼働数＝合計棟数×稼働率（四捨五入）。色は稼働率で赤→黄→緑（<see cref="UtilColor"/>・棟が無い間は墨）。
            //   内訳（国営・民営・建設予定・全国目標・稼働率）は縦線のツールチップ（FactoryTip）に残る。
            int totU = stU + pvU, wantU = planU - totU;
            double utilV = !(util > 0) ? 0 : util < 1 ? util : 1;   // NaN（産出なし＝未測定）は0・上限1（否定形で NaN を落とす）
            nd.TotalCount = wantU > 0 ? $"合計{totU}+{wantU}" : $"合計{totU}";
            nd.RunningCount = $"稼働{Math.Round(totU * utilV):0}";
            nd.RunningBrush = totU > 0 ? UtilColor(utilV) : PDimText;
            nd.CanUpFactory = planU < _player.FactoryTargetMax;
            nd.CanDownFactory = planU > stU + pvU + pendU;
            nd.FactoryTip = $"国営 {stU}棟　民営 {pvU}棟　建設予定 {pendU}棟　全国目標 {planU}棟　稼働率 {utilV:P0}";
        }
        _sBlocked = blockedOf; _sInactive = inactiveOf;
        // 財の表・設備の表・労働の表（本体の目録が行と列を名乗る）。財の表は盤の節点と同じ集計から（二度走査しない）。
        var shown = ProductionTables.ShownLines(all);
        ProdGoodTable = TableSnapshot.From((ProductionTables.Goods with { Rows = (_, _) => shown }).Read(_sim, _player));
        ProdEquipTable = TableSnapshot.From(ProductionTables.Equipment.Read(_sim, _player));
        ProdLaborTable = TableSnapshot.From(ProductionTables.Labor.Read(_sim, _player));
        ProdEquipmentOn = _player.Coef.RecipeEquipment > 0;
        ApplyProdSelection();   // 選択に依存する見た目（節点の枠/地色/沈み・辺の強調）＝選択だけの塗り直しと同じ経路を通す

        BuildMegaRows();
        // 統計配列を保持＝詳細パネル（価格は日次）だけを軽く再構築できるようにする（グラフ本体は四半期）
        // ★2026-08-10（作者フィードバック項目5）：設備欄の全廃で、全国の従業員（emp）・技術による産出上限（gate）・産地（hasProducer）・
        //   能力（cap）は詳細パネルから消えた＝保持もやめる（cap/gate/hasProducer は節点カードのために局所で使うだけ）。
        // ★同日 項目4：「不足」行の全廃で産出（outp）と原料充足（inpFillW）の読み手も消えた＝保持をやめた（節点カードの局所値だけ）。
        _sProd = prod; _sSold = sold; _sExp = exp; _sImp = imp;
        _sSubsRate = subsRate;
        _sProdRgo = prodRgo; _sProdFac = prodFac;
        _sUnavail = unavail;
        ApplyProdDetail(BuildProdDetail(prod, sold, exp, imp, subsRate, prodRgo, prodFac));
    }

    // 保持した統計配列（RefreshProduction が更新）。日次の価格更新で詳細パネルだけ作り直すのに使う。
    private double[]? _sProd, _sSold, _sExp, _sImp, _sSubsRate, _sProdRgo, _sProdFac;
    private bool[]? _sBlocked, _sInactive;   // 技術不足・休止（選択だけの塗り直しへ持ち越す＝集計を再走させない）
    // ⚠2026-09-07 撤去：「供給が十分＝緑」（要望(2)前半）は作者指示で全撤去＝材は要素が多く、光っていても何が良いのか判別できないため。過不足は行の文字（緑/赤）だけで示す。
    private bool[]? _sUnavail;               // まだ存在しない財（世界のどこにも無く誰も作れない）＝価格・シェア・廃棄率・価格グラフを「—」にする（作者裁定2026-08-10）

    /// <summary>詳細パネルだけを更新（価格＝日次。生産統計は四半期の保持値を再利用）。グラフ本体（節点・辺）は作り直さない＝軽量。</summary>
    public void RefreshProdDetail()
    {
        if (!ProductionShown || _sProd is null) return;
        ApplyProdDetail(BuildProdDetail(_sProd, _sSold!, _sExp!, _sImp!, _sSubsRate!, _sProdRgo!, _sProdFac!));
    }

    /// <summary>州別の見出し「州名」のホバー＝**工業化の州別上位8**（2026-09-23＝移送先が決まらないまま消えていた内訳を置き直す）。
    /// ⚠行は増やさない＝既にある見出しのセルに載せる（画面の作法）。
    /// ⚠内訳の式は1つも書き写さない＝既に組んである <c>IndustrializationTerms()</c>（GameViewModel.Header.cs・同じ partial class）を**呼ぶだけ**
    ///   （財政タブの <c>AdminTip</c>／<c>ForeignInTip</c> と同じ形）。
    /// 置き場の理由＝工業化は**州の量**で、生産タブでその量が並ぶのは州別の表だけ。表の行の「州名」（<c>CapitalTip</c>＝その州の新旧資本）は
    ///   1州の話なので、全国の順位をそこへ入れると36行ぶん同じ8行をくり返すことになる＝1つしかない見出しの側に置く。
    ///   縦線の工場カウンター（<c>FactoryTip</c>）には置かない＝▲▼で建つ州を選ぶのは工業化ではなく
    ///   <c>UtilityAgent.FacilityBuildScore</c>（不足の深さ×人が余っている）なので、上位8を並べると「次はこの州に建つ」と読める嘘になる。</summary>
    public string ProdRegionHeadTip => $"工業化　{_player.MeanIndustrialization:P0}\n" + string.Join("\n", IndustrializationTerms().Select(t => t.Text));

    /// <summary>詳細パネルの据え置き適用（2026-07-19）：同じ財なら既存インスタンスへ値だけ流す＝州別の建設/売却ボタンを作り直さない
    /// （旧・毎日の丸ごと差し替えは最大20回/秒でボタンを破棄＝「時間を止めないと押せない」の主因）。</summary>
    private void ApplyProdDetail(ProdDetail? fresh)
    {
        if (fresh is null) { ProdDetail = null; return; }
        if (ProdDetail is { } cur && cur.GoodId == fresh.GoodId) cur.CopyFrom(fresh);
        else ProdDetail = fresh;
        OnPropertyChanged(nameof(ProdSparkPoints));   // CopyFrom 経路（同じ財の更新）は OnProdDetailChanged が走らない
        OnPropertyChanged(nameof(ProdSparkGrid));
        OnPropertyChanged(nameof(ProdRegionHeadTip));   // 州別の見出しのホバー（工業化の州別上位8）＝get-only なので追従には通知が要る
    }

    // 素材（労働・役務でない実物の投入）か（本体の目録の 1 本）
    private static bool IsMaterial(int gid) => ProductionStats.IsMaterial(gid);

    private double LaborRate(Region rg, int labId, int classId)
    {
        if (labId >= rg.Markets.Length) return 0;
        double lp = rg.Markets[labId].LastProduced;
        if (!(lp > QtyEps)) return 0;   // 労働の供給が事実上0の州＝人数への換算率を作らない（否定形＝NaN も0へ）
        double size = 0;
        foreach (var p in rg.Pops) if (p.ClassId == classId) size += p.Size;
        return size / lp;
    }

    private ProdDetail? BuildProdDetail(double[] prod, double[] sold, double[] exp, double[] imp, double[] subsRate,
                                        double[] prodRgo, double[] prodFac)
    {
        int gi = _prodSelGood;
        if (gi < 0 || _pLayout is null || !_pLayout.ContainsKey(gi)) return null;
        var d = Catalog.Goods[gi];
        // ★まだ存在しない財（作者裁定2026-08-10）＝世界のどこにも無く、どの国も作れない。数字を作らず全て「—」で出す
        //   （参照価格そのものは撤去できない＝建設費の換算・材料価格の物差し・交易判定が構造定数として読むため。表示だけを揃える）。
        bool unavail = _sUnavail is { } ua && gi < ua.Length && ua[gi];
        double world = gi < _sim.Market.Price.Count ? _sim.Market.Price[gi] : 0;
        double ex = _player.ExchangeRate;   // エンジンの輸出入判定は金建て（局所価格×為替）＝表示も揃える
        var cap0 = _player.Regions[0];
        double local = gi < cap0.Markets.Length ? cap0.Markets[gi].Price : 0;
        // 稼働率とその内訳（作者フィードバック2026-08-10 項目5）：設備欄を全廃したので、この分解は
        // **州別テーブルの「生産」列のツールチップ**が受け持つ（全国値＝集計範囲は節点カードと同一）。
        string utilTip = BuildUtilTip(ProductionDiag.ExplainUtilization(_player, gi));

        // 州別：生産・過不足（生産−域内販売）・材料価格・建設・売却（生産量の多い順）。補助金は品目別（州別でなく）＝下部の一括トグル
        var spec = Catalog.FactorySpec(gi);
        // ★着工の採算ゲート（2026-08-09・作者報告「着工ボタンがたまに効かない」の原因A）：
        //   エンジン（World.BuildFactory）は建材の束が高すぎる間、着工を**無言で**拒む。UI にその判定が無かったので
        //   「押せる・押した・何も起きない」になっていた。閾値も採算率も**エンジンと同じ関数/定数**を読む＝UI に第二の基準を作らない。
        //   建材の相場は首都州の1本（World.ConstructionAfford）＝全州共通なので州ループの外で1回だけ求める。
        double buildAfford = World.ConstructionAfford(_player);
        // 着工ボタンの常時ツールチップ（作者フィードバック2026-08-10 項目7で4行構成へ）：
        //   建設費 ＝ FactorySpec.Cost（工場カテゴリの基本費）
        //   材料費リスト ＝ 総必要量（FactoryMaterialFrac×Cost/costPerUnitRef×Cons*＝EconomyStep.AccumulateFactoryBuild の建設束 u の
        //     全期間合計・採算率100%時）×首都州の現在価格。（★2026-09-30 段1：素材階層+α FactoryTierMachinePerDepth は撤去＝機械の行は基本束だけ）
        //   合計コスト ＝ 工賃（(1−FactoryMaterialFrac)×Cost＝World.BuildFactory が日割りで国庫から工員へ払う総額）＋材料費（現在価格）。
        //     ＝素材が高い時期は建設費より高くつく＝エンジンの「真の価格依存」がそのまま数字に出る。
        //   完成まで ＝ spec.Quarters ÷ 四半期/年。
        // ★2026-08-10（項目3の1行化）：ボタンの文字は「建設」だけになった＝**何を建てるのか**（RGOは鉱山／漁場／伐採場／農場…）は
        //   この1行目が受け持つ（D2の RecipeName をそのまま使う）。
        string facilityWord = d.Inputs.Length > 0 ? "工場" : Catalog.RgoFacilityName(gi);
        string buildCostTip;
        {
            var cf = _player.Coef;
            var bun = EconomyStep.BundleOf(cap0, cf);   // ★(568) 今の建設の資材の組（エンジンと同じ関数＝旗 BuildingGoodBundle が正なら建造物＋機械・0 なら旧の鋼の束）
            double cpuRef = EconomyStep.BundleRef(cf);
            double units = cpuRef > 1e-9 ? cf.FactoryMaterialFrac.Value * spec.Cost / cpuRef : 0;   // 総建設ユニット
            double P(int id) => id >= 0 && id < cap0.Markets.Length ? cap0.Markets[id].Price : 0;
            var mats = new System.Text.StringBuilder();
            double matCostSum = 0;
            void Row(int id, double qty) { double amt = qty * P(id); matCostSum += amt; mats.Append($"\n{Catalog.Goods[id].Name} {Fmt.Qty(qty)}　{Fmt.Money(amt)}"); }   // 材料数量もk統一（項目4）
            Row(bun.G0, bun.Q0 * units);
            Row(bun.G1, bun.Q1 * units);
            if (bun.Slots == 4) { Row(bun.G2, bun.Q2 * units); Row(bun.G3, bun.Q3 * units); }   // 建造物の組は 2 枠
            double wageTotal = (1.0 - cf.FactoryMaterialFrac.Value) * spec.Cost;
            buildCostTip = $"{facilityWord}　建設費 {Fmt.Money(spec.Cost)}" + mats
                + $"\n合計コスト {Fmt.Money(wageTotal + matCostSum)}"
                + $"\n完成まで {spec.Quarters / (double)Calendar.QuartersPerYear:F1}年"
                + $"\n国庫 {Fmt.Money(_player.Treasury.Value)}（着工{(wageTotal + matCostSum > 1e-9 ? System.Math.Floor(System.Math.Max(0, _player.Treasury.Value) / (wageTotal + matCostSum)) : 0):F0}回分）";   // ★2026-08-31（清レポート4-4＝60連打で国庫271の無警告）：一括着工の帰結を事前に読める
        }
        // 州に依存しないループ不変量（州ループの外で1回だけ求める＝buildAfford と同じ場所）。
        double payPerDay = World.FactoryPayPerDay(gi, _player.Coef, out _);   // 着工に要る初日の労務費（World.BuildFactory と同じ1本・(405)）
        var keyed = new List<(double prod, ProdRegionRow row)>();
        for (int ri = 0; ri < _player.Regions.Count; ri++)
        {
            var rg = _player.Regions[ri];
            if (gi >= rg.Markets.Length) continue;
            var m = rg.Markets[gi];
            // エンジンの動詞（SetSubsidy/BuildFactory/CanSellFactory/SetStateTarget）は全て World.FindFactory
            // （①投資済みで操業可→②操業可→③門が閉じた投資済み→④先頭の優先順）で個体を選ぶ＝表示も同じ個体に揃える
            // （並存レシピ財＝衣料の職人工房等で「表示と操作が別個体」になる齟齬をレビューで確認・2026-07-05）
            var pr = World.FindFactory(rg, gi);
            // 失業した働き手（作者指示2026-08-03「工場労働者が余っている州に建てたい」）：この財の生産者が雇う pop
            // （工場＝工場労働者・農場/鉱山＝農家）の失業数。どこに建てるかの判断材料＝2026-08-10に**列**へ昇格（項目6）。
            double idle = 0;
            if (pr is not null && pr.LaborPopIndex >= 0 && pr.LaborPopIndex < rg.Pops.Count)
            {
                var lp2 = rg.Pops[pr.LaborPopIndex];
                idle = lp2.Size * lp2.UnemploymentRate.Value;
            }
            // 従業員（作者フィードバック2026-08-10 項目6）＝この州でこの財を作る全生産者の労働投入を人数へ換算した和。
            // 換算率は失業率の定義と整合する LaborRate（労働popの人数 ÷ 今月供給した労働units）＝旧・設備欄の全国「従業員」と同じ式の州版。
            double empR = 0;
            {
                double perW = LaborRate(rg, Catalog.LaborId, PopClass.Worker), perF = LaborRate(rg, Catalog.FarmLaborId, PopClass.Peasant), perA = LaborRate(rg, Catalog.AdvLaborId, PopClass.Engineer);
                foreach (var p2 in rg.Producers)
                {
                    if (p2.GoodId != gi || p2.Bankrupt) continue;
                    foreach (var inp in p2.Inputs)
                    {
                        double units = p2.Output * inp.QtyPerOutput;
                        if (inp.GoodId == Catalog.LaborId) empR += units * perW;
                        else if (inp.GoodId == Catalog.FarmLaborId) empR += units * perF;
                        else if (inp.GoodId == Catalog.AdvLaborId) empR += units * perA;
                    }
                }
            }
            double surplusRate = SurplusRate(m.LastProduced, m.LastSold);   // 生産に対する比（項目3）。絶対量はツールチップへ
            // 材料価格（作者仕様2026-08-09 項目11／旧「世界価格差」を置換）＝**その州でこの財を作る仕入れの高さ**。
            //   定義：この州のレシピの素材投入（労働・役務は除く）を Σ 投入係数×その州の市場価格 で積んだ束の費用を、
            //         同じ束を Σ 投入係数×世界価格 で積んだ費用と比べた比率−1（局所価格は為替で金建てに直す＝世界価格と同じ物差し）。
            //   マイナス（青）＝世界より安く仕入れられる州＝そこで作るのが有利。RGO（素材投入なし）は「—」。
            //   ⚠製品の売値は見ない（それは右上の首都価格行と各カードの％）＝ここは「その地域で安く作れるか」だけを読む。
            double matLocal = 0, matWorld = 0;
            foreach (var inp in pr is not null ? (IEnumerable<Input>)pr.Inputs : d.Inputs)
            {
                if (!IsMaterial(inp.GoodId)) continue;
                matWorld += inp.QtyPerOutput * (inp.GoodId < _sim.Market.Price.Count ? _sim.Market.Price[inp.GoodId] : 0);
                matLocal += inp.QtyPerOutput * (inp.GoodId < rg.Markets.Length ? rg.Markets[inp.GoodId].Price : 0);
            }
            double mdiff = matWorld > 1e-6 ? matLocal * ex / matWorld - 1.0 : 0;
            double regionGate = pr is null ? 0 : Discovery.Gate(rg, pr);   // 「作れるか」の式はエンジン側1本（発見の門）から読む
            bool cantMakeHere = pr is not null && !(regionGate > ProductionDiag.TechGateClosed);            // この州（の国）はこの財のレシピを発見していない＝1単位も作れない
            // RGOの建設を解禁（プランテーションdoc D1・2026-07-30）：旧 `d.Inputs.Length > 0`（投入を持つ財だけ）が
            // 投入ゼロのRGO（鉱山・牧場・プランテーション）を弾いていた。エンジン（World.BuildFactory）は Tier==Service しか弾かず、
            // 建てられる条件は「その州にRGOの生産者が立っている」＝ pr is not null に既に畳み込まれている
            // （気候ゲート＝米/綿花/植民地財は非適地で生産者ごと除去済み・非賦存州は Productivity×0.4＝市場が採算で判断）。
            bool canBuild = pr is not null && pr.ConstructionLeft <= 0
                         && _player.Treasury.Value >= payPerDay && !cantMakeHere   // 技術で産出0の工場・国庫不足の空クリックは無効化
                         && buildAfford >= _player.Coef.FactoryStartAffordFloor;    // 建材が高すぎる間はエンジンが拒む＝ボタンも押せない（原因A）
            // 建設できない理由（ボタンにカーソルを合わせた時に表示）。建設可能なら null＝ツールチップ無し
            string? buildReason = canBuild ? null
                : pr is null ? "この州に産地なし"
                : pr.ConstructionLeft > 0 ? $"建設中 あと{pr.ConstructionLeft / (double)Calendar.DaysPerYear:F1}年"
                : cantMakeHere ? "技術不足"
                : _player.Treasury.Value < payPerDay ? $"国庫不足(初日の労務費 {Fmt.Money(payPerDay)} が必要)"
                : buildAfford < _player.Coef.FactoryStartAffordFloor ? $"建材の採算 {buildAfford:P0}/必要 {_player.Coef.FactoryStartAffordFloor:P0}"
                : "建設不可";
            var sale = pr is not null && pr.BuiltScale > 1e-9 ? _player.CanSellFactory(ri, gi) : (Ok: false, Price: 0.0, Reason: "");
            // ★国営工場カウンター（作者裁定2026-08-10）＝目標（意志）と現在（実測）。1カウント＝1棟＝Coefficients.FactoryBuildScale。
            //   棟数はエンジンの換算（World.StateUnits）をそのまま読む＝UI に第二の式を置かない。
            //   ★幻の工場（作者スクリーンショット2026-08-10）＝機関銃が「技術不足・生産量0.0k」なのに江戸2・大阪1…計7棟と出ていた。
            //     Producer.Scale は scaleTarget（EconomyStep が 1+margin から算出）を基底に育ち技術ゲートを知らない＝**作れない財にも棟数が立つ**。
            //     この州の技術で1単位も作れない財は棟数を0にする（エンジン側の是正は技術ゲート0の操業規模0。
            //     点火すれば Scale 自体が0になりここは自然に0＝**表示は点火の前後どちらでも同じ数**を出す）。
            int stateUnits = pr is not null && !cantMakeHere ? World.StateUnits(pr, _player.Coef) : 0;
            int privUnits = pr is not null && !pr.Bankrupt && !cantMakeHere ? _player.PrivateUnitsOf(pr) : 0;   // 換算はエンジンの1本（棟数の式をUIに持たない）
            int target = pr?.StateTarget ?? 0;
            // ★進まない月は理由を必ず出す（作者裁定2026-08-10 C節「無言にしない」）。常時ツールチップ＝
            //   目標＞現在は着工のゲート（建設中／技術／国庫／建材の採算）、目標＜現在は払い下げのゲート（CanSellFactory の理由）。
            // 目標と現在が食い違う月だけ、その行の下に理由を常に見せる（ホバーは廃止・作者の指示 2026-11-11。建設費・材料の内訳は画面から外した＝作者確認）。
            string counterNote = pr is null ? ""
                : target > stateUnits ? (buildReason ?? "建設中")
                : target < stateUnits ? (sale.Ok ? $"払い下げ 1棟 {Fmt.Money(sale.Price)}" : sale.Reason.Length > 0 ? sale.Reason : "払い下げ不可")
                : "";
            // ★行の命令は**州の実体**を捕まえる（2026-08-09）＝旧実装は生成時の添字 rIdx を閉じ込めていた。
            //   行は据え置き同期（CopyFrom）で使い回されるので、州の増減で添字がずれると**別の州を操作してしまう**。
            var rgRef = rg;
            keyed.Add((m.LastProduced, new ProdRegionRow(
                () => StepStateTargetAt(rgRef, gi, +1),
                () => StepStateTargetAt(rgRef, gi, -1))
            {
                Name = rg.Name,
                // 接17（監査2026-07-11・2026-07-25実装）：新旧資本の内訳＝旧式資本を抱えた産業と新鋭産業の区別（売却・補助金判断の材料）。休眠（旧世代0）なら出さない
                CapitalTip = rg.OldVintageCapital > 1e-9
                    ? $"工業化資本 {rg.TotalIndustrialization:F1}=新鋭 {rg.Industrialization:F1}+旧世代 {rg.OldVintageCapital:F1}\n座礁 {rg.StrandedVintage:F1}"
                    : null,
                // ★国営工場の列＝「− 現在/目標 ＋」1本（作者フィードバック2026-08-10 項目5＝カウンター列を統合）。
                //   現在＝実際に建っている国営の棟数（World.StateUnits）／目標＝カウンター（Producer.StateTarget）。
                //   単位は棟（小数の Scale は使わない）。民営工場の列はそのまま残す。
                HasCounter = pr is not null,
                CanUp = pr is not null && target < _player.Coef.StateTargetMax,
                CanDown = pr is not null && target > 0,
                CounterNote = counterNote,
                PrivFac = pr is null ? "—" : privUnits.ToString(),
                StateFac = pr is null ? "—" : $"{stateUnits}/{target}",
                // 従業員・失業者（項目6・2026-08-10）＝産地の無い州は「—」（0で埋めない＝行は残る＝レイアウトが上下しない）
                Emp = pr is null ? "—" : Fmt.People(empR),
                Idle = pr is null ? "—" : Fmt.People(idle),
                Prod = unavail ? "—" : Fmt.Qty(m.LastProduced),   // まだ存在しない財は量も出さない（作者裁定2026-08-10）
                ProdTip = utilTip,   // 稼働率とその内訳（項目5で設備欄から移設・全国値）
                // 過不足＝生産に対する比（項目3・2026-08-10）。絶対量（生産・消費・その差）はツールチップで読む
                Surplus = !(surplusRate > double.MinValue && surplusRate < double.MaxValue) ? "—" : surplusRate.ToString("+0%;-0%;0%"),
                SurplusBrush = !(surplusRate < 0) ? PPlus : PMinus,
                SurplusTip = $"生産 {Fmt.Qty(m.LastProduced)}/月　消費 {Fmt.Qty(m.LastSold)}/月　差 {(m.LastProduced - m.LastSold >= 0 ? "＋" : "−")}{Fmt.Qty(Math.Abs(m.LastProduced - m.LastSold))}",
                MatCost = matWorld > 1e-6 ? $"{mdiff:+0%;-0%;0%}" : "—",
                MatCostBrush = mdiff <= 0 ? PPriceBelow : PPriceAbove,
            }));
        }
        keyed.Sort((a, b) => b.prod.CompareTo(a.prod));
        var rows = keyed.Select(k => k.row).ToList();

        // ★2026-09-20（M5）：専売の ON/OFF は**UI側の鏡を持たない**＝ProdDetail.MonopolyOn がエンジンの配列を直に読み書きする
        //   （ここで先に読んでコンストラクタへ渡していた `taxOn` を削除＝「鏡が真値とずれる」余地を構造的に消す）。
        // 独占バッジ：塩(6)/茶(74)の法的独占。式はUIで再導出せず、エンジンの価格分解（ExplainPrice＝代表州で実際に効いているレント/国内マークアップ）をそのまま見る
        // （MonopolyRent は独禁法E6の region.MonopolyRentFactor 込みの実値＝解体後は自然に0になる）。
        var parts = ProductionDiag.ExplainPrice(_player, cap0, gi);
        bool legalMonopoly = (gi == Catalog.SaltId || gi == Catalog.TeaId) && parts.MonopolyRent > 1e-9;

        // 用途（使い道・2026-07-19）：この財を材料に使う財ごとの今月の実消費量（産出×投入係数・全国合算）。
        // レシピ上の消費先は今月0でも一覧に出す（何に使えるかの地図）。行クリックでその財を選択。
        var useAmt = new Dictionary<int, double>();
        if (_pLayout.TryGetValue(gi, out var selNode))
            foreach (var outId in selNode.Out) useAmt[outId] = 0;
        foreach (var rg in _player.Regions)
        {
            foreach (var p in rg.Producers)
            {
                if (p.Bankrupt || p.GoodId == gi) continue;
                foreach (var inp in p.Inputs)
                    if (inp.GoodId == gi) useAmt[p.GoodId] = useAmt.GetValueOrDefault(p.GoodId) + p.Output * inp.QtyPerOutput;
            }
        }
        // 役務の消費先（教育が紙を使う等）も一覧には出すが、グラフに無い＝クリック不可（レビュー2026-07-19：選択すると詳細消滅・全ノード減光の実バグ）
        var uses = useAmt.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
            .Select(kv => new ProdUseRow
            {
                GoodId = kv.Key,
                Name = Catalog.Goods[kv.Key].Name,
                Amount = $"{Fmt.Qty(kv.Value)}/月",   // 消費量＝数量（k統一・項目4）
                Used = kv.Value > QtyEps,
                CanSelect = _pLayout.ContainsKey(kv.Key),
                SelectCmd = SelectUseGoodCommand,
            })
            .ToList();
        // ★一覧の**先頭**＝popがそのまま消費する分（作者フィードバック2026-08-10 項目6）。
        //   旧は下流の財（加工先）しか出ておらず、パンや茶のような「そのまま食べる/飲む」財は用途が空に見えた。
        //   量の出所は <see cref="ProductionDiag.ExplainPopConsumption"/>＝EconomyStep が渡した瞬間に記帳した
        //   Market.PopBought を足すだけ（UIで式を作らない）。
        //   ⚠**階級ごとの好みは存在しない**＝1人あたりの必要量は全階級共通（違うのは予算だけ）。だから
        //     「奢侈品（上流階級）」のような好みの表記は書かない＝書けるのは**実際に誰が買ったか**（ホバーの階級別シェア）だけ。
        var popUse = ProductionDiag.ExplainPopConsumption(_player, gi);
        if (d.BaseNeed > 0 || popUse.Recorded)   // BaseNeed>0＝popが買う財（財の静的属性＝月ごとに行が出入りしない）。Recorded＝係数で需要が立った財（住居）
            uses.Insert(0, new ProdUseRow
            {
                GoodId = -1,
                Name = UseTierName(d.Tier),   // 用途としての呼び名（主食＝食料）
                Amount = $"{Fmt.Qty(popUse.Total)}/月",
                Used = popUse.Total > QtyEps,
                CanSelect = false,             // popは財ではない＝飛び先が無い
                Tip = BuyerShareTip(popUse),
            });

        // 「不足 —」の行は廃止（作者フィードバック2026-08-10 項目4）＝どの材が足りないかは
        // 生産コスト欄の各行の「供給:n%」（材別の配給率）が既に1行ずつ言っている＝集計した名前の羅列は重複。

        // 廃棄率（2026-08-08の「廃棄（直近1年）」を率へ・作者仕様2026-08-10 項目6）。
        //   定義＝**直近1年の廃棄量 ÷ 直近1年の生産量**＝Σ_active州 Market.SpoiledLastYear ÷ (月ごとの生産 × 12)。
        //   廃棄量＝腐敗・劣化で在庫から消えた量（EconomyStep が月次リングへ記帳）。数量は出さない（率だけ）。
        // ★行は常に出す（0%でも・生産0なら「—」）。
        double spoilYr = 0;
        foreach (var rg in _player.Regions)
            if (gi < rg.Markets.Length) spoilYr += rg.Markets[gi].SpoiledLastYear;
        // ★実用ゼロで足切り（幻の生産2026-08-10）＝旧 1e-9 では、目標0のまま残った 1e-8 級の産出が分母に立ち
        //   「廃棄率 12,543,372%」（分子＝初期在庫200の腐敗＝実量）になっていた。
        // ⚠分子（直近12ヶ月合計）と分母（今月の生産×12ヶ月）は別窓＝生産が今月だけ落ちた財では分母が痩せて跳ねる（cross_file_needs参照）。
        //   当面の最小修正：裸の12だけ Calendar.MonthsPerYear に置換。
        double spoilRate = !unavail && prod[gi] > QtyEps ? spoilYr / Calendar.PerMonthToPerYear(prod[gi]) : double.NaN;

        // ★補助金は国営持分に効かない（作者裁定2026-08-10「国営工場は補助金の対象外」）＝可否と理由はエンジンの
        //   World.CanSubsidize から取る（UIに第二の式を置かない）。品目のボタンは全 active 州へ一括で効くので、
        //   **どこか1州でも民営持分が残っていれば押せる**（1棟だけ国営でボタンが死ぬ崖を作らない）。
        bool subOk = false; double subPriv = 0; string? subTip = "産地なし";
        for (int ri = 0; ri < _player.Regions.Count; ri++)
        {
            var (ok, priv, reason) = _player.CanSubsidize(ri, gi);
            if (ok) { subOk = true; subPriv = Math.Max(subPriv, priv); }
            else if (reason == "国営100%") subTip = reason;
        }
        if (subOk) subTip = subPriv < 1.0 ? $"民営持分 {subPriv:P0}" : null;

        var trade = BuildTradeInfo(gi, unavail);
        var (sparkPts, sparkGrid, sparkTip) = unavail ? (new global::Avalonia.Points(), SparkGridEmpty, (string?)null) : BuildRatioSpark(gi);   // まだ存在しない財は価格グラフを描かない（枠は残る＝下が跳ねない）
        var detail = new ProdDetail(_player, gi, RefreshProduction, subsRate[gi], r => SetItemSubsidyRate(gi, r),
                                    _player.TradeBiasOf(gi), b => _player.SetTradeBias(gi, b))
        {
            ProdShareLine = trade.ProdShare,
            ExportShareLine = trade.ExportShare,
            TopProducerLine = trade.TopProducer,
            TopExporterLine = trade.TopExporter,
            IsOpium = gi == _player.Coef.OpiumGoodId && _player.Coef.OpiumAddictionGrowth > 0,   // アヘン禁令を生産タブのアヘンノードに出す（2026-07-20ユーザー指示）
            Title = d.Name,
            Tier = TierLabel(gi),   // 見出しだけ素材の産地区分を出す（作者要望2026-09-07 (4)）
            // 項目7（2026-08-10）：詳細パネルでも財名の横に世界価格との比を出す（カードと同じ様式＝安い青・高い赤）。
            //   式はカード（RefreshProduction）と同一＝首都価格×為替／世界価格−1。
            PriceDiff = !unavail && world > 1e-6 && local > 1e-9 ? (local * ex / world - 1.0).ToString("+0%;-0%;0%") : "",
            PriceDiffBrush = world > 1e-6 && local > 1e-9 && local * ex / world - 1.0 > 0 ? PPriceAbove : PPriceBelow,
            LegalMonopoly = legalMonopoly,
            // 数値ブロック＝**2行**（作者指示2026-08-10 C）：
            //   1行目 RGO生産量　工場生産量　輸入量／2行目 国内消費　輸出量　廃棄率
            // 国内消費＝Market.LastSold の合算＝**量**（旧「国内消費率」＝比は生産量・輸出量から読めるので廃止）。
            // RGO/工場の割り方は Producer.IsRgo（＝Catalog.RgoFacilityName と同じ判定）。合計＝Market.LastProduced の全国合算。
            // 両方を常時出す（0でも「—」にしない＝★横断原則）。量は全て k 単位（Qty）＝「200」でなく「0.2k」。
            // 廃棄率＝直近1年の廃棄量÷直近1年の生産量（生産0の月は「—」）。
            // ★まだ存在しない財は量も「—」（作者裁定2026-08-10）＝産出は0でも、開始在庫（Simulation の Stock=200）が
            //   交易で動くと輸出入だけ数字が立つ＝「誰も作っていない財の輸出」が見えてしまう。全部まとめて伏せる。
            NatLine = unavail ? "RGO生産量:—　工場生産量:—　輸入量:—"
                              : $"RGO生産量:{Fmt.Qty(prodRgo[gi])}　工場生産量:{Fmt.Qty(prodFac[gi])}　輸入量:{Fmt.Qty(imp[gi])}",
            NatLine2 = unavail ? "国内消費:—　輸出量:—　廃棄率:—"
                               : $"国内消費:{Fmt.Qty(sold[gi])}　輸出量:{Fmt.Qty(exp[gi])}　廃棄率:{(double.IsNaN(spoilRate) ? "—" : spoilRate.ToString("P0"))}",
            WorldPriceLine = !unavail && world > 1e-6 ? $"世界価格 £{world:F2}" : "世界価格 —",   // 価格グラフの中央線が指す実数値
            WorldSparkPoints = sparkPts,
            SparkGrid = sparkGrid,
            SparkScaleTip = sparkTip,
            // 「製品1あたり: …」「月ごとの消費: …」の2行は廃止（作者フィードバック2026-08-10 項目4）＝
            // レシピの必要量は生産コスト欄の各行「材名(×数量)」が、今月の実消費は同じ行の金額が既に持っている。
            HasUses = uses.Count > 0,
            CanSubsidize = subOk,
            SubsidyTip = subTip,
        };
        // 以下は生成後に器へ積む（PriceStack / CostRows は据え置き同期のため get-only）。
        //
        // ★設備の節（稼働率・従業員・技術による産出上限・産地）は**全廃**（作者フィードバック2026-08-10 項目5）＝州別の表と重複していた。
        //   稼働率の内訳だけは州別テーブルの「生産」列のツールチップへ移設（上の utilTip）＝式の担保（ProductionDiagTests）はそのまま。
        // 右段＝価格の積み上げ（項目8）＝エンジンの実式から。合計は表示している価格と厳密に一致する（ProductionDiagTests が固定）。parts は独占バッジと同じ1個体（上で算出済み）。
        BuildPriceRows(parts, world, detail.PriceStack, unavail);
        detail.PriceTotalText = unavail ? "" : $"£{parts.BuyerPrice:F2}";   // ★出せないときは空（作者指示2026-08-11＝「—」は情報量がない）
        detail.PriceTotalTip = unavail ? null : $"市場価格 £{parts.MarketPrice:F2}";   // 段1（基本価格×物価水準×需要と供給）の小計＝データのツールチップ
        // 左段＝生産にかかるコストの羅列（エンジンの限界費用 mc の内訳）。合計は mc と一致する（番人テスト）。
        BuildCostRows(ProductionDiag.ExplainCostByRecipe(_player, cap0, gi), detail);
        foreach (var r in rows) detail.Regions.Add(r);
        foreach (var u in uses) detail.Uses.Add(u);
        return detail;
    }

    /// <summary>巨大建造物の一覧（自国が建設地の州を持つものだけ）。★着工は**プレイヤーの判断**（作者裁定2026-08-02）＝
    /// 工場と同じく「コストを出してクリック」。ボタンは常に出し、押せない時はグレーアウト＋理由をツールチップ（工場の流儀）。
    /// 実体は StartMega → Megaprojects.TryStart で、AIの StartMegaproject と同一経路（機構は対称・判断だけが違う）。
    /// コストは見積り＝資材相場で実支出は上下する（工場の FactorySpec.Cost のような固定額ではない）。取り消しは無い。</summary>
    internal void BuildMegaRows()
    {
        var list = new List<MegaRow>();
        foreach (var mp in _sim.Diplomacy.Megaprojects)
        {
            if (mp.Owner is not { } o || !ReferenceEquals(o, Me)) continue;   // 自国が建設地を保有するものだけ
            double pct = mp.BuildUnits > 1e-9 ? System.Math.Clamp(mp.BuildProgress / mp.BuildUnits, 0, 1) : 0;
            bool techOk = _player.TechLevel >= mp.TechGate;
            var host = _player.Regions.FirstOrDefault(r => r.Name == mp.Host);
            double cost = host is null ? 0 : Megaprojects.EstimateCost(host, mp, _player.Coef);
            bool canStart = Megaprojects.CanStart(_player, mp);
            list.Add(new MegaRow
            {
                Name = mp.Name,
                Host = mp.Host,
                // ★行に「技術n%（不足）」は出さない（作者指示2026-08-02）＝押せない理由はボタンのホバーで読む。
                State = mp.IsComplete ? "完成" : mp.Started ? $"建設中 {pct:P0}" : "未着工",
                Effect = $"{JaEffect(mp.EffectTarget)} {(mp.EffectMode == "scale" ? $"×{1 + mp.EffectValue:F2}" : $"＋{mp.EffectValue:F0}")}",
                Brush = mp.IsComplete ? PPlus : techOk ? PUtilText : PNameText,
                CanStart = canStart,
                ShowStart = !mp.IsComplete,   // ボタンは完成後だけ消す（それ以外は出してグレーアウト＝工場の流儀）
                StartLabel = $"着工 {Fmt.Money(cost)}",
                // データのツールチップ（規律準拠＝説明文でなく値）。行には理由を書かない＝カーソルを合わせればここが出る。
                StartReason = $"{mp.Host}/残り {mp.BuildUnits - mp.BuildProgress:F0} 束/見積 {Fmt.Money(cost)}"
                            + (techOk ? "" : $"\n技術:{(mp.TechGate > 1e-9 ? _player.TechLevel / mp.TechGate : 1):P0}(不足)"),
                Project = mp,
            });
        }
        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));   // 決定論の固定順
        MegaRows = list;
        OnPropertyChanged(nameof(HasMegaRows));
    }
    /// <summary>着工（動詞・2026-08-02）。★AIの StartMegaproject と**同じ Megaprojects.TryStart** を通る＝
    /// プレイヤーだけが持つ経路も、AIだけが持つ経路も作らない。取り消しは無い（半分掘った運河は埋め戻せない）。</summary>
    [RelayCommand]
    private void StartMega(MegaRow? row)
    {
        if (row?.Project is not { } mp) return;
        if (!Megaprojects.TryStart(_player, mp)) return;
        BuildMegaRows();
    }

    private static string JaEffect(string t) => t switch
    {
        "ArableLand" => "耕地", "GridCapacity" => "送電容量", "Industrialization" => "工業化", "Prestige" => "威信", _ => t,
    };

    /// <summary>用途行のクリック＝その財をグラフ上で選択（詳細パネルも切替）。</summary>
    [RelayCommand] private void SelectUseGood(int gid) => SelectProdGood(gid);

    // ===== 財パネル拡張（外国経済doc 3-1＋貿易特化レバー3・2026-07-24）＝世界シェア・首位生産国・世界価格スパークライン =====
    // （輸入依存度・首位供給/需要国・独占上乗せは2026-08-10 項目2で全廃＝他の行と重複していた）
    private List<double>[]? _wpHist;   // 対世界価格の比の直近40四半期（DetectEvents＝四半期ごとにサンプル。★開始前の助走4四半期もここに入る＝2026-08-10 項目1）
    private int _wpVersion;            // 標本の版（四半期ごとに進む）。点列の記憶はこれで無効化する＝40本で満杯後は点数が変わらないので点数はキーにできない
    private global::Avalonia.Points[]? _wpSpark;   // 財ごとのスパークラインの点列（版が同じなら使い回す）
    private Geometry[]? _wpGrid;                   // 財ごとの補助線（枠が変わらない限り同じ実体＝再描画が起きない）
    private string[]? _wpTip;                      // 財ごとの目盛りのツールチップ
    private int[]? _wpSparkVer;

    /// <summary>価格グラフの標本（作者仕様2026-08-10）。★世界価格の実数ではなく**首都価格÷世界価格**を積む
    /// ＝グラフの中央線が世界価格（比＝1.0）になり、上下が「世界より高い／安い」を意味する。
    /// 物差しは見出しの価格割合・節点カードの％と同一（局所価格×為替／世界価格）＝画面のどこでも同じ数を指す。
    /// ★呼ばれるのは2箇所＝開始前の助走（<see cref="MultiNationSim.WarmupEconomy"/> の各四半期・4点）と本編の四半期（DetectEvents）。
    /// どちらも刻みは四半期＝x軸は一様のまま（項目1・2026-08-10）。</summary>
    private void SampleWorldPrices()
    {
        _wpHist = SampleWorldPrices(_sim, _player, _wpHist);
        _wpVersion++;
    }
    /// <summary>標本の本体（唯一定義）。開始前の助走（MainWindowViewModel＝GameViewModel がまだ無い）も同じ1本を呼ぶ＝式と40本上限が2つに割れない（2026-09-11 #19a）。
    /// <paramref name="hist"/> が null なら作って返す。</summary>
    internal static List<double>[] SampleWorldPrices(MultiNationSim sim, World player, List<double>[]? hist)
    {
        var wp = sim.Market.Price;
        var cap = player.Region;
        double ex = player.ExchangeRate;
        hist ??= Enumerable.Range(0, wp.Count).Select(_ => new List<double>()).ToArray();
        for (int g = 0; g < wp.Count && g < hist.Length; g++)
        {
            var h = hist[g];
            double local = g < cap.Markets.Length ? cap.Markets[g].Price : 0;
            h.Add(wp[g] > 1e-6 && local > 1e-9 ? local * ex / wp[g] : double.NaN);   // 値が無い四半期は非有限＝点ごと落ちる（線は途切れる）
            if (h.Count > 40) h.RemoveAt(0);
        }
        return hist;
    }

    // 価格の分解（#128・2026-07-25／★2026-08-10に「積み上げて最後に大きく合計」へ改装＝作者仕様 項目8）。
    //
    // ★式は**エンジンから導出**する（UI に第二の式を置かない）＝ProductionDiag.ExplainPrice が
    //   MarketFlow.StepDay と EconomyStep の買い手価格を1箇所で再評価し、ここはその戻り値を文字に直すだけ。
    //   番人テスト（ProductionDiagTests）が「積み上げ＝実際の価格」を恒等式として固定している。
    //
    //   段1（市場価格＝首都価格）  基本価格 × 物価水準 × 需要と供給 ＝ 市場価格
    //   段2（買い手価格）          市場価格 × (1＋専売＋流通コスト＋独占の影響＋同業組合) × (1−社会保障) ＝ 価格
    //   ＝エンジンの語彙（DistributionMargin／MonopolyRent／GuildRent／Insurance）に対する画面の呼び名（作者指定2026-08-10）。
    //
    // ⚠関税・穀物法・寡占・在庫は**行にしない**＝局所価格に乗る項ではなく「需要と供給」を動かす駆動値
    //   （関税は輸入の敷居＝InternationalMarket、寡占とSoftBandは価格という状態変数に積分済み）。
    //   行に足すと二重計上になるので、「需要と供給」行のツールチップで読ませる。
    // ★行は**常に出す**（作者原則2026-08-10）＝値が0でも「0%」で埋める。旧は専売・関税・独占が
    //   出たり消えたりし、下の州別リストが毎日上下していた（着工ボタンが逃げる原因B と同じ病）。
    // ★2026-08-10の再構成：段1（乗法）と段2（楔）を**1本の積み上げ**に畳んだ＝作者指定の7項目＋合計「価格」。
    //   中間小計だった「市場価格」の行は廃止（段1の積＝合計行のツールチップで読む）。
    /// ★<paramref name="unavailable"/>＝まだ存在しない財（作者裁定2026-08-10）。行は**消さず**（消すとレイアウトが動く）、
    ///   値を全て「—」にする。基本価格を伏せる以上、そこから積み上がる段も合計も数として出さない。
    ///   ⚠<see cref="Catalog.RefPrices"/> 自体は撤去しない（建設費の換算・材料価格の物差し・交易判定が構造定数として読む）。
    private static void BuildPriceRows(ProductionDiag.PriceParts p, double world, ICollection<ProdPriceRow> stack, bool unavailable)
    {
        // ★まだ存在しない財は**行を1つも積まない**（作者指示2026-08-11）＝「—」だけの積み上げは読む物がない。
        if (unavailable) return;
        // 「需要と供給」の駆動値＝在庫の詰まり具合・寡占の上乗せ・輸入の敷居（関税/穀物法）・世界価格。
        string? sdTip = $"在庫/目標 {(double.IsNaN(p.StockRatio) ? "測定なし" : p.StockRatio.ToString("P0"))}"
                     + $"\n関税の楔 {Fmt.SignedPct(p.TariffWedge)}"
                     + $"\n穀物法の楔 {Fmt.SignedPct(p.CornLawWedge)}"
                     + $"\n世界価格 {(world > 1e-6 ? $"£{world:F2}" : "—")}";
        // ★0%の項は出さない（作者指示2026-08-11）＝「独占の影響 +0%」のように、効いていない項目が並ぶと
        //   どれが効いているのか読み取れない。⚠2026-08-10の「行は常に出す」原則の**撤回**＝あれは
        //   「出たり消えたりで下のリストが上下する」対策だったが、価格の積み上げは行数が少なく上下の実害が小さい。
        void AddIfAny(string name, double v, string? tip = null)
        {
            if (!(System.Math.Abs(v) >= 0.005)) return;   // 表示が「0%」に丸まる項は置かない（否定形＝NaN も行を積まない）
            stack.Add(new ProdPriceRow(name) { Value = Fmt.SignedPct(v), Tip = tip });
        }
        stack.Add(new ProdPriceRow("基本価格") { Value = $"£{p.RefPrice:F2}" });   // 起点＝常に出す
        AddIfAny("物価水準", p.PriceLevel - 1.0);
        AddIfAny("需要と供給", p.SupplyDemand - 1.0, sdTip);
        AddIfAny("専売", p.StateMonopoly);
        AddIfAny("流通コスト", p.DistributionMargin);
        AddIfAny("独占の影響", p.MonopolyRent);
        AddIfAny("同業組合", p.GuildRent);   // 同業組合のレント（GuildRentPremium・EconomyStep.BuyerWedge の項＝無いと積み上げが買値と合わない・2026-09-11 M5）
        AddIfAny("社会保障", p.Insurance > 0 ? -p.Insurance : 0.0);   // 負のゼロ（-0.0）は「−0%」と出るので正のゼロへ寄せる
    }

    /// <summary>左段＝生産にかかるコストの羅列（作者仕様2026-08-10）。
    /// エンジンの限界費用 <c>mc = MineralSelfCost（MarginalCost×自己費用の物価水準×州の乗数） + InputCostPerUnit</c>（EconomyStep）の内訳を
    /// <see cref="ProductionDiag.ExplainCost"/> が1箇所で再評価し、ここはその戻り値を文字に直すだけ
    /// （UI に第二の式を置かない）。合計＝mc の一致は番人テストが固定する。
    /// ★書式（作者フィードバック2026-08-10 項目1）：左＝<c>果実(×0.5)</c>（材名と産出1単位あたりの必要量）／
    /// 右＝<c>£0.43×94% = £0.20</c>。
    ///   ・比の**基準を世界価格から基本価格（<see cref="Catalog.RefPrices"/>）へ変更**＝右段の積み上げ（基本価格→物価水準→…）と
    ///     同じ物差しになる（100%＝基本価格どおり。物価水準ぶんは全材に等しく乗る）。色は安い青・高い赤で100%が境。
    ///   ・★第5ラウンド（同日）：「基本価格」という**語でなくその材の基本価格の実数**を出す＝
    ///     <c>£0.43×94%</c> の £0.43 が <c>Catalog.RefPrices[材]</c>。何と比べているのかが読み手の記憶に依存しない。
    ///   ・£は従来どおり 数量×その州の価格（＝<see cref="ProductionDiag.CostItem.Amount"/>）。
    /// ★行は常に出す＝人件費・管理・資本費は生産者が国内に居ない財では「—」（0で埋めない）。数量を持たないので左に名前・右に金額。</summary>
    private void BuildCostRows(ProductionDiag.CostParts[] recipes, ProdDetail detail)
    {
        for (int ri = 0; ri < recipes.Length; ri++) BuildCostRowsOne(recipes[ri], ri, detail);
    }

    /// <summary>レシピ1本ぶんの材料行と、その末尾の合計行（作者要望2026-09-07 (3)）。
    /// ★据え置き同期のキー（<see cref="ProdCostRow.Name"/>）に**レシピの番号を前置**する＝同じ材名が複数のレシピに出ても
    ///   キーが衝突しない。衝突すると SyncRows が毎更新で全行を作り直し、ツールチップがホバー中に消える。
    /// ★合計行＝左にレシピ式（材名×係数を「＋」で連結）・右に「生産コスト £n」を右寄せ。1本しか無い財でも同じ形。</summary>
    private void BuildCostRowsOne(ProductionDiag.CostParts c, int recipeIndex, ProdDetail detail)
    {
        string key = recipeIndex.ToString() + "|";
        var expr = new System.Text.StringBuilder();
        foreach (var it in c.Items)
        {
            if (it.IsLabor) continue;   // 労働は人件費の1行へまとめる（内訳はその行のツールチップ）
            expr.Append(expr.Length > 0 ? " ＋ " : "").Append($"{Catalog.Goods[it.GoodId].Name}×{it.Qty.ToString("0.###")}");
            double rp = it.GoodId >= 0 && it.GoodId < Catalog.RefPrices.Length ? Catalog.RefPrices[it.GoodId] : 0;
            bool hasRef = rp > 1e-9 && it.UnitPrice > 1e-9;
            double ratio = hasRef ? it.UnitPrice / rp : 0;
            detail.CostRows.Add(new ProdCostRow(key + Catalog.Goods[it.GoodId].Name)
            {
                Label = $"{Catalog.Goods[it.GoodId].Name}(×{it.Qty.ToString("0.###")})",
                Fill = $"供給:{it.Fill:P0}",   // ★材別の供給率（作者指示2026-08-10 F）＝Market.LastRation（エンジンの配給率そのもの）
                FillBrush = it.Fill < MaterialFillThreshold ? PMinus : PUtilText,
                Ratio = hasRef ? $"£{rp:F2}×{ratio:P0}" : "—",   // 基準＝その材の基本価格の**実数**（項目1・2026-08-10 第5ラウンド）
                RatioBrush = !hasRef ? PUtilText : ratio <= 1.0 ? PPriceBelow : PPriceAbove,
                Amount = $"= £{it.Amount:F2}",
            });
        }
        // 人件費にも同じ作法で労働の充足を出す（作者の判断依頼2026-08-10 F）＝出せる。労働も市場で配給される財なので
        // 素材とまったく同じ Market.LastRation を持つ。3種の労働（労働力/高度労働力/農業労働力）を使う工場では
        // **最小**を出す＝律速している方（素材行の「不足」と同じ選び方）。労働を1つも買わない財は行の性質上「—」。
        var lab = new System.Text.StringBuilder();
        double labFill = double.NaN;
        foreach (var it in c.Items)
            if (it.IsLabor)
            {
                lab.Append(lab.Length > 0 ? "\n" : "").Append($"{Catalog.Goods[it.GoodId].Name} ×{it.Qty:0.###}　供給:{it.Fill:P0}　£{it.Amount:F2}");
                if (double.IsNaN(labFill) || it.Fill < labFill) labFill = it.Fill;
            }
        detail.CostRows.Add(new ProdCostRow(key + "人件費")
        {
            Label = "人件費",
            Fill = double.IsNaN(labFill) ? "供給:—" : $"供給:{labFill:P0}",
            FillBrush = !double.IsNaN(labFill) && labFill < MaterialFillThreshold ? PMinus : PUtilText,
            Amount = c.HasProducer ? $"£{c.LaborCost:F2}" : "—", RatioBrush = PUtilText, Tip = lab.Length > 0 ? lab.ToString() : null,
        });
        detail.CostRows.Add(new ProdCostRow(key + "管理費と資本費") { Label = "管理費と資本費", Fill = "供給:—", Amount = c.HasProducer ? $"£{c.OwnCost:F2}" : "—", RatioBrush = PUtilText, FillBrush = PUtilText });
        // 合計行＝左にレシピ式・右に生産コスト（右寄せ）。材料を買わない財（RGO）は式が空なのでレシピ名を出す。
        detail.CostRows.Add(new ProdCostRow(key + "合計")
        {
            IsTotal = true,
            Label = expr.Length > 0 ? expr.ToString() : c.Recipe,
            Amount = $"生産コスト £{c.Mc:F2}",
            Tip = c.Recipe.Length > 0 ? c.Recipe : null,   // レシピ名（ハーバーボッシュ／グアノ肥料…）＝データのツールチップ
            RatioBrush = PUtilText, FillBrush = PUtilText,
        });
    }

    /// <summary>稼働率の内訳（作者仕様2026-08-10 項目5「低い稼働率の原因がわからない」）。
    /// 旧UIは**素材のみの充足率**しか出しておらず、労働不足・採算割れ・在庫過剰が画面から読めなかった＝原因を追えない直接の理由。
    /// エンジンの実式（EconomyStep ①供給）を <see cref="ProductionDiag.ExplainUtilization"/> が再評価し、
    /// <c>採算 × 投入充足 × 在庫調整 × 慣性 ＝ 稼働率</c> の恒等式で返す。ここはその戻り値を文字に直すだけ。
    /// 素材のみの配給率（LastMaterialFill）は積に入らない併記＝投入充足との差が**労働不足の量**。
    /// ★2026-08-10（項目5）：設備欄の全廃に伴い、この文字列は**州別テーブルの「生産」列のツールチップ**になった。
    ///   分解は全国値（集計範囲は節点カードと同一＝active州・非倒産）なので、稼働率の行にも「全国の」と明記する。</summary>
    private string BuildUtilTip(ProductionDiag.UtilParts u)
    {
        string P(double v) => u.Valid ? v.ToString("P0") : "—";
        return $"全国の稼働率 {(u.Capacity > QtyEps ? u.Utilization.ToString("P0") : "—")}"
             + $"\n採算 {P(u.ProfitFactor)}"
             + $"\n投入充足 {P(u.InputFill)}(素材のみ {(u.Output > QtyEps ? u.MaterialFill.ToString("P0") : "—")})"   // 産出が事実上0では加重平均の分母が消える＝「—」
             + $"\n在庫調整 {P(u.InventoryAdjust)}"
             + $"\n慣性による遅れ {P(u.Inertia)}"
             + $"\n能力 {Fmt.Qty(u.Capacity)}　産出 {Fmt.Qty(u.Output)}";   // 量はk統一（項目4・2026-08-10）
    }

    /// <summary>世界に対する位置（作者仕様2026-08-10）。数値ブロック3行目（生産シェア・輸出シェア）と
    /// 4行目（世界最大の生産国）を1度の走査で作る。
    /// ★貿易行（輸入依存度・独占上乗せ・首位輸出国・首位輸入国）は**全廃**（作者フィードバック2026-08-10 項目2
    ///   「全て不要。重複しているため」）＝輸入量は数値ブロック1行目、独占は見出しの独占バッジと価格の積み上げ
    ///   「独占の影響」、首位国はこの4行目が既に持っていた。読み手が消えたので走査（総需要・ImportsByGood・HHI）ごと落とす。</summary>
    /// ★<paramref name="unavailable"/>＝まだ存在しない財（世界のどこにも無く誰も作れない）は4行とも「—」。
    ///   産出が0でも**開始在庫（Stock=200）の交易**で輸出量だけが立つため、シェアの分母が数量の足切りをすり抜けることがある
    ///   （作者スクリーンショット2026-08-10＝1783年の機関銃で「トスカーナが世界の99%を輸出」）。判定は財の存在そのもので行う。
    private (string ProdShare, string ExportShare, string TopProducer, string TopExporter) BuildTradeInfo(int gi, bool unavailable)
    {
        // ★出せないときは行ごと出さない（作者指示2026-08-11）＝「—」だけの行は情報量ゼロで場所だけ取る。
        if (unavailable) return ("", "", "", "");
        var w = _player;
        // ⚠旧「国内消費率」＝(国内生産−輸出)÷国内生産 は廃止（2026-08-10の再構成）＝生産量・輸出量が
        //   数値ブロック1行目に量で並んだので、同じ2つの数から作る比は重複。行き先の内訳は引き算で読める。
        double myProd = 0, worldProd = 0, myExp = 0, worldExp = 0;
        // 世界最大の生産国（作者仕様2026-08-10）＝国ごとの active 州の LastProduced 合算の首位。自国が首位なら自国名が出る。
        string topProd = ""; double topProdV = 0;
        // 世界最大の**輸出国**（作者指示2026-08-10 D）＝ExportsByGood の首位。生産の首位と別の国になる（作って自国で食う国／作らず中継する国）
        string topExp = ""; double topExpV = 0;
        foreach (var n in _sim.Nations)
        {
            double pn = 0;
            foreach (var r in n.World.Regions) if (gi < r.Markets.Length) pn += r.Markets[gi].LastProduced;
            worldProd += pn;
            if (pn > topProdV) { topProdV = pn; topProd = n.World.SelfPower?.Name ?? ""; }
            if (ReferenceEquals(n.World, w)) myProd = pn;
            if (n.World.ExportsByGood is { } ne && gi < ne.Length)
            {
                worldExp += ne[gi];
                if (ne[gi] > topExpV) { topExpV = ne[gi]; topExp = n.World.SelfPower?.Name ?? ""; }
                if (ReferenceEquals(n.World, w)) myExp = ne[gi];
            }
        }
        // 実用ゼロ（QtyEps）で足切り＝世界の総量が事実上0の財で、ノイズ同士の比を「世界の12%」と出さない（幻の生産2026-08-10）
        return (worldProd > QtyEps ? $"生産シェア {myProd / worldProd:P0}" : "",
                worldExp > QtyEps ? $"輸出シェア {myExp / worldExp:P0}" : "",
                worldProd > QtyEps && topProdV > QtyEps ? $"{topProd}が世界の{topProdV / worldProd:P0}を生産" : "",
                worldExp > QtyEps && topExpV > QtyEps ? $"{topExp}が世界の{topExpV / worldExp:P0}を輸出" : "");
    }

    /// <summary>世界価格スパークラインの点列（2026-08-02に文字列→Points へ変更）。
    /// ★旧実装は "x,y x,y …" の**文字列**を Polyline.Points へバインドし、Avalonia の型変換に解析させていた。
    ///   価格に非有限値が1つでも混じると "NaN,NaN" となり、変換が **null** を返す。Polyline は Points が null だと
    ///   MeasureOverride の CreateDefiningGeometry で ArgumentNullException を投げ、**毎フレーム**レイアウト/描画が
    ///   中断する＝「商品をクリックするとフリーズする」の正体（作者報告2026-08-02）。
    ///   点列を自前で組めば解析が描画経路から消え、この種の失敗が構造的に起きなくなる。非有限値は点ごと落とす
    ///   （否定形の判定＝NaN も弾く）。
    /// ★2026-08-10：系列を**対世界価格の比**へ変更（作者仕様）。中央（y=32）が比1.0＝世界価格。
    /// ★同日の項目2：縦を**対数**にした＝比は0未満にならないので、×b と ×(1/b) が中心線から等距離に来る唯一の整合的な置き方。
    ///   y ＝ 32 − ln(比) ÷ ln(枠) × 30。枠は <see cref="SparkBrackets"/>（×1.25/1.5/2/3/5＝＋25/50/100/200/400%）の中から
    ///   **窓の全点が収まる最小のもの**を選ぶ（連続的な伸縮をやめた＝旧 amp 方式では同じ縦位置が財ごと・月ごとに別の比を指していた）。
    ///   補助線は枠に収まる段階とその逆数に引く（中心線は常に y=32＝XAML の1本）。データが最大枠（×5）を超える月は点が上下端へ張り付く。</summary>
    private (global::Avalonia.Points Pts, Geometry Grid, string? Tip) BuildRatioSpark(int gi)
    {
        if (_wpHist is null || gi < 0 || gi >= _wpHist.Length) return (new(), SparkGridEmpty, null);   // 標本を1度も採っていない（ロード直後）＝ツールチップも出さない
        // ★2026-08-10：詳細パネルは**日次**（速度3で毎秒約20回）に組み直されるが、世界価格の標本は四半期にしか動かない。
        //   版が同じなら同じ実体を返す＝バインド先の Polyline / Path に同一参照が入り、再描画そのものが起きない。
        _wpSpark ??= new global::Avalonia.Points[_wpHist.Length];
        _wpGrid ??= new Geometry[_wpHist.Length];
        _wpTip ??= new string[_wpHist.Length];
        if (_wpSparkVer is null) { _wpSparkVer = new int[_wpHist.Length]; System.Array.Fill(_wpSparkVer, -1); }
        if (_wpSparkVer[gi] == _wpVersion && _wpSpark[gi] is { } memo) return (memo, _wpGrid[gi] ?? SparkGridEmpty, _wpTip[gi]);
        var h = _wpHist[gi];
        // 枠＝|ln(比)| の最大が収まる最小の段階。有限な点が1つも無ければ最小枠（×1.25）。
        double maxDev = 0, last = double.NaN;
        foreach (double v in h) { if (!Finite(v)) continue; maxDev = Math.Max(maxDev, Math.Abs(Math.Log(v))); last = v; }
        double frame = SparkBrackets[^1];
        foreach (double b in SparkBrackets) if (maxDev <= Math.Log(b) + 1e-12) { frame = b; break; }
        double lnFrame = Math.Log(frame);
        var pts = new global::Avalonia.Points();
        for (int i = 0; i < h.Count; i++)
        {
            if (!Finite(h[i])) continue;   // 非有限・非正は点ごと落とす（線は途切れる）
            double x = 240.0 * i / Math.Max(1, h.Count - 1);
            double y = Math.Clamp(SparkMidY - Math.Log(h[i]) / lnFrame * SparkHalfH, 2.0, 62.0);   // 上＝世界より高い
            pts.Add(new global::Avalonia.Point(x, y));
        }
        // 補助線＝枠以下の各段階とその逆数。1px の線が2行に滲まないよう半ピクセルへ寄せ、上下は中心から同じ距離に置く。
        var geo = new StreamGeometry();
        var levels = new List<double>();
        using (var ctx = geo.Open())
            foreach (double b in SparkBrackets)
            {
                if (b > frame + 1e-12) break;
                levels.Add(1.0 / b); levels.Add(b);
                double dy = Math.Round(Math.Log(b) / lnFrame * SparkHalfH) + 0.5;
                Line(ctx, SparkMidY - dy); Line(ctx, SparkMidY + dy);
            }
        levels.Sort();
        string tip = $"枠 ×{1.0 / frame:0.##}〜×{frame:0.##}"
                   + $"\n目盛り {string.Join("・", levels.Select(v => $"×{v:0.##}"))}"
                   + $"\n直近 {(Finite(last) ? $"×{last:0.###}" : "—")}"
                   + $"\n標本 {pts.Count}四半期";
        _wpSpark[gi] = pts; _wpGrid[gi] = geo; _wpTip[gi] = tip; _wpSparkVer[gi] = _wpVersion;
        return (pts, geo, tip);

        static bool Finite(double v) => v > 1e-12 && v < double.MaxValue;   // 否定形でなく下限つきの肯定形＝NaN も0以下も同時に弾く（対数が定義される値だけ通す）
        static void Line(StreamGeometryContext ctx, double y)
        {
            ctx.BeginFigure(new global::Avalonia.Point(0, y), false);
            ctx.LineTo(new global::Avalonia.Point(240, y));
            ctx.EndFigure(false);
        }
    }

    /// <summary>誰がこの財を買ったか（作者フィードバック2026-08-10 項目6）＝用途の先頭行のツールチップ。
    /// 階級名は<see cref="Pop.ClassName"/>（エンジンが持つ実名）をそのまま使う＝UI に階級の一覧を書き写さない。
    /// ★これは**好みではなく結果**＝1人あたりの必要量は全階級共通で、違うのは買える予算だけ。
    /// 量が0の月はツールチップを出さない（データが無いものに枠だけ作らない）。</summary>
    private string? BuyerShareTip(ProductionDiag.PopUseParts u)
    {
        if (!(u.Total > QtyEps)) return null;   // 事実上0の消費に階級別シェアを作らない（幻の生産2026-08-10）
        var classNames = new Dictionary<int, string>();
        foreach (var rg in _player.Regions)
        {
            foreach (var p in rg.Pops)
                if (p.ClassName.Length > 0 && !classNames.ContainsKey(p.ClassId)) classNames[p.ClassId] = p.ClassName;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var (ci, amt) in Enumerable.Range(0, u.ByClass.Length).Select(i => (i, u.ByClass[i]))
                                            .Where(t => t.Item2 > 1e-9).OrderByDescending(t => t.Item2).ThenBy(t => t.i))
            sb.Append(sb.Length > 0 ? "\n" : "")
              .Append($"{(classNames.TryGetValue(ci, out var nm) ? nm : ci.ToString())} {amt / u.Total:P0}　{Fmt.Qty(amt)}");
        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>州別の国営工場カウンターの増減（作者裁定2026-08-10）。州は**実体**で受け、押した瞬間に添字を引き直す
    /// ＝据え置き同期で使い回された行が別の州を操作するのを防ぐ。
    /// ★目標は必ず動く（意志は記録される）が、実現は既存のゲート（国庫の日次工賃・建材の採算・技術・買い手の資金と不満）が決める。
    ///   その場で1歩進めなかった月も詳細パネルを組み直す＝カウンターのツールチップに**なぜ進まないか**が今の相場で出る。</summary>
    private void StepStateTargetAt(Region rg, int gid, int delta)
    {
        int ri = _player.Regions.IndexOf(rg);
        if (ri < 0) return;
        var p = World.FindFactory(rg, gid);   // エンジンの動詞と**同じ1本**を掴む（代替レシピの取り違えを作らない＝World.FindFactory の全文）
        if (p is null) return;
        if (_player.SetStateTarget(ri, gid, p.StateTarget + delta)) Refresh(); else RefreshProduction();
    }

    /// <summary>全国工場目標の増減（作者要望2026-09-07 (1)）＝財カードの縦線の中の▲/▼（2026-09-23に＋/−から）＝**分母を動かす**。
    /// ★動かす起点はカードが出している分母そのもの（＝合計＋建設予定。目標がそれを超えていれば目標）
    ///   ＝押した数字と置く数字が一致する＝1回押せば必ず1動く。
    /// ★これは「意志」＝目標を上げただけで無から工場は湧かない。満たすまでエンジンが**最も効率の良い州**へ
    ///   月に1棟ずつ国営工場を建てる（World.StepFactoryTargets＝州の選び方はAIの物差しと同じ1本）。
    /// ★下げられるのは予定のうち**まだ州へ配っていない分**だけ（建った棟も着工した棟も壊さない
    ///   ／国営の払い下げは州別カウンター）。</summary>
    private void StepFactoryTargetAll(int gid, int delta)
    {
        _player.SetFactoryTarget(gid, _player.FactoryCounter(gid).Planned + delta);
        RefreshProduction();
    }

    /// <summary>品目別の補助率（州別でなく全州一括・2026-07-19に5段階化）。全 active 州の当該財の生産者に一律で率を適用。</summary>
    private void SetItemSubsidyRate(int gid, double rate)
    {
        for (int ri = 0; ri < _player.Regions.Count; ri++)
            _player.SetSubsidyRate(ri, gid, rate);
        RefreshProduction();
    }
}

// ノード＝財カード（位置は Canvas.Left/Top へ ReflectionBinding）。
// **可変プロパティ＝毎回の更新は既存インスタンスへ値だけ流す**（器の作り直し＝126枚×約8視覚要素の破棄・再生成を避ける・2026-08-10）。
// 品名と大きさは静的＝init のまま。位置は盤の視野の幅に追従して置き直す（(501) 段3＝分割の枠の幅に盤が合わせる）ので可変。
public sealed partial class ProdNode : ObservableObject
{
    public int GoodId { get; init; }
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    public double W { get; init; }
    public double H { get; init; }
    public string Name { get; init; } = "";
    [ObservableProperty] private IBrush? _nameBrush;        // 通常#2A2118／技術不足・産地なし＝灰(PDimText)
    [ObservableProperty] private string _priceDiff = "";    // 品名の隣＝首都価格の世界価格比（＋12%／−8%）。安い＝青・高い＝赤（2026-08-09 項目3）
    [ObservableProperty] private IBrush? _priceDiffBrush;
    [ObservableProperty] private IBrush? _accent;
    // 工場カウンター（作者要望2026-09-07 (1)）＝カテゴリを示す縦線を広げてその中に「今ある棟数/予定を含めた棟数」を出し、分母を▲/▼で動かす（2026-09-23）。
    // 民営＝完成した棟数の**1つ書き**（2026-09-23＝分子と分母が同じ値だった2つ書きをやめた）、
    // 国営＝完成/完成＋建設予定の2つ書き（World.FactoryCounter の1本）。満たすまでエンジンが最も効率の良い州へ国営工場を建てる。
    [ObservableProperty] private string _totalCount = "";     // 「合計12」「合計12+1」（国営＋民営の完成した棟数＋まだ建っていない意志）
    [ObservableProperty] private string _runningCount = "";   // 「稼働9」＝実質稼働数（棟数×稼働率）
    [ObservableProperty] private IBrush? _runningBrush;       // 稼働率で赤→黄→緑
    [ObservableProperty] private bool _canUpFactory;
    [ObservableProperty] private bool _canDownFactory;
    [ObservableProperty] private string? _factoryTip;   // 「国営 n棟　民営 n棟　目標 n棟」＝データのツールチップ
    private readonly Action? _onUpFactory, _onDownFactory;
    public ProdNode() { }
    public ProdNode(Action onUp, Action onDown) { _onUpFactory = onUp; _onDownFactory = onDown; }
    // ClickMode=Press と同じ理由でコマンドは軽い（押下で確定）。州別カウンター（ProdRegionRow）と同じ作法。
    [RelayCommand] private void UpFactory() => _onUpFactory?.Invoke();
    [RelayCommand] private void DownFactory() => _onDownFactory?.Invoke();
    [ObservableProperty] private IBrush? _background;
    [ObservableProperty] private IBrush? _borderBrush;
    [ObservableProperty] private double _borderThickness = 1.0;
    [ObservableProperty] private double _opacity = 1.0;
}


// 辺＝レシピ（ベジェ曲線）。形（Data）はレイアウトが静的＝一度だけ作る。選択で変わるのは色・太さ・不透明度・重なりだけ。
public sealed partial class ProdEdge : ObservableObject
{
    public int From { get; init; }
    public int To { get; init; }
    [ObservableProperty] private Geometry? _data;   // 盤の幅に追従して置き直す（<see cref="GameViewModel.SetProdViewport"/>）
    [ObservableProperty] private IBrush? _stroke;
    [ObservableProperty] private double _thickness = 1.3;
    [ObservableProperty] private double _opacity = 0.42;
    [ObservableProperty] private int _z;   // 強調した辺を上に描く（旧＝不透明度で並べ替えていた＝器の並べ替え＝コンテナ移動が走る）
}

// tier区切り線（作者指示2026-08-15）＝層（加工の深さ）の境目の水平1px。レイアウトが静的＝全プロパティ init
// （選択でも月次更新でも値を流し直さない＝ObservableObject を負わない）。
public sealed class ProdTierLine
{
    public double X { get; init; }
    public double Y { get; init; }
    public double W { get; init; }   // 0＝線なし（tier1のラベル専用エントリ）
    public string Label { get; init; } = "";   // tier番号ラベル（作者指示2026-08-16＝数字が大きいほど加工が深く作るのが難しい）
    public double LabelX { get; init; }
    public double LabelY { get; init; }
}

// 選択財の詳細（右パネル）。物品専売・補助金はトグル（品目別＝全国一括）。
// **可変プロパティ＝同じ財の間は既存インスタンスへ値だけ流す**（CopyFrom＝州別の建設/売却ボタンを日次更新で破棄しない・2026-07-19）。
public sealed partial class ProdDetail : ObservableObject
{
    public int GoodId { get; init; } = -1;
    // ★2026-09-20（M5）：専売のチェックボックスは**エンジンの真値1本**を読み書きする（UI側に鏡を持たない）。
    //   ⚠無引数コンストラクタ（設計時のみ）では null＝MonopolyOn は常に false・押しても何も起きない。
    private readonly World? _w;
    public bool IsOpium { get; init; }   // この財がアヘン（かつ依存機構ON）＝詳細パネルにアヘン禁令チェックを出す
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _tier = "";
    // 項目7（2026-08-10）：財名の横の世界価格比（カードと同じ様式＝安い青 #1F5FA8・高い赤 #B02418）。値が無い財は空文字
    [ObservableProperty] private string _priceDiff = "";
    [ObservableProperty] private IBrush? _priceDiffBrush;
    // 数値ブロック＝**2行**（作者指示2026-08-10）：
    //   1行目 RGO生産量　工場生産量　輸入量／2行目 国内消費　輸出量　廃棄率
    [ObservableProperty] private string _natLine = "";
    [ObservableProperty] private string _natLine2 = "";
    // 設備の節（FacSegs＝稼働率・従業員・技術による産出上限・産地）は全廃（作者フィードバック2026-08-10 項目5）＝州別の表と重複。
    // 稼働率の内訳は州別テーブルの「生産」列のツールチップ（ProdRegionRow.ProdTip）へ移設した。
    [ObservableProperty] private string _worldPriceLine = "";   // 世界価格＝価格グラフの中央線が指す実数値
    // 「製品1あたり」「月ごとの消費」「不足」の3行は全廃（作者フィードバック2026-08-10 項目4）＝
    //   レシピの必要量と実消費は生産コスト欄の各行「材名(×数量)　供給:n%　£…」が、どの材が足りないかも同じ行が持っている。
    // 廃棄率は数値ブロック2行目へ畳んだ（作者指示2026-08-10）／材料供給は生産コスト欄の材別「供給:n%」へ移した（同）
    [ObservableProperty] private string _prodShareLine = "";  // 生産シェア（世界の生産に占める自国の比）
    [ObservableProperty] private string _exportShareLine = ""; // 輸出シェア（世界の輸出に占める自国の比）
    [ObservableProperty] private string _topProducerLine = ""; // 「〇〇が世界のn%を生産」＝世界最大の生産国
    [ObservableProperty] private string _topExporterLine = ""; // 「〇〇が世界のn%を輸出」＝世界最大の輸出国（ExportsByGood の首位）
    // 価格の積み上げ（作者仕様2026-08-10 再構成）＝基本価格・物価水準・需要と供給・専売・流通コスト・独占の影響・同業組合・社会保障の8行。
    // 合計＝「価格」（PriceTotalText＝買い手が実際に払う価格）。中間小計の市場価格は合計行のツールチップ。
    // ★行数は**固定**（値が0でも消えない）。器も据え置き＝「需要と供給」のツールチップがホバー中に消えない。
    public ObservableCollection<ProdPriceRow> PriceStack { get; } = new();
    [ObservableProperty] private string _priceTotalText = "";   // 積み上げの合計＝popが実際に払う価格（大きく出す）
    [ObservableProperty] private string? _priceTotalTip;        // 市場価格（段1の小計）＝データのツールチップ
    // 生産コストの内訳（左段）＝限界費用 mc の内訳。★2026-09-07：レシピごとに分け、合計行（IsTotal）を各レシピの末尾に置いた
    // ＝合計は行の一部（旧 CostTotalText の器の外の1行は廃止＝レシピが2本以上のとき「どのレシピの合計か」が言えなかった）。
    public ObservableCollection<ProdCostRow> CostRows { get; } = new();
    // 財パネル拡張（外国経済doc 3-1＋貿易特化レバー3・2026-07-24）：世界シェア・首位生産国＋世界価格40四半期スパークライン
    // （TradeLine＝輸入依存度・独占上乗せ・首位輸出国・首位輸入国 は2026-08-10 項目2で全廃＝他の行と重複していた）
    /// <summary>全角スペース区切りの1行を、折返し可能な断片へ割る（2026-08-02・作者指摘
    /// 「輸入ウェアなどの項目が文字数で折り返されることがある」）。日本語は語境界を持たないので
    /// TextWrapping は**文字の途中**で割る。断片ごとに TextBlock を置き WrapPanel に並べれば、
    /// 折返しは必ず項目の切れ目で起きる。</summary>
    public static string[] Segs(string line) =>
        string.IsNullOrEmpty(line) ? [] : line.Replace("\n", "　").Split('　', System.StringSplitOptions.RemoveEmptyEntries);
    public string[] NatSegs => Segs(NatLine);
    public string[] NatSegs2 => Segs(NatLine2);
    partial void OnNatLineChanged(string value) => OnPropertyChanged(nameof(NatSegs));
    partial void OnNatLine2Changed(string value) => OnPropertyChanged(nameof(NatSegs2));
    // ★2026-08-02 描画クラッシュの修正：Avalonia の Polyline は Points が null だと CreateDefiningGeometry で
    //   ArgumentNullException を投げ、**毎フレーム**その例外で描画パスが中断する（＝操作を受け付けない「フリーズ」に見える）。
    //   ProdDetail 自体が null（財を未選択）だと `{Binding ProdDetail.WorldSparkPoints}` が null を返すため、
    //   「見えないから安全」という前提が成り立たない。**空文字を作らない**＋XAML側で TargetNullValue を置く、の二重で塞ぐ。
    // ★枠は**常に出す**（作者原則2026-08-10）＝標本が2点に満たない開幕だけ 68px の枠が消え、下の全てが跳ね上がっていた。
    //   点が無い時は空の枠（Polyline が0点＝何も描かない・null ではないのでクラッシュしない）。
    [ObservableProperty] private global::Avalonia.Points _worldSparkPoints = new();
    // 価格グラフの目盛り（作者仕様2026-08-10 項目2）＝補助線の形と、枠/段階/直近/標本数のツールチップ（データのみ）。
    // 中心線（比1.0）は常に y=32 なので XAML の1本（濃く太い）。補助線は淡い1px＝既存のグラフ作法に合わせる。
    [ObservableProperty] private Geometry? _sparkGrid;
    [ObservableProperty] private string? _sparkScaleTip;
    [ObservableProperty] private bool _legalMonopoly;   // 塩/茶の法的独占＝バッジ表示（独占機構が有効な時のみ）
    public ObservableCollection<ProdRegionRow> Regions { get; } = new();
    public ObservableCollection<ProdUseRow> Uses { get; } = new();   // 用途（この財の消費先・2026-07-19）
    [ObservableProperty] private bool _hasUses;
    [ObservableProperty] private double _subsidyRate;
    // ★国営100%の生産者は補助金の対象外（作者裁定2026-08-10）＝ボタンを無効化し、理由（World.CanSubsidize の文字列）を
    //   ツールチップで読ませる＝払い下げ（CanSellFactory）と同じ理由連鎖の作法。押せる時は効く持分（民営持分）をデータで出す。
    [ObservableProperty] private bool _canSubsidize = true;
    [ObservableProperty] private string? _subsidyTip;
    // 補助金・専売はチェックボックス（2026-07-21ユーザー指示：5段階ボタンから戻す）。ON＝補助金は満額(1.0)・専売は標準率。OFF＝0。
    // ★率はUIが持たない（作者裁定2026-09-20＝標準率は Coefficients.StateMonopolyMargin へ移設）。
    // ★★2026-09-20（M5）：専売は**鏡を持たない**＝読みも書きもエンジンの配列1本。旧は `_taxOnState`（UI側の真似）と
    //   `_onSetTax`（片道のコールバック）の2つで、「MonopolyOn をno-opにする」変異を入れてもチェックが点いたまま残り、
    //   xUnit の99件が全て緑のままだった（＝チェックボックスがエンジンへ届かなくなっても誰も気づかない）。
    //   鏡が無ければ嘘の表示を作れない＝押しても表示が戻らない形で必ず現れる。`_onChanged` は再描画のためだけ
    //   （これが no-op でも物理は正しい）。政治の門が通さなければセッターは何も変えず、getter はそのまま false を返す。
    public bool SubsidyOn { get => SubsidyRate >= 0.5; set { double r = value ? 1.0 : 0.0; SubsidyRate = r; _onSetSubsidy?.Invoke(r); } }
    public bool MonopolyOn
    {
        get => _w is not null && GoodId >= 0 && GoodId < _w.StateMonopolyMargins.Length && _w.StateMonopolyMargins[GoodId] > 1e-9;
        set { _w?.SetStateMonopolyOn(GoodId, value); OnPropertyChanged(); _onChanged?.Invoke(); }
    }
    // ★スイッチ式ボタン（作者仕様2026-08-09 項目2）：4つのレバーを「1つのボタンのON/OFF」に統一（旧＝ToggleSwitch＋別置きのラベル）。
    //   押すたびに反転し、ONは Button.mini.active の濃い塗りで示す＝法律タブ・出来事の絞り込みと同じ操作感。
    [RelayCommand] private void ToggleSubsidy() => SubsidyOn = !SubsidyOn;
    [RelayCommand] private void ToggleMonopoly() => MonopolyOn = !MonopolyOn;
    [RelayCommand] private void ToggleBuyDear() => BuyDearOn = !BuyDearOn;
    [RelayCommand] private void ToggleSellCheap() => SellCheapOn = !SellCheapOn;
    partial void OnSubsidyRateChanged(double value) => OnPropertyChanged(nameof(SubsidyOn));
    // ★財ごとの売買方針（2026-08-04・作者仕様(3)）。補助金/専売と同じトグル形式＝並べて置ける。
    //   相反するので排他（高価でも輸入⇔安価でも輸出）＝一方をONにすると他方はOFF。
    [ObservableProperty] private int _tradeBias;
    public bool BuyDearOn { get => TradeBias > 0; set { TradeBias = value ? 1 : 0; _onSetBias?.Invoke(TradeBias); } }
    public bool SellCheapOn { get => TradeBias < 0; set { TradeBias = value ? -1 : 0; _onSetBias?.Invoke(TradeBias); } }
    partial void OnTradeBiasChanged(int value) { OnPropertyChanged(nameof(BuyDearOn)); OnPropertyChanged(nameof(SellCheapOn)); }
    private readonly Action<int>? _onSetBias;

    private readonly Action? _onChanged;   // ★再描画の合図だけ（専売の真値はエンジンが持つ＝これが no-op でも物理は正しい）
    private readonly Action<double>? _onSetSubsidy;
    public ProdDetail() { }
    public ProdDetail(World w, int goodId, Action onChanged, double subsidyRate, Action<double> onSetSubsidy, int tradeBias, Action<int> onSetBias)
    { _w = w; GoodId = goodId; _onChanged = onChanged; _subsidyRate = subsidyRate; _onSetSubsidy = onSetSubsidy; _tradeBias = tradeBias; _onSetBias = onSetBias; }
    public void CopyFrom(ProdDetail s)
    {
        Title = s.Title; Tier = s.Tier; PriceDiff = s.PriceDiff; PriceDiffBrush = s.PriceDiffBrush;
        NatLine = s.NatLine; NatLine2 = s.NatLine2; WorldPriceLine = s.WorldPriceLine;
        LegalMonopoly = s.LegalMonopoly;
        ProdShareLine = s.ProdShareLine; ExportShareLine = s.ExportShareLine; TopProducerLine = s.TopProducerLine; TopExporterLine = s.TopExporterLine;
        WorldSparkPoints = s.WorldSparkPoints; SparkGrid = s.SparkGrid; SparkScaleTip = s.SparkScaleTip;   // 財パネル拡張（2026-07-24）＋目盛り（2026-08-10 項目2）
        PriceTotalText = s.PriceTotalText; PriceTotalTip = s.PriceTotalTip;   // 右段の合計（左段の合計はレシピごとの行＝CostRows の中）
        SubsidyRate = s.SubsidyRate; TradeBias = s.TradeBias;
        OnPropertyChanged(nameof(MonopolyOn));   // ★専売は鏡を持たない＝値は流さず「読み直せ」とだけ言う（譲歩・強制施行でエンジン側が動いた時も表示が追う）
        CanSubsidize = s.CanSubsidize; SubsidyTip = s.SubsidyTip;   // 国営100%で無効＋理由（作者裁定2026-08-10）
        // ★並べ替えない同期（2026-08-09・着工クリック不発の原因B）：州別行は生産量の降順だが、生産量は月次決算で入れ替わる。
        //   Move で行が動くと、押下と離上の間にボタンが別の位置へ移り Click が捨てられる（Avalonia は離上時にも当たり判定を取る）。
        //   並び順は財を選び直した時＝ProdDetail を作り直す時に確定する（生産量順の意図はそこで保たれる）。用途行も同じ理由で固定。
        // ★価格の分解と設備の断片も器を据え置く（2026-08-10）＝ツールチップを持つ行（需要と供給・稼働率）が
        //   日次の更新でホバー中に破棄されない。キーは Label／Key＝値が動いても不変。
        GameViewModel.SyncRows(PriceStack, s.PriceStack, r => r.Label, (t, o) => t.CopyFrom(o), reorder: false);
        GameViewModel.SyncRows(CostRows, s.CostRows, r => r.Name, (t, o) => t.CopyFrom(o), reorder: false);
        GameViewModel.SyncRows(Regions, s.Regions, r => r.Name, (t, o) => t.CopyFrom(o), reorder: false);
        GameViewModel.SyncRows(Uses, s.Uses, r => r.Name, (t, o) => t.CopyFrom(o), reorder: false);
        HasUses = s.HasUses;
    }
}

/// <summary>価格の分解の1行（項目8・2026-08-10）。Label＝据え置き同期のキー（不変）。Tip＝データのツールチップ（無い行は null）。</summary>
public sealed partial class ProdPriceRow : ObservableObject
{
    public string Label { get; init; } = "";
    [ObservableProperty] private string _value = "";
    [ObservableProperty] private string? _tip;
    public ProdPriceRow() { }
    public ProdPriceRow(string label) => Label = label;
    public void CopyFrom(ProdPriceRow s) { Value = s.Value; Tip = s.Tip; }
}

/// <summary>生産コストの1行（作者フィードバック2026-08-10 項目1の書式）。Name＝据え置き同期のキー（材名／人件費／管理・資本費＝不変）。
/// Label＝左寄せの表示「果実(×0.5)」＝材名と産出1単位あたりの必要量（電化・機械化で数量が動くので**キーには使えない**）。
/// Ratio＝右寄せの「基本価格×94%」＝その材のこの州の価格 ÷ 基本価格（安い青・高い赤／100%が境）。
/// Amount＝「= £0.20」＝数量×その州の価格。人件費・管理・資本費は比を持たない（Ratio が空＝金額だけが右に出る）。</summary>
/// Fill＝「供給:94%」＝**その材の供給率**（作者指示2026-08-10 F）。出所は <see cref="ProductionDiag.CostItem.Fill"/>
/// ＝<c>Market.LastRation</c>（EconomyStep のクリアリングが決めた配給率そのもの）。
/// 旧「材料供給」（素材の最小1本の集計値）を廃止してこちらへ移した＝**どの材が足りないか**が1行で読める。
public sealed partial class ProdCostRow : ObservableObject
{
    public string Name { get; init; } = "";
    /// <summary>そのレシピの合計行か（作者要望2026-09-07 (3)）＝左にレシピ式・右に「生産コスト £n」を右寄せで出す行。
    /// 材料行と器を分けない（同じ ItemsControl の中で並ぶ＝レシピが2本以上でも材料と合計の対応が崩れない）。</summary>
    public bool IsTotal { get; init; }
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private string _fill = "";
    [ObservableProperty] private IBrush? _fillBrush;
    [ObservableProperty] private string _ratio = "";
    [ObservableProperty] private IBrush? _ratioBrush;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string? _tip;
    public ProdCostRow() { }
    public ProdCostRow(string name) => Name = name;
    public void CopyFrom(ProdCostRow s) { Label = s.Label; Fill = s.Fill; FillBrush = s.FillBrush; Ratio = s.Ratio; RatioBrush = s.RatioBrush; Amount = s.Amount; Tip = s.Tip; }
}

// ProdSeg（折返し可能な断片＋ホバー）は削除（2026-08-10）＝設備の節の全廃で参照が0件になった。
// 文字だけの断片（NatSegs）は string 配列のままで足りる。

// 州別の1行：州名・民営工場・国営工場（− 現在/目標 ＋）・従業員・失業者・生産・過不足・材料価格。キー＝州名（据え置き同期）。
// ★着工/売却ボタンは廃止（作者裁定2026-08-10）＝操作はカウンター1本（目標を上げる＝建てさせる／下げる＝1棟ずつ払い下げる）。
// ★カウンター列は廃止（作者フィードバック2026-08-10 項目5）＝国営工場の列に畳んだ（− 現在/目標 ＋）＝「今いくつ建っていて、いくつを目指しているか」が1つの場所で読める。
public sealed partial class ProdRegionRow : ObservableObject
{
    public string Name { get; init; } = "";
    [ObservableProperty] private string? _capitalTip;    // 接17（2026-07-25）：州名ホバー＝工業化資本の新旧内訳（新鋭/旧世代/座礁・旧世代の据付技術）。ヴィンテージ層が休眠なら null＝ツールチップ無し
    [ObservableProperty] private bool _hasCounter;       // 産地のある州だけ ＋/− を出す（★出す/出さないは州で決まり、月ごとに入れ替わらない＝行の高さは動かない）
    [ObservableProperty] private bool _canUp;
    [ObservableProperty] private bool _canDown;
    [ObservableProperty] private string _counterNote = "";   // 目標と現在が食い違う月の「なぜ進まないか」（建材の採算／国庫不足／技術不足／赤字で買い手がつかない／資本家の資金不足／国有化法の対象／資本家が政府に不満・払い下げの理由）。行の下に常に見せる
    [ObservableProperty] private string _privFac = "";   // 民営工場（棟数＝(Scale−BuiltScale)/FactoryBuildScale）
    // 国営工場＝「現在/目標」（棟数。現在＝World.StateUnits・目標＝Producer.StateTarget＝−/＋で動かす当の数）。産地の無い州は「—」
    [ObservableProperty] private string _stateFac = "";
    // 従業員・失業者（項目6・2026-08-10）＝旧・設備欄の全国「従業員」と、旧・着工ボタン脇の「失業者 nK」を列へ昇格。産地なしは —
    [ObservableProperty] private string _emp = "";
    [ObservableProperty] private string _idle = "";
    [ObservableProperty] private string _prod = "";
    [ObservableProperty] private string? _prodTip;         // 稼働率とその内訳（採算／投入充足／在庫調整／慣性）＝設備欄から移設した全国値（項目5）
    [ObservableProperty] private string _surplus = "";     // 過不足＝(生産−消費)÷生産（項目3・2026-08-10）。輸入は分子にも分母にも入らない
    [ObservableProperty] private IBrush? _surplusBrush;
    [ObservableProperty] private string? _surplusTip;      // 絶対量（生産・消費・差）はここで読む＝表は率だけに保つ
    // 材料価格（2026-08-09 項目11／旧「世界価格差」を置換）＝この州の仕入れ束（レシピの素材×その州の価格）の、同じ束の世界価格に対する比率−1。
    // 安い（マイナス）＝青・高い（プラス）＝赤。素材投入を持たないRGOは「—」。
    [ObservableProperty] private string _matCost = "";
    [ObservableProperty] private IBrush? _matCostBrush;
    private readonly Action? _onUp, _onDown;
    public ProdRegionRow() { }
    public ProdRegionRow(Action onUp, Action onDown) { _onUp = onUp; _onDown = onDown; }
    // ClickMode=Press と同じ理由でコマンドは軽い（押下で確定＝行が伸縮してもクリックが捨てられない）
    [RelayCommand] private void Up() => _onUp?.Invoke();
    [RelayCommand] private void Down() => _onDown?.Invoke();
    public void CopyFrom(ProdRegionRow s)
    {
        CapitalTip = s.CapitalTip;
        HasCounter = s.HasCounter; CanUp = s.CanUp; CanDown = s.CanDown; CounterNote = s.CounterNote;
        PrivFac = s.PrivFac; StateFac = s.StateFac;
        Emp = s.Emp; Idle = s.Idle; Prod = s.Prod; ProdTip = s.ProdTip;
        Surplus = s.Surplus; SurplusBrush = s.SurplusBrush; SurplusTip = s.SurplusTip; MatCost = s.MatCost; MatCostBrush = s.MatCostBrush;
    }
}

// 用途（使い道）の1行：消費先の財と今月の実消費量。クリックでその財を選択（グラフ・詳細パネルが切替）。
// 役務の消費先（教育・行政等）はグラフに無い＝CanSelect=false（クリック不可・レビュー2026-07-19）。
public sealed partial class ProdUseRow : ObservableObject
{
    public int GoodId { get; init; }
    public string Name { get; init; } = "";   // キー（財名＝一意。popの直接消費の行はカテゴリ名＝食料/日用品/奢侈品…）
    [ObservableProperty] private string _amount = "";
    // 誰が買ったか（項目6・2026-08-10）＝階級別シェアのツールチップ。下流の財の行は null（データが無い行に枠を作らない）。
    // ⚠クリックできない行（popの直接消費）は Button が無効＝ツールチップを出せないので、XAML では**透明Borderで包む**（画面の作法）。
    [ObservableProperty] private string? _tip;
    // ★2026-08-02（作者指摘「用途の灰色が使っているのか使っていないのかよくわからない」）：
    //   旧実装の灰色は IsEnabled=false（＝役務でグラフに無いからクリックできない）に対する Fluent の既定表示で、
    //   使用/未使用とは無関係だった。灰色は直感どおり「今月の消費が無い」を意味させ、クリック可否は色で語らせない。
    [ObservableProperty] private bool _used;
    public bool CanSelect { get; init; } = true;
    public IRelayCommand? SelectCmd { get; init; }
    public void CopyFrom(ProdUseRow s) { Amount = s.Amount; Used = s.Used; Tip = s.Tip; }
}

public sealed class MegaRow
{
    public string Name { get; init; } = "";
    public string Host { get; init; } = "";
    public string State { get; init; } = "";      // 完成／建設中 n%／未着工
    public string Effect { get; init; } = "";
    public IBrush Brush { get; init; } = Brushes.Gray;
    public bool CanStart { get; init; }              // 着工できるか（2026-08-02）＝Megaprojects.CanStart と同一判定＝ボタンの活性
    public bool ShowStart { get; init; }             // ボタンを出すか（完成後だけ消す）
    public string StartLabel { get; init; } = "";    // 「着工 £1.20M」＝工場と同じ「コストを出してクリック」
    public string StartReason { get; init; } = "";   // 押せない理由／押せる時は残量と見積（データのツールチップ）
    // SubLine（状態＋効果を1行に畳んだ文字列）は削除（2026-08-10のカード化）＝カードは State と Effect を別の行で出す＝読み手0件
    public Megaproject? Project { get; init; }
}
