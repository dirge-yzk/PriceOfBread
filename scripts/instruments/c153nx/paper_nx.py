# (1429) c153nx 紙の台本：基線の生の wexit_s から、旗 NoContactCollapseBlank を 1 にした時に
# 〈接触なし（累計の押しの和 0）かつ旧の崩壊の側あり・まだ終わりの述語に入っていない〉の月が何本の戦争に出るかを数える。
# 旗 0（何もしない実装）の門の値も同じ表から出す＝母集合の月にその月で終わった割合（旗 0 は終わらない月が残る）。
# 使い方：python paper_nx.py <生のフォルダ> [種の数]
import sys, os, collections

def rows_of(path):
    with open(path, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t')
    ix = {n: i for i, n in enumerate(h)}
    out = []
    for l in lines[1:]:
        r = l.split('\t')
        if len(r) != len(h):
            continue
        out.append(r)
    return ix, out

def runs(ix, rows):
    # 戦争＝同じ (powerIdA,powerIdB) の行で月が 1 ずつ続く列
    by = collections.defaultdict(list)
    for r in rows:
        by[(r[ix['powerIdA']], r[ix['powerIdB']])].append(r)
    out = []
    for k, lst in by.items():
        lst.sort(key=lambda r: int(r[ix['month']]))
        cur = [lst[0]]
        for r in lst[1:]:
            if int(r[ix['month']]) == int(cur[-1][ix['month']]) + 1:
                cur.append(r)
            else:
                out.append(cur); cur = [r]
        out.append(cur)
    return out

def main():
    d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    tot = collections.Counter()
    print('seed\twars\tpopRows\tpopRowsEnded0\tpopWarsIntl\tpopWarsCivil\tendedAtFirstPop0\tcontRun1950\tmedianExtraMonths\tmaxExtraMonths\totherEndG3')
    for s in range(1, ns + 1):
        p = os.path.join(d, f'wexit_s{s}.tsv')
        if not os.path.exists(p):
            print(f'{s}\tMISSING'); continue
        ix, rows = rows_of(p)
        lastm = max(int(r[ix['month']]) for r in rows)
        rs = runs(ix, rows)
        popRows = popEnded0 = intl = civ = endedFirst = cont = otherEnd = 0
        extra = []
        for run in rs:
            first = None
            for j, r in enumerate(run):
                col = r[ix['oldCollapseAFlag']] == '1' or r[ix['oldCollapseBFlag']] == '1'
                nocontact = r[ix['pushSumPositiveFlag']] == '0'
                oldEnd = r[ix['oldDecisiveFlag']] == '1' or r[ix['oldWearyFlag']] == '1' or r[ix['forcedFlag']] == '1'
                if not (col and nocontact):
                    if (j == len(run) - 1) and int(r[ix['month']]) < lastm and not oldEnd:
                        otherEnd += 1   # G3（記録）：母集合の外で、旧の終わりの述語に入らないのに終わった月
                    continue
                popRows += 1
                endsHere = (j == len(run) - 1) and int(r[ix['month']]) < lastm
                if endsHere:
                    popEnded0 += 1
                if first is None:
                    first = j
            if first is None:
                continue
            if run[first][ix['civilFlag']] == '1':
                civ += 1
            else:
                intl += 1
                if first == len(run) - 1 and int(run[first][ix['month']]) < lastm:
                    endedFirst += 1
                extra.append(len(run) - 1 - first)
                if int(run[-1][ix['month']]) == lastm:
                    cont += 1
        extra.sort()
        med = extra[len(extra) // 2] if extra else 0
        mx = extra[-1] if extra else 0
        print(f'{s}\t{len(rs)}\t{popRows}\t{popEnded0}\t{intl}\t{civ}\t{endedFirst}\t{cont}\t{med}\t{mx}\t{otherEnd}')
        for k, v in (('popRows', popRows), ('popEnded0', popEnded0), ('intl', intl), ('civ', civ), ('endedFirst', endedFirst), ('cont', cont), ('otherEnd', otherEnd)):
            tot[k] += v
    print('total\t' + '\t'.join(f'{k}={v}' for k, v in tot.items()))
    if tot['popRows']:
        print(f'旗0の門の値（母集合の月にその月で終わった割合）= {tot["popEnded0"]}/{tot["popRows"]} = {tot["popEnded0"]/tot["popRows"]:.4f}')

if __name__ == '__main__':
    main()
