# (1155) S27 の行ごとの頻度（読むだけ）。使い方: python -X utf8 -I freq.py <生のフォルダ> <種の最初> <種の最後> <ラベル>
# 列は名前で引く（head -1 の名）。計器が書かない表は〈表なし〉と書き、0 と数えない。
import sys, os, csv, statistics as st
D, S0, S1, LAB = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
sys.stdout.reconfigure(newline='\n')
SEEDS = range(S0, S1 + 1)
THR = 1.43  # SecessionThreshold（Coefficients.cs:122 の既定・この木で grep した値）
FORMS = ['ローマ', '神聖ローマ', 'ビザンツ', 'ドイツ', 'イタリア', '北ドイツ連邦', 'フランス帝国', '大英帝国', 'ポーランド＝リトアニア', 'スカンジナビア', 'イベリア', '大ネーデルラント', 'ユーゴスラビア', '大ブルガリア', 'ギリシャ', 'ハンガリー', '大ルーマニア', 'チェコスロバキア', 'カリフ国', 'トゥーラーン', 'マシュリク', 'ペルシア帝国', 'トルキスタン', 'ドゥッラーニー帝国', 'サウード', 'ルーム', 'ムワッヒド', '大モンゴル', 'バーラト', 'ムガル帝国', 'インドシナ', '大日本', '中華', '大チベット', 'マラーター帝国', '越南', 'シャム帝国', 'ビルマ帝国', '統一朝鮮', '満洲国',
         'イタリア王国', 'ローマ帝国', 'ドイツ帝国', 'アラブ王国', 'マラーター王国', '中華帝国']  # Formations.cs の 40 行＋CultureRealms.cs の Name・GreatName（カリフ国・マラーター帝国・中華は重複）
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        return ix, [x for x in r if len(x) >= len(h) - 0]
def fl(x):
    try: return float(x)
    except: return float('nan')
def med(v): return st.median(v) if v else float('nan')
out = []
def P(*a): out.append(' '.join(str(x) for x in a))
P(f'# {LAB} = {D} 種 {S0}〜{S1}')
# A 分離の圧と保持（secession_s）
tot = {}
for s in SEEDS:
    R = rows('secession', s)
    if R is None: tot = None; break
    ix, L = R
    a = tot.setdefault('a', {})
    for x in L:
        tier = x[ix['tier']]; dom = fl(x[ix['dominanceShare']]); mg = fl(x[ix['marginIndex']]); pr = fl(x[ix['pressureIndex']])
        hb = fl(x[ix['holdBaseIndex']]); sd = fl(x[ix['sharedDivIndex']]); eff = fl(x[ix['effHoldBeforeShareIndex']])
        k = a.setdefault(tier, dict(n=0, full=0, lt1=0, zero=0, go=0, neg=0, restive=0, projflip=0, cult=0, eth=0, cons=0, suff=0, effratio=[], seeds_go=set(), seeds_neg=set()))
        k['n'] += 1
        if dom >= 0.999999: k['full'] += 1
        if dom < 0.999: k['lt1'] += 1
        if not (dom > 0): k['zero'] += 1
        if x[ix['goFlag']] == '1': k['go'] += 1; k['seeds_go'].add(s)
        if mg < 0: k['neg'] += 1; k['seeds_neg'].add(s)
        if pr > THR: k['restive'] += 1
        if mg < 0 and sd > 0 and hb / sd - pr >= 0: k['projflip'] += 1
        if fl(x[ix['cultureDistanceIndex']]) > 1e-9: k['cult'] += 1
        if fl(x[ix['ethnicTermIndex']]) > 1e-9: k['eth'] += 1
        if fl(x[ix['consciousnessFactorShare']]) < 0.99: k['cons'] += 1
        if fl(x[ix['suffrageFactorShare']]) < 0.99: k['suff'] += 1
        if hb > 1e-9: k['effratio'].append(eff / hb)
    nq = tot.setdefault('nq', {});
    for x in L:
        key = (s, x[ix['quarter']], x[ix['nation']])
        if key not in nq: nq[key] = (fl(x[ix['sharedDivIndex']]), fl(x[ix['restiveCount']]))
