# (812) c13ib I5 の A1（NavalProcurementNeedsPort）・A2（CrossBorderMigrationByOpportunity）の焼き込み。
# バイトで読み書きする。旧の文字列がちょうど 1 回のときだけ置き換える。
import sys

ROOT = sys.argv[1]


def edit(rel, old, new):
    p = ROOT + '/' + rel
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    if c != 1:
        print('NG', rel, c, old[:70])
        sys.exit(1)
    open(p, 'wb').write(b.replace(o, n))
    print('ok', rel)


C = 'PriceOfBread.SimCore/Coefficients.cs'

# A1 --------------------------------------------------------------------------------------------
edit(C,
     '    // ★軍艦は港でしか造れない（2026-08-01）。0＝旧（内陸州も軍艦を買う）／1＝港の州が1つも無い国は艦を買わない（(189) 海軍の調達の必要額が0＝海軍の予算は装備の器の残りへ）。\n'
     '    // 実測の動機＝1914年の海軍力1位が**内陸の西安王国**、艦隊ストック2位が広州王国だった（docs/202608/99_実装ログ）。\n'
     '    public double NavalProcurementNeedsPort = 1.0;\n',
     '    // 【墓標2026-11-03 (812)・旗の焼き込み】NavalProcurementNeedsPort（軍艦は港でしか造れない・2026-08-01・既定 1）＝港の州が1つも無い国は艦を買わない（EconomyStep.FleetOrder が常にこの門を通る・(189) 海軍の調達の必要額が0＝海軍の予算は装備の器の残りへ）。\n'
     '    //   動機＝1914年の海軍力1位が**内陸の西安王国**、艦隊ストック2位が広州王国だった（docs/202608/99_実装ログ）。旧（内陸州も軍艦を買う）の枝は消した。\n')
edit(C,
     '（NavalProcurementNeedsPort）と同じ式を運動に当てる',
     '（EconomyStep.FleetOrder の港の門）と同じ式を運動に当てる')
edit('PriceOfBread.SimCore/EconomyStep.cs',
     '・港の州が1つも無い国は艦を買わない（<see cref="Coefficients.NavalProcurementNeedsPort"/>＝造船は港）。',
     '・港の州が1つも無い国は艦を買わない（造船は港）。')
edit('PriceOfBread.SimCore/EconomyStep.cs',
     '        if (coef.NavalProcurementNeedsPort > 0 && !region.NationHasPort) return null;\n',
     '        if (!region.NationHasPort) return null;\n')
edit('PriceOfBread.Tests/EquipDiscoveryTests.cs',
     '        Assert.True(c.NavalProcurementNeedsPort > 0, "前提が崩れた：港の無い国も軍艦を買う");\n',
     '')

# A2 --------------------------------------------------------------------------------------------
edit(C,
     '    public double CrossBorderMigrationByOpportunity = 1.0;   // 人3/外14：国家間移民を州の機会で判定する（★回1-2・2026-09-27＝全ての Active 州が送出州・★(141) 他の国の全ての Active 州が受入州・差＝受入州の機会−送出州の機会×持ち分×食料充足−閾値＝MultiNationSim.MigrateBetweenNations の5段）。0で越境移民なし（旧・満足度ベース経路は2026-07-28に係数ごと削除済み）\n',
     '    // 【墓標2026-11-03 (812)・旗の焼き込み】CrossBorderMigrationByOpportunity（人3/外14・既定 1）＝国家間移民を州の機会で常に判定する（★回1-2・2026-09-27＝全ての Active 州が送出州・★(141) 他の国の全ての Active 州が受入州・差＝受入州の機会−送出州の機会×持ち分×食料充足−閾値＝MultiNationSim.MigrateBetweenNations の5段）。0＝越境移民なしの枝は消した（旧・満足度ベース経路は2026-07-28に係数ごと削除済み）。\n')
edit('PriceOfBread.SimCore/MultiNationSim.cs',
     '        if (!(_coef.CrossBorderMigrationByOpportunity > 0)) return;   // 0＝越境移民なし（旧・満足度ベース経路は2026-07-28に係数ごと削除済み）\n',
     '')

# 墓標（RetiredFields）-------------------------------------------------------------------------------
edit('PriceOfBread.Tests/DormantIdentityTests.cs',
     '        "ColoniesAreRegions",\n',
     '        "ColoniesAreRegions",\n'
     '        // ★旗の焼き込み 2026-11-03 (812) c13ib（I5・港と移民の旗。既定 1 の経路だけ残した）\n'
     '        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",\n')
