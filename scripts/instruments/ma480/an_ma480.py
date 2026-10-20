#!/usr/bin/env python
# (480) 線 ma 後半の読み手＝事前登録 measurements/ma480_20261021_prereg.txt [8]（G0〜G7）と追記 3（G6b 類別・G8・絶対数）を腕 B の計器の生から読む（読むだけ）。
# 使い方：python an_ma480.py <B の ins のフォルダ> <基準 m475_ins のフォルダ> [種の数=10]
#   例）python an_ma480.py D:/PoB_work/ma_480/out_B_10/ins D:/PoB_work/keep_475/m475_ins
# 失業の定義は 1 本＝labor_s の plainUnemploymentShare。縛る行＝enactedNearFloorShare>0.5。施行行＝lawCode>0。
import csv, sys, collections, io, statistics, math
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
csv.field_size_limit(1 << 30)
B = sys.argv[1].rstrip('/') + '/'
A = sys.argv[2].rstrip('/') + '/'
N = int(sys.argv[3]) if len(sys.argv) > 3 else 10

def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def rd(path):
    return csv.DictReader(open(path, encoding='utf-8'), delimiter='\t')
def med(v):
    v = [x for x in v if x == x]
    return statistics.median(v) if v else float('nan')
def mean(v):
    v = [x for x in v if x == x]
    return sum(v) / len(v) if v else float('nan')
def ratio(a, b): return a / b if b else float('nan')

def labor(d, s):
    rows = []
    for r in rd(d + f'labor_s{s}.tsv'):
        rows.append(dict(q=int(r['quarter']), y=int(r['year']), n=r['nation'], law=int(r['lawCode']),
                         u=fl(r['plainUnemploymentShare']), fu=fl(r['farmUnemploymentShare']), near=fl(r['enactedNearFloorShare']),
                         pmin=fl(r['plainRealPriceMinIndex']), tiny=fl(r['plainTinyPriceRegionShare'])))
    return rows

def lstats(rows):
    enc = [r for r in rows if r['law'] > 0]
    bind = [r for r in enc if r['near'] > 0.5]
    first = {}; down = 0; maxlv = 0
    byn = collections.defaultdict(list)
    for r in rows: byn[r['n']].append(r)
    for n, l in byn.items():
        l.sort(key=lambda r: r['q']); prev = 0
        for r in l:
            if r['law'] < prev: down += 1
            if r['law'] > 0 and n not in first: first[n] = r['y']
            prev = r['law']; maxlv = max(maxlv, r['law'])
    return dict(rows=rows, enc=enc, bind=bind, first=first, down=down, maxlv=maxlv)

tot = collections.Counter()
print('=== labor_s（G0〜G3・全ての州）===')
lstA, lstB = {}, {}
for s in range(1, N + 1):
    a = lstats(labor(A, s)); b = lstats(labor(B, s)); lstA[s] = a; lstB[s] = b
    for tag, st in (('A', a), ('B', b)):
        bu = sum(1 for r in st['bind'] if r['u'] > 0.5); eu = sum(1 for r in st['enc'] if r['u'] > 0.5)
        fu = sum(1 for r in st['enc'] if r['fu'] > 0.5)
        tot[tag + 'bind'] += len(st['bind']); tot[tag + 'bindU'] += bu
        tot[tag + 'enc'] += len(st['enc']); tot[tag + 'encU'] += eu; tot[tag + 'encFU'] += fu
        tot[tag + 'nat'] += len(st['first']); tot[tag + 'down'] += st['down']
        tot[tag + 'pre1800'] += sum(1 for y in st['first'].values() if y < 1800)
        tot[tag + 'seedsGE5'] += 1 if len(st['first']) >= 5 else 0
        tot[tag + 'pminLow'] += sum(1 for r in st['rows'] if r['pmin'] == r['pmin'] and r['pmin'] < 1e-3)
        tot[tag + 'rows'] += len(st['rows'])
        tot[tag + 'tinySum'] += sum(r['tiny'] for r in st['rows'] if r['tiny'] == r['tiny'])
        tot[tag + 'tinyN'] += sum(1 for r in st['rows'] if r['tiny'] == r['tiny'])
    # 両腕で施行した国に絞った施行行の失業>0.5
    both = set(a['first']) & set(b['first'])
    ea = [r for r in a['enc'] if r['n'] in both]; eb = [r for r in b['enc'] if r['n'] in both]
    tot['bothA_rows'] += len(ea); tot['bothA_U'] += sum(1 for r in ea if r['u'] > 0.5)
    tot['bothB_rows'] += len(eb); tot['bothB_U'] += sum(1 for r in eb if r['u'] > 0.5)
    print(f"seed{s}: 施行した国 A {len(a['first'])} B {len(b['first'])}（両方 {len(both)}）／施行行 A {len(a['enc'])} B {len(b['enc'])}／縛る行 A {len(a['bind'])} B {len(b['bind'])}／最高の段 A {a['maxlv']} B {b['maxlv']}／法の下がり A {a['down']} B {b['down']}／1800 年より前の施行 A {sum(1 for y in a['first'].values() if y < 1800)} B {sum(1 for y in b['first'].values() if y < 1800)}")
