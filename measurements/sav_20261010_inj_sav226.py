# (226) 線S 前半の計器（読むだけ・隔離した写しだけに当てる・コミットしない）。使い方： python inj_sav226.py <木の根>
# 環境変数 POB_SAV226_DIR の下に s<seed>.txt。未設定なら1命令も走らない（分岐の中だけ）。力学は読まない（加算と記録だけ）。
#   R 行＝置き直しの前後（phase 0＝前・1＝後）の全国：基礎貨幣・与信・カバー率・階級別の貯蓄/所得/人数・所得が正でない POP・利益集団の富の土地の分と貨幣の分
#   S 行＝国×四半期（英は毎四半期・他は年末）：貨幣・与信・国庫・国債・物価・兌換・カバー率・階級別の貯蓄・投資の予算が縛った月・消費の予算が縛った人口比（階級別）・
#        借入の月の内訳（門が閉じた月・1件の額が縛った月・プールが縛った月・満額払えた月＝払えない額の和と借りた額）
import sys
root = sys.argv[1]


def edit(rel, pairs, tail=''):
    p = root + '/' + rel
    raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
    CR, LF = chr(13), chr(10)
    crlf = (CR + LF) in s
    for old, new in pairs:
        if crlf: old = old.replace(LF, CR + LF); new = new.replace(LF, CR + LF)
        assert s.count(old) == 1, (rel, old[:90], s.count(old))
        s = s.replace(old, new)
    s += tail.replace(LF, CR + LF) if crlf else tail
    open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))


