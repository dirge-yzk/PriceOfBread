# 流れG (350) G3 の決め直し：計器つき 10 種の disc_s／disccontact_s を A・B・N1・N2 で並べて読む（読むだけ・事前登録 measurements/g3_20261015_prereg.txt の [4] の線）。
# 使い方：PYTHONUTF8=1 python an_g3.py <A の ins> <B の ins> <N1 の ins> <N2 の ins>
#   （A だけ・A と B だけでも動く：N が無ければ雑音の床は下限の 5 年だけ）
# 出す物：L1（薄い国の線・窓 1784/1788/1792）・L2（清）・L3（工業国の後退・史実の幅からの距離・雑音の床）・L3b（記録）・R1（1783〜92 年の出来事の数）・R2（伝わりの相手・世界で蒸気機関を持つ国の数）。
import sys, glob, os, re, statistics, collections

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from an_dg2 import Arm, fl, med   # 読むだけの共通部（disc_s の E 行・T 行）

CENSOR = 1950
# 工業国 8 か国と史実の幅 [lo, hi]（【推・AI の記憶・出典未確認】＝事前登録 [3]）
# 止める線に使う幅（訂正 A2）：幅の内の 6 国は [lo, 1830]、西・オスマンは [1830, 1850]。国ごとの hi（元の [3]）は NARROW に落とす（記録 L3n）。
INDUSTRIAL = {
    'フランス': (1781, 1830), 'プロイセン': (1785, 1830), 'オーストリア': (1790, 1830), 'ロシア': (1790, 1830),
    'アメリカ': (1790, 1830), 'スペイン': (1830, 1850), 'オスマン': (1830, 1850), 'ネーデルラント': (1780, 1830),
}
NARROW = {
    'フランス': (1781, 1810), 'プロイセン': (1785, 1810), 'オーストリア': (1790, 1820), 'ロシア': (1790, 1820),
    'アメリカ': (1790, 1830), 'スペイン': (1830, 1850), 'オスマン': (1830, 1850), 'ネーデルラント': (1780, 1830),
}
CENS_MIN = 5   # 訂正 A4：打ち切りの種がこの数以上の国は解禁した種だけの中央値も見る
FLOOR_MIN = 5


def first_year(arm, sd, nation, row='蒸気機関'):
    y = arm.year(sd, nation, row)
    return y if y is not None else CENSOR


def med_year(arm, nation, row='蒸気機関'):
    return statistics.median([first_year(arm, sd, nation, row) for sd in arm.seeds])


def dist(m, lo, hi):
    return max(0, lo - m, m - hi)


def load_contact(d):
    out = collections.defaultdict(list)
    for f in sorted(glob.glob(os.path.join(d, 'disccontact_s*.tsv'))):
        sd = int(re.search(r'disccontact_s(\d+)\.tsv', f).group(1))
        with open(f, encoding='utf-8') as fh:
            hdr = fh.readline().rstrip('\n').split('\t')
            for l in fh:
                p = l.rstrip('\n').split('\t')
                if len(p) == len(hdr):
                    out[sd].append(dict(zip(hdr, p)))
    return out


def l1_table(label, a, base):
    print(f'## {label}（{len(a.seeds)}種）：蒸気機関を解禁した英以外の国の数（種ごと）')
    print('種 | 〜1784 | 〜1788 | 〜1792 | 清の最初の年 | うち 1783-92 で伝わり主導（transmitTerm>0）')
    for sd in a.seeds:
        n92 = a.steam_nations(sd, 1792)
        tr = sum(1 for n in n92 if fl(a.E[(sd, n, '蒸気機関')]['transmitTerm']) > 0)
        print(f"{sd} | {len(a.steam_nations(sd, 1784))} | {len(a.steam_nations(sd, 1788))} | {len(n92)} | {a.year(sd, '清', '蒸気機関')} | {tr}")
    if base is not None and a is not base:
        seeds = [s for s in base.seeds if s in a.seeds]
        less = {w: sum(1 for s in seeds if len(a.steam_nations(s, w)) < len(base.steam_nations(s, w))) for w in (1784, 1788, 1792)}
        half = sum(1 for s in seeds if len(a.steam_nations(s, 1792)) * 2 <= len(base.steam_nations(s, 1792)))
        l2 = sum(1 for s in seeds if (a.year(s, '清', '蒸気機関') or 9999) <= 1784)
        print(f"L1：A より減った種 〜1784 {less[1784]}/{len(seeds)}・〜1788 {less[1788]}/{len(seeds)}・〜1792 {less[1792]}/{len(seeds)}（線＝〜1792 で 10/10）・〜1792 が A の 1/2 以下の種 {half}/{len(seeds)}（線＝8 以上）")
        print(f"L2：清の蒸気機関が 1783〜84 の種 {l2}/{len(seeds)}（線＝3 以下）")


