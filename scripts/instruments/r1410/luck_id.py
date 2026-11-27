# 戦争の運の再現：wexit の毎月の戦勝点の差から、Hash01 の運を引いた残り（=0.10*pushShare）が [-0.10,0.10] に収まる国の Id の組を探す（numpy なし）。
# 使い方: python luck_id.py <wexit_sN.tsv> [maxid] [nwars]
import sys, collections
M64 = (1 << 64) - 1
WL, WP = 0.12, 0.10
def hash01(key, salt):
    x = (key + (salt & 0xFFFFFFFF) * 0x9E3779B97F4A7C15) & M64
    x = ((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9) & M64
    x = ((x ^ (x >> 27)) * 0x94D049BB133111EB) & M64
    x ^= x >> 31
    return (x >> 11) * (1.0 / 9007199254740992.0)
def key(a, b):
    return (a << 32) | b if a < b else (b << 32) | a
def luck(a, b, year, months):
    return WL * (hash01(key(a, b) ^ (year << 20), months) * 2 - 1)

def load(p):
    rows = open(p, encoding='utf-8').read().splitlines()
    h = rows[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, [r.split('\t') for r in rows[1:]]

def wars_by_seq(ix, rows):
    # 1 本の戦争＝同じ組（A, B, 内戦の旗）で月が続く行。月が飛んだら別の戦争。
    d = collections.OrderedDict(); last = {}; cnt = collections.Counter()
    for r in rows:
        g = (r[ix['sideA']], r[ix['sideB']], r[ix['civilFlag']]); m = int(r[ix['month']])
        if g not in last or m != last[g] + 1: cnt[g] += 1
        last[g] = m; d.setdefault(g + (cnt[g],), []).append(r)
    return d

def steps(ix, rs):
    """(月数 k, 年, 差, 使えるか, 戦勝点) を返す。使える＝押しの和が正・調停なし・崩壊の代入なし・端の切りなし。"""
    out = []; prev = 0.0
    for k, r in enumerate(rs):
        s = float(r[ix['scoreIndex']])
        # pushSumPositiveFlag は戦争の始めからの押しの和（その月の押しではない）＝その月の押しが 0 なら運も 0 で差がちょうど 0。
        use = (s != prev and r[ix['forcedFlag']] == '0'
               and r[ix['oldCollapseAFlag']] == '0' and r[ix['oldCollapseBFlag']] == '0'
               and abs(s) < 1.5 - 1e-9 and abs(prev) < 1.5 - 1e-9)
        out.append((k + 1, int(r[ix['year']]), s - prev, use, s)); prev = s
    return out

def fits(a, b, st):
    for k, y, d, use, _ in st:
        if use and abs(d - luck(a, b, y, k)) > WP + 1e-9: return False
    return True

if __name__ == '__main__':
    ix, rows = load(sys.argv[1]); maxid = int(sys.argv[2]) if len(sys.argv) > 2 else 160
    nw = int(sys.argv[3]) if len(sys.argv) > 3 else 8
    ws = wars_by_seq(ix, rows)
    longest = sorted(ws.items(), key=lambda kv: -sum(1 for s in steps(ix, kv[1]) if s[3]))[:nw]
    for seq, rs in longest:
        st = steps(ix, rs)
        c = [(a, b) for a in range(maxid) for b in range(a + 1, maxid) if fits(a, b, st)]
        print(seq, rs[0][ix['sideA']], rs[0][ix['sideB']], len(rs), 'usable', sum(1 for s in st if s[3]), 'cand', len(c), c[:6])
