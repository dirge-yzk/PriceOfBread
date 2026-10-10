"""(207) 不履行の窓の中身（読むだけ・反証役 vf2#1・#2）。python an_d207.py <out_dir（d204 を含む）> [タグ]
入力＝inj_d204 の X 行（不履行の瞬間・窓の月ごとの r）。
出すもの（不履行ごと→帯）：
  窓の長さ n・累計 Σ(1−r)・**本当の直前8か月の r の平均**（窓の最後の min(8,n) か月・窓の外の月は r＝1 とみなさない＝窓の中だけ）・
  窓の平均（inj_d204 の avgR＝an_d204 の「前触れ」の帯）・Σ(1−r) のうち r≥0.99 の月が積んだ割合・最も深い月（最小の r）が不履行の何か月前か・
  最後の深い月（r＜0.95）から不履行までの月数（＝残りの月が窓をつないだ長さ）。"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; TAG = sys.argv[2] if len(sys.argv) > 2 else D
NATS = ['イギリス', 'フランス', 'アメリカ', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本', 'スペイン', '清']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


rows = []
for f in sorted(glob.glob(os.path.join(D, 'd204', 'd*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'X': continue
        ms = '|'.join(t[22:])
        months = [[fl(x) for x in m.split(',')] for m in ms.split(';')[1:]] if ms.startswith('M') else []
        r = [m[0] for m in months if m and m[0] == m[0]]
        if not r: continue
        n = len(r); acc = sum(1 - x for x in r)
        last8 = r[-8:]; l8 = sum(last8) / len(last8)
        resid = sum(1 - x for x in r if x >= 0.99); share = resid / acc if acc > 0 else float('nan')
        imin = min(range(n), key=lambda i: r[i]); ago_min = n - 1 - imin
        deep = [i for i in range(n) if r[i] < 0.95]
        ago_deep = (n - 1 - deep[-1]) if deep else n
        rows.append(dict(sd=sd, yr=int(t[3]), name=t[4], n=n, acc=acc, l8=l8, avg=fl(t[7]), minr=min(r), share=share, ago_min=ago_min, ago_deep=ago_deep))


def band(x):
    return '≥0.99' if x >= 0.99 else ('0.95-0.99' if x >= 0.95 else ('0.70-0.95' if x >= 0.7 else '<0.70'))


B = ['≥0.99', '0.95-0.99', '0.70-0.95', '<0.70']
ns = len({x['sd'] for x in rows})
print(f'######## {TAG}＝不履行 {len(rows)} 件（種 {ns}）の窓の中身')
print('## ① 帯（窓の平均＝an_d204 の前触れの読み）×（本当の直前8か月の平均）')
cnt = collections.Counter((band(x['avg']), band(x['l8'])) for x in rows)
print('窓の平均＼直前8か月 | ' + ' | '.join(B))
for a in B: print(f'{a} | ' + ' | '.join(str(cnt[(a, b)]) for b in B))
print('## ② 窓の平均 0.95-0.99 と ≥0.99 の不履行（残りの月が窓をつないだか）')
sel = [x for x in rows if x['avg'] >= 0.95]
print(f'件数 {len(sel)}')
for x in sorted(sel, key=lambda x: (x['name'], x['yr'])):
    print(f"  {x['name']} {x['yr']}（種{x['sd']}）n={x['n']} 累計 {x['acc']:.3f} 直前8か月 {x['l8']:.3f} 窓の平均 {x['avg']:.3f} 最小 {x['minr']:.3f}（{x['ago_min']}か月前）・残り（r≥0.99）が積んだ割合 {x['share']:.2f}・最後の深い月（r＜0.95）から {x['ago_deep']}か月")
if sel:
    print(f"  中央：n {st.median([x['n'] for x in sel])}・直前8か月 {st.median([x['l8'] for x in sel]):.3f}・残りの割合 {st.median([x['share'] for x in sel]):.2f}・最後の深い月から {st.median([x['ago_deep'] for x in sel])}か月・最小の r {st.median([x['minr'] for x in sel]):.3f}")
print('## ③ 全ての不履行：最後の深い月（r＜0.95）から不履行までの月数の分布（＝残りの月がつないだ長さ）')
dist = collections.Counter(min(x['ago_deep'], 25) // 5 * 5 for x in rows)
print('  ' + '・'.join(f"{k}〜{k+4}か月 {dist[k]}" if k < 25 else f"25か月以上 {dist[k]}" for k in sorted(dist)))
print('## ④ 主要10か国の不履行（10種の和）')
c = collections.Counter(x['name'] for x in rows)
print('  ' + '・'.join(f'{n} {c[n]}' for n in NATS) + f'＝{sum(c[n] for n in NATS)}・全ての国 {len(rows)}')
uk = [x for x in rows if x['name'] == 'イギリス']
print('  英＝' + '・'.join(f"{x['yr']}（種{x['sd']}・n={x['n']}・直前8か月 {x['l8']:.2f}・最小 {x['minr']:.2f}）" for x in sorted(uk, key=lambda x: x['yr'])))
