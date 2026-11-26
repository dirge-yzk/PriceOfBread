import sys,subprocess
f,o,n=sys.argv[1:4]
s=open(f,'rb').read().decode(); assert s.count(o)==1; open(f,'wb').write(s.replace(o,n).encode())
