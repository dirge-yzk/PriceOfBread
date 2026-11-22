# (1303) 検算の台本：arrears_s の underLimit を前の月の比へ直した時の、基準の生と腕の生の違いが規則どおりかを確かめる（読むだけ・エンジンを呼ばない）。
#   python -I arrears_recheck.py <基準の arrears_s の tsv> <腕の arrears_s の tsv>   → 0＝規則どおり・1＝違反・2＝読めない
# 限界：払える率か国債が変わった行は〈基準だけ＋腕だけ〉の組に見える（(e) の条件を満たせば通る）＝エンジンの変更は成績表と他の表のバイト一致が守る。
# 規則は scripts の事前登録 measurements/m1303_20261122_prereg_ar.txt [2] の (a)〜(e)。
import sys, csv
from collections import OrderedDict

csv.field_size_limit(1 << 26)
FREE = ('underLimit', 'owes', 'predNew', 'arrOldMonth', 'arrNewMonth', 'cumOld', 'cumNew')
KEYCOLS_SKIP = set(FREE)


def read(p):
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        head = next(rd)
        return head, [r for r in rd if r]


def main():
    if len(sys.argv) != 3:
        print('使い方：arrears_recheck.py <基準> <腕>'); return 2
    try:
        hb, rb = read(sys.argv[1]); ha, ra = read(sys.argv[2])
    except Exception as e:
        print('読めない：', e); return 2
    bad = []

    def viol(s):
        bad.append(s)

    if hb != ha:
        viol('(a) 見出しが違う'); return finish(bad, 0, 0, 0, 0)
    ix = {h: i for i, h in enumerate(hb)}
    need = ('quarter', 'nation', 'payRate', 'govDebt', 'warm') + FREE + ('predOld',)
    if any(n not in ix for n in need):
        viol('(a) 必要な列が無い'); return finish(bad, 0, 0, 0, 0)
    keyidx = [ix['payRate'], ix['govDebt']]   # 月の鍵＝（四半期・国）の中で払える率と国債（月ごとに違う）。他の列は共通の行で突き合わせる
    fixed = [i for h, i in ix.items() if h not in KEYCOLS_SKIP]

    def owes_of(r):
        return float(r[ix['payRate']]) < 1 and float(r[ix['govDebt']]) > 1e-9 and r[ix['underLimit']] == '0' and r[ix['warm']] == '0'

    # (b) 自分の列で owes が成り立つ
    for tag, rows in (('基準', rb), ('腕', ra)):
        n = 0
        for k, r in enumerate(rows):
            if (r[ix['owes']] == '1') != owes_of(r):
                n += 1
                if n <= 2: viol(f'(b) {tag} の行 {k + 2} で owes が自分の列と食い違う')
        if n > 2: viol(f'(b) {tag} の食い違いは計 {n} 行')

    def group(rows, tag):
        g = OrderedDict(); dup = 0
        for r in rows:
            gk = (r[ix['quarter']], r[ix['nation']])
            key = tuple(r[i] for i in keyidx)
            d = g.setdefault(gk, OrderedDict())
            if key in d: dup += 1
            d[key] = r
        if dup: viol(f'(c) {tag} に鍵の重複 {dup} 行')
        return g

    gb, ga = group(rb, '基準'), group(ra, '腕')
    dirty = set()      # owes の食い違いが先にあった国
    shared = bo = ao = 0
    order = list(OrderedDict.fromkeys(list(gb.keys()) + list(ga.keys())))
    # 国ごとに四半期の順で数える（dirty は国をまたいで混ざらないよう国ごと・四半期の昇順）
    order.sort(key=lambda k: (k[1], int(k[0])))
    for gk in order:
        nat = gk[1]
        db, da = gb.get(gk, {}), ga.get(gk, {})
        kb, ka = list(db.keys()), list(da.keys())
        sb, sa = set(kb), set(ka)
        cb = [k for k in kb if k in sa]; ca = [k for k in ka if k in sb]
        if cb != ca:
            viol(f'(c) {gk} の共通の行の順序が違う')
        # この四半期の食い違いは先に数える（cum は同じ四半期の後の行にも及ぶ）
        for k in kb:
            if k not in sa:
                bo += 1; dirty.add(nat)
                r = db[k]
                if not (r[ix['owes']] == '1' and r[ix['underLimit']] == '0' and r[ix['predOld']] == '0'):
                    viol(f'(e) 基準だけの行が規則の外 {gk}')
        for k in ka:
            if k not in sb:
                ao += 1; dirty.add(nat)
                r = da[k]
                if not (r[ix['owes']] == '1' and r[ix['underLimit']] == '0' and r[ix['predOld']] == '0'):
                    viol(f'(e) 腕だけの行が規則の外 {gk}')
        for k in cb:
            shared += 1
            x, y = db[k], da[k]
            if x[ix['owes']] != y[ix['owes']]: dirty.add(nat)
        for k in cb:
            x, y = db[k], da[k]
            if any(x[i] != y[i] for i in fixed):
                viol(f'(d) {gk} の共通の行で自由でない列が違う（{",".join(hb[i] for i in fixed if x[i] != y[i])}）')
            for h in FREE:
                if x[ix[h]] == y[ix[h]]: continue
                i = ix[h]
                if h == 'underLimit': continue
                if h == 'owes': continue
                if h in ('predNew', 'arrOldMonth', 'arrNewMonth'):
                    if x[ix['owes']] == y[ix['owes']]:
                        viol(f'(d) {gk} の共通の行で owes が同じなのに {h} が違う')
                else:   # cumOld・cumNew
                    if nat not in dirty:
                        viol(f'(d) {gk} で食い違いの前に {h} が違う')
    return finish(bad, shared, bo, ao, len(ra))


def finish(bad, shared, bo, ao, nrows):
    if bad:
        print(f'違反 {len(bad)} 件')
        for s in bad[:5]: print('  ' + s)
        return 1
    print(f'規則どおり：共通の行 {shared}・基準だけの行 {bo}・腕だけの行 {ao}・腕の行 {nrows}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
