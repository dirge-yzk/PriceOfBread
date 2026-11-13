using System;
using System.Collections.Generic;

namespace PriceOfBread.SimCore;

public static class NationBrief
{
    public static readonly string[] Recommended =
        ["イギリス", "アメリカ", "プロイセン", "日本", "ロシア", "清", "ネーデルラント"];

    public static string Description(string name) => name switch
    {
        "アメリカ" =>
            "独立が認められたこの年、13の州が勝手に関税をかけ、軍も財源も持たない連合会議が名ばかりの中央政府を務めている。" +
            "戦時債務は返せず、6月には給与の未払いに怒った兵士が議場を包囲し、会議はフィラデルフィアから逃げ出した。" +
            "王も世襲の貴族も戴かない体制は当時として例外的で、大西洋が巨大な城壁となって外敵を遠ざける。" +
            "盤面は3州と小さく、脅かす者もいない。この緩い寄せ集めを一つの国家に鍛え直す時間だけは、たっぷりある。",
        "ネーデルラント" =>
            "王を戴かない、商人たちの共和国。アムステルダムの銀行はヨーロッパ中の王侯に金を貸し、東インド会社の船はアジアの香辛料を運んできた——対英戦争までは。" +
            "4年の戦いで海の主導権を失い、会社は赤字を垂れ流し、街頭では総督派と愛国派が睨み合う。" +
            "7つの州は誰も他州に命令できず、決定はいつも遅れる。盤面は2州と小さく手が届くが、陸軍は薄い。金で時間を買えるかどうか。",
        "プロイセン" =>
            "71歳のフリードリヒ大王が、41年目の統治を続けている。人口も国土も列強で最小でありながら、それに全く見合わない精強な軍と、" +
            "士官として軍を支える地方貴族(ユンカー)の忠誠がある。領土はライン川沿いから東プロイセンまで飛び地だらけで、地続きの国土を持たない。" +
            "オーストリア・ロシア・フランスに三方を囲まれ、守るには広く、攻めるには小さい。散らばった点を、どう線で繋いでいくか。",
        "日本" =>
            "将軍が全国の大名を従える武家の政権が、180年続いてきた。外国船が入れるのは長崎の一港のみ、渡航も禁制。" +
            "この年の夏、浅間山が大噴火した。降り積もった灰と冷夏が、東北の飢饉に火を付けようとしている。" +
            "老中・田沼意次が実権を握り、商人の力を借りて金を回す政策を進めている。町では寺子屋が広がり、庶民の読み書きの力はアジアで最も高い。" +
            "海の守りの内側は静かだが、扉を開くか閉じるかを、いずれ選ぶことになる。",
        "イギリス" =>
            "アメリカ13植民地を失ったばかりの敗戦国。戦争の後始末をめぐって内閣が次々と倒れ、政局は定まらない。" +
            "だが本土では蒸気機関と紡績機が回りはじめ、世界初の工業国になろうとしている。" +
            "一方で議会に票を持つのは成人男性の数%。大地主が有権者数十人の「腐敗選挙区」から議員を送り込み、穀物法がパンの値段を吊り上げている。" +
            "議会・工場・11の植民地を同時に回す、最も忙しい盤面。",
        "ロシア" =>
            "女帝エカチェリーナ2世の治世21年目。この年クリミアを併合し、ついに黒海への出口を手に入れた。" +
            "国土は地球の陸地の1割を超えるが、人口の大半は土地に縛られた農奴で、領主同士の間で売り買いされる。" +
            "読み書きできる者はごく一部、産業も街道もない。広さと兵は最初から手にある。中身が空のまま走り出すことになる。",
        "清" =>
            "乾隆帝の治世48年目。世界の人口の4割がこの国に住む。茶と絹を求めて世界中の銀が流れ込むが、" +
            "ヨーロッパ商人が入れるのは広州の一港だけで、彼らの持ち込む品に用はない。" +
            "科挙を勝ち抜いた官僚が広大な国土を紙の上で統べる仕組みは、かつて世界の最先端だった。" +
            "いまは皇帝の寵臣・和珅が売官を取り仕切る。戸部の銀庫にはまだ盛世の蓄えが残るが、国力は最大。それを食い潰す速度も最大。",
        _ => "",
    };

    public static double Population(NationConfig c)
        => c.Nobility + c.Peasant + c.Worker + c.Bourgeois + c.Professional + c.Merchant + c.Artisan;

    public static double TreasuryMonths(NationConfig c, Coefficients coef)
    {
        if (c.TreasuryMonths > 0) return c.TreasuryMonths;
        double cost = coef.AdminNeedPerPop * Catalog.RefPrices[Catalog.AdminId] * Population(c);
        return cost > 1e-9 ? c.Treasury / cost : double.PositiveInfinity;
    }

