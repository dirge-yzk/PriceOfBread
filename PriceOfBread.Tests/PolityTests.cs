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

    // (393) 軍政の支持への足し算 Politics.CoupSupport（と職業軍の度合い π・MilitaryCoupWeight・MilitaryCoupSupport）は削除した（作者 2026-10-16 のバフ・デバフ削除）。
    //   復活を禁じる番人は SoldierHoldSplitTests（兵力を極大にしても引力点が国民の集計だけで決まる）。
}
