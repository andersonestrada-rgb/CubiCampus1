path=r'A:\Proyectos\Unity\CubiCampus1\Assets\Scripts\MenuManager.cs'
with open(path,'r',encoding='utf-8',errors='ignore') as f:
    s=f.read()
# borrar desde public void CambiarTutorial hasta antes de public void SetVolumen
i=s.find('public void CambiarTutorial')
k=s.find('public void SetVolumen')
if i!=-1 and k!=-1 and k>i:
    s2=s[:i]+s[k:]
    with open(path,'w',encoding='utf-8') as f: f.write(s2)
    print('ok')
else:
    print('skip')
