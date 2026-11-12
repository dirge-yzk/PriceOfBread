#!/usr/bin/env python3
"""Noto Sans JP の可変字体（NotoSansJP-VF.ttf・wght 100〜900・既定 100）から、太さ 400 と 700 の静的な字体を作る。

理由：Avalonia は可変字体の軸を選べず、既定の太さ（100＝細字）でしか描けない。
標準ライブラリだけで書いた（外部の部品を取ってこない）。glyf/gvar の字体だけを扱う。
使い方：python make_static_instances.py <VF のパス> <出力のフォルダ>
出力：NotoSansJP-Regular.ttf（400）・NotoSansJP-Bold.ttf（700）。許諾は SIL Open Font License 1.1（元の字体と同じ）。
"""
import struct, sys, os


def u16(b, o): return struct.unpack_from('>H', b, o)[0]
def i16(b, o): return struct.unpack_from('>h', b, o)[0]
def u32(b, o): return struct.unpack_from('>I', b, o)[0]


def read_tables(data):
    n = u16(data, 4)
    t = {}
    for i in range(n):
        r = 12 + 16 * i
        tag = data[r:r + 4].decode('latin1')
        off, ln = u32(data, r + 8), u32(data, r + 12)
        t[tag] = data[off:off + ln]
    return t


def f2dot14(b, o): return struct.unpack_from('>h', b, o)[0] / 16384.0


def normalize(fvar, avar, value):
    ao = u16(fvar, 4)
    mn, df, mx = [struct.unpack_from('>i', fvar, ao + 4 + 4 * k)[0] / 65536.0 for k in range(3)]
    c = -(df - value) / (df - mn) if value < df else ((value - df) / (mx - df) if value > df else 0.0)
    c = max(-1.0, min(1.0, c))
    if avar:
        pc = u16(avar, 6)
        pairs = [(f2dot14(avar, 8 + 4 * k), f2dot14(avar, 10 + 4 * k)) for k in range(pc)]
        for k in range(1, len(pairs)):
            (a0, b0), (a1, b1) = pairs[k - 1], pairs[k]
            if a0 <= c <= a1:
                c = b0 if a1 == a0 else b0 + (b1 - b0) * (c - a0) / (a1 - a0)
                break
    return c


def unpack_points(d, o):
    c = d[o]; o += 1
    if c == 0: return None, o
    if c & 0x80:
        c = ((c & 0x7F) << 8) | d[o]; o += 1
    pts = []; last = 0
    while len(pts) < c:
        ctl = d[o]; o += 1
        run = (ctl & 0x7F) + 1
        for _ in range(run):
            if ctl & 0x80:
                last += u16(d, o); o += 2
            else:
                last += d[o]; o += 1
            pts.append(last)
    return pts, o


def unpack_deltas(d, o, n):
    out = []
    while len(out) < n:
        ctl = d[o]; o += 1
        run = (ctl & 0x3F) + 1
        if ctl & 0x80:
            out += [0] * run
        elif ctl & 0x40:
            for _ in range(run):
                out.append(i16(d, o)); o += 2
        else:
            for _ in range(run):
                v = d[o]; o += 1
                out.append(v - 256 if v > 127 else v)
    return out[:n], o


def iup_axis(orig, delta, touched, ends):
    """輪郭ごとに、触られていない点の差分を前後の触られた点から補間する（1 軸）。"""
    res = list(delta)
    start = 0
    for e in ends:
        idx = list(range(start, e + 1))
        start = e + 1
        tp = [i for i in idx if touched[i]]
        if not tp:
            for i in idx: res[i] = 0.0
            continue
        if len(tp) == 1:
            for i in idx: res[i] = delta[tp[0]]
            continue
        for k in range(len(tp)):
            a, b = tp[k], tp[(k + 1) % len(tp)]
            # a の次から b の前まで（輪郭の周回を含む）
            i = a + 1 if a + 1 <= e else idx[0]
            while i != b:
                x = orig[i]; xa, xb = orig[a], orig[b]; da, db = delta[a], delta[b]
                if xa == xb:
                    res[i] = da if da == db else 0.0
                else:
                    lo, hi, dlo, dhi = (xa, xb, da, db) if xa <= xb else (xb, xa, db, da)
                    if x <= lo: res[i] = dlo
                    elif x >= hi: res[i] = dhi
                    else: res[i] = dlo + (dhi - dlo) * (x - lo) / (hi - lo)
                i = i + 1 if i + 1 <= e else idx[0]
    return res


