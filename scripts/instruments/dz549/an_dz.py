# (549) 線 dz2：d≦0 の発見の行（蒸気機関）の初期値の読み手（読むだけ・採否に使わない）。
# 使い方：python -X utf8 an_dz.py <ラベル=出力フォルダ>... （各フォルダに disc_s<種>.tsv があること）
#   各ラベルについて、種ごとに disc_s の E 行（蒸気機関）から国別の解禁の年と、解禁した四半期の 3 項（技術力・必要性・伝わり）を読み、次を出す
#   （事前登録 measurements/dz549_20261025_prereg.txt の G1〜G3 と記録 R1〜R3 の定義と同じ）：
#   G1 英以外で 1786 年・1790 年・1800 年・1850 年までに解禁した国の数（種の値と中央値・最大）
#   G3 主要 8 国（仏・普・米・日・清・露・オスマン・墺）のうち 1850 年までに解禁した国の数と、任意の 5 年窓（y〜y+4）に入る数の最大（種の値と 10 種の中央値）
#   R1 主要 8 国の解禁の年（9999＝出力の期間内に未解禁）
#   R2 解禁した四半期に必要性＞伝わりだった主要国の割合（必要性が契機）／伝わり以上の割合
#   R3 開幕（四半期 0）の T 行：主要国・産業の薄い国・大陸の小さい工業国の技術力・必要性・伝わりの項
#   （追記1・腕の前）門の合否を出す：G1（中央値 ≤2 かつ最大 ≤5）・G1b(i)（1800 年までに 1 国以上の種 ≥8/10）・G1b(ii)（1850 年までの国数の中央値 ≥10）・
#   G2（英の E 行が 0＝開幕から知ったまま）・G3（5 年窓の最大の中央値 ≤3 かつ 5 以上の種 ≤3）・G5（米を除く主要 7 国のうち 1850 年までに 1 国以上の種 ≥6/10）。
#   基線 m542 の生に当てると 事前登録の既知の数（1786 年 中央値 23.5・窓の中央値 2.0・G5 9/10・G1b(i) 10/10）が出る＝読み手の検算。
import csv, glob, os, sys, statistics as st
MAJOR = ["フランス", "プロイセン", "アメリカ", "日本", "清", "ロシア", "オスマン", "オーストリア"]
Q0_SHOW = MAJOR[:5] + ["ネーデルラント", "スイス", "デンマーク", "スウェーデン", "ヴェネツィア", "アラビア", "ラオス", "マラータ同盟"]
ROW = "蒸気機関"
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def seeds(d):
    return sorted(int(os.path.basename(p)[len('disc_s'):-4]) for p in glob.glob(os.path.join(d, 'disc_s*.tsv')))
def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
def one(d, s):
    got = {}; why = {}; t0 = {}
    for x in rows(os.path.join(d, f'disc_s{s}.tsv')):
        if x['row'] != ROW: continue
        if x['kind'] == 'E' and x['nation'] not in got:
            got[x['nation']] = int(x['year']); why[x['nation']] = (fl(x['techTerm']), fl(x['needTerm']), fl(x['transmitTerm']))
        if x['kind'] == 'T' and int(x['quarter']) <= 2 and (x['nation'], int(x['quarter'])) not in t0:
            t0[(x['nation'], int(x['quarter']))] = (fl(x['techTerm']), fl(x['needTerm']), fl(x['transmitTerm']))
    return got, why, t0
def max_window(years, width=5):
    ys = sorted(years)
    return max((sum(1 for v in ys if y <= v < y + width) for y in ys), default=0)
def main():
    for arg in sys.argv[1:]:
        lab, d = arg.split('=', 1)
        ss = seeds(d)
        print(f'## {lab}（種 {ss}）')
        cnt = {y: [] for y in (1786, 1790, 1800, 1850)}; nmaj = []; win = []; needFirst = 0; transFirst = 0; uk_e = 0; g5 = []
        for s in ss:
            got, why, t0 = one(d, s)
            non_uk = {n: y for n, y in got.items() if n != 'イギリス'}
            uk_e += 1 if 'イギリス' in got else 0
            for y in cnt: cnt[y].append(sum(1 for v in non_uk.values() if v <= y))
            maj = {n: got.get(n, 9999) for n in MAJOR}
            open_m = [v for v in maj.values() if v <= 1850]
            nmaj.append(len(open_m)); win.append(max_window(open_m))
            g5.append(sum(1 for n in MAJOR if n != 'アメリカ' and maj[n] <= 1850))
            for n in MAJOR:
                if n in why and maj[n] <= 1850:
                    if why[n][1] > why[n][2]: needFirst += 1
                    else: transFirst += 1
            print(f'  種 {s}：英以外の解禁国数 ≤1786:{cnt[1786][-1]} ≤1790:{cnt[1790][-1]} ≤1800:{cnt[1800][-1]} ≤1850:{cnt[1850][-1]}（E の国数 {len(got)}）　主要 8 国の 1850 年までの解禁 {len(open_m)} 国・5 年窓の最大 {win[-1]}')
            print('    主要 8 国の年：' + '・'.join(f'{n}{maj[n]}' for n in MAJOR))
            if s == ss[0]:
                for n in Q0_SHOW:
                    if (n, 0) in t0: print(f'    T Q0 {n} tech {t0[(n, 0)][0]:.2f} need {t0[(n, 0)][1]:.2f} trans {t0[(n, 0)][2]:.2f}')
        print('  G1 中央値 ' + ' '.join(f'≤{y}:{med(cnt[y]):.1f}' for y in cnt) + '　最大 ' + ' '.join(f'≤{y}:{max(cnt[y])}' for y in cnt))
        print(f'  G3 主要 8 国の 1850 年までの解禁国数の中央値 {med(nmaj):.1f}・5 年窓の最大の中央値 {med(win):.1f}（最大 {max(win)}）')
        ns = len(ss)
        ok = lambda b: 'OK' if b else 'NG'
        g1 = med(cnt[1786]) <= 2 and max(cnt[1786]) <= 5
        g1bi = sum(1 for v in cnt[1800] if v >= 1)
        g1bii = med(cnt[1850])
        g3a = med(win) <= 3; g3n = sum(1 for v in win if v >= 5)
        g5n = sum(1 for v in g5 if v >= 1)
        print(f'  [門] G1 {ok(g1)}（中央値 {med(cnt[1786]):.1f}≤2・最大 {max(cnt[1786])}≤5）')
        print(f'  [門] G1b(i) {ok(g1bi >= 0.8 * ns)}（1800 年までに英以外 1 国以上の種 {g1bi}/{ns}≥{0.8 * ns:.0f}）　G1b(ii) {ok(g1bii >= 10)}（1850 年までの国数の中央値 {g1bii:.1f}≥10）')
        print(f'  [門] G2 {ok(uk_e == 0)}（英の E 行 {uk_e} 件＝0 で開幕から知ったまま）')
        print(f'  [門] G3 {ok(g3a and g3n <= 3)}（5 年窓の最大の中央値 {med(win):.1f}≤3・5 以上の種 {g3n}≤3）')
        print(f'  [門] G5 {ok(g5n >= 0.6 * ns)}（米を除く主要 7 国のうち 1850 年までに 1 国以上の種 {g5n}/{ns}≥{0.6 * ns:.0f}）')
        tot = needFirst + transFirst
        print(f'  R2 主要国の解禁の四半期に 必要性＞伝わり {needFirst}/{tot}・伝わり以上 {transFirst}/{tot}')
main()
