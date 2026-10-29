# -*- coding: utf-8 -*-
"""(647) 線 fsc2 の読み手＝事前登録 §5・§14 の狙いの数字を計器つき 10 種の生（fsc2_s*.tsv）から出す。読むだけ。
使い方：
  python an_fsc2_arm.py <B の ins フォルダ> <Ains0 の ins フォルダ>     … B と Ains0 を並べ、①〜⑥・③'・記録を出す
  python an_fsc2_arm.py --selftest                                       … T10：③ の読み手が〈門の拒否〉と〈丸めで段が動かない〉を分けること（合成の行で陽性対照）
主要 9 国＝計器の NATIONS。行は国×四半期（9 国は毎四半期・他は年末）。
①a 配線：xApply>0 かつ 関税の目標（潤沢の前）>1e-9 の行で、B は 後=前 の割合≥0.99／Ains0 は 後<前 の割合≥0.99。
①b 帯の比：stance が Industrialize(1)・Militarize(4) の行だけ・国庫÷線の帯 <1 と ≥4 の両方に 20 行以上ある国（B と Ains0 の両方）を数え、B の比−Ains0 の比≥0.2 の国が 3 分の 2 以上（切り上げ）。数えた国が 3 未満は判定不能。
②  処分の向き：xApply>0 で DisposeOpulence が走った行（usedAgree が nan でない）。税 4 本＝Agree<0 で after≤before・Agree≥0 で after==before。支出 2 本＝Agree>0 で after≥before・Agree≤0 で after==before。割合≥0.99。
③  税の下げが門を通る：xEnd>0 かつ Agree(土地税 or 人頭税)<0 の国×四半期の、次の 4 四半期に実効が下がった割合。B≥Ains0−0.05。外れた行を〈門の拒否（目標の段が実効より下）〉と〈目標が実効と同じ段〉に分ける。
③' 支出の引き上げが門を通る：xEnd>0 かつ Agree(社会保障 or 教育費)>0 の行の、次の 4 四半期に社会保障の段か教育費の実効が上がった割合。B≥Ains0。
④  片方向の錠：9 国それぞれで tariffEnd の上げと下げの両方（10 種を合わせて）／9 国×5 時代の 45 マスのうち上げと下げの両方がある数（種の中央値）B≥Ains0−2.0／書き込み（税の下げ・支出の引き上げ）が 10 種を合わせて 1 件以上。
⑤  端：tariffEnd≥0.9 の割合・≤0.01 の割合 が B≤max(2×Ains0, 0.01)。⑥ 負：tariffEnd<0 の割合 B≤max(2×Ains0, 0.002)。
記録：①´ 国庫が線の上にある割合・R6 目標が前の四半期より上がった割合（スタンスが書く軸／書かない軸）・R4' 社会保障の段の上がった後 8 四半期以内の下がり・R7 税が段 0 にある割合と連続の長さ・R8 どの軸にも書かなかった行の割合・④ の国×種の書き込み件数"""
import csv, sys, glob, re, os, io, statistics as st, math
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
NAT = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
ERAS = [(0, 1829), (1830, 1869), (1870, 1913), (1914, 1939), (1940, 9999)]
# スタンスが毎四半期その軸を書く行（事前登録 §14-1）。0 Modernize／1 Industrialize／2 Welfare／3 Fiscal／4 Militarize。書く軸の集合
WRITES = {'income': {0, 1, 2, 3, 4}, 'poll': {0, 1, 2, 3, 4}, 'land': {0, 1, 2, 3, 4}, 'prog': {0, 2, 3}, 'social': {0, 2, 3}, 'edu': {0, 1, 2, 3, 4}}
AX = ['income', 'poll', 'land', 'prog', 'social', 'edu']
EPS = 1e-9

def fl(x):
    try: return float(x)
    except: return float('nan')

def isn(x): return x != x

