"""(176) 軍事費の段の施行と月初の借入の読み手（読むだけ）＝inj_war176 の g<seed>.txt（G 行）と inj_war171 の q<seed>.txt（Q 行）。
  ①交戦×総動員の国×四半期：施行の前の値 x0 の分布・目標（門を通った目標 tgt）・段4（x≧0.875）に居る割合・Move の速さ・純支持・抵抗・在庫で止まった割合・押しの予算
  ②総動員の連続（交戦かつ総動員の四半期の続き）ごとの長さと、その中の x の最大・段4 に着いたか・着くまでの四半期
  ③月初の借入（交戦の四半期 vs 平時）＝払えない額があった月の割合・借りた額÷払えない額・門で止まった月（冷却・利払いの限界・助走）・利払い÷歳入
  ④器の請求と r（Q 行・交戦×総動員 vs 平時）
python an_stage176.py <out_dir（w176・w171 を含む）> [ラベル] [--seeds N]"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]; maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; LAB = args[1] if len(args) > 1 else D
ERAS = [(1784, 1849), (1850, 1913), (1914, 1950)]
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')


def q4(x):
    x = sorted(y for y in x if y == y)
    if not x: return 'nan'
    return f'{x[len(x) // 4]:.3f}〜{x[(3 * len(x)) // 4]:.3f}'


def era(y):
    for a, b in ERAS:
        if a <= y <= b: return f'{a}-{b}'
    return None


G = []
for f in sorted(glob.glob(os.path.join(D, 'w176', 'g*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'G': continue
        v = [fl(x) for x in t[5:]]
        G.append(dict(sd=sd, q=int(t[1]), yr=int(t[2]), i=int(t[3]), n=t[4], rec=v[0], war=v[1] > 0, mob=v[2] > 0, x0=v[3], raw=v[4], tgt=v[5], x1=v[6],
                      mc=v[7], net=v[8], res=v[9], spd=v[10], blk=v[11], push=v[12], months=v[13], shortM=v[14], shortS=v[15], bor=v[16],
                      coolM=v[17], warmM=v[18], capM=v[19], irS=v[20], dev=v[21]))
seeds = sorted({g['sd'] for g in G})
print(f'######## {LAB}（種 {len(seeds)}・国×四半期 {len(G)}）')
print('\n# ① 軍事費の段の施行（四半期の頭の Move・stageRec=1 の行＝政治の施行が走った四半期）')
print('時代 | 区分 | 国×四半期 | x0 の中央〔四分位〕 | 目標=1.0 の割合 | 段4(x0≧0.875) の割合 | 速さの中央 | 抵抗の中央 | 押しの予算の中央 | 純支持<0 | 在庫で止まった | 動かない(目標=現状)')
for e in ERAS:
    for lab, cond in (('交戦×総動員', lambda g: g['war'] and g['mob']), ('交戦・総動員なし', lambda g: g['war'] and not g['mob']), ('平時', lambda g: not g['war'])):
        rs = [g for g in G if g['rec'] > 0 and e[0] <= g['yr'] <= e[1] and cond(g)]
        if not rs: continue
        mv = [g for g in rs if g['mc'] > 0]
        print(f"{e[0]}-{e[1]} | {lab} | {len(rs)} | {med([g['x0'] for g in rs]):.3f}〔{q4([g['x0'] for g in rs])}〕 | {sum(1 for g in rs if g['tgt'] >= 0.999) / len(rs):.2f} | "
              f"{sum(1 for g in rs if g['x0'] >= 0.875) / len(rs):.3f} | {med([g['spd'] for g in mv]):.4f} | {med([g['res'] for g in mv]):.3f} | {med([g['push'] for g in rs]):.3f} | "
              f"{sum(1 for g in mv if g['net'] < 0) / max(1, len(mv)):.2f} | {sum(1 for g in mv if g['blk'] > 0) / max(1, len(mv)):.2f} | {sum(1 for g in rs if g['mc'] == 0) / len(rs):.2f}")
# ② 総動員の連続
print('\n# ② 総動員の連続（交戦かつ総動員の四半期の続き・国ごと）＝長さ・x の最大・段4 に着いた割合・着くまでの四半期')
by = collections.defaultdict(list)
for g in G:
    if g['rec'] > 0: by[(g['sd'], g['i'])].append(g)
spells = []
for k, gs in by.items():
    gs.sort(key=lambda g: g['q']); cur = []
    for g in gs + [None]:
        if g is not None and g['war'] and g['mob'] and (not cur or g['q'] == cur[-1]['q'] + 1): cur.append(g); continue
        if cur:
            xs = [c['x1'] for c in cur]; hit = next((j + 1 for j, x in enumerate(xs) if x >= 0.875), None)
            spells.append(dict(yr=cur[0]['yr'], n=cur[0]['n'], L=len(cur), x0=cur[0]['x0'], xmax=max(xs), hit=hit))
        cur = [g] if (g is not None and g['war'] and g['mob']) else []
for e in ERAS:
    ss = [s for s in spells if e[0] <= s['yr'] <= e[1]]
    if not ss: continue
    print(f"{e[0]}-{e[1]} | 連続 {len(ss)} | 長さの中央 {med([s['L'] for s in ss]):.0f}〔{q4([s['L'] for s in ss])}〕四半期 | 始まりの x {med([s['x0'] for s in ss]):.3f} | x の最大の中央 {med([s['xmax'] for s in ss]):.3f} | "
          f"段4 に着いた {sum(1 for s in ss if s['hit']) / len(ss):.2f} | 着くまでの中央 {med([s['hit'] for s in ss if s['hit']]):.0f} 四半期 | 4四半期以上の連続 {sum(1 for s in ss if s['L'] >= 4)}")
# ③ 月初の借入
print('\n# ③ 月初の借入（Fiscal.Borrow の前後・月の和）')
print('時代 | 区分 | 国×四半期 | 払えない月の割合 | 借りた÷払えない額 | 冷却中の月 | 利払いの限界の月 | 助走の月 | 利払い÷歳入の中央 | 切り下げ÷払えない額')
for e in ERAS:
    for lab, cond in (('交戦×総動員', lambda g: g['war'] and g['mob']), ('交戦・総動員なし', lambda g: g['war'] and not g['mob']), ('平時', lambda g: not g['war'])):
        rs = [g for g in G if g['months'] > 0 and e[0] <= g['yr'] <= e[1] and cond(g)]
        if not rs: continue
        M = sum(g['months'] for g in rs); S = sum(g['shortS'] for g in rs)
        print(f"{e[0]}-{e[1]} | {lab} | {len(rs)} | {sum(g['shortM'] for g in rs) / M:.3f} | {sum(g['bor'] for g in rs) / S if S > 0 else float('nan'):.3f} | "
              f"{sum(g['coolM'] for g in rs) / M:.3f} | {sum(g['capM'] for g in rs) / M:.3f} | {sum(g['warmM'] for g in rs) / M:.3f} | {med([g['irS'] / g['months'] for g in rs]):.3f} | "
              f"{sum(g['dev'] for g in rs) / S if S > 0 else float('nan'):.3f}")
# ④ 器の請求と r（Q 行）
Q = []
for f in glob.glob(os.path.join(D, 'w171', 'q*.txt')):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'Q': continue
        v = [fl(x) for x in t[5:]]
        if not (v[0] > 0): continue
        Q.append(dict(yr=int(t[2]), n=t[4], m=v[0], env=v[1], payFull=v[2], equipRoom=v[5], equipClaim=v[6], need=v[7], claims=v[10], payable=v[11], r=v[12] / v[0],
                      eqFill=v[13] / v[0], warM=v[16], mobM=v[18], stage=v[19] / v[0], rev=v[20]))
print('\n# ④ 器と請求と r（Q 行の国×四半期の中央）')
print('時代 | 区分 | 国×四半期 | 段 | 器÷歳入 | 請求÷払える額 | r | 装備の上限÷必要額 | 装備の充足')
for e in ERAS:
    for lab, cond in (('交戦×総動員', lambda q: q['warM'] > 0 and q['mobM'] > 0), ('交戦・総動員なし', lambda q: q['warM'] > 0 and q['mobM'] == 0), ('平時', lambda q: q['warM'] == 0)):
        rs = [q for q in Q if e[0] <= q['yr'] <= e[1] and cond(q)]
        if not rs: continue
        print(f"{e[0]}-{e[1]} | {lab} | {len(rs)} | {med([q['stage'] for q in rs]):.3f} | {med([q['env'] / q['rev'] for q in rs if q['rev'] > 0]):.3f} | "
              f"{med([q['claims'] / q['payable'] for q in rs if q['payable'] > 0]):.3f} | {med([q['r'] for q in rs]):.3f} | {med([q['equipRoom'] / q['need'] for q in rs if q['need'] > 0]):.3f} | {med([q['eqFill'] for q in rs]):.3f}")
