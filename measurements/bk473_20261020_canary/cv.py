import sys,collections,statistics as st
sys.stdout.reconfigure(encoding='utf-8')
exec(open('D:/PoB_work/bk473/an.py',encoding='utf-8').read().split("mode=sys.argv[1]")[0])
for fn in sys.argv[1:]:
    d=load(fn+'.tsv')
    for label,nm in (('WORLD',list(d)),('MAIN8',[n for n in MAIN if n in d])):
        cap=series(d,'capSum',nm); px=series(d,'dailyPx',nm,'med')
        print(fn,label,'q: cap20chg% / px20chg%')
        print(' '.join(f'{q}:{(cap[q]/cap[q-20]-1)*100:+.1f}/{(px[q]/px[q-20]-1)*100:+.1f}' for q in range(40,401,20)))
        ok=[(abs(cap[q]/cap[q-20]-1)<.02 and abs(px[q]/px[q-20]-1)<.05) for q in range(21,401)]
        runs=[];s=None
        for i,o in enumerate(ok):
            if o and s is None: s=i+21
            if not o and s is not None: runs.append((s,i+20)); s=None
        if s is not None: runs.append((s,400))
        print(' satisfied runs (q start-end):',[r for r in runs if r[1]-r[0]>=4][:12])
