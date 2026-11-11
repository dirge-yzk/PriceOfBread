# (952) c84kp 紙の台本：開幕の一括昇格の植民地の支配度（旗 OpeningColonyDominanceFull）の門 G1〜G4 を腕の生から数える。
#   python scripts/instruments/c84kp/paper952.py <腕の名=生のフォルダ> ...   例 base=D:/PoB_work/keep_947/base_ins n1=D:/PoB_work/keep_947/n1
#   読む表：secession_s<種>.tsv（quarter・nation・region・tier・dominanceShare）と cederule_s<種>.tsv（quarter・how・sender・region・tierBefore）。
#   エンジンの関数は呼ばない（表を自分で読む）。種は 1〜20 のうち表がある物。
# 定義（事前登録 measurements/m952_20261108_prereg_kp.txt と同じ）：
#   開幕の植民地格の州＝secession_s で四半期<4 に tier=Colonial で現れた（国・州）の最初の行。G1＝その dominanceShare の中央（全ての州×種）。
#     国ごとの中央も出す（突然変異 M3〈蘭だけ 1.0〉を拾う G1b＝国ごとの中央の最小）。
#   G2＝cederule_s の how=Seceded・quarter<40・tierBefore=Colonial の件数（10 種の合計）。
#   G3＝四半期>=4 で初めて現れた州の名（どの国でも 1 度も出ていない名）で tier=Colonial の最初の行の dominanceShare の中央（接収＝Diplomacy.cs:2269 の母集合）。
#   G4＝cederule_s の how=Seceded・quarter>=40 の件数（10 種の合計）。
#   国ごとの表（蘭・英・西・葡）：開幕の植民地格の州の数・その中央・Seceded（sender＝その国・tierBefore=Colonial）の q<40 と q>=40・独立の確定（その国の開幕の植民地の州が後で tier=Core の行を持つ件数）。
import sys, os, statistics as st
from collections import defaultdict

FOCUS = ['ネーデルラント', 'イギリス', 'スペイン', 'ポルトガル']
WIN = 40
# 門の数（事前登録と同じ）：G1 ≧ 0.9・G2 ≦ 17（m947 のヌル腕 3 本の最小 35 の半分の切り捨て）・G3 ＜ 0.05
G1_MIN, G2_MAX, G3_MAX = 0.9, 17, 0.05
# 突然変異の紙：起点の支配度（開幕の植民地は国の関数・接収は定数）。M4（Tier も Core）は支配度の起点が旗 1 と同じ＝計器の門では見えない（試験で読む）。
MODES = {
    '旗0（M5）': (lambda n: 0.0, 0.0),
    '旗1（形）': (lambda n: 1.0, 0.0),
    'M1 全ての昇格を 1.0': (lambda n: 1.0, 1.0),
    'M2 開幕を 0.5': (lambda n: 0.5, 0.0),
    'M3 蘭だけ 1.0': (lambda n: 1.0 if n == 'ネーデルラント' else 0.0, 0.0),
}


def med(xs):
    return st.median(xs) if xs else float('nan')


def seeds_of(d):
    out = []
    for s in range(1, 21):
        if os.path.exists(os.path.join(d, f'secession_s{s}.tsv')) and os.path.exists(os.path.join(d, f'cederule_s{s}.tsv')):
            out.append(s)
    return out


