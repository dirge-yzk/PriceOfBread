# (1296) ㉜㊴ の試験の書き換え（バイトで読み書き）。使い方: python fix32.py <木>
import sys, os
T = sys.argv[1]
def rep(f, a, b):
    p = os.path.join(T, f); s = open(p, 'rb').read(); a = a.encode('utf-8'); b = b.encode('utf-8')
    assert s.count(a) == 1, (f, a[:60]); open(p, 'wb').write(s.replace(a, b))
rep('PriceOfBread.Tests/DormantIdentityTests.cs',
    '        "UrbanFlowReal",               // ★(706)',
    '        // ★UrbanFlowReal（(706) 線 urb3）は (1296) c134f1 が 2026-11-22 に 1 で**点火**＝この表から外した（旗 0 の経路の番人は UrbanFlowRealTests の旗0は新しい入力…・FamineGateReturnsRuralTests）。旧の注：★(706)')
rep('PriceOfBread.Tests/UrbanFlowRealTests.cs',
    '        Assert.Equal(0.0, ca.UrbanFlowReal); Assert.Equal(0.0, cb.UrbanFlowReal);',
    '        ca.UrbanFlowReal = 0; cb.UrbanFlowReal = 0;   // 旗 0 の経路の番人（(1296) で既定が 1 になった＝旗を明示で 0 に置く）')
rep('PriceOfBread.Tests/FamineGateReturnsRuralTests.cs',
    '        Assert.Equal(0.0, c.UrbanFlowReal);   // 前提：今の既定（シグモイドの旧の式）の上で門を見る',
    '        c.UrbanFlowReal = 0;   // 前提：シグモイドの旧の式の上で門を見る（(1296) で既定が 1 になった＝旗を明示で 0 に置く・旗 1 では門は流れ＞0＝g＞0 の月だけで fg の枝に届かない）')
# FamineTests：州の中の移動（Mobility）を盤面から外す＝飢えの死の機構を移動の規則（旧のシグモイドか値打ちの g か）に依らずに読む（㊴ 軌道に依る前提）
rep('PriceOfBread.Tests/FamineTests.cs',
    '''        var on = new Simulation(Famine(1), new NullAgent());
        double rich0''',
    '''        var son = Famine(1); son.Coef.Mobility = 0;   // (1296) 州の中の移動を外す（移動の規則に依らない盤面・㊴）
        Assert.Equal(0.0, son.Coef.Mobility);
        var on = new Simulation(son, new NullAgent());
        double rich0''')
rep('PriceOfBread.Tests/FamineTests.cs',
    '''        var off = new Simulation(Famine(1, mortality: 0), new NullAgent());''',
    '''        var soff = Famine(1, mortality: 0); soff.Coef.Mobility = 0;
        var off = new Simulation(soff, new NullAgent());''')
rep('PriceOfBread.Tests/FamineTests.cs',
    '''        var a = new Simulation(Famine(1), new NullAgent());
        var b = new Simulation(Famine(1), new NullAgent());''',
    '''        var sa = Famine(1); sa.Coef.Mobility = 0; var sb = Famine(1); sb.Coef.Mobility = 0;   // (1296) 州の中の移動を外す（移動の規則に依らない盤面・㊴）
        var a = new Simulation(sa, new NullAgent());
        var b = new Simulation(sb, new NullAgent());
        int checkedPops = 0;''')
rep('PriceOfBread.Tests/FamineTests.cs',
    '''                double fill = p.FoodMet / p.FoodNeed;
                Assert.True(fill < c.FamineDeathFillFloor''',
    '''                double fill = p.FoodMet / p.FoodNeed;
                checkedPops++;
                Assert.True(fill < c.FamineDeathFillFloor''')
rep('PriceOfBread.Tests/FamineTests.cs',
    '''        Assert.True(poor.SumPopulation() < poor0 * 0.8, $"慢性の不足で不足州が減らない''',
    '''        Assert.True(checkedPops >= 12, $"対照：調べた POP×四半期が {checkedPops}＝前提を何も検査していない");
        Assert.True(poor.SumPopulation() < poor0 * 0.8, $"慢性の不足で不足州が減らない''')
print('ok')
