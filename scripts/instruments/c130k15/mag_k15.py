# (1269) c130k15 史実の桁の線（この線が一番動かす量）の読み手。
#   使い方: python -X utf8 -I mag_k15.py B=<腕の ins> base=<基準の ins> [SB=1] [SE=10]
#   読む表：labor_s（plainUnemploymentShare・farmUnemploymentShare＝国の労働の市場の売れ残り＝POP の失業率の書き手 EconomyStep:2490・:2502 の元）
#           class_s（階級の大きさ＝年の最後の四半期の行）。どちらも ident.cfg の SMALL に入っている。
#   量：
#     U(国,年)＝plainUnemploymentShare のその年の四半期の平均（9 国＝四半期の行のある国）。種ごと→種の中央。
#     F(国,年)＝工場労働者（classId 2）の大きさ÷全階級の和（年の最後の四半期の行）。
#     R(国,年)＝½Σ_階級 |s_c(年+1) − s_c(年)|（階級の構成の 1 年の入れ替わり＝社会移動の流れの正味の代わり）。
#   門（事前登録 measurements/m1269_20261121_prereg_k15.txt §3-2）：
#     H-a：U の腕の種の中央÷史実（下の HIST・全て〈推〉）が 10 以上か 0.1 以下で、基準の同じ比が (0.1,10) の内のとき赤。
#           基準がすでに外なら〈元から〉＝記録。
#     H-b：U（基準 ≧0.005 の行だけ）と R（基準 ≧1e-4 の行だけ）の 新÷旧（種の中央どうし）が 10 以上か 0.1 以下なら赤。
#           基準が床の下の行は記録（比を名乗らない）。
#     F は記録（新÷旧）。S＝兵士（classId 5）の割合も記録。
import sys, os, statistics as st

