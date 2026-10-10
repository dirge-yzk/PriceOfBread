"""(181) 英（など）の財政の軌道と不履行の前の道筋（読むだけ）＝F 行（v2＝兌換・クーポン・金利・金本位の月・国債・期待インフレ・議会の統制）と D 行・hist の A 行（年GDP）。
python an_uk181.py <out_dir> [ラベル] [国名]"""
import sys, os, glob, statistics as st, collections
D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D; NAT = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')


A = {}
for f in glob.glob(os.path.join(D, 'hist', 'a*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'A' and t[4] == NAT: A[(sd, int(t[1]))] = fl(t[6])
S = collections.defaultdict(list); DEF = []
for f in sorted(glob.glob(os.path.join(D, 'f181', 'f*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == 'F' and t[4] == NAT:
            v = [fl(x) for x in t[5:]]
            if len(v) < 36 or not (v[0] > 0): continue
            q, yr = int(t[1]), int(t[2]); g = A.get((sd, q), float('nan')); m = v[0]
            S[sd].append(dict(q=q, yr=yr, debt=v[32] / g if g > 0 else float('nan'), coupon=v[29] / m, rate=v[30] / m, conv=v[28] / m, gold=v[31] / m,
                              ir=v[8] / v[4] if v[4] > 0 else float('nan'), rev=v[4] * 4 / g if g > 0 else float('nan'), dev=v[6] * 4 / g if g > 0 else float('nan'),
                              bor=v[5] * 4 / g if g > 0 else float('nan'), war=v[35] / m, ins=v[25], df=v[26], cap=v[27] / m, r=v[7] / m, infl=v[33] / m, parl=v[34] / m,
                              mil=v[11] * 4 / g if g > 0 else float('nan')))
        elif t[0] == 'D' and t[4] == NAT:
            DEF.append((sd, int(t[1]), int(t[3]), [fl(x) for x in t[5:]]))
print(f'######## {LAB}＝{NAT} の財政の軌道（種 {len(S)}・10年の窓・種ごとの窓の中央→種の中央）')
print('窓 | 国債÷年GDP（窓の終わり） | 平均クーポン | 市場の金利 | 兌換 | 金本位の月の割合 | 利払い÷歳入 | 歳入÷年GDP | 切り下げ÷年GDP | 借入÷年GDP | 軍の請求÷年GDP | 交戦の月 | r | 利払いの限界の月 | 期待インフレ | 議会の統制')
for y0 in range(1783, 1950, 10):
    y1 = y0 + 9 if y0 < 1943 else 1950
    rows = collections.defaultdict(list)
    for sd, xs in S.items():
        w = [x for x in xs if y0 <= x['yr'] <= y1]
        if not w: continue
        rows['debt'].append(w[-1]['debt'])
        for k in ('coupon', 'rate', 'conv', 'gold', 'ir', 'rev', 'dev', 'bor', 'mil', 'war', 'r', 'cap', 'infl', 'parl'):
            rows[k].append(med([x[k] for x in w]) if k not in ('dev', 'bor', 'war', 'cap', 'gold') else sum(x[k] for x in w) / len(w))
    if not rows: continue
    print(f"{y0}-{y1} | {med(rows['debt']):.2f} | {med(rows['coupon']):.3f} | {med(rows['rate']):.3f} | {med(rows['conv']):.2f} | {med(rows['gold']):.2f} | {med(rows['ir']):.2f} | {med(rows['rev']):.3f} | {med(rows['dev']):.4f} | {med(rows['bor']):.4f} | {med(rows['mil']):.3f} | {med(rows['war']):.2f} | {med(rows['r']):.3f} | {med(rows['cap']):.2f} | {med(rows['infl']):.3f} | {med(rows['parl']):.2f}")
print(f'\n不履行 {len(DEF)} 件（種 {len(set(d[0] for d in DEF))}）＝不履行の前の道筋：最後に金本位だった年・その時の国債÷年GDP・10年前と5年前と直前のクーポンと利払い÷歳入・直前8か月の r・金利の内訳（基礎・債務・期待インフレ・政策金利×兌換の割引×議会の割引）')
for sd, q, yr, v in sorted(DEF):
    xs = S.get(sd, [])
    before = [x for x in xs if x['q'] <= q]
    lastGold = next((x for x in reversed(before) if x['gold'] > 0.5), None)
    def at(dy):
        c = [x for x in before if x['yr'] <= yr - dy]
        return c[-1] if c else None
    b10, b5, b0 = at(10), at(5), (before[-1] if before else None)
    f = lambda x, k, fmt: (format(x[k], fmt) if x else '—')
    extra = v[25:35] if len(v) >= 35 else []
    comp = ''
    if extra:
        infl, parl, base, dterm, fisher, pol, convd, reprd, onGold, dcount = extra
        comp = f' ｜ 金利の内訳 基礎 {base:.3f}＋債務 {dterm:.3f}＋期待インフレ {fisher:.3f}＋政策 {pol:.3f}（切り下げの信認は月による）×兌換 {convd:.2f}×議会 {reprd:.2f}・これまでの不履行 {int(dcount)}'
    print(f"種{sd} {yr} | 最後の金本位 {lastGold['yr'] if lastGold else 'なし'}（国債 {f(lastGold, 'debt', '.2f')}） | 10年前 クーポン {f(b10, 'coupon', '.3f')}・利払い÷歳入 {f(b10, 'ir', '.2f')}・国債 {f(b10, 'debt', '.2f')} | 5年前 {f(b5, 'coupon', '.3f')}・{f(b5, 'ir', '.2f')}・{f(b5, 'debt', '.2f')} | 直前 {f(b0, 'coupon', '.3f')}・{f(b0, 'ir', '.2f')}・{f(b0, 'debt', '.2f')} | r {v[16]:.4f}・切り下げ÷請求 {v[15] / v[12] if v[12] > 0 else float('nan'):.3f}{comp}")
