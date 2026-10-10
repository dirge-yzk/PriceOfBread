using System;
using System.Collections.Generic;

namespace PriceOfBread.SimCore;

/// <summary>国家選択画面が読む「開始条件の要約」＝おすすめ国と難易度（docs/202608/おすすめ国家と難易度_2026-08-09_完了.md）。
///
/// ★難易度を world.json に固定値で書かない（同doc §3-3）。理由＝序列を決めている2係数
/// （行政の必要量 <see cref="Coefficients.AdminNeedPerPop"/>〔2026-09-30 (135) まで AdminCostPerPop〕・<see cref="Coefficients.SecessionHoldPerPop"/>）は
/// 「人口だけの式」としてA級に分類され、いずれ機構へ置き換える予定＝固定値で書くとラベルだけが取り残される。
/// ここで**実行時に導出**しておけば、係数を直した日にラベルが自動で追随する。
///
/// ★UIロジックでなくSimCore側に置く理由＝係数と world.json を読む導出だから（Appから係数の意味を再解釈させない）。</summary>
public static class NationBrief
{
    /// <summary>おすすめ国＝「有名だから」でなく**このエンジンが何を回すか**で選んだ7カ国（同doc §1・§6。作者指示2026-08-11で6→7＝簡単2／普通4／挑戦的1）。
    /// 各国がそれぞれ違う系統を教える（英＝議会／米＝ゴールドラッシュと識字／普＝2段の変体／
    /// 日＝幕府という固有政体／露＝勅令の効く専制／清＝人口が罰になる構造／蘭＝小国・金融・海運＝人口比の富が断トツ）。
    /// ★おすすめは「入口」であって制限ではない＝シム側は全国プレイ可能（NationSelectViewModel）。</summary>
    public static readonly string[] Recommended =
        ["イギリス", "アメリカ", "プロイセン", "日本", "ロシア", "清", "ネーデルラント"];

    /// <summary>カードに出す説明＝**1783年までの歴史的背景＋最後の1文でプレイフィール**（作者裁定2026-08-11）。
    /// ★裁定②＝開始年までを書き、**その後に起きたことは書かない**（思想①台本禁止と噛み合う＝筋書きを与えない）。
    /// 裁定③＝歴史に詳しくない人が読んで分かる短さ。**この画面はUI規律⑦（説明文を書かない）の例外**
    /// ＝ゲーム開始前の画面であり、選択のための情報提供が目的（作者裁定2026-08-09 ①）。
    /// ⚠アメリカの文章に「自由な国」と書いてはならない（奴隷制が未実装＝同doc §5-1）。
    /// ★ホバーは置かない（作者指示2026-08-11）＝画面に出ている文章が全部。読むために操作を要求しない。</summary>
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

    /// <summary>開始時の総人口（config の階級の和）。行政費と分離耐性の分母＝難易度の主因。</summary>
    public static double Population(NationConfig c)
        => c.Nobility + c.Peasant + c.Worker + c.Bourgeois + c.Professional + c.Merchant + c.Artisan;

    /// <summary>国庫が行政の固定費だけで何ヶ月持つか（同doc §3 ①）。分母＝全ての頭数の行政の必要量を参照価格で買う月の額（官僚の給与×人数＝EconomyStep の行政の購買の充足1）。
    /// 2026-09-30 (135)：旧の分母＝行政費の人頭割り AdminCostPerPop×人口（削除＝行政の購買と同じ役務の二重の支払い）。</summary>
    public static double TreasuryMonths(NationConfig c, Coefficients coef)
    {
        double cost = coef.AdminNeedPerPop * Catalog.RefPrices[Catalog.AdminId] * Population(c);
        return cost > 1e-9 ? c.Treasury / cost : double.PositiveInfinity;
    }

    /// <summary>分離独立に抗う力（同doc §3 ②）。Politics の保持力の分母と同式＝人口が大きいほど下がる。</summary>
    public static double SecessionHold(NationConfig c, Coefficients coef)
        => 1.0 / (1.0 + Population(c) * coef.SecessionHoldPerPop);

