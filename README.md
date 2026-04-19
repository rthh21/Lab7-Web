# Lab7-Web - Întrebări Conceptuale

### 1. De ce Logout este implementat ca `<form method="post">` și nu ca un link `<a href="/Auth/Logout">`?

Dacă delogarea s-ar face printr-un simplu request `GET` folosind un link, aplicația ar fi vulnerabilă la atacuri de tip CSRF (Cross-Site Request Forgery). Un atacator ar putea, de exemplu, să ascundă un tag de imagine `<img src="https://domeniul-tau.com/Auth/Logout">` pe un alt site malițios, iar atunci când vizitezi acel site, browserul tău ar face automat un request GET către acea adresă, delogându-te fără intenția ta. În plus, browserele moderne pot face "prefetching" (preîncărcare) pe link-uri pentru a optimiza viteza de navigare, ceea ce ar duce la delogări accidentale. Un request de tip `POST` necesită o acțiune explicită (trimiterea unui formular) și include de obicei un token anti-CSRF, oferind un strat puternic de securitate.

### 2. De ce login-ul face doi pași în loc de unul?

```csharp
var user = await _userManager.FindByEmailAsync(model.Email);
var result = await _signInManager.PasswordSignInAsync(user.UserName!, ...);
```

Metoda `PasswordSignInAsync` din ASP.NET Core Identity se bazează pe `UserName` (numele de utilizator) ca identificator unic pentru autentificarea internă, și nu direct pe `Email`. În Identity, `UserName` și `Email` sunt două proprietăți diferite care nu trebuie neapărat să fie identice (deși adesea la înregistrare i se asignează adresa de email și câmpului `UserName`). Prin urmare, sistemul trebuie mai întâi să găsească utilizatorul apelând baza de date printr-o căutare după adresa de email introdusă (`FindByEmailAsync`), iar apoi să extragă explicit `UserName`-ul persoanei respective pentru a verifica parola și a efectua mecanismul de validare completă prin `PasswordSignInAsync`.

### 3. De ce nu este suficient să ascunzi butoanele Edit/Delete în View?

Avem `@if (User.Identity.IsAuthenticated)` în View exclusiv pentru ca aplicația să fie „user-friendly” (UX bun). Ascunderea butoanelor din interfața vizuală nu oferă protecție reală per se; este pur și simplu un filtru estetic. Dacă ne-am baza doar pe view, un utilizator rău intenționat ar putea deduce sau ghici URL-ul acțiunii (ex: `/Articles/Delete/5`) și ar putea trimite o cerere HTTP direct (prin browser, Postman, cURL etc.) modificând baza de date. Deci trebuie validat și în Controller - acela este "poarta" și protecția principală (`[Authorize]` și `.IsOwnerOrAdmin()`).

Dacă, în schimb, am securiza rutele în Controller, dar *nu* am ascunde butoanele din View: din perspectiva securității lucrurile ar fi sigure (actul malițios ar fi blocat). Dar, din perspectiva utilizatorului, UX-ul ar fi groaznic. Un vizitator oarecare ar vedea opțiunile de Editare și Ștergere, ar da click pe ele crezând că le poate folosi, doar pentru a fi enervat de mesaje subite gen "Access Denied / 403 Forbidden" sau de o redirecționare forțată către pagina de Login.

### 4. Ce este middleware pipeline-ul în ASP.NET Core?

Pipeline-ul de middleware din ASP.NET Core este practic linia de asamblare a unei cereri HTTP. În momentul în care aplicația primește o cerere de la browser, ea trece liniar printr-un lanț de componente denumite middleware-uri. Fiecare componentă are flexibilitatea și puterea de a lucra cu acea cerere, de a opri execuția și de a da un răspuns imediat, sau de a o modifica și pasa următoarei componente. La întoarcerea răspunsului, fluxul se parcurge și în direcție inversă.