NEWFILE = r'''namespace PriceOfBread.SimCore;

/// <summary>[計測の写しだけ] (226) 開幕の貯蓄の式の計器（読むだけ）。環境変数 POB_SAV226_DIR の下に s&lt;seed&gt;.txt。未設定なら1命令も走らない。</summary>
public static class Sav226
{
    public static readonly string? Dir = System.Environment.GetEnvironmentVariable("POB_SAV226_DIR");
    public static readonly bool On = Dir is { Length: > 0 };
    static string F(double v) => double.IsNaN(v) ? "nan" : v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);
    const int NC = 14;
    sealed class S
    {
        public double InvN, InvBound, InvDes, InvAff;
        public double ConsMonths; public double[] ConsSize = new double[NC], ConsBound = new double[NC];
        public double[] TaxClaim = new double[NC], TaxPaid = new double[NC], RentC = new double[NC], WageC = new double[NC], TithePaid = new double[NC], PaidC = new double[NC], DesC = new double[NC * 3], DemC = new double[NC * 3];
        public double BDirect, BIndirect, BLeak, BBase;
        public double BShortMonths, BGate, BTicket, BPool, BFull, BShortGate, BShortTicket, BShortPool, BShortFull, BBorrowed, BDomPool, BForPool, BTicketSum;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, S> G = new();
    static S V(World w) => G.GetValue(w, _ => new S());
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, System.Text.StringBuilder> B = new();
    static System.Text.StringBuilder Buf(MultiNationSim s) => B.GetValue(s, _ => new System.Text.StringBuilder());
    static string Name(World w) => w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "?";

    static double LandedW(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Closed && w.Coef.LandedPowerCapitalization > 0) s += w.Coef.LandedPowerCapitalization * System.Math.Max(0, p.LandRent); return s; }
    static double MoneyW(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) s += System.Math.Max(0, p.Savings); return s; }

    public static void Rebase(MultiNationSim sim, World w, int phase)
    {
        var sb = Buf(sim);
        double[] sav = new double[NC], earn = new double[NC], last = new double[NC], size = new double[NC];
        double zeroN = 0, zeroSav = 0, zeroSize = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            sav[c] += p.Savings; earn[c] += p.Earned; last[c] += p.LastEarned; size[c] += p.Size;
            if (!(p.LastEarned > 0)) { zeroN += 1; zeroSav += p.Savings; zeroSize += p.Size; }
        }
        double bm = w.BroadMoney, cov = bm > 1e-9 ? w.BaseMoney / bm : 1.0;
        sb.Append("R|").Append(Name(w)).Append('|').Append(phase).Append('|').Append(w.SavingsFromIncome ? 1 : 0);
        foreach (var v in new[] { bm, w.BaseMoney, w.Bank?.Loans ?? 0.0, w.Bank?.HouseholdLoans ?? 0.0, w.Treasury, cov, w.NominalGdp * 12, w.Gdp * 12, w.PriceLevel, w.Convertibility, w.OnGoldStandard ? 1 : 0, LandedW(w), MoneyW(w), zeroN, zeroSav, zeroSize })
            sb.Append('|').Append(F(v));
        foreach (var arr in new[] { sav, earn, last, size }) foreach (var v in arr) sb.Append('|').Append(F(v));
        sb.Append('\n');
    }

    public static void Consume(World w, Region region)
    {
        var s = V(w); s.ConsMonths += 1;
        foreach (var p in region.Pops)
        {
            if (!(p.Size > 0) || !(p.Savings > 0)) continue;   // 貯蓄が正の POP だけ（貯蓄0の植民地の現地住民は予算の比に入れない）
            int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
            s.ConsSize[c] += p.Size; s.PaidC[c] += p.Paid;
            if (p.RemainingBudget <= 0.01 * System.Math.Max(p.Savings, 1e-9)) s.ConsBound[c] += p.Size;
        }
    }

    public static void Prod(World w, Pop owner, double rent, Pop labor, double wages)
    {
        var s = V(w);
        s.RentC[(uint)owner.ClassId < (uint)NC ? owner.ClassId : NC - 1] += rent; s.WageC[(uint)labor.ClassId < (uint)NC ? labor.ClassId : NC - 1] += wages;
    }

    public static void Tax(World w, Pop p, double claims, double tax, double tithe)
    {
        var s = V(w); int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
        s.TaxClaim[c] += claims; s.TaxPaid[c] += tax; s.TithePaid[c] += tithe;
    }

    public static void Budget(World w, double direct, double indirect, double leak, double budgetBase)
    {
        var s = V(w); s.BDirect += direct; s.BIndirect += indirect; s.BLeak += leak; s.BBase += budgetBase;
    }

    public static void Row(World w, Pop p, int tier, double desired, double demand)
    {
        var s = V(w); int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1;
        s.DesC[c * 3 + tier] += desired; s.DemC[c * 3 + tier] += demand;
    }

    public static void Invest(World w, double desired, double afford)
    {
        var s = V(w); s.InvN += 1; s.InvDes += desired; s.InvAff += afford;
        if (afford < desired) s.InvBound += 1;
    }

    public static void Gate(World w, double shortfall) { var s = V(w); s.BShortMonths += 1; s.BGate += 1; s.BShortGate += shortfall; }

    public static void Borrow(World w, double shortfall, double ticket, double domPool, double forPool, double borrowed)
    {
        var s = V(w); s.BShortMonths += 1; s.BBorrowed += borrowed; s.BDomPool += domPool; s.BForPool += forPool; s.BTicketSum += ticket;
        double want = System.Math.Min(ticket, shortfall);
        if (borrowed < want - 1e-9) { s.BPool += 1; s.BShortPool += shortfall; }
        else if (ticket < shortfall) { s.BTicket += 1; s.BShortTicket += shortfall; }
        else { s.BFull += 1; s.BShortFull += shortfall; }
    }

    public static void Quarter(MultiNationSim sim, ulong seed, int q, int quarters)
    {
        var sb = Buf(sim);
        for (int i = 0; i < sim.Nations.Count; i++)
        {
            var w = sim.Nations[i].World;
            string nm = Name(w);
            bool uk = nm == "イギリス";
            G.TryGetValue(w, out var s);
            if (uk || q % 4 == 3)
            {
                double bm = w.BroadMoney, cov = bm > 1e-9 ? w.BaseMoney / bm : 1.0;
                double[] sav = new double[NC], inc = new double[NC];
                foreach (var r in w.Regions) foreach (var p in r.Pops) { int c = (uint)p.ClassId < (uint)NC ? p.ClassId : NC - 1; sav[c] += p.Savings; inc[c] += p.LastEarned; }
                sb.Append("S|").Append(q).Append('|').Append(w.Year).Append('|').Append(i).Append('|').Append(nm);
                foreach (var v in new[] { w.NominalGdp * 12, w.Gdp * 12, bm, w.BaseMoney, w.Bank?.Loans ?? 0.0, w.Bank?.HouseholdLoans ?? 0.0, w.Treasury, w.GovDebt, w.PriceLevel, w.Convertibility, w.OnGoldStandard ? 1 : 0, cov, LandedW(w), MoneyW(w) })
                    sb.Append('|').Append(F(v));
                foreach (var v in sav) sb.Append('|').Append(F(v));
                foreach (var v in inc) sb.Append('|').Append(F(v));
                sb.Append('|').Append(uk ? F(PolicyPreferences.NetSupport(w, PolicyAxis.ProgressiveTax, true)) : "nan");
                if (s is not null)
                {
                    foreach (var v in new[] { s.InvN, s.InvBound, s.InvDes, s.InvAff, s.ConsMonths }) sb.Append('|').Append(F(v));
                    for (int c = 0; c < NC; c++) sb.Append('|').Append(F(s.ConsSize[c] > 0 ? s.ConsBound[c] / s.ConsSize[c] : double.NaN));
                    foreach (var v in new[] { s.BShortMonths, s.BGate, s.BTicket, s.BPool, s.BFull, s.BShortGate, s.BShortTicket, s.BShortPool, s.BShortFull, s.BBorrowed, s.BDomPool, s.BForPool, s.BTicketSum }) sb.Append('|').Append(F(v));
                    foreach (var v in s.TaxClaim) sb.Append('|').Append(F(v));
                    foreach (var v in s.TaxPaid) sb.Append('|').Append(F(v));
                    foreach (var v in s.RentC) sb.Append('|').Append(F(v));
                    foreach (var v in s.WageC) sb.Append('|').Append(F(v));
                    foreach (var v in s.TithePaid) sb.Append('|').Append(F(v));
                    foreach (var v in new[] { s.BDirect, s.BIndirect, s.BLeak, s.BBase }) sb.Append('|').Append(F(v));
                    foreach (var v in s.PaidC) sb.Append('|').Append(F(v));
                    foreach (var v in s.DesC) sb.Append('|').Append(F(v));
                    foreach (var v in s.DemC) sb.Append('|').Append(F(v));
                }
                sb.Append('\n');
            }
            if (s is not null)
            {   // 四半期の計数は毎四半期ゼロへ（英以外は年末の行に出る分が直近の1四半期ぶんだけ＝記録の粒度）
                s.InvN = s.InvBound = s.InvDes = s.InvAff = 0; s.ConsMonths = 0; System.Array.Clear(s.ConsSize); System.Array.Clear(s.ConsBound);
                s.BShortMonths = s.BGate = s.BTicket = s.BPool = s.BFull = s.BShortGate = s.BShortTicket = s.BShortPool = s.BShortFull = s.BBorrowed = s.BDomPool = s.BForPool = s.BTicketSum = 0; System.Array.Clear(s.TaxClaim); System.Array.Clear(s.TaxPaid); System.Array.Clear(s.RentC); System.Array.Clear(s.WageC); System.Array.Clear(s.TithePaid); System.Array.Clear(s.PaidC); System.Array.Clear(s.DesC); System.Array.Clear(s.DemC); s.BDirect = s.BIndirect = s.BLeak = s.BBase = 0;
            }
        }
        if (q != quarters - 1) return;
        using var W = new System.IO.StreamWriter(System.IO.Path.Combine(Dir!, $"s{seed}.txt"), false, new System.Text.UTF8Encoding(false), 1 << 16);
        W.Write("#R|name|phase|flag|broadMoney|baseMoney|bankLoans|hhLoans|treasury|coverage|annNomGdp|annGdp|P|conv|onGold|landedW|moneyW|zeroIncomePops|zeroIncomeSav|zeroIncomeSize|sav[14]|earned[14]|lastEarned[14]|size[14]\n");
        W.Write("#S|q|yr|idx|name|annNomGdp|annGdp|broadMoney|baseMoney|bankLoans|hhLoans|treasury|govDebt|P|conv|onGold|coverage|landedW|moneyW|sav[14]|lastEarned[14]|progTaxNetSupport|invMonths|invBound|invDesired|invAfford|consMonths|consBoundShare[14]|borrowShortMonths|gateClosed|ticketBound|poolBound|full|shortGate|shortTicket|shortPool|shortFull|borrowed|domPool|forPool|ticketSum|taxClaim[14]|taxPaid[14]|rentEarned[14]|wageEarned[14]|tithePaid[14]|budgetDirect|budgetIndirect|budgetLeak|budgetBase|paid[14]|desired[14x3tier]|demand[14x3tier]\n");
        W.Write(sb.ToString());
    }
}
'''

