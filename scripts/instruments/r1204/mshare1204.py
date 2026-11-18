# (1204) 国ごとの造幣の価値（買い×平価 0.54・mintregの parity 列で確かめた）の累計÷その国の貨幣（stock_s totalMoneyStock）。
# 旗 1 で造幣が 0.079 倍になると被覆（(貨幣＋国庫−貸出)÷貨幣）が崩れうる国を紙で選ぶ。旗 0 の生だけ。
import sys, os, glob
src, out = sys.argv[1], sys.argv[2]
PAR = 0.54
L = [f'# src {src}']
res = []
for f in sorted(glob.glob(os.path.join(src, 'mintcap_s*.tsv'))):
    seed = f.rsplit('_s', 1)[1].split('.')[0]
    cum = {}; capsh = {}; loansMax = {}
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t'); n = c[ix['nation']]
            b = sum(float(c[ix[k]]) for k in ('boughtCapAg', 'boughtCapAu', 'boughtOtherAg', 'boughtOtherAu'))
            bc = float(c[ix['boughtCapAg']]) + float(c[ix['boughtCapAu']])
            cum[n] = cum.get(n, 0.0) + b * PAR; capsh[n] = capsh.get(n, 0.0) + bc * PAR
    money = {}
    with open(os.path.join(src, f'stock_s{seed}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            try: tm = float(c[ix['totalMoneyStock']])
            except ValueError: continue
            if tm == tm and tm > 0: money[c[ix['nation']]] = max(money.get(c[ix['nation']], 0.0), tm)
    for n, v in cum.items():
        if n in money and money[n] > 0: res.append((v / money[n], (v - capsh[n]) / money[n], seed, n))
res.sort(reverse=True)
L.append(f'国×種 {len(res)}・造幣の累計÷貨幣の最大値（stock_s の最大）')
for k in (0.5, 0.2, 0.1):
    L.append(f'  首都の外の造幣の累計÷貨幣 ≥ {k}: {sum(1 for r in res if r[1] >= k)}')
for r in res[:15]: L.append(f'  {r[3]} s{r[2]} 造幣÷貨幣 {r[0]:.3f} 首都の外の分 {r[1]:.3f}')
with open(out, 'w', encoding='utf-8', newline='\n') as w: w.write('\n'.join(L) + '\n')
print('\n'.join(L))
