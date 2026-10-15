"""(354) 流れM 段0 読み手＝読むだけ・挙動は変えない。Runner の副命令 navyneed の TSV を読む。
python an_m6_static.py <navyneed.tsv> [<navyneed2.tsv> …]      （PYTHONUTF8=1）
出す物（断面ごと・warm／q40／q148／q308／q504／q524）：
 ・T1 主要国の海軍力 navalPower（英＝1）・重み wA（平均で割った越境の交易額）・海の必要量 needSeaA の英比・その組成（艦隊の項／海を越えて来る陸軍の項）・払える側の上限つきの目標の艦隊 targetRefA の英比・海の金の割合 spendShare
 ・T2 重みの基数を入れ替えた反実仮想（A 今・U 全て同じ・B 交易額÷GDP・C 交易額²÷GDP・D 交易額÷√GDP）の海の必要量の 英比（清・仏・露・米・日・普・蘭・葡）と、英の取り分（英の海の必要量÷全ての国の和）
 ・T3 重みの上位 8 国（基数ごと）と、その国の海の必要量の A に対する倍率＝受け皿の確認（小さい開いた国が上位に来ないか）
 ・T4 1914 の艦の順位の相関（q524・海軍力×史実のトン数〔Kennedy 1988 の表の 1914 の列・記憶による【推】〕の Spearman）と、英の取り分
限界の自己申告：静的＝同じ盤面で重みだけを入れ替えた 1 回の再計算（艦隊の積み増し・他国の応答は動かさない）。seed 1 本ごとの読み（種の中央ではない）。
史実の値は記憶による【推】（出典未取得）。"""
import sys, csv, collections, statistics

# 1914 の軍艦トン数（千トン・Kennedy『大国の興亡』表の 1880-1914 の列＝記憶による【推】・英の割合は 10 種の NG の表の HIST_NAVAL 1914 と同じ出所）
TON1914 = {'イギリス': 2714, 'プロイセン': 1305, 'アメリカ': 985, 'フランス': 900, '日本': 700, 'ロシア': 679, 'サルデーニャ＝ピエモンテ': 498, 'オーストリア': 372}
MAIN = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'スペイン', 'ロシア', 'プロイセン', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']
BASES = ['A', 'U', 'B', 'C', 'D']


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def spearman(xs, ys):
    def rank(v):
        o = sorted(range(len(v)), key=lambda i: v[i]); r = [0.0] * len(v); i = 0
        while i < len(o):
            j = i
            while j + 1 < len(o) and v[o[j + 1]] == v[o[i]]:
                j += 1
            for k in range(i, j + 1):
                r[o[k]] = (i + j) / 2 + 1
            i = j + 1
        return r
    if len(xs) < 3:
        return float('nan')
    a, b = rank(xs), rank(ys)
    ma, mb = statistics.mean(a), statistics.mean(b)
    num = sum((p - ma) * (q - mb) for p, q in zip(a, b))
    den = (sum((p - ma) ** 2 for p in a) * sum((q - mb) ** 2 for q in b)) ** 0.5
    return num / den if den > 0 else float('nan')