args = dict(a.split('=', 1) for a in sys.argv[1:])
B, BASE = args['B'], args['base']
SB, SE = int(args.get('SB', 1)), int(args.get('SE', 10))
SEEDS = range(SB, SE + 1)
YEARS = (1850, 1913)
NAT = ['ロシア', '清', 'オスマン', 'イギリス', 'フランス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
# 史実の失業率（〈推〉＝全て AI の記憶の値・出典の名だけ・桁の線にだけ使う）
HIST = {
    ('イギリス', 1850): (0.04, '英 1850 前後 約 4%〈推〉（Feinstein 1972 の組合の失業の系列の 1850 年代）'),
    ('イギリス', 1913): (0.021, '英 1913 約 2.1%〈推〉（商務院の組合の失業の報告）'),
    ('プロイセン', 1913): (0.029, '独 1913 約 2.9%〈推〉（組合の失業の報告）'),
    ('フランス', 1913): (0.047, '仏 1913 約 4.7%〈推〉（組合の失業の報告）'),
    ('アメリカ', 1913): (0.043, '米 1913 約 4.3%〈推〉（Lebergott 1964）'),
}
UFLOOR, RFLOOR = 0.005, 1e-4


def rows(path):
    if not os.path.exists(path):
        return None, []
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        return h, [l.rstrip('\n').split('\t') for l in f]


def fl(x):
    try:
        v = float(x)
        return v if v == v else None
    except ValueError:
        return None


def read_seed(ins, s):
    U, F, R, S = {}, {}, {}, {}
    h, rs = rows(f'{ins}/labor_s{s}.tsv')
    if h:
        iy, inn, iu = h.index('year'), h.index('nation'), h.index('plainUnemploymentShare')
        acc = {}
        for r in rs:
            y = int(r[iy])
            if y in YEARS and r[inn] in NAT:
                v = fl(r[iu])
                if v is not None:
                    acc.setdefault((r[inn], y), []).append(v)
        for k, v in acc.items():
            U[k] = sum(v) / len(v)
    h, rs = rows(f'{ins}/class_s{s}.tsv')
    if h:
        it, iy, inn, ic, isz = h.index('time'), h.index('year'), h.index('nation'), h.index('classId'), h.index('sizeK')
        want = set(YEARS) | {y + 1 for y in YEARS}
        last = {}   # (nation, year) -> (quarter, {cls: size})
        for r in rs:
            if r[it] == 'open' or r[inn] not in NAT:
                continue
            y = int(r[iy])
            if y not in want:
                continue
            q = int(r[it][1:])
            k = (r[inn], y)
            cur = last.get(k)
            if cur is None or q > cur[0]:
                cur = (q, {}); last[k] = cur
            if q == cur[0]:
                cur[1][int(r[ic])] = fl(r[isz]) or 0.0
        def shares(k):
            d = last.get(k)
            if not d:
                return None
            tot = sum(d[1].values())
            return {c: v / tot for c, v in d[1].items()} if tot > 0 else None
        for n in NAT:
            for y in YEARS:
                a, b = shares((n, y)), shares((n, y + 1))
                if a:
                    F[(n, y)] = a.get(2, 0.0)
                    S[(n, y)] = a.get(5, 0.0)
                if a and b:
                    R[(n, y)] = 0.5 * sum(abs(b.get(c, 0.0) - a.get(c, 0.0)) for c in set(a) | set(b))
    return U, F, R, S


def med(ins):
    acc = [{}, {}, {}, {}]
    for s in SEEDS:
        for i, d in enumerate(read_seed(ins, s)):
            for k, v in d.items():
                acc[i].setdefault(k, []).append(v)
    return [{k: st.median(v) for k, v in a.items()} for a in acc], [{k: len(v) for k, v in a.items()} for a in acc]


(mb, nb), (mo, no) = med(B), med(BASE)
red_a = red_b = 0
print(f'# mag_k15 腕={B} 基準={BASE} 種 {SB}〜{SE}')
print('== H-a 失業率 U（plainUnemploymentShare の年の平均・種の中央）÷史実〈推〉')
for (n, y), (hv, src) in HIST.items():
    a, o = mb[0].get((n, y)), mo[0].get((n, y))
    if a is None or o is None:
        print(f'{n}\t{y}\t欠け（腕 {a}・基準 {o}）\t{src}'); continue
    ra, ro = a / hv, o / hv
    inA, inO = 0.1 < ra < 10, 0.1 < ro < 10
    tag = '緑' if inA else ('記録〈元から〉' if not inO else '赤')
    if tag == '赤':
        red_a += 1
    print(f'{n}\t{y}\t腕 {a:.4f}（÷史実 {ra:.2f}）\t基準 {o:.4f}（÷史実 {ro:.2f}）\t{tag}\t{src}')
print('== H-b 新÷旧（種の中央どうし）：U（基準 ≧0.005）・R＝階級の構成の 1 年の入れ替わり（基準 ≧1e-4）')
for lab, i, floor in (('U', 0, UFLOOR), ('R', 2, RFLOOR)):
    for n in NAT:
        for y in YEARS:
            a, o = mb[i].get((n, y)), mo[i].get((n, y))
            if a is None or o is None:
                print(f'{lab}\t{n}\t{y}\t欠け（腕 {a}・基準 {o}）'); continue
            if o < floor:
                print(f'{lab}\t{n}\t{y}\t腕 {a:.5f}\t基準 {o:.5f}\t記録〈基準が床の下〉'); continue
            r = a / o
            tag = '赤' if (r >= 10 or r <= 0.1) else '緑'
            if tag == '赤':
                red_b += 1
            print(f'{lab}\t{n}\t{y}\t腕 {a:.5f}\t基準 {o:.5f}\t新÷旧 {r:.3f}\t{tag}\t種 {nb[i].get((n, y))}/{no[i].get((n, y))}')
print('== 記録 F＝工場労働者の割合（年の最後の四半期・種の中央）新÷旧')
for n in NAT:
    for y in YEARS:
        a, o = mb[1].get((n, y)), mo[1].get((n, y))
        if a is None or o is None:
            print(f'F\t{n}\t{y}\t欠け'); continue
        print(f'F\t{n}\t{y}\t腕 {a:.4f}\t基準 {o:.4f}\t新÷旧 {a / o if o > 0 else float("nan"):.3f}')
print('== 記録 S＝兵士（classId 5）の割合（年の最後の四半期・種の中央）新÷旧＝兵の行の forced の道（RouteViaPool）も同じ eff を読む')
for n in NAT:
    for y in YEARS:
        a, o = mb[3].get((n, y)), mo[3].get((n, y))
        if a is None or o is None:
            print(f'S	{n}	{y}	欠け'); continue
        print(f'S	{n}	{y}	腕 {a:.5f}	基準 {o:.5f}	新÷旧 {a / o if o > 0 else float("nan"):.3f}')
print(f'判定 H-a 赤 {red_a}・H-b 赤 {red_b}（門 H は両方 0 で緑）')
