#!/usr/bin/env python
"""(440) 線 dp の〈機構が狙いどおり働いたか〉の読み手（D3＝侵略の近接の緊張の標的の座標）：計器の表 invade_s<種>.tsv から事前登録 [12]（追記 4）の T-a・健全性・記録を式のとおりに出す。
使い方：python an_invade.py <計器の出力フォルダ（…/ins）> [旗 0 の…/ins]
  1 つ渡す＝その腕だけを読む（旗の値は表から決めない＝読み手は旗 0 と旗 1 を区別しない。旗 1 の腕では積んだ量と旧の量が違う行が出る）。2 つ渡す＝先を B（旗 1）、後を旗 0 の対照として並べる。
事前登録 measurements/dp_20261019_prereg.txt [12] 追記 4：
  T-a（狙い・外れうる）抽象標的（targetIsNation=0）の屈服（submitted=1）の行で proximityWouldHave＝0 かつ proximityAdded＞0 が、B の延べで ≥1（0 なら〈この盤面は旗を踏まなかった〉）。
  必要性の確認：旗 0 の腕で抽象標的の屈服が 0 件なら、旗 1 の腕は同一の軌道（旗が効く点が無い）＝B を回さない。
  健全性（作りの上で成る＝踏んだ証拠ではない）：抽象の屈服以外の全行で proximityAdded＝proximityWouldHave（旗に依らず座標が動かない）。
  記録：侵略の件数と屈服率・屈服（実在／抽象）と拒否（実在／抽象）の積んだ量の和・標的自身の組（q==target）に積んだ量と第三国に積んだ量の分け。
⚠限界（この読み手が自己申告する）：invade_s は World.Invade が Subjugate を終えた所の 1 行（AI もプレイヤーも同じ動詞）。proximityAdded は AddTension に渡した増分の和で、
  緊張の上限 200 で切れた分は含まない（積もうとした量）。wouldHave は旗 1 のときだけ〈屈服の後の座標〉で計算し直した量（旗 0 では added と同じ）。
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
    print(f'  旧の規則での量の和：屈服（実在 {s(subN, "proximityWouldHave"):.3f}・抽象 {s(subA, "proximityWouldHave"):.3f}）／拒否（実在 {s(refN, "proximityWouldHave"):.3f}・抽象 {s(refA, "proximityWouldHave"):.3f}）')
    tot = s(rows, 'proximityAdded')
    toT = s(rows, 'addedToTarget')
    print(f'  記録：標的自身の組（q==target）に積んだ量 {toT:.3f}／第三国に積んだ量 {tot - toT:.3f}（積んだ量の和 {tot:.3f}・標的自身の割合 {toT / tot if tot > 0 else 0:.3f}）'
          f'／拒否の枝の分 標的自身 {s(ref, "addedToTarget"):.3f}・第三国 {s(ref, "proximityAdded") - s(ref, "addedToTarget"):.3f}')
    ta = [r for r in subA if fl(r['proximityWouldHave']) == 0 and fl(r['proximityAdded']) > 0]
    print(f'  T-a（狙い・外れうる）抽象標的の屈服で 旧の量=0 かつ 積んだ量>0 = {len(ta)} 行（狙い ≥1・旗 1 の腕で。0 なら旗を踏んでいない）')
    other = [r for r in rows if not (r['submitted'] == '1' and r['targetIsNation'] == '0')]
    bad = [r for r in other if abs(fl(r['proximityAdded']) - fl(r['proximityWouldHave'])) > 1e-9]
    print(f'  健全性（踏んだ証拠ではない）抽象の屈服以外で 積んだ量≠旧の量 の行 = {len(bad)}（0 のはず）')
    absAll0 = [r for r in subA if abs(fl(r['proximityAdded']) - fl(r['proximityWouldHave'])) > 1e-9]
    print(f'  （参考）抽象の屈服で 積んだ量≠旧の量 = {len(absAll0)} 行：旗 0 の腕では 0 のはず・旗 1 の腕では T-a の行数と同じ）')


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    summarize('腕 B（旗 1）' if len(sys.argv) > 2 else '腕', sys.argv[1])
    if len(sys.argv) > 2:
        summarize('旗 0 の対照', sys.argv[2])
