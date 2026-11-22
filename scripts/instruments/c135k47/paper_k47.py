# (1305) c135k47 紙の台本と史実の桁の線（読むだけ）：灯油の近代レシピ「石油灯油」を消す線が一番動かす量を recipeout_s（計器の 9 国）から読む。
#   使い方: python -X utf8 -I paper_k47.py <ins（D:/…）> <種の起点> [<基準の ins> <基準の種の起点>]
#   ① 年ごと（1850・1870・1913・1950）の 9 国の和：灯油の産出のレシピ別（既定＝鯨油・魚油／石油ケロシン／石油灯油）・原油の産出・
#      原油の使い（産出×投入の量＝石油ケロシン 0.5・石油灯油 1.0）÷9 国の原油の産出（9 国の外からの輸入があるので 1 を超えうる）・石油灯油の取り分・原油の単位費用（unitCostOutW の産出の重みの平均＝値段の近似・記録）
#   ② 史実の桁の線（門）：(a) 灯油の中の近代の灯油（石油の 2 本の和）の取り分 1913 年〈推 0.95〉・(b) 原油の産出の 1913÷1870〈推 約 65＝世界 0.8→52 Mt〉
#      ゲームの値÷史実が 10 倍以上か 1/10 以下なら線に当たる（種の中央値）。1870 年の近代の取り分〈推 0.5〉は記録。
#   ③ 記録：鯨油・魚油（既定）の取り分が 0.5 を下回る最初の年（種ごと・recipeout の年の刻み）
#   基準を渡すと同じ年の〈新÷旧〉を並べる。
import sys, os, csv, statistics as st, collections
YEARS = (1850, 1870, 1913, 1950)
HIST_MODERN_1913, HIST_CRUDE_GROWTH, HIST_MODERN_1870 = 0.95, 65.0, 0.5
CRUDE_PER_OUT = {'石油ケロシン': 0.5, '石油灯油': 1.0}   # 産出 1 単位あたりの原油（Input.QtyPerOutput）

def seeds(d, sb):
    return [s for s in range(sb, sb + 10) if os.path.exists(os.path.join(d, f'recipeout_s{s}.tsv'))]

def read(d, s):
    lamp = collections.defaultdict(lambda: collections.defaultdict(float))
    crude = collections.defaultdict(float); ccost = collections.defaultdict(float)
    with open(os.path.join(d, f'recipeout_s{s}.tsv'), encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if len(row) < len(h) or not row[0].isdigit(): continue
            y, g = int(row[0]), row[ix['good']]
            o = float(row[ix['outputSum']])
            if not (o == o): continue
            if g == '灯油': lamp[y][row[ix['recipe']]] += o
            elif g == '原油':
                crude[y] += o
                c = float(row[ix['unitCostOutW']])
                if c == c: ccost[y] += c * o
    return lamp, crude, {y: (ccost[y] / crude[y] if crude[y] > 0 else float('nan')) for y in crude}

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

def summary(d, sb):
    per = {}
    for s in seeds(d, sb):
        lamp, crude, cc = read(d, s)
        row = {}
        for y in sorted(lamp):
            tot = sum(lamp[y].values()); old = lamp[y].get('石油灯油', 0.0); kero = lamp[y].get('石油ケロシン', 0.0)
            use_old = old * CRUDE_PER_OUT['石油灯油']; use_k = kero * CRUDE_PER_OUT['石油ケロシン']
            row[y] = dict(tot=tot, dflt=lamp[y].get('(既定)', 0.0), kero=kero, old=old, crude=crude.get(y, 0.0), ccost=cc.get(y, float('nan')),
                          modern=(kero + old) / tot if tot > 0 else float('nan'), oldshare=old / tot if tot > 0 else float('nan'),
                          olduse=use_old / crude[y] if crude.get(y, 0) > 0 else float('nan'), kuse=use_k / crude[y] if crude.get(y, 0) > 0 else float('nan'))
        endy = next((y for y in sorted(row) if row[y]['tot'] > 0 and row[y]['dflt'] / row[y]['tot'] < 0.5), None)
        per[s] = (row, endy)
    return per

def show(tag, per):
    print(f'## {tag} 種={sorted(per)}')
    print('year\tlampTot\tdefault\tkerosene\tpetroLamp\tpetroLampShare\tmodernShare\tcrude\tpetroLampCrudeUseShare\tkeroCrudeUseShare\tcrudeUnitCost（種の中央値）')
    for y in YEARS:
        v = lambda k: med([per[s][0][y][k] for s in per if y in per[s][0]])
        print(f'{y}\t{v("tot"):.1f}\t{v("dflt"):.1f}\t{v("kero"):.1f}\t{v("old"):.4f}\t{v("oldshare"):.6f}\t{v("modern"):.4f}\t{v("crude"):.1f}\t{v("olduse"):.6f}\t{v("kuse"):.4f}\t{v("ccost"):.4f}')
    mx = max((per[s][0][y]['oldshare'] for s in per for y in per[s][0] if per[s][0][y]['oldshare'] == per[s][0][y]['oldshare']), default=float('nan'))
    print(f'石油灯油の取り分の最大（全ての種・年）={mx:.6f}')
    print('鯨油・魚油の取り分が 0.5 を下回る最初の年（種ごと）=' + ' '.join(f's{s}:{per[s][1]}' for s in sorted(per)))
    m13 = med([per[s][0][1913]['modern'] for s in per if 1913 in per[s][0]]); m70 = med([per[s][0][1870]['modern'] for s in per if 1870 in per[s][0]])
    gr = med([per[s][0][1913]['crude'] / per[s][0][1870]['crude'] for s in per if 1870 in per[s][0] and per[s][0][1870]['crude'] > 0])
    ra, rb = m13 / HIST_MODERN_1913, gr / HIST_CRUDE_GROWTH
    hit = lambda r: not (0.1 < r < 10.0)
    print(f'史実の桁 (a) 近代の灯油の取り分 1913={m13:.4f} 史実〈推〉{HIST_MODERN_1913} 比={ra:.3f} → {"当たる" if hit(ra) else "通る"}')
    print(f'史実の桁 (b) 原油の産出 1913÷1870={gr:.2f} 史実〈推〉{HIST_CRUDE_GROWTH} 比={rb:.3f} → {"当たる" if hit(rb) else "通る"}')
    print(f'記録 近代の灯油の取り分 1870={m70:.4f} 史実〈推〉{HIST_MODERN_1870} 比={m70 / HIST_MODERN_1870:.3f}')
    return dict(m13=m13, gr=gr)

if __name__ == '__main__':
    d, sb = sys.argv[1], int(sys.argv[2])
    a = summary(d, sb); show('腕', a)
    if len(sys.argv) > 4:
        b = summary(sys.argv[3], int(sys.argv[4])); show('基準', b)
        print('## 同じ年の新÷旧（種の中央値どうし）')
        for k in ('tot', 'kero', 'crude', 'ccost'):
            print(k + '\t' + '\t'.join(f'{y}:{med([a[s][0][y][k] for s in a if y in a[s][0]]) / med([b[s][0][y][k] for s in b if y in b[s][0]]):.4f}' for y in YEARS))
