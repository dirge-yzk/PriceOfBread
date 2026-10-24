# -*- coding: utf-8 -*-
"""(536) ivf：計器の生の表を基線（m533_ins）と比べる。使い方：python -I cmp_ins.py <基線の ins> <新しい ins> [<増えた国>,...]
挙げる国を 9→12 に増やした（マラータ同盟・ネーデルラント・スペイン）ので、増えた国は毎四半期の行が増える（年末の行は基線にもある）＝両側から〈増えた国の名を含む行〉を除いて、残りの行の順と中身が同じかを見る。
同じバイトなら OK。違うファイルは、両側から増えた国の行を除いた後の行の一致を見る。
時間（秒）を書く表（burscan・clock）は揺れる＝比べない。ivf_s と基線に無いファイルは比べない（一覧に出す）。
⚠NATIONS を 9→12 に増やした走りでは、国ごとの積算が毎四半期の行で戻る国が増える＝増えた国が途中で別の国の領土だった州（cons_s など）は、行の中身が基線と違って見える（走りは同じ）。
  厳密に比べるときは基線と同じ NATIONS で 1 種を走らせる（追記 3 の chk9＝種 1・65 表がバイト一致・flow_s は呼び手の行番号の列だけ違う）。"""
import sys, os, hashlib

SKIP = {'burscan', 'clock'}   # 時間（秒）を書く表は揺れる

def sha(p):
    h = hashlib.sha256()
    with open(p, 'rb') as f:
        for ch in iter(lambda: f.read(1 << 22), b''):
            h.update(ch)
    return h.hexdigest()

def main():
    base, new = sys.argv[1], sys.argv[2]
    added = [s.encode('utf-8') for s in (sys.argv[3].split(',') if len(sys.argv) > 3 else ['マラータ同盟', 'ネーデルラント', 'スペイン'])]
    names = sorted(os.listdir(base))
    ok = 0; filt = 0; skipped = []; bad = []
    extra = sorted(set(os.listdir(new)) - set(names))
    for n in names:
        if n.split('_s')[0] in SKIP:
            skipped.append(n); continue
        pb, pn = os.path.join(base, n), os.path.join(new, n)
        if not os.path.exists(pn):
            bad.append((n, '新しい側に無い')); continue
        if os.path.getsize(pb) == os.path.getsize(pn) and sha(pb) == sha(pn):
            ok += 1; continue
        with open(pb, 'rb') as fb, open(pn, 'rb') as fn:
            ia = (l for l in fb if not any(a in l for a in added))
            ib = (l for l in fn if not any(a in l for a in added))
            good = True
            for x, y in zip(ia, ib):
                if x != y:
                    good = False; bad.append((n, f'行が違う: 基線 {x[:80]!r} 新 {y[:80]!r}')); break
            else:
                if next(ia, None) is not None or next(ib, None) is not None:
                    good = False; bad.append((n, '行数が違う'))
            if good: filt += 1
    print(f'一致（バイト）{ok}／増えた国の行を除いて一致 {filt}／不一致 {len(bad)}／揺れる表を除外 {len(skipped)}／基線に無い新しいファイル {extra}')
    for n, why in bad[:20]: print('  NG', n, why)

if __name__ == '__main__':
    main()
