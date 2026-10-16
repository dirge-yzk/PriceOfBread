import sys, re, os
arms = sys.argv[1:]
D = 'D:/PoB_work/bg'
data = {}
for a in arms:
    p = f'{D}/anq_{a}.txt'
    d = {}
    if os.path.exists(p):
        for ln in open(p, encoding='utf-8'):
            if ' = ' in ln and not ln.startswith('#'):
                k, v = ln.rstrip('\n').split(' = ', 1); d[k] = v
    data[a] = d
keys = []
for a in arms:
    for k in data[a]:
        if k not in keys and not k.startswith('R7') and 'H1s' not in k and '種の数' not in k: keys.append(k)
print('項目 | ' + ' | '.join(arms))
for k in keys:
    print(k + ' | ' + ' | '.join(data[a].get(k, '-')[:60] for a in arms))
