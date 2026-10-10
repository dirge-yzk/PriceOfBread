# python sub.py <file> <pairsfile>   pairsfile: blocks '@@OLD' / '@@NEW' / '@@END' ; literal text (no escapes processed)
import sys
p = sys.argv[1]
raw = open(sys.argv[2], encoding='utf-8').read()
b = open(p, 'rb').read()
bom = b[:3] == b'\xef\xbb\xbf'
s = (b[3:] if bom else b).decode('utf-8')
crlf = '\r\n' in s
for blk in raw.split('@@OLD\n')[1:]:
    old, rest = blk.split('@@NEW\n', 1)
    new = rest.split('@@END', 1)[0]
    old = old[:-1]
    new = new[:-1]
    if crlf:
        old = old.replace('\n', '\r\n')
        new = new.replace('\n', '\r\n')
    n = s.count(old)
    assert n >= 1, (p, old[:120], n)
    s = s.replace(old, new)
    print(n, 'x', old[:70].replace('\r', '').replace('\n', '|'))
open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
