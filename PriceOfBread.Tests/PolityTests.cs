using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 政体（8種）と利益集団駆動の遷移（§9-E）。権威主義の君主政は意識が育つと倒れ、漸進改革は立憲君主制（明治型）、
/// 革命は主力ブロックで分岐（大衆＝プロレタリア独裁・ブルジョワ＝共和）。自由側のみ革新（世界の技術水準）可能。
/// </summary>
public class PolityTests
{
    [Fact]
    public void 幕府は教育改革で近代政体へ転換する()   // 人口正規化後：宗教教育の幕府は意識が閾値に届かず封建のまま（徳川の罠）。公教育（明治の改革）が意識を育て初めて権威主義が倒れる
    {
        // 宗教教育のままの幕府＝意識が育たず封建が持続（外圧だけでは倒れない＝徳川の罠）
        // ★計器の隔離（回B 2026-09-22）：この番人が測るのは**意識の経路**（教育→意識→保持力の閾値）だけ。飢餓の駆動は回Bで平常からの急な落ち込みを測るようになり、
        //   意識と独立の経路（モラル・エコノミー＝文盲の群衆でも急な落ち込みで立つ）として働く＝宗教教育の幕府でも、34年かけて充足が0.67→0.88へ上がった後の不作
        //   （135四半期目に0.77＝平常から0.11の落差＝飽和）で倒れる（Jカーブ）。それは「徳川の罠」（意識が閾値に届かない）の反証ではない＝両腕で外して意識の経路だけを比べる。
        var stagnant = new Simulation(NationConfig.Japan().ToScenario(1789), new NullAgent());
        stagnant.World.Coef.FamineDriveGain = 0;
        Assert.Equal(Polity.Shogunate, stagnant.World.Polity);
        stagnant.Run(300);
        Assert.Equal(Polity.Shogunate, stagnant.World.Polity);   // 宗教教育＝意識の上限が低く保持力の閾値に届かない＝権威主義が持続

        // 教育改革（公教育）＝意識が育ち、保持力の閾値を超えて権威主義が倒れる（明治型の近代化）
        var reformed = new Simulation(NationConfig.Japan().ToScenario(1789), new NullAgent());
        reformed.World.Coef.FamineDriveGain = 0;   // 同じ隔離（両腕で揃える＝差は教育法だけ）
        reformed.World.EducationLaw = EducationLaw.Public;
        reformed.World.Policy.EducationLaw = EducationLaw.Public;
        reformed.World.ExternalPressure = 0.8;
        reformed.Run(300);
        Assert.NotEqual(Polity.Shogunate, reformed.World.Polity);   // 教育で意識が育てば権威主義は倒れる（恒久的な罠を作らない）
        Assert.True(reformed.World.Polity is Polity.ConstitutionalMonarchy or Polity.Republic or Polity.ProletarianDictatorship
                    or Polity.BourgeoisDictatorship or Polity.MilitaryDictatorship,
            $"想定外の転換先（{reformed.World.Polity}）");
    }

    [Fact]
    public void 自由な政体だけが革新可能で権威主義は採用のみ()   // 倍率排除②：世界の技術水準は法律（参政権＋報道）から創発。代議制は報道を解放し革新・検閲する独裁は形式的参政権があっても停滞（閾値0.6）
    {
        double thr = new Coefficients().FrontierFreedomThreshold;
        // 各政体の建国制度（含意参政権＋解放される報道）での探究の自由
        double Freedom(Polity p) => Politics.FreedomFromLaws(Politics.PolitySuffrage(p), Politics.FoundingPress(p));
        Assert.True(Freedom(Polity.ConstitutionalMonarchy) >= thr);   // 立憲＝制限選挙＋自由化報道→革新可能
        Assert.True(Freedom(Polity.Republic) >= thr);                 // 共和＝普通選挙＋自由報道→革新可能
        Assert.True(Freedom(Polity.Despotic) < thr);                  // 専制＝無参政権＋検閲→採用のみ
        Assert.True(Freedom(Polity.BourgeoisDictatorship) < thr);     // ブルジョア独裁＝制限選挙でも検閲を保つ→先端で停滞（倍率排除②の核）
        Assert.True(Freedom(Polity.ProletarianDictatorship) < thr);   // プロレタリア独裁＝普通選挙でも検閲を保つ→先端で停滞
        Assert.True(Freedom(Polity.Absolutist) < thr);                // 絶対君主＝閉鎖
    }

