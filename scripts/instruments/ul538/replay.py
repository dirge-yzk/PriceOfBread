import csv, sys, collections, statistics, math
# 回す前の必要性の確認（読み捨て）：基線 m533_ins の engineer_s の T 行（8 四半期ごと・38 行×全ての国）と E 行（開き）を使い、
# 伝わりの率の形（世界の行ごとの積み上げ T_r）を粗く再生する。接触は 0（保守的）・tech/need は census の値を次の census まで保つ。
def load(seed, base):
    T = collections.defaultdict(lambda: collections.defaultdict(list))  # row -> nation -> [(q,tech,need,known)]
    E = {}  # (row,nation) -> q
    with open(f'{base}/engineer_s{seed}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); next(r)
        for x in r:
            q = int(x[1]); n = x[3]; row = x[4]
            if x[0] == 'T':
                tech = float(x[7]); need = float(x[9]); known = x[8] == '1'
                T[row][n].append((q, tech, need, known, float(x[6])))
            elif x[0] == 'E':
                E[(row, n)] = q
    return T, E

def pct(a, p):
    a = sorted(a); k = (len(a) - 1) * p; lo = int(math.floor(k)); hi = min(lo + 1, len(a) - 1)
    return a[lo] + (a[hi] - a[lo]) * (k - lo)

def replay(T, E, rate, nq=668, contact=0.0, mode='int'):
    out = []
    for row, nd in T.items():
        # 稼働中の国＝census に出る国（最初の census 以後）
        info = {}
        for n, lst in nd.items():
            lst.sort()
            info[n] = lst
        base_open = {n: E[(row, n)] for n in info if (row, n) in E}
        # census で known の国は、E が無い＝開幕で既知（q=0 から持つ）
        held0 = set(n for n, lst in info.items() if lst[0][3] and n not in base_open)
        t_of = {}
        first = {n: lst[0][0] for n, lst in info.items()}
        cur = {n: 0 for n in info}
        holders = {n: -1 for n in held0}
        spillT = {n: 0.0 for n in info}
        sim_open = {}
        for q in range(nq):
            # 今の断面
            members = [n for n in info if first[n] <= q or n in holders]
            M = len(members)
            K = sum(1 for n in members if n in holders)
            newly = []
            for n in members:
                if n in holders: continue
                lst = info[n]
                i = cur[n]
                while i + 1 < len(lst) and lst[i + 1][0] <= q: i += 1
                cur[n] = i
                _, tech, need, kn, _a = lst[i]
                if n in base_open and base_open[n] <= q:
                    newly.append((n, 'nat')); continue
                if kn: continue
                p = K / (M - 1) if M > 1 else 0.0
                score = tech + need + contact + spillT[n]
                if mode == 'inst': score = tech + need + contact + 100.0 * p
                if score >= 100.0: newly.append((n, 'spill' if tech + need + contact < 100.0 else 'self'))
            for n, k in newly:
                holders[n] = q; sim_open[n] = (q, k)
            # 積み上げ（次の四半期へ）
            if mode == 'int':
                M2 = len([n for n in info if first[n] <= q or n in holders]); K2 = len(holders)
                p2 = K2 / (M2 - 1) if M2 > 1 else 0.0
                for n in info:
                    if n not in holders and first[n] <= q: spillT[n] += rate * 100.0 * p2
        out.append((row, base_open, sim_open, held0, len(info)))
    return out

def summarize(res, label):
    widths = []; conc = []; early4 = 0; early8 = 0; resc = 0; spl = 0; bigq = 0; h1 = 0; h1b = 0; nsim = 0; nbase = 0; spill_only = 0; adv = 0; hold = 0
    for row, bo, so, held0, nm in res:
        qs = sorted(q for n, (q, k) in so.items())
        nsim += len(qs); nbase += len(bo)
        spill_only += sum(1 for n, (q, k) in so.items() if k == 'spill')
        adv += sum(1 for n, (q, k) in so.items() if k == 'spill' and (n in bo and bo[n] > q)); resc += sum(1 for n, (q, k) in so.items() if k == 'spill' and n not in bo)
        if qs:
            q0 = qs[0]
            for n, (q, k) in so.items():
                if k == 'spill':
                    spl += 1; early4 += (q - q0 <= 4); early8 += (q - q0 <= 8)
            c2 = collections.Counter(qs); bigq += sum(1 for v in c2.values() if v >= 8)
        if len(qs) >= 7:
            fol = qs[1:]
            if len(fol) >= 6:
                widths.append(pct(fol, .9) - pct(fol, .1))
                c = collections.Counter(fol); conc.append(max(c.values()) / len(fol))
    wm = statistics.median(widths) if widths else float('nan')
    cm = statistics.median(conc) if conc else float('nan')
    return dict(resc=resc, early4=early4, early8=early8, spl=spl, bigq=bigq, label=label, rows=len(widths), width_med=wm, conc_med=cm, sim=nsim, base=nbase, spill=spill_only, adv=adv)

if __name__ == '__main__' and sys.argv[1] != 'report':
    base = sys.argv[1]; seeds = [int(x) for x in sys.argv[2].split(',')]
    for label, rate, mode in [('base', 0.0, 'int'), ('inst', 0.0, 'inst'), ('r1/40', 1 / 40, 'int'), ('r1/80', 1 / 80, 'int')]:
        agg = collections.defaultdict(list)
        tot = collections.Counter()
        for s in seeds:
            T, E = load(s, base)
            r = summarize(replay(T, E, rate, mode=mode), label)
            agg['w'].append(r['width_med']); agg['c'].append(r['conc_med'])
            for k in ('sim', 'base', 'spill', 'adv', 'early4', 'early8', 'spl', 'bigq', 'resc'): tot[k] += r[k]
        print(label, 'width_med(per seed)=', [round(x, 1) for x in agg['w']], 'conc=', [round(x, 2) for x in agg['c']], dict(tot), flush=True)


# ---- 報告用（ul3_538 の事前登録の数字を出す）：python replay.py report <基線の計器の生> <種> ----
def _rank(a):
    idx = sorted(range(len(a)), key=lambda i: a[i]); r = [0.0] * len(a); i = 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and a[idx[j + 1]] == a[idx[i]]: j += 1
        for k in range(i, j + 1): r[idx[k]] = (i + j) / 2
        i = j + 1
    return r

def _spearman(x, y):
    if len(x) < 5: return None
    rx, ry = _rank(x), _rank(y); mx = sum(rx) / len(rx); my = sum(ry) / len(ry)
    n = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); d = (sum((a - mx) ** 2 for a in rx) * sum((b - my) ** 2 for b in ry)) ** .5
    return n / d if d else None

