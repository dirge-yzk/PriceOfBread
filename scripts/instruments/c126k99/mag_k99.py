# (1243) c126k99 史実の桁の線の読み手（読むだけ）：K99 反帝世論の削除が一番動かす量＝植民地を持つ国の本国の闘争度と蜂起（内戦）の数。
# 使い方: python -X utf8 -I mag_k99.py <ins の生（新）> [<ins の生（旧＝基線）>]
#   本国の闘争度＝secession_s の Core・Integrated の行の popK 加重の militancyMeanIndex（州の全ての POP の平均＝大衆だけではない【近似】）。
#   旧の規則の月の加算（反事実）＝0.006×（Colonial の行の popK 加重の闘争度）×min(1, Colonial の行の数÷3)【近似：(1148) freq.py と同じ式・係数は grep で写した値】。
#   蜂起の数＝wars_s の civilWarFlag=1 の行（sideA の国）。
#   門 H（史実の桁の線・この線が一番動かす量）：国ごとの〈種ごとの四半期の中央〉の種の中央の 新÷旧 が 10 以上か 0.1 以下なら破れ。旧を渡さないときは記録だけ。
import sys, os, statistics as st
sys.stdout.reconfigure(newline='\n')
COLZ = ['イギリス', 'フランス', 'ネーデルラント', 'スペイン', 'ポルトガル']
W_OLD = 0.006; FULL_COL = 3.0; TRACK = 0.30   # p06_rank_discovery.cs:78・p05_port_insurance.cs:19・Politics の追従の速さ（台帳 S26-23 の写し）

def fl(x):
    try: return float(x)
    except: return float('nan')

def read(D):
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(D, f'secession_s{s}.tsv'))]
    home = {n: [] for n in COLZ}; hp = {n: [] for n in COLZ}; cw = {n: 0 for n in COLZ}; cwall = 0
    for s in seeds:
        f = open(os.path.join(D, f'secession_s{s}.tsv'), encoding='utf-8')
        h = f.readline().rstrip('\n').split('\t')
        iq, it, inat, ipk, imil = h.index('quarter'), h.index('tier'), h.index('nation'), h.index('popK'), h.index('militancyMeanIndex')
        acc = {}
        for l in f:
            x = l.rstrip('\n').split('\t')
            n = x[inat]
            if n not in home: continue
            a = acc.setdefault((x[iq], n), [0.0, 0.0, 0.0, 0.0, 0])
            pk, m = fl(x[ipk]), fl(x[imil])
            if not (pk == pk and m == m): continue
            if x[it] == 'Colonial': a[2] += pk * m; a[3] += pk; a[4] += 1
            else: a[0] += pk * m; a[1] += pk
        per = {n: ([], []) for n in COLZ}
        for (q, n), a in acc.items():
            if a[1] > 1e-9: per[n][0].append(a[0] / a[1])
            if a[3] > 1e-9: per[n][1].append(W_OLD * (a[2] / a[3]) * min(1.0, a[4] / FULL_COL))
        for n in COLZ:
            if per[n][0]: home[n].append(st.median(per[n][0]))
            if per[n][1]: hp[n].append(st.median(per[n][1]))
        p = os.path.join(D, f'wars_s{s}.tsv')
        if os.path.exists(p):
            g = open(p, encoding='utf-8'); hh = g.readline().rstrip('\n').split('\t'); ic, ia = hh.index('civilWarFlag'), hh.index('sideA')
            for l in g:
                x = l.rstrip('\n').split('\t')
                if x[ic] == '1':
                    cwall += 1
                    if x[ia] in cw: cw[x[ia]] += 1
    return seeds, home, hp, cw, cwall

def med(v): return st.median(v) if v else float('nan')

A = read(sys.argv[1]); B = read(sys.argv[2]) if len(sys.argv) > 2 else None
print('# (1243) mag_k99 new=%s seeds=%s' % (sys.argv[1], ','.join(map(str, A[0]))) + (' old=%s seeds=%s' % (sys.argv[2], ','.join(map(str, B[0]))) if B else ''))
print('nation\thomeMilitancyNewIndex\thomeMilitancyOldIndex\tratioNewOld\toldRuleAddPerMonthIndex\toldRuleOffsetIndex\tcivilWarsNewCount\tcivilWarsOldCount\tgateH')
bad = 0; nread = 0
for n in COLZ:
    hn = med(A[1][n]); ho = med(B[1][n]) if B else float('nan'); r = hn / ho if (B and ho == ho and ho > 0) else float('nan')
    add = med(A[2][n]) if A[2][n] else float('nan')
    g = '-' if r != r else ('BREAK' if (r >= 10 or r <= 0.1) else 'ok')
    if g != '-': nread += 1
    if g == 'BREAK': bad += 1
    print('%s\t%.5g\t%.5g\t%.4g\t%.4g\t%.4g\t%d\t%s\t%s' % (n, hn, ho, r, add, add / TRACK if add == add else float('nan'), A[3][n], B[3][n] if B else '-', g))
print('civilWarsAll\tnew=%d\told=%s' % (A[4], B[4] if B else '-'))
print('GATE_H %s（読めた国 %d・破れ %d）' % ('-' if not B else ('BREAK' if bad else ('ok' if nread else 'NOREAD')), nread, bad))
