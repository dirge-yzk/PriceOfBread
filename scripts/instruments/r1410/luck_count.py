# 戦争の運の数え：毎月の戦勝点の差 d から Hash01 の運 L を引き、押し p=(d-L)/0.10 を戻す（腕は回さない・生を読むだけ）。
# 使い方: python luck_count.py <base_ins のフォルダ> <出力 tsv の頭> [種の数]
# 国の Id：最初の 61 国は fsx の w1 の並び（MultiNationSim の Powers.Add の順）。それ以外（建国・標的）は、片側が分かる時だけ 0..599 から
#   〈|p|>1 の月が最も少ない Id〉を探す（外れの月が使える月の 1 割を超えるか、同点が 2 つ以上なら〈不明〉）。
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from luck_id import load, wars_by_seq, steps, luck, WP

def initial_ids(d, s):
    ix, rows = load(f'{d}/fsx_s{s}.tsv'); out = {}
    for r in rows:
        if r[ix['time']] != 'w1': break
        out.setdefault(r[ix['nation']], len(out))
    return out

def outliers(a, b, st):
    return sum(1 for k, y, dd, u, s in st if u and abs(dd - luck(a, b, y, k)) > WP * 1.0001)

def resolve(ids, A, B, st, nuse):
    ia, ib = ids.get(A), ids.get(B)
    if ia is not None and ib is not None: return ia, ib, 'init'
    if ia is None and ib is None: return None, None, 'both-unknown'
    known = ia if ia is not None else ib
    sc = sorted((outliers(known, x, st), x) for x in range(600) if x != known)
    if nuse >= 6 and sc[0][0] <= 0.1 * nuse and sc[1][0] > sc[0][0]:
        x = sc[0][1]; ids[A if ia is None else B] = x
        return (known, x, 'found') if ia is not None else (x, known, 'found')
    return None, None, 'ambiguous'

def lenbin(n):
    for lo, hi in ((1, 6), (7, 12), (13, 24), (25, 48), (49, 9999)):
        if lo <= n <= hi: return f'{lo}-{hi if hi < 9999 else ""}'

def endpath(ix, last):
    f = lambda c: last[ix[c]] == '1'
    if f('forcedFlag'): return 'mediation'
    if f('oldCollapseAFlag') or f('oldCollapseBFlag'): return 'collapse'
    if f('oldDecisiveFlag'): return 'decisive'
    if f('oldWearyFlag'): return 'weary'
    return 'other'

if __name__ == '__main__':
    d, outp = sys.argv[1], sys.argv[2]; ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
    W = []   # 1 戦争 1 行
    stat = collections.Counter()
    for s in range(1, ns + 1):
        ids = initial_ids(d, s)
        ix, rows = load(f'{d}/wexit_s{s}.tsv'); ws = wars_by_seq(ix, rows)
        for g, rs in ws.items():
            st = steps(ix, rs); nuse = sum(1 for x in st if x[3]); n = len(rs)
            last = rs[-1]; ep = endpath(ix, last)
            ended = int(last[ix['month']]) < 667 * 3 or ep != 'other'
            if nuse == 0:
                W.append((s, g[0], g[1], g[2], n, 0, 'nopush', ep, ended, 0, 0, 0, 0, 0, float(last[ix['scoreIndex']]), 0.0)); stat['nopush'] += 1; continue
            a, b, how = resolve(ids, g[0], g[1], st, nuse); stat[how] += 1
            if a is None:
                W.append((s, g[0], g[1], g[2], n, nuse, how, ep, ended, 0, 0, 0, 0, 0, float(last[ix['scoreIndex']]), 0.0)); continue
            flip = mag = out = 0; spush = sluck = 0.0
            for k, y, dd, u, sc in st:
                if not u: continue
                L = luck(a, b, y, k); push = dd - L
                if abs(push) > WP * 1.0001: out += 1; continue
                spush += push; sluck += L
                if push * dd < 0: flip += 1      # 運が、その月の得点の向きを押しの向きと逆にした
                if abs(L) > abs(push): mag += 1  # 運の大きさが押しより大きい月
            sf = float(last[ix['scoreIndex']])
            W.append((s, g[0], g[1], g[2], n, nuse, how, ep, ended, flip, mag, out, spush, sluck, sf, sf - sluck))
    with open(outp + '_wars.tsv', 'w', encoding='utf-8', newline='\n') as f:
        f.write('seed\tA\tB\tcivil\tmonths\tusable\tid\tend\tended\tflipMonths\tluckBiggerMonths\toutlierMonths\tsumPush\tsumLuck\tscoreEnd\tscoreEndMinusLuck\n')
        for w in W: f.write('\t'.join(str(x) for x in w) + '\n')
    print('ids', dict(stat))
