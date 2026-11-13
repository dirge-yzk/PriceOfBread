# (1118) c108ps 突然変異を写しの木の Parliament.cs に当てる（バイトで読み書き・旧がちょうど 1 回ある時だけ）。呼び方：python mutapply.py <M1..M7> <木の根>
import sys, os
M = {
 "M1": [(b"bool gapless = c.ParliamentSeatsWithoutGap > 0;", b"bool gapless = false;")],
 "M2": [(b"&& !HasIdeology(w))\n            Ideologies.UpdateShares(w);", b"&& !HasIdeology(w) && false)\n            Ideologies.UpdateShares(w);")],
 "M3": [(b"&& !HasIdeology(w))\n            Ideologies.UpdateShares(w);", b"&& !HasIdeology(w) && false)\n            Ideologies.UpdateShares(w);"),
        (b"(gapless ? HasIdeology(w) : w.QuartersSinceElection >= 1)", b"(gapless ? true : w.QuartersSinceElection >= 1)")],
 "M4": [(b"SnapshotSeats(w);\n            w.GoverningParty = (int)Winner(w);\n        }\n        w.QuartersSinceElection++;", b"SnapshotSeats(w);\n        }\n        w.QuartersSinceElection++;")],
 "M5": [(b"if (c.NonElectiveSeatsEmpty > 0) { w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 0; }\n            else SnapshotSeats(w);", b"SnapshotSeats(w);")],
 "M6": [(b"if (gapless && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic\n            && w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2] < 1e-9 && !HasIdeology(w))", b"if (gapless && w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic)")],
 "M7": [(b"< 1e-9 && !HasIdeology(w))\n            Ideologies.UpdateShares(w);", b"< 1e-9)\n            Ideologies.UpdateShares(w);")],
}
m, root = sys.argv[1], sys.argv[2]
p = os.path.join(root, "PriceOfBread.SimCore", "Parliament.cs")
b = open(p, "rb").read()
for old, new in M[m]:
    n = b.count(old)
    if n != 1: sys.exit(f"MUTFAIL {m}: 旧が {n} 回")
    b = b.replace(old, new)
open(p, "wb").write(b)
print(f"MUTOK {m}")