def parse_glyph(g):
    """simple: (contours, endPts, instr, flags, xs, ys) / composite: components"""
    if len(g) == 0: return None
    nc = i16(g, 0)
    if nc >= 0:
        ends = [u16(g, 10 + 2 * i) for i in range(nc)]
        o = 10 + 2 * nc
        il = u16(g, o); instr = g[o + 2:o + 2 + il]; o += 2 + il
        npts = ends[-1] + 1 if ends else 0
        flags = []
        while len(flags) < npts:
            f = g[o]; o += 1; flags.append(f)
            if f & 8:
                r = g[o]; o += 1; flags += [f] * r
        xs = []; v = 0
        for f in flags:
            if f & 2:
                dx = g[o]; o += 1; v += dx if f & 16 else -dx
            elif not f & 16:
                v += i16(g, o); o += 2
            xs.append(v)
        ys = []; v = 0
        for f in flags:
            if f & 4:
                dy = g[o]; o += 1; v += dy if f & 32 else -dy
            elif not f & 32:
                v += i16(g, o); o += 2
            ys.append(v)
        return ('s', ends, instr, flags, xs, ys)
    comps = []; o = 10
    while True:
        fl = u16(g, o); gi = u16(g, o + 2); o += 4
        if fl & 1:
            a1, a2 = i16(g, o), i16(g, o + 2); o += 4
        else:
            a1, a2 = struct.unpack_from('>bb', g, o); o += 2
        tr = b''
        if fl & 8: tr = g[o:o + 2]; o += 2
        elif fl & 0x40: tr = g[o:o + 4]; o += 4
        elif fl & 0x80: tr = g[o:o + 8]; o += 8
        comps.append([fl, gi, a1, a2, tr])
        if not fl & 0x20: break
    tail = g[o:]
    return ('c', comps, tail)


def build_simple(ends, instr, flags, xs, ys):
    if not xs:
        return b''
    out = bytearray(struct.pack('>hhhhh', len(ends), min(xs), min(ys), max(xs), max(ys)))
    for e in ends: out += struct.pack('>H', e)
    out += struct.pack('>H', len(instr)) + instr
    fb = bytearray(); xb = bytearray(); yb = bytearray()
    px = py = 0
    for f, x, y in zip(flags, xs, ys):
        nf = f & 0x41  # on-curve と overlap だけ残す
        dx, dy = x - px, y - py; px, py = x, y
        if dx == 0: nf |= 16
        elif -255 <= dx <= 255:
            nf |= 2 | (16 if dx > 0 else 0); xb.append(abs(dx))
        else: xb += struct.pack('>h', dx)
        if dy == 0: nf |= 32
        elif -255 <= dy <= 255:
            nf |= 4 | (32 if dy > 0 else 0); yb.append(abs(dy))
        else: yb += struct.pack('>h', dy)
        fb.append(nf)
    out += fb + xb + yb
    while len(out) % 4: out.append(0)
    return bytes(out)


def build_composite(comps, tail, bbox):
    out = bytearray(struct.pack('>hhhhh', -1, *bbox))
    for fl, gi, a1, a2, tr in comps:
        fl = fl | 1  # 引数は常に 16 ビット
        out += struct.pack('>HHhh', fl, gi, a1, a2) + tr
    out += tail
    while len(out) % 4: out.append(0)
    return bytes(out)