if tot is None: P('A secession: 表なし')
else:
    for tier, k in sorted(tot['a'].items()):
        P(f'A secession tier={tier} 州×四半期={k["n"]} 支配度=1:{k["full"]} <0.999:{k["lt1"]} =0:{k["zero"]} go:{k["go"]}(種{sorted(k["seeds_go"])}) 余り<0:{k["neg"]}(種の数{len(k["seeds_neg"])}) 圧>閾値1.43:{k["restive"]} 投射が無ければ余り≥0:{k["projflip"]} 文化距離>0:{k["cult"]} 民族の加算>0:{k["eth"]} 国民意識の割引<0.99:{k["cons"]} 参政権の割引<0.99:{k["suff"]} 投射後÷前の中央:{med(k["effratio"]):.3f}')
    nq = tot['nq']; sd1 = sum(1 for v in nq.values() if v[0] > 1.0 + 1e-9); rs = sum(1 for v in nq.values() if v[1] >= 1)
    P(f'A secession 国×四半期={len(nq)} 軍が割れる(sharedDiv>1):{sd1} 圧>閾値の州が1以上:{rs}')
# B 戦争の入口
wc = {}; wars_all = {}
for s in SEEDS:
    R = rows('wars', s)
    if R is None: continue
    ix, L = R; wars_all[s] = (ix, L)
    for x in L: wc[x[ix['entryCode']]] = wc.get(x[ix['entryCode']], 0) + 1
P('B wars entryCode ' + ' '.join(f'{k}:{v}' for k, v in sorted(wc.items())) if wc else 'B wars: 表なし')
for s, (ix, L) in sorted(wars_all.items()):
    sec = [x for x in L if x[ix['entryCode']] in ('secession', 'declare')]
    for x in sec: P(f'B  種{s} {x[ix["entryCode"]]} {x[ix["startYear"]]} {x[ix["sideA"]]}×{x[ix["sideB"]]} {x[ix["months"]]}か月')
# C 州の移り（cederule_s）と V01
hc = {}; sec_t = {}; ret_peace = ret_war = ret_unk = 0; lost = []; nC = 0
for s in SEEDS:
    R = rows('cederule', s)
    if R is None: continue
    nC += 1
    ix, L = R
    W = wars_all.get(s)
    for x in L:
        how = x[ix['how']]; hc[how] = hc.get(how, 0) + 1
        if how == 'Seceded': sec_t[x[ix['tierBefore']]] = sec_t.get(x[ix['tierBefore']], 0) + 1
        if how == 'Returned':
            yr = fl(x[ix['year']]); snd, rcv = x[ix['sender']], x[ix['receiver']]; end = None
            if W:
                wix, WL = W
                for w in WL:
                    if w[wix['civilWarFlag']] == '1' and {w[wix['sideA']], w[wix['sideB']]} == {snd, rcv}:
                        e = fl(w[wix['startYear']]) + fl(w[wix['months']]) / 12.0
                        if end is None or e > end: end = e
            if end is None: ret_unk += 1
            elif yr > end + 4: ret_peace += 1
            else: ret_war += 1
    # V01：中核・編入の州を失った（割譲・分離）→ 同じ送り手へ戻ったか
    for i, x in enumerate(L):
        how = x[ix['how']]
        if how not in ('Ceded', 'Seceded') or x[ix['tierBefore']] not in ('Core', 'Integrated'): continue
        snd, reg, yr = x[ix['sender']], x[ix['region']], fl(x[ix['year']])
        back = None
        for y in L[i + 1:]:
            if y[ix['region']] == reg and y[ix['receiver']] == snd: back = (fl(y[ix['year']]) - yr, y[ix['how']]); break
        lost.append((s, how, snd, reg, yr, back))
