# (1009) c93wn 戦争の段0 の数え直し（r1004/wr1004_count.py の写しを直した・読むだけ）。
# 使い方：python -X utf8 -I wr1009_count.py <keep のルート> <出力のフォルダ> [盤面 ...]
# 数え方は D:/PoB_work/r1009_work/prereg.txt（旧 measurements/m1004_20261110/prereg.txt を引き継ぐ）。出力はバイトで書く（LF）。
# 直した所：年数 167（1783〜1949）・窓 1915〜1949＝35 年・打ち切りの帯 startYear＋months/12≥1949・1783 年の格は q==0 の行・
#   列強どうしに 1815〜1913 の列と判断の時点の格の〈参戦を除く〉・名前の衝突の件数。
import sys, os, statistics as st
from collections import defaultdict, Counter

ROOT, OUT = sys.argv[1], sys.argv[2]
BOARDS = sys.argv[3:] or ['base_ins', 'n1', 'n2', 'a11']
SEEDS = {'base_ins': range(1, 11), 'n1': range(1, 11), 'n2': range(1, 11), 'a11': range(11, 21)}
NINE = {'イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア'}
FIVE = {'イギリス', 'フランス', 'ロシア', 'プロイセン', 'オーストリア'}
Y0, Y1 = 1783, 1949          # 668 四半期＝1783 Q1〜1949 Q4（打ち切りの帯の端は Y1）
NYEARS = 668 / 4             # 167＝外交の段 668 回（ラベル 1783〜1950・prereg 追記 1）
# ラベルで切る窓と、その窓の外交の段の数÷4（ラベル 1783 は 3 段・1950 は 1 段）
WINDOWS = [('1783-1814', 1783, 1814, 127 / 4), ('1815-1914', 1815, 1914, 100.0), ('1915-1949', 1915, 1949, 35.0), ('1950(1段)', 1950, 1950, 0.25)]
agg_old_cens = defaultdict(int)

