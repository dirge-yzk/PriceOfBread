namespace PriceOfBread.SimCore.Display;

/// <summary>問題の一覧の種類（設計 §5-2）。宣言の順が画面の並び（種類ごとに大きさの順）。</summary>
public enum ProblemKind { GoodShortage, TransportShortage, Bankruptcy, RegionHunger, Unemployment, Discontent, Deficit, War, Faction, MovableLaw }

/// <summary>問題の一覧の 1 行。<paramref name="Key"/>＝種類と対象で一意（前の四半期との突き合わせの鍵）。<paramref name="Size"/>＝その種類の中での大きさ（単位は <paramref name="Unit"/>）。
/// 押すと <paramref name="Page"/> を開き、<paramref name="TableKey"/> の表の <paramref name="RowKey"/> の行へ飛ぶ（表を持たない対象は空）。<paramref name="Section"/>＝飛ぶ先の欄（表を持たない対象。空なら指定なし）。</summary>
public sealed record ProblemItem(ProblemKind Kind, string Key, string Target, double Size, Unit Unit, Page Page, string TableKey, string RowKey, string Section = "");

/// <summary>問題の一覧を作る（設計 §5・§6-1）。<b>読むだけ</b>：盤面に書かない・状態を持たない・計算の順序も係数も持たない（<see cref="PlayText"/> と同じ立場＝シムの軌道に触れない）。
/// ・閾値で選ばない：〈0 でない物〉を大きさの順に並べる。どこからが問題かは画面に出さない（閾値は画面に出さない規則）。実用ゼロの床（<see cref="Floor"/>）は丸めで 0 に見える物を落とすだけ。
/// ・前の四半期からの向きは状態（前の結果）が要る＝呼び手（App）が前の結果を持ち、<see cref="Key"/> で突き合わせる。本体に新しい状態を作らない。
/// ・検出の式は既存の目録を呼ぶ（財の不足は <see cref="MarketTables"/>・州の飢えと失業と闘争は <see cref="PopTables"/>・動かせる法は <see cref="Government.CanStep"/>）＝画面に第二の式を置かない。
/// Runner の play の報告も同じ関数を読める。</summary>
public static class ProblemScan
{
    /// <summary>種類の名前（画面の見出し）。漢字 1 字の名前・かっこの補足は置かない（番人が守る）。</summary>
    public static string KindName(ProblemKind k) => k switch
    {
        ProblemKind.GoodShortage => "財の不足",
        ProblemKind.TransportShortage => "輸送力不足",
        ProblemKind.Bankruptcy => "倒産と休止",
        ProblemKind.RegionHunger => "州の飢え",
        ProblemKind.Unemployment => "失業",
        ProblemKind.Discontent => "闘争に加わる人数",
        ProblemKind.Deficit => "赤字と支払い",
        ProblemKind.War => "戦況",
        ProblemKind.Faction => "反乱勢力",
        ProblemKind.MovableLaw => "動かせる法",
        _ => "",
    };

    /// <summary>実用ゼロの床。これ以下は表示の解像度で 0 に見える＝問題として並べない（問題と問題でないものの境ではない）。</summary>
    public static double Floor(Unit u) => u switch
    {
        Unit.People => 0.001,          // 千人単位で 1 人
        Unit.Count => 0.5,             // 棟（休止は割合で足すので半棟未満は落とす）
        Unit.Share => 0.0005,          // 0.05%
        Unit.MoneyPerMonth or Unit.MoneyStock => 1.0,
        _ => 1e-3,
    };

    /// <summary>法の改正の方向（動かせる法の点灯と同じ 9 本・親軍法は 2026-08-18 に撤去）。App の点灯もこの 1 本を読む。</summary>
    public static readonly IReadOnlyList<(PolicyAxis Axis, bool Increasing)> ReformSteps =
    [
        (PolicyAxis.Suffrage, true), (PolicyAxis.EducationLaw, true), (PolicyAxis.Censorship, true),
        (PolicyAxis.LaborUnion, true), (PolicyAxis.ShorterHours, false),
        (PolicyAxis.WomenRights, true), (PolicyAxis.CornLaws, false), (PolicyAxis.LandReform, true), (PolicyAxis.SerfEmancipation, true),
    ];

    /// <summary>いま動かせる改正の方向の印（<see cref="ReformSteps"/> の並びの bit）。前の四半期から増えたかで法のページを光らせる（App）。</summary>
    public static ulong MovableLawBits(World w)
    {
        ulong bits = 0;
        for (int i = 0; i < ReformSteps.Count; i++)
            if (Government.CanStep(w, ReformSteps[i].Axis, ReformSteps[i].Increasing)) bits |= 1UL << i;
        return bits;
    }

