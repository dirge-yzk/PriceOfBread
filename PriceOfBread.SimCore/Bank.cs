namespace PriceOfBread.SimCore;

public sealed class Bank
{
    public MoneyStock Gold;
    public MoneyStock Loans;
    public MoneyStock HouseholdLoans;
    public double Confidence = 1.0;
    public double Heat;

    internal static void Lend(World w, Pop p, double amount)
    {
        double moved = amount > 0
            ? World.Transfer((null, null), (w, [p], [1.0]), amount, outside: World.Outside.HouseholdCredit)
            : -World.Transfer((w, [p]), (null, null, null), -amount, outside: World.Outside.HouseholdCredit);
        p.Debt += new MoneyStock(moved);
    }

    internal void Carry(Bank? to, MoneyStock debt, MoneyStock? debtIn = null)
    {
        HouseholdLoans -= debt;
        if (to is not null) to.HouseholdLoans += debtIn ?? debt;
    }
}
