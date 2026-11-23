# (1307) 読むだけ：urxrt_s（計器の 9 国・国×四半期×呼び手のファイル）から、PopDecision と Meritocracy の都市への純の押し合いを数える。
#   純＝urbanInK − urbanOutK（千人・正＝都市へ）。逆符号の国×四半期の数・打ち消し合った量 Σmin(|pd|,|me|)・粗の量 Σ(|pd|+|me|)。
#   州×四半期の帰属は今の生に無い（urxrt は国の和）＝この表は国×四半期の下限の読み。
#   呼び方：python pushpull.py <出力の tsv> <腕の名>=<生のフォルダ> ...
import sys, os, collections

def main():
    out = sys.argv[1]
    arms = [a.split('=', 1) for a in sys.argv[2:]]
    rows = []
    for name, d in arms:
        for s in range(1, 21):   # 種 1〜20（無い種は飛ばす）
            p = os.path.join(d, f'urxrt_s{s}.tsv')
            if not os.path.exists(p):
                continue
            net = collections.defaultdict(lambda: collections.defaultdict(float))
            gin = collections.defaultdict(float); gout = collections.defaultdict(float)
            with open(p, 'r', encoding='utf-8', newline='') as f:
                hdr = f.readline().rstrip('\r\n').split('\t')
                ix = {h: i for i, h in enumerate(hdr)}
                for line in f:
                    c = line.rstrip('\r\n').split('\t')
                    try:
                        q = int(c[ix['quarter']]); i_ = float(c[ix['urbanInK']]); o_ = float(c[ix['urbanOutK']])
                    except ValueError:
                        continue
                    rt = c[ix['route']]
                    net[(c[ix['nation']], q)][rt] += i_ - o_
                    gin[rt] += i_; gout[rt] += o_
            per = collections.defaultdict(lambda: [0, 0, 0.0, 0.0, 0.0, 0.0])  # nation -> [nq, opp, cancel, gross, pdsum, mesum]
            for (nat, q), m in net.items():
                pd = m.get('PopDecision', 0.0); me = m.get('Meritocracy', 0.0)
                for k in (nat, '*'):
                    a = per[k]
                    a[0] += 1
                    if pd * me < 0:
                        a[1] += 1; a[2] += min(abs(pd), abs(me))
                    a[3] += abs(pd) + abs(me); a[4] += pd; a[5] += me
            for k, a in sorted(per.items()):
                rows.append((name, s, k, a[0], a[1], round(a[2], 1), round(a[3], 1), round(a[4], 1), round(a[5], 1)))
            for rt in sorted(gin):
                rows.append((name, s, 'gross:' + rt, '', '', round(gin[rt], 1), round(gout[rt], 1), '', ''))
    with open(out, 'wb') as f:
        f.write(b'arm\tseed\tnation\tnq\toppSignNQ\tcancelK\tgrossK\tsumNetPD\tsumNetMe\n')
        for r in rows:
            f.write(('\t'.join(str(x) for x in r) + '\n').encode('utf-8'))

if __name__ == '__main__':
    main()
