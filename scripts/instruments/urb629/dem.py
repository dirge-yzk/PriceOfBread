import sys
f=sys.argv[1]; step=int(sys.argv[2])
i=0
print('yr  size   sizeShare birthShare deathShare births deaths net(b-d) fill')
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    i+=1
    if i%step!=1: continue
    sz=float(c[1]);rk=float(c[2]);mbk=float(c[3]);mp=float(c[4]);dws=float(c[5]);mdw=float(c[6]);tb=float(c[7]);dr=float(c[8]);rec=c[9];net=float(c[10]);fill=float(c[11])
    bs=rk/mbk if mbk>0 else 0; ds=dws/mdw if mdw>0 else 0
    births=(net*bs if rec=='True' else tb*bs); deaths=0 if rec=='True' else dr*dws
    print('%4d %7.1f %.4f %.4f %.4f %7.3f %7.3f %+7.3f %.2f %s'%(1783+(i-1)//12,sz,sz/mp,bs,ds,births,deaths,births-deaths,fill,rec))
