"""(184) 農奴解放の経路の読み手（読むだけ）＝inj_serf184 の Q 行と E 行。
  ① 主要10か国の解放の年（限定・全面・種ごと）｜全面の種の中央（種の数）｜史実
  ② 段が動いた出来事の経路（目標を置いた者＝AI の飢饉／AI の余地／危機の要求〔一揆・運動〕／勢力の要求の直接の施行・施行の時の純支持と代価と残り）
  ③ 動かない種の理由（10年の窓ごとの国×四半期の割合＝AI の条件〔飢饉・余地〕が立つ・門が開く・代価が政治資本の最大に届かない・目標が現在より上）
  ④ 純支持・代価・政治資本の残り・ブロックの力の10年の窓の中央（全面になっていない国×四半期）
python an_serf184.py <serf184 の dir> [--seeds N] [--all]"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 99; allnat = False
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
if '--all' in args:
    args.remove('--all'); allnat = True
D = args[0]
WATCH = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
SERF = {'プロイセン': '1807〔十月勅令〕〜1850〔償却法〕', 'ロシア': '1861', 'オーストリア': '1781〔人格的隷属〕〜1848〔賦役〕', '日本': '1871〔廃藩置県〕〜1876〔秩禄処分〕', 'オスマン': '1839〔ギュルハネ勅令〕',
        'フランス': '1789〔8月4日〕〜1793', 'スペイン': '1811〔カディス〕〜1837', '清': '—（農奴制なし【推】）', 'イギリス': '開幕', 'アメリカ': '開幕'}
DEC = [(y, y + 9) for y in range(1784, 1944, 10)] + [(1944, 1950)]


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


Q = collections.defaultdict(list)   # (n, sd) -> rows
E = collections.defaultdict(list)   # (n, sd) -> events
for f in sorted(glob.glob(os.path.join(D, 's*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('\t')
        if t[0] == 'Q':
            Q[(t[3], sd)].append(dict(q=int(t[1]), y=int(t[2]), cur=int(t[4]), tgt=int(t[5]), pol=t[6], food=fl(t[7]), fam=int(t[8]), rc=fl(t[9]), rn=fl(t[10]),
                                      net=fl(t[11]), gate=int(t[12]), cost=fl(t[13]), rem=fl(t[14]), leg=fl(t[15]), pt=fl(t[16]), pm=fl(t[17]), ps=fl(t[18]),
                                      rupt=int(t[19]), war=int(t[20]), sec=fl(t[21]), wx=fl(t[22]), fund=fl(t[23]), debt=fl(t[24])))
        elif t[0] == 'E':
            E[(t[2], sd)].append(dict(y=int(t[1]), k=t[3], a=t[4:]))
SEEDS = sorted({sd for (_, sd) in Q})
nats = WATCH if not allnat else sorted({n for (n, _) in Q})


def med(x):
    x = [v for v in x if v == v]
    return st.median(x) if x else float('nan')


print(f'# 種 {SEEDS}')
print('\n## ① 主要10か国の解放の年（全面〔限定〕・種ごと・開幕から全面＝開）｜全面の種の中央（種の数）｜史実')
for n in nats:
    cells = []; fulls = []; lims = []
    for sd in SEEDS:
        v = sorted(Q.get((n, sd), []), key=lambda r: r['q'])
        if not v: cells.append('-'); continue
        if v[0]['cur'] == 2: cells.append('開'); continue
        yf = next((r['y'] for r in v if r['cur'] == 2), None)
        yl = next((r['y'] for r in v if r['cur'] >= 1), None) if v[0]['cur'] == 0 else None
        if yf: fulls.append(yf)
        if yl: lims.append(yl)
        cells.append((str(yf) if yf else '—') + (f'〔{yl}〕' if yl else ''))
    print(f'  {n}: ' + ' '.join(cells) + f'｜全面 中央 {med(fulls) if fulls else "—"}（{len(fulls)}）・限定 中央 {med(lims) if lims else "—"}（{len(lims)}）｜史実 {SERF.get(n, "")}')

print('\n## ② 段が動いた出来事（国・種・年・前→後・経路）＝目標を置いた直前の出来事（A＝AI〔飢饉1/余地〕・C＝危機の要求〔一揆の記録/運動〕・R＝一揆の譲歩の梯子）と施行（G＝純支持・代価・残り・政体・正統性／F＝勢力の要求の直接の施行）')
kinds = collections.Counter()
for n in nats:
    for sd in SEEDS:
        ev = E.get((n, sd), [])
        for i, e in enumerate(ev):
            if e['k'] not in ('G', 'F'): continue
            prior = [x for x in ev[:i] if x['k'] in ('A', 'C', 'R') and x['y'] >= e['y'] - 30]
            src = prior[-1] if prior else None
            srcs = (f"{src['k']}{src['y']}:" + ','.join(src['a'])) if src else '（目標の出来事なし）'
            if e['k'] == 'G':
                b, a, net, cost, rem, pol, leg = e['a']
                print(f'  {n} s{sd} {e["y"]} {b}→{a} 施行 純支持{float(net):+.2f} 代価{float(cost):.3f} 残り{float(rem):.3f} {pol} 正統{float(leg):.2f}｜目標 {srcs}')
            else:
                print(f'  {n} s{sd} {e["y"]} {e["a"][0]}→{e["a"][1]} 勢力の要求をのむ（直接）｜目標 {srcs}')
            kinds[(e['k'], src['k'] if src else '-', (src['a'][0] if src and src['k'] == 'A' else ''))] += 1
print('  経路の数（施行の種類, 目標の出来事, AI なら飢饉1/余地0）: ' + ', '.join(f'{k}={v}' for k, v in sorted(kinds.items())))

print('\n## ②b 全ての国の経路の数（施行 G/F × 目標の出来事）')
kinds2 = collections.Counter(); first = collections.Counter()
for (n, sd), ev in E.items():
    for i, e in enumerate(ev):
        if e['k'] not in ('G', 'F'): continue
        prior = [x for x in ev[:i] if x['k'] in ('A', 'C', 'R') and x['y'] >= e['y'] - 30]
        src = prior[-1] if prior else None
        kinds2[(e['k'], src['k'] if src else '-', (src['a'][0] if src and src['k'] == 'A' else ''))] += 1
        dec = (e['y'] - 1784) // 20 * 20 + 1784
        first[dec] += 1
print('  ' + ', '.join(f'{k}={v}' for k, v in sorted(kinds2.items())))
print('  20年の窓ごとの段の動き（全ての国）: ' + ' '.join(f'{d}:{first[d]}' for d in sorted(first)))
pk = collections.Counter(e['a'][0] + '→' + e['a'][1] for ev in E.values() for e in ev if e['k'] == 'P')
print('  政体の変更（全ての国・全ての種）: ' + ', '.join(f'{k} {v}' for k, v in pk.most_common(12)))

print('\n## ③ 全面でない国×四半期の割合%（10年の窓）＝飢饉｜余地(今)>0.05｜門が開く｜代価＞政治資本の最大の推定（残りが代価に届かない）｜目標＞現在')
for n in nats:
    rows = [r for sd in SEEDS for r in Q.get((n, sd), []) if r['cur'] < 2]
    if not rows: continue
    print(f'  {n}（国×四半期 {len(rows)}）')
    out = []
    for y0, y1 in DEC:
        v = [r for r in rows if y0 <= r['y'] <= y1]
        if not v: out.append('-'); continue
        k = len(v)
        f = 100 * sum(r['fam'] for r in v) / k; rr = 100 * sum(r['rc'] > 0.05 for r in v) / k
        g = 100 * sum(r['gate'] == 1 for r in v) / k; lack = 100 * sum((r['cost'] > r['rem']) for r in v if r['cost'] == r['cost']) / k
        tg = 100 * sum(r['tgt'] > r['cur'] for r in v) / k
        out.append(f'{y0}:{f:.0f}/{rr:.0f}/{g:.0f}/{lack:.0f}/{tg:.0f}')
    for i in range(0, len(out), 6): print('    ' + '  '.join(out[i:i + 6]))

print('\n## ④ 全面でない国×四半期の中央（10年の窓）＝純支持｜代価｜残り｜正統性｜力 t/m/s｜安全保障の不安の割合｜段（中央）')
for n in nats:
    rows = [r for sd in SEEDS for r in Q.get((n, sd), []) if r['cur'] < 2]
    if not rows: continue
    print(f'  {n}')
    out = []
    for y0, y1 in DEC:
        v = [r for r in rows if y0 <= r['y'] <= y1]
        if not v: out.append('-'); continue
        out.append(f"{y0}:{med([r['net'] for r in v]):+.2f}|{med([r['cost'] for r in v]):.2f}|{med([r['rem'] for r in v]):.2f}|{med([r['leg'] for r in v]):.2f}|"
                   f"{med([r['pt'] for r in v]):.2f}/{med([r['pm'] for r in v]):.2f}/{med([r['ps'] for r in v]):.2f}|{med([r['sec'] for r in v]):.2f}")
    for i in range(0, len(out), 4): print('    ' + '  '.join(out[i:i + 4]))
