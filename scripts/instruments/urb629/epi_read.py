"""(629) 疫病 1 件ごとの表 epi_s の読み手（読むだけ）。python epi_read.py <計器の出力ディレクトリ> [<種の範囲 a-b>]
  事前登録 [9] ⓒ：ネーデルラントの active な州が 1 つになった最初の四半期 Q1（region_s から機械的に決める）で前期・後期に割り、
  アムステルダムに当たった疫病（一次＝origin 0/1）の件数と人口の減りを出す。二次（origin 2）は別列。国ごとの一次の合計（全種）も出す。"""
import csv, sys, collections, statistics, os
d = sys.argv[1]
a, b = (1, 10)
if len(sys.argv) > 2: a, b = map(int, sys.argv[2].split('-'))
NAT = 'ネーデルラント'; AMS = 'アムステルダム'
tot = collections.Counter(); tot2 = collections.Counter()
print('seed\tQ1\tprimAms_pre\tprimAms_post\tsecAms\tamsPeak\tamsMin\tmin/peak\tamsAtQ1\tamsEnd\tprimNL_all')
for s in range(a, b + 1):
    ef = f'{d}/epi_s{s}.tsv'; rf = f'{d}/region_s{s}.tsv'
    if not os.path.exists(rf): print(s, 'missing region'); continue
    by = collections.defaultdict(list); ams = {}
    for row in csv.DictReader(open(rf, encoding='utf-8'), delimiter='\t'):
        if row['nation'] != NAT or row['activeFlag'] != '1': continue
        q = int(row['quarter']); by[q].append(row['region'])
        if row['region'] == AMS: ams[q] = float(row['populationK'])
    Q1 = next((q for q in sorted(by) if len(by[q]) == 1), None)
    pre = post = sec = 0; allp = 0
    if os.path.exists(ef):
        for row in csv.DictReader(open(ef, encoding='utf-8'), delimiter='\t'):
            o = int(row['origin'])
            if o in (0, 1): tot[row['nation']] += 1
            else: tot2[row['nation']] += 1
            if row['nation'] != NAT: continue
            if o in (0, 1): allp += 1
            if row['region'] != AMS: continue
            if o == 2: sec += 1
            elif Q1 is not None and int(row['quarter']) >= Q1: post += 1
            else: pre += 1
    else: print(s, 'no epi file')
    if not ams: print(s, 'no ams'); continue
    pk = max(ams.values()); mq = min(ams, key=ams.get)
    atq = ams.get(Q1, float('nan')) if Q1 is not None else float('nan')
    print(f'{s}\t{Q1}\t{pre}\t{post}\t{sec}\t{pk:.0f}\t{ams[mq]:.0f}\t{ams[mq]/pk:.3f}\t{atq:.0f}\t{ams[max(ams)]:.0f}\t{allp}')
print('## 国ごとの一次の疫病の合計（全種）:', dict(sorted(tot.items(), key=lambda kv: -kv[1])[:12]))
print('## 二次の波:', dict(sorted(tot2.items(), key=lambda kv: -kv[1])[:6]))
