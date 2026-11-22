import sys
# usage: rep.py file  (reads pairs from a python file given as argv[2] defining PAIRS=[(old,new),...]) ; bytes, exact, unique
f=sys.argv[1]; ns={}
exec(open(sys.argv[2],'rb').read().decode('utf-8'),ns)
b=open(f,'rb').read()
crlf=b'\r\n' in b
for old,new in ns['PAIRS']:
    o=old.encode('utf-8'); n=new.encode('utf-8')
    if crlf: o=o.replace(b'\n',b'\r\n'); n=n.replace(b'\n',b'\r\n')
    c=b.count(o)
    if c!=1: sys.exit(f'count {c} for {old[:60]!r}')
    b=b.replace(o,n)
open(f,'wb').write(b)
print('ok',f)
