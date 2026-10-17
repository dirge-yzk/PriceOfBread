"""(389) 段0 その2：AI の官僚制度の判断の表 burblock_s の読み手。

使い方：PYTHONUTF8=1 python -I scripts/instruments/ab/an_bur1.py <計器の出力フォルダ（burblock_s*.tsv がある所）> [出力ファイル]

読むだけ。入力の列＝quarter・year・nation・bur・burIn・burOut・wantMask・blockMask・feasMask・need（SimCore/InstrumentsBur.cs の冒頭の注が定義）。
出力 1（国ごと・種を通して）：
  wantQ＝官僚制度が前提の法を欲しがった四半期の数／blockQ＝さらに今の官僚制度では足りなかった四半期の数／feasQ＝止まった四半期のうち官僚制度が足りていれば法律タブの門も通る法があった四半期の数（旗 2 の対象）
  stuckQ＝目標が現行と違うのに欲しい法が無かった四半期の数（残った目標）／firstWant・firstBlock＝最初の年（種の中央値）／unblockYears＝最初に止まった年から官僚制度が届いた年までの年数の中央値（届かない種は数えない）
  reached＝届いた種の数／bits＝止まった四半期のうち法の別の数（1 社会保障・2 独占禁止・4 国有化・8 国有化〔全て〕・16 累進課税・32 その他）
出力 2（十年ごと・全国の合計）：decade・wantQ・blockQ・feasQ・feasFirstBits（最初に feasMask が立った国の数ではなく、feasMask の法の別の数）
標準ライブラリだけ・ファイルはバイトで読み UTF-8 で解く。
"""
import glob
import statistics
import sys
from collections import defaultdict

QUARTER, YEAR, NATION, BUR, BUR_IN, BUR_OUT, WANT, BLOCK, FEAS, NEED = range(10)
BITS = [(1, 'ss'), (2, 'anti'), (4, 'nat'), (8, 'natAll'), (16, 'prog'), (32, 'other')]


def median_or_dash(values):
    return str(statistics.median(values)) if values else '-'


def bit_text(counter):
    return ','.join('%s=%d' % (name, counter[name]) for _, name in BITS if counter[name]) or '-'


def main():
    folder = sys.argv[1]
    seeds_seen = defaultdict(set)
    stats = defaultdict(lambda: {'wantQ': 0, 'blockQ': 0, 'feasQ': 0, 'stuckQ': 0, 'bits': defaultdict(int),
                                 'firstWant': [], 'firstBlock': [], 'unblock': []})
    decades = defaultdict(lambda: {'wantQ': 0, 'blockQ': 0, 'feasQ': 0, 'feasBits': defaultdict(int)})
    for path in sorted(glob.glob(folder + '/burblock_s*.tsv')):
        seed = path.rsplit('_s', 1)[-1].split('.')[0]
        per_nation_rows = defaultdict(list)
        with open(path, 'rb') as fh:
            for raw in fh:
                cols = raw.decode('utf-8').rstrip('\r\n').split('\t')
                if cols[0] == 'quarter':
                    continue
                per_nation_rows[cols[NATION]].append([int(cols[YEAR]), int(cols[BUR]), int(cols[WANT]), int(cols[BLOCK]), int(cols[FEAS])])
        for nation, rows in per_nation_rows.items():
            seeds_seen[nation].add(seed)
            st = stats[nation]
            first_want = first_block = reach_year = None
            for year, bur, want, block, feas in rows:
                dec = decades[year // 10 * 10]
                if want:
                    st['wantQ'] += 1
                    dec['wantQ'] += 1
                    if first_want is None:
                        first_want = year
                else:
                    st['stuckQ'] += 1
                if feas:
                    st['feasQ'] += 1
                    dec['feasQ'] += 1
                    for bit, name in BITS:
                        if feas & bit:
                            dec['feasBits'][name] += 1
                if block:
                    st['blockQ'] += 1
                    dec['blockQ'] += 1
                    if first_block is None:
                        first_block = year
                    for bit, name in BITS:
                        if block & bit:
                            st['bits'][name] += 1
                if first_block is not None and reach_year is None and want and not block and bur >= 1:
                    reach_year = year
            if first_want is not None:
                st['firstWant'].append(first_want)
            if first_block is not None:
                st['firstBlock'].append(first_block)
                if reach_year is not None:
                    st['unblock'].append(reach_year - first_block)
    lines = ['nation\tseeds\twantQ\tblockQ\tfeasQ\tstuckQ\tfirstWant\tfirstBlock\tunblockYears\treached\tbits']
    for nation in sorted(stats, key=lambda n: -stats[n]['blockQ']):
        st = stats[nation]
        lines.append('%s\t%d\t%d\t%d\t%d\t%d\t%s\t%s\t%s\t%d\t%s' % (
            nation, len(seeds_seen[nation]), st['wantQ'], st['blockQ'], st['feasQ'], st['stuckQ'], median_or_dash(st['firstWant']),
            median_or_dash(st['firstBlock']), median_or_dash(st['unblock']), len(st['unblock']), bit_text(st['bits'])))
    lines.append('')
    lines.append('decade\twantQ\tblockQ\tfeasQ\tfeasBits')
    for dec in sorted(decades):
        d = decades[dec]
        lines.append('%d\t%d\t%d\t%d\t%s' % (dec, d['wantQ'], d['blockQ'], d['feasQ'], bit_text(d['feasBits'])))
    text = '\n'.join(lines) + '\n'
    if len(sys.argv) > 2:
        with open(sys.argv[2], 'wb') as out:
            out.write(text.encode('utf-8'))
    else:
        sys.stdout.buffer.write(text.encode('utf-8'))


if __name__ == '__main__':
    main()
