# (1216) c122nr 紙の台本：world.json から開幕の f（主の文化の f と人口の平均の f）を国ごとに数える。使い方: python f_open.py <world.json>
import json,sys,collections
d=json.load(open(sys.argv[1],encoding='utf-8'))
cls=['nobility','peasant','worker','bourgeois','professional','merchant','artisan']
own=collections.defaultdict(lambda: collections.defaultdict(float))
for n in d['nations']:
    tot=sum(n.get(k,0) or 0 for k in cls)
    for p in n.get('provinces',[]):
        c=p.get('culture','')
        if c: own[n['name']][c]+=tot*p['share']
    for col in n.get('initialColonies',[]) or []:
        c=col.get('culture','')
        if c: own[n['name']][c]+=col.get('popK',0)
world=collections.defaultdict(float)
for nm,t in own.items():
    for c,v in t.items(): world[c]+=v
out=[]
for nm,t in own.items():
    if not t: continue
    main=max(t.items(),key=lambda kv:kv[1])[0]
    tot=sum(t.values())
    fm=1-t[main]/world[main]
    wavg=sum(v*((1-v/world[c]) if c==main else 1.0) for c,v in t.items())/tot
    out.append((nm,main,round(t[main]),round(world[main]),round(fm,3),round(wavg,3)))
out.sort(key=lambda r:-r[5])
print("nation\tmain\townMainK\tworldMainK\tf_main\tf_popmean")
for r in out: print("\t".join(map(str,r)))
