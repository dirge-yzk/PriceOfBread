# (1158) c114qn 遠征の側の記録（colqn_s の末尾 6 列・読むだけ・門ではない）。腕ごとに 10 種の和と、在庫が尽きた遠征の割合・遠征 1 回あたりの残りの在庫と充足の上界。
# 使い方: python -X utf8 -I expread.py <ラベル=生のフォルダ> ...
import sys, os
sys.stdout.reconfigure(newline='\n')
COLS = ('expN', 'expDepletedN', 'expNeed', 'expReq', 'expFillUpper', 'expStockAfter')
for a in sys.argv[1:]:
    lab, d = a.split('=', 1)
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(d, f'colqn_s{s}.tsv'))]
    S = dict.fromkeys(COLS, 0.0); per = []
    for s in seeds:
        n = dep = 0.0
        with open(os.path.join(d, f'colqn_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t')
            if any(c not in h for c in COLS):
                raise SystemExit(f'{lab} 種 {s}：遠征の列が無い')
            ix = {c: h.index(c) for c in COLS}
            for l in f:
                x = l.rstrip('\n').split('\t')
                if len(x) < len(h):
                    continue
                for c in COLS:
                    S[c] += float(x[ix[c]])
                n += float(x[ix['expN']]); dep += float(x[ix['expDepletedN']])
        per.append(f'{s}:{int(dep)}/{int(n)}')
    N = S['expN']
    q = lambda v: f'{v / N:.4g}' if N > 0 else 'nan'
    print(f"{lab}\tseeds={len(seeds)}\texpN={int(N)}\texpDepletedN={int(S['expDepletedN'])}\tdepletedShare={q(S['expDepletedN'])}"
          f"\tfillUpperPerExp={q(S['expFillUpper'])}\tstockAfterPerExp={q(S['expStockAfter'])}\tneedPerExp={q(S['expNeed'])}\treqPerExp={q(S['expReq'])}\tper_seed(depleted/n)={' '.join(per)}")
