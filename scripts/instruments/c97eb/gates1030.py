# (1030) c97eb：門の判定（事前登録 measurements/m1030_20261111_prereg_eb.txt の §3）。m1030.py の出力（種別の量）を腕・基線・ヌル腕 2 本で読む。
# 使い方: python -X utf8 -I gates1030.py <腕の met.tsv> <基線の met.tsv> <n1 の met.tsv> <n2 の met.tsv> [ラベル]
#   旗 0 の確かめ＝腕に基線そのものを渡す（M0）。比は和÷和・種の中央値は statistics.median。
import sys, statistics as st, collections
A, B, N1, N2 = sys.argv[1:5]; LAB = sys.argv[5] if len(sys.argv) > 5 else 'arm'
def load(p):
    d = collections.defaultdict(dict)
    for i, l in enumerate(open(p, encoding='utf-8')):
        if i == 0: continue
        s, k, v = l.rstrip('\n').split('\t'); d[k][int(s)] = float(v)
    return d
a, b, n1, n2 = load(A), load(B), load(N1), load(N2)
YEARS = (1870, 1913); G5 = ['石炭', '鉄鉱石', '銅鉱', '木', '畜産']
def med(d, k): return st.median(d[k].values())
def medr(d, num, den):   # 種ごとの比の中央値
    return st.median(d[num][s] / d[den][s] for s in d[num] if d[den][s] > 0)
def pool(d, num, den): return sum(d[num].values()) / sum(d[den].values()) if sum(d[den].values()) > 0 else float('nan')
res = []; P = lambda *x: print(*x)
P(f'# (1030) 門の判定・腕={LAB}')
# G1
g1 = []
for key in ('T2|1870', 'ALL|1870'):
    r = med(a, key) / med(b, key); g1.append(r >= 1.25)
    P(f'G1 {key} 腕 {med(a,key):.5f} 基線 {med(b,key):.5f} 新÷旧 {r:.3f}（≧1.25）ヌル腕 n1÷基線 {med(n1,key)/med(b,key):.3f} n2÷基線 {med(n2,key)/med(b,key):.3f}')
for key in ('T2|1913', 'ALL|1913'):
    P(f'G1記録 {key} 新÷旧 {med(a,key)/med(b,key):.3f} ヌル腕 {med(n1,key)/med(b,key):.3f}・{med(n2,key)/med(b,key):.3f}')
res.append(('G1', all(g1)))
# G2
g2 = []
for Y in YEARS:
    xs = {}; nb = 0
    for g in G5:
        xs[g] = [medr(d, f'exp|{Y}|{g}', f'out|{Y}|{g}') for d in (a, b, n1, n2)]
        if xs[g][1] > max(xs[g][2], xs[g][3]): nb += 1
    three = nb >= 4   # 基線がヌル腕 2 本の最大を超える財が 4 つ以上なら、閾値を 3 本（基線・n1・n2）の最大にする（事前登録 §3 G2）
    cnt = 0
    for g in G5:
        thr = max(xs[g][1:]) if three else max(xs[g][2:])
        ok = xs[g][0] > thr; cnt += ok
        P(f'G2 {Y} {g} 越境の輸出÷産出 腕 {xs[g][0]:.5f} 基線 {xs[g][1]:.5f} n1 {xs[g][2]:.5f} n2 {xs[g][3]:.5f} 閾値 {thr:.5f}{"（3本）" if three else "（n1・n2）"} {"上" if ok else "下"}')
    sh = []
    for g in ('鉄鉱石', '石炭'):
        s0 = [st.median(1 - d[f'outE|{Y}|{g}'][s] / d[f'out|{Y}|{g}'][s] for s in d[f'out|{Y}|{g}'] if d[f'out|{Y}|{g}'][s] > 0) for d in (a, b, n1, n2)]
        ok = s0[0] <= s0[1] - 0.05; sh.append(ok)
        P(f'G2 {Y} {g} share0 腕 {s0[0]:.4f} 基線 {s0[1]:.4f}（新≦旧−0.05）n1 {s0[2]:.4f} n2 {s0[3]:.4f} {"通る" if ok else "落ちる"}')
    P(f'G2 {Y} 上回った財 {cnt}/5（≧4）')
    g2.append(cnt >= 4 and all(sh))