def load(d):
    """seed -> nation -> [row dict] を四半期順に。数値列は float、nation は文字列。"""
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'fsc2_s*.tsv'))):
        sd = int(re.search(r'_s(\d+)', os.path.basename(f)).group(1))
        with open(f, 'rb') as fh:
            rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
            nat = {}
            for r in rd:
                if len(r) != len(h): continue
                row = {k: (v if k == 'nation' else fl(v)) for k, v in zip(h, r)}
                nat.setdefault(row['nation'], []).append(row)
        for n in nat: nat[n].sort(key=lambda r: r['quarter'])
        out[sd] = nat
    return out

def each(D, nations=NAT, only_every_quarter=True):
    """計器の 9 国は毎四半期の行（連続）。次の 4 四半期を読む関数は quarter が連続の行だけを使う。"""
    for sd, nat in D.items():
        for n in nations:
            for i, r in enumerate(nat.get(n, [])):
                yield sd, n, nat[n], i, r

def nxt(rows, i, k=4):
    """次の k 四半期（連続）。足りなければ None。"""
    q0 = rows[i]['quarter']
    seg = rows[i + 1:i + 1 + k]
    if len(seg) < k: return None
    for j, s in enumerate(seg):
        if s['quarter'] != q0 + j + 1: return None
    return seg

def agree_key(ax): return {'income': 'agreeIncome', 'poll': 'agreePoll', 'land': 'agreeLand', 'prog': 'agreeProg', 'social': 'agreeSocial', 'edu': 'agreeEdu'}[ax]
def used_key(ax): return {'income': 'usedAgreeIncome', 'poll': 'usedAgreePoll', 'land': 'usedAgreeLand', 'prog': 'usedAgreeProg', 'social': 'usedAgreeSocial', 'edu': 'usedAgreeEdu'}[ax]
def before_key(ax): return {'income': 'beforeIncome', 'poll': 'beforePoll', 'land': 'beforeLand', 'prog': 'beforeProg', 'social': 'beforeSocial', 'edu': 'beforeEdu'}[ax]
def after_key(ax): return {'income': 'afterIncome', 'poll': 'afterPoll', 'land': 'afterLand', 'prog': 'afterProg', 'social': 'afterSocial', 'edu': 'afterEdu'}[ax]

def classify_drop(rows, i, effk, stagek, tstagek):
    """③ の分類：次の 4 四半期に実効が下がった='drop'。下がらなかった行は 目標の段<実効の段='refused'（門が拒んだ）／目標の段≥実効の段='same'（目標が丸めで実効と同じ段＝門は要らなかった）。"""
    seg = nxt(rows, i, 4)
    if seg is None: return None
    cur = rows[i][effk]
    if any(s[effk] < cur - EPS for s in seg): return 'drop'
    ts, es = rows[i][tstagek], rows[i][stagek]
    if ts < es: return 'refused'
    return 'same'