if nC == 0: P('C cederule: 表なし')
else:
    P('C cederule how ' + ' '.join(f'{k}:{v}' for k, v in sorted(hc.items())) + ' Seceded の格 ' + ' '.join(f'{k}:{v}' for k, v in sorted(sec_t.items())))
    P(f'C Returned の内訳（内戦の終わり+4年より後＝残骸の平和的復帰の候補）:{ret_peace} 内戦の決着:{ret_war} 対の内戦が見つからない:{ret_unk}')
    for how in ('Ceded', 'Seceded'):
        v = [t for t in lost if t[1] == how]; b = [t for t in v if t[5]]
        P(f'V01 {how} 中核・編入の州を失った:{len(v)} 同じ送り手へ戻った:{len(b)} 戻るまでの年の中央:{med([t[5][0] for t in b]):.1f} 戻った道 ' + ' '.join(f'{k}:{sum(1 for t in b if t[5][1]==k)}' for k in ('Ceded', 'Returned', 'Seceded')))
    big = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'スペイン', 'オスマン', '清', 'アメリカ', '日本']
    for n in big:
        v = [t for t in lost if t[2] == n]
        if v: P(f'V01  {n} 失った中核・編入:{len(v)} 戻らない:{sum(1 for t in v if not t[5])} 種ごと ' + ','.join(str(sum(1 for t in v if t[0]==s)) for s in SEEDS))
# D 侵略（invade_s）
ni = sub = nat = 0; seen = 0
for s in SEEDS:
    R = rows('invade', s)
    if R is None: continue
    seen += 1; ix, L = R
    for x in L:
        ni += 1
        if x[ix['submitted']] in ('1', 'True', 'true'): sub += 1
        if x[ix['targetIsNation']] in ('1', 'True', 'true'): nat += 1
P(f'D invade 行:{ni} 屈服:{sub} 標的が国:{nat}（種 {seen}）' if seen else 'D invade: 表なし')
# E 変名（nation_s の nation の名）
fh = {}
for s in SEEDS:
    R = rows('nation', s)
    if R is None: continue
    ix, L = R
    for x in L:
        nm = x[ix['nation']]
        if nm in FORMS:
            k = fh.setdefault(nm, {})
            if s not in k: k[s] = x[ix['year']]
P('E 変名の名が出た種と最初の年 ' + ' ; '.join(f'{n}:' + ','.join(f's{s}={y}' for s, y in sorted(v.items())) for n, v in sorted(fh.items(), key=lambda t: -len(t[1]))) if fh else 'E 変名：nation の名の列は BaseName（Instruments.cs:285・286 の NameOf）＝変名は表に出ない＝0 と数えない（計器が書かない）')
# F 支配の 4 区分と支配度（rule_s）
seenR = 0; full = n = 0; q0col = []; core_rng = []; tp = pf = rf = 0
for s in SEEDS:
    R = rows('rule', s)
    if R is None: continue
    seenR += 1; ix, L = R; lo = {}; hi = {}
    for x in L:
        n += 1; dm = fl(x[ix['dominanceShare']])
        if dm >= 0.999999: full += 1
        if x[ix['quarter']] == '0' and x[ix['tier']] == 'Colonial': q0col.append(dm)
        if x[ix['treatyPortInForceFlag']] == '1': tp += 1
        if fl(x[ix['portForeignShare']]) > 1e-9: pf += 1
        if fl(x[ix['ruleForeignShare']]) > 1e-9: rf += 1
        if x[ix['tier']] == 'Core':
            k = (x[ix['nation']], x[ix['region']]); v = fl(x[ix['ruleStateShare']])
            lo[k] = min(lo.get(k, 9), v); hi[k] = max(hi.get(k, -9), v)
    core_rng += [hi[k] - lo[k] for k in lo]
if seenR:
    P(f'F rule 州×四半期:{n} 支配度=1:{full}（{full/max(1,n):.3f}） 四半期0の植民地格の支配度 中央:{med(q0col):.4f} 件:{len(q0col)}')
    P(f'F rule 中核の州（国×州）の国家の取り分の幅（最大−最小）：件 {len(core_rng)} 中央 {med(core_rng):.3f} 幅<0.02 の割合 {sum(1 for v in core_rng if v < 0.02)/max(1,len(core_rng)):.3f}')
    P(f'F rule 条約港が効く州×四半期:{tp} 港の外国の持分>0:{pf} 外国の取り分>0:{rf}')
else: P('F rule: 表なし')
print('\n'.join(out))