def instance(tables, wght):
    fvar, avar, gvar = tables['fvar'], tables.get('avar'), tables['gvar']
    c = normalize(fvar, avar, wght)
    head, maxp, hhea = tables['head'], tables['maxp'], tables['hhea']
    ng = u16(maxp, 4)
    loca_long = i16(head, 50) == 1
    loca = tables['loca']; glyf = tables['glyf']
    offs = [u32(loca, 4 * i) if loca_long else 2 * u16(loca, 2 * i) for i in range(ng + 1)]
    raw = [glyf[offs[i]:offs[i + 1]] for i in range(ng)]
    nh = u16(hhea, 34)
    hmtx = tables['hmtx']
    adv = [u16(hmtx, 4 * i) if i < nh else u16(hmtx, 4 * (nh - 1)) for i in range(ng)]
    lsb = [i16(hmtx, 4 * i + 2) if i < nh else i16(hmtx, 4 * nh + 2 * (i - nh)) for i in range(ng)]
    # gvar
    sc = u16(gvar, 6); sto = u32(gvar, 8); gc = u16(gvar, 12); fl = u16(gvar, 14); gdo = u32(gvar, 16)
    shared = [f2dot14(gvar, sto + 2 * k) for k in range(sc)]
    go = [u32(gvar, 20 + 4 * i) if fl & 1 else 2 * u16(gvar, 20 + 2 * i) for i in range(gc + 1)]

    new_glyphs = []
    new_adv = list(adv); new_lsb = list(lsb)
    bboxes = [None] * ng
    parsed = [parse_glyph(r) for r in raw]

    def deltas_for(gi, npts, orig, ends):
        dx = [0.0] * npts; dy = [0.0] * npts
        if go[gi + 1] == go[gi]: return dx, dy
        d = gvar[gdo + go[gi]: gdo + go[gi + 1]]
        tvc = u16(d, 0); dofs = u16(d, 2); o = 4
        has_shared_pts = bool(tvc & 0x8000); tvc &= 0x0FFF
        heads = []
        for _ in range(tvc):
            size = u16(d, o); ti = u16(d, o + 2); o += 4
            peak = None; st = en = None
            if ti & 0x8000: peak = f2dot14(d, o); o += 2
            else: peak = shared[ti & 0x0FFF]
            if ti & 0x4000:
                st = f2dot14(d, o); en = f2dot14(d, o + 2); o += 4
            heads.append((size, ti, peak, st, en))
        p = dofs
        shared_pts = None
        if has_shared_pts:
            shared_pts, p = unpack_points(d, p)
        for size, ti, peak, st, en in heads:
            seg_end = p + size
            if ti & 0x2000:
                pts, p = unpack_points(d, p)
            else:
                pts = shared_pts
            # 係数
            if st is None:
                st, en = (min(0.0, peak), max(0.0, peak))
            if peak == 0 or c == 0 and peak != 0 and False:
                scalar = 0.0
            elif c == peak: scalar = 1.0
            elif c <= st or c >= en: scalar = 0.0
            elif c < peak: scalar = (c - st) / (peak - st)
            else: scalar = (en - c) / (en - peak)
            if scalar == 0.0:
                p = seg_end; continue
            allp = pts is None
            cnt = npts if allp else len(pts)
            tx, p = unpack_deltas(d, p, cnt)
            ty, p = unpack_deltas(d, p, cnt)
            if allp:
                ax, ay = [float(v) for v in tx], [float(v) for v in ty]
            else:
                touched = [False] * npts
                ax = [0.0] * npts; ay = [0.0] * npts
                for k, pi in enumerate(pts):
                    if pi < npts:
                        touched[pi] = True; ax[pi] = float(tx[k]); ay[pi] = float(ty[k])
                ax = iup_axis(orig[0], ax, touched, ends)
                ay = iup_axis(orig[1], ay, touched, ends)
            for i in range(npts):
                dx[i] += ax[i] * scalar; dy[i] += ay[i] * scalar
            p = seg_end
        return dx, dy

    simple_xy = {}
    for gi in range(ng):
        g = parsed[gi]
        if g is None:
            # 空の字形：位相点だけ
            xmin = 0
            orig = ([0, adv[gi], 0, 0], [0, 0, 0, 0])
            dx, dy = deltas_for(gi, 4, orig, [0, 1, 2, 3])
            new_adv[gi] = max(0, round(adv[gi] + dx[1] - dx[0]))
            new_glyphs.append(b'')
            continue
        if g[0] == 's':
            _, ends, instr, flags, xs, ys = g
            n = len(xs)
            xmin = min(xs) if xs else 0
            ph = [xmin - lsb[gi], xmin - lsb[gi] + adv[gi], 0, 0]
            ox = xs + ph; oy = ys + [0, 0, 0, 0]
            allends = ends + [n, n + 1, n + 2, n + 3]
            dx, dy = deltas_for(gi, n + 4, (ox, oy), allends)
            nx = [x + round(dx[i]) for i, x in enumerate(xs)]
            ny = [y + round(dy[i]) for i, y in enumerate(ys)]
            new_adv[gi] = max(0, round(adv[gi] + dx[n + 1] - dx[n]))
            new_glyphs.append(build_simple(ends, instr, flags, nx, ny))
            if nx:
                bboxes[gi] = (min(nx), min(ny), max(nx), max(ny)); new_lsb[gi] = min(nx)
        else:
            new_glyphs.append(None)  # 後で
    # 合成字形（部品の字形の枠が確定してから）
    for gi in range(ng):
        if new_glyphs[gi] is not None: continue
        _, comps, tail = parsed[gi]
        n = len(comps)
        ox = [cm[2] for cm in comps] + [0, adv[gi], 0, 0]
        oy = [cm[3] for cm in comps] + [0, 0, 0, 0]
        dx, dy = deltas_for(gi, n + 4, (ox, oy), list(range(n + 4)))
        bb = None
        for k, cm in enumerate(comps):
            if cm[0] & 2:
                cm[2] += round(dx[k]); cm[3] += round(dy[k])
            cb = bboxes[cm[1]]
            if cb:
                t = (cb[0] + cm[2], cb[1] + cm[3], cb[2] + cm[2], cb[3] + cm[3]) if cm[0] & 2 else cb
                bb = t if bb is None else (min(bb[0], t[0]), min(bb[1], t[1]), max(bb[2], t[2]), max(bb[3], t[3]))
        bb = bb or (0, 0, 0, 0)
        bboxes[gi] = bb
        new_adv[gi] = max(0, round(adv[gi] + dx[n + 1] - dx[n]))
        new_lsb[gi] = bb[0]
        new_glyphs[gi] = build_composite(comps, tail, bb)

    # 再構築
    glyf_out = bytearray(); loca_out = bytearray()
    for g in new_glyphs:
        loca_out += struct.pack('>I', len(glyf_out)); glyf_out += g
    loca_out += struct.pack('>I', len(glyf_out))
    hm = bytearray()
    for i in range(nh): hm += struct.pack('>Hh', new_adv[i], new_lsb[i])
    for i in range(nh, ng): hm += struct.pack('>h', new_lsb[i])
    t = dict(tables)
    for k in ('fvar', 'gvar', 'avar', 'HVAR', 'MVAR', 'cvar', 'STAT'): t.pop(k, None)
    t['glyf'] = bytes(glyf_out); t['loca'] = bytes(loca_out); t['hmtx'] = bytes(hm)
    h = bytearray(head); struct.pack_into('>h', h, 50, 1)
    t['head'] = bytes(h)
    os2 = bytearray(t['OS/2']); struct.pack_into('>H', os2, 4, int(wght))
    fs = struct.unpack_from('>H', os2, 62)[0]
    fs = (fs | 0x20) & ~0x40 if wght >= 700 else (fs | 0x40) & ~0x20
    struct.pack_into('>H', os2, 62, fs)
    t['OS/2'] = bytes(os2)
    h = bytearray(t['head']); ms = struct.unpack_from('>H', h, 44)[0]
    ms = (ms | 1) if wght >= 700 else (ms & ~1)
    struct.pack_into('>H', h, 44, ms)
    t['head'] = bytes(h)
    return t


