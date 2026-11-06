# (897) c76db 読む線の 2 本目（紙の予言の材料・読むだけ）：人の居ない・debt＞0 の商人の行（cpop_s）について
#   python -X utf8 debt897b.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   B5〈相続の前に債務を返す〉の紙：その四半期の inMarket（月末の精算の所得の側＝相続が抜く前の額）÷3 が debt 以上の行の割合
#     （月 1 回の相続で 1 か月分の所得から返せる＝その行は 1 か月で返し切れる・推定＝月の内訳は表に無い）。
#   B1〈債務も相続人へ移す〉の紙：同じ州×四半期の cearn_s の savings（州の POP の在庫の和）が debt 以上の行の割合（相続人が返せる上限の目安）。
#   止まった長さ：同じ鍵で人数 ≦1e-12・debt＞0 が続いた四半期の長さの分布（続きの区切り＝人が来たか debt が 0 か表の終わり）。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def pct(v, p):
    if not v: return float('nan')
    v = sorted(v); return v[min(len(v) - 1, max(0, int(round(p * (len(v) - 1)))))]
for tag, d in [a.split('=', 1) for a in args]:
    n = b5 = b1 = 0; runs = []; ends = defaultdict(int); clr = []; g1 = 0
    for s in range(s0, s1 + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv'); pc = os.path.join(d, f'cearn_s{s}.tsv')
        if not (os.path.exists(p) and os.path.exists(pc)): print(f'# {tag} 種 {s} 表なし'); continue
        regsav = {}
        with open(pc, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); iq, ireg, isv = h.index('quarter'), h.index('region'), h.index('savings')
            for line in f:
                x = line.rstrip('\n').split('\t'); regsav[(x[ireg], int(x[iq]))] = fl(x[isv])
        ser = defaultdict(list)
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, ireg, ipi, icl, ipk, idb, imk = ix['quarter'], ix['region'], ix['popIndex'], ix['className'], ix['popK'], ix['debt'], ix['inMarketPerQuarter']; icr = ix['inCreditPerQuarter']
            for line in f:
                hd = line.split('\t', 9)
                if hd[icl] != '商人': continue
                x = line.rstrip('\n').split('\t')
                ser[(x[ireg], x[ipi])].append((int(x[iq]), fl(x[ipk]), fl(x[idb]), fl(x[imk]), fl(x[icr])))
        for key, v in ser.items():
            # B5 の紙の 2：最初の止まった続きで、q* からの inMarket の累積が q* の debt に届くまでの四半期の数（旗 0 の軌道の上の近似・届かなければ None）
            js = [j for j, t in enumerate(v) if not (t[1] > DEAD) and t[2] > 0]
            if js:
                j0 = js[0]; need = v[j0][2]; acc = 0.0; k = None
                for j in range(j0, len(v)):
                    if v[j][1] > DEAD or not (v[j][2] > 0): break
                    acc += v[j][3]
                    if acc >= need: k = j - j0 + 1; break
                clr.append(k)
            run = 0
            for j, (q, pk, db, mk, cr) in enumerate(v):
                stuck = not (pk > DEAD) and db > 0
                if stuck:
                    n += 1; run += 1
                    if mk / 3 >= db: b5 += 1
                    if db - cr > 1e-9 * max(1.0, db): g1 += 1   # G1′ の旗 0 の値：前の四半期から債務を持ち越した行
                    rs = regsav.get((key[0], q), float('nan'))
                    if rs >= db: b1 += 1
                elif run:
                    runs.append(run); ends['人が来た' if pk > DEAD else 'debt が 0'] += 1; run = 0
            if run: runs.append(run); ends['表の終わり（668 四半期目か植民地格を出た）'] += 1
    print(f'# {tag} {d} 種 {s0}-{s1}')
    print(f'{tag}\t人の居ない・debt＞0 の行\t{n}\tB5 の紙：inMarket÷3 ≧ debt の行 {b5}（{b5 / n if n else float("nan"):.4f}）\tB1 の紙：州の savings ≧ debt の行 {b1}（{b1 / n if n else float("nan"):.4f}）')
    print(f'{tag}	G1′ の旗 0 の値（人数 ≦1e-12 で debt − inCredit ＞1e-9×max(1, debt) の行）	{g1}')
    got = [x for x in clr if x is not None]
    print(f'{tag}\tB5 の紙の 2：inMarket の累積が q* の debt に届く四半期の数（最初の続き）\t鍵 {len(clr)}\t届いた {len(got)}\t1 四半期 {sum(1 for x in got if x == 1)}\t2〜4 {sum(1 for x in got if 2 <= x <= 4)}\t5 以上 {sum(1 for x in got if x >= 5)}\t届かない {len(clr) - len(got)}\t中央 {pct(got, 0.5)}\t最大 {max(got) if got else 0}')
    print(f'{tag}\t止まった続きの長さ（四半期）\t続きの数 {len(runs)}\t中央 {pct(runs, 0.5)}\t90 点 {pct(runs, 0.9)}\t最大 {max(runs) if runs else 0}\t終わり方 {dict(ends)}')
