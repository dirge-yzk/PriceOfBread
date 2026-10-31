"""(704) r1 門 G1・G1b・G1' の判定（読むだけ）。閾値は事前登録 measurements/r1_704_20261031_prereg.txt [6] の数そのまま（腕の前に固定）。
使い方：python -X utf8 -I judge_gate.py <計器の出力フォルダ（class_s<種>.tsv がある）> <種の起点> <種の数> [G1 の閾値=0.0097] [G1b の閾値=0.02]
種ごとに計器の 9 国の工場労働者の割合の |warm-open| の中央値と最大を出し、G1（中央値≦0.0097）・G1b（最大≦0.02）を全種で判定する。
記録の列（門ではない）：兵士を除く全階級の構成の差（割合の差の絶対値の和の半分・9 国の中央値と最大）。"""
import sys, csv, statistics as st
FOCUS = ['フランス','日本','イギリス','アメリカ','ロシア','プロイセン','オスマン','清','オーストリア']
def load(path):
    d = {}
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); next(r)
        for row in r:
            if row[0] in ('open', 'warm'): d.setdefault(row[0], {}).setdefault(row[1], {})[row[4]] = float(row[5])
    return d
def main():
    out, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
    g1 = float(sys.argv[4]) if len(sys.argv) > 4 else 0.0097
    g1b = float(sys.argv[5]) if len(sys.argv) > 5 else 0.02
    ok1 = ok1b = True; nread = 0
    print('種\tG1量(9国の|差|中央値)\tG1b量(9国の最大)\t記録:全階級(兵士除く)の構成差 中央値\t最大')
    for s in range(s0, s0 + n):
        try: d = load(f'{out}/class_s{s}.tsv')
        except Exception as e: print('ERR', s, e); ok1 = ok1b = False; continue
        nread += 1
        dw = []; dc = []
        for nat in FOCUS:
            if nat not in d['open'] or nat not in d['warm']: continue
            o, w = d['open'][nat], d['warm'][nat]
            dw.append(abs(w['工場労働者'] / sum(w.values()) - o['工場労働者'] / sum(o.values())))
            ks = [c for c in o if c != '兵士']; to = sum(o[c] for c in ks); tw = sum(w.get(c, 0) for c in ks)
            dc.append(sum(abs(w.get(c, 0) / tw - o[c] / to) for c in ks) / 2)
        m, mx = st.median(dw), max(dw)
        ok1 &= m <= g1; ok1b &= mx <= g1b
        print(f'{s}\t{m:.5f}\t{mx:.5f}\t{st.median(dc):.5f}\t{max(dc):.5f}')
    print(f'読めた種 {nread}/{n}')
    tag = "G1'" if s0 >= 11 else 'G1'
    print(f'{tag} (中央値 ≦ {g1}) 全種で: {"PASS" if ok1 and nread == n else "FAIL"}')
    print(f'{tag}b (最大 ≦ {g1b}) 全種で: {"PASS" if ok1b and nread == n else "FAIL"}')
main()
