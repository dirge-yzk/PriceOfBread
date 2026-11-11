# (849) c69ss 突然変異の編集（batch.sh prep の編集の python・使い捨ての写しの木だけを書き換える）
#   呼び方：batch.sh prep <腕> commit:<旗 1 の木> plain "scripts/instruments/c69ss/mut849.py <M1..M6>"  → python mut849.py <名> <写しの木の根>
#   事前登録 measurements/m849_20261105_prereg_ss.txt [4] の表（M1 ρ＝0・M2 U に能力の全部・M3 D2・M4 収穫係数を外す・M5 :2783 を直さない・M6 技術の比を掛けない）。
import sys, os
name, root = sys.argv[1], sys.argv[2]
p = os.path.join(root, "PriceOfBread.SimCore", "EconomyStep.cs")
s = open(p, "rb").read().decode("utf-8")
HEAD_U = "        double u = UnsoldFarmCapacity(sold, cap), t = StapleEffTech(region, coef), need = StapleNeedPerCapita;\n"
E = {
    "M1": [("{ p.SubsistenceBasis = p.SubsistenceBasis * need * p.Size / (u * t); p.SubsistenceBasisKind", "{ p.SubsistenceBasis = 0; p.SubsistenceBasisKind")],
    "M2": [(HEAD_U, "        double u = cap, t = StapleEffTech(region, coef), need = StapleNeedPerCapita;\n")],
    "M3": [(HEAD_U, "        double prd = Catalog.FarmLaborId < region.Markets.Length ? region.Markets[Catalog.FarmLaborId].LastProduced : 0;\n"
                    "        double u = prd > 1e-9 ? cap * Math.Clamp(1.0 - sold / prd, 0.0, 1.0) : 0.0, t = StapleEffTech(region, coef), need = StapleNeedPerCapita;\n")],
    "M4": [("double sub = staple ? p.GoodNeed * p.SubsistenceShare * subFactor : 0;", "double sub = staple ? p.GoodNeed * p.SubsistenceShare : 0;")],
    "M5": [("if (coef.SubsistenceStage2 > 0 && lpop.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) unsold = FarmUnemploymentStage2",
            "if (coef.SubsistenceStage2 < 0 && lpop.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) unsold = FarmUnemploymentStage2")],
    "M6": [(HEAD_U, "        double u = UnsoldFarmCapacity(sold, cap), t = 1.0, need = StapleNeedPerCapita;\n")],
}
for a, b in E[name]:
    assert s.count(a) == 1, (name, a)
    s = s.replace(a, b)
open(p, "wb").write(s.encode("utf-8"))
print("MUT", name, "ok")
