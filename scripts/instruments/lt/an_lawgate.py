# (423) 線 lt の読み手：lawgate_s（法の門の旧と新の表）と reform_s／metal_s から、事前登録 measurements/lt_20261019_prereg.txt の
#   [6]・[13]②・[14]（追記4）の判定 T1〜T4・T8・T9 と、記録 R1〜R5 を読む。判定は事前登録の数字のまま印字する（手で数えない）。
# 使い方：python -I scripts/instruments/lt/an_lawgate.py <計器の出力フォルダ（…/ins）> [--report <成績表の報告のファイル>] [--line 0.75]
#   ・T1：--report の9行目〈金本位の強制離脱（兌換停止）: 総N回〉
#   ・T2・T4：継承分を除いた金の法の国の数（lawgate_s・年末の行・全国・種の平均）で判定。生の nGoldLaw（metal_s・10 種×4 四半期の平均）も並べる
#   ・T3：L(1913)＝継承分を含む金の法の国の数（metal_s）・W(1913)＝本国の識字（lawgate_s の literacyHome・兵士を含む）が線を一度でも超えた国の数
#   ・T8：寿命の中で銀から金へ移った国（年末の行の間で金でない→金・同じ国の繰り返しは 1）の数・1 種あたり平均
#   ・T9：金の願いが立ったまま法が金でない状態が連続 5 年末以上続いた国×種の数・1 種あたり平均
# 読むだけ。力学を再計算しない。NaN は nan のまま。標準出力は UTF-8。
import csv, collections, glob, os, re, statistics as st, sys
sys.stdout.reconfigure(encoding='utf-8')

INHERIT_LIT = 0.1          # 継承分の区別＝金の法の国で本国の識字がこの値未満（分離建国の植民地王国。追記4 [14]）
YEARS = (1793, 1803, 1823, 1833, 1843, 1853, 1873, 1893, 1913, 1933)

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        yield from csv.DictReader(f, delimiter='\t')

def seeds(d, kind):
    return sorted(glob.glob(os.path.join(d, f'{kind}_s*.tsv')), key=lambda p: int(re.search(r'_s(\d+)\.tsv$', p).group(1)))

def sid(p):
    return int(re.search(r'_s(\d+)\.tsv$', p).group(1))

def mean(xs):
    xs = list(xs)
    return st.mean(xs) if xs else float('nan')

