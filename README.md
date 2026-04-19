# Lab7 - Întrebări Conceptuale

### 1. De ce Logout este implementat ca `<form method="post">` și nu ca un link `<a href="/Auth/Logout">`?

Dacă am folosi un simplu link de tip GET pentru logout, ar fi super nesigur pentru că aplicația ar rămâne vulnerabilă la atacuri CSRF (Cross-Site Request Forgery). Mai exact, un site dubios ar putea ascunde undeva un tag de imagine gen `<img src="https://site-ul-nostru.ro/Auth/Logout">`, iar când ai intra acolo, browserul tău ar face automat acel request în fundal și te-ar deloga de la noi fără ca tu să vrei. Pe lângă asta, browserele moderne mai fac câte un "prefetch" pe link-uri ca să se încarce paginile mai repede, deci iar te-ai putea trezi delogat din greșeală. Folosind un request POST (printr-un formular care nu e afișat efectiv, doar butonul de submit), te asiguri că acțiunea este 100% intenționată și ajută enorm sistemul anti-CSRF al framework-ului.

### 2. De ce login-ul face doi pași în loc de unul?

```csharp
var user = await _userManager.FindByEmailAsync(model.Email);
var result = await _signInManager.PasswordSignInAsync(user.UserName!, ...);
```

Pentru că în ASP.NET Core Identity, autentificarea internă (metoda `PasswordSignInAsync`) are nevoie de `UserName`, nu direct de adresa de email (chiar dacă noi teoretic la înregistrare i-am zis să le seteze fix la fel). Din punctul de vedere al bazei de date, `Email` și `UserName` sunt două câmpuri separate. Așa că trebuie să o luăm pas cu pas: prima oară căutăm contul omului în baza de date după email-ul pe care l-a băgat în formular (`FindByEmailAsync`), iar abia după ce i-am găsit contul, extragem de acolo username-ul aferent și rulăm verificarea oficială a parolei pentru a-i seta cookie-ul de login.

### 3. De ce nu este suficient să ascunzi butoanele Edit/Delete în View?

Când scriem `@if (User.Identity.IsAuthenticated)` în View, noi practic doar ascundem butoanele din interfață pentru un UX mai curat (să nu le vadă vizitatorii anonimi). Dar asta nu blochează pe bune backend-ul. Dacă cineva știe sau doar ghicește URL-ul de rutare (cum ar fi `/Articles/Delete/5`), e prea ușor să facă un request manual (din Postman sau modificând în Inspector direct butonul de submit) și ne va șterge articolul.

Și viceversa e o problemă: dacă securizăm treaba perfect în Controller cu `[Authorize]`, dar lăsăm butoanele la fel de vizibile pentru toată lumea în View, aplicația ar fi sigură tehnic, dar utilizatorii ar fi frustrați. Practic ei ar vedea tot timpul opțiunile de Edit și Delete, iar când ar da click s-ar trezi aruncați pe o pagină de Access Denied sau cu o eroare. De asta facem și ascunderea estetică în frontend, dar și bariera reală în backend.

### 4. Ce este middleware pipeline-ul în ASP.NET Core?

Pipeline-ul e efectiv fluxul (lanțul) prin care trece orice request HTTP de la client către server. Când vine o cerere nouă din browser, e luată la mână de o serie de module mai mici (middleware-uri). Orice middleware din lanț ba face o procesare mică și pasează mingea mai departe, ba blochează cererea cu totul și răspunde înapoi cu o eroare (sau un fișier cerut).

E vital ca `UseAuthentication()` să fie mereu pus înaintea `UseAuthorization()` în `Program.cs`. 
- **Autentificarea** este cea care verifică *cine este* cel ce face request-ul (se uită pe cookie și zice "acesta este contul Ion"). 
- **Autorizarea** vine după și vede *dacă are voie* Ion să facă acea pagină, accesând rolurile lui.
Dacă ne-am trezi să le punem pe dos, Auth-ul (autorizarea) s-ar executa prima, ar vedea ca zona e cu acces restrâns și, pentru că ea încă nu știe cine ești (neevaluând cookie-ul), și-ar seta o presupunere că ești un simplu vizitator anonim și ți-ar da acces interzis. Evident, o greșeală stupidă care te-ar ține deoparte chiar dacă tu ești corect logat.

### 5. Ce am fi trebuit să implementăm manual dacă nu foloseam ASP.NET Core Identity?

Dacă nu foloseam sistemul lor gata pus la punct, trebuia să reinventăm singuri roata de la 0 și riscam super tare să o dăm în bară la elementele sensibile de securitate:
- Trebuia să hash-uim noi manual parolele, să ne batem capul cu librării de criptografie și "salting" la crearea conturilor.
- Să creăm de mână toate cele vreo 7 tabele din baza de date pentru useri, roluri și relația dintre ele (cine e admin, cine e vizitator).
- Să ne scriem singuri codul corect pentru procesarea de cookie-uri HTTP-only, să le emitem la login, să rescriem sesiunile de timeout.
- Să facem partea funcțională de "Remember me" cu log-out corect.
- Să implementăm un sistem de timeout ("lockout") care să înghețe conturile atunci când detectează că un bot tot încearcă parole la întâmplare pe o adresă de email.

### 6. Care sunt dezavantajele folosirii ASP.NET Core Identity?

Totuși, Identity nu e glonțul de argint pentru toate proiectele, are și niște zone gri:
- E un pic prea uriaș ("overkill") pentru aplicații mititele. Dacă tu ai pe site doar 2 conturi statice de admin ca sa postezi niște articole, Identity oricum vine cu tot arsenalul lui, generându-ți o groază de tabele goale nefolositoare și cod în background, ceea ce doar poluează structura simplă pe care ți-o doreai.
- Te ține foarte strâns legat de ecosistemul Entity Framework Core și baza de date relațională pe care lucrezi (gen MS SQL). De îndată ce vrei să schimbi regulile jocului (să mergi spre o DB de MongoDB/Firebase sau să desprinzi framework-ul într-o arhitectură mult mai rafinată de tip Clean Architecture), îți va da mari dureri de cap pentru că e greu să-i adaptezi interfețele din spate ("custom stores").
- Deoarece soluția clasică Identity este menită pentru tipul standard de aplicații cu View-uri și Cookie-uri MVC, va fi destul de multă bătaie de cap să schimbi la token-uri (JWT). Practic, e incomod dacă pe viitor ai vrea să folosești fix acest login pe o aplicație dedicată de telefon sau pe un frontend total separat în Angular/React, necesitând ori alte dependențe masive (precum framework-ul Duende IdentityServer), ori setări laborioase ale opțiunilor sale abia recent introduse ca API.
