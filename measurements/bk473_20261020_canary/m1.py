import sys
sys.stdout.reconfigure(encoding='utf-8')
exec(open('D:/PoB_work/bk473/an.py',encoding='utf-8').read().split("mode=sys.argv[1]")[0])
for fn in ('B1','C1','D2'):
    d=load(fn+'.tsv')
    nm=[n for n in MAIN if n in d]
    cap=series(d,'capSum',nm); px=series(d,'dailyPx',nm,'med')
    ok={q:(abs(cap[q]/cap[q-20]-1)<.05 and abs(px[q]/px[q-20]-1)<.10) for q in range(21,401)}
    first=None
    for q in range(21,361):
        if all(ok[k] for k in range(q,q+41)): first=q; break
    print(fn,'M1prime (±5%/±10%, 41 consecutive quarters) first q =',first)
    for q in (20,120,200):
        print('  worker share',q,[f"{n}:{d[n][q]['workerShare']*100:.1f}" for n in nm])