def tsv(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t')
    return [dict(zip(h, l.split('\t'))) for l in lines[1:] if l]

def lineage(n):
    for pre in ('共和制', '立憲王政'):
        if n.startswith(pre): n = n[len(pre):]
    if '・' in n and '大衆運動' in n: n = n.split('・')[-1]
    return n

def q(xs, p):
    xs = sorted(xs)
    if not xs: return float('nan')
    k = (len(xs) - 1) * p; i = int(k); f = k - i
    return xs[i] if i + 1 >= len(xs) else xs[i] * (1 - f) + xs[i + 1] * f

def fmt(x, d=2):
    return 'nan' if x != x else f'{x:.{d}f}'

out = []
def P(s=''): out.append(s)

summary = {}
for b in BOARDS:
    P(f'#### 盤面 {b}')
    agg = defaultdict(list)          # 種ごとの値
    pool = defaultdict(list)         # 全種を通した分布
    gp_pairs_all = Counter(); inv_tab = Counter(); inv_rank = Counter()
    check = []
    for s in SEEDS[b]:
        d = f'{ROOT}/{b}'
        wars = tsv(f'{d}/wars_s{s}.tsv'); inv = tsv(f'{d}/invade_s{s}.tsv')
        nations = set(); name_idx = defaultdict(set)
        with open(f'{d}/nation_s{s}.tsv', 'rb') as f:
            for l in f.read().decode('utf-8').splitlines()[1:]:
                c = l.split('\t')
                if len(c) > 3: nations.add(c[3]); name_idx[c[3]].add(c[2])
        agg['name_multi_idx'].append(sum(1 for v in name_idx.values() if len(v) > 1))
        rank = {}
        for r in tsv(f'{d}/score_s{s}.tsv'):
            qq = int(r['quarter'])
            # ★(1006)：ラベル Y・q%4==3 の行＝Y−1 年末（Y の年初）の盤面＝出来事の年 Y の判断の時点。1783 は q==0 の行（開幕の年）。
            if qq % 4 == 3 or qq == 0: rank[(int(r['year']), r['nation'])] = r['rankInForce']   # score_s は年末の四半期だけ全ての国を書く
        # 侵略の拒否の突き合わせ用
        refuse = defaultdict(int)
        for r in inv:
            if r['submitted'] == '0': refuse[(int(r['year']), frozenset((r['invader'], r['target'])))] += 1
        rows = []
        # 参戦の疑い（近似）：AddBelligerent の参戦は開戦の行の直後に new War{A=参戦国, B=敵} で続く＝
        #   同じ年・B が直前の鎖の参加国・A が鎖の外で国（抽象勢力は同盟を持たない）・内戦の印 0。当たらない行で鎖を始め直す。
        chain = set(); cy = None
        for r in wars:
            y = int(r['startYear']); a, bb = r['sideA'], r['sideB']; m = float(r['months'])
            key = (y, frozenset((a, bb)))
            if r['civilWarFlag'] == '1': cls = 'civil'
            elif refuse.get(key, 0) > 0: cls = 'invade'; refuse[key] -= 1
            elif y == cy and bb in chain and a not in chain and a in nations: cls = 'join'
            else: cls = 'primary'
            if cls == 'join': chain.add(a)
            else: cy = y; chain = {a, bb}
            cens = y + m / 12.0 >= Y1           # 打ち切りの帯（prereg）
            if m > (1950 - y) * 12: agg_old_cens[b] += 1   # 旧の判定（並べるだけ）
            rows.append((y, a, bb, m, cls, cens, float(r['deadK'])))
        # 検算：_warCount の集合（内戦・侵略・主）
        pc = Counter(frozenset((a, bb)) for (y, a, bb, m, cls, c_, dk) in rows if cls != 'join')
        check.append((len(pc), sum(1 for v in pc.values() if v >= 3), max(pc.values()) if pc else 0))
        intl = [x for x in rows if x[4] != 'civil']; civ = [x for x in rows if x[4] == 'civil']
        agg['n_all'].append(len(rows)); agg['n_intl'].append(len(intl)); agg['n_civil'].append(len(civ))
        for k in ('primary', 'invade', 'join'): agg['n_' + k].append(sum(1 for x in rows if x[4] == k))
        agg['n_nat_intl'].append(sum(1 for x in intl if x[1] in nations and x[2] in nations))
        agg['n_cens'].append(sum(1 for x in rows if x[5]))
        for wn, lo, hi, _yr in WINDOWS:
            agg['intl_' + wn].append(sum(1 for x in intl if lo <= x[0] <= hi))
            agg['prim_' + wn].append(sum(1 for x in rows if x[4] == 'primary' and lo <= x[0] <= hi))
            agg['civ_' + wn].append(sum(1 for x in civ if lo <= x[0] <= hi))
        # 列強どうし
        for nm, S in (('NINE', NINE), ('FIVE', FIVE)):
            g = [x for x in intl if x[1] in S and x[2] in S]
            agg['gp_' + nm].append(len(g))
            agg['gpnj_' + nm].append(sum(1 for x in g if x[4] != 'join'))
            agg['gp1815_' + nm].append(sum(1 for x in g if 1815 <= x[0] <= 1914))
            agg['gpnj1815_' + nm].append(sum(1 for x in g if 1815 <= x[0] <= 1914 and x[4] != 'join'))
            agg['gp1913_' + nm].append(sum(1 for x in g if 1815 <= x[0] <= 1913))
            agg['gpnj1913_' + nm].append(sum(1 for x in g if 1815 <= x[0] <= 1913 and x[4] != 'join'))
            if nm == 'NINE':
                for x in g: gp_pairs_all[tuple(sorted((x[1], x[2])))] += 1
        gdyn = [x for x in intl if rank.get((x[0], x[1])) == 'Great' and rank.get((x[0], x[2])) == 'Great']
        agg['gp_DYN'].append(len(gdyn)); agg['gpnj_DYN'].append(sum(1 for x in gdyn if x[4] != 'join'))
        agg['gp1815_DYN'].append(sum(1 for x in gdyn if 1815 <= x[0] <= 1914))
        agg['gpnj1815_DYN'].append(sum(1 for x in gdyn if 1815 <= x[0] <= 1914 and x[4] != 'join'))
        agg['gp1913_DYN'].append(sum(1 for x in gdyn if 1815 <= x[0] <= 1913))
        agg['gpnj1913_DYN'].append(sum(1 for x in gdyn if 1815 <= x[0] <= 1913 and x[4] != 'join'))
        agg['dyn_unknown'].append(sum(1 for x in intl if rank.get((x[0], x[1])) is None or rank.get((x[0], x[2])) is None))
        agg['n_y1783'].append(sum(1 for x in rows if x[0] == 1783))
        # 英仏
        ef = [x for x in rows if {x[1], x[2]} == {'イギリス', 'フランス'}]
        agg['ef_all'].append(len(ef)); agg['ef_nj'].append(sum(1 for x in ef if x[4] != 'join'))
        agg['ef_1815'].append(sum(1 for x in ef if 1815 <= x[0] <= 1914))
        # 期間
        for x in rows:
            kk = 'civ' if x[4] == 'civil' else 'intl'
            pool['dur_' + kk].append(x[3])
            if not x[5]: pool['durnc_' + kk].append(x[3])
            pool['dur_' + x[4]].append(x[3])
        # 再戦の間隔
        for tag, sel in (('main', lambda x: x[4] != 'join'), ('all', lambda x: True), ('intl_main', lambda x: x[4] in ('primary', 'invade'))):
            byp = defaultdict(list)
            for x in rows:
                if sel(x): byp[frozenset((x[1], x[2]))].append(x)
            for p, xs in byp.items():
                xs.sort(key=lambda x: x[0])
                for u, v in zip(xs, xs[1:]):
                    pool['gap_ss_' + tag].append(v[0] - u[0])
                    pool['gap_es_' + tag].append(v[0] - (u[0] + u[3] / 12.0))
                    if p == frozenset(('イギリス', 'フランス')):
                        pool['efgap_ss_' + tag].append(v[0] - u[0])
        # 系譜（副）
        lp = Counter(frozenset((lineage(x[1]), lineage(x[2]))) for x in rows if x[4] != 'join')
        agg['lin_pairs'].append(len(lp)); agg['lin_ge3'].append(sum(1 for v in lp.values() if v >= 3))
        # 侵略
        agg['inv_n'].append(len(inv)); agg['inv_sub'].append(sum(1 for r in inv if r['submitted'] == '1'))
        for r in inv:
            y = int(r['year'])
            tgt_kind = 'nation' if r['targetIsNation'] == '1' else 'abstract'
            inv_tab[(('NINE' if r['invader'] in NINE else 'other'), tgt_kind, ('NINE' if r['target'] in NINE else 'other'))] += 1
            inv_rank[(rank.get((y, r['invader']), '?'), rank.get((y, r['target']), 'abstract' if tgt_kind == 'abstract' else '?'))] += 1
        # 侵略が起点の戦争の割合は agg['n_invade']
    def med(k): return st.median(agg[k])
    def rate(k, years=NYEARS): return sum(agg[k]) / (len(agg[k]) * years)
    P('## 検算（_warCount と同じ集合＝内戦・侵略の拒否・主の開戦の行の組）')
    P(f'交戦ペア 平均 {fmt(sum(c[0] for c in check)/len(check),1)}・3 回以上 {fmt(sum(c[1] for c in check)/len(check),1)}・最多 平均 {fmt(sum(c[2] for c in check)/len(check),1)}')
    P(f'種ごと（ペア,3回以上,最多）: {check}')
    P('## 頻度（行の数・種の中央／和÷和の年あたり）')
    for k, lab in (('n_all', '全ての行'), ('n_intl', '国際（内戦以外）'), ('n_civil', '内戦'), ('n_primary', '主の開戦'), ('n_invade', '侵略の拒否'), ('n_join', '参戦の疑い'), ('n_nat_intl', '国際のうち国どうし'), ('n_cens', '打ち切りの帯')):
        P(f'{lab}: 種の中央 {med(k)}・年あたり {fmt(rate(k),3)}・種ごと {agg[k]}')
    for wn, lo, hi, yrs in WINDOWS:
        P(f'窓 {wn}: 国際 年あたり {fmt(rate("intl_"+wn, yrs),3)}・主の開戦 {fmt(rate("prim_"+wn, yrs),3)}・内戦 {fmt(rate("civ_"+wn, yrs),3)}')
    P(f'打ち切りの帯（startYear＋months/12≥1949）: 和 {sum(agg["n_cens"])}・種ごと {agg["n_cens"]}・旧の判定 m>(1950−y)×12 の和 {agg_old_cens[b]}')
    P(f'1783 年の行（格は q==0 の行）: 和 {sum(agg["n_y1783"])}・格が引けない国際の行の和 {sum(agg["dyn_unknown"])}・2 つ以上の nationIndex を持つ名前（種ごと）{agg["name_multi_idx"]}')
    P('## 期間（月・全種を通した分布）')
    for k in ('dur_intl', 'durnc_intl', 'dur_civ', 'durnc_civ', 'dur_primary', 'dur_invade', 'dur_join'):
        xs = pool[k]
        P(f'{k}: 件 {len(xs)}・中央 {fmt(q(xs,.5),1)}・四分位 {fmt(q(xs,.25),1)}〜{fmt(q(xs,.75),1)}・90% {fmt(q(xs,.9),1)}')
    P('## 列強どうしの戦争（国際・種の中央と種ごと）')
    for nm in ('NINE', 'FIVE', 'DYN'):
        P(f'{nm} 1815-1914: 和 {sum(agg["gp1815_"+nm])}（参戦を除く {sum(agg["gpnj1815_"+nm])}）・種の平均 {fmt(sum(agg["gp1815_"+nm])/len(agg["gp1815_"+nm]),1)}（{fmt(sum(agg["gpnj1815_"+nm])/len(agg["gpnj1815_"+nm]),1)}）・1815-1913 種の平均 {fmt(sum(agg["gp1913_"+nm])/len(agg["gp1913_"+nm]),1)}（{fmt(sum(agg["gpnj1913_"+nm])/len(agg["gpnj1913_"+nm]),1)}）・1815-1914 種の中央 {med("gp1815_"+nm)}（{med("gpnj1815_"+nm)}）')
        P(f'{nm}: 全期間 全ての行 中央 {med("gp_"+nm)}（参戦を除く {med("gpnj_"+nm)}）・1815-1914 中央 {med("gp1815_"+nm)}' + (f'（参戦を除く {med("gpnj1815_"+nm)}）' if nm != 'DYN' else '') + f'・種ごと {agg["gp_"+nm]}')
    P('NINE の組の上位（全種の和）: ' + '・'.join(f'{a}×{b} {n}' for (a, b), n in gp_pairs_all.most_common(10)))
    P('## 英仏')
    P(f'英仏の行: 回/種 平均 {fmt(sum(agg["ef_all"])/len(agg["ef_all"]),2)}（参戦を除く {fmt(sum(agg["ef_nj"])/len(agg["ef_nj"]),2)}）・1815-1914 平均 {fmt(sum(agg["ef_1815"])/len(agg["ef_1815"]),2)}・種ごと {agg["ef_all"]}')
    for tag in ('main', 'all'):
        xs = pool['efgap_ss_' + tag]
        P(f'英仏の再戦の間隔（開始→開始・{tag}）: 件 {len(xs)}・中央 {fmt(q(xs,.5),1)}・四分位 {fmt(q(xs,.25),1)}〜{fmt(q(xs,.75),1)}')
    P('## 再戦の間隔（同じ組・年）')
    for tag in ('main', 'all', 'intl_main'):
        for kind in ('ss', 'es'):
            xs = pool[f'gap_{kind}_{tag}']
            P(f'{tag} {"開始→開始" if kind=="ss" else "終わり→開始"}: 件 {len(xs)}・中央 {fmt(q(xs,.5),1)}・四分位 {fmt(q(xs,.25),1)}〜{fmt(q(xs,.75),1)}・0 年以下 {sum(1 for v in xs if v<=0)}')
    P(f'系譜（副）: 交戦ペア 平均 {fmt(sum(agg["lin_pairs"])/len(agg["lin_pairs"]),1)}・3 回以上 平均 {fmt(sum(agg["lin_ge3"])/len(agg["lin_ge3"]),1)}')
    P('## 侵略（invade_s）')
    P(f'件/種 平均 {fmt(sum(agg["inv_n"])/len(agg["inv_n"]),1)}・屈服の割合（和÷和） {fmt(sum(agg["inv_sub"])/max(1,sum(agg["inv_n"])),3)}・侵略の拒否から開いた戦争/種 平均 {fmt(sum(agg["n_invade"])/len(agg["n_invade"]),1)}')
    P('侵略者（固定の名簿）×標的の種類×標的（固定の名簿）: ' + '・'.join(f'{k} {v}' for k, v in sorted(inv_tab.items(), key=lambda t: -t[1])))
    P('判断の時点の rankInForce（侵略者,標的）: ' + '・'.join(f'{k} {v}' for k, v in sorted(inv_rank.items(), key=lambda t: -t[1])))
    inv_by = Counter(); tgt_by = Counter()
    for (ir, tr), v in inv_rank.items(): inv_by[ir] += v; tgt_by[tr] += v
    tot = sum(inv_rank.values())
    P('侵略者の判断の時点の格の和（ラベル y・q%4==3＝y の年初）: ' + '・'.join(f'{k} {inv_by[k]}' for k in ('Minor', 'Middle', 'Secondary', 'Great', '?')) + f'・計 {tot}')
    nonminor = tgt_by['Middle'] + tgt_by['Secondary'] + tgt_by['Great']
    P('標的の判断の時点の格の和: ' + '・'.join(f'{k} {tgt_by[k]}' for k in ('abstract', 'Minor', 'Middle', 'Secondary', 'Great', '?')) + f'・非 Minor の国 {nonminor}（{fmt(nonminor/max(1,tot)*100,1)}%）・標的が NINE {sum(v for k, v in inv_tab.items() if k[2]=="NINE")}')
    P()
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, 'wr1009_count.txt'), 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', os.path.join(OUT, 'wr1009_count.txt'), len(out))
