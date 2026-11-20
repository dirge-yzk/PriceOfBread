# (1269) c130k15 旗 JobSearchLiteracyOff の既定を 1 にする（バイトで読み書き・ちょうど 1 回の置き換え）。使い方: python flip1.py <木>
import sys
t = sys.argv[1]
R = [
    ('PriceOfBread.SimCore/Coefficients.p03_debt_money.cs',
     '    public double JobSearchLiteracyOff = 0;          // (1269) c130k15 K15：1＝職探しの率から識字の倍率を外す（eff＝JobSearchRatePerQuarter）・0＝今の式',
     '    public double JobSearchLiteracyOff = 1;          // (1269) c130k15 K15：1＝職探しの率から識字の倍率を外す（eff＝JobSearchRatePerQuarter）・0＝旧の式'),
    ('PriceOfBread.Tests/DormantIdentityTests.cs',
     '        "JobSearchLiteracyOff",            // ★器（2026-11-21 (1269) c130k15 K15）＝職探しの率から識字の倍率を外す（Meritocracy.Step）。0＝今の式（番人 JobSearchLiteracyOffTests）',
     '        // ★JobSearchLiteracyOff（(1269) c130k15 K15 職探しの率から識字の倍率を外す）は 2026-11-21 に 1 で腕 B の木へ＝この表から外した（旗 0 の経路の番人は JobSearchLiteracyOffTests の T2・T3 が旗を明示して持つ）。'),
]
for f, o, n in R:
    p = f'{t}/{f}'; b = open(p, 'rb').read(); ob, nb = o.encode('utf-8'), n.encode('utf-8')
    if b.count(ob) != 1: print(f'{f} 旧が {b.count(ob)} 回'); sys.exit(1)
    open(p, 'wb').write(b.replace(ob, nb)); print(f'入れた：{p}')
