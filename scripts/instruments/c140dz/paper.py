# (1338) 紙の台本：旧の式と新の式を同じ盤面（乱数 10^6 個）で比べ、ビットの食い違いを数える（予言 0）。
import random, struct
random.seed(1338)
def bits(x): return struct.pack('<d', x)
n = 0; bad = {'1+a*1.0': 0, 'a*b*1.0': 0, 's+0*m': 0, 'size*(1+0*sh)': 0}
for _ in range(1_000_000):
    a = random.uniform(0, 3) * 10 ** random.randint(-6, 6); b = random.uniform(0, 5); s = random.uniform(0, 5e4) * (random.random() < 0.9); sh = random.random(); k = random.uniform(0, 2)
    if bits(1 + a * 1.0) != bits(1 + a): bad['1+a*1.0'] += 1
    if bits(a * (1.0 + k) * 1.0) != bits(a * (1.0 + k)): bad['a*b*1.0'] += 1
    if bits(s + 0.0 * 0.0) != bits(s): bad['s+0*m'] += 1
    if bits(b * (1.0 + 0.0 * sh)) != bits(b): bad['size*(1+0*sh)'] += 1
print(bad)
