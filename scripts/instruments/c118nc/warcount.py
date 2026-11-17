"""(1186) c118nc：wars_s の行を種ごとに数える（外の戦争＝civilWarFlag 0・内戦＝entryCode civil・分離＝entryCode secession）。
止める線〈数の発散〉＝腕の 10 種の合計が基線の合計の 10 倍以上（事前登録 §4）。死者 deadK の和も出す（記録）。
使い方: python warcount.py <名>=<ins の生のフォルダ> ...  [--seeds 1-10]"""
import sys, os, collections
seeds = range(1, 11)
args = [a for a in sys.argv[1:] if not a.startswith('--')]
for a in sys.argv[1:]:
    if a.startswith('--seeds='):
        s0, s1 = a.split('=')[1].split('-'); seeds = range(int(s0), int(s1) + 1)
res = {}
for a in args:
    name, d = a.split('=', 1)
    tot = collections.Counter(); per = []
    for s in seeds:
        p = os.path.join(d, f'wars_s{s}.tsv')
        if not os.path.exists(p): per.append(None); continue
        c = collections.Counter()
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ic, ie, idk = h.index('civilWarFlag'), h.index('entryCode'), h.index('deadK')
            for line in f:
                x = line.rstrip('\n').split('\t')
                k = 'secession' if x[ie] == 'secession' else 'civil' if x[ic] == '1' else 'war'
                c[k] += 1
                try: c['deadK_' + k] += float(x[idk])
                except ValueError: pass
        per.append(c); tot.update(c)
    res[name] = tot
    print(f"{name}: war={tot['war']} civil={tot['civil']} secession={tot['secession']} deadK_war={tot['deadK_war']:.0f} deadK_civil={tot['deadK_civil']:.0f} deadK_sec={tot['deadK_secession']:.0f} 種ごとの分離={[c['secession'] if c else '-' for c in per]} 種ごとの内戦={[c['civil'] if c else '-' for c in per]}")
if len(res) >= 2:
    names = list(res); b = res[names[0]]
    for n in names[1:]:
        r = res[n]
        for k in ('war', 'civil', 'secession'):
            ratio = r[k] / b[k] if b[k] else float('inf')
            print(f"{n}/{names[0]} {k}: {r[k]}/{b[k]} = {ratio:.3f} {'STOP(>=10倍)' if ratio >= 10 else 'ok'}")
