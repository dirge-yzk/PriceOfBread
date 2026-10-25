# (545) 線 fl の M4・M5 の読み手（事前登録 measurements/fl545_20261025_prereg.txt の [11] 追記4 のとおり）
# 使い方：python read_m5.py <基線の ins フォルダ> <腕 B の ins フォルダ>
# M4：sbr_s の capErr の最大・legBreach の最大（全国・全期間・種 1〜10）≦ 1e-9
# M5：分類 A の27国・分類 B/C の16国について、種ごとに（四半期 0 と 1 の行ごとに rgoRent/rgoVa を取ってから平均）の腕Bー基線の差 Δ。
#     判定は種 2〜10（種 1 は記録）。rgoVa が 0 の行は飛ばす（両方の行が飛ぶ国×種は組から外す）。
import sys, csv, os
sys.stdout.reconfigure(encoding='utf-8')
A = {  # 国: 旧→新の向き（+1 上がる・-1 下がる）事前登録 [2] の表
 'ポーランド':+1,'デンマーク':+1,'両シチリア':+1,'バイエルン':-1,'ハノーファー':-1,'ヴュルテンベルク':-1,'ライン諸邦':-1,'スイス':-1,
 'マラータ同盟':-1,'シク連邦':-1,'ビルマ':-1,'シャム':-1,'西山朝':-1,'阮氏':-1,'カンボジア':-1,'ラオス':-1,'アラビア':-1,'イエメン':-1,
 'オマーン':-1,'アフガニスタン':-1,'ラヘジ':-1,'モロッコ':-1,'エチオピア':-1,'マダガスカル':-1,'アシャンティ王国':-1,'コンゴ王国':-1,'オヨ帝国':-1}
BC = ['トスカーナ','ヴェネツィア','朝鮮','マイソール','ハイデラバード','ネパール','ペルシャ','ブハラ','ヒヴァ','コーカンド','デリー','ダホメ王国','アルジェ摂政国','ザクセン','教皇領','アワド']
def load(d, s):
    p = os.path.join(d, 'sbr_s%d.tsv' % s)
    with open(p, encoding='utf-8') as f:
        return list(csv.DictReader(f, delimiter='\t'))
def ratio(rows, nation):
    rs = []
    for r in rows:
        if r['nation'] == nation and int(r['quarter']) in (0, 1):
            va = float(r['rgoVa'])
            if va > 0: rs.append(float(r['rgoRent']) / va)
    return sum(rs) / len(rs) if rs else None
def main(base, arm):
    capmax = 0.0; legmax = 0.0
    for s in range(1, 11):
        for r in load(arm, s):
            ce = float(r['capErr']); lb = float(r['legBreach'])
            if ce != ce or lb != lb: capmax = float('inf'); legmax = float('inf'); continue
            capmax = max(capmax, abs(ce)); legmax = max(legmax, abs(lb))
    print('M4 capErr最大 %.3e legBreach最大 %.3e  -> %s' % (capmax, legmax, '通る' if capmax <= 1e-9 and legmax <= 1e-9 else '外れ'))
    D = {}
    for s in range(1, 11):
        b = load(base, s); a = load(arm, s)
        for n in list(A) + BC:
            rb = ratio(b, n); ra = ratio(a, n)
            D[(n, s)] = None if (rb is None or ra is None) else ra - rb
    EPS = 1e-9
    match = opp = zero = nj = 0; per = {}
    for n, sg in A.items():
        m = o = z = 0
        for s in range(2, 11):
            d = D[(n, s)]
            if d is None: nj += 1; continue
            if abs(d) < EPS: z += 1
            elif d * sg > 0: m += 1
            else: o += 1
        per[n] = (m, o, z); match += m; opp += o; zero += z
    inf = match + opp
    print('M5 分類A（種2〜10）: 同じ向き %d 逆向き %d 動かず(Δ=0) %d 判定不能 %d' % (match, opp, zero, nj))
    ok1 = inf > 0 and match >= 0.9 * inf; ok2 = zero <= 24; ok3 = all(v[1] <= 2 for v in per.values())
    print('  同じ向き/(同+逆)=%.3f (>=0.90:%s)  動かずの組<=24:%s(%d)  どの国も逆向き<=2:%s' % (match / inf if inf else 0, ok1, ok2, zero, ok3))
    for n, (m, o, z) in per.items():
        d1 = D[(n, 1)]
        print('   %-10s 同%d 逆%d 零%d  種1のΔ=%s' % (n, m, o, z, 'NA' if d1 is None else '%+.4f' % d1))
    small = tot = 0
    for n in BC:
        for s in range(2, 11):
            d = D[(n, s)]
            if d is None: continue
            tot += 1
            if abs(d) <= 0.01: small += 1
    ok4 = tot > 0 and small >= 0.9 * tot
    print('M5 分類B/C（種2〜10）: |Δ|<=0.01 が %d/%d = %.3f (>=0.90:%s)' % (small, tot, small / tot if tot else 0, ok4))
    big = [(n, s, D[(n, s)]) for n in BC for s in range(1, 11) if D[(n, s)] is not None and abs(D[(n, s)]) > 0.01]
    for n, s, d in big[:40]: print('   外れ組 %s 種%d Δ=%+.4f' % (n, s, d))
    print('M5 総合: %s' % ('通る' if (ok1 and ok2 and ok3 and ok4) else '外れ'))
if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
