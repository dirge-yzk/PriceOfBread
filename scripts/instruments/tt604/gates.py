import sys, os, glob, statistics, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tariff_series as ts
# (604) 線 tt：狙いの数字 R0・G1・G2・G3 と記録（貿易÷GDP の年代）を、計器の生 <出力>/ins から読む（読むだけ）。
# 使い方：PYTHONUTF8=1 python gates.py <ins フォルダ> [<ins フォルダ> ...]
def seeds(d): return sorted(int(''.join(c for c in os.path.basename(f) if c.isdigit())) for f in glob.glob(os.path.join(d, 'scalars_s*.tsv')))
def scal(path):
    out = []
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for ln in f:
            p = ln.rstrip('\n').split('\t')
            out.append({n: p[i] for n, i in ix.items() if i < len(p)})
    return out
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def run(d, nexp=None):
    print('==', d)
    ss = seeds(d)
    if nexp is not None: print('種の数', len(ss), '/ 登録', nexp, 'OK' if len(ss) == nexp else '*** 不一致 ***')
    tot_move = 0; moved_seeds = 0; ups = []; downs = []; locks = []; med1913 = []
    era = collections.defaultdict(list)       # 年 → 種ごとの 9 国中央値
    maj = collections.defaultdict(list)       # 年 → 種ごとの 6 大国平均
    majors = ('イギリス', 'フランス', 'ロシア', 'プロイセン', '清', 'アメリカ')   # 成績表の 貿易（復活計器）の 6 大国（名前に含むものを同じ国とみなす＝変名の接頭辞を許す）
    for s in ss:
        rows = scal(os.path.join(d, f'scalars_s{s}.tsv'))
        if not rows or 'tradeTreatyMoveCumCount' not in rows[0]:
            print('seed', s, '新しい列が無い'); continue
        # 国ごとの累計の最大（終局）の合計
        mx = {}
        for r in rows:
            nm = r['nation']; mx[nm] = max(mx.get(nm, 0), int(float(r['tradeTreatyMoveCumCount'])))
        mv = sum(mx.values()); tot_move += mv; moved_seeds += 1 if mv > 0 else 0
        up, down, lock, lv = ts.per_seed(ts.read(os.path.join(d, f'secession_s{s}.tsv')))
        ups.append(up); downs.append(down); locks.append(lock); med1913.append(lv[1913])
        # 貿易÷GDP：毎四半期出る行（q%4==0 かつ年が yr）の 9 国の中央値
        for yr in (1800, 1850, 1880, 1913, 1949):
            xs = [fl(r['tradeToGdpRatio']) for r in rows if int(r['year']) == yr and int(r['quarter']) % 4 == 0]
            xs = [x for x in xs if x == x]
            era[yr].append(statistics.median(xs) if xs else float('nan'))
            ms = [fl(r['tradeToGdpRatio']) for r in rows if int(r['year']) == yr and int(r['quarter']) % 4 == 0 and any(m in r['nation'] for m in majors)]
            ms = [x for x in ms if x == x]
            maj[yr].append(statistics.mean(ms) if ms else float('nan'))
        print('seed', s, '締結で動いた回数(国の合計)', mv, 'up', up, 'down', down, 'lock1900 %.3f' % lock, 'T1913中央 %.2f' % lv[1913])
    if not ups: return
    print('R0/G1: 締結で動いた回数の合計', tot_move, '/ 動いた種', moved_seeds, '/', len(ss))
    print('G2: 全種で up>=1 かつ down>=1 :', all(u >= 1 and dn >= 1 for u, dn in zip(ups, downs)), '/ 合計 下げ÷上げ %.2f' % (sum(downs) / max(1, sum(ups))), '(>=0.35)')
    print('G3: lock1900 の最大 %.3f (<=0.10)' % max(locks), '/ T1913 中央の種の中央値 %.2f (<=0.5)' % statistics.median(med1913))
    print('記録 貿易÷GDP(6 大国の平均の種の中央値) ', ' '.join('%d:%.3f' % (y, statistics.median([v for v in maj[y] if v == v] or [float('nan')])) for y in sorted(maj)))
    print('記録 貿易÷GDP(9 国の中央値の種の中央値) ', ' '.join('%d:%.3f' % (y, statistics.median([v for v in era[y] if v == v] or [float('nan')])) for y in sorted(era)))
if __name__ == '__main__':
    # 使い方：gates.py <ins> [<登録の種の数>]  （第 2 引数が数字なら種の数を確かめる）
    a = sys.argv[1:]
    nexp = int(a[1]) if len(a) > 1 and a[1].isdigit() else None
    run(a[0], nexp)