    [Fact]
    public void 史実準拠の初期政体()   // 仏=絶対君主・日=幕府・英=立憲君主・米=共和
    {
        Assert.Equal(Polity.Absolutist, new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Shogunate, new Simulation(NationConfig.Japan().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.ConstitutionalMonarchy, new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World.Polity);
        Assert.Equal(Polity.Republic, new Simulation(NationConfig.America().ToScenario(1), new NullAgent()).World.Polity);
    }

    // ★作者裁定2026-09-19 R3/R4（クーデタ項・2026-09-20 に既定化＝腕 A4）：軍政の支持への加算 Politics.CoupSupport＝MCS×σ×π と職業軍の度合い π の機構・不変条件。
    //   CoupSupport は π を世界から自分で読む（2026-09-20）＝π の検査は兵の規模を指定した世界で行う（呼び出し側が π を落とす穴＝突然変異 M5 を塞いだ）。
    //   旧⑤「旗0のクーデタ項は旧式とビット一致する」は、旗と旧式の枝の削除（2026-09-20）とともに退役した（恒等を主張する相手の式がもう無い）。

    private static World 世界() => new Simulation(NationConfig.France().ToScenario(1789), new NullAgent()).World;

    /// <summary>兵の規模 x＝兵数÷(総人口×平時の上限の人口比) を指定した世界（兵士POPの Size を一律に拡縮。総人口は兵を含むので解いてから掛ける）。
    /// ★(145)：上限の人口比＝Military.PeaceCapShareOfPop（MaxSoldierFraction＝働き盛りの男の割合×標準の構成の働き盛りの男の割合）＝CoupSupport が読む物差しと同じ。</summary>
    private static World 兵の規模の世界(double x)
    {
        var w = 世界();
        double msf = Military.PeaceCapShareOfPop(w.Coef), soldiers = w.SoldierSizeK(), others = w.TotalPopulation - soldiers;
        Assert.True(soldiers > 0 && others > 0);
        double k = x * msf * others / (1 - x * msf) / soldiers;
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Size *= k;
        return w;
    }

    private static bool 同じビット(double a, double b) => System.BitConverter.DoubleToInt64Bits(a) == System.BitConverter.DoubleToInt64Bits(b);

    [Fact]
    public void クーデタ項の取り分は保持力が崩れても上限を超えない()   // ① R3＝発散する量（兵数に線形の兵力）への漸近ロック：加算 ≤ MCS
    {
        var w = 世界(); var c = w.Coef;
        foreach (double alleg in new[] { -1.0, -0.3, 0.0, 0.5, 1.0 })
        {
            w.ArmyAllegiance = alleg;
            foreach (double sf in new[] { 0.0, 1.0, 717.3, 1e6, 1e12 })
                foreach (double coerce in new[] { 0.0, 1e-300, 1e-9, 0.5, 1.0, 18332.1 })
                {
                    double t = Politics.CoupSupport(w, c, sf, coerce);
                    Assert.True(t >= 0 && t <= c.MilitaryCoupSupport, $"sf={sf} κ={coerce} 帰属={alleg}：加算 {t} が上限 MCS={c.MilitaryCoupSupport} の外");
                }
        }
        // 取り分の曲がり所＝門2と同じ物差し：兵力が保持力を上回る（sf>κ）ときだけ取り分が半分を超える（π=1 の平時の規模で見る）
        var p = 兵の規模の世界(0.68); var cp = p.Coef;
        Assert.Equal(1.0, Politics.ProfessionalArmyShare(p.SoldierSizeK(), p.TotalPopulation, Military.PeaceCapShareOfPop(cp)));
        Assert.True(Politics.CoupSupport(p, cp, 2.0, 1.0) > 0.5 * cp.MilitaryCoupSupport);
        Assert.True(Politics.CoupSupport(p, cp, 1.0, 2.0) < 0.5 * cp.MilitaryCoupSupport);
        // 保持力が0へ崩れても加算は上限 MCS に張り付くだけ（旧式の裸の比 sf/max(1,κ) は同じ入力で兵力そのものに比例した＝上限が無い＝分子と分母の非対称）
        Assert.Equal(cp.MilitaryCoupSupport, Politics.CoupSupport(p, cp, 1e6, 0.0));
    }

    [Fact]
    public void 平時の上限に収まる軍は割り引かない()   // ② R4：x＝兵数÷(総人口×平時の上限の人口比)≤1 で π=1、π=1 の割引は1ビットも動かさない
    {
        double msf0 = Military.PeaceCapShareOfPop(new Coefficients()), pop = 1e6;   // ★(145) 上限の人口比
        foreach (double x in new[] { 0.0, 0.25, 0.68, 0.999, 1.0 })
            Assert.Equal(1.0, Politics.ProfessionalArmyShare(x * pop * msf0, pop, msf0));
        // π=1（平時）の ×1.0 は厳密＝加算は割り引かない取り分 MCS×σ とビット一致。π は CoupSupport が世界の兵数と総人口から読む
        var rng = new System.Random(1789);
        foreach (double x in new[] { 0.25, 0.68, 0.999 })
        {
            var w = 兵の規模の世界(x); var c = w.Coef;
            Assert.Equal(1.0, Politics.ProfessionalArmyShare(w.SoldierSizeK(), w.TotalPopulation, Military.PeaceCapShareOfPop(c)));
            for (int i = 0; i < 700; i++)
            {
                double sf = rng.NextDouble() * 3000, coerce = rng.NextDouble() * 4000;
                w.ArmyAllegiance = rng.NextDouble() * 2 - 1;
                double undiscounted = c.MilitaryCoupSupport * (sf / (sf + coerce));
                Assert.True(同じビット(undiscounted, Politics.CoupSupport(w, c, sf, coerce)), $"x={x}：π=1 の割引で加算が動いた（sf={sf} κ={coerce}）");
            }
        }
    }

    [Fact]
    public void 上限を超えた戦時徴兵の分は軍政へ足さない()   // ③ R4：x=2.5（総動員の平均の規模）で π=0.4＝加算は職業軍の分だけ
    {
        double msf0 = Military.PeaceCapShareOfPop(new Coefficients()), pop = 1e6, capSoldiers = pop * msf0;   // ★(145) 上限の人口比
        Assert.Equal(0.4, Politics.ProfessionalArmyShare(2.5 * capSoldiers, pop, msf0), 12);
        Assert.Equal(0.5, Politics.ProfessionalArmyShare(2.0 * capSoldiers, pop, msf0), 12);
        Assert.True(Politics.ProfessionalArmyShare(10 * capSoldiers, pop, msf0) > 0);   // 割引は比例＝どれだけ膨らんでも職業軍の分は残る（0へ畳まない）
        // 割引は CoupSupport 自身が世界から π を読んで掛ける（呼び出し側は π を渡さない）：同じ兵力と保持力で、平時の規模（π=1）の加算の π 倍
        var peace = 兵の規模の世界(0.68);
        double full = Politics.CoupSupport(peace, peace.Coef, 500, 800);
        Assert.True(full > 0);
        var w = 兵の規模の世界(2.5); var c = w.Coef;
        w.ArmyAllegiance = 0.2;
        Assert.Equal(0.4, Politics.ProfessionalArmyShare(w.SoldierSizeK(), w.TotalPopulation, Military.PeaceCapShareOfPop(c)), 9);
        Assert.Equal(0.4 * full, Politics.CoupSupport(w, c, 500, 800), 9);
    }

    [Fact]
    public void 取り分の形はNaNを出さず兵も保持力も無ければ加算0()   // ④ 0/0 と NaN は否定形で0・π の NaN は1（割り引かない側）・軍帰属は読まない
    {
        var w = 世界(); var c = w.Coef;
        w.ArmyAllegiance = 0.0;
        Assert.Equal(0.0, Politics.CoupSupport(w, c, 0.0, double.NaN));   // κ=NaN・sf=0
        Assert.Equal(0.0, Politics.CoupSupport(w, c, 0.0, 0.0));          // 兵も保持力も無い＝0/0
        Assert.Equal(0.0, Politics.CoupSupport(w, c, 500.0, double.NaN)); // κ=NaN
        Assert.Equal(0.0, Politics.CoupSupport(w, c, double.NaN, 800.0)); // sf=NaN
        // 軍帰属は読まない（旧の変調 max(0,1−0.5×帰属) は兵士の思想の読み直し＝P9 で 2026-09-20 に削除）＝帰属が NaN でも加算は1ビットも動かない
        double r0 = Politics.CoupSupport(w, c, 500.0, 800.0);
        Assert.True(r0 > 0 && r0 < c.MilitaryCoupSupport);
        foreach (double alleg in new[] { double.NaN, -1.0, 1.0 })
        {
            w.ArmyAllegiance = alleg;
            Assert.True(同じビット(r0, Politics.CoupSupport(w, c, 500.0, 800.0)), $"帰属={alleg} で加算が動いた（軍帰属は読まない）");
        }
        double msf = Military.PeaceCapShareOfPop(c);
        Assert.Equal(1.0, Politics.ProfessionalArmyShare(double.NaN, 1e6, msf));
        Assert.Equal(1.0, Politics.ProfessionalArmyShare(100, double.NaN, msf));
        Assert.Equal(1.0, Politics.ProfessionalArmyShare(100, 1e6, double.NaN));
    }
}
