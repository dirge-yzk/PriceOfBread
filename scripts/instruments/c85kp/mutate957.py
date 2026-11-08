# (957) c85kp 突然変異：使い捨ての木 D:/PoB_work/c85kp_work/mut（c85kp の T3′ のコミット＝点火の形の上）に M1〜M6 を当てる（c84kp/mutate952.py の型）。
#   python mutate957.py <木> <形: M1|M2|M3|M4|M5|M6>   （木は点火の形＝呼び手が git で戻してから当てる）
import sys
T, MODE = sys.argv[1], sys.argv[2]


def rep(rel, old, new):
    p = f'{T}/{rel}'
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    assert b.count(o) == 1, (rel, old)
    open(p, 'wb').write(b.replace(o, n))


MUT = {
    'M1': lambda: rep('PriceOfBread.SimCore/Simulation.cs', 'Tier = tier, Dominance = 0.0, Malarial', 'Tier = tier, Dominance = 1.0, Malarial'),
    'M2': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 0.5;'),
    'M3': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0 && sp.Name == "ネーデルラント") reg.Dominance = 1.0;'),
    'M4': lambda: rep('PriceOfBread.SimCore/MultiNationSim.cs', 'if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;', 'if (_coef.OpeningColonyDominanceFull > 0) { reg.Dominance = 1.0; reg.Tier = RegionTier.Core; }'),
    'M5': lambda: rep('PriceOfBread.SimCore/Coefficients.cs', 'public double OpeningColonyDominanceFull = 1.0;', 'public double OpeningColonyDominanceFull = 0.0;'),
    # M6 の Diplomacy.cs は CRLF（git ls-files --eol）
    'M6': lambda: rep('PriceOfBread.SimCore/Diplomacy.cs', '            Simulation.PromoteColonyToRegion(rw, c, col, RegionTier.Colonial);\r\n        }\r\n        else receiver.ColonyList.Add(col);',
                      '            { var reg6 = Simulation.PromoteColonyToRegion(rw, c, col, RegionTier.Colonial); reg6.Dominance = 1.0; }\r\n        }\r\n        else receiver.ColonyList.Add(col);'),
}

MUT[MODE]()
print('当てた', MODE)
