# (946) c83hs 紙の台本 2：(1) 29 州の紙（a）の経路が基線の経路に重なる四半期（以後は厳密に同じ＝力学の差が消える）
#   と、全期間で紙（a）だけが 0 に届く州があるか。(2) 開幕の 10 年の分離の建国の中身（送り手・州・四半期・前の支配度・格・人口）。
# 使い方: python -X utf8 -I paper946b.py <world.json> <keep_dir> <out_dir>
# 限界：paper946.py と同じ（年の末だけの国は margin×4・最初の分離の事象の後の紙は厳密でない）。
import sys, os, json
from collections import defaultdict, Counter
SPEED, MATURE, START = 0.025, 30.0, 1783

def clamp01(x):
    if not (x > 0): return 0.0
    return 1.0 if x > 1 else x

def main():
    wp, keep, outd = sys.argv[1], sys.argv[2], sys.argv[3]
    w = json.load(open(wp, encoding='utf-8'))
    tg = {}
    for n in w['nations']:
        for p in n['provinces']:
            if 'heldSince' in p:
                hs = p['heldSince']; tg[(n['name'], p['city'])] = 1.0 if hs <= 0 else clamp01((START - hs) / MATURE)
    L = []
    for arm in ['base_ins', 'n1', 'n2']:
        conv = defaultdict(list); only_paper = []
        early = []
        for s in range(1, 11):
            ser = defaultdict(list)
            with open(os.path.join(keep, arm, f'secession_s{s}.tsv'), encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in f:
                    a = line.rstrip('\n').split('\t'); key = (a[ix['nation']], a[ix['region']])
                    if key in tg:
                        try: ser[key].append((int(a[ix['quarter']]), float(a[ix['marginIndex']]), float(a[ix['dominanceShare']])))
                        except ValueError: pass
            for key, d0 in tg.items():
                xs = ser.get(key, [])
                if not xs: continue
                step = 4 if xs[0][0] == 3 else 1
                d = d0; qc = None; hitp = None
                prevq = None
                for (q, m, db) in xs:
                    if prevq is not None and q - prevq != step: break   # 持ち主が替わって戻った等の途切れ＝そこで読むのをやめる
                    prevq = q
                    d = clamp01(d + m * SPEED * step)
                    if qc is None and abs(d - db) < 1e-12: qc = q
                    if hitp is None and not (d > 0) and db > 0: hitp = q
                    if qc is not None: break
                conv[key].append(qc if qc is not None else -1)
                if hitp is not None: only_paper.append((s, key, hitp))
            # 開幕の 10 年の分離の建国
            with open(os.path.join(keep, arm, f'cederule_s{s}.tsv'), encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in f:
                    a = line.rstrip('\n').split('\t')
                    if a[ix['how']] != 'Seceded': continue
                    q = int(a[ix['quarter']])
                    if q >= 40: continue
                    early.append((s, q, a[ix['sender']], a[ix['region']], a[ix['tierBefore']], a[ix['dominanceBeforeShare']], a[ix['popK']]))
        L.append(f'## arm={arm}')
        L.append('# 29 州のうち d0<1 の州：紙（a）の経路が基線に重なる四半期（種ごと・-1＝重ならないうちに途切れた）')
        for key, d0 in sorted(tg.items(), key=lambda kv: kv[1]):
            if d0 < 1: L.append(f'{key[0]}\t{key[1]}\td0={d0:.3f}\t' + ','.join(map(str, conv[key])))
        L.append(f'# 紙（a）だけが 0 に届いた（基線は >0）州×種：{len(only_paper)} {only_paper[:10]}')
        L.append(f'# 開幕の 10 年の分離の建国：{len(early)} 件・送り手の数 {Counter(e[2] for e in early).most_common()}')
        L.append(f'#   格 {Counter(e[4] for e in early).most_common()}・1 年以内 {sum(1 for e in early if e[1] < 4)}・前の支配度 0 {sum(1 for e in early if not (float(e[5]) > 0))}')
        for e in sorted(early)[:60]:
            L.append('\t'.join(map(str, e)))
    open(os.path.join(outd, 'paper946b.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')

if __name__ == '__main__':
    main()
