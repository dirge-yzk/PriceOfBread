"""(389) 段0 その1：官僚制度（資格任用）への到達の読み手。

使い方：PYTHONUTF8=1 python -I scripts/instruments/ab/an_bur0.py <計器の出力フォルダ（scalars_s*.tsv がある所）> [出力ファイル]

読むだけ。入力は INS=1 の計器つき走行の scalars_s*.tsv（列 3 が国・列 2 が年・列 11 が bureaucracy＝0 猟官・1 資格任用・2 専門官僚）。
出力は国ごとに：種の数・開幕の官僚制度・最終が猟官制の種の数・猟官制から資格任用以上へ届いた種の数とその年の中央値・専門官僚制へ届いた種の数。
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import statistics
import sys
from collections import defaultdict

COL_YEAR, COL_NATION, COL_BUREAUCRACY = 1, 2, 10


def read_seed(path):
    start, last, merit_year, prof_year = {}, {}, {}, {}
    with open(path, 'rb') as fh:
        for raw in fh:
            cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
            if cols[0] == 'quarter':
                continue
            nation, year, level = cols[COL_NATION], int(cols[COL_YEAR]), int(cols[COL_BUREAUCRACY])
            start.setdefault(nation, level)
            last[nation] = level
            if level >= 1 and nation not in merit_year:
                merit_year[nation] = year
            if level >= 2 and nation not in prof_year:
                prof_year[nation] = year
    return start, last, merit_year, prof_year


def main():
    folder = sys.argv[1]
    per_nation = defaultdict(list)
    for path in sorted(glob.glob(folder + '/scalars_s*.tsv')):
        start, last, merit_year, prof_year = read_seed(path)
        for nation in start:
            per_nation[nation].append((start[nation], last[nation], merit_year.get(nation), prof_year.get(nation)))
    lines = ['nation\tseeds\tstart0\tend0(Spoils)\tmeritReached\tmeritYearMedian\tprofReached']
    for nation in sorted(per_nation, key=lambda n: -len(per_nation[n])):
        rows = per_nation[nation]
        if len(rows) < 3:
            continue
        years = [r[2] for r in rows if r[2] is not None and r[0] == 0]
        lines.append('%s\t%d\t%s\t%d\t%d\t%s\t%d' % (
            nation, len(rows), ''.join(str(r[0]) for r in rows), sum(1 for r in rows if r[1] == 0),
            len(years), (statistics.median(years) if years else '-'), sum(1 for r in rows if r[3] is not None)))
    # 採否の規則（事前登録 追記3 [A]）の数：開幕が猟官制で、最終が資格任用以上の（国, 種）の数。国の別は 3 種以上ある国だけ
    reach_pairs = sum(sum(1 for r in rows if r[0] == 0 and r[1] >= 1) for rows in per_nation.values() if len(rows) >= 3)
    spoils_pairs = sum(sum(1 for r in rows if r[0] == 0) for rows in per_nation.values() if len(rows) >= 3)
    lines.append('TOTAL\treachedPairs=%d\tspoilsStartPairs=%d' % (reach_pairs, spoils_pairs))
    text = '\n'.join(lines) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as out:
            out.write(text.encode('utf-8'))
    else:
        sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
