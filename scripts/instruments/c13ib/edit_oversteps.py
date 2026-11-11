# (812) c13ib 時間 doc 1-7 の形 D-2：1 - Math.Pow(1 - 率, 回数) を Calendar.OverSteps へ（3 か所）。
# バイトで読み書きする（CR の数と BOM を保つ）。旧の文字列がちょうど 1 回のときだけ置き換える。
import sys

ROOT = sys.argv[1]


def edit(rel, old, new):
    p = ROOT + '/' + rel
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    if c != 1:
        print('NG', rel, c, old[:60])
        sys.exit(1)
    open(p, 'wb').write(b.replace(o, n))
    print('ok', rel)


edit('PriceOfBread.SimCore/Credit.cs',
     'if (days > 1) spd = 1.0 - Math.Pow(1.0 - spd, days);',
     'if (days > 1) spd = Calendar.OverSteps(spd, days);')
edit('PriceOfBread.SimCore/Credit.cs',
     'if (days > 1) adjSpd = 1.0 - Math.Pow(1.0 - adjSpd, days);',
     'if (days > 1) adjSpd = Calendar.OverSteps(adjSpd, days);')
edit('PriceOfBread.SimCore/Meritocracy.cs',
     'double dischargeQ = 1.0 - Math.Pow(1.0 - c.SoldierMigrationRatePerMonth.Value, Calendar.MonthsPerQuarter);',
     'double dischargeQ = Calendar.OverSteps(c.SoldierMigrationRatePerMonth.Value, Calendar.MonthsPerQuarter);')
edit('PriceOfBread.SimCore/Calendar.cs',
     '    public static double YearsToMonths(double years) => years * MonthsPerYear;\n',
     '    public static double YearsToMonths(double years) => years * MonthsPerYear;\n'
     '    /// <summary>刻みの換算（率を <paramref name="steps"/> 回に合成する）＝1 回あたりの率 <paramref name="rate"/> を毎回かけて steps 回進んだときの動いた割合 `1 − (1 − rate)^steps`。\n'
     '    /// 中身は呼び元（Credit の旬次の緩和・Meritocracy の四半期の離職）が書いていた式そのまま（Math.Pow を 1 回）。a*a*a と書いた Discovery.PriceSmoothingQ はビットが違うので通さない。</summary>\n'
     '    public static double OverSteps(double rate, double steps) => 1.0 - Math.Pow(1.0 - rate, steps);\n')