def read_arm(D):
    R = {}
    # ①a
    n1 = m1 = n2 = m2 = 0
    for sd, n, rows, i, r in each(D):
        if r['xApply'] > 0 and r['tariffTargetBeforeMul'] > EPS:
            n1 += 1
            if r['tariffTargetAfterMul'] == r['tariffTargetBeforeMul']: m1 += 1
            if r['tariffTargetAfterMul'] < r['tariffTargetBeforeMul']: m2 += 1
    R['a_n'] = n1; R['a_same'] = m1 / n1 if n1 else float('nan'); R['a_less'] = m2 / n1 if n1 else float('nan')
    # ①b
    band = {}
    for sd, n, rows, i, r in each(D):
        if r['stance'] not in (1, 4): continue
        t = r['treasuryOverLine']
        if isn(t) or isn(r['tariffTargetEnd']): continue
        k = 'lo' if t < 1 else 'hi' if t >= 4 else None
        if k: band.setdefault(n, {'lo': [], 'hi': []})[k].append(r['tariffTargetEnd'])
    R['band'] = {n: (len(v['lo']), len(v['hi']), (st.mean(v['hi']) / st.mean(v['lo']) if len(v['lo']) >= 20 and len(v['hi']) >= 20 and st.mean(v['lo']) > EPS else float('nan'))) for n, v in band.items()}
    # ②
    tot = ok_dir = 0; tot_un = ok_un = 0
    for sd, n, rows, i, r in each(D):
        if not (r['xApply'] > 0) or isn(r['usedAgreeIncome']): continue
        for ax in AX:
            ag, b, a = r[used_key(ax)], r[before_key(ax)], r[after_key(ax)]
            if isn(ag) or isn(b) or isn(a): continue
            tax = ax in ('income', 'poll', 'land', 'prog')
            if tax and ag < 0 and b > EPS:
                tot += 1; ok_dir += (a <= b + EPS)
            elif tax and ag < 0:
                pass
            elif (not tax) and ag > 0:
                tot += 1; ok_dir += (a >= b - EPS)
            else:
                tot_un += 1; ok_un += (abs(a - b) <= EPS)
    R['b_dir'] = ok_dir / tot if tot else float('nan'); R['b_dir_n'] = tot
    R['b_un'] = ok_un / tot_un if tot_un else float('nan'); R['b_un_n'] = tot_un
    # ③
    c = {'drop': 0, 'refused': 0, 'same': 0}
    for sd, n, rows, i, r in each(D):
        if not (r['xEnd'] > 0): continue
        for ax, effk, stk, tsk in (('land', 'landEff', 'landStage', 'landTargetStage'), ('poll', 'pollEff', 'pollStage', 'pollTargetStage')):
            if r[agree_key(ax)] < 0:
                k = classify_drop(rows, i, effk, stk, tsk)
                if k: c[k] += 1
    tot3 = sum(c.values())
    R['c3_n'] = tot3; R['c3'] = c['drop'] / tot3 if tot3 else float('nan'); R['c3_refused'] = c['refused'] / tot3 if tot3 else float('nan'); R['c3_same'] = c['same'] / tot3 if tot3 else float('nan')
    # ③'
    n4 = m4 = 0
    for sd, n, rows, i, r in each(D):
        if not (r['xEnd'] > 0) or not (r['agreeSocial'] > 0 or r['agreeEdu'] > 0): continue
        seg = nxt(rows, i, 4)
        if seg is None: continue
        n4 += 1
        if any(s['socialLawStage'] > r['socialLawStage'] or s['eduEff'] > r['eduEff'] + EPS for s in seg): m4 += 1
    R['c3p_n'] = n4; R['c3p'] = m4 / n4 if n4 else float('nan')
    # ④ 関税の上げ下げ（国別）と 45 マス
    ud = {n: [0, 0] for n in NAT}; cells = []
    for sd, nat in D.items():
        cs = 0
        for n in NAT:
            rows = nat.get(n, [])
            by = {}
            for a, b in zip(rows, rows[1:]):
                if b['quarter'] != a['quarter'] + 1: continue
                d = b['tariffEnd'] - a['tariffEnd']
                e = next((k for k, (lo, hi) in enumerate(ERAS) if lo <= b['year'] <= hi), 4)
                if d > 1e-12: ud[n][0] += 1; by.setdefault(e, [0, 0])[0] += 1
                elif d < -1e-12: ud[n][1] += 1; by.setdefault(e, [0, 0])[1] += 1
            cs += sum(1 for v in by.values() if v[0] > 0 and v[1] > 0)
        cells.append(cs)
    R['ud'] = ud; R['cells'] = st.median(cells) if cells else float('nan')
    # 書き込み件数（国×種）
    wr = {}
    for sd, n, rows, i, r in each(D):
        if isn(r['usedAgreeIncome']): continue
        w = wr.setdefault(n, {sd2: [0, 0] for sd2 in D})[sd]
        for ax in ('income', 'poll', 'land', 'prog'):
            b, a = r[before_key(ax)], r[after_key(ax)]
            if not isn(b) and a < b - EPS: w[0] += 1
        for ax in ('social', 'edu'):
            b, a = r[before_key(ax)], r[after_key(ax)]
            if not isn(b) and a > b + EPS: w[1] += 1
    R['wr'] = wr
    # ⑤⑥ と記録
    allr = [r for sd, nat in D.items() for rows in nat.values() for r in rows if not isn(r['tariffEnd'])]
    R['hi'] = sum(1 for r in allr if r['tariffEnd'] >= 0.9) / len(allr) if allr else float('nan')
    R['lo'] = sum(1 for r in allr if r['tariffEnd'] <= 0.01) / len(allr) if allr else float('nan')
    R['neg'] = sum(1 for r in allr if r['tariffEnd'] < 0) / len(allr) if allr else float('nan')
    tr = [r['treasuryOverLine'] for r in allr if not isn(r['treasuryOverLine'])]
    R['over'] = sum(1 for t in tr if t > 1) / len(tr) if tr else float('nan')
    R['over4'] = sum(1 for t in tr if t >= 4) / len(tr) if tr else float('nan')
    R['rows'] = len(allr)
    # R6：x>0 かつ Agree>0 の行で目標が前の四半期より上がった割合（書く軸／書かない軸）
    r6 = {'writes': [0, 0], 'nowrites': [0, 0]}
    for sd, n, rows, i, r in each(D):
        if i == 0 or rows[i - 1]['quarter'] != r['quarter'] - 1 or not (r['xEnd'] > 0): continue
        for ax, tk in (('social', 'socialTargetEnd'), ('edu', 'eduTargetEnd')):
            if r[agree_key(ax)] > 0 and r['stance'] >= 0:
                k = 'writes' if r['stance'] in WRITES[ax] else 'nowrites'
                r6[k][0] += 1; r6[k][1] += (r[tk] > rows[i - 1][tk] + EPS)
    R['r6'] = {k: (v[1] / v[0] if v[0] else float('nan'), v[0]) for k, v in r6.items()}
    # R4'：社会保障の段が上がった後 8 四半期以内に下がった行の割合
    up = dn = 0
    for sd, n, rows, i, r in each(D):
        if i == 0 or rows[i - 1]['quarter'] != r['quarter'] - 1: continue
        if r['socialLawStage'] > rows[i - 1]['socialLawStage']:
            seg = nxt(rows, i, 8)
            if seg is None: continue
            up += 1; dn += any(s['socialLawStage'] < r['socialLawStage'] for s in seg)
    R['r4p'] = (dn / up if up else float('nan'), up)
    # R7：税 4 本が段 0 にある割合と連続の長さ
    s0 = {}
    for sd, n, rows, i, r in each(D):
        for ax in ('income', 'poll', 'land', 'prog'):
            v = r[ax + 'Stage']
            if isn(v) or v < 0: continue
            s0.setdefault(ax, [0, 0])[1] += 1; s0[ax][0] += (v == 0)
    R['r7'] = {k: v[0] / v[1] for k, v in s0.items() if v[1]}
    runs = []
    for sd, nat in D.items():
        for n in NAT:
            cur = 0
            for r in nat.get(n, []):
                if all(r[ax + 'Stage'] == 0 for ax in ('income', 'poll', 'land', 'prog')): cur += 1
                else:
                    if cur: runs.append(cur)
                    cur = 0
            if cur: runs.append(cur)
    R['r7run'] = (st.median(runs) if runs else 0, (sorted(runs)[int(len(runs) * 0.9)] if runs else 0), len(runs))
    # R8：x>0（Apply）で Dispose が走ったのにどの軸にも書かなかった行の割合
    ran = nowr = 0
    for sd, n, rows, i, r in each(D):
        if not (r['xApply'] > 0) or isn(r['usedAgreeIncome']): continue
        ran += 1
        wrote = False
        for ax in AX:
            b, a = r[before_key(ax)], r[after_key(ax)]
            if not isn(b) and abs(a - b) > EPS: wrote = True
        nowr += (not wrote)
    R['r8'] = (nowr / ran if ran else float('nan'), ran)
    return R

