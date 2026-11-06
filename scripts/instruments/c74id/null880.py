# (880) c74id holown880 のヌル腕どうしの自己試験と区別する力の記録（旗 0 とヌル腕の生だけ・事前登録 measurements/m880_20261106_prereg_id.txt [2]）。
#   python -X utf8 -I null880.py <出力>
#   6 対（N1→M・N2→M・M→N1・N2→N1・M→N2・N1→N2＝左を腕と見なす）×水準 1/10・1/4 で A2 と R880 の〈線が原因〉を数え、R880 が A2 に足した件を全て書く。
#   区別する力：種 1〜10 の全ての州について、3 本（M・N1・N2）の持ち主の軌道（nation の名前の四半期ごとの並び）が割れる州の割合と、3 本の 1/4 事象（例外を除く）のうち割れた州にある割合。
import sys, os, collections
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import holown880 as H
REPO = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
M873 = os.path.join(REPO, 'measurements', 'm873_20261106')
RUNS = {'M': (os.path.join(M873, 'armM', 'stop_mM.txt'), 'D:/PoB_work/keep_873/base_ins'),
        'N1': (os.path.join(M873, 'null', 'stop_n1.txt'), 'D:/PoB_work/keep_873/n1'),
        'N2': (os.path.join(M873, 'null', 'stop_n2.txt'), 'D:/PoB_work/keep_873/n2')}
OUT = sys.argv[1]
L = ['(880) null880：ヌル腕どうしの holown880（旗 0 とヌル腕の生だけ・m873 の M＝keep_873/base_ins・N1・N2）']
L.append('対\t水準\t事象の行\tA2 の線が原因\tR880 の線が原因\tR880 が A2 に足した\t読めない')
added = []
for a, b in (('N1', 'M'), ('N2', 'M'), ('M', 'N1'), ('N2', 'N1'), ('M', 'N2'), ('N1', 'N2')):
    for lv in ('1/10', '1/4'):
        res, C = H.classify(RUNS[a][0], RUNS[a][1], [RUNS[b]], lv)
        L.append('%s→%s\t%s\t%d\t%d\t%d\t%d\t%d' % (a, b, lv, C['行'] - C['例外'], C['A2 線が原因'], C['線が原因'], C['線が原因'] - C['A2 線が原因'], C['読めない']))
        for lab, why, a2, c in res:
            if lab != a2 and lab != 'L0': added.append('%s→%s\t%s\t%s\t%s\tA2=%s\t%s' % (a, b, lv, lab, why, a2, c['text']))
L.append('## R880 が A2 と違う行（全て）：%d' % len(added))
L += added if added else ['（無い）']
L.append('## 1/10 の水準：ヌル腕の 1/10 事象は 0 件＝〈ヌル腕どうしで線が原因 0〉は空振り（何も検査していない）。1/4 の水準の数は記録（A2 はもともと 1/4 の水準のための規則ではない）。')
L.append('## 突然変異 MU-a3（閾を 1/4 へ緩める）＝上の表の 1/4 の行の R880 の線が原因（1/10 の 0 件から増える）。')
# 区別する力
n_reg = n_split = 0; ev = ev_split = 0
evs = collections.defaultdict(set)
for k, (an, _) in RUNS.items():
    for c in H.cases(an):
        if c['level'] == '1/4' and not c['exempt']: evs[c['seed']].add(c['region'])
for seed in range(1, 11):
    seqs = []
    for k in ('M', 'N1', 'N2'):
        H._cache.clear()
        ins = RUNS[k][1]
        D = H.series(ins, seed, '__全て__')   # 読み込みだけ（キャッシュを作る）
        seqs.append({r: tuple(n for _, _, _, n in rows) for r, rows in H._cache[(ins, seed)].items()})
    H._cache.clear()
    common = set(seqs[0]) & set(seqs[1]) & set(seqs[2])
    for r in common:
        n_reg += 1
        sp = len({seqs[0][r], seqs[1][r], seqs[2][r]}) > 1
        n_split += sp
        if r in evs[seed]: ev += 1; ev_split += sp
L.append('## 区別する力（記録）：種 1〜10 の 3 本に在る州 %d のうち持ち主の軌道が割れる州 %d（%.4f）・3 本の 1/4 事象（例外を除く・種と州で重複を除く）%d のうち割れた州 %d（%.4f）'
         % (n_reg, n_split, n_split / max(n_reg, 1), ev, ev_split, ev_split / max(ev, 1)))
L.append('##   持ち主の照合が効くのは〈A2 が元からの弱さ〉の件だけ＝その件の数（上の表の A2 の線が原因でない行）が区別する力の上限。')
with open(OUT, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
print('\n'.join(L))
