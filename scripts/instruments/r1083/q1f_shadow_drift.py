# (1083) 問い 5 の補い：影の最大の区分の行の数と世界の陸軍力の和 ΣL の動き（q と τ は四半期 0 で固定＝L が落ちると F_S だけが落ち、F_I・F_U は固定のまま）。
# 使い方: python -X utf8 -I q1f_shadow_drift.py <表のフォルダ> > q1f_out.txt
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QS = [0, 4, 8, 20, 40, 100, 200, 300, 400, 500, 600]
cnt = collections.defaultdict(lambda: collections.defaultdict(list)); sl = collections.defaultdict(list); cov = collections.defaultdict(list)
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    cc = collections.defaultdict(collections.Counter); tl = collections.defaultdict(float); seen = set(); cv = collections.defaultdict(list)
    popn = collections.defaultdict(float); Ln = {}
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > max(QS): break
        if q not in QS: continue
        cc[q][c[ix['bestClass']]] += 1; cc[q]['行'] += 1; cc[q]['今の最大が持ち主でない'] += c[ix['nowBestClass']] != 'owner'
        k = (q, c[ix['nation']]); popn[k] += float(c[ix['popK']])
        if k not in seen: seen.add(k); L = float(c[ix['lNationIndex']]); tl[q] += L; Ln[k] = (L, float(c[ix['qIndex']]))
    for k, (L, qq) in Ln.items():
        n = popn[k] * qq
        if n > 0: cv[k[0]].append(L / n)
    for q in QS:
        for b in ('行', 'owner', 'local', 'foreign', 'ungov', '今の最大が持ち主でない'): cnt[q][b].append(cc[q][b])
        sl[q].append(tl[q]); cov[q].append(sum(1 for v in cv[q] if v >= 1))
print('四半期\t行\t影 owner\tlocal\tforeign\tungov\t今の最大が持ち主でない\tΣL\t被覆 L÷ΣN≧1 の国（種の中央・10 種の中央）')
for q in QS:
    m = lambda b: st.median(cnt[q][b])
    print(f'{q}\t{m("行"):.0f}\t{m("owner"):.0f}\t{m("local"):.0f}\t{m("foreign"):.0f}\t{m("ungov"):.0f}\t{m("今の最大が持ち主でない"):.0f}\t{st.median(sl[q]):.3g}\t{st.median(cov[q]):.0f}')
