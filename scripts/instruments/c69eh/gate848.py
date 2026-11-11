# (848) c69eh 門 G2 の台本と全ての州の反実仮想の数（読むだけ）：cearn_s の末尾 8 列と cpop_s の末尾 2 列を読む。
#   呼び方：python -I scripts/instruments/c69eh/gate848.py <出力ファイル> <生のフォルダ（cearn_s{種}.tsv・cpop_s{種}.tsv）> [種の数=10]
#   事前登録 measurements/m848_20261105_prereg_eh.txt §4 G2 ①〜⑥。G1 は paper848.py（cpop_s の断面）。
#   全ての州の数（記録）：格ごとに emptyPopSavings（四半期の末の断面）÷州の在庫（savings が正の行）と、estateDue（その月に移るはずの額の四半期の和）・estateNoHeir。
import sys, os
from collections import defaultdict
out, d = sys.argv[1], sys.argv[2]; ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
L = []
def P(s=''): L.append(s); print(s, flush=True)
P(f'(848) c69eh 門 G2 と全ての州の反実仮想の数  {d}')
tot = defaultdict(float); viol = defaultdict(int); worst = defaultdict(float)
P('seed\trows\tmovedSum\tdueSum\tnoHeirSum\tv2_inOut\tv3_outLive\tv3_inEmpty\tv4_spread\tv5_overDue\tv6_cpop\tmaxSpread')
bytier = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0, 0, 0])   # 断面の空の在庫・断面の州の在庫・due・noHeir・due＞0 の組・組
for s in range(1, ns + 1):
    fn = os.path.join(d, f'cearn_s{s}.tsv')
    if not os.path.exists(fn): P(f'{s}\t(無い)'); continue
    v = defaultdict(int); mv = du = nh = 0.0; mx = 0.0; rows = 0
    with open(fn, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        need = ['estateMovedPerQuarter', 'estateInPerQuarter', 'estateDuePerQuarter', 'estateNoHeirPerQuarter', 'estateOutFromLivePerQuarter', 'estateInToEmptyPerQuarter', 'estateRecvPerCapitaSpread', 'emptyPopSavings']
        if any(c not in h for c in need): P(f'{s}\t(列が無い＝旧の木の生)'); continue
        ix = {c: h.index(c) for c in need + ['tier', 'savings', 'quarter', 'region']}
        for line in f:
            x = line.rstrip('\n').split('\t'); rows += 1
            g = lambda c: float(x[ix[c]])
            m, i, due, noh = g('estateMovedPerQuarter'), g('estateInPerQuarter'), g('estateDuePerQuarter'), g('estateNoHeirPerQuarter')
            mv += m; du += due; nh += noh
            if abs(i - m) > 1e-9 * max(1.0, m): v['2'] += 1
            if g('estateOutFromLivePerQuarter') > 1e-12: v['3a'] += 1
            if g('estateInToEmptyPerQuarter') > 1e-12: v['3b'] += 1
            sp = g('estateRecvPerCapitaSpread'); mx = max(mx, sp)
            if sp > 1e-9: v['4'] += 1
            if m > due * (1 + 1e-9) + 1e-12: v['5'] += 1
            t = bytier[x[ix['tier']]]; sv = g('savings')
            if sv > 0: t[0] += g('emptyPopSavings'); t[1] += sv
            t[2] += due; t[3] += noh; t[5] += 1
            if due > 0: t[4] += 1
    fp = os.path.join(d, f'cpop_s{s}.tsv')
    if os.path.exists(fp):
        agg = defaultdict(lambda: [0.0, 0.0])
        with open(fp, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t')
            if 'estateOutPerQuarter' in h:
                io, ii, iq, ir = h.index('estateOutPerQuarter'), h.index('estateInPerQuarter'), h.index('quarter'), h.index('region')
                for line in f:
                    x = line.rstrip('\n').split('\t'); a = agg[(x[iq], x[ir])]; a[0] += float(x[io]); a[1] += float(x[ii])
                for a in agg.values():
                    if abs(a[0] - a[1]) > 1e-9 * max(1.0, a[0]): v['6'] += 1
            else: v['6'] = -1
    P(f'{s}\t{rows}\t{mv:.6g}\t{du:.6g}\t{nh:.6g}\t{v["2"]}\t{v["3a"]}\t{v["3b"]}\t{v["4"]}\t{v["5"]}\t{v["6"]}\t{mx:.3g}')
    tot['moved'] += mv
    for k in ('2', '3a', '3b', '4', '5', '6'): viol[k] += max(0, v[k])
P(f'G2① movedSum(10 種)={tot["moved"]:.6g}（＞0 で通る）')
P('G2②〜⑥ 違反の組の数（0 で通る）：' + '  '.join(f'{k}={viol[k]}' for k in ('2', '3a', '3b', '4', '5', '6')))
P('判定 G2：' + ('通る' if tot['moved'] > 0 and all(viol[k] == 0 for k in ('2', '3a', '3b', '4', '5', '6')) else '落ちる'))
P('記録（全ての州・格ごと・10 種の和）：tier\t断面の空の在庫÷州の在庫\tdue の和\tnoHeir の和\tdue＞0 の組\t組')
for k, t in sorted(bytier.items()):
    P(f'  {k}\t{t[0]/t[1] if t[1] > 0 else float("nan"):.4g}\t{t[2]:.6g}\t{t[3]:.6g}\t{t[4]}\t{t[5]}')
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
