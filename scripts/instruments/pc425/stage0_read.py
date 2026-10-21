# 段0：開幕の前提を m420 の生（pop_s）から読む。読むだけ。
import csv, glob, math, statistics, sys, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める
sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1]
NAT = ['イギリス','フランス','プロイセン','オーストリア','ロシア','日本','清','ネーデルラント','ポルトガル','スペイン']
rows = collections.defaultdict(list)   # (nation, era) -> list of dict per seed-quarter
def num(v):
    try: return float(v)
    except: return float('nan')
for f in sorted(glob.glob(D + '/pop_s*.tsv')):
    with open(f, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['scope'] != 'N' or r['nation'] not in NAT: continue
            q = int(r['quarter']); y = int(r['year'])
            if num(r['recoveringMonths']) > 0.25 * num(r['monthsCount']): continue
            era = 'Q0-12' if q <= 12 else ('1783-99' if y < 1800 else None)
            for e in ([era] if era else []) + (['1783-99'] if q <= 12 else []):
                rows[(r['nation'], e)].append(r)
def med(xs):
    xs = [x for x in xs if x == x and not math.isinf(x)]
    return statistics.median(xs) if xs else float('nan')
print('国\t時代\t行\ttfr代理\ttfr影\t影÷代理\tm\tcbr‰\tcdr‰\tA比(手取り÷籠)')
tab = {}
for (n, e), rs in sorted(rows.items(), key=lambda t: (NAT.index(t[0][0]), t[0][1])):
    tp = med([num(r['tfrProxy']) for r in rs]); ts = med([num(r['tfrShadow']) for r in rs]); m = med([num(r['marriedMNewShare']) for r in rs])
    y = med([num(r['yAdultPerMonth']) for r in rs]); b = med([num(r['basketPerMonth']) for r in rs])
    tab[(n, e)] = (tp, ts, m)
    print(f"{n}\t{e}\t{len(rs)}\t{tp:.2f}\t{ts:.2f}\t{ts/tp:.2f}\t{m:.3f}\t{med([num(r['cbrPerMille']) for r in rs]):.1f}\t{med([num(r['cdrPerMille']) for r in rs]):.1f}\t{y/b:.2f}")
for e in ('Q0-12', '1783-99'):
    if ('フランス', e) in tab and ('イギリス', e) in tab:
        print(f"仏÷英 の m（{e}）= {tab[('フランス', e)][2] / tab[('イギリス', e)][2]:.3f}")
