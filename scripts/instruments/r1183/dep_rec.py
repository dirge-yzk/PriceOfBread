# (1183) 鉱脈の推し：prodcap_s（10 年ごとの断面・州×財の月の産出）を台形で積分し、州の鉱脈の残りと品位を推す。
# エンジンの関数を呼ばない。初期の在庫＝最初に鉱物の産出が正の断面の月の産出×12×YEARS（エンジンは助走明けの最初の産出の月）。
# 大鉱床の発見（site_s の kind O・siteKind D）で初期×BONANZA/YEARS を足す（初期を捕えた州だけ＝Discovery.cs:937）。
import csv, collections
MIN = ['鉄鉱石','石炭','銅鉱','鉛鉱','金','硫黄','石灰石','珪砂','粘土','原油','クロム','錫鉱','硝石','銀']
UBI = {'石灰石','珪砂','粘土'}
FLOOR, KNEE, BONANZA = 0.15, 0.5, 60.0
csv.field_size_limit(10**9)

def grade_knee(frac):
    return 1.0 if frac >= KNEE else FLOOR + (1-FLOOR)*max(0.0, frac)/KNEE
def grade_prop(frac):
    return FLOOR + (1-FLOOR)*min(1.0, max(0.0, frac))

def load(raw, seed):
    snaps = collections.OrderedDict(); owner = {}
    with open(f'{raw}/prodcap_s{seed}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            g = r[ix['good']]
            if g not in MIN: continue
            y = int(r[ix['year']]); reg = r[ix['region']]
            try: o = float(r[ix['output']])
            except ValueError: continue
            if not (o > 0): o = 0.0
            snaps.setdefault(y, {}).setdefault(reg, collections.Counter())[g] += o
            owner[(y, reg)] = r[ix['ownerNation']]
    bon = collections.defaultdict(list)
    with open(f'{raw}/site_s{seed}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            if r[ix['kind']] == 'O' and r[ix['siteKind']] == 'D':
                bon[r[ix['region']]].append(int(r[ix['quarter']]))
    return snaps, owner, bon

def simulate(snaps, bon, years=100.0, mode='knee', exclude_ubi=False):
    """戻り値：year -> region -> {grade（合算の在庫）, frac, gOwn{g}（鉱物ごとの在庫の反実仮想）, cum{g}, out{g}}"""
    gf = grade_knee if mode == 'knee' else grade_prop
    goods = [g for g in MIN if not (exclude_ubi and g in UBI)]
    ys = list(snaps.keys()); st = {}; res = {}
    for k, y in enumerate(ys):
        cur = snaps[y]; prev = snaps[ys[k-1]] if k > 0 else {}
        months = (y - ys[k-1]) * 12 if k > 0 else 0
        for reg in set(cur) | set(prev):
            co = cur.get(reg, {}); po = prev.get(reg, {})
            s = st.get(reg)
            if s is not None and months > 0:
                for g in goods:
                    d = 0.5*(po.get(g,0.0)+co.get(g,0.0))*months
                    if d <= 0: continue
                    s['cum'][g] += d
                    s['remT'] = max(0.0, s['remT'] - d)
                    if g in s['init']: s['rem'][g] = max(0.0, s['rem'][g] - d)
                for qq in bon.get(reg, []):
                    yq = 1783 + qq/4.0
                    if ys[k-1] < yq <= y:
                        s['remT'] += BONANZA*s['initT']/years
                        for g in s['init']: s['rem'][g] += BONANZA*s['init'][g]/years
            tot = sum(co.get(g,0.0) for g in goods)
            if s is None:
                if not (tot > 0): continue
                s = {'cum': collections.Counter(), 'init': {}, 'rem': {}, 'initT': 12*years*tot, 'remT': 12*years*tot, 'y0': y}
                st[reg] = s
            for g in goods:
                if co.get(g,0.0) > 0 and g not in s['init']:
                    s['init'][g] = 12*years*co[g]; s['rem'][g] = s['init'][g]
            fracT = s['remT']/s['initT'] if s['initT'] > 0 else 1.0
            res.setdefault(y, {})[reg] = {'grade': gf(fracT), 'frac': fracT,
                'gOwn': {g: gf(s['rem'][g]/s['init'][g]) for g in s['init']},
                'cum': dict(s['cum']), 'out': {g: co.get(g,0.0) for g in goods if co.get(g,0.0) > 0}, 'y0': s['y0']}
    return res

def load_minecap(raw, seed):
    """(year, nation, good) -> [Σout, Σout×grade, Σout×bin0, producers]（格を足し合わせる）"""
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    with open(f'{raw}/minecap_s{seed}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            try:
                o = float(r[ix['outputSum']]); gr = float(r[ix['gradeOutW']]); b0 = float(r[ix['gradeBin0']])
            except ValueError:
                o = float(r[ix['outputSum']]) if r[ix['outputSum']] not in ('', 'NaN') else 0.0; gr = b0 = float('nan')
            a = acc[(int(r[ix['year']]), r[ix['nation']], r[ix['good']], r[ix['class']])]
            a[0] += o; a[3] += int(r[ix['producers']])
            if o > 0 and gr == gr: a[1] += o*gr; a[2] += o*b0
    return acc
