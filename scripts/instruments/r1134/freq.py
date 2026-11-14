# (1134) S24 開戦と戦争と講和：行ごとの頻度を生の計器の表から数える（読むだけ・pandas なし）
# 使い方: python -X utf8 -I freq.py <生のフォルダ> <名> [<最初の種>] > out_freq_<名>.txt
# 計器が書かない表・列は n/a と書く（0 と数えない）。
import csv, sys, os, math
from collections import Counter, defaultdict
csv.field_size_limit(10**9)
D = sys.argv[1]; NAME = sys.argv[2]
S0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
SEEDS = list(range(S0, S0 + 10))
sys.stdout.reconfigure(newline='\n')

def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    f = open(p, encoding='utf-8', newline='')
    r = csv.reader(f, delimiter='\t')
    h = next(r)
    ix = {k: i for i, k in enumerate(h)}
    def gen():
        for row in r:
            if len(row) < len(h): continue
            yield row
    return ix, gen()

def fl(x):
    try:
        v = float(x)
        return v if math.isfinite(v) else None
    except Exception: return None

acc = defaultdict(dict)   # key -> {seed: value}
def add(k, s, v): acc[k][s] = v
def pc(a, b): return 100.0 * a / b if b else None

for s in SEEDS:
    # ---- wars（戦争ごと・終わりに書く・deadK は戦争の台帳＝等分の記帳）
    g = rows('wars', s)
    wars = []
    if g:
        ix, it = g
        for r in it:
            wars.append(dict(y=int(r[ix['startYear']]), civ=r[ix['civilWarFlag']] == '1', a=r[ix['sideA']], b=r[ix['sideB']],
                             m=fl(r[ix['months']]) or 0, dead=fl(r[ix['deadK']]) or 0, q=int(float(r[ix['q']])), code=r[ix['entryCode']]))
        n = len(wars); add('wars.n', s, n)
        cc = Counter(w['code'] for w in wars)
        for k in ['main', 'ally', 'suzerain', 'subjugate', 'mediation', 'civil', 'declare', 'ultimatum', 'calltoarms', 'secession', 'unknown']:
            add(f'wars.code.{k}', s, cc.get(k, 0))
        ms = sorted(w['m'] for w in wars)
        if ms:
            add('wars.months.med', s, ms[len(ms) // 2]); add('wars.months.p90', s, ms[int(0.9 * (len(ms) - 1))]); add('wars.months.max', s, ms[-1])
            add('wars.months.ge48.pct', s, pc(sum(1 for x in ms if x >= 48), n))
        add('wars.dead0.pct', s, pc(sum(1 for w in wars if w['dead'] <= 1e-9), n))
        add('wars.deadK.sum', s, sum(w['dead'] for w in wars))
        # 多正面：同じ国が同じ時に別の戦争にもいる戦争（開戦の四半期 q と月数から区間を作る＝四半期の粒の近似）
        iv = defaultdict(list)
        for i, w in enumerate(wars):
            lo = w['q']; hi = w['q'] + max(1, math.ceil(w['m'] / 3.0)) - 1
            iv[w['a']].append((lo, hi, i)); iv[w['b']].append((lo, hi, i))
        multi = set()
        for side, L in iv.items():
            for (lo, hi, i) in L:
                for (lo2, hi2, j) in L:
                    if i != j and lo <= hi2 and lo2 <= hi: multi.add(i); break
        add('wars.multifront.pct', s, pc(len(multi), n))
        add('wars.multifront.dead.pct', s, pc(sum(wars[i]['dead'] for i in multi), sum(w['dead'] for w in wars)))
        # 同じ年・同じ月数・同じ死者の組（観戦 N2／Y6 の型＝等分の記帳の跡）
        sig = Counter((w['q'], round(w['dead'], 6)) for w in wars if w['dead'] > 1e-9)
        add('wars.sameDeadSameQ.pct', s, pc(sum(v for v in sig.values() if v >= 2), sum(1 for w in wars if w['dead'] > 1e-9)))
    # ---- casualty（国×年・兵の戦死の実数）
    g = rows('casualty', s)
    if g:
        ix, it = g; tot = 0
        for r in it: tot += fl(r[ix['deadK']]) or 0
        add('cas.deadK.sum', s, tot)
        if wars: add('wars.over.cas', s, (sum(w['dead'] for w in wars) / tot) if tot > 0 else None)
    # ---- cpmed（講和ごと）
    g = rows('cpmed', s)
    if g:
        ix, it = g; n = 0; c = Counter()
        for r in it:
            n += 1
            civ = r[ix['civilFlag']] == '1'; forced = r[ix['forcedFlag']] == '1'; col = r[ix['collapsedIterFlag']] == '1'
            sc = fl(r[ix['scoreIndex']]) or 0; exA = fl(r[ix['exA']]) or 0; exB = fl(r[ix['exB']]) or 0
            c['civ'] += civ; c['forced'] += forced; c['collapsed'] += col; c['blank'] += r[ix['blankFlag']] == '1'
            c['decisive'] += abs(sc) >= 1.0
            c['weary'] += (exA > 0.9 and exB > 0.9)
            c['col_score0'] += col and abs(sc) < 1e-9
            c['rep'] += (fl(r[ix['repMovedIndex']]) or 0) > 1e-9
            c['capt'] += (fl(r[ix['capturedCount']]) or 0) > 0
            c['ceded'] += r[ix['cededFlag']] == '1'
            c['moved'] += (fl(r[ix['regionsMovedCount']]) or 0) > 0
        add('peace.n', s, n)
        for k in ['civ', 'forced', 'collapsed', 'blank', 'decisive', 'weary', 'col_score0', 'rep', 'capt', 'ceded', 'moved']:
            add(f'peace.{k}.pct', s, pc(c[k], n))
    # ---- warpair（組×四半期）
    g = rows('warpair', s)
    if g:
        ix, it = g; n = hi = hiNoStart = gate = st = sub = 0
        for r in it:
            n += 1
            ge = fl(r[ix['monthsGe140']]) or 0; sts = fl(r[ix['starts']]) or 0
            if ge > 0: hi += 1
            if ge >= 3 and sts == 0: hiNoStart += 1
            if (fl(r[ix['gateOpenMonths']]) or 0) > 0: gate += 1
            st += sts; sub += fl(r[ix['submits']]) or 0
        add('pair.rows', s, n); add('pair.ge140.rows', s, hi); add('pair.ge140full.noStart.rows', s, hiNoStart)
        add('pair.gateOpen.rows', s, gate); add('pair.starts', s, st); add('pair.submits', s, sub)
    # ---- warband（四半期の束）
    g = rows('warband', s)
    if g:
        ix, it = g; c = Counter()
        for r in it:
            for k in ix:
                if k.startswith('start_') or k in ('submits', 'peaceResets', 'secessionResets'):
                    c[k] += fl(r[ix[k]]) or 0
        for k, v in c.items(): add(f'band.{k}', s, v)
    # ---- invade（要求ごと）
    g = rows('invade', s)
    if g:
        ix, it = g; n = sb = tn = 0
        for r in it:
            n += 1; sb += r[ix['submitted']] == '1'; tn += r[ix['targetIsNation']] == '1'
        add('inv.n', s, n); add('inv.submitted.pct', s, pc(sb, n)); add('inv.targetNation.pct', s, pc(tn, n))
    # ---- warentry（国×四半期・CanInvade の理由）
    g = rows('warentry', s)
    if g:
        ix, it = g; c = Counter()
        keys = [k for k in ix if k.startswith('r_')] + ['calls', 'evals']
        for r in it:
            for k in keys: c[k] += fl(r[ix[k]]) or 0
        for k in keys: add(f'entry.{k}', s, c[k])
    # ---- imp（政体の押し付け）
    g = rows('imp', s)
    if g:
        ix, it = g; add('imp.n', s, sum(1 for _ in it))
    # ---- dep（従属を張った行・kindCode と呼び手）
    g = rows('dep', s)
    if g:
        ix, it = g; c = Counter()
        for r in it:
            if r[ix['kindCode']] == '2': continue
            c[r[ix['callerMember']]] += 1
        for k, v in c.items(): add(f'dep.caller.{k}', s, v)
    # ---- cederule（移った州・how）
    g = rows('cederule', s)
    if g:
        ix, it = g; c = Counter(r[ix['how']] for r in it)
        for k, v in c.items(): add(f'cede.{k}', s, v)
    # ---- rulepeace（講和ごと・州の取り分）
    g = rows('rulepeace', s)
    if g:
        ix, it = g; n = tk = 0
        for r in it:
            n += 1; tk += (fl(r[ix['takenCount']]) or 0) > 0
        add('rp.n', s, n); add('rp.taken.pct', s, pc(tk, n))
    # ---- cwmult（国×四半期・接触）
    g = rows('cwmult', s)
    if g:
        ix, it = g; fw = cw = z = 0
        for r in it:
            fw += fl(r[ix['foreignWarMonthsCount']]) or 0; cw += fl(r[ix['civilWarMonthsCount']]) or 0; z += fl(r[ix['zeroContactWarMonthsCount']]) or 0
        add('cw.warMonths', s, fw + cw); add('cw.zeroContact.pct', s, pc(z, fw + cw))
    # ---- scalars（国×四半期）
    g = rows('scalars', s)
    if g:
        ix, it = g; aw = ex9 = ex1 = ds = n = 0
        for r in it:
            n += 1
            if (fl(r[ix['defeatShock']]) or 0) > 1e-9: ds += 1
            if r[ix['atWar']] in ('1', 'True', 'true'):
                aw += 1; e = fl(r[ix['warExhaustion']]) or 0
                ex9 += e >= 0.9; ex1 += e >= 0.999
        add('sc.atWar.pct', s, pc(aw, n)); add('sc.ex09.atWar.pct', s, pc(ex9, aw)); add('sc.ex1.atWar.pct', s, pc(ex1, aw))
        add('sc.defeatShock.pct', s, pc(ds, n))
    # ---- monlaw（国×四半期・封鎖の危険と戦時配給法）
    g = rows('monlaw', s)
    if g:
        ix, it = g; n = br = ra = 0; brmax = 0
        for r in it:
            n += 1; b = fl(r[ix['blockadeRiskShare']]) or 0
            br += b > 1e-12; brmax = max(brmax, b)
            ra += (fl(r[ix['WartimeRationing']]) or 0) > 0
        add('ml.blockade>0.pct', s, pc(br, n)); add('ml.blockade.max', s, brmax); add('ml.rationing>0.pct', s, pc(ra, n))
    # ---- lwk（民兵の割合）
    g = rows('lwk', s)
    if g:
        ix, it = g; n = m = 0
        for r in it:
            n += 1; m += (fl(r[ix['militiaShare']]) or 0) > 1e-12
        add('lwk.militia>0.pct', s, pc(m, n))
    # ---- nation（国×四半期）
    g = rows('nation', s)
    if g:
        ix, it = g; n = aw = 0
        for r in it:
            n += 1; aw += (fl(r[ix['atWarMonths']]) or 0) > 0
        add('nat.atWarQ.pct', s, pc(aw, n))

def fmt(v):
    if v is None: return 'n/a'
    if isinstance(v, float): return f'{v:.4g}'
    return str(v)
print(f'# 生 {D}（{NAME}・種 {SEEDS[0]}〜{SEEDS[-1]}）')
print('key\tmin\tmed\tmax\tseeds\tvalues')
for k in sorted(acc):
    d = acc[k]; v = [d[s] for s in SEEDS if s in d and d[s] is not None]
    if not v: print(f'{k}\tn/a\tn/a\tn/a\t0'); continue
    vs = sorted(v)
    print(f'{k}\t{fmt(vs[0])}\t{fmt(vs[len(vs)//2])}\t{fmt(vs[-1])}\t{len(v)}\t' + ','.join(fmt(d.get(s)) for s in SEEDS))