def write_font(tables, path):
    tags = sorted(tables)
    n = len(tags)
    es = 1
    while es * 2 <= n: es *= 2
    sr = es * 16
    out = bytearray(struct.pack('>IHHHH', 0x00010000, n, sr, es.bit_length() - 1, n * 16 - sr))
    off = 12 + 16 * n
    body = bytearray(); dirs = bytearray()
    csum_adj_pos = None
    for tag in tags:
        d = tables[tag]
        if tag == 'head':
            d = bytearray(d); struct.pack_into('>I', d, 8, 0); d = bytes(d)
        pad = d + b'\0' * (-len(d) % 4)
        cs = sum(struct.unpack('>%dI' % (len(pad) // 4), pad)) & 0xFFFFFFFF
        dirs += tag.encode('latin1') + struct.pack('>III', cs, off + len(body), len(d))
        if tag == 'head': csum_adj_pos = len(body) + 8
        body += pad
    out += dirs + body
    total = sum(struct.unpack('>%dI' % (len(out) // 4), bytes(out) + b'\0' * (-len(out) % 4))) & 0xFFFFFFFF
    adj = (0xB1B0AFBA - total) & 0xFFFFFFFF
    struct.pack_into('>I', out, off + csum_adj_pos, adj)
    with open(path, 'wb') as f: f.write(out)


def main():
    src, dst = sys.argv[1], sys.argv[2]
    tables = read_tables(open(src, 'rb').read())
    for w, name in ((400, 'NotoSansJP-Regular.ttf'), (700, 'NotoSansJP-Bold.ttf')):
        write_font(instance(tables, w), os.path.join(dst, name))
        print('wrote', name)


if __name__ == '__main__':
    main()
