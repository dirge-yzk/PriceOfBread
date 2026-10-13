"""(294) 読むだけ：英の fiscal_s の borrowBinder の内訳（払えない額のある月 shortfall>0 の月だけ）と、兌換停止（onGold 1→0）の前 N か月の内訳。
python binder_split.py <ins> [前N=6] [四半期<=40]"""
import sys, os, glob, collections
D = sys.argv[1]; N = int(sys.argv[2]) if len(sys.argv) > 2 else 6; QMAX = int(sys.argv[3]) if len(sys.argv) > 3 else 40
tot = collections.Counter(); pre = collections.Counter(); n_susp = 0
for p in sorted(glob.glob(os.path.join(D, 'fiscal_s*.tsv')), key=lambda s: int(os.path.basename(s)[8:-4])):
    sd = int(os.path.basename(p)[8:-4])
    rows = []
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in fh:
            r = ln.rstrip('\n').split('\t')
            if r[ix['nation']] != 'イギリス': continue
            rows.append((int(r[ix['monthIndex']]), float(r[ix['borrowShortfallPerMonth']]), r[ix['borrowBinder']], float(r[ix['onGold']]), float(r[ix['interestToRevenueShareNow']])))
    rows.sort()
    rows = [r for r in rows if r[0] < QMAX * 3]
    sc = collections.Counter(r[2] for r in rows if r[1] > 0)
    tot.update(sc)
    prev = 1.0; sus = []
    for i, r in enumerate(rows):
        if prev == 1.0 and r[3] == 0.0: sus.append(i)
        prev = r[3]
    line = f'seed {sd}: 払えない額のある月の binder {dict(sc)} 停止{len(sus)}回'
    for i in sus[:1]:
        c = collections.Counter(rows[j][2] for j in range(max(0, i - N), i + 1) if rows[j][1] > 0)
        pre.update(c); n_susp += 1
        line += f'  最初の停止 月{rows[i][0]}(Q{rows[i][0]//3+1}) 直前{N+1}か月の払えない月の binder {dict(c)} 利払い÷歳入 {[round(rows[j][4],2) for j in range(max(0,i-N), i+1)]}'
    print(line)
print('全体(shortfall>0の月)', dict(tot)); print(f'最初の停止{n_susp}件の直前{N+1}か月', dict(pre))
