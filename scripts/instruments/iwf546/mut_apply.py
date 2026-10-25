import sys
p, old, new = sys.argv[1], sys.argv[2], sys.argv[3]
b = open(p, 'rb').read().decode('utf-8')
if b.count(old) != 1:
    print('anchor count', b.count(old)); sys.exit(1)
open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
