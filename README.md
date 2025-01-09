Nesneye Yönelik Programlama Projesi - Hisse Senedi Borsa Sistemi Konsol Uygulaması

# Borsa Sistemi
Bu borsa sistemi projesi, kullanıcı hesaplarının yönetimini ve hisse alım-satımı, portföy yönetimi gibi gerçek bir borsa sisteminde gerçekleşebilecek işlemleri simüle etmeyi amaçlamaktadır.

## Kullanım
İstediğiniz işlemi seçeneklerin numarasını yazarak gerçekleştirebilirsiniz.

### Yönetici İşlemleri
- Yönetici olarak giriş yaptıktan sonra:
  - Hisse ekleyebilir, düzenleyebilir veya silebilirsiniz.
  - Mevcut kullanıcıların listesini görüntüleyebilirsiniz.
  - Müşteri hesaplarını kaldırabilirsiniz, ancak yönetici hesapları kaldırılamaz.

### Müşteri İşlemleri
- Müşteri olarak giriş yaptıktan sonra:
  - Hisse satın alabilir, hisse satabilir veya bakiye ekleyebilirsiniz.
  - Portföyünüzü görüntüleyebilir ve hesaplayabilirsiniz.
  - Hesabınızı silebilirsiniz.

## Varsayılan Veriler
- Varsayılan yönetici hesabı:
  - Kullanıcı Adı: `admin`
  - Şifre: `admin`
- Varsayılan hisseler:
  - `AAPL`, `GOOG`, `AMZN`, `TSLA` (örnek fiyatlarla)

## Teknik Detaylar

### Yönetim Sınıfları
Hesap oluşturma, kaldırma ve giriş yapmak gibi işlemler `AccountHandler` sınıfı ile yönetilmiştir.
`StockExchangeHandler` sınıfı ile ayrı bir borsa yönetimi mekanizması geliştirilmiştir.

### Soyutlama ve Kalıtım
Hesap `Account` soyut sınıfı ile müşteri `Customer` ve yönetici `Admin` alt sınıfları türetilmiştir.

### Enkapsülasyon
Şifrelenmiş parola `_passwordHash` gizli özelliğinde saklanmış ve şifreleme `HashPassword`, şifre doğrulama `VerifyPassword` metodları özel alanlarla korunmuştur.
Şifreleme için kullanılan algoritma oldukça basit ve pek koruma sağlamayan bir örnektir.

### Polimorfizm
Kullanıcı seçenekleri için `InitialiseMenu` soyut metodu, farklı hesap türelerinde farklı şekilde uygulanmıştır.
Ayrıca, bir çok yerde `Customer` veya `Admin` yerine üst sınıf olan `Account` kullanılması polimorfizme örnektir.

### Kütüphane ve Koleksiyon
Müşteri portföyü ve hisse fiyatları için `Dictionary`, mevcut hesaplar için `List` koleksiyonları oluşturulmuştur.
`System.Linq` ve `System.Collections` etkin bir şekilde kullanılmıştır.

### Kullanıcı Deneyimi
Konsol arayüzü, renk kodları ve düzenli mesajlarla kullanıcı dostu hale getirilmiştir. Konsol çıktıları net ve anlaşılırdır.

### Ek Özellikler
Task kütüphanesi; çoklu thread yapısı kullanılarak hisse fiyatları asenkron bir şekilde güncellenmektedir.
Basitleştirilmiş konsol kontrolü ve daha iyi kullanıcı arayüzü için `ConsoleHandler` sınıfı yapılmıştır.
