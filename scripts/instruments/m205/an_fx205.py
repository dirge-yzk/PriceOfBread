"""(205) 英の交易の読み手（読むだけ）。python an_fx205.py <out_dir/fx205> [ラベル]
① 銀本位国の為替（下限 ExchangeRateFloor=0.30 への張り付きの割合・中央）と平価の元（銀の世界価格÷参照）・統計のクランプ・器A の量
② 英の輸出÷輸入（FOB÷CIF）・貿易/GDP（四半期に足した越境の成約・年換算）・運賃の受け取り÷GDP・貿易＋運賃の収支÷GDP・国境の台帳の動き÷GDP
③ 英の輸入の売り手の金建ての錨（輸入の積出し価格÷参照）と英の州の値段（買い手の金建ての州の価格÷参照）の比
④ 主要国の貿易/GDP（10年の窓・種の中央）と輸出÷輸入・運賃の受け取り÷GDP
10年の窓ごとに種ごとの窓の平均→種の中央。"""
import sys, os, re, statistics as st
from collections import defaultdict

d = sys.argv[1]; lab = sys.argv[2] if len(sys.argv) > 2 else d
FLOOR = 0.30
MAJ = ["イギリス", "フランス", "プロイセン", "ロシア", "アメリカ", "清", "日本", "オーストリア"]
H1913 = {"イギリス": 0.45, "フランス": 0.35, "プロイセン": 0.37, "ロシア": 0.16, "アメリカ": 0.11, "清": 0.08, "日本": 0.31, "オーストリア": 0.22}
WIN = [(y, y + 9) for y in range(1783, 1950, 10)]


def win_of(y):
    for a, b in WIN:
        if a <= y <= b: return (a, b)
    return None


seeds = {}
for fn in sorted(os.listdir(d)):
    m = re.match(r'fx(\d+)\.txt$', fn)
    if not m: continue
    N = []; W = []
    for ln in open(os.path.join(d, fn), encoding='utf-8'):
        if ln.startswith('N|'):
            p = ln.rstrip('\n').split('|')
            N.append(p)
        elif ln.startswith('W|'):
            W.append(ln.rstrip('\n').split('|'))
    seeds[int(m.group(1))] = (N, W)
S = sorted(seeds)
print(f'# {lab}：英の交易の計器 fx205・{len(S)}種（10年の窓ごとに種ごとの窓の平均→種の中央）')


def fl(x):
    try: return float(x)
    except: return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


# ---- ① 銀本位国の為替 ----
print('\n## ① 銀本位国の為替（法＝銀の国×四半期）と平価の元')
print('窓 | 銀の国×四半期の数（種の中央）| 下限 0.30 に張り付き（E≤0.3015）の割合 | E≤0.35 の割合 | E の中央 | 平価の元（銀の世界価格÷参照・N 行）| 統計÷参照（W）| 州の銀×為替の平均÷参照（W・クランプ前の近似）| 器A の量（同じ州の比÷参照の比）| 州の銀の値段（自国通貨）÷参照 | 統計がクランプの床（0.1）の四半期の割合')
for (a, b) in WIN:
    cnt = []; atf = []; lt35 = []; emed = []; rel = []; stat = []; lpe = []; rat = []; loc = []; clamp = []
    for s in S:
        N, W = seeds[s]
        es = [fl(p[6]) for p in N if a <= int(p[2]) <= b and p[5] == '1' and fl(p[24]) > 0]
        if es:
            cnt.append(len(es)); atf.append(sum(1 for e in es if e <= FLOOR * 1.005) / len(es)); lt35.append(sum(1 for e in es if e <= 0.35) / len(es)); emed.append(st.median(es))
        rs = [fl(p[10]) for p in N if a <= int(p[2]) <= b and p[5] == '1']
        if rs: rel.append(st.mean(rs))
        ws = [w for w in W if a <= int(w[2]) <= b]
        if ws:
            stat.append(st.mean(fl(w[3]) for w in ws)); lpe.append(st.mean(fl(w[4]) for w in ws)); rat.append(st.mean(fl(w[5]) for w in ws)); loc.append(st.mean(fl(w[7]) for w in ws))
            clamp.append(sum(1 for w in ws if fl(w[3]) <= 0.1001) / len(ws))
    print(f'{a}-{b} | {med(cnt):.0f} | {med(atf):.2f} | {med(lt35):.2f} | {med(emed):.3f} | {med(rel):.3f} | {med(stat):.3f} | {med(lpe):.3f} | {med(rat):.3f} | {med(loc):.3f} | {med(clamp):.2f}')


# ---- ②③ 英 ----
def nat_rows(N, name):
    return [p for p in N if p[4] == name]


