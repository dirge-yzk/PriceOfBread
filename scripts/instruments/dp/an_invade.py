#!/usr/bin/env python
"""(440) 線 dp の読み手（侵略の近接の緊張の標的の座標）：計器の表 invade_s<種>.tsv（列＝year・invader・target・targetIsNation・submitted・proximityAdded・addedToTarget）から、事前登録 [12]（追記 4・5）の記録を式のとおりに出す。
使い方：python an_invade.py <計器の出力フォルダ（…/ins）> [比べる側の…/ins]
  2 つ渡すと並べる。旗は焼き込み済み（2026-10-19）＝旗 0 の旧の挙動は、焼き込み前の旗の木 d7012bfe（腕 A）で作った表にしか無い（基点 e1973001 には invade_s が無い）。⚠ 腕 A・B の生（measurements/dp_20261019_invade_A・_B）は 9 列の旧形式（この読み手で読める）・canary の dp_20261019_invade_s1_flag0_canary.tsv は別の 7 列（proximityWouldHave 入り・addedToTarget 無し）で読めない（KeyError）。
  記録：侵略の件数と屈服率・屈服（実在／抽象）と拒否（実在／抽象）の積んだ量の和・標的自身の組（q==target）に積んだ量と第三国に積んだ量の分け。
  T-a（旗の効きを見た行）：抽象標的（targetIsNation=0）の屈服（submitted=1）の行で proximityAdded＞0 の件数（旧の形では 0 のはずの行＝旧の欠陥が直った行）。
⚠限界（この読み手が自己申告する）：invade_s は World.Invade が Subjugate を終えた所の 1 行（AI もプレイヤーも同じ動詞）。proximityAdded は AddTension に渡した増分の和で、
  緊張の上限 200 で切れた分は含まない（積もうとした量）。表が 1 本も無い試合（侵略が起きなかった）は行 0 として数える。合否は止める線と合わせて報告で行う（この読み手は採否を決めない）。
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


def summarize(label, d):
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
    print(f'  屈服 {len(sub)}（実在の国 {len(subN)}・抽象標的 {len(subA)}）／拒否 {len(ref)}（実在の国 {len(refN)}・抽象標的 {len(refA)}）／屈服率 {len(sub) / len(rows):.3f}')
    print(f'  積んだ量の和：屈服（実在 {s(subN, "proximityAdded"):.3f}・抽象 {s(subA, "proximityAdded"):.3f}）／拒否（実在 {s(refN, "proximityAdded"):.3f}・抽象 {s(refA, "proximityAdded"):.3f}）')
    tot = s(rows, 'proximityAdded')
    toT = s(rows, 'addedToTarget')
    print(f'  記録：標的自身の組（q==target）に積んだ量 {toT:.3f}／第三国に積んだ量 {tot - toT:.3f}（積んだ量の和 {tot:.3f}・標的自身の割合 {toT / tot if tot > 0 else 0:.3f}）'
          f'／拒否の枝の分 標的自身 {s(ref, "addedToTarget"):.3f}・第三国 {s(ref, "proximityAdded") - s(ref, "addedToTarget"):.3f}')
    ta = [r for r in subA if fl(r['proximityAdded']) > 0]
    print(f'  T-a 抽象標的の屈服で積んだ量>0 = {len(ta)} 行／抽象標的の屈服 {len(subA)} 行中（旧の形では全て 0）')


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    summarize('腕', sys.argv[1])
    if len(sys.argv) > 2:
        summarize('比べる側', sys.argv[2])
