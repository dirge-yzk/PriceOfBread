# (886) の自己試験の突然変異：mut_a は Factions.cs の末尾に注を 1 行、mut_b は CivilWarEconomyDisruption の既定 0.2→0.25（バイトで読み書き・LF を保つ）
import sys, os
w = sys.argv[1]
p = os.path.join(w, 'mut_a', 'PriceOfBread.SimCore', 'Factions.cs')
b = open(p, 'rb').read(); assert b.count(b'\r\n') == 0
open(p, 'wb').write(b + b'// r886 mut_a\n')
p = os.path.join(w, 'mut_b', 'PriceOfBread.SimCore', 'Coefficients.cs')
b = open(p, 'rb').read()
a = b'public double CivilWarEconomyDisruption = 0.2;'
assert b.count(a) == 1
open(p, 'wb').write(b.replace(a, b'public double CivilWarEconomyDisruption = 0.25;'))
print('mutated')
