# (1204) 被覆＝(貨幣＋国庫−貸出)÷貨幣（World.cs:2230）の低い行は国庫の負か貸出か。旗 0 の生の stock_s と money_s を読む。
import sys, os, glob
src, out = sys.argv[1], sys.argv[2]
L = [f'# src {src}']
n = neg = 0; trmin = 9e9; low = []
for f in sorted(glob.glob(os.path.join(src, 'stock_s*.tsv'))):
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            try:
                tm, tr = float(c[ix['totalMoneyStock']]), float(c[ix['treasuryStock']])
            except ValueError:
                continue
            if not (tm > 0) or tr != tr: continue
            n += 1; r = tr / tm
            if tr < 0: neg += 1
            if r < trmin: trmin = r
            if r < -0.5: low.append((os.path.basename(f), c[ix['nation']], c[ix['year']], round(r, 3)))
L.append(f'stock_s 行 {n} 国庫が負の行 {neg} 国庫÷貨幣の最小 {trmin:.4f} −0.5 未満の行 {len(low)}')
for x in low[:8]: L.append(f'  {x}')
mn = 9e9; lowc = 0; m = 0; lnmax = 0
for f in sorted(glob.glob(os.path.join(src, 'money_s*.tsv'))):
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            bm, base, ln = float(c[ix['broadMoneyStock']]), float(c[ix['baseMoneyStock']]), float(c[ix['outstandingLoansStock']])
            if not (bm > 0): continue
            m += 1; cv = base / bm; mn = min(mn, cv); lnmax = max(lnmax, ln / bm)
            if cv < 0.357: lowc += 1
L.append(f'money_s（主要国）行 {m} 被覆の最小 {mn:.4f} 0.357 未満 {lowc} 貸出÷貨幣の最大 {lnmax:.4f}')
with open(out, 'w', encoding='utf-8', newline='\n') as w: w.write('\n'.join(L) + '\n')
print('\n'.join(L))
