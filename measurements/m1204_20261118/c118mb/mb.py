# (1187) c118mb B′-1：造幣の買いを首都の州の市場だけに（旗 MintBuysAtCapital 1）の紙の台本と門。
#   エンジンの関数を呼ばない・計器の表だけを読む（教訓 ①）。読む表：mintcap_s（国×四半期）・nationgood_s（10 年ごと＋1913 の年末）・mintreg_s（州×四半期）。
#   使い方:
#     python -X utf8 -I mb.py paper <生のフォルダ> <種の起点> <種の終わり> <出力.json>
#     python -X utf8 -I mb.py gates <腕.json> <基準.json> [<N1.json> <N2.json>]
#   門（事前登録 measurements/m1187_20261117_prereg_mb.txt）：E1〈首都の外で造幣が買った量〉＝0・E2〈首都の値段＞平価の月に床の枝で買った回数〉＝0（どちらも厳密）。
#   ほかは全て記録（G1 越境の銀の輸入の量・金の同じ量・E3 銀の産出・金の産出・E4 銀本位の持ち主の銀を産む州の値段÷平価の中央・Q 越境の輸入の額・E5 銀の賦存の 6 州の雇用 0.3 未満）。
import sys, os, csv, json, math, statistics, collections

YEARS = (1830, 1870, 1900, 1913)
SILVER6 = {"オムスク", "プラハ", "ヌエバ・エスパーニャ銀地帯", "ペルー", "ラ・プラタ", "ドレスデン"}


def fl(v):
    try:
        x = float(v)
    except (TypeError, ValueError):
        return float('nan')
    return x


