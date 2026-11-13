# 門 3 の変換：[System.Runtime.CompilerServices.CallerLineNumber] の属性を宣言から消す（既定値 0 が入る）。使い方：python strip_callerline.py <木>
import sys, os
root = sys.argv[1]
ATTR = '[System.Runtime.CompilerServices.CallerLineNumber] '
n = 0
for d, _, fs in os.walk(os.path.join(root, 'PriceOfBread.SimCore')):
    if '\obj' in d or '\bin' in d or '/obj' in d or '/bin' in d:
        continue
    for f in fs:
        if f.endswith('.cs'):
            p = os.path.join(d, f)
            b = open(p, 'rb').read()
            c = b.count(ATTR.encode())
            if c:
                open(p, 'wb').write(b.replace(ATTR.encode(), b''))
                n += c
print('stripped', n)
