import sys, glob, os, collections
D = sys.argv[1]
cnt = collections.Counter(); ex = []
for f in sorted(glob.glob(os.path.join(D,'t*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    last = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] != 'E': continue
        reg = t[3]
        if t[2] == 'Seceded': last[reg] = (t[4], t[6])   # 割れる前の格・親
        if t[2] == 'Returned':
            pre = last.get(reg, ('?','?'))
            key = (f'割れる前 {pre[0]}', f'戻る直前 {t[4]}', f'戻った後 {t[5]}', f'1783の持ち主が受け手 {t[8]}', '親へ戻る' if pre[1]==t[7] else '別の国へ')
            cnt[key] += 1
            if pre[0] == 'Colonial' and t[5] != 'Colonial': ex.append((sd, reg, t[1], t[7], t[8]))
for k, v in sorted(cnt.items(), key=lambda x: -x[1]): print(v, k)
print('植民地格から本国の格へ化けた戻り（種・州・年・受け手・1783の持ち主が受け手）:')
for e in ex: print(e)
