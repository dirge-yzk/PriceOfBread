if (args.Length > 0 && args[0] == "listgoods")
{
    foreach (var d in Catalog.Goods) Console.WriteLine($"{d.Id}\t{d.Tier}\t{d.Name}\t{d.BaseNeed}\t{d.Inputs.Length}");
    return;
}