def report(base, seeds):
    for label, rate, mode in [('基線（広がりなし）', 0.0, 'int'), ('瞬時の形（(510) と同じ・参考）', 0.0, 'inst'), ('率の形 1/80（2.5÷2 点/四半期）', 1 / 80, 'int'), ('率の形 1/40（2.5 点/四半期・登録値）', 1 / 40, 'int')]:
        tot = collections.Counter(); widths = []; concs = []; lags = []; rhos = []
        for s in seeds:
            T, E = load(s, base)
            res = replay(T, E, rate, mode=mode)
            r = summarize(res, label)
            widths.append(r['width_med']); concs.append(r['conc_med'])
            for k in ('sim', 'base', 'spill', 'adv', 'resc', 'early4', 'bigq'): tot[k] += r[k]
            for row, bo, so, held0, nm in res:
                qs = sorted(q for n, (q, k) in so.items())
                if not qs: continue
                lags += [q - qs[0] for n, (q, k) in so.items() if k == 'spill']
                xs = []; ys = []
                for n, (q, k) in so.items():
                    if k != 'spill': continue
                    lst = [t for t in T[row][n] if t[0] <= q]
                    if lst: xs.append(100 - lst[-1][1] - lst[-1][2]); ys.append(q)
                rho = _spearman(xs, ys)
                if rho is not None: rhos.append(rho)
        lags.sort()
        print(f'{label}\n  幅の中央値（種ごと）{[round(x, 1) for x in widths]}・一斉度の中央値（種ごと）{[round(x, 2) for x in concs]}')
        print(f'  開いた数 {tot["sim"]}（基線 {tot["base"]}）・広がりが要った {tot["spill"]}（{tot["spill"] / max(1, tot["sim"]):.3f}）＝前倒し {tot["adv"]}・救済 {tot["resc"]}・1 四半期に 8 国以上開く（行,四半期）{tot["bigq"]}')
        if lags: print(f'  広がりが要った開きの最初の開きからの遅れ 中央値 {statistics.median(lags)}・p10 {lags[len(lags) // 10]}・4 四半期以内 {sum(1 for x in lags if x <= 4) / len(lags):.4f}・欠けの順位相関 中央値 {statistics.median(rhos) if rhos else None}')

if __name__ == '__main__' and len(sys.argv) > 1 and sys.argv[1] == 'report':
    report(sys.argv[2], [int(x) for x in sys.argv[3].split(',')])
    sys.exit(0)