if 'SavingsFromIncome' not in open(root + '/PriceOfBread.SimCore/World.cs', encoding='utf-8-sig').read(): NEWFILE = NEWFILE.replace('w.SavingsFromIncome ? 1 : 0', '0')
open(root + '/PriceOfBread.SimCore/Sav226Diag.cs', 'wb').write(b'\xef\xbb\xbf' + NEWFILE.replace('\r', '').encode('utf-8'))

# --- MultiNationSim: 置き直しの前後
p = root + '/PriceOfBread.SimCore/MultiNationSim.cs'
raw = open(p, 'rb').read(); bom = raw.startswith(bytes([0xef, 0xbb, 0xbf])); s = raw.decode('utf-8-sig')
key = '        foreach (var n in Nations) n.World.RebaseOpeningMoney();'
assert s.count(key) == 1
i = s.index(key)
j = s.index('\n', i)
line = s[i:j]
crlf = line.endswith('\r')
NL = '\r\n' if '\r\n' in s else '\n'
if crlf: line = line[:-1]
new = '        if (Sav226.On) foreach (var n in Nations) Sav226.Rebase(this, n.World, 0);' + NL + line + NL + '        if (Sav226.On) foreach (var n in Nations) Sav226.Rebase(this, n.World, 1);'
s = s[:i] + new + (s[j - 1:] if crlf else s[j:])
open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))

