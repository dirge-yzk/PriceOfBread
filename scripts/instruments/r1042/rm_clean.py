# (1042) 札の近似を外した組だけの割合と参照 B（clean）・札が同じ組の上位（結果の後に足した表＝助言で足した）
import sys, json, collections
D = json.load(open(sys.argv[1], encoding='utf-8'))
by = collections.defaultdict(dict)
for r in D: by[r['arm']][r['seed']] = r['v']
S = lambda a, k: sum(v.get(k, 0.0) for v in by[a].values())
for a in ['B', 'N1', 'N2']:
    t = S(a, 'cl|tot'); b = S(a, 'clB|base')
    print(a, f'clean の量 {t:.0f}（全体の {t / S(a, "tot|all"):.3f}）',
          ' '.join(f'{k}{c} 実{S(a, f"cl|{k}|{c}") / t:.4f}/参照{S(a, f"clB|{k}|{c}") / b:.4f}' for k in ['c', 'r'] for c in ['same', 'fam']))
pc = collections.Counter()
for v in by['B'].values():
    for k, x in v.items():
        if k.startswith('samepair|'): pc[k[9:]] += x
T = S('B', 'tot|all')
print('札が同じ組の上位（B・全体に対する割合・cl＝実の札・ap＝近似の札を含む）')
for k, x in pc.most_common(12): print(k, round(x / T, 4))
