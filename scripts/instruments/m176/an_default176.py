"""(176) 国債の消滅（不履行か否認）の読み手（読むだけ）＝hist の C 行（国債）と A 行（GDP）：国債が1四半期で半分以下へ落ちた事象（前の国債÷年GDP≧5%）を国×年で数える。
python an_default176.py <out_dir> [ラベル] [国名,...]"""
import sys, os, glob, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
NAT = sys.argv[3].split(',') if len(sys.argv) > 3 else ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン', 'スペイン']
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
ev = collections.defaultdict(list); allev = 0; seeds = set()
for f in sorted(glob.glob(os.path.join(D, 'hist', 'c*.txt'))):
    sd = int(os.path.basename(f)[1:-4]); seeds.add(sd)
    A = {}
    for ln in open(os.path.join(D, 'hist', f'a{sd}.txt'), encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'A': A[(int(t[1]), int(t[3]))] = fl(t[6])
    prev = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'C' or t[5] != '1': continue
        q, i, n, yr = int(t[1]), int(t[3]), t[4], int(t[2]); debt = fl(t[25]); gdp = A.get((q, i), float('nan'))
        if n in prev:
            d0, g0 = prev[n]
            if g0 > 0 and d0 / g0 >= 0.05 and debt < 0.5 * d0:
                allev += 1
                if n in NAT: ev[n].append((sd, yr, d0 / g0))
        prev[n] = (debt, gdp)
print(f'######## {LAB}（種 {len(seeds)}）＝国債が1四半期で半分以下へ（前の国債÷年GDP≧5%）＝全ての国 {allev} 件（種あたり {allev / max(1, len(seeds)):.1f}）')
for n in NAT:
    v = ev.get(n, [])
    print(f'{n}: {len(v)} 件　' + '・'.join(f'種{s} {y}（{100 * r:.0f}%）' for s, y, r in v))
