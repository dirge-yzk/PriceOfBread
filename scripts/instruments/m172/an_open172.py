"""(172) 反証役 vm3#2＝終局で続く戦争（終わっていない戦争）の表（読むだけ）＝inj_war171 の w<seed>.txt の WS/WE（WE の ended=0）。
python an_open172.py <w171 ディレクトリ> [ラベル] [最後の月=2003]
  開戦からの月数＝最後の月−開戦の月＋1（668四半期×3＝月 0〜2003）。接触＝|運|の累計>0 か 押しの項の累計≠0（WarLuckNeedsContact＝1 で押しが0の月は運も0）。
  型＝内戦／対外・抽象の相手（片側が国を持たない）／列強どうし（開戦時のランク≧3）／列強と非列強／非列強どうし（an_war171 と同じ区分）。"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D; LAST = int(sys.argv[3]) if len(sys.argv) > 3 else 2003
GREAT = 3
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
rows = []; tot = 0; seeds = 0
for f in sorted(glob.glob(os.path.join(D, 'w*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4]); seeds += 1; S = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'WS': S[t[1]] = t; tot += 1
        elif t[0] == 'WE' and t[2] == '0':
            s = S[t[1]]; t0 = int(s[2]); y0 = int(s[3]); civil = s[5] == '1'; an, bn = s[9] == '1', s[10] == '1'; ar, br = int(s[11]), int(s[12])
            if civil: ty = '内戦'
            elif not (an and bn): ty = '対外・抽象の相手'
            elif ar >= GREAT and br >= GREAT: ty = '列強どうし'
            elif ar >= GREAT or br >= GREAT: ty = '列強と非列強'
            else: ty = '非列強どうし'
            contact = fl(t[11]) > 0 or fl(t[9]) != 0
            rows.append((sd, y0, LAST - t0 + 1, ty, s[7], s[8], contact))
print(f'######## {LAB}（種 {seeds}・戦争 {tot} 本・終局で続く戦争 {len(rows)} 本＝種あたり {len(rows) / max(1, seeds):.1f}）')
if rows:
    ms = [r[2] for r in rows]
    print(f'開戦からの月数＝中央 {st.median(ms):.0f}・最大 {max(ms)}・60 か月以上 {sum(1 for m in ms if m >= 60)} 本・120 か月以上 {sum(1 for m in ms if m >= 120)} 本')
    nc = [r for r in rows if not r[6]]
    print(f'接触の無い戦争（|運|と押しの累計が0）＝{len(nc)} 本・その月数の中央 {st.median([r[2] for r in nc]) if nc else float("nan"):.0f}・最大 {max([r[2] for r in nc]) if nc else 0}')
    c = collections.Counter(r[3] for r in rows)
    print('型＝' + '・'.join(f'{k} {v}' for k, v in c.most_common()))
    for r in sorted(rows, key=lambda r: -r[2])[:12]:
        print(f'  種{r[0]} {r[1]}〜 {r[2]}か月・{r[3]}・{r[4]} 対 {r[5]}・接触{"あり" if r[6] else "なし"}')