def load(path):
    snaps = collections.OrderedDict()
    with open(path, encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            snaps.setdefault(r['time'], {})[r['nation']] = {k: f(v) for k, v in r.items() if k not in ('time', 'nation')}
    return snaps


def ratio(a, b):
    return a / b if b and b == b and b > 0 else float('nan')


def show(path):
    snaps = load(path)
    print(f'######## {path}')
    for tm, d in snaps.items():
        uk = d.get('イギリス')
        if uk is None:
            continue
        print(f'\n=== {tm} ===')
        print('T1 主要国（英＝1）：国・海軍力・艦(隻)・重みA・海の必要量A・（艦隊の項／陸軍の項の割合）・目標の艦隊A・海の金の割合')
        for n in MAIN:
            r = d.get(n)
            if r is None:
                continue
            fleet = r['needSeaA'] - r['needSeaCross']
            tot = r['needSeaA'] if r['needSeaA'] > 0 else float('nan')
            print(f"  {n:<10} 海軍力 {ratio(r['navalPower'], uk['navalPower']):7.3f}  艦 {r['ships']:9.1f}  重み {r['wA']:6.2f}  必要量 {ratio(r['needSeaA'], uk['needSeaA']):7.3f}"
                  f"（艦隊 {fleet / tot:4.2f}／陸 {r['needSeaCross'] / tot:4.2f}）  目標 {ratio(r['targetRefA'], uk['targetRefA']):7.3f}  海の金 {r['spendShare']:5.3f}")
        tot = {b: sum(x['needSea' + b] for x in d.values()) for b in BASES if 'needSea' + b in uk}
        print('T2 反実仮想の海の必要量（英比）と英の取り分（英÷世界の和）')
        print('  基数        ' + '  '.join(f'{b:>7}' for b in BASES))
        for n in ['清', 'フランス', 'ロシア', 'アメリカ', '日本', 'プロイセン', 'ネーデルラント', 'ポルトガル']:
            r = d.get(n)
            if r is None:
                continue
            print(f'  {n:<10} ' + '  '.join(f"{ratio(r['needSea' + b], uk['needSea' + b]):7.3f}" for b in BASES))
        print('  英の取り分  ' + '  '.join(f"{ratio(uk['needSea' + b], tot[b]):7.3f}" for b in BASES))
        print('  英の目標(参照価格) ' + '  '.join(f"{uk['targetRef' + b]:9.3g}" for b in BASES if 'targetRef' + b in uk))
        print('T3 重みの上位 8 国（基数ごと・倍率＝その国の海の必要量÷A の海の必要量）')
        for b, wk in (('A', 'wA'), ('B', 'wB'), ('C', 'wC'), ('D', 'wD')):
            top = sorted(d.items(), key=lambda kv: -kv[1][wk])[:8]
            print(f'  {b}: ' + '・'.join(f"{n}{x[wk]:.1f}(×{ratio(x['needSea' + b], x['needSeaA']):.2f})" for n, x in top))
        print('T5 英が払える側で縛られているか（目標の艦隊〔参照価格〕÷払える額）・陸の側の大きさと海の必要量（英・清・仏・露）')
        for n in ['イギリス', '清', 'フランス', 'ロシア', 'アメリカ']:
            r = d.get(n)
            if r is None:
                continue
            print(f"  {n:<10} 目標÷払える {ratio(r['targetRefA'], r['affordRef']):7.3f}  力の大きさ(陸) {r['forcePot']:9.3g}  陸+内の必要量 {r['needLand'] + r['needInt']:9.3g}  海の必要量 {r['needSeaA']:9.3g}  海の金の割合 {r['spendShare']:5.3f}")
        print(f"  越境の交易額（清÷英）{ratio(d['清']['tradeSm'], uk['tradeSm']) if '清' in d else float('nan'):6.2f}  名目GDP（清÷英）{ratio(d['清']['gdpYear'], uk['gdpYear']) if '清' in d else float('nan'):6.2f}")
        if tm in ('q504', 'q524'):
            ns = [n for n in TON1914 if n in d]
            if len(ns) >= 5:
                print('T4b 基数ごとの目標の艦隊（参照価格）×1914 のトン数の Spearman と英の取り分（英÷Σ）：' + '　'.join(
                    f"{b} {spearman([d[n]['targetRef' + b] for n in ns], [TON1914[n] for n in ns]):.3f}/{ratio(uk['targetRef' + b], sum(x['targetRef' + b] for x in d.values())):.3f}" for b in BASES))
        if tm in ('q504', 'q524'):
            ns = [n for n in TON1914 if n in d]
            if len(ns) >= 5:
                print(f"T4 {tm} 海軍力×1914 のトン数の Spearman（{len(ns)}国）{spearman([d[n]['navalPower'] for n in ns], [TON1914[n] for n in ns]):.3f}"
                      f"  艦×トン {spearman([d[n]['ships'] for n in ns], [TON1914[n] for n in ns]):.3f}  英の海軍力÷上位8国の和 {ratio(uk['navalPower'], sum(x['navalPower'] for x in sorted(d.values(), key=lambda x: -x['navalPower'])[:8])):.3f}")
                print('   海軍力の順位（上位 8）：' + '　'.join(f"{n} {x['navalPower']:.3g}" for n, x in sorted(d.items(), key=lambda kv: -kv[1]['navalPower'])[:8]))
        inl = [(n, x['ships']) for n, x in d.items() if x['port'] == 0 and x['ships'] > 0]
        if inl:
            print('  ⚠港の無い国で艦がある：' + '・'.join(f'{n}{s:.1f}' for n, s in inl))


if __name__ == '__main__':
    for p in sys.argv[1:]:
        show(p)
        print()
