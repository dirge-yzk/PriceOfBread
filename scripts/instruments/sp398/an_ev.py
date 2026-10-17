import json, sys, collections
p = sys.argv[1]; top = int(sys.argv[2]) if len(sys.argv) > 2 else 50
d = json.load(open(p, 'rb'))
frames = [f['name'] for f in d['shared']['frames']]
def run(profiles):
    incl = collections.Counter(); excl = collections.Counter(); tot = 0.0
    for pr in profiles:
        stack = []; last = pr['startValue']
        for ev in pr['events']:
            t = ev['at']; dt = t - last; last = t
            if stack and dt > 0:
                excl[stack[-1]] += dt
                for fi in set(stack): incl[fi] += dt
                tot += dt
            if ev['type'] == 'O': stack.append(ev['frame'])
            else:
                if stack: stack.pop()
    return incl, excl, tot
main = [pr for pr in d['profiles'] if pr['name'] == d['profiles'][0]['name']]
work = [pr for pr in d['profiles'][1:]]
for label, profs in (('MAIN', main), ('WORKERS', work)):
    incl, excl, tot = run(profs)
    print(f'===== {label}  total busy ms {tot:.0f}')
    print('--- exclusive')
    for fi, w in excl.most_common(top): print(f'{w/1000:8.1f}s {100*w/tot:5.1f}% {frames[fi][:140]}')
    print('--- inclusive')
    for fi, w in incl.most_common(top * 2): print(f'{w/1000:8.1f}s {100*w/tot:5.1f}% {frames[fi][:140]}')
