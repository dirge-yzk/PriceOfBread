# (1228) c124bk 突然変異 μ1〜μ5 を使い捨ての木の InternationalMarket.cs へ入れる（バイトで読み書き）。使い方: python -I mut_apply.py <木> <μ>
import sys
T, M = sys.argv[1], sys.argv[2]
p = T + '/PriceOfBread.SimCore/InternationalMarket.cs'
s = open(p, 'rb').read()
BOOK = b'{ pr.LastRevenue += amt; pr.LastCost += wages; }'
GATE = b'if (coef.PaySalesToProducerBook > 0 && wages >= 0)'
R = {
  'mu1': [(BOOK, b'{ pr.LastCost += wages; }')],
  'mu2': [(BOOK, b'{ pr.LastRevenue += amt; }')],
  'mu3': [(GATE, b'if (wages >= 0)')],
  'mu4': [(b'if (Sells(pr, rr, goodId)) totalOut += Math.Max(0, pr.Output);', b'if (Sells(pr, rr, goodId)) totalOut += 1.0;'),
          (b'double amt = amount * (Math.Max(0, pr.Output) / totalOut);', b'double amt = amount * (1.0 / totalOut);')],
  'mu5': [(GATE, b'if (coef.PaySalesToProducerBook > 0 && wages >= 0 && !minted)')],
}[M]
for a, b in R:
    n = s.count(a)
    if n != 1: print(f'{M}: 旧の文字列が {n} 回'); sys.exit(1)
    s = s.replace(a, b)
open(p, 'wb').write(s)
print(f'{M}: 入れた（{len(R)} 箇所）')
