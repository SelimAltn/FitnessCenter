# FitnessCenter — Spor Salonu Yönetim & Randevu Sistemi (ASP.NET Core MVC)

Bu proje, Web Programlama dersi kapsamında **ASP.NET Core MVC** kullanılarak geliştirilmiş bir **Fitness Center Yönetim ve Randevu Sistemi**dir.  
Sistem; **üyelik giriş/kayıt**, **rol bazlı yetkilendirme**, **Hizmet–Eğitmen–Müsaitlik–Randevu** yönetimi ve **LINQ filtreli REST API** uç noktaları ile çalışır.

---

## ✅ Projede Yapılanlar (Özet)

### 1) Kimlik Doğrulama & Rol Bazlı Yetki
- Login / Register / Logout akışı eklendi.
- Navbar ve ekranlar rol bazlı gösterildi.
- Policy tabanlı koruma uygulandı (örn. `MemberOnly`).
- Varsayılan **Admin hesabı ve roller** seed edildi.

### 2) Veritabanı & EF Core
- Entity Framework Core kuruldu.
- İlk migration alındı ve veritabanı şeması oluşturuldu.
- Temel tablolar: Hizmet, Eğitmen, Müsaitlik, Randevu (ve ilişkiler).

### 3) CRUD Modülleri + Doğrulama
- **Hizmet** CRUD (listele/ekle/güncelle/sil).
- **Eğitmen** CRUD (listele/ekle/güncelle/sil).
- Form doğrulamaları ve temel validasyonlar eklendi.

### 4) Randevu Motoru (Appointment Engine)
- Eğitmen **müsaitlik saatleri** üzerinden randevu alma altyapısı.
- Randevu oluştururken:
  - çakışma (overlap) kontrolü
  - tarih/saat uygunluğu kontrolü
  - minimum ara kuralı (arka arkaya randevu engeli) gibi validasyonlar
- “Randevularım” sayfası üzerinden üyenin randevuları listelenir.

### 5) REST API (LINQ Filtreli) + Sayfalama + ProblemDetails
- Projenin en az bir kısmında veritabanı iletişimi **REST API** ile sağlandı.
- API tarafında LINQ sorguları ile filtreleme yapıldı.
- Sayfalama (pagination) eklendi.
- Hata sözleşmesi için **ProblemDetails** yapısı kullanıldı.

### 6) API Authorization + Swagger
- API uç noktalarına Authorization eklendi.
- Swagger dokümantasyonu projeye eklendi ve API’ler Swagger üzerinden test edilebilir hale getirildi.

---

## 👥 Roller

### Admin
- Email: `ogrencinumarasi@sakarya.edu.tr`
- Şifre: `sau`

### Üye (Member)
- Register sayfasından oluşturulur.
- Üye ekranları policy ile korunur (örn. `MemberOnly`).

---

## 🔌 Örnek API Uç Noktaları

Aşağıdaki senaryolar LINQ filtreleme ile desteklenir:
- Belirli bir tarihte uygun eğitmenleri getirme
- Üyenin randevularını getirme (`Randevularım` sayfasında kullanıldı)
- Listeleme işlemlerinde sayfalama

> Not: Endpoint adları projedeki controller’lara göre değişebilir.

---

## 🧰 Kullanılan Teknolojiler
- ASP.NET Core MVC (C#)
- Entity Framework Core + LINQ
- SQL Server / PostgreSQL (connection string’e göre)
- Bootstrap 5, HTML, CSS, JavaScript

---

## 🚀 Kurulum & Çalıştırma

1) Projeyi klonla
```bash
git clone <repo-link>
cd <proje-klasoru>
````

2. Veritabanını hazırla

* `appsettings.json` içindeki connection string’i düzenle
* Migration’ları uygula:

```bash
dotnet ef database update
```

3. Çalıştır

```bash
dotnet run
```

4. Tarayıcıdan aç

* `https://localhost:<port>/`

---

## 📌 Proje Notları

* Admin paneli ve üye ekranları rol/policy ile ayrılmıştır.
* Randevu oluşturma sürecinde çakışma ve uygunluk kontrolleri yapılır.
* Swagger üzerinden API test edilebilir.


## 📄 Lisans

Bu proje, Sakarya Üniversitesi Bilgisayar Mühendisliği Bölümü  
**Web Programlama** dersi kapsamında **Selim Altın** tarafından geliştirilmiştir.

Proje, **akademik ve eğitim amaçlıdır**.  
İzinsiz ticari kullanım veya kopyalanması uygun değildir.