def f3(x): return 'nan' if (x is None or (isinstance(x, float) and isn(x))) else f'{x:.4f}'

def band_pass(B, A):
    nb, na = B['band'], A['band']
    cnt = hit = 0
    detail = []
    for n in NAT:
        if n in nb and n in na and not isn(nb[n][2]) and not isn(na[n][2]):
            cnt += 1; d = nb[n][2] - na[n][2]; hit += (d >= 0.2); detail.append(f'{n}:{na[n][2]:.2f}→{nb[n][2]:.2f}')
    need = math.ceil(cnt * 2 / 3)
    return cnt, hit, need, detail

def judge(B, A):
    L = []
    cnt, hit, need, detail = band_pass(B, A)
    L.append(f"①a 配線：B 後=前 {f3(B['a_same'])}（n={B['a_n']}）{'当たり' if B['a_same'] >= 0.99 else '外れ'}／Ains0 後<前 {f3(A['a_less'])}（n={A['a_n']}）{'当たり' if A['a_less'] >= 0.99 else '外れ'}")
    L.append(f"①b 帯の比（Industrialize・Militarize の行）：数えた国 {cnt}・B が 0.2 以上大きい国 {hit}（必要 {need}）{'判定不能' if cnt < 3 else ('当たり' if hit >= need else '外れ')}  " + ' '.join(detail))
    L.append(f"②  処分の向き：税・支出の下げ上げが一致度の向き {f3(B['b_dir'])}（n={B['b_dir_n']}）{'当たり' if B['b_dir'] >= 0.99 else '外れ'}／向きでない軸の不変 {f3(B['b_un'])}（n={B['b_un_n']}）{'当たり' if B['b_un'] >= 0.99 else '外れ'}")
    L.append(f"③  税の下げが門を通る：B {f3(B['c3'])}（n={B['c3_n']}）Ains0 {f3(A['c3'])}（n={A['c3_n']}）差 {f3(B['c3']-A['c3'])} → {'当たり' if B['c3'] >= A['c3'] - 0.05 else '外れ（採らない）'}。外れた行の内訳 B＝門の拒否 {f3(B['c3_refused'])}・段が同じ {f3(B['c3_same'])}／Ains0＝拒否 {f3(A['c3_refused'])}・同じ {f3(A['c3_same'])}")
    L.append(f"③' 支出の引き上げが門を通る：B {f3(B['c3p'])}（n={B['c3p_n']}）Ains0 {f3(A['c3p'])}（n={A['c3p_n']}） → {'当たり' if B['c3p'] >= A['c3p'] else '外れ（採らない）'}")
    bad = [n for n in NAT if not (B['ud'][n][0] > 0 and B['ud'][n][1] > 0)]
    L.append(f"④  関税の上げ下げ：両方ある国 {9-len(bad)}/9{('・欠け ' + ','.join(bad)) if bad else ''}・45 マス（種の中央値）B {B['cells']}／Ains0 {A['cells']} → {'当たり' if B['cells'] >= A['cells'] - 2.0 else '外れ'}")
    tw = sum(sum(v[0] for v in d.values()) for d in B['wr'].values()), sum(sum(v[1] for v in d.values()) for d in B['wr'].values())
    L.append(f"④' 処分の書き込み（10 種合計）：税の下げ {tw[0]}・支出の引き上げ {tw[1]} → {'当たり' if tw[0] >= 1 and tw[1] >= 1 else '外れ'}。国別（税の下げ/支出の引き上げ・種の合計）：" + ' '.join(f"{n}:{sum(v[0] for v in B['wr'].get(n, {}).values())}/{sum(v[1] for v in B['wr'].get(n, {}).values())}" for n in NAT))
    L.append(f"⑤  端：≥0.9 B {f3(B['hi'])} 上限 {f3(max(2*A['hi'], 0.01))} {'当たり' if B['hi'] <= max(2*A['hi'], 0.01) else '外れ'}／≤0.01 B {f3(B['lo'])} 上限 {f3(max(2*A['lo'], 0.01))} {'当たり' if B['lo'] <= max(2*A['lo'], 0.01) else '外れ'}")
    L.append(f"⑥  負：B {f3(B['neg'])} 上限 {f3(max(2*A['neg'], 0.002))} {'当たり' if B['neg'] <= max(2*A['neg'], 0.002) else '外れ'}")
    L.append(f"記録 ①´ 国庫が線の上 B {f3(B['over'])}／Ains0 {f3(A['over'])}・4 倍以上 B {f3(B['over4'])}／Ains0 {f3(A['over4'])}（行数 B {B['rows']}・Ains0 {A['rows']}）")
    L.append(f"記録 R6 x>0 かつ Agree>0 で目標が上がった割合（書く軸／書かない軸）B {f3(B['r6']['writes'][0])}(n={B['r6']['writes'][1]})／{f3(B['r6']['nowrites'][0])}(n={B['r6']['nowrites'][1]})  Ains0 {f3(A['r6']['writes'][0])}(n={A['r6']['writes'][1]})／{f3(A['r6']['nowrites'][0])}(n={A['r6']['nowrites'][1]})")
    L.append(f"記録 R4' 社会保障の段が上がった後 8 四半期以内に下がった割合 B {f3(B['r4p'][0])}(上がり {B['r4p'][1]})／Ains0 {f3(A['r4p'][0])}(上がり {A['r4p'][1]})")
    L.append("記録 R7 税が段 0 にある割合 B " + ' '.join(f'{k}:{v:.3f}' for k, v in B['r7'].items()) + "／Ains0 " + ' '.join(f'{k}:{v:.3f}' for k, v in A['r7'].items()))
    L.append(f"記録 R7 4 税が全て段 0 の連続の長さ（中央値・90 分位・回数）B {B['r7run']}／Ains0 {A['r7run']}")
    L.append(f"記録 R8 Dispose が走ったのにどの軸にも書かなかった行 B {f3(B['r8'][0])}(n={B['r8'][1]})")
    return L