def main():
    a = sys.argv[1:]
    d = a[0]
    line = 0.75
    report = None
    i = 1
    while i < len(a):
        if a[i] == '--line': line = float(a[i + 1]); i += 2
        elif a[i] == '--report': report = a[i + 1]; i += 2
        else: i += 1
    P = print

    # ---- lawgate_s を読む（年末の行だけ・種ごとに国の列）
    seq = {}      # seed -> nation -> [(year, row)]
    antiq = collections.defaultdict(lambda: collections.defaultdict(list))   # 国の列（都市の割合の割れの回数）
    chk_below = 0; chk_rows = 0
    for p in seeds(d, 'lawgate'):
        s = sid(p); per = collections.defaultdict(list)
        for r in rows(p):
            q = int(r['quarter']); n = r['nation']
            chk_rows += 1
            if int(r['antiTargetKept']) < int(r['antitrustLawCode']): chk_below += 1
            antiq[s][n].append((q, float(r['urbanHome'])))
            if q % 4 == 3: per[n].append((int(r['year']), r))
        seq[s] = per
    nseed = len(seq)

    # ---- T1
    t1 = None
    if report:
        with open(report, encoding='utf-8') as f:
            for ln in f:
                m = re.search(r'金本位の強制離脱（兌換停止）: 総(\d+)回', ln)
                if m: t1 = int(m.group(1)); break
    P(f'== 判定（事前登録 [13]②・[14]。種の数 {nseed}）')
    if t1 is None: P('T1 強制離脱の総回数：（--report が無い）')
    else: P(f'T1 強制離脱の総回数 B={t1}（線 ＜270）→ {"通過" if t1 < 270 else "外れ"}（参考：≦200 なら m412 の近く・≧360 は上限の外）')

    # ---- 年ごとの金の法の国の数（継承分を除く・含む）
    def counts(y):
        raw, non = [], []
        for s, per in seq.items():
            r_ = n_ = 0
            for n, lst in per.items():
                rr = next((r for yy, r in lst if yy == y), None)
                if rr is None or int(rr['goldLawCode']) != 2: continue
                r_ += 1
                if float(rr['literacyHome']) >= INHERIT_LIT: n_ += 1
            raw.append(r_); non.append(n_)
        return mean(raw), mean(non)
    cn = {y: counts(y) for y in YEARS}
    by = collections.defaultdict(list)
    for p in seeds(d, 'metal'):
        for r in rows(p): by[int(r['year'])].append(float(r['nGoldLaw']))
    P('nGoldLaw（metal_s・10 種×4 四半期の年平均・継承分を含む）：' + '  '.join(f'{y}={mean(by[y]):.1f}' for y in YEARS if by[y]))
    P('金の法の国の数（lawgate_s・年末・全国・種の平均）継承分を含む：' + '  '.join(f'{y}={cn[y][0]:.1f}' for y in YEARS))
    P('　　　　　　　　　　　　　　　　　　　　　　　　継承分を除く（literacyHome≧0.1）：' + '  '.join(f'{y}={cn[y][1]:.1f}' for y in YEARS))
    t2 = cn[1833][1] <= 10 and cn[1853][1] <= 20
    P(f'T2 波の起点（継承分を除く）1833 年={cn[1833][1]:.1f}（線 ≦10）・1853 年={cn[1853][1]:.1f}（線 ≦20）→ {"通過" if t2 else "外れ"}　[生の metal_s：1833={mean(by[1833]):.1f}・1853={mean(by[1853]):.1f}]')
    t4 = cn[1793][1] <= 4 and cn[1803][1] <= 6
    P(f'T4 早い波（継承分を除く）1793 年={cn[1793][1]:.1f}（線 ≦4）・1803 年={cn[1803][1]:.1f}（線 ≦6・追記4 で 8 から戻した）→ {"通過" if t4 else "外れ"}　[生の metal_s：1793={mean(by[1793]):.1f}・1803={mean(by[1803]):.1f}]')

    # ---- T3：L(1913) と W(1913)
    L = mean(by[1913]) if by[1913] else float('nan')
    Ws = []
    for s, per in seq.items():
        w = 0
        for n, lst in per.items():
            if any(yy <= 1913 and float(r['literacyHome']) > line for yy, r in lst): w += 1
        Ws.append(w)
    W = mean(Ws)

    # ---- T8・T9
    ntrans = 0; transall = []
    trans = []; stuck = []; stuck_names = collections.Counter(); trans_names = collections.Counter()
    for s, per in seq.items():
        c = k = 0
        for n, lst in per.items():
            prev = None; run = 0; best = 0; moved = False
            for yy, r in lst:
                code = int(r['goldLawCode'])
                if prev is not None and prev != 2 and code == 2:   # 銀から金への移り（年末の行の間）。願いの先立ちは数えない〔移りの経路は政治軸だけ・追記5〕
                    moved = True; ntrans += 1
                prev = code
                if int(r['goldTargetKept']) == 2 and code != 2: run += 1; best = max(best, run)
                else: run = 0
            if moved: c += 1; trans_names[n] += 1
            if best >= 5: k += 1; stuck_names[n] += 1
        trans.append(c); stuck.append(k); transall.append(ntrans); ntrans = 0
    t8 = mean(trans) >= 5
    if 12 <= L <= 40: t3 = '通過'
    elif L < 12 and 12 <= W <= 40: t3 = '門が止めた型（T9 で確かめる）'
    elif L < 12: t3 = '波が死んだ（外れ）'
    else: t3 = '識字の線が効いていない（外れ）'
    P(f'T3 1913 年：L=nGoldLaw={L:.1f}（窓 [12,40]）・W=識字＞{line} を一度でも超えた国の数={W:.1f} → {t3}')
    P(f'T8 寿命の中で銀から金へ移った国（国の数・同じ国の繰り返しは 1）：1 種あたり平均 {mean(trans):.1f}（線 ≧5）→ {"通過" if t8 else "外れ"}　種別 {trans}　〔記録：移りの回数の合計 1 種あたり平均 {mean(transall):.1f}〕')
    P(f'T9 金の願いが立ったまま法が金でない状態が年末で連続 5 回以上続いた国×種：1 種あたり平均 {mean(stuck):.1f}（門が止めた型を名乗るには ≧5）　種別 {stuck}')

    # ---- 採否
    ok_core = (t1 is not None and t1 < 270) and t2 and t4 and t8
    t3ok = (t3 == '通過') or (t3.startswith('門が止めた') and mean(stuck) >= 5)
    P(f'採否の読み（[14] の表）：T1∧T2∧T4∧T8 = {"成立" if ok_core else "不成立"}・T3 = {"成立" if t3ok else "不成立"} → {"採る（旗の既定を 3 へ）" if ok_core and t3ok else "採らない"}')

    # ---- 点検（[13]① と [14]）
    P()
    P('== 点検')
    P(f'下げる目標が書かれた行（antiTargetKept ＜ antitrustLawCode）：{chk_below}/{chk_rows} 行（0 が正しい）')
    dec = 0; stk = 0; tot = 0
    for s, per in seq.items():
        for n, lst in per.items():
            prev = None
            for yy, r in lst:
                c = int(r['antitrustLawCode']); tot += 1
                if prev is not None and c < prev: dec += 1
                if int(r['antiTargetKept']) > c: stk += 1
                prev = c
    P(f'反トラストの法が下がった年末の数（antitrustLawCode の減少）：{dec}（0 のはず）・目標が現行より上に張り付いた国×年末（antiTargetKept ＞ 現行）：{stk}/{tot}')
    cross = 0
    for s, per in antiq.items():
        for n, lst in per.items():
            for (q0, u0), (q1, u1) in zip(lst, lst[1:]):
                if u0 > 1.0 / 3.0 >= u1: cross += 1
    P(f'本国の都市の割合が 1/3 を上から下へ割った回数（全行の連続する 2 行・挙げた国は四半期・他は年末）：{cross}（戦時の兵士の分母・1870 年以後の下落の影響の記録）')

    # ---- R1〜R5
    P()
    P('== 記録')
    ever = {}
    for p in seeds(d, 'reform'):
        s = sid(p); e = {}
        for r in rows(p):
            if int(r['quarter']) % 4 == 3 and float(r['literacyShare']) > line: e.setdefault(r['nation'], int(r['year']))
        ever[s] = e
    if ever:
        P(f'R5 識字＞{line} を一度でも超えた国の数（reform_s・見積もりの下限側）：' + '  '.join(f'{y}={mean(sum(1 for v in e.values() if v <= y) for e in ever.values()):.1f}' for y in YEARS))
    first = collections.defaultdict(lambda: collections.defaultdict(list))
    for s, per in seq.items():
        for n, lst in per.items():
            dd = {}
            for yy, r in lst:
                for col, ok in (('goldWishOld', r['goldWishOld'] == '1'), ('goldWishNew', r['goldWishNew'] == '1'), ('goldKept', r['goldTargetKept'] == '2'),
                                ('goldLaw', r['goldLawCode'] == '2'), ('antiOld', r['antiTargetOld'] != '-1'), ('antiNew', r['antiTargetNew'] != '-1'),
                                ('antiLaw', r['antitrustLawCode'] != '0')):
                    if ok: dd.setdefault(col, yy)
            for col, yy in dd.items(): first[n][col].append(yy)
    cols = ('goldWishOld', 'goldWishNew', 'goldKept', 'goldLaw', 'antiOld', 'antiNew', 'antiLaw')
    P('R2 国ごとの最初の年末（種の中央値／何種で起きたか）：国\t' + '\t'.join(cols))
    for n in sorted(first):
        P(n + '\t' + '\t'.join((f'{st.median(first[n][c]):.0f}/{len(first[n][c])}' if first[n].get(c) else '-') for c in cols))
    face = collections.Counter(); fl = collections.defaultdict(list)
    for s, per in seq.items():
        for n, lst in per.items():
            rr = next((r for yy, r in lst if yy == 1913), None)
            if rr is not None and int(rr['goldLawCode']) == 2:
                face[n] += 1; fl[n].append(float(rr['literacyHome']))
    P(f'R-顔ぶれ 1913 年の年末に金の法の国（種数／{nseed}・識字の中央値）：' + '・'.join(f'{n} {c}/{nseed}（{st.median(fl[n]):.2f}）' for n, c in sorted(face.items(), key=lambda x: -x[1])))
    P('R-移った国の名簿（銀→金・種数の合計）：' + '・'.join(f'{n} {c}' for n, c in trans_names.most_common(20)))
    P('R-止まった国の名簿（T9）：' + '・'.join(f'{n} {c}' for n, c in stuck_names.most_common(20)))

main()
