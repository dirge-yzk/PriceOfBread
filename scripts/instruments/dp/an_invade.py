#!/usr/bin/env python
"""(440) 線 dp の〈機構が狙いどおり働いたか〉の読み手（D3＝侵略の近接の緊張）：計器の表 invade_s<種>.tsv から事前登録 [4]・[9] の T1〜T3 を式のとおりに出す。
使い方：python an_invade.py <計器の出力フォルダ（…/ins）> [--flag1]　または　python an_invade.py <腕 B の…/ins> <旗 0 の…/ins>
  1 つ渡す＝その腕を旗 0 として T3（--flag1 を付けると旗 1 として T1・T2）。2 つ渡す＝B（先・旗 1）と旗 0 の対照（後）を並べる。
事前登録 measurements/dp_20261019_prereg.txt [4]・[9] 追記 1：
  T1 拒否（submitted=0）の行の proximityAdded の和が厳密 0。かつ拒否の行の proximityWouldHave が正の行が延べ ≥1（0 なら〈機構を踏んでいない〉）。
  T2 屈服（submitted=1）の行：実在の国が標的（targetIsNation=1）では proximityAdded ＝ proximityWouldHave。抽象標的（0）では proximityWouldHave＝0 で proximityAdded＞0 の行が ≥1。
  T3 旗 0 の対照：全行で proximityAdded ＝ proximityWouldHave。
⚠限界（この読み手が自己申告する）：invade_s は World.Invade が Subjugate を終えた所の 1 行（AI もプレイヤーも同じ動詞）。proximityAdded は AddTension に渡した増分の和で、
  緊張の上限 200 で切れた分は含まない（積もうとした量）。wouldHave は旗 1 のときだけ〈屈服の後の座標・両方の枝〉で計算し直した量（旗 0 では added と同じ）。
  合否は止める線と合わせて報告で行う（この読み手は採否を決めない）。表が 1 本も無い試合（侵略が起きなかった）は行 0 として数える。
"""
import csv, glob, sys

sys.stdout.reconfigure(encoding='utf-8')


def load(d):
    rows = []
    files = sorted(glob.glob(d + '/invade_s*.tsv'))
    for f in files:
        with open(f, encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                rows.append(r)
    return rows, len(files)


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def summarize(label, d, flag1):
    rows, nfiles = load(d)
    print(f'== {label}: {d}  表 {nfiles} 本・侵略 {len(rows)} 行')
    if not rows:
        return
    sub = [r for r in rows if r['submitted'] == '1']
    ref = [r for r in rows if r['submitted'] == '0']
    subN = [r for r in sub if r['targetIsNation'] == '1']
    subA = [r for r in sub if r['targetIsNation'] == '0']
    refN = [r for r in ref if r['targetIsNation'] == '1']
    refA = [r for r in ref if r['targetIsNation'] == '0']
    s = lambda rs, k: sum(fl(r[k]) for r in rs)
    pos = lambda rs, k: sum(1 for r in rs if fl(r[k]) > 0)
    print(f'  屈服 {len(sub)}（実在の国 {len(subN)}・抽象標的 {len(subA)}）／拒否 {len(ref)}（実在の国 {len(refN)}・抽象標的 {len(refA)}）／屈服率 {len(sub) / len(rows):.3f}')
    print(f'  積んだ量の和：屈服（実在 {s(subN, "proximityAdded"):.3f}・抽象 {s(subA, "proximityAdded"):.3f}）／拒否（実在 {s(refN, "proximityAdded"):.3f}・抽象 {s(refA, "proximityAdded"):.3f}）')
    print(f'  旧の規則での量の和：屈服（実在 {s(subN, "proximityWouldHave"):.3f}・抽象 {s(subA, "proximityWouldHave"):.3f}）／拒否（実在 {s(refN, "proximityWouldHave"):.3f}・抽象 {s(refA, "proximityWouldHave"):.3f}）')
    if flag1:
        t1a = s(ref, 'proximityAdded')
        t1b = pos(ref, 'proximityWouldHave')
        print(f'  T1 拒否の行の積んだ量の和 = {t1a:.6f}（狙い＝厳密 0）／拒否の行で旧の量が正 = {t1b} 行（狙い ≥1・0 なら機構を踏んでいない）')
        bad = [r for r in subN if abs(fl(r['proximityAdded']) - fl(r['proximityWouldHave'])) > 1e-9]
        print(f'  T2 実在の国への屈服で 積んだ量≠旧の量 の行 = {len(bad)}（狙い 0）／抽象標的の屈服で 旧の量=0 かつ 積んだ量>0 = '
              f'{sum(1 for r in subA if fl(r["proximityWouldHave"]) == 0 and fl(r["proximityAdded"]) > 0)} 行（狙い ≥1）')
        absBad = [r for r in subA if fl(r['proximityWouldHave']) != 0]
        print(f'     （抽象標的の屈服で 旧の量≠0 = {len(absBad)} 行：植民地を接収した後も座標が残るのは標的が植民地以外の座標を持つとき）')
    else:
        bad = [r for r in rows if abs(fl(r['proximityAdded']) - fl(r['proximityWouldHave'])) > 1e-9]
        print(f'  T3 旗 0 の対照：積んだ量≠旧の量 の行 = {len(bad)}（狙い 0）／拒否の行で積んだ量が正 = {pos(ref, "proximityAdded")} 行／屈服の抽象標的の行で積んだ量が正 = {pos(subA, "proximityAdded")} 行（旧の欠陥＝0 のはず）')


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    summarize('腕 B（旗 1）' if len(sys.argv) > 2 else '腕', sys.argv[1], flag1=len(sys.argv) > 2 or '--flag1' in sys.argv)
    if len(sys.argv) > 2 and not sys.argv[2].startswith('--'):
        summarize('旗 0 の対照', sys.argv[2], flag1=False)