def unlocked_med(arm, nation, row='蒸気機関'):
    ys = [arm.year(sd, nation, row) for sd in arm.seeds]
    ys = [y for y in ys if y is not None]
    return (statistics.median(ys), len(ys)) if ys else (None, 0)


def censored(arm, nation, row='蒸気機関'):
    return sum(1 for sd in arm.seeds if arm.year(sd, nation, row) is None)


def l3(arms, names):
    """訂正 A1（生の差の床）・A2（保守的な幅）・A4（打ち切りの段差は両方の判定が後退のときだけ止める）。"""
    A = arms[0]
    print('\n## L3（工業国の後退・訂正後）：m＝種の中央値の最初の年（打ち切り 1950）・dist＝幅（止める線の幅）からの距離・d＝dist(arm)−dist(A)')
    print("   床＝max(5, 2×max|m(N)−m(A)|)（生の差）・参考の dist 版の床＝max(5, 2×max|d(N)|)・m'＝解禁した種だけの中央値（打ち切り 5 以上の国）")
    nulls = [(n, a) for n, a in zip(names[1:], arms[1:]) if n.upper().startswith('N')]
    arm_b = [(n, a) for n, a in zip(names[1:], arms[1:]) if not n.upper().startswith('N')]
    hdr = '国 | 幅 | m(A) dist(A) 打切(A)'
    for n, _ in arm_b + nulls: hdr += f' | m({n}) d({n}) 打切({n})'
    hdr += ' | 床(生) | 床(dist・参考)'
    print(hdr)
    regress = collections.defaultdict(list)
    suspect = collections.defaultdict(list)
    for nm, (lo, hi) in INDUSTRIAL.items():
        mA = med_year(A, nm); dA = dist(mA, lo, hi); cA = censored(A, nm)
        raw = [abs(med_year(a, nm) - mA) for n, a in nulls]
        dn = [abs(dist(med_year(a, nm), lo, hi) - dA) for n, a in nulls]
        floor = max(FLOOR_MIN, 2 * max(raw)) if raw else FLOOR_MIN
        floor_d = max(FLOOR_MIN, 2 * max(dn)) if dn else FLOOR_MIN
        uA, nA = unlocked_med(A, nm)
        uraw = []
        for n, a in nulls:
            u, k = unlocked_med(a, nm)
            if u is not None and uA is not None: uraw.append(abs(u - uA))
        ufloor = max(FLOOR_MIN, 2 * max(uraw)) if uraw else FLOOR_MIN
        row = f'{nm} | {lo}-{hi} | {mA:g} {dA:g} {cA}'
        for n, a in arm_b + nulls:
            m = med_year(a, nm); d = dist(m, lo, hi) - dA; c = censored(a, nm)
            row += f' | {m:g} {d:+g} {c}'
        for n, a in arm_b:
            m = med_year(a, nm); d = dist(m, lo, hi) - dA; c = censored(a, nm)
            if d > floor:
                if max(cA, c) >= CENS_MIN:
                    u, k = unlocked_med(a, nm)
                    if u is None or uA is None:
                        regress[n].append(nm)
                    else:
                        du = dist(u, lo, hi) - dist(uA, lo, hi)
                        row += f" | m'(A)={uA:g}({nA}) m'({n})={u:g}({k}) d'={du:+g} 床'={ufloor:g}"
                        if du > ufloor: regress[n].append(nm)
                        else: suspect[n].append(nm)
                else:
                    regress[n].append(nm)
        print(row + f' | {floor:g} | {floor_d:g}')
    for n, _ in arm_b:
        print(f"L3 {n}：後退の国 {len(regress[n])}（{'・'.join(regress[n]) or '—'}）＝{'★止める（正しい挙動の後退）' if regress[n] else '合格'}"
              + (f"／打ち切りの段差の疑い（止めずに記録）{'・'.join(suspect[n])}" if suspect[n] else ''))
    print('\n## L3n（記録・止める線にしない）：国ごとの hi（元の事前登録 [3]）での後退')
    for n, a in arm_b:
        bad = []
        for nm, (lo, hi) in NARROW.items():
            mA = med_year(A, nm); m = med_year(a, nm)
            raw = [abs(med_year(x, nm) - mA) for _, x in nulls]
            fl_ = max(FLOOR_MIN, 2 * max(raw)) if raw else FLOOR_MIN
            if dist(m, lo, hi) - dist(mA, lo, hi) > fl_: bad.append(f'{nm}({mA:g}→{m:g})')
        print(f"  {n}：{len(bad)} 国 {'・'.join(bad) or '—'}")
    return arm_b, nulls


