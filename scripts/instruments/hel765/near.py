import pickle,sys
vbW,vbH=2752.766,1537.631
lon=lambda x:-166.4+x/vbW*360
lat=lambda y:98-y/vbH*198
P=pickle.load(open('svgpts.pkl','rb'))
cc=sys.argv[1];a,b,c,d=map(float,sys.argv[2:6])
for i,poly in enumerate(P[cc]):
    s=[(round(lon(x),3),round(lat(y),3)) for x,y in poly if a<=lon(x)<=b and c<=lat(y)<=d]
    if s: print(i,len(poly),s)