    /// <summary>問題の一覧。種類の宣言順・同じ種類の中は大きさの降順（同じ大きさは鍵の順＝決定的）。<paramref name="fiscal"/>＝収支を読むための月の差分（App が持つ。null なら収支の赤は出さない）。</summary>
    public static IReadOnlyList<ProblemItem> Scan(MultiNationSim sim, World w, FiscalFlows? fiscal = null)
    {
        var all = new List<ProblemItem>();
        Shortage(sim, w, all);
        Transport(w, all);
        Bankruptcy(w, all);
        Hunger(w, all);
        Classes(w, all);
        Deficit(w, fiscal, all);
        Wars(sim, w, all);
        Factions(w, all);
        MovableLaws(w, all);
        return all
            .Where(p => p.Size > Floor(p.Unit))
            .OrderBy(p => (int)p.Kind).ThenByDescending(p => p.Size).ThenBy(p => p.Key, StringComparer.Ordinal)
            .ToList();
    }

    // 財の不足＝必要量（前期の総需要）に対して満たせなかった割合（2026-11-11 作者指示：額では程度が分からない）。財の値段の表と同じ行・同じ値から作る。
    private static void Shortage(MultiNationSim sim, World w, List<ProblemItem> o)
    {
        var unavailable = ProductionDiag.UnavailableGoods(sim, w.Coef);   // まだ世界のどこにも作れない財（技術が無い）は不足に並べない
        foreach (var l in MarketTables.Lines(sim, w))
        {
            if (l.Good >= 0 && l.Good < unavailable.Length && unavailable[l.Good]) continue;
            if (!(l.Shortage > 0) || !(l.Demand > 0)) continue;   // 否定形＝NaN も落とす
            o.Add(new ProblemItem(ProblemKind.GoodShortage, "short|" + l.Good, l.Name, Math.Min(1.0, l.Shortage / l.Demand), Unit.Share, Page.Market, "market.prices", l.Good.ToString()));   // 財の値段の表の行（国内で作られない財は生産の財の表に行が無い＝不足の列を持つ値段の表へ飛ぶ）
        }
    }

    // 輸送力不足＝州ごとの、港（と船腹・鉄道）で運びきれなかった交易の割合（1 − 港の律速率）。律速率は BilateralMarket の港の段が毎清算に記帳する量
    // （上限の容量 ÷ 運びたかった潜在量・1 で頭打ち）＝ここで新しい式も状態も作らない。運びたかった潜在量が無い州（律速率は初期値の 1）は大きさ 0 で床に落ちる。
    // 貿易の頁へ結ぶ（欄は指定しない＝輸送の表は無い）。
    private static void Transport(World w, List<ProblemItem> o)
    {
        foreach (var r in w.Regions)
        {
            if (!(r.LastTradePotential > 0)) continue;   // 否定形＝NaN も落とす
            double cut = 1.0 - r.LastTradeThrottle;
            if (!(cut > 0)) continue;
            o.Add(new ProblemItem(ProblemKind.TransportShortage, "transport|" + r.Name, r.Name, Math.Min(1.0, cut), Unit.Share, Page.Trade, "", ""));
        }
    }

    // 倒産と休止＝止まった工場の棟数。財ごとに自国の全州を足す。数え方は財カードの工場カウンターと同じ 1 本（ProductionTables.StoppedUnits）。
    private static void Bankruptcy(World w, List<ProblemItem> o)
    {
        int n = Catalog.Goods.Length;
        for (int g = 0; g < n; g++)
        {
            if (Catalog.Goods[g].Tier == GoodTier.Service) continue;
            double stopped = ProductionTables.StoppedUnits(w, g);
            if (stopped > 0) o.Add(new ProblemItem(ProblemKind.Bankruptcy, "bankrupt|" + g, Catalog.Goods[g].Name, stopped, Unit.Count, Page.Production, "production.goods", g.ToString(), "regions"));
        }
    }

    // 州の飢え＝必要量に対する主食の不足の割合（1 − 主食の充足・2026-11-11 作者指示）。州の表と同じ行。
    private static void Hunger(World w, List<ProblemItem> o)
    {
        foreach (var l in PopTables.RegionLines(w))
        {
            double short_ = 1.0 - l.FoodFill;
            if (!(short_ > 0) || !(l.Population > 0)) continue;
            o.Add(new ProblemItem(ProblemKind.RegionHunger, "hunger|" + l.Name, l.Name, Math.Min(1.0, short_), Unit.Share, Page.Population, "pop.regions", l.Name));
        }
    }

