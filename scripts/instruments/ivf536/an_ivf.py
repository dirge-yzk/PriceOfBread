# -*- coding: utf-8 -*-
"""(536) 線 ivf の読み手：ivf_s<seed>.tsv（SimCore/InstrumentsIvf.cs）から事前登録 measurements/ivf536_20261024_prereg.txt の G1〜G9 と R1〜R4 の素の値を読む。
使い方：PYTHONUTF8=1 python an_ivf.py <計器の出力フォルダ> [<種> ...]   例）python an_ivf.py D:/PoB_work/x/out_B/ins 1 2 3
出力：国ごとに Q0 の素の値（G1〜G4・R1・R4）と、全期間の積算（G5 時代の組の割合・G6 切り替え÷（州×年）・G7 束の最低の配給率）と、資本財の能力の比（G8）。
⚠読むだけ。採否の判定は事前登録の段（機構→止める線→採否）で人が行う。列の定義と限界は InstrumentsIvf.cs の先頭。"""
import csv, sys, os, statistics

def rd(p):
    return list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))

def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')

def main():
    d = sys.argv[1]
    seeds = [int(a) for a in sys.argv[2:]] or [1]
    for sd in seeds:
        p = os.path.join(d, f'ivf_s{sd}.tsv')
        if not os.path.exists(p):
            print(f'seed {sd}: {p} が無い'); continue
        rows = rd(p)
        nats = sorted({r['nation'] for r in rows})
        print(f'=== seed {sd}（{len(rows)} 行）===')
        q0 = {r['nation']: r for r in rows if r['quarter'] == '0'}
        eu = [n for n in ('フランス', 'プロイセン', 'ロシア', 'オスマン', 'オーストリア') if n in q0]
        if 'イギリス' in q0 and eu:
            med = statistics.median(f(q0[n]['coalOwnCostMain']) for n in eu)
            print(f'G4 英の石炭の自己費用÷他の欧州の中央値（Q0）= {f(q0["イギリス"]["coalOwnCostMain"]) / med:.3f}')
        for n in ('イギリス', 'フランス', 'プロイセン', 'アメリカ', '清', 'マラータ同盟'):
            if n not in q0: continue
            r = q0[n]
            print(f'Q0 {n}: lpmMain {f(r["lpmMain"]):.4f} lpmCol {f(r["lpmCol"]):.4f} wagePremMain {f(r["wagePremMain"]):.4f} coalOwnCost {f(r["coalOwnCostMain"]):.4f} steamMc/wage {f(r["steamMcOverWageMain"]):.3f} laborProd/Demand {f(r["laborProdOverDemandMain"]):.3f} bundleSteel/ref {f(r["bundleSteelOverRefMain"]):.2f} bundleEra/ref {f(r["bundleEraOverRefMain"]):.2f}')
        for n in nats:
            rr = [r for r in rows if r['nation'] == n and f(r['bundleMonths']) > 0]
            if not rr: continue
            m = sum(int(r['bundleMonths']) for r in rr); e = sum(int(r['bundleEraMonths']) for r in rr); fl = sum(int(r['bundleFlips']) for r in rr)
            bm = sum(int(r['bottleneckMonths']) for r in rr); bs = sum(f(r['bottleneckRationSum']) for r in rr)
            cap = {int(r['quarter']): f(r['capGoodsCapacity']) for r in rows if r['nation'] == n}
            c0 = cap.get(0, float('nan')); c68 = cap.get(67, cap.get(68, float('nan')))
            print(f'{n}: G5 era {e / m:.3f}  G6 flips/州・年 {12 * fl / m:.2f}  G7 最低の配給率 {bs / bm if bm else float("nan"):.3f}  G8 能力 Q67/Q0 {c68 / c0 if c0 else float("nan"):.3f}（Q0 {c0:.0f}）')

if __name__ == '__main__':
    main()
