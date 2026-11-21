FILE='PriceOfBread.SimCore/World.cs'
PAIRS=[("        double budget = half / (1.0 + half / demand);","        double budget = Math.Min(demand, half);")]
FILTER='RevoltIgnitionTests'