    // 失業＝失業者の数（階級ごと）／闘争に加わる人数＝闘争度 × 人数（階級ごと）。階級の表と同じ行（兵士の〈失業〉は給与の未払い＝失業に数えない規則も表の側）。
    private static void Classes(World w, List<ProblemItem> o)
    {
        foreach (var l in PopTables.ClassLines(w))
        {
            string rowKey = "class." + l.ClassId;
            if (l.Unemployed > 0) o.Add(new ProblemItem(ProblemKind.Unemployment, "unemp|" + l.ClassId, l.Name, l.Unemployed, Unit.People, Page.Population, "pop.classes", rowKey));
            double mil = l.Militancy * l.Size;
            if (mil > 0) o.Add(new ProblemItem(ProblemKind.Discontent, "milit|" + l.ClassId, l.Name, mil, Unit.People, Page.Population, "pop.classes", rowKey));
        }
    }

    // 赤字と支払い＝収支の赤（月）・払えなかった割合（国家の支払い率が 1 を下回った分）・国庫の赤字（在庫が負）。
    private static void Deficit(World w, FiscalFlows? fiscal, List<ProblemItem> o)
    {
        if (fiscal is not null)
        {
            double bal = FiscalTables.LedgerBalance(fiscal);
            if (bal < 0) o.Add(new ProblemItem(ProblemKind.Deficit, "deficit|balance", "収支", -bal, Unit.MoneyPerMonth, Page.FiscalMoney, "", "", "spend"));
        }
        double unpaid = 1.0 - w.FiscalFundingRatio.Value;
        if (unpaid > 0) o.Add(new ProblemItem(ProblemKind.Deficit, "deficit|unpaid", "払えなかった割合", unpaid, Unit.Share, Page.FiscalMoney, "", "", "spend"));
        double tre = w.Treasury.Value;
        if (tre < 0) o.Add(new ProblemItem(ProblemKind.Deficit, "deficit|treasury", "国庫", -tre, Unit.MoneyStock, Page.FiscalMoney, "", "", "ledger"));
    }

    // 戦況＝戦勝点の負け幅（自国から見て負けている戦争だけが大きさを持つ）。
    private static void Wars(MultiNationSim sim, World w, List<ProblemItem> o)
    {
        if (w.SelfPower is not { } me) return;
        foreach (var war in sim.Diplomacy.Wars)
        {
            bool meIsA = war.A == me;
            if (!meIsA && war.B != me) continue;
            var enemy = meIsA ? war.B : war.A;
            double sc = meIsA ? war.Score : -war.Score;
            if (sc < 0) o.Add(new ProblemItem(ProblemKind.War, "war|" + enemy.Id, PowerLabel.Of(enemy, sim.Diplomacy.Wars), -sc, Unit.Share, Page.DiplomacyWar, "", "", "war"));
        }
    }

    /// <summary>いま自国の州に本拠のある反乱勢力（表示用の 1 本）。World.Factions は州を手放した後も勢力を持ち続ける（シミュの側に残る）ので、本拠の州が自国の州に無い勢力は出さない。</summary>
    public static List<FactionState> LiveFactions(World w)
    {
        var o = new List<FactionState>();
        foreach (var f in w.Factions) if (w.Regions.Contains(f.Region)) o.Add(f);
        return o;
    }

    // 反乱勢力＝勢力の強さ（エンジンの勢力の強さの式 1 本）。永続の勢力があるときだけ（無い間のスナップショットは強さを持たない）。
    private static void Factions(World w, List<ProblemItem> o)
    {
        foreach (var f in LiveFactions(w))
        {
            string name = PriceOfBread.SimCore.Factions.DisplayName(f);
            double strength = Politics.FactionStrength(w, w.Coef, f);
            o.Add(new ProblemItem(ProblemKind.Faction, "faction|" + name, name, strength, Unit.Quantity, Page.DiplomacyWar, "", "", "faction"));
        }
    }

    // 動かせる法＝いま改正の一歩が通る法（動かせる段の数＝1）。点灯と同じ 9 本の方向を、法の表の行へ結ぶ。
    private static void MovableLaws(World w, List<ProblemItem> o)
    {
        ulong bits = MovableLawBits(w);
        for (int i = 0; i < ReformSteps.Count; i++)
        {
            if ((bits & (1UL << i)) == 0) continue;
            var axis = ReformSteps[i].Axis;
            var def = LawCatalog.All.FirstOrDefault(d => d.Axis == axis && d.Slot == LawSlot.Law && d.IsVisible(w));
            if (def is null) continue;   // 画面の行を持たない法・出す条件が偽の法は並べない（押しても行が無い）
            o.Add(new ProblemItem(ProblemKind.MovableLaw, "law|" + def.Key, def.Name, 1, Unit.Count, Page.PoliticsLaw, "politics.laws", def.Key));
        }
    }
}