def read_arm(d):
    R = dict(seeds=[], g1=[], g1p=[], g3p=[], modes={}, g1nat=defaultdict(list), g3=[], g3n=0, g3_ge05=0, g2=0, g4=0, g2_by=defaultdict(int), g2_seed=[], g4o=0, g4x=0, nat03=[],
             nat=defaultdict(lambda: dict(cols=0, sec10=0, sec40=0, indep=0, doms=[], ind2=0)))
    for s in seeds_of(d):
        R['seeds'].append(s)
        seen = set(); opening = {}   # (nat, reg) -> dom
        first_late = {}               # reg -> dom（q>=4 で初めて出た名・Colonial）
        core_after = set()            # (開幕の国, reg) で後に Core の行
        reg_open_nat = {}
        nats03 = set()                # 四半期<=3 に行を持つ国（G3 の〈初めて出た名〉の前提の確かめ）
        quarterly = set()             # 年の末以外の行を持つ国（四半期の行）
        mrow = {}                     # 最初の行の marginIndex（予言用）
        with open(os.path.join(d, f'secession_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inat, ireg, itier, idom = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['dominanceShare']
            im = ix['marginIndex']
            for line in f:
                a = line.rstrip('\n').split('\t')
                q = int(a[iq]); reg = a[ireg]; tier = a[itier]
                if q % 4 != 3: quarterly.add(a[inat])
                try:
                    dv = float(a[idom])
                except ValueError:
                    dv = None
                if q < 4:
                    nats03.add(a[inat])
                    if tier == 'Colonial' and (a[inat], reg) not in opening and dv is not None:
                        opening[(a[inat], reg)] = dv; reg_open_nat[reg] = a[inat]
                        mrow[('o', a[inat], reg)] = (q, a[im])
                else:
                    if reg not in seen and tier == 'Colonial' and reg not in first_late and dv is not None:
                        first_late[reg] = dv
                        mrow[('l', a[inat], reg)] = (q, a[im])
                    if tier == 'Core' and reg in reg_open_nat:
                        core_after.add((reg_open_nat[reg], reg))
                seen.add(reg)
        # 予言：1.0 から始めたときの最初の行＝clamp01(1 + margin×0.025×k)（k＝四半期の国 1・年の末の行の国 4＝年の分解能の近似）
        def pred(kind, n, r, start=1.0):
            q, m = mrow[(kind, n, r)]
            try: m = float(m)
            except ValueError: return None
            k = 1 if n in quarterly else 4
            x = start + m * 0.025 * k
            return 0.0 if not (x > 0) else (1.0 if x > 1 else x)
        for (n, r) in opening:
            v = pred('o', n, r)
            if v is not None: R['g1p'].append(v)
        for (kk, n, r) in list(mrow):
            if kk == 'l':
                v = pred('l', n, r)
                if v is not None: R['g3p'].append(v)
        # 突然変異ごとの紙（起点の支配度＝開幕の植民地 S_o(国)・接収 S_l）
        for mode, (so, sl) in MODES.items():
            mm = R['modes'].setdefault(mode, dict(g1=[], g3=[]))
            for (n, r) in opening:
                v = pred('o', n, r, so(n))
                if v is not None: mm['g1'].append(v)
            for (kk, n, r) in list(mrow):
                if kk == 'l':
                    v = pred('l', n, r, sl)
                    if v is not None: mm['g3'].append(v)
        for (n, r), dv in opening.items():
            R['g1'].append(dv); R['g1nat'][n].append(dv)
            if n in FOCUS:
                R['nat'][n]['cols'] += 1; R['nat'][n]['doms'].append(dv)
        for (n, r) in core_after:
            if n in FOCUS: R['nat'][n]['indep'] += 1
        R['g3'] += list(first_late.values())
        R['nat03'].append(len(nats03))
        open_names = {r for (n, r) in opening}
        g2s = 0; sec_by = {}; returned = set()
        with open(os.path.join(d, f'cederule_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                a = line.rstrip('\n').split('\t')
                if a[ix['how']] == 'Returned':
                    returned.add(a[ix['region']]); continue
                if a[ix['how']] != 'Seceded': continue
                q = int(a[ix['quarter']]); snd = a[ix['sender']]; tb = a[ix['tierBefore']]
                if tb == 'Colonial' and snd in FOCUS: sec_by[a[ix['region']]] = snd
                if q < WIN:
                    if tb == 'Colonial':
                        R['g2'] += 1; g2s += 1; R['g2_by'][snd] += 1
                        if snd in FOCUS: R['nat'][snd]['sec10'] += 1
                else:
                    R['g4'] += 1
                    if a[ix['region']] in open_names: R['g4o'] += 1
                    else: R['g4x'] += 1
                    if tb == 'Colonial' and snd in FOCUS: R['nat'][snd]['sec40'] += 1
        R['g2_seed'].append(g2s)
        # 独立の確定（近似）＝Seceded の州のうち、後で Returned の行が無い州（cederule_s だけで数える・再び割譲された州は区別しない）
        for reg, snd in sec_by.items():
            if reg not in returned: R['nat'][snd]['ind2'] += 1
    R['g3n'] = len(R['g3']); R['g3_ge05'] = sum(1 for x in R['g3'] if x >= 0.05)
    return R


def main():
    arms = []
    for a in sys.argv[1:]:
        k, v = a.split('=', 1); arms.append((k, v))
    res = {k: read_arm(v) for k, v in arms}
    print('# (952) 紙の台本 paper952.py の出力')
    print('# 予言（旗 1・紙）：G1p＝開幕の植民地の最初の行を 1.0 から積み直した中央／M1 の G3p＝接収の最初の行を 1.0 から積み直した中央')
    for k, R in res.items():
        print(f"{k}\tG1p {med(R['g1p']):.4f}（{len(R['g1p'])}・0.9 未満 {sum(1 for x in R['g1p'] if x < 0.9)}）\tM1 の G3p {med(R['g3p']):.4f}（{len(R['g3p'])}）")
    print('腕\t種\tG1 開幕の植民地の最初の行の支配度の中央\t州×種\tG1b 国ごとの中央の最小（国）\tG2 q<40 Seceded Colonial\tG2 種ごと\tG3 接収の最初の行の中央\tG3 州×種\tG3 0.05 以上\tG4 q>=40 Seceded（全て）\tG4o 開幕の植民地の名\tG4x それ以外（G4 の母集合）\tq<=3 に行のある国（種ごと）')
    for k, R in res.items():
        nm = [(med(v), n) for n, v in R['g1nat'].items() if v]
        mn = min(nm) if nm else (float('nan'), '-')
        print(f"{k}\t{len(R['seeds'])}\t{med(R['g1']):.4f}\t{len(R['g1'])}\t{mn[0]:.4f}（{mn[1]}）\t{R['g2']}\t{','.join(map(str, R['g2_seed']))}\t{med(R['g3']):.4f}\t{R['g3n']}\t{R['g3_ge05']}\t{R['g4']}\t{R['g4o']}\t{R['g4x']}\t{','.join(map(str, R['nat03']))}")
    print()
    print('# 国ごと（蘭・英・西・葡）：開幕の植民地格の州×種・最初の行の支配度の中央・Seceded（tierBefore=Colonial）q<40・q>=40・後に Core の行（secession_s）・独立の確定の近似（cederule_s の Seceded のうち Returned の無い州）')
    print('腕\t国\t州×種\t中央\tq<40\tq>=40\tCore の行\t独立の確定（近似）')
    for k, R in res.items():
        for n in FOCUS:
            x = R['nat'][n]
            print(f"{k}\t{n}\t{x['cols']}\t{med(x['doms']):.4f}\t{x['sec10']}\t{x['sec40']}\t{x['indep']}\t{x['ind2']}")
    print()
    print('# 開幕の植民地格の州×種の国ごとの数と中央（全ての国）＝突然変異 M3 の紙')
    for k, R in res.items():
        tot = len(R['g1'])
        parts = sorted(((len(v), n, med(v)) for n, v in R['g1nat'].items()), reverse=True)
        print(k + '\t' + '・'.join(f'{n} {c}（{c / tot:.2f}・中央 {m:.3f}）' for c, n, m in parts[:8]))
    print()
    print('# 突然変異ごとの門の紙（G1・G3＝最初の行の margin から起点を替えて積み直した中央／G2＝起点が 0 のままの送り手の基線の件数＝起点 >0 の送り手の早い分離は 0 と置く（(946) の紙：10 年の積分の和の最小 −0.09 ＞ −0.5））')
    print('腕\t形\tG1\t判定\tG2（予言）\t判定\tG3\t判定')
    for k, R in res.items():
        for mode, (so, sl) in MODES.items():
            mm = R['modes'][mode]
            g1 = med(mm['g1']); g3 = med(mm['g3'])
            g2 = sum(c for n, c in R['g2_by'].items() if not (so(n) > 0))
            j = lambda ok: '緑' if ok else '赤'
            print(f"{k}\t{mode}\t{g1:.4f}\t{j(g1 >= G1_MIN)}\t{g2}\t{j(g2 <= G2_MAX)}\t{g3:.4f}\t{j(g3 < G3_MAX)}")
    print()
    print('# G2 の送り手の内訳（q<40・Colonial）')
    for k, R in res.items():
        print(k + '\t' + '・'.join(f'{n} {c}' for n, c in sorted(R['g2_by'].items(), key=lambda t: -t[1])))


if __name__ == '__main__':
    main()
