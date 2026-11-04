# (848) c69eh 段0 の紙の台本（読むだけ）：人の居ない POP（人数 ≦ 1e-12＝Model.cs の Blend の門と同じ）の在庫が、植民地格の州のお金に占める割合を cpop_s から数える。
#   呼び方：python scripts/instruments/c69eh/paper848.py <出力ファイル> <名:生のフォルダ> [<名:生のフォルダ> ...]
#   母集合 P＝植民地格の州×四半期（cpop_s の行のある組）のうち、州のお金 M＝Σmax(0,在庫) が正で、人数 ＞1e-12 の POP が 1 つ以上ある組。
#   E＝人数 ≦1e-12 で在庫が正の POP の在庫の和（旗 1 なら移るはずの額の、四半期の末の断面）。share＝E/M。W＝ΣE/ΣM（金額加重・種ごと）。
#   受け手の居ない組（人数 ＞1e-12 の POP が 0 で E＞0）は母集合の外＝別に数える（旗 1 でも移らない）。
#   資本家（popIndex 3＝Region.InvestorPopIndex＝Simulation.cs:549）が人数 ≦1e-12 で在庫が正の組＝EconomyStep.cs:1039 の投資の原資 0.05×在庫が旗 1 で消える組。
#   凍結の 15 件と対照（measurements/m829_20261104/read/events.tsv・controls.tsv）は入りの四半期の share（古い木の出来事の名簿＝この盤面では記録）。
import sys, os, statistics as st
from collections import defaultdict
EMPTY = 1e-12
def main():
    out = sys.argv[1]; sets = [a.split(':', 1) for a in sys.argv[2:]]
    root = os.path.dirname(os.path.abspath(__file__)); repo = os.path.abspath(os.path.join(root, '..', '..', '..'))
    ev = {}; ctl = defaultdict(list)
    with open(os.path.join(repo, 'measurements/m829_20261104/read/events.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            x = dict(zip(h, line.rstrip('\n').split('\t'))); ev[(int(x['seed']), x['region'])] = int(x['onsetQ'])
    with open(os.path.join(repo, 'measurements/m829_20261104/read/controls.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            x = dict(zip(h, line.rstrip('\n').split('\t'))); ctl[(int(x['seed']), x['event'])].append(x['control'])
    L = []
    def P(s=''): L.append(s); print(s, flush=True)
    P('(848) c69eh 段0 の紙：人の居ない POP の在庫÷植民地格の州のお金（cpop_s・四半期の末の断面・旗 0）')
    summary = {}
    for name, d in sets:
        P(f'== {name}  {d}')
        P('seed\tnP\tfracBelow0.01\tmedianShare\tW\tsumE\tsumM\tnNoHeir\tsumENoHeir\tnCapEmpty\tsumCapEmpty\tE_artisan\tE_merchant\tE_capitalist\tE_other\tWq<200\tWq>=500')
        allfrac = []; tot_n = 0; tot_below = 0; Ws = []
        evrows = []
        for s in range(1, 11):
            fn = os.path.join(d, f'cpop_s{s}.tsv')
            if not os.path.exists(fn): P(f'{s}\t(無い)'); continue
            agg = {}
            with open(fn, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t')
                iq, ir, ip, ic, ik, isv = h.index('quarter'), h.index('region'), h.index('popIndex'), h.index('className'), h.index('popK'), h.index('savings')
                for line in f:
                    x = line.split('\t')
                    key = (int(x[iq]), x[ir])
                    a = agg.get(key)
                    if a is None: a = agg[key] = [0.0, 0.0, False, defaultdict(float), 0.0]
                    k = float(x[ik]); sv = float(x[isv])
                    if sv > 0: a[1] += sv
                    if k > EMPTY: a[2] = True
                    elif sv > 0:
                        a[0] += sv; a[3][x[ic]] += sv
                        if x[ip] == '3': a[4] += sv
            n = below = 0; shares = []; sE = sM = 0.0; nNo = 0; sNo = 0.0; nCap = 0; sCap = 0.0
            cls = defaultdict(float); eq = [0.0, 0.0]; mq = [0.0, 0.0]; eL = [0.0, 0.0]; mL = [0.0, 0.0]
            for (q, r), a in agg.items():
                E, M, live = a[0], a[1], a[2]
                if not live:
                    if E > 0: nNo += 1; sNo += E
                    continue
                if not (M > 0): continue
                n += 1; sh = E / M; shares.append(sh)
                if sh < 0.01: below += 1
                sE += E; sM += M
                for c, v in a[3].items(): cls[c] += v
                if a[4] > 0: nCap += 1; sCap += a[4]
                if q < 200: eq[0] += E; mq[0] += M
                if q >= 500: eL[0] += E; mL[0] += M
            tot_n += n; tot_below += below; W = sE / sM if sM > 0 else float('nan'); Ws.append(W)
            oth = sum(v for c, v in cls.items() if c not in ('職人', '商人', '資本家'))
            P(f'{s}\t{n}\t{below/n if n else float("nan"):.4f}\t{st.median(shares) if shares else float("nan"):.4g}\t{W:.4g}\t{sE:.6g}\t{sM:.6g}\t{nNo}\t{sNo:.6g}\t{nCap}\t{sCap:.6g}\t{cls["職人"]:.4g}\t{cls["商人"]:.4g}\t{cls["資本家"]:.4g}\t{oth:.4g}\t{eq[0]/mq[0] if mq[0] else float("nan"):.4g}\t{eL[0]/mL[0] if mL[0] else float("nan"):.4g}')
            for (es, er), oq in ev.items():
                if es != s: continue
                a = agg.get((oq, er))
                sh = (a[0] / a[1]) if a and a[1] > 0 else float('nan')
                cs = []
                for cr in ctl.get((s, er), []):
                    b = agg.get((oq, cr))
                    if b and b[1] > 0 and b[2]: cs.append(b[0] / b[1])
                evrows.append((s, er, oq, sh, len(cs), st.median(cs) if cs else float('nan'), sum(1 for v in cs if v >= 0.5)))
        P(f'pooled\tnP={tot_n}\tfracBelow0.01={tot_below/tot_n if tot_n else float("nan"):.4f}\tW(seed median)={st.median(Ws) if Ws else float("nan"):.4g}\tW(seed min..max)={min(Ws):.4g}..{max(Ws):.4g}')
        P('event: seed\tregion\tonsetQ\tshareAtOnset\tnControls\tcontrolMedianShare\tcontrols>=0.5')
        for r in evrows: P('event: ' + '\t'.join(str(v) if not isinstance(v, float) else f'{v:.4g}' for v in r))
        summary[name] = (tot_n, tot_below, Ws)
    with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
main()
