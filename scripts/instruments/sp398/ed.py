import sys
def edit(path, pairs):
    b = open(path, 'rb').read()
    crlf = b.count(b'\r\n')
    t = b.decode('utf-8')
    for old, new in pairs:
        n = t.count(old)
        assert n == 1, (path, old[:70], n)
        t = t.replace(old, new)
    open(path, 'wb').write(t.encode('utf-8'))
    print('edited', path, 'CRLF count before', crlf)