res.append(('G2', all(g2)))
# G3
g3 = []; rec3 = False
for Y in YEARS:
    for g in ('鉄鉱石', '石炭'):
        v = [pool(d, f'expE|{Y}|{g}', f'exp|{Y}|{g}') for d in (a, b, n1, n2)]
        if not (v[1] == v[1]): rec3 = True
        ok = v[0] >= v[1]; g3.append(ok)
        P(f'G3 {Y} {g} 産地の国の越境の輸出の割合（和÷和）腕 {v[0]:.4f} 基線 {v[1]:.4f}（≧基線）n1 {v[2]:.4f} n2 {v[3]:.4f} {"通る" if ok else "落ちる"}')
res.append(('G3', 'record' if rec3 else all(g3)))
# U1
k = 'pxw|1913|硫黄'; r = med(a, k) / med(b, k)
P(f'U1 1913 作る国の硫黄の値段（産出の重み）腕 {med(a,k):.4f} 基線 {med(b,k):.4f} 新÷旧 {r:.3f}（≧0.6）ヌル腕 {med(n1,k)/med(b,k):.3f}・{med(n2,k)/med(b,k):.3f}')
res.append(('U1', r >= 0.6))
# U2
k = 'popNon|1913'; r = med(a, k) / med(b, k)
P(f'U2 1913 6 大国の非産地の州の人口の和 腕 {med(a,k):.0f} 基線 {med(b,k):.0f} 新÷旧 {r:.4f}（≧0.9）ヌル腕 {med(n1,k)/med(b,k):.4f}・{med(n2,k)/med(b,k):.4f}・名簿の外の州 腕 {med(a,"popUnk|1913"):.0f}')
res.append(('U2', r >= 0.9))
# U3
u3 = []
for Y in YEARS:
    def c(d, cl, kk): return pool(d, f'ro|{Y}|{cl}|{kk}', f'ro|{Y}|{cl}|o')
    paper = c(b, 'E', 'a') / c(b, 'N', 'u')
    v = [c(d, 'E', 'u') / c(d, 'N', 'u') for d in (a, b, n1, n2)]
    r = v[0] / paper; ok = 0.9 <= r <= 1.5; u3.append(ok)
    P(f'U3 {Y} 級 E の単位費用÷級 N の単位費用 腕 {v[0]:.4f}・紙（基線の unitCostIfAbundance÷N）{paper:.4f}・比 {r:.3f}（0.9〜1.5）基線 {v[1]:.4f} n1 {v[2]:.4f} n2 {v[3]:.4f} 級 E の単位費用 腕 {c(a,"E","u"):.4f} 基線 {c(b,"E","u"):.4f} {"通る" if ok else "落ちる"}')
res.append(('U3', all(u3)))
# U4
u4 = []
for Y in YEARS:
    k = f'pxw|{Y}|麦'; r = med(a, k) / med(b, k); ok = r >= 0.33; u4.append(ok)
    P(f'U4 {Y} 麦の世界の値段（産出の重み）腕 {med(a,k):.4f} 基線 {med(b,k):.4f} 新÷旧 {r:.3f}（≧0.33）ヌル腕 {med(n1,k)/med(b,k):.3f}・{med(n2,k)/med(b,k):.3f} {"通る" if ok else "落ちる"}')
res.append(('U4', all(u4)))
# 記録
for Y in YEARS:
    for g in ('金', '鉛鉱', '麦', '硫黄'):
        P(f'記録 {Y} {g} 産出（種の中央値）腕 {med(a,f"out|{Y}|{g}"):.1f} 基線 {med(b,f"out|{Y}|{g}"):.1f}・越境の輸出÷産出 腕 {medr(a,f"exp|{Y}|{g}",f"out|{Y}|{g}"):.4f} 基線 {medr(b,f"exp|{Y}|{g}",f"out|{Y}|{g}"):.4f}・値段 腕 {med(a,f"pxw|{Y}|{g}"):.4f} 基線 {med(b,f"pxw|{Y}|{g}"):.4f}')
P('判定 ' + '・'.join(f'{n} {"通る" if v is True else "落ちる" if v is False else "記録"}' for n, v in res))
