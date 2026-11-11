# (901) 突然変異の置き換え（バイトで読み書き・(896) mut896.py の型）：python -X utf8 mut901.py <木> <名前>
import sys
tree, name = sys.argv[1], sys.argv[2]
CP = tree + "/PriceOfBread.SimCore/CrownParliament.cs"
IC = tree + "/PriceOfBread.SimCore/InstrumentsCrown.cs"
ENG = "w.ParliamentControl = w.ParliamentControl + c.SummonParliamentRate * s * System.Math.Max(0, t - w.ParliamentControl);"
M = {
 "M0_none": None,
 "M1e_engine": (CP, ENG, "_ = s * t;"),   # エンジンだけ何もしない（:39 を外す・計器は旗 1 のまま）
 "M1_s0": (CP, "double s = PoliticalQuantities.Clamp01(x / c.MaxInterestShareOfRevenue.Value);", "double s = 0;"),
 "M2_t1": (CP, "double t = PowerShares.DebtGrip(w);", "double t = 1.0;"),
 "M3_noguard": (CP, "double x = !(rev > 0) ? 0 : w.LastInterestPerMonth.Value / rev;", "double x = w.LastInterestPerMonth.Value / rev;"),
 "M4_step": (CP, "double s = PoliticalQuantities.Clamp01(x / c.MaxInterestShareOfRevenue.Value);", "double s = x >= c.MaxInterestShareOfRevenue.Value ? 1 : 0;"),
 "M5_insnosplit": (IC, "if (c.SummonParliamentRate > 0 && c.SummonByInterestShare > 0)", "if (false)"),
 "M6_clamp": (CP, ENG, "w.ParliamentControl = PoliticalQuantities.Clamp01(w.ParliamentControl + c.SummonParliamentRate * s * System.Math.Max(0, t - w.ParliamentControl));"),
 "M7_nomax": (CP, "c.SummonParliamentRate * s * System.Math.Max(0, t - w.ParliamentControl);", "c.SummonParliamentRate * s * (t - w.ParliamentControl);"),
 "M8_flag0": (CP, "(w.AusterityIntensity - 0.5) * 2.0);\n        }", "(w.AusterityIntensity - 0.5) * 2.1);\n        }"),
}
if M[name] is None:
    print("applied", name); sys.exit(0)
p, a, b = M[name]
s = open(p, "rb").read()
assert s.count(a.encode()) == 1, (name, s.count(a.encode()))
open(p, "wb").write(s.replace(a.encode(), b.encode()))
print("applied", name)