    public static double SecessionHold(NationConfig c, Coefficients coef)
        => 1.0 / (1.0 + Population(c) * coef.SecessionHoldPerPop);


    public static Dictionary<string, double> OpeningSoldiers(NationConfig[] all, Coefficients coef) => OpeningCore(all, coef, warm: false).Soldiers;

    private static double NeighborMilitary(NationConfig c, NationConfig[] all, IReadOnlyDictionary<string, double> soldiers)
    {
        var mine = new HashSet<string>();
        foreach (var p in c.Provinces) if (Landmass.IsRegistered(p.City)) mine.Add(Landmass.LandmassOf(p.City));
        double a = 0, b = 0, d = 0;
        if (mine.Count > 0)
            foreach (var other in all)
            {
                if (ReferenceEquals(other, c)) continue;
                bool touches = false;
                foreach (var p in other.Provinces)
                    if (Landmass.IsRegistered(p.City) && mine.Contains(Landmass.LandmassOf(p.City))) { touches = true; break; }
                if (!touches) continue;
                double m = soldiers.GetValueOrDefault(other.Name);
                if (m > a) { d = b; b = a; a = m; } else if (m > b) { d = b; b = m; } else if (m > d) d = m;
            }
        return a + b + d;
    }

    public static (double Ease, double Resource, double Hold, double Safety, double Load)
        Terms(NationConfig c, NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null)
    {
        soldiers ??= OpeningSoldiers(all, coef);
        double resource = PoliticalQuantities.Clamp01(Math.Log10(Math.Max(1.0, TreasuryMonths(c, coef))) / 4.0);
        double hold = SecessionHold(c, coef);
        double safety = 1.0 / (1.0 + NeighborMilitary(c, all, soldiers) / Math.Max(1e-9, soldiers.GetValueOrDefault(c.Name)));
        double load = PoliticalQuantities.Clamp01((c.Provinces.Length + c.InitialColonies.Length + 3.0 * c.Industrialization) / 25.0);
        double ease = 0.35 * resource + 0.25 * hold + 0.15 * safety - 0.25 * load;
        return (ease, resource, hold, safety, load);
    }

    public static double Ease(NationConfig c, NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null) => Terms(c, all, coef, soldiers).Ease;

    public static Dictionary<string, string> Difficulties(NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null)
    {
        soldiers ??= OpeningSoldiers(all, coef);
        var scored = new List<(string Name, double Ease)>();
        foreach (var name in Recommended)
        {
            var c = Array.Find(all, n => n.Name == name);
            if (c is not null) scored.Add((name, Ease(c, all, coef, soldiers)));
        }
        scored.Sort((a, b) => b.Ease != a.Ease ? b.Ease.CompareTo(a.Ease) : string.CompareOrdinal(a.Name, b.Name));
        var map = new Dictionary<string, string>();
        for (int i = 0; i < scored.Count; i++)
            map[scored[i].Name] = i < EasyCount ? "簡単" : i < EasyCount + NormalCount ? "普通" : "挑戦的";
        return map;
    }
    private const int EasyCount = 2, NormalCount = 4;

    public static Dictionary<string, string> Ranks(NationConfig[] all, Coefficients coef)
        => RanksAndScores(all, coef).Ranks;

    public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores)
        RanksAndScores(NationConfig[] all, Coefficients coef)
    {
        var o = Opening(all, coef);
        return (o.Ranks, o.Scores);
    }

    public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores, Dictionary<string, double> Soldiers)
        Opening(NationConfig[] all, Coefficients coef) => OpeningCore(all, coef, warm: true);

    private static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores, Dictionary<string, double> Soldiers)
        OpeningCore(NationConfig[] all, Coefficients coef, bool warm)
    {
        var sim = new MultiNationSim(all, 1789UL, _ => new NullAgent());
        var map = new Dictionary<string, string>();
        var score = new Dictionary<string, double>();
        var soldiers = new Dictionary<string, double>();
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is not null) soldiers[p.Name] = p.Nation.SoldierSizeK();
        if (warm) sim.StartWarmup();
        var scoreByPower = NationalScore.ScoresByPower(sim.Diplomacy);
        foreach (var p in sim.Diplomacy.Powers)
        {
            if (p.Nation is null) continue;
            score[p.Name] = scoreByPower[p.Id];
            map[p.Name] = sim.Diplomacy.RankOf(p) switch
            {
                PowerRank.Great => "列強",
                PowerRank.Secondary => "準列強",
                PowerRank.Middle => "中堅",
                _ => "弱小",
            };
        }
        return (map, score, soldiers);
    }
}
