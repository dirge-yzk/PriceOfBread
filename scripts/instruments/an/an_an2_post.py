# (369) 後半 事後の探り（探索・事前登録の外・採否に使わない）: 古い錨が残る残りの国（born＝0・活きた州あり・anchorRegionsN＝0・anchorOld ≧ 5×popK）の r。
# H1 の主の定義は残りの国全体の中央値で、全体は他の型の国（もともと本国の州が無い国）に占められ A の中央が 1.36 に収まった（予測 P2 の〈A は 10 以上〉は外れ）。
# 〈古い錨が残る〉型だけを事後に切ると、(367) のポルトガル型の行が見える。事後の切り方なので採否には使わない。
# 使い方: PYTHONUTF8=1 python an_an2_post.py <作業フォルダ> <出力txt>
import csv, sys, statistics as st
W, OUT = sys.argv[1], sys.argv[2]
out = []
for arm in ('A', 'B', 'N1', 'N2'):
    stale = []; ns = set(); por = []
    for s in range(1, 11):
        with open(f'{W}/out_{arm}_10/ins/anchor_s{s}.tsv', encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for ln in r:
                b = int(ln[ix['born']]); pop = float(ln[ix['popK']]); old = float(ln[ix['anchorOld']])
                act = int(ln[ix['activeRegionsN']]); arn = int(ln[ix['anchorRegionsN']]); cap = float(ln[ix['capacity']])
                if act >= 1 and arn == 0 and cap > 0 and pop > 0 and b == 0:
                    if old >= 5 * pop:
                        stale.append(cap / pop); ns.add((s, ln[ix['nation']]))
                    if ln[ix['nation']] == 'ポルトガル':
                        por.append((cap / pop, pop))
    m = st.median(stale) if stale else None
    out.append(f"{arm}: 古い錨が残る行 {len(stale)}・国×種 {len(ns)}・r の中央 {m if m is None else round(m, 2)}・r＞3 の割合 {round(100*sum(1 for x in stale if x > 3)/len(stale), 1) if stale else '-'}%"
               + (f"／ポルトガルが残りの国の行 {len(por)}・r の中央 {round(st.median(p[0] for p in por), 2)}・人口の中央 {round(st.median(p[1] for p in por))}" if por else ''))
open(OUT, 'w', encoding='utf-8').write('\n'.join(out) + '\n'); print('\n'.join(out))
