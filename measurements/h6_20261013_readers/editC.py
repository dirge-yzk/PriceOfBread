import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/world.json'
b = open(p, 'rb').read()
old = b'"landTaxStage": 2,\n   "govDebt": 750000,'
new = b'"landTaxStage": 1.275,\n   "govDebt": 750000,'
assert b.count(old) == 1
open(p, 'wb').write(b.replace(old, new))
