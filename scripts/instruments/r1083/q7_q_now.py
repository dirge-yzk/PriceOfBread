# (1083) 作者に聞く物 ② の材料：治める密度 q を四半期 0 に固定した今の O と、四半期ごとに q_t＝ΣL_t÷Σ人口_t で導いた O を並べる。
# 式の写し（名乗る）：InstrumentsRulePower.cs:157・:173〜175＝wUse＝min(W_A, L)・H＝min(L−wUse, 人口K×q)・O＝max(0, L−wUse−H)。人口K は国の全ての州の popK の和（:151 の SumPop と同じ母集合）。
# 自己検査：q 固定の再計算が表の oNationIndex と合うか（相対 1e-6）。使い方: python -X utf8 -I q7_q_now.py <表のフォルダ> > q7_out.txt
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QS = [0, 20, 40, 100, 200, 300, 400, 500, 600]
out = collections.defaultdict(lambda: collections.defaultdict(list)); bad = 0; chk = 0
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    nat = collections.defaultdict(dict); pop = collections.defaultdict(float)
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > max(QS): break
        if q not in QS: continue
        k = (q, c[ix['nation']]); pop[k] += float(c[ix['popK']])
        if not nat[q].get(c[ix['nation']]):
            nat[q][c[ix['nation']]] = (float(c[ix['lNationIndex']]), float(c[ix['wNationIndex']]), float(c[ix['oNationIndex']]), float(c[ix['qIndex']]))
    for q in QS:
        ns = nat[q]
        if not ns: continue
        sL = sum(v[0] for v in ns.values()); sP = sum(pop[(q, n)] for n in ns); q0 = next(iter(ns.values()))[3]; qt = sL / sP
        def O(n, qq):
            L, WA, _, _ = ns[n]; w = min(WA, L); H = min(max(0.0, L - w), pop[(q, n)] * qq); return max(0.0, L - w - H)
        of = {n: O(n, q0) for n in ns}; ot = {n: O(n, qt) for n in ns}
        for n in ns:
            chk += 1
            if abs(of[n] - ns[n][2]) > 1e-6 * max(1e-12, abs(ns[n][2])) + 1e-9: bad += 1
        so, st_ = sum(of.values()), sum(ot.values())
        r = out[q]
        r['qt÷q0'].append(qt / q0); r['ΣO÷ΣL 固定'].append(so / sL); r['ΣO÷ΣL q_t'].append(st_ / sL)
        r['O>0 の国 固定'].append(sum(1 for v in of.values() if v > 0)); r['O>0 の国 q_t'].append(sum(1 for v in ot.values() if v > 0))
        r['清の取り分 固定'].append(of.get('清', 0) / so if so > 0 else 0); r['清の取り分 q_t'].append(ot.get('清', 0) / st_ if st_ > 0 else 0)
print('自己検査：q 固定の再計算と表の O の不一致', bad, '/', chk)
cols = ['qt÷q0', 'ΣO÷ΣL 固定', 'ΣO÷ΣL q_t', 'O>0 の国 固定', 'O>0 の国 q_t', '清の取り分 固定', '清の取り分 q_t']
print('四半期\t' + '\t'.join(cols) + '（10 種の中央）')
for q in QS: print(f'{q}\t' + '\t'.join(f'{st.median(out[q][c]):.3g}' for c in cols))
