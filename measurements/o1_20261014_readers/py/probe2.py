import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]; NAT = sys.argv[2] if len(sys.argv)>2 else 'イギリス'
def table(p):
    with open(p, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))
def fl(x):
    try: return float(x)
    except: return float('nan')
win = [(0,12),(12,24),(24,36),(36,60),(60,120),(120,480),(480,2100)]
cnt = collections.Counter(); closed = collections.Counter(); binder = collections.Counter()
for sd in range(1, 11):
    for r in table(os.path.join(D, f'fiscal_s{sd}.tsv')):
        if r['nation'] != NAT: continue
        m = int(r['monthIndex'])
        for lo,hi in win:
            if lo <= m < hi:
                cnt[(lo,hi)] += 1
                if r['borrowGateOpenFlag'] == '0': closed[(lo,hi)] += 1
                if r['borrowBinder'] == 'gateClosed': binder[(lo,hi)] += 1
for w in win:
    print(w, '月数', cnt[w], '門閉(旗0)', closed[w], f'{closed[w]/max(1,cnt[w]):.3f}', 'binder=gateClosed', binder[w])