    // 【削除2026-09-26（棚卸し x-04）】IsIsolated（本国州が他国と陸続きでないか）＝呼び手がテストだけだった。
    //   陸続きの判定の実体は下の NeighborMilitary（同じ陸塊の他国の軍＝島国と大洋の向こうは0）が難易度に使っている。

    /// <summary>★(235) 開始時の兵の数（千人）＝**構築した世界**の兵士POPの和（World.SoldierSize）。(231) で military 欄を外したあと、安全の項は人口（農民＋都市労働者）に比例する写しを読んでいた
    /// ＝構築の播種の式と第二の式を抱えていた。世界を1つ組んで実数を読む（ランクと国力と同じ1本の構築＝<see cref="Opening"/>）。呼び手が同じ世界を何度も組まないよう、引数で受け取れる。</summary>
    public static Dictionary<string, double> OpeningSoldiers(NationConfig[] all, Coefficients coef) => Opening(all, coef).Soldiers;

    /// <summary>同じ陸塊に本国州を持つ他国の軍のうち、上位3つの和（＝取り囲まれの度合い）。
    /// 最大1つでは足りない＝プロイセンは墺2.6・露2.0・仏1.8に**同時に**面しており、最大値だけ見ると
    /// 「墺だけの隣国」に見えて実態より安全に出る。海を隔てた国は数えない（英・米・日はここで0になる）。</summary>
    private static double NeighborMilitary(NationConfig c, NationConfig[] all, IReadOnlyDictionary<string, double> soldiers)
    {
        var mine = new HashSet<string>();
        foreach (var p in c.Provinces) if (Landmass.IsRegistered(p.City)) mine.Add(Landmass.LandmassOf(p.City));
        double a = 0, b = 0, d = 0;   // 上位3つ（並べ替えを作らずに3枠へ落とす＝決定論・割り当てゼロ）
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

    /// <summary>難易度の素点の内訳（0..1／素点は加重和）。UIと診断（selectprobe）が同じ1本を読む。</summary>
    public static (double Ease, double Resource, double Hold, double Safety, double Load, double Decay)
        Terms(NationConfig c, NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null)
    {
        soldiers ??= OpeningSoldiers(all, coef);   // 呼び手が渡さなければ世界を1つ組んで読む（国ごとに繰り返し呼ぶ側は渡すこと）
        // ①資源の余裕＝国庫が行政費だけで持つ月数。桁で効く（仏4・米5 と 普274＝2026-09-29 に開幕の国庫を史実の桁へ直した後・旧＝清4 と 蘭6667）ので対数で圧縮（1ヶ月=0／10000ヶ月=1）。
        double resource = PoliticalQuantities.Clamp01(Math.Log10(Math.Max(1.0, TreasuryMonths(c, coef))) / 4.0);
        // ②人口による分離圧に抗う力（Politics の保持力と同式）。
        double hold = SecessionHold(c, coef);
        // ③安全＝陸続きの隣国の軍（上位3和）に対する自国の軍。海に隔てられていれば隣国0＝1.0（英・米・日）。
        double safety = 1.0 / (1.0 + NeighborMilitary(c, all, soldiers) / Math.Max(1e-9, soldiers.GetValueOrDefault(c.Name)));
        // ④操作量＝**プレイヤーが毎期見る対象の数**（本国州＋植民地＋工業の厚み）。
        //   ★作者指摘2026-08-11「イギリスは操作量が多く簡単でない」＝資源が豊かでも、州5＋植民地11＋世界一の工業は
        //   最も忙しい盤面になる。難易度は「生き残れるか」だけでなく「回せるか」でもある。
        //   工業を3倍で数えるのは、1点の工業化度が概ね数州ぶんの生産・補助金・建設の判断を増やすため。
        double load = PoliticalQuantities.Clamp01((c.Provinces.Length + c.InitialColonies.Length + 3.0 * c.Industrialization) / 25.0);
        double ease = 0.35 * resource + 0.25 * hold + 0.15 * safety - 0.25 * load - 0.30 * c.InstitutionalDecay;
        return (ease, resource, hold, safety, load, c.InstitutionalDecay);
    }

    /// <summary>難易度の素点（高いほど易しい）。★エンジンが実際に課す量だけで作る（docs §3 ＋作者指摘2026-08-11の操作量）。
    /// ⚠識字は入れていない＝序列が識字を入れても変わらないため（入れると Simulation の13階級の初期識字を
    /// 組み直す第二の式を抱えることになる。序列が変わる日が来たら、世界を1つ組んで実測値を読む形で足すこと）。</summary>
    public static double Ease(NationConfig c, NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null) => Terms(c, all, coef, soldiers).Ease;

    /// <summary>おすすめ国の難易度ラベルを**素点の順位**で割り当てる（簡単2／普通4／挑戦的1・作者指示2026-08-11）。
    /// ★絶対値の閾値でなく順位にする理由＝閾値は係数が動くと全員が同じ側へ寄って空の列ができる。</summary>
    public static Dictionary<string, string> Difficulties(NationConfig[] all, Coefficients coef, IReadOnlyDictionary<string, double>? soldiers = null)
    {
        soldiers ??= OpeningSoldiers(all, coef);
        var scored = new List<(string Name, double Ease)>();
        foreach (var name in Recommended)
        {
            var c = Array.Find(all, n => n.Name == name);
            if (c is not null) scored.Add((name, Ease(c, all, coef, soldiers)));
        }
        scored.Sort((a, b) => b.Ease != a.Ease ? b.Ease.CompareTo(a.Ease) : string.CompareOrdinal(a.Name, b.Name));   // 同点は名前順＝決定論
        var map = new Dictionary<string, string>();
        for (int i = 0; i < scored.Count; i++)
            map[scored[i].Name] = i < EasyCount ? "簡単" : i < EasyCount + NormalCount ? "普通" : "挑戦的";
        return map;
    }
    private const int EasyCount = 2, NormalCount = 4;   // 挑戦的は残り（＝おすすめ7カ国なら1）

    /// <summary>開始時（1783）の国力ランク＝**エンジンの分類そのもの**（列強／準列強／中堅／弱小＝Diplomacy.RankOf）。
    /// ★選択画面のために第二の強さ指標を作らない＝世界を1つ組んで <see cref="Diplomacy"/> に数えさせる。
    /// ⚠ここは世界の構築を1回走らせる（selectprobe で実測＝約1秒）。開始時のランクは不変なので、
    /// 呼び手は結果を保持して使い回すこと。</summary>
    public static Dictionary<string, string> Ranks(NationConfig[] all, Coefficients coef)
        => RanksAndScores(all, coef).Ranks;

    /// <summary>開始時のランクと**国力そのもの**（NationalScore）。選択画面の並べ替えが読む
    /// ＝「その他の国」を国力順に並べるのに、ラベル（列強/準列強…）だけでは同ランク内の順が決まらない。
    /// ★世界の構築は1回だけ＝ランクと国力を同じ1本の走行から取る（2度組むと遅いうえ食い違いうる）。</summary>
    public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores)
        RanksAndScores(NationConfig[] all, Coefficients coef)
    {
        var o = Opening(all, coef);
        return (o.Ranks, o.Scores);
    }

    /// <summary>★(235) 構築した世界の読み＝開始時のランク・国力・兵の数（千人）。世界の構築は1回だけ（3つを別々に組むと遅いうえ食い違いうる）。選択画面は1度これを呼んで、
    /// 難易度（<see cref="Difficulties"/>）と素点（<see cref="Ease"/>）へ兵の数を渡す。</summary>
    public static (Dictionary<string, string> Ranks, Dictionary<string, double> Scores, Dictionary<string, double> Soldiers)
        Opening(NationConfig[] all, Coefficients coef)
    {
        var sim = new MultiNationSim(all, 1789UL, _ => new NullAgent());
        var map = new Dictionary<string, string>();
        var score = new Dictionary<string, double>();
        var soldiers = new Dictionary<string, double>();
        foreach (var p in sim.Diplomacy.Powers)
        {
            if (p.Nation is null) continue;
            score[p.Name] = NationalScore.Of(p.Nation);
            soldiers[p.Name] = p.Nation.SoldierSize();
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
