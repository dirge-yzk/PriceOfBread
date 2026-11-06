# (894) c76lb 突然変異の置き換え（バイトで読み書き＝改行と BOM を保つ・ちょうど 1 か所でなければ失敗）。
#   python -X utf8 mutapply894.py <ファイル> <番号>      （c75ce の mutapply888.py の型を写した）
import sys

MUT = {
    'U1': [("double privBase = p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0;", "double privBase = 1.0;")],
    'U2': [("foreach (var r in w.Regions) foreach (var p in r.Pops) s += System.Math.Max(0, p.LandRentPerMonth.Value);",
            "foreach (var r in w.Regions) foreach (var p in r.Pops) s += p.LandRentPerMonth.Value;")],
    'U3': [("foreach (var r in w.Regions) foreach (var p in r.Pops) s += System.Math.Max(0, p.LandRentPerMonth.Value);",
            "foreach (var r in w.Regions) foreach (var p in r.Pops) s += System.Math.Max(0, p.LandRentPerMonth.Value); if (w.ExportsByGood is { } eg) foreach (var e in eg) s += e;")],
    'U4': [("internal const int LandbaseLagQuarters = 4;", "internal const int LandbaseLagQuarters = 3;")],
    'U5': [("I(regBase <= 0 && regBefore > 0 ? 1 : 0)", "I(regBase < 0 && regBefore > 0 ? 1 : 0)")],
    'U6': [("double rentRaw = p.LandRentPerMonth.Value;", "double rentRaw = p.LandRentPerMonth.Value; p.LandRentPerMonth = new MoneyPerMonth(0);")],
    'U7': [("if (double.IsFinite(rs)) extShare += landRate * rs * v;", "if (double.IsFinite(rs)) extShare += landRate * v;")],
    'U8': [("extGross += landRate * v;", "extGross += landRate * (double.IsFinite(rs) ? rs : 1.0) * v;")],
    'U9': [("shortGoods += (cl - held) / stapleP;", "shortGoods += (cl - held);")],
    'U10': [("else if (p.Size <= 1000) k1000++; else kOver++;", "else if (p.Size <= 1000) k1000++; else k1000++;")],
    'U11': [("cl += titheRate * p.LastEarnedPerMonth.Value;", "cl += 0;")],
    'U12': [("r.Pops[s.OwnerPopIndex].Size > 1e-12)) empty++;", "r.Pops[s.OwnerPopIndex].Size <= 1e-12)) empty++;")],
    'U13': [("internal const double LandbaseIdentTol = 1e-12;", "internal const double LandbaseIdentTol = 1;")],
    'U14': [("pw += Catalog.Goods[g].BaseNeed * r.Markets[g].Price; pn += Catalog.Goods[g].BaseNeed;", "pw += r.Markets[g].Price; pn += 1;")],
    'U15': [("claim += k; regClaim += k;", "claim += k; regClaim += landRate * rentRaw;")],
}

def main():
    path, idx = sys.argv[1], sys.argv[2]
    with open(path, 'rb') as fh:
        b = fh.read()
    for a, c in MUT[idx]:
        ab, cb = a.encode('utf-8'), c.encode('utf-8')
        n = b.count(ab)
        if n != 1:
            print('mut', idx, '一致の数', n, a[:60]); sys.exit(1)
        b = b.replace(ab, cb)
    with open(path, 'wb') as fh:
        fh.write(b)
    print('mut', idx, '当てた')

main()
