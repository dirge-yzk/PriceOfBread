"""[計測の写しだけ] (161) の写し＝army/inj_mil3.py の A 行の末尾に兵士の補給の充足（SupplyFill）と主食の充足（FoodFill）の人口加重を足した版。以下は元の説明＝(155) の写し＝sol/inj_mil2.py の Military.cs の錨を (155) の木（定員 EstablishmentOf）へ合わせた版（YMsus＝定員・YMdPeace＝定員・YMman＝国の母数）。末尾に兵士の未払いの割合・陸海の熟練・安全保障の不安・器を足した。旧の説明＝(147) の写し＝mil/inj_mil.py の錨を (147) の木（平時の目標の行）にも合わせた版。軍の費用と規模の計器（(142) 線2 前半・読むだけ）。
Military.Step の兵数の目標の内訳（平時の人口の項・扶養限界・戦時の徴兵の目標・母数）と、EconomyStep の軍の支払い（財ごと＝兵力・小銃・弾薬・火砲・馬・自動車・軍艦・汽船・上位段・tier3・空軍・燃料）を
World の診断の欄へ積み、SelfPlayMatch.Run の各四半期の後に全ての国の1行（A|…）を書く。出力＝環境変数 POB_MIL_DIR の下に a<seed>.txt。未設定なら行を作らない。
A 列：A|四半期|年|添字|BaseName|生存|年名目GDP|人口|兵士|戦争の月|内戦の月|徴兵法|陸の維持費|海の維持費|総動員|月数|
      扶養限界の目標(月平均)|平時の目標Σ(月平均)|最終の目標Σ(月平均)|戦時の徴兵の目標Σ(月平均)|母数Σ(月平均)|
      大衆の1人あたり月収|兵士の1人あたり月収|前期の歳入|使える歳入|
      支払い(四半期)：兵力|小銃|弾薬|火砲|馬|自動車|軍艦|汽船|機関銃|装甲艦|戦車|弩級|軍用機|化学|燃料|
      装備の充足|即応|首都の軍需予算|艦隊|兵士の給与の請求(四半期)
力学は読まない（加算と記録だけ）。使い方：python inj_mil.py <写しの木>"""
import sys, re
root = sys.argv[1]
MARK = '[' + '計器の写しだけ' + ']'


