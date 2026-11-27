# (1420) 使い捨ての突然変異：python -I mutate.py <1|2|3|4>（バイトで読み書き・戻すのは git checkout）
import sys
n = sys.argv[1]
BM = 'PriceOfBread.SimCore/BilateralMarket.cs'; IN = 'PriceOfBread.SimCore/Instruments/Instruments.cs'
def sub(p, old, new):
    b = open(p, 'rb').read().decode('utf-8'); assert b.count(old) == 1, (p, old); open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
LINE = 'if (Instruments.On) Instruments.DiscoveryContact(wb, nations[seller].World, r, g, value);'
if n == '1':
    sub(BM, '        foreach (int r in rows)\n            if (Discovery.Known(ks, r) && !Discovery.Known(kb, r))\n            {', '        foreach (int r in rows)\n        {\n            if (Instruments.On && Discovery.Known(ks, r)) Instruments.DiscoveryContact(wb, nations[seller].World, r, g, value);\n            if (Discovery.Known(ks, r) && !Discovery.Known(kb, r))\n            {')
    sub(BM, '                ' + LINE + '\n            }\n', '            }\n        }\n')
elif n == '2':
    sub(BM, 'if (Discovery.Known(ks, r) && !Discovery.Known(kb, r))', 'if (Discovery.Known(ks, r))')
elif n == '3':
    sub(BM, 'Instruments.DiscoveryContact(wb, nations[seller].World, r, g, value);', 'Instruments.DiscoveryContact(wb, nations.First(n0 => Instruments.NameOf(n0.World) == Instruments.NameOf(nations[seller].World)).World, r, g, value);')
elif n == '4':
    sub(IN, 'F(v), I(st.Uid), I(S(sellerW).Uid)));', 'F(v), I(S(sellerW).Uid), I(S(sellerW).Uid)));')