def rows(B, t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p):
        return None
    with open(p, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def era(y):
    return '1783-1829' if y < 1830 else '1830-1869' if y < 1870 else '1870-1913' if y < 1914 else '1914-1950'


def med(a):
    return statistics.median(a) if a else float('nan')


def paper(B, sb, se, out):
    r = collections.Counter(); miss = collections.Counter(); seeds = 0
    e4 = []; e4era = collections.defaultdict(list); cover = collections.defaultdict(list); capNoStock = collections.Counter(); nonFiat = collections.Counter()
    e1rows = 0; e2rows = 0; ng = collections.defaultdict(float); e5 = 0; e5rows = 0
    for s in range(sb, se + 1):
        mc = rows(B, 'mintcap', s)
        if mc is None:
            miss['mintcap'] += 1
            continue
        seeds += 1
        for x in mc:
            y = int(x['year'])
            for c in ('boughtCapAg', 'boughtCapAu', 'boughtOtherAg', 'boughtOtherAu', 'oldShadowAg', 'oldShadowAu', 'newShadowAg', 'newShadowAu'):
                v = fl(x[c])
                if math.isfinite(v):
                    r[c] += v
            oth = fl(x['boughtOtherAg']) + fl(x['boughtOtherAu'])
            if oth > 0:
                e1rows += 1
            ab = int(x['aboveBoughtN'])
            r['aboveBoughtN'] += ab
            if ab > 0:
                e2rows += 1
            r['rows'] += 1
            months = int(x['months']); fiat = int(x['fiatM'])
            nonFiat[era(y)] += months - fiat
            capNoStock[era(y)] += int(x['capNoStockM']) + int(x['capNoGoodM'])
            if x['law'] == '1' and 1870 <= y <= 1913:
                v = fl(x['originPoverMed'])
                if math.isfinite(v):
                    e4.append(v)
            if x['law'] == '1':
                v = fl(x['originPoverMed'])
                if math.isfinite(v):
                    e4era[era(y)].append(v)
            cv = fl(x['cover'])
            if math.isfinite(cv):
                cover[era(y)].append(cv)
        ngr = rows(B, 'nationgood', s)
        if ngr is None:
            miss['nationgood'] += 1
        else:
            for x in ngr:
                y = int(x['year'])
                if y not in YEARS:
                    continue
                iv = fl(x['crossImportValue'])
                if math.isfinite(iv):
                    ng['Q'] += iv
                if x['good'] == '銀':
                    ng['G1'] += fl(x['crossImportQty']); ng['E3'] += fl(x['producedQty'])
                    ng[f'G1_{y}'] += fl(x['crossImportQty'])
                elif x['good'] == '金':
                    ng['G1Au'] += fl(x['crossImportQty']); ng['E3Au'] += fl(x['producedQty'])
        mr = rows(B, 'mintreg', s)
        if mr is None:
            miss['mintreg'] += 1
        else:
            for x in mr:
                if x['region'] in SILVER6:
                    e5rows += 1
                    em = fl(x['employed'])
                    if math.isfinite(em) and em < 0.3:
                        e5 += 1
    res = {
        'src': B, 'seeds': seeds, 'miss': dict(miss), 'rows': r['rows'],
        'E1': r['boughtOtherAg'] + r['boughtOtherAu'], 'E1Ag': r['boughtOtherAg'], 'E1Au': r['boughtOtherAu'], 'E1rows': e1rows,
        'E2': r['aboveBoughtN'], 'E2rows': e2rows,
        'capAg': r['boughtCapAg'], 'capAu': r['boughtCapAu'],
        'capShareAg': r['boughtCapAg'] / (r['boughtCapAg'] + r['boughtOtherAg']) if r['boughtCapAg'] + r['boughtOtherAg'] > 0 else float('nan'),
        'capShareAu': r['boughtCapAu'] / (r['boughtCapAu'] + r['boughtOtherAu']) if r['boughtCapAu'] + r['boughtOtherAu'] > 0 else float('nan'),
        'boughtAll': r['boughtCapAg'] + r['boughtCapAu'] + r['boughtOtherAg'] + r['boughtOtherAu'],
        'oldShadow': r['oldShadowAg'] + r['oldShadowAu'], 'newShadow': r['newShadowAg'] + r['newShadowAu'],
        'G1': ng['G1'], 'G1Au': ng['G1Au'], 'E3': ng['E3'], 'E3Au': ng['E3Au'], 'Q': ng['Q'],
        'G1_by_year': {str(y): ng[f'G1_{y}'] for y in YEARS},
        'E4': med(e4), 'E4n': len(e4), 'E4_by_era': {k: med(v) for k, v in sorted(e4era.items())},
        'E5': e5, 'E5rows': e5rows,
        'cover_by_era': {k: med(v) for k, v in sorted(cover.items())},
        'cover_lt_0357': sum(1 for v in sum(cover.values(), []) if v < 0.357),
        'capEmptyShare_by_era': {k: (capNoStock[k] / nonFiat[k] if nonFiat[k] else float('nan')) for k in sorted(nonFiat)},
    }
    with open(out, 'w', encoding='utf-8') as f:
        json.dump(res, f, ensure_ascii=False, indent=1)
    for k, v in res.items():
        print(k, v)


def gates(arm, base, nulls):
    A = json.load(open(arm, encoding='utf-8')); M = json.load(open(base, encoding='utf-8'))
    Ns = [json.load(open(p, encoding='utf-8')) for p in nulls]
    print(f"# 腕 {A['src']}（種 {A['seeds']}）・基準 {M['src']}（種 {M['seeds']}）")
    if A['seeds'] != M['seeds']:
        print(f"!! 種の数が違う {A['seeds']} と {M['seeds']}")
    bad = 0
    for k, name in (('E1', '首都の外で造幣が買った量'), ('E2', '首都の値段＞平価の月に床の枝で買った回数')):
        ok = A[k] == 0
        bad += 0 if ok else 1
        print(f"門 {k} {name}：腕 {A[k]}（行 {A[k + 'rows']}）・基準（旗 0）{M[k]}（行 {M[k + 'rows']}）→ {'緑' if ok else '赤'}")
    print('記録（新÷旧・同じ盤面でない＝別の木の比は向きまで外れうる・★(1184)）')
    for k, name in (('G1', '越境の銀の輸入の量'), ('G1Au', '越境の金の輸入の量'), ('E3', '銀の産出'), ('E3Au', '金の産出'), ('E4', '銀本位の持ち主の銀を産む州の値段÷平価の中央 1870〜1913'), ('Q', '越境の輸入の額（全ての財）'), ('E5', '銀の賦存の 6 州の雇用 0.3 未満の四半期'), ('boughtAll', '造幣が買った量の和（金と銀）')):
        a, m = A[k], M[k]
        rat = a / m if m else float('nan')
        nl = ''
        if Ns:
            vals = [M[k]] + [n[k] for n in Ns]
            lo, hi = min(vals), max(vals)
            nl = f"・ヌル腕の範囲 {lo:.6g}〜{hi:.6g}（幅 {hi / lo if lo > 0 else float('nan'):.4g}・{'内' if lo <= a <= hi else '外'}）"
        print(f"  {k} {name}：腕 {a:.6g}・基準 {m:.6g}・新÷旧 {rat:.4g}{nl}")
    print(f"  首都の取り分 銀 {A['capShareAg']:.4g}（基準 {M['capShareAg']:.4g}）・金 {A['capShareAu']:.4g}（基準 {M['capShareAu']:.4g}）")
    print(f"  G1 の年ごと 腕 {A['G1_by_year']}・基準 {M['G1_by_year']}")
    print(f"  E4 の時代ごと 腕 {A['E4_by_era']}・基準 {M['E4_by_era']}")
    print(f"  被覆の中央（時代ごと）腕 {A['cover_by_era']}・基準 {M['cover_by_era']}・0.357 未満の行 腕 {A['cover_lt_0357']}・基準 {M['cover_lt_0357']}")
    print(f"  首都に在庫なし（市場なしを含む）の月の割合 腕 {A['capEmptyShare_by_era']}・基準 {M['capEmptyShare_by_era']}")
    print(f"門の赤 {bad}")
    return bad


if __name__ == '__main__':
    if sys.argv[1] == 'paper':
        paper(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5])
    elif sys.argv[1] == 'gates':
        sys.exit(1 if gates(sys.argv[2], sys.argv[3], sys.argv[4:]) else 0)
