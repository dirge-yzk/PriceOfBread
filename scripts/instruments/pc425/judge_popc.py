#!/usr/bin/env python
"""(425) 人口の段C の〈機構が狙いどおり働いたか〉の判定（読むだけ）。事前登録 measurements/pc_20261019_prereg.txt の [7] T1〜T11 と追記3 [14] の定義だけで書く。
使い方：PYTHONUTF8=1 python judge_popc.py <腕 B の ins フォルダ> [--base <基準の ins フォルダ（既定 D:/PoB_work/keep_420/m420_ins）>] [--seeds 1,2,...] [-v]
・種ごとに T1〜T10 の合否を出し、10 種のうち 7 種以上で〈当たり〉・4〜6 で〈部分〉・3 以下で〈外れ〉（事前登録 [7]）。T11 は記録だけ。
・〈機構が働いた〉＝T1・T2・T3・T4・T6・T7・T9 が全て〈当たり〉か〈部分〉（外れが 1 本も無い）。T5・T8 の外れは先頭に書く。T10・T11 は記録。
・腕の結果を見てから式を書かない：閾値は事前登録 [7] の数字だけ。事前登録に無い操作の定義（中央の年・窓・件数の数え方）は [14] に先に書いた。
・⚠限界（自己申告）：T1・T2・T6 は較正か構造の確認で機構の証拠ではない（[14] ③）。T5 の POP の相関は popc_s の eduFillClimitCorr（国×窓の重みつき相関）で読む。T9 の〈縮む〉は件数だけ記録（採否は飢えの死の割合と人口密度の p99）。
  popc_s の国名は表示名（政体で変わる）＝共和制・立憲王政などの接頭辞を他の名前の末尾と一致するなら外して束ねる。popc_s は挙げた国だけ四半期ごと・他は年末に 4 四半期ぶんの行。
"""
import csv, glob, math, os, statistics, sys, collections
import os as _os, sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), '..', 'pop'))
import age_guard as _age_guard
_age_guard.require_bands8_argv()   # (490) 8 層（10 歳刻み）の pop_s だけを前提にする＝11 層の生なら止める

csv.field_size_limit(1 << 30)
if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

MAJOR8 = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', '日本', '清', 'ネーデルラント']
T1N = ['イギリス', 'フランス', 'ロシア', '清']


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def fin(x):
    return x == x and not math.isinf(x)


def med(v):
    v = [x for x in v if fin(x)]
    return statistics.median(v) if v else float('nan')


def mean(v):
    v = [x for x in v if fin(x)]
    return sum(v) / len(v) if v else float('nan')


class Norm:
    """表示名の接頭辞（共和制・立憲王政ほか）を外す：他の名前の末尾と一致し、接頭辞が 2 文字以上のとき。"""
    def __init__(self, names):
        ns = set(names)
        self.m = {}
        for n in ns:
            best = n
            for k in ns:
                if k != n and len(k) >= 2 and n.endswith(k) and len(n) - len(k) >= 2 and len(k) < len(best):
                    best = k
            self.m[n] = best

    def __call__(self, n):
        return self.m.get(n, n)


