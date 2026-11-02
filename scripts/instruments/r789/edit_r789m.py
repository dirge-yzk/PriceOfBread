# r789: 使い捨ての木の InstrumentsR789.cs に種・年の絞りと州の列と棟ごとの表を足す（読むだけの計器）。引数：<木の根>
import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/InstrumentsR789.cs'
s = open(p, encoding='utf-8').read()
BS = chr(92)
def rep(a, b):
    global s
    a = a.replace('@', BS); b = b.replace('@', BS)
    assert s.count(a) == 1, (a[:60], s.count(a))
    s = s.replace(a, b)
rep('    static string R789F(double v)',
    '    static readonly ulong R789Seed = ulong.TryParse(System.Environment.GetEnvironmentVariable("POB_R789_SEED"), out var sv) ? sv : 0;   // 0＝全ての種\n'
    '    static readonly int R789Y0 = int.TryParse(System.Environment.GetEnvironmentVariable("POB_R789_Y0"), out var yv) ? yv : 0;           // この年より前は書かない\n'
    '    static string R789F(double v)')
rep('        if (names.Length == 0) return;',
    '        if (names.Length == 0) return;\n'
    '        if (R789Seed != 0 && seed != R789Seed) return;\n'
    '        if (sim.Nations.Count > 0 && sim.Nations[0].World.Year < R789Y0) return;')
rep('var rr = new System.Text.StringBuilder();', 'var rr = new System.Text.StringBuilder(); var pp = new System.Text.StringBuilder();')
rep('@tpriceLevel@tcostPriceLevel@tmechanization@n");',
    '@tpriceLevel@tcostPriceLevel@tmechanization@tindustrialization@tproductiveIndustrialization@tblockadeRisk@tpopTotalK@n");\n'
    '            pp.Append("quarter@tyear@tnation@tregion@tprodIndex@tgoodId@tgood@trecipe@tisRgo@tbankruptFlag@tscale@tbuiltScale@tcapacity@toutput@tdesired@theads@tlastRevenue@tlastCost@tinputFill@tmcSelf@tinputCostPerUnit@tlaborCostPerUnit@tprice@townerClass@tlaborClass@tsteamFill@telecFill@tsubsidyRate@n");')
rep("R789F(w.PriceLevel), R789F(w.CostPriceLevel), R789F(r.Mechanization))).Append('@n');",
    "R789F(w.PriceLevel), R789F(w.CostPriceLevel), R789F(r.Mechanization), R789F(r.Industrialization), R789F(r.ProductiveIndustrialization), R789F(w.LastBlockadeRisk), R789F(r.SumPopulation()))).Append('@n');")
rep('''                    if (s.OwnerPopIndex == r.InvestorPopIndex && r.InvestorPopIndex >= 0) a[k, 15] += s.Output;
                }''', '''                    if (s.OwnerPopIndex == r.InvestorPopIndex && r.InvestorPopIndex >= 0) a[k, 15] += s.Output;
                    if (r.Name == names[0])
                    {
                        string oc = s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count ? r.Pops[s.OwnerPopIndex].ClassName : "-";
                        string lcN = s.LaborPopIndex >= 0 && s.LaborPopIndex < r.Pops.Count ? r.Pops[s.LaborPopIndex].ClassName : "-";
                        pp.Append(head).Append('@t').Append(string.Join('@t', r.Producers.IndexOf(s).ToString(inv), s.GoodId.ToString(inv), Catalog.Goods[s.GoodId].Name, s.RecipeName.Length == 0 ? "-" : s.RecipeName, s.IsRgo ? "1" : "0", s.Bankrupt ? "1" : "0",
                            R789F(s.Scale), R789F(s.BuiltScale), R789F(s.Capacity), R789F(s.Output), R789F(s.DesiredOutput), R789F(s.HeadsLastMonth), R789F(s.LastRevenue), R789F(s.LastCost), R789F(s.LastInputFill),
                            R789F(mcSelf), R789F(ic), R789F(lc), R789F(r.Markets[s.GoodId].Price), oc, lcN, R789F(s.SteamFill), R789F(s.ElecFill), R789F(s.SubsidyRate.Value))).Append('@n');
                    }
                }''')
rep('Put("r789r", rr);', 'Put("r789r", rr); Put("r789p", pp);')
open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('EDITED', p)
