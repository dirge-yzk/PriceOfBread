import re,glob
t=''
for f in ['PriceOfBread.SimCore/Formations.cs','PriceOfBread.SimCore/CultureRealms.cs']:
    try: t+=open(f,encoding='utf-8').read()
    except Exception as e: print(f,e)
ms=re.findall(r'new FormationRule\("([^"]+)"[^;]*?Tier: *(\d+)',t,re.S)
from collections import Counter
print(len(ms)); c=Counter(n for n,_ in ms); print([k for k,v in c.items() if v>1]); print(ms[:40])
