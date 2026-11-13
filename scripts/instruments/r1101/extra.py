# (1101) 追加の読み：①運河の開通と、要る発見（汽船・ダイナマイト）の最初の四半期の差 ②運賃の受け取り÷払いの上位の国
# 呼び方：python -X utf8 -I extra.py <腕の名> <生のフォルダ> <出力>
import csv, sys, os
from collections import defaultdict
arm, root, outp = sys.argv[1], sys.argv[2], sys.argv[3]
out = open(outp, 'w', encoding='utf-8', newline='\n')
def P(*a): out.write('\t'.join(str(x) for x in a) + '\n')
seeds = sorted({int(f.split('_s')[1].split('.')[0]) for f in os.listdir(root) if f.startswith('canal_s')})
need = {'suez': '汽船', 'panama': 'ダイナマイト'}
lags = defaultdict(list)
for s in seeds:
    first = {}
    with open(os.path.join(root, f'disc_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); H = {k: i for i, k in enumerate(h)}
        for row in r:
            if row[H['kind']] != 'E': continue
            nm = row[H['row']]
            if nm in need.values():
                q = int(row[H['quarter']])
                if nm not in first or q < first[nm]: first[nm] = q
    with open(os.path.join(root, f'canal_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); H = {k: i for i, k in enumerate(h)}
        for row in r:
            cid, oq = row[H['canalId']], int(row[H['openedQuarter']])
            fq = first.get(need[cid], -1)
            lags[cid].append((s, fq, oq, (oq - fq) if oq >= 0 and fq >= 0 else 'na'))
for cid, L in lags.items():
    P('canalLag', cid, 'seed:firstDiscQ:openQ:lagQ', ' '.join(f'{a}:{b}:{c}:{d}' for a, b, c, d in L))
paid = defaultdict(float); recv = defaultdict(float)
for s in seeds:
    with open(os.path.join(root, f'trnation_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); H = {k: i for i, k in enumerate(h)}
        for row in r:
            n = row[H['nation']]
            try:
                paid[n] += float(row[H['freightPaidGoldPerYear']]); recv[n] += float(row[H['freightReceivedGoldPerYear']])
            except ValueError:
                pass
tp, tr = sum(paid.values()), sum(recv.values())
top = sorted(recv, key=lambda n: -recv[n])[:8]
P('freight', 'nation:recvShare:paidShare:recvOverPaid', ' '.join(f'{n}:{100*recv[n]/tr:.2f}%:{100*paid[n]/tp:.2f}%:{recv[n]/paid[n] if paid[n] else float("inf"):.2f}' for n in top))
for n in ('イギリス', 'オランダ', 'アメリカ', 'フランス'):
    if n in recv: P('freight', n, f'{100*recv[n]/tr:.2f}%', f'{100*paid[n]/tp:.2f}%', f'{recv[n]/paid[n] if paid[n] else float("inf"):.2f}')
out.close()