def load(d, name, seed):
    p = os.path.join(d, f'{name}_s{seed}.tsv')
    if not os.path.exists(p):
        return []
    with open(p, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def pop_rows(d, seed):
    rows = [r for r in load(d, 'pop', seed) if r['scope'] == 'N']
    nm = Norm({r['nation'] for r in rows})
    out = []
    for r in rows:
        out.append(dict(nation=nm(r['nation']), uid=r['nationUid'], q=int(r['quarter']), year=int(r['year']), months=fl(r['monthsCount']), popK=fl(r['popK']),
                        cbr=fl(r['cbrPerMille']), cdr=fl(r['cdrPerMille']), cdrN=fl(r['cdrNormalPerMille']), tfr=fl(r['tfrProxy']), mu0=fl(r['muFrom0PerYear']),
                        rec=fl(r['recoveringMonths']), dN=fl(r['deathsNormalK']), dF=fl(r['deathsFamineK']), dO=fl(r['deathsOtherK']), b=fl(r['birthsK'])))
    return out


def popc_rows(d, seed):
    rows = load(d, 'popc', seed)
    nm = Norm({r['nation'] for r in rows})
    out = []
    for r in rows:
        out.append(dict(nation=nm(r['nation']), q=int(r['quarter']), year=int(r['year']), months=fl(r['monthsCount']), age=fl(r['marriageAgeYears']), m=fl(r['marriedShare']),
                        c=fl(r['cLimitShare']), k=fl(r['knowledgeShare']), v=fl(r['childValueIndex']), pi=fl(r['pensionPiShare']), E=fl(r['s10EarningsTermE']),
                        O=fl(r['oldAgeTermO']), C=fl(r['childCostC']), corr=fl(r['eduFillClimitCorr']), ef=fl(r['nEduFillShare']), ad=fl(r['adultShare']),
                        y=fl(r['yAdultPerMonth']), b=fl(r['basketPerMonth']), r=fl(r['realIncomeIndex'])))
    return out


def rng(rows, lo, hi):
    return [r for r in rows if lo <= r['year'] <= hi and r['months'] > 0]


def decade_half_year(series, y0, y_end_lo, y_end_hi, step=10):
    """series＝{年: [値]}。開幕 10 年平均 → y_end_lo〜y_end_hi 平均 への低下の半分に届いた最初の 10 年の中央の年。届かなければ None。"""
    def m(lo, hi):
        v = [x for y, xs in series.items() if lo <= y <= hi for x in xs if fin(x)]
        return mean(v)
    start = m(y0, y0 + step - 1); end = m(y_end_lo, y_end_hi)
    if not (fin(start) and fin(end)) or not (end < start):
        return None
    half = start - 0.5 * (start - end)
    y = y0
    while y + step - 1 <= y_end_hi:
        if m(y, y + step - 1) <= half:
            return y + step // 2
        y += step
    return None


def judge_seed(dB, dBase, seed, verbose):
    P = pop_rows(dB, seed); C = popc_rows(dB, seed)
    res = {}; info = {}
    byN = collections.defaultdict(list)
    for r in P: byN[r['nation']].append(r)
    byC = collections.defaultdict(list)
    for r in C: byC[r['nation']].append(r)

    # T1 開幕の水準
    t1 = {}
    for n in T1N:
        rs = rng(byN.get(n, []), 1783, 1799)
        t1[n] = (med([r['tfr'] for r in rs]), med([r['cbr'] for r in rs]))
    info['T1'] = ' '.join(f'{n}:tfr{t1[n][0]:.2f}/cbr{t1[n][1]:.1f}' for n in T1N)
    res['T1'] = all(fin(t1[n][0]) and 4.0 <= t1[n][0] <= 7.0 and 30.0 <= t1[n][1] <= 50.0 for n in T1N)

    # T2 開幕の自然増
    nat = {}
    for n in MAJOR8:
        rs = rng(byN.get(n, []), 1783, 1799)
        nat[n] = med([(r['cbr'] - r['cdr']) / 10.0 for r in rs])
    vals = [nat[n] for n in MAJOR8 if fin(nat[n])]
    mid = med(vals); neg = sum(1 for x in vals if x < 0)
    info['T2'] = f'中央{mid:+.2f}%/年 負{neg}国 ' + ' '.join(f'{n}{nat[n]:+.2f}' for n in MAJOR8)
    res['T2'] = fin(mid) and 0.2 <= mid <= 0.8 and neg <= 3

    # T3 結婚の年齢
    age = {}
    for n in MAJOR8:
        rs = rng(byC.get(n, []), 1783, 1799)
        age[n] = med([r['age'] for r in rs])
    info['T3'] = ' '.join(f'{n}{age[n]:.1f}' for n in MAJOR8)
    e = age['イギリス']
    res['T3'] = fin(e) and 23.0 <= e <= 28.0 and fin(age['ロシア']) and fin(age['清']) and fin(age['フランス']) \
        and e - age['ロシア'] >= 3.0 and e - age['清'] >= 3.0 and abs(age['フランス'] - e) <= 2.0

    # T4 順序（英・普）
    t4ok = True; t4i = []
    for n in ['イギリス', 'プロイセン']:
        mu = collections.defaultdict(list); tf = collections.defaultdict(list)
        for r in byN.get(n, []):
            if r['months'] > 0:
                mu[r['year']].append(r['mu0']); tf[r['year']].append(r['tfr'])
        ym = decade_half_year(mu, 1783, 1900, 1919); yt = decade_half_year(tf, 1783, 1900, 1919)
        t4i.append(f'{n}:μ0半分{ym} tfr半分{yt}')
        if ym is None or yt is None or not (yt - ym >= 20):
            t4ok = False
    info['T4'] = ' '.join(t4i); res['T4'] = t4ok

    # T5 教育と児童労働
    sc = load(dB, 'scalars', seed)
    nmS = Norm({r['nation'] for r in sc})
    legal = {}
    for r in sc:
        legal[(nmS(r['nation']), int(r['year']))] = fl(r['childLaborLegal'])
    sign_ok = []; t5i = []
    for lo, hi in [(1850, 1899), (1900, 1950)]:
        ban = []; allow = []
        for n, rs in byC.items():
            for r in rng(rs, lo, hi):
                lg = legal.get((n, r['year']))
                if lg is None or not fin(lg) or not fin(r['c']): continue
                (ban if lg < 0.5 else allow).append(r['c'])
        if ban and allow:
            d = med(ban) - med(allow); sign_ok.append(d < 0); t5i.append(f'{lo}-{hi}: 禁止c{med(ban):.3f}({len(ban)})-許可c{med(allow):.3f}({len(allow)})={d:+.4f}')
        else:
            t5i.append(f'{lo}-{hi}: 比べられない（禁止{len(ban)}・許可{len(allow)}）')
    corrs = [r['corr'] for rs in byC.values() for r in rs if fin(r['corr'])]
    cmed = med(corrs)
    t5i.append(f'POP相関(就学×c)の中央{cmed:+.3f}（{len(corrs)}行）')
    info['T5'] = ' | '.join(t5i)
    res['T5'] = bool(sign_ok) and all(sign_ok) and fin(cmed) and cmed < 0

    # T6 回復期
    recm = sum(r['rec'] for r in P if fin(r['rec']))
    zero = sum(1 for r in P if r['months'] > 0 and r['popK'] > 1.0 and fin(r['dN']) and r['dN'] <= 0)
    info['T6'] = f'回復期の月の和{recm:.0f}・通常の死が0の行{zero}'
    res['T6'] = recm == 0 and zero == 0

    # T7 発散の不在（出生−死亡の 10 年平均が年 ±4% を超える国×窓の数。基準の同じ種と比べる）
    def diverge(pr):
        n_hit = 0; hits = []
        byU = collections.defaultdict(list)
        for r in pr:
            if r['months'] > 0 and fin(r['cbr']) and fin(r['cdr']): byU[r['uid']].append(r)
        for u, rs in byU.items():
            rs.sort(key=lambda r: (r['year'], r['q']))
            wsz = 40
            if len(rs) < wsz: continue
            hit = False
            for i in range(0, len(rs) - wsz + 1, 4):
                w = rs[i:i + wsz]
                tw = sum(x['months'] for x in w)
                g = sum((x['cbr'] - x['cdr']) * x['months'] for x in w) / tw / 10.0   # %/年
                if g > 4.0 or g < -4.0:
                    hit = True; break
            if hit: n_hit += 1; hits.append(rs[0]['nation'])
        return n_hit, hits
    hB, lB = diverge(P)
    PB = pop_rows(dBase, seed) if dBase else []
    hA, lA = diverge(PB) if PB else (None, [])
    info['T7'] = f'B 発散{hB}国 基準{hA}国 {lB[:6]}'
    res['T7'] = (hA is None and hB == 0) or (hA is not None and hB <= hA)

    # T8 知識の届き
    disc = load(dB, 'disc', seed); nmD = Norm({r['nation'] for r in disc})
    unl = {}
    for r in disc:
        if r['kind'] == 'E' and r['row'] == '出生の制限':
            n = nmD(r['nation']); y = int(r['year'])
            if n not in unl or y < unl[n]: unl[n] = y
    t8down = 0; t8n = 0; early = 0
    for n, y in unl.items():
        if y < 1870: early += 1
        bef = [r['c'] for r in byC.get(n, []) if y - 30 <= r['year'] < y and r['months'] > 0]
        aft = [r['c'] for r in byC.get(n, []) if y <= r['year'] <= y + 30 and r['months'] > 0]
        if len(bef) >= 4 and len(aft) >= 4:
            t8n += 1
            if med(aft) < med(bef): t8down += 1
    info['T8'] = f'解禁{len(unl)}国・1870前{early}・比べた{t8n}国で下がった{t8down}'
    res['T8'] = len(unl) > 0 and t8n > 0 and t8down > 0.5 * t8n and early <= 3

    # T9 飢えの働き
    def fam_share(pr):
        f = sum(r['dF'] for r in pr if fin(r['dF'])); a = sum(r['dN'] + r['dF'] + r['dO'] for r in pr if fin(r['dN']) and fin(r['dF']) and fin(r['dO']))
        return f / a if a > 0 else float('nan')
    def region_stats(d):
        rg = load(d, 'region', seed)
        dens = []; byR = collections.defaultdict(list)
        for r in rg:
            if r['activeFlag'] != '1': continue
            ar = fl(r['arableLand']); pk = fl(r['populationK'])
            if ar > 0 and fin(pk): dens.append(pk * 1000.0 / ar)
            byR[(r['nation'], r['region'])].append((int(r['quarter']), fl(r['foodFillShare']), pk))
        dens.sort()
        p99 = dens[int(0.99 * (len(dens) - 1))] if dens else float('nan')
        eps = 0; shr = 0
        for k, xs in byR.items():
            xs.sort(); run = []
            for q, ff, pk in xs + [(10 ** 9, 1.0, 0.0)]:
                if fin(ff) and ff < 0.6: run.append((q, pk))
                else:
                    if len(run) >= 12:
                        eps += 1
                        if run[-1][1] < run[0][1]: shr += 1
                    run = []
        return p99, eps, shr
    fsB = fam_share(P); fsA = fam_share(PB) if PB else float('nan')
    p99B, epB, shB = region_stats(dB)
    p99A, epA, shA = region_stats(dBase) if dBase else (float('nan'), 0, 0)
    info['T9'] = f'飢えの死の割合 B{fsB:.4f} 基準{fsA:.4f}・人口÷耕地のp99 B{p99B:.2f} 基準{p99A:.2f}・FoodFill<0.6が3年続く州 B{epB}件(縮んだ{shB}) 基準{epA}件(縮んだ{shA})'
    res['T9'] = fin(fsB) and (not fin(fsA) or fsB > fsA) and fin(p99B) and (not fin(p99A) or p99B <= 3.0 * p99A)

    # T10 老後の扶養（記録）
    n_pi0 = 0; n_o_main = 0
    for n, rs in byC.items():
        for r in rs:
            if r['months'] > 0 and fin(r['pi']) and r['pi'] <= 1e-9 and fin(r['v']) and r['v'] > 0 and fin(r['O']) and fin(r['E']):
                n_pi0 += 1
                if r['O'] > r['E']: n_o_main += 1
    info['T10'] = f'π=0かつv>0の行{n_pi0}・うちO>稼ぎの項 {n_o_main}'
    res['T10'] = n_pi0 > 0 and n_o_main > 0.5 * n_pi0

    # T11 人口の水準（記録）
    def mult(pr, n, y):
        rs = [r for r in pr if r['nation'] == n and r['months'] > 0]
        if not rs: return float('nan')
        r0 = min(rs, key=lambda r: (r['year'], r['q'])); r1 = [r for r in rs if r['year'] == y]
        return r1[-1]['popK'] / r0['popK'] if r1 and r0['popK'] > 0 else float('nan')
    t11 = ' '.join(f'{n}1913×{mult(P, n, 1913):.2f}(基準{mult(PB, n, 1913):.2f})' for n in ['イギリス', 'フランス', '清'])
    info['T11'] = t11
    return res, info


def verdict(k, n):
    return '当たり' if k >= 7 else ('部分' if k >= 4 else '外れ')


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__); return
    dB = a[0]; dBase = 'D:/PoB_work/keep_420/m420_ins'; seeds = None; verbose = False
    i = 1
    while i < len(a):
        if a[i] == '--base': dBase = a[i + 1]; i += 2
        elif a[i] == '--seeds': seeds = [int(x) for x in a[i + 1].split(',')]; i += 2
        elif a[i] == '-v': verbose = True; i += 1
        else: i += 1
    if seeds is None:
        seeds = sorted(int(''.join(ch for ch in os.path.basename(f) if ch.isdigit())) for f in glob.glob(os.path.join(dB, 'pop_s*.tsv')))
    print(f'# 腕 B={dB}  基準={dBase}  種={seeds}')
    allres = {}; allinfo = {}
    for s in seeds:
        if not os.path.exists(os.path.join(dB, f'popc_s{s}.tsv')):
            print(f'# 種 {s}：popc_s が無い（旗 1 でない走行か未完）— 判定から外す'); continue
        r, inf = judge_seed(dB, dBase, s, verbose)
        allres[s] = r; allinfo[s] = inf
    ts = ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10']
    print('種\t' + '\t'.join(ts))
    for s in sorted(allres):
        print(f'{s}\t' + '\t'.join('○' if allres[s][t] else '×' for t in ts))
    print('\n判定（10 種のうち 7 以上＝当たり・4〜6＝部分・3 以下＝外れ）')
    ver = {}
    for t in ts:
        k = sum(1 for s in allres if allres[s][t]); n = len(allres)
        ver[t] = verdict(k, n)
        print(f'  {t}\t{k}/{n}\t{ver[t]}')
    gate = ['T1', 'T2', 'T3', 'T4', 'T6', 'T7', 'T9']
    ok = len(allres) > 0 and all(ver[t] != '外れ' for t in gate)
    print(f'\n機構が働いた（T1・T2・T3・T4・T6・T7・T9 に外れが無い）：{"はい" if ok else "いいえ"}  外れ：{[t for t in gate if ver[t] == "外れ"]}')
    print(f'T5・T8 の外れ（報告の先頭）：{[t for t in ["T5", "T8"] if ver[t] == "外れ"]}')
    print('\n種ごとの内訳')
    for s in sorted(allinfo):
        print(f'--- 種 {s}')
        for t in ts + ['T11']:
            print(f'  {t}: {allinfo[s].get(t, "")}')


if __name__ == '__main__':
    main()
