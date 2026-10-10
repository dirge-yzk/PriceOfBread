# usage: python ren.py <file> <old>=<new> ...   (word-boundary on right and left; keeps BOM and CRLF)
import re,sys
p=sys.argv[1]
b=open(p,'rb').read(); bom=b[:3]==b'\xef\xbb\xbf'
s=(b[3:] if bom else b).decode('utf-8')
for arg in sys.argv[2:]:
    a,c=arg.split('=',1)
    pat=r'(?<![A-Za-z0-9_])'+re.escape(a)+r'(?![A-Za-z0-9_])'
    n=len(re.findall(pat,s)); s=re.sub(pat,lambda m:c,s); print(n,a,'->',c)
out=(b'\xef\xbb\xbf' if bom else b'')+s.encode('utf-8')
open(p,'wb').write(out); print('bom',bom,'crlf',b.count(b'\r\n'),out.count(b'\r\n'))
