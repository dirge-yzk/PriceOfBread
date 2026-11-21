FILE='PriceOfBread.SimCore/FactionSnapshot.cs'
PAIRS=[("        return Math.Max(DiscontentOf(w, p, coord, lawGap), p.Militancy);\n","        return p.Militancy <= MilitancyThreshold ? 0.0 : 1.0;\n")]
FILTER='DiscontentReaderTests|PolicyPoliticsTests'
