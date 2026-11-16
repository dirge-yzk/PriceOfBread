import sys
p, old, new = sys.argv[1], sys.argv[2].encode('utf-8'), sys.argv[3].encode('utf-8')
b = open(p, 'rb').read()
assert b.count(old) == 1, (b.count(old), old)
open(p, 'wb').write(b.replace(old, new))
