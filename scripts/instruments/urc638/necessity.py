# (638) 必要性の確認（事前登録 [12] ⓑ・読むだけ）：核の州の減り（走るピーク → 最小）のうち、疫病が奪った人数の割合 R。
# R = (走るピークの時点から最小の時点までの epi_s の deadK の和) ÷ (ピーク − 最小)。種ごとに出し、州ごとの種の中央値を出す。
# 門（登録 [12] ⓑ）：3 州のうち 2 州以上で中央値 R が 1/3 以上なければ前提が崩れた＝B を回さない。
# 使い方：python -X utf8 -I necessity.py <計器の出力ディレクトリ(ins)> [種の数]
import csv, sys, collections, statistics as st

CORE = [('ネーデルラント', 'アムステルダム'), ('イギリス', 'ロンドン'), ('プロイセン', 'ベルリン')]
d = sys.argv[1]
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
res = collections.defaultdict(list)
for s in range(1, NS + 1):
    ser = collections.defaultdict(list)   # (nation, region) -> [(quarter, popK)]
    for r in csv.DictReader(open(f'{d}/region_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        k = (r['nation'], r['region'])
        if k in CORE:
            ser[k].append((int(r['quarter']), float(r['populationK'])))
    dead = collections.defaultdict(list)   # (nation, region) -> [(quarter, deadK)]
    try:
        for r in csv.DictReader(open(f'{d}/epi_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            k = (r['nation'], r['region'])
            if k in CORE:
                dead[k].append((int(r['quarter']), float(r['deadK'])))
    except FileNotFoundError:
        print(f'epi_s{s}.tsv が無い')
    for k in CORE:
        v = ser[k]
        if not v:
            continue
        pk = v[0][1]; pkq = v[0][0]; best = 1e9; bq = pkq; bpk = pk; bpkq = pkq; bpop = pk
        for q, x in v:
            if x > pk:
                pk, pkq = x, q
            ratio = x / pk if pk > 0 else 1.0
            if ratio < best:
                best, bq, bpk, bpkq, bpop = ratio, q, pk, pkq, x
        drop = bpk - bpop
        dsum = sum(dk for q, dk in dead[k] if bpkq < q <= bq)
        R = dsum / drop if drop > 1e-9 else float('nan')
        res[k].append((s, best, bpkq, bq, drop, dsum, R))
print(f'## 必要性の確認 {d}')
ok = 0
for k in CORE:
    rows = res[k]
    for (s, best, pq, tq, drop, dsum, R) in rows:
        print(f'  {k[1]} 種{s} 最小÷ピーク {best:.3f} ピークq{pq}→最小q{tq} 減り {drop:.0f}千人 疫病の死 {dsum:.0f}千人 R={R:.2f}')
    rs = [r[6] for r in rows if r[6] == r[6]]
    med = st.median(rs) if rs else float('nan')
    print(f'  ▶ {k[1]} R の種の中央値 {med:.2f}')
    if med >= 1 / 3:
        ok += 1
print(f'## 門：1/3 以上の州 {ok}/3 → ' + ('前提は生きている（B を回す）' if ok >= 2 else '前提が崩れた（B を回さない）'))
