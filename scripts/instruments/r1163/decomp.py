# (1163) ③の形の分解（事前登録の下書き §1・§3）：CTL（旗 1・種 1・2・四半期 0〜79）の rulepower_s・rulepoweralloc_s・nation_s を読むだけ。
# 使い方: python -X utf8 -I decomp.py <出力.txt>
import sys, os, collections, statistics as st, math
D = 'D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
out = []
P = lambda *x: out.append(' '.join(str(y) for y in x))
# (1214) fPushbackIndex（押し返し A）は rulepower_s から消えた（c122cr）。基線 m1212 以前の生を読む台本＝列の名は残す。新しい生では列が無い。
COLS = ['nation', 'region', 'tier', 'popK', 'nIndex', 'qEngIndex', 'fStateIndex', 'fInterIndex', 'fResistIndex', 'fPushbackIndex', 'fUngovIndex', 'fForeignEngIndex',
        'rhoEngIndex', 'ownerWeightShare', 'nowRule0Share', 'nowRule1Share', 'nowRule2Share', 'nowRule3Share', 'foreignTopKey', 'oEngNationIndex', 'lNationIndex', 'hEngNationIndex', 't0EngShare', 't1EngShare', 't3EngShare', 'qIndex']
allrec = {}
summary = collections.defaultdict(list)
for s in (1, 2):
    war = set()
    with open(os.path.join(D, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if fl(c[ix['atWarMonths']]) > 0: war.add((int(c[0]), c[ix['nation']]))
    rp = collections.defaultdict(dict); nat = collections.defaultdict(dict)
    with open(os.path.join(D, f'rulepower_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q not in (0, 40, 79): continue
            r = {k: (c[ix[k]] if k in ('nation', 'region', 'tier', 'foreignTopKey') else fl(c[ix[k]])) for k in COLS if k in ix}   # (1220) fPushbackIndex は有れば読む（新しい生には列が無い）
            rp[q][r['region']] = r
            v = nat[q].setdefault(r['nation'], {'O': r['oEngNationIndex'], 'L': r['lNationIndex'], 'H': r['hEngNationIndex'], 'pop': 0.0})
            v['pop'] += r['popK'] if not math.isnan(r['popK']) else 0.0
    al = collections.defaultdict(dict); sumw = collections.Counter(); ncand = collections.Counter(); suma = collections.Counter()
    with open(os.path.join(D, f'rulepoweralloc_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q != 40 or c[ix['candEngFlag']] != '1': continue
            a = {k: fl(c[ix[k]]) for k in ('yIndex', 'rhoF', 'rhoFDIndex', 'sShare', 'bShare', 'wEngIndex', 'allocEngIndex', 'fForeignEngIndex')}; a['rhoF'] = a['rhoFDIndex']   # 凍結を外した〈D〉の ρ_f（rhoF は凍結あり）
            a['kind'] = c[ix['candDKind']]
            al[c[ix['nation']]][c[ix['region']]] = a; sumw[c[ix['nation']]] += a['wEngIndex']; ncand[c[ix['nation']]] += 1; suma[c[ix['nation']]] += a['allocEngIndex']
    # 組み直しの確かめ（★(1136)）：配り＝O×w÷Σw・力＝配り×ρ_f をエンジンの値と突き合わせる
    ok = bad = 0
    for n, rows in al.items():
        O = nat[40].get(n, {}).get('O', float('nan'))
        for rg, a in rows.items():
            want = O * a['wEngIndex'] / sumw[n] if sumw[n] > 0 else 0.0
            good = abs(want - a['allocEngIndex']) <= 1e-9 * max(1.0, abs(want)) and abs(a['allocEngIndex'] * a['rhoF'] - a['fForeignEngIndex']) <= 1e-9 * max(1.0, abs(a['fForeignEngIndex']))
            ok += good; bad += not good
    P(f'## 種 {s}：組み直しの一致（四半期 40・配り＝O×w÷Σw・力＝配り×ρ_f）一致 {ok}・不一致 {bad}')
    qv = rp[40][next(iter(rp[40]))]['qEngIndex']
    P(f'  q（四半期 40）{qv:.6g}')
    # P15
    for lim in (0.15, 0.05):
        rows = [r for r in rp[79].values() if r['tier'] == 'Core' and (79, r['nation']) not in war and r['nowRule2Share'] >= lim]
        summary[(lim, 'n')].append(len(rows))
        if lim == 0.05: continue
        P(f'## 種 {s}：四半期 79・Core・持ち主が平時・外国の取り分≧{lim}：{len(rows)} 州')
        P('  州\t持ち主\t外国(主)\tx79\tb0→b79\t[40] 区分\tO_f/L_f\t候補数\tw の割合\talloc/N\tρ_f\tρ_持ち主\tF_f/F_S\tF_f/力の和\tH÷必要（持ち主）\tq影÷qEng')
        for r in sorted(rows, key=lambda r: -r['nowRule2Share']):
            fk = r['foreignTopKey']; rg = r['region']
            r0 = rp[0].get(rg); r40 = rp[40].get(rg); a = al.get(fk, {}).get(rg)
            b0 = r0['ownerWeightShare'] if r0 and r0['nation'] == r['nation'] else float('nan')
            nf = nat[40].get(fk)
            if a is None or r40 is None or nf is None:
                P(f'  {rg}\t{r["nation"]}\t{fk}\t{r["nowRule2Share"]:.3f}\t{b0:.3f}→{r["ownerWeightShare"]:.3f}\t（四半期 40 に候補の行なし）'); continue
            # 力の列 fState ほかは影の計器の値（エンジンの t0 と合わない＝tot_check.py）＝エンジンだけで組む：外国の目標の和 T_F＝1−t0−t1−t3・力の和＝F_F÷T_F
            TF = 1 - r40['t0EngShare'] - r40['t1EngShare'] - r40['t3EngShare']
            tot = r40['fForeignEngIndex'] / TF if TF > 0 else float('nan')
            Neng = r40['popK'] * r40['qEngIndex']; ownn = nat[40][r['nation']]
            hn = ownn['H'] / (ownn['pop'] * r40['qEngIndex']) if ownn['pop'] > 0 else float('nan')
            ysum = sum(x['yIndex'] for x in al[fk].values())
            rec = dict(reg=rg, own=r['nation'], fk=fk, x79=r['nowRule2Share'], b0=b0, b79=r['ownerWeightShare'], kind=a['kind'], OL=nf['O'] / nf['L'] if nf['L'] > 0 else float('nan'),
                       nc=ncand[fk], wsh=a['wEngIndex'] / sumw[fk], aN=a['allocEngIndex'] / Neng, rf=a['rhoF'], ro=r40['rhoEngIndex'], hn=hn, qr=r40['qIndex'] / r40['qEngIndex'],
                       FfFS=a['fForeignEngIndex'] / (r40['t0EngShare'] * tot) if r40['t0EngShare'] > 0 else float('inf'), Fft=a['fForeignEngIndex'] / tot, omb=1 - a['bShare'], ysh=a['yIndex'] / ysum,
                       s=a['sShare'], y=a['yIndex'], b=a['bShare'], w=a['wEngIndex'], alloc=a['allocEngIndex'], tot=tot, F=a['fForeignEngIndex'], O=nf['O'], sumw=sumw[fk], N=r40['nIndex'],
                       FS=r40['fStateIndex'], seed=s, ysum=ysum, x40=r40['nowRule2Share'])
            allrec[(s, rg)] = rec
            P(f"  {rg}\t{r['nation']}\t{fk}\t{rec['x79']:.3f}\t{b0:.3f}→{rec['b79']:.3f}\t{rec['kind']}\t{rec['OL']:.3f}\t{rec['nc']}\t{rec['wsh']:.3f}\t{rec['aN']:.3g}\t{rec['rf']:.3f}\t{rec['ro']:.3f}\t{rec['FfFS']:.3g}\t{rec['Fft']:.3f}\t{rec['hn']:.3f}\t{rec['qr']:.3g}")
    # 国の O＞0 の一覧（四半期 40）
    pos = sorted(((v['O'] / v['L'], n, v['O'], v['L'], v['pop']) for n, v in nat[40].items() if v['O'] > 0 and v['L'] > 0), reverse=True)
    P(f'## 種 {s}：四半期 40 の O＞0 の国 {len(pos)}（O÷L・O・候補数）：' + '・'.join(f'{n} {ol:.2f} {o:.3g} {ncand[n]}' for ol, n, o, l, p in pos[:15]))
recs = list(allrec.values())
P('## まとめ（種 1・2 の P15 で四半期 40 に候補の行がある州）')
P(f'  件数 {len(recs)}・≧0.05 の州の数 {summary[(0.05, "n")]}・≧0.15 {summary[(0.15, "n")]}')
if recs:
    for k, lab in (('aN', 'alloc÷N'), ('FfFS', 'F_f÷F_S'), ('rf', 'ρ_f'), ('ro', 'ρ_持ち主'), ('wsh', 'w の割合'), ('Fft', 'F_f÷力の和'), ('hn', 'H÷必要（持ち主）'), ('OL', 'O÷L')):
        v = sorted(x[k] for x in recs if not math.isnan(x[k]))
        P(f'  {lab}：中央 {st.median(v):.3g}・最小 {v[0]:.3g}・最大 {v[-1]:.3g}')
    P(f"  A1 alloc÷N＞1 の州 {sum(x['aN'] > 1 for x in recs)}/{len(recs)}")
    P(f"  A2 ρ_f÷ρ_持ち主＜2 の州 {sum(x['rf'] / x['ro'] < 2 for x in recs if x['ro'] > 0)}/{len(recs)}・ρ_f≦ρ_持ち主 {sum(x['rf'] <= x['ro'] for x in recs)}/{len(recs)}")
    P(f"  A4 w の割合≧0.10 の州 {sum(x['wsh'] >= 0.10 for x in recs)}/{len(recs)}")
    P(f"  A5 b79＜b0 の州 {sum(x['b79'] < x['b0'] for x in recs if not math.isnan(x['b0']))}/{sum(not math.isnan(x['b0']) for x in recs)}")
    P(f"  区分（四半期 40 の〈D〉の候補の区分 1＝①だけ・3＝③）{dict(collections.Counter(x['kind'] for x in recs))}")
with open(sys.argv[1], 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
import pickle
with open(sys.argv[1] + '.pkl', 'wb') as f: pickle.dump(allrec, f)
