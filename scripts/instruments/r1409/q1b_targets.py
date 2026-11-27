# (1409) 問い 1 の続き：アメリカの標的。旗 1 の invade_s（成った侵略の全行）と旗 0 の wars_s の subjugate（拒否された侵略だけ）で、アメリカが狙った相手を誰がいつ取ったか。
import csv, os, sys, collections
def rows(D, t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return []
    with open(p, encoding='utf-8') as f: return list(csv.DictReader(f, delimiter='\t'))
F1, F0 = sys.argv[1], sys.argv[2]
NAT = sys.argv[3] if len(sys.argv) > 3 else 'アメリカ'
t0 = collections.Counter(); t1 = collections.Counter(); taker = collections.Counter(); firstyear = collections.defaultdict(list)
for s in range(1, 11):
    for r in rows(F0, 'wars', s):
        if r['entryCode'] == 'subjugate' and r['sideA'] == NAT: t0[r['sideB']] += 1
    for r in rows(F1, 'invade', s):
        if r['invader'] == NAT: t1[r['target']] += 1
for s in range(1, 11):
    for r in rows(F1, 'invade', s):
        if r['target'] in t0 and r['invader'] != NAT:
            taker[(r['target'], r['invader'])] += 1; firstyear[(r['target'], r['invader'])].append(int(float(r['year'])))
print(f'# {NAT} の標的：旗 0（subjugate の戦争・10 種の和）')
print(dict(t0.most_common()))
print(f'# {NAT} の標的：旗 1（invade の全行・10 種の和）')
print(dict(t1.most_common()))
print(f'# 旗 0 で {NAT} が狙った相手を旗 1 で取った他の国（10 種の和・年の中央）')
for (tg, inv), k in sorted(taker.items(), key=lambda x: -x[1])[:15]:
    ys = sorted(firstyear[(tg, inv)]); print(f'{tg}\t{inv}\t{k}\t{ys[len(ys)//2]}')
