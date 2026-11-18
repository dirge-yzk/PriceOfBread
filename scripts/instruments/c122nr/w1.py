"""(1216) c122nr 狙い W1 と記録の読み手（ideology_s・種 1〜10）。
W1＝プロイセンの国の民族主義の取り分（picked の四半期の行・1848〜1871 の平均・種の中央）の 腕−基線 が +0.05 以上なら 通る（旗 0 は +0＝落ちる）。
記録＝同じ行の自由主義・ファシズム・自由主義＋民族主義・他の国（picked は四半期の行・それ以外は年末の行）・1800/1830/1900/1930 の帯。
使い方: python w1.py <基線の生> <腕の生>"""
import sys, os, statistics as st
NATS = ['プロイセン', 'オーストリア', 'イギリス', 'フランス', 'ロシア', 'オスマン', '清', '日本', 'アメリカ',
        'サルデーニャ＝ピエモンテ', 'ヴュルテンベルク', 'バイエルン', 'ザクセン', '教皇領', 'ポーランド', 'スペイン']
COLS = [('nat', 'ideoNationalNationalismShare'), ('lib', 'ideoNationalLiberalismShare'), ('fas', 'ideoNationalFascismShare')]
SPANS = [(1784, 1800), (1801, 1830), (1848, 1871), (1872, 1900), (1901, 1930), (1931, 1950)]

def read(raw):
    out = {}   # (nation, span) -> col -> [種ごとの平均]
    for s in range(1, 11):
        p = os.path.join(raw, f'ideology_s{s}.tsv')
        if not os.path.exists(p): return None
        acc = {}
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in fh:
                c = line.rstrip('\n').split('\t')
                n = c[ix['nation']]
                if n not in NATS: continue
                y = int(c[ix['year']])
                for sp in SPANS:
                    if sp[0] <= y <= sp[1]:
                        a = acc.setdefault((n, sp), {k: [] for k, _ in COLS})
                        for k, col in COLS: a[k].append(float(c[ix[col]]))
        for key, a in acc.items():
            d = out.setdefault(key, {k: [] for k, _ in COLS}); d.setdefault('rows', [])
            for k, _ in COLS: d[k].append(sum(a[k]) / len(a[k]))
            d['rows'].append(len(a['nat']))
    return out

base, arm = read(sys.argv[1]), read(sys.argv[2])
if base is None or arm is None: print('未了：ideology_s が欠けている'); sys.exit(2)
med = lambda v: st.median(v) if v else float('nan')
print('# (1216) w1.py 基線', sys.argv[1], '腕', sys.argv[2])
print('nation\tspan\trowsPerSeed\tnat_base\tnat_arm\tdNat\tlib_base\tlib_arm\tdLib\tfas_base\tfas_arm\tdNat+dLib')
for n in NATS:
    for sp in SPANS:
        b, a = base.get((n, sp)), arm.get((n, sp))
        if not b or not a: continue
        nb, na, lb, la, fb, fa = med(b['nat']), med(a['nat']), med(b['lib']), med(a['lib']), med(b['fas']), med(a['fas'])
        print(f"{n}\t{sp[0]}-{sp[1]}\t{med(a['rows']):.0f}\t{nb:.4f}\t{na:.4f}\t{na-nb:+.4f}\t{lb:.4f}\t{la:.4f}\t{la-lb:+.4f}\t{fb:.4f}\t{fa:.4f}\t{(na-nb)+(la-lb):+.4f}")
b, a = base[('プロイセン', (1848, 1871))], arm[('プロイセン', (1848, 1871))]
d = med(a['nat']) - med(b['nat'])
print(f"W1 プロイセン 1848-1871 民族主義（四半期の行 {med(a['rows']):.0f}/種）: 基線 {med(b['nat']):.4f} 腕 {med(a['nat']):.4f} 差 {d:+.4f} 床 +0.05 → {'通る' if d >= 0.05 else '落ちる'}")