def selftest():
    # T10：合成の行で ③ の分類（陽性対照）。実効の段 2・目標の段 1（門が拒んだ）／目標の段 2（丸めで動かない）／実効が下がった
    def row(q, eff, stage, tstage): return {'quarter': q, 'landEff': eff, 'landStage': stage, 'landTargetStage': tstage}
    base = [row(q, 0.30, 2, 1) for q in range(5)]
    assert classify_drop(base, 0, 'landEff', 'landStage', 'landTargetStage') == 'refused'
    same = [row(q, 0.30, 2, 2) for q in range(5)]
    assert classify_drop(same, 0, 'landEff', 'landStage', 'landTargetStage') == 'same'
    drop = [row(0, 0.30, 2, 1)] + [row(q, 0.20, 1, 1) for q in range(1, 5)]
    assert classify_drop(drop, 0, 'landEff', 'landStage', 'landTargetStage') == 'drop'
    gap = [row(0, 0.30, 2, 1)] + [row(q + 1, 0.30, 2, 1) for q in range(1, 5)]   # 四半期が連続でない行は読まない
    assert classify_drop(gap, 0, 'landEff', 'landStage', 'landTargetStage') is None
    print('selftest ok')

if __name__ == '__main__':
    if len(sys.argv) >= 2 and sys.argv[1] == '--selftest': selftest(); sys.exit(0)
    B = read_arm(load(sys.argv[1])); A = read_arm(load(sys.argv[2]))
    for l in judge(B, A): print(l)