Ordinea este vitală! `UseAuthentication()` trebuie să se apeleze înaintea `UseAuthorization()` deoarece ele îndeplinesc scopuri complet diferite:
- **Authentication**: Află „*Cine* ești tu?”. Se uită pe cerere, verifică cookie-ul/token-ul și îți atașează "viza" de identitate pe cerere.
- **Authorization**: Află „*Ai permisiunea* să intri aici?”. 

Dacă am inversa aceste apeluri, Autorizarea s-ar executa prima, ar vedea că resursa e protejată, s-ar uita cine o accesează și, ignorând cookie-urile (pentru că nu le-a evaluat Autentificarea încă), ar zice „Nu te cunosc, cerere anonimă!” și ți-ar refuza accesul instant. Chiar dacă ești un utilizator logat, funcționalitățile protejate nu te vor lăsa să treci.

### 5. Ce am fi trebuit să implementăm manual dacă nu foloseam ASP.NET Core Identity?

Dacă am fi realizat totul "from scratch", viața noastră de dezvoltatori ar fi fost extrem de grea deoarece am fi luat asupra noastră și o marjă enormă de risc pe partea de atacuri cibernetice. Am fi trebuit să scriem logică pentru:
- Hash-uirea și "sărarea" (salting) criptografică a parolelor la crearea contului, și compararea sigură a re-hash-ului la logare (ex: lucrul manual cu biblioteci BCrypt sau PBKDF2).
- Proiectarea bazei de date cap-coadă, la nivel de tabele de Useri, de Roluri (Manager/Admin/Client) și de relații Role-Claims (gestiunea permisiunilor - RBAC).
- Generarea, setarea, extragerea și validarea sesiunilor via Cookie-uri HTTP sigure (HttpOnly, Secure) – inclusiv opțiunea esențială de "Remember Me" care să persiste între redeschideri ale browser-ului.
- Implementarea unor mecanisme defensive contra brute-force: interzicerea logărilor temporar ("Account Lockout") dacă introduci parola greșit de x ori.
- Fluxul complex necesitat de o uitare a parolei sau confirmarea contului (generat de link-uri/token-uri sigure și unice trimise pe email ce au un timp de expirare alocat).

### 6. Care sunt dezavantajele folosirii ASP.NET Core Identity?

Dezavantajele principale ale includerii framework-ului pre-construit prevăd în principal constrângeri și opulențe arhitecturale:
- **Este destul de greu ("Overkill")**: Creează rapid nu mai puțin de 7+ tabele in baza de date (`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims` etc.). Pentru o aplicație cu un domeniu foarte restrâns (ex: un blog cu 1-2 admini fixați manual și niciun client inregistrat), integrarea tot a acestui sistem este pur și simplu nejustificată și mărește inutil mărimea și timpul de inițializare al soluției.
- **Cuplare strânsă**: Arhitectura Identity funcționează perfect cu un ecosistem Entity Framework Core + O bază de date relațională (SQL Server, SQLite etc). Dacă vrei să renunți la ORM sau să folosești o bază de date non-relațională (ex. MongoDB), efortul de a modifica Identity să cunoască noile tale interfețe („Custom Identity Stores”) este extenuant. În același fel, poate genera dificultăți în respectarea principiilor din Clean Architecture unde Domain Driven Design interesează excluderea dependențelor de framework în nucleul aplicației.
- **Arhitectură tradițională Client-Server orientată pe Razor/MVC**: Făcând lucrarea grea prin cookie-uri, implementările mai vechi de .NET Identity presupuneau dureri masive de cap atunci când aveai nevoie de token-uri pentru un Single Page Application (Angular/React) disociate complet de backend, ori pentru o aplicație de telefon. Atunci aveai nevoie de alte extensii masive terțe ce necesitau alte bătăi de cap ca IdentityServer (Duende). (Desi in .NET 8, prin API Endpoints integrati direct, acest gol a început să fie redus, rămâne în continuare mai anevoios de modelat și extins decât abordarea MVC default).