edit('PriceOfBread.SimCore/SelfPlay.cs', [
    ("                sim.StepQuarter();\n",
     "                sim.StepQuarter();\n                if (Sav226.On) Sav226.Quarter(sim, seed, q, quarters);\n"),
])
edit('PriceOfBread.SimCore/EconomyStep.cs', [
    ("        // === 工場の投入支払い：同じ額を「投入費」と「投入財の売上」へ計上（相殺＝厳密保存）===\n",
     "        if (Sav226.On && world is not null) Sav226.Consume(world, region);\n        // === 工場の投入支払い：同じ額を「投入費」と「投入財の売上」へ計上（相殺＝厳密保存）===\n"),
    ("                    if (BudgetBindDiag.On) BudgetBindDiag.Row(world, staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2, !good.NonTradeable && !good.IsColonial, desired, demand, buyerPrice);\n",
     "                    if (BudgetBindDiag.On) BudgetBindDiag.Row(world, staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2, !good.NonTradeable && !good.IsColonial, desired, demand, buyerPrice);\n                    if (Sav226.On && world is not null) Sav226.Row(world, p, staple ? 0 : good.Tier == GoodTier.Daily ? 1 : 2, desired * buyerPrice, demand * buyerPrice);\n"),
    ("            region.Pops[s.LaborPopIndex].Earned += wages;\n",
     "            region.Pops[s.LaborPopIndex].Earned += wages;\n            if (Sav226.On && world is not null) Sav226.Prod(world, region.Pops[s.OwnerPopIndex], rent, region.Pops[s.LaborPopIndex], wages);\n"),
    ("            construction = Math.Min(desiredGrowth / Math.Max(Eps, coef.IndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)));",
     "            if (Sav226.On && world is not null) Sav226.Invest(world, desiredGrowth / Math.Max(Eps, coef.IndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)));\n            construction = Math.Min(desiredGrowth / Math.Max(Eps, coef.IndPerConstruction), budget / (buildCostPerUnit * (1.0 + coef.ConstructionLaborWageFrac)));"),
])
edit('PriceOfBread.SimCore/Fiscal.cs', [
    ("        double budgetBase = revenue + c.IndirectTaxInBudgetBase * indirectRevenue;\n",
     "        double budgetBase = revenue + c.IndirectTaxInBudgetBase * indirectRevenue;\n        if (Sav226.On) Sav226.Budget(w, revenue, indirectRevenue, leak, budgetBase);\n"),
    ("            titheCollected += tithe;\n            revenue += tax;\n",
     "            titheCollected += tithe;\n            if (Sav226.On) Sav226.Tax(w, p, claims, tax, tithe);\n            revenue += tax;\n"),
    ("        if (!(shortfall > 0) || !BorrowGateOpen(w, c)) return 0;",
     "        if (Sav226.On && shortfall > 0 && !BorrowGateOpen(w, c)) Sav226.Gate(w, shortfall);\n        if (!(shortfall > 0) || !BorrowGateOpen(w, c)) return 0;"),
    ("        double borrowed = Math.Min(borrowWant, pool);\n        if (!(borrowed > 1e-9)) return 0;",
     "        double borrowed = Math.Min(borrowWant, pool);\n        if (Sav226.On) Sav226.Borrow(w, shortfall, BorrowTicket(nominalGdp, c), domesticPool, foreignPool, borrowed);\n        if (!(borrowed > 1e-9)) return 0;"),
])
print('ok sav226', root)