for tag, name in (('A', '基準 m475'), ('B', '腕 B')):
    t = tot
    print(f"{name}: 縛る行 {t[tag+'bind']}（G0 {'成立' if t[tag+'bind'] >= 50 else '不成立'}）／縛る行の失業>0.5 {t[tag+'bindU']}（比 {ratio(t[tag+'bindU'], t[tag+'bind']):.3f}）／施行行 {t[tag+'enc']}／施行行の失業>0.5 {t[tag+'encU']}（比 {ratio(t[tag+'encU'], t[tag+'enc']):.3f}）／施行行の農業失業>0.5 {t[tag+'encFU']}（比 {ratio(t[tag+'encFU'], t[tag+'enc']):.3f}）")
    print(f"   施行した国の合計 {t[tag+'nat']}／5 国以上の種 {t[tag+'seedsGE5']}／1800 年より前の施行 {t[tag+'pre1800']}／法の下がり（全国・10 種）{t[tag+'down']}")
    print(f"   止める線②の材料：plainRealPriceMinIndex<1e-3 の行の割合 {ratio(t[tag+'pminLow'], t[tag+'rows']):.5f}／plainTinyPriceRegionShare の平均 {ratio(t[tag+'tinySum'], t[tag+'tinyN']):.5f}")
print(f"両腕で施行した国に絞った施行行の失業>0.5：A {tot['bothA_U']}/{tot['bothA_rows']}={ratio(tot['bothA_U'], tot['bothA_rows']):.3f}  B {tot['bothB_U']}/{tot['bothB_rows']}={ratio(tot['bothB_U'], tot['bothB_rows']):.3f}")
g1 = ratio(tot['Bbind' + 'U'], tot['Bbind']); g2 = ratio(tot['BencU'], tot['Benc'])
def g1v():
    if tot['Bbind'] < 50: return '外れ（G0 不成立＝判定不能）'
    if g1 <= 0.50: v = '当たり'
    elif g1 <= 0.70: v = '部分'
    else: v = '外れ'
    if v == '当たり' and tot['BbindU'] > tot['AbindU']: v = '部分（比は当たりでも絶対数が m475 の 443 より増えた）'
    return v
def g2v():
    if g2 <= 0.097: v = '当たり'
    elif g2 <= 0.15: v = '部分'
    else: v = '外れ'
    if v == '当たり' and tot['BencU'] > tot['AencU']: v = '部分（比は当たりでも絶対数が m475 の 598 より増えた）'
    return v
print(f"G0 縛る行の合計 B {tot['Bbind']}（≧50）／G1 {g1:.3f}＝{g1v()}／G2 {g2:.3f}＝{g2v()}")
g3 = tot['BseedsGE5']
print(f"G3 5 国以上の種 {g3}/10＝{'当たり' if g3 >= 7 else ('部分' if g3 >= 4 else '外れ')}（床であって機構の証拠ではない）／施行した国の中央（種ごと）{med([len(lstB[s]['first']) for s in lstB])}")

print('\n=== ma_s（G4〜G7・G6b・挙げた 8 国）===')
cnt = collections.Counter(); agree = collections.Counter(); wrong = collections.Counter()
seedsDown = seedsUp = 0; osc = {}; holdShareRows = collections.Counter()
capDiff = collections.Counter()
lawNow = collections.Counter()
for s in range(1, N + 1):
    rows = list(rd(B + f'ma_s{s}.tsv'))
    byn = collections.defaultdict(list)
    for r in rows: byn[r['nation']].append(r)
    sd = su = 0; oscN = 0
    for n, l in byn.items():
        l.sort(key=lambda r: int(r['quarter']))
        prev = None; lastSign = 0; lastQ = -99; sw = 0
        for r in l:
            q = int(r['quarter'])
            if prev is not None and int(prev['quarter']) == q - 1:
                t = int(r['ruleTargetOrd']); pl = int(prev['lawCode']); wn = int(r['wantNew'])
                if t < 0: c = 'none'
                else: c = 'up' if t > pl else ('hold' if t == pl else 'down')
                cnt[c] += 1
                if wn == t: agree[c] += 1
                elif c == 'hold': wrong['hold→' + ('上げ' if wn > t else ('下げ' if 0 <= wn < t else '書かない'))] += 1
                if c == 'down': sd = 1
                if c == 'up': su = 1
                if c in ('up', 'down'):
                    sg = 1 if c == 'up' else -1
                    if lastSign and sg != lastSign and q - lastQ <= 4: sw += 1
                    lastSign = sg; lastQ = q
                bc = fl(r['bindCur']); bcc = fl(r.get('bindCurCapped', 'nan'))
                if bc == bc and bcc == bcc:
                    capDiff['n'] += 1
                    if abs(bc - bcc) > 1e-9: capDiff['differ'] += 1
            prev = r
        if sw >= 2: oscN += 1
    seedsDown += sd; seedsUp += su; osc[s] = oscN