def l3e(arms, names):
    """訂正 A3：早まる側の線。工業国 8 か国×{蒸気機関・機関車}で、A より早まった量がヌル腕の生の差の床を超える国×行が 3 以上なら止める。"""
    A = arms[0]
    nulls = [a for n, a in zip(names[1:], arms[1:]) if n.upper().startswith('N')]
    arm_b = [(n, a) for n, a in zip(names[1:], arms[1:]) if not n.upper().startswith('N')]
    print('\n## L3e（早まる側・工業国 8 か国×{蒸気機関・機関車}）：A−arm の中央値の差（正＝早まった）が床＝max(5, 2×max|m(N)−m(A)|) を超える国×行')
    for n, a in arm_b:
        hit = []
        for nm in INDUSTRIAL:
            for row in ('蒸気機関', '機関車'):
                mA = med_year(A, nm, row); m = med_year(a, nm, row)
                raw = [abs(med_year(x, nm, row) - mA) for x in nulls]
                fl_ = max(FLOOR_MIN, 2 * max(raw)) if raw else FLOOR_MIN
                if mA - m > fl_: hit.append(f'{nm}{row}({mA:g}→{m:g}・床{fl_:g})')
        print(f"L3e {n}：早まった国×行 {len(hit)}（{'・'.join(hit) or '—'}）＝{'★止める（早すぎる発見）' if len(hit) >= 3 else '合格'}")


def r6(arms, names):
    """訂正 A3：蒸気機関以外の行の最初の年を（種×国×行）の対で A と並べる（記録）。"""
    A = arms[0]
    print('\n## R6（記録）：蒸気機関以外の行の最初の年の対（A→arm）：5 年以上早まった・5 年以上遅れた・腕だけ解禁・A だけ解禁')
    for n, a in zip(names[1:], arms[1:]):
        seeds = [s for s in A.seeds if s in a.seeds]
        early = late = onlyarm = onlyA = 0
        byrow = collections.defaultdict(lambda: [0, 0, 0, 0])
        keys = {k for k in A.E if k[0] in seeds and k[2] != '蒸気機関'} | {k for k in a.E if k[0] in seeds and k[2] != '蒸気機関'}
        for k in keys:
            rb, ra = A.E.get(k), a.E.get(k)
            if rb and ra:
                d = int(ra['year']) - int(rb['year'])
                if d <= -5: early += 1; byrow[k[2]][0] += 1
                elif d >= 5: late += 1; byrow[k[2]][1] += 1
            elif ra: onlyarm += 1; byrow[k[2]][2] += 1
            else: onlyA += 1; byrow[k[2]][3] += 1
        print(f'  {n}: 早まり {early}・遅れ {late}・腕だけ {onlyarm}・A だけ {onlyA}')
        top = sorted(byrow.items(), key=lambda kv: -(kv[1][0] + kv[1][2]))[:12]
        print('    行別（早まり＋腕だけの多い順 12）：' + '・'.join(f'{r}[早{v[0]}/遅{v[1]}/腕のみ{v[2]}/Aのみ{v[3]}]' for r, v in top))


