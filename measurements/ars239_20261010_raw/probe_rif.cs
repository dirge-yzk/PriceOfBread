if (args.Length > 0 && args[0] == "rifprobe")
{
    foreach (double rmonths in new[] { 0.0, 12.0, 60.0 })
    {
        var rsc = NationConfig.Britain().ToScenario(29);
        rsc.Coef.InitialArsenalMonths = rmonths;
        var rsim = new Simulation(rsc, new NullAgent());
        var rr0 = rsim.World.Region;
        var rmusket = rr0.Producers.First(p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        double rcum = 0;
        Console.WriteLine($"months {rmonths}: q0 stock {rr0.Markets[Catalog.RifleId].Stock:F1} price {rr0.Markets[Catalog.RifleId].Price:F3} target {rr0.Markets[Catalog.RifleId].TargetStock:F1}");
        for (int q = 0; q < 24; q++)
        {
            rsim.Run(1); rcum += rmusket.Output;
            if (q % 4 == 3) Console.WriteLine($"  q{q + 1}: stock {rr0.Markets[Catalog.RifleId].Stock:F1} price {rr0.Markets[Catalog.RifleId].Price:F3} cumOutput {rcum:F2} stateSpentArmy {rr0.StateSpent[(int)TreasuryCat.Army]:F0}");
        }
    }
    return;
}
