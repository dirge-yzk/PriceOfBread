# (895) c76ls 恒等の計器 landshare_s の門と読み（事前登録 measurements/m895_20261106_prereg_ls.txt [3]・[4]）。
#   python -X utf8 gate895.py <ins の生のフォルダ> <出力の .txt>      … ident の生の landshare_s*.tsv を全て読む
#   python -X utf8 gate895.py --selftest                              … 人工のずれの行で門が落ちることを確かめる（突然変異 M10＝TOL を 1 へ緩めると赤）
# 門（データ無しの盤面＝全ての国が null）：全ての種×国×四半期で maxRelDiffSlotVsActual ≦ TOL・maxRelDiffDataVsField ≦ TOL・rgoFieldCount＝0・rgoFieldEndowCount＝0・rgoFieldColonialCount＝0・slotFallbackCount＝0。
#   double のまま比べる（TSV の F は G10＝相対 1e-10 の印字の床＝TOL を床に置かない：計器が double の差を書き、ここは書かれた値と 1e-12 を比べるだけ）。
# 読み（記録・採否に使わない）：maxRelDiffSlotVsActual の分布・colonialRemitPerQuarter が正の国（訂正 1 の (a) が効いた国）・rentNonEndowByOwnerClass* の q0 の国ごとの値（B-2a の予言の材料）・rentEndow の割合 e。
import sys, os, glob, csv, math

TOL = 1e-12
CLS = ['Noble', 'Clergy', 'Peasant', 'Capitalist', 'Other']


def fl(x):
    try:
        return float(x)
    except Exception:
        return math.nan


def check_rows(rows):
    """門の破れの一覧（(種, 国, 四半期, 列, 値)）。"""
    bad = []
    for seed, r in rows:
        for c in ('maxRelDiffSlotVsActual', 'maxRelDiffDataVsField'):
            v = fl(r[c])
            if not (v <= TOL):
                bad.append((seed, r['nation'], r['quarter'], c, r[c]))
        for c in ('rgoFieldCount', 'rgoFieldEndowCount', 'rgoFieldColonialCount', 'slotFallbackCount'):
            if fl(r[c]) != 0:
                bad.append((seed, r['nation'], r['quarter'], c, r[c]))
    return bad


def read_dir(d):
    rows = []
    for p in sorted(glob.glob(os.path.join(d, 'landshare_s*.tsv'))):
        seed = os.path.basename(p)[len('landshare_s'):-4]
        with open(p, encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                rows.append((seed, r))
    return rows


def selftest():
    base = {'nation': 'X', 'quarter': '0', 'maxRelDiffSlotVsActual': '0', 'maxRelDiffDataVsField': '0',
            'rgoFieldCount': '0', 'rgoFieldEndowCount': '0', 'rgoFieldColonialCount': '0', 'slotFallbackCount': '0'}
    ok = check_rows([('1', dict(base))]) == []
    r2 = dict(base); r2['maxRelDiffSlotVsActual'] = '1e-06'
    r3 = dict(base); r3['maxRelDiffDataVsField'] = 'nan'
    r4 = dict(base); r4['rgoFieldCount'] = '3'
    r5 = dict(base); r5['maxRelDiffSlotVsActual'] = 'inf'
    caught = [len(check_rows([('1', x)])) == 1 for x in (r2, r3, r4, r5)]
    print('selftest 綺麗な行が通る', ok, '・人工のずれ（1e-6・nan・欄 3・inf）を落とす', caught)
    return 0 if ok and all(caught) else 1


def main():
    if len(sys.argv) > 1 and sys.argv[1] == '--selftest':
        sys.exit(selftest())
    d, out = sys.argv[1], sys.argv[2]
    rows = read_dir(d)
    bad = check_rows(rows)
    L = []
    seeds = sorted({s for s, _ in rows}, key=lambda x: int(x) if x.isdigit() else 0)
    L.append(f'(895) gate895.py：{d}・種 {len(seeds)}（{",".join(seeds)}）・行 {len(rows)}・TOL {TOL}')
    L.append(f'門の破れ {len(bad)} 件' + ('' if not bad else '（先頭 20）'))
    for b in bad[:20]:
        L.append('  ' + '\t'.join(str(x) for x in b))
    # 分布
    vals = sorted(fl(r['maxRelDiffSlotVsActual']) for _, r in rows)
    nz = [v for v in vals if v != 0]
    L.append(f'maxRelDiffSlotVsActual：0 の行 {len(vals) - len(nz)}・0 でない行 {len(nz)}・最大 {max(vals) if vals else math.nan:.3e}')
    vd = sorted(fl(r['maxRelDiffDataVsField']) for _, r in rows)
    L.append(f'maxRelDiffDataVsField：0 でない行 {sum(1 for v in vd if v != 0)}・最大 {max(vd) if vd else math.nan:.3e}')
    # 送金の項
    remit = {}
    for s, r in rows:
        if fl(r['colonialRemitPerQuarter']) > 0:
            remit.setdefault(r['nation'], set()).add(s)
    L.append('colonialRemitPerQuarter が正の国（種の数）：' + '・'.join(f'{k} {len(v)}' for k, v in sorted(remit.items(), key=lambda kv: -len(kv[1]))))
    # q0 の B-2a の材料（種の中央値）
    by = {}
    for s, r in rows:
        if r['quarter'] != '0':
            continue
        ne = fl(r['rentNonEndowPerQuarter']); en = fl(r['rentEndowPerQuarter'])
        by.setdefault(r['nation'], []).append((ne, en, [fl(r[f'rentNonEndowByOwnerClass{c}PerQuarter']) for c in CLS]))
    L.append('q0 の非賦存の地代（種の中央値）と賦存の割合 e＝rentEndow÷(rentEndow＋rentNonEndow)・非賦存の地代の持ち主の階級の割合（Noble/Clergy/Peasant/Capitalist/Other）')
    L.append('nation\tseeds\trentNonEndowPerQuarter\te\t' + '\t'.join('share' + c for c in CLS))

    def med(xs):
        xs = sorted(xs)
        n = len(xs)
        return math.nan if n == 0 else (xs[n // 2] if n % 2 else 0.5 * (xs[n // 2 - 1] + xs[n // 2]))
    for nat, lst in sorted(by.items()):
        ne = med([x[0] for x in lst]); e = med([x[1] / (x[0] + x[1]) if x[0] + x[1] > 0 else math.nan for x in lst])
        sh = [med([x[2][k] / x[0] if x[0] > 0 else math.nan for x in lst]) for k in range(5)]
        L.append(f'{nat}\t{len(lst)}\t{ne:.6g}\t{e:.4f}\t' + '\t'.join(f'{v:.4f}' for v in sh))
    L.append('判定: ' + ('門 通る' if not bad else '門 破れ'))
    with open(out, 'wb') as fh:
        fh.write(('\n'.join(L) + '\n').encode('utf-8'))
    print('\n'.join(L[:6]))
    print(L[-1])
    sys.exit(0 if not bad else 1)


if __name__ == '__main__':
    main()