nrow = sum(cnt.values())
print('類別（前の四半期の lawCode と ruleTargetOrd）：' + '／'.join(f"{c} {cnt[c]}（一致 {agree[c]}＝{ratio(agree[c], cnt[c]):.3f}）" for c in ('up', 'hold', 'down', 'none')))
print(f"現状維持の不一致の向き：{dict(wrong)}／現状維持の行の割合 {ratio(cnt['hold'], nrow):.3f}")
def g6v(c, line):
    if cnt[c] < 50: return '判定不能（50 行未満）'
    return '当たり' if ratio(agree[c], cnt[c]) >= line else '外れ'
print(f"G6b 上げ {g6v('up', 0.90)}（線 0.90）／現状維持 {g6v('hold', 0.70)}（線 0.70）／下げ {g6v('down', 1.0)}（件数 {cnt['down']}・一致 {agree['down']}・記録だけ）")
print(f"G4 下げの目標が書かれた種 {seedsDown}/10＝{'当たり' if seedsDown >= 7 else ('部分' if seedsDown >= 4 else '外れ')}／G5 上げの目標が書かれた種 {seedsUp}/10＝{'当たり' if seedsUp >= 7 else ('部分' if seedsUp >= 4 else '外れ')}")
print(f"G7 往復（4 四半期以内に上げと下げが入れ替わる回数が 2 回以上の国）種ごと {osc}／3 国以上の種 {sum(1 for v in osc.values() if v >= 3)}")
print(f"天井の効いた州：bindCur と bindCurCapped が食い違う行 {capDiff['differ']}/{capDiff['n']}={ratio(capDiff['differ'], capDiff['n']):.4f}")

print('\n=== nation_s（G8・戦線の希釈・同じ 8 国）===')
def nat(d, s):
    out = dict(of=[], pb=[], pc=[], mw=0, n=0)
    for r in rd(d + f'nation_s{s}.tsv'):
        c = fl(r['openFrontsCount'])
        if not (c >= 0): continue
        out['of'].append(c); out['pb'].append(fl(r['pushBudgetFactor'])); out['pc'].append(fl(r['politicalCapitalSpentIndex']))
        out['n'] += 1
        if 'L.MinimumWage:' in r['openFrontStates']: out['mw'] += 1
    return out
ofA, ofB, pbA, pbB = [], [], [], []
mwA = mwB = nA = nB = 0; pcA, pcB = [], []
for s in range(1, N + 1):
    a = nat(A, s); b = nat(B, s)
    ofA.append(med(a['of'])); ofB.append(med(b['of'])); pbA.append(med(a['pb'])); pbB.append(med(b['pb']))
    mwA += a['mw']; nA += a['n']; mwB += b['mw']; nB += b['n']; pcA.append(mean(a['pc'])); pcB.append(mean(b['pc']))
    print(f"seed{s}: openFrontsCount の中央 A {ofA[-1]} B {ofB[-1]}／pushBudgetFactor の中央 A {pbA[-1]:.4f} B {pbB[-1]:.4f}／L.MinimumWage が開いている割合 A {ratio(a['mw'], a['n']):.3f} B {ratio(b['mw'], b['n']):.3f}")
dof = med(ofB) - med(ofA); dpb = med(pbB) - med(pbA)
print(f"G8 ①openFrontsCount の中央の差（B−基準）{dof:+.2f}（≦+1）／②pushBudgetFactor の中央の差 {dpb:+.4f}（≧−0.05）＝{'当たり' if dof <= 1 and dpb >= -0.05 else '外れ'}")
print(f"記録：L.MinimumWage が開いている国×四半期の割合 A {ratio(mwA, nA):.3f} B {ratio(mwB, nB):.3f}／politicalCapitalSpentIndex の平均（種の平均）A {mean(pcA):.4f} B {mean(pcB):.4f}")

print('\n=== 州の数（region_s・activeFlag）===')
for tag, d in (('A', A), ('B', B)):
    fin = []
    for s in range(1, N + 1):
        c = collections.Counter()
        for r in rd(d + f'region_s{s}.tsv'):
            if r['activeFlag'] == '1': c[int(r['quarter'])] += 1
        qs = sorted(c)
        fin.append((c[qs[-1]] if qs else 0, mean([c[q] for q in qs])))
    print(f"{'基準 m475' if tag == 'A' else '腕 B'}：最終四半期の有効な州の数 {[f[0] for f in fin]}／時間平均 {[round(f[1]) for f in fin]}")