def load(rel):
    p = root + '/' + rel
    raw = open(p, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    s = raw.decode('utf-8-sig')
    return p, bom, s, ('\r\n' in s)


def save(p, bom, s):
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


def edit(rel, pairs):
    p, bom, s, crlf = load(rel)

    def nl(x):
        return x.replace('\n', '\r\n') if crlf else x
    for a, b, mode, cnt in pairs:
        a = nl(a)
        b = nl(b)
        assert s.count(a) == cnt, (rel, a[:70], s.count(a))
        if mode == 'after':
            s = s.replace(a, a + b)
        elif mode == 'before':
            s = s.replace(a, b + a)
        else:
            s = s.replace(a, b)
    save(p, bom, s)


edit('PriceOfBread.SimCore/World.cs', [
    ('    public double[] TreasuryLedger = new double[(int)TreasuryCat.Count];',
     '\n    public double YMn, YMwar, YMcw, YMsus, YMdPeace, YMdFinal, YMdWar, YMman, YMpayClaim; public double[] YMeq = new double[256];   // ' + MARK, 'after', 1),
])
edit('PriceOfBread.SimCore/Military.cs', [
    ('        double seatsNat = EstablishmentOf(w, c, out double natManpower);\n',
     '        w.YMn += 1; w.YMsus += seatsNat; w.YMdPeace += seatsNat; w.YMman += natManpower; if (atWar) w.YMwar += 1; if (w.InCivilWar) w.YMcw += 1;\n', 'after', 1),
    ('            if (wartimeConscript) { desired = Math.Max(desired, conscriptFrac * lawFrac * manpower); rate = c.ConscriptionRate; }  // 戦時＝徴兵（速く・上限まで）\n',
     '            w.YMdFinal += desired; if (wartimeConscript) w.YMdWar += conscriptFrac * lawFrac * manpower;\n', 'after', 1),
])
_e = open(root + '/PriceOfBread.SimCore/EconomyStep.cs', encoding='utf-8-sig').read()
PAYANCHOR = '            revenue[Catalog.MilitaryPowerId] += pay;\n' if 'revenue[Catalog.MilitaryPowerId] += pay;' in _e else '            region.Pops[region.SoldierPopIndex].EarnedPerMonth += pay;\n'
edit('PriceOfBread.SimCore/EconomyStep.cs', [
    (PAYANCHOR, '            if (world is not null) world.YMeq[Catalog.MilitaryPowerId] += pay;\n', 'before', 1),
    ('                region.StateSpent[(int)TreasuryCat.Army] += mfPay;\n', '                if (world is not null) world.YMeq[Catalog.CrudeOilId] += mfPay;\n', 'after', 1),
    ('                    region.StateSpent[(int)TreasuryCat.Army] += afPay;\n', '                    if (world is not null) world.YMeq[Catalog.CrudeOilId] += afPay;\n', 'after', 1),
])
p, bom, t, crlf = load('PriceOfBread.SimCore/EconomyStep.cs')
pat = re.compile(r'(revenue\[gi\] \+= pay;)(\r?\n[ ]*region\.StateSpent\[\(int\)TreasuryCat\.Army\] \+= pay;)')
t, n = pat.subn(lambda m: m.group(1) + ' if (world is not null) world.YMeq[gi] += pay;' + m.group(2), t)
assert n == 3, n
save(p, bom, t)
edit('PriceOfBread.SimCore/Simulation.cs', [
    (('                    World.LastSoldierPayPerMonth += r.SoldierPayBudget;' if '                    World.LastSoldierPayPerMonth += r.SoldierPayBudget;' in open(root + '/PriceOfBread.SimCore/Simulation.cs', encoding='utf-8-sig').read() else '            World.LastSoldierPayPerMonth += r.SoldierPayBudget;'), ' World.YMpayClaim += r.SoldierPayBudget;', 'after', 1),
])
BLOCK = '''                if (ymSb is not null)
                {
                    int[] ymG = [Catalog.MilitaryPowerId, Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId, Catalog.HorseId, Catalog.AutomobileId, Catalog.WarshipId, Catalog.SteamshipId, Catalog.MachineGunId, Catalog.IroncladId, Catalog.TankId, Catalog.DreadnoughtId, Catalog.MilitaryAircraftId, Catalog.ChemicalWeaponId, Catalog.CrudeOilId];
                    for (int yi = 0; yi < sim.Nations.Count; yi++)
                    {
                        var yw = sim.Nations[yi].World;
                        double ymE = 0, ymS = 0, ysS = 0, ysE = 0, yap = 0, yUnpW = 0, ySupW = 0, yFoodW = 0;
                        foreach (var yr in yw.Regions)
                        {
                            if (yr.State != RegionState.Active) continue;
                            foreach (var ypp in yr.Pops)
                            {
                                yap += ypp.Size;
                                if (ypp.IsSoldier) { ysS += ypp.Size; ysE += ypp.EarnedPerMonth; yUnpW += ypp.Size * ypp.UnemploymentRate; ySupW += ypp.Size * ypp.SupplyFill; yFoodW += ypp.Size * ypp.FoodFill; continue; }
                                if (Politics.IsMassPop(ypp)) { ymS += ypp.Size; ymE += ypp.EarnedPerMonth; }
                            }
                        }
                        double yn = System.Math.Max(1, yw.YMn); double yUnp = ysS > 0 ? yUnpW / ysS : 0;
                        var ysb = new System.Text.StringBuilder();
                        foreach (int g in ymG) { ysb.Append('|'); ysb.Append(yw.YMeq[g].ToString("F2")); }
                        ymSb.Append($"A|{q + 1}|{yw.Year}|{yi}|{yw.SelfPower?.BaseName}|{(yw.TotalPopulation > 0 ? 1 : 0)}|{yw.NominalGdpPerMonth * 12:F1}|{yap:F2}|{ysS:F3}|{yw.YMwar}|{yw.YMcw}|{(int)yw.ConscriptionLaw}|{yw.ArmyUpkeep:F3}|{yw.NavyUpkeep:F3}|{(yw.Mobilized ? 1 : 0)}|{yw.YMn}|"
                            + $"{yw.YMsus / yn:F3}|{yw.YMdPeace / yn:F3}|{yw.YMdFinal / yn:F3}|{yw.YMdWar / yn:F3}|{yw.YMman / yn:F2}|{(ymS > 0 ? ymE / ymS : 0):F5}|{(ysS > 0 ? ysE / ysS : 0):F5}|{yw.LastRevenuePerMonth:F2}|{yw.SpendableRevenue():F2}"
                            + ysb.ToString() + $"|{yw.Region.MilitaryEquipFill:F3}|{yw.MilitaryReadiness:F3}|{yw.Region.MilitaryBudget:F2}|{yw.Region.FleetStock:F2}|{yw.YMpayClaim:F2}|{yUnp:F4}|{yw.ArmySkill:F4}|{yw.NavySkill:F4}|{yw.SecurityThreat:F4}|{yw.SecurityThreatOwnMass:F2}|{yw.MilitarySpending:F3}|{PriceOfBread.SimCore.Military.EnvelopeOf(yw, yw.Coef):F3}|{yw.MilitaryTrainingFill:F3}|{yw.FiscalFundingRatio:F3}|{(yw.SecurityThreat + yw.SecurityThreatOwnMass > 0 ? yw.SecurityThreat / (yw.SecurityThreat + yw.SecurityThreatOwnMass) : 0):F4}|{(ysS > 0 ? ySupW / ysS : 0):F4}|{(ysS > 0 ? yFoodW / ysS : 0):F4}").AppendLine();
                        yw.YMn = yw.YMwar = yw.YMcw = yw.YMsus = yw.YMdPeace = yw.YMdFinal = yw.YMdWar = yw.YMman = yw.YMpayClaim = 0; System.Array.Clear(yw.YMeq);
                    }
                    if (q == quarters - 1) System.IO.File.WriteAllText(System.IO.Path.Combine(ymDir!, $"a{seed}.txt"), ymSb.ToString());
                }
'''
edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ('        sim.StartWarmup();\n', '        var ymDir = System.Environment.GetEnvironmentVariable("POB_MIL_DIR"); var ymSb = ymDir is { Length: > 0 } ? new System.Text.StringBuilder() : null;\n'
        '        foreach (var ym0 in sim.Nations) { var yw0 = ym0.World; yw0.YMn = yw0.YMwar = yw0.YMcw = yw0.YMsus = yw0.YMdPeace = yw0.YMdFinal = yw0.YMdWar = yw0.YMman = yw0.YMpayClaim = 0; System.Array.Clear(yw0.YMeq); }\n', 'after', 1),
    ('                if (sim.Nations.Count > gdpPeak.Length)', BLOCK, 'before', 1),
])
print('ok mil', root)
