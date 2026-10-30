# (667) 線 igi4 の後半の判定の読み手（読むだけ）。事前登録 [3][4]＋追記 [11] の T2・P1・P2・P3・P4 を、腕の生から出す。
# 使い方：PYTHONUTF8=1 python judge.py <名前=計器の生のフォルダ[:種の起点]> ...   例：m655=D:/PoB_work/keep_655/m655_ins B=D:/PoB_work/igi4_667/out_B_10/ins B21=…/out_B21_10_s21/ins:21
#   種は起点から 10 本。最初に並べた名前が基線（「比べる側」）。最後に並べた物が腕の側ではない＝比べ方は引数 --pairs 腕=基線[,基線…] で指す（無ければ最初の名前が全ての腕の基線）。
#   例：--pair B=m655,N1,N2 --pair B21=A21
# 出力：[P1] 疫病の死 年合計／[P2] 縛られた日の割合・貸出÷GDP の最大／[P4] 扶養力÷基準の 30 年区切り・人口の倍率 1913/1783／[T2] 世界の工場の能力（eq_s scaleSum 年末の世界の和）の十年区切り・最小の基線に対する比
import sys, csv, statistics as st, collections
NATS = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float('nan')
def rd(d, name, s):
    f = open(f'{d}/{name}{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r); return f, r, h
def p1(d, S):
    out = []
    for s in S:
        t = 0.0
        f, r, h = rd(d, 'epi_s', s); i = h.index('deadK')
        for x in r: t += float(x[i])
        f.close(); out.append(t)
    return out
def p2(d, S):
    days = unb = 0.0; rows = urows = 0; mx_per_seed = []
    for s in S:
        f, r, h = rd(d, 'nation_s', s)
        j = h.index('creditJudgedDaysCount'); b = h.index('creditRepayableBoundDaysCount')
        for x in r:
            J = float(x[j]); B = float(x[b])
            if J <= 0: continue
            rows += 1; days += J; unb += J - B
            if J - B > 0: urows += 1
        f.close()
        mx = collections.defaultdict(float)
        f, r, h = rd(d, 'money_s', s); inn, il, ig = h.index('nation'), h.index('outstandingLoansStock'), h.index('gdpRealPerMonth')
        for x in r:
            if x[inn] not in NATS: continue
            try: g = float(x[ig]); l = float(x[il])
            except ValueError: continue
            if g > 0: mx[x[inn]] = max(mx[x[inn]], l / (12 * g))
        f.close(); mx_per_seed.append(med(list(mx.values())))
    return days, unb, rows, urows, mx_per_seed
def popmult(d, S):
    res = []
    for s in S:
        first = {}; last = {}
        f, r, h = rd(d, 'nation_s', s); iy, inn, ip = h.index('year'), h.index('nation'), h.index('populationK')
        for x in r:
            if x[inn] not in NATS: continue
            y = int(x[iy]); p = float(x[ip])
            if y == 1783 and x[inn] not in first: first[x[inn]] = p
            if y <= 1913: last[x[inn]] = p
        f.close(); res.append(med([last[n] / first[n] for n in first if n in last and first[n] > 0]))
    return res
def p4(d, S):
    C = collections.defaultdict(list); R = collections.defaultdict(list)
    for s in S:
        f, r, h = rd(d, 'anchor_s', s)
        for x in r:
            q = int(x[0]); y = int(x[1])
            if q % 4: continue
            cap = float(x[h.index('capacity')]); cb = float(x[h.index('capBase')]); pop = float(x[h.index('popK')])
            if cb <= 0 or cap <= 0: continue
            k = y // 30 * 30; C[k].append(cap / cb); R[k].append(pop / cap)
        f.close()
    return {k: (med(C[k]), med(R[k])) for k in C}
def t2(d, S):
    per = []
    for s in S:
        W = collections.defaultdict(float)
        f, r, h = rd(d, 'eq_s', s); iq = 0; iy = 1; isc = h.index('scaleSum')
        for x in r:
            if int(x[iq]) % 4 != 3: continue
            W[int(x[iy])] += float(x[isc])
        f.close()
        D = collections.defaultdict(list)
        for y, v in W.items(): D[y // 10 * 10].append(v)
        per.append({k: med(v) for k, v in D.items()})
    ks = sorted({k for p in per for k in p})
    return {k: med([p.get(k, float('nan')) for p in per]) for k in ks}
specs = []; pairs = {}
argv = sys.argv[1:]; i = 0
while i < len(argv):
    a = argv[i]
    if a == '--pair':
        k, v = argv[i + 1].split('=', 1); pairs[k] = v.split(','); i += 2; continue
    n, rest = a.split('=', 1)
    if rest.count(':') >= 2 or (rest.count(':') == 1 and not rest[1:2] == ':' ):
        d, st0 = rest.rsplit(':', 1); st0 = int(st0)
    else: d, st0 = rest, 1
    specs.append((n, d, list(range(st0, st0 + 10)))); i += 1
M = {}
for n, d, S in specs:
    E = p1(d, S); days, unb, rows, urows, mxs = p2(d, S); pm = popmult(d, S); P4 = p4(d, S); T2 = t2(d, S)
    M[n] = dict(E=E, days=days, unb=unb, rows=rows, urows=urows, mxs=mxs, pm=pm, P4=P4, T2=T2)
    print(f'## {n}  入力={d} 種={S[0]}..{S[-1]}')
    print(f'  [P1] 疫病 deadK 年合計 中央値={med(E):.0f} 範囲=[{min(E):.0f},{max(E):.0f}]')
    print(f'  [P2] 縛らない日の割合={unb/days:.5f}（縛られた日の割合={1-unb/days:.5f}）縛らない行={urows}/{rows}  貸出÷GDP最大(9国中央値の種中央値)={med(mxs):.3f} [{min(mxs):.3f},{max(mxs):.3f}]')
    print(f'  [P4] 人口の倍率 1913/1783(9国中央値の種中央値)={med(pm):.3f} [{min(pm):.3f},{max(pm):.3f}]')
    print('       扶養力÷基準 30年区切り：' + '  '.join(f'{k}:{P4[k][0]:.4f}' for k in sorted(P4)) + '   人口÷扶養力：' + '  '.join(f'{k}:{P4[k][1]:.3f}' for k in sorted(P4)))
    print('  [T2] 世界の scaleSum 十年（年末の世界の和・十年内の中央値の種中央値）：' + '  '.join(f'{k}:{v:.0f}' for k, v in sorted(T2.items())))
print()
for arm, refs in pairs.items():
    if arm not in M: continue
    a = M[arm]; rs = [r for r in refs if r in M]
    print(f'## 判定 {arm} 対 {",".join(rs)}')
    # P4 の符号：基線ごと（最初の基線が主）。6 区切りのうち腕が小さい数
    for r in rs:
        b = M[r]; ks = sorted(set(a['P4']) & set(b['P4']))
        neg = sum(1 for k in ks if a['P4'][k][0] < b['P4'][k][0])
        rel = ' '.join(f"{k}:{(a['P4'][k][0]/b['P4'][k][0]-1)*100:+.1f}%" for k in ks)
        print(f'  [P4符号] 対{r}：扶養力÷基準が小さい区切り={neg}/{len(ks)}  差(腕/基線-1)：{rel}')
    pred = {1770: -0.022, 1800: -0.079, 1830: -0.144, 1860: -0.177, 1890: -0.124, 1920: -0.069}
    b = M[rs[0]]
    inband = []
    for k in sorted(set(a['P4']) & set(b['P4'])):
        dlt = a['P4'][k][0] / b['P4'][k][0] - 1
        pr = pred.get(k)
        if pr is not None: inband.append((k, dlt / pr))
    print('  [P4大きさ] 実測÷予想（0.5〜1.5 が枠・対' + rs[0] + '）：' + '  '.join(f'{k}:{v:.2f}' for k, v in inband))
    for r in rs:
        b = M[r]; print(f'  [P4人口の倍率] 腕={med(a["pm"]):.3f} 対{r}={med(b["pm"]):.3f}（予言 1.46〜1.55・基線 1.721）')
    for r in rs:
        b = M[r]; print(f'  [P1] 疫病の死 腕/基線={med(a["E"])/med(b["E"]):.3f}（枠 0.75〜1.25）対{r}')
    for r in rs:
        b = M[r]; print(f'  [P2] 縛られた日の割合 腕={1-a["unb"]/a["days"]:.5f} 対{r}={1-b["unb"]/b["days"]:.5f}（予言 0.999 以上）  貸出÷GDP最大 腕={med(a["mxs"]):.3f} 対{r}={med(b["mxs"]):.3f}')
    # T2：最小の基線に対する比
    ks = sorted(a['T2']); worst = None; row = []
    for k in ks:
        mn = min(M[r]['T2'].get(k, float('inf')) for r in rs)
        if mn in (0, float('inf')): continue
        q = a['T2'][k] / mn; row.append((k, q)); worst = q if worst is None else min(worst, q)
    print('  [T2] 腕÷最小の基線（0.5 以上が条件）：' + '  '.join(f'{k}:{q:.2f}' for k, q in row) + f'   最悪={worst:.2f}' if worst is not None else '  [T2] 比べられない')
