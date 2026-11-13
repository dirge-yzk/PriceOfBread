# (1109) c106rn〈追加の読み・集計の後に書いた〉：買えない側の確かな下限。
# 年中ずっと T≦need（g7ExcessRows＝0）なら毎月の欲しい量 ≧ 置換＝TransportDecay×Td（Transport.cs:227）＝全部買えれば 買った容量 ≧ 減耗の失い（地形の倍率は 1 以上＝Coefficients.TerrainRailCost）。
# よって g7ExcessRows＝0 かつ boughtCapPerYear ＜ 0.99×decayLossPerYear の州×年は、年の中の need の道に依らず〈Procure が欲しい量を満たさなかった〉。
# 呼び方：python -X utf8 -I buyside_rn.py <ins> <出力の txt>
import sys, os, glob
from collections import Counter

ins, outp = sys.argv[1], sys.argv[2]
LOW = 0.99
low = 0; robust = 0; zero_buy = 0; zero_track = 0; crossed = 0; by_tier = Counter(); by_nat = Counter(); low_nat = Counter(); low_tier = Counter()
by_decade = Counter(); low_decade = Counter()
for path in sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv'))):
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    tier = {}
    with open(os.path.join(ins, f'region_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.split('\t', ix['tier'] + 1)
            try: tier[(c[ix['region']], int(c[ix['year']]))] = c[ix['tier']]
            except ValueError: pass
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if not (float(c[ix['needEnd']]) > 0) or c[ix['waterFlag']] == '1': continue
            if not (float(c[ix['phiEnd']]) < LOW): continue
            y = int(c[ix['year']]); tr = tier.get((c[ix['region']], y), '?'); nn = c[ix['nation']]
            low += 1; low_tier[tr] += 1; low_nat[nn] += 1; low_decade[y // 10 * 10] += 1
            bought, dec, excess_rows = float(c[ix['boughtCapPerYear']]), float(c[ix['decayLossPerYear']]), int(c[ix['g7ExcessRows']])
            # 〈追加の読みの直し〉置換は減耗の後の T に掛かる＝全部買えても減耗の失い×0.98＝切り口は 0.97。T＝0 のまま買い 0 の行（減耗 0）も確かに買えない側へ
            if not (excess_rows > 0) and ((dec > 0 and bought < 0.97 * dec) or not (bought > 0)):
                robust += 1; by_tier[tr] += 1; by_nat[nn] += 1; by_decade[y // 10 * 10] += 1
                if not (bought > 0): zero_buy += 1
                if not (float(c[ix['railTrackEnd']]) > 0) and not (bought > 0): zero_track += 1
            if excess_rows > 0: crossed += 1
out = [f'== {ins}',
       f'X1 年の中で need が T をまたいだ {crossed}（{crossed / max(1, low):.3f}）・X2 確かに買えない {robust}（{robust / max(1, low):.3f}）・X3 残り {low - crossed - robust}（{(low - crossed - robust) / max(1, low):.3f}）・X2 のうち T＝0 のまま買い 0 {zero_track}',
       f'φ＜0.99 の州×年 {low}・うち〈年中 T≦need で買いが減耗の失いに届かない〉{robust}（{robust / max(1, low):.3f}）・うち買い 0 {zero_buy}',
       '格ごと（その格の φ＜0.99 に対する割合）：' + '・'.join(f'{k} {by_tier[k]}／{low_tier[k]}（{by_tier[k] / max(1, low_tier[k]):.3f}）' for k in sorted(low_tier)),
       '国の上位 12（件数）：' + '・'.join(f'{k} {v}（{v / max(1, low_nat[k]):.2f}）' for k, v in by_nat.most_common(12)),
       '10 年ごと（割合）：' + ' '.join(f'{d} {by_decade[d] / max(1, low_decade[d]):.2f}' for d in sorted(low_decade))]
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
