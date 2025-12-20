# FitnessCenter – Web Programlama Proje Ödevi

Bu proje, **Sakarya Üniversitesi Bilgisayar Mühendisliği Web Programlama dersi** kapsamında geliştirilmiş bir **Spor Salonu (Fitness Center) Yönetim ve Randevu Sistemi**dir.  
Uygulama, ASP.NET Core MVC mimarisi kullanılarak geliştirilmiş olup rol tabanlı yetkilendirme, randevu motoru, REST API ve yapay zekâ entegrasyonu içermektedir.

---

## 📌 Projenin Amacı

Bu projenin amacı; bir veya birden fazla spor salonunun:
- Şube yönetimi  
- Eğitmen ve hizmet tanımları  
- Üyelik ve randevu süreçleri  
- Takvim tabanlı randevu takibi  
- Yapay zekâ destekli kişisel antrenman ve beslenme önerileri  

gibi işlemlerinin **tek bir web sistemi üzerinden** yönetilmesini sağlamaktır.

---

## 🛠️ Kullanılan Teknolojiler

- **Framework:** ASP.NET Core 8.0 MVC  
- **Dil:** C#  
- **ORM:** Entity Framework Core (Code-First)  
- **Veritabanı:** SQL Server (LocalDB)  
- **Kimlik Doğrulama:** ASP.NET Core Identity  
- **Yetkilendirme:** Policy tabanlı Authorization  
- **Arayüz:** Bootstrap 5, jQuery  
- **Takvim:** FullCalendar.js  
- **REST API:** ASP.NET Core Web API + LINQ  
- **API Dokümantasyonu:** Swagger (Development ortamında)

---

## 🧠 Yapay Zekâ Entegrasyonu

Projede üç farklı yapay zekâ servisi entegre edilmiştir:

### 1️⃣ Groq Vision API
- Kullanıcının yüklediği fotoğrafı analiz eder  
- Fotoğrafta insan olup olmadığını kontrol eder  
- Fiziksel özellikler hakkında özet üretir  

### 2️⃣ DeepSeek API
- Fotoğraf analizi veya kullanıcı ölçü bilgilerini kullanır  
- Türkçe olarak:
  - Haftalık antrenman planı  
  - Beslenme önerileri  
  üretir  

### 3️⃣ OpenAI Image API
- Image-to-image yöntemi ile çalışır  
- Kullanıcının “before” fotoğrafını referans alır  
- Hedefe göre (kilo verme / kaslanma vb.) **after görseli** üretir  

---

## 👥 Kullanıcı Rolleri

Sistem dört ana rol içermektedir:

- **Admin:**  
  Tüm sistem yönetimi (şubeler, eğitmenler, üyeler, randevular, destek talepleri)

- **Member (Üye):**  
  Üyelik, randevu oluşturma, takvim görüntüleme ve AI modülü kullanımı

- **Trainer (Eğitmen):**  
  Kendi randevularını ve mesajlarını yönetme

- **BranchManager (Şube Müdürü):**  
  Yalnızca kendi şubesine ait yönetim işlemleri

Yetkilendirme işlemleri `Policy` yapısı ile uygulanmıştır.

---

## 📅 Randevu Sistemi

- Üyeler yalnızca **aktif üyelikleri bulunan şubelerden** randevu alabilir  
- Eğitmen uygunluğu:
  - Şube bilgisi  
  - Hizmet yetkinliği  
  - Müsaitlik saatleri  
  - Çakışan randevu kontrolü  
- Randevular **Beklemede / Onaylandı / İptal** durumlarına sahiptir  
- Admin ve BranchManager randevuları onaylayabilir  

---

## 🔗 REST API ve LINQ Filtreleme

Projenin belirli bölümlerinde REST API kullanılmıştır.  
API üzerinden LINQ sorguları ile filtreleme yapılmaktadır:

- Uygun eğitmenleri getirme  
- Üyenin randevularını tarih ve duruma göre listeleme  

Swagger arayüzü development ortamında aktiftir.

---

## 🧩 Sistem Mimarisi

- **Controllers:** İş akışları  
- **Models / Entities:** Veritabanı modelleri  
- **Data / Context:** DbContext, Migration, Seed  
- **Services:** AI servisleri ve yardımcı sınıflar  
- **Views:** Razor Pages  
- **Areas:** Admin, Trainer ve BranchManager panelleri  

---

## 👤 Geliştirici Bilgileri

- **Ad Soyad:** Selim Altın  
- **Bölüm:** Bilgisayar Mühendisliği 
- **Ders:** Web Programlama  
- **Üniversite:** Sakarya Üniversitesi  
- **Danışman:** Öğr. Gör. Dr. Ahmet Şanslı  

---

## 📂 GitHub

🔗 **Proje Bağlantısı:**  
https://github.com/SelimAltn/FitnessCenter

---

## 📝 Not

Bu proje, Web Programlama dersi kapsamında **bireysel** olarak geliştirilmiş olup,  
rol tabanlı yetkilendirme, randevu motoru, REST API ve çoklu yapay zekâ entegrasyonu içeren **kapsamlı bir web uygulamasıdır**.