def nat_windows(name):
    out = {}
    for (a, b) in WIN:
        R = defaultdict(list)
        for s in S:
            N, W = seeds[s]
            rows = [p for p in nat_rows(N, name) if a <= int(p[2]) <= b]
            if not rows: continue
            ex = sum(fl(p[25]) for p in rows); im = sum(fl(p[26]) for p in rows); imf = sum(fl(p[27]) for p in rows)
            imr = sum(fl(p[28]) for p in rows); iml = sum(fl(p[29]) for p in rows); exr = sum(fl(p[30]) for p in rows)
            frp = sum(fl(p[31]) for p in rows); fri = sum(fl(p[32]) for p in rows)
            gdpg = sum(fl(p[13]) * 3 * fl(p[6]) for p in rows)   # 四半期の名目GDP（月×3）×為替＝金建て
            R['exim'].append(ex / im if im > 1e-9 else float('nan'))
            R['tg'].append((ex + im) / gdpg if gdpg > 1e-9 else float('nan'))
            R['frin'].append(fri / gdpg if gdpg > 1e-9 else float('nan'))
            R['frpaid'].append(frp / gdpg if gdpg > 1e-9 else float('nan'))
            R['bal'].append((ex - im) / gdpg if gdpg > 1e-9 else float('nan'))
            R['cab'].append((ex - im + fri) / gdpg if gdpg > 1e-9 else float('nan'))
            R['anchor'].append((imf / imr) / (iml / imr) if imr > 1e-9 and iml > 1e-9 else float('nan'))
            R['sellrel'].append(imf / imr if imr > 1e-9 else float('nan'))
            R['buyrel'].append(iml / imr if imr > 1e-9 else float('nan'))
            R['exrel'].append(ex / exr if exr > 1e-9 else float('nan'))
            # 国境の台帳の動き（窓の最初と最後の四半期）
            rows_s = sorted(rows, key=lambda p: int(p[1]))
            if len(rows_s) >= 2:
                dctf = (fl(rows_s[-1][15]) - fl(rows_s[0][15])) * fl(rows_s[-1][6])
                R['dctf'].append(dctf / gdpg if gdpg > 1e-9 else float('nan'))
            last = rows_s[-1]; g12 = fl(last[13]) * 12
            R['abroad'].append(fl(last[16]) / g12 if g12 > 1e-9 else float('nan'))
            R['capin'].append(fl(last[17]) / g12 if g12 > 1e-9 else float('nan'))
            R['claims'].append(fl(last[18]) / g12 if g12 > 1e-9 else float('nan'))
            R['fdebt'].append(fl(last[19]) / g12 if g12 > 1e-9 else float('nan'))
            R['E'].append(st.mean(fl(p[6]) for p in rows))
            R['ship'].append(st.mean(fl(p[23]) for p in rows))
        out[(a, b)] = {k: med(v) for k, v in R.items()}
    return out


uk = nat_windows('イギリス')
print('\n## ② 英の交易（越境の成約を四半期に足した額＝金建て・÷金建ての名目GDP）')
print('窓 | 輸出÷輸入（FOB÷CIF）| 貿易/GDP | 貿易の収支÷GDP | 運賃の受け取り÷GDP | 払った運賃÷GDP | 貿易＋運賃の収支÷GDP | 国境の台帳の動き÷GDP（全ての国境の出入り）| 対外資産÷年GDP | 対外債権（国債）÷年GDP | 外資÷年GDP | 対外債務÷年GDP | 為替')
for w, v in uk.items():
    if not v: continue
    print(f"{w[0]}-{w[1]} | {v['exim']:.3f} | {v['tg'] * 100:.2f}% | {v['bal'] * 100:+.2f}% | {v['frin'] * 100:.2f}% | {v['frpaid'] * 100:.2f}% | {v['cab'] * 100:+.2f}% | {v.get('dctf', float('nan')) * 100:+.2f}% | {v['abroad']:.3f} | {v['claims']:.3f} | {v['capin']:.3f} | {v['fdebt']:.3f} | {v['E']:.3f}")
print('\n## ③ 英の輸入の売り手の金建ての錨（輸入の積出し価格÷参照）÷英の州の値段（買い手の金建ての州の価格÷参照）')
print('窓 | 売り手の錨÷参照 | 英の州の値段÷参照 | 比（錨÷英）| 英の輸出の積出し価格÷参照')
for w, v in uk.items():
    if not v: continue
    print(f"{w[0]}-{w[1]} | {v['sellrel']:.3f} | {v['buyrel']:.3f} | {v['anchor']:.3f} | {v['exrel']:.3f}")

print('\n## ④ 主要国の貿易/GDP（越境の成約を四半期に足した額・年換算÷年の名目GDP）・輸出÷輸入・運賃の受け取り÷GDP（10年の窓・種の中央）')
print('国 | ' + ' | '.join(f'{a}' for a, b in WIN))
allw = {n: nat_windows(n) for n in MAJ}
for n in MAJ:
    print(f'{n} 貿易/GDP | ' + ' | '.join(f"{allw[n][w].get('tg', float('nan')) * 100:.1f}" if allw[n].get(w) else '—' for w in WIN))
for n in MAJ:
    print(f'{n} 輸出÷輸入 | ' + ' | '.join(f"{allw[n][w].get('exim', float('nan')):.2f}" if allw[n].get(w) else '—' for w in WIN))
for n in MAJ:
    print(f'{n} 運賃÷GDP | ' + ' | '.join(f"{allw[n][w].get('frin', float('nan')) * 100:.2f}" if allw[n].get(w) else '—' for w in WIN))
for n in MAJ:
    print(f'{n} 為替 | ' + ' | '.join(f"{allw[n][w].get('E', float('nan')):.2f}" if allw[n].get(w) else '—' for w in WIN))
print('\n国ごと 1904-13（種の中央÷史実 1913・(輸出＋輸入)÷GDP）：' + '・'.join(f"{n} {allw[n][(1903, 1912)].get('tg', float('nan')) * 100:.2f}%÷{H1913[n] * 100:.0f}%＝{allw[n][(1903, 1912)].get('tg', float('nan')) / H1913[n]:.3f}" for n in MAJ if allw[n].get((1903, 1912))))
