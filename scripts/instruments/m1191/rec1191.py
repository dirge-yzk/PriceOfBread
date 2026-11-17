# (1191) 合流の記録（門ではない）：線 nc の反証役 v2 の物差しを生から数え直す（v2 の使い捨ての sold.py・sec.py の型を 1 本にした）。
#   ①兵÷人口：nation_s の soldierK÷populationK（人口 500 千人以上・皆兵の法が 0／None の行は除く）を、戦時（atWarMonths≧3）×総動員（mobilizedFlag）・戦時・平時（atWarMonths 0）で平均。
#   ②分離の圧：secession_s の pressureIndex の種ごとの p90 と popK 加重の平均（種の中央を出す）。
#   python -X utf8 -I rec1191.py <種の起点> <名=ins フォルダ> ...
import csv, sys, statistics as st
SB = int(sys.argv[1]); ARMS = [a.split('=', 1) for a in sys.argv[2:]]
def sold(d):
    o = {'warmob': [], 'war': [], 'peace': []}
    for s in range(SB, SB + 10):
        with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                try:
                    pk = float(r['populationK']); sk = float(r['soldierK']); aw = float(r['atWarMonths']); law = r['conscriptionLaw']; mob = r['mobilizedFlag']
                except (KeyError, ValueError): continue
                if pk < 500 or law in ('0', 'None'): continue
                x = sk / pk
                if aw >= 3:
                    o['war'].append(x)
                    if mob in ('1', 'True'): o['warmob'].append(x)
                elif aw == 0: o['peace'].append(x)
    return {k: (len(v), st.mean(v) if v else float('nan')) for k, v in o.items()}
def sec(d):
    p90 = []; wm = []
    for s in range(SB, SB + 10):
        P = []; W = 0.0; PW = 0.0
        try:
            with open(f'{d}/secession_s{s}.tsv', encoding='utf-8') as fh:
                for r in csv.DictReader(fh, delimiter='\t'):
                    try: p = float(r['pressureIndex']); w = float(r['popK'])
                    except (KeyError, ValueError): continue
                    P.append(p); W += w; PW += p * w
        except FileNotFoundError: return None
        P.sort(); p90.append(P[int(.9 * len(P))]); wm.append(PW / W if W else 0.0)
    return st.median(p90), min(p90), max(p90), st.median(wm)
for lab, d in ARMS:
    a = sold(d); b = sec(d)
    print(lab, '兵÷人口 ' + ' '.join(f'{k} n={n} 平均={m:.4f}' for k, (n, m) in a.items()),
          '| 分離の圧 ' + ('表なし' if b is None else f'p90 種の中央 {b[0]:.4f}（{b[1]:.4f}〜{b[2]:.4f}）・popK 加重の平均の種の中央 {b[3]:.4f}'))
