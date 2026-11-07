# (952) c84kp 突然変異：使い捨ての木 D:/PoB_work/c84kp_work/mut に、点火の形（ign）と突然変異 M1〜M5 を当てる。
#   python mutate952.py <木> <形: ign|M1|M2|M3|M4|M5>   （ign は点火の形そのもの・M* は点火の形の上に当てる＝呼び手が先に ign を当てた木を git で戻してから当てる）
import sys
T, MODE = sys.argv[1], sys.argv[2]


def rep(rel, old, new):
    p = f'{T}/{rel}'
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    assert b.count(o) == 1, (rel, old)
    open(p, 'wb').write(b.replace(o, n))


def ign():
    rep('PriceOfBread.SimCore/Coefficients.cs', 'public double OpeningColonyDominanceFull = 0.0;', 'public double OpeningColonyDominanceFull = 1.0;')
    rep('PriceOfBread.Tests/DormantIdentityTests.cs', '        "OpeningColonyDominanceFull",   // (952)', '        // ★OpeningColonyDominanceFull（(952)')
    rep('PriceOfBread.Tests/OpeningColonyDominanceTests.cs', '        var sim = Build(1.0);\n        foreach (var name in ColonialNations)',
        '        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());   // 既定の係数＝旗の既定 1 も見る\n        foreach (var name in ColonialNations)')


MUT = {
    'M1': lambda: rep('PriceOfBread.SimCore/Simulation.cs', 'Tier = tier, Dominance = 0.0, Malarial', 'Tier = tier, Dominance = 1.0, Malarial'),
    'M2': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 0.5;'),
    'M3': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0 && sp.Name == "ネーデルラント") reg.Dominance = 1.0;'),
    'M4': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0) { reg.Dominance = 1.0; reg.Tier = RegionTier.Core; }'),
    'M5': lambda: rep('PriceOfBread.SimCore/Coefficients.cs', 'public double OpeningColonyDominanceFull = 1.0;', 'public double OpeningColonyDominanceFull = 0.0;'),
}

if MODE == 'ign':
    ign()
else:
    MUT[MODE]()
print('当てた', MODE)
