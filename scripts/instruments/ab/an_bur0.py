"""(389) 段0 その1：官僚制度（資格任用）への到達の読み手。

使い方：PYTHONUTF8=1 python -I scripts/instruments/ab/an_bur0.py <計器の出力フォルダ（scalars_s*.tsv と burblock_s*.tsv がある所）> [出力ファイル]

読むだけ。入力は INS=1 の計器つき走行の scalars_s*.tsv（列 0 が四半期・列 2 が国・列 1 が年・列 10 が bureaucracy＝0 猟官・1 資格任用・2 専門官僚）と、あれば burblock_s*.tsv（SimCore/InstrumentsBur.cs の冒頭の注が定義）。
出力は国ごとに：種の数・開幕の官僚制度・最終が猟官制の種の数・猟官制から資格任用以上へ届いた種の数とその年の中央値・専門官僚制へ届いた種の数。
TOTAL 行（事前登録 追記3 [B]・追記4 [P]）：
  reachedPairs＝開幕が猟官制で最終が資格任用以上の（国, 種）の数（3 種以上ある国だけ）／spoilsStartPairs＝開幕が猟官制の（国, 種）の数
  reachedFeasPairs＝reachedPairs のうち、施行の直前（官僚制度が 0 の最後の burblock 行・施行の四半期か、それより前）の feasMask が 0 でない組（錠でなく門を通る欲しさの下で届いた組）
  reachedWantOnlyPairs＝同じ行の wantMask は立つが feasMask が 0／reachedNoRowPairs＝その行が無い、または wantMask も 0（議会の経路など、欲しさに由来しない到達）
NAMES 行：3 種以上ある国の数と、国名の集合の指紋（腕の間で国の割れ方がずれていないかを見る）。
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import hashlib
import os
import statistics
import sys
from collections import defaultdict

COL_QUARTER, COL_YEAR, COL_NATION, COL_BUREAUCRACY = 0, 1, 2, 10
BCOL_QUARTER, BCOL_NATION, BCOL_BUR, BCOL_WANT, BCOL_FEAS = 0, 2, 3, 6, 8


def read_burblock(path):
    """国ごとに (四半期, bur, wantMask, feasMask) のリスト（ファイルの順）。ファイルが無ければ空。"""
    rows = defaultdict(list)
    if not os.path.exists(path):
        return rows
    with open(path, 'rb') as fh:
        for raw in fh:
            cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
            if cols[0] == 'quarter':
                continue
            rows[cols[BCOL_NATION]].append((int(cols[BCOL_QUARTER]), int(cols[BCOL_BUR]), int(cols[BCOL_WANT]), int(cols[BCOL_FEAS])))
    return rows


def read_seed(path):
    start, last, merit_year, merit_quarter, prof_year = {}, {}, {}, {}, {}
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
                merit_quarter[nation] = int(cols[COL_QUARTER])
            if level >= 2 and nation not in prof_year:
                prof_year[nation] = year
    return start, last, merit_year, merit_quarter, prof_year


def route_of(burblock_rows, nation, enact_quarter):
    """施行の直前の burblock 行（官僚制度が 0 の最後の行・施行の四半期か、それより前）で、到達の経路を分ける。"""
    before = [r for r in burblock_rows.get(nation, []) if r[1] == 0 and r[0] <= enact_quarter]
    if not before:
        return 'noRow'
    quarter, bur, want, feas = before[-1]
    if feas:
        return 'feas'
    return 'wantOnly' if want else 'noRow'


def main():
    folder = sys.argv[1]
    per_nation = defaultdict(list)
    for path in sorted(glob.glob(folder + '/scalars_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        burblock = read_burblock(os.path.join(folder, 'burblock_s%s.tsv' % seed))
        start, last, merit_year, merit_quarter, prof_year = read_seed(path)
        for nation in start:
            route = route_of(burblock, nation, merit_quarter[nation]) if nation in merit_quarter and start[nation] == 0 else '-'
            per_nation[nation].append((start[nation], last[nation], merit_year.get(nation), prof_year.get(nation), route))
    lines = ['nation\tseeds\tstart0\tend0(Spoils)\tmeritReached\tmeritYearMedian\tprofReached\troutes(feas/wantOnly/noRow)']
    for nation in sorted(per_nation, key=lambda n: (-len(per_nation[n]), n)):
        rows = per_nation[nation]
        if len(rows) < 3:
            continue
        years = [r[2] for r in rows if r[2] is not None and r[0] == 0]
        routes = [r[4] for r in rows if r[0] == 0 and r[1] >= 1]
        lines.append('%s\t%d\t%s\t%d\t%d\t%s\t%d\t%d/%d/%d' % (
            nation, len(rows), ''.join(str(r[0]) for r in rows), sum(1 for r in rows if r[1] == 0),
            len(years), (statistics.median(years) if years else '-'), sum(1 for r in rows if r[3] is not None),
            routes.count('feas'), routes.count('wantOnly'), routes.count('noRow')))
    # 採否の規則（事前登録 追記3 [B]・追記4 [P]）の数：開幕が猟官制で、最終が資格任用以上の（国, 種）の数。国の別は 3 種以上ある国だけ
    kept = {n: rows for n, rows in per_nation.items() if len(rows) >= 3}
    reached = [r for rows in kept.values() for r in rows if r[0] == 0 and r[1] >= 1]
    spoils_pairs = sum(sum(1 for r in rows if r[0] == 0) for rows in kept.values())
    lines.append('TOTAL\treachedPairs=%d\tspoilsStartPairs=%d\treachedFeasPairs=%d\treachedWantOnlyPairs=%d\treachedNoRowPairs=%d' % (
        len(reached), spoils_pairs, sum(1 for r in reached if r[4] == 'feas'), sum(1 for r in reached if r[4] == 'wantOnly'), sum(1 for r in reached if r[4] == 'noRow')))
    names = sorted(kept)
    lines.append('NAMES\tnations3=%d\tdigest=%s' % (len(names), hashlib.sha256('\n'.join(names).encode('utf-8')).hexdigest()[:12]))
    text = '\n'.join(lines) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as out:
            out.write(text.encode('utf-8'))
    else:
        sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
