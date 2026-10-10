"""(176) 接触の無い戦争（読むだけ）＝inj_war171 の WS/WE。終わった戦争と続く戦争の両方で、接触（|運|の累計>0 か 押しの項の累計≠0）の有無ごとの月数と終わり方。
python an_nocontact176.py <w171 dir> [ラベル]"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
RS = {0: '国の除去', 1: '決定的', 2: '崩壊', 3: '崩壊', 4: '両方の崩壊', 5: '双方の厭戦', 6: '調停', 7: '他', -1: '続行中'}
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
rows = []
for f in glob.glob(os.path.join(D, 'w*.txt')):
    S = {}
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'WS': S[t[1]] = t
        elif t[0] == 'WE':
            s = S[t[1]]; ended = t[2] == '1'
            months = int(t[4]) if ended else 2003 - int(s[2]) + 1
            contact = fl(t[11]) > 0 or fl(t[9]) != 0
            rows.append((ended, months, contact, int(t[5]) if ended else -1, s[5] == '1', fl(t[12]), fl(t[13])))
print(f'######## {LAB}（戦争 {len(rows)} 本）')
for ended in (True, False):
    for contact in (True, False):
        rs = [r for r in rows if r[0] == ended and r[2] == contact]
        if not rs: print(f"{'終わった' if ended else '続く'}・接触{'あり' if contact else 'なし'} 0本"); continue
        ms = [r[1] for r in rs]
        c = collections.Counter(RS.get(r[3], '?') for r in rs)
        print(f"{'終わった' if ended else '続く'}・接触{'あり' if contact else 'なし'} {len(rs)}本・月数の中央 {st.median(ms):.0f}・最大 {max(ms)}・60か月以上 {sum(1 for m in ms if m >= 60)}・終わり方 {dict(c.most_common())}")
