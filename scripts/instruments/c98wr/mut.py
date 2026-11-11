# (1038) c98wr 突然変異（使い捨ての木に当てる・旗 3 本＝1 の木から）。呼び方：python -X utf8 -I mut.py <木の根> <M1..M7>
# 事前登録 measurements/m1038_20261111_prereg_wr.txt の表のとおり。当たらなければ assert で止まる（台本の錨は旗 1 の木の文）。
import sys, os
root, m = sys.argv[1], sys.argv[2]
W = os.path.join(root, "PriceOfBread.SimCore", "World.cs")
D = os.path.join(root, "PriceOfBread.SimCore", "Diplomacy.cs")
def rep(p, o, n):
    b = open(p, "rb").read()
    o, n = o.encode("utf-8"), n.encode("utf-8")
    assert b.count(o) == 1, (m, o[:60])
    open(p, "wb").write(b.replace(o, n))
if m == "M1":   # 余剰の門を ExpansionWill のまま
    rep(W, "if (!((surplus ?? Diplomacy.InvadeSurplus(SelfPower, Coef)) >= need))", "if (ExpansionWill < WillNeed(target))")
elif m == "M2":   # S から前線の項を外す
    rep(D, "return ArmyShape.LandPotential(p) * (1 - b) - (p.NeedLand + p.NeedInternal);", "return ArmyShape.LandPotential(p) - (p.NeedLand + p.NeedInternal);")
elif m == "M3":   # 1−τ
    rep(D, "        return 1 - tau * tau;", "        return 1 - tau;")
elif m == "M4":   # τ÷100
    rep(D, "double tau = !(tension > 0) ? 0 : tension >= declare ? 1 : tension / declare;\r\n        return 1 - tau * tau;",
        "double tau = !(tension > 0) ? 0 : tension >= declare ? 1 : tension / declare / 100;\r\n        return 1 - tau * tau;")
elif m == "M5":   # 弱小国の門を残す
    rep(W, "if (!(Coef.WarTensionDiscountOpen > 0) && Diplomacy.RankOf(target) != PowerRank.Minor)", "if (Diplomacy.RankOf(target) != PowerRank.Minor)")
elif m == "M6":   # 侵略の入口だけ旧の屈服
    rep(D, "bool submit = c.WarSubmitUnified > 0 ? SubmitYields(great, target, c) : force >= SubmitLine(target, c);", "bool submit = force >= SubmitLine(target, c);")
elif m == "M7":   # 緊張 100% の自動開戦を外す（旗で）
    rep(D, "return Tension(a, b) >= c.WarDeclareTension && !(atWar is null", "return !(c.WarTensionDiscountOpen > 0) && Tension(a, b) >= c.WarDeclareTension && !(atWar is null")
else:
    raise SystemExit("unknown " + m)
print("applied", m)
