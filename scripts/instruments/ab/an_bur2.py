"""(389) 事前登録 追記4 [R] R6：開いた戦線の数（置いた官僚制度の目標が残ると増える経路）の読み手。

使い方：PYTHONUTF8=1 python -I scripts/instruments/ab/an_bur2.py <計器の出力フォルダ（nation_s*.tsv がある所）> [出力ファイル]

読むだけ。nation_s*.tsv の列を名前で引く：year・openFrontsCount・openFrontsLevelLawCount・militaryMoveOpenFrontsCount（-1 は無効の印＝数えない）。
出力：十年ごと・全国×四半期の平均。A と B1・B2 の出力を並べて差を見る（採否には使わない・記録）。
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import sys
from collections import defaultdict

WANT = ('openFrontsCount', 'openFrontsLevelLawCount', 'militaryMoveOpenFrontsCount')


def main():
    folder = sys.argv[1]
    sums = defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0])
    for path in sorted(glob.glob(folder + '/nation_s*.tsv')):
        with open(path, 'rb') as fh:
            head = fh.readline().decode('utf-8').rstrip('\r\n').split('\t')
            index = {name: i for i, name in enumerate(head)}
            year_i = index['year']
            idx = [index[name] for name in WANT]
            for raw in fh:
                cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
                decade = int(cols[year_i]) // 10 * 10
                row = sums[decade]
                row[0] += 1
                row[1] += float(cols[idx[0]])
                row[2] += float(cols[idx[1]])
                move = float(cols[idx[2]])
                if move >= 0:
                    row[3] += move
                    row[4] += 1
    lines = ['decade\trows\topenFronts\topenFrontsLevelLaw\tmilitaryMoveOpenFronts']
    for decade in sorted(sums):
        n, a, b, c, m = sums[decade]
        lines.append('%d\t%d\t%.4f\t%.4f\t%s' % (decade, n, a / n, b / n, ('%.4f' % (c / m)) if m else '-'))
    text = '\n'.join(lines) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as out:
            out.write(text.encode('utf-8'))
    else:
        sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
