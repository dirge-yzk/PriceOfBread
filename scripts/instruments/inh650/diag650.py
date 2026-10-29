"""(650) 線 inh 段0：相続法の下見（読むだけ）。基線 m645 の計器つき10種の生（age_s・class_s・nation_s）から、
相続の課税の底（成人の死者が運ぶ貯蓄）の大きさと、子供の死を混ぜたときの過大を、国×年で出す。
使い方：PYTHONUTF8=1 python diag650.py <生のフォルダ> [種の数=10]  標準出力へ。コードの挙動は一切変えない。

式（事前登録 [1] と同じ）：
  成人の死者割合 a  = 層の下端が 21 歳以上の層の死者 ÷ 全層の死者（age_s の deathsK）
  成人の死亡率 m    = 成人の死者 ÷ 成人の人数（年率・age_s の countK で下端 21 以上）
  貯蓄の総額 S      = class_s の savingsStock の和（正の在庫・名目）／ nominalGdpPerYear（nation_s）= S/GDP
  相続の底（年）    = m × S（死者の貯蓄は成人1人あたりの平均で運ばれるとして）＝ 底/GDP = m × S/GDP
  素朴な式（死者全員×1人あたり貯蓄）の底 = (全層の死者÷全人口) × S ＝ 過大の倍率 = 素朴÷成人
"""
import sys, os, statistics as st

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for l in f:
            p = l.rstrip('\n').split('\t')
            yield dict(zip(h, p))

def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')

def main():
    root = sys.argv[1]
    nseeds = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    nations = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オーストリア']
    years = [1790, 1830, 1870, 1900, 1913, 1930, 1950]
    # 国×年（その年の最初の四半期）→ 種ごとの値
    acc = {}
    for s in range(1, nseeds + 1):
        age = {}
        for r in rows(os.path.join(root, f'age_s{s}.tsv')):
            if r['scope'] != 'B':
                continue
            key = (int(r['quarter']), r['nation'])
            lo = fl(r['lowerAge'])
            d = age.setdefault(key, {'year': int(r['year']), 'dAll': 0.0, 'dAd': 0.0, 'nAll': 0.0, 'nAd': 0.0})
            d['dAll'] += fl(r['deathsK']); d['nAll'] += fl(r['countK'])
            if lo >= 21:
                d['dAd'] += fl(r['deathsK']); d['nAd'] += fl(r['countK'])
        sav = {}
        for r in rows(os.path.join(root, f'class_s{s}.tsv')):
            t = r['time']
            if not t.startswith('q'):
                continue
            key = (int(t[1:]), r['nation'])
            v = fl(r['savingsStock'])
            if v == v and v > 0:
                e = sav.setdefault(key, {'S': 0.0, 'cls': {}})
                e['S'] += v
                e['cls'][r['className']] = e['cls'].get(r['className'], 0.0) + v
        gdp = {}
        for r in rows(os.path.join(root, f'nation_s{s}.tsv')):
            gdp[(int(r['quarter']), r['nation'])] = (fl(r['nominalGdpPerYear']), fl(r.get('revenueBaseMeanPerMonth', 'nan')) * 12.0)
        # 年ごとの最初の四半期
        firstq = {}
        for (q, n), d in age.items():
            firstq.setdefault((d['year'], n), q)
            firstq[(d['year'], n)] = min(firstq[(d['year'], n)], q)
        for n in nations:
            for y in years:
                q = firstq.get((y, n))
                if q is None:
                    continue
                d = age[(q, n)]; sv = sav.get((q, n)); g = gdp.get((q, n))
                if sv is None or g is None or not (g[0] > 0) or d['nAd'] <= 0 or d['nAll'] <= 0:
                    continue
                dAd, dAll = d['dAd'] * 4.0, d['dAll'] * 4.0   # 年率（四半期の死者×4）
                m = dAd / d['nAd']                       # 成人の死亡率（年）
                mAll = dAll / d['nAll']
                a = d['dAd'] / d['dAll'] if d['dAll'] > 0 else float('nan')
                sg = sv['S'] / g[0]
                top = sorted(sv['cls'].items(), key=lambda kv: -kv[1])
                top3 = sum(v for _, v in top[:3]) / sv['S']
                acc.setdefault((n, y), []).append({
                    'a': a, 'm': m, 'sg': sg, 'base': m * sg, 'naive': mAll * sg, 'top3': top3,
                    'top1': top[0][0], 'rev': g[1] / g[0] if g[0] > 0 else float('nan')})
    def med(xs):
        xs = [x for x in xs if x == x]
        return st.median(xs) if xs else float('nan')
    print('# (650) 相続法 段0 下見：成人の死者が運ぶ貯蓄の底（基線 m645 の計器つき10種・種の中央値・読むだけ）')
    print('# 列：国 年 種数 成人の死者割合 成人の死亡率(年) 貯蓄/GDP(年) 底/GDP(年)＝m×S/GDP 素朴÷成人 貯蓄の上位3階級の割合 最大の保有階級(最頻) 歳入基礎/GDP(年)')
    for n in nations:
        for y in years:
            v = acc.get((n, y))
            if not v:
                continue
            tops = [x['top1'] for x in v]
            mode = max(set(tops), key=tops.count)
            print('\t'.join([n, str(y), str(len(v)), f"{med([x['a'] for x in v]):.3f}", f"{med([x['m'] for x in v]):.4f}",
                             f"{med([x['sg'] for x in v]):.3f}", f"{med([x['base'] for x in v]):.4f}",
                             f"{med([x['naive'] / x['base'] if x['base'] > 0 else float('nan') for x in v]):.2f}",
                             f"{med([x['top3'] for x in v]):.3f}", mode, f"{med([x['rev'] for x in v]):.3f}"]))

main()
