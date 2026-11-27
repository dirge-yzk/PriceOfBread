# luck_count.py の 1 戦争 1 行の表を、戦争の長さ別にまとめる。
import sys, collections
from luck_count import lenbin
rows = [l.rstrip('\n').split('\t') for l in open(sys.argv[1], encoding='utf-8')][1:]
H = 'seed A B civil months usable id end ended flip mag out spush sluck sf sfml'.split()
R = [dict(zip(H, r)) for r in rows]
for r in R:
    for k in ('months', 'usable', 'flip', 'mag', 'out'): r[k] = int(r[k])
    for k in ('spush', 'sluck', 'sf', 'sfml'): r[k] = float(r[k])
bins = ['1-6', '7-12', '13-24', '25-48', '49-']
print('# 戦争の数（全 10 種）', len(R), ' 押しが毎月 0（運も 0）', sum(r['id'] == 'nopush' for r in R),
      ' Id 不明', sum(r['id'] in ('both-unknown', 'ambiguous') for r in R))
print('# 終わり方（最後の月の旗）×長さ')
c = collections.Counter((lenbin(r['months']), r['end']) for r in R)
ends = ['decisive', 'weary', 'collapse', 'mediation', 'other']
print('len\t' + '\t'.join(ends))
for b in bins: print(b + '\t' + '\t'.join(str(c[(b, e)]) for e in ends))
print('# 押し 0 の戦争（運が入らない＝今の盤面での〈運 0〉の自然の対照）の終わり方×長さ')
c = collections.Counter((lenbin(r['months']), r['end']) for r in R if r['id'] == 'nopush')
for b in bins: print(b + '\t' + '\t'.join(str(c[(b, e)]) for e in ends))
K = [r for r in R if r['id'] in ('init', 'found')]
print('# Id が分かった戦争', len(K), ' 外れの月（|p|>1）', sum(r['out'] for r in K), '/', sum(r['usable'] for r in K))
print('len\twars\tusableMonths\tflipShare\tluckBiggerShare\tmean|sumPush|\tmean|sumLuck|\tsignChanged\tdecisiveLostWithoutLuck')
for b in bins:
    G = [r for r in K if lenbin(r['months']) == b]
    if not G: continue
    um = sum(r['usable'] - r['out'] for r in G)
    # 勝ち負けの入れ替え（1 次の読み〈推〉）：終わりの戦勝点と、運の和を引いた戦勝点の符号が違う（白紙 0.25 未満は除く）
    sc = sum(1 for r in G if abs(r['sf']) >= 0.25 and r['sf'] * r['sfml'] < 0)
    dl = sum(1 for r in G if r['end'] == 'decisive' and abs(r['sfml']) < 1.0)
    print(f"{b}\t{len(G)}\t{um}\t{sum(r['flip'] for r in G)/max(1,um):.3f}\t{sum(r['mag'] for r in G)/max(1,um):.3f}\t"
          f"{sum(abs(r['spush']) for r in G)/len(G):.3f}\t{sum(abs(r['sluck']) for r in G)/len(G):.3f}\t{sc}\t{dl}")
# 押しの平均の大きさ別（1 月の平均 |p|）
print('# 1 月の平均の押し |sumPush|/(0.10*使えた月) の分布')
cc = collections.Counter()
for r in K:
    u = r['usable'] - r['out']
    if u <= 0: continue
    p = abs(r['spush']) / (0.10 * u)
    cc[min(5, int(p / 0.2))] += 1
print({f'{0.2*k:.1f}-': v for k, v in sorted(cc.items())})
print('# 1 月の平均の押し |p̄| 別の終わり方（Id が分かった戦争）')
c = collections.Counter(); 
for r in K:
    u = r['usable'] - r['out']
    if u <= 0: continue
    c[(min(4, int(abs(r['spush']) / (0.10 * u) / 0.2)), r['end'])] += 1
print('pbar\t' + '\t'.join(ends))
for k in range(5): print(f'{0.2*k:.1f}-\t' + '\t'.join(str(c[(k, e)]) for e in ends))
print('# 決定的に終わった戦争：運を引いた 1 次の読み〈推〉で |S|>=1 に届くまでの追加の月（p̄ が続くとして）')
ex = []
for r in K:
    u = r['usable'] - r['out']
    if r['end'] != 'decisive' or u <= 0: continue
    pb = abs(r['spush']) / u
    rest = 1.0 - abs(r['sfml'])
    ex.append(0 if rest <= 0 else (rest / pb if pb > 1e-9 else 9999))
ex.sort(); n = len(ex)
print('n', n, 'q10/50/90', [round(ex[int(q * (n - 1))], 1) for q in (0.1, 0.5, 0.9)], ' 0 か月', sum(1 for x in ex if x == 0), ' 12 か月超', sum(1 for x in ex if x > 12))
