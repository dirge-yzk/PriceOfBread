# (997) c91sl 突然変異（mut991.py の写し・旗 1＋c91lc の頭を merge した使い捨ての木で 1 本ずつ当て、LandSaleTests と LandsaleInstrumentTests を回して赤の試験を記録し、元へ戻す）。M9・M10 は等価の予言（事前登録 m997 [6]）。
# 使い方: python mut991.py <使い捨ての木> <artifacts> <出力> [M1,M2,...]
import sys, subprocess, re, os
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
only = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None
LS = 'PriceOfBread.SimCore/LandSale.cs'; FI = 'PriceOfBread.SimCore/Fiscal.cs'
V = 'kLand * ruleMul[pi] * (1.0 - rp)'
M = [
 ('M1', '値段を総額で（買い手の税を引かない）', LS, 'double net = seller.LandRentPerMonth.Value - tau * Fiscal.LandTaxBase(c, seller).Value * (sumSpb / sumS);', 'double net = seller.LandRentPerMonth.Value;'),
 ('M2', 'V に届き（ruleMul）を掛けない', FI, '(landSales ??= []).Add((pi, ' + V + '));', '(landSales ??= []).Add((pi, kLand * (1.0 - rp)));'),
 ('M3', 'V に (1−rp) を掛けない', FI, '(landSales ??= []).Add((pi, ' + V + '));', '(landSales ??= []).Add((pi, kLand * ruleMul[pi]));'),
 ('M4', '代金を走査の revenue にも足す', FI, '(landSales ??= []).Add((pi, ' + V + '));', '{ (landSales ??= []).Add((pi, ' + V + ')); revenue += ' + V + '; }'),
 ('M5', '持ち分を動かさない', LS, 'if (!(aSum > 0)) return movedSum;', 'return movedSum;'),
 ('M6', '買い手を投資家（枠 3）だけ', LS, 'if (j >= 0 && j != i && !buyers.Contains(j)) buyers.Add(j);', 'if (j >= 0 && j != i && k == 3 && !buyers.Contains(j)) buyers.Add(j);'),
 ('M7', '法の門を外す', LS, 'if (w.SerfEmancipation == ReformLevel.None || owed.Count == 0) return;', 'if (owed.Count == 0) return;'),
 ('M8', '国家の枠（k＝4）も売る', LS, 'for (int k = 0, nk = System.Math.Min(4, ls.Length); k < nk; k++)', 'for (int k = 0, nk = ls.Length; k < nk; k++)'),
 ('M9', '寄せの枠を買い手に入れる（生の受け手の代わりに寄せた受け手）', LS, 'return j >= 0 && j < r.Pops.Count ? j : -1;', 'return k < 4 ? EconomyStep.LandShareRecipient(r, s, k) : -1;'),
 ('M10', '貯蓄の上限（φ≤ΣS÷P と額≤S_j）を外す', LS, [('if (phi > sumS / price) phi = sumS / price;', ''), ('double amt = System.Math.Min(phi * (sj / sumS) * price, sj);', 'double amt = phi * (sj / sumS) * price;')], None),
 ('M11', 'φ≤1 を外す', LS, 'if (phi > 1) phi = 1;', ''),
 ('M12', '×12 を落とす（値段を月で）', LS, 'double price = Calendar.PerMonthToPerYear(net) / rate;', 'double price = net / rate;'),
]
res = []
for name, desc, f, old, new in M:
    if only and name not in only: continue
    p = os.path.join(tree, f)
    orig = open(p, 'rb').read()
    s = orig.decode('utf-8')
    pairs = old if isinstance(old, list) else [(old, new)]
    for o, n in pairs:
        assert s.count(o) == 1, (name, o)
        s = s.replace(o, n, 1)
    open(p, 'wb').write(s.encode('utf-8'))
    try:
        b = subprocess.run(['dotnet', 'build', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '-v', 'q', '-nologo'], cwd=tree, capture_output=True, text=True, encoding='utf-8', errors='replace')
        if b.returncode != 0:
            res.append((name, desc, 'ビルド失敗', '')); continue
        t = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--no-build', '-nologo',
                            '--filter', 'FullyQualifiedName~LandSaleTests|FullyQualifiedName~LandsaleInstrumentTests', '--logger', 'console;verbosity=normal'], cwd=tree, capture_output=True, text=True, encoding='utf-8', errors='replace')
        fails = sorted(set(re.findall(r'(?:LandSaleTests|LandsaleInstrumentTests)\.(\w+) \[FAIL\]', t.stdout) + re.findall(r'失敗 PriceOfBread\.Tests\.(?:LandSaleTests|LandsaleInstrumentTests)\.(\w+)', t.stdout)))
        nfail = re.findall(r'失敗:\s+(\d+)', t.stdout)
        summ = [l for l in t.stdout.splitlines() if '合計' in l]
        red = bool(fails) or (nfail and int(nfail[-1]) > 0) or t.returncode != 0
        res.append((name, desc, '赤' if red else '緑', ' '.join(fails) + ' ｜ ' + (summ[-1].strip() if summ else '')))
    finally:
        open(p, 'wb').write(orig)
with open(out, 'w', encoding='utf-8') as fo:
    fo.write('# (997) c91sl 突然変異（旗 1＋c91lc を merge した使い捨ての木・LandSaleTests と LandsaleInstrumentTests）\n')
    for r in res: fo.write('\t'.join(r) + '\n')
print('MUTDONE')
