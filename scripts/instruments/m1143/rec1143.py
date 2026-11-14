# (1143) 合流の記録（採否に使わない）：効きの足し算と線の反証役が挙げた副作用を、腕ごとに同じ台本で数える。
#   python -X utf8 -I rec1143.py <ラベル>=<ins のフォルダ> [...]（種 1〜10・表が無い腕は〈無い〉と書く）
#   rb：世界の線路の量 1900 年（trrail_s の railTrackEnd の和・種の平均）・needUsedShareEnd の前年比 |ln| の中央（G-W と同じ量）
#   tp：engine の旗の国×四半期の割合（tport_s・英・仏・清・全て）
#   lo：pre の機関車の払いの和（railspend_s の発見の前の行・gates_lo.py の armLocoPre と同じ定義は読まず、列の和だけ）は gates_lo.py の出力で読む＝ここでは数えない
import sys, os, math, statistics

def fl(s):
    try: return float(s)
    except ValueError: return float('nan')

def rail(ins):
    tot = []; lnw = []
    for s in range(1, 11):
        p = os.path.join(ins, f'trrail_s{s}.tsv')
        if not os.path.exists(p): return None
        t1900 = 0.0; prev = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                if len(c) < len(h): continue
                y = int(c[ix['year']])
                if y == 1900: t1900 += fl(c[ix['railTrackEnd']])
                if 'needUsedShareEnd' in ix:
                    k = (c[ix['nation']], c[ix['region']]); v = fl(c[ix['needUsedShareEnd']])
                    pv = prev.get(k)
                    if pv is not None and pv[0] == y - 1 and pv[1] > 0 and v > 0: lnw.append(abs(math.log(v / pv[1])))
                    prev[k] = (y, v)
        tot.append(t1900)
    return statistics.mean(tot), (statistics.median(lnw) if lnw else float('nan'))

def tport(ins):
    n = {}; e = {}
    for s in range(1, 11):
        p = os.path.join(ins, f'tport_s{s}.tsv')
        if not os.path.exists(p): return None
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                if len(c) < len(h): continue
                for k in (c[ix['nation']], '全て'):
                    n[k] = n.get(k, 0) + 1; e[k] = e.get(k, 0) + (c[ix['treatyEngineFlag']] == '1')
    return {k: e[k] / n[k] for k in ('イギリス', 'フランス', '清', '日本', 'ロシア', '全て') if n.get(k)}

print('ラベル\t世界の線路 1900 年（種の平均）\tG-W の量\tengine の旗の割合（英・仏・清・日・露・全て）')
for a in sys.argv[1:]:
    lab, ins = a.split('=', 1)
    r = rail(ins); t = tport(ins)
    rs = f'{r[0]:.4g}\t{r[1]:.4f}' if r else '無い\t無い'
    ts = '・'.join(f'{k} {v*100:.2f}%' for k, v in t.items()) if t else '無い'
    print(f'{lab}\t{rs}\t{ts}')