def l3b(arms, names):
    A = arms[0]
    print('\n## L3b（記録）：解禁した種の数（〜1950）・最初の年の中央値が A の ±15 年の外・機関車の遅れ')
    for n, a in zip(names[1:], arms[1:]):
        for nm in list(INDUSTRIAL):
            cA = sum(1 for s in A.seeds if A.year(s, nm, '蒸気機関')); ca = sum(1 for s in a.seeds if a.year(s, nm, '蒸気機関'))
            mA = med_year(A, nm); ma = med_year(a, nm)
            lA = med_year(A, nm, '機関車'); la = med_year(a, nm, '機関車')
            flag = (' 〜1950 の種が −2 超' if ca < cA - 2 else '') + (' 中央値 ±15 年の外' if abs(ma - mA) > 15 else '') + (' 機関車が 15 年超遅れ' if la - lA > 15 else '')
            print(f'  {n} {nm}: 解禁した種 {cA}→{ca}・蒸気機関の中央値 {mA:g}→{ma:g}・機関車 {lA:g}→{la:g}{flag}')


def r1(arms, names):
    print('\n## R1（1783〜92 年の全行の出来事の数・種ごと）／蒸気機関を持つ国の数（英以外・種の中央値）')
    for n, a in zip(names, arms):
        cnt = [sum(1 for (s, nat, row), r in a.E.items() if s == sd and int(r['year']) <= 1792) for sd in a.seeds]
        holders = {y: statistics.median([len(a.steam_nations(sd, y)) for sd in a.seeds]) for y in (1790, 1800, 1820, 1900)}
        print(f'  {n}: 出来事 {cnt}（中央 {statistics.median(cnt)}）・蒸気機関を持つ国 {holders}')


def r2(dirs, names, arms):
    A = arms[0]
    print('\n## R2（伝わりの相手・訂正 A6）：工業国が蒸気機関を解禁する前に運び手の財を買った売り手の構成（disccontact_s・種の合計）')
    print('  売り手の分類(1)：英／その種で A が 1783〜84 に蒸気機関を解禁した国（A薄）／その他。分類(2)：英／工業国 8 か国／その他')
    for d, n, a in zip(dirs, names, arms):
        con = load_contact(d)
        t1 = collections.Counter(); t2 = collections.Counter()
        for nm in INDUSTRIAL:
            for sd in a.seeds:
                y = a.year(sd, nm, '蒸気機関') or CENSOR
                thin = A.steam_nations(sd, 1784) if sd in A.seeds else set()
                for r in con.get(sd, []):
                    if r['buyer'] != nm or r['row'] != '蒸気機関' or int(r['year']) > y: continue
                    v = fl(r['valuePerQuarter']); seller = r['seller']
                    t1['英' if seller == 'イギリス' else ('A薄' if seller in thin else 'その他')] += v
                    t2['イギリス' if seller == 'イギリス' else ('工業国' if seller in INDUSTRIAL else 'その他')] += v
        s1 = sum(t1.values()) or 1; s2 = sum(t2.values()) or 1
        print(f"  {n}: (1) 英 {t1['英'] / s1:.1%}・A薄 {t1['A薄'] / s1:.1%}・その他 {t1['その他'] / s1:.1%}／(2) イギリス {t2['イギリス'] / s2:.1%}・工業国 {t2['工業国'] / s2:.1%}・その他 {t2['その他'] / s2:.1%}（合計 {s2:.3g}）")


def r4r5(arms, names):
    print('\n## R4（港の門・蒸気機関の最初の年が 1783 の国の数〔英以外・種の中央値〕）／R5（建国した国の数・最初の 8 四半期の E 行）')
    for n, a in zip(names, arms):
        c83 = [sum(1 for (s, nat, row), r in a.E.items() if s == sd and row == '蒸気機関' and int(r['year']) == 1783 and nat != 'イギリス') for sd in a.seeds]
        new = {(s, nat): q for (s, nat), q in a.first_q.items() if q > 1}
        e8 = sum(1 for (s, nat, row), r in a.E.items() if (s, nat) in new and int(r['quarter']) - new[(s, nat)] < 8)
        print(f'  {n}: 1783 に蒸気機関を解禁した国 {statistics.median(c83):g}（種ごと {c83}）・建国した国×種 {len(new)}・その最初の 8 四半期の E 行 {e8}')


if __name__ == '__main__':
    dirs = sys.argv[1:]
    names = ['A', 'B', 'N1', 'N2'][:len(dirs)]
    arms = [Arm(d) for d in dirs]
    for d, n, a in zip(dirs, names, arms):
        l1_table(f'{n} {d}', a, arms[0])
    if len(arms) > 1:
        l3(arms, names)
        l3e(arms, names)
        l3b(arms, names)
        r6(arms, names)
    r1(arms, names)
    r2(dirs, names, arms)
    r4r5(arms, names)
